using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderAftercareApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task OnlyAssignedProvider_CanReadAfterService_AndClosedCaseHidesContact()
    {
        var open = await SeedAfterService("RECEIVED");
        var closed = await SeedAfterService("RESOLVED");
        using var assigned = Client(); await Login(assigned, factory.Credentials[RoleCodes.Provider]);
        using var other = Client(); await Login(other, factory.ServiceMismatchProviderCredential);

        Assert.Equal(HttpStatusCode.OK, (await assigned.GetAsync($"/api/v1/providers/me/after-services/{open}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/after-services/{open}")).StatusCode);
        var detail = await assigned.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{closed}");
        Assert.NotNull(detail); Assert.False(detail.ContactAvailable); Assert.Null(detail.CustomerPhone); Assert.Null(detail.DetailAddress);
    }

    [Fact]
    public async Task ProviderAfterService_ConfirmationIsIdempotent_AndCannotUseAdminTransition()
    {
        var id = await SeedAfterService("RECEIVED"); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var before = await client.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{id}"); var key = Key();
        var command = new { Response = "방문 점검을 진행하겠습니다.", VisitRequired = true, IdempotencyKey = key, RowVersion = before!.RowVersion };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/providers/me/after-services/{id}/confirm", command)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/providers/me/after-services/{id}/confirm", command)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Single(await db.AfterServiceActions.Where(x => x.AfterServiceCaseId == db.AfterServiceCases.Single(v => v.PublicId == id).Id && x.IdempotencyKey == key).ToListAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/admin/after-services/{id}/status", new { TargetStatusCode = "RESOLVED", IdempotencyKey = Key() })).StatusCode);
    }

    [Fact]
    public async Task AssignedProvider_CanScheduleRecordTreatmentAndReportCompletion()
    {
        var id = await SeedAfterService("RECEIVED"); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var item = await client.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{id}");
        item = await Post<ProviderAfterServiceDetail>(client, $"/api/v1/providers/me/after-services/{id}/confirm", new { Response = "접수 확인", VisitRequired = true, IdempotencyKey = Key(), item!.RowVersion });
        item = await Post<ProviderAfterServiceDetail>(client, $"/api/v1/providers/me/after-services/{id}/visit-schedule", new { ScheduledAt = DateTime.UtcNow.AddDays(1), Note = "오후 방문 제안", IdempotencyKey = Key(), item.RowVersion });
        item = await Post<ProviderAfterServiceDetail>(client, $"/api/v1/providers/me/after-services/{id}/actions", new { ActionType = "VISIT", Note = "현장 점검", PerformedAt = DateTime.UtcNow, VisitOccurred = true, Result = "부품 교체", IdempotencyKey = Key(), item.RowVersion });
        item = await Post<ProviderAfterServiceDetail>(client, $"/api/v1/providers/me/after-services/{id}/completion-report", new { Resolved = true, Summary = "정상 작동 확인", RecurrenceOccurred = false, IdempotencyKey = Key(), item.RowVersion });
        Assert.Equal("RESOLVED", item.Status); Assert.NotNull(item.CompletedAt); Assert.Contains(item.Timeline, x => x.ActionType == "VISIT_SCHEDULED"); Assert.Contains(item.Timeline, x => x.ActionType == "VISIT");
    }

    [Fact]
    public async Task CustomerEvidence_IsFailClosed_ButProviderOwnEvidenceRemainsVisible()
    {
        var id = await SeedAfterService("IN_PROGRESS", addFiles: true); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var detail = await client.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{id}");
        Assert.NotNull(detail); Assert.Single(detail.Evidence); Assert.Equal("PROVIDER_UPLOAD", detail.Evidence[0].SourceType);
        Assert.DoesNotContain(detail.Evidence, x => x.SourceType == "CUSTOMER_EVIDENCE");
    }

    [Fact]
    public async Task PrivacySafeCustomerEvidence_IsPublishedWithoutStorageMetadata()
    {
        var id = await SeedAfterService("IN_PROGRESS", addSafeCustomerFile: true); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var detail = await client.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{id}");
        var evidence = Assert.Single(detail!.Evidence); Assert.Equal("CUSTOMER_EVIDENCE", evidence.SourceType); Assert.Equal("PRIVACY_SAFE_ORIGINAL", evidence.PublicationMode);
        var json = await client.GetStringAsync($"/api/v1/providers/me/after-services/{id}"); Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("physical", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnlyConnectedProvider_CanRespondToDispute_WithoutResolvingIt()
    {
        var id = await SeedDispute(); using var assigned = Client(); await Login(assigned, factory.Credentials[RoleCodes.Provider]);
        using var other = Client(); await Login(other, factory.ServiceMismatchProviderCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/disputes/{id}")).StatusCode);
        var detail = await assigned.GetFromJsonAsync<ProviderDisputeDetail>($"/api/v1/providers/me/disputes/{id}"); var key = Key();
        var input = new { Statement = "요청 범위와 처리 증빙을 기준으로 소명합니다.", IdempotencyKey = key, RowVersion = detail!.RowVersion };
        Assert.Equal(HttpStatusCode.OK, (await assigned.PostAsJsonAsync($"/api/v1/providers/me/disputes/{id}/responses", input)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await assigned.PostAsJsonAsync($"/api/v1/providers/me/disputes/{id}/responses", input)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var row = await db.DisputeCases.SingleAsync(x => x.PublicId == id);
        Assert.Equal("OPEN", row.StatusCode); Assert.Single(await db.DisputeActions.Where(x => x.DisputeCaseId == row.Id && x.IdempotencyKey == key).ToListAsync());
    }

    [Fact]
    public async Task InvalidRowVersion_IsRejected()
    {
        var id = await SeedAfterService("RECEIVED"); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var response = await client.PostAsJsonAsync($"/api/v1/providers/me/after-services/{id}/confirm", new { Response = "확인", VisitRequired = false, IdempotencyKey = Key(), RowVersion = "not-base64" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaleRowVersion_IsRejectedWhenAnotherChangeWins()
    {
        var id = await SeedAfterService("RECEIVED"); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var detail = await client.GetFromJsonAsync<ProviderAfterServiceDetail>($"/api/v1/providers/me/after-services/{id}");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var row = await db.AfterServiceCases.SingleAsync(x => x.PublicId == id); row.RowVersion = [2]; await db.SaveChangesAsync(); }
        var response = await client.PostAsJsonAsync($"/api/v1/providers/me/after-services/{id}/confirm", new { Response = "확인", VisitRequired = false, IdempotencyKey = Key(), RowVersion = detail!.RowVersion });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ExistingCustomerAfterServiceAndDisputeEndpoints_StillRejectProviderScope()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/customers/me/after-services")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/customers/me/disputes")).StatusCode);
    }

    private async Task<Guid> SeedAfterService(string status, bool addFiles = false, bool addSafeCustomerFile = false)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await (from profile in db.ProviderProfiles join user in db.Users on profile.UserId equals user.Id where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var customer = await (from profile in db.CustomerProfiles join user in db.Users on profile.UserId equals user.Id where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile).SingleAsync();
        var now = DateTime.UtcNow; var item = new AfterServiceCase { CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, ReportedByUserId = customer.UserId,
            StatusCode = status, Subject = "공급자 A/S 검증", Description = "고객 접수 증상", ReceivedAt = now, CompletedAt = status == "RESOLVED" ? now : null,
            IdempotencyKey = Key(), CreatedAt = now, CreatedByUserId = customer.UserId, UpdatedAt = now, UpdatedByUserId = customer.UserId, RowVersion = [1] };
        db.AfterServiceCases.Add(item); await db.SaveChangesAsync();
        db.AfterServiceActions.Add(new AfterServiceAction { AfterServiceCaseId = item.Id, ToStatusCode = status, ActionTypeCode = status == "RESOLVED" ? "RESOLUTION" : status == "IN_PROGRESS" ? "TREATMENT" : "RECEIVED", OccurredAt = now, ActorUserId = customer.UserId, IdempotencyKey = Key() });
        if (addFiles)
        {
            var customerFile = File(customer.UserId, "customer.jpg", now); var providerFile = File(provider.UserId, "provider.jpg", now); db.Files.AddRange(customerFile, providerFile); await db.SaveChangesAsync();
            db.AfterServiceFiles.AddRange(new AfterServiceFile { AfterServiceCaseId = item.Id, FileId = customerFile.Id, CreatedAt = now, CreatedByUserId = customer.UserId }, new AfterServiceFile { AfterServiceCaseId = item.Id, FileId = providerFile.Id, CreatedAt = now, CreatedByUserId = provider.UserId });
        }
        if (addSafeCustomerFile)
        {
            var safe = File(customer.UserId, "safe.jpg", now); safe.MalwareScanStatusCode = "CLEAN"; safe.PrivacyInspectionStatusCode = "SAFE"; db.Files.Add(safe); await db.SaveChangesAsync();
            db.AfterServiceFiles.Add(new AfterServiceFile { AfterServiceCaseId = item.Id, FileId = safe.Id, CreatedAt = now, CreatedByUserId = customer.UserId });
        }
        await db.SaveChangesAsync(); return item.PublicId;
    }

    private async Task<Guid> SeedDispute()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await (from profile in db.ProviderProfiles join user in db.Users on profile.UserId equals user.Id where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var customer = await (from profile in db.CustomerProfiles join user in db.Users on profile.UserId equals user.Id where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile).SingleAsync(); var now = DateTime.UtcNow;
        var item = new DisputeCase { ApplicantUserId = customer.UserId, CounterpartyUserId = provider.UserId, Subject = "고객 분쟁 주장", Description = "고객이 제출한 주장 원문", StatusCode = "OPEN", ReceivedAt = now, LastActionAt = now, CreatedAt = now, CreatedByUserId = customer.UserId, UpdatedAt = now, UpdatedByUserId = customer.UserId, RowVersion = [1] };
        db.DisputeCases.Add(item); await db.SaveChangesAsync(); db.DisputeActions.Add(new DisputeAction { DisputeCaseId = item.Id, ActionTypeCode = "CREATED", ToStatusCode = "OPEN", ActionNote = "고객 접수", OccurredAt = now, ActorUserId = customer.UserId, IdempotencyKey = Key() }); await db.SaveChangesAsync(); return item.PublicId;
    }

    private static StoredFile File(long user, string name, DateTime now) => new() { PurposeCode = "AFTER_SERVICE", StorageContainer = "test", StorageKey = $"test/{Guid.NewGuid():N}.jpg", StorageKeyHash = Guid.NewGuid().ToByteArray(), OriginalFileName = name, ContentType = "image/jpeg", SizeBytes = 3, Sha256Hex = new string('a', 64), StatusCode = "ACTIVE", MalwareScanStatusCode = "NOT_INTEGRATED", PrivacyInspectionStatusCode = "NOT_INTEGRATED", SanitizationStatusCode = "NOT_INTEGRATED", UploadedByUserId = user, CreatedAt = now, ActivatedAt = now };
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private static async Task<T> Post<T>(HttpClient client, string path, object input) { var response = await client.PostAsJsonAsync(path, input); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>())!; }
    private static string Key() => $"provider-aftercare-{Guid.NewGuid():N}";
}
