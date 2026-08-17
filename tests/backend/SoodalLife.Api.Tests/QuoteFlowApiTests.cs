using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class QuoteFlowApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Draft_Revisions_Submission_CustomerSelection_CreateSingleTransaction()
    {
        using var provider = CreateClient();
        using var secondProvider = CreateClient();
        using var customer = CreateClient();
        await LoginAsync(provider, factory.Credentials[RoleCodes.Provider]);
        await LoginAsync(secondProvider, factory.ServiceMismatchProviderCredential);
        await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);
        await ConfigureProviderAsync(provider);
        await ConfigureProviderAsync(secondProvider);
        await EnsureTradingReadyAsync(factory.Credentials[RoleCodes.Provider]);
        await EnsureTradingReadyAsync(factory.ServiceMismatchProviderCredential);

        var request = await CreateAndPublishRequestAsync(customer);
        var firstInput = QuoteInput("첫 견적", 35m, 100m, 50m, $"quote-{Guid.NewGuid():N}");
        var draftResponse = await provider.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", firstInput);
        Assert.Equal(HttpStatusCode.OK, draftResponse.StatusCode);
        var draft = (await draftResponse.Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        Assert.Equal("DRAFT", draft.Status);
        Assert.Equal(1, draft.Revision.RevisionNo);
        Assert.Equal(350m, draft.Revision.SubtotalAmount);
        Assert.Equal(385m, draft.Revision.TotalAmount);
        Assert.Equal(2, draft.Revision.Items.Count);
        var repeatedDraft = (await (await provider.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", firstInput))
            .Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        Assert.Equal(1, repeatedDraft.Revision.RevisionNo);

        var hiddenDrafts = await customer.GetFromJsonAsync<List<QuoteListItemResponse>>($"/api/v1/requests/{request.Id}/quotes");
        Assert.Empty(hiddenDrafts!);

        var secondInput = QuoteInput("수정 견적", 40m, 120m, 60m, $"quote-{Guid.NewGuid():N}");
        var revisionResponse = await provider.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/revisions", secondInput);
        var revised = (await revisionResponse.Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        Assert.Equal(2, revised.Revision.RevisionNo);
        Assert.Equal(420m, revised.Revision.SubtotalAmount);
        Assert.Equal(460m, revised.Revision.TotalAmount);

        var submitResponse = await provider.PostAsync($"/api/v1/quotes/{draft.Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitted = (await submitResponse.Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        Assert.Equal("SUBMITTED", submitted.Status);
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsync($"/api/v1/quotes/{draft.Id}/submit", null)).StatusCode);

        var otherDraft = (await (await secondProvider.PostAsJsonAsync(
            $"/api/v1/requests/{request.Id}/quotes",
            QuoteInput("비교 견적", 10m, 90m, 40m, $"quote-{Guid.NewGuid():N}")))
            .Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        var otherSubmit = await secondProvider.PostAsync($"/api/v1/quotes/{otherDraft.Id}/submit", null);
        Assert.True(otherSubmit.IsSuccessStatusCode, await otherSubmit.Content.ReadAsStringAsync());

        var listResponse = await customer.GetAsync($"/api/v1/requests/{request.Id}/quotes");
        var listJson = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("overallRating", listJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wallet", listJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fee", listJson, StringComparison.OrdinalIgnoreCase);
        var list = JsonSerializer.Deserialize<List<CustomerQuoteComparisonResponse>>(listJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(2, list!.Count);
        Assert.All(list, item => Assert.Equal("신규·평가중", item.TrustDisplay));
        var detail = await customer.GetFromJsonAsync<CustomerQuoteDetailResponse>($"/api/v1/quotes/{draft.Id}");
        Assert.Equal(2, detail!.Revision.RevisionNo);
        Assert.Equal(2, detail.Revision.Items.Count);
        Assert.Equal("신규·평가중", detail.Comparison.TrustDisplay);
        var profileResponse = await customer.GetAsync($"/api/v1/customer/providers/{detail.ProviderId}?requestId={request.Id}");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profileJson = await profileResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("decisionReason", profileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin", profileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", profileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", profileJson, StringComparison.OrdinalIgnoreCase);

        var missingAddress = await customer.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/accept", new { detailAddress = "" });
        Assert.Equal(HttpStatusCode.BadRequest, missingAddress.StatusCode);
        var acceptedResponse = await customer.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/accept", new { detailAddress = "대구광역시 동구 테스트로 1" });
        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        var accepted = (await acceptedResponse.Content.ReadFromJsonAsync<AcceptQuoteResponse>())!;
        Assert.Equal("CREATED", accepted.TransactionStatus);
        Assert.Equal(460m, accepted.AgreedAmount);
        var repeated = (await (await customer.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/accept", new { detailAddress = "대구광역시 동구 테스트로 1" }))
            .Content.ReadFromJsonAsync<AcceptQuoteResponse>())!;
        Assert.Equal(accepted.TransactionId, repeated.TransactionId);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync($"/api/v1/quotes/{otherDraft.Id}/accept", new { detailAddress = "대구광역시 동구 테스트로 1" })).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var quote = await db.Quotes.SingleAsync(item => item.PublicId == draft.Id);
        var revisions = await db.QuoteRevisions.Where(item => item.QuoteId == quote.Id).OrderBy(item => item.RevisionNo).ToListAsync();
        Assert.Equal(2, revisions.Count);
        Assert.Equal(385m, revisions[0].TotalAmount);
        Assert.Equal(460m, revisions[1].TotalAmount);
        var transaction = await db.Transactions.SingleAsync(item => item.PublicId == accepted.TransactionId);
        Assert.Equal("CREATED", transaction.StatusCode);
        Assert.Equal(460m, transaction.AgreedAmount);
        var chatRoom = await db.ChatRooms.SingleAsync(item => item.ResourceTypeCode == "TRANSACTION" && item.ResourcePublicId == transaction.PublicId);
        var chatParticipants = await db.ChatParticipants.Where(item => item.ChatRoomId == chatRoom.Id).ToListAsync();
        Assert.Equal(2, chatParticipants.Count);
        Assert.Contains(chatParticipants, item => item.ParticipantRoleCode == "CUSTOMER");
        Assert.Contains(chatParticipants, item => item.ParticipantRoleCode == "PROVIDER");
        var unselectedProviderProfileId = await db.Quotes.Where(item => item.PublicId == otherDraft.Id).Select(item => item.ProviderProfileId).SingleAsync();
        var unselectedProviderUserId = await db.ProviderProfiles.Where(item => item.Id == unselectedProviderProfileId).Select(item => item.UserId).SingleAsync();
        Assert.DoesNotContain(chatParticipants, item => item.UserId == unselectedProviderUserId);
        Assert.True(JsonDocument.Parse(transaction.QuoteSnapshotJson).RootElement.TryGetProperty("items", out var snapshotItems));
        Assert.Equal(2, snapshotItems.GetArrayLength());
        Assert.Single(await db.Transactions.Where(item => item.ServiceRequestId == transaction.ServiceRequestId).ToListAsync());
        Assert.Equal("NOT_SELECTED", (await db.Quotes.SingleAsync(item => item.PublicId == otherDraft.Id)).StatusCode);
    }

    [Fact]
    public async Task QuoteObjectAuthorization_BlocksUnrelatedProvidersAndCustomers()
    {
        using var provider = CreateClient();
        using var unrelatedProvider = CreateClient();
        using var customer = CreateClient();
        using var otherCustomer = CreateClient();
        await LoginAsync(provider, factory.Credentials[RoleCodes.Provider]);
        await LoginAsync(unrelatedProvider, factory.AreaMismatchProviderCredential);
        await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);
        await LoginAsync(otherCustomer, factory.OtherCustomerCredential);
        await ConfigureProviderAsync(provider);
        var request = await CreateAndPublishRequestAsync(customer);

        var input = QuoteInput("권한 테스트 견적", 0m, 100m, 100m, $"quote-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unrelatedProvider.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", input)).StatusCode);
        var draft = (await (await provider.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", input))
            .Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unrelatedProvider.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/revisions", QuoteInput("침범", 0m, 1m, 1m, $"quote-{Guid.NewGuid():N}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await otherCustomer.GetAsync($"/api/v1/requests/{request.Id}/quotes")).StatusCode);

        await provider.PostAsync($"/api/v1/quotes/{draft.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.GetAsync($"/api/v1/quotes/{draft.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/accept", new { detailAddress = "대구광역시 동구 테스트로 1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", input)).StatusCode);
    }

    private async Task ConfigureProviderAsync(HttpClient client)
    {
        await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
        await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new
        {
            services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } },
        });
    }

    private async Task EnsureTradingReadyAsync(TestCredential credential)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var serviceId = await db.ServiceCategories.Where(item => item.PublicId == factory.Catalog.ServiceId).Select(item => item.Id).SingleAsync();
        var link = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                          join service in db.ProviderServiceCategories on profile.Id equals service.ProviderProfileId
                          where user.LoginId == credential.LoginId && service.CategoryId == serviceId select service).SingleAsync();
        var approval = await db.ProviderServiceApprovals.SingleOrDefaultAsync(item => item.ProviderServiceCategoryId == link.Id);
        if (approval is null)
            db.ProviderServiceApprovals.Add(new() { ProviderServiceCategoryId = link.Id, ApprovalStatusCode = "APPROVED", ApprovalRequestedAt = DateTime.UtcNow, ApprovalDecidedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        else
        {
            approval.ApprovalStatusCode = "APPROVED";
            approval.ApprovalDecidedAt = DateTime.UtcNow;
        }
        var assignment = await db.CategoryProviderRequirementAssignments.SingleAsync(item => item.IsActive);
        var verification = await db.ProviderServiceRequirementVerifications.SingleOrDefaultAsync(item => item.ProviderServiceCategoryId == link.Id && item.RequirementAssignmentId == assignment.Id);
        if (verification is null)
            db.ProviderServiceRequirementVerifications.Add(new() { ProviderServiceCategoryId = link.Id, RequirementAssignmentId = assignment.Id, VerificationStatusCode = "APPROVED", VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        else
        {
            verification.VerificationStatusCode = "APPROVED";
            verification.VerifiedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private async Task<ServiceRequestCreatedResponse> CreateAndPublishRequestAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "Quote flow request",
            description = "A provider-visible quote test request.",
            detailAddress = "Private customer address",
            isUrgent = false,
            idempotencyKey = $"quote-flow-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "A sufficiently detailed request answer for quote flow tests." },
                new { fieldId = factory.Catalog.FieldIds[1], value = DateTimeOffset.UtcNow.AddDays(2).ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var request = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/requests/{request.Id}/publish", null)).StatusCode);
        return request;
    }

    private static SaveQuoteRevisionInput QuoteInput(string summary, decimal vat, decimal firstUnitPrice, decimal secondUnitPrice, string key) => new(
        summary,
        "부품과 작업비를 포함합니다.",
        vat,
        "약 2시간",
        DateTime.UtcNow.AddMinutes(20),
        DateTime.UtcNow.AddMinutes(60),
        null,
        key,
        [
            new QuoteItemInput("작업비", "기본 작업", 2m, "시간", firstUnitPrice),
            new QuoteItemInput("자재비", "필수 자재", 3m, "개", secondUnitPrice),
        ]);

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
