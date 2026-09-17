using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.LivingContent;

namespace SoodalLife.Api.Tests;

public sealed class LivingContentApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_CanRead_AutomaticLivingContent_WithoutPrivateTradeFields()
    {
        using var client=factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
        var response=await client.GetAsync("/api/v1/public/living-home");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Server-Timing",out var timings));Assert.Contains(timings,value=>value.StartsWith("app;dur=",StringComparison.Ordinal));
        var payload=await response.Content.ReadFromJsonAsync<LivingHomeResponse>();
        Assert.NotNull(payload);Assert.Equal(3,payload.DailyChecks.Count);Assert.NotEmpty(payload.MaintenanceCalendar);Assert.False(payload.IsPersonalized);Assert.Contains("상세주소",payload.PrivacyNotice);
        var json=(await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        Assert.DoesNotContain("detailaddress",json);Assert.DoesNotContain("customerprofileid",json);Assert.DoesNotContain("providerprofileid",json);Assert.DoesNotContain("agreedamount",json);Assert.DoesNotContain("roadaddress",json);
    }
}
