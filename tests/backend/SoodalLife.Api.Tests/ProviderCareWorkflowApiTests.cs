using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderCareWorkflowApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task ProviderCare_StartRejectsStaleRowVersion()
    {
        var flow = await CreateFlow();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var visit = await db.SubscriptionVisitSchedules.FirstAsync(x => x.SubscriptionContractId == db.SubscriptionContracts.Where(c => c.PublicId == flow.Contract.Id).Select(c => c.Id).Single() && x.StatusCode == "SCHEDULED");
        visit.RowVersion = [1, 2, 3, 4]; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var before = (await flow.Provider.GetFromJsonAsync<List<ProviderCareVisitListItem>>("/api/v1/providers/me/care/visits?filter=UPCOMING"))!.Single(x => x.Id == visit.PublicId);
        var changed = await db.SubscriptionVisitSchedules.SingleAsync(x => x.Id == visit.Id); changed.RowVersion = [5, 6, 7, 8]; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var response = await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/care/visits/{visit.PublicId}/start", new { idempotencyKey = $"stale-{Guid.NewGuid():N}", rowVersion = before.RowVersion });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProviderCare_EligibilityOwnershipSelectionAndPrivacy_AreEnforced()
    {
        await EnableSubscription();
        using var customer = Client(); using var provider = Client(); using var other = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(other, factory.AreaMismatchProviderCredential);
        var request = await CreateRequest(customer);

        var open = await provider.GetFromJsonAsync<List<ProviderCareRequestItem>>("/api/v1/providers/me/care/requests/open");
        Assert.Contains(open!, item => item.Id == request.Id && !item.HasApplied);
        Assert.DoesNotContain((await other.GetFromJsonAsync<List<ProviderCareRequestItem>>("/api/v1/providers/me/care/requests/open"))!, item => item.Id == request.Id);
        var openJson = await (await provider.GetAsync("/api/v1/providers/me/care/requests/open")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("상세주소", openJson); Assert.DoesNotContain("phone", openJson, StringComparison.OrdinalIgnoreCase);

        var application = await Apply(provider, request.Id);
        var applications = await provider.GetFromJsonAsync<List<ProviderCareApplicationItem>>("/api/v1/providers/me/care/applications");
        Assert.Contains(applications!, item => item.Id == application.Id && item.StatusCode == "SUBMITTED");
        Assert.DoesNotContain((await other.GetFromJsonAsync<List<ProviderCareApplicationItem>>("/api/v1/providers/me/care/applications"))!, item => item.Id == application.Id);
        var selected = await customer.PostAsJsonAsync($"/api/v1/subscriptions/requests/{request.Id}/selection", new { applicationId = application.Id, idempotencyKey = $"select-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, selected.StatusCode); var contract = (await selected.Content.ReadFromJsonAsync<SubscriptionContractResponse>())!;

        var contractDetail = await provider.GetFromJsonAsync<ProviderCareContractDetail>($"/api/v1/providers/me/care/contracts/{contract.Id}");
        Assert.False(contractDetail!.ContactAvailable); Assert.Null(contractDetail.CustomerPhone); Assert.Null(contractDetail.DetailAddress);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/care/contracts/{contract.Id}")).StatusCode);
        var contractJson = await (await provider.GetAsync($"/api/v1/providers/me/care/contracts/{contract.Id}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("wallet", contractJson, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("netAmount", contractJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", contractJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProviderCare_MatchingRequestRemainsVisibleWhileServiceApprovalNeedsAction()
    {
        await EnableSubscription();
        using var customer = Client(); using var provider = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var request = await CreateRequest(customer);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var providerId = await db.ProviderProfiles.Where(x => x.UserId == db.Users
            .Where(user => user.NormalizedLoginId == factory.Credentials[RoleCodes.Provider].LoginId.ToUpperInvariant())
            .Select(user => user.Id).Single()).Select(x => x.Id).SingleAsync();
        var approval = await (from service in db.ProviderServiceCategories
                              join item in db.ProviderServiceApprovals on service.Id equals item.ProviderServiceCategoryId
                              where service.ProviderProfileId == providerId && service.CategoryId == db.ServiceCategories
                                  .Where(category => category.PublicId == factory.Catalog.ServiceId).Select(category => category.Id).Single()
                              select item).SingleAsync();
        var originalStatus = approval.ApprovalStatusCode;
        try
        {
            approval.ApprovalStatusCode = "PENDING";
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var visible = (await provider.GetFromJsonAsync<List<ProviderCareRequestItem>>("/api/v1/providers/me/care/requests/open"))!
                .Single(item => item.Id == request.Id);
            Assert.False(visible.CanApply);
            Assert.Equal("SERVICE_NOT_APPROVED", visible.EligibilityReasonCode);
            Assert.Equal("이 서비스의 승인이 완료되면 제안할 수 있습니다.", visible.EligibilityReason);
            Assert.Equal(HttpStatusCode.Forbidden, (await provider.PostAsJsonAsync(
                $"/api/v1/providers/me/care/requests/{request.Id}/applications",
                new { proposedScopeText = "정기 방문", proposedMonthlyAmount = 120000m, proposedVisitAmount = 30000m,
                    availableScheduleText = "평일 오전", idempotencyKey = $"blocked-{Guid.NewGuid():N}" })).StatusCode);
        }
        finally
        {
            approval = await db.ProviderServiceApprovals.SingleAsync(x => x.Id == approval.Id);
            approval.ApprovalStatusCode = originalStatus;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ProviderCare_StartUploadComplete_UsesConcurrencyIdempotencyAndPreservesProtectedDomains()
    {
        var flow = await CreateFlow(); using var other = Client(); await Login(other, factory.AreaMismatchProviderCredential);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var visit = (await flow.Provider.GetFromJsonAsync<List<ProviderCareVisitListItem>>("/api/v1/providers/me/care/visits?filter=UPCOMING"))!.First(item => item.ContractId == flow.Contract.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/care/visits/{visit.Id}")).StatusCode);
        var walletBefore = await db.ProviderWallets.AsNoTracking().Select(x => new { x.Id, x.AvailableBalance, x.ReservedBalance }).ToListAsync();
        var ledgerBefore = await db.WalletLedgerEntries.CountAsync(); var feeBefore = await db.FeeCharges.CountAsync(); var trustBefore = await db.TrustScoreEvents.CountAsync();
        var startKey = $"provider-care-start-{Guid.NewGuid():N}";
        var start = await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/care/visits/{visit.Id}/start", new { idempotencyKey = startKey, rowVersion = visit.RowVersion });
        Assert.True(start.StatusCode == HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/care/visits/{visit.Id}/start", new { idempotencyKey = startKey, rowVersion = visit.RowVersion })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/care/visits/{visit.Id}/start", new { idempotencyKey = $"second-{Guid.NewGuid():N}", rowVersion = visit.RowVersion })).StatusCode);
        var started = await start.Content.ReadFromJsonAsync<ProviderCareVisitDetail>(); Assert.Equal("IN_PROGRESS", started!.Visit.StatusCode); Assert.NotNull(started.WorkStartedAt);

        var ids = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            using var form = new MultipartFormDataContent(); using var bytes = new ByteArrayContent(TestFileSamples.ValidJpeg()); bytes.Headers.ContentType = new("image/jpeg"); form.Add(bytes, "file", $"evidence-{i}.jpg");
            var upload = await flow.Provider.PostAsync($"/api/v1/providers/me/care/visits/{visit.Id}/files", form); Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            ids.Add((await upload.Content.ReadFromJsonAsync<ProviderCareUploadResponse>())!.FileId);
        }
        using (var invalid = new MultipartFormDataContent()) { using var bytes = new ByteArrayContent([1, 2, 3]); bytes.Headers.ContentType = new("image/jpeg"); invalid.Add(bytes, "file", "fake.jpg"); Assert.Equal(HttpStatusCode.BadRequest, (await flow.Provider.PostAsync($"/api/v1/providers/me/care/visits/{visit.Id}/files", invalid)).StatusCode); }
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/care/visits/{visit.Id}/files/{ids[0]}")).StatusCode);

        var current = await flow.Provider.GetFromJsonAsync<ProviderCareVisitDetail>($"/api/v1/providers/me/care/visits/{visit.Id}");
        var complete = await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/care/visits/{visit.Id}/completion", new { verificationMethodCode = "AUTH_CODE", verificationResultCode = "VERIFIED", checklistJson = "{\"basicCheck\":true}", completionNote = "정기 방문 완료", fileIds = ids, idempotencyKey = $"complete-{Guid.NewGuid():N}", rowVersion = current!.Visit.RowVersion, gpsEvidence = (string?)null, possessionEvidence = (string?)null });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/subscriptions/visits/{visit.Id}/confirmation", new { idempotencyKey = $"confirm-{Guid.NewGuid():N}", rowVersion = (string?)null })).StatusCode);
        var closed = await flow.Provider.GetFromJsonAsync<ProviderCareVisitDetail>($"/api/v1/providers/me/care/visits/{visit.Id}"); Assert.False(closed!.ContactAvailable); Assert.Null(closed.CustomerPhone); Assert.Null(closed.DetailAddress);
        Assert.All(closed.Files, file => { Assert.Equal("NOT_INTEGRATED", file.MalwareScanStatus); Assert.Equal("NOT_INTEGRATED", file.PrivacyInspectionStatus); Assert.DoesNotContain("storage", file.DownloadUrl, StringComparison.OrdinalIgnoreCase); });

        db.ChangeTracker.Clear(); Assert.Contains(await db.SubscriptionEvents.AsNoTracking().ToListAsync(), x => x.SubscriptionVisitScheduleId != null && x.EventTypeCode == "VISIT_STARTED" && x.IdempotencyKey == startKey);
        Assert.Contains(await db.AuditLogs.AsNoTracking().ToListAsync(), x => x.EntityPublicId == visit.Id && x.ActionCode == "SUBSCRIPTION_VISIT_STARTED");
        Assert.Equal(walletBefore, await db.ProviderWallets.AsNoTracking().Select(x => new { x.Id, x.AvailableBalance, x.ReservedBalance }).ToListAsync()); Assert.Equal(ledgerBefore, await db.WalletLedgerEntries.CountAsync()); Assert.Equal(feeBefore, await db.FeeCharges.CountAsync()); Assert.Equal(trustBefore, await db.TrustScoreEvents.CountAsync());
    }

    private async Task<Flow> CreateFlow()
    {
        await EnableSubscription(); var customer = Client(); var provider = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var request = await CreateRequest(customer); var application = await Apply(provider, request.Id);
        var response = await customer.PostAsJsonAsync($"/api/v1/subscriptions/requests/{request.Id}/selection", new { applicationId = application.Id, idempotencyKey = $"select-{Guid.NewGuid():N}" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending=(await response.Content.ReadFromJsonAsync<SubscriptionContractResponse>())!;using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);var payment=(await admin.GetFromJsonAsync<List<SubscriptionPaymentResponse>>("/api/v1/admin/subscription-accounting/payments"))!.First(x=>x.ContractId==pending.Id&&x.StatusCode=="REQUESTED");var confirmed=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payments/{payment.Id}/development-confirmation",new{idempotencyKey=$"confirm-provider-care-{Guid.NewGuid():N}",externalPaymentReference="TEST",rowVersion=payment.RowVersion});Assert.Equal(HttpStatusCode.OK,confirmed.StatusCode);var active=(await admin.GetFromJsonAsync<List<SubscriptionContractResponse>>("/api/v1/admin/subscriptions/contracts"))!.Single(x=>x.Id==pending.Id);return new(customer, provider, active);
    }
    private async Task<SubscriptionRequestResponse> CreateRequest(HttpClient customer)
    {
        var response = await customer.PostAsJsonAsync("/api/v1/subscriptions/requests", new { serviceCategoryId = factory.Catalog.ServiceId, careProductId = (Guid?)null, administrativeAreaId = factory.Catalog.AreaId, requestTypeCode = "CUSTOM", requestedScopeText = "정기 생활 점검", preferredStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), detailAddress = "서울 상세주소 101호", idempotencyKey = $"request-{Guid.NewGuid():N}", recurrence = new { frequencyTypeCode = "BIWEEKLY", intervalValue = 2, visitsPerPeriod = 2, weekdays = new[] { 1, 4 }, preferredTimeFrom = new TimeOnly(10, 0), preferredTimeTo = new TimeOnly(11, 0), expectedDurationMinutes = 60, startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), endDate = (DateOnly?)null } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<SubscriptionRequestResponse>())!;
    }
    private async Task<SubscriptionApplicationResponse> Apply(HttpClient provider, Guid requestId)
    {
        var response = await provider.PostAsJsonAsync($"/api/v1/providers/me/care/requests/{requestId}/applications", new { proposedScopeText = "정기 방문", proposedMonthlyAmount = 120000m, proposedVisitAmount = 30000m, availableScheduleText = "격주 평일 오전", idempotencyKey = $"apply-{Guid.NewGuid():N}" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<SubscriptionApplicationResponse>())!;
    }
    private async Task EnableSubscription()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId); var policy = await db.CategoryOperationPolicies.SingleAsync(x => x.CategoryId == category.Id); policy.SubscriptionOptionText = "허용"; await db.SaveChangesAsync();
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static async Task Login(HttpClient client, TestCredential credential) => Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);
    private sealed record Flow(HttpClient Customer, HttpClient Provider, SubscriptionContractResponse Contract);
}
