namespace SoodalLife.Api.Features.Work;

public sealed record ServiceHistoryListItem(
    Guid Id, Guid TransactionId, string ServiceName, string? CategoryName, string ProviderName,
    DateTime? CompletedAt, decimal? FinalAmount, string? CurrencyCode, DateOnly? WarrantyEndDate,
    string WarrantyDisplay, bool HasAfterService, bool HasReview, bool HasDispute);

public sealed record ServiceHistoryLine(int LineNo, string ItemName, string? Description, decimal? Quantity, string? UnitText, decimal? Amount, string? CurrencyCode);
public sealed record ServiceHistoryEvidence(Guid FileId, string FileName, string ContentType, string Role, string? Description, string? DownloadUrl, string PublicationStatus="AVAILABLE", string? PublicationMessage=null);
public sealed record ServiceHistoryAsset(Guid Id, string Type, string Name, string? Manufacturer, string? ModelName);
public sealed record ServiceHistoryDetail(
    Guid Id, Guid TransactionId, string Title, string Summary, string ServiceName, string? CategoryName,
    string ProviderName, DateTime? CompletedAt, decimal? FinalAmount, string? CurrencyCode,
    DateOnly? WarrantyStartDate, DateOnly? WarrantyEndDate, string WarrantyDisplay,
    IReadOnlyList<ServiceHistoryLine> Items, IReadOnlyList<ServiceHistoryEvidence> CompletionEvidence,
    IReadOnlyList<ServiceHistoryAsset> Assets, WorkRelatedCase? AfterService, WorkRelatedCase? Dispute,
    WorkReviewState Review);

public sealed record CreateAfterServiceInput(
    string Subject, string Description, string? RequestDetails, DateTime? DesiredVisitAt,
    string IdempotencyKey);
public sealed record AfterServiceEvidenceResponse(Guid FileId, string FileName, string ContentType, long SizeBytes, string? Role, string? Description, string? DownloadUrl, string PublicationStatus="AVAILABLE", string? PublicationMessage=null);
public sealed record AfterServiceTimelineItem(string Status, string DisplayStatus, string ActionType, string? PublicNote, DateTime? ScheduledAt, DateTime? PerformedAt, DateTime OccurredAt);
public sealed record CustomerAfterServiceResponse(
    Guid Id, Guid? TransactionId, Guid? InteriorProjectId, Guid? ServiceHistoryId, string Subject, string Description, string? RequestDetails,
    string Status, string DisplayStatus, DateTime ReceivedAt, DateOnly? WarrantyStartDate, DateOnly? WarrantyEndDate,
    bool? IsWithinWarranty, string WarrantyDisplay, DateTime? DueAt, DateTime? ProviderConfirmedAt,
    string? ProviderResponse, bool? VisitRequired, DateTime? StartedAt, DateTime? CompletedAt,
    string? ResolutionSummary, string? UnresolvedReason, bool? RecurrenceOccurred, Guid? DisputeId,
    IReadOnlyList<AfterServiceTimelineItem> Timeline, IReadOnlyList<AfterServiceEvidenceResponse> Evidence,
    string? SourceTitle, string? SourceServiceName, string? SourceProviderName);

public sealed record ConvertAfterServiceToDisputeInput(string Subject, string Reason, string RequestedResolution, string IdempotencyKey);

public sealed record CustomerReportType(Guid Id, string Code, string Name, string? Description);
public sealed record CreateCustomerReportInput(string TargetType, Guid TargetId, Guid ReportTypeId, string Description, string IdempotencyKey);
public sealed record CustomerReportEvidence(Guid FileId, string FileName, string ContentType, long SizeBytes, string? Description, string DownloadUrl);
public sealed record CustomerReportResponse(
    Guid Id, string ReportNumber, Guid ReportTypeId, string ReportTypeCode, string ReportTypeName,
    string TargetType, Guid TargetId, string TargetDisplay, string Description, string Status, string DisplayStatus,
    DateTime ReceivedAt, DateTime? ResolvedAt, string? PublicResult, IReadOnlyList<CustomerReportEvidence> Evidence);
