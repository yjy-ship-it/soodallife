namespace SoodalLife.Api.Domain.Entities;

public sealed class InteriorProject
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ServiceCategoryId { get; set; }
    public string StatusCode { get; set; } = "CONSULTATION";
    public long? SelectedSiteVisitProviderId { get; set; }
    public long? SelectedContractorProviderId { get; set; }
    public DateTime? SiteVisitSelectedAt { get; set; }
    public DateTime? ContractorSelectedAt { get; set; }
    public long? CurrentQuoteRevisionId { get; set; }
    public long? CurrentContractId { get; set; }
    public decimal? SiteVisitProviderTrustScoreSnapshot { get; set; }
    public decimal? ContractorTrustScoreSnapshot { get; set; }
    public string FeeAssessmentStatusCode { get; set; } = "PENDING_SELECTION";
    public decimal? ReservedFeeAmount { get; set; }
    public long? FeeReservationWalletId { get; set; }
    public long? FeeReservationLedgerEntryId { get; set; }
    public long? FeeReleaseLedgerEntryId { get; set; }
    public DateTime? FeeReservedAt { get; set; }
    public DateTime? FeeCapturedAt { get; set; }
    public DateTime? FeeReleasedAt { get; set; }
    public string? FeeReleaseReasonCode { get; set; }
    public string? ContractExpiryPhaseCode { get; set; }
    public DateTime? ContractActionDueAt { get; set; }
    public DateTime? ContractExpiryReminderSentAt { get; set; }
    public DateTime? ContractExpiryPausedAt { get; set; }
    public string? ContractExpiryPauseReasonCode { get; set; }
    public DateTime? ContractExpiredAt { get; set; }
    public DateOnly? ProjectStartDate { get; set; }
    public DateOnly? ExpectedCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public DateTime? ProviderCompletionSubmittedAt { get; set; }
    public long? ProviderCompletionSubmittedByUserId { get; set; }
    public string? ProviderCompletionSummary { get; set; }
    public string? ProviderFinalChecklistJson { get; set; }
    public DateTime? CustomerCompletionAcknowledgedAt { get; set; }
    public long? CustomerCompletionAcknowledgedByUserId { get; set; }
    public string? CustomerCompletionComment { get; set; }
    public DateTime? AdminCompletedAt { get; set; }
    public long? AdminCompletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorProjectParticipant
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public long ProviderProfileId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public bool IsPrimary { get; set; }
    public string? ScopeText { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorSiteVisit
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public long ProviderProfileId { get; set; }
    public string StatusCode { get; set; } = "PROPOSED";
    public DateTime? ProposedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public string? AccessConditionText { get; set; }
    public DateTime? VisitedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? MeasurementSummaryText { get; set; }
    public string? ConstraintText { get; set; }
    public string? RiskNoteText { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorSiteVisitMeasurement
{
    public long Id { get; set; }
    public long SiteVisitId { get; set; }
    public string MeasurementKey { get; set; } = string.Empty;
    public decimal? MeasurementValue { get; set; }
    public string? MeasurementText { get; set; }
    public string? UnitText { get; set; }
    public string? LocationText { get; set; }
    public string? NoteText { get; set; }
    public string? AdditionalDataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorSiteVisitFile
{
    public long Id { get; set; }
    public long SiteVisitId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorDesignVersion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public int VersionNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public DateTime? CustomerApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorDesignFile
{
    public long Id { get; set; }
    public long DesignVersionId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorContract
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public int ContractVersion { get; set; } = 1;
    public string StatusCode { get; set; } = "PENDING_AGREEMENT";
    public long SelectedQuoteRevisionId { get; set; }
    public decimal ContractAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string ScopeSnapshotJson { get; set; } = "{}";
    public string QuoteSnapshotJson { get; set; } = "{}";
    public string ScheduleSnapshotJson { get; set; } = "{}";
    public string? WarrantySnapshotJson { get; set; }
    public decimal? ProviderTrustScoreSnapshot { get; set; }
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedCompletionDate { get; set; }
    public DateTime? CustomerAgreedAt { get; set; }
    public DateTime? ProviderAgreedAt { get; set; }
    public DateTime? EffectiveAt { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public DateOnly ContractSignedDate { get; set; }
    public DateTime RegisteredByProviderAt { get; set; }
    public string? ProviderDeclarationText { get; set; }
    public string? CustomerMismatchReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorContractDocument
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorContractId { get; set; }
    public long FileId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorContractVersion
{
    public long Id { get; set; }
    public long InteriorContractId { get; set; }
    public int VersionNo { get; set; }
    public long? SourceContractChangeId { get; set; }
    public decimal ContractAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string ScopeSnapshotJson { get; set; } = "{}";
    public string ScheduleSnapshotJson { get; set; } = "{}";
    public string PaymentPlanSnapshotJson { get; set; } = "[]";
    public string? WarrantySnapshotJson { get; set; }
    public DateTime? CustomerAgreedAt { get; set; }
    public DateTime? ProviderAgreedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorPaymentPlan
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorContractId { get; set; }
    public int SequenceNo { get; set; }
    public string PaymentName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public DateOnly? PlannedDueDate { get; set; }
    public string? ConditionText { get; set; }
    public string StatusCode { get; set; } = "PLANNED";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorPaymentConfirmation
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PaymentPlanId { get; set; }
    public long ConfirmedByUserId { get; set; }
    public string ConfirmationTypeCode { get; set; } = string.Empty;
    public decimal ConfirmedAmount { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public long? EvidenceFileId { get; set; }
    public string? NoteText { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class InteriorWorkStage
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public int SequenceNo { get; set; }
    public string StageName { get; set; } = string.Empty;
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public int ProgressPercent { get; set; }
    public string StatusCode { get; set; } = "PLANNED";
    public string? ProviderNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorWorkStageAssignment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WorkStageId { get; set; }
    public long ProjectParticipantId { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorWorkUpdate
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WorkStageId { get; set; }
    public int ProgressPercent { get; set; }
    public string UpdateText { get; set; } = string.Empty;
    public string? IssueText { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorWorkUpdateFile
{
    public long Id { get; set; }
    public long WorkUpdateId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorStageInspection
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WorkStageId { get; set; }
    public string InspectionStatusCode { get; set; } = "RECORDED";
    public long InspectedByUserId { get; set; }
    public string? ChecklistJson { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public string? RequestedCorrectionText { get; set; }
    public DateTime InspectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorStageInspectionFile
{
    public long Id { get; set; }
    public long StageInspectionId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = "INSPECTION_EVIDENCE";
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorStageInspectionAcknowledgement
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long StageInspectionId { get; set; }
    public long CustomerProfileId { get; set; }
    public DateTime AcknowledgedAt { get; set; }
    public string? Comment { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorContractChange
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorContractId { get; set; }
    public int ChangeNo { get; set; }
    public long RequestedByUserId { get; set; }
    public string? ChangeTypeCode { get; set; }
    public string ReasonText { get; set; } = string.Empty;
    public string ScopeChangeText { get; set; } = string.Empty;
    public decimal AmountDelta { get; set; }
    public int? ScheduleImpactDays { get; set; }
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? CustomerDecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class InteriorContractChangeFile
{
    public long Id { get; set; }
    public long ContractChangeId { get; set; }
    public long FileId { get; set; }
    public string PurposeCode { get; set; } = "CHANGE_EVIDENCE";
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorDefect
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public long AfterServiceCaseId { get; set; }
    public long? WorkStageId { get; set; }
    public int? ContractVersion { get; set; }
    public string DefectLocationText { get; set; } = string.Empty;
    public string DefectDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class InteriorProjectEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InteriorProjectId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public long? ActorUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
