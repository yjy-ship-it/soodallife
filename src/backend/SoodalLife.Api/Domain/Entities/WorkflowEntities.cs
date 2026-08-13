namespace SoodalLife.Api.Domain.Entities;

public sealed class ServiceRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public long CategoryId { get; set; }
    public long CategoryPolicyId { get; set; }
    public long? AdministrativeAreaId { get; set; }
    public string? DetailAddress { get; set; }
    public byte[]? DetailAddressEncrypted { get; set; }
    public short? PrivacyProtectionVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public bool IsUrgent { get; set; }
    public string PolicySnapshotJson { get; set; } = string.Empty;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RequestAnswer
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long FieldDefinitionId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBoolean { get; set; }
    public DateOnly? ValueDate { get; set; }
    public DateTime? ValueDateTime { get; set; }
    public string? ValueJson { get; set; }
    public string? ValueCurrencyCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RequestAnswerFile
{
    public long Id { get; set; }
    public long RequestAnswerId { get; set; }
    public long FileId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ServiceRequestFile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long FileId { get; set; }
    public long? FieldDefinitionId { get; set; }
    public string PurposeCode { get; set; } = "REQUEST_REFERENCE";
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class DispatchCandidate
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long ProviderProfileId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public bool CategoryMatch { get; set; }
    public bool AreaMatch { get; set; }
    public bool ApprovalMatch { get; set; }
    public string? ReasonCode { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class RequestDispatch
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long ProviderProfileId { get; set; }
    public long CandidateId { get; set; }
    public string StatusCode { get; set; } = "AVAILABLE";
    public DateTime AvailableAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Notification
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long RecipientUserId { get; set; }
    public long? RequestDispatchId { get; set; }
    public long? TemplateId { get; set; }
    public long? ServiceRequestId { get; set; }
    public long? QuoteId { get; set; }
    public long? TransactionId { get; set; }
    public long? AfterServiceCaseId { get; set; }
    public long? DisputeCaseId { get; set; }
    public long? ReviewId { get; set; }
    public long? SanctionId { get; set; }
    public long? SubscriptionContractId { get; set; }
    public long? SubscriptionVisitScheduleId { get; set; }
    public long? InteriorProjectId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string PriorityCode { get; set; } = "NORMAL";
    public string? SourceTypeCode { get; set; }
    public Guid? SourcePublicId { get; set; }
    public string? TargetTypeCode { get; set; }
    public Guid? TargetPublicId { get; set; }
    public string? TemplateCodeSnapshot { get; set; }
    public string StatusCode { get; set; } = "PENDING";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long? CreatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationDelivery
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long NotificationId { get; set; }
    public long? NotificationRecipientId { get; set; }
    public string ChannelCode { get; set; } = string.Empty;
    public short AttemptNo { get; set; } = 1;
    public string StatusCode { get; set; } = "PENDING";
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ExternalProviderCode { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime AttemptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class Quote
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long ProviderProfileId { get; set; }
    public long RequestDispatchId { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class QuoteRevision
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long QuoteId { get; set; }
    public int RevisionNo { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Terms { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string? EstimatedDurationText { get; set; }
    public DateTime? AvailableStartAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? RevisionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public long SubmittedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RevisionPurposeCode { get; set; }
}

public sealed class QuoteItem
{
    public long Id { get; set; }
    public long QuoteRevisionId { get; set; }
    public int LineNo { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public string? UnitText { get; set; }
    public decimal UnitPriceAmount { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string? WorkTradeText { get; set; }
    public string? SpaceText { get; set; }
    public string? ItemCategoryCode { get; set; }
    public string? MaterialSpecText { get; set; }
    public string? LaborNoteText { get; set; }
}

public sealed class TransactionRecord
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long AcceptedQuoteRevisionId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public long CategoryId { get; set; }
    public long? CategoryFeePolicyId { get; set; }
    public long? WalletLedgerEntryId { get; set; }
    public string StatusCode { get; set; } = "CREATED";
    public decimal AgreedAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string QuoteSnapshotJson { get; set; } = string.Empty;
    public string CategoryPolicySnapshotJson { get; set; } = string.Empty;
    public string CompletionPolicySnapshotJson { get; set; } = string.Empty;
    public string? FeePolicySnapshotJson { get; set; }
    public string? FeePolicyVersionSnapshot { get; set; }
    public string? FeePolicyKindSnapshot { get; set; }
    public string? FeeTransactionTypeSnapshot { get; set; }
    public string? FeeCalculationMethodSnapshot { get; set; }
    public decimal? CalculatedFeeAmount { get; set; }
    public decimal? ActualChargedFeeAmount { get; set; }
    public string? FeeCurrencyCode { get; set; }
    public string? FeeChargeTimingSnapshot { get; set; }
    public string? FeeRestoreRuleSnapshot { get; set; }
    public short WarrantyDaysSnapshot { get; set; }
    public decimal? ProviderTrustScoreSnapshot { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransactionAppointment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? CustomerMemo { get; set; }
    public string? ProviderMemo { get; set; }
    public string StatusCode { get; set; } = "PROPOSED";
    public string? ProposalIdempotencyKey { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransactionDirectPayment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long RegisteredByUserId { get; set; }
    public string RegisteredByRoleCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string PaymentMethodCode { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string? NoteText { get; set; }
    public long? EvidenceFileId { get; set; }
    public string StatusCode { get; set; } = "REGISTERED";
    public DateTime RegisteredAt { get; set; }
    public long? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string RegistrationIdempotencyKey { get; set; } = string.Empty;
    public string? DecisionIdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransactionAppointmentChangeRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionAppointmentId { get; set; }
    public long RequestedByUserId { get; set; }
    public string ChangeTypeCode { get; set; } = "RESCHEDULE";
    public DateTime? RequestedStartAt { get; set; }
    public DateTime? RequestedEndAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public string? ProcessingNote { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransactionAppointmentEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionAppointmentId { get; set; }
    public long ActorUserId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string? BeforeStatusCode { get; set; }
    public string AfterStatusCode { get; set; } = string.Empty;
    public DateTime? BeforeStartAt { get; set; }
    public DateTime? BeforeEndAt { get; set; }
    public DateTime? AfterStartAt { get; set; }
    public DateTime? AfterEndAt { get; set; }
    public string? Reason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public sealed class TransactionCancellationRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long RequestedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public string? ProcessingNote { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? DecisionIdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WorkCompletion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long? InteriorProjectId { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public int LatestRevisionNo { get; set; }
    public DateTime? FirstSubmittedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WorkCompletionRevision
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WorkCompletionId { get; set; }
    public int RevisionNo { get; set; }
    public string StatusCode { get; set; } = "SUBMITTED";
    public string WorkSummary { get; set; } = string.Empty;
    public string? ChecklistJson { get; set; }
    public DateTime ProviderAttestationAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public long SubmittedByUserId { get; set; }
    public string? RevisionReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class CompletionEvidenceFile
{
    public long Id { get; set; }
    public long CompletionRevisionId { get; set; }
    public long FileId { get; set; }
    public long PhotoRoleId { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class CustomerConfirmation
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long CompletionRevisionId { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public long ConfirmedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ServiceHistoryEntry
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public long? TransactionId { get; set; }
    public long? SubscriptionVisitScheduleId { get; set; }
    public long? InteriorProjectId { get; set; }
    public long? SourceCompletionRevisionId { get; set; }
    public long? AfterServiceCaseId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ProviderNameSnapshot { get; set; }
    public string? CategoryNameSnapshot { get; set; }
    public decimal? TotalAmountSnapshot { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? CompletedAtSnapshot { get; set; }
    public DateOnly? WarrantyStartDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ServiceHistoryItem
{
    public long Id { get; set; }
    public long ServiceHistoryEntryId { get; set; }
    public int LineNo { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitText { get; set; }
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
}

public sealed class ServiceAsset
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public string AssetTypeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelName { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? InstalledAt { get; set; }
    public string? AttributesJson { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransactionAssetLink
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public long ServiceAssetId { get; set; }
    public long? SourceHistoryEntryId { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public string? CorrectionReason { get; set; }
    public DateTime LinkedAt { get; set; }
    public long? LinkedByUserId { get; set; }
    public DateTime? CorrectedAt { get; set; }
    public long? CorrectedByUserId { get; set; }
}

public sealed class AfterServiceCase
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? TransactionId { get; set; }
    public long? SubscriptionVisitScheduleId { get; set; }
    public long? InteriorProjectId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public long? ReportedByUserId { get; set; }
    public long? AssignedAdminUserId { get; set; }
    public string StatusCode { get; set; } = "RECEIVED";
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RequestDetails { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateOnly? WarrantyStartDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public bool? IsWithinWarranty { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ProviderConfirmedAt { get; set; }
    public string? ProviderResponseText { get; set; }
    public bool? VisitRequired { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastActionAt { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? UnresolvedReason { get; set; }
    public bool? RecurrenceOccurred { get; set; }
    public DateTime? ConvertedToDisputeAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AfterServiceAction
{
    public long Id { get; set; }
    public long AfterServiceCaseId { get; set; }
    public string? FromStatusCode { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public string ActionTypeCode { get; set; } = "STATE_CHANGE";
    public string? ActionNote { get; set; }
    public string? Reason { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? PerformedAt { get; set; }
    public long? ProviderProfileId { get; set; }
    public bool VisitOccurred { get; set; }
    public string? MaterialsText { get; set; }
    public string? ResultText { get; set; }
    public bool? RecurrenceOccurred { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? ActorUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class AfterServiceFile
{
    public long Id { get; set; }
    public long AfterServiceCaseId { get; set; }
    public long? AfterServiceActionId { get; set; }
    public long FileId { get; set; }
    public string? RoleCode { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? ActorUserId { get; set; }
    public string? ActorRoleCode { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityPublicId { get; set; }
    public string ResultCode { get; set; } = "SUCCESS";
    public Guid? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class OutboxEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregatePublicId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "PENDING";
    public DateTime OccurredAt { get; set; }
    public DateTime AvailableAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long? CreatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ScheduledJobLease
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string JobName { get; set; } = string.Empty;
    public string ConfigurationStatusCode { get; set; } = "DISABLED";
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastSucceededAt { get; set; }
    public DateTime? LastFailedAt { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime? NextScheduledAt { get; set; }
    public int ProcessingCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ScheduledJobRun
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string JobName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "RUNNING";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorCode { get; set; }
}
