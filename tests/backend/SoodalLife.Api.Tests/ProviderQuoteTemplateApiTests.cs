using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Quotes;

namespace SoodalLife.Api.Tests;

public sealed class ProviderQuoteTemplateApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Provider_CanSaveLoadUpdateAndDeleteOwnTemplate()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var credential = factory.Credentials[RoleCodes.Provider];
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);
        var name = $"기본 전기견적 {Guid.NewGuid():N}";
        var input = new SaveQuoteTemplateInput(name, "조명 교체 기본 견적", "철거와 폐기 포함", "2 시간", "EXCLUDED",
            [new QuoteItemInput("LED 조명", "제품과 설치", 2, "개", 30_000)]);

        var savedResponse = await client.PostAsJsonAsync("/api/v1/providers/me/quote-templates", input);
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        var saved = (await savedResponse.Content.ReadFromJsonAsync<ProviderQuoteTemplateResponse>())!;
        Assert.Equal(name, saved.Name);
        Assert.Single(saved.Items);

        var updatedInput = input with { Summary = "수정된 기본 견적", VatMode = "INCLUDED" };
        var updated = (await (await client.PostAsJsonAsync("/api/v1/providers/me/quote-templates", updatedInput))
            .Content.ReadFromJsonAsync<ProviderQuoteTemplateResponse>())!;
        Assert.Equal(saved.Id, updated.Id);
        Assert.Equal("INCLUDED", updated.VatMode);

        var templates = (await client.GetFromJsonAsync<List<ProviderQuoteTemplateResponse>>("/api/v1/providers/me/quote-templates"))!;
        Assert.Contains(templates, item => item.Id == saved.Id && item.Summary == "수정된 기본 견적");
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/providers/me/quote-templates/{saved.Id}")).StatusCode);
        templates = (await client.GetFromJsonAsync<List<ProviderQuoteTemplateResponse>>("/api/v1/providers/me/quote-templates"))!;
        Assert.DoesNotContain(templates, item => item.Id == saved.Id);
    }
}
