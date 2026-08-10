using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderRequirementStandardService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminProviderRequirementStandardsResponse> GetAsync(CancellationToken cancellationToken) => new(
        await dbContext.ProviderRequirementTypes.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new AdminProviderRequirementTypeResponse(item.Code, item.Name, item.IsActive)).ToListAsync(cancellationToken),
        await dbContext.ProviderRequirementDefinitions.AsNoTracking().OrderBy(item => item.RequirementTypeCode).ThenBy(item => item.Name)
            .Select(item => new AdminProviderRequirementDefinitionResponse(item.PublicId, item.RequirementTypeCode, item.RequirementCode, item.Name, item.Description, item.IsActive)).ToListAsync(cancellationToken),
        await dbContext.ProviderDocumentTypes.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new AdminProviderDocumentTypeResponse(item.PublicId, item.Code, item.Name, item.SupportsExpiry, item.IsActive)).ToListAsync(cancellationToken));

    public async Task<AdminProviderRequirementTypeResponse> CreateTypeAsync(SaveAdminProviderRequirementTypeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await dbContext.ProviderRequirementTypes.AnyAsync(item => item.Code == code, cancellationToken)) throw Conflict("ADMIN_PROVIDER_REQUIREMENT_TYPE_DUPLICATED", "같은 요건유형 코드가 이미 있습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken);
        var entity = new ProviderRequirementType { Code = code, Name = request.Name.Trim(), IsActive = request.IsActive };
        dbContext.ProviderRequirementTypes.Add(entity);
        AddAudit(actor, "PROVIDER_REQUIREMENT_TYPE_CREATED", "PROVIDER_REQUIREMENT_TYPE", Guid.Empty, null, JsonSerializer.Serialize(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Code, entity.Name, entity.IsActive);
    }

    public async Task<AdminProviderRequirementTypeResponse> UpdateTypeAsync(string code, SaveAdminProviderRequirementTypeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeCode(code);
        var entity = await dbContext.ProviderRequirementTypes.SingleOrDefaultAsync(item => item.Code == normalized, cancellationToken)
            ?? throw NotFound("요건유형을 찾을 수 없습니다.");
        if (NormalizeCode(request.Code) != normalized) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_TYPE_CODE_IMMUTABLE", "요건유형 코드는 변경할 수 없습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken);
        var before = JsonSerializer.Serialize(entity);
        entity.Name = request.Name.Trim(); entity.IsActive = request.IsActive;
        AddAudit(actor, "PROVIDER_REQUIREMENT_TYPE_UPDATED", "PROVIDER_REQUIREMENT_TYPE", Guid.Empty, before, JsonSerializer.Serialize(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Code, entity.Name, entity.IsActive);
    }

    public async Task<AdminProviderRequirementDefinitionResponse> CreateDefinitionAsync(SaveAdminProviderRequirementDefinitionRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var typeCode = NormalizeCode(request.RequirementTypeCode); var code = NormalizeCode(request.RequirementCode);
        await EnsureActiveTypeAsync(typeCode, cancellationToken);
        if (await dbContext.ProviderRequirementDefinitions.AnyAsync(item => item.RequirementCode == code, cancellationToken)) throw Conflict("ADMIN_PROVIDER_REQUIREMENT_DEFINITION_DUPLICATED", "같은 요건 코드가 이미 있습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        var entity = new ProviderRequirementDefinition { RequirementTypeCode = typeCode, RequirementCode = code, Name = request.Name.Trim(), Description = Clean(request.Description), IsActive = request.IsActive,
            CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        dbContext.ProviderRequirementDefinitions.Add(entity);
        AddAudit(actor, "PROVIDER_REQUIREMENT_DEFINITION_CREATED", "PROVIDER_REQUIREMENT_DEFINITION", entity.PublicId, null, Snapshot(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<AdminProviderRequirementDefinitionResponse> UpdateDefinitionAsync(Guid id, SaveAdminProviderRequirementDefinitionRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProviderRequirementDefinitions.SingleOrDefaultAsync(item => item.PublicId == id, cancellationToken) ?? throw NotFound("요건 정의를 찾을 수 없습니다.");
        var typeCode = NormalizeCode(request.RequirementTypeCode); var code = NormalizeCode(request.RequirementCode);
        await EnsureActiveTypeAsync(typeCode, cancellationToken);
        if (await dbContext.ProviderRequirementDefinitions.AnyAsync(item => item.Id != entity.Id && item.RequirementCode == code, cancellationToken)) throw Conflict("ADMIN_PROVIDER_REQUIREMENT_DEFINITION_DUPLICATED", "같은 요건 코드가 이미 있습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var before = Snapshot(entity);
        entity.RequirementTypeCode = typeCode; entity.RequirementCode = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = actor;
        AddAudit(actor, "PROVIDER_REQUIREMENT_DEFINITION_UPDATED", "PROVIDER_REQUIREMENT_DEFINITION", entity.PublicId, before, Snapshot(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<AdminProviderDocumentTypeResponse> CreateDocumentTypeAsync(SaveAdminProviderDocumentTypeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await dbContext.ProviderDocumentTypes.AnyAsync(item => item.Code == code, cancellationToken)) throw Conflict("ADMIN_PROVIDER_DOCUMENT_TYPE_DUPLICATED", "같은 증빙유형 코드가 이미 있습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        var entity = new ProviderDocumentType { Code = code, Name = request.Name.Trim(), SupportsExpiry = request.SupportsExpiry, IsActive = request.IsActive,
            CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        dbContext.ProviderDocumentTypes.Add(entity);
        AddAudit(actor, "PROVIDER_DOCUMENT_TYPE_CREATED", "PROVIDER_DOCUMENT_TYPE", entity.PublicId, null, JsonSerializer.Serialize(request));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<AdminProviderDocumentTypeResponse> UpdateDocumentTypeAsync(Guid id, SaveAdminProviderDocumentTypeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProviderDocumentTypes.SingleOrDefaultAsync(item => item.PublicId == id, cancellationToken) ?? throw NotFound("증빙유형을 찾을 수 없습니다.");
        var code = NormalizeCode(request.Code);
        if (await dbContext.ProviderDocumentTypes.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken)) throw Conflict("ADMIN_PROVIDER_DOCUMENT_TYPE_DUPLICATED", "같은 증빙유형 코드가 이미 있습니다.");
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var before = JsonSerializer.Serialize(new { entity.Code, entity.Name, entity.SupportsExpiry, entity.IsActive });
        entity.Code = code; entity.Name = request.Name.Trim(); entity.SupportsExpiry = request.SupportsExpiry; entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = actor;
        AddAudit(actor, "PROVIDER_DOCUMENT_TYPE_UPDATED", "PROVIDER_DOCUMENT_TYPE", entity.PublicId, before, JsonSerializer.Serialize(request));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task EnsureActiveTypeAsync(string code, CancellationToken cancellationToken)
    {
        if (!await dbContext.ProviderRequirementTypes.AnyAsync(item => item.Code == code && item.IsActive, cancellationToken)) throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIREMENT_TYPE_INVALID", "활성 요건유형을 선택해 주세요.");
    }
    private async Task<long> GetActorAsync(Guid id, CancellationToken cancellationToken) => await dbContext.Users.Where(user => user.PublicId == id && user.StatusCode == "ACTIVE").Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
    private void AddAudit(long actor, string action, string type, Guid publicId, string? before, string after) => dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = action, EntityType = type, EntityPublicId = publicId, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = after });
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Snapshot(ProviderRequirementDefinition entity) => JsonSerializer.Serialize(new { entity.RequirementTypeCode, entity.RequirementCode, entity.Name, entity.Description, entity.IsActive });
    private static AdminProviderRequirementDefinitionResponse ToResponse(ProviderRequirementDefinition entity) => new(entity.PublicId, entity.RequirementTypeCode, entity.RequirementCode, entity.Name, entity.Description, entity.IsActive);
    private static AdminProviderDocumentTypeResponse ToResponse(ProviderDocumentType entity) => new(entity.PublicId, entity.Code, entity.Name, entity.SupportsExpiry, entity.IsActive);
    private static AdminServiceCategoryException Conflict(string code, string message) => new(code, message, StatusCodes.Status409Conflict);
    private static AdminServiceCategoryException NotFound(string message) => new("ADMIN_PROVIDER_REQUIREMENT_STANDARD_NOT_FOUND", message, StatusCodes.Status404NotFound);
}
