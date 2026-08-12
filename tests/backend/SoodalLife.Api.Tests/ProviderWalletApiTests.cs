using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderWalletApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Provider_CanReadOnlyOwnWalletAndLedgerSummary()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var response = await client.GetAsync(Path); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProviderWalletDashboardResponse>(); Assert.NotNull(result);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var owned = await (from user in db.Users join provider in db.ProviderProfiles on user.Id equals provider.UserId
                           join wallet in db.ProviderWallets on provider.Id equals wallet.ProviderProfileId
                           where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId
                           select new { provider.PublicId, WalletId = wallet.PublicId }).SingleAsync();
        Assert.Equal(owned.PublicId, result.ProviderId); Assert.Equal(owned.WalletId, result.WalletId);
        Assert.True(result.AvailableBalance >= 0); Assert.True(result.TotalUsed >= 0); Assert.True(result.TotalRefunded >= 0);
    }

    [Fact]
    public async Task OtherProvider_ReceivesDifferentOwnedWallet()
    {
        using var first = Client(); using var second = Client();
        await Login(first, factory.Credentials[RoleCodes.Provider]); await Login(second, factory.AreaMismatchProviderCredential);
        var firstWallet = await first.GetFromJsonAsync<ProviderWalletDashboardResponse>(Path);
        var secondWallet = await second.GetFromJsonAsync<ProviderWalletDashboardResponse>(Path);
        Assert.NotEqual(firstWallet!.WalletId, secondWallet!.WalletId); Assert.NotEqual(firstWallet.ProviderId, secondWallet.ProviderId);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Admin)]
    public async Task NonProvider_CannotReadProviderWallet(string role)
    {
        using var client = Client(); await Login(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Path)).StatusCode);
    }

    [Fact]
    public async Task ProviderWallet_DoesNotExposeInternalKeysOrSensitiveData()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var json = await client.GetStringAsync(Path);
        Assert.DoesNotContain("providerProfileId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("walletId\":1", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connectionString", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accountNumber", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProviderWallet_StatesNoRealPaymentAndDoesNotOpenGeneralRefundRequest()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var result = await client.GetFromJsonAsync<ProviderWalletDashboardResponse>(Path);
        Assert.False(result!.ActualPaymentIntegrated); Assert.False(result.ProviderRefundRequestSupported);
        Assert.Contains("PG", result.ChargeGuidance); Assert.Contains("탈퇴", result.RefundGuidance);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Path + "/refunds", new { amount = 1000 })).StatusCode);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private const string Path = "/api/v1/providers/me/wallet";
}
