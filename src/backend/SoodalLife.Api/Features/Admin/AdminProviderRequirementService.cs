using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderRequirementService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminProviderRequirementListResponse?> GetPoliciesAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;

        var policies = await dbContext.CategoryOperationPolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == serviceId)
            .OrderByDescending(policy => policy.EffectiveFrom)
            .ThenByDescending(policy => policy.Id)
            .ToListAsync(cancellationToken);
        var responses = await BuildResponsesAsync(policies, cancellationToken);
        return new AdminProviderRequirementListResponse(
            responses,
            responses.FirstOrDefault(policy => policy.IsCurrentlyEffective)?.Id,
            responses.Count > 1,
            true,
            true,
            true);
    }

    public async Task<AdminProviderRequirementResponse?> GetCurrentAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var policy = await dbContext.CategoryOperationPolicies.AsNoTracking()
            .Where(item => item.CategoryId == serviceId && item.IsActive && item.EffectiveFrom <= today
                && (item.EffectiveTo == null || item.EffectiveTo > today))
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return policy is null ? null : (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminProviderRequirementResponse?> GetPolicyAsync(
        Guid servicePublicId,
        Guid policyPublicId,
        CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var policy = await dbContext.CategoryOperationPolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == serviceId && item.PublicId == policyPublicId, cancellationToken);
        return policy is null ? null : (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminProviderRequirementResponse> CreateAssignmentAsync(Guid servicePublicId, Guid policyPublicId, SaveAdminCategoryProviderRequirementRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        Validate(request);
        var policy = await GetPolicyEntityAsync(servicePublicId, policyPublicId, cancellationToken);
        var definition = await dbContext.ProviderRequirementDefinitions.SingleOrDefaultAsync(item => item.PublicId == request.RequirementDefinitionId && item.IsActive, cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_DEFINITION_NOT_FOUND", "활성 요건 정의를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (await dbContext.CategoryProviderRequirementAssignments.AnyAsync(item => item.CategoryOperationPolicyId == policy.Id && item.RequirementDefinitionId == definition.Id, cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_ASSIGNMENT_DUPLICATED", "이 정책버전에 같은 요건이 이미 연결되어 있습니다.", StatusCodes.Status409Conflict);
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        var entity = new CategoryProviderRequirementAssignment { CategoryOperationPolicyId = policy.Id, RequirementDefinitionId = definition.Id, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        Apply(entity, request); dbContext.CategoryProviderRequirementAssignments.Add(entity);
        AddAudit(actor, "PROVIDER_REQUIREMENT_ASSIGNED", entity.PublicId, null, Snapshot(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminProviderRequirementResponse> UpdateAssignmentAsync(Guid servicePublicId, Guid policyPublicId, Guid assignmentPublicId, SaveAdminCategoryProviderRequirementRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        Validate(request);
        var policy = await GetPolicyEntityAsync(servicePublicId, policyPublicId, cancellationToken);
        var entity = await dbContext.CategoryProviderRequirementAssignments.SingleOrDefaultAsync(item => item.PublicId == assignmentPublicId && item.CategoryOperationPolicyId == policy.Id, cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_ASSIGNMENT_NOT_FOUND", "서비스 요건 연결을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var requestedDefinitionId = await dbContext.ProviderRequirementDefinitions.Where(item => item.PublicId == request.RequirementDefinitionId).Select(item => (long?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (requestedDefinitionId != entity.RequirementDefinitionId) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_DEFINITION_IMMUTABLE", "연결된 요건 정의는 변경할 수 없습니다. 기존 연결을 비활성화하고 새 요건을 추가해 주세요.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var before = Snapshot(entity);
        Apply(entity, request); entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = actor;
        AddAudit(actor, entity.IsActive ? "PROVIDER_REQUIREMENT_ASSIGNMENT_UPDATED" : "PROVIDER_REQUIREMENT_ASSIGNMENT_DEACTIVATED", entity.PublicId, before, Snapshot(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminProviderRequirementResponse> ReplaceEvidenceAsync(Guid servicePublicId, Guid policyPublicId, Guid assignmentPublicId, ReplaceAdminCategoryProviderRequirementEvidenceRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        if (request.EvidenceTypes.GroupBy(item => item.DocumentTypeId).Any(group => group.Count() > 1)) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_EVIDENCE_DUPLICATED", "같은 증빙유형을 중복 연결할 수 없습니다.");
        if (request.EvidenceTypes.Any(item => item.DisplayOrder < 0)) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_DISPLAY_ORDER_INVALID", "표시순서는 0 이상이어야 합니다.");
        var policy = await GetPolicyEntityAsync(servicePublicId, policyPublicId, cancellationToken);
        var assignment = await dbContext.CategoryProviderRequirementAssignments.SingleOrDefaultAsync(item => item.PublicId == assignmentPublicId && item.CategoryOperationPolicyId == policy.Id, cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_ASSIGNMENT_NOT_FOUND", "서비스 요건 연결을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var requestedIds = request.EvidenceTypes.Select(item => item.DocumentTypeId).ToArray();
        var documentTypes = await dbContext.ProviderDocumentTypes.Where(item => requestedIds.Contains(item.PublicId) && item.IsActive).ToListAsync(cancellationToken);
        if (documentTypes.Count != requestedIds.Length) throw new AdminServiceCategoryException("ADMIN_PROVIDER_DOCUMENT_TYPE_NOT_FOUND", "활성 증빙유형을 확인해 주세요.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        var existing = await dbContext.CategoryProviderRequirementEvidenceTypes.Where(item => item.RequirementAssignmentId == assignment.Id).ToListAsync(cancellationToken);
        var before = JsonSerializer.Serialize(existing.Select(item => new { item.DocumentTypeId, item.IsRequired, item.DisplayOrder }));
        dbContext.CategoryProviderRequirementEvidenceTypes.RemoveRange(existing);
        foreach (var item in request.EvidenceTypes)
        {
            var type = documentTypes.Single(documentType => documentType.PublicId == item.DocumentTypeId);
            dbContext.CategoryProviderRequirementEvidenceTypes.Add(new CategoryProviderRequirementEvidenceType { RequirementAssignmentId = assignment.Id, DocumentTypeId = type.Id, IsRequired = item.IsRequired, DisplayOrder = item.DisplayOrder, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
        }
        AddAudit(actor, "PROVIDER_REQUIREMENT_EVIDENCE_REPLACED", assignment.PublicId, before, JsonSerializer.Serialize(request));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminProviderRequirementResponse> UpdateOperationPolicyAsync(Guid servicePublicId, Guid policyPublicId,
        SaveAdminOperationPolicyRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ValidateOperationPolicy(request.QualificationAndLicenseRequirement, request.InsuranceRequirement, request.SafetyGradeCode);
        var policy = await GetPolicyEntityAsync(servicePublicId, policyPublicId, cancellationToken); EnsureCurrent(policy);
        var token = ParseRowVersion(request.RowVersion); if (!policy.RowVersion.SequenceEqual(token)) throw OperationPolicyConflict();
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var before = OperationSnapshot(policy); var now = DateTime.UtcNow;
        ApplyOperationPolicy(policy, request.QualificationAndLicenseRequirement, request.InsuranceRequirement, request.SafetyGradeCode);
        policy.UpdatedAt = now; policy.UpdatedByUserId = actor; dbContext.Entry(policy).Property(value => value.RowVersion).OriginalValue = token;
        AddOperationAudit(actor, "CATEGORY_OPERATION_POLICY_UPDATED", policy, before, OperationSnapshot(policy));
        try { await dbContext.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw OperationPolicyConflict(); }
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<AdminProviderRequirementResponse>> UpdateMiddleOperationPoliciesAsync(Guid middlePublicId,
        SaveAdminMiddleOperationPolicyRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ValidateOperationPolicy(request.QualificationAndLicenseRequirement, request.InsuranceRequirement, request.SafetyGradeCode);
        if (request.Services is null || request.Services.Count == 0) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_TARGET_REQUIRED", "일괄 적용할 하위 서비스를 선택해 주세요.");
        if (request.Services.Select(value => value.ServiceId).Distinct().Count() != request.Services.Count || request.Services.Select(value => value.PolicyId).Distinct().Count() != request.Services.Count)
            throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_TARGET_DUPLICATED", "중복된 하위 서비스 정책이 포함되어 있습니다.");
        var middleId = await dbContext.ServiceCategories.AsNoTracking().Where(value => value.PublicId == middlePublicId && value.LevelCode == "MIDDLE").Select(value => (long?)value.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_MIDDLE_CATEGORY_NOT_FOUND", "중분류를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var policyIds = request.Services.Select(value => value.PolicyId).ToArray();
        var rows = await (from category in dbContext.ServiceCategories join policy in dbContext.CategoryOperationPolicies on category.Id equals policy.CategoryId
                          where category.ParentId == middleId && policyIds.Contains(policy.PublicId)
                          select new { Category = category, Policy = policy }).ToListAsync(cancellationToken);
        if (rows.Count != request.Services.Count || request.Services.Any(target => !rows.Any(row => row.Category.PublicId == target.ServiceId && row.Policy.PublicId == target.PolicyId)))
            throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_TARGET_MISMATCH", "선택한 서비스의 운영정책이 최신 상태가 아닙니다. 새로고침 후 다시 시도해 주세요.", StatusCodes.Status409Conflict);
        var tokens = new Dictionary<Guid, byte[]>(); foreach (var target in request.Services) tokens[target.PolicyId] = ParseRowVersion(target.RowVersion);
        if (rows.Any(row => !row.Policy.RowVersion.SequenceEqual(tokens[row.Policy.PublicId]))) throw OperationPolicyConflict();
        foreach (var row in rows) EnsureCurrent(row.Policy);
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var before = OperationSnapshot(row.Policy); var token = tokens[row.Policy.PublicId];
            ApplyOperationPolicy(row.Policy, request.QualificationAndLicenseRequirement, request.InsuranceRequirement, request.SafetyGradeCode);
            row.Policy.UpdatedAt = now; row.Policy.UpdatedByUserId = actor; dbContext.Entry(row.Policy).Property(value => value.RowVersion).OriginalValue = token;
            AddOperationAudit(actor, "CATEGORY_OPERATION_POLICY_BULK_UPDATED", row.Policy, before, OperationSnapshot(row.Policy));
        }
        try { await dbContext.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw OperationPolicyConflict(); }
        return await BuildResponsesAsync(rows.OrderBy(row => row.Category.SortOrder).Select(row => row.Policy).ToArray(), cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProviderRequirementResponse>> ApplyMiddleProviderRequirementsAsync(Guid middlePublicId,
        ApplyAdminMiddleProviderRequirementsRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        if (request.Services is null || request.Services.Count == 0) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_TARGET_REQUIRED", "적용할 중분류 하위 서비스가 없습니다.");
        if (request.Services.Select(value => value.ServiceId).Distinct().Count() != request.Services.Count || request.Services.Select(value => value.PolicyId).Distinct().Count() != request.Services.Count)
            throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_TARGET_DUPLICATED", "중복된 하위 서비스 정책이 포함되어 있습니다.");
        var middleId = await dbContext.ServiceCategories.AsNoTracking().Where(value => value.PublicId == middlePublicId && value.LevelCode == "MIDDLE").Select(value => (long?)value.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_MIDDLE_CATEGORY_NOT_FOUND", "중분류를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var policyIds = request.Services.Select(value => value.PolicyId).ToArray();
        var rows = await (from category in dbContext.ServiceCategories join policy in dbContext.CategoryOperationPolicies on category.Id equals policy.CategoryId
                          where category.ParentId == middleId && policyIds.Contains(policy.PublicId)
                          select new { Category = category, Policy = policy }).ToListAsync(cancellationToken);
        if (rows.Count != request.Services.Count || request.Services.Any(target => !rows.Any(row => row.Category.PublicId == target.ServiceId && row.Policy.PublicId == target.PolicyId)))
            throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_TARGET_MISMATCH", "중분류 서비스 정책이 최신 상태가 아닙니다. 새로고침 후 다시 시도해 주세요.", StatusCodes.Status409Conflict);
        var source = rows.SingleOrDefault(row => row.Category.PublicId == request.SourceServiceId && row.Policy.PublicId == request.SourcePolicyId)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_SOURCE_INVALID", "기준으로 사용할 전문가 요건 정책을 찾을 수 없습니다.");
        var tokens = new Dictionary<Guid, byte[]>(); foreach (var target in request.Services) tokens[target.PolicyId] = ParseRowVersion(target.RowVersion);
        if (rows.Any(row => !row.Policy.RowVersion.SequenceEqual(tokens[row.Policy.PublicId]))) throw OperationPolicyConflict();
        foreach (var row in rows) EnsureCurrent(row.Policy);

        var sourceAssignments = await dbContext.CategoryProviderRequirementAssignments.AsNoTracking().Where(value => value.CategoryOperationPolicyId == source.Policy.Id).OrderBy(value => value.DisplayOrder).ThenBy(value => value.Id).ToListAsync(cancellationToken);
        var sourceAssignmentIds = sourceAssignments.Select(value => value.Id).ToArray();
        var sourceEvidence = await dbContext.CategoryProviderRequirementEvidenceTypes.AsNoTracking().Where(value => sourceAssignmentIds.Contains(value.RequirementAssignmentId)).OrderBy(value => value.DisplayOrder).ToListAsync(cancellationToken);
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        await using var transaction = dbContext.Database.IsRelational() ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var pendingEvidence = new List<(CategoryProviderRequirementAssignment Target, CategoryProviderRequirementAssignment Source)>();
        foreach (var row in rows.Where(row => row.Policy.Id != source.Policy.Id))
        {
            var existing = await dbContext.CategoryProviderRequirementAssignments.Where(value => value.CategoryOperationPolicyId == row.Policy.Id).ToListAsync(cancellationToken);
            foreach (var targetAssignment in existing.Where(value => sourceAssignments.All(sourceAssignment => sourceAssignment.RequirementDefinitionId != value.RequirementDefinitionId)))
            { targetAssignment.IsActive = false; targetAssignment.UpdatedAt = now; targetAssignment.UpdatedByUserId = actor; }
            foreach (var sourceAssignment in sourceAssignments)
            {
                var targetAssignment = existing.SingleOrDefault(value => value.RequirementDefinitionId == sourceAssignment.RequirementDefinitionId);
                var isNew = targetAssignment is null;
                if (targetAssignment is null)
                {
                    targetAssignment = new CategoryProviderRequirementAssignment { CategoryOperationPolicyId = row.Policy.Id, RequirementDefinitionId = sourceAssignment.RequirementDefinitionId, CreatedAt = now, CreatedByUserId = actor };
                    dbContext.CategoryProviderRequirementAssignments.Add(targetAssignment); existing.Add(targetAssignment);
                }
                targetAssignment.IsRequired = sourceAssignment.IsRequired; targetAssignment.VerificationRequired = sourceAssignment.VerificationRequired;
                targetAssignment.ExpiryCheckRequired = sourceAssignment.ExpiryCheckRequired; targetAssignment.MinimumValidDays = sourceAssignment.MinimumValidDays;
                targetAssignment.DisplayOrder = sourceAssignment.DisplayOrder; targetAssignment.IsActive = sourceAssignment.IsActive; targetAssignment.UpdatedAt = now; targetAssignment.UpdatedByUserId = actor;
                if (isNew) pendingEvidence.Add((targetAssignment, sourceAssignment));
                else
                {
                    var currentEvidence = await dbContext.CategoryProviderRequirementEvidenceTypes.Where(value => value.RequirementAssignmentId == targetAssignment.Id).ToListAsync(cancellationToken);
                    dbContext.CategoryProviderRequirementEvidenceTypes.RemoveRange(currentEvidence);
                    foreach (var evidence in sourceEvidence.Where(value => value.RequirementAssignmentId == sourceAssignment.Id))
                        dbContext.CategoryProviderRequirementEvidenceTypes.Add(new CategoryProviderRequirementEvidenceType { RequirementAssignmentId = targetAssignment.Id, DocumentTypeId = evidence.DocumentTypeId, IsRequired = evidence.IsRequired, DisplayOrder = evidence.DisplayOrder, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
                }
            }
            var token = tokens[row.Policy.PublicId]; row.Policy.UpdatedAt = now; row.Policy.UpdatedByUserId = actor; dbContext.Entry(row.Policy).Property(value => value.RowVersion).OriginalValue = token;
            dbContext.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "MIDDLE_PROVIDER_REQUIREMENTS_APPLIED", EntityType = "CATEGORY_OPERATION_POLICY", EntityPublicId = row.Policy.PublicId, ResultCode = "SUCCESS", MetadataJson = JsonSerializer.Serialize(new { MiddleId = middlePublicId, SourceServiceId = request.SourceServiceId, TargetServiceId = row.Category.PublicId, RequirementCount = sourceAssignments.Count }) });
        }
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var pending in pendingEvidence)
                foreach (var evidence in sourceEvidence.Where(value => value.RequirementAssignmentId == pending.Source.Id))
                    dbContext.CategoryProviderRequirementEvidenceTypes.Add(new CategoryProviderRequirementEvidenceType { RequirementAssignmentId = pending.Target.Id, DocumentTypeId = evidence.DocumentTypeId, IsRequired = evidence.IsRequired, DisplayOrder = evidence.DisplayOrder, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
            if (pendingEvidence.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { throw OperationPolicyConflict(); }
        return await BuildResponsesAsync(rows.OrderBy(row => row.Category.SortOrder).Select(row => row.Policy).ToArray(), cancellationToken);
    }

    private Task<long?> GetServiceIdAsync(Guid publicId, CancellationToken cancellationToken) =>
        dbContext.ServiceCategories.AsNoTracking()
            .Where(category => category.PublicId == publicId && category.LevelCode == "SERVICE")
            .Select(category => (long?)category.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<AdminProviderRequirementResponse>> BuildResponsesAsync(IReadOnlyList<CategoryOperationPolicy> policies, CancellationToken cancellationToken)
    {
        var policyIds = policies.Select(item => item.Id).ToArray();
        var assignments = await (from assignment in dbContext.CategoryProviderRequirementAssignments.AsNoTracking()
            join definition in dbContext.ProviderRequirementDefinitions.AsNoTracking() on assignment.RequirementDefinitionId equals definition.Id
            where policyIds.Contains(assignment.CategoryOperationPolicyId)
            orderby assignment.DisplayOrder, assignment.Id
            select new { Assignment = assignment, Definition = definition }).ToListAsync(cancellationToken);
        var assignmentIds = assignments.Select(item => item.Assignment.Id).ToArray();
        var evidence = await (from link in dbContext.CategoryProviderRequirementEvidenceTypes.AsNoTracking()
            join type in dbContext.ProviderDocumentTypes.AsNoTracking() on link.DocumentTypeId equals type.Id
            where assignmentIds.Contains(link.RequirementAssignmentId)
            orderby link.DisplayOrder, link.Id
            select new { Link = link, Type = type }).ToListAsync(cancellationToken);
        return policies.Select(policy => BuildResponse(policy, assignments.Where(item => item.Assignment.CategoryOperationPolicyId == policy.Id).Select(item =>
            new AdminCategoryProviderRequirementResponse(item.Assignment.PublicId, item.Definition.PublicId, item.Definition.RequirementTypeCode, item.Definition.RequirementCode, item.Definition.Name,
                item.Assignment.IsRequired, item.Assignment.VerificationRequired, item.Assignment.ExpiryCheckRequired, item.Assignment.MinimumValidDays, item.Assignment.DisplayOrder, item.Assignment.IsActive,
                evidence.Where(link => link.Link.RequirementAssignmentId == item.Assignment.Id).Select(link => new AdminCategoryProviderRequirementEvidenceResponse(link.Type.PublicId, link.Type.Code, link.Type.Name, link.Link.IsRequired, link.Link.DisplayOrder)).ToArray())).ToArray())).ToArray();
    }

    private static AdminProviderRequirementResponse BuildResponse(CategoryOperationPolicy policy, IReadOnlyList<AdminCategoryProviderRequirementResponse> structuredRequirements)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var status = !policy.IsActive ? "INACTIVE"
            : policy.EffectiveFrom > today ? "SCHEDULED"
            : policy.EffectiveTo.HasValue && policy.EffectiveTo <= today ? "ENDED"
            : "CURRENT";
        return new AdminProviderRequirementResponse(
            policy.PublicId,
            policy.PolicyVersion,
            policy.RequiredQualificationSummaryText,
            policy.InsuranceRequirementText,
            policy.SafetyGradeCode,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            status,
            status == "CURRENT",
            policy.IsActive,
            Convert.ToBase64String(policy.RowVersion),
            structuredRequirements);
    }

    private async Task<CategoryOperationPolicy> GetPolicyEntityAsync(Guid servicePublicId, Guid policyPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken) ?? throw new AdminServiceCategoryException("ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        return await dbContext.CategoryOperationPolicies.SingleOrDefaultAsync(item => item.CategoryId == serviceId && item.PublicId == policyPublicId, cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_NOT_FOUND", "전문가 요건 정책을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    }
    private async Task<long> GetActorAsync(Guid id, CancellationToken cancellationToken) => await dbContext.Users.Where(user => user.PublicId == id && user.StatusCode == "ACTIVE").Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
    private static void Validate(SaveAdminCategoryProviderRequirementRequest request)
    {
        if (request.RequirementDefinitionId == Guid.Empty) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_DEFINITION_REQUIRED", "요건 정의를 선택해 주세요.");
        if (request.DisplayOrder < 0) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_DISPLAY_ORDER_INVALID", "표시순서는 0 이상이어야 합니다.");
        if (request.MinimumValidDays < 0) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_MINIMUM_VALID_DAYS_INVALID", "최소 유효일수는 0 이상이어야 합니다.");
        if (!request.ExpiryCheckRequired && request.MinimumValidDays.HasValue) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_EXPIRY_RULE_INVALID", "만료확인을 사용하지 않으면 최소 유효일수를 설정할 수 없습니다.");
    }
    private static void Apply(CategoryProviderRequirementAssignment entity, SaveAdminCategoryProviderRequirementRequest request)
    { entity.IsRequired = request.IsRequired; entity.VerificationRequired = request.VerificationRequired; entity.ExpiryCheckRequired = request.ExpiryCheckRequired; entity.MinimumValidDays = request.ExpiryCheckRequired ? request.MinimumValidDays : null; entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive; }
    private static void ValidateOperationPolicy(string qualification, string insurance, string safetyGradeCode)
    {
        if (string.IsNullOrWhiteSpace(qualification)) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_QUALIFICATION_REQUIRED", "자격·면허 요구사항을 입력해 주세요.");
        if (qualification.Trim().Length > 1000) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_QUALIFICATION_TOO_LONG", "자격·면허 요구사항은 1,000자 이하여야 합니다.");
        if (string.IsNullOrWhiteSpace(insurance)) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_INSURANCE_REQUIRED", "보험 요구수준을 입력해 주세요.");
        if (insurance.Trim().Length > 100) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_INSURANCE_TOO_LONG", "보험 요구수준은 100자 이하여야 합니다.");
        if (safetyGradeCode.Trim().ToUpperInvariant() is not ("NORMAL" or "MEDIUM" or "HIGH")) throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_SAFETY_GRADE_INVALID", "안전등급은 일반, 중, 고 중에서 선택해 주세요.");
    }
    private static void EnsureCurrent(CategoryOperationPolicy policy)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!policy.IsActive || policy.EffectiveFrom > today || policy.EffectiveTo.HasValue && policy.EffectiveTo <= today)
            throw new AdminServiceCategoryException("ADMIN_OPERATION_POLICY_NOT_CURRENT", "현재 적용 중인 운영정책만 수정할 수 있습니다.", StatusCodes.Status409Conflict);
    }
    private static byte[] ParseRowVersion(string value) { if (value is null) throw OperationPolicyConflict(); try { return Convert.FromBase64String(value); } catch (FormatException) { throw OperationPolicyConflict(); } }
    private static void ApplyOperationPolicy(CategoryOperationPolicy policy, string qualification, string insurance, string safetyGradeCode)
    { policy.RequiredQualificationSummaryText = qualification.Trim(); policy.InsuranceRequirementText = insurance.Trim(); policy.SafetyGradeCode = safetyGradeCode.Trim().ToUpperInvariant(); }
    private static string OperationSnapshot(CategoryOperationPolicy policy) => JsonSerializer.Serialize(new { policy.RequiredQualificationSummaryText, policy.InsuranceRequirementText, policy.SafetyGradeCode });
    private void AddOperationAudit(long actor, string action, CategoryOperationPolicy policy, string before, string after) => dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = action, EntityType = "CATEGORY_OPERATION_POLICY", EntityPublicId = policy.PublicId, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = after, MetadataJson = JsonSerializer.Serialize(new { policy.CategoryId, policy.PolicyVersion }) });
    private static AdminServiceCategoryException OperationPolicyConflict() => new("ADMIN_OPERATION_POLICY_CONFLICT", "다른 화면에서 운영정책이 변경되었습니다. 새로고침 후 다시 시도해 주세요.", StatusCodes.Status409Conflict);
    private void AddAudit(long actor, string action, Guid publicId, string? before, string after) => dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = action, EntityType = "CATEGORY_PROVIDER_REQUIREMENT_ASSIGNMENT", EntityPublicId = publicId, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = after });
    private static string Snapshot(CategoryProviderRequirementAssignment entity) => JsonSerializer.Serialize(new { entity.RequirementDefinitionId, entity.IsRequired, entity.VerificationRequired, entity.ExpiryCheckRequired, entity.MinimumValidDays, entity.DisplayOrder, entity.IsActive });
}
