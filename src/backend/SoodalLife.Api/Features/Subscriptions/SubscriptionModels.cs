using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed record RecurrenceRuleRequest(
    [param:Required] string FrequencyTypeCode,
    [param:Range(1,24)] int IntervalValue,
    int? VisitsPerPeriod,
    IReadOnlyList<int>? Weekdays,
    TimeOnly PreferredTimeFrom,
    TimeOnly? PreferredTimeTo,
    [param:Range(1,1440)] int ExpectedDurationMinutes,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed record CreateCareProductRequest(Guid ServiceCategoryId,[param:Required,StringLength(200)]string ProductName,string? Description,[param:Required,StringLength(4000)]string ServiceScopeText,[param:Range(1,100)]int VisitsPerPeriod,[param:Range(1,1440)]int ExpectedDurationMinutes,string BillingPeriodCode,decimal? StandardMonthlyAmount,decimal? StandardVisitAmount,DateOnly EffectiveFrom,DateOnly? EffectiveTo,bool IsActive=true);
public sealed record CreateSubscriptionRequest(Guid ServiceCategoryId,Guid? CareProductId,Guid AdministrativeAreaId,string RequestTypeCode,[param:Required,StringLength(4000)]string RequestedScopeText,DateOnly PreferredStartDate,string? DetailAddress,RecurrenceRuleRequest Recurrence,Guid? CustomerAddressId=null,bool PriceNegotiable=true,decimal? DesiredMonthlyAmount=null,decimal? DesiredVisitAmount=null,[param:StringLength(150)]string? IdempotencyKey=null);
public sealed record SubmitSubscriptionApplicationRequest([param:Required,StringLength(4000)]string ProposedScopeText,decimal? ProposedMonthlyAmount,decimal? ProposedVisitAmount,string? AvailableScheduleText,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record WithdrawSubscriptionApplicationRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record ProviderSubscriptionContractActionRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CancelSubscriptionRequestRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record SelectSubscriptionProviderRequest(Guid ApplicationId,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record ChangeContractStateRequest(string? Reason,DateTime? ResumePlannedAt,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record ReplaceSubscriptionProviderRequest(Guid ProviderId,[param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record RequestScheduleChangeRequest(DateTime ScheduledStartAt,DateTime? ScheduledEndAt,[param:Required,StringLength(2000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record DecideScheduleChangeRequest(bool Approve,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record ForceScheduleChangeDecisionRequest(bool Approve,[param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CompleteSubscriptionVisitRequest(string? VerificationMethodCode,string? VerificationResultCode,string? ChecklistJson,string? CompletionNote,IReadOnlyList<Guid>? FileIds,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion,string? GpsEvidence=null,string? PossessionEvidence=null);
public sealed record OverrideSubscriptionVisitVerificationRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record ConfirmSubscriptionVisitRequest([param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CreateSubscriptionReviewRequest([param:Required,StringLength(4000)]string BodyText,decimal? OverallRating,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record CreateSubscriptionCaseRequest([param:Required,StringLength(200)]string Subject,[param:Required,StringLength(4000)]string Description,[param:Required,StringLength(150)]string IdempotencyKey);

public sealed record SubscriptionServiceItem(Guid Id,string MajorName,string MiddleName,string ServiceName,string CategoryPath);
public sealed record CareProductResponse(Guid Id,Guid ServiceCategoryId,string ServiceName,string ProductName,string? Description,string ServiceScopeText,int VisitsPerPeriod,int ExpectedDurationMinutes,string BillingPeriodCode,decimal? StandardMonthlyAmount,decimal? StandardVisitAmount,bool IsActive,DateOnly EffectiveFrom,DateOnly? EffectiveTo,string RowVersion);
public sealed record SubscriptionRecurrenceResponse(string FrequencyTypeCode,int IntervalValue,int? VisitsPerPeriod,IReadOnlyList<int> Weekdays,TimeOnly PreferredTimeFrom,TimeOnly? PreferredTimeTo,int ExpectedDurationMinutes,DateOnly StartDate,DateOnly? EndDate);
public sealed record SubscriptionRequestResponse(Guid Id,string RequestNumber,Guid CustomerId,string CustomerName,Guid ServiceCategoryId,string ServiceName,Guid? CareProductId,string RequestTypeCode,string RequestedScopeText,DateOnly PreferredStartDate,Guid AdministrativeAreaId,string AreaName,string StatusCode,int ApplicationCount,bool ProviderSelected,bool PriceNegotiable,decimal? DesiredMonthlyAmount,decimal? DesiredVisitAmount,SubscriptionRecurrenceResponse Recurrence,string RowVersion);
public sealed record SubscriptionApplicationResponse(Guid Id,Guid ProviderId,string ProviderName,string ProposedScopeText,decimal? ProposedMonthlyAmount,decimal? ProposedVisitAmount,string? AvailableScheduleText,decimal? TrustScore,string StatusCode,DateTime SubmittedAt,string RowVersion);
public sealed record AdminSubscriptionApplicationResponse(Guid Id,Guid RequestId,string RequestNumber,string CustomerName,string ServiceName,Guid ProviderId,string ProviderName,decimal? ProposedMonthlyAmount,decimal? ProposedVisitAmount,string? AvailableScheduleText,decimal? TrustScore,string StatusCode,DateTime SubmittedAt);
public sealed record SubscriptionContractResponse(Guid Id,string ContractNumber,Guid RequestId,string CustomerName,string ProviderName,string ServiceName,string StatusCode,DateTime StartedAt,DateTime? EndedAt,DateTime? PauseStartedAt,DateTime? ResumePlannedAt,DateTime? TerminationRequestedAt,DateTime? TerminatedAt,decimal? MonthlyAmount,decimal? VisitAmount,string CurrencyCode,DateTime? NextVisitAt,string PriceSnapshotJson,string? FeePolicySnapshotJson,string ServiceScopeSnapshotJson,string RecurrenceSnapshotJson,decimal? ProviderTrustScoreSnapshot,string RowVersion);
public sealed record ProviderSubscriptionContractDetail(Guid Id,string ContractNumber,string CustomerName,string? CustomerPhone,string? DetailAddress,string AreaName,string ServiceName,string ServiceScopeSnapshotJson,string RecurrenceSnapshotJson,string StatusCode,DateTime? NextVisitAt);
public sealed record SubscriptionVisitResponse(Guid Id,Guid ContractId,string ContractNumber,int VisitNo,Guid ProviderId,string ProviderName,DateTime ScheduledStartAt,DateTime? ScheduledEndAt,string StatusCode,DateTime? VisitVerifiedAt,string? VisitVerificationMethodCode,DateTime? ProviderCompletionSubmittedAt,DateTime? CustomerConfirmedAt,string SettlementStatusCode,string RowVersion);
public sealed record SubscriptionScheduleChangeResponse(Guid Id,Guid VisitId,DateTime OldStartAt,DateTime? OldEndAt,DateTime NewStartAt,DateTime? NewEndAt,string Reason,string StatusCode,DateTime RequestedAt,DateTime? DecidedAt,string RowVersion);
public sealed record SubscriptionEventResponse(Guid Id,string EventTypeCode,DateTime OccurredAt,string? EventDataJson);

public sealed class SubscriptionBusinessException(int statusCode,string businessCode,string message,string? field=null):Exception(message)
{
    public int StatusCode { get; }=statusCode;
    public string BusinessCode { get; }=businessCode;
    public string? Field { get; }=field;
}
