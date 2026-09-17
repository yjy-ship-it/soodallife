using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed record ProviderCareDashboardResponse(
    int OpenRequestCount, int MyApplicationCount, int WaitingSelectionCount, int ActiveContractCount,
    int TodayVisitCount, int PendingScheduleChangeCount, int WaitingCustomerConfirmationCount,
    int OpenAfterServiceCount, int OpenDisputeCount, IReadOnlyList<ProviderCareVisitListItem> RecentCompletedVisits);

public sealed record ProviderCareRequestItem(
    Guid Id, string RequestNumber, Guid ServiceCategoryId, string ServiceName, string AreaName,
    string RequestTypeCode, string RequestedScope, DateOnly PreferredStartDate,
    bool PriceNegotiable, decimal? DesiredMonthlyAmount, decimal? DesiredVisitAmount,
    SubscriptionRecurrenceResponse Recurrence, string StatusCode, bool HasApplied, DateTime CreatedAt,
    bool CanApply, string? EligibilityReasonCode, string? EligibilityReason);

public sealed record ProviderCareApplicationItem(
    Guid Id, Guid RequestId, string RequestNumber, string ServiceName, string AreaName,
    string ProposedScope, decimal? ProposedMonthlyAmount, decimal? ProposedVisitAmount,
    string? AvailableSchedule, string StatusCode, string StatusDisplay, DateTime SubmittedAt,
    Guid? ContractId, string RowVersion);

public sealed record ProviderCareContractListItem(
    Guid Id, string ContractNumber, string ServiceName, string CustomerDisplayName, string StatusCode,
    string StatusDisplay, DateTime StartedAt, DateTime? EndedAt, DateTime? TerminationRequestedAt,
    decimal? MonthlyAmount, decimal? VisitAmount, DateTime? NextVisitAt, string RowVersion);

public sealed record ProviderCareContractDetail(
    ProviderCareContractListItem Contract, string ServiceScopeSnapshotJson, string RecurrenceSnapshotJson,
    decimal? ProviderTrustScoreSnapshot, string BillingStatusDisplay, string SettlementStatusDisplay,
    bool ContactAvailable, string? CustomerPhone, string? DetailAddress, string ContactPolicy);

public sealed record ProviderCareVisitFile(
    Guid Id, string FileName, string ContentType, long SizeBytes, string MalwareScanStatus,
    string PrivacyInspectionStatus, string SanitizationStatus, string DownloadUrl);

public sealed record ProviderCareVisitListItem(
    Guid Id, Guid ContractId, string ContractNumber, string ServiceName, int VisitNo,
    DateTime ScheduledStartAt, DateTime? ScheduledEndAt, string StatusCode, string StatusDisplay,
    bool ScheduleChangePending, string VisitVerificationStatus, bool CompletionReported,
    bool CustomerConfirmed, string RowVersion);

public sealed record ProviderCareVisitDetail(
    ProviderCareVisitListItem Visit, string ServiceScopeSnapshotJson, string CompletionEvidenceRule,
    int RequiredPhotoCount, bool ChecklistRequired, bool VisitVerificationRequired,
    string GpsVerificationStatus, string PossessionVerificationStatus, string VisitVerificationStatus,
    DateTime? WorkStartedAt, DateTime? WorkCompletedAt, string? CompletionChecklistJson,
    string? CompletionNote, bool ContactAvailable, string? CustomerPhone, string? DetailAddress,
    string ContactPolicy, IReadOnlyList<ProviderCareVisitFile> Files, Guid? AfterServiceId, Guid? DisputeId,
    string CustomerConfirmationStatus);

public sealed record ProviderCareScheduleChangeItem(
    Guid Id, Guid VisitId, string ContractNumber, string ServiceName, DateTime OldStartAt,
    DateTime? OldEndAt, DateTime NewStartAt, DateTime? NewEndAt, string Reason,
    string StatusCode, bool RequiresProviderDecision, DateTime RequestedAt, string RowVersion);

public sealed record StartProviderCareVisitRequest(
    [param: Required, StringLength(150)] string IdempotencyKey,
    string? RowVersion);

public sealed record ProviderCareUploadResponse(
    Guid FileId, string FileName, string ContentType, long SizeBytes, string MalwareScanStatus,
    string PrivacyInspectionStatus, string SanitizationStatus);
