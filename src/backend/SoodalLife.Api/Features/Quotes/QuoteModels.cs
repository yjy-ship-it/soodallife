using System.Text.Json.Serialization;

namespace SoodalLife.Api.Features.Quotes;

public sealed record QuoteItemInput(
    string ItemName,
    string? Description,
    decimal Quantity,
    string? UnitText,
    decimal UnitPriceAmount,
    string? WorkTradeText = null,
    string? SpaceText = null,
    string? ItemCategoryCode = null,
    string? MaterialSpecText = null,
    string? LaborNoteText = null);

public sealed record SaveQuoteRevisionInput(
    string Summary,
    string? Terms,
    decimal VatAmount,
    string? EstimatedDurationText,
    DateTime? AvailableStartAt,
    DateTime ValidUntil,
    string? RevisionReason,
    string IdempotencyKey,
    IReadOnlyList<QuoteItemInput> Items,
    string? RevisionPurposeCode = null,
    string? VatMode = null);

public sealed record SaveQuoteTemplateInput(string Name, string Summary, string? Terms, string? EstimatedDurationText,
    string VatMode, IReadOnlyList<QuoteItemInput> Items);

public sealed record ProviderQuoteTemplateResponse(Guid Id, string Name, string Summary, string? Terms,
    string? EstimatedDurationText, string VatMode, IReadOnlyList<QuoteItemInput> Items, DateTime UpdatedAt);

public sealed record QuoteItemResponse(
    int LineNo,
    string ItemName,
    string? Description,
    decimal Quantity,
    string? UnitText,
    decimal UnitPriceAmount,
    decimal LineTotalAmount,
    string CurrencyCode,
    string? WorkTradeText,
    string? SpaceText,
    string? ItemCategoryCode,
    string? MaterialSpecText,
    string? LaborNoteText);

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
    IReadOnlyList<QuoteItemResponse> Items,
    string? RevisionPurposeCode);

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
    Guid RequestId,
    string RequestTitle,
    string Domain,
    string CategoryPath,
    string ProviderName,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    DateTime? SubmittedAt,
    int RevisionNo,
    DateTime ValidUntil,
    bool IsSelected,
    Guid? TransactionId);

public sealed record CustomerRatingAverageResponse(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    decimal AverageValue,
    int RatingCount,
    decimal MinValue,
    decimal MaxValue);

public sealed record CustomerQuoteComparisonResponse(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string Status,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    string CurrencyCode,
    DateTime? SubmittedAt,
    int RevisionNo,
    DateTime ValidUntil,
    DateTime? AvailableStartAt,
    string? EstimatedDurationText,
    string? Terms,
    IReadOnlyList<string> IncludedItems,
    int DefaultWarrantyDays,
    decimal? TrustScore,
    string? TrustGrade,
    string TrustEvaluationStatus,
    string TrustDisplay,
    int ReviewCount,
    int PublicReviewCount,
    IReadOnlyList<CustomerRatingAverageResponse> RatingItemAverages,
    string ProviderApprovalStatus,
    string ServiceApprovalStatus,
    bool RequirementsConfigured,
    bool RequiredEvidenceSatisfied,
    bool IsSelected);

public sealed record CustomerQuoteDetailResponse(
    Guid Id,
    Guid RequestId,
    Guid ProviderId,
    string ProviderName,
    string Status,
    DateTime? SubmittedAt,
    DateTime? AcceptedAt,
    DateTime? ExpiresAt,
    bool CanEdit,
    bool CanSubmit,
    QuoteRevisionResponse Revision,
    Guid? TransactionId,
    CustomerQuoteComparisonResponse Comparison);

public sealed record CustomerProviderReviewResponse(
    Guid Id,
    string BodyText,
    DateTime SubmittedAt,
    IReadOnlyList<CustomerRatingAverageResponse> Ratings);

public sealed record CustomerProviderProfileResponse(
    Guid Id,
    string BusinessName,
    string? IntroductionHtml,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PublicPhone,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PublicEmail,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PublicAddress,
    string? PublicBlogUrl,
    string? PublicWebsiteUrl,
    string? PublicLogoUrl,
    IReadOnlyList<string> PublicPhotoUrls,
    string ApprovalStatus,
    string ActivityStatus,
    string ServiceApprovalStatus,
    IReadOnlyList<string> ActiveServices,
    decimal? TrustScore,
    string? TrustGrade,
    string TrustEvaluationStatus,
    string TrustDisplay,
    int CompletedServiceCount,
    int PublicReviewCount,
    IReadOnlyList<CustomerRatingAverageResponse> RatingItemAverages,
    bool RequirementsConfigured,
    int RequiredEvidenceCount,
    int ApprovedEvidenceCount,
    bool RequiredEvidenceSatisfied,
    IReadOnlyList<CustomerProviderReviewResponse> RecentReviews);

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

public sealed record AcceptQuoteRequest(string DetailAddress);

public sealed record AcceptQuoteResponse(
    Guid TransactionId,
    Guid QuoteId,
    Guid RequestId,
    string TransactionStatus,
    decimal AgreedAmount,
    string CurrencyCode,
    [property: JsonIgnore] decimal? ChargedFeeAmount = null,
    [property: JsonIgnore] Guid? WalletLedgerEntryId = null);

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
