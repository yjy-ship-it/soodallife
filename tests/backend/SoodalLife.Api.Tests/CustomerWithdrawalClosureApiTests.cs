using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CustomerWithdrawalClosureApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Request_IsIdempotent_AndOwnerCanCancel_ButOtherCustomerCannot()
    {
        var account = await SeedCustomer(false); using var owner = Client(); await Login(owner, account.Credential);
        var key = $"withdraw-{Guid.NewGuid():N}"; var input = new { scopeCode = "CUSTOMER_ROLE", reason = "이용 종료", idempotencyKey = key };
        var first = await (await owner.PostAsJsonAsync(Path, input)).Content.ReadFromJsonAsync<CustomerWithdrawalRequestResponse>();
        var second = await (await owner.PostAsJsonAsync(Path, input)).Content.ReadFromJsonAsync<CustomerWithdrawalRequestResponse>();
        Assert.Equal(first!.Id, second!.Id); var version = await SetVersion(first.Id);
        using var other = Client(); await Login(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"{Path}/{first.Id}/cancel", new { reason = "타인 신청", idempotencyKey = $"cancel-{Guid.NewGuid():N}", rowVersion = version })).StatusCode);
        var cancelled = await owner.PostAsJsonAsync($"{Path}/{first.Id}/cancel", new { reason = "계속 이용", idempotencyKey = $"cancel-{Guid.NewGuid():N}", rowVersion = version });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode); Assert.Equal("CANCELLED", (await cancelled.Content.ReadFromJsonAsync<CustomerWithdrawalRequestResponse>())!.Status);
    }

    [Fact]
    public async Task Readiness_ReportsEveryBusinessDomainAndFinancialPending()
    {
        var account = await SeedCustomer(false); await SeedBlockers(account);
        using var customer = Client(); await Login(customer, account.Credential);
        var data = await customer.GetFromJsonAsync<CustomerWithdrawalDashboardResponse>("/api/v1/customer/account/withdrawal");
        Assert.False(data!.Readiness.CanComplete);
        foreach (var domain in new[] { "GENERAL", "CARE", "INTERIOR", "EMERGENCY", "AFTER_SERVICE", "DISPUTE", "FINANCIAL" })
            Assert.Contains(data.Readiness.Blockers, x => x.Domain == domain);
    }

    [Fact]
    public async Task AdminQueue_RechecksAndBlocksWhenNewWorkAppears()
    {
        var account = await SeedCustomer(false); var request = await Request(account); using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/customer-withdrawals")).StatusCode);
        await SeedGeneralTransaction(account); var version = await SetVersion(request.Id);
        var complete = await admin.PostAsJsonAsync($"/api/v1/admin/customer-withdrawals/{request.Id}/complete", Decision(version));
        Assert.Equal(HttpStatusCode.Conflict, complete.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customerRole = await db.Roles.Where(x => x.Code == RoleCodes.Customer).Select(x => x.Id).SingleAsync();
        Assert.Null(await db.UserRoles.Where(x => x.UserId == account.UserId && x.RoleId == customerRole).Select(x => x.RevokedAt).SingleAsync());
    }

    [Fact]
    public async Task Completion_RevokesOnlyCustomerRole_EndsOnlyCustomerChat_AndPreservesHistoryReviewAndUser()
    {
        var account = await SeedCustomer(true); var preserved = await SeedPreservedRecords(account); var request = await Request(account); var version = await SetVersion(request.Id);
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        var response = await admin.PostAsJsonAsync($"/api/v1/admin/customer-withdrawals/{request.Id}/complete", Decision(version));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var roles = await (from link in db.UserRoles join role in db.Roles on link.RoleId equals role.Id where link.UserId == account.UserId select new { role.Code, link.RevokedAt }).ToListAsync();
        Assert.NotNull(roles.Single(x => x.Code == RoleCodes.Customer).RevokedAt); Assert.Null(roles.Single(x => x.Code == RoleCodes.Provider).RevokedAt);
        Assert.Equal("ACTIVE", await db.Users.Where(x => x.Id == account.UserId).Select(x => x.StatusCode).SingleAsync());
        Assert.Equal("ENDED", await db.ChatParticipants.Where(x => x.Id == preserved.CustomerChatId).Select(x => x.StatusCode).SingleAsync());
        Assert.Equal("ACTIVE", await db.ChatParticipants.Where(x => x.Id == preserved.ProviderChatId).Select(x => x.StatusCode).SingleAsync());
        Assert.True(await db.ServiceHistoryEntries.AnyAsync(x => x.Id == preserved.HistoryId)); Assert.True(await db.Reviews.AnyAsync(x => x.Id == preserved.ReviewId));
        Assert.Contains(await db.AuditLogs.Where(x => x.EntityPublicId == request.Id).Select(x => x.ActionCode).ToListAsync(), x => x == "CUSTOMER_WITHDRAWAL_COMPLETED");
        Assert.DoesNotContain(await db.AuditLogs.Where(x => x.EntityPublicId == request.Id).Select(x => (x.BeforeJson ?? "") + (x.AfterJson ?? "")).ToListAsync(), x => x.Contains("@") || x.Contains("010-"));
    }

    [Fact]
    public async Task CompletedCustomerSessionCannotCreateNewCustomerWork_WhileProviderRoleCanLogin()
    {
        var account = await SeedCustomer(true); var request = await Request(account); var version = await SetVersion(request.Id);
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        (await admin.PostAsJsonAsync($"/api/v1/admin/customer-withdrawals/{request.Id}/complete", Decision(version))).EnsureSuccessStatusCode();
        using var staleCustomer = Client(); await Login(staleCustomer, account.Credential);
        Assert.Equal(HttpStatusCode.Forbidden, (await staleCustomer.GetAsync("/api/v1/customer/account/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staleCustomer.GetAsync("/api/v1/providers/me")).StatusCode);
    }

    [Fact]
    public async Task AccountScope_RemainsPolicyRequired_AndCannotComplete()
    {
        var account = await SeedCustomer(false); using var customer = Client(); await Login(customer, account.Credential);
        var response = await customer.PostAsJsonAsync(Path, new { scopeCode = "ACCOUNT", reason = "전체 계정 검토", idempotencyKey = $"account-{Guid.NewGuid():N}" });
        var item = await response.Content.ReadFromJsonAsync<CustomerWithdrawalRequestResponse>(); Assert.Contains(item!.Readiness.Blockers, x => x.Code == "ACCOUNT_CLOSURE_POLICY");
    }

    private async Task<SeededCustomer> SeedCustomer(bool providerRole)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(); var now = DateTime.UtcNow;
        var credential = new TestCredential($"withdraw-customer-{Guid.NewGuid():N}", $"Withdraw!{Guid.NewGuid():N}aA1");
        var user = new User { LoginId = credential.LoginId, NormalizedLoginId = credential.LoginId.ToUpperInvariant(), StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now }; user.PasswordHash = hasher.HashPassword(user, credential.Password); db.Users.Add(user); await db.SaveChangesAsync();
        var customerRole = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Customer); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id, GrantedAt = now });
        if (providerRole) { var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Provider); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now }); }
        var profile = new CustomerProfile { UserId = user.Id, DisplayName = "탈퇴 검증 고객", CreatedAt = now, UpdatedAt = now }; db.CustomerProfiles.Add(profile); await db.SaveChangesAsync();
        if (providerRole) { db.ProviderProfiles.Add(new ProviderProfile { UserId = user.Id, BusinessName = "복수역할 전문가", ApprovalStatusCode = "APPROVED", ActivityStatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now }); await db.SaveChangesAsync(); }
        return new(user.Id, profile.Id, credential);
    }

    private async Task SeedBlockers(SeededCustomer account)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var now = DateTime.UtcNow; var category = await db.ServiceCategories.FirstAsync(x => x.LevelCode == "SERVICE");
        var general = new ServiceRequest { CustomerProfileId = account.ProfileId, CategoryId = category.Id, CategoryPolicyId = await db.CategoryPolicies.Select(x => x.Id).FirstAsync(), Title = "일반 진행", StatusCode = "OPEN", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now }; db.ServiceRequests.Add(general);
        var emergency = new ServiceRequest { CustomerProfileId = account.ProfileId, CategoryId = category.Id, CategoryPolicyId = general.CategoryPolicyId, Title = "긴급 진행", StatusCode = "OPEN", IsUrgent = true, PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now }; db.ServiceRequests.Add(emergency); await db.SaveChangesAsync();
        var provider = await db.ProviderProfiles.FirstAsync(); var transaction = NewTransaction(general.Id, account.ProfileId, provider.Id, category.Id, "IN_PROGRESS", false, now); db.Transactions.Add(transaction); await db.SaveChangesAsync();
        var careRequest = new SubscriptionRequest { CustomerProfileId = account.ProfileId, ServiceCategoryId = category.Id, AdministrativeAreaId = await db.AdministrativeAreas.Select(x => x.Id).FirstAsync(), RequestTypeCode = "CUSTOM", RequestedScopeText = "정기관리", PreferredStartDate = DateOnly.FromDateTime(now), StatusCode = "OPEN", CreatedAt = now, UpdatedAt = now }; db.SubscriptionRequests.Add(careRequest); await db.SaveChangesAsync();
        var contract = new SubscriptionContract { SubscriptionRequestId = careRequest.Id, CustomerProfileId = account.ProfileId, ProviderProfileId = provider.Id, ServiceCategoryId = category.Id, SubscriptionApplicationId = 1, StatusCode = "ACTIVE", StartedAt = now, PriceSnapshotJson = "{}", ServiceScopeSnapshotJson = "{}", RecurrenceSnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now }; db.SubscriptionContracts.Add(contract);
        db.InteriorProjects.Add(new InteriorProject { CustomerProfileId = account.ProfileId, ServiceRequestId = emergency.Id, ServiceCategoryId = category.Id, StatusCode = "CONSTRUCTION", FeeAssessmentStatusCode = "POLICY_PENDING", CreatedAt = now, UpdatedAt = now });
        db.AfterServiceCases.Add(new AfterServiceCase { CustomerProfileId = account.ProfileId, TransactionId = transaction.Id, ProviderProfileId = provider.Id, Subject = "미종결 A/S", Description = "확인 중", StatusCode = "RECEIVED", ReceivedAt = now, CreatedAt = now, UpdatedAt = now });
        db.DisputeCases.Add(new DisputeCase { TransactionId = transaction.Id, ApplicantUserId = account.UserId, CounterpartyUserId = provider.UserId, Subject = "미종결 분쟁", Description = "검토 중", StatusCode = "OPEN", ReceivedAt = now, CreatedAt = now, UpdatedAt = now });
        db.TransactionDirectPayments.Add(new TransactionDirectPayment { TransactionId = transaction.Id, RegisteredByUserId = account.UserId, RegisteredByRoleCode = RoleCodes.Customer, Amount = 1000, PaymentMethodCode = "CASH", PaidAt = now, StatusCode = "REGISTERED", RegisteredAt = now, RegistrationIdempotencyKey = $"pay-{Guid.NewGuid():N}", CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        db.SubscriptionPaymentRequests.Add(new SubscriptionPaymentRequest { SubscriptionContractId = contract.Id, CustomerProfileId = account.ProfileId, BillingPeriodStart = DateOnly.FromDateTime(now), BillingPeriodEnd = DateOnly.FromDateTime(now), RequestedAmount = 1000, StatusCode = "REQUESTED", RequestedAt = now, IdempotencyKey = $"care-pay-{Guid.NewGuid():N}", CreatedAt = now, UpdatedAt = now }); await db.SaveChangesAsync();
    }

    private async Task SeedGeneralTransaction(SeededCustomer account)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var category = await db.ServiceCategories.FirstAsync(x => x.LevelCode == "SERVICE"); var provider = await db.ProviderProfiles.FirstAsync(); var now = DateTime.UtcNow;
        db.Transactions.Add(NewTransaction(1, account.ProfileId, provider.Id, category.Id, "IN_PROGRESS", false, now)); await db.SaveChangesAsync();
    }

    private async Task<(long HistoryId, long ReviewId, long CustomerChatId, long ProviderChatId)> SeedPreservedRecords(SeededCustomer account)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var category = await db.ServiceCategories.FirstAsync(x => x.LevelCode == "SERVICE"); var provider = await db.ProviderProfiles.FirstAsync(); var now = DateTime.UtcNow;
        var transaction = NewTransaction(1, account.ProfileId, provider.Id, category.Id, "COMPLETED", false, now); db.Transactions.Add(transaction); await db.SaveChangesAsync();
        var history = new ServiceHistoryEntry { CustomerProfileId = account.ProfileId, TransactionId = transaction.Id, EventTypeCode = "COMPLETION", OccurredAt = now, Title = "보존 이력", Summary = "완료 기록", ProviderNameSnapshot = provider.BusinessName, SnapshotJson = "{}", IdempotencyKey = $"history-{Guid.NewGuid():N}", CreatedAt = now }; db.ServiceHistoryEntries.Add(history);
        var review = new Review { TransactionId = transaction.Id, CustomerProfileId = account.ProfileId, ProviderProfileId = provider.Id, BodyText = "보존 리뷰", VerificationStatusCode = "VERIFIED_TRANSACTION", VisibilityStatusCode = "PUBLIC", IdempotencyKey = $"review-{Guid.NewGuid():N}", SubmittedAt = now, PublishedAt = now, CreatedAt = now, UpdatedAt = now }; db.Reviews.Add(review);
        var room = new ChatRoom { ResourceTypeCode = "TRANSACTION", ResourcePublicId = transaction.PublicId, RoomTypeCode = "DIRECT", StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now }; db.ChatRooms.Add(room); await db.SaveChangesAsync();
        var customerChat = new ChatParticipant { ChatRoomId = room.Id, UserId = account.UserId, ParticipantRoleCode = "CUSTOMER", StatusCode = "ACTIVE", AccessStartedAt = now, JoinedAt = now };
        var providerChat = new ChatParticipant { ChatRoomId = room.Id, UserId = account.UserId, ParticipantRoleCode = "PROVIDER", StatusCode = "ACTIVE", AccessStartedAt = now, JoinedAt = now }; db.ChatParticipants.AddRange(customerChat, providerChat); await db.SaveChangesAsync();
        return (history.Id, review.Id, customerChat.Id, providerChat.Id);
    }

    private static TransactionRecord NewTransaction(long requestId, long customerId, long providerId, long categoryId, string status, bool emergency, DateTime now) => new()
    { ServiceRequestId = requestId, AcceptedQuoteRevisionId = 1, CustomerProfileId = customerId, ProviderProfileId = providerId, CategoryId = categoryId, StatusCode = status, AgreedAmount = 0, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", FeePolicyKindSnapshot = emergency ? "EMERGENCY" : "STANDARD", CreatedAt = now, UpdatedAt = now };
    private async Task<CustomerWithdrawalRequestResponse> Request(SeededCustomer account) { using var client = Client(); await Login(client, account.Credential); var response = await client.PostAsJsonAsync(Path, new { scopeCode = "CUSTOMER_ROLE", reason = "고객 역할 종료", idempotencyKey = $"withdraw-{Guid.NewGuid():N}" }); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<CustomerWithdrawalRequestResponse>())!; }
    private async Task<string> SetVersion(Guid id) { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var item = await db.CustomerWithdrawalRequests.SingleAsync(x => x.PublicId == id); item.RowVersion = [1]; await db.SaveChangesAsync(); return Convert.ToBase64String(item.RowVersion); }
    private static object Decision(string version) => new { reason = "최종 업무상태 확인", idempotencyKey = $"decision-{Guid.NewGuid():N}", rowVersion = version };
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private const string Path = "/api/v1/customer/account/withdrawal-requests";
    private sealed record SeededCustomer(long UserId, long ProfileId, TestCredential Credential);
}
