using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class SubscriptionBillingAndSettlementApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task AccountingEndpoints_AllowAdmin_AndRejectCustomerProvider()
    {
        using var admin=Client();using var customer=Client();using var provider=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);await Login(customer,factory.Credentials[RoleCodes.Customer]);await Login(provider,factory.Credentials[RoleCodes.Provider]);
        Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync("/api/v1/admin/subscription-accounting/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await customer.GetAsync("/api/v1/admin/subscription-accounting/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await provider.GetAsync("/api/v1/admin/subscription-accounting/dashboard")).StatusCode);
    }

    [Fact]
    public void FeeCalculator_OnlyCalculatesExplicitPolicySnapshots()
    {
        var calculator=new SubscriptionSettlementFeeCalculator();
        Assert.False(calculator.Calculate(null,30000).Calculable);
        Assert.Equal(2400,calculator.Calculate("{\"policyCode\":\"SUB-MONTH\",\"monthlyAmount\":30000}",30000).FeeAmount);
        Assert.Equal(2667,calculator.Calculate("{\"policyCode\":\"SUB-MONTH\",\"monthlyAmount\":120000}",33333).FeeAmount);
        Assert.Equal(2667,calculator.Calculate("{\"policyCode\":\"SUB-RATE\",\"rate\":0.15}",33333).FeeAmount);
        Assert.Equal(8000,calculator.Calculate("{\"policyCode\":\"SUB-VISIT\",\"perVisitAmount\":5000}",100000).FeeAmount);
        Assert.Equal(8000,calculator.Calculate("{\"policyCode\":\"SUB-MIX\",\"rate\":0.15,\"perVisitAmount\":5000}",100000).FeeAmount);
        Assert.Equal(2400,calculator.Calculate("{\"policyCode\":\"SUB-RATE\",\"ratePercent\":10}",30000).FeeAmount);
        Assert.Equal(2400,calculator.Calculate("{\"policyCode\":\"SUB-VISIT\",\"perVisitAmount\":2000}",30000).FeeAmount);
        Assert.Equal(2400,calculator.Calculate("{\"policyCode\":\"SUB-MIX\",\"ratePercent\":10,\"perVisitAmount\":2000}",30000).FeeAmount);
    }

    [Fact]
    public async Task PaymentMethod_StoresOnlyReferenceAndMaskedDisplay_AndRejectsSensitiveText()
    {
        var data=await SeedContract(2101,1,true);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);
        var rejected=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payment-methods",new{customerId=data.CustomerId,paymentMethodTypeCode="CARD",providerCode="TEST",externalTokenReference="cvc-should-not-be-stored",maskedDisplayText="카드 **** 1234",isDefault=true});Assert.Equal(HttpStatusCode.BadRequest,rejected.StatusCode);
        var response=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payment-methods",new{customerId=data.CustomerId,paymentMethodTypeCode="CARD",providerCode="TEST",externalTokenReference="vault-reference-alpha-beta",maskedDisplayText="카드 **** 1234",isDefault=true});Assert.Equal(HttpStatusCode.OK,response.StatusCode);var body=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("externalTokenReference",body,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("vault-reference",body,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaymentRequest_IsIdempotent_DevelopmentConfirmationCreatesOneAppendOnlyEntry_AndNeverTouchesWallet()
    {
        var data=await SeedContract(2102,1,true);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);var key=$"payment-{Guid.NewGuid():N}";var request=new{contractId=data.ContractId,paymentMethodId=(Guid?)null,billingPeriodStart=new DateOnly(2102,1,1),billingPeriodEnd=new DateOnly(2102,1,31),requestedAmount=120000m,idempotencyKey=key};
        var first=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payments",request);var second=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payments",request);Assert.Equal(HttpStatusCode.OK,first.StatusCode);Assert.Equal(HttpStatusCode.OK,second.StatusCode);var payment=await first.Content.ReadFromJsonAsync<SubscriptionPaymentResponse>();var repeated=await second.Content.ReadFromJsonAsync<SubscriptionPaymentResponse>();Assert.Equal(payment!.Id,repeated!.Id);
        var confirmationKey=$"payment-confirm-{Guid.NewGuid():N}";var confirmed=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payments/{payment.Id}/development-confirmation",new{idempotencyKey=confirmationKey,externalPaymentReference=(string?)null,rowVersion=payment.RowVersion});Assert.Equal(HttpStatusCode.OK,confirmed.StatusCode);var confirmedAgain=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payments/{payment.Id}/development-confirmation",new{idempotencyKey=confirmationKey,externalPaymentReference=(string?)null,rowVersion=payment.RowVersion});Assert.Equal(HttpStatusCode.OK,confirmedAgain.StatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Single(await db.SubscriptionPaymentLedger.Where(x=>x.PaymentRequestId==db.SubscriptionPaymentRequests.Single(p=>p.PublicId==payment.Id).Id).ToListAsync());Assert.Equal(data.WalletBalance,await db.ProviderWallets.Where(x=>x.Id==data.WalletId).Select(x=>x.AvailableBalance).SingleAsync());Assert.Equal(data.WalletLedgerCount,await db.WalletLedgerEntries.CountAsync());Assert.Equal(data.FeeChargeCount,await db.FeeCharges.CountAsync());Assert.Equal(data.TrustEventCount,await db.TrustScoreEvents.CountAsync());
    }

    [Fact]
    public async Task PaymentAndPayoutLedgers_AreAppendOnly()
    {
        var data=await SeedContract(2103,1,true);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var entry=new SubscriptionPaymentLedgerEntry{SubscriptionContractId=data.ContractDbId,EntryTypeCode="ADJUSTMENT",Amount=1,CurrencyCode="KRW",ReferenceType="TEST",OccurredAt=DateTime.UtcNow,IdempotencyKey=$"append-{Guid.NewGuid():N}",CreatedAt=DateTime.UtcNow};db.SubscriptionPaymentLedger.Add(entry);await db.SaveChangesAsync();entry.Amount=2;await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();var payout=new SubscriptionPayout{MonthlySettlementId=await SeedApprovedSettlement(db,data.ProviderDbId,2103,2),ProviderProfileId=data.ProviderDbId,RequestedAmount=1000,StatusCode="REQUESTED",RequestedAt=DateTime.UtcNow,IdempotencyKey=$"payout-{Guid.NewGuid():N}",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.SubscriptionPayouts.Add(payout);await db.SaveChangesAsync();var payoutEvent=new SubscriptionPayoutEvent{SubscriptionPayoutId=payout.Id,EventTypeCode="REQUESTED",OccurredAt=DateTime.UtcNow,IdempotencyKey=$"payout-event-{Guid.NewGuid():N}"};db.SubscriptionPayoutEvents.Add(payoutEvent);await db.SaveChangesAsync();payoutEvent.EventTypeCode="CHANGED";await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
    }

    [Fact]
    public async Task SettlementPreparation_IncludesOnlyReadyCompletedVisit_AndLeavesUnknownPolicyPending()
    {
        var ready=await SeedContract(2104,1,false);await AddExcludedVisits(ready,2104,1);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);await CreateAndConfirmPayment(admin,ready,2104,1);var response=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/settlement-items/prepare",new{year=2104,month=1,idempotencyKey=$"prepare-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);var items=await response.Content.ReadFromJsonAsync<List<SubscriptionSettlementItemResponse>>();var item=Assert.Single(items!,x=>x.ContractId==ready.ContractId);Assert.Equal("POLICY_PENDING",item.StatusCode);Assert.Null(item.CalculatedFeeAmount);
        var monthly=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/monthly-settlements",new{providerId=ready.ProviderId,year=2104,month=1,idempotencyKey=$"monthly-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,monthly.StatusCode);var header=await monthly.Content.ReadFromJsonAsync<MonthlySettlementResponse>();var approve=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/monthly-settlements/{header!.Id}/approve",new{idempotencyKey=$"approve-{Guid.NewGuid():N}",rowVersion=header.RowVersion});Assert.Equal(HttpStatusCode.Conflict,approve.StatusCode);
    }

    [Fact]
    public async Task MonthlySettlementAndPayout_AreUniqueIdempotent_AndDevelopmentCompletionDoesNotCreateWalletEntries()
    {
        var data=await SeedContract(2105,1,true);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);await CreateAndConfirmPayment(admin,data,2105,1);await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/settlement-items/prepare",new{year=2105,month=1,idempotencyKey=$"prepare-{Guid.NewGuid():N}"});var monthlyKey=$"monthly-{Guid.NewGuid():N}";var monthlyResponse=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/monthly-settlements",new{providerId=data.ProviderId,year=2105,month=1,idempotencyKey=monthlyKey});var header=await monthlyResponse.Content.ReadFromJsonAsync<MonthlySettlementResponse>();Assert.Equal(30000,header!.GrossTotal);Assert.Equal(2400,header.FeeTotal);Assert.Equal(27600,header.NetTotal);var repeated=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/monthly-settlements",new{providerId=data.ProviderId,year=2105,month=1,idempotencyKey=monthlyKey});Assert.Equal(header.Id,(await repeated.Content.ReadFromJsonAsync<MonthlySettlementResponse>())!.Id);
        var approvedResponse=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/monthly-settlements/{header.Id}/approve",new{idempotencyKey=$"approve-{Guid.NewGuid():N}",rowVersion=header.RowVersion});var approved=await approvedResponse.Content.ReadFromJsonAsync<MonthlySettlementResponse>();var payoutResponse=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payouts",new{monthlySettlementId=approved!.Id,idempotencyKey=$"payout-{Guid.NewGuid():N}"});var payout=await payoutResponse.Content.ReadFromJsonAsync<SubscriptionPayoutResponse>();var payoutApprovedResponse=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payouts/{payout!.Id}/approve",new{approvedAmount=payout.RequestedAmount,idempotencyKey=$"payout-approve-{Guid.NewGuid():N}",rowVersion=payout.RowVersion});var payoutApproved=await payoutApprovedResponse.Content.ReadFromJsonAsync<SubscriptionPayoutResponse>();var complete=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payouts/{payout.Id}/bank-transfer-completion",new{bankTransferReference=$"BANK-{Guid.NewGuid():N}",reason="관리자 은행 송금 완료",idempotencyKey=$"payout-complete-{Guid.NewGuid():N}",rowVersion=payoutApproved!.RowVersion});Assert.Equal(HttpStatusCode.OK,complete.StatusCode);Assert.Equal("COMPLETED",(await complete.Content.ReadFromJsonAsync<SubscriptionPayoutResponse>())!.StatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(data.WalletBalance,await db.ProviderWallets.Where(x=>x.Id==data.WalletId).Select(x=>x.AvailableBalance).SingleAsync());Assert.Equal(data.WalletLedgerCount,await db.WalletLedgerEntries.CountAsync());Assert.Equal(data.FeeChargeCount,await db.FeeCharges.CountAsync());Assert.Equal(data.TrustEventCount,await db.TrustScoreEvents.CountAsync());
    }

    [Fact]
    public async Task AutonomousSettlement_CalculatesEightPercent_AndStopsAtAdminDraft()
    {
        var data=await SeedContract(2110,1,true);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);await CreateAndConfirmPayment(admin,data,2110,1);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var service=scope.ServiceProvider.GetRequiredService<SubscriptionBillingService>();await service.ProcessAutonomousSettlements(default);
        var item=await db.SubscriptionSettlementItems.SingleAsync(x=>x.SubscriptionContractId==data.ContractDbId);Assert.Equal(30000,item.GrossAmount);Assert.Equal(2400,item.CalculatedFeeAmount);Assert.Equal(27600,item.NetAmount);Assert.Equal("READY",item.StatusCode);
        var monthly=await db.MonthlySettlements.SingleAsync(x=>x.Id==item.MonthlySettlementId);Assert.Equal("DRAFT",monthly.StatusCode);Assert.Equal(2400,monthly.FeeTotal);Assert.False(await db.SubscriptionPayouts.AnyAsync(x=>x.MonthlySettlementId==monthly.Id));
    }

    [Fact]
    public async Task RefundRequiresManualAmount_CompletionAddsSingleReverseLedger_WithoutAutomaticTrustOrWalletChange()
    {
        var data=await SeedContract(2106,1,true);using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);var payment=await CreateAndConfirmPayment(admin,data,2106,1);var key=$"refund-{Guid.NewGuid():N}";var create=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/refund-adjustments",new{contractId=data.ContractId,paymentRequestId=payment.Id,visitId=(Guid?)null,typeCode="REFUND",requestedAmount=10000m,reason="관리자 수동 검토 필요",idempotencyKey=key});var refund=await create.Content.ReadFromJsonAsync<SubscriptionRefundAdjustmentResponse>();Assert.Null(refund!.ApprovedAmount);var duplicate=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/refund-adjustments",new{contractId=data.ContractId,paymentRequestId=payment.Id,visitId=(Guid?)null,typeCode="REFUND",requestedAmount=10000m,reason="관리자 수동 검토 필요",idempotencyKey=key});Assert.Equal(refund.Id,(await duplicate.Content.ReadFromJsonAsync<SubscriptionRefundAdjustmentResponse>())!.Id);
        var approvedResponse=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/refund-adjustments/{refund.Id}/approve",new{approvedAmount=8000m,idempotencyKey=$"refund-approve-{Guid.NewGuid():N}",rowVersion=refund.RowVersion});var approved=await approvedResponse.Content.ReadFromJsonAsync<SubscriptionRefundAdjustmentResponse>();var completionKey=$"refund-complete-{Guid.NewGuid():N}";var complete=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/refund-adjustments/{refund.Id}/development-completion",new{approvedAmount=approved!.ApprovedAmount,idempotencyKey=completionKey,rowVersion=approved.RowVersion});Assert.Equal(HttpStatusCode.OK,complete.StatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Single(await db.SubscriptionPaymentLedger.ToListAsync(),x=>x.ReferencePublicId==refund.Id&&x.EntryTypeCode=="REFUND"&&x.Amount==-8000);Assert.Equal(data.WalletBalance,await db.ProviderWallets.Where(x=>x.Id==data.WalletId).Select(x=>x.AvailableBalance).SingleAsync());Assert.Equal(data.WalletLedgerCount,await db.WalletLedgerEntries.CountAsync());Assert.Equal(data.FeeChargeCount,await db.FeeCharges.CountAsync());Assert.Equal(data.TrustEventCount,await db.TrustScoreEvents.CountAsync());
    }

    [Fact]
    public async Task ProductionEnvironmentBlocksManualPaymentPayoutAndRefundConfirmation()
    {
        var options=new DbContextOptionsBuilder<SoodalLifeDbContext>().UseInMemoryDatabase($"production-{Guid.NewGuid():N}").ConfigureWarnings(warnings=>warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)).Options;await using var db=new SoodalLifeDbContext(options);var service=new SubscriptionBillingService(db,new TestEnvironment("Production"),new SubscriptionSettlementFeeCalculator());var paymentError=await Assert.ThrowsAsync<SubscriptionBusinessException>(()=>service.ConfirmPayment(Guid.NewGuid(),new DevelopmentPaymentConfirmationRequest("key",null,""),Guid.NewGuid(),default));var payoutError=await Assert.ThrowsAsync<SubscriptionBusinessException>(()=>service.CompletePayout(Guid.NewGuid(),new PayoutDecisionRequest(null,"key",""),Guid.NewGuid(),default));var refundError=await Assert.ThrowsAsync<SubscriptionBusinessException>(()=>service.CompleteRefund(Guid.NewGuid(),new RefundAdjustmentDecisionRequest(null,"key",""),Guid.NewGuid(),default));Assert.All(new[]{paymentError,payoutError,refundError},error=>Assert.Equal(HttpStatusCode.NotFound,(HttpStatusCode)error.StatusCode));
    }

    private async Task<Seeded> SeedContract(int year,int month,bool knownPolicy)
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var customer=await db.CustomerProfiles.FirstAsync();var provider=await db.ProviderProfiles.FirstAsync();var category=await db.ServiceCategories.SingleAsync(x=>x.PublicId==factory.Catalog.ServiceId);var area=await db.AdministrativeAreas.SingleAsync(x=>x.PublicId==factory.Catalog.AreaId);var wallet=await db.ProviderWallets.SingleAsync(x=>x.ProviderProfileId==provider.Id);var now=DateTime.UtcNow;
        var request=new SubscriptionRequest{CustomerProfileId=customer.Id,ServiceCategoryId=category.Id,AdministrativeAreaId=area.Id,RequestTypeCode="CUSTOM",RequestedScopeText="정기 방문 서비스",PreferredStartDate=new DateOnly(year,month,1),StatusCode="CONTRACTED",CreatedAt=now,UpdatedAt=now};db.SubscriptionRequests.Add(request);await db.SaveChangesAsync();var application=new SubscriptionApplication{SubscriptionRequestId=request.Id,ProviderProfileId=provider.Id,ProposedScopeText="정기 방문 서비스",ProposedMonthlyAmount=120000,ProposedVisitAmount=30000,StatusCode="SELECTED",SubmittedAt=now,IdempotencyKey=$"seed-application-{Guid.NewGuid():N}",CreatedAt=now,UpdatedAt=now};db.SubscriptionApplications.Add(application);await db.SaveChangesAsync();var contract=new SubscriptionContract{SubscriptionRequestId=request.Id,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,ServiceCategoryId=category.Id,SubscriptionApplicationId=application.Id,StatusCode="ACTIVE",StartedAt=now,PriceSnapshotJson="{\"monthlyAmount\":120000,\"visitAmount\":30000}",FeePolicySnapshotJson=knownPolicy?"{\"policyCode\":\"SUB-RATE\",\"ratePercent\":10}":null,ServiceScopeSnapshotJson="{}",RecurrenceSnapshotJson="{}",CurrencyCode="KRW",CreatedAt=now,UpdatedAt=now};db.SubscriptionContracts.Add(contract);await db.SaveChangesAsync();request.SelectedApplicationId=application.Id;var visit=new SubscriptionVisitSchedule{SubscriptionContractId=contract.Id,VisitNo=1,ProviderProfileId=provider.Id,ScheduledStartAt=new DateTime(year,month,15,10,0,0,DateTimeKind.Utc),StatusCode="COMPLETED",ProviderCompletionSubmittedAt=now,CustomerConfirmedAt=now,SettlementStatusCode="READY",CreatedAt=now,UpdatedAt=now};db.SubscriptionVisitSchedules.Add(visit);await db.SaveChangesAsync();return new(contract.PublicId,contract.Id,customer.PublicId,provider.PublicId,provider.Id,visit.PublicId,wallet.Id,wallet.AvailableBalance,await db.WalletLedgerEntries.CountAsync(),await db.FeeCharges.CountAsync(),await db.TrustScoreEvents.CountAsync());
    }

    private async Task AddExcludedVisits(Seeded data,int year,int month){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var now=DateTime.UtcNow;var values=new[]{("COMPLETED","NOT_READY"),("COMPLETED","HOLD"),("SKIPPED","NOT_READY"),("CANCELLED","NOT_READY")};var number=2;foreach(var value in values)db.SubscriptionVisitSchedules.Add(new SubscriptionVisitSchedule{SubscriptionContractId=data.ContractDbId,VisitNo=number++,ProviderProfileId=data.ProviderDbId,ScheduledStartAt=new DateTime(year,month,16+number,10,0,0,DateTimeKind.Utc),StatusCode=value.Item1,SettlementStatusCode=value.Item2,CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync();}
    private static async Task<long> SeedApprovedSettlement(SoodalLifeDbContext db,long providerId,int year,int month){var row=new MonthlySettlement{ProviderProfileId=providerId,SettlementYear=year,SettlementMonth=month,StatusCode="APPROVED",NetTotal=1000,IdempotencyKey=$"seed-settlement-{Guid.NewGuid():N}",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.MonthlySettlements.Add(row);await db.SaveChangesAsync();return row.Id;}
    private async Task<SubscriptionPaymentResponse> CreateAndConfirmPayment(HttpClient admin,Seeded data,int year,int month){var response=await admin.PostAsJsonAsync("/api/v1/admin/subscription-accounting/payments",new{contractId=data.ContractId,paymentMethodId=(Guid?)null,billingPeriodStart=new DateOnly(year,month,1),billingPeriodEnd=new DateOnly(year,month,28),requestedAmount=120000m,idempotencyKey=$"payment-{Guid.NewGuid():N}"});var payment=await response.Content.ReadFromJsonAsync<SubscriptionPaymentResponse>();var confirmed=await admin.PostAsJsonAsync($"/api/v1/admin/subscription-accounting/payments/{payment!.Id}/development-confirmation",new{idempotencyKey=$"payment-confirm-{Guid.NewGuid():N}",externalPaymentReference=(string?)null,rowVersion=payment.RowVersion});return(await confirmed.Content.ReadFromJsonAsync<SubscriptionPaymentResponse>())!;}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});private static Task<HttpResponseMessage> Login(HttpClient client,TestCredential credential)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=credential.LoginId,credential.Password});
    private sealed record Seeded(Guid ContractId,long ContractDbId,Guid CustomerId,Guid ProviderId,long ProviderDbId,Guid VisitId,long WalletId,decimal WalletBalance,int WalletLedgerCount,int FeeChargeCount,int TrustEventCount);
    private sealed class TestEnvironment(string name):IWebHostEnvironment{public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="Tests";public string WebRootPath{get;set;}="";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
