namespace SoodalLife.Api.Features.Admin;

public sealed record AdminRequestListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminRequestListItem> Items);
public sealed record AdminRequestListItem(Guid Id, string RequestNumber, string CustomerName, string? MaskedPhone,
    string ServiceName, string CategoryPath, string AreaName, DateTime RequestedAt, string StatusCode,
    int MatchedProviderCount, int SubmittedQuoteCount, bool IsAccepted, bool HasTransaction);
public sealed record AdminRequestAnswerItem(string Question, string FieldType, string? Answer, bool IsRequired);
public sealed record AdminRequestCandidateItem(Guid ProviderId, string ProviderName, string StatusCode, string? ReasonCode,
    bool CategoryMatch, bool AreaMatch, bool ApprovalMatch);
public sealed record AdminRequestQuoteItem(Guid Id, Guid ProviderId, string ProviderName, DateTime? SubmittedAt,
    decimal? LatestAmount, string CurrencyCode, string StatusCode, int RevisionCount, bool IsAccepted, string ProviderApprovalStatus);
public sealed record AdminRequestDetailResponse(Guid Id, string RequestNumber, string CustomerName, string? CustomerPhone,
    string ServiceName, string CategoryPath, string AreaName, string? DetailAddress, string Title, string? Description,
    DateTime RequestedAt, string StatusCode, string PrivacyDisclosureStatus, IReadOnlyList<AdminRequestAnswerItem> Answers,
    IReadOnlyList<AdminRequestCandidateItem> Candidates, IReadOnlyList<AdminRequestQuoteItem> Quotes,
    Guid? AcceptedQuoteId, Guid? TransactionId, IReadOnlyList<AdminOperationHistoryItem> History);

public sealed record AdminTransactionListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminTransactionListItem> Items);
public sealed record AdminTransactionListItem(Guid Id, string TransactionNumber, string ServiceName, string CustomerName,
    string ProviderName, decimal AgreedAmount, decimal? ActualChargedFeeAmount, string CurrencyCode, string StatusCode,
    DateTime? StartedAt, DateTime? CompletedAt, bool HasAfterService);
public sealed record AdminTransactionDetailResponse(Guid Id, string TransactionNumber, string StatusCode, string ServiceName,
    string CategoryPath, string CustomerName, string? CustomerPhone, string ProviderName, string RequestTitle,
    string AreaName, string? DetailAddress, decimal AgreedAmount, string CurrencyCode, DateTime CreatedAt,
    DateTime? StartedAt, DateTime? CompletedAt, AdminAppliedFeePolicy? FeePolicy, AdminWalletFeeLink? WalletFee,
    IReadOnlyList<AdminTransactionQuoteItem> QuoteItems, AdminCompletionSummary Completion,
    AdminDirectPaymentSummary? DirectPayment, IReadOnlyList<AdminAfterServiceSummary> AfterServices, IReadOnlyList<AdminOperationHistoryItem> History);
public sealed record AdminDirectPaymentSummary(Guid Id, string StatusCode, decimal Amount, string CurrencyCode,
    string PaymentMethodCode, DateTime PaidAt, string RegisteredByRoleCode, DateTime RegisteredAt, DateTime? DecidedAt, string? RejectionReason);
public sealed record AdminAppliedFeePolicy(Guid? PolicyId, string? Version, string? PolicyKind, string? TransactionType,
    string? CalculationMethod, decimal? CalculatedFeeAmount, decimal? ActualChargedFeeAmount, string? CurrencyCode,
    string? ChargeTiming, string? RestoreRule);
public sealed record AdminWalletFeeLink(Guid? WalletId, Guid? LedgerId, Guid? FeeChargeId, string? LedgerType,
    decimal? LedgerAmount, decimal? BalanceAfter, string? RestoreStatus);
public sealed record AdminTransactionQuoteItem(int LineNo, string Name, string? Description, decimal Quantity,
    string? Unit, decimal UnitPrice, decimal LineTotal);
public sealed record AdminCompletionSummary(bool Exists, string? StatusCode, int RevisionCount, DateTime? SubmittedAt, DateTime? ConfirmedAt);
public sealed record AdminAfterServiceSummary(Guid Id, string Subject, string StatusCode, DateTime ReceivedAt, DateTime? CompletedAt);
public sealed record AdminOperationHistoryItem(DateTime OccurredAt, string Action, string Result, string? Reason);
