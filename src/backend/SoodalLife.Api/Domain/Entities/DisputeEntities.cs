namespace SoodalLife.Api.Domain.Entities;

public sealed class DisputeCase
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long? AfterServiceCaseId { get; set; }
    public long ApplicantUserId { get; set; }
    public long CounterpartyUserId { get; set; }
    public long? AssignedAdminUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "OPEN";
    public DateTime ReceivedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? LastActionAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DisputeEvidence
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long DisputeCaseId { get; set; }
    public long? FileId { get; set; }
    public long? SubmittedByUserId { get; set; }
    public string SourceTypeCode { get; set; } = string.Empty;
    public Guid? SourcePublicId { get; set; }
    public string? Description { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime SubmittedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public long? WithdrawnByUserId { get; set; }
    public string? WithdrawalReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class DisputeAction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long DisputeCaseId { get; set; }
    public string ActionTypeCode { get; set; } = string.Empty;
    public string? FromStatusCode { get; set; }
    public string? ToStatusCode { get; set; }
    public string? ActionNote { get; set; }
    public string? Reason { get; set; }
    public string? RelatedReferenceType { get; set; }
    public Guid? RelatedReferencePublicId { get; set; }
    public DateTime OccurredAt { get; set; }
    public long ActorUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class DisputeResolution
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long DisputeCaseId { get; set; }
    public int VersionNo { get; set; }
    public string ResultSummary { get; set; } = string.Empty;
    public string DecisionDetails { get; set; } = string.Empty;
    public string BasisText { get; set; } = string.Empty;
    public string? FollowUpAction { get; set; }
    public DateTime DecidedAt { get; set; }
    public long DecidedByUserId { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}
