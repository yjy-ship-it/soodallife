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
    string? PublicIntroductionHtml,
    string? PublicPhone,
    string? PublicEmail,
    string? PublicAddress,
    string? PublicBlogUrl,
    string? PublicWebsiteUrl,
    string? PublicLogoUrl,
    IReadOnlyList<string> PublicPhotoUrls,
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
    string? PublicIntroductionHtml,
    string? PublicPhone,
    string? PublicEmail,
    string? PublicAddress,
    string? PublicBlogUrl,
    string? PublicWebsiteUrl,
    string? PublicLogoUrl,
    IReadOnlyList<string>? PublicPhotoUrls,
    string? ConcurrencyToken);

public sealed record ProviderPromotionImageResponse(
    Guid FileId,
    string Url,
    string ContentType,
    long SizeBytes);

public sealed record ProviderPromotionStorageStatusResponse(bool Writable, string Message);
public sealed record ProviderPromotionImageContentInput(string FileName, string ContentType, string Base64Content);
public sealed record ProviderPromotionPhotoContentInput(ProviderPromotionImageContentInput File, bool ReplaceExisting);
public sealed record ProviderPromotionImageChunkInput(Guid UploadId, string Purpose, bool ReplaceExisting,
    string FileName, string ContentType, int ChunkIndex, int TotalChunks, string Base64Chunk);
public sealed record ProviderPromotionImageChunkResponse(bool Completed, ProviderProfileResponse? Profile);

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
    Guid MiddleCategoryId,
    string MiddleCategoryName,
    string MiddleCategoryPath,
    string CategoryPath,
    bool IsNationwide,
    bool NationwideAllowed,
    string CoverageTypeCode,
    string CoverageTypeName,
    IReadOnlyList<ProviderAreaResponse> Areas);

public sealed record ProviderAreaResponse(Guid Id, string Name, string AreaCode);

public sealed record ProviderServiceAreaSelection(
    Guid ServiceCategoryId,
    IReadOnlyList<Guid> AdministrativeAreaIds);

public sealed record ProviderMiddleServiceAreaSelection(
    Guid MiddleCategoryId,
    IReadOnlyList<Guid> AdministrativeAreaIds);

public sealed record ReplaceProviderServiceAreasInput(
    IReadOnlyList<ProviderServiceAreaSelection>? Services,
    IReadOnlyList<ProviderMiddleServiceAreaSelection>? Middles,
    IReadOnlyList<Guid>? NationwideServiceCategoryIds = null);

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
public sealed record ResubmitProviderApprovalInput(string? Note, string ConcurrencyToken);

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

public sealed record ProviderOperationsDashboardResponse(
    int NewMatchedRequestCount,
    int SubmittedQuoteCount,
    int WaitingSelectionQuoteCount,
    int SelectedTransactionCount,
    int AppointmentActionRequiredCount,
    int TodayAppointmentCount,
    int InProgressWorkCount,
    int WaitingCompletionConfirmationCount,
    int RevisionRequestedCount,
    int UnreadNotificationCount);

public sealed record ProviderLegalDocumentResponse(Guid Id, Guid VersionId, string Code, string RequirementCode,
    string Title, string Content, int Version, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPlaceholder);
public sealed record ProviderConsentInput(Guid LegalDocumentVersionId, bool Agreed);
public sealed record RegisterProviderRequest(string LoginId, string Password, string PasswordConfirmation,
    string? Email, string? Phone, string? PhoneVerificationToken, string? ProviderTypeCode, string BusinessName, string RepresentativeName, string ContactName,
    string? BusinessRegistrationNumber, string? BusinessAddress, string? BusinessTypeText,
    string? BusinessItemText, string? Introduction, IReadOnlyList<ProviderConsentInput> Consents);
public sealed record AddProviderRoleRequest(string? ProviderTypeCode, string BusinessName, string RepresentativeName, string ContactName,
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
public sealed record BusinessRegistrationAvailabilityResponse(string NormalizedValue, bool Valid, bool Available);
