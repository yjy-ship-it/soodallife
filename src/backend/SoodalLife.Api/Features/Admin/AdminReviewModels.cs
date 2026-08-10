using SoodalLife.Api.Features.Reviews;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminReviewListResponse(int TotalCount,int Page,int PageSize,IReadOnlyList<AdminReviewListItemResponse> Items);
public sealed record AdminReviewListItemResponse(Guid Id,string ReviewNumber,Guid TransactionId,string TransactionNumber,string ServiceName,string CustomerName,string ProviderName,DateTime SubmittedAt,string VerificationStatusCode,string VisibilityStatusCode,IReadOnlyList<ReviewRatingResponse> Ratings);
public sealed record AdminReviewDetailResponse(Guid Id,string ReviewNumber,string BodyText,decimal? OverallRating,string VerificationStatusCode,string VisibilityStatusCode,DateTime SubmittedAt,DateTime? PublishedAt,DateTime? HiddenAt,string RowVersion,AdminReviewTransactionResponse Transaction,AdminReviewPartyResponse Customer,AdminReviewPartyResponse Provider,IReadOnlyList<ReviewRatingResponse> Ratings,IReadOnlyList<ReviewFileResponse> Files,ReviewReplyResponse? ProviderReply,IReadOnlyList<AdminReviewAuditResponse> Audit);
public sealed record AdminReviewTransactionResponse(Guid Id,string Number,string StatusCode,string ServiceName,string RequestTitle,decimal AgreedAmount,string CurrencyCode,DateTime? CompletedAt);
public sealed record AdminReviewPartyResponse(Guid Id,string Name,string? Phone,string? Email);
public sealed record AdminReviewAuditResponse(DateTime OccurredAt,string ActionCode,string? Reason,string? BeforeJson,string? AfterJson);
public sealed record ChangeReviewVisibilityRequest(string TargetStatusCode,string Reason,string RowVersion);
