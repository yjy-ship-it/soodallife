using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Features.Subscriptions;

namespace SoodalLife.Api.Features.Automation;

public sealed class PasswordResetExpiryJob(SoodalLifeDbContext db, IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "PASSWORD_RESET_CLEANUP";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, options.Value.PasswordResetIntervalMinutes));
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)
    {
        // Expiry is enforced by ExpiresAt during token validation. Until deletion retention is approved,
        // the job records safe observability only and never deletes or rewrites reset requests.
        _ = await db.PasswordResetRequests.AsNoTracking().CountAsync(x => x.UsedAt == null && x.ExpiresAt <= DateTime.UtcNow, token);
        return new(0);
    }
}

public sealed class SubscriptionVisitGeneratorJob(CareSubscriptionService service, IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "SUBSCRIPTION_VISIT_GENERATOR";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, options.Value.SubscriptionIntervalMinutes));
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => new(await service.GenerateRollingWindows(token));
}

public sealed class EmergencyExpiryJob(IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "EMERGENCY_EXPIRY";
    public AutomationConfigurationStatus ConfigurationStatus => options.Value.EmergencyResponseExpiryMinutes.HasValue
        ? AutomationConfigurationStatus.Enabled : AutomationConfigurationStatus.NotConfigured;
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => Task.FromResult(new AutomationJobResult(0));
}

public sealed class NotConfiguredAutomationJob(string name) : IAutomationJob
{
    public string Name { get; } = name;
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.NotConfigured;
    public TimeSpan Interval => TimeSpan.FromHours(24);
    public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => Task.FromResult(new AutomationJobResult(0));
}
