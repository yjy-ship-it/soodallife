using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CustomerCareSubscriptionApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task PublicCatalog_UsesEligibleCategories_AndActiveProducts_WithoutInternalFields()
    {
        await EnableSubscription();
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        var product = await CreateProduct(admin);

        using var anonymous = Client();
        var services = await anonymous.GetFromJsonAsync<List<SubscriptionServiceItem>>("/api/v1/public/care/services");
        Assert.Contains(services!, item => item.Id == factory.Catalog.ServiceId);
        Assert.DoesNotContain(services!, item => item.Id == factory.Catalog.OtherServiceId);
        var response = await anonymous.GetAsync("/api/v1/public/care/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<CustomerCareProductResponse>>();
        Assert.Contains(products!, item => item.Id == product.Id);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("feePolicy", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomerRequestAndComparison_SupportSavedAddress_Idempotency_TrustReviews_AndPrivacy()
    {
        await EnableSubscription();
        using var customer = Client(); using var provider = Client(); using var other = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(other, factory.OtherCustomerCredential);
        var addressId = await AddCustomerAddress();
        var key = $"customer-request-{Guid.NewGuid():N}";
        var request = await CreateRequest(customer, addressId, key);
        var repeated = await CreateRequest(customer, addressId, key);
        Assert.Equal(request.Id, repeated.Id);
        var application = await Apply(provider, request.Id);
        await SetProviderTrust(application.ProviderId, 87.5m, "A", "CALCULATED");

        var list = await customer.GetFromJsonAsync<List<CustomerSubscriptionRequestResponse>>("/api/v1/customers/me/care/requests");
        Assert.Contains(list!, item => item.Id == request.Id && item.ApplicationCount == 1);
        var home = await customer.GetAsync("/api/v1/customers/me/care/home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/customers/me/care/requests/{request.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/v1/customers/me/care/requests")).StatusCode);

        var response = await customer.GetAsync($"/api/v1/customers/me/care/requests/{request.Id}/applications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var applications = await response.Content.ReadFromJsonAsync<List<CustomerSubscriptionApplicationResponse>>();
        var compared = Assert.Single(applications!);
        Assert.Equal(87.5m, compared.TrustScore);
        Assert.Equal("A", compared.TrustGrade);
        Assert.Equal("CALCULATED", compared.TrustEvaluationStatus);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("010-", json);
        Assert.DoesNotContain("상세주소", json);
        Assert.DoesNotContain("businessRegistration", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wallet", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fee", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomerContractAndVisits_AreOwnerOnly_Idempotent_AndPreserveFinancialAndTrustData()
    {
        var flow = await CreateFlow();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var contractRow = await db.SubscriptionContracts.SingleAsync(item => item.PublicId == flow.Contract.Id);
        var pastVisit = await db.SubscriptionVisitSchedules.Where(item => item.SubscriptionContractId == contractRow.Id).OrderBy(item => item.ScheduledStartAt).FirstAsync();
        pastVisit.ScheduledStartAt = DateTime.UtcNow.AddDays(-2); pastVisit.ScheduledEndAt = DateTime.UtcNow.AddDays(-2).AddHours(1); pastVisit.StatusCode = "SCHEDULED";
        await db.SaveChangesAsync();
        var pastVisitId = pastVisit.Id; var pastStart = pastVisit.ScheduledStartAt;
        var walletLedgerBefore = await db.WalletLedgerEntries.CountAsync();
        var walletsBefore = await db.ProviderWallets.AsNoTracking().Select(item => new { item.Id, item.AvailableBalance, item.ReservedBalance }).ToListAsync();
        var feeBefore = await db.FeeCharges.CountAsync();
        var trustEventsBefore = await db.TrustScoreEvents.CountAsync();
        var notificationsBefore = await db.Notifications.CountAsync();
        var interiorsBefore = await db.InteriorProjects.CountAsync();
        var trustBefore = await db.ProviderTrustScoreCurrent.AsNoTracking().Select(item => new { item.Id, item.Score, item.GradeCode, item.EvaluationStatusCode }).ToListAsync();

        var contractResponse = await flow.Customer.GetAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}");
        Assert.Equal(HttpStatusCode.OK, contractResponse.StatusCode);
        var contractJson = await contractResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("feePolicy", contractJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("settlement", contractJson, StringComparison.OrdinalIgnoreCase);
        var visits = await flow.Customer.GetFromJsonAsync<List<CustomerSubscriptionVisitListItem>>($"/api/v1/customers/me/care/visits?contractId={flow.Contract.Id}");
        var visitItems = visits ?? throw new InvalidOperationException("고객 회차 목록 응답이 없습니다.");
        var visit = Assert.Single(visitItems.Take(1));
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.GetAsync($"/api/v1/customers/me/care/visits/{visit.Id}")).StatusCode);

        using var other = Client(); await Login(other, factory.OtherCustomerCredential);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/customers/me/care/visits/{visit.Id}")).StatusCode);

        var pauseKey = $"pause-{Guid.NewGuid():N}";
        var pause = await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}/pause", new { idempotencyKey = pauseKey, reason = "고객 일정", resumePlannedAt = (DateTime?)null, rowVersion = (string?)null });
        Assert.Equal(HttpStatusCode.OK, pause.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}/pause", new { idempotencyKey = pauseKey, reason = "중복", resumePlannedAt = (DateTime?)null, rowVersion = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}/resume", new { idempotencyKey = $"resume-{Guid.NewGuid():N}", reason = "재개", resumePlannedAt = (DateTime?)null, rowVersion = (string?)null })).StatusCode);

        var changeKey = $"change-{Guid.NewGuid():N}";
        var changeResponse = await flow.Customer.PostAsJsonAsync($"/api/v1/subscriptions/visits/{visit.Id}/schedule-changes", new { scheduledStartAt = visit.ScheduledStartAt.AddDays(1), scheduledEndAt = visit.ScheduledEndAt?.AddDays(1), reason = "고객 일정 변경", idempotencyKey = changeKey });
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        var change = await changeResponse.Content.ReadFromJsonAsync<SubscriptionScheduleChangeResponse>();
        var cancelKey = $"cancel-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/schedule-changes/{change!.Id}/cancel", new { idempotencyKey = cancelKey, rowVersion = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/schedule-changes/{change.Id}/cancel", new { idempotencyKey = cancelKey, rowVersion = (string?)null })).StatusCode);
        var skipVisit = visitItems.First(item => item.Id != visit.Id && item.ScheduledStartAt > DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.OK, (await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/visits/{skipVisit.Id}/skip", new { idempotencyKey = $"skip-{Guid.NewGuid():N}", reason = "이번 회차 제외", rowVersion = (string?)null })).StatusCode);
        var terminate = await flow.Customer.PostAsJsonAsync($"/api/v1/customers/me/care/contracts/{flow.Contract.Id}/terminate", new { idempotencyKey = $"terminate-{Guid.NewGuid():N}", reason = "고객 해지 요청", resumePlannedAt = (DateTime?)null, rowVersion = (string?)null });
        Assert.Equal(HttpStatusCode.OK, terminate.StatusCode);
        var pendingTermination = await terminate.Content.ReadFromJsonAsync<CustomerSubscriptionContractResponse>();
        Assert.Equal("ACTIVE", pendingTermination!.StatusCode); Assert.Equal("해지 처리 대기", pendingTermination.StatusDisplay); Assert.NotNull(pendingTermination.TerminationRequestedAt);

        db.ChangeTracker.Clear();
        var preservedPast = await db.SubscriptionVisitSchedules.AsNoTracking().SingleAsync(item => item.Id == pastVisitId);
        Assert.Equal("SCHEDULED", preservedPast.StatusCode); Assert.Equal(pastStart, preservedPast.ScheduledStartAt);
        Assert.Equal(walletLedgerBefore, await db.WalletLedgerEntries.CountAsync());
        Assert.Equal(walletsBefore, await db.ProviderWallets.AsNoTracking().Select(item => new { item.Id, item.AvailableBalance, item.ReservedBalance }).ToListAsync());
        Assert.Equal(feeBefore, await db.FeeCharges.CountAsync());
        Assert.Equal(trustEventsBefore, await db.TrustScoreEvents.CountAsync());
        Assert.Equal(notificationsBefore, await db.Notifications.CountAsync());
        Assert.Equal(interiorsBefore, await db.InteriorProjects.CountAsync());
        var trustAfter = await db.ProviderTrustScoreCurrent.AsNoTracking().Select(item => new { item.Id, item.Score, item.GradeCode, item.EvaluationStatusCode }).ToListAsync();
        Assert.Equal(trustBefore, trustAfter);
    }

    [Fact]
    public async Task CustomerPaymentViews_ShowOnlyOwnWorkflow_WithoutExternalReferencesOrPayouts()
    {
        var flow = await CreateFlow();
        await AddPaymentWorkflow(flow.Contract.Id);
        var methods = await flow.Customer.GetAsync("/api/v1/customers/me/care/payment-methods");
        var payments = await flow.Customer.GetAsync("/api/v1/customers/me/care/payments");
        Assert.Equal(HttpStatusCode.OK, methods.StatusCode);
        Assert.Equal(HttpStatusCode.OK, payments.StatusCode);
        var methodsJson = await methods.Content.ReadAsStringAsync();
        var paymentsJson = await payments.Content.ReadAsStringAsync();
        Assert.Contains("****-4242", methodsJson);
        Assert.Contains("PG 연동 준비 중", paymentsJson);
        Assert.DoesNotContain("external-token-secret", methodsJson);
        Assert.DoesNotContain("ExternalPaymentReference", paymentsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payout", paymentsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("netAmount", paymentsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fee", paymentsJson, StringComparison.OrdinalIgnoreCase);

        using var other = Client(); await Login(other, factory.OtherCustomerCredential);
        Assert.Empty((await other.GetFromJsonAsync<List<CustomerSubscriptionPaymentHistoryResponse>>("/api/v1/customers/me/care/payments"))!);
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/subscriptions/contracts")).StatusCode);
    }

    private async Task<Flow> CreateFlow()
    {
        await EnableSubscription();
        var customer = Client(); var provider = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var request = await CreateRequest(customer, null, $"request-{Guid.NewGuid():N}");
        var application = await Apply(provider, request.Id);
        var response = await customer.PostAsJsonAsync($"/api/v1/subscriptions/requests/{request.Id}/selection", new { applicationId = application.Id, idempotencyKey = $"select-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return new Flow(customer, provider, request, application, (await response.Content.ReadFromJsonAsync<SubscriptionContractResponse>())!);
    }

    private async Task<SubscriptionRequestResponse> CreateRequest(HttpClient customer, Guid? addressId, string key)
    {
        var response = await customer.PostAsJsonAsync("/api/v1/subscriptions/requests", new { serviceCategoryId = factory.Catalog.ServiceId, customerAddressId = addressId, careProductId = (Guid?)null, administrativeAreaId = factory.Catalog.AreaId, requestTypeCode = "CUSTOM", requestedScopeText = "정기 생활 점검", preferredStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), detailAddress = addressId.HasValue ? null : "직접 입력 상세주소", idempotencyKey = key, recurrence = new { frequencyTypeCode = "BIWEEKLY", intervalValue = 2, visitsPerPeriod = 2, weekdays = new[] { 1, 4 }, preferredTimeFrom = new TimeOnly(10, 0), preferredTimeTo = new TimeOnly(11, 0), expectedDurationMinutes = 60, startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), endDate = (DateOnly?)null } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubscriptionRequestResponse>())!;
    }

    private async Task<SubscriptionApplicationResponse> Apply(HttpClient provider, Guid requestId)
    {
        var response = await provider.PostAsJsonAsync($"/api/v1/subscriptions/requests/{requestId}/applications", new { proposedScopeText = "요청 범위 정기 방문", proposedMonthlyAmount = 120000m, proposedVisitAmount = 30000m, availableScheduleText = "격주 월요일 오전", idempotencyKey = $"apply-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubscriptionApplicationResponse>())!;
    }

    private async Task<Guid> AddCustomerAddress()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customer = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile).SingleAsync();
        var area = await db.AdministrativeAreas.SingleAsync(item => item.PublicId == factory.Catalog.AreaId);
        var now = DateTime.UtcNow;
        var address = new CustomerAddress { CustomerProfileId = customer.Id, AdministrativeAreaId = area.Id, AddressName = "구독 테스트 주소", RecipientName = "테스트 고객", RoadAddress = "서울 테스트로 10", DetailAddress = "상세주소 101호", IsActive = true, IsDefault = true, CreatedAt = now, UpdatedAt = now };
        db.CustomerAddresses.Add(address); await db.SaveChangesAsync(); return address.PublicId;
    }

    private async Task SetProviderTrust(Guid providerId, decimal score, string grade, string status)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.SingleAsync(item => item.PublicId == providerId);
        var trust = await db.ProviderTrustScoreCurrent.SingleOrDefaultAsync(item => item.ProviderProfileId == provider.Id);
        if (trust is null) { trust = new ProviderTrustScoreCurrent { ProviderProfileId = provider.Id, CreatedAt = DateTime.UtcNow }; db.ProviderTrustScoreCurrent.Add(trust); }
        trust.Score = score; trust.GradeCode = grade; trust.EvaluationStatusCode = status; trust.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
    }

    private async Task AddPaymentWorkflow(Guid contractId)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var contract = await db.SubscriptionContracts.SingleAsync(item => item.PublicId == contractId); var now = DateTime.UtcNow;
        var method = new SubscriptionPaymentMethod { CustomerProfileId = contract.CustomerProfileId, PaymentMethodTypeCode = "CARD_REFERENCE", ProviderCode = "NOT_CONNECTED", ExternalTokenReference = "external-token-secret", MaskedDisplayText = "****-4242", StatusCode = "PENDING", IsDefault = true, RegisteredAt = now, CreatedAt = now, UpdatedAt = now };
        db.SubscriptionPaymentMethods.Add(method); await db.SaveChangesAsync();
        var payment = new SubscriptionPaymentRequest { SubscriptionContractId = contract.Id, CustomerProfileId = contract.CustomerProfileId, PaymentMethodId = method.Id, BillingPeriodStart = DateOnly.FromDateTime(DateTime.Today), BillingPeriodEnd = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)), RequestedAmount = 120000m, CurrencyCode = "KRW", StatusCode = "FAILED", RequestedAt = now, FailedAt = now, FailureReason = "PG 연동 준비 중", ExternalPaymentReference = "not-a-real-payment", IdempotencyKey = $"payment-{Guid.NewGuid():N}", CreatedAt = now, UpdatedAt = now };
        db.SubscriptionPaymentRequests.Add(payment); await db.SaveChangesAsync();
        db.SubscriptionRefundAdjustments.Add(new SubscriptionRefundAdjustment { SubscriptionContractId = contract.Id, PaymentRequestId = payment.Id, TypeCode = "REFUND", RequestedAmount = 10000m, Reason = "정책 확인 중", StatusCode = "REQUESTED", RequestedAt = now, IdempotencyKey = $"refund-{Guid.NewGuid():N}", CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    private async Task<CareProductResponse> CreateProduct(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/admin/subscriptions/products", new { serviceCategoryId = factory.Catalog.ServiceId, productName = $"고객 공개상품 {Guid.NewGuid():N}", description = "표준 구독상품", serviceScopeText = "기본 점검", visitsPerPeriod = 2, expectedDurationMinutes = 60, billingPeriodCode = "MONTHLY", standardMonthlyAmount = 100000m, standardVisitAmount = 25000m, effectiveFrom = DateOnly.FromDateTime(DateTime.Today), effectiveTo = (DateOnly?)null, isActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<CareProductResponse>())!;
    }

    private async Task EnableSubscription()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var category = await db.ServiceCategories.SingleAsync(item => item.PublicId == factory.Catalog.ServiceId);
        var policy = await db.CategoryOperationPolicies.SingleAsync(item => item.CategoryId == category.Id);
        policy.SubscriptionOptionText = "허용"; await db.SaveChangesAsync();
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static async Task Login(HttpClient client, TestCredential credential) => Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password })).StatusCode);
    private sealed record Flow(HttpClient Customer, HttpClient Provider, SubscriptionRequestResponse Request, SubscriptionApplicationResponse Application, SubscriptionContractResponse Contract);
}
