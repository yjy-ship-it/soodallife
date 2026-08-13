namespace SoodalLife.Api.Features.Work;

public sealed record ProviderCaseSource(Guid? TransactionId, Guid? SubscriptionVisitId, Guid? InteriorProjectId, Guid? AfterServiceId);
public sealed record ProviderCaseFile(Guid Id, string FileName, string ContentType, long SizeBytes, string? Role, string? Description,
    string SourceType, string PublicationMode, string? DownloadUrl, string PublicationStatus="AVAILABLE", string? PublicationMessage=null);
public sealed record ProviderAfterServiceTimeline(string ActionType, string Status, string DisplayStatus, string? Note,
    DateTime? ScheduledAt, DateTime? PerformedAt, DateTime OccurredAt);
public sealed record ProviderAfterServiceListItem(Guid Id, string CaseNumber, string Subject, string Status, string DisplayStatus,
    string SourceType, DateTime ReceivedAt, DateTime? ScheduledAt, bool ContactAvailable);
public sealed record ProviderAfterServiceDetail(Guid Id, string CaseNumber, ProviderCaseSource Source, string Subject,
    string Description, string? RequestDetails, string Status, string DisplayStatus, DateTime ReceivedAt,
    DateOnly? WarrantyStartDate, DateOnly? WarrantyEndDate, bool? IsWithinWarranty, DateTime? DueAt,
    DateTime? ProviderConfirmedAt, string? ProviderResponse, bool? VisitRequired, DateTime? StartedAt,
    DateTime? CompletedAt, string? ResolutionSummary, string? UnresolvedReason, bool? RecurrenceOccurred,
    string CustomerName, string? CustomerPhone, string? DetailAddress, bool ContactAvailable, string ContactPolicy,
    string RowVersion, IReadOnlyList<ProviderAfterServiceTimeline> Timeline, IReadOnlyList<ProviderCaseFile> Evidence);

public sealed record ProviderConfirmAfterServiceInput(string Response, bool VisitRequired, string IdempotencyKey, string RowVersion);
public sealed record ProviderScheduleAfterServiceInput(DateTime ScheduledAt, string? Note, string IdempotencyKey, string RowVersion);
public sealed record ProviderAfterServiceActionInput(string ActionType, string Note, DateTime? PerformedAt,
    bool VisitOccurred, string? Materials, string? Result, string IdempotencyKey, string RowVersion);
public sealed record ProviderCompleteAfterServiceInput(bool Resolved, string Summary, string? UnresolvedReason,
    bool RecurrenceOccurred, string IdempotencyKey, string RowVersion);

public sealed record ProviderDisputeTimeline(string ActionType, string? Note, string? Reason, DateTime OccurredAt, bool IsProviderSubmission);
public sealed record ProviderDisputeListItem(Guid Id, string CaseNumber, string Subject, string Status, string DisplayStatus,
    string SourceType, DateTime ReceivedAt, DateTime? LastActionAt);
public sealed record ProviderDisputeDetail(Guid Id, string CaseNumber, ProviderCaseSource Source, string Subject,
    string CustomerClaim, string Status, string DisplayStatus, DateTime ReceivedAt, DateTime? DueAt, DateTime? ResolvedAt,
    string RowVersion, IReadOnlyList<ProviderDisputeTimeline> Timeline, IReadOnlyList<ProviderCaseFile> Evidence,
    bool ProviderMayRespond, string DecisionPolicy);
public sealed record ProviderDisputeResponseInput(string Statement, string IdempotencyKey, string RowVersion);
