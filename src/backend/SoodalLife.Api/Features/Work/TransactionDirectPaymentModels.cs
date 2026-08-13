namespace SoodalLife.Api.Features.Work;

public sealed class RegisterDirectPaymentInput
{
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
    public string? Note { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? TransactionRowVersion { get; init; }
    public IFormFile? Evidence { get; init; }
}

public sealed record DecideDirectPaymentInput(string Decision, string? Reason, string IdempotencyKey, string? RowVersion);

public sealed record DirectPaymentEvidenceResponse(
    Guid? FileId, string FileName, string ContentType, long SizeBytes, string? DownloadUrl,
    string PublicationStatus, string? PublicationMessage);

public sealed record TransactionDirectPaymentResponse(
    Guid Id, string Status, decimal Amount, string CurrencyCode, string PaymentMethod, DateTime PaidAt,
    string? Note, string RegisteredByRole, DateTime RegisteredAt, DateTime? DecidedAt,
    string? RejectionReason, bool CanDecide, string RowVersion, DirectPaymentEvidenceResponse? Evidence);

public sealed record DirectPaymentContextResponse(decimal AgreedAmount, string CurrencyCode, string TransactionStatus,
    string TransactionRowVersion, TransactionDirectPaymentResponse? Payment);

public sealed record DirectPaymentFileResult(Stream Stream, string ContentType, string FileName);
