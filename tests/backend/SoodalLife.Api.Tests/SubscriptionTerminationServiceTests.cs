using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class SubscriptionTerminationServiceTests
{
    [Fact]
    public async Task Termination_CalculatesUnusedVisits_RefundsPg_AdjustsState_AndTerminates()
    {
        await using var db=Db();var seeded=await Seed(db,false);var gateway=new FakeGateway();var service=new SubscriptionTerminationService(db,gateway);var now=DateTime.UtcNow;
        await service.RequestTerminationAsync(seeded.Contract,1,"고객 해지","terminate-test",now,default);
        var refund=await db.SubscriptionRefundAdjustments.SingleAsync();Assert.Equal(90000m,refund.RequestedAmount);Assert.Equal("COMPLETED",refund.StatusCode);Assert.Equal(0m,refund.ProviderAdjustmentAmount);Assert.Contains("UNUSED_VISIT_RATIO",refund.CalculationJson);
        Assert.Equal("TERMINATED",seeded.Contract.StatusCode);Assert.Equal("NOT_REQUIRED",seeded.Contract.GatewayTerminationStatusCode);Assert.Equal("ACTIVE",seeded.Method.StatusCode);Assert.Equal("PARTIALLY_REFUNDED",seeded.Payment.StatusCode);Assert.Equal(-90000m,(await db.SubscriptionPaymentLedger.SingleAsync()).Amount);Assert.Equal(0,gateway.CancelCount);Assert.Equal(1,gateway.RefundCount);
    }

    [Fact]
    public async Task Termination_WaitsForAfterServiceAndDisputeClosure_ThenContinuesAutomatically()
    {
        await using var db=Db();var seeded=await Seed(db,true);var gateway=new FakeGateway();var service=new SubscriptionTerminationService(db,gateway);var now=DateTime.UtcNow;
        await service.RequestTerminationAsync(seeded.Contract,1,"A/S 확인 후 해지","terminate-cases",now,default);
        Assert.Equal("TERMINATION_REQUESTED",seeded.Contract.StatusCode);Assert.Equal("WAITING_CASES",(await db.SubscriptionRefundAdjustments.SingleAsync()).StatusCode);Assert.Equal(0,gateway.RefundCount);
        var after=await db.AfterServiceCases.SingleAsync();after.StatusCode="COMPLETED";var dispute=await db.DisputeCases.SingleAsync();dispute.StatusCode="RESOLVED";await db.SaveChangesAsync();await service.ProcessPendingAsync(default);
        Assert.Equal("TERMINATED",seeded.Contract.StatusCode);Assert.Equal("COMPLETED",(await db.SubscriptionRefundAdjustments.SingleAsync()).StatusCode);Assert.Equal(1,gateway.RefundCount);
    }

    private static SoodalLifeDbContext Db()=>new(new DbContextOptionsBuilder<SoodalLifeDbContext>().UseInMemoryDatabase($"termination-{Guid.NewGuid():N}").ConfigureWarnings(warnings=>warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)).Options);

    private static async Task<Seeded> Seed(SoodalLifeDbContext db,bool openCases)
    {
        var now=DateTime.UtcNow;var today=DateOnly.FromDateTime(now);var contract=new SubscriptionContract{StatusCode="ACTIVE",CustomerProfileId=10,ProviderProfileId=20,ServiceCategoryId=30,SubscriptionRequestId=40,SubscriptionApplicationId=50,StartedAt=now.AddMonths(-1),PriceSnapshotJson="{\"monthlyAmount\":120000,\"visitAmount\":30000}",BillingStatusCode="ACTIVE",NextBillingAt=now.AddDays(10),CreatedAt=now,UpdatedAt=now};db.SubscriptionContracts.Add(contract);await db.SaveChangesAsync();
        var method=new SubscriptionPaymentMethod{CustomerProfileId=10,ProviderCode="TOSS",PaymentMethodTypeCode="CARD",ExternalTokenReference="billing-key",StatusCode="ACTIVE",IsDefault=true,RegisteredAt=now,CreatedAt=now,UpdatedAt=now};db.SubscriptionPaymentMethods.Add(method);await db.SaveChangesAsync();
        var payment=new SubscriptionPaymentRequest{SubscriptionContractId=contract.Id,CustomerProfileId=10,PaymentMethodId=method.Id,BillingPeriodStart=today.AddDays(-5),BillingPeriodEnd=today.AddDays(25),RequestedAmount=120000,StatusCode="COMPLETED",ExternalPaymentReference="payment-key",RequestedAt=now.AddDays(-5),CompletedAt=now.AddDays(-5),IdempotencyKey=$"payment-{Guid.NewGuid():N}",CreatedAt=now,UpdatedAt=now};db.SubscriptionPaymentRequests.Add(payment);await db.SaveChangesAsync();
        var visits=new List<SubscriptionVisitSchedule>();for(var index=0;index<4;index++)visits.Add(new SubscriptionVisitSchedule{SubscriptionContractId=contract.Id,VisitNo=index+1,ProviderProfileId=20,ScheduledStartAt=now.AddDays(index==0?-1:index+2),StatusCode=index==0?"COMPLETED":"SCHEDULED",SettlementStatusCode=index==0?"READY":"NOT_READY",CreatedAt=now,UpdatedAt=now});db.SubscriptionVisitSchedules.AddRange(visits);await db.SaveChangesAsync();
        if(openCases){db.AfterServiceCases.Add(new AfterServiceCase{SubscriptionVisitScheduleId=visits[0].Id,CustomerProfileId=10,ProviderProfileId=20,StatusCode="RECEIVED",Subject="A/S",Description="확인",ReceivedAt=now,IdempotencyKey=$"as-{Guid.NewGuid():N}",CreatedAt=now,UpdatedAt=now});db.DisputeCases.Add(new DisputeCase{SubscriptionVisitScheduleId=visits[0].Id,ApplicantUserId=1,CounterpartyUserId=2,StatusCode="OPEN",Subject="분쟁",Description="확인",ReceivedAt=now,CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync();}
        return new(contract,method,payment);
    }

    private sealed record Seeded(SubscriptionContract Contract,SubscriptionPaymentMethod Method,SubscriptionPaymentRequest Payment);
    private sealed class FakeGateway : ISubscriptionPaymentGateway
    {
        public int CancelCount { get; private set; } public int RefundCount { get; private set; }
        public Task<SubscriptionGatewayBillingKeyResult> IssueBillingKeyAsync(string providerCode,string authKey,string customerKey,string idempotencyKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayBillingKeyResult("billing","CARD","****1111"));
        public Task<SubscriptionGatewayPaymentResult> ChargeRecurringAsync(string providerCode,string billingKey,string customerKey,string orderId,string orderName,decimal amount,string currencyCode,string idempotencyKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentResult("payment",null));
        public Task<SubscriptionGatewayPaymentStatus> GetPaymentAsync(string providerCode,string paymentKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentStatus(paymentKey,"SUB_"+Guid.NewGuid().ToString("N"),"DONE",100,100));
        public Task<SubscriptionGatewayPaymentStatus> GetPaymentByOrderIdAsync(string providerCode,string orderId,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentStatus("payment",orderId,"DONE",100,100));
        public Task CancelBillingKeyAsync(string providerCode,string billingKey,string idempotencyKey,CancellationToken token){CancelCount++;return Task.CompletedTask;}
        public Task<SubscriptionGatewayPaymentResult> RefundPaymentAsync(string providerCode,string paymentKey,decimal amount,string reason,string idempotencyKey,CancellationToken token){RefundCount++;return Task.FromResult(new SubscriptionGatewayPaymentResult(paymentKey,$"refund-{RefundCount}"));}
    }
}
