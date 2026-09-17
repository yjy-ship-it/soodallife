using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.ServiceRequests;

namespace SoodalLife.Api.Tests;

public sealed class CustomerRequestAbusePolicyApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task SameService_ThirdPublishedRequestWithin24Hours_IsRejected()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            LoginOrEmail = factory.Credentials[RoleCodes.Customer].LoginId,
            factory.Credentials[RoleCodes.Customer].Password,
        });

        for (var sequence = 1; sequence <= 2; sequence++)
        {
            var request = await CreateValidDraftAsync(client, sequence);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/requests/{request.Id}/publish", null)).StatusCode);
        }

        var third = await CreateValidDraftAsync(client, 3);
        var response = await client.PostAsync($"/api/v1/requests/{third.Id}/publish", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("REQUEST_SAME_SERVICE_DAILY_LIMIT", error!.BusinessCode);
    }

    private async Task<ServiceRequestCreatedResponse> CreateValidDraftAsync(HttpClient client, int sequence)
    {
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = $"요청 제한 검증 {sequence}",
            description = $"동일 서비스 공개 제한 검증을 위한 서로 다른 요청 {sequence}",
            detailAddress = "테스트로 10, 101호",
            isUrgent = false,
            idempotencyKey = $"abuse-policy-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = $"충분히 자세한 요청 내용 {sequence}입니다." },
                new { fieldId = factory.Catalog.FieldIds[1], value = FutureThirtyMinuteSlot(sequence + 1).ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
    }

    private static DateTimeOffset FutureThirtyMinuteSlot(int days)
    {
        var value = DateTimeOffset.UtcNow.AddDays(days);
        var slot = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute < 30 ? 30 : 0, 0, TimeSpan.Zero);
        return value.Minute < 30 ? slot : slot.AddHours(1);
    }
}
