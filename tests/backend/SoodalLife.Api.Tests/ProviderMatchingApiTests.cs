using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderMatchingApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task ProviderConfiguration_MatchingDispatchAndInbox_WorkEndToEnd()
    {
        using var providerClient = CreateClient();
        await LoginAsync(providerClient, factory.Credentials[RoleCodes.Provider]);

        var servicesResponse = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        Assert.Equal(HttpStatusCode.OK, servicesResponse.StatusCode);
        Assert.Single((await servicesResponse.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);
        var repeatedServices = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        Assert.Single((await repeatedServices.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);

        var areasResponse = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            });
        Assert.Equal(HttpStatusCode.OK, areasResponse.StatusCode);
        var areaGroups = (await areasResponse.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>())!;
        Assert.Single(areaGroups);
        Assert.Single(areaGroups[0].Areas);
        var repeatedAreas = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            });
        Assert.Single((await repeatedAreas.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>())![0].Areas);

        using var customerClient = CreateClient();
        await LoginAsync(customerClient, factory.Credentials[RoleCodes.Customer]);
        var created = await CreateValidRequestAsync(customerClient);
        var publishResponse = await customerClient.PostAsync($"/api/v1/requests/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = (await publishResponse.Content.ReadFromJsonAsync<PublishServiceRequestResponse>())!;
        Assert.Equal("OPEN", published.Status);
        Assert.Equal(1, published.EligibleCandidateCount);
        Assert.Equal(1, published.DispatchCount);

        using var adminClient = CreateClient();
        await LoginAsync(adminClient, factory.Credentials[RoleCodes.Admin]);
        var repeatMatching = await adminClient.PostAsync($"/api/v1/internal/requests/{created.Id}/match-and-dispatch", null);
        var repeatedResult = (await repeatMatching.Content.ReadFromJsonAsync<MatchAndDispatchResult>())!;
        Assert.Equal(1, repeatedResult.DispatchCount);
        Assert.Equal(0, repeatedResult.NewDispatchCount);

        var inbox = await providerClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.Contains(inbox!, item => item.RequestId == created.Id);
        var detailResponse = await providerClient.GetAsync($"/api/v1/providers/me/matched-requests/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<ProviderMatchedRequestDetail>())!;
        Assert.Equal(created.Id, detail.RequestId);
        Assert.Equal("VIEWED", detail.DispatchStatus);
        Assert.Null(detail.DetailAddress);
        Assert.Null(detail.CustomerPhone);

        await AssertCandidateOutcomesAsync(created.Id);

        using var otherProviderClient = CreateClient();
        await LoginAsync(otherProviderClient, factory.ServiceMismatchProviderCredential);
        var otherInbox = await otherProviderClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.DoesNotContain(otherInbox!, item => item.RequestId == created.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProviderClient.GetAsync($"/api/v1/providers/me/matched-requests/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customerClient.GetAsync("/api/v1/providers/me/matched-requests")).StatusCode);
    }

    [Fact]
    public async Task DuplicateProviderServiceAndAreaSelections_AreRejected()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Provider]);
        var duplicateServices = await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId, factory.Catalog.ServiceId } });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateServices.StatusCode);

        await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        var duplicateAreas = await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId, factory.Catalog.AreaId } },
                },
            });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateAreas.StatusCode);
    }

    private async Task AssertCandidateOutcomesAsync(Guid requestPublicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var candidates = await (
                from candidate in dbContext.DispatchCandidates
                join request in dbContext.ServiceRequests on candidate.ServiceRequestId equals request.Id
                join provider in dbContext.ProviderProfiles on candidate.ProviderProfileId equals provider.Id
                join user in dbContext.Users on provider.UserId equals user.Id
                where request.PublicId == requestPublicId
                select new { user.LoginId, Candidate = candidate })
            .ToListAsync();

        var matched = candidates.Single(item => item.LoginId == factory.Credentials[RoleCodes.Provider].LoginId).Candidate;
        Assert.Equal("DISPATCHED", matched.StatusCode);
        Assert.True(matched.CategoryMatch && matched.AreaMatch && matched.ApprovalMatch);

        var serviceMismatch = candidates.Single(item => item.LoginId == factory.ServiceMismatchProviderCredential.LoginId).Candidate;
        Assert.Equal("SERVICE_CATEGORY_MISMATCH", serviceMismatch.ReasonCode);
        Assert.False(serviceMismatch.CategoryMatch);

        var areaMismatch = candidates.Single(item => item.LoginId == factory.AreaMismatchProviderCredential.LoginId).Candidate;
        Assert.Equal("SERVICE_AREA_MISMATCH", areaMismatch.ReasonCode);
        Assert.True(areaMismatch.CategoryMatch);
        Assert.False(areaMismatch.AreaMatch);

        var inactive = candidates.Single(item => item.LoginId == factory.InactiveProviderCredential.LoginId).Candidate;
        Assert.Equal("PROVIDER_NOT_APPROVED_ACTIVE", inactive.ReasonCode);
        Assert.False(inactive.ApprovalMatch);
        Assert.Single(await dbContext.RequestDispatches.Where(dispatch => dispatch.ServiceRequestId == matched.ServiceRequestId).ToListAsync());
    }

    private async Task<ServiceRequestCreatedResponse> CreateValidRequestAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "Provider matching request",
            description = "Provider-visible request summary without customer identity.",
            detailAddress = "Never expose this detail address",
            isUrgent = false,
            idempotencyKey = $"provider-flow-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "A sufficiently detailed request answer for provider matching tests." },
                new { fieldId = factory.Catalog.FieldIds[1], value = DateTimeOffset.UtcNow.AddDays(2).ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
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
