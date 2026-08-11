using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class QuoteAcceptanceWalletIntegrationTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Eligibility_RejectsOverallApproval_ServiceApproval_Area_AndMissingStructuredRequirements()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var evaluator = scope.ServiceProvider.GetRequiredService<ProviderTradingEligibilityService>();
        var provider = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                              where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var categoryId = await db.ServiceCategories.Where(item => item.PublicId == factory.Catalog.ServiceId).Select(item => item.Id).SingleAsync();
        var areaId = await db.AdministrativeAreas.Where(item => item.PublicId == factory.Catalog.AreaId).Select(item => item.Id).SingleAsync();
        var otherAreaId = await db.AdministrativeAreas.Where(item => item.PublicId == factory.Catalog.OtherAreaId).Select(item => item.Id).SingleAsync();
        var service = await db.ProviderServiceCategories.SingleAsync(item => item.ProviderProfileId == provider.Id && item.CategoryId == categoryId);
        var approval = await db.ProviderServiceApprovals.SingleAsync(item => item.ProviderServiceCategoryId == service.Id);

        provider.ApprovalStatusCode = "PENDING"; await db.SaveChangesAsync();
        Assert.Equal("PROVIDER_NOT_APPROVED_ACTIVE", (await evaluator.EvaluateAsync(provider.Id, categoryId, areaId, default)).ReasonCode);
        provider.ApprovalStatusCode = "APPROVED"; approval.ApprovalStatusCode = "PENDING"; await db.SaveChangesAsync();
        Assert.Equal("SERVICE_NOT_APPROVED", (await evaluator.EvaluateAsync(provider.Id, categoryId, areaId, default)).ReasonCode);
        approval.ApprovalStatusCode = "APPROVED"; await db.SaveChangesAsync();
        Assert.Equal("SERVICE_AREA_MISMATCH", (await evaluator.EvaluateAsync(provider.Id, categoryId, otherAreaId, default)).ReasonCode);
        var assignments = await db.CategoryProviderRequirementAssignments.Where(item => item.IsActive).ToListAsync();
        assignments.ForEach(item => item.IsActive = false); await db.SaveChangesAsync();
        Assert.Equal("STRUCTURED_REQUIREMENTS_NOT_CONFIGURED", (await evaluator.EvaluateAsync(provider.Id, categoryId, areaId, default)).ReasonCode);
        assignments.ForEach(item => item.IsActive = true); await db.SaveChangesAsync();
        Assert.True((await evaluator.EvaluateAsync(provider.Id, categoryId, areaId, default)).IsEligible);
    }

    [Fact]
    public async Task Acceptance_ChargesExactlyOnce_StoresSnapshots_AndRevealsPrivateContactOnlyToSelectedProvider()
    {
        using var selectedProvider = Client();
        using var otherProvider = Client();
        using var customer = Client();
        using var admin = Client();
        await Login(selectedProvider, factory.Credentials[RoleCodes.Provider]);
        await Login(otherProvider, factory.AreaMismatchProviderCredential);
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(admin, factory.Credentials[RoleCodes.Admin]);
        await ResetWalletsAsync();
        await ConfigureArea(selectedProvider);
        await ConfigureArea(otherProvider);
        await SetCustomerPhoneAsync("01012345678");

        var request = await CreateRequest(customer, "욕실 수전 교체 요청", "101동 1203호");
        var beforeMatch = await otherProvider.GetFromJsonAsync<ProviderMatchedRequestDetail>($"/api/v1/providers/me/matched-requests/{request.Id}");
        Assert.Null(beforeMatch!.CustomerPhone);
        Assert.Null(beforeMatch.DetailAddress);

        var readiness = await selectedProvider.GetFromJsonAsync<QuoteSubmissionReadinessResponse>(
            $"/api/v1/providers/me/requests/{request.Id}/quote-submission-readiness");
        Assert.True(readiness!.CanSubmit);
        Assert.Equal(3000m, readiness.ExpectedAcceptanceFee);
        Assert.Equal(100000m, readiness.AvailableWalletBalance);

        var selectedQuote = await CreateAndSubmitQuote(selectedProvider, request.Id, "수전 교체 견적", 120000m);
        var otherQuote = await CreateAndSubmitQuote(otherProvider, request.Id, "비교 견적", 125000m);
        var acceptedResponse = await customer.PostAsync($"/api/v1/quotes/{selectedQuote.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        var acceptedJson = await acceptedResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("chargedFeeAmount", acceptedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("walletLedgerEntryId", acceptedJson, StringComparison.OrdinalIgnoreCase);
        var accepted = JsonSerializer.Deserialize<AcceptQuoteResponse>(acceptedJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var repeated = await customer.PostAsync($"/api/v1/quotes/{selectedQuote.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(accepted.TransactionId, (await repeated.Content.ReadFromJsonAsync<AcceptQuoteResponse>())!.TransactionId);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsync($"/api/v1/quotes/{otherQuote.Id}/accept", null)).StatusCode);

        var selectedWork = await selectedProvider.GetFromJsonAsync<WorkTransactionDetail>($"/api/v1/providers/me/transactions/{accepted.TransactionId}");
        Assert.Equal("01012345678", selectedWork!.CustomerPhone);
        Assert.Equal("101동 1203호", selectedWork.DetailAddress);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync($"/api/v1/providers/me/transactions/{accepted.TransactionId}")).StatusCode);
        var otherAfter = await otherProvider.GetFromJsonAsync<ProviderMatchedRequestDetail>($"/api/v1/providers/me/matched-requests/{request.Id}");
        Assert.Null(otherAfter!.CustomerPhone);
        Assert.Null(otherAfter.DetailAddress);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var transaction = await db.Transactions.SingleAsync(item => item.PublicId == accepted.TransactionId);
        Assert.Equal(3000m, transaction.CalculatedFeeAmount);
        Assert.Equal(3000m, transaction.ActualChargedFeeAmount);
        Assert.NotNull(transaction.CategoryFeePolicyId);
        Assert.NotNull(transaction.FeePolicyVersionSnapshot);
        Assert.NotNull(transaction.FeePolicySnapshotJson);
        Assert.NotNull(transaction.WalletLedgerEntryId);
        Assert.Single(await db.FeeCharges.Where(item => item.TransactionId == transaction.Id).ToListAsync());
        Assert.Single(await db.WalletLedgerEntries.Where(item => item.TransactionId == transaction.Id && item.EntryTypeCode == "USE").ToListAsync());
        var wallet = await db.ProviderWallets.SingleAsync(item => item.ProviderProfileId == transaction.ProviderProfileId);
        Assert.Equal(97000m, wallet.AvailableBalance);

        var adminRequests = await admin.GetFromJsonAsync<AdminRequestListResponse>($"/api/v1/admin/requests?search={request.Id}");
        Assert.Single(adminRequests!.Items);
        var adminRequest = await admin.GetFromJsonAsync<AdminRequestDetailResponse>($"/api/v1/admin/requests/{request.Id}");
        Assert.Equal(2, adminRequest!.Quotes.Count);
        Assert.Equal(accepted.TransactionId, adminRequest.TransactionId);
        var adminTransactions = await admin.GetFromJsonAsync<AdminTransactionListResponse>($"/api/v1/admin/transactions?search={accepted.TransactionId}");
        Assert.Single(adminTransactions!.Items);
        var adminTransaction = await admin.GetFromJsonAsync<AdminTransactionDetailResponse>($"/api/v1/admin/transactions/{accepted.TransactionId}");
        Assert.Equal(3000m, adminTransaction!.FeePolicy!.ActualChargedFeeAmount);
        Assert.Equal("USE", adminTransaction.WalletFee!.LedgerType);
    }

    [Fact]
    public async Task InsufficientWallet_BlocksSubmission_AndCreatesNoTransactionOrFee()
    {
        using var provider = Client();
        using var customer = Client();
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await ConfigureArea(provider);
        int feeCountBefore; int useCountBefore;
        using (var beforeScope = factory.Services.CreateScope())
        {
            var beforeDb = beforeScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            feeCountBefore = await beforeDb.FeeCharges.CountAsync();
            useCountBefore = await beforeDb.WalletLedgerEntries.CountAsync(item => item.EntryTypeCode == "USE");
        }
        await SetProviderBalanceAsync(0);
        var request = await CreateRequest(customer, "현관 조명 점검 요청", "201동 301호");
        var draft = await CreateQuote(provider, request.Id, "조명 점검 견적", 70000m);
        var readiness = await provider.GetFromJsonAsync<QuoteSubmissionReadinessResponse>(
            $"/api/v1/providers/me/requests/{request.Id}/quote-submission-readiness");
        Assert.False(readiness!.CanSubmit);
        Assert.Equal("WALLET_INSUFFICIENT_BALANCE", readiness.UnavailableReason);
        Assert.Equal(HttpStatusCode.Conflict, (await provider.PostAsync($"/api/v1/quotes/{draft.Id}/submit", null)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var internalRequestId = await db.ServiceRequests.Where(item => item.PublicId == request.Id).Select(item => item.Id).SingleAsync();
        Assert.False(await db.Transactions.AnyAsync(item => item.ServiceRequestId == internalRequestId));
        Assert.Equal(feeCountBefore, await db.FeeCharges.CountAsync());
        Assert.Equal(useCountBefore, await db.WalletLedgerEntries.CountAsync(item => item.EntryTypeCode == "USE"));
        var wallet = await db.ProviderWallets.SingleAsync(item => item.AvailableBalance == 0);
        wallet.AvailableBalance = 100000m;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ConcurrentAcceptanceOfTwoQuotes_AllowsOnlyOneTransactionAndOneFeeCharge()
    {
        using var firstProvider = Client(); using var secondProvider = Client(); using var customer = Client();
        await Login(firstProvider, factory.Credentials[RoleCodes.Provider]);
        await Login(secondProvider, factory.AreaMismatchProviderCredential);
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await ResetWalletsAsync();
        await ConfigureArea(firstProvider); await ConfigureArea(secondProvider);
        var request = await CreateRequest(customer, "주방 배수 점검 요청", "301동 502호");
        var first = await CreateAndSubmitQuote(firstProvider, request.Id, "첫 번째 점검 견적", 80000m);
        var second = await CreateAndSubmitQuote(secondProvider, request.Id, "두 번째 점검 견적", 85000m);
        var responses = await Task.WhenAll(
            customer.PostAsync($"/api/v1/quotes/{first.Id}/accept", null),
            customer.PostAsync($"/api/v1/quotes/{second.Id}/accept", null));
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var requestId = await db.ServiceRequests.Where(item => item.PublicId == request.Id).Select(item => item.Id).SingleAsync();
        var transaction = await db.Transactions.SingleAsync(item => item.ServiceRequestId == requestId);
        Assert.Single(await db.FeeCharges.Where(item => item.TransactionId == transaction.Id).ToListAsync());
        Assert.Single(await db.WalletLedgerEntries.Where(item => item.TransactionId == transaction.Id && item.EntryTypeCode == "USE").ToListAsync());
    }

    [Fact]
    public async Task AdminOperations_AreForbiddenToCustomerAndProvider()
    {
        using var customer = Client();
        using var provider = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]);
        await Login(provider, factory.Credentials[RoleCodes.Provider]);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/admin/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/admin/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/v1/admin/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/v1/admin/transactions")).StatusCode);
    }

    private async Task<ServiceRequestCreatedResponse> CreateRequest(HttpClient customer, string title, string detailAddress)
    {
        var response = await customer.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId, administrativeAreaId = factory.Catalog.AreaId, title,
            description = "현장 확인 후 안전하게 작업해 주세요.", detailAddress, isUrgent = false,
            idempotencyKey = $"거래통합-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "증상과 요청사항을 충분히 설명한 고객 요청 내용입니다." },
                new { fieldId = factory.Catalog.FieldIds[1], value = DateTimeOffset.UtcNow.AddDays(2).ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var request = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsync($"/api/v1/requests/{request.Id}/publish", null)).StatusCode);
        return request;
    }

    private async Task<QuoteDetailResponse> CreateAndSubmitQuote(HttpClient provider, Guid requestId, string summary, decimal amount)
    {
        var quote = await CreateQuote(provider, requestId, summary, amount);
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsync($"/api/v1/quotes/{quote.Id}/submit", null)).StatusCode);
        return quote;
    }

    private static async Task<QuoteDetailResponse> CreateQuote(HttpClient provider, Guid requestId, string summary, decimal amount)
    {
        var response = await provider.PostAsJsonAsync($"/api/v1/requests/{requestId}/quotes", new SaveQuoteRevisionInput(
            summary, "작업비와 기본 자재비 포함", 0, "약 2시간", DateTime.UtcNow.AddMinutes(10),
            DateTime.UtcNow.AddMinutes(60), null, $"견적-{Guid.NewGuid():N}",
            [new QuoteItemInput("작업비", "현장 작업", 1, "회", amount)]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<QuoteDetailResponse>())!;
    }

    private async Task ConfigureArea(HttpClient provider)
    {
        var response = await provider.PutAsJsonAsync("/api/v1/providers/me/service-areas", new
        {
            services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } },
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SetCustomerPhoneAsync(string phone)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(item => item.LoginId == factory.Credentials[RoleCodes.Customer].LoginId);
        user.Phone = phone;
        await db.SaveChangesAsync();
    }

    private async Task SetProviderBalanceAsync(decimal balance)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var providerId = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                                where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile.Id).SingleAsync();
        var wallet = await db.ProviderWallets.SingleAsync(item => item.ProviderProfileId == providerId);
        wallet.AvailableBalance = balance;
        await db.SaveChangesAsync();
    }

    private async Task ResetWalletsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        foreach (var wallet in await db.ProviderWallets.ToListAsync()) wallet.AvailableBalance = 100000m;
        await db.SaveChangesAsync();
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
