namespace SoodalLife.Api.Features.Reviews;

public sealed record CreateReviewRequest(string BodyText, IReadOnlyList<CreateReviewRatingRequest> Ratings, IReadOnlyList<Guid> FileIds, string IdempotencyKey);
public sealed record CreateReviewRatingRequest(Guid RatingItemId, decimal RatingValue);
public sealed record CreateProviderReplyRequest(string BodyText, string IdempotencyKey);
public sealed record ReviewResponse(Guid Id, Guid TransactionId, string TransactionNumber, Guid ProviderId, string ProviderName,
    string CustomerDisplayName, string BodyText, decimal? OverallRating, string VerificationStatusCode,
    string VisibilityStatusCode, DateTime SubmittedAt, IReadOnlyList<ReviewRatingResponse> Ratings,
    IReadOnlyList<ReviewFileResponse> Files, ReviewReplyResponse? ProviderReply);
public sealed record ReviewRatingResponse(Guid ItemId, string ItemCode, string ItemName, decimal RatingValue, decimal MinValue, decimal MaxValue, int DisplayOrder);
public sealed record ReviewFileResponse(
    Guid FileId,
    string FileName,
    string ContentType,
    int DisplayOrder,
    string? DownloadUrl = null,
    string? PublicationMode = null);
public sealed record ReviewReplyResponse(Guid Id, string ProviderName, string BodyText, DateTime SubmittedAt);
public sealed record PublicReviewListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<ReviewResponse> Items);
public sealed record ProviderReviewStatisticsResponse(Guid ProviderId, int ReviewCount, int PublicReviewCount, IReadOnlyList<RatingItemAverageResponse> RatingItemAverages);
public sealed record RatingItemAverageResponse(Guid ItemId, string ItemCode, string ItemName, decimal AverageValue, int RatingCount, decimal MinValue, decimal MaxValue);
public sealed record ReviewRatingItemOption(Guid Id, string Code, string Name, string? Description, decimal MinValue, decimal MaxValue, bool IsRequired, int DisplayOrder);
public sealed record ReviewUploadResponse(Guid FileId, string FileName, string ContentType, long SizeBytes, string ScanStatus);

public sealed class ReviewBusinessException(int statusCode,string businessCode,string message,string? field=null):Exception(message)
{
    public int StatusCode { get; }=statusCode; public string BusinessCode { get; }=businessCode; public string? Field { get; }=field;
}
