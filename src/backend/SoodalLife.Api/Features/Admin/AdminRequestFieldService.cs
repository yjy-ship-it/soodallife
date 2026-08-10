using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminRequestFieldService(SoodalLifeDbContext dbContext)
{
    private static readonly string[] SupportedInputTypes =
        ["ADDRESS", "DATETIME", "FILE", "LONG_TEXT", "MONEY", "NUMBER", "PERIOD", "RECURRENCE", "SELECT", "TEXT"];
    private static readonly string[] DefinitionStatusCodes = ["ACTIVE", "INACTIVE"];

    public async Task<AdminRequestFieldSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var inputTypeCodes = await dbContext.CategoryFieldDefinitions.AsNoTracking()
            .Select(field => field.FieldTypeCode)
            .ToListAsync(cancellationToken);
        var inputTypes = inputTypeCodes
            .GroupBy(code => code, StringComparer.Ordinal)
            .Select(group => new AdminRequestFieldTypeCountResponse(group.Key, group.LongCount()))
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ToArray();

        return new AdminRequestFieldSummaryResponse(
            await dbContext.CategoryFieldDefinitions.LongCountAsync(cancellationToken),
            await dbContext.CategoryFieldAssignments.LongCountAsync(cancellationToken),
            inputTypes);
    }

    public async Task<IReadOnlyList<AdminRequestFieldResponse>?> GetFieldsAsync(
        Guid servicePublicId,
        CancellationToken cancellationToken)
    {
        var service = await GetServiceAsync(servicePublicId, cancellationToken);
        if (service?.ParentId is null) return null;

        var middleAssignments = await dbContext.CategoryFieldAssignments.AsNoTracking()
            .Where(assignment => assignment.TargetCategoryId == service.ParentId)
            .ToListAsync(cancellationToken);
        var serviceAssignments = await dbContext.CategoryFieldAssignments.AsNoTracking()
            .Where(assignment => assignment.TargetCategoryId == service.Id)
            .ToListAsync(cancellationToken);
        var fieldIds = middleAssignments.Select(assignment => assignment.FieldDefinitionId)
            .Concat(serviceAssignments.Select(assignment => assignment.FieldDefinitionId))
            .Distinct()
            .ToArray();
        var fields = await dbContext.CategoryFieldDefinitions.AsNoTracking()
            .Where(field => fieldIds.Contains(field.Id))
            .ToListAsync(cancellationToken);
        var options = await dbContext.CategoryFieldOptions.AsNoTracking()
            .Where(option => fieldIds.Contains(option.FieldDefinitionId))
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Id)
            .ToListAsync(cancellationToken);
        var servicesInMiddle = await dbContext.ServiceCategories.AsNoTracking()
            .CountAsync(category => category.ParentId == service.ParentId && category.LevelCode == "SERVICE", cancellationToken);

        return fields
            .Select(field => BuildResponse(
                field,
                serviceAssignments.SingleOrDefault(assignment => assignment.FieldDefinitionId == field.Id),
                middleAssignments.SingleOrDefault(assignment => assignment.FieldDefinitionId == field.Id),
                options.Where(option => option.FieldDefinitionId == field.Id),
                servicesInMiddle))
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.Label, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<AdminRequestFieldResponse?> GetFieldAsync(
        Guid servicePublicId,
        Guid fieldPublicId,
        CancellationToken cancellationToken)
    {
        var context = await GetFieldContextAsync(servicePublicId, fieldPublicId, tracking: false, cancellationToken);
        return context is null ? null : BuildResponse(
            context.Field,
            context.ServiceAssignment,
            context.MiddleAssignment,
            context.Options,
            context.ServicesInMiddle);
    }

    public async Task<AdminRequestFieldResponse> UpdateDefinitionAsync(
        Guid servicePublicId,
        Guid fieldPublicId,
        UpdateAdminRequestFieldDefinitionRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var context = await GetFieldContextAsync(servicePublicId, fieldPublicId, tracking: true, cancellationToken) ?? throw NotFound();
        if (context.ServicesInMiddle > 1 && !request.ConfirmSharedChange)
        {
            throw new AdminServiceCategoryException(
                "ADMIN_REQUEST_FIELD_SHARED_CONFIRMATION_REQUIRED",
                $"이 질문 정의는 같은 중분류의 서비스 {context.ServicesInMiddle}곳에 공통 적용됩니다. 영향 범위를 확인해 주세요.",
                StatusCodes.Status409Conflict);
        }

        var inputType = NormalizeCode(request.InputType, SupportedInputTypes, "ADMIN_REQUEST_FIELD_TYPE_INVALID", "지원하지 않는 입력유형입니다.");
        var statusCode = NormalizeCode(request.StatusCode, DefinitionStatusCodes, "ADMIN_REQUEST_FIELD_STATUS_INVALID", "질문 상태를 확인해 주세요.");
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        var before = DefinitionSnapshot(context.Field);

        context.Field.Label = request.Label.Trim();
        context.Field.FieldTypeCode = inputType;
        context.Field.UnitText = inputType == "SELECT" ? null : NullIfEmpty(request.Unit);
        context.Field.ValidationRuleText = request.ValidationRule?.Trim() ?? string.Empty;
        context.Field.StatusCode = statusCode;
        context.Field.UpdatedAt = now;
        context.Field.UpdatedByUserId = actorUserId;

        AddAuditLog(actorUserId, "REQUEST_FIELD_DEFINITION_UPDATED", "CATEGORY_FIELD_DEFINITION", context.Field.PublicId, before, DefinitionSnapshot(context.Field), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetFieldAsync(servicePublicId, fieldPublicId, cancellationToken))!;
    }

    public async Task<AdminRequestFieldResponse> UpdateAssignmentAsync(
        Guid servicePublicId,
        Guid fieldPublicId,
        UpdateAdminRequestFieldAssignmentRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var context = await GetFieldContextAsync(servicePublicId, fieldPublicId, tracking: true, cancellationToken) ?? throw NotFound();
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        var effective = context.ServiceAssignment ?? context.MiddleAssignment!;
        var before = AssignmentSnapshot(effective, context.ServiceAssignment is null ? "MIDDLE_DEFAULT" : "SERVICE_OVERRIDE");

        if (request.DisplayOrder != effective.DisplayOrder)
        {
            await ShiftServiceDisplayOrderAsync(context, effective.DisplayOrder, request.DisplayOrder, actorUserId, now, cancellationToken);
        }

        var assignment = EnsureServiceAssignment(context, effective, actorUserId, now);
        assignment.IsActive = request.IsActive;
        assignment.IsRequired = request.IsRequired;
        assignment.DisplayOrder = request.DisplayOrder;
        assignment.UpdatedAt = now;
        assignment.UpdatedByUserId = actorUserId;

        AddAuditLog(
            actorUserId,
            "REQUEST_FIELD_ASSIGNMENT_UPDATED",
            "CATEGORY_FIELD_ASSIGNMENT",
            context.Field.PublicId,
            before,
            AssignmentSnapshot(assignment, "SERVICE_OVERRIDE"),
            now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetFieldAsync(servicePublicId, fieldPublicId, cancellationToken))!;
    }

    public async Task<AdminRequestFieldResponse> UpdateOptionAsync(
        Guid servicePublicId,
        Guid fieldPublicId,
        Guid optionPublicId,
        UpdateAdminRequestFieldOptionRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var context = await GetFieldContextAsync(servicePublicId, fieldPublicId, tracking: true, cancellationToken) ?? throw NotFound();
        var option = context.Options.SingleOrDefault(candidate => candidate.PublicId == optionPublicId)
            ?? throw new AdminServiceCategoryException(
                "ADMIN_REQUEST_FIELD_OPTION_NOT_FOUND",
                "선택값을 찾을 수 없습니다.",
                StatusCodes.Status404NotFound);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        var before = OptionSnapshot(option);

        if (request.DisplayOrder != option.DisplayOrder)
        {
            var affected = request.DisplayOrder < option.DisplayOrder
                ? context.Options.Where(candidate => candidate.Id != option.Id && candidate.DisplayOrder >= request.DisplayOrder && candidate.DisplayOrder < option.DisplayOrder)
                : context.Options.Where(candidate => candidate.Id != option.Id && candidate.DisplayOrder > option.DisplayOrder && candidate.DisplayOrder <= request.DisplayOrder);
            foreach (var candidate in affected)
            {
                candidate.DisplayOrder += request.DisplayOrder < option.DisplayOrder ? 1 : -1;
                candidate.UpdatedAt = now;
                candidate.UpdatedByUserId = actorUserId;
            }
        }

        option.Label = request.Label.Trim();
        option.DisplayOrder = request.DisplayOrder;
        option.IsActive = request.IsActive;
        option.UpdatedAt = now;
        option.UpdatedByUserId = actorUserId;
        AddAuditLog(actorUserId, "REQUEST_FIELD_OPTION_UPDATED", "CATEGORY_FIELD_OPTION", option.PublicId, before, OptionSnapshot(option), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetFieldAsync(servicePublicId, fieldPublicId, cancellationToken))!;
    }

    private async Task<FieldContext?> GetFieldContextAsync(
        Guid servicePublicId,
        Guid fieldPublicId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var service = await GetServiceAsync(servicePublicId, cancellationToken);
        if (service?.ParentId is null) return null;

        IQueryable<CategoryFieldDefinition> fieldQuery = dbContext.CategoryFieldDefinitions;
        IQueryable<CategoryFieldAssignment> assignmentQuery = dbContext.CategoryFieldAssignments;
        IQueryable<CategoryFieldOption> optionQuery = dbContext.CategoryFieldOptions;
        if (!tracking)
        {
            fieldQuery = fieldQuery.AsNoTracking();
            assignmentQuery = assignmentQuery.AsNoTracking();
            optionQuery = optionQuery.AsNoTracking();
        }

        var field = await fieldQuery.SingleOrDefaultAsync(candidate => candidate.PublicId == fieldPublicId, cancellationToken);
        if (field is null) return null;
        var assignments = await assignmentQuery
            .Where(assignment => assignment.FieldDefinitionId == field.Id &&
                (assignment.TargetCategoryId == service.Id || assignment.TargetCategoryId == service.ParentId))
            .ToListAsync(cancellationToken);
        var serviceAssignment = assignments.SingleOrDefault(assignment => assignment.TargetCategoryId == service.Id);
        var middleAssignment = assignments.SingleOrDefault(assignment => assignment.TargetCategoryId == service.ParentId);
        if (serviceAssignment is null && middleAssignment is null) return null;

        var options = await optionQuery
            .Where(option => option.FieldDefinitionId == field.Id)
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Id)
            .ToListAsync(cancellationToken);
        var servicesInMiddle = await dbContext.ServiceCategories.AsNoTracking()
            .CountAsync(category => category.ParentId == service.ParentId && category.LevelCode == "SERVICE", cancellationToken);
        return new FieldContext(service, field, serviceAssignment, middleAssignment, options, servicesInMiddle);
    }

    private async Task ShiftServiceDisplayOrderAsync(
        FieldContext selectedContext,
        int oldOrder,
        int newOrder,
        long actorUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var service = selectedContext.Service;
        var middleAssignments = await dbContext.CategoryFieldAssignments
            .Where(assignment => assignment.TargetCategoryId == service.ParentId)
            .ToListAsync(cancellationToken);
        var serviceAssignments = await dbContext.CategoryFieldAssignments
            .Where(assignment => assignment.TargetCategoryId == service.Id)
            .ToListAsync(cancellationToken);
        var fieldIds = middleAssignments.Select(assignment => assignment.FieldDefinitionId)
            .Concat(serviceAssignments.Select(assignment => assignment.FieldDefinitionId))
            .Distinct()
            .ToArray();
        var fields = await dbContext.CategoryFieldDefinitions.AsNoTracking()
            .Where(field => fieldIds.Contains(field.Id))
            .ToListAsync(cancellationToken);

        foreach (var field in fields.Where(field => field.Id != selectedContext.Field.Id))
        {
            var serviceAssignment = serviceAssignments.SingleOrDefault(assignment => assignment.FieldDefinitionId == field.Id);
            var middleAssignment = middleAssignments.SingleOrDefault(assignment => assignment.FieldDefinitionId == field.Id);
            var effective = serviceAssignment ?? middleAssignment;
            if (effective is null) continue;
            var shouldShift = newOrder < oldOrder
                ? effective.DisplayOrder >= newOrder && effective.DisplayOrder < oldOrder
                : effective.DisplayOrder > oldOrder && effective.DisplayOrder <= newOrder;
            if (!shouldShift) continue;

            var context = new FieldContext(service, field, serviceAssignment, middleAssignment, [], selectedContext.ServicesInMiddle);
            var assignment = EnsureServiceAssignment(context, effective, actorUserId, now);
            assignment.DisplayOrder += newOrder < oldOrder ? 1 : -1;
            assignment.UpdatedAt = now;
            assignment.UpdatedByUserId = actorUserId;
        }
    }

    private CategoryFieldAssignment EnsureServiceAssignment(
        FieldContext context,
        CategoryFieldAssignment effective,
        long actorUserId,
        DateTime now)
    {
        if (context.ServiceAssignment is not null) return context.ServiceAssignment;
        var assignment = new CategoryFieldAssignment
        {
            FieldDefinitionId = context.Field.Id,
            TargetCategoryId = context.Service.Id,
            ScopeCode = "SERVICE",
            IsActive = effective.IsActive,
            IsRequired = effective.IsRequired,
            DisplayOrder = effective.DisplayOrder,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            UpdatedAt = now,
            UpdatedByUserId = actorUserId,
        };
        dbContext.CategoryFieldAssignments.Add(assignment);
        return assignment;
    }

    private Task<ServiceCategory?> GetServiceAsync(Guid publicId, CancellationToken cancellationToken) =>
        dbContext.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.PublicId == publicId && category.LevelCode == "SERVICE", cancellationToken);

    private async Task<long> GetActorUserIdAsync(Guid publicId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.PublicId == publicId && user.StatusCode == "ACTIVE")
            .Select(user => (long?)user.Id)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);

    private void AddAuditLog(long actorUserId, string actionCode, string entityType, Guid entityPublicId, string before, string after, DateTime now) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorRoleCode = RoleCodes.Admin,
            ActionCode = actionCode,
            EntityType = entityType,
            EntityPublicId = entityPublicId,
            ResultCode = "SUCCESS",
            BeforeJson = before,
            AfterJson = after,
        });

    private static AdminRequestFieldResponse BuildResponse(
        CategoryFieldDefinition field,
        CategoryFieldAssignment? serviceAssignment,
        CategoryFieldAssignment? middleAssignment,
        IEnumerable<CategoryFieldOption> options,
        int servicesInMiddle)
    {
        var effective = serviceAssignment ?? middleAssignment;
        var optionResponses = options.OrderBy(option => option.DisplayOrder).ThenBy(option => option.Id)
            .Select(option => new AdminRequestFieldOptionResponse(option.PublicId, option.Value, option.Label, option.DisplayOrder, option.IsActive))
            .ToArray();
        return new AdminRequestFieldResponse(
            field.PublicId,
            field.Label,
            field.FieldTypeCode,
            effective?.IsRequired ?? false,
            effective?.DisplayOrder ?? 0,
            field.StatusCode,
            effective?.IsActive ?? false,
            serviceAssignment is null ? "MIDDLE_DEFAULT" : "SERVICE_OVERRIDE",
            optionResponses,
            field.UnitText,
            field.ValidationRuleText,
            optionResponses.Length > 0,
            !string.IsNullOrWhiteSpace(field.ValidationRuleText),
            middleAssignment is null ? 1 : servicesInMiddle);
    }

    private static string DefinitionSnapshot(CategoryFieldDefinition field) => JsonSerializer.Serialize(new
    {
        field.Label,
        field.FieldTypeCode,
        field.UnitText,
        field.ValidationRuleText,
        field.StatusCode,
    });

    private static string AssignmentSnapshot(CategoryFieldAssignment assignment, string scope) => JsonSerializer.Serialize(new
    {
        Scope = scope,
        assignment.IsActive,
        assignment.IsRequired,
        assignment.DisplayOrder,
    });

    private static string OptionSnapshot(CategoryFieldOption option) => JsonSerializer.Serialize(new
    {
        option.Value,
        option.Label,
        option.DisplayOrder,
        option.IsActive,
    });

    private static string NormalizeCode(string value, IReadOnlyCollection<string> allowed, string businessCode, string message)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return allowed.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : throw new AdminServiceCategoryException(businessCode, message);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdminServiceCategoryException NotFound() => new(
        "ADMIN_REQUEST_FIELD_NOT_FOUND",
        "해당 서비스에 배정된 요청항목을 찾을 수 없습니다.",
        StatusCodes.Status404NotFound);

    private sealed record FieldContext(
        ServiceCategory Service,
        CategoryFieldDefinition Field,
        CategoryFieldAssignment? ServiceAssignment,
        CategoryFieldAssignment? MiddleAssignment,
        List<CategoryFieldOption> Options,
        int ServicesInMiddle);
}
