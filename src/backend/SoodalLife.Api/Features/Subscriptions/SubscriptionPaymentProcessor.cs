using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Subscriptions;

public static class SubscriptionPaymentIdentity
{
    public static string CustomerKey(Guid customerId)=>$"soodal_{customerId:N}";
    public static string OrderId(Guid paymentId)=>$"SUB_{paymentId:N}";
}

public sealed class SubscriptionPaymentProcessor(SoodalLifeDbContext db,ISubscriptionPaymentGateway gateway,ISubscriptionPaymentTokenProtector tokenProtector,CareSubscriptionService careService,ILogger<SubscriptionPaymentProcessor> logger)
{
    public async Task<int> ProcessPendingAsync(CancellationToken token)
    {
        var ids=await db.SubscriptionPaymentRequests.AsNoTracking().Where(x=>x.StatusCode=="REQUESTED"&&x.PaymentMethodId!=null).OrderBy(x=>x.RequestedAt).Select(x=>x.PublicId).Take(50).ToListAsync(token);
        var completed=0;foreach(var id in ids)if(await ChargeAsync(id,token))completed++;return completed;
    }

    public async Task<bool> ChargeAsync(Guid paymentPublicId,CancellationToken token)
    {
        var row=await(from payment in db.SubscriptionPaymentRequests
                      join contract in db.SubscriptionContracts on payment.SubscriptionContractId equals contract.Id
                      join method in db.SubscriptionPaymentMethods on payment.PaymentMethodId equals method.Id
                      join customer in db.CustomerProfiles on payment.CustomerProfileId equals customer.Id
                      where payment.PublicId==paymentPublicId
                      select new{payment,contract,method,customer}).SingleOrDefaultAsync(token);
        if(row is null||row.payment.StatusCode=="COMPLETED")return row?.payment.StatusCode=="COMPLETED";
        if(row.payment.StatusCode!="REQUESTED")return false;
        if(row.method.StatusCode!="ACTIVE"||string.IsNullOrWhiteSpace(row.method.ExternalTokenReference)){await Fail(row.payment,row.contract,"PAYMENT_METHOD_REQUIRED","사용 가능한 자동결제 빌링키가 없습니다.",token);return false;}
        var actor=row.customer.UserId;var now=DateTime.UtcNow;row.payment.StatusCode="PROCESSING";row.payment.AuthorizedAt=now;row.payment.UpdatedAt=now;row.payment.UpdatedByUserId=actor;row.contract.BillingStatusCode="PAYMENT_PROCESSING";row.contract.UpdatedAt=now;row.contract.UpdatedByUserId=actor;
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){return false;}
        try
        {
            var result=await gateway.ChargeRecurringAsync(row.method.ProviderCode??"TOSS",tokenProtector.Unprotect(row.method.ExternalTokenReference),SubscriptionPaymentIdentity.CustomerKey(row.customer.PublicId),SubscriptionPaymentIdentity.OrderId(row.payment.PublicId),"수달 라이프 정기구독",row.payment.RequestedAmount,row.payment.CurrencyCode,$"{row.payment.IdempotencyKey}:attempt:{row.payment.GatewayAttemptNo}",token);
            await Complete(row.payment,row.contract,actor,result.PaymentKey,token);return true;
        }
        catch(SubscriptionPaymentGatewayException error) when(error.Retryable){logger.LogWarning(error,"Subscription payment result is unknown. payment={PaymentId}, code={Code}",row.payment.PublicId,error.Code);await MarkUnknown(row.payment,row.contract,error.Code,error.Message,token);return false;}
        catch(SubscriptionPaymentGatewayException error){logger.LogWarning(error,"Subscription payment failed. payment={PaymentId}, code={Code}",row.payment.PublicId,error.Code);await Fail(row.payment,row.contract,error.Code,error.Message,token);return false;}
        catch(Exception error) when(error is HttpRequestException or TaskCanceledException){logger.LogWarning(error,"Subscription payment transport failed. payment={PaymentId}",row.payment.PublicId);await MarkUnknown(row.payment,row.contract,"PG_RESULT_UNKNOWN","결제기관 응답을 확인하지 못했습니다. 주문번호로 결과를 확인한 뒤 같은 요청키로 재시도합니다.",token);return false;}
    }

    public async Task<bool> ReconcileByOrderIdAsync(Guid paymentPublicId,CancellationToken token)
    {
        var row=await(from payment in db.SubscriptionPaymentRequests join contract in db.SubscriptionContracts on payment.SubscriptionContractId equals contract.Id join customer in db.CustomerProfiles on payment.CustomerProfileId equals customer.Id where payment.PublicId==paymentPublicId select new{payment,contract,customer}).SingleOrDefaultAsync(token);
        if(row is null||row.payment.StatusCode=="COMPLETED")return row?.payment.StatusCode=="COMPLETED";
        try
        {
            var orderId=SubscriptionPaymentIdentity.OrderId(row.payment.PublicId);var status=await gateway.GetPaymentByOrderIdAsync("TOSS",orderId,token);if(status.OrderId!=orderId)return false;
            if(status.Status=="DONE"){if(status.TotalAmount!=row.payment.RequestedAmount){await Fail(row.payment,row.contract,"PG_AMOUNT_MISMATCH","PG 결제금액과 청구금액이 일치하지 않습니다.",token);return false;}await Complete(row.payment,row.contract,row.customer.UserId,status.PaymentKey,token);return true;}
            if(status.Status is "ABORTED" or "EXPIRED"){await Fail(row.payment,row.contract,$"PG_{status.Status}","결제가 승인되지 않았습니다.",token);return false;}
        }
        catch(SubscriptionPaymentGatewayException error) when(error.Code is "NOT_FOUND_PAYMENT" or "NOT_FOUND") { }
        catch(Exception error) when(error is SubscriptionPaymentGatewayException or HttpRequestException or TaskCanceledException){logger.LogWarning(error,"Unable to reconcile subscription payment by order id. payment={PaymentId}",paymentPublicId);}
        return false;
    }

    public async Task ReconcileAsync(string paymentKey,string orderIdHint,CancellationToken token)
    {
        if(!orderIdHint.StartsWith("SUB_",StringComparison.Ordinal)||!Guid.TryParseExact(orderIdHint[4..],"N",out var id)||!await db.SubscriptionPaymentRequests.AsNoTracking().AnyAsync(x=>x.PublicId==id,token))return;var status=await gateway.GetPaymentAsync("TOSS",paymentKey,token);if(status.OrderId!=orderIdHint)return;
        var row=await(from payment in db.SubscriptionPaymentRequests join contract in db.SubscriptionContracts on payment.SubscriptionContractId equals contract.Id join customer in db.CustomerProfiles on payment.CustomerProfileId equals customer.Id where payment.PublicId==id select new{payment,contract,customer}).SingleOrDefaultAsync(token);
        if(row is null)return;
        if(status.Status=="DONE")
        {
            if(status.TotalAmount!=row.payment.RequestedAmount){await Fail(row.payment,row.contract,"PG_AMOUNT_MISMATCH","PG 결제금액과 청구금액이 일치하지 않습니다.",token);return;}
            await Complete(row.payment,row.contract,row.customer.UserId,status.PaymentKey,token);
        }
        else if(status.Status is "ABORTED" or "EXPIRED")await Fail(row.payment,row.contract,$"PG_{status.Status}","결제가 승인되지 않았습니다.",token);
        else if(status.Status is "CANCELED" or "PARTIAL_CANCELED")await ReconcileCancellation(row.payment,row.customer.UserId,status,token);
    }

    private async Task Complete(SubscriptionPaymentRequest payment,SubscriptionContract contract,long actor,string paymentKey,CancellationToken token)
    {
        if(payment.StatusCode=="COMPLETED")return;var ledgerKey=$"subscription-payment:{payment.PublicId:N}:completed";
        if(!await db.SubscriptionPaymentLedger.AnyAsync(x=>x.IdempotencyKey==ledgerKey,token))db.SubscriptionPaymentLedger.Add(new SubscriptionPaymentLedgerEntry{SubscriptionContractId=payment.SubscriptionContractId,PaymentRequestId=payment.Id,EntryTypeCode="PAYMENT",Amount=payment.RequestedAmount,CurrencyCode=payment.CurrencyCode,ReferenceType="PAYMENT_REQUEST",ReferencePublicId=payment.PublicId,OccurredAt=DateTime.UtcNow,ProcessedByUserId=actor,IdempotencyKey=ledgerKey,CreatedAt=DateTime.UtcNow});
        var now=DateTime.UtcNow;var recovering=contract.BillingStatusCode=="OVERDUE";if(contract.StatusCode=="PAYMENT_PENDING"){payment.BillingPeriodStart=DateOnly.FromDateTime(now);payment.BillingPeriodEnd=payment.BillingPeriodStart.AddMonths(1).AddDays(-1);contract.BillingAnchorDay=now.Day;}payment.StatusCode="COMPLETED";payment.ExternalPaymentReference=paymentKey;payment.CompletedAt=now;payment.FailedAt=null;payment.FailureCode=null;payment.FailureReason=null;payment.UpdatedAt=now;payment.UpdatedByUserId=actor;contract.BillingStatusCode="ACTIVE";contract.NextBillingAt=payment.BillingPeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);contract.UpdatedAt=now;contract.UpdatedByUserId=actor;
        if(recovering&&contract.StatusCode=="PAUSED"){contract.StatusCode="ACTIVE";contract.PauseStartedAt=null;foreach(var visit in await db.SubscriptionVisitSchedules.Where(x=>x.SubscriptionContractId==contract.Id&&x.ScheduledStartAt>=now&&x.StatusCode=="PAUSED").ToListAsync(token))visit.StatusCode="SCHEDULED";}
        await db.SaveChangesAsync(token);await careService.ActivateAfterPayment(contract.Id,actor,now,token);
    }

    private async Task Fail(SubscriptionPaymentRequest payment,SubscriptionContract contract,string code,string reason,CancellationToken token)
    {
        var now=DateTime.UtcNow;payment.StatusCode="FAILED";payment.FailedAt=now;payment.FailureCode=code.Length>100?code[..100]:code;payment.FailureReason=reason.Length>1000?reason[..1000]:reason;payment.UpdatedAt=now;contract.BillingStatusCode="OVERDUE";contract.UpdatedAt=now;
        if(contract.StatusCode=="ACTIVE"){contract.StatusCode="PAUSED";contract.PauseStartedAt=now;foreach(var visit in await db.SubscriptionVisitSchedules.Where(x=>x.SubscriptionContractId==contract.Id&&x.ScheduledStartAt>=now&&x.StatusCode=="SCHEDULED").ToListAsync(token))visit.StatusCode="PAUSED";}
        await db.SaveChangesAsync(token);
    }

    private async Task MarkUnknown(SubscriptionPaymentRequest payment,SubscriptionContract contract,string code,string reason,CancellationToken token)
    {
        var now=DateTime.UtcNow;payment.StatusCode="PROCESSING";payment.FailureCode=code.Length>100?code[..100]:code;payment.FailureReason=reason.Length>1000?reason[..1000]:reason;payment.UpdatedAt=now;contract.BillingStatusCode="PAYMENT_RESULT_UNKNOWN";contract.UpdatedAt=now;await db.SaveChangesAsync(token);
    }

    private async Task ReconcileCancellation(SubscriptionPaymentRequest payment,long actor,SubscriptionGatewayPaymentStatus status,CancellationToken token)
    {
        var cancelled=Math.Max(0,payment.RequestedAmount-status.BalanceAmount);if(cancelled<=0)return;var key=$"toss-webhook-cancel:{payment.PublicId:N}:{status.BalanceAmount:0}";if(!await db.SubscriptionPaymentLedger.AnyAsync(x=>x.IdempotencyKey==key,token))db.SubscriptionPaymentLedger.Add(new SubscriptionPaymentLedgerEntry{SubscriptionContractId=payment.SubscriptionContractId,PaymentRequestId=payment.Id,EntryTypeCode="REFUND",Amount=-cancelled,CurrencyCode=payment.CurrencyCode,ReferenceType="PG_WEBHOOK",ReferencePublicId=payment.PublicId,ReasonText="토스 결제 취소 상태 자동 반영",OccurredAt=DateTime.UtcNow,ProcessedByUserId=actor,IdempotencyKey=key,CreatedAt=DateTime.UtcNow});payment.StatusCode=status.BalanceAmount<=0?"REFUNDED":"PARTIALLY_REFUNDED";payment.UpdatedAt=DateTime.UtcNow;payment.UpdatedByUserId=actor;await db.SaveChangesAsync(token);
    }
}
