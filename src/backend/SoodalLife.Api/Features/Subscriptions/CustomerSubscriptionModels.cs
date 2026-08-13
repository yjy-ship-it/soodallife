using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed record CustomerCareHomeResponse(
    int EligibleServiceCount,
    int ActiveProductCount,
    int OpenRequestCount,
    int ActiveContractCount,
    int UpcomingVisitCount,
    int RecentCompletedVisitCount,
    int UnreadNotificationCount,
    IReadOnlyList<CustomerSubscriptionVisitListItem> UpcomingVisits,
    IReadOnlyList<CustomerSubscriptionVisitListItem> RecentCompletedVisits);

public sealed record CustomerCareProductResponse(
    Guid Id,
    Guid ServiceCategoryId,
    string ServiceName,
    string ProductName,
    string? Description,
    string ServiceScope,
    int VisitsPerPeriod,
    int ExpectedDurationMinutes,
    string BillingPeriodCode,
    decimal? MonthlyAmount,
    decimal? VisitAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record CustomerSubscriptionRequestResponse(
    Guid Id,
    string RequestNumber,
    Guid ServiceCategoryId,
    string ServiceName,
    Guid? CareProductId,
    string? CareProductName,
    string RequestTypeCode,
    string RequestedScope,
    DateOnly PreferredStartDate,
    Guid AdministrativeAreaId,
    string AreaName,
    string? CustomerAddress,
    string StatusCode,
    int ApplicationCount,
    bool ProviderSelected,
    SubscriptionRecurrenceResponse Recurrence,
    DateTime CreatedAt,
    string RowVersion);

public sealed record CustomerSubscriptionRatingAverage(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    decimal AverageValue,
    int RatingCount,
    decimal MinValue,
    decimal MaxValue);

public sealed record CustomerSubscriptionApplicationResponse(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string ProposedScope,
    decimal? ProposedMonthlyAmount,
    decimal? ProposedVisitAmount,
    string? AvailableSchedule,
    decimal? TrustScore,
    string? TrustGrade,
    string TrustEvaluationStatus,
    string TrustDisplay,
    int PublicReviewCount,
    IReadOnlyList<CustomerSubscriptionRatingAverage> RatingItemAverages,
    string ProviderApprovalSummary,
    string ServiceApprovalSummary,
    string RequirementSummary,
    string StatusCode,
    DateTime SubmittedAt,
    bool IsSelected,
    string RowVersion);

public sealed record CustomerSubscriptionContractResponse(
    Guid Id,
    string ContractNumber,
    Guid RequestId,
    string ServiceName,
    string? CareProductName,
    Guid ProviderId,
    string ProviderName,
    string StatusCode,
    string StatusDisplay,
    DateTime StartedAt,
    DateTime? EndedAt,
    DateTime? PauseStartedAt,
    DateTime? ResumePlannedAt,
    DateTime? TerminationRequestedAt,
    DateTime? TerminatedAt,
    decimal? MonthlyAmount,
    decimal? VisitAmount,
    string CurrencyCode,
    DateTime? NextVisitAt,
    string PriceSnapshotJson,
    string ServiceScopeSnapshotJson,
    string RecurrenceSnapshotJson,
    decimal? ProviderTrustScoreSnapshot,
    bool ProviderReplacementRequiresSupport,
    string RowVersion);

public sealed record CustomerSubscriptionVisitFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int DisplayOrder,
    string? DownloadUrl,
    string PublicationStatus = "AVAILABLE",
    string? PublicationMessage = null);

public sealed record CustomerSubscriptionVisitListItem(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string ServiceName,
    int VisitNo,
    Guid ProviderId,
    string ProviderName,
    DateTime ScheduledStartAt,
    DateTime? ScheduledEndAt,
    string StatusCode,
    string StatusDisplay,
    bool VisitVerified,
    bool ProviderCompletionSubmitted,
    bool CustomerConfirmed);

public sealed record CustomerSubscriptionVisitDetailResponse(
    CustomerSubscriptionVisitListItem Visit,
    string? VisitVerificationResult,
    DateTime? WorkCompletedAt,
    string? CompletionChecklistJson,
    string? CompletionNote,
    string CustomerProgressDisplay,
    IReadOnlyList<CustomerSubscriptionVisitFileResponse> Files,
    Guid? ReviewId,
    Guid? AfterServiceId,
    Guid? DisputeId,
    IReadOnlyList<SubscriptionScheduleChangeResponse> ScheduleChanges);

public sealed record CustomerSubscriptionPaymentMethodResponse(
    Guid Id,
    string PaymentMethodTypeCode,
    string? ProviderCode,
    string? MaskedDisplayText,
    string StatusCode,
    bool IsDefault,
    DateTime RegisteredAt);

public sealed record CustomerSubscriptionPaymentHistoryResponse(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string ServiceName,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    decimal RequestedAmount,
    string CurrencyCode,
    string StatusCode,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? FailureReason,
    IReadOnlyList<CustomerSubscriptionRefundResponse> Refunds);

public sealed record CustomerSubscriptionRefundResponse(
    Guid Id,
    string TypeCode,
    decimal RequestedAmount,
    decimal? ApprovedAmount,
    string StatusCode,
    DateTime RequestedAt,
    DateTime? CompletedAt);

public sealed record CustomerContractActionRequest(
    [param: Required, StringLength(150)] string IdempotencyKey,
    [param: StringLength(1000)] string? Reason,
    DateTime? ResumePlannedAt,
    string? RowVersion);

public sealed record CustomerSkipVisitRequest(
    [param: Required, StringLength(150)] string IdempotencyKey,
    [param: StringLength(1000)] string? Reason,
    string? RowVersion);

public sealed record CustomerCancelScheduleChangeRequest(
    [param: Required, StringLength(150)] string IdempotencyKey,
    string? RowVersion);
