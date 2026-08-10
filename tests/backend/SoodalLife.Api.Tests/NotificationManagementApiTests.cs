using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Notifications;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class NotificationManagementApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_ManagesTemplates_RejectsUnsafeVariables_AndOtherRolesAreForbidden()
    {
        using var admin = Client(); using var customer = Client(); using var provider = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]); var code = $"REQUEST-{Guid.NewGuid():N}";
        var created = await CreateTemplate(admin, code, "REQUEST_DISPATCHED", "PROVIDER", "WEB", "새 요청이 도착했습니다.", "요청번호 {{request_no}}를 확인해 주세요."); Assert.True(created.IsActive); Assert.Contains("request_no", created.Variables);
        var disabled = await admin.PatchAsJsonAsync($"/api/v1/admin/notifications/templates/{created.Id}/status", new { isActive = false, rowVersion = created.RowVersion }); Assert.Equal(HttpStatusCode.OK, disabled.StatusCode); Assert.False((await disabled.Content.ReadFromJsonAsync<NotificationTemplateResponse>())!.IsActive);
        var unsafeResponse = await admin.PostAsJsonAsync("/api/v1/admin/notifications/templates", new { templateCode = $"BAD-{Guid.NewGuid():N}", name = "잘못된 템플릿", description = (string?)null, audienceTypeCode = "CUSTOMER", eventTypeCode = "QUOTE_SUBMITTED", channelCode = "WEB", titleTemplate = "견적 안내", bodyTemplate = "{{phone}} 확인", isRequiredBusinessNotice = true, isMarketing = false, effectiveFrom = (DateTime?)null, effectiveTo = (DateTime?)null }); Assert.Equal(HttpStatusCode.Conflict, unsafeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/admin/notifications/templates")).StatusCode); Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/v1/admin/notifications/templates")).StatusCode);
    }

    [Fact]
    public async Task Admin_ReadsAndUpdatesTemplate_WithRowVersion()
    {
        using var admin = Client();
        await Login(admin, factory.Credentials[RoleCodes.Admin]);
        var created = await CreateTemplate(admin, $"EDIT-{Guid.NewGuid():N}", "QUOTE_SUBMITTED", "CUSTOMER", "WEB", "견적 안내", "견적번호 {{quote_no}}를 확인해 주세요.");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/admin/notifications/templates/{created.Id}")).StatusCode);
        var response = await admin.PutAsJsonAsync($"/api/v1/admin/notifications/templates/{created.Id}", new
        {
            created.TemplateCode,
            name = "견적 도착 안내",
            description = "고객 업무 알림",
            created.AudienceTypeCode,
            created.EventTypeCode,
            created.ChannelCode,
            titleTemplate = "새 견적 안내",
            bodyTemplate = "견적번호 {{quote_no}}를 확인해 주세요.",
            created.IsRequiredBusinessNotice,
            created.IsMarketing,
            effectiveFrom = DateTime.UtcNow.AddMinutes(-1),
            effectiveTo = DateTime.UtcNow.AddDays(30),
            created.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("견적 도착 안내", (await response.Content.ReadFromJsonAsync<NotificationTemplateResponse>())!.Name);
    }

    [Fact]
    public async Task OutboxProcessing_CreatesSnapshotRecipientsAndChannelsOnce_WithoutExternalFakeSuccess()
    {
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); var templateCode = $"DISPATCH-{Guid.NewGuid():N}"; await CreateTemplate(admin, templateCode, "REQUEST_DISPATCHED", "PROVIDER", "WEB", "새 요청 안내", "요청번호 {{request_no}}를 확인해 주세요."); await CreateTemplate(admin, templateCode, "REQUEST_DISPATCHED", "PROVIDER", "KAKAO", "새 요청 안내", "요청번호 {{request_no}}를 확인해 주세요."); var providerUser = await UserId(factory.Credentials[RoleCodes.Provider]); var outbox = await SeedOutbox("RequestDispatch", Guid.NewGuid(), "REQUEST_DISPATCHED", new { recipientUserId = providerUser, request_no = "REQ-2026-0001" });
        var first = await admin.PostAsync($"/api/v1/admin/notifications/outbox/{outbox}/process", null); Assert.Equal(HttpStatusCode.OK, first.StatusCode); var result = (await first.Content.ReadFromJsonAsync<ProcessOutboxNotificationResponse>())!; Assert.Equal(1, result.NotificationsCreated); Assert.Equal(1, result.RecipientsCreated); Assert.Equal(2, result.DeliveriesCreated); var second = (await (await admin.PostAsync($"/api/v1/admin/notifications/outbox/{outbox}/process", null)).Content.ReadFromJsonAsync<ProcessOutboxNotificationResponse>())!; Assert.Equal(0, second.NotificationsCreated);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var keyPart = outbox.ToString("N"); var notification = await db.Notifications.SingleAsync(x => x.IdempotencyKey.Contains(keyPart)); Assert.Equal(templateCode, notification.TemplateCodeSnapshot); Assert.DoesNotContain("010", notification.Body); Assert.Single(await db.NotificationRecipients.Where(x => x.NotificationId == notification.Id).ToListAsync()); var deliveries = await db.NotificationDeliveries.Where(x => x.NotificationId == notification.Id).ToListAsync(); Assert.Contains(deliveries, x => x.ChannelCode == "WEB" && x.StatusCode == "DELIVERED"); Assert.Contains(deliveries, x => x.ChannelCode == "KAKAO" && x.StatusCode == "PENDING"); var template = await db.NotificationTemplates.FirstAsync(x => x.TemplateCode == templateCode); template.BodyTemplate = "변경된 내용"; await db.SaveChangesAsync(); Assert.Contains("REQ-2026-0001", (await db.Notifications.SingleAsync(x => x.Id == notification.Id)).Body);
    }

    [Fact]
    public async Task UserWebNotifications_AreIsolated_ReadableAndArchivable_WithPreferences()
    {
        using var admin = Client(); using var customer = Client(); using var provider = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]); var unreadBefore = (await customer.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/v1/notifications/unread-count"))!.Count; var eventType = $"CUSTOMER_EVENT_{Guid.NewGuid():N}"; await CreateTemplate(admin, $"CUSTOMER-{Guid.NewGuid():N}", eventType, "CUSTOMER", "WEB", "고객 업무 안내", "업무번호 {{source_no}}를 확인해 주세요."); var customerUser = await UserId(factory.Credentials[RoleCodes.Customer]); var outbox = await SeedOutbox("ServiceRequest", Guid.NewGuid(), eventType, new { recipientUserId = customerUser, source_no = "REQ-CUSTOMER" }); await admin.PostAsync($"/api/v1/admin/notifications/outbox/{outbox}/process", null);
        var list = (await customer.GetFromJsonAsync<List<NotificationListItem>>("/api/v1/notifications"))!; var item = Assert.Single(list, x => x.EventTypeCode == eventType.ToUpperInvariant()); Assert.Equal(unreadBefore + 1, (await customer.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/v1/notifications/unread-count"))!.Count); Assert.Equal(HttpStatusCode.NotFound, (await provider.GetAsync($"/api/v1/notifications/{item.Id}")).StatusCode); Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsync($"/api/v1/notifications/{item.Id}/read", null)).StatusCode); Assert.Equal(unreadBefore, (await customer.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/v1/notifications/unread-count"))!.Count); Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsync("/api/v1/notifications/read-all", null)).StatusCode); Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsync($"/api/v1/notifications/{item.Id}/archive", null)).StatusCode);
        var preference = await customer.PutAsJsonAsync("/api/v1/notifications/preferences", new { eventGroupCode = "BUSINESS", webEnabled = true, kakaoEnabled = false, smsEnabled = false, emailEnabled = false, pushEnabled = false }); Assert.Equal(HttpStatusCode.OK, preference.StatusCode); Assert.False((await preference.Content.ReadFromJsonAsync<NotificationPreferenceResponse>())!.KakaoEnabled);
    }

    [Fact]
    public async Task ExternalRetry_FailsHonestly_AndAttemptsAndEventsAreAppendOnly()
    {
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); var eventType = $"EXTERNAL_EVENT_{Guid.NewGuid():N}".ToUpperInvariant(); var code = $"EXTERNAL-{Guid.NewGuid():N}"; await CreateTemplate(admin, code, eventType, "CUSTOMER", "KAKAO", "업무 안내", "업무번호 {{source_no}}를 확인해 주세요."); var customerUser = await UserId(factory.Credentials[RoleCodes.Customer]); var outbox = await SeedOutbox("ServiceRequest", Guid.NewGuid(), eventType, new { recipientUserId = customerUser, source_no = "NO-1" }); await admin.PostAsync($"/api/v1/admin/notifications/outbox/{outbox}/process", null); var failedCandidate = Assert.Single((await admin.GetFromJsonAsync<List<NotificationDeliveryAdminItem>>("/api/v1/admin/notifications/deliveries?status=PENDING"))!, x => x.EventTypeCode == eventType); var retry = await admin.PostAsync($"/api/v1/admin/notifications/deliveries/{failedCandidate.Id}/retry", null); Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal("FAILED", (await retry.Content.ReadFromJsonAsync<NotificationDeliveryAdminItem>())!.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var delivery = await db.NotificationDeliveries.SingleAsync(x => x.PublicId == failedCandidate.Id); var attempt = Assert.Single(await db.NotificationDeliveryAttempts.Where(x => x.NotificationDeliveryId == delivery.Id).ToListAsync()); Assert.Equal("CHANNEL_NOT_CONFIGURED", attempt.ResultCode); attempt.FailureReason = "수정 시도"; await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear(); var evt = await db.NotificationEvents.FirstAsync(x => x.NotificationDeliveryId == delivery.Id); evt.EventDataJson = "{}"; await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ProcessingRepresentativeEvents_DoesNotChangeWalletFeesOrTrust()
    {
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); var events = new[] { "QUOTE_SUBMITTED", "QUOTE_ACCEPTED", "AFTER_SERVICE_RECEIVED", "DISPUTE_OPENED", "SUBSCRIPTION_VISIT_SCHEDULED", "INTERIOR_PROJECT_COMPLETED" }; var customerUser = await UserId(factory.Credentials[RoleCodes.Customer]); long walletCount, feeCount, trustEventCount; using (var before = factory.Services.CreateScope()) { var db = before.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); walletCount = await db.WalletLedgerEntries.CountAsync(); feeCount = await db.FeeCharges.CountAsync(); trustEventCount = await db.TrustScoreEvents.CountAsync(); }
        foreach (var type in events) { await CreateTemplate(admin, $"{type}-{Guid.NewGuid():N}", type, "CUSTOMER", "WEB", "업무 상태 안내", "업무번호 {{source_no}}를 확인해 주세요."); var outbox = await SeedOutbox(type.StartsWith("INTERIOR") ? "InteriorProject" : type.StartsWith("SUBSCRIPTION") ? "SubscriptionVisitSchedule" : "Business", Guid.NewGuid(), type, new { recipientUserId = customerUser, source_no = $"SRC-{type}" }); Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/notifications/outbox/{outbox}/process", null)).StatusCode); }
        using var after = factory.Services.CreateScope(); var verify = after.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Equal(walletCount, await verify.WalletLedgerEntries.CountAsync()); Assert.Equal(feeCount, await verify.FeeCharges.CountAsync()); Assert.Equal(trustEventCount, await verify.TrustScoreEvents.CountAsync());
    }

    private async Task<NotificationTemplateResponse> CreateTemplate(HttpClient admin, string code, string eventType, string audience, string channel, string title, string body) { var response = await admin.PostAsJsonAsync("/api/v1/admin/notifications/templates", new { templateCode = code, name = $"{eventType} 안내", description = (string?)null, audienceTypeCode = audience, eventTypeCode = eventType, channelCode = channel, titleTemplate = title, bodyTemplate = body, isRequiredBusinessNotice = true, isMarketing = false, effectiveFrom = (DateTime?)null, effectiveTo = (DateTime?)null }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<NotificationTemplateResponse>())!; }
    private async Task<Guid> SeedOutbox(string aggregate, Guid aggregateId, string type, object payload) { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var now = DateTime.UtcNow; var row = new OutboxEvent { AggregateType = aggregate, AggregatePublicId = aggregateId, EventType = type, PayloadJson = System.Text.Json.JsonSerializer.Serialize(payload), StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"notification-test:{Guid.NewGuid():N}" }; db.OutboxEvents.Add(row); await db.SaveChangesAsync(); return row.PublicId; }
    private async Task<long> UserId(TestCredential credential) { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); return await db.Users.Where(x => x.LoginId == credential.LoginId).Select(x => x.Id).SingleAsync(); }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true }); private static async Task Login(HttpClient c, TestCredential credential) => Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);
}
