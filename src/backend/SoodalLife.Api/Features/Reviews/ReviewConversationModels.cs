namespace SoodalLife.Api.Features.Reviews;

public sealed record CreateReviewCommentRequest(string BodyText, Guid? ParentCommentId, string IdempotencyKey);
public sealed record ReviewCommentResponse(Guid Id, Guid? ParentCommentId, string AuthorRole, string AuthorDisplayName, string BodyText, string Status, DateTime SubmittedAt, int Depth, string RowVersion);
public sealed record ReviewConversationResponse(Guid ReviewId, Guid? TransactionId, string SourceLabel, Guid ProviderId, string ProviderName, string CustomerDisplayName, string ReviewBody, string VisibilityStatus, DateTime SubmittedAt, DateTime LatestActivityAt, int UnreadCommentCount, IReadOnlyList<ReviewCommentResponse> Comments);
public sealed record ReviewConversationUnreadCountResponse(int Count);
public sealed record ModerateReviewCommentRequest(string TargetStatus, string Reason, string RowVersion);
