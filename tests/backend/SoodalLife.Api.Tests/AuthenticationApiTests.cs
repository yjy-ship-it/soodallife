using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AuthenticationApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    [InlineData(RoleCodes.Admin)]
    public async Task Login_WithValidCredentials_ReturnsCurrentUser(string role)
    {
        using var client = CreateClient();
        var credential = factory.Credentials[role];

        var loginResponse = await LoginAsync(client, credential);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginUser = await loginResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.NotNull(loginUser);
        Assert.Contains(role, loginUser.Roles);

        var meResponse = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var currentUser = await meResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.NotNull(currentUser);
        Assert.Equal(loginUser.PublicId, currentUser.PublicId);
        Assert.Contains(role, currentUser.Roles);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsStandardUnauthorizedError()
    {
        using var client = CreateClient();
        var credential = factory.Credentials[RoleCodes.Customer];

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            LoginOrEmail = credential.LoginId,
            Password = $"invalid-{Guid.NewGuid():N}",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", error.BusinessCode);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
    }

    [Theory]
    [InlineData(RoleCodes.Customer, "customer")]
    [InlineData(RoleCodes.Provider, "provider")]
    [InlineData(RoleCodes.Admin, "admin")]
    public async Task RoleProtectedApis_AllowOwnRole_AndDenyOtherRoles(string role, string allowedPath)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);

        var allowedResponse = await client.GetAsync($"/api/v1/access/{allowedPath}");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

        foreach (var deniedPath in new[] { "customer", "provider", "admin" }.Where(path => path != allowedPath))
        {
            var deniedResponse = await client.GetAsync($"/api/v1/access/{deniedPath}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
            var error = await deniedResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal("ACCESS_DENIED", error.BusinessCode);
        }
    }

    [Fact]
    public async Task ProtectedApi_WithoutLogin_ReturnsStandardUnauthorizedError()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/access/customer");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("AUTHENTICATION_REQUIRED", error.BusinessCode);
    }

    [Fact]
    public async Task Logout_InvalidatesAuthenticationCookie()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_AdminCanAccess_AndReceivesOnlyConfirmedMetrics()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<AdminDashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalCustomers.Value);
        Assert.Equal(4, summary.TotalProviders.Value);
        Assert.Equal(1, summary.PendingProviders.Value);
        Assert.Null(summary.ActiveRequests.Value);
        Assert.Equal("집계 기준 미확정", summary.ActiveRequests.UnavailableReason);
        Assert.Null(summary.ActiveTransactions.Value);
        Assert.Null(summary.UnresolvedAfterServiceCases.Value);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task AdminDashboard_NonAdminRole_IsForbidden(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);

        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ACCESS_DENIED", error.BusinessCode);
    }

    [Fact]
    public async Task AdminDashboard_WithoutLogin_IsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminLoginAndLogout_AreWrittenToAuditLog()
    {
        using var client = CreateClient();
        var credential = factory.Credentials[RoleCodes.Admin];

        await LoginAsync(client, credential);
        await client.PostAsync("/api/v1/auth/logout", null);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var actionCodes = await dbContext.AuditLogs
            .Where(log => log.ActorRoleCode == RoleCodes.Admin)
            .Select(log => log.ActionCode)
            .ToListAsync();
        Assert.Contains("ADMIN_LOGIN", actionCodes);
        Assert.Contains("ADMIN_LOGOUT", actionCodes);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            LoginOrEmail = credential.LoginId,
            credential.Password,
        });
}
