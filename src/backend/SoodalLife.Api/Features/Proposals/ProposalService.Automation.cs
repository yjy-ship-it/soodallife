using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Features.Proposals;

public sealed partial class ProposalService
{
    public async Task<int> ProcessDue(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var campaigns = await db.ProviderProposalCampaigns
            .Where(x => x.StatusCode == "PUBLISHED" || x.StatusCode == "MINIMUM_MET" || x.StatusCode == "FULL")
            .OrderBy(x => x.EndAt).Take(100).ToListAsync(token);
        var processed = 0;
        foreach (var campaign in campaigns)
        {
            if (campaign.EndAt <= now)
            {
                await CloseExpired(campaign, token); processed++; continue;
            }
            if (campaign.PublishedNotificationQueuedAt is null && campaign.StartAt <= now)
            {
                await QueueMarketing(campaign, "PUBLISHED", token); campaign.PublishedNotificationQueuedAt = now; processed++;
            }
            var midpoint = campaign.StartAt + TimeSpan.FromTicks((campaign.EndAt - campaign.StartAt).Ticks / 2);
            if (campaign.MidpointNotificationQueuedAt is null && midpoint <= now)
            {
                await QueueMarketing(campaign, "MIDPOINT", token); campaign.MidpointNotificationQueuedAt = now; processed++;
            }
            if (campaign.ClosingNotificationQueuedAt is null && campaign.EndAt <= now.AddHours(1))
            {
                await QueueMarketing(campaign, "CLOSING", token); campaign.ClosingNotificationQueuedAt = now; processed++;
            }
            if (processed > 0) await db.SaveChangesAsync(token);
        }
        return processed;
    }

    private async Task CloseExpired(ProviderProposalCampaign campaign, CancellationToken token)
    {
        var actor = campaign.CreatedByUserId ?? await db.ProviderProfiles.Where(x => x.Id == campaign.ProviderProfileId).Select(x => x.UserId).SingleAsync(token);
        await using var transaction = await wallets.BeginTransactionAsync(token);
        if (campaign.ConfirmedParticipants < campaign.MinimumParticipants)
        {
            await RestoreAndRelease(campaign, actor, "최소 모집인원 미달 자동 취소", true, token);
        }
        else
        {
            var wallet = await db.ProviderWallets.SingleAsync(x => x.Id == campaign.WalletId, token);
            var now = DateTime.UtcNow;
            if (campaign.ReservedFeeAmount > 0)
            {
                wallet.ReservedBalance -= campaign.ReservedFeeAmount; wallet.AvailableBalance += campaign.ReservedFeeAmount;
                var release = wallets.AddLedger(wallet, null, "RELEASE", campaign.ReservedFeeAmount,
                    $"proposal-close-release:{campaign.PublicId:N}", "미확정 모집인원 수수료 예약 해제",
                    "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, actor);
                campaign.ReleaseLedgerEntryId = release.Id; campaign.ReservedFeeAmount = 0;
            }
            var waiting = await db.ProviderProposalApplications.Where(x => x.CampaignId == campaign.Id && x.StatusCode == "APPLIED").ToListAsync(token);
            foreach (var row in waiting) { row.StatusCode = "DECLINED"; row.DeclinedAt = now; row.UpdatedAt = now; row.UpdatedByUserId = actor; }
            campaign.StatusCode = "EXPIRED"; campaign.FeeStatusCode = campaign.CapturedFeeAmount > 0 ? "CAPTURED" : "RELEASED";
            campaign.ClosedAt = now; campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor;
            wallet.UpdatedAt = now; wallet.UpdatedByUserId = actor;
            await wallets.SaveWithConcurrencyAsync(token);
        }
        if (transaction is not null) await transaction.CommitAsync(token);
    }

    private async Task QueueMarketing(ProviderProposalCampaign campaign, string stage, CancellationToken token)
    {
        var serviceCategoryId = await db.ProviderServiceCategories.Where(x => x.Id == campaign.ProviderServiceCategoryId).Select(x => x.CategoryId).SingleAsync(token);
        var localAreaIds = await db.ProviderProposalAreas.Where(x => x.CampaignId == campaign.Id).Select(x => x.AdministrativeAreaId).ToListAsync(token);
        var profiles = await db.CustomerProfiles.AsNoTracking().Select(x => new { x.Id, x.UserId }).ToListAsync(token);
        foreach (var customer in profiles)
        {
            if (!await HasFinalMarketingConsent(customer.UserId, token)) continue;
            var preference = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == customer.UserId && x.EventGroupCode == "MARKETING" && x.WithdrawnAt == null, token);
            if (preference is null || !preference.WebEnabled) continue;

            var explicitCategory = await db.CustomerProposalCategoryInterests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.CategoryId == serviceCategoryId && x.IsActive, token);
            var historicalCategory = await db.ServiceRequests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.CategoryId == serviceCategoryId, token)
                || await db.Transactions.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.CategoryId == serviceCategoryId, token)
                || await db.CustomerProposalSignals.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.CategoryId == serviceCategoryId && x.ExpiresAt > DateTime.UtcNow, token);
            var hasAnyCategorySignal = await db.CustomerProposalCategoryInterests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.IsActive, token)
                || await db.ServiceRequests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id, token)
                || await db.Transactions.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id, token)
                || await db.CustomerProposalSignals.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.ExpiresAt > DateTime.UtcNow, token);
            var categoryMatched = explicitCategory || historicalCategory || (stage == "PUBLISHED" && !hasAnyCategorySignal);
            if (!categoryMatched) continue;

            var localMatched = campaign.ScopeCode == "NATIONWIDE";
            if (!localMatched)
            {
                localMatched = await db.CustomerProposalAreaInterests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.IsActive && localAreaIds.Contains(x.AdministrativeAreaId), token)
                    || await db.ServiceRequests.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.AdministrativeAreaId.HasValue && localAreaIds.Contains(x.AdministrativeAreaId.Value), token)
                    || await db.CustomerAddresses.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id && x.IsActive && x.AdministrativeAreaId.HasValue && localAreaIds.Contains(x.AdministrativeAreaId.Value), token);
            }
            if (!localMatched) continue;

            var today = DateTime.UtcNow.AddDays(-1); var week = DateTime.UtcNow.AddDays(-7);
            if (await db.Notifications.AsNoTracking().CountAsync(x => x.RecipientUserId == customer.UserId && x.TypeCode.StartsWith("PROPOSAL_MARKETING_") && x.RecordedAt >= today, token) >= 1) continue;
            if (await db.Notifications.AsNoTracking().CountAsync(x => x.RecipientUserId == customer.UserId && x.TypeCode.StartsWith("PROPOSAL_MARKETING_") && x.RecordedAt >= week, token) >= 3) continue;
            await AddMarketingNotification(campaign, customer.UserId, preference, stage, token);
        }
    }

    private async Task AddMarketingNotification(ProviderProposalCampaign campaign, long userId, NotificationPreference preference, string stage, CancellationToken token)
    {
        var key = $"proposal-marketing:{campaign.PublicId:N}:{stage}:{userId}";
        if (await db.Notifications.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        var now = DateTime.UtcNow;
        var title = stage switch { "CLOSING" => "마감 1시간 전 공동모집", "MIDPOINT" => "모집기간 절반이 지났어요", _ => "새로운 전문가 제안·공동모집" };
        var notification = new Notification
        {
            RecipientUserId = userId, TypeCode = $"PROPOSAL_MARKETING_{stage}", PriorityCode = "NORMAL",
            SourceTypeCode = "PROVIDER_PROPOSAL_CAMPAIGN", SourcePublicId = campaign.PublicId,
            TargetTypeCode = "PROPOSAL", TargetPublicId = campaign.PublicId, StatusCode = "PENDING", Title = title,
            Body = campaign.Title, DataJson = JsonSerializer.Serialize(new { proposalId = campaign.PublicId, route = $"/proposals/{campaign.PublicId}" }),
            IsUrgent = false, RecordedAt = now, ExpiresAt = campaign.EndAt, IdempotencyKey = key, CreatedByUserId = campaign.CreatedByUserId
        };
        db.Notifications.Add(notification); await db.SaveChangesAsync(token);
        var recipient = new NotificationRecipient { NotificationId = notification.Id, UserId = userId, RecipientRoleCode = "CUSTOMER", CreatedAt = now };
        db.NotificationRecipients.Add(recipient); await db.SaveChangesAsync(token);
        await AddDelivery(notification, recipient, "WEB", true, now, token);
        if (preference.PushEnabled) await AddDelivery(notification, recipient, "PUSH", await ExternalChannelEnabled("PUSH", token), now, token);
        if (campaign.ScopeCode == "LOCAL" && preference.SmsEnabled) await AddDelivery(notification, recipient, "SMS", await ExternalChannelEnabled("SMS", token), now, token);
        await db.SaveChangesAsync(token);
    }

    private async Task AddDelivery(Notification notification, NotificationRecipient recipient, string channel, bool enabled, DateTime now, CancellationToken token)
    {
        if (await db.NotificationDeliveries.AnyAsync(x => x.NotificationId == notification.Id && x.ChannelCode == channel, token)) return;
        db.NotificationDeliveries.Add(new NotificationDelivery
        {
            NotificationId = notification.Id, NotificationRecipientId = recipient.Id, ChannelCode = channel, AttemptNo = 1,
            StatusCode = enabled && channel != "WEB" ? "PENDING" : enabled ? "DELIVERED" : "SKIPPED",
            ScheduledAt = enabled && channel != "WEB" ? now : null, DeliveredAt = enabled && channel == "WEB" ? now : null,
            AttemptedAt = now, CompletedAt = enabled && channel == "WEB" ? now : null, CreatedAt = now, UpdatedAt = now
        });
    }

    private async Task<bool> ExternalChannelEnabled(string channel, CancellationToken token)
    {
        if (!options.MarketingTermsFinalized || channel == "PUSH" && !options.PwaPushEnabled) return false;
        return await db.NotificationChannelSettings.AsNoTracking().AnyAsync(x => x.ChannelCode == channel && x.IsEnabled && x.OperationModeCode != "DISABLED", token);
    }

    private async Task<bool> HasFinalMarketingConsent(long userId, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        return await (from consent in db.UserConsents.AsNoTracking()
            join version in db.LegalDocumentVersions.AsNoTracking() on consent.LegalDocumentVersionId equals version.Id
            join document in db.LegalDocuments.AsNoTracking() on version.LegalDocumentId equals document.Id
            where consent.UserId == userId && consent.ConsentStatusCode == "CONSENTED" && consent.WithdrawnAt == null &&
                  document.Code == "MARKETING_CONSENT" && document.IsActive && !document.IsPlaceholder &&
                  version.IsActive && !version.IsPlaceholder && version.EffectiveFrom <= now && (version.EffectiveTo == null || version.EffectiveTo > now)
            select consent.Id).AnyAsync(token);
    }

    private async Task AddBusinessNotification(long customerProfileId, ProviderProposalCampaign campaign, string title, string body, string suffix, CancellationToken token)
    {
        var userId = await db.CustomerProfiles.Where(x => x.Id == customerProfileId).Select(x => x.UserId).SingleAsync(token);
        var key = $"proposal-business:{campaign.PublicId:N}:{suffix}:{userId}";
        if (await db.Notifications.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        var now = DateTime.UtcNow;
        var notification = new Notification { RecipientUserId = userId, TypeCode = "PROPOSAL_BUSINESS_NOTICE", PriorityCode = "NORMAL", SourceTypeCode = "PROVIDER_PROPOSAL_CAMPAIGN", SourcePublicId = campaign.PublicId, TargetTypeCode = "PROPOSAL", TargetPublicId = campaign.PublicId, StatusCode = "PENDING", Title = title, Body = $"{campaign.Title} · {body}", DataJson = JsonSerializer.Serialize(new { proposalId = campaign.PublicId, route = $"/proposals/{campaign.PublicId}" }), RecordedAt = now, IdempotencyKey = key, CreatedByUserId = campaign.CreatedByUserId };
        db.Notifications.Add(notification); await db.SaveChangesAsync(token);
        var recipient = new NotificationRecipient { NotificationId = notification.Id, UserId = userId, RecipientRoleCode = "CUSTOMER", CreatedAt = now };
        db.NotificationRecipients.Add(recipient); await db.SaveChangesAsync(token);
        await AddDelivery(notification, recipient, "WEB", true, now, token); await db.SaveChangesAsync(token);
    }
}
