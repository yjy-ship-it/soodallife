using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Features.Wallet;

namespace SoodalLife.Api.Tests;

public sealed class ProviderOperationsHubApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Hub_ReturnsOwnedRealSummaries_WithoutPersonalOrInternalData()
    {
        using var provider = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]);

        var response = await provider.GetAsync("/api/v1/providers/me/operations-hub?pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hub = await response.Content.ReadFromJsonAsync<ProviderOperationsHubResponse>();
        Assert.NotNull(hub);
        Assert.Equal(7, hub.Summary.Count);
        Assert.InRange(hub.Inbox.PageSize, 1, 50);
        Assert.All(hub.Inbox.Items, item => Assert.StartsWith("/provider", item.Route));
        Assert.All(hub.Schedule, item => Assert.StartsWith("/provider", item.Route));
        Assert.All(hub.RecentChats, item => Assert.StartsWith("/provider/messages/", item.Route));

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("customerPhone", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detailAddress", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerProfileId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("walletLedger", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("feeCharge", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Hub_RejectsNonProviderRoles()
    {
        using var customer = Client(); using var admin = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/providers/me/operations-hub")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/providers/me/operations-hub")).StatusCode);
    }

    [Fact]
    public async Task Hub_CountsMatchExistingProviderSources()
    {
        using var provider = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var hub = await provider.GetFromJsonAsync<ProviderOperationsHubResponse>("/api/v1/providers/me/operations-hub?pageSize=50");
        var legacy = await provider.GetFromJsonAsync<ProviderOperationsDashboardResponse>("/api/v1/providers/me/operations-dashboard");
        var care = await provider.GetFromJsonAsync<ProviderCareDashboardResponse>("/api/v1/providers/me/care/home");
        var wallet = await provider.GetFromJsonAsync<ProviderWalletDashboardResponse>("/api/v1/providers/me/wallet");

        Assert.Equal(legacy!.NewMatchedRequestCount, Metric(hub!, "new-general"));
        Assert.Equal(legacy.UnreadNotificationCount, Metric(hub!, "notification"));
        Assert.Equal(care!.OpenRequestCount, Metric(hub!, "new-care"));
        Assert.Equal(care.TodayVisitCount, Metric(hub!, "today-care"));
        Assert.Equal(wallet!.AvailableBalance, hub!.Wallet.AvailableBalance);
        Assert.Equal(wallet.StatusCode, hub.Wallet.Status);
    }

    [Fact]
    public async Task Hub_FiltersAndPaginationAreBoundedAndRoutesRemainInternal()
    {
        using var provider = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var page = await provider.GetFromJsonAsync<ProviderOperationsHubResponse>(
            "/api/v1/providers/me/operations-hub?domain=GENERAL&group=ACTION_REQUIRED&page=1&pageSize=500");
        Assert.NotNull(page);
        Assert.Equal(50, page.Inbox.PageSize);
        Assert.All(page.Inbox.Items, item =>
        {
            Assert.Equal("GENERAL", item.Domain);
            Assert.Equal("ACTION_REQUIRED", item.PriorityGroup);
            Assert.DoesNotContain("://", item.Route);
            Assert.DoesNotContain("..", item.Route);
        });
    }

    [Fact]
    public async Task Hub_IsObjectScopedAcrossProviders()
    {
        using var first = Client(); using var second = Client();
        await Login(first, factory.Credentials[RoleCodes.Provider]);
        await Login(second, factory.AreaMismatchProviderCredential);
        var firstHub = await first.GetFromJsonAsync<ProviderOperationsHubResponse>("/api/v1/providers/me/operations-hub?pageSize=50");
        var secondHub = await second.GetFromJsonAsync<ProviderOperationsHubResponse>("/api/v1/providers/me/operations-hub?pageSize=50");
        Assert.NotNull(firstHub); Assert.NotNull(secondHub);
        var firstIds = firstHub.Inbox.Items.Select(x => x.PublicId).ToHashSet();
        Assert.DoesNotContain(secondHub.Inbox.Items, x => firstIds.Contains(x.PublicId));
    }

    private static int Metric(ProviderOperationsHubResponse hub, string key) =>
        hub.Summary.SelectMany(x => x.Items).Single(x => x.Key == key).Count;

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private static async Task Login(HttpClient client, TestCredential credential)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
        response.EnsureSuccessStatusCode();
    }
}
