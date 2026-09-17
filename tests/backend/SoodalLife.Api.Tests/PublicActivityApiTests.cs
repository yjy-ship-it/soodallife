using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.PublicActivity;

namespace SoodalLife.Api.Tests;

public sealed class PublicActivityApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_CanRead_PrivacyProtected_RecentActivity()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/api/v1/public/activity?take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Server-Timing", out var timings));
        Assert.Contains(timings, value => value.StartsWith("app;dur=", StringComparison.Ordinal));
        var payload = await response.Content.ReadFromJsonAsync<PublicActivityFeedResponse>();
        Assert.NotNull(payload);
        Assert.InRange(payload.Items.Count, 0, 20);
        Assert.Equal(60, payload.NextRefreshSeconds);
        Assert.Contains("개인 식별정보", payload.PrivacyNotice);

        var json = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        Assert.DoesNotContain("detailaddress", json);
        Assert.DoesNotContain("customerprofileid", json);
        Assert.DoesNotContain("providerprofileid", json);
        Assert.DoesNotContain("agreedamount", json);
        Assert.DoesNotContain("bodytext", json);

        var cachedResponse = await client.GetAsync("/api/v1/public/activity?take=20");
        var cachedPayload = await cachedResponse.Content.ReadFromJsonAsync<PublicActivityFeedResponse>();
        Assert.NotNull(cachedPayload);
        Assert.Equal(payload.ServerTime, cachedPayload.ServerTime);
    }
}
