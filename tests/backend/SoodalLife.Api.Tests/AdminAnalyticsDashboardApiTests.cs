using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminAnalyticsDashboardApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task ManagementDashboard_NonAdminRole_IsForbidden(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);

        var response = await client.GetAsync("/api/v1/admin/dashboard/management");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ManagementDashboard_AppliesPeriodCategoryAndAreaFilters_WithoutMutatingBusinessData()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        await SeedRequestsAsync();
        var before = await ReadInvariantSnapshotAsync();

        var response = await client.GetAsync(
            $"/api/v1/admin/dashboard/management?range=CUSTOM&from=2026-07-01&to=2026-07-01&categoryId={factory.Catalog.ServiceId}&areaId={factory.Catalog.AreaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<AdminAnalyticsDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(new DateOnly(2026, 7, 1), dashboard.AppliedFilter.From);
        Assert.Equal(new DateOnly(2026, 7, 1), dashboard.AppliedFilter.To);
        Assert.Equal(2m, Metric(dashboard, "requests_total").Value);
        Assert.Equal(1m, Metric(dashboard, "request_open").Value);
        Assert.Equal(1m, Metric(dashboard, "request_cancelled").Value);
        Assert.Single(dashboard.RequestsByCategory);
        Assert.Single(dashboard.RequestsByRegion);
        Assert.Null(Metric(dashboard, "overall_rating").Value);
        Assert.Null(Metric(dashboard, "dispute_liability_warning").Value);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("detailAddress", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("loginId", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"email\":", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"phone\":", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, await ReadInvariantSnapshotAsync());
    }

    [Fact]
    public async Task ManagementDashboard_EmptyPeriod_ReturnsZerosAndExplicitUnavailableMetrics()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var response = await client.GetAsync("/api/v1/admin/dashboard/management?range=CUSTOM&from=2020-01-01&to=2020-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<AdminAnalyticsDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(0m, Metric(dashboard, "requests_total").Value);
        Assert.Empty(dashboard.RequestsByCategory);
        Assert.Empty(dashboard.RequestsByRegion);
        Assert.NotEmpty(dashboard.UnavailableMetrics);
        Assert.Contains(dashboard.UnavailableMetrics, item => item.Contains("AI", StringComparison.Ordinal));
        Assert.Contains(dashboard.UnavailableMetrics, item => item.Contains("POLICY_PENDING", StringComparison.Ordinal));
    }

    private async Task SeedRequestsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        if (await db.ServiceRequests.AnyAsync(item => item.IdempotencyKey == "analytics-dashboard-filter-1")) return;

        var customerId = await db.CustomerProfiles.Select(item => item.Id).FirstAsync();
        var categoryId = await db.ServiceCategories.Where(item => item.PublicId == factory.Catalog.ServiceId).Select(item => item.Id).SingleAsync();
        var otherCategoryId = await db.ServiceCategories.Where(item => item.PublicId == factory.Catalog.OtherServiceId).Select(item => item.Id).SingleAsync();
        var policyId = await db.CategoryPolicies.Where(item => item.CategoryId == categoryId).Select(item => item.Id).SingleAsync();
        var otherPolicyId = await db.CategoryPolicies.Where(item => item.CategoryId == otherCategoryId).Select(item => item.Id).SingleAsync();
        var areaId = await db.AdministrativeAreas.Where(item => item.PublicId == factory.Catalog.AreaId).Select(item => item.Id).SingleAsync();
        var otherAreaId = await db.AdministrativeAreas.Where(item => item.PublicId == factory.Catalog.OtherAreaId).Select(item => item.Id).SingleAsync();
        var at = new DateTime(2026, 6, 30, 16, 0, 0, DateTimeKind.Utc); // 2026-07-01 KST

        db.ServiceRequests.AddRange(
            Request("analytics-dashboard-filter-1", "OPEN", categoryId, policyId, areaId, customerId, at),
            Request("analytics-dashboard-filter-2", "CANCELLED", categoryId, policyId, areaId, customerId, at.AddHours(2)),
            Request("analytics-dashboard-other-filter", "OPEN", otherCategoryId, otherPolicyId, otherAreaId, customerId, at),
            Request("analytics-dashboard-previous", "OPEN", categoryId, policyId, areaId, customerId, at.AddDays(-1)));
        await db.SaveChangesAsync();
    }

    private static ServiceRequest Request(string key, string status, long categoryId, long policyId, long areaId, long customerId, DateTime createdAt) => new()
    {
        CustomerProfileId = customerId,
        CategoryId = categoryId,
        CategoryPolicyId = policyId,
        AdministrativeAreaId = areaId,
        DetailAddress = "서울시 테스트 개인정보 주소",
        Title = "analytics aggregate fixture",
        StatusCode = status,
        PolicySnapshotJson = "{}",
        IdempotencyKey = key,
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
    };

    private async Task<(int Requests, int Transactions, int Ledger, int Fees, int TrustPolicies, decimal Wallet)> ReadInvariantSnapshotAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        return (
            await db.ServiceRequests.CountAsync(),
            await db.Transactions.CountAsync(),
            await db.WalletLedgerEntries.CountAsync(),
            await db.FeeCharges.CountAsync(),
            await db.TrustPolicies.CountAsync(),
            await db.ProviderWallets.SumAsync(item => item.AvailableBalance + item.ReservedBalance));
    }

    private static AdminAnalyticsMetricResponse Metric(AdminAnalyticsDashboardResponse dashboard, string code) =>
        dashboard.Kpis.Concat(dashboard.Sections.SelectMany(section => section.Metrics)).Single(item => item.Code == code);

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
