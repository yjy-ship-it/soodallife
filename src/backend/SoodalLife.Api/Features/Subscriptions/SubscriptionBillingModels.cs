using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed record RegisterSubscriptionPaymentMethodRequest(Guid CustomerId,[param:Required,StringLength(30)]string PaymentMethodTypeCode,string? ProviderCode,string? ExternalTokenReference,string? MaskedDisplayText,bool IsDefault);
public sealed record CreateSubscriptionPaymentRequest(Guid ContractId,Guid? PaymentMethodId,DateOnly BillingPeriodStart,DateOnly BillingPeriodEnd,decimal RequestedAmount,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record DevelopmentPaymentConfirmationRequest([param:Required,StringLength(150)]string IdempotencyKey,string? ExternalPaymentReference,string RowVersion);
public sealed record PaymentStateRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion);
public sealed record PrepareSettlementItemsRequest(int Year,int Month,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record CreateMonthlySettlementRequest(Guid ProviderId,int Year,int Month,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record SettlementDecisionRequest([param:Required,StringLength(150)]string IdempotencyKey,string RowVersion,string? Reason=null);
public sealed record CreateSubscriptionPayoutRequest(Guid MonthlySettlementId,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record PayoutDecisionRequest(decimal? ApprovedAmount,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion,string? ExternalPayoutReference=null);
public sealed record CreateRefundAdjustmentRequest(Guid ContractId,Guid? PaymentRequestId,Guid? VisitId,string TypeCode,decimal RequestedAmount,[param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record RefundAdjustmentDecisionRequest(decimal? ApprovedAmount,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion,string? Reason=null);
public sealed record CompleteBankTransferPayoutRequest([param:Required,StringLength(100)]string BankTransferReference,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion,[param:StringLength(1000)]string? Reason=null);
public sealed record CompleteBillingAuthorizationRequest([param:Required,StringLength(300)]string AuthKey,[param:Required,StringLength(300)]string CustomerKey,bool IsDefault=true);
public sealed record SubscriptionBillingRegistrationResponse(bool Enabled,string ProviderCode,string ClientKey,string CustomerKey,string SuccessUrl,string FailUrl);
public sealed record SubscriptionPaymentWebhookRequest(string EventType,string PaymentKey);

public sealed record SubscriptionPaymentMethodResponse(Guid Id,Guid CustomerId,string CustomerName,string PaymentMethodTypeCode,string? ProviderCode,string? MaskedDisplayText,string StatusCode,bool IsDefault,DateTime RegisteredAt,string RowVersion);
public sealed record SubscriptionPaymentResponse(Guid Id,Guid ContractId,string ContractNumber,string CustomerName,string ServiceName,DateOnly BillingPeriodStart,DateOnly BillingPeriodEnd,decimal RequestedAmount,string CurrencyCode,string StatusCode,string? PaymentMethodDisplay,DateTime RequestedAt,DateTime? CompletedAt,string? FailureReason,string RowVersion);
public sealed record SubscriptionPaymentLedgerResponse(Guid Id,Guid ContractId,Guid? PaymentRequestId,string EntryTypeCode,decimal Amount,string CurrencyCode,string ReferenceType,Guid? ReferencePublicId,string? ReasonText,DateTime OccurredAt);
public sealed record SubscriptionSettlementItemResponse(Guid Id,Guid VisitId,Guid ContractId,string ContractNumber,Guid ProviderId,string ProviderName,string ServiceName,int VisitNo,DateTime ScheduledStartAt,decimal? GrossAmount,string? FeePolicyCode,decimal? CalculatedFeeAmount,decimal AdjustmentAmount,decimal? NetAmount,string StatusCode,string? HoldReason,Guid? MonthlySettlementId,string RowVersion);
public sealed record MonthlySettlementResponse(Guid Id,Guid ProviderId,string ProviderName,int Year,int Month,string StatusCode,decimal GrossTotal,decimal FeeTotal,decimal AdjustmentTotal,decimal NetTotal,int ItemCount,DateTime? ApprovedAt,DateTime? PaidAt,string RowVersion);
public sealed record SubscriptionPayoutResponse(Guid Id,Guid MonthlySettlementId,Guid ProviderId,string ProviderName,decimal RequestedAmount,decimal? ApprovedAmount,string StatusCode,DateTime RequestedAt,DateTime? CompletedAt,string? ExternalPayoutReference,string RowVersion);
public sealed record SubscriptionRefundAdjustmentResponse(Guid Id,Guid ContractId,Guid? PaymentRequestId,Guid? VisitId,string CustomerName,string TypeCode,decimal RequestedAmount,decimal? ApprovedAmount,string Reason,string StatusCode,DateTime RequestedAt,DateTime? CompletedAt,string RowVersion);
public sealed record SubscriptionAccountingDashboard(int PaymentRequestCount,int FailedPaymentCount,int ReadySettlementItemCount,int PendingPolicyItemCount,int MonthlySettlementCount,int PendingPayoutCount,int OpenRefundCount,bool DevelopmentSimulationAvailable);

public sealed record SubscriptionFeeCalculationResult(bool Calculable,string? PolicyCode,decimal? FeeAmount,string? Reason)
{
    public static SubscriptionFeeCalculationResult Pending(string reason)=>new(false,null,null,reason);
    public static SubscriptionFeeCalculationResult Success(string code,decimal amount)=>new(true,code,decimal.Round(amount,0,MidpointRounding.AwayFromZero),null);
}

public interface ISubscriptionSettlementFeeCalculator
{
    SubscriptionFeeCalculationResult Calculate(string? policySnapshotJson,decimal? grossAmount);
}

public sealed record SubscriptionGatewayPaymentResult(string PaymentKey,string? TransactionKey);
public sealed record SubscriptionGatewayBillingKeyResult(string BillingKey,string MethodType,string MaskedDisplayText);
public sealed record SubscriptionGatewayPaymentStatus(string PaymentKey,string OrderId,string Status,decimal TotalAmount,decimal BalanceAmount);

public interface ISubscriptionPaymentGateway
{
    Task<SubscriptionGatewayBillingKeyResult> IssueBillingKeyAsync(string providerCode,string authKey,string customerKey,string idempotencyKey,CancellationToken cancellationToken);
    Task<SubscriptionGatewayPaymentResult> ChargeRecurringAsync(string providerCode,string billingKey,string customerKey,string orderId,string orderName,decimal amount,string currencyCode,string idempotencyKey,CancellationToken cancellationToken);
    Task<SubscriptionGatewayPaymentStatus> GetPaymentAsync(string providerCode,string paymentKey,CancellationToken cancellationToken);
    Task<SubscriptionGatewayPaymentStatus> GetPaymentByOrderIdAsync(string providerCode,string orderId,CancellationToken cancellationToken);
    Task CancelBillingKeyAsync(string providerCode,string billingKey,string idempotencyKey,CancellationToken cancellationToken);
    Task<SubscriptionGatewayPaymentResult> RefundPaymentAsync(string providerCode,string paymentKey,decimal amount,string reason,string idempotencyKey,CancellationToken cancellationToken);
}

public interface ISubscriptionPayoutGateway
{
    Task<string> CreatePayout(CancellationToken cancellationToken);
    Task ConfirmPayout(string externalReference,CancellationToken cancellationToken);
}
