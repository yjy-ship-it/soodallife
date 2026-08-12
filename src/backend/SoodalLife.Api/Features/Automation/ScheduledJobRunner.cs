using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Automation;

public sealed class ScheduledJobRunner(
    SoodalLifeDbContext db,
    IEnumerable<IAutomationJob> jobs,
    IOptions<AutomationOptions> options)
{
    private readonly string _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private readonly AutomationOptions _options = options.Value;

    public async Task RunDueAsync(CancellationToken token)
    {
        foreach (var job in jobs)
        {
            try { await RunAsync(job, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch { /* A job failure must never stop the remaining jobs or the worker loop. */ }
        }
    }

    private async Task RunAsync(IAutomationJob job, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var configuration = job.ConfigurationStatus switch
        {
            AutomationConfigurationStatus.Enabled => "ENABLED",
            AutomationConfigurationStatus.Disabled => "DISABLED",
            _ => "NOT_CONFIGURED"
        };
        var lease = await EnsureLeaseAsync(job.Name, configuration, now, token);
        if (configuration != "ENABLED") return;
        if (lease.NextScheduledAt.HasValue && lease.NextScheduledAt > now) return;
        if (!await TryAcquireAsync(lease.Id, now, token)) return;
        if (db.Database.IsRelational()) db.Entry(lease).State = EntityState.Detached;

        try
        {
            var result = await job.ExecuteAsync(token);
            db.ScheduledJobRuns.Add(new ScheduledJobRun
            {
                JobName = job.Name, InstanceId = _instanceId, StatusCode = "SUCCEEDED", StartedAt = now,
                CompletedAt = DateTime.UtcNow, ProcessedCount = result.ProcessedCount, FailedCount = result.FailedCount
            });
            await db.SaveChangesAsync(token);
            await ReleaseAsync(lease.Id, job.Interval, result, null, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var errorCode = SecurityTextSanitizer.ErrorCode(exception);
            db.ScheduledJobRuns.Add(new ScheduledJobRun
            {
                JobName = job.Name, InstanceId = _instanceId, StatusCode = "FAILED", StartedAt = now,
                CompletedAt = DateTime.UtcNow, FailedCount = 1, ErrorCode = errorCode
            });
            await db.SaveChangesAsync(token);
            await ReleaseAsync(lease.Id, job.Interval, new(0, 1), errorCode, token);
        }
    }

    private async Task<ScheduledJobLease> EnsureLeaseAsync(string name, string configuration, DateTime now, CancellationToken token)
    {
        var row = await db.ScheduledJobLeases.SingleOrDefaultAsync(x => x.JobName == name, token);
        if (row is null)
        {
            row = new ScheduledJobLease { JobName = name, ConfigurationStatusCode = configuration, UpdatedAt = now };
            db.ScheduledJobLeases.Add(row);
            try { await db.SaveChangesAsync(token); }
            catch (DbUpdateException)
            {
                db.Entry(row).State = EntityState.Detached;
                row = await db.ScheduledJobLeases.SingleAsync(x => x.JobName == name, token);
            }
        }
        if (row.ConfigurationStatusCode != configuration)
        {
            row.ConfigurationStatusCode = configuration;
            row.UpdatedAt = now;
            await db.SaveChangesAsync(token);
        }
        return row;
    }

    private async Task<bool> TryAcquireAsync(long id, DateTime now, CancellationToken token)
    {
        var expires = now.AddSeconds(Math.Max(30, _options.LeaseSeconds));
        if (db.Database.IsRelational())
        {
            var count = await db.ScheduledJobLeases.Where(x => x.Id == id && (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= now))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LeaseOwner, _instanceId)
                    .SetProperty(x => x.LeaseExpiresAt, expires).SetProperty(x => x.LastStartedAt, now)
                    .SetProperty(x => x.UpdatedAt, now), token);
            return count == 1;
        }
        var row = await db.ScheduledJobLeases.SingleAsync(x => x.Id == id, token);
        if (row.LeaseExpiresAt > now) return false;
        row.LeaseOwner = _instanceId;
        row.LeaseExpiresAt = expires;
        row.LastStartedAt = now;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(token);
        return true;
    }

    private async Task ReleaseAsync(long id, TimeSpan interval, AutomationJobResult result, string? errorCode, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var row = await db.ScheduledJobLeases.SingleAsync(x => x.Id == id, token);
        if (row.LeaseOwner != _instanceId) return;
        row.LeaseOwner = null;
        row.LeaseExpiresAt = null;
        row.NextScheduledAt = now.Add(interval);
        row.ProcessingCount = result.ProcessedCount;
        row.FailedCount = result.FailedCount;
        row.LastErrorCode = errorCode;
        row.UpdatedAt = now;
        if (errorCode is null) row.LastSucceededAt = now; else row.LastFailedAt = now;
        await db.SaveChangesAsync(token);
    }
}
