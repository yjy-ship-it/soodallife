using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class SubscriptionLifecycleAutomationService(SoodalLifeDbContext db)
{
    private static readonly string[] ClosedDisputeStatuses = ["RESOLVED", "CLOSED", "REJECTED", "CANCELLED"];
    private static readonly string[] ClosedAfterServiceStatuses = ["COMPLETED", "CLOSED", "REJECTED", "CANCELLED"];

    public async Task<int> ProcessDueAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var processed = await ExpireScheduleChanges(now, token);
        processed += await ProcessProviderCompletions(now, token);
        if (processed > 0) await db.SaveChangesAsync(token);
        return processed;
    }

    private async Task<int> ExpireScheduleChanges(DateTime now, CancellationToken token)
    {
        var due = await db.SubscriptionScheduleChanges
            .Where(x => x.StatusCode == "REQUESTED" && x.RequestedAt <= now.AddHours(-48))
            .ToListAsync(token);
        foreach (var item in due)
        {
            item.StatusCode = "EXPIRED";
            item.DecidedAt = now;
            item.UpdatedAt = now;
            var visit = await db.SubscriptionVisitSchedules.SingleAsync(x => x.Id == item.SubscriptionVisitScheduleId, token);
            var contract = await db.SubscriptionContracts.SingleAsync(x => x.Id == visit.SubscriptionContractId, token);
            db.SubscriptionEvents.Add(new SubscriptionEvent
            {
                SubscriptionRequestId = contract.SubscriptionRequestId,
                SubscriptionContractId = contract.Id,
                SubscriptionVisitScheduleId = visit.Id,
                EventTypeCode = "SCHEDULE_CHANGE_EXPIRED",
                EventDataJson = JsonSerializer.Serialize(new { item.PublicId, expiryHours = 48 }),
                OccurredAt = now,
                IdempotencyKey = $"subscription-schedule-change:{item.PublicId:N}:expired"
            });
        }
        return due.Count;
    }

    private async Task<int> ProcessProviderCompletions(DateTime now, CancellationToken token)
    {
        var visits = await db.SubscriptionVisitSchedules
            .Where(x => x.StatusCode == "PROVIDER_COMPLETED" && x.ProviderCompletionSubmittedAt != null)
            .OrderBy(x => x.ProviderCompletionSubmittedAt)
            .ToListAsync(token);
        var processed = 0;
        foreach (var visit in visits)
        {
            var submittedAt = visit.ProviderCompletionSubmittedAt!.Value;
            var contract = await db.SubscriptionContracts.SingleAsync(x => x.Id == visit.SubscriptionContractId, token);
            var customerUserId = await db.CustomerProfiles.Where(x => x.Id == contract.CustomerProfileId).Select(x => x.UserId).SingleAsync(token);
            var providerUserId = await db.ProviderProfiles.Where(x => x.Id == contract.ProviderProfileId).Select(x => x.UserId).SingleAsync(token);

            if (submittedAt <= now.AddDays(-1))
                await NotifyIfConfigured("SUBSCRIPTION_COMPLETION_REMINDER", visit.PublicId, customerUserId,
                    new { recipientUserId = customerUserId, visitId = visit.PublicId, remainingDays = submittedAt <= now.AddDays(-2) ? 1 : 2 },
                    $"subscription-visit:{visit.PublicId:N}:completion-reminder:{(submittedAt <= now.AddDays(-2) ? 1 : 2)}", now, token);

            if (submittedAt > now.AddDays(-3) || await HasOpenCase(visit.Id, token)) continue;

            visit.CustomerConfirmedAt = now;
            visit.StatusCode = "COMPLETED";
            visit.SettlementStatusCode = "READY";
            visit.UpdatedAt = now;
            visit.UpdatedByUserId = null;
            db.SubscriptionEvents.Add(new SubscriptionEvent
            {
                SubscriptionRequestId = contract.SubscriptionRequestId,
                SubscriptionContractId = contract.Id,
                SubscriptionVisitScheduleId = visit.Id,
                EventTypeCode = "VISIT_AUTO_CONFIRMED",
                EventDataJson = JsonSerializer.Serialize(new { confirmationWindowDays = 3 }),
                OccurredAt = now,
                IdempotencyKey = $"subscription-visit:{visit.PublicId:N}:auto-confirmed"
            });
            await EnsureHistory(contract, visit, now, token);
            await NotifyIfConfigured("SUBSCRIPTION_VISIT_AUTO_CONFIRMED", visit.PublicId, customerUserId,
                new { recipientUserId = customerUserId, visitId = visit.PublicId },
                $"subscription-visit:{visit.PublicId:N}:auto-confirmed:customer", now, token);
            await NotifyIfConfigured("SUBSCRIPTION_VISIT_AUTO_CONFIRMED", visit.PublicId, providerUserId,
                new { recipientUserId = providerUserId, visitId = visit.PublicId },
                $"subscription-visit:{visit.PublicId:N}:auto-confirmed:provider", now, token);
            processed++;
        }
        return processed;
    }

    private async Task<bool> HasOpenCase(long visitId, CancellationToken token) =>
        await db.DisputeCases.AnyAsync(x => x.SubscriptionVisitScheduleId == visitId && !ClosedDisputeStatuses.Contains(x.StatusCode), token) ||
        await db.AfterServiceCases.AnyAsync(x => x.SubscriptionVisitScheduleId == visitId && !ClosedAfterServiceStatuses.Contains(x.StatusCode), token);

    private async Task EnsureHistory(SubscriptionContract contract, SubscriptionVisitSchedule visit, DateTime now, CancellationToken token)
    {
        var key = $"subscription-visit:{visit.PublicId:N}:completed";
        if (await db.ServiceHistoryEntries.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        var provider = await db.ProviderProfiles.SingleAsync(x => x.Id == contract.ProviderProfileId, token);
        var category = await db.ServiceCategories.SingleAsync(x => x.Id == contract.ServiceCategoryId, token);
        db.ServiceHistoryEntries.Add(new ServiceHistoryEntry
        {
            CustomerProfileId = contract.CustomerProfileId,
            SubscriptionVisitScheduleId = visit.Id,
            EventTypeCode = "SUBSCRIPTION_VISIT_COMPLETED",
            Title = $"{category.Name} 정기 방문 완료",
            Summary = visit.CompletionNote ?? "전문가 완료보고 후 확인 기한이 지나 자동 완료되었습니다.",
            ProviderNameSnapshot = provider.BusinessName,
            CategoryNameSnapshot = category.Name,
            CompletedAtSnapshot = now,
            SnapshotJson = JsonSerializer.Serialize(new { contractId = contract.PublicId, visitId = visit.PublicId, visit.VisitNo, autoConfirmed = true }),
            OccurredAt = now,
            IdempotencyKey = key,
            CreatedAt = now
        });
    }

    private async Task NotifyIfConfigured(string eventType, Guid aggregateId, long recipientUserId, object payload, string key, DateTime now, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token) ||
            await db.OutboxEvents.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        db.OutboxEvents.Add(new OutboxEvent
        {
            AggregateType = "SubscriptionVisitSchedule",
            AggregatePublicId = aggregateId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            StatusCode = "PENDING",
            OccurredAt = now,
            AvailableAt = now,
            IdempotencyKey = key,
            CreatedByUserId = recipientUserId
        });
    }
}
