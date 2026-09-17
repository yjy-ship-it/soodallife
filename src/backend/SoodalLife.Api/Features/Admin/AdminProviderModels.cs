namespace SoodalLife.Api.Features.Admin;

public sealed record AdminProviderListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminProviderListItemResponse> Items);
public sealed record AdminProviderListItemResponse(Guid Id, string ProviderName, string BusinessName, string ProviderTypeCode, string? MaskedPhone, string? MaskedEmail,
    string? MaskedBusinessRegistrationNo, string AccountStatusCode, string ApprovalStatusCode, string ReviewStageCode, string ActivityStatusCode,
    int RegisteredServiceCount, int? ApprovedServiceCount, string EvidenceSummary, DateTime JoinedAt, DateTime? LastActiveAt, IReadOnlyList<string> Roles);

public sealed record AdminProviderDetailResponse(AdminProviderBasicResponse Basic, AdminProviderSummaryResponse Summary,
    AdminProviderBusinessResponse Business, IReadOnlyList<AdminProviderServiceResponse> Services, IReadOnlyList<AdminProviderAreaResponse> Areas,
    IReadOnlyList<AdminProviderDocumentResponse> Documents, IReadOnlyList<AdminProviderServiceReviewResponse> ServiceReviews,
    IReadOnlyList<AdminProviderQuoteResponse> Quotes, IReadOnlyList<AdminProviderTransactionResponse> Transactions,
    IReadOnlyList<AdminProviderApprovalEventResponse> ApprovalHistory, IReadOnlyList<AdminCustomerAuditResponse> ManagementHistory);

public sealed record AdminProviderBasicResponse(Guid Id, Guid UserId, string ProviderName, string? Phone, string? Email, string AccountStatusCode,
    string ApprovalStatusCode, string ReviewStageCode, string ActivityStatusCode, DateTime JoinedAt, DateTime? LastLoginAt, IReadOnlyList<string> Roles, string RowVersion);
public sealed record AdminProviderSummaryResponse(int RegisteredServiceCount, int? ApprovedServiceCount, int? PendingServiceCount,
    int SubmittedDocumentCount, int? VerifiedDocumentCount, int ExpiringDocumentCount, int ExpiredDocumentCount,
    int SubmittedQuoteCount, int AcceptedQuoteCount, int CompletedTransactionCount, DateTime? LastActiveAt);
public sealed record AdminProviderBusinessResponse(string ProviderTypeCode, string BusinessName, string? BusinessRegistrationNo,
    string? RepresentativeName, string? ContactName, string? BusinessAddress, string? BusinessTypeText, string? BusinessItemText,
    string? Introduction, string? PublicIntroductionHtml, string? PublicPhone, string? PublicEmail, string? PublicAddress,
    string? PublicBlogUrl, string? PublicWebsiteUrl, string? PublicLogoUrl, IReadOnlyList<string> PublicPhotoUrls, bool DetailFieldsSupported);
public sealed record AdminProviderServiceResponse(long InternalId, Guid ServiceId, string MajorName, string MiddleName, string ServiceName,
    string RegistrationStatusCode, string ApprovalStatus, DateTime ActivatedAt, DateTime? DeactivatedAt);
public sealed record AdminProviderAreaResponse(Guid ServiceId, string ServiceName, Guid AreaId, string ProvinceName, string DistrictName, string AreaLevelCode,
    string StatusCode, DateTime ActivatedAt, DateTime? DeactivatedAt, bool IsPrioritySupported);
public sealed record AdminProviderDocumentResponse(long InternalId, Guid FileId, string DocumentTypeName, string DocumentTypeCode, string? DocumentNumber,
    DateOnly? IssuedAt, DateOnly? ExpiresAt, string VerificationStatusCode, DateTime? VerifiedAt, string? VerifierName,
    string OriginalFileName, bool CanOpenFile, string FileAccessMessage);
public sealed record AdminUpdateProviderRequest(string ProviderTypeCode, string BusinessName, string RepresentativeName, string ContactName,
    string? BusinessRegistrationNo, string? BusinessAddress, string? BusinessTypeText, string? BusinessItemText, string? Introduction,
    string? PublicIntroductionHtml, string? PublicPhone, string? PublicEmail, string? PublicAddress, string? PublicBlogUrl,
    string? PublicWebsiteUrl, string RowVersion);
public sealed record AdminReplaceProviderServicesRequest(IReadOnlyList<Guid>? CategoryIds);
public sealed record AdminProviderAreaSelection(Guid ServiceCategoryId, IReadOnlyList<Guid>? AdministrativeAreaIds);
public sealed record AdminReplaceProviderAreasRequest(IReadOnlyList<AdminProviderAreaSelection>? Services);
public sealed record AdminProviderServiceReviewResponse(Guid ServiceId, string ServiceName, string RegistrationStatusCode,
    string ApprovalStatusCode, DateTime ApprovalRequestedAt, DateTime? ApprovalDecidedAt, string? DecisionReason, string RowVersion,
    IReadOnlyList<AdminProviderServiceApprovalEventResponse> ApprovalHistory, bool StructuredRequirementsConfigured,
    string LegacyQualificationText, string LegacyInsuranceText, string LegacySafetyGradeCode, IReadOnlyList<AdminProviderRequirementComparisonResponse> Requirements);
public sealed record AdminProviderRequirementComparisonResponse(Guid AssignmentId, string RequirementName, bool IsRequired, bool VerificationRequired,
    bool ExpiryCheckRequired, IReadOnlyList<string> RequiredEvidenceTypes, string VerificationStatusCode, string? LinkedDocumentType,
    DateTime? VerifiedAt, DateOnly? ExpiresAt, string? RejectionReason);
public sealed record AdminProviderQuoteResponse(Guid Id, Guid RequestId, string RequestTitle, string ServiceName, DateTime? SubmittedAt,
    decimal? TotalAmount, string CurrencyCode, string StatusCode, bool IsAccepted);
public sealed record AdminProviderTransactionResponse(Guid Id, string ServiceName, string StatusCode, decimal AgreedAmount, string CurrencyCode,
    DateTime? StartedAt, DateTime? CompletedAt);
public sealed record AdminProviderApprovalEventResponse(string? FromStatusCode, string ToStatusCode, string ActionCode, string? Reason, DateTime DecidedAt);
public sealed record AdminProviderApprovalDecisionRequest(string ActionCode, string? Reason, string RowVersion);
public sealed record AdminProviderApprovalDecisionResponse(string ApprovalStatusCode, string ActivityStatusCode, DateTime DecidedAt, string? Reason, string RowVersion);
public sealed record AdminProviderServiceApprovalEventResponse(string? FromStatusCode, string ToStatusCode, string ActionCode, string? DecisionReason, DateTime DecidedAt);
public sealed record AdminProviderServiceApprovalDecisionRequest(string ActionCode, string? DecisionReason, string RowVersion);
public sealed record AdminProviderServiceBulkApprovalDecisionRequest(string ActionCode, string? DecisionReason,
    IReadOnlyList<AdminProviderServiceApprovalVersionRequest> Services);
public sealed record AdminProviderServiceApprovalVersionRequest(Guid ServiceId, string RowVersion);
public sealed record AdminProviderServiceApprovalDecisionResponse(Guid ServiceId, string ApprovalStatusCode, DateTime ApprovalRequestedAt,
    DateTime? ApprovalDecidedAt, string? DecisionReason, string RowVersion, IReadOnlyList<AdminProviderServiceApprovalEventResponse> History);
