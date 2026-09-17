using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Features.Notifications;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Automation;

public sealed class OutboxProcessor(
    SoodalLifeDbContext db,
    NotificationManagementService notifications,
    IOptions<AutomationOptions> options)
{
    private readonly AutomationOptions _options = options.Value;

    public async Task<AutomationJobResult> ProcessBatchAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var candidates = await db.OutboxEvents.AsNoTracking()
            .Where(x =>
                ((x.StatusCode == "PENDING" || x.StatusCode == "PROCESSING") || x.StatusCode == "FAILED" && x.AttemptCount < _options.OutboxMaxAttempts) &&
                x.AvailableAt <= now)
            .OrderBy(x => x.AvailableAt).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.PublicId })
            .Take(Math.Clamp(_options.OutboxBatchSize, 1, 500))
            .ToListAsync(token);

        var processed = 0;
        var failed = 0;
        foreach (var candidate in candidates)
        {
            if (!await TryClaimAsync(candidate.Id, now, token)) continue;
            IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
            try
            {
                await notifications.ProcessOutbox(candidate.PublicId, token);
                await CompleteAsync(candidate.Id, token);
                if (transaction is not null) await transaction.CommitAsync(token);
                processed++;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                if (transaction is not null) await transaction.RollbackAsync(token);
                db.ChangeTracker.Clear();
                await FailAsync(candidate.Id, exception, token);
                failed++;
            }
            finally { if (transaction is not null) await transaction.DisposeAsync(); }
        }
        return new(processed, failed);
    }

    private async Task<bool> TryClaimAsync(long id, DateTime now, CancellationToken token)
    {
        if (db.Database.IsRelational())
        {
            var count = await db.OutboxEvents
                .Where(x => x.Id == id && x.AvailableAt <= now &&
                    ((x.StatusCode == "PENDING" || x.StatusCode == "PROCESSING") || x.StatusCode == "FAILED" && x.AttemptCount < _options.OutboxMaxAttempts))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.StatusCode, "PROCESSING")
                    .SetProperty(x => x.LastAttemptAt, now)
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                    .SetProperty(x => x.AvailableAt, now.AddSeconds(Math.Max(30, _options.LeaseSeconds)))
                    .SetProperty(x => x.ErrorMessage, (string?)null), token);
            return count == 1;
        }

        var row = await db.OutboxEvents.SingleAsync(x => x.Id == id, token);
        if (row.AvailableAt > now || row.StatusCode is not ("PENDING" or "FAILED" or "PROCESSING") || row.StatusCode == "FAILED" && row.AttemptCount >= _options.OutboxMaxAttempts) return false;
        row.StatusCode = "PROCESSING";
        row.LastAttemptAt = now;
        row.AttemptCount++;
        row.AvailableAt = now.AddSeconds(Math.Max(30, _options.LeaseSeconds));
        row.ErrorMessage = null;
        await db.SaveChangesAsync(token);
        db.Entry(row).State = EntityState.Detached;
        return true;
    }

    private async Task CompleteAsync(long id, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        if (db.Database.IsRelational())
        {
            await db.OutboxEvents.Where(x => x.Id == id && x.StatusCode == "PROCESSING")
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StatusCode, "PUBLISHED")
                    .SetProperty(x => x.ProcessedAt, now).SetProperty(x => x.ErrorMessage, (string?)null), token);
            await db.OutboxRetryRequests.Where(x=>x.OutboxEventId==id&&x.StatusCode=="REQUESTED")
                .ExecuteUpdateAsync(setters=>setters.SetProperty(x=>x.StatusCode,"CONSUMED").SetProperty(x=>x.ConsumedAt,now),token);
            return;
        }
        var row = await db.OutboxEvents.SingleAsync(x => x.Id == id, token);
        row.StatusCode = "PUBLISHED";
        row.ProcessedAt = now;
        row.ErrorMessage = null;
        var retries=await db.OutboxRetryRequests.Where(x=>x.OutboxEventId==id&&x.StatusCode=="REQUESTED").ToListAsync(token);foreach(var retry in retries){retry.StatusCode="CONSUMED";retry.ConsumedAt=now;}
        await db.SaveChangesAsync(token);
    }

    private async Task FailAsync(long id, Exception exception, CancellationToken token)
    {
        var row = await db.OutboxEvents.SingleAsync(x => x.Id == id, token);
        var dead = row.AttemptCount >= _options.OutboxMaxAttempts;
        row.StatusCode = dead ? "DEAD" : "FAILED";
        row.ErrorMessage = SecurityTextSanitizer.ErrorCode(exception);
        row.AvailableAt = dead ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(BackoffSeconds(row.AttemptCount));
        if(dead){var retries=await db.OutboxRetryRequests.Where(x=>x.OutboxEventId==id&&x.StatusCode=="REQUESTED").ToListAsync(token);foreach(var retry in retries)retry.StatusCode="FAILED";}
        await db.SaveChangesAsync(token);
    }

    private int BackoffSeconds(int attempt)
    {
        var seconds = Math.Max(1, _options.OutboxBaseRetrySeconds) * Math.Pow(2, Math.Max(0, attempt - 1));
        return (int)Math.Min(Math.Max(1, _options.OutboxMaxRetrySeconds), seconds);
    }
}

public sealed class OutboxAutomationJob(OutboxProcessor processor, IOptions<AutomationOptions> options) : IAutomationJob
{
    public string Name => "OUTBOX_PROCESSOR";
    public AutomationConfigurationStatus ConfigurationStatus => options.Value.WorkerEnabled ? AutomationConfigurationStatus.Enabled : AutomationConfigurationStatus.Disabled;
    public TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
    public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => processor.ProcessBatchAsync(token);
}
