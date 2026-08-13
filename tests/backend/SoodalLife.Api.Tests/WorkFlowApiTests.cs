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
        await ConfirmAppointment(customer, provider, transactionId);

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
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(before.DownloadUrl)).StatusCode);
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

    [Fact]
    public async Task Appointment_Proposal_Decision_ChangeDecision_AndHistory_AreMutual()
    {
        using var provider = Client(); using var customer = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var transactionId = await ArrangeTransaction(provider, customer);
        var start = DateTime.UtcNow.AddDays(3); var end = start.AddHours(2);
        var created = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-proposals", new
        { scheduledStartAt = start, scheduledEndAt = end, estimatedDurationMinutes = 120, memo = "Please call before arrival.", idempotencyKey = $"proposal-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var appointment = (await created.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        Assert.Equal("PROPOSED", appointment.Status); Assert.False(appointment.IsConfirmed);
        Assert.Equal(HttpStatusCode.OK, (await provider.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).StatusCode);
        var approvedResponse = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment/decision", new
        { decision = "APPROVE", reason = (string?)null, idempotencyKey = $"decision-{Guid.NewGuid():N}", rowVersion = appointment.RowVersion });
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        var approved = (await approvedResponse.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        Assert.True(approved.IsConfirmed); Assert.Equal(2, approved.Events.Count);
        var key = $"change-{Guid.NewGuid():N}";
        var change = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests", new
        { requestedStartAt = start.AddDays(1), requestedEndAt = end.AddDays(1), reason = "Customer schedule changed.", idempotencyKey = key });
        var value = (await change.Content.ReadFromJsonAsync<AppointmentChangeResponse>())!;
        Assert.Equal("REQUESTED", value.Status);
        var repeated = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests", new
        { requestedStartAt = start.AddDays(1), requestedEndAt = end.AddDays(1), reason = "Customer schedule changed.", idempotencyKey = key });
        Assert.Equal(value.Id, (await repeated.Content.ReadFromJsonAsync<AppointmentChangeResponse>())!.Id);
        var refreshed = (await (await customer.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        Assert.Equal("CONFIRMED", refreshed.Status); Assert.Single(refreshed.Changes); Assert.Equal("REQUESTED", refreshed.Changes[0].Status);
        var changeDecision = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests/{value.Id}/decision", new
        { decision = "APPROVE", note = "Agreed", idempotencyKey = $"change-decision-{Guid.NewGuid():N}", rowVersion = value.RowVersion });
        Assert.Equal(HttpStatusCode.OK, changeDecision.StatusCode);
        var changed = (await (await customer.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        Assert.Equal(start.AddDays(1).Date, changed.ScheduledStartAt.Date); Assert.Contains(changed.Events, x => x.EventType == "APPOINTMENT_CHANGED");
    }

    [Fact]
    public async Task Appointment_Access_IsLimitedToCustomerAndSelectedProvider()
    {
        using var provider = Client(); using var customer = Client(); using var otherProvider = Client(); using var otherCustomer = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(otherProvider, factory.AreaMismatchProviderCredential); await Login(otherCustomer, factory.OtherCustomerCredential);
        var transactionId = await ArrangeTransaction(provider, customer);
        var create = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-proposals", new
        { scheduledStartAt = DateTime.UtcNow.AddDays(2), scheduledEndAt = (DateTime?)null, estimatedDurationMinutes = (int?)null, memo = (string?)null, idempotencyKey = $"proposal-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests", new
        { requestedStartAt = DateTime.UtcNow.AddDays(4), requestedEndAt = (DateTime?)null, reason = "Not allowed", idempotencyKey = Guid.NewGuid().ToString("N") })).StatusCode);
    }

    [Fact]
    public async Task ProviderProposal_CustomerRejectAndApprove_AppointmentCancellation_PreservesTransaction()
    {
        using var provider = Client(); using var customer = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var transactionId = await ArrangeTransaction(provider, customer); var start = DateTime.UtcNow.AddDays(4);
        var proposedResponse = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-proposals", new { scheduledStartAt = start, scheduledEndAt = start.AddHours(2), estimatedDurationMinutes = 120, memo = "Provider proposal", idempotencyKey = $"provider-proposal-{Guid.NewGuid():N}" });
        var proposed = (await proposedResponse.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!; Assert.True(proposed.CanRespond == false);
        var customerView = (await (await customer.GetAsync($"/api/v1/transactions/{transactionId}/appointment")).Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!; Assert.True(customerView.CanRespond);
        var rejectedResponse = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment/decision", new { decision = "REJECT", reason = "Different day", idempotencyKey = $"reject-{Guid.NewGuid():N}", rowVersion = customerView.RowVersion });
        Assert.Equal("REJECTED", (await rejectedResponse.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!.Status);
        var secondResponse = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-proposals", new { scheduledStartAt = start.AddDays(1), scheduledEndAt = start.AddDays(1).AddHours(2), estimatedDurationMinutes = 120, memo = "Counter proposal", idempotencyKey = $"provider-proposal-{Guid.NewGuid():N}" });
        var second = (await secondResponse.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        var approved = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment/decision", new { decision = "APPROVE", reason = (string?)null, idempotencyKey = $"approve-{Guid.NewGuid():N}", rowVersion = second.RowVersion }); Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var cancellation = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests", new { requestedStartAt = (DateTime?)null, requestedEndAt = (DateTime?)null, reason = "No longer available", idempotencyKey = $"appointment-cancel-{Guid.NewGuid():N}", changeType = "CANCEL" });
        var cancellationRow = (await cancellation.Content.ReadFromJsonAsync<AppointmentChangeResponse>())!;
        var cancellationDecision = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-change-requests/{cancellationRow.Id}/decision", new { decision = "APPROVE", note = "Agreed", idempotencyKey = $"appointment-cancel-decision-{Guid.NewGuid():N}", rowVersion = cancellationRow.RowVersion }); Assert.Equal(HttpStatusCode.OK, cancellationDecision.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var tx = await db.Transactions.SingleAsync(x => x.PublicId == transactionId); Assert.Equal("CREATED", tx.StatusCode);
        var appointment = await db.TransactionAppointments.SingleAsync(x => x.TransactionId == tx.Id); Assert.Equal("CANCELLED", appointment.StatusCode); Assert.Contains(await db.TransactionAppointmentEvents.Where(x => x.TransactionAppointmentId == appointment.Id).Select(x => x.EventTypeCode).ToListAsync(), x => x == "APPOINTMENT_CANCELLED");
    }

    [Fact]
    public async Task TransactionCancellation_BeforeStart_RequiresCounterparty_AndDoesNotRestoreFee()
    {
        using var provider = Client(); using var customer = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var transactionId = await ArrangeTransaction(provider, customer); var requested = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancellation-requests", new { reason = "Schedule no longer works", idempotencyKey = $"cancel-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode); var row = (await requested.Content.ReadFromJsonAsync<TransactionCancellationResponse>())!; Assert.Equal("REQUESTED", row.Status); Assert.Equal("NOT_EVALUATED", row.FeeRestoreStatus);
        var self = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancellation-requests/{row.Id}/decision", new { decision = "APPROVE", note = (string?)null, idempotencyKey = $"self-{Guid.NewGuid():N}", rowVersion = row.RowVersion }); Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        var decided = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancellation-requests/{row.Id}/decision", new { decision = "APPROVE", note = "Agreed", idempotencyKey = $"approve-{Guid.NewGuid():N}", rowVersion = row.RowVersion }); Assert.Equal(HttpStatusCode.OK, decided.StatusCode);
        var providerDetail = (await (await provider.GetAsync($"/api/v1/providers/me/transactions/{transactionId}")).Content.ReadFromJsonAsync<WorkTransactionDetail>())!; Assert.Null(providerDetail.CustomerPhone); Assert.Null(providerDetail.DetailAddress);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var tx = await db.Transactions.SingleAsync(x => x.PublicId == transactionId); Assert.Equal("CANCELLED", tx.StatusCode); Assert.False(await db.FeeRestores.AnyAsync(x => x.FeeChargeId == db.FeeCharges.Where(f => f.TransactionId == tx.Id).Select(f => f.Id).FirstOrDefault()));
        Assert.True(await db.OutboxEvents.AnyAsync(x => x.AggregatePublicId == transactionId && x.EventType == "TRANSACTION_CANCELLED"));
    }

    [Fact]
    public async Task Cancellation_AfterWorkStart_RequiresAdminReview_AndCannotBeFinalizedByParty()
    {
        using var provider = Client(); using var customer = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var transactionId = await ArrangeTransaction(provider, customer); await ConfirmAppointment(customer, provider, transactionId); Assert.Equal(HttpStatusCode.OK, (await provider.PostAsync($"/api/v1/transactions/{transactionId}/start", null)).StatusCode);
        var response = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancellation-requests", new { reason = "Work must stop", idempotencyKey = $"cancel-{Guid.NewGuid():N}" }); var row = (await response.Content.ReadFromJsonAsync<TransactionCancellationResponse>())!; Assert.Equal("ADMIN_REVIEW_REQUIRED", row.Status);
        var decision = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancellation-requests/{row.Id}/decision", new { decision = "APPROVE", note = (string?)null, idempotencyKey = $"decision-{Guid.NewGuid():N}", rowVersion = row.RowVersion }); Assert.Equal(HttpStatusCode.Conflict, decision.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Equal("IN_PROGRESS", (await db.Transactions.SingleAsync(x => x.PublicId == transactionId)).StatusCode);
    }

    private async Task ConfirmAppointment(HttpClient customer, HttpClient provider, Guid transactionId)
    {
        var response = await customer.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment-proposals", new { scheduledStartAt = DateTime.UtcNow.AddDays(2), scheduledEndAt = (DateTime?)null, estimatedDurationMinutes = 60, memo = (string?)null, idempotencyKey = $"proposal-{Guid.NewGuid():N}" });
        var appointment = (await response.Content.ReadFromJsonAsync<TransactionAppointmentResponse>())!;
        var approved = await provider.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/appointment/decision", new { decision = "APPROVE", reason = (string?)null, idempotencyKey = $"decision-{Guid.NewGuid():N}", rowVersion = appointment.RowVersion }); Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
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
