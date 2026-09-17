using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.ServiceRequests;

namespace SoodalLife.Api.Tests;

public sealed class CustomerRequestDraftPostApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Customer_CanUpdateDraftThroughPostRoute()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var credential = factory.Credentials[RoleCodes.Customer];
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            LoginOrEmail = credential.LoginId,
            credential.Password,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var create = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            idempotencyKey = $"post-draft-{Guid.NewGuid():N}",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var draft = (await create.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;

        var update = await client.PostAsJsonAsync($"/api/v1/requests/{draft.Id}/draft", new
        {
            administrativeAreaId = factory.Catalog.AreaId,
            title = "POST 방식 임시저장",
            description = "PUT 네트워크 경로에 의존하지 않는 고객 임시저장입니다.",
            detailAddress = "테스트 상세주소",
            detailAddressDisclosureCode = "AFTER_SELECTION",
            isUrgent = false,
            answers = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var detail = await update.Content.ReadFromJsonAsync<ServiceRequestDetailResponse>();
        Assert.Equal("POST 방식 임시저장", detail!.Title);
        Assert.Equal("테스트 상세주소", detail.DetailAddress);
    }
}
