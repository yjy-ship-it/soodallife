namespace SoodalLife.Api.Features.Admin;

public sealed record AdminAuditLogQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? AdminId,
    string? Area,
    string? Action,
    int Page = 1,
    int PageSize = 30);

public sealed record AdminAuditLogItem(
    DateTime OccurredAt,
    Guid? AdminId,
    string AdminLoginId,
    string? ActorRole,
    string Area,
    string Action,
    string TargetType,
    Guid? TargetId,
    string? Reason,
    Guid? CorrelationId,
    string Result,
    string? BeforeJson,
    string? AfterJson);

public sealed record AdminAuditFilterOption(string Value, string Label);

public sealed record AdminAuditLogResponse(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<AdminAuditLogItem> Items,
    IReadOnlyList<AdminAuditFilterOption> Administrators,
    IReadOnlyList<AdminAuditFilterOption> Areas,
    IReadOnlyList<AdminAuditFilterOption> Actions);

public sealed record AdminSystemMetric(string Code, string Label, long Count, string Severity, string Path);
public sealed record AdminOutboxStatus(string Status, long Count);
public sealed record AdminOutboxFailure(Guid Id, string AggregateType, string EventType, int AttemptCount, DateTime? LastAttemptAt, string? LastError);
public sealed record AdminIntegrationStatus(string Code, string Label, string Status, string Note);
public sealed record AdminManagedSetting(string Area, string Source, string Path, string Note);
public sealed record AdminAccountSummary(Guid Id, string LoginId, string Status, IReadOnlyList<string> Roles, DateTime CreatedAt, DateTime? LastLoginAt);
public sealed record AdminDatabaseStatus(string Connection, string MigrationStatus, int PendingMigrationCount, IReadOnlyList<string> PendingMigrations);

public sealed record AdminSystemStatusResponse(
    DateTime GeneratedAt,
    AdminDatabaseStatus Database,
    IReadOnlyList<AdminSystemMetric> Attention,
    IReadOnlyList<AdminOutboxStatus> Outbox,
    IReadOnlyList<AdminOutboxFailure> RecentOutboxFailures,
    IReadOnlyList<AdminIntegrationStatus> Integrations,
    IReadOnlyList<AdminManagedSetting> ManagedSettings,
    IReadOnlyList<AdminAccountSummary> Administrators,
    IReadOnlyList<string> SecurityLimitations);

public sealed class AdminSystemException(string businessCode, string message, int statusCode = 400) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
