namespace SoodalLife.Api.Domain.Entities;

public sealed class TrustPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string PolicyVersion { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string TargetTypeCode { get; set; } = string.Empty;
    public string ScopeTypeCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string RulesJson { get; set; } = "{}";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderTrustScoreCurrent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public long? TrustPolicyId { get; set; }
    public decimal? Score { get; set; }
    public string? GradeCode { get; set; }
    public string EvaluationStatusCode { get; set; } = "NEW_OR_EVALUATING";
    public DateTime? CalculatedAt { get; set; }
    public long? LastEventId { get; set; }
    public string SourceTypeCode { get; set; } = "INITIAL_MIGRATION";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TrustScoreEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public long? TrustPolicyId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string SourceTypeCode { get; set; } = string.Empty;
    public Guid? SourcePublicId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public decimal? ScoreBefore { get; set; }
    public decimal? ScoreDelta { get; set; }
    public decimal? ScoreAfter { get; set; }
    public string? GradeBefore { get; set; }
    public string? GradeAfter { get; set; }
    public string? DecisionCode { get; set; }
    public string? ReasonText { get; set; }
    public string? PolicySnapshotJson { get; set; }
    public string? SourceSnapshotJson { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
