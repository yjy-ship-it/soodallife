using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminPricePolicyApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanReadServicePoliciesCurrentAndDetail()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var list = await client.GetFromJsonAsync<AdminPricePolicyListResponse>(BasePath(factory.Catalog.ServiceId));
        Assert.NotNull(list);
        Assert.NotEmpty(list.Policies);
        Assert.Contains("견적형", list.AllowedPriceMethods);
        Assert.Contains("예약가", list.AllowedPriceMethods);
        Assert.True(list.SupportsRecommendedMaximumPrice);
        Assert.True(list.SupportsExplicitActiveStatus);
        Assert.True(list.SupportsPriceOptions);
        Assert.True(list.SupportsSurcharges);

        var current = await client.GetFromJsonAsync<AdminPricePolicyResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
        Assert.NotNull(current);
        Assert.True(current.IsCurrentlyEffective);
        Assert.Empty(current.Options);
        Assert.Empty(current.Surcharges);
        var detail = await client.GetFromJsonAsync<AdminPricePolicyResponse>($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}");
        Assert.Equal(current.Id, detail!.Id);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessPricePolicyAdminApi(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath(factory.Catalog.ServiceId))).StatusCode);
    }

    [Fact]
    public async Task PricePolicyValidation_RejectsInvalidAmountPeriodAndUnknownType()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var invalidAmount = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("관리-v2", tomorrow, basePrice: -1));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);
        var invalidPeriod = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("관리-v2", tomorrow, effectiveTo: tomorrow));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPeriod.StatusCode);
        var invalidMethod = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("관리-v2", tomorrow, method: "임의가격형"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidMethod.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateAndUpdateFutureVersion_WithoutChangingOtherService_AndAuditIsWritten()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var version = $"관리-{Guid.NewGuid():N}"[..17];
        var otherBefore = await client.GetFromJsonAsync<AdminPricePolicyListResponse>(BasePath(factory.Catalog.OtherServiceId));
        var legacyBefore = await LegacyPolicyStateAsync();
        AdminPricePolicyResponse? created = null;

        try
        {
            var createResponse = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input(version, tomorrow, method: "예약가", basePrice: 135000, minimumBudget: 80000));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            created = await createResponse.Content.ReadFromJsonAsync<AdminPricePolicyResponse>();
            Assert.NotNull(created);
            Assert.Equal("SCHEDULED", created.EffectiveStatus);
            Assert.Equal(135000m, created.BasePriceAmount);
            Assert.Equal(80000m, created.MinimumBudgetAmount);

            var updateResponse = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{created.Id}", Input(version, tomorrow, method: "예약가", basePrice: 145000, minimumBudget: 85000));
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminPricePolicyResponse>();
            Assert.Equal(145000m, updated!.BasePriceAmount);

            var otherAfter = await client.GetFromJsonAsync<AdminPricePolicyListResponse>(BasePath(factory.Catalog.OtherServiceId));
            Assert.Equal(
                otherBefore!.Policies.Select(policy => (policy.Id, policy.PolicyVersion, policy.BasePriceAmount, policy.MinimumBudgetAmount, policy.EffectiveFrom, policy.EffectiveTo)),
                otherAfter!.Policies.Select(policy => (policy.Id, policy.PolicyVersion, policy.BasePriceAmount, policy.MinimumBudgetAmount, policy.EffectiveFrom, policy.EffectiveTo)));
            Assert.Equal(legacyBefore, await LegacyPolicyStateAsync());
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(log => log.EntityPublicId == created.Id && log.ActionCode == "PRICE_POLICY_CREATED"));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.EntityPublicId == created.Id && log.ActionCode == "PRICE_POLICY_UPDATED"));
        }
        finally
        {
            if (created is not null) await RemoveCreatedVersionAsync(created.Id);
        }
    }

    [Fact]
    public async Task CurrentPolicy_IsProtectedFromInPlaceUpdate()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var current = await client.GetFromJsonAsync<AdminPricePolicyResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
        var response = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current!.Id}", Input(current.PolicyVersion, current.EffectiveFrom, current.EffectiveTo, current.PriceMethod, current.BasePriceAmount, current.MinimumBudgetAmount));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task RemoveCreatedVersionAsync(Guid publicId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var created = await db.CategoryPricePolicies.SingleAsync(policy => policy.PublicId == publicId);
        var previous = await db.CategoryPricePolicies.Where(policy => policy.CategoryId == created.CategoryId && policy.Id != created.Id)
            .OrderByDescending(policy => policy.EffectiveFrom).FirstAsync();
        previous.EffectiveTo = null;
        db.CategoryPricePolicies.Remove(created);
        await db.SaveChangesAsync();
    }

    private async Task<string[]> LegacyPolicyStateAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        return await db.CategoryPolicies.AsNoTracking().OrderBy(policy => policy.Id)
            .Select(policy => $"{policy.Id}|{policy.PolicyVersion}|{policy.BasePriceAmount}|{policy.MinimumBudgetAmount}|{policy.EffectiveFrom}|{policy.EffectiveTo}")
            .ToArrayAsync();
    }

    private static object Input(string version, DateOnly effectiveFrom, DateOnly? effectiveTo = null, string method = "예약가", decimal? basePrice = 120000, decimal? minimumBudget = 70000) => new
    {
        PolicyVersion = version, PriceMethod = method, BasePriceAmount = basePrice, MinimumBudgetAmount = minimumBudget,
        Unit = "1회", VatRule = "포함/별도 필수표시", EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo, IsActive = true,
    };

    private static string BasePath(Guid serviceId) => $"/api/v1/admin/service-categories/services/{serviceId}/price-policies";
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
