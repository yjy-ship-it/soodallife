using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class SelfTradePreventionApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task DualRoleAccount_CannotReceiveQuoteOrAcceptItsOwnRequest()
    {
        long providerId;
        long providerUserId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Provider].LoginId);
            var customerRole = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Customer);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id, GrantedAt = DateTime.UtcNow });
            db.CustomerProfiles.Add(new CustomerProfile
            {
                UserId = user.Id,
                DisplayName = "Dual role customer",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            providerUserId = user.Id;
            providerId = await db.ProviderProfiles.Where(x => x.UserId == user.Id).Select(x => x.Id).SingleAsync();
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var credential = factory.Credentials[RoleCodes.Provider];
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);

        var create = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "Dual role self trade prevention",
            description = "The same account must never receive or quote this request.",
            detailAddress = "Protected detail address",
            isUrgent = false,
            idempotencyKey = $"self-trade-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "A sufficiently detailed self-trade prevention answer." },
                new { fieldId = factory.Catalog.FieldIds[1], value = TestScheduleSlots.Future().ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var request = (await create.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/requests/{request.Id}/publish", null)).StatusCode);

        var inbox = await client.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.DoesNotContain(inbox!, x => x.RequestId == request.Id);

        Guid quoteId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var storedRequest = await db.ServiceRequests.SingleAsync(x => x.PublicId == request.Id);
            var candidate = await db.DispatchCandidates.SingleAsync(x => x.ServiceRequestId == storedRequest.Id && x.ProviderProfileId == providerId);
            Assert.Equal("INELIGIBLE", candidate.StatusCode);
            Assert.Equal("SELF_REQUEST_NOT_ALLOWED", candidate.ReasonCode);

            candidate.StatusCode = "DISPATCHED";
            var dispatch = new RequestDispatch
            {
                ServiceRequestId = storedRequest.Id,
                ProviderProfileId = providerId,
                CandidateId = candidate.Id,
                StatusCode = "AVAILABLE",
                AvailableAt = DateTime.UtcNow,
                ExpiresAt = storedRequest.ExpiresAt!.Value,
                IdempotencyKey = $"legacy-self-dispatch-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
            };
            db.RequestDispatches.Add(dispatch);
            await db.SaveChangesAsync();
            var quote = new Quote
            {
                ServiceRequestId = storedRequest.Id,
                ProviderProfileId = providerId,
                RequestDispatchId = dispatch.Id,
                StatusCode = "DRAFT",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = providerUserId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedByUserId = providerUserId,
            };
            db.Quotes.Add(quote);
            await db.SaveChangesAsync();
            db.QuoteRevisions.Add(new QuoteRevision
            {
                QuoteId = quote.Id,
                RevisionNo = 1,
                Summary = "Legacy self quote",
                SubtotalAmount = 10000,
                VatAmount = 1000,
                TotalAmount = 11000,
                CurrencyCode = "KRW",
                ValidUntil = DateTime.UtcNow.AddHours(1),
                SubmittedAt = DateTime.UtcNow,
                SubmittedByUserId = providerUserId,
                IdempotencyKey = $"legacy-self-revision-{Guid.NewGuid():N}",
            });
            await db.SaveChangesAsync();
            quoteId = quote.PublicId;
        }

        inbox = await client.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.DoesNotContain(inbox!, x => x.RequestId == request.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/providers/me/matched-requests/{request.Id}")).StatusCode);
        await AssertSelfTradeBlocked(client.GetAsync($"/api/v1/providers/me/requests/{request.Id}/quote-submission-readiness"));
        await AssertSelfTradeBlocked(client.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", new
        {
            summary = "Blocked self quote",
            terms = (string?)null,
            vatAmount = 0,
            estimatedDurationText = "1 hour",
            availableStartAt = DateTime.UtcNow.AddMinutes(30),
            validUntil = DateTime.UtcNow.AddHours(1),
            revisionReason = (string?)null,
            idempotencyKey = $"blocked-self-{Guid.NewGuid():N}",
            items = new[] { new { itemName = "Work", description = (string?)null, quantity = 1, unitText = "job", unitPriceAmount = 10000 } },
        }));
        await AssertSelfTradeBlocked(client.PostAsync($"/api/v1/quotes/{quoteId}/submit", null));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var quote = await db.Quotes.SingleAsync(x => x.PublicId == quoteId);
            quote.StatusCode = "SUBMITTED";
            quote.SubmittedAt = DateTime.UtcNow;
            quote.ExpiresAt = DateTime.UtcNow.AddHours(1);
            await db.SaveChangesAsync();
        }
        Assert.Empty((await client.GetFromJsonAsync<List<CustomerQuoteComparisonResponse>>($"/api/v1/requests/{request.Id}/quotes"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/quotes/{quoteId}")).StatusCode);
        await AssertSelfTradeBlocked(client.PostAsJsonAsync($"/api/v1/quotes/{quoteId}/accept", new { detailAddress = "Self address" }));
    }

    private static async Task AssertSelfTradeBlocked(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("SELF_REQUEST_NOT_ALLOWED", await response.Content.ReadAsStringAsync());
    }
}
