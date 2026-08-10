using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AfterServiceDisputeApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanAccessAfterServiceAndDisputeLists()
    {
        using var client = Client();
        await Login(client, RoleCodes.Admin);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(AfterServicePath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(DisputePath)).StatusCode);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessAfterServiceOrDisputeApi(string role)
    {
        using var client = Client();
        await Login(client, role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(AfterServicePath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(DisputePath)).StatusCode);
    }

    [Fact]
    public async Task Reception_SnapshotsWarranty_AndAllowsOutOfWarrantyCase()
    {
        var inWarranty = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-5), 30);
        var outOfWarranty = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-60), 30);
        using var client = Client(); await Login(client, RoleCodes.Admin);

        var inside = await CreateAfterService(client, inWarranty.Id, "보증기간 내 누수 확인");
        var outside = await CreateAfterService(client, outOfWarranty.Id, "보증기간 외 작동 점검");

        Assert.True(inside.Warranty.IsWithinWarranty);
        Assert.False(outside.Warranty.IsWithinWarranty);
        Assert.Equal(DateOnly.FromDateTime(inWarranty.CompletedAt).AddDays(30), inside.Warranty.EndDate);
        Assert.Equal(inWarranty.Id, inside.Transaction.Id);
    }

    [Fact]
    public async Task AfterService_EnforcesFlow_PreservesMultipleVisits_AndDeduplicatesHistory()
    {
        var transaction = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-2), 30);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var item = await CreateAfterService(client, transaction.Id, "배수 상태 재점검");

        var invalid = await client.PostAsJsonAsync($"{AfterServicePath}/{item.Id}/status", new
        {
            TargetStatusCode = "IN_PROGRESS", IdempotencyKey = Key()
        });
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);

        item = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "PROVIDER_CONFIRMED", ProviderResponse = "현장 확인이 필요합니다.",
            VisitRequired = true, IdempotencyKey = Key()
        });
        item = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "VISIT_SCHEDULED", ScheduledAt = DateTime.UtcNow.AddDays(1), IdempotencyKey = Key()
        });
        item = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "IN_PROGRESS", Note = "현장 점검을 시작합니다.", IdempotencyKey = Key()
        });
        item = await PostAfterAction(client, item.Id, "VISIT", "배수구를 점검했습니다.", false);
        item = await PostAfterAction(client, item.Id, "REVISIT", "부품 교체 후 재방문했습니다.", true);
        var resolutionKey = Key();
        item = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "RESOLVED", ResolutionSummary = "부품 교체 후 정상 작동을 확인했습니다.",
            RecurrenceOccurred = true, IdempotencyKey = resolutionKey
        });
        var retry = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "RESOLVED", ResolutionSummary = "부품 교체 후 정상 작동을 확인했습니다.",
            RecurrenceOccurred = true, IdempotencyKey = resolutionKey
        });

        Assert.Equal("RESOLVED", retry.StatusCode);
        Assert.Contains(retry.Actions, value => value.ActionTypeCode == "VISIT");
        Assert.Contains(retry.Actions, value => value.ActionTypeCode == "REVISIT" && value.RecurrenceOccurred == true);
        Assert.Single(retry.ServiceHistory, value => value.EventTypeCode == "AFTER_SERVICE_COMPLETED");
    }

    [Fact]
    public async Task UnresolvedClosure_RequiresReason()
    {
        var transaction = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-3), 30);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var item = await CreateAfterService(client, transaction.Id, "간헐적 소음 확인");
        item = await ChangeAfterService(client, item.Id, new { TargetStatusCode = "PROVIDER_CONFIRMED", ProviderResponse = "원격 확인", VisitRequired = false, IdempotencyKey = Key() });
        item = await ChangeAfterService(client, item.Id, new { TargetStatusCode = "IN_PROGRESS", IdempotencyKey = Key() });

        var missing = await client.PostAsJsonAsync($"{AfterServicePath}/{item.Id}/status", new
        {
            TargetStatusCode = "UNRESOLVED_CLOSED", IdempotencyKey = Key()
        });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        item = await ChangeAfterService(client, item.Id, new
        {
            TargetStatusCode = "UNRESOLVED_CLOSED", UnresolvedReason = "증상이 재현되지 않아 고객과 협의 후 종료합니다.", IdempotencyKey = Key()
        });
        Assert.Equal("UNRESOLVED_CLOSED", item.StatusCode);
    }

    [Fact]
    public async Task AfterService_ConvertsToOneDispute_WithOriginalEvidenceAndNoWalletMutation()
    {
        var transaction = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-4), 30, true);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var item = await CreateAfterService(client, transaction.Id, "시공 결과 이견");
        var before = await FinancialCounts();

        var response = await client.PostAsJsonAsync($"{AfterServicePath}/{item.Id}/convert-to-dispute", new
        {
            Subject = "시공 결과 분쟁", Reason = "A/S 조정으로 합의되지 않았습니다.", IdempotencyKey = Key()
        });
        response.EnsureSuccessStatusCode();
        var dispute = (await response.Content.ReadFromJsonAsync<AdminDisputeDetail>())!;
        var duplicate = await client.PostAsJsonAsync($"{AfterServicePath}/{item.Id}/convert-to-dispute", new
        {
            Subject = "중복 전환", Reason = "중복 전환 확인", IdempotencyKey = Key()
        });

        Assert.Equal(item.Id, dispute.AfterServiceId);
        Assert.Contains(dispute.OriginalEvidence, value => value.SourceTypeCode == "REQUEST");
        Assert.Contains(dispute.OriginalEvidence, value => value.SourceTypeCode == "QUOTE");
        Assert.Contains(dispute.OriginalEvidence, value => value.SourceTypeCode == "COMPLETION_EVIDENCE");
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(before, await FinancialCounts());
    }

    [Fact]
    public async Task DirectDispute_SeparatesEvidence_EnforcesScope_AndRecordsResolutionHistory()
    {
        var transaction = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-6), 30, true);
        var other = await CreateCompletedTransaction(DateTime.UtcNow.AddDays(-7), 30, true);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var createdResponse = await client.PostAsJsonAsync(DisputePath, new
        {
            TransactionId = transaction.Id, ApplicantRoleCode = "CUSTOMER", Subject = "작업 범위 분쟁",
            Description = "견적 범위와 완료 결과를 함께 검토해 주세요.", IdempotencyKey = Key()
        });
        createdResponse.EnsureSuccessStatusCode();
        var dispute = (await createdResponse.Content.ReadFromJsonAsync<AdminDisputeDetail>())!;
        var originalCount = dispute.OriginalEvidence.Count;

        var added = await client.PostAsJsonAsync($"{DisputePath}/{dispute.Id}/evidence", new
        {
            FileId = transaction.EvidenceFileId, SourceTypeCode = "OTHER", Description = "현장 추가 확인 사진", IdempotencyKey = Key()
        });
        added.EnsureSuccessStatusCode();
        dispute = (await added.Content.ReadFromJsonAsync<AdminDisputeDetail>())!;
        Assert.Equal(originalCount, dispute.OriginalEvidence.Count);
        Assert.Contains(dispute.SubmittedEvidence, value => value.SourceTypeCode == "OTHER" && value.FileId == transaction.EvidenceFileId);

        var foreign = await client.PostAsJsonAsync($"{DisputePath}/{dispute.Id}/evidence", new
        {
            FileId = other.EvidenceFileId, SourceTypeCode = "OTHER", Description = "다른 거래 증빙", IdempotencyKey = Key()
        });
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

        dispute = await ChangeDispute(client, dispute.Id, "UNDER_REVIEW");
        var resolution = await client.PostAsJsonAsync($"{DisputePath}/{dispute.Id}/resolution", new
        {
            ResultSummary = "당사자 안내 완료", DecisionDetails = "원 견적과 완료자료를 기준으로 범위를 확인했습니다.",
            BasisText = "요청서, 채택 견적, 작업완료 증빙", FollowUpAction = "추가 이행 여부를 별도 확인합니다.", IdempotencyKey = Key()
        });
        resolution.EnsureSuccessStatusCode();
        dispute = (await resolution.Content.ReadFromJsonAsync<AdminDisputeDetail>())!;
        dispute = await ChangeDispute(client, dispute.Id, "CLOSED");

        Assert.Equal("CLOSED", dispute.StatusCode);
        Assert.Single(dispute.Resolutions);
        Assert.Contains(dispute.Actions, value => value.ActionTypeCode == "RESOLUTION");
        Assert.False(dispute.Financials.PaymentHoldSupported);
    }

    private async Task<TestTransaction> CreateCompletedTransaction(DateTime completedAt, short warrantyDays, bool addEvidence = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customer = await db.CustomerProfiles.FirstAsync();
        var provider = await db.ProviderProfiles.FirstAsync();
        var category = await db.ServiceCategories.SingleAsync(value => value.PublicId == factory.Catalog.ServiceId);
        var policy = await db.CategoryPolicies.FirstAsync(value => value.CategoryId == category.Id);
        var area = await db.AdministrativeAreas.FirstAsync();
        var now = DateTime.UtcNow;
        var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = category.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, Title = "A/S 검증용 고객 요청", StatusCode = "ACCEPTED", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now };
        db.ServiceRequests.Add(request); await db.SaveChangesAsync();
        var quote = new Quote { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, StatusCode = "ACCEPTED", SubmittedAt = now, AcceptedAt = now, CreatedAt = now, UpdatedAt = now };
        db.Quotes.Add(quote); await db.SaveChangesAsync();
        var revision = new QuoteRevision { QuoteId = quote.Id, RevisionNo = 1, Summary = "A/S 검증용 채택 견적", TotalAmount = 120000, CurrencyCode = "KRW", ValidUntil = now.AddDays(1), SubmittedAt = now, SubmittedByUserId = provider.UserId, IdempotencyKey = Key() };
        db.QuoteRevisions.Add(revision); await db.SaveChangesAsync();
        var transaction = new TransactionRecord { ServiceRequestId = request.Id, AcceptedQuoteRevisionId = revision.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, CategoryId = category.Id, StatusCode = "COMPLETED", AgreedAmount = 120000, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", WarrantyDaysSnapshot = warrantyDays, StartedAt = completedAt.AddHours(-2), CompletedAt = completedAt, CreatedAt = now, UpdatedAt = now };
        db.Transactions.Add(transaction); await db.SaveChangesAsync();
        Guid? fileId = null;
        if (addEvidence)
        {
            var completion = new WorkCompletion { TransactionId = transaction.Id, StatusCode = "CONFIRMED", LatestRevisionNo = 1, FirstSubmittedAt = completedAt, ConfirmedAt = completedAt, CreatedAt = now, UpdatedAt = now };
            db.WorkCompletions.Add(completion); await db.SaveChangesAsync();
            var completionRevision = new WorkCompletionRevision { WorkCompletionId = completion.Id, RevisionNo = 1, StatusCode = "CONFIRMED", WorkSummary = "작업 완료 확인", ProviderAttestationAt = completedAt, SubmittedAt = completedAt, SubmittedByUserId = provider.UserId, IdempotencyKey = Key() };
            db.WorkCompletionRevisions.Add(completionRevision); await db.SaveChangesAsync();
            var file = new StoredFile { PurposeCode = "COMPLETION_EVIDENCE", StorageContainer = "test", StorageKey = $"completion/{Guid.NewGuid():N}.jpg", StorageKeyHash = Guid.NewGuid().ToByteArray(), OriginalFileName = "작업완료사진.jpg", ContentType = "image/jpeg", SizeBytes = 1024, Sha256Hex = new string('a', 64), StatusCode = "ACTIVE", ActivatedAt = now, UploadedByUserId = provider.UserId, CreatedAt = now };
            db.Files.Add(file); await db.SaveChangesAsync();
            var role = await db.CompletionPhotoRoles.FirstAsync(value => value.Code == "AFTER");
            db.CompletionEvidenceFiles.Add(new CompletionEvidenceFile { CompletionRevisionId = completionRevision.Id, FileId = file.Id, PhotoRoleId = role.Id, DisplayOrder = 1, Description = "작업 완료 상태", CreatedAt = now, CreatedByUserId = provider.UserId });
            await db.SaveChangesAsync(); fileId = file.PublicId;
        }
        return new(transaction.PublicId, completedAt, fileId);
    }

    private static async Task<AdminAfterServiceDetail> CreateAfterService(HttpClient client, Guid transactionId, string subject)
    {
        var response = await client.PostAsJsonAsync(AfterServicePath, new { TransactionId = transactionId, ReporterRoleCode = "CUSTOMER", Subject = subject, SymptomDescription = "고객이 접수한 증상을 확인합니다.", RequestDetails = "원 거래와 완료 증빙을 함께 확인해 주세요.", IdempotencyKey = Key() });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AdminAfterServiceDetail>())!;
    }

    private static async Task<AdminAfterServiceDetail> ChangeAfterService(HttpClient client, Guid id, object request)
    { var response = await client.PostAsJsonAsync($"{AfterServicePath}/{id}/status", request); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AdminAfterServiceDetail>())!; }
    private static async Task<AdminAfterServiceDetail> PostAfterAction(HttpClient client, Guid id, string type, string result, bool recurrence)
    { var response = await client.PostAsJsonAsync($"{AfterServicePath}/{id}/actions", new { ActionTypeCode = type, PerformedAt = DateTime.UtcNow, VisitOccurred = true, ActionNote = "현장 방문 기록", ResultText = result, RecurrenceOccurred = recurrence, IdempotencyKey = Key() }); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AdminAfterServiceDetail>())!; }
    private static async Task<AdminDisputeDetail> ChangeDispute(HttpClient client, Guid id, string status)
    { var response = await client.PostAsJsonAsync($"{DisputePath}/{id}/status", new { TargetStatusCode = status, Note = "관리자 상태 변경", IdempotencyKey = Key() }); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AdminDisputeDetail>())!; }
    private async Task<(int Ledger, int FeeCharge, int FeeRestore)> FinancialCounts()
    { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); return (await db.WalletLedgerEntries.CountAsync(), await db.FeeCharges.CountAsync(), await db.FeeRestores.CountAsync()); }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private Task<HttpResponseMessage> Login(HttpClient client, string role) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials[role].LoginId, factory.Credentials[role].Password });
    private static string Key() => $"as-dispute-{Guid.NewGuid():N}";
    private const string AfterServicePath = "/api/v1/admin/after-services";
    private const string DisputePath = "/api/v1/admin/disputes";
    private sealed record TestTransaction(Guid Id, DateTime CompletedAt, Guid? EvidenceFileId);
}
