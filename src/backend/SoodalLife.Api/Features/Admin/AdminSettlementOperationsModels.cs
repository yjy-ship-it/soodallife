namespace SoodalLife.Api.Features.Admin;

public sealed record AdminSettlementSummaryResponse(
    decimal WalletAvailableBalance,
    decimal WalletReservedBalance,
    int ReservedQuoteFeeCount,
    decimal ReservedQuoteFeeAmount,
    decimal CapturedQuoteFeeAmount,
    int FailedSubscriptionPaymentCount,
    decimal CompletedSubscriptionPaymentAmount,
    int PendingMonthlySettlementCount,
    decimal PendingMonthlySettlementAmount,
    int PendingPayoutCount,
    decimal PendingPayoutAmount,
    int PendingAdvertisingFeeCount,
    decimal PendingAdvertisingFeeAmount,
    int OpenRefundAdjustmentCount,
    decimal OpenRefundAdjustmentAmount);

public sealed record AdminQuoteFeeOperationResponse(Guid Id, Guid QuoteId, Guid ProviderId, string ProviderName,
    decimal Amount, string CurrencyCode, string StatusCode, DateTime ReservedAt, DateTime? CapturedAt,
    DateTime? ReleasedAt, string? ReleaseReasonCode);

public sealed record AdminSubscriptionPaymentOperationResponse(Guid Id, Guid ContractId, string ProviderName,
    DateOnly BillingPeriodStart, DateOnly BillingPeriodEnd, decimal RequestedAmount, string CurrencyCode,
    string StatusCode, DateTime RequestedAt, DateTime? CompletedAt, string? FailureReason);

public sealed record AdminMonthlySettlementOperationResponse(Guid Id, Guid ProviderId, string ProviderName,
    int Year, int Month, string StatusCode, decimal GrossTotal, decimal FeeTotal, decimal AdjustmentTotal,
    decimal NetTotal, int ItemCount, DateTime? ApprovedAt, DateTime? PaidAt);

public sealed record AdminPayoutOperationResponse(Guid Id, Guid MonthlySettlementId, Guid ProviderId,
    string ProviderName, decimal RequestedAmount, decimal? ApprovedAmount, string StatusCode,
    DateTime RequestedAt, DateTime? CompletedAt, string? BankTransferReference);

public sealed record AdminAdvertisingFeeOperationResponse(Guid Id, string CampaignName, Guid ProviderId,
    string ProviderName, decimal FeeAmount, string CurrencyCode, string StatusCode, string FeeStatusCode,
    bool AutoRenewEnabled, DateTime SubmittedAt, DateTime? PublishedAt, DateTime? NextRenewalAt);

public sealed record AdminRefundAdjustmentOperationResponse(Guid Id, string SourceCode, Guid? ProviderId,
    string PartyName, string TypeCode, decimal RequestedAmount, decimal? ApprovedAmount, string StatusCode,
    string Reason, DateTime RequestedAt, DateTime? CompletedAt);

public sealed record AdminSettlementOperationsDashboardResponse(DateTime GeneratedAt,
    AdminSettlementSummaryResponse Summary,
    IReadOnlyList<AdminQuoteFeeOperationResponse> QuoteFees,
    IReadOnlyList<AdminSubscriptionPaymentOperationResponse> SubscriptionPayments,
    IReadOnlyList<AdminMonthlySettlementOperationResponse> MonthlySettlements,
    IReadOnlyList<AdminPayoutOperationResponse> Payouts,
    IReadOnlyList<AdminAdvertisingFeeOperationResponse> AdvertisingFees,
    IReadOnlyList<AdminRefundAdjustmentOperationResponse> RefundAdjustments);

public sealed record AdminUnifiedLedgerItemResponse(string SourceCode, Guid? Id, DateTime OccurredAt,
    string EntryTypeCode, decimal? Amount, string CurrencyCode, string PartyName, string StatusCode,
    string? ReferenceType, Guid? ReferenceId, string? Reason, string? ActorRoleCode);

public sealed record AdminUnifiedLedgerResponse(int TotalCount, int Page, int PageSize,
    IReadOnlyList<AdminUnifiedLedgerItemResponse> Items);
