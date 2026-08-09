using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Catalog;
using SoodalLife.Api.Features.ServiceRequests;

namespace SoodalLife.Api.Tests;

public sealed class CustomerRequestApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Customer_CanBrowseCategoryHierarchyAndDynamicFields()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);

        var majors = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/v1/categories/majors");
        Assert.Contains(majors!, item => item.Id == factory.Catalog.MajorId);
        var middles = await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/v1/categories/{factory.Catalog.MajorId}/middles");
        Assert.NotNull(middles);
        Assert.Single(middles);
        var services = await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/v1/categories/{middles[0].Id}/services");
        Assert.Contains(services!, item => item.Id == factory.Catalog.ServiceId);
        var fields = await client.GetFromJsonAsync<List<RequestFieldResponse>>($"/api/v1/categories/{factory.Catalog.ServiceId}/request-fields");
        Assert.Equal(3, fields!.Count);
        Assert.Contains(fields, field => field.InputType == "SELECT" && field.Options.SequenceEqual(["주거", "상가"]));
    }

    [Fact]
    public async Task Customer_CanCreateAndReadOwnRequest()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);

        var created = await CreateValidRequestAsync(client);
        var list = await client.GetFromJsonAsync<List<ServiceRequestListItemResponse>>("/api/v1/requests");
        Assert.Contains(list!, item => item.Id == created.Id && item.Status == "DRAFT");
        var detail = await client.GetFromJsonAsync<ServiceRequestDetailResponse>($"/api/v1/requests/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal(3, detail.Answers.Count);
    }

    [Fact]
    public async Task MissingRequiredDynamicField_ReturnsBadRequest()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "필수 필드 누락 테스트",
            answers = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("DYNAMIC_FIELD_REQUIRED", error!.BusinessCode);
    }

    [Fact]
    public async Task OtherCustomer_CannotReadRequest()
    {
        using var ownerClient = CreateClient();
        await LoginAsync(ownerClient, factory.Credentials[RoleCodes.Customer]);
        var created = await CreateValidRequestAsync(ownerClient);

        using var otherClient = CreateClient();
        await LoginAsync(otherClient, factory.OtherCustomerCredential);
        var response = await otherClient.GetAsync($"/api/v1/requests/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RoleCodes.Provider, HttpStatusCode.Forbidden)]
    [InlineData(RoleCodes.Admin, HttpStatusCode.Forbidden)]
    public async Task NonCustomerRole_CannotCreateRequest(string role, HttpStatusCode expected)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        var response = await client.PostAsJsonAsync("/api/v1/requests", new { });
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotCreateRequest()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/requests", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<ServiceRequestCreatedResponse> CreateValidRequestAsync(HttpClient client)
    {
        var desiredAt = DateTimeOffset.UtcNow.AddDays(2).ToString("O");
        var answers = new object[]
        {
            new { fieldId = factory.Catalog.FieldIds[0], value = "요청 내용을 충분히 자세하게 작성한 테스트 데이터입니다." },
            new { fieldId = factory.Catalog.FieldIds[1], value = desiredAt },
            new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
        };
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "통합 테스트 요청",
            description = "고객 요청 생성 통합 테스트",
            detailAddress = "테스트 상세주소",
            isUrgent = false,
            idempotencyKey = $"test-{Guid.NewGuid():N}",
            answers,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
