namespace SoodalLife.Api.Features.Work;

public sealed record CreateCustomerDisputeInput(string Subject, string Reason, string RequestedResolution, string IdempotencyKey);
public sealed record CustomerDisputeEvidenceResponse(Guid Id, Guid? FileId, string SourceType, string? Description, DateTime SubmittedAt, string? DownloadUrl);
public sealed record CustomerDisputeUpdate(string DisplayStatus, DateTime OccurredAt);
public sealed record CustomerDisputeResponse(Guid Id, Guid TransactionId, string Subject, string Reason, string RequestedResolution,
    string Status, string DisplayStatus, DateTime ReceivedAt, DateTime? ResolvedAt, string? ResultSummary, string? FollowUpAction,
    IReadOnlyList<CustomerDisputeEvidenceResponse> Evidence, IReadOnlyList<CustomerDisputeUpdate> PublicUpdates);
