using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;

namespace SoodalLife.Api.Tests;

public sealed class AdminMatchingOperationsApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task Dashboard_NonAdminRole_IsForbidden(string role)
    {
        using var client = Client();
        await Login(client, role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/matching")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_Admin_CanReadOperationalMatchingData()
    {
        using var client = Client();
        await Login(client, RoleCodes.Admin);

        var response = await client.GetAsync("/api/v1/admin/matching");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<AdminMatchingDashboard>();
        Assert.NotNull(dashboard);
        Assert.True(dashboard.Summary.OpenRequestCount >= 0);
        Assert.NotNull(dashboard.Regions);
        Assert.NotNull(dashboard.Requests);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private Task<HttpResponseMessage> Login(HttpClient client, string role) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials[role].LoginId, factory.Credentials[role].Password });
}
