namespace SoodalLife.Api.Domain.Entities;

public sealed class ReportType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Report
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ReporterUserId { get; set; }
    public long? ReportedUserId { get; set; }
    public long ReportTypeId { get; set; }
    public long? ServiceRequestId { get; set; }
    public long? TransactionId { get; set; }
    public long? ReviewId { get; set; }
    public long? AfterServiceCaseId { get; set; }
    public long? DisputeCaseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "RECEIVED";
    public long? AssignedAdminUserId { get; set; }
    public string? ResultSummary { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ReportEvidence
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ReportId { get; set; }
    public long FileId { get; set; }
    public long SubmittedByUserId { get; set; }
    public string? Description { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime SubmittedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public long? WithdrawnByUserId { get; set; }
    public string? WithdrawalReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ReportAction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ReportId { get; set; }
    public string ActionTypeCode { get; set; } = string.Empty;
    public string? FromStatusCode { get; set; }
    public string? ToStatusCode { get; set; }
    public long ActorUserId { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class SanctionType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Sanction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TargetUserId { get; set; }
    public long? TargetRoleId { get; set; }
    public long? ProviderProfileId { get; set; }
    public long? ProviderServiceCategoryId { get; set; }
    public long SanctionTypeId { get; set; }
    public string StatusCode { get; set; } = "DECIDED";
    public string Reason { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime DecidedAt { get; set; }
    public long DecidedByUserId { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public long? ReleasedByUserId { get; set; }
    public string? ReleaseReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SanctionSource
{
    public long Id { get; set; }
    public long SanctionId { get; set; }
    public long? ReportId { get; set; }
    public long? DisputeCaseId { get; set; }
    public long? ReviewId { get; set; }
    public long? AfterServiceCaseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class SanctionEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SanctionId { get; set; }
    public string ActionTypeCode { get; set; } = string.Empty;
    public string? FromStatusCode { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public long ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class SanctionAppeal
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SanctionId { get; set; }
    public long ApplicantUserId { get; set; }
    public long? PreviousAppealId { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "RECEIVED";
    public long? AssignedAdminUserId { get; set; }
    public string? DecisionCode { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SanctionAppealEvidence
{
    public long Id { get; set; }
    public long SanctionAppealId { get; set; }
    public long FileId { get; set; }
    public long SubmittedByUserId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class SanctionAppealAction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SanctionAppealId { get; set; }
    public string ActionTypeCode { get; set; } = string.Empty;
    public string? FromStatusCode { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public long ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class DisputeLiabilityType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
