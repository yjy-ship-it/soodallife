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

public sealed class AdminCustomerApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_ListContainsOnlyCustomerRoleUsers_AndShowsAllActiveRoles()
    {
        await EnsureCustomerDirectoryDataAsync();
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var response = await client.GetFromJsonAsync<AdminCustomerListResponse>(BasePath);

        Assert.NotNull(response);
        Assert.Equal(2, response.TotalCount);
        var multiRole = Assert.Single(response.Items, item => item.Name == "김수달");
        Assert.Contains(RoleCodes.Provider, multiRole.Roles);
        Assert.All(response.Items, item => Assert.Contains(RoleCodes.Customer, item.Roles));
    }

    [Fact]
    public async Task Admin_CanSearchByNamePhoneEmail_AndUsePagination_WithMaskedListData()
    {
        await EnsureCustomerDirectoryDataAsync();
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var byName = await client.GetFromJsonAsync<AdminCustomerListResponse>($"{BasePath}?search={Uri.EscapeDataString("김수달")}");
        var byPhone = await client.GetFromJsonAsync<AdminCustomerListResponse>($"{BasePath}?search=01012345678");
        var byEmail = await client.GetFromJsonAsync<AdminCustomerListResponse>($"{BasePath}?search={Uri.EscapeDataString("sudal.customer@example.kr")}");
        var firstPage = await client.GetFromJsonAsync<AdminCustomerListResponse>($"{BasePath}?page=1&pageSize=1");
        var secondPage = await client.GetFromJsonAsync<AdminCustomerListResponse>($"{BasePath}?page=2&pageSize=1");

        Assert.Equal("김수달", Assert.Single(byName!.Items).Name);
        Assert.Equal("김수달", Assert.Single(byPhone!.Items).Name);
        var emailCustomer = Assert.Single(byEmail!.Items);
        Assert.Equal("010-****-5678", emailCustomer.MaskedPhone);
        Assert.Equal("s***@example.kr", emailCustomer.MaskedEmail);
        Assert.Single(firstPage!.Items);
        Assert.Single(secondPage!.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);
        var body = await (await client.GetAsync(BasePath)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("01012345678", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sudal.customer@example.kr", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_CustomerDetailConnectsBusinessHistory_WithoutMixingAnotherCustomer()
    {
        var customerId = await SeedCustomerHistoryAsync();
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var detail = await client.GetFromJsonAsync<AdminCustomerDetailResponse>($"{BasePath}/{customerId}");

        Assert.NotNull(detail);
        Assert.Equal("김수달", detail.Basic.Name);
        Assert.Equal("010-****-5678", detail.Basic.Phone);
        Assert.Contains(RoleCodes.Customer, detail.Basic.Roles);
        Assert.Contains(RoleCodes.Provider, detail.Basic.Roles);
        Assert.False(detail.Basic.IdentityVerificationSupported);
        Assert.False(detail.Addresses.IsSupported);
        Assert.False(detail.Reviews.IsSupported);
        Assert.False(detail.Consents.IsSupported);
        Assert.False(detail.Status.WithdrawalWorkflowSupported);
        Assert.Single(detail.Requests);
        Assert.Equal("욕실 수전 교체 요청", detail.Requests[0].Title);
        Assert.True(detail.Requests[0].HasAcceptedQuote);
        Assert.Single(detail.Quotes);
        Assert.True(detail.Quotes[0].IsAccepted);
        Assert.Equal(85000m, detail.Quotes[0].TotalAmount);
        Assert.Single(detail.Transactions);
        Assert.Equal("COMPLETED", detail.Transactions[0].StatusCode);
        Assert.Single(detail.AfterServices);
        Assert.Single(detail.ServiceHistory);
        Assert.NotEmpty(detail.ManagementHistory);
        Assert.DoesNotContain(detail.Requests, request => request.Title == "다른 고객의 방문청소 요청");
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessCustomerManagement(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath)).StatusCode);
    }

    private async Task EnsureCustomerDirectoryDataAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customerUser = await db.Users.SingleAsync(user => user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId);
        var customer = await db.CustomerProfiles.SingleAsync(profile => profile.UserId == customerUser.Id);
        customer.DisplayName = "김수달";
        customerUser.Phone = "01012345678";
        customerUser.Email = "sudal.customer@example.kr";
        var otherUser = await db.Users.SingleAsync(user => user.LoginId == factory.OtherCustomerCredential.LoginId);
        var otherCustomer = await db.CustomerProfiles.SingleAsync(profile => profile.UserId == otherUser.Id);
        otherCustomer.DisplayName = "박다정";
        otherUser.Phone = "01098765432";
        otherUser.Email = "dajung.park@example.kr";
        var providerRoleId = await db.Roles.Where(role => role.Code == RoleCodes.Provider).Select(role => role.Id).SingleAsync();
        if (!await db.UserRoles.AnyAsync(role => role.UserId == customerUser.Id && role.RoleId == providerRoleId && role.RevokedAt == null))
            db.UserRoles.Add(new UserRole { UserId = customerUser.Id, RoleId = providerRoleId, GrantedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc) });
        if (!await db.ProviderProfiles.AnyAsync(profile => profile.UserId == customerUser.Id))
            db.ProviderProfiles.Add(new ProviderProfile { UserId = customerUser.Id, BusinessName = "수달홈케어", ApprovalStatusCode = "APPROVED", ActivityStatusCode = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedCustomerHistoryAsync()
    {
        await EnsureCustomerDirectoryDataAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customerUser = await db.Users.SingleAsync(user => user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId);
        var customer = await db.CustomerProfiles.SingleAsync(profile => profile.UserId == customerUser.Id);
        if (await db.ServiceRequests.AnyAsync(request => request.CustomerProfileId == customer.Id && request.Title == "욕실 수전 교체 요청")) return customer.PublicId;

        var service = await db.ServiceCategories.SingleAsync(category => category.PublicId == factory.Catalog.ServiceId);
        var area = await db.AdministrativeAreas.SingleAsync(item => item.PublicId == factory.Catalog.AreaId);
        var policy = await db.CategoryPolicies.FirstAsync(item => item.CategoryId == service.Id);
        var provider = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var now = new DateTime(2026, 8, 9, 9, 0, 0, DateTimeKind.Utc);
        var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = service.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id,
            DetailAddress = "101동 1203호", Title = "욕실 수전 교체 요청", Description = "누수되는 수전을 교체해 주세요.", StatusCode = "ACCEPTED", PolicySnapshotJson = "{}", OpenedAt = now, AcceptedAt = now.AddHours(2), CreatedAt = now, UpdatedAt = now.AddHours(2) };
        db.ServiceRequests.Add(request); await db.SaveChangesAsync();
        var candidate = new DispatchCandidate { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, StatusCode = "DISPATCHED", CategoryMatch = true, AreaMatch = true, ApprovalMatch = true, EvaluatedAt = now, CreatedAt = now };
        db.DispatchCandidates.Add(candidate); await db.SaveChangesAsync();
        var dispatch = new RequestDispatch { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, CandidateId = candidate.Id, StatusCode = "RESPONDED", AvailableAt = now, RespondedAt = now.AddMinutes(20), ExpiresAt = now.AddHours(1), IdempotencyKey = "admin-customer-dispatch", CreatedAt = now };
        db.RequestDispatches.Add(dispatch); await db.SaveChangesAsync();
        var quote = new Quote { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, RequestDispatchId = dispatch.Id, StatusCode = "ACCEPTED", SubmittedAt = now.AddMinutes(20), AcceptedAt = now.AddHours(2), CreatedAt = now.AddMinutes(20), UpdatedAt = now.AddHours(2) };
        db.Quotes.Add(quote); await db.SaveChangesAsync();
        var revision = new QuoteRevision { QuoteId = quote.Id, RevisionNo = 1, Summary = "수전 교체 및 누수 점검", SubtotalAmount = 77273m, VatAmount = 7727m, TotalAmount = 85000m, CurrencyCode = "KRW", ValidUntil = now.AddDays(1), SubmittedAt = now.AddMinutes(20), SubmittedByUserId = provider.UserId, IdempotencyKey = "admin-customer-quote-revision" };
        db.QuoteRevisions.Add(revision); await db.SaveChangesAsync();
        var transaction = new TransactionRecord { ServiceRequestId = request.Id, AcceptedQuoteRevisionId = revision.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, CategoryId = service.Id,
            StatusCode = "COMPLETED", AgreedAmount = 85000m, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", WarrantyDaysSnapshot = 30,
            StartedAt = now.AddDays(1), CompletedAt = now.AddDays(1).AddHours(2), CreatedAt = now.AddHours(2), UpdatedAt = now.AddDays(1).AddHours(2) };
        db.Transactions.Add(transaction); await db.SaveChangesAsync();
        var afterService = new AfterServiceCase { TransactionId = transaction.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, StatusCode = "RECEIVED", Subject = "수전 연결부 재점검", Description = "연결부에 물방울이 보여 재점검을 요청합니다.", ReceivedAt = now.AddDays(3), IdempotencyKey = "admin-customer-as", CreatedAt = now.AddDays(3), UpdatedAt = now.AddDays(3) };
        db.AfterServiceCases.Add(afterService);
        db.ServiceHistoryEntries.Add(new ServiceHistoryEntry { CustomerProfileId = customer.Id, TransactionId = transaction.Id, EventTypeCode = "COMPLETION", Title = "욕실 수전 교체 완료", Summary = "수전 교체와 누수 점검을 완료했습니다.", ProviderNameSnapshot = provider.BusinessName, CategoryNameSnapshot = service.Name,
            TotalAmountSnapshot = 85000m, CurrencyCode = "KRW", CompletedAtSnapshot = transaction.CompletedAt, WarrantyStartDate = DateOnly.FromDateTime(transaction.CompletedAt!.Value), WarrantyEndDate = DateOnly.FromDateTime(transaction.CompletedAt.Value).AddDays(30), SnapshotJson = "{}", OccurredAt = transaction.CompletedAt.Value, IdempotencyKey = "admin-customer-history", CreatedAt = transaction.CompletedAt.Value });
        db.AuditLogs.Add(new AuditLog { OccurredAt = now.AddDays(2), ActorUserId = (await db.Users.SingleAsync(user => user.LoginId == factory.Credentials[RoleCodes.Admin].LoginId)).Id, ActorRoleCode = RoleCodes.Admin, ActionCode = "CUSTOMER_INFORMATION_UPDATED", EntityType = "CUSTOMER_PROFILE", EntityPublicId = customer.PublicId, ResultCode = "SUCCESS", Reason = "고객 문의 확인" });

        var otherUser = await db.Users.SingleAsync(user => user.LoginId == factory.OtherCustomerCredential.LoginId);
        var otherCustomer = await db.CustomerProfiles.SingleAsync(profile => profile.UserId == otherUser.Id);
        db.ServiceRequests.Add(new ServiceRequest { CustomerProfileId = otherCustomer.Id, CategoryId = service.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, Title = "다른 고객의 방문청소 요청", StatusCode = "OPEN", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        return customer.PublicId;
    }

    private const string BasePath = "/api/v1/admin/customers";
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
