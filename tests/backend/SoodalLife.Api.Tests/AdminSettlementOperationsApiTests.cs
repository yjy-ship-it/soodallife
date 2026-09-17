using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Tests;

public sealed class AdminSettlementOperationsApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task Dashboard_NonAdminRole_IsForbidden(string role)
    {
        using var client = Client();
        await Login(client, role);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/v1/admin/settlement-operations/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_Admin_CanReadEverySettlementDomain()
    {
        using var client = Client();
        await Login(client, RoleCodes.Admin);
        var response = await client.GetAsync("/api/v1/admin/settlement-operations/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<AdminSettlementOperationsDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.True(dashboard.Summary.WalletAvailableBalance >= 0);
        Assert.NotNull(dashboard.QuoteFees);
        Assert.NotNull(dashboard.SubscriptionPayments);
        Assert.NotNull(dashboard.MonthlySettlements);
        Assert.NotNull(dashboard.Payouts);
        Assert.NotNull(dashboard.AdvertisingFees);
        Assert.NotNull(dashboard.RefundAdjustments);
    }

    [Fact]
    public async Task Ledger_Admin_CanFilterAndPageAppendOnlySources()
    {
        using var client = Client();
        await Login(client, RoleCodes.Admin);
        var response = await client.GetAsync("/api/v1/admin/settlement-operations/ledger?source=WALLET&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ledger = await response.Content.ReadFromJsonAsync<AdminUnifiedLedgerResponse>();
        Assert.NotNull(ledger);
        Assert.Equal(1, ledger.Page);
        Assert.Equal(10, ledger.PageSize);
        Assert.All(ledger.Items, item => Assert.Equal("WALLET", item.SourceCode));
        var allResponse = await client.GetAsync("/api/v1/admin/settlement-operations/ledger?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
        Assert.NotNull(await allResponse.Content.ReadFromJsonAsync<AdminUnifiedLedgerResponse>());
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private Task<HttpResponseMessage> Login(HttpClient client, string role) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials[role].LoginId, factory.Credentials[role].Password });
}
