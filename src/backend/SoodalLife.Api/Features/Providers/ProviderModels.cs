namespace SoodalLife.Api.Features.Providers;

public sealed record ProviderProfileResponse(
    Guid Id,
    string BusinessName,
    string? RepresentativeName,
    string? ContactName,
    string? Phone,
    string? Email,
    string? BusinessRegistrationNumber,
    string? BusinessAddress,
    string? BusinessTypeText,
    string? BusinessItemText,
    string? Introduction,
    string? ProviderType,
    string ApprovalStatus,
    string ActivityStatus,
    decimal? TrustScore,
    string TrustDisplayStatus,
    string? RejectionReason,
    string ConcurrencyToken);

public sealed record UpdateProviderProfileInput(
    string BusinessName,
    string RepresentativeName,
    string ContactName,
    string? Phone,
    string? Email,
    string? BusinessRegistrationNumber,
    string? BusinessAddress,
    string? BusinessTypeText,
    string? BusinessItemText,
    string? Introduction,
    string? ConcurrencyToken);

public sealed record ProviderServiceCategoryResponse(
    Guid CategoryId,
    string CategoryPath,
    string Status,
    string ApprovalStatus,
    string? DecisionReason,
    int RequiredRequirementCount,
    int ApprovedRequirementCount);

public sealed record ReplaceProviderServiceCategoriesInput(IReadOnlyList<Guid> CategoryIds);

public sealed record ProviderServiceAreaResponse(
    Guid ServiceCategoryId,
    string CategoryPath,
    IReadOnlyList<ProviderAreaResponse> Areas);

public sealed record ProviderAreaResponse(Guid Id, string Name, string AreaCode);

public sealed record ProviderServiceAreaSelection(
    Guid ServiceCategoryId,
    IReadOnlyList<Guid> AdministrativeAreaIds);

public sealed record ReplaceProviderServiceAreasInput(
    IReadOnlyList<ProviderServiceAreaSelection> Services);

public sealed record ProviderRequirementResponse(
    Guid VerificationId,
    Guid AssignmentId,
    Guid ServiceCategoryId,
    string CategoryPath,
    string RequirementCode,
    string RequirementName,
    string RequirementType,
    bool IsRequired,
    bool VerificationRequired,
    bool ExpiryCheckRequired,
    int? MinimumValidDays,
    string VerificationStatus,
    Guid? DocumentId,
    string? DocumentName,
    DateOnly? ExpiresAt,
    string? RejectionReason,
    IReadOnlyList<ProviderEvidenceTypeResponse> AcceptedEvidenceTypes);

public sealed record ProviderEvidenceTypeResponse(Guid DocumentTypeId, string Code, string Name, bool IsRequired);
public sealed record ProviderDocumentTypeResponse(Guid Id, string Code, string Name, string? Description);
public sealed record ProviderDocumentResponse(Guid Id, Guid FileId, string DocumentTypeCode, string DocumentTypeName,
    string OriginalFileName, long SizeBytes, string ContentType, string VerificationStatus, string MalwareStatus,
    DateOnly? IssuedAt, DateOnly? ExpiresAt, string? PublicNote, DateTime CreatedAt);
public sealed record RegisterProviderDocumentInput(Guid DocumentTypeId, string? DocumentNumber, DateOnly? IssuedAt, DateOnly? ExpiresAt);
public sealed record LinkProviderEvidenceInput(Guid DocumentId);

public sealed record ProviderOnboardingDashboardResponse(
    string ApprovalStatus,
    string ActivityStatus,
    int RegisteredServiceCount,
    int ApprovedServiceCount,
    int PendingServiceCount,
    int RejectedServiceCount,
    int ActiveAreaCount,
    int RequiredEvidenceCount,
    int SubmittedEvidenceCount,
    int ApprovedEvidenceCount,
    IReadOnlyList<string> NextActions,
    string? RejectionReason);

public sealed record ProviderLegalDocumentResponse(Guid Id, Guid VersionId, string Code, string RequirementCode,
    string Title, string Content, int Version, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPlaceholder);
public sealed record ProviderConsentInput(Guid LegalDocumentVersionId, bool Agreed);
public sealed record RegisterProviderRequest(string LoginId, string Password, string PasswordConfirmation,
    string? Email, string? Phone, string BusinessName, string RepresentativeName, string ContactName,
    string? BusinessRegistrationNumber, string? BusinessAddress, string? BusinessTypeText,
    string? BusinessItemText, string? Introduction, IReadOnlyList<ProviderConsentInput> Consents);
public sealed record AddProviderRoleRequest(string BusinessName, string RepresentativeName, string ContactName,
    string? BusinessRegistrationNumber, string? BusinessAddress, string? BusinessTypeText,
    string? BusinessItemText, string? Introduction, IReadOnlyList<ProviderConsentInput> Consents);
public sealed record ProviderRegistrationResponse(Guid UserId, Guid ProviderId, string LoginId, IReadOnlyList<string> Roles,
    string ApprovalStatus, string ActivityStatus);

public sealed class ProviderConfigurationException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
