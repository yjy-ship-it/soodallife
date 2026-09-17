using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderRequirementStandardService(SoodalLifeDbContext dbContext)
{
    private static readonly (string Code, string Name)[] DefaultTypes =
    [
        ("LICENSE", "면허"), ("INSURANCE", "보험"), ("SAFETY", "안전"),
        ("QUALIFICATION", "자격"), ("EVIDENCE_VALIDITY", "증빙 유효성")
    ];
    private static readonly (string Type, string Code, string Name, string Description)[] DefaultDefinitions =
    [
        ("QUALIFICATION", "BUSINESS_REGISTRATION_VERIFICATION", "사업자등록 확인", "사업자등록 상태와 제출 정보의 일치 여부를 확인합니다."),
        ("QUALIFICATION", "IDENTITY_AND_REPRESENTATIVE_VERIFICATION", "본인·대표자 확인", "전문가 본인 또는 법인 대표자 정보를 확인합니다."),
        ("LICENSE", "PROFESSIONAL_LICENSE_VERIFICATION", "관련 자격·면허 확인", "서비스 수행에 필요한 자격증·면허·등록증의 유효 여부를 확인합니다."),
        ("INSURANCE", "LIABILITY_INSURANCE_VERIFICATION", "배상책임보험 확인", "서비스 수행 중 사고에 대비한 배상책임보험 가입과 유효기간을 확인합니다."),
        ("SAFETY", "SAFETY_EDUCATION_VERIFICATION", "안전교육 이수 확인", "업무에 필요한 안전교육 이수 여부와 유효기간을 확인합니다."),
        ("EVIDENCE_VALIDITY", "EVIDENCE_EXPIRY_VERIFICATION", "증빙 유효기간 확인", "제출 증빙이 심사일과 서비스 수행기간 동안 유효한지 확인합니다.")
    ];
    private static readonly (string Code, string Name, bool SupportsExpiry)[] DefaultDocuments =
    [
        ("BUSINESS_REGISTRATION_CERTIFICATE", "사업자등록증", false),
        ("IDENTITY_VERIFICATION_DOCUMENT", "본인·대표자 확인서류", false),
        ("PROFESSIONAL_LICENSE_CERTIFICATE", "자격증·면허증·등록증", true),
        ("LIABILITY_INSURANCE_CERTIFICATE", "배상책임보험 가입증명서", true),
        ("SAFETY_EDUCATION_CERTIFICATE", "안전교육 이수증", true),
        ("CAREER_CERTIFICATE", "경력증명서", false),
        ("TAX_PAYMENT_CERTIFICATE", "국세·지방세 납세증명서", true),
        ("BANK_ACCOUNT_COPY", "정산계좌 통장사본", false)
    ];
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

    public async Task<AdminProviderRequirementDefaultsResponse> ApplyDefaultsAsync(Guid actorPublicId, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(actorPublicId, cancellationToken); var now = DateTime.UtcNow;
        var existingTypes = await dbContext.ProviderRequirementTypes.Select(x => x.Code).ToListAsync(cancellationToken);
        var existingDefinitions = await dbContext.ProviderRequirementDefinitions.Select(x => x.RequirementCode).ToListAsync(cancellationToken);
        var existingDocuments = await dbContext.ProviderDocumentTypes.Select(x => x.Code).ToListAsync(cancellationToken);
        var createdTypes = 0; var createdDefinitions = 0; var createdDocuments = 0;
        foreach (var item in DefaultTypes.Where(x => !existingTypes.Contains(x.Code)))
        {
            dbContext.ProviderRequirementTypes.Add(new ProviderRequirementType { Code = item.Code, Name = item.Name, IsActive = true }); createdTypes++;
        }
        foreach (var item in DefaultDefinitions.Where(x => !existingDefinitions.Contains(x.Code)))
        {
            dbContext.ProviderRequirementDefinitions.Add(new ProviderRequirementDefinition { RequirementTypeCode = item.Type, RequirementCode = item.Code,
                Name = item.Name, Description = item.Description, IsActive = true, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor }); createdDefinitions++;
        }
        foreach (var item in DefaultDocuments.Where(x => !existingDocuments.Contains(x.Code)))
        {
            dbContext.ProviderDocumentTypes.Add(new ProviderDocumentType { Code = item.Code, Name = item.Name, SupportsExpiry = item.SupportsExpiry,
                IsActive = true, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor }); createdDocuments++;
        }
        var result = new AdminProviderRequirementDefaultsResponse(createdTypes, createdDefinitions, createdDocuments,
            DefaultTypes.Length - createdTypes, DefaultDefinitions.Length - createdDefinitions, DefaultDocuments.Length - createdDocuments);
        AddAudit(actor, "PROVIDER_REQUIREMENT_DEFAULTS_APPLIED", "PROVIDER_REQUIREMENT_STANDARD", Guid.Empty, null, JsonSerializer.Serialize(result));
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
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
