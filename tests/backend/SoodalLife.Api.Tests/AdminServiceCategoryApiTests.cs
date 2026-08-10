using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminServiceCategoryApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanBrowseSummaryHierarchySearchFilterAndDetail()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var summary = await client.GetFromJsonAsync<AdminCategorySummaryResponse>("/api/v1/admin/service-categories/summary");
        Assert.NotNull(summary);
        Assert.Equal(1, summary.MajorCount);
        Assert.Equal(1, summary.MiddleCount);
        Assert.Equal(2, summary.ServiceCount);
        Assert.Equal(2, summary.ActiveServiceCount);

        var majors = await client.GetFromJsonAsync<List<AdminCategoryOptionResponse>>("/api/v1/admin/service-categories/majors");
        var major = Assert.Single(majors!);
        Assert.Equal("테스트 대분류", major.Name);

        var middles = await client.GetFromJsonAsync<List<AdminCategoryOptionResponse>>($"/api/v1/admin/service-categories/majors/{major.Id}/middles");
        var middle = Assert.Single(middles!);
        Assert.Equal("테스트 중분류", middle.Name);

        var hierarchyResult = await client.GetFromJsonAsync<AdminServiceCategoryListResponse>($"/api/v1/admin/service-categories/services?majorId={major.Id}&middleId={middle.Id}");
        Assert.NotNull(hierarchyResult);
        Assert.Equal(2, hierarchyResult.TotalCount);

        var searchResult = await client.GetFromJsonAsync<AdminServiceCategoryListResponse>("/api/v1/admin/service-categories/services?search=TEST-001");
        var searchedService = Assert.Single(searchResult!.Items);
        Assert.Equal("테스트 서비스", searchedService.Name);

        var statusResult = await client.GetFromJsonAsync<AdminServiceCategoryListResponse>("/api/v1/admin/service-categories/services?status=ACTIVE");
        Assert.Equal(2, statusResult!.TotalCount);

        var detail = await client.GetFromJsonAsync<AdminServiceCategoryDetailResponse>($"/api/v1/admin/service-categories/services/{factory.Catalog.ServiceId}");
        Assert.NotNull(detail);
        Assert.Equal("테스트 대분류", detail.MajorName);
        Assert.Equal("테스트 중분류", detail.MiddleName);
        Assert.Equal("TEST-001", detail.ExternalCode);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessAdminServiceCategoryApi(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);

        var response = await client.GetAsync("/api/v1/admin/service-categories/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ACCESS_DENIED", error.BusinessCode);
    }

    [Fact]
    public async Task Admin_CanUpdateAllowedFieldsAndPauseService_WithoutChangingImportIdentity()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var serviceId = factory.Catalog.ServiceId;

        try
        {
            var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/service-categories/services/{serviceId}", new
            {
                Name = "테스트 서비스 운영 수정",
                StatusCode = "PAUSED",
                SortOrder = 9,
            });

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminServiceCategoryDetailResponse>();
            Assert.NotNull(updated);
            Assert.Equal("테스트 서비스 운영 수정", updated.Name);
            Assert.Equal("PAUSED", updated.StatusCode);
            Assert.Equal(9, updated.SortOrder);
            Assert.Equal("TEST-001", updated.ExternalCode);

            var pausedResult = await client.GetFromJsonAsync<AdminServiceCategoryListResponse>("/api/v1/admin/service-categories/services?status=PAUSED");
            Assert.Contains(pausedResult!.Items, item => item.Id == serviceId);

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var stored = await dbContext.ServiceCategories.SingleAsync(category => category.PublicId == serviceId);
            Assert.Equal("TEST-001", stored.SourceRecordId);
            Assert.NotNull(stored.ParentId);
            Assert.Contains(await dbContext.AuditLogs.Select(log => log.ActionCode).ToListAsync(), action => action == "SERVICE_CATEGORY_UPDATED");
        }
        finally
        {
            await client.PutAsJsonAsync($"/api/v1/admin/service-categories/services/{serviceId}", new
            {
                Name = "테스트 서비스",
                StatusCode = "ACTIVE",
                SortOrder = 1,
            });
        }
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
