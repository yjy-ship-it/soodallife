namespace SoodalLife.Api.Domain.Entities;

public sealed class CareProduct
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceCategoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ServiceScopeText { get; set; } = string.Empty;
    public int VisitsPerPeriod { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public string BillingPeriodCode { get; set; } = "MONTHLY";
    public decimal? StandardMonthlyAmount { get; set; }
    public decimal? StandardVisitAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public long ServiceCategoryId { get; set; }
    public long? CareProductId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public string RequestTypeCode { get; set; } = "CUSTOM";
    public string RequestedScopeText { get; set; } = string.Empty;
    public DateOnly PreferredStartDate { get; set; }
    public string? DetailAddress { get; set; }
    public byte[]? DetailAddressEncrypted { get; set; }
    public short? PrivacyProtectionVersion { get; set; }
    public string StatusCode { get; set; } = "OPEN";
    public long? SelectedApplicationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionRecurrenceRule
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionRequestId { get; set; }
    public string FrequencyTypeCode { get; set; } = "MONTHLY";
    public int IntervalValue { get; set; } = 1;
    public int? VisitsPerPeriod { get; set; }
    public string? WeekdaysJson { get; set; }
    public TimeOnly PreferredTimeFrom { get; set; }
    public TimeOnly? PreferredTimeTo { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? AdditionalRuleJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionApplication
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionRequestId { get; set; }
    public long ProviderProfileId { get; set; }
    public string ProposedScopeText { get; set; } = string.Empty;
    public decimal? ProposedMonthlyAmount { get; set; }
    public decimal? ProposedVisitAmount { get; set; }
    public string? AvailableScheduleText { get; set; }
    public string StatusCode { get; set; } = "SUBMITTED";
    public DateTime SubmittedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionContract
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionRequestId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public long ServiceCategoryId { get; set; }
    public long? CareProductId { get; set; }
    public long SubscriptionApplicationId { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? PauseStartedAt { get; set; }
    public DateTime? ResumePlannedAt { get; set; }
    public DateTime? TerminationRequestedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public string PriceSnapshotJson { get; set; } = "{}";
    public string? FeePolicySnapshotJson { get; set; }
    public string ServiceScopeSnapshotJson { get; set; } = "{}";
    public string RecurrenceSnapshotJson { get; set; } = "{}";
    public string CompletionPolicySnapshotJson { get; set; } = "{}";
    public decimal? ProviderTrustScoreSnapshot { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public DateTime? NextBillingAt { get; set; }
    public string? BillingStatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionVisitSchedule
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionContractId { get; set; }
    public int VisitNo { get; set; }
    public long ProviderProfileId { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public string StatusCode { get; set; } = "SCHEDULED";
    public DateTime? VisitVerifiedAt { get; set; }
    public string? VisitVerificationMethodCode { get; set; }
    public string? VisitVerificationResultCode { get; set; }
    public string GpsVerificationStatusCode { get; set; } = "NOT_INTEGRATED";
    public string PossessionVerificationStatusCode { get; set; } = "NOT_INTEGRATED";
    public string VisitVerificationStatusCode { get; set; } = "NOT_VERIFIED";
    public DateTime? VerificationOverrideAt { get; set; }
    public long? VerificationOverrideByUserId { get; set; }
    public string? VerificationOverrideReason { get; set; }
    public DateTime? WorkStartedAt { get; set; }
    public DateTime? WorkCompletedAt { get; set; }
    public DateTime? ProviderCompletionSubmittedAt { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }
    public string? CompletionChecklistJson { get; set; }
    public string? CompletionNote { get; set; }
    public string SettlementStatusCode { get; set; } = "NOT_READY";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionVisitFile
{
    public long Id { get; set; }
    public long SubscriptionVisitScheduleId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = "SUBSCRIPTION_COMPLETION";
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class SubscriptionScheduleChange
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionVisitScheduleId { get; set; }
    public long RequestedByUserId { get; set; }
    public string OldScheduleJson { get; set; } = "{}";
    public string NewScheduleJson { get; set; } = "{}";
    public string Reason { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public long? DecidedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? SubscriptionRequestId { get; set; }
    public long? SubscriptionContractId { get; set; }
    public long? SubscriptionVisitScheduleId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? ActorUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
