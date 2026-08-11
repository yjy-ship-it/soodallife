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

public sealed class AdminTrustApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanAccessTrustListAndDetail_WithMaskedListPersonalData()
    {
        var providerId = await PrepareProvider("수달 안심설비", null);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var list = await client.GetFromJsonAsync<AdminTrustListResponse>(BasePath);
        Assert.NotNull(list); var item = Assert.Single(list.Items, value => value.ProviderId == providerId);
        Assert.Equal("신규·평가중", item.GradeLabel); Assert.Equal("NEW_OR_EVALUATING", item.EvaluationStatusCode);
        var raw = await (await client.GetAsync(BasePath)).Content.ReadAsStringAsync(); Assert.DoesNotContain("01024681357", raw); Assert.DoesNotContain("trust@sudal.example.kr", raw);
        var detail = await client.GetFromJsonAsync<AdminTrustDetailResponse>($"{BasePath}/{providerId}");
        Assert.NotNull(detail); Assert.Equal("010-****-1357", detail.Provider.Phone); Assert.Equal("신규·평가중", detail.Trust.StatusNotice);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessTrustManagement(string role)
    {
        using var client = Client(); await Login(client, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath)).StatusCode);
    }

    [Fact]
    public async Task LegacyScore_IsPreservedAndClearlyMarkedAsUnknownPolicy()
    {
        var providerId = await PrepareProvider("수달 주거관리", 79.5m);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var detail = await client.GetFromJsonAsync<AdminTrustDetailResponse>($"{BasePath}/{providerId}");
        Assert.NotNull(detail); Assert.Equal(79.5m, detail.Trust.Score); Assert.Equal("믿음수달", detail.Trust.GradeLabel);
        Assert.Equal("LEGACY_UNKNOWN_POLICY", detail.Trust.EvaluationStatusCode); Assert.Equal("기존 점수 / 산정정책 확인필요", detail.Trust.StatusNotice);
    }

    [Fact]
    public async Task Admin_CanSearchAndFilterTrustEvidenceOnServer()
    {
        var providerId = await PrepareProvider("수달 필터검증", 65m); await AddEvidenceAndOperationalHistory(providerId);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var query = $"{BasePath}?search={Uri.EscapeDataString("01024681357")}&evaluationStatus=LEGACY_UNKNOWN_POLICY&grade=SAFE&approvalStatus=APPROVED&activityStatus=ACTIVE&hasExpiredEvidence=false&hasAfterService=true&hasDispute=true";
        var result = await client.GetFromJsonAsync<AdminTrustListResponse>(query);
        Assert.NotNull(result); Assert.Contains(result.Items, value => value.ProviderId == providerId);
    }

    [Theory]
    [InlineData(null, "신규·평가중")]
    [InlineData("0", "새싹수달")]
    [InlineData("59.9999", "새싹수달")]
    [InlineData("60", "안심수달")]
    [InlineData("69.9999", "안심수달")]
    [InlineData("70", "믿음수달")]
    [InlineData("79.9999", "믿음수달")]
    [InlineData("80", "우수수달")]
    [InlineData("89.9999", "우수수달")]
    [InlineData("90", "명예수달")]
    [InlineData("100", "명예수달")]
    public void GradeBoundaries_FollowConfirmedDesign(string? rawScore, string expected)
    {
        decimal? score = rawScore is null ? null : decimal.Parse(rawScore, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, AdminTrustService.GradeLabel(score));
    }

    [Fact]
    public async Task Detail_OnlyReturnsSelectedProvidersPerformanceEvidenceAfterServiceAndDispute()
    {
        var providerId = await PrepareProvider("수달 공간관리", null); await AddEvidenceAndOperationalHistory(providerId);
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var detail = await client.GetFromJsonAsync<AdminTrustDetailResponse>($"{BasePath}/{providerId}");
        Assert.NotNull(detail); Assert.Single(detail.Documents); Assert.Single(detail.AfterServices); Assert.Single(detail.Disputes);
        Assert.Equal(1, detail.Performance.SubmittedQuoteCount); Assert.Equal(1, detail.Performance.AcceptedQuoteCount);
        Assert.Equal(1, detail.Performance.CompletedTransactionCount); Assert.Contains("책임판정 구조", detail.Disputes[0].ResponsibilityNotice);
    }

    [Fact]
    public async Task AddingOperationalHistory_DoesNotCalculateScoreOrCreateTrustEvent()
    {
        var providerId = await PrepareProvider("수달 안전관리", null); await AddEvidenceAndOperationalHistory(providerId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.SingleAsync(value => value.PublicId == providerId); var current = await db.ProviderTrustScoreCurrent.SingleAsync(value => value.ProviderProfileId == provider.Id);
        Assert.Null(provider.TrustScore); Assert.Null(current.Score); Assert.Equal("NEW_OR_EVALUATING", current.EvaluationStatusCode);
        Assert.False(await db.TrustScoreEvents.AnyAsync(value => value.ProviderProfileId == provider.Id));
    }

    [Fact]
    public async Task TrustScoreEvent_IsAppendOnlyAtApplicationLevel()
    {
        var providerId = await PrepareProvider("수달 이력검증", null);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var provider = await db.ProviderProfiles.SingleAsync(value => value.PublicId == providerId);
        var item = new TrustScoreEvent { ProviderProfileId = provider.Id, EventTypeCode = "POLICY_DECISION", SourceTypeCode = "ADMIN_REVIEW", IdempotencyKey = $"trust-test-{Guid.NewGuid():N}", ReasonText = "append-only 검증", OccurredAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        db.TrustScoreEvents.Add(item); await db.SaveChangesAsync(); item.ReasonText = "기존 이력 수정 시도";
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync()); Assert.Contains("수정하거나 삭제할 수 없습니다", error.Message);
    }

    [Fact]
    public void TrustScoreEvent_IdempotencyKeyHasUniqueIndex()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var entity = db.Model.FindEntityType(typeof(TrustScoreEvent)); Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Count == 1 && index.Properties[0].Name == nameof(TrustScoreEvent.IdempotencyKey));
    }

    private async Task<Guid> PrepareProvider(string businessName, decimal? legacyScore)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(value => value.LoginId == factory.Credentials[RoleCodes.Provider].LoginId); user.Phone = "01024681357"; user.Email = "trust@sudal.example.kr";
        var provider = await db.ProviderProfiles.SingleAsync(value => value.UserId == user.Id); provider.BusinessName = businessName; provider.BusinessRegistrationNo = "1234567890"; provider.TrustScore = legacyScore;
        var current = await db.ProviderTrustScoreCurrent.SingleOrDefaultAsync(value => value.ProviderProfileId == provider.Id);
        if (current is null) db.ProviderTrustScoreCurrent.Add(new ProviderTrustScoreCurrent { ProviderProfileId = provider.Id, Score = legacyScore, EvaluationStatusCode = legacyScore is null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY", SourceTypeCode = "TEST_EXISTING_PROFILE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        else { current.Score = legacyScore; current.GradeCode = null; current.EvaluationStatusCode = legacyScore is null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY"; }
        await db.SaveChangesAsync(); return provider.PublicId;
    }

    private async Task AddEvidenceAndOperationalHistory(Guid providerId)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.SingleAsync(value => value.PublicId == providerId); if (await db.AfterServiceCases.AnyAsync(value => value.ProviderProfileId == provider.Id)) return;
        var customer = await db.CustomerProfiles.FirstAsync(); var category = await db.ServiceCategories.SingleAsync(value => value.PublicId == factory.Catalog.ServiceId); var policy = await db.CategoryPolicies.FirstAsync(value => value.CategoryId == category.Id); var area = await db.AdministrativeAreas.FirstAsync(); var now = DateTime.UtcNow;
        var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = category.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, Title = "신뢰도 근거 조회용 요청", StatusCode = "ACCEPTED", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now }; db.ServiceRequests.Add(request); await db.SaveChangesAsync();
        var quote = new Quote { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, StatusCode = "ACCEPTED", SubmittedAt = now, AcceptedAt = now, CreatedAt = now, UpdatedAt = now }; db.Quotes.Add(quote); await db.SaveChangesAsync();
        var revision = new QuoteRevision { QuoteId = quote.Id, RevisionNo = 1, Summary = "배관 점검", TotalAmount = 90000, CurrencyCode = "KRW", ValidUntil = now.AddDays(1), SubmittedAt = now, SubmittedByUserId = provider.UserId, IdempotencyKey = $"trust-revision-{Guid.NewGuid():N}" }; db.QuoteRevisions.Add(revision); await db.SaveChangesAsync();
        var transaction = new TransactionRecord { ServiceRequestId = request.Id, AcceptedQuoteRevisionId = revision.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, CategoryId = category.Id, StatusCode = "COMPLETED", AgreedAmount = 90000, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", CompletedAt = now, CreatedAt = now, UpdatedAt = now }; db.Transactions.Add(transaction); await db.SaveChangesAsync();
        var file = new StoredFile { PurposeCode = "PROVIDER_DOCUMENT", StorageContainer = "테스트", StorageKey = $"trust/{Guid.NewGuid():N}.pdf", StorageKeyHash = Guid.NewGuid().ToByteArray(), OriginalFileName = "사업자등록증.pdf", ContentType = "application/pdf", SizeBytes = 100, Sha256Hex = new string('a',64), StatusCode = "ACTIVE", CreatedAt = now }; db.Files.Add(file); await db.SaveChangesAsync();
        db.ProviderDocuments.Add(new ProviderDocument { ProviderProfileId = provider.Id, FileId = file.Id, DocumentTypeCode = "BUSINESS_REGISTRATION", VerificationStatusCode = "APPROVED", VerifiedAt = now, ExpiresAt = DateOnly.FromDateTime(now.AddDays(30)), CreatedAt = now, UpdatedAt = now });
        var afterService = new AfterServiceCase { TransactionId = transaction.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, StatusCode = "RECEIVED", Subject = "작업 상태 확인", Description = "처리상태 조회 검증", ReceivedAt = now, IdempotencyKey = $"trust-as-{Guid.NewGuid():N}", CreatedAt = now, UpdatedAt = now }; db.AfterServiceCases.Add(afterService); await db.SaveChangesAsync();
        db.DisputeCases.Add(new DisputeCase { TransactionId = transaction.Id, AfterServiceCaseId = afterService.Id, ApplicantUserId = customer.UserId, CounterpartyUserId = provider.UserId, Subject = "처리범위 확인", Description = "책임판정 전 조회 검증", StatusCode = "OPEN", ReceivedAt = now, CreatedAt = now, UpdatedAt = now }); await db.SaveChangesAsync();
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private Task<HttpResponseMessage> Login(HttpClient client, string role) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials[role].LoginId, factory.Credentials[role].Password });
    private const string BasePath = "/api/v1/admin/trust";
}
