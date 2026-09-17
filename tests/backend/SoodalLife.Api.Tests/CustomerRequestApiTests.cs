using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Catalog;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Infrastructure.Persistence;

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
    public async Task ProjectService_RemainsAvailableInRequestCatalogAndCanCreateDraft()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var serviceId = await db.ServiceCategories
            .Where(category => category.PublicId == factory.Catalog.ServiceId)
            .Select(category => category.Id)
            .SingleAsync();
        var policy = await db.CategoryPolicies.SingleAsync(item => item.CategoryId == serviceId);
        var originalTransactionType = policy.TransactionTypeCode;

        try
        {
            policy.TransactionTypeCode = "PROJECT";
            await db.SaveChangesAsync();

            using var client = CreateClient();
            await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);

            var majors = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/v1/categories/majors");
            Assert.Contains(majors!, item => item.Id == factory.Catalog.MajorId);
            var fields = await client.GetFromJsonAsync<List<RequestFieldResponse>>($"/api/v1/categories/{factory.Catalog.ServiceId}/request-fields");
            Assert.NotEmpty(fields!);

            var response = await client.PostAsJsonAsync("/api/v1/requests", new
            {
                categoryId = factory.Catalog.ServiceId,
                idempotencyKey = $"project-draft-{Guid.NewGuid():N}",
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        finally
        {
            policy.TransactionTypeCode = originalTransactionType;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task EmergencyCategoryHierarchy_UsesPolicyEvenWithoutAvailableProviders()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.False(await db.ProviderEmergencySettings.AnyAsync());

        var majors = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/v1/categories/majors?emergencyOnly=true");
        Assert.Contains(majors!, item => item.Id == factory.Catalog.MajorId);

        var middles = await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/v1/categories/{factory.Catalog.MajorId}/middles?emergencyOnly=true");
        var middle = Assert.Single(middles!);

        var services = await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/v1/categories/{middle.Id}/services?emergencyOnly=true");
        Assert.Contains(services!, item => item.Id == factory.Catalog.ServiceId);
        Assert.DoesNotContain(services!, item => item.Id == factory.Catalog.OtherServiceId);
    }

    [Fact]
    public async Task Customer_CannotCreateEmergencyRequestForPolicyDisallowedService()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);

        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.OtherServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "허용되지 않은 긴급 요청",
            isUrgent = true,
            idempotencyKey = $"emergency-policy-{Guid.NewGuid():N}",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmergencyRequest_UsesAllowedPolicyWhenAnotherCurrentPolicyIsDisallowed()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var categoryId = await db.ServiceCategories.Where(x => x.PublicId == factory.Catalog.ServiceId).Select(x => x.Id).SingleAsync();
        var feePolicyId = await db.CategoryPolicies.Where(x => x.CategoryId == categoryId).Select(x => x.FeePolicyId).FirstAsync();
        var conflictingPolicy = new SoodalLife.Api.Domain.Entities.CategoryPolicy
        {
            CategoryId = categoryId,
            PolicyVersion = $"emergency-conflict-{Guid.NewGuid():N}",
            TransactionTypeCode = "ONE_TIME",
            IsEmergencyAllowed = false,
            CurrencyCode = "KRW",
            MaxQuoteCount = 5,
            QuoteValidityMinutes = 120,
            ProviderResponseDeadlineMinutes = 30,
            FeePolicyId = feePolicyId,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.CategoryPolicies.Add(conflictingPolicy);
        await db.SaveChangesAsync();

        try
        {
            using var client = CreateClient();
            await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
            var detail = await client.GetFromJsonAsync<PublicServiceDetailResponse>($"/api/v1/public/catalog/services/{factory.Catalog.ServiceId}");
            Assert.True(detail!.EmergencyRequestAllowed);

            var response = await client.PostAsJsonAsync("/api/v1/requests", new
            {
                categoryId = factory.Catalog.ServiceId,
                title = "긴급 정책 선택 테스트",
                isUrgent = true,
                idempotencyKey = $"emergency-selection-{Guid.NewGuid():N}",
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        finally
        {
            db.CategoryPolicies.Remove(conflictingPolicy);
            await db.SaveChangesAsync();
        }
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
    public async Task NationwideRemoteRequest_DoesNotCollectAddress_AndStillMatchesNationwideProvider()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var categoryId = await db.ServiceCategories.Where(x => x.PublicId == factory.Catalog.ServiceId).Select(x => x.Id).SingleAsync();
        var operation = await db.CategoryOperationPolicies.SingleAsync(x => x.CategoryId == categoryId && x.IsActive);
        var provider = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                              where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var providerService = await db.ProviderServiceCategories.SingleAsync(x => x.ProviderProfileId == provider.Id && x.CategoryId == categoryId);
        var originalCoverage = operation.CoverageTypeCode;
        var originalNationwide = providerService.IsNationwide;

        try
        {
            operation.CoverageTypeCode = "NATIONWIDE_REMOTE";
            providerService.IsNationwide = true;
            await db.SaveChangesAsync();

            using var customer = CreateClient();
            await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);
            var serviceDetail = await customer.GetFromJsonAsync<PublicServiceDetailResponse>($"/api/v1/public/catalog/services/{factory.Catalog.ServiceId}");
            Assert.NotNull(serviceDetail);
            Assert.Equal("NATIONWIDE_REMOTE", serviceDetail.CoverageTypeCode);
            Assert.False(serviceDetail.RequiresServiceAddress);
            var response = await customer.PostAsJsonAsync("/api/v1/requests", new
            {
                categoryId = factory.Catalog.ServiceId,
                title = "전국 온라인 요청",
                description = "주소 없이 온라인으로 진행합니다.",
                detailAddress = "저장되면 안 되는 주소",
                detailAddressDisclosureCode = "BEFORE_QUOTE",
                isUrgent = false,
                idempotencyKey = $"remote-{Guid.NewGuid():N}",
                answers = ValidAnswers(),
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
            var createdRequest = await db.ServiceRequests.SingleAsync(item => item.PublicId == created.Id);
            createdRequest.AbuseCountExcluded = true;
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.OK, (await customer.PostAsync($"/api/v1/requests/{created.Id}/publish", null)).StatusCode);

            var detail = await customer.GetFromJsonAsync<ServiceRequestDetailResponse>($"/api/v1/requests/{created.Id}");
            Assert.Null(detail!.AdministrativeAreaId);
            Assert.Null(detail.DetailAddress);
            Assert.Equal("AFTER_SELECTION", detail.DetailAddressDisclosureCode);

            using var providerClient = CreateClient();
            await LoginAsync(providerClient, factory.Credentials[RoleCodes.Provider]);
            var inbox = await providerClient.GetFromJsonAsync<List<SoodalLife.Api.Features.Matching.ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
            Assert.Contains(inbox!, item => item.RequestId == created.Id && item.AreaName == "전국·온라인");
            var matchedRequest = await providerClient.GetFromJsonAsync<SoodalLife.Api.Features.Matching.ProviderMatchedRequestDetail>($"/api/v1/providers/me/matched-requests/{created.Id}");
            Assert.NotNull(matchedRequest);
            Assert.False(matchedRequest.RequiresServiceAddress);
            Assert.Null(matchedRequest.DetailAddress);
            Assert.Null(matchedRequest.ApproximateDistanceKm);
        }
        finally
        {
            operation.CoverageTypeCode = originalCoverage;
            providerService.IsNationwide = originalNationwide;
            await db.SaveChangesAsync();
        }
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

    public static TheoryData<string, string, byte[], HttpStatusCode> RequestFileCases => new()
    {
        { "sample.jpg", "image/jpeg", TestFileSamples.ValidJpeg(), HttpStatusCode.OK },
        { "sample.jpg", "image/jpeg", new byte[] { 0x00, 0x01, 0x02 }, HttpStatusCode.BadRequest },
        { "sample.exe", "application/octet-stream", new byte[] { 0x4D, 0x5A }, HttpStatusCode.BadRequest },
    };

    [Theory]
    [MemberData(nameof(RequestFileCases))]
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
        using var other = CreateClient(); await LoginAsync(other, factory.OtherCustomerCredential); using var form = new MultipartFormDataContent(); using var content = new ByteArrayContent(TestFileSamples.ValidJpeg()); content.Headers.ContentType = new("image/jpeg"); form.Add(content, "file", "safe.jpg");
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/v1/requests/{draft.Id}/files", form)).StatusCode);
    }

    [Fact]
    public async Task RequestFile_OwnerCanDownload_ButOtherCustomerCannot_AndStatusesAreSeparated()
    {
        using var owner = CreateClient();
        await LoginAsync(owner, factory.Credentials[RoleCodes.Customer]);
        var draft = await CreateValidRequestAsync(owner);
        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent(TestFileSamples.ValidJpeg());
        content.Headers.ContentType = new("image/jpeg");
        form.Add(content, "file", "customer-private.jpg");
        var uploaded = await owner.PostAsync($"/api/v1/requests/{draft.Id}/files", form);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var file = (await uploaded.Content.ReadFromJsonAsync<ServiceRequestFileResponse>())!;
        Assert.Equal("NOT_INTEGRATED", file.MalwareScanStatus);
        Assert.Equal("NOT_INTEGRATED", file.PrivacyInspectionStatus);
        Assert.Equal("WITHHELD_PRIVACY_PROTECTION_PENDING", file.ProviderVisibilityStatus);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(file.DownloadUrl)).StatusCode);

        using var other = CreateClient();
        await LoginAsync(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(file.DownloadUrl)).StatusCode);
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

    [Fact]
    public async Task Customer_CanPublishRequestWithMinutePrecisionDesiredDate()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Customer]);
        var desiredAt = DateTimeOffset.UtcNow.AddDays(2).AddMinutes(7).ToString("O");
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "분 단위 희망일시 테스트",
            detailAddress = "테스트 상세주소",
            idempotencyKey = $"minute-precision-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "분 단위 희망일시가 저장되는지 확인하는 요청입니다." },
                new { fieldId = factory.Catalog.FieldIds[1], value = desiredAt },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;

        var publish = await client.PostAsync($"/api/v1/requests/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
    }

    private async Task<ServiceRequestCreatedResponse> CreateValidRequestAsync(HttpClient client)
    {
        var desiredAt = FutureThirtyMinuteSlot();
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
        var created = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        await TestRequestData.ExcludeFromAbuseLimitsAsync(factory, created.Id);
        return created;
    }

    private object[] ValidAnswers() =>
    [
        new { fieldId = factory.Catalog.FieldIds[0], value = "요청 내용을 충분히 자세하게 작성한 테스트 데이터입니다." },
        new { fieldId = factory.Catalog.FieldIds[1], value = FutureThirtyMinuteSlot() },
        new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
    ];

    private static string FutureThirtyMinuteSlot()
    {
        var future = DateTimeOffset.UtcNow.AddDays(2);
        var hour = new DateTimeOffset(future.Year, future.Month, future.Day, future.Hour, 0, 0, TimeSpan.Zero);
        return hour.AddMinutes(future.Minute < 30 ? 30 : 60).ToString("O");
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
