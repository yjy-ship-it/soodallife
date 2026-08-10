using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record CaseMasterRequest([param:Required,StringLength(50)] string Code,[param:Required,StringLength(100)] string Name,[param:StringLength(1000)] string? Description,bool IsActive,int DisplayOrder,DateTime? EffectiveFrom,DateTime? EffectiveTo,string? RowVersion);
public sealed record CaseMasterResponse(Guid Id,string Code,string Name,string? Description,bool IsActive,int DisplayOrder,DateTime? EffectiveFrom,DateTime? EffectiveTo,string RowVersion,bool IsInUse);
public sealed record CaseAssigneeResponse(Guid Id,string Name);

public sealed record CreateAdminReportRequest(Guid ReporterUserId,Guid? ReportedUserId,Guid ReportTypeId,Guid? ServiceRequestId,Guid? TransactionId,Guid? ReviewId,Guid? AfterServiceCaseId,Guid? DisputeCaseId,[param:Required,StringLength(4000)] string Description,[param:Required,StringLength(150)] string IdempotencyKey);
public sealed record ChangeReportStatusRequest([param:Required,StringLength(30)] string TargetStatusCode,[param:StringLength(2000)] string? ResultSummary,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(150)] string IdempotencyKey,string RowVersion);
public sealed record AssignReportRequest(Guid? AdminUserId,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(150)] string IdempotencyKey,string RowVersion);
public sealed record AddReportEvidenceRequest(Guid FileId,[param:StringLength(1000)] string? Description,[param:Required,StringLength(150)] string IdempotencyKey);
public sealed record AdminReportListResponse(int TotalCount,int Page,int PageSize,IReadOnlyList<AdminReportListItem> Items);
public sealed record AdminReportListItem(Guid Id,string Number,string TypeName,string ReporterName,string? ReportedName,string? ServiceName,string? TransactionNumber,string StatusCode,string? AssignedAdminName,DateTime ReceivedAt,DateTime? ResolvedAt);
public sealed record AdminReportDetail(Guid Id,string Number,Guid ReportTypeId,string TypeCode,string TypeName,string StatusCode,string Description,string? ResultSummary,AdminCaseParty Reporter,AdminCaseParty? Reported,AdminReportRelations Relations,string? AssignedAdminName,DateTime ReceivedAt,DateTime? ResolvedAt,DateTime? CancelledAt,IReadOnlyList<AdminCaseEvidence> Evidence,IReadOnlyList<AdminCaseAction> Actions,IReadOnlyList<AdminCaseAudit> Audit,string RowVersion);
public sealed record AdminCaseParty(Guid Id,string Name,string? Phone,string? Email,string[] Roles);
public sealed record AdminReportRelations(Guid? ServiceRequestId,string? RequestTitle,Guid? TransactionId,string? TransactionNumber,Guid? ReviewId,string? ReviewSummary,Guid? AfterServiceCaseId,string? AfterServiceSubject,Guid? DisputeCaseId,string? DisputeSubject,string? ServiceName);
public sealed record AdminCaseEvidence(Guid Id,Guid FileId,string FileName,string ContentType,string? Description,string StatusCode,DateTime SubmittedAt,string DownloadUrl);
public sealed record CaseFileUploadResponse(Guid FileId,string FileName,string ContentType,long SizeBytes);
public sealed record AdminCaseAction(Guid Id,string ActionTypeCode,string? FromStatusCode,string? ToStatusCode,string? Reason,string? Note,DateTime OccurredAt);
public sealed record AdminCaseAudit(DateTime OccurredAt,string ActionCode,string ResultCode,string? Reason,string? BeforeJson,string? AfterJson);

public sealed record SanctionSourceRequest(Guid? ReportId,Guid? DisputeCaseId,Guid? ReviewId,Guid? AfterServiceCaseId);
public sealed record CreateSanctionRequest(Guid TargetUserId,string? TargetRoleCode,Guid? ProviderId,Guid? ProviderServiceId,Guid SanctionTypeId,[param:Required,StringLength(2000)] string Reason,DateTime StartAt,DateTime? EndAt,[param:MinLength(1)] IReadOnlyList<SanctionSourceRequest> Sources,[param:Required,StringLength(150)] string IdempotencyKey);
public sealed record ChangeSanctionStatusRequest([param:Required,StringLength(20)] string ActionCode,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(150)] string IdempotencyKey,string RowVersion);
public sealed record CreateSanctionAppealRequest(Guid ApplicantUserId,Guid? PreviousAppealId,[param:Required,StringLength(4000)] string Statement,[param:Required,StringLength(150)] string IdempotencyKey);
public sealed record AssignSanctionAppealRequest(Guid? AdminUserId,[param:Required,StringLength(1000)] string Reason,[param:Required,StringLength(150)] string IdempotencyKey,string RowVersion);
public sealed record DecideSanctionAppealRequest([param:Required,StringLength(20)] string DecisionCode,[param:Required,StringLength(2000)] string DecisionReason,[param:Required,StringLength(150)] string IdempotencyKey,string RowVersion);
public sealed record AddSanctionAppealEvidenceRequest(Guid FileId,[param:StringLength(1000)] string? Description,[param:Required,StringLength(150)] string IdempotencyKey);
public sealed record AdminSanctionListResponse(int TotalCount,int Page,int PageSize,IReadOnlyList<AdminSanctionListItem> Items);
public sealed record AdminSanctionListItem(Guid Id,string Number,string TargetName,string TypeName,string ScopeLabel,string StatusCode,DateTime StartAt,DateTime? EndAt,int SourceCount,string DecidedAdminName);
public sealed record AdminSanctionDetail(Guid Id,string Number,Guid SanctionTypeId,string TypeCode,string TypeName,string StatusCode,string Reason,AdminCaseParty Target,string ScopeLabel,DateTime StartAt,DateTime? EndAt,DateTime DecidedAt,string DecidedAdminName,DateTime? ReleasedAt,string? ReleasedAdminName,string? ReleaseReason,IReadOnlyList<AdminSanctionSource> Sources,IReadOnlyList<AdminSanctionEvent> Events,IReadOnlyList<AdminSanctionAppeal> Appeals,IReadOnlyList<AdminCaseAudit> Audit,string RowVersion);
public sealed record AdminSanctionSource(Guid? ReportId,Guid? DisputeCaseId,Guid? ReviewId,Guid? AfterServiceCaseId,string Label);
public sealed record AdminSanctionEvent(Guid Id,string ActionTypeCode,string? FromStatusCode,string ToStatusCode,string Reason,DateTime OccurredAt);
public sealed record AdminSanctionAppeal(Guid Id,string Number,Guid ApplicantUserId,string ApplicantName,Guid? PreviousAppealId,string Statement,string StatusCode,string? AssignedAdminName,string? DecisionCode,string? DecisionReason,DateTime SubmittedAt,DateTime? DecidedAt,IReadOnlyList<AdminCaseEvidence> Evidence,IReadOnlyList<AdminCaseAction> Actions,string RowVersion);

public sealed class CaseManagementException(string businessCode,string message,int statusCode=400):Exception(message)
{ public string BusinessCode{get;}=businessCode; public int StatusCode{get;}=statusCode; }
