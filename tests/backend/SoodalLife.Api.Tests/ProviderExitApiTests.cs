using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderExitApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task ProviderRoleExit_ZeroWallet_CanBeCompletedWhileCustomerRoleIsPreserved()
    {
        var account = await SeedProvider(true);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "PROVIDER_ROLE_EXIT");
        Assert.Equal("READY_TO_COMPLETE", created.Status); Assert.True(created.Readiness.CanComplete);
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ProviderExitService>().RefreshActiveAsync(default);
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var providerRoleId = await db.Roles.Where(x => x.Code == RoleCodes.Provider).Select(x => x.Id).SingleAsync();
        var customerRoleId = await db.Roles.Where(x => x.Code == RoleCodes.Customer).Select(x => x.Id).SingleAsync();
        Assert.NotNull(await db.UserRoles.Where(x => x.UserId == account.UserId && x.RoleId == providerRoleId).Select(x => x.RevokedAt).SingleAsync());
        Assert.Null(await db.UserRoles.Where(x => x.UserId == account.UserId && x.RoleId == customerRoleId).Select(x => x.RevokedAt).SingleAsync());
        Assert.Equal("INACTIVE", await db.ProviderProfiles.Where(x => x.Id == account.ProviderId).Select(x => x.ActivityStatusCode).SingleAsync());
        Assert.Equal("CLOSED", await db.ProviderWallets.Where(x => x.ProviderProfileId == account.ProviderId).Select(x => x.StatusCode).SingleAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, (await provider.GetAsync("/api/v1/providers/me")).StatusCode);
        using var customer = Client(); await Login(customer, account.Credential);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/customers/me/my-soodal/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/providers/me")).StatusCode);
    }

    [Fact]
    public async Task AvailableBalance_CreatesLinkedRefundWithoutChangingBalanceOrLedger()
    {
        var account = await SeedProvider(false, 5000);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "PROVIDER_ROLE_EXIT");
        Assert.Equal("REFUND_REQUIRED", created.Status); Assert.NotNull(created.RefundRequestId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Equal(5000, await db.ProviderWallets.Where(x => x.ProviderProfileId == account.ProviderId).Select(x => x.AvailableBalance).SingleAsync());
        Assert.Empty(await db.WalletLedgerEntries.Where(x => x.WalletId == account.WalletId).ToListAsync());
        Assert.Equal("REQUESTED", await db.WalletRefundRequests.Where(x => x.PublicId == created.RefundRequestId).Select(x => x.StatusCode).SingleAsync());
    }

    [Fact]
    public async Task ReservedBalance_BlocksCompletionAndIsNotMoved()
    {
        var account = await SeedProvider(false, 0, 1000);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "PROVIDER_ROLE_EXIT");
        Assert.Equal("BLOCKED_BY_ACTIVE_WORK", created.Status);
        Assert.Contains(created.Readiness.Blockers, x => x.Code == "WALLET_RESERVED");
        created = created with { RowVersion = await SetRowVersion(created.Id) };
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/provider-exits/{created.Id}/complete", new { reason = "완료 시도", rowVersion = created.RowVersion })).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Equal(1000, await db.ProviderWallets.Where(x => x.Id == account.WalletId).Select(x => x.ReservedBalance).SingleAsync());
    }

    [Fact]
    public async Task ActiveTransactionAndOpenDispute_BlockButCompletedHistoryDoesNot()
    {
        var account = await SeedProvider(false);
        await SeedTransactionsAndDispute(account);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "PROVIDER_ROLE_EXIT");
        Assert.Equal("BLOCKED_BY_ACTIVE_WORK", created.Status);
        Assert.Contains(created.Readiness.Blockers, x => x.Code == "GENERAL_TRANSACTION" && x.Count == 1);
        Assert.Contains(created.Readiness.Blockers, x => x.Code == "DISPUTE" && x.Count == 1);
    }

    [Fact]
    public async Task OtherProviderCannotReadOrCancelExitAndDuplicateKeyIsIdempotent()
    {
        var first = await SeedProvider(false); var other = await SeedProvider(false);
        using var firstClient = Client(); await Login(firstClient, first.Credential);
        var key = $"exit-{Guid.NewGuid():N}";
        var a = await firstClient.PostAsJsonAsync(Path, new { requestType = "PROVIDER_ROLE_EXIT", reason = "활동 종료", idempotencyKey = key });
        var b = await firstClient.PostAsJsonAsync(Path, new { requestType = "PROVIDER_ROLE_EXIT", reason = "활동 종료", idempotencyKey = key });
        var firstResult = await a.Content.ReadFromJsonAsync<ProviderExitRequestResponse>(); var secondResult = await b.Content.ReadFromJsonAsync<ProviderExitRequestResponse>();
        Assert.Equal(firstResult!.Id, secondResult!.Id);
        using var otherClient = Client(); await Login(otherClient, other.Credential);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PostAsJsonAsync($"{Path}/{firstResult.Id}/cancel", new { reason = "타인 요청", rowVersion = "AQ==" })).StatusCode);
    }

    [Fact]
    public async Task ProviderCanCancelRequestWithoutDeletingBusinessData()
    {
        var account = await SeedProvider(false);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "ACCOUNT_WITHDRAWAL");
        created = created with { RowVersion = await SetRowVersion(created.Id) };
        var response = await provider.PostAsJsonAsync($"{Path}/{created.Id}/cancel", new { reason = "계속 이용", rowVersion = created.RowVersion });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELLED", (await response.Content.ReadFromJsonAsync<ProviderExitRequestResponse>())!.Status);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.True(await db.Users.AnyAsync(x => x.Id == account.UserId)); Assert.True(await db.ProviderProfiles.AnyAsync(x => x.Id == account.ProviderId));
    }

    [Fact]
    public async Task AdminCanInspectRecheckAndRejectWithAudit()
    {
        var account = await SeedProvider(false, reserved: 1000);
        using var provider = Client(); await Login(provider, account.Credential);
        var created = await Request(provider, "PROVIDER_ROLE_EXIT");
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/provider-exits")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/admin/provider-exits/{created.Id}")).StatusCode);
        created = created with { RowVersion = await SetRowVersion(created.Id) };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/provider-exits/{created.Id}/recheck", new { reason = "종료 조건 재검증", rowVersion = created.RowVersion })).StatusCode);
        created = created with { RowVersion = await SetRowVersion(created.Id) };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/provider-exits/{created.Id}/reject", new { reason = "관리자 검토 거절", rowVersion = created.RowVersion })).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityPublicId == created.Id).Select(x => x.ActionCode).ToListAsync(), x => x == "PROVIDER_EXIT_RECHECKED");
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityPublicId == created.Id).Select(x => x.ActionCode).ToListAsync(), x => x == "PROVIDER_EXIT_REJECTED");
    }

    [Fact]
    public async Task ExitRequestImmediatelyLocksAllTradingEligibility()
    {
        var account=await SeedProvider(false);using var provider=Client();await Login(provider,account.Credential);_ = await Request(provider,"PROVIDER_ROLE_EXIT");
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var category=await db.ServiceCategories.FirstAsync(x=>x.LevelCode=="SERVICE");var area=await db.AdministrativeAreas.FirstAsync();
        var result=await scope.ServiceProvider.GetRequiredService<ProviderTradingEligibilityService>().EvaluateAsync(account.ProviderId,category.Id,area.Id,default);
        Assert.False(result.IsEligible);Assert.Equal("PROVIDER_EXIT_IN_PROGRESS",result.ReasonCode);
    }

    [Fact]
    public async Task AccountWithdrawalAutomaticallyCompletesAndPreservesProfiles()
    {
        var account=await SeedProvider(true);using var provider=Client();await Login(provider,account.Credential);var created=await Request(provider,"ACCOUNT_WITHDRAWAL");Assert.Equal("READY_TO_COMPLETE",created.Status);
        using var scope=factory.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<ProviderExitService>().RefreshActiveAsync(default);var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Equal("WITHDRAWN",await db.Users.Where(x=>x.Id==account.UserId).Select(x=>x.StatusCode).SingleAsync());Assert.All(await db.UserRoles.Where(x=>x.UserId==account.UserId).ToListAsync(),x=>Assert.NotNull(x.RevokedAt));Assert.True(await db.ProviderProfiles.AnyAsync(x=>x.Id==account.ProviderId));Assert.True(await db.CustomerProfiles.AnyAsync(x=>x.UserId==account.UserId));Assert.Equal("COMPLETED",await db.ProviderExitRequests.Where(x=>x.PublicId==created.Id).Select(x=>x.StatusCode).SingleAsync());
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task OnlyAdminCanUseExitQueue(string role)
    {
        using var client = Client(); await Login(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/provider-exits")).StatusCode);
    }

    private async Task<SeededProvider> SeedProvider(bool customerRole, decimal available = 0, decimal reserved = 0)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var credential = new TestCredential($"exit-provider-{Guid.NewGuid():N}", $"Exit!{Guid.NewGuid():N}aA1");
        var now = DateTime.UtcNow;
        var user = new User { LoginId = credential.LoginId, NormalizedLoginId = credential.LoginId.ToUpperInvariant(), StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now };
        user.PasswordHash = hasher.HashPassword(user, credential.Password); db.Users.Add(user); await db.SaveChangesAsync();
        var providerRole = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Provider);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = providerRole.Id, GrantedAt = now });
        if (customerRole)
        {
            var customerRoleId = await db.Roles.Where(x => x.Code == RoleCodes.Customer).Select(x => x.Id).SingleAsync();
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRoleId, GrantedAt = now });
            db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = "복수 역할 고객", CreatedAt = now, UpdatedAt = now });
        }
        var profile = new ProviderProfile { UserId = user.Id, BusinessName = "종료 테스트 전문가", ApprovalStatusCode = "APPROVED", ActivityStatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now };
        db.ProviderProfiles.Add(profile); await db.SaveChangesAsync();
        var wallet = new ProviderWallet { ProviderProfileId = profile.Id, CurrencyCode = "KRW", AvailableBalance = available, ReservedBalance = reserved, StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now };
        db.ProviderWallets.Add(wallet); await db.SaveChangesAsync();
        return new(user.Id, profile.Id, wallet.Id, credential);
    }

    private async Task SeedTransactionsAndDispute(SeededProvider account)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var now = DateTime.UtcNow;
        var customer = await db.CustomerProfiles.FirstAsync(); var category = await db.ServiceCategories.FirstAsync(x => x.LevelCode == "SERVICE");
        async Task<TransactionRecord> Add(string status)
        {
            var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = category.Id, StatusCode = "PUBLISHED", Title = "종료 검증", Description = "종료 검증 요청", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now };
            db.ServiceRequests.Add(request); await db.SaveChangesAsync();
            var quote = new Quote { ServiceRequestId = request.Id, ProviderProfileId = account.ProviderId, StatusCode = "ACCEPTED", CreatedAt = now, UpdatedAt = now };
            db.Quotes.Add(quote); await db.SaveChangesAsync();
            var revision = new QuoteRevision { QuoteId = quote.Id, RevisionNo = 1, TotalAmount = 0, CurrencyCode = "KRW" };
            db.QuoteRevisions.Add(revision); await db.SaveChangesAsync();
            var transaction = new TransactionRecord { ServiceRequestId = request.Id, AcceptedQuoteRevisionId = revision.Id, CustomerProfileId = customer.Id, ProviderProfileId = account.ProviderId, CategoryId = category.Id, StatusCode = status, AgreedAmount = 0, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now };
            db.Transactions.Add(transaction); await db.SaveChangesAsync(); return transaction;
        }
        _ = await Add("COMPLETED"); var active = await Add("IN_PROGRESS");
        db.DisputeCases.Add(new DisputeCase { TransactionId = active.Id, ApplicantUserId = customer.UserId, CounterpartyUserId = account.UserId, Subject = "미종결 분쟁", Description = "검토 중", StatusCode = "OPEN", ReceivedAt = now, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    private static async Task<ProviderExitRequestResponse> Request(HttpClient client, string type)
    {
        var response = await client.PostAsJsonAsync(Path, new { requestType = type, reason = "전문가 활동 종료 요청", idempotencyKey = $"exit-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<ProviderExitRequestResponse>())!;
    }
    private async Task<string> SetRowVersion(Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var request = await db.ProviderExitRequests.SingleAsync(x => x.PublicId == id);
        request.RowVersion = [1];
        await db.SaveChangesAsync();
        return Convert.ToBase64String(request.RowVersion);
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private const string Path = "/api/v1/providers/me/exit/requests";
    private sealed record SeededProvider(long UserId, long ProviderId, long WalletId, TestCredential Credential);
}
