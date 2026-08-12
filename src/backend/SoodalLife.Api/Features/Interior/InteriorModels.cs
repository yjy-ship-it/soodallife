using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SoodalLife.Api.Features.Admin;

namespace SoodalLife.Api.Features.Interior;

public sealed record CreateInteriorProjectRequest(Guid ServiceRequestId,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record CreateInteriorSiteVisitRequest(Guid ProviderId,DateTime ScheduledStartAt,DateTime? ScheduledEndAt,string? AccessConditionText,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record ConfirmInteriorSiteVisitRequest([param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record InteriorMeasurementInput([param:Required,StringLength(100)]string Key,decimal? Value,string? Text,string? Unit,string? Location,string? Note,string? AdditionalDataJson);
public sealed record CompleteInteriorSiteVisitRequest(string? MeasurementSummary,string? Constraint,string? RiskNote,IReadOnlyList<InteriorMeasurementInput>? Measurements,IReadOnlyList<Guid>? FileIds,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CreateInteriorDesignRequest([param:Required,StringLength(300)]string Title,string? Description,IReadOnlyList<Guid>? FileIds,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record CreateInteriorContractRequest(Guid ProviderId,Guid QuoteRevisionId,decimal ContractAmount,string CurrencyCode,string ScopeSnapshotJson,string ScheduleSnapshotJson,string? WarrantySnapshotJson,DateOnly PlannedStartDate,DateOnly PlannedCompletionDate,IReadOnlyList<PaymentPlanInput>? PaymentPlans,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record PaymentPlanInput([param:Range(1,int.MaxValue)]int SequenceNo,[param:Required,StringLength(200)]string Name,decimal Amount,DateOnly? DueDate,string? Condition);
public sealed record AgreeInteriorContractRequest([param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record ConfirmInteriorPaymentRequest(string ConfirmationTypeCode,decimal Amount,DateTime ConfirmedAt,Guid? EvidenceFileId,string? Note,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record CreateInteriorWorkStageRequest(int SequenceNo,[param:Required,StringLength(200)]string Name,DateOnly PlannedStartDate,DateOnly PlannedEndDate,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record AddInteriorWorkUpdateRequest([param:Range(0,100)]int ProgressPercent,[param:Required,StringLength(4000)]string UpdateText,string? IssueText,IReadOnlyList<Guid>? FileIds,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record InspectInteriorStageRequest(string InspectionStatusCode,string? ChecklistJson,[param:Required,StringLength(4000)]string ResultText,string? RequestedCorrectionText,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record AddInteriorParticipantRequest(Guid ProviderId,[param:Required,StringLength(40)]string RoleCode,DateTime EffectiveFrom,DateTime? EffectiveTo,bool IsPrimary,string? ScopeText,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record AcknowledgeInteriorInspectionRequest(string? Comment,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record SubmitInteriorProjectCompletionRequest([param:Required,StringLength(4000)]string CompletionSummary,string? FinalChecklistJson,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record AcknowledgeInteriorProjectCompletionRequest(string? Comment,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CreateInteriorChangeRequest(string? ChangeTypeCode,[param:Required,StringLength(2000)]string Reason,[param:Required,StringLength(4000)]string ScopeChange,decimal AmountDelta,int? ScheduleImpactDays,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record DecideInteriorChangeRequest(bool Approve,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record CompleteInteriorProjectRequest(string FinalChecklistJson,string CompletionSummary,[param:Required,StringLength(150)]string IdempotencyKey,string? RowVersion);
public sealed record LinkInteriorDefectRequest(Guid AfterServiceCaseId,Guid? WorkStageId,int? ContractVersion,[param:Required,StringLength(500)]string Location,[param:Required,StringLength(4000)]string Description,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record LinkInteriorDisputeRequest(Guid DisputeCaseId,Guid? ContractChangeId,Guid? WorkStageId,[param:Required,StringLength(150)]string IdempotencyKey);

public sealed record InteriorServiceResponse(Guid Id,string Code,string CategoryPath);
public sealed record InteriorProjectListItem(Guid Id,string ProjectNumber,string CustomerName,string ServiceName,string AreaName,string StatusCode,string? SiteVisitProvider,string? Contractor,decimal? ContractAmount,DateOnly? StartDate,DateOnly? CompletionDate,int ProgressPercent,bool HasAfterService,bool HasDispute,DateTime UpdatedAt);
public sealed record InteriorSiteVisitResponse(Guid Id,Guid ProviderId,string ProviderName,string StatusCode,DateTime ScheduledStartAt,DateTime? ScheduledEndAt,DateTime? ConfirmedAt,DateTime? CompletedAt,string? MeasurementSummary,string RowVersion);
public sealed record InteriorDesignResponse(Guid Id,int VersionNo,string Title,string? Description,string StatusCode,DateTime? CustomerApprovedAt);
public sealed record InteriorContractResponse(Guid Id,int Version,string ProviderName,string StatusCode,decimal Amount,string CurrencyCode,DateOnly PlannedStartDate,DateOnly PlannedCompletionDate,DateTime? CustomerAgreedAt,DateTime? ProviderAgreedAt,DateTime? EffectiveAt,string RowVersion);
public sealed record InteriorPaymentPlanResponse(Guid Id,int SequenceNo,string Name,decimal Amount,DateOnly? DueDate,string StatusCode,int ConfirmationCount);
public sealed record InteriorWorkStageResponse(Guid Id,int SequenceNo,string Name,string StatusCode,int ProgressPercent,DateOnly PlannedStartDate,DateOnly PlannedEndDate,int UpdateCount,int InspectionCount,string RowVersion);
public sealed record InteriorChangeResponse(Guid Id,int ChangeNo,string StatusCode,string Reason,string ScopeChange,decimal AmountDelta,int? ScheduleImpactDays,DateTime RequestedAt,DateTime? CustomerDecidedAt,string RowVersion);
public sealed record InteriorEventResponse(Guid Id,string EventTypeCode,DateTime OccurredAt,string? Data);
public sealed record InteriorParticipantResponse(Guid Id,Guid ProviderId,string ProviderName,string RoleCode,DateTime EffectiveFrom,DateTime? EffectiveTo,string StatusCode,bool IsPrimary,string? ScopeText,string RowVersion);
public sealed record InteriorInspectionAcknowledgementResponse(Guid Id,Guid InspectionId,DateTime AcknowledgedAt,string? Comment,string RowVersion);
public sealed record ProviderInteriorProjectPrivateResponse(Guid ProjectId,string CustomerName,string? CustomerPhone,string AreaName,string? DetailAddress,string StatusCode,string RoleCode,DateTime EffectiveFrom,DateTime? EffectiveTo);
public sealed record InteriorProjectDetail(Guid Id,string ProjectNumber,string StatusCode,string FeeAssessmentStatusCode,Guid RequestId,string CustomerName,[property:JsonIgnore]string? RawCustomerPhone,string ServiceName,string AreaName,[property:JsonIgnore]string? RawDetailAddress,string? SiteVisitProvider,string? Contractor,decimal? SiteVisitTrustSnapshot,decimal? ContractorTrustSnapshot,DateOnly? StartDate,DateOnly? ExpectedCompletionDate,DateOnly? ActualCompletionDate,string RowVersion,IReadOnlyList<InteriorSiteVisitResponse> SiteVisits,IReadOnlyList<InteriorDesignResponse> Designs,IReadOnlyList<InteriorContractResponse> Contracts,IReadOnlyList<InteriorPaymentPlanResponse> PaymentPlans,IReadOnlyList<InteriorWorkStageResponse> WorkStages,IReadOnlyList<InteriorChangeResponse> Changes,IReadOnlyList<InteriorEventResponse> Events)
{
    public string? CustomerPhone => AdminPrivacy.Phone(RawCustomerPhone);
    public string? DetailAddress => AdminPrivacy.DetailAddress(RawDetailAddress);
}
public sealed record ProviderSiteVisitPrivateResponse(Guid SiteVisitId,Guid ProjectId,string CustomerName,string? CustomerPhone,string AreaName,string? DetailAddress,DateTime ScheduledStartAt,DateTime? ScheduledEndAt,string? AccessConditionText,string StatusCode);

public sealed class InteriorBusinessException(int statusCode,string businessCode,string message):Exception(message)
{ public int StatusCode { get; }=statusCode; public string BusinessCode { get; }=businessCode; }
