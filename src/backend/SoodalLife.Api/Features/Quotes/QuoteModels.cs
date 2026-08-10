namespace SoodalLife.Api.Features.Quotes;

public sealed record QuoteItemInput(
    string ItemName,
    string? Description,
    decimal Quantity,
    string? UnitText,
    decimal UnitPriceAmount);

public sealed record SaveQuoteRevisionInput(
    string Summary,
    string? Terms,
    decimal VatAmount,
    string? EstimatedDurationText,
    DateTime? AvailableStartAt,
    DateTime ValidUntil,
    string? RevisionReason,
    string IdempotencyKey,
    IReadOnlyList<QuoteItemInput> Items);

public sealed record QuoteItemResponse(
    int LineNo,
    string ItemName,
    string? Description,
    decimal Quantity,
    string? UnitText,
    decimal UnitPriceAmount,
    decimal LineTotalAmount,
    string CurrencyCode);

public sealed record QuoteRevisionResponse(
    Guid Id,
    int RevisionNo,
    string Summary,
    string? Terms,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    string CurrencyCode,
    string? EstimatedDurationText,
    DateTime? AvailableStartAt,
    DateTime ValidUntil,
    string? RevisionReason,
    DateTime RecordedAt,
    IReadOnlyList<QuoteItemResponse> Items);

public sealed record QuoteDetailResponse(
    Guid Id,
    Guid RequestId,
    string ProviderName,
    string Status,
    DateTime? SubmittedAt,
    DateTime? AcceptedAt,
    DateTime? ExpiresAt,
    bool CanEdit,
    bool CanSubmit,
    QuoteRevisionResponse Revision,
    Guid? TransactionId);

public sealed record QuoteListItemResponse(
    Guid Id,
    string ProviderName,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    DateTime? SubmittedAt,
    int RevisionNo,
    DateTime ValidUntil,
    bool IsSelected);

public sealed record QuoteSubmissionReadinessResponse(
    Guid RequestId,
    Guid ProviderId,
    Guid CategoryFeePolicyId,
    string PolicyVersion,
    decimal ExpectedAcceptanceFee,
    string CurrencyCode,
    decimal AvailableWalletBalance,
    string WalletStatus,
    bool CanSubmit,
    string? UnavailableReason);

public sealed record AcceptQuoteResponse(
    Guid TransactionId,
    Guid QuoteId,
    Guid RequestId,
    string TransactionStatus,
    decimal AgreedAmount,
    string CurrencyCode,
    decimal? ChargedFeeAmount = null,
    Guid? WalletLedgerEntryId = null);

public sealed class QuoteBusinessException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status409Conflict,
    IReadOnlyDictionary<string, string[]>? fieldErrors = null) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; } = fieldErrors;
}
