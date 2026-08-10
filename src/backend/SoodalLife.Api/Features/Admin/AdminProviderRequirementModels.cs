namespace SoodalLife.Api.Features.Admin;

public sealed record AdminProviderRequirementListResponse(
    IReadOnlyList<AdminProviderRequirementResponse> Policies,
    Guid? CurrentPolicyId,
    bool HasHistory,
    bool SupportsStructuredRequirements,
    bool SupportsEvidenceValidityRules,
    bool CanEdit);

public sealed record AdminProviderRequirementResponse(
    Guid Id,
    string PolicyVersion,
    string QualificationAndLicenseRequirement,
    string InsuranceRequirement,
    string SafetyGradeCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string EffectiveStatus,
    bool IsCurrentlyEffective,
    bool IsActive,
    IReadOnlyList<AdminCategoryProviderRequirementResponse> StructuredRequirements);

public sealed record AdminCategoryProviderRequirementResponse(
    Guid Id,
    Guid RequirementDefinitionId,
    string RequirementTypeCode,
    string RequirementCode,
    string RequirementName,
    bool IsRequired,
    bool VerificationRequired,
    bool ExpiryCheckRequired,
    short? MinimumValidDays,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AdminCategoryProviderRequirementEvidenceResponse> EvidenceTypes);

public sealed record AdminCategoryProviderRequirementEvidenceResponse(Guid DocumentTypeId, string Code, string Name, bool IsRequired, int DisplayOrder);

public sealed record SaveAdminCategoryProviderRequirementRequest(
    Guid RequirementDefinitionId,
    bool IsRequired,
    bool VerificationRequired,
    bool ExpiryCheckRequired,
    short? MinimumValidDays,
    int DisplayOrder,
    bool IsActive);

public sealed record SaveAdminCategoryProviderRequirementEvidenceRequest(Guid DocumentTypeId, bool IsRequired, int DisplayOrder);
public sealed record ReplaceAdminCategoryProviderRequirementEvidenceRequest(IReadOnlyList<SaveAdminCategoryProviderRequirementEvidenceRequest> EvidenceTypes);
