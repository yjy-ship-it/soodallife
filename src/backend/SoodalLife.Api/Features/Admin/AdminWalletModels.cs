namespace SoodalLife.Api.Features.Admin;

public sealed record AdminWalletSummaryResponse(int TotalWalletCount, decimal TotalAvailableBalance, decimal TodayChargeAmount,
    decimal TodayFeeUseAmount, decimal TodayRestoreAmount, decimal RefundInProgressAmount, bool DevelopmentManualConfirmationAvailable);
public sealed record AdminWalletListResponse(AdminWalletSummaryResponse Summary, int TotalCount, int Page, int PageSize,
    IReadOnlyList<AdminWalletListItemResponse> Items);
public sealed record AdminWalletListItemResponse(Guid ProviderId, Guid WalletId, string ProviderName, string BusinessName,
    string? MaskedPhone, string? MaskedEmail, string StatusCode, decimal AvailableBalance, decimal ReservedBalance,
    DateTime? LastChargeAt, DateTime? LastUseAt, DateTime? LastLedgerAt, bool RefundInProgress, bool WithdrawalRefundRequired,
    bool LedgerBalanceMatches, string RowVersion);
public sealed record AdminWalletDetailResponse(AdminWalletIdentityResponse Provider, AdminWalletBalanceSummaryResponse Balance,
    IReadOnlyList<AdminWalletLedgerResponse> Ledger, IReadOnlyList<AdminWalletChargeResponse> Charges,
    IReadOnlyList<AdminFeeChargeResponse> FeeCharges, IReadOnlyList<AdminFeeRestoreResponse> Restores,
    IReadOnlyList<AdminWalletRefundResponse> Refunds, IReadOnlyList<AdminWalletAuditResponse> ManagementHistory,
    bool DevelopmentManualConfirmationAvailable);
public sealed record AdminWalletIdentityResponse(Guid ProviderId, string ProviderName, string BusinessName, string? MaskedPhone, string? MaskedEmail);
public sealed record AdminWalletBalanceSummaryResponse(Guid WalletId, string CurrencyCode, decimal AvailableBalance, decimal ReservedBalance,
    decimal RefundableAmount, decimal RefundInProgressAmount, string StatusCode, bool WithdrawalRefundRequired,
    bool HasPendingHold, bool LedgerBalanceMatches, decimal? LatestLedgerBalance, string RowVersion);
public sealed record AdminWalletLedgerResponse(Guid Id, DateTime OccurredAt, string EntryTypeCode, decimal Amount, decimal BalanceAfter,
    string Reason, string? ReferenceType, Guid? ReferencePublicId, string? PaymentMethodCode, Guid? TransactionId, string ProcessedBy);
public sealed record AdminWalletChargeResponse(Guid Id, decimal RequestedAmount, string PaymentMethodCode, string StatusCode,
    DateTime RequestedAt, DateTime? CompletedAt, string? FailureReason, string? ExternalPaymentReference, string RowVersion);
public sealed record AdminFeeChargeResponse(Guid Id, Guid TransactionId, Guid CategoryFeePolicyId, decimal FeeAmount,
    DateTime ChargedAt, string RestoreStatusCode, Guid LedgerEntryId, Guid? RestoreLedgerEntryId, string RowVersion);
public sealed record AdminFeeRestoreResponse(Guid Id, Guid FeeChargeId, string ReasonCode, string Reason, DateTime RestoredAt, string ProcessedBy);
public sealed record AdminWalletRefundResponse(Guid Id, decimal RequestedAmount, string StatusCode, string RequestReason,
    DateTime RequestedAt, DateTime? ReviewedAt, string? ReviewReason, DateTime? CompletedAt, string? FailureReason, string RowVersion);
public sealed record AdminWalletAuditResponse(DateTime OccurredAt, string ActionCode, string ResultCode, string? Reason, string? ActorRoleCode);

public sealed record AdminDevelopmentChargeRequest(decimal Amount, string Reason, string IdempotencyKey);
public sealed record AdminChargeConfirmationRequest(string RowVersion);
public sealed record AdminWalletAdjustmentRequest(string DirectionCode, decimal Amount, string Reason, string IdempotencyKey, string RowVersion);
public sealed record AdminFeeRestoreRequest(string ReasonCode, string Reason, string IdempotencyKey, string RowVersion);
public sealed record AdminWalletRefundCreateRequest(decimal Amount, string Reason, string IdempotencyKey);
public sealed record AdminWalletRefundReviewRequest(string Reason, string RowVersion);
public sealed record AdminWalletStatusRequest(string StatusCode, string Reason, string RowVersion);
public sealed record AdminChargeRequestResponse(Guid Id, Guid WalletId, decimal RequestedAmount, string StatusCode, DateTime RequestedAt,
    DateTime? CompletedAt, string RowVersion);
public sealed record AdminRefundRequestResponse(Guid Id, Guid WalletId, decimal RequestedAmount, string StatusCode, DateTime RequestedAt,
    DateTime? ReviewedAt, DateTime? CompletedAt, string RowVersion);
