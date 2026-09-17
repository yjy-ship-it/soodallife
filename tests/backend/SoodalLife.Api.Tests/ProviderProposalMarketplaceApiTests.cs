using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Proposals;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderProposalMarketplaceApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    private const string ProviderRoot = "/api/v1/providers/me/proposals";

    [Fact]
    public async Task InterestedService_IsStoredOnCustomerAccount_AndCanBeRemoved()
    {
        using var anonymous = Client();
        var unauthorized = await anonymous.PutAsJsonAsync($"/api/v1/customers/me/interested-services/{factory.Catalog.ServiceId}", new { Interested = true });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var customer = Client(); await Login(customer, RoleCodes.Customer);
        var saved = await customer.PutAsJsonAsync($"/api/v1/customers/me/interested-services/{factory.Catalog.ServiceId}", new { Interested = true });
        Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync());
        var state = await customer.GetFromJsonAsync<InterestedServiceStateResponse>($"/api/v1/customers/me/interested-services/{factory.Catalog.ServiceId}/state");
        Assert.True(state!.Interested);
        var items = await customer.GetFromJsonAsync<List<InterestedServiceResponse>>("/api/v1/customers/me/interested-services");
        Assert.Contains(items!, x => x.Id == factory.Catalog.ServiceId);

        var removed = await customer.PutAsJsonAsync($"/api/v1/customers/me/interested-services/{factory.Catalog.ServiceId}", new { Interested = false });
        Assert.True(removed.IsSuccessStatusCode, await removed.Content.ReadAsStringAsync());
        state = await customer.GetFromJsonAsync<InterestedServiceStateResponse>($"/api/v1/customers/me/interested-services/{factory.Catalog.ServiceId}/state");
        Assert.False(state!.Interested);
    }

    [Fact]
    public async Task LocalCampaign_ReservesCapacityFee_AndCapturesOnlyConfirmedParticipant()
    {
        long walletId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Provider].LoginId);
            var profile = await db.ProviderProfiles.SingleAsync(x => x.UserId == user.Id);
            var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == profile.Id);
            wallet.AvailableBalance = 100000; wallet.ReservedBalance = 0; walletId = wallet.Id;
            await db.SaveChangesAsync();
        }

        using var provider = Client(); await Login(provider, RoleCodes.Provider);
        var setup = await provider.GetFromJsonAsync<ProviderProposalSetupResponse>($"{ProviderRoot}/setup");
        Assert.NotNull(setup); var service = Assert.Single(setup!.Services, x => x.Id == factory.Catalog.ServiceId);
        var area = Assert.Single(service.Areas, x => x.Id == factory.Catalog.AreaId);
        var response = await provider.PostAsJsonAsync(ProviderRoot, Input(service.Id, [area.Id]));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var campaign = (await response.Content.ReadFromJsonAsync<ProposalCampaignResponse>())!;
        Assert.Equal(50000, campaign.ReservedFeeAmount);
        await Wallet(50000, 50000);

        using var customer = Client(); await Login(customer, RoleCodes.Customer);
        var applied = await customer.PostAsync($"/api/v1/customers/me/proposals/{campaign.Id}/applications", null);
        Assert.True(applied.IsSuccessStatusCode, await applied.Content.ReadAsStringAsync());
        var application = (await applied.Content.ReadFromJsonAsync<ProposalApplicationResponse>())!;
        var confirmed = await provider.PostAsync($"{ProviderRoot}/{campaign.Id}/applications/{application.Id}/confirm", null);
        Assert.True(confirmed.IsSuccessStatusCode, await confirmed.Content.ReadAsStringAsync());
        await Wallet(50000, 45000);

        var cancel = await provider.PostAsync($"{ProviderRoot}/{campaign.Id}/cancel", null);
        Assert.True(cancel.IsSuccessStatusCode, await cancel.Content.ReadAsStringAsync());
        await Wallet(100000, 0);

        async Task Wallet(decimal available, decimal reserved)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var wallet = await db.ProviderWallets.AsNoTracking().SingleAsync(x => x.Id == walletId);
            Assert.Equal(available, wallet.AvailableBalance); Assert.Equal(reserved, wallet.ReservedBalance);
        }
    }

    [Fact]
    public async Task Campaign_CanBeEditedBeforeFirstApplication_AndReservationIsAdjusted()
    {
        long walletId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Provider].LoginId);
            var profile = await db.ProviderProfiles.SingleAsync(x => x.UserId == user.Id);
            var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == profile.Id);
            wallet.AvailableBalance = 100000; wallet.ReservedBalance = 0; walletId = wallet.Id;
            await db.SaveChangesAsync();
        }

        using var provider = Client(); await Login(provider, RoleCodes.Provider);
        var setup = (await provider.GetFromJsonAsync<ProviderProposalSetupResponse>($"{ProviderRoot}/setup"))!;
        var service = Assert.Single(setup.Services, x => x.Id == factory.Catalog.ServiceId);
        var area = Assert.Single(service.Areas, x => x.Id == factory.Catalog.AreaId);
        var createdResponse = await provider.PostAsJsonAsync(ProviderRoot, Input(service.Id, [area.Id]));
        Assert.True(createdResponse.IsSuccessStatusCode, await createdResponse.Content.ReadAsStringAsync());
        var campaign = (await createdResponse.Content.ReadFromJsonAsync<ProposalCampaignResponse>())!;

        var edited = new
        {
            campaign.ProposalTypeCode, campaign.ScopeCode, campaign.ServiceCategoryId, AreaIds = campaign.Areas.Select(x => x.Id).ToArray(),
            Title = "신청 전 수정된 공동모집", Summary = "신청자가 생기기 전 조건을 안전하게 변경합니다.",
            NormalPriceAmount = 50000m, OfferPriceAmount = 32000m, MinimumParticipants = 2, MaximumParticipants = 4,
            campaign.StartAt, EndAt = campaign.StartAt.AddDays(10), ServiceAt = campaign.StartAt.AddDays(11),
            CancellationPolicyText = "모집 마감 전 신청 취소 가능", IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        var updateResponse = await provider.PutAsJsonAsync($"{ProviderRoot}/{campaign.Id}", edited);
        Assert.True(updateResponse.IsSuccessStatusCode, await updateResponse.Content.ReadAsStringAsync());
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ProposalCampaignResponse>())!;
        Assert.Equal("신청 전 수정된 공동모집", updated.Title);
        Assert.Equal(20000, updated.ReservedFeeAmount);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var wallet = await db.ProviderWallets.AsNoTracking().SingleAsync(x => x.Id == walletId);
            Assert.Equal(80000, wallet.AvailableBalance); Assert.Equal(20000, wallet.ReservedBalance);
        }

        using var customer = Client(); await Login(customer, RoleCodes.Customer);
        var applied = await customer.PostAsync($"/api/v1/customers/me/proposals/{campaign.Id}/applications", null);
        Assert.True(applied.IsSuccessStatusCode, await applied.Content.ReadAsStringAsync());
        var blocked = await provider.PutAsJsonAsync($"{ProviderRoot}/{campaign.Id}", edited with { Title = "신청 후 변경 시도" });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var cancel = await provider.PostAsync($"{ProviderRoot}/{campaign.Id}/cancel", null);
        Assert.True(cancel.IsSuccessStatusCode, await cancel.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NationwideCampaign_IsRejected_WhenApprovedServiceIsNotNationwide()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Provider].LoginId);
            var profile = await db.ProviderProfiles.SingleAsync(x => x.UserId == user.Id);
            var service = await db.ProviderServiceCategories.SingleAsync(x => x.ProviderProfileId == profile.Id && x.CategoryId == db.ServiceCategories.Where(c => c.PublicId == factory.Catalog.ServiceId).Select(c => c.Id).Single());
            service.IsNationwide = false; var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == profile.Id); wallet.AvailableBalance = 100000;
            await db.SaveChangesAsync();
        }
        using var provider = Client(); await Login(provider, RoleCodes.Provider);
        var response = await provider.PostAsJsonAsync(ProviderRoot, Input(factory.Catalog.ServiceId, [], "NATIONWIDE"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CanApplyAgain_AfterCancellingOwnApplication()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Provider].LoginId);
            var profile = await db.ProviderProfiles.SingleAsync(x => x.UserId == user.Id);
            var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == profile.Id);
            wallet.AvailableBalance = 100000; wallet.ReservedBalance = 0;
            await db.SaveChangesAsync();
        }
        using var provider = Client(); await Login(provider, RoleCodes.Provider);
        var setup = (await provider.GetFromJsonAsync<ProviderProposalSetupResponse>($"{ProviderRoot}/setup"))!;
        var service = Assert.Single(setup.Services, x => x.Id == factory.Catalog.ServiceId);
        var area = Assert.Single(service.Areas, x => x.Id == factory.Catalog.AreaId);
        var created = await provider.PostAsJsonAsync(ProviderRoot, Input(service.Id, [area.Id]));
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var campaign = (await created.Content.ReadFromJsonAsync<ProposalCampaignResponse>())!;

        using var customer = Client(); await Login(customer, RoleCodes.Customer);
        var first = await customer.PostAsync($"/api/v1/customers/me/proposals/{campaign.Id}/applications", null);
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());
        var cancelled = await customer.DeleteAsync($"/api/v1/customers/me/proposals/{campaign.Id}/applications/me");
        Assert.True(cancelled.IsSuccessStatusCode, await cancelled.Content.ReadAsStringAsync());
        var reapplied = await customer.PostAsync($"/api/v1/customers/me/proposals/{campaign.Id}/applications", null);
        Assert.True(reapplied.IsSuccessStatusCode, await reapplied.Content.ReadAsStringAsync());
        Assert.Equal("APPLIED", (await reapplied.Content.ReadFromJsonAsync<ProposalApplicationResponse>())!.StatusCode);

        var mine = (await customer.GetFromJsonAsync<IReadOnlyList<CustomerProposalParticipationResponse>>("/api/v1/customers/me/proposals/applications"))!;
        var participation = Assert.Single(mine, x => x.CampaignId == campaign.Id);
        Assert.Equal("APPLIED", participation.ApplicationStatusCode);
        Assert.True(participation.CanCancel);
        await provider.PostAsync($"{ProviderRoot}/{campaign.Id}/cancel", null);
    }

    private static object Input(Guid serviceId, Guid[] areas, string scope = "LOCAL") => new
    {
        ProposalTypeCode = "DISCOUNT_SERVICE", ScopeCode = scope, ServiceCategoryId = serviceId, AreaIds = areas,
        Title = "겨울철 사전 점검 공동모집", Summary = "같은 지역 고객을 모아 합리적인 가격으로 점검합니다.",
        NormalPriceAmount = 50000m, OfferPriceAmount = 35000m, MinimumParticipants = 2, MaximumParticipants = 10,
        StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(7), ServiceAt = DateTime.UtcNow.AddDays(8),
        CancellationPolicyText = "모집 마감 전 신청 취소 가능", IdempotencyKey = Guid.NewGuid().ToString("N")
    };
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private Task<HttpResponseMessage> Login(HttpClient client, string role) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials[role].LoginId, factory.Credentials[role].Password });
}
