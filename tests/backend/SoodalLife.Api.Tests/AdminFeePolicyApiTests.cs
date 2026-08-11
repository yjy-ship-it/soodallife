using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminFeePolicyApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private const string ChargeTiming = "수요자 견적 채택 시";
    private const string RestoreRule = "허위요청·시스템오류·본사 승인 사유 시 원장 복원";

    [Fact]
    public async Task Admin_CanReadServicePoliciesCurrentDetailAndEffectivePolicy()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var list = await client.GetFromJsonAsync<AdminFeePolicyListResponse>(BasePath(factory.Catalog.ServiceId));
        Assert.NotNull(list);
        var current = Assert.Single(list.Policies, policy => policy.IsCurrentlyEffective);
        Assert.Equal(3000m, current.FeeAmount);
        Assert.Equal("QUOTE", current.PolicyKindCode);
        Assert.Equal("ONE_TIME", current.TransactionTypeCode);
        Assert.Null(current.CalculationMethod);
        Assert.Equal(ChargeTiming, current.ChargeTiming);
        Assert.Equal(RestoreRule, current.RestoreRule);
        Assert.Contains("QUOTE", list.AllowedPolicyKinds);
        Assert.Contains("ONE_TIME", list.AllowedTransactionTypes);
        Assert.Empty(list.AllowedCalculationMethods);

        var detail = await client.GetFromJsonAsync<AdminFeePolicyResponse>($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}");
        Assert.Equal(current.Id, detail!.Id);
        var effective = await client.GetFromJsonAsync<EffectiveFeePolicyResult>($"{BasePath(factory.Catalog.ServiceId)}/effective?transactionType=ONE_TIME");
        Assert.Equal(current.Id, effective!.CategoryFeePolicyId);
        Assert.Equal(current.PolicyVersion, effective.PolicyVersion);
        Assert.Equal(ChargeTiming, effective.ChargeTiming);
        Assert.Equal(RestoreRule, effective.RestoreRule);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessFeePolicyAdminApi(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath(factory.Catalog.ServiceId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{BasePath(factory.Catalog.ServiceId)}/effective?transactionType=ONE_TIME")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateAndUpdateFutureVersion_WithoutChangingOtherService_AndAuditIsWritten()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var version = $"관리-{Guid.NewGuid():N}"[..17];
        var otherBefore = await PolicyStateAsync(factory.Catalog.OtherServiceId);
        AdminFeePolicyResponse? created = null;

        try
        {
            var createResponse = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input(version, tomorrow, 4000m));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            created = await createResponse.Content.ReadFromJsonAsync<AdminFeePolicyResponse>();
            Assert.NotNull(created);
            Assert.Equal("SCHEDULED", created.EffectiveStatus);
            Assert.Equal(4000m, created.FeeAmount);
            Assert.Equal(ChargeTiming, created.ChargeTiming);
            Assert.Equal(RestoreRule, created.RestoreRule);

            var updateResponse = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{created.Id}", Input(version, tomorrow.AddDays(1), 4500m));
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminFeePolicyResponse>();
            Assert.Equal(4500m, updated!.FeeAmount);
            Assert.Equal(tomorrow.AddDays(1), updated.EffectiveFrom);
            Assert.Equal(otherBefore, await PolicyStateAsync(factory.Catalog.OtherServiceId));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(log => log.EntityPublicId == created.Id && log.ActionCode == "FEE_POLICY_CREATED"));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.EntityPublicId == created.Id && log.ActionCode == "FEE_POLICY_UPDATED"));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "FEE_POLICY_PERIOD_UPDATED"));
        }
        finally
        {
            if (created is not null) await RemoveCreatedVersionAsync(created.Id);
        }
    }

    [Fact]
    public async Task OverlappingFuturePolicy_IsRejected_AndCurrentPolicyCannotBeOverwritten()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var currentList = await client.GetFromJsonAsync<AdminFeePolicyListResponse>(BasePath(factory.Catalog.ServiceId));
        var current = currentList!.Policies.Single(policy => policy.IsCurrentlyEffective);
        var overwrite = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}", Input(current.PolicyVersion, current.EffectiveFrom, current.FeeAmount));
        Assert.Equal(HttpStatusCode.Conflict, overwrite.StatusCode);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var firstVersion = $"기간-{Guid.NewGuid():N}"[..17];
        AdminFeePolicyResponse? first = null;
        try
        {
            var firstResponse = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input(firstVersion, tomorrow, 4000m));
            first = await firstResponse.Content.ReadFromJsonAsync<AdminFeePolicyResponse>();
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondVersion = $"중복-{Guid.NewGuid():N}"[..17];
            var overlap = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input(secondVersion, tomorrow, 5000m));
            Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        }
        finally
        {
            if (first is not null) await RemoveCreatedVersionAsync(first.Id);
        }
    }

    [Fact]
    public async Task Validation_RejectsUnknownTypeInvalidAmountAndInvalidPeriod()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var unknownType = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("검증-유형", tomorrow, 4000m, policyKind: "UNKNOWN"));
        Assert.Equal(HttpStatusCode.BadRequest, unknownType.StatusCode);
        var invalidAmount = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("검증-금액", tomorrow, -1m));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);
        var invalidPeriod = await client.PostAsJsonAsync(BasePath(factory.Catalog.ServiceId), Input("검증-기간", tomorrow, 4000m, effectiveTo: tomorrow));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPeriod.StatusCode);
    }

    [Fact]
    public async Task ServiceExplorer_CanFilterByCurrentFeeStatusAndEffectivePeriod()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var response = await client.GetFromJsonAsync<AdminServiceCategoryListResponse>(
            "/api/v1/admin/service-categories/services?feeAmount=3000&feeStatus=CURRENT&feeEffectiveFrom=2026-01-01&feeEffectiveTo=2026-12-31");
        Assert.NotNull(response);
        Assert.Contains(response.Items, service => service.Id == factory.Catalog.ServiceId);
        Assert.DoesNotContain(response.Items, service => service.Id == factory.Catalog.OtherServiceId);
    }

    private async Task RemoveCreatedVersionAsync(Guid publicId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var created = await db.CategoryFeePolicies.SingleAsync(policy => policy.PublicId == publicId);
        var previous = await db.CategoryFeePolicies.Where(policy => policy.CategoryId == created.CategoryId && policy.Id != created.Id)
            .OrderByDescending(policy => policy.EffectiveFrom).FirstAsync();
        previous.EffectiveTo = null;
        db.CategoryFeePolicies.Remove(created);
        await db.SaveChangesAsync();
    }

    private async Task<string[]> PolicyStateAsync(Guid servicePublicId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var serviceId = await db.ServiceCategories.Where(category => category.PublicId == servicePublicId).Select(category => category.Id).SingleAsync();
        return await db.CategoryFeePolicies.AsNoTracking().Where(policy => policy.CategoryId == serviceId).OrderBy(policy => policy.Id)
            .Select(policy => $"{policy.Id}|{policy.PolicyVersion}|{policy.FeeAmount}|{policy.ChargeTimingText}|{policy.RestoreRuleText}|{policy.EffectiveFrom}|{policy.EffectiveTo}")
            .ToArrayAsync();
    }

    private static object Input(
        string version,
        DateOnly effectiveFrom,
        decimal? feeAmount,
        DateOnly? effectiveTo = null,
        string policyKind = "QUOTE") => new
        {
            PolicyVersion = version,
            PolicyKindCode = policyKind,
            TransactionTypeCode = "ONE_TIME",
            CalculationMethod = (string?)null,
            FeeAmount = feeAmount,
            MinBaseAmount = (decimal?)null,
            MaxBaseAmount = (decimal?)null,
            Rate = (decimal?)null,
            MonthlyAmount = (decimal?)null,
            PerVisitAmount = (decimal?)null,
            CurrencyCode = "KRW",
            ChargeTiming,
            RestoreRule,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true,
        };

    private static string BasePath(Guid serviceId) => $"/api/v1/admin/service-categories/services/{serviceId}/fee-policies";
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
