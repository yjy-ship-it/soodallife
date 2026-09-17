namespace SoodalLife.Api.Domain.Entities;

public sealed class AdminSecurityProfile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string DetailRoleCode { get; set; } = "OPERATIONS";
    public bool MfaEnabled { get; set; }
    public string? TotpSecretProtected { get; set; }
    public DateTime? MfaConfirmedAt { get; set; }
    public int FailedMfaAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AdminReauthenticationSession
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string PurposeCode { get; set; } = "SENSITIVE_ADMIN_ACTION";
}

public sealed class OutboxRetryRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long OutboxEventId { get; set; }
    public int AttemptSnapshot { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public long RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public sealed class DataRetentionPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string DomainCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = "DELETE";
    public int RetentionDays { get; set; }
    public int LegalHoldDays { get; set; }
    public bool IsEnabled { get; set; }
    public bool DryRun { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DataRetentionExecution
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PolicyId { get; set; }
    public string StatusCode { get; set; } = "RUNNING";
    public bool DryRun { get; set; }
    public int CandidateCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SkippedLegalHoldCount { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ExecutedByUserId { get; set; }
}

public sealed class AnalyticsEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string EventTypeCode { get; set; } = string.Empty;
    public Guid VisitorId { get; set; }
    public long? UserId { get; set; }
    public Guid? ProviderPublicId { get; set; }
    public Guid? SourcePublicId { get; set; }
    public string? SourceTypeCode { get; set; }
    public string? RouteTemplate { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public int? DurationMs { get; set; }
    public string? OutcomeCode { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? MetadataJson { get; set; }
}
