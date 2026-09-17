namespace SoodalLife.Api.Features.Wallet;

public sealed record WalletBalanceResponse(Guid WalletId, Guid ProviderId, string CurrencyCode, decimal AvailableBalance,
    decimal ReservedBalance, string StatusCode, bool HasSufficientBalance, string RowVersion);
public sealed record WalletOperationResponse(Guid WalletId, decimal AvailableBalance, decimal ReservedBalance,
    Guid LedgerEntryId, string EntryTypeCode, decimal Amount, decimal BalanceAfter, string RowVersion);
public sealed record DebitFeeCommand(Guid ProviderId, Guid TransactionId, Guid CategoryFeePolicyId, decimal Amount,
    string IdempotencyKey, string Reason);
public sealed record ProviderWalletDashboardResponse(
    Guid WalletId,
    Guid ProviderId,
    string CurrencyCode,
    decimal AvailableBalance,
    decimal ReservedBalance,
    string StatusCode,
    decimal TotalCharged,
    decimal TotalUsed,
    decimal TotalRestored,
    decimal TotalRefunded,
    IReadOnlyList<ProviderWalletLedgerResponse> RecentLedger,
    IReadOnlyList<ProviderWalletChargeResponse> Charges,
    IReadOnlyList<ProviderWalletFeeResponse> FeeCharges,
    IReadOnlyList<ProviderWalletRefundResponse> Refunds,
    bool ActualPaymentIntegrated,
    string PaymentMode,
    IReadOnlyList<decimal> TopUpProducts,
    decimal MonthlyPurchaseLimit,
    decimal MonthlyPurchasedAmount,
    bool ProviderRefundRequestSupported,
    bool WithdrawalRefundRequired,
    string ChargeGuidance,
    string RefundGuidance,
    string RowVersion);
public sealed record ProviderWalletLedgerResponse(Guid Id, DateTime OccurredAt, string EntryTypeCode, decimal Amount,
    decimal SupplyAmount, decimal VatAmount, string TaxTreatmentCode, decimal BalanceAfter, string Reason,
    string? ReferenceType, Guid? ReferenceId, Guid? TransactionId, string? PaymentMethodCode);
public sealed record ProviderWalletChargeResponse(Guid Id, decimal RequestedAmount, string PaymentMethodCode,
    string StatusCode, DateTime RequestedAt, DateTime? CompletedAt, string? FailureReason);
public sealed record ProviderWalletFeeResponse(Guid Id, Guid TransactionId, string ServiceName, decimal FeeAmount,
    decimal SupplyAmount, decimal VatAmount, bool IsVatIncluded, DateTime ChargedAt, string RestoreStatusCode);
public sealed record ProviderWalletRefundResponse(Guid Id, decimal RequestedAmount, string StatusCode,
    string RequestReason, DateTime RequestedAt, DateTime? ReviewedAt, DateTime? CompletedAt, string? FailureReason);

public sealed class WalletOperationException(string businessCode, string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}

public interface IWalletPaymentGateway
{
    Task<string> CreatePaymentAsync(Guid chargeRequestId, decimal amount, string currencyCode, CancellationToken cancellationToken);
    Task ConfirmPaymentAsync(string externalPaymentReference, CancellationToken cancellationToken);
    Task CancelPaymentAsync(string externalPaymentReference, CancellationToken cancellationToken);
    Task<string> RefundPaymentAsync(Guid refundRequestId, decimal amount, string currencyCode, CancellationToken cancellationToken);
}
