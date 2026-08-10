namespace SoodalLife.Api.Features.Wallet;

public sealed record WalletBalanceResponse(Guid WalletId, Guid ProviderId, string CurrencyCode, decimal AvailableBalance,
    decimal ReservedBalance, string StatusCode, bool HasSufficientBalance, string RowVersion);
public sealed record WalletOperationResponse(Guid WalletId, decimal AvailableBalance, decimal ReservedBalance,
    Guid LedgerEntryId, string EntryTypeCode, decimal Amount, decimal BalanceAfter, string RowVersion);
public sealed record DebitFeeCommand(Guid ProviderId, Guid TransactionId, Guid CategoryFeePolicyId, decimal Amount,
    string IdempotencyKey, string Reason);

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
