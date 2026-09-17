using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Automation;

public sealed class CustomerEngagementReminderJob(
    SoodalLifeDbContext db,
    IOptions<AutomationOptions> options) : IAutomationJob
{
    private readonly AutomationOptions _options = options.Value;
    public string Name => "CUSTOMER_ENGAGEMENT_REMINDERS";
    public AutomationConfigurationStatus ConfigurationStatus => _options.WorkerEnabled
        ? AutomationConfigurationStatus.Enabled
        : AutomationConfigurationStatus.Disabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(5, _options.CustomerEngagementReminderIntervalMinutes));

    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)
    {
        await EnsureTemplatesAsync(token);
        var now = DateTime.UtcNow;
        var processed = 0;
        processed += await QueueQuoteRemindersAsync(now, token);
        processed += await QueueCompletionConfirmationRemindersAsync(now, token);
        processed += await QueueReviewRemindersAsync(now, token);
        processed += await QueueProviderOnboardingRemindersAsync(now, token);
        return new(processed);
    }

    private async Task<int> QueueCompletionConfirmationRemindersAsync(DateTime now, CancellationToken token)
    {
        var candidates = await (from transaction in db.Transactions.AsNoTracking()
                                join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                                join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                                join completion in db.WorkCompletions.AsNoTracking() on transaction.Id equals completion.TransactionId
                                where transaction.StatusCode == "COMPLETION_SUBMITTED" && completion.FirstSubmittedAt != null && completion.FirstSubmittedAt <= now.AddHours(-24)
                                    && !db.CustomerConfirmations.Any(confirmation => confirmation.TransactionId == transaction.Id)
                                    && !db.DisputeCases.Any(dispute => dispute.TransactionId == transaction.Id && dispute.StatusCode != "RESOLVED")
                                orderby completion.FirstSubmittedAt
                                select new { Transaction = transaction, customer.UserId, ServiceName = category.Name, SubmittedAt = completion.FirstSubmittedAt!.Value })
            .Take(200).ToListAsync(token);
        var count = 0;
        foreach (var candidate in candidates)
        {
            var finalReminder = candidate.SubmittedAt <= now.AddHours(-72);
            var phase = finalReminder ? "H72" : "H24";
            var eventType = finalReminder ? "WORK_COMPLETION_CONFIRMATION_REMINDER_H72" : "WORK_COMPLETION_CONFIRMATION_REMINDER_H24";
            var key = $"engagement:completion-confirmation:{candidate.Transaction.PublicId:N}:{phase.ToLowerInvariant()}";
            if (await AddOutboxAsync(candidate.Transaction.PublicId, eventType, key, candidate.UserId,
                    candidate.ServiceName, candidate.Transaction.PublicId, null, now, token, "Transaction")) count++;
        }
        return count;
    }

    private async Task<int> QueueProviderOnboardingRemindersAsync(DateTime now, CancellationToken token)
    {
        var candidates = await db.ProviderProfiles.AsNoTracking()
            .Where(provider => provider.CreatedAt <= now.AddHours(-24) &&
                db.Users.Any(user => user.Id == provider.UserId && user.StatusCode == "ACTIVE") &&
                (!db.ProviderServiceCategories.Any(service => service.ProviderProfileId == provider.Id && service.StatusCode == "ACTIVE") ||
                 !db.ProviderServiceCategories.Any(service => service.ProviderProfileId == provider.Id && service.StatusCode == "ACTIVE" &&
                    (service.IsNationwide || db.ProviderServiceAreas.Any(area => area.ProviderServiceCategoryId == service.Id && area.StatusCode == "ACTIVE")))))
            .OrderBy(provider => provider.CreatedAt)
            .Select(provider => new { provider.PublicId, provider.UserId, provider.BusinessName, provider.CreatedAt })
            .Take(200).ToListAsync(token);

        var count = 0;
        foreach (var candidate in candidates)
        {
            var phase = candidate.CreatedAt <= now.AddDays(-7) ? "D7" : "D1";
            var key = $"engagement:provider-onboarding:{candidate.PublicId:N}:{phase.ToLowerInvariant()}";
            if (await AddProviderOnboardingOutboxAsync(candidate.PublicId, key, candidate.UserId, candidate.BusinessName, now, token)) count++;
        }
        return count;
    }

    private async Task<bool> AddProviderOnboardingOutboxAsync(Guid providerId, string key, long recipientUserId,
        string providerName, DateTime now, CancellationToken token)
    {
        if (await db.OutboxEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == key, token)) return false;
        db.OutboxEvents.Add(new OutboxEvent
        {
            AggregateType = "ProviderProfile",
            AggregatePublicId = providerId,
            EventType = "PROVIDER_ONBOARDING_INCOMPLETE_REMINDER",
            PayloadJson = JsonSerializer.Serialize(new { recipientUserId, provider_name = providerName }),
            StatusCode = "PENDING",
            OccurredAt = now,
            AvailableAt = now,
            IdempotencyKey = key
        });
        try { await db.SaveChangesAsync(token); return true; }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return false; }
    }

    private async Task<int> QueueQuoteRemindersAsync(DateTime now, CancellationToken token)
    {
        var candidates = await (from request in db.ServiceRequests.AsNoTracking()
                                join customer in db.CustomerProfiles.AsNoTracking() on request.CustomerProfileId equals customer.Id
                                join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
                                let firstSubmittedAt = db.Quotes.Where(quote => quote.ServiceRequestId == request.Id && quote.StatusCode == "SUBMITTED" && quote.SubmittedAt != null && (quote.ExpiresAt == null || quote.ExpiresAt > now)).Min(quote => (DateTime?)quote.SubmittedAt)
                                let quoteCount = db.Quotes.Count(quote => quote.ServiceRequestId == request.Id && quote.StatusCode == "SUBMITTED" && (quote.ExpiresAt == null || quote.ExpiresAt > now))
                                where request.StatusCode == "OPEN" && request.AcceptedAt == null && (request.ExpiresAt == null || request.ExpiresAt > now) && firstSubmittedAt != null
                                orderby firstSubmittedAt
                                select new { Request = request, customer.UserId, ServiceName = category.Name, FirstSubmittedAt = firstSubmittedAt!.Value, QuoteCount = quoteCount })
            .Take(200).ToListAsync(token);

        var count = 0;
        foreach (var candidate in candidates)
        {
            if (candidate.Request.CustomerQuotesViewedAt is null && candidate.FirstSubmittedAt <= now.AddMinutes(-30))
            {
                var key = $"engagement:quote-unviewed:{candidate.Request.PublicId:N}";
                if (await AddOutboxAsync(candidate.Request.PublicId, "QUOTE_UNVIEWED_REMINDER", key, candidate.UserId,
                        candidate.ServiceName, candidate.Request.PublicId, candidate.QuoteCount, now, token)) count++;
            }

            if (candidate.FirstSubmittedAt <= now.AddHours(-24))
            {
                var key = $"engagement:quote-selection:{candidate.Request.PublicId:N}";
                if (await AddOutboxAsync(candidate.Request.PublicId, "QUOTE_SELECTION_REMINDER", key, candidate.UserId,
                        candidate.ServiceName, candidate.Request.PublicId, candidate.QuoteCount, now, token)) count++;
            }
        }
        return count;
    }

    private async Task<int> QueueReviewRemindersAsync(DateTime now, CancellationToken token)
    {
        var candidates = await (from transaction in db.Transactions.AsNoTracking()
                                join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                                join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                                where transaction.StatusCode == "COMPLETED" && transaction.CompletedAt != null && transaction.CompletedAt <= now.AddHours(-24)
                                    && !db.Reviews.Any(review => review.TransactionId == transaction.Id)
                                    && !db.DisputeCases.Any(dispute => dispute.TransactionId == transaction.Id)
                                    && !db.AfterServiceCases.Any(after => after.TransactionId == transaction.Id && after.StatusCode != "RESOLVED" && after.StatusCode != "UNRESOLVED_CLOSED")
                                orderby transaction.CompletedAt
                                select new { Transaction = transaction, customer.UserId, ServiceName = category.Name })
            .Take(200).ToListAsync(token);

        var count = 0;
        foreach (var candidate in candidates)
        {
            var finalReminder = candidate.Transaction.CompletedAt <= now.AddDays(-7);
            var phase = finalReminder ? "D7" : "D1";
            var eventType = finalReminder ? "SERVICE_REVIEW_REMINDER_D7" : "SERVICE_REVIEW_REMINDER_D1";
            var key = $"engagement:review:{candidate.Transaction.PublicId:N}:{phase.ToLowerInvariant()}";
            if (await AddOutboxAsync(candidate.Transaction.PublicId, eventType, key, candidate.UserId,
                    candidate.ServiceName, candidate.Transaction.PublicId, null, now, token, "Transaction")) count++;
        }
        return count;
    }

    private async Task<bool> AddOutboxAsync(Guid aggregateId, string eventType, string key, long recipientUserId,
        string serviceName, Guid sourceId, int? quoteCount, DateTime now, CancellationToken token, string aggregateType = "ServiceRequest")
    {
        if (await db.OutboxEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == key, token)) return false;
        db.OutboxEvents.Add(new OutboxEvent
        {
            AggregateType = aggregateType,
            AggregatePublicId = aggregateId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(new
            {
                recipientUserId,
                service_name = serviceName,
                request_no = sourceId.ToString("N")[..10].ToUpperInvariant(),
                quote_count = quoteCount
            }),
            StatusCode = "PENDING",
            OccurredAt = now,
            AvailableAt = now,
            IdempotencyKey = key
        });
        try { await db.SaveChangesAsync(token); return true; }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return false; }
    }

    private async Task EnsureTemplatesAsync(CancellationToken token)
    {
        var templates = new[]
        {
            new Template("QUOTE_SUBMITTED_NOTICE", "견적 도착 안내", "QUOTE_SUBMITTED", "새 견적이 도착했습니다", "요청번호 {{request_no}}에 전문가 견적이 도착했습니다. 견적을 확인해 주세요."),
            new Template("QUOTE_UNVIEWED_REMINDER", "미확인 견적 안내", "QUOTE_UNVIEWED_REMINDER", "아직 확인하지 않은 견적이 있습니다", "{{service_name}} 요청에 도착한 견적을 비교해 보세요. 요청번호 {{request_no}}"),
            new Template("QUOTE_SELECTION_REMINDER", "전문가 선택 안내", "QUOTE_SELECTION_REMINDER", "견적을 비교하고 진행 여부를 알려주세요", "{{service_name}} 견적을 비교한 뒤 전문가를 선택하거나 요청을 종료해 주세요. 요청번호 {{request_no}}"),
            new Template("WORK_COMPLETION_CONFIRMATION_REMINDER_H24", "작업 완료 확인 1차 안내", "WORK_COMPLETION_CONFIRMATION_REMINDER_H24", "전문가의 작업 완료보고를 확인해 주세요", "{{service_name}} 작업 완료자료를 확인한 뒤 완료·보완 요청·분쟁 중 하나를 선택해 주세요. 거래번호 {{request_no}}"),
            new Template("WORK_COMPLETION_CONFIRMATION_REMINDER_H72", "작업 완료 확인 재안내", "WORK_COMPLETION_CONFIRMATION_REMINDER_H72", "작업 완료 확인이 기다리고 있습니다", "{{service_name}} 완료보고가 72시간 동안 확인되지 않았습니다. 자동 완료되지는 않으며 고객님이 직접 결과를 선택할 수 있습니다. 거래번호 {{request_no}}"),
            new Template("SERVICE_REVIEW_REMINDER_D1", "서비스 리뷰 1차 안내", "SERVICE_REVIEW_REMINDER_D1", "서비스는 만족스러우셨나요?", "{{service_name}} 서비스의 평가와 이용 후기를 남겨 주세요. 거래번호 {{request_no}}"),
            new Template("SERVICE_REVIEW_REMINDER_D7", "서비스 리뷰 최종 안내", "SERVICE_REVIEW_REMINDER_D7", "서비스 리뷰를 남길 수 있습니다", "{{service_name}} 서비스의 평가와 이용 후기를 아직 작성하지 않으셨다면 남겨 주세요. 거래번호 {{request_no}}"),
            new Template("PROVIDER_ONBOARDING_INCOMPLETE_REMINDER", "전문가 활동정보 등록 안내", "PROVIDER_ONBOARDING_INCOMPLETE_REMINDER", "전문가 활동정보를 등록해 주세요", "{{provider_name}} 전문가님, 서비스 분야와 활동 지역 등록 상태를 확인해 주세요. 등록이 완료되어야 고객 요청을 정상적으로 받을 수 있습니다.")
        };
        var now = DateTime.UtcNow;
        foreach (var definition in templates)
        {
            foreach (var channel in new[] { "WEB", "KAKAO" })
            {
                if (await db.NotificationTemplates.AnyAsync(item => item.TemplateCode == definition.Code && item.ChannelCode == channel, token)) continue;
                db.NotificationTemplates.Add(new NotificationTemplate
                {
                    TemplateCode = definition.Code,
                    Name = definition.Name,
                    Description = channel == "KAKAO" ? "NHN 알림톡 템플릿 승인 후 관리자가 활성화합니다." : definition.Code.StartsWith("PROVIDER_") ? "전문가 등록 완료 안내" : "고객 거래 후속 안내",
                    AudienceTypeCode = definition.Code.StartsWith("PROVIDER_") ? "PROVIDER" : "CUSTOMER",
                    EventTypeCode = definition.EventType,
                    ChannelCode = channel,
                    TitleTemplate = definition.Title,
                    BodyTemplate = definition.Body,
                    AllowedVariablesJson = definition.Code.StartsWith("PROVIDER_") ? "[\"provider_name\"]" : "[\"request_no\",\"service_name\"]",
                    IsRequiredBusinessNotice = false,
                    IsMarketing = false,
                    IsActive = channel == "WEB",
                    EffectiveFrom = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await db.SaveChangesAsync(token);
    }

    private sealed record Template(string Code, string Name, string EventType, string Title, string Body);
}
