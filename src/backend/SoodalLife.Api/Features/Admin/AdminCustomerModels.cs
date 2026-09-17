namespace SoodalLife.Api.Features.Admin;

public sealed record AdminCustomerListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminCustomerListItemResponse> Items);
public sealed record AdminCustomerRequestAbuseExclusionRequest(bool Excluded, string Reason);

public sealed record AdminCustomerListItemResponse(
    Guid Id,
    string Name,
    string? MaskedPhone,
    string? MaskedEmail,
    string StatusCode,
    DateTime JoinedAt,
    IReadOnlyList<string> Roles,
    int RequestCount,
    int TransactionCount,
    DateTime? LastUsedAt);

public sealed record AdminCustomerDetailResponse(
    AdminCustomerBasicResponse Basic,
    AdminCustomerUsageSummaryResponse Usage,
    AdminCustomerAddressSectionResponse Addresses,
    IReadOnlyList<AdminCustomerRequestResponse> Requests,
    IReadOnlyList<AdminCustomerQuoteResponse> Quotes,
    IReadOnlyList<AdminCustomerTransactionResponse> Transactions,
    AdminCustomerReviewSectionResponse Reviews,
    IReadOnlyList<AdminCustomerAfterServiceResponse> AfterServices,
    IReadOnlyList<AdminCustomerServiceHistoryResponse> ServiceHistory,
    AdminCustomerConsentSectionResponse Consents,
    AdminCustomerStatusResponse Status,
    IReadOnlyList<AdminCustomerAuditResponse> ManagementHistory);

public sealed record AdminCustomerBasicResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Phone,
    string? Email,
    string StatusCode,
    DateTime JoinedAt,
    DateTime? LastLoginAt,
    bool IdentityVerificationSupported,
    string IdentityVerificationStatus,
    IReadOnlyList<string> Roles);

public sealed record AdminCustomerUsageSummaryResponse(
    int TotalRequestCount,
    int InProgressRequestCount,
    int CompletedTransactionCount,
    int InProgressAfterServiceCount,
    DateTime? LastUsedAt);

public sealed record AdminCustomerAddressSectionResponse(bool IsSupported, string Message, IReadOnlyList<AdminCustomerAddressResponse> Items);
public sealed record AdminCustomerAddressResponse(string Alias, bool IsDefault, string? Region, string Address, string? DetailAddress, DateTime RegisteredAt);

public sealed record AdminCustomerRequestResponse(
    Guid Id,
    string Title,
    string ServiceName,
    DateTime CreatedAt,
    string StatusCode,
    string AreaName,
    string? DetailAddress,
    int QuoteCount,
    bool HasAcceptedQuote,
    bool AbuseCountExcluded,
    string? AbuseExclusionReason);

public sealed record AdminCustomerQuoteResponse(
    Guid Id,
    Guid RequestId,
    string RequestTitle,
    string ProviderName,
    decimal? TotalAmount,
    string CurrencyCode,
    string StatusCode,
    DateTime? SubmittedAt,
    bool IsAccepted,
    DateTime? AcceptedAt);

public sealed record AdminCustomerTransactionResponse(
    Guid Id,
    Guid RequestId,
    string ServiceName,
    string ProviderName,
    string StatusCode,
    decimal AgreedAmount,
    decimal? ActualAmount,
    string CurrencyCode,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? VisitInformation);

public sealed record AdminCustomerReviewSectionResponse(bool IsSupported, string Message);

public sealed record AdminCustomerAfterServiceResponse(
    Guid Id,
    Guid TransactionId,
    string Subject,
    string StatusCode,
    DateTime ReceivedAt,
    DateTime? CompletedAt,
    string? ProcessingResult);

public sealed record AdminCustomerServiceHistoryResponse(
    Guid Id,
    Guid? TransactionId,
    string EventTypeCode,
    string Title,
    string Summary,
    string? ProviderName,
    string? CategoryName,
    decimal? TotalAmount,
    string? CurrencyCode,
    DateTime? CompletedAt,
    DateTime OccurredAt,
    DateOnly? WarrantyEndDate);

public sealed record AdminCustomerConsentSectionResponse(bool IsSupported, string Message);

public sealed record AdminCustomerStatusResponse(
    string CurrentStatusCode,
    IReadOnlyList<AdminCustomerRoleHistoryResponse> RoleHistory,
    bool WithdrawalWorkflowSupported,
    string WithdrawalMessage);

public sealed record AdminCustomerRoleHistoryResponse(string RoleCode, DateTime GrantedAt, DateTime? RevokedAt);

public sealed record AdminCustomerAuditResponse(
    DateTime OccurredAt,
    string ActionCode,
    string EntityType,
    string ResultCode,
    string? Reason,
    string? ActorRoleCode);
