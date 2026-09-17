using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.Advertising;
using SoodalLife.Api.Features.Interior;

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

public sealed class SubscriptionRecurringBillingJob(SubscriptionBillingService service, SubscriptionTerminationService terminationService, IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "SUBSCRIPTION_RECURRING_BILLING";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, options.Value.SubscriptionIntervalMinutes));
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)
    {
        var billing=await service.GenerateRecurringPaymentRequests(token);
        var settlements=await service.ProcessAutonomousSettlements(token);
        var terminations=await terminationService.ProcessPendingAsync(token);
        return new(billing+settlements+terminations);
    }
}

public sealed class SubscriptionLifecycleJob(SubscriptionLifecycleAutomationService service, IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "SUBSCRIPTION_LIFECYCLE";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, options.Value.SubscriptionIntervalMinutes));
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => new(await service.ProcessDueAsync(token));
}

public sealed class EmergencyExpiryJob(IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "EMERGENCY_EXPIRY";
    public AutomationConfigurationStatus ConfigurationStatus => options.Value.EmergencyResponseExpiryMinutes.HasValue
        ? AutomationConfigurationStatus.Enabled : AutomationConfigurationStatus.NotConfigured;
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => Task.FromResult(new AutomationJobResult(0));
}

public sealed class QuoteFeeReservationExpiryJob(QuoteFeeReservationService service) : IAutomationJob
{
    public string Name => "QUOTE_FEE_RESERVATION_EXPIRY";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => new(await service.ExpireDueAsync(token));
}

public sealed class ServiceRequestExpiryJob(SoodalLifeDbContext db) : IAutomationJob
{
    public string Name => "SERVICE_REQUEST_EXPIRY";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(1);

    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var requests = await db.ServiceRequests
            .Where(request => request.StatusCode == "OPEN" && request.AcceptedAt == null && request.ExpiresAt != null && request.ExpiresAt <= now)
            .OrderBy(request => request.ExpiresAt).Take(200).ToListAsync(token);
        if (requests.Count == 0) return new(0);

        var requestIds = requests.Select(request => request.Id).ToArray();
        var customerProfileIds = requests.Select(request => request.CustomerProfileId).Distinct().ToArray();
        var customerUserIds = await db.CustomerProfiles.AsNoTracking().Where(profile => customerProfileIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.UserId, token);
        foreach (var request in requests)
        {
            request.StatusCode = "EXPIRED";
            request.UpdatedAt = now;
            var key = $"service-request-expired:{request.PublicId:N}";
            if (!await db.OutboxEvents.AnyAsync(item => item.IdempotencyKey == key, token))
                db.OutboxEvents.Add(new OutboxEvent { AggregateType = "ServiceRequest", AggregatePublicId = request.PublicId,
                    EventType = "SERVICE_REQUEST_EXPIRED", PayloadJson = JsonSerializer.Serialize(new { recipientUserId = customerUserIds.GetValueOrDefault(request.CustomerProfileId), requestId = request.PublicId }),
                    StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key });
        }
        foreach (var dispatch in await db.RequestDispatches.Where(item => requestIds.Contains(item.ServiceRequestId) && (item.StatusCode == "AVAILABLE" || item.StatusCode == "VIEWED")).ToListAsync(token))
        {
            dispatch.StatusCode = "EXPIRED";
            dispatch.ExpiresAt = now;
        }
        foreach (var candidate in await db.DispatchCandidates.Where(item => requestIds.Contains(item.ServiceRequestId) && item.StatusCode != "RESPONDED" && item.StatusCode != "DECLINED").ToListAsync(token))
        {
            candidate.StatusCode = "EXPIRED";
            candidate.ReasonCode = "REQUEST_EXPIRED";
            candidate.ExpiresAt = now;
        }
        await db.SaveChangesAsync(token);
        return new(requests.Count);
    }
}

public sealed class InteriorContractExpiryJob(InteriorContractExpiryService service) : IAutomationJob
{
    public string Name => "INTERIOR_CONTRACT_EXPIRY";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => new(await service.ProcessDueAsync(token));
}

public sealed class ProviderAdvertisingRenewalJob(ProviderAdvertisingRenewalService service,IOptions<AutomationOptions> options):IAutomationJob
{
    public string Name=>"PROVIDER_ADVERTISING_RENEWAL";
    public AutomationConfigurationStatus ConfigurationStatus=>AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval=>TimeSpan.FromMinutes(Math.Max(1,options.Value.ProviderAdvertisingRenewalIntervalMinutes));
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)=>new(await service.ProcessDueAsync(token));
}

public sealed class NotConfiguredAutomationJob(string name) : IAutomationJob
{
    public string Name { get; } = name;
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.NotConfigured;
    public TimeSpan Interval => TimeSpan.FromHours(24);
    public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => Task.FromResult(new AutomationJobResult(0));
}
