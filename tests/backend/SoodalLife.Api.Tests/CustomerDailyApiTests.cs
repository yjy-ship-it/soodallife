using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Features.Notifications;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CustomerDailyApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task MySoodalSummary_ReturnsOnlyRealAggregatesWithoutSensitiveFields()
    {
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var response = await customer.GetAsync("/api/v1/customers/me/my-soodal/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        Assert.DoesNotContain("password", json); Assert.DoesNotContain("rowversion", json); Assert.DoesNotContain("userid", json);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<MySoodalSummaryResponse>());
    }

    [Fact]
    public async Task CustomerDailyApis_BlockProvider_AndKeepAdminRegressionHealthy()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        foreach (var path in new[] { "/api/v1/customers/me/my-soodal/summary", "/api/v1/customers/me/consent-history", "/api/v1/customers/me/withdrawal-readiness" })
            Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync(path)).StatusCode);
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/dashboard/summary")).StatusCode);
    }

    [Fact]
    public async Task ConsentHistory_ReturnsExactOwnedVersionAndWithdrawalState()
    {
        var versionId = await SeedConsent(factory.Credentials[RoleCodes.Customer], "WITHDRAWN");
        await SeedConsent(factory.OtherCustomerCredential, "CONSENTED");
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var items = await customer.GetFromJsonAsync<List<ConsentHistoryResponse>>("/api/v1/customers/me/consent-history");
        var item = Assert.Single(items!, x => x.LegalDocumentVersionId == versionId);
        Assert.Equal("WITHDRAWN", item.ConsentStatus); Assert.NotNull(item.WithdrawnAt);
        Assert.DoesNotContain(items!, x => x.Title.Contains("다른 고객", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WithdrawalReadiness_DoesNotRevokeRolesOrChangeAccountStatus()
    {
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var readiness = await customer.GetFromJsonAsync<WithdrawalReadinessResponse>("/api/v1/customers/me/withdrawal-readiness");
        Assert.NotNull(readiness);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Customer].LoginId);
        Assert.Equal("ACTIVE", user.StatusCode); Assert.True(await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RevokedAt == null));
    }

    [Fact]
    public async Task NotificationViews_SeparateUnreadArchivedAndDefaultLists()
    {
        var values = await SeedNotifications(); using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var all = await customer.GetFromJsonAsync<List<NotificationListItem>>("/api/v1/notifications?view=ALL");
        var unread = await customer.GetFromJsonAsync<List<NotificationListItem>>("/api/v1/notifications?view=UNREAD");
        var archived = await customer.GetFromJsonAsync<List<NotificationListItem>>("/api/v1/notifications?view=ARCHIVED");
        Assert.Contains(all!, x => x.Id == values.Unread); Assert.DoesNotContain(all!, x => x.Id == values.Archived);
        Assert.Contains(unread!, x => x.Id == values.Unread); Assert.Contains(archived!, x => x.Id == values.Archived);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.GetAsync("/api/v1/notifications?view=https://example.com")).StatusCode);
    }

    [Fact]
    public async Task OtherCustomerNotification_IsHiddenAsNotFound()
    {
        var values = await SeedNotifications(); using var other = Client(); await Login(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/notifications/{values.Unread}")).StatusCode);
    }

    [Fact]
    public async Task ExistingProfileAndAddressApisRemainAvailable()
    {
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/customer/account/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/customer/account/addresses")).StatusCode);
    }

    [Fact]
    public async Task CustomerDailyReads_DoNotChangeProtectedBusinessData()
    {
        var before = await ProtectedCounts(); using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await customer.GetAsync("/api/v1/customers/me/my-soodal/summary");
        await customer.GetAsync("/api/v1/customers/me/withdrawal-readiness");
        Assert.Equal(before, await ProtectedCounts());
    }

    private async Task<Guid> SeedConsent(TestCredential credential, string status)
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var now = DateTime.UtcNow;
        var user = await db.Users.SingleAsync(x => x.LoginId == credential.LoginId); var code = $"TEST_{Guid.NewGuid():N}";
        var document = new LegalDocument { Code = code, AudienceCode = "CUSTOMER", RequirementCode = "OPTIONAL", DisplayOrder = 900, IsActive = true, IsPlaceholder = true };
        db.LegalDocuments.Add(document); await db.SaveChangesAsync();
        var version = new LegalDocumentVersion { LegalDocumentId = document.Id, VersionNo = 1, Title = credential == factory.OtherCustomerCredential ? "다른 고객 동의" : "내 동의 이력", Content = "개발용", EffectiveFrom = now.AddDays(-1), IsActive = true, IsPlaceholder = true };
        db.LegalDocumentVersions.Add(version); await db.SaveChangesAsync();
        db.UserConsents.Add(new UserConsent { UserId = user.Id, LegalDocumentVersionId = version.Id, ConsentStatusCode = status, ConsentedAt = now.AddHours(-1), WithdrawnAt = status == "WITHDRAWN" ? now : null, SourceCode = "MY_SOODAL", CreatedAt = now });
        await db.SaveChangesAsync(); return version.PublicId;
    }

    private async Task<(Guid Unread, Guid Archived)> SeedNotifications()
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Customer].LoginId); var now = DateTime.UtcNow;
        var unread = new Notification { RecipientUserId = user.Id, TypeCode = "QUOTE_SUBMITTED", TargetTypeCode = "ServiceRequest", TargetPublicId = Guid.NewGuid(), StatusCode = "RECORDED", Title = "미읽음", Body = "업무 알림", RecordedAt = now, IdempotencyKey = $"daily:{Guid.NewGuid():N}" };
        var archived = new Notification { RecipientUserId = user.Id, TypeCode = "SYSTEM", StatusCode = "RECORDED", Title = "보관", Body = "보관 알림", RecordedAt = now.AddMinutes(-1), IdempotencyKey = $"daily:{Guid.NewGuid():N}" };
        db.Notifications.AddRange(unread, archived); await db.SaveChangesAsync();
        db.NotificationRecipients.AddRange(new NotificationRecipient { NotificationId = unread.Id, UserId = user.Id, RecipientRoleCode = RoleCodes.Customer, CreatedAt = now }, new NotificationRecipient { NotificationId = archived.Id, UserId = user.Id, RecipientRoleCode = RoleCodes.Customer, ReadAt = now, ArchivedAt = now, CreatedAt = now });
        await db.SaveChangesAsync(); return (unread.PublicId, archived.PublicId);
    }

    private async Task<int[]> ProtectedCounts()
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        return [await db.WalletLedgerEntries.CountAsync(), await db.FeeCharges.CountAsync(), await db.TrustScoreEvents.CountAsync(), await db.SubscriptionContracts.CountAsync(), await db.InteriorProjects.CountAsync()];
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static async Task Login(HttpClient client, TestCredential credential) => Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);
}
