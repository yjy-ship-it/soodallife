using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class SubscriptionTerminationService(SoodalLifeDbContext db,ISubscriptionPaymentGateway gateway)
{
    private static readonly string[] OpenVisitStatuses=["IN_PROGRESS","PROVIDER_COMPLETED","DISPUTED"];
    private static readonly string[] ClosedAfterServiceStatuses=["COMPLETED","CLOSED","REJECTED","CANCELLED","CONVERTED_TO_DISPUTE"];
    private static readonly string[] ClosedDisputeStatuses=["RESOLVED","CLOSED","REJECTED","CANCELLED"];
    private static readonly string[] OpenRefundStatuses=["REQUESTED","WAITING_CASES","MANUAL_REQUIRED","APPROVED","PROCESSING","FAILED"];

    public async Task RequestTerminationAsync(SubscriptionContract contract,long actor,string? reason,string idempotencyKey,DateTime now,CancellationToken token)
    {
        contract.StatusCode="TERMINATION_REQUESTED";contract.TerminationRequestedAt=now;contract.TerminationReason=reason;contract.BillingStatusCode="STOPPED";contract.NextBillingAt=null;contract.UpdatedAt=now;contract.UpdatedByUserId=actor;
        foreach(var visit in await db.SubscriptionVisitSchedules.Where(x=>x.SubscriptionContractId==contract.Id&&x.ScheduledStartAt>=now&&(x.StatusCode=="SCHEDULED"||x.StatusCode=="PAUSED")).ToListAsync(token)){visit.StatusCode="CANCELLED";visit.UpdatedAt=now;visit.UpdatedByUserId=actor;}
        foreach(var payment in await db.SubscriptionPaymentRequests.Where(x=>x.SubscriptionContractId==contract.Id&&(x.StatusCode=="REQUESTED"||x.StatusCode=="PROCESSING"||x.StatusCode=="FAILED")).ToListAsync(token)){payment.StatusCode="CANCELLED";payment.CancelledAt=now;payment.UpdatedAt=now;payment.UpdatedByUserId=actor;}

        contract.GatewayTerminationStatusCode="NOT_REQUIRED";
        await CreateAutomaticRefunds(contract,actor,idempotencyKey,now,token);
        await db.SaveChangesAsync(token);
        await ProcessContractAsync(contract,actor,now,token);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken token)
    {
        var contracts=await db.SubscriptionContracts.Where(x=>x.StatusCode=="TERMINATION_REQUESTED").ToListAsync(token);var completed=0;var now=DateTime.UtcNow;
        foreach(var contract in contracts){var before=contract.StatusCode;await RetryGatewayCancellation(contract,now,token);await ProcessContractAsync(contract,contract.UpdatedByUserId??contract.CreatedByUserId??0,now,token);if(before!=contract.StatusCode)completed++;}
        return completed;
    }

    public async Task ProcessContractAsync(SubscriptionContract contract,long actor,DateTime now,CancellationToken token)
    {
        if(await HasOpenCases(contract.Id,token))
        {
            foreach(var refund in await db.SubscriptionRefundAdjustments.Where(x=>x.SubscriptionContractId==contract.Id&&OpenRefundStatuses.Contains(x.StatusCode)).ToListAsync(token))if(refund.StatusCode!="MANUAL_REQUIRED")refund.StatusCode="WAITING_CASES";
            await db.SaveChangesAsync(token);return;
        }
        var refunds=await db.SubscriptionRefundAdjustments.Where(x=>x.SubscriptionContractId==contract.Id&&OpenRefundStatuses.Contains(x.StatusCode)).ToListAsync(token);
        foreach(var refund in refunds)await ProcessRefund(contract,refund,actor,now,token);
        await AdjustSettlementTargets(contract.Id,now,actor,token);
        var hasOpenRefund=await db.SubscriptionRefundAdjustments.AnyAsync(x=>x.SubscriptionContractId==contract.Id&&OpenRefundStatuses.Contains(x.StatusCode),token);
        var gatewayDone=contract.GatewayTerminationStatusCode is null or "NOT_REQUIRED" or "COMPLETED";
        if(!hasOpenRefund&&gatewayDone&&!await HasOpenCases(contract.Id,token)){contract.StatusCode="TERMINATED";contract.TerminatedAt=now;contract.EndedAt=now;contract.UpdatedAt=now;contract.UpdatedByUserId=actor;}
        await db.SaveChangesAsync(token);
    }

    private async Task CreateAutomaticRefunds(SubscriptionContract contract,long actor,string requestKey,DateTime now,CancellationToken token)
    {
        var today=DateOnly.FromDateTime(now);var payments=await db.SubscriptionPaymentRequests.Where(x=>x.SubscriptionContractId==contract.Id&&(x.StatusCode=="COMPLETED"||x.StatusCode=="PARTIALLY_REFUNDED")&&x.BillingPeriodEnd>=today).ToListAsync(token);
        foreach(var payment in payments)
        {
            var existing=await db.SubscriptionRefundAdjustments.Where(x=>x.PaymentRequestId==payment.Id&&x.TypeCode=="REFUND"&&x.StatusCode!="REJECTED"&&x.StatusCode!="CANCELLED").SumAsync(x=>(decimal?)x.RequestedAmount,token)??0m;
            var visits=await db.SubscriptionVisitSchedules.Where(x=>x.SubscriptionContractId==contract.Id&&DateOnly.FromDateTime(x.ScheduledStartAt)>=payment.BillingPeriodStart&&DateOnly.FromDateTime(x.ScheduledStartAt)<=payment.BillingPeriodEnd).ToListAsync(token);var completed=visits.Count(x=>x.StatusCode=="COMPLETED");decimal amount;string method;
            if(visits.Count>0){amount=payment.RequestedAmount*(visits.Count-completed)/visits.Count;method="UNUSED_VISIT_RATIO";}
            else{var effective=today<payment.BillingPeriodStart?payment.BillingPeriodStart:today;var total=payment.BillingPeriodEnd.DayNumber-payment.BillingPeriodStart.DayNumber+1;var remaining=Math.Max(0,payment.BillingPeriodEnd.DayNumber-effective.DayNumber+1);amount=payment.RequestedAmount*remaining/total;method="REMAINING_DAY_RATIO";}
            amount=decimal.Round(Math.Max(0,amount-existing),0,MidpointRounding.AwayFromZero);if(amount<=0)continue;var key=$"subscription-termination-refund:{contract.PublicId:N}:{payment.PublicId:N}";if(await db.SubscriptionRefundAdjustments.AnyAsync(x=>x.IdempotencyKey==key,token))continue;
            db.SubscriptionRefundAdjustments.Add(new SubscriptionRefundAdjustment{SubscriptionContractId=contract.Id,PaymentRequestId=payment.Id,TypeCode="REFUND",RequestedAmount=amount,ApprovedAmount=amount,ProviderAdjustmentAmount=0,Reason="구독 해지에 따른 남은 기간·미사용 회차 자동 환불",CalculationJson=JsonSerializer.Serialize(new{method,payment.BillingPeriodStart,payment.BillingPeriodEnd,payment.RequestedAmount,totalVisits=visits.Count,completedVisits=completed,previousRefundAmount=existing,refundAmount=amount,terminationRequestedAt=now}),StatusCode="REQUESTED",RequestedAt=now,ApprovedAt=now,ProcessedByUserId=actor,IdempotencyKey=key,CreatedAt=now,CreatedByUserId=actor,UpdatedAt=now,UpdatedByUserId=actor});
        }
    }

    private async Task ProcessRefund(SubscriptionContract contract,SubscriptionRefundAdjustment refund,long actor,DateTime now,CancellationToken token)
    {
        if(!refund.PaymentRequestId.HasValue){refund.StatusCode="MANUAL_REQUIRED";refund.FailureCode="PAYMENT_REFERENCE_REQUIRED";refund.FailureReason="원 결제 연결이 없어 관리자가 확인해야 합니다.";return;}
        var payment=await db.SubscriptionPaymentRequests.SingleAsync(x=>x.Id==refund.PaymentRequestId,token);if(string.IsNullOrWhiteSpace(payment.ExternalPaymentReference)){refund.StatusCode="MANUAL_REQUIRED";refund.FailureCode="PG_PAYMENT_KEY_REQUIRED";refund.FailureReason="운영 PG 결제키가 없는 이전 결제이므로 관리자가 별도 환불해야 합니다.";return;}
        var method=payment.PaymentMethodId.HasValue?await db.SubscriptionPaymentMethods.SingleOrDefaultAsync(x=>x.Id==payment.PaymentMethodId,token):null;var provider=method?.ProviderCode??"TOSS";refund.StatusCode="PROCESSING";refund.UpdatedAt=now;await db.SaveChangesAsync(token);
        try
        {
            var result=await gateway.RefundPaymentAsync(provider,payment.ExternalPaymentReference,refund.ApprovedAmount??refund.RequestedAmount,refund.Reason,$"{refund.IdempotencyKey}:pg",token);refund.StatusCode="COMPLETED";refund.CompletedAt=now;refund.ExternalRefundReference=result.TransactionKey??result.PaymentKey;refund.FailureCode=null;refund.FailureReason=null;refund.ProcessedByUserId=actor;refund.UpdatedAt=now;refund.UpdatedByUserId=actor;
            var ledgerKey=$"{refund.IdempotencyKey}:ledger";if(!await db.SubscriptionPaymentLedger.AnyAsync(x=>x.IdempotencyKey==ledgerKey,token))db.SubscriptionPaymentLedger.Add(new SubscriptionPaymentLedgerEntry{SubscriptionContractId=contract.Id,PaymentRequestId=payment.Id,EntryTypeCode="REFUND",Amount=-(refund.ApprovedAmount??refund.RequestedAmount),CurrencyCode=contract.CurrencyCode,ReferenceType="REFUND_ADJUSTMENT",ReferencePublicId=refund.PublicId,ReasonText=refund.Reason,OccurredAt=now,ProcessedByUserId=actor,IdempotencyKey=ledgerKey,CreatedAt=now});var refunded=await db.SubscriptionRefundAdjustments.Where(x=>x.PaymentRequestId==payment.Id&&x.TypeCode=="REFUND"&&x.StatusCode=="COMPLETED"&&x.Id!=refund.Id).SumAsync(x=>(decimal?)x.ApprovedAmount,token)??0m;refunded+=refund.ApprovedAmount??refund.RequestedAmount;payment.StatusCode=refunded>=payment.RequestedAmount?"REFUNDED":"PARTIALLY_REFUNDED";payment.UpdatedAt=now;payment.UpdatedByUserId=actor;
        }
        catch(SubscriptionPaymentGatewayException ex){refund.StatusCode=ex.Code=="PG_NOT_CONFIGURED"?"MANUAL_REQUIRED":"FAILED";refund.FailureCode=ex.Code;refund.FailureReason=ex.Message;refund.UpdatedAt=now;refund.UpdatedByUserId=actor;}
        await db.SaveChangesAsync(token);
    }

    private async Task AdjustSettlementTargets(long contractId,DateTime now,long actor,CancellationToken token)
    {
        var items=await(from item in db.SubscriptionSettlementItems join visit in db.SubscriptionVisitSchedules on item.SubscriptionVisitScheduleId equals visit.Id where item.SubscriptionContractId==contractId&&item.MonthlySettlementId==null&&(visit.StatusCode=="CANCELLED"||visit.StatusCode=="SKIPPED") select item).ToListAsync(token);foreach(var item in items){item.StatusCode="CANCELLED";item.AdjustmentAmount=-(item.NetAmount??0);item.NetAmount=0;item.HoldReason="구독 해지·환불로 정산 대상에서 자동 제외";item.UpdatedAt=now;item.UpdatedByUserId=actor;}
        var drafts=await db.MonthlySettlements.Where(x=>x.StatusCode=="DRAFT"&&db.SubscriptionSettlementItems.Any(i=>i.MonthlySettlementId==x.Id&&i.SubscriptionContractId==contractId)).ToListAsync(token);foreach(var draft in drafts){var rows=await db.SubscriptionSettlementItems.Where(x=>x.MonthlySettlementId==draft.Id&&x.StatusCode!="CANCELLED").ToListAsync(token);draft.GrossTotal=rows.Sum(x=>x.GrossAmount??0);draft.FeeTotal=rows.Sum(x=>x.CalculatedFeeAmount??0);draft.AdjustmentTotal=rows.Sum(x=>x.AdjustmentAmount);draft.NetTotal=rows.Sum(x=>x.NetAmount??0);draft.ItemCount=rows.Count;draft.UpdatedAt=now;draft.UpdatedByUserId=actor;}
    }

    private async Task RetryGatewayCancellation(SubscriptionContract contract,DateTime now,CancellationToken token)
    {
        if(contract.GatewayTerminationStatusCode is "FAILED" or "CONFIG_REQUIRED" or "PROCESSING"){contract.GatewayTerminationStatusCode="NOT_REQUIRED";contract.GatewayTerminationFailureReason=null;contract.UpdatedAt=now;}
    }

    private async Task<bool> HasOpenCases(long contractId,CancellationToken token)
    {
        if(await db.SubscriptionVisitSchedules.AnyAsync(x=>x.SubscriptionContractId==contractId&&OpenVisitStatuses.Contains(x.StatusCode),token))return true;
        if(await db.AfterServiceCases.AnyAsync(x=>x.SubscriptionVisitScheduleId.HasValue&&!ClosedAfterServiceStatuses.Contains(x.StatusCode)&&db.SubscriptionVisitSchedules.Any(v=>v.Id==x.SubscriptionVisitScheduleId&&v.SubscriptionContractId==contractId),token))return true;
        return await db.DisputeCases.AnyAsync(x=>x.SubscriptionVisitScheduleId.HasValue&&!ClosedDisputeStatuses.Contains(x.StatusCode)&&db.SubscriptionVisitSchedules.Any(v=>v.Id==x.SubscriptionVisitScheduleId&&v.SubscriptionContractId==contractId),token);
    }
}
