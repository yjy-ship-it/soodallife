using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Catalog;

namespace SoodalLife.Api.Tests;

public sealed class PublicCatalogApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_CanBrowseHierarchy_AndReadServiceDetail()
    {
        using var client = Client();
        var majors = await client.GetFromJsonAsync<List<PublicCategoryResponse>>("/api/v1/public/catalog/categories/majors");
        var major = Assert.Single(majors!, x => x.Id == factory.Catalog.MajorId);
        Assert.True(major.ChildCount > 0);

        var middles = await client.GetFromJsonAsync<List<PublicCategoryResponse>>($"/api/v1/public/catalog/categories/{major.Id}/children");
        var middle = Assert.Single(middles!);
        var services = await client.GetFromJsonAsync<List<PublicCategoryResponse>>($"/api/v1/public/catalog/categories/{middle.Id}/children");
        Assert.Contains(services!, x => x.Id == factory.Catalog.ServiceId);

        var detail = await client.GetFromJsonAsync<PublicServiceDetailResponse>($"/api/v1/public/catalog/services/{factory.Catalog.ServiceId}");
        Assert.NotNull(detail);
        Assert.Equal("TEST-001", detail.Code);
        Assert.Equal("테스트 대분류", detail.MajorName);
        Assert.NotNull(detail.Price);
        Assert.Contains(detail.RequestFields, x => x.Required);
    }

    [Fact]
    public async Task Anonymous_SearchesServiceMajorAndMiddleNames()
    {
        using var client = Client();
        foreach (var query in new[] { "테스트 서비스", "테스트 중분류", "테스트 대분류" })
        {
            var result = await client.GetFromJsonAsync<List<PublicServiceSummaryResponse>>(
                $"/api/v1/public/catalog/services/search?q={Uri.EscapeDataString(query)}");
            Assert.Contains(result!, x => x.Id == factory.Catalog.ServiceId);
        }
    }

    [Fact]
    public async Task PublicPayload_DoesNotExposeInternalOperationsFeesOrConcurrencyData()
    {
        using var client = Client();
        var response = await client.GetAsync($"/api/v1/public/catalog/services/{factory.Catalog.ServiceId}");
        response.EnsureSuccessStatusCode();
        var json = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();

        Assert.DoesNotContain("adminnote", json);
        Assert.DoesNotContain("feeamount", json);
        Assert.DoesNotContain("rowversion", json);
        Assert.DoesNotContain("internal", json);
        Assert.DoesNotContain("minimumbudget", json);
    }

    [Fact]
    public async Task PublicCatalog_DoesNotChangeExistingRoleProtectedEndpoints()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/categories/majors")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/service-categories/summary")).StatusCode);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });
}
