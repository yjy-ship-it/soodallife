using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class WorkFlowApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Start_CompletionRevision_PhotoPolicy_Confirmation_CreateHistoryAtomically()
    {
        using var provider = Client(); using var customer = Client(); using var otherProvider = Client(); using var otherCustomer = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(otherProvider, factory.AreaMismatchProviderCredential); await Login(otherCustomer, factory.OtherCustomerCredential);
        var transactionId = await ArrangeTransaction(provider, customer);

        var started = await (await provider.PostAsync($"/api/v1/transactions/{transactionId}/start", null)).Content.ReadFromJsonAsync<WorkTransactionDetail>();
        Assert.Equal("IN_PROGRESS", started!.Status);
        var repeatedStart = await (await provider.PostAsync($"/api/v1/transactions/{transactionId}/start", null)).Content.ReadFromJsonAsync<WorkTransactionDetail>();
        Assert.Equal(started.StartedAt, repeatedStart!.StartedAt);

        var draftKey = $"draft-{Guid.NewGuid():N}";
        var draft = await SaveDraft(provider, transactionId, draftKey, "Initial completed work", 121m, null);
        Assert.Equal("DRAFT", draft.Status);
        Assert.Equal(draft.Id, (await SaveDraft(provider, transactionId, draftKey, "Initial completed work", 121m, null)).Id);
        Assert.Equal(HttpStatusCode.Conflict, (await provider.PostAsync($"/api/v1/transactions/{transactionId}/completions/submit", null)).StatusCode);
        var before = await Upload(provider, transactionId, "BEFORE", "before.png");
        Assert.Equal("BEFORE", before.RoleCode);
        Assert.Equal(HttpStatusCode.Conflict, (await provider.PostAsync($"/api/v1/transactions/{transactionId}/completions/submit", null)).StatusCode);
        var after = await Upload(provider, transactionId, "AFTER", "after.png");
        var submitted = await (await provider.PostAsync($"/api/v1/transactions/{transactionId}/completions/submit", null)).Content.ReadFromJsonAsync<WorkCompletionRevisionResponse>();
        Assert.Equal("SUBMITTED", submitted!.Status); Assert.True(submitted.Policy.IsSatisfied); Assert.Equal(2, submitted.Evidence.Count);
        Assert.Equal(submitted.Id, (await (await provider.PostAsync($"/api/v1/transactions/{transactionId}/completions/submit", null)).Content.ReadFromJsonAsync<WorkCompletionRevisionResponse>())!.Id);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync(before.DownloadUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync(before.DownloadUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.GetAsync(before.DownloadUrl)).StatusCode);

        var revisionRequested = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/confirm-completion", new
        { completionRevisionId = submitted.Id, result = "REVISION_REQUESTED", comment = "Please add the final detail.", idempotencyKey = $"confirm-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, revisionRequested.StatusCode);
        var revised = await SaveDraft(provider, transactionId, $"draft-{Guid.NewGuid():N}", "Revised completed work", 123m, "Customer request");
        Assert.Equal(2, revised.RevisionNo); Assert.Equal(2, revised.Evidence.Count);
        var resubmitted = await (await provider.PostAsync($"/api/v1/transactions/{transactionId}/completions/submit", null)).Content.ReadFromJsonAsync<WorkCompletionRevisionResponse>();
        var completeKey = $"confirm-{Guid.NewGuid():N}";
        var completed = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/confirm-completion", new
        { completionRevisionId = resubmitted!.Id, result = "COMPLETED", comment = (string?)null, idempotencyKey = completeKey });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var result = (await completed.Content.ReadFromJsonAsync<CompletionConfirmationResponse>())!;
        Assert.Equal("COMPLETED", result.TransactionStatus); Assert.NotNull(result.ServiceHistoryId);
        var repeated = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/confirm-completion", new
        { completionRevisionId = resubmitted.Id, result = "COMPLETED", comment = (string?)null, idempotencyKey = completeKey });
        Assert.Equal(result.ConfirmationId, (await repeated.Content.ReadFromJsonAsync<CompletionConfirmationResponse>())!.ConfirmationId);

        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var transaction = await db.Transactions.SingleAsync(item => item.PublicId == transactionId);
        Assert.Equal("COMPLETED", transaction.StatusCode); Assert.NotNull(transaction.CompletedAt);
        Assert.Single(await db.ServiceHistoryEntries.Where(item => item.TransactionId == transaction.Id && item.EventTypeCode == "COMPLETION").ToListAsync());
        Assert.Single(await db.CustomerConfirmations.Where(item => item.TransactionId == transaction.Id && item.ResultCode == "COMPLETED").ToListAsync());
        Assert.Equal(2, await db.WorkCompletionRevisions.CountAsync(item => db.WorkCompletions.Any(c => c.Id == item.WorkCompletionId && c.TransactionId == transaction.Id)));
        var files = await db.Files.Where(item => item.PurposeCode == "COMPLETION_EVIDENCE").ToListAsync();
        Assert.Equal(2, files.Count);
        Assert.All(files, file => Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(file.StorageKey)), file.StorageKeyHash));
    }

    [Fact]
    public async Task ObjectAndRoleAuthorization_BlockOtherUsers()
    {
        using var provider = Client(); using var otherProvider = Client(); using var customer = Client(); using var otherCustomer = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(otherProvider, factory.AreaMismatchProviderCredential);
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(otherCustomer, factory.OtherCustomerCredential);
        var transactionId = await ArrangeTransaction(provider, customer);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync($"/api/v1/providers/me/transactions/{transactionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.GetAsync($"/api/v1/customers/me/transactions/{transactionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsync($"/api/v1/transactions/{transactionId}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync($"/api/v1/customers/me/transactions/{transactionId}")).StatusCode);
    }

    private async Task<Guid> ArrangeTransaction(HttpClient provider, HttpClient customer)
    {
        await provider.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
        await provider.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } } });
        var requestResponse = await customer.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId, administrativeAreaId = factory.Catalog.AreaId, title = "Work flow request",
            description = "Completion integration request", detailAddress = "Private", isUrgent = false, idempotencyKey = $"work-{Guid.NewGuid():N}",
            answers = new object[] { new { fieldId = factory.Catalog.FieldIds[0], value = "Detailed work flow answer with enough length." }, new { fieldId = factory.Catalog.FieldIds[1], value = DateTimeOffset.UtcNow.AddDays(2).ToString("O") }, new { fieldId = factory.Catalog.FieldIds[2], value = "주거" } },
        });
        var request = (await requestResponse.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        await customer.PostAsync($"/api/v1/requests/{request.Id}/publish", null);
        var quote = (await (await provider.PostAsJsonAsync($"/api/v1/requests/{request.Id}/quotes", new SaveQuoteRevisionInput(
            "Work quote", null, 10m, "one hour", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddMinutes(50), null,
            $"quote-{Guid.NewGuid():N}", [new QuoteItemInput("Work", null, 1, "job", 111m)]))).Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
        await provider.PostAsync($"/api/v1/quotes/{quote.Id}/submit", null);
        return (await (await customer.PostAsync($"/api/v1/quotes/{quote.Id}/accept", null)).Content.ReadFromJsonAsync<AcceptQuoteResponse>())!.TransactionId;
    }

    private static async Task<WorkCompletionRevisionResponse> SaveDraft(HttpClient client, Guid id, string key, string summary, decimal amount, string? reason) =>
        (await (await client.PostAsJsonAsync($"/api/v1/transactions/{id}/completions/drafts", new SaveCompletionDraftInput(summary, amount, reason, key))).Content.ReadFromJsonAsync<WorkCompletionRevisionResponse>())!;

    private static async Task<CompletionEvidenceResponse> Upload(HttpClient client, Guid id, string role, string name)
    {
        using var form = new MultipartFormDataContent(); using var content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png"); form.Add(new StringContent(role), "roleCode"); form.Add(new StringContent("test evidence"), "description"); form.Add(content, "file", name);
        var response = await client.PostAsync($"/api/v1/transactions/{id}/completion-evidence", form); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompletionEvidenceResponse>())!;
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
