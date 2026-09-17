namespace SoodalLife.Api.Features.SiteVisits;

public sealed record SaveSiteVisitProposalInput(DateTime ScheduledAt,int EstimatedDurationMinutes,decimal VisitFeeAmount,
    string PaymentMode,string? Terms,bool DeductFromWorkAmount,string? PaymentInstruction,int NoShowWaitMinutes,
    DateTime ExpiresAt,string IdempotencyKey,string? RowVersion);
public sealed record AcceptSiteVisitInput(string DetailAddress,bool TermsAccepted,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitActionInput(string Action,string? Note,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitPaymentReportInput(string? Memo,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitPaymentDecisionInput(string Decision,string? Reason,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitNoShowInput(string SubjectRole,int ContactAttempts,string? EvidenceNote,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitDisputeInput(string Reason,string IdempotencyKey,string RowVersion);
public sealed record SiteVisitEventResponse(Guid Id,string EventType,string? Note,DateTime OccurredAt);
public sealed record SiteVisitProposalResponse(Guid Id,Guid RequestId,Guid ProviderId,string ProviderName,string Status,
    DateTime ScheduledAt,int EstimatedDurationMinutes,decimal VisitFeeAmount,string PaymentMode,string PaymentStatus,
    bool DeductFromWorkAmount,string? Terms,string? PaymentInstruction,int NoShowWaitMinutes,DateTime ExpiresAt,
    DateTime? AcceptedAt,DateTime? DepartedAt,DateTime? ArrivedAt,DateTime? CompletedAt,string? NoShowStatus,
    string? DetailAddress,bool CanAccept,bool CanEdit,bool CanProgress,IReadOnlyList<SiteVisitEventResponse> Events,string RowVersion);

public sealed class SiteVisitWorkflowException(string businessCode,string message,int statusCode):Exception(message)
{
    public string BusinessCode { get; }=businessCode;
    public int StatusCode { get; }=statusCode;
}
