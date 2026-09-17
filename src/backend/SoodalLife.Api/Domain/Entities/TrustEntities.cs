namespace SoodalLife.Api.Domain.Entities;

public static class TrustPolicyDraftDefaults
{
    public static readonly Guid PublicId=new("f84f8728-8e1e-4ef8-a7ee-4bea94548ff0");
    public const string Version="v1.0-draft";
    public const string RulesJson="""{"minimumCompletedTransactions":3,"minimumVerifiedReviews":3,"components":[{"code":"EVIDENCE","weight":15,"ruleType":"EVIDENCE_COMPLETENESS","settings":{"approvalRatio":0.25,"serviceApprovalRatio":0.25,"requiredVerificationRatio":0.4,"notExpiredRatio":0.1}},{"code":"TRANSACTION","weight":30,"ruleType":"TRANSACTION_COMPLETION_RATE","settings":{"completionRateRatio":0.8,"completionEvidenceRatio":0.2}},{"code":"REVIEW","weight":30,"ruleType":"VERIFIED_PUBLIC_RATING_AVERAGE","settings":{}},{"code":"AFTER_SERVICE","weight":10,"ruleType":"FINALIZED_AFTER_SERVICE_OUTCOME","settings":{"noCaseValue":1.0,"resolvedValue":1.0,"unresolvedValue":0.0,"recurrencePenalty":0.25,"disputeConversionPenalty":0.25}},{"code":"DISPUTE","weight":10,"ruleType":"STRUCTURED_LIABILITY_MAPPING","settings":{"noDisputeScore":100,"liabilityScores":{"NO_PROVIDER_LIABILITY":100,"CUSTOMER_LIABILITY":100,"MUTUAL_MINOR":70,"PROVIDER_PARTIAL":50,"PROVIDER_FULL":0}}},{"code":"SANCTION","weight":5,"ruleType":"DECIDED_SANCTION_MAPPING","settings":{"noSanctionScore":100,"sanctionScores":{"NOTICE":80,"FORMAL_WARNING":60,"SERVICE_RESTRICTION":30,"SUSPENSION":0}}}]}""";
}

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

public sealed class ProviderTrustCalculationResult
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public long TrustPolicyId { get; set; }
    public string CalculationModeCode { get; set; } = "SIMULATION";
    public string ResultStatusCode { get; set; } = "INSUFFICIENT_DATA";
    public decimal? Score { get; set; }
    public string? GradeCode { get; set; }
    public string EvaluationStatusCode { get; set; } = "NEW_OR_EVALUATING";
    public string? InsufficiencyReason { get; set; }
    public int CompletedTransactionCount { get; set; }
    public int VerifiedReviewCount { get; set; }
    public string PolicySnapshotJson { get; set; } = "{}";
    public string SourceSnapshotJson { get; set; } = "{}";
    public DateTime CalculatedAt { get; set; }
    public long? RequestedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long? AppliedTrustScoreEventId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProviderTrustScoreComponent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CalculationResultId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string RawValueJson { get; set; } = "{}";
    public decimal? NormalizedScore { get; set; }
    public decimal? WeightedScore { get; set; }
    public int SampleCount { get; set; }
    public bool IsCalculable { get; set; }
    public string? UnavailableReason { get; set; }
    public string SourceSnapshotJson { get; set; } = "{}";
    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
