using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record CreateAfterServiceRequest(Guid TransactionId, [param:Required,StringLength(20)] string ReporterRoleCode,
    [param:Required,StringLength(200)] string Subject, [param:Required] string SymptomDescription,
    [param:StringLength(2000)] string? RequestDetails, [param:Required,StringLength(100)] string IdempotencyKey);
public sealed record ChangeAfterServiceStatusRequest([param:Required,StringLength(30)] string TargetStatusCode,
    [param:StringLength(2000)] string? Note, [param:StringLength(2000)] string? ProviderResponse,
    bool? VisitRequired, DateTime? ScheduledAt, [param:StringLength(2000)] string? ResolutionSummary,
    [param:StringLength(2000)] string? UnresolvedReason, bool? RecurrenceOccurred,
    [param:StringLength(1000)] string? AdminOverrideReason, [param:Required,StringLength(100)] string IdempotencyKey);
public sealed record AddAfterServiceActionRequest([param:Required,StringLength(30)] string ActionTypeCode,
    DateTime? ScheduledAt, DateTime? PerformedAt, bool VisitOccurred, [param:StringLength(2000)] string? ActionNote,
    [param:StringLength(2000)] string? MaterialsText, [param:StringLength(2000)] string? ResultText,
    bool? RecurrenceOccurred, [param:Required,StringLength(100)] string IdempotencyKey);
public sealed record AddAfterServiceFileRequest(Guid FileId, [param:StringLength(50)] string? RoleCode,
    [param:StringLength(500)] string? Description);
public sealed record ConvertAfterServiceToDisputeRequest([param:Required,StringLength(200)] string Subject,
    [param:Required,StringLength(2000)] string Reason, [param:Required,StringLength(100)] string IdempotencyKey);

public sealed record AdminAfterServiceListResponse(int TotalCount,int Page,int PageSize,IReadOnlyList<AdminAfterServiceListItem> Items);
public sealed record AdminAfterServiceListItem(Guid Id,string CaseNumber,Guid TransactionId,string TransactionNumber,string ServiceName,
    string CustomerName,string? CustomerPhone,string ProviderName,string? ProviderPhone,bool? IsWithinWarranty,DateOnly? WarrantyEndDate,
    DateTime ReceivedAt,string StatusCode,DateTime? LastActionAt,string? AssignedAdminName,DateTime? DueAt,bool HasDispute);
public sealed record AdminAfterServiceDetail(Guid Id,string CaseNumber,string StatusCode,string Subject,string SymptomDescription,
    string? RequestDetails,DateTime ReceivedAt,AdminWarrantySnapshot Warranty,AdminCaseTransaction Transaction,
    AdminParty Customer,AdminParty Provider,DateTime? ProviderConfirmedAt,string? ProviderResponse,bool? VisitRequired,
    DateTime? DueAt,string? ResolutionSummary,string? UnresolvedReason,bool? RecurrenceOccurred,
    IReadOnlyList<AdminAfterServiceActionItem> Actions,IReadOnlyList<AdminEvidenceItem> Evidence,
    IReadOnlyList<AdminCompletionEvidenceItem> CompletionEvidence,IReadOnlyList<AdminServiceHistoryItemResponse> ServiceHistory,
    Guid? DisputeId,IReadOnlyList<AdminAuditItem> Audit,string RowVersion);
public sealed record AdminWarrantySnapshot(DateOnly? StartDate,DateOnly? EndDate,DateOnly ReceivedDate,bool? IsWithinWarranty,short WarrantyDays);
public sealed record AdminCaseTransaction(Guid Id,string Number,string StatusCode,string ServiceName,string CategoryPath,string RequestTitle,
    decimal AgreedAmount,string CurrencyCode,DateTime? CompletedAt,Guid AcceptedQuoteRevisionId,string QuoteSnapshotJson,string CompletionPolicySnapshotJson);
public sealed record AdminParty(Guid UserId,string Name,string? Phone,string? Email);
public sealed record AdminAfterServiceActionItem(long Id,string ActionTypeCode,string? FromStatusCode,string ToStatusCode,string? Note,
    string? Reason,DateTime? ScheduledAt,DateTime? PerformedAt,bool VisitOccurred,string? MaterialsText,string? ResultText,
    bool? RecurrenceOccurred,DateTime OccurredAt);
public sealed record AdminEvidenceItem(Guid? Id,Guid? FileId,string? FileName,string SourceTypeCode,Guid? SourcePublicId,string? Description,string StatusCode,DateTime SubmittedAt);
public sealed record AdminCompletionEvidenceItem(Guid FileId,string FileName,string RoleCode,string? Description);
public sealed record AdminServiceHistoryItemResponse(Guid Id,string EventTypeCode,string Title,string Summary,DateTime OccurredAt,DateOnly? WarrantyEndDate);
public sealed record AdminAuditItem(DateTime OccurredAt,string ActionCode,string ResultCode,string? Reason);

public sealed record CreateDisputeRequest(Guid TransactionId,[param:Required,StringLength(20)] string ApplicantRoleCode,
    [param:Required,StringLength(200)] string Subject,[param:Required] string Description,[param:Required,StringLength(100)] string IdempotencyKey);
public sealed record ChangeDisputeStatusRequest([param:Required,StringLength(30)] string TargetStatusCode,
    [param:StringLength(2000)] string? Note,[param:Required,StringLength(100)] string IdempotencyKey);
public sealed record AssignDisputeRequest(Guid? AdminUserId,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(100)] string IdempotencyKey);
public sealed record AddDisputeEvidenceRequest(Guid? FileId,[param:Required,StringLength(40)] string SourceTypeCode,Guid? SourcePublicId,
    [param:StringLength(1000)] string? Description,[param:Required,StringLength(100)] string IdempotencyKey);
public sealed record ResolveDisputeRequest(Guid? LiabilityTypeId,[param:Required,StringLength(1000)] string ResultSummary,[param:Required] string DecisionDetails,
    [param:Required,StringLength(2000)] string BasisText,[param:StringLength(2000)] string? FollowUpAction,
    [param:Required,StringLength(100)] string IdempotencyKey);
public sealed record LinkFeeRestoreRequest(Guid FeeRestoreId,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(100)] string IdempotencyKey);

public sealed record AdminDisputeListResponse(int TotalCount,int Page,int PageSize,IReadOnlyList<AdminDisputeListItem> Items);
public sealed record AdminDisputeListItem(Guid Id,string CaseNumber,Guid TransactionId,string TransactionNumber,string Subject,
    string CustomerName,string? CustomerPhone,string ProviderName,string? ProviderPhone,string StatusCode,string? AssignedAdminName,
    DateTime ReceivedAt,DateTime? DueAt,DateTime? LastActionAt,bool HasAfterService);
public sealed record AdminDisputeDetail(Guid Id,string CaseNumber,string StatusCode,string Subject,string Description,DateTime ReceivedAt,
    DateTime? DueAt,DateTime? ResolvedAt,DateTime? ClosedAt,AdminCaseTransaction Transaction,AdminParty Applicant,AdminParty Counterparty,
    string? AssignedAdminName,Guid? AfterServiceId,IReadOnlyList<AdminDisputeOriginalEvidence> OriginalEvidence,
    IReadOnlyList<AdminEvidenceItem> SubmittedEvidence,IReadOnlyList<AdminDisputeActionItem> Actions,
    IReadOnlyList<AdminDisputeResolutionItem> Resolutions,AdminDisputeFinancials Financials,IReadOnlyList<AdminAuditItem> Audit,string RowVersion);
public sealed record AdminDisputeOriginalEvidence(string SourceTypeCode,Guid SourceId,string Label);
public sealed record AdminDisputeActionItem(Guid Id,string ActionTypeCode,string? FromStatusCode,string? ToStatusCode,string? Note,
    string? Reason,string? RelatedReferenceType,Guid? RelatedReferenceId,DateTime OccurredAt);
public sealed record AdminDisputeResolutionItem(Guid Id,int VersionNo,Guid? LiabilityTypeId,string? LiabilityTypeName,string ResultSummary,string DecisionDetails,string BasisText,
    string? FollowUpAction,DateTime DecidedAt,string DecidedBy,bool IsCurrent);
public sealed record AdminDisputeFinancials(Guid? FeeChargeId,decimal? FeeAmount,string? RestoreStatusCode,Guid? FeeRestoreId,
    DateTime? RestoredAt,bool PaymentHoldSupported,string Message);

public sealed class AfterServiceDisputeException(string businessCode,string message,int statusCode=400):Exception(message)
{ public string BusinessCode{get;}=businessCode; public int StatusCode{get;}=statusCode; }
