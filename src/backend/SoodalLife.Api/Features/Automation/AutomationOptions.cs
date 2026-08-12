namespace SoodalLife.Api.Features.Automation;

public sealed class AutomationOptions
{
    public const string SectionName = "OperationsAutomation";
    public bool WorkerEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
    public int LeaseSeconds { get; set; } = 120;
    public int OutboxBatchSize { get; set; } = 50;
    public int OutboxMaxAttempts { get; set; } = 5;
    public int OutboxBaseRetrySeconds { get; set; } = 30;
    public int OutboxMaxRetrySeconds { get; set; } = 900;
    public int SubscriptionIntervalMinutes { get; set; } = 60;
    public int PasswordResetIntervalMinutes { get; set; } = 60;
    public int? EmergencyResponseExpiryMinutes { get; set; }
}

public enum AutomationConfigurationStatus { Enabled, Disabled, NotConfigured }
public sealed record AutomationJobResult(int ProcessedCount, int FailedCount = 0);

public interface IAutomationJob
{
    string Name { get; }
    AutomationConfigurationStatus ConfigurationStatus { get; }
    TimeSpan Interval { get; }
    Task<AutomationJobResult> ExecuteAsync(CancellationToken token);
}
