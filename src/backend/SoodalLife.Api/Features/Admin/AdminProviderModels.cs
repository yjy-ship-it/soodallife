namespace SoodalLife.Api.Features.Admin;

public sealed record AdminProviderListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminProviderListItemResponse> Items);
public sealed record AdminProviderListItemResponse(Guid Id, string ProviderName, string BusinessName, string? MaskedPhone, string? MaskedEmail,
    string? MaskedBusinessRegistrationNo, string AccountStatusCode, string ApprovalStatusCode, string ActivityStatusCode,
    int RegisteredServiceCount, int? ApprovedServiceCount, string EvidenceSummary, DateTime JoinedAt, DateTime? LastActiveAt, IReadOnlyList<string> Roles);

public sealed record AdminProviderDetailResponse(AdminProviderBasicResponse Basic, AdminProviderSummaryResponse Summary,
    AdminProviderBusinessResponse Business, IReadOnlyList<AdminProviderServiceResponse> Services, IReadOnlyList<AdminProviderAreaResponse> Areas,
    IReadOnlyList<AdminProviderDocumentResponse> Documents, IReadOnlyList<AdminProviderServiceReviewResponse> ServiceReviews,
    IReadOnlyList<AdminProviderQuoteResponse> Quotes, IReadOnlyList<AdminProviderTransactionResponse> Transactions,
    IReadOnlyList<AdminProviderApprovalEventResponse> ApprovalHistory, IReadOnlyList<AdminCustomerAuditResponse> ManagementHistory);

public sealed record AdminProviderBasicResponse(Guid Id, Guid UserId, string ProviderName, string? Phone, string? Email, string AccountStatusCode,
    string ApprovalStatusCode, string ActivityStatusCode, DateTime JoinedAt, DateTime? LastLoginAt, IReadOnlyList<string> Roles);
public sealed record AdminProviderSummaryResponse(int RegisteredServiceCount, int? ApprovedServiceCount, int? PendingServiceCount,
    int SubmittedDocumentCount, int? VerifiedDocumentCount, int ExpiringDocumentCount, int ExpiredDocumentCount,
    int SubmittedQuoteCount, int AcceptedQuoteCount, int CompletedTransactionCount, DateTime? LastActiveAt);
public sealed record AdminProviderBusinessResponse(string BusinessName, string? BusinessRegistrationNo, bool DetailFieldsSupported, string Message);
public sealed record AdminProviderServiceResponse(long InternalId, Guid ServiceId, string MajorName, string MiddleName, string ServiceName,
    string RegistrationStatusCode, string ApprovalStatus, DateTime ActivatedAt, DateTime? DeactivatedAt);
public sealed record AdminProviderAreaResponse(Guid ServiceId, string ServiceName, Guid AreaId, string AreaName, string AreaLevelCode,
    string StatusCode, DateTime ActivatedAt, DateTime? DeactivatedAt, bool IsPrioritySupported);
public sealed record AdminProviderDocumentResponse(long InternalId, string DocumentTypeName, string DocumentTypeCode, string? DocumentNumber,
    DateOnly? IssuedAt, DateOnly? ExpiresAt, string VerificationStatusCode, DateTime? VerifiedAt, string? VerifierName,
    string OriginalFileName, bool CanOpenFile, string FileAccessMessage);
public sealed record AdminProviderServiceReviewResponse(Guid ServiceId, string ServiceName, string RegistrationStatusCode, bool StructuredRequirementsConfigured,
    string LegacyQualificationText, string LegacyInsuranceText, string LegacySafetyGradeCode, IReadOnlyList<AdminProviderRequirementComparisonResponse> Requirements);
public sealed record AdminProviderRequirementComparisonResponse(Guid AssignmentId, string RequirementName, bool IsRequired, bool VerificationRequired,
    bool ExpiryCheckRequired, IReadOnlyList<string> RequiredEvidenceTypes, string VerificationStatusCode, string? LinkedDocumentType,
    DateTime? VerifiedAt, DateOnly? ExpiresAt, string? RejectionReason);
public sealed record AdminProviderQuoteResponse(Guid Id, Guid RequestId, string RequestTitle, string ServiceName, DateTime? SubmittedAt,
    decimal? TotalAmount, string CurrencyCode, string StatusCode, bool IsAccepted);
public sealed record AdminProviderTransactionResponse(Guid Id, string ServiceName, string StatusCode, decimal AgreedAmount, string CurrencyCode,
    DateTime? StartedAt, DateTime? CompletedAt);
public sealed record AdminProviderApprovalEventResponse(string? FromStatusCode, string ToStatusCode, string ActionCode, string? Reason, DateTime DecidedAt);
