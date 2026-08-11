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
    public async Task Draft_AllowsMissingFields_ButPublishValidatesRequiredFields()
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        var publish = await client.PostAsync($"/api/v1/requests/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
        var error = await publish.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("REQUEST_PUBLISH_VALIDATION_FAILED", error!.BusinessCode);
    }

    [Fact]
    public async Task Customer_CanUpdateAndResumeDraft()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        var created = await client.PostAsJsonAsync("/api/v1/requests", new { categoryId = factory.Catalog.ServiceId, idempotencyKey = $"draft-{Guid.NewGuid():N}" });
        var draft = (await created.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        var updated = await client.PutAsJsonAsync($"/api/v1/requests/{draft.Id}", new { administrativeAreaId = factory.Catalog.AreaId, title = "수정한 임시 요청", detailAddress = "비공개 상세주소", isUrgent = false, answers = ValidAnswers() });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = await client.GetFromJsonAsync<ServiceRequestDetailResponse>($"/api/v1/requests/{draft.Id}");
        Assert.Equal("수정한 임시 요청", detail!.Title); Assert.True(detail.CanEdit); Assert.True(detail.CanPublish); Assert.Equal(3, detail.Answers.Count);
    }

    [Fact]
    public async Task OtherCustomer_CannotUpdateDraft()
    {
        using var owner = CreateClient(); await LoginAsync(owner, factory.Credentials[RoleCodes.Customer]); var created = await CreateValidRequestAsync(owner);
        using var other = CreateClient(); await LoginAsync(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/v1/requests/{created.Id}", new { title = "침범", answers = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task InvalidSelectValue_IsRejected()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        var answers = ValidAnswers().ToArray(); answers[2] = new { fieldId = factory.Catalog.FieldIds[2], value = "허용되지않음" };
        var response = await client.PostAsJsonAsync("/api/v1/requests", new { categoryId = factory.Catalog.ServiceId, answers });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("sample.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0x01 }, HttpStatusCode.OK)]
    [InlineData("sample.jpg", "image/jpeg", new byte[] { 0x00, 0x01, 0x02 }, HttpStatusCode.BadRequest)]
    [InlineData("sample.exe", "application/octet-stream", new byte[] { 0x4D, 0x5A }, HttpStatusCode.BadRequest)]
    public async Task RequestFile_ValidatesTypeExtensionAndSignature(string name, string contentType, byte[] bytes, HttpStatusCode expected)
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(client);
        using var form = new MultipartFormDataContent(); using var content = new ByteArrayContent(bytes); content.Headers.ContentType = new(contentType); form.Add(content, "file", name);
        var response = await client.PostAsync($"/api/v1/requests/{draft.Id}/files", form); Assert.Equal(expected, response.StatusCode);
        if (response.IsSuccessStatusCode) { var json = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase); Assert.Contains("NOT_INTEGRATED", json); }
    }

    [Fact]
    public async Task OtherCustomer_CannotUploadToOwnedDraft()
    {
        using var owner = CreateClient(); await LoginAsync(owner, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(owner);
        using var other = CreateClient(); await LoginAsync(other, factory.OtherCustomerCredential); using var form = new MultipartFormDataContent(); using var content = new ByteArrayContent([0xFF, 0xD8, 0xFF]); content.Headers.ContentType = new("image/jpeg"); form.Add(content, "file", "safe.jpg");
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/v1/requests/{draft.Id}/files", form)).StatusCode);
    }

    [Fact]
    public async Task RequestFile_OverTenMegabytes_IsRejected()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(client);
        var bytes = new byte[10 * 1024 * 1024 + 1]; bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        using var form = new MultipartFormDataContent(); using var content = new ByteArrayContent(bytes); content.Headers.ContentType = new("image/jpeg"); form.Add(content, "file", "large.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/v1/requests/{draft.Id}/files", form)).StatusCode);
    }

    [Theory]
    [InlineData(RoleCodes.Provider)]
    [InlineData(RoleCodes.Admin)]
    public async Task NonCustomer_CannotReadCustomerRequestList(string role)
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/requests")).StatusCode);
    }

    [Fact]
    public async Task OtherCustomer_CannotCancelRequest()
    {
        using var owner = CreateClient(); await LoginAsync(owner, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(owner);
        using var other = CreateClient(); await LoginAsync(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/v1/requests/{draft.Id}/cancel", new { reason = "권한 없음" })).StatusCode);
    }

    [Fact]
    public async Task Draft_CanBeCancelledIdempotently()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(client);
        var first = await client.PostAsJsonAsync($"/api/v1/requests/{draft.Id}/cancel", new { reason = "더 이상 필요하지 않음" });
        var second = await client.PostAsJsonAsync($"/api/v1/requests/{draft.Id}/cancel", new { reason = "더 이상 필요하지 않음" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("CANCELLED", (await second.Content.ReadFromJsonAsync<ServiceRequestDetailResponse>())!.Status);
    }

    [Fact]
    public async Task DuplicateCreateIdempotencyKey_ReturnsSameDraft()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]); var key = $"same-{Guid.NewGuid():N}";
        var first = await client.PostAsJsonAsync("/api/v1/requests", new { categoryId = factory.Catalog.ServiceId, idempotencyKey = key });
        var second = await client.PostAsJsonAsync("/api/v1/requests", new { categoryId = factory.Catalog.ServiceId, idempotencyKey = key });
        Assert.Equal((await first.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!.Id, (await second.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!.Id);
    }

    [Fact]
    public async Task UnknownDynamicField_IsRejected()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        var response = await client.PostAsJsonAsync("/api/v1/requests", new { categoryId = factory.Catalog.ServiceId, answers = new[] { new { fieldId = Guid.NewGuid(), value = "알 수 없음" } } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublishedRequest_CannotBeEdited()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Customer]); var draft = await CreateValidRequestAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/requests/{draft.Id}/publish", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/requests/{draft.Id}", new { title = "공개 후 수정", answers = ValidAnswers() })).StatusCode);
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

    private object[] ValidAnswers() =>
    [
        new { fieldId = factory.Catalog.FieldIds[0], value = "요청 내용을 충분히 자세하게 작성한 테스트 데이터입니다." },
        new { fieldId = factory.Catalog.FieldIds[1], value = DateTimeOffset.UtcNow.AddDays(2).ToString("O") },
        new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
    ];

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
