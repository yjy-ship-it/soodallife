using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionPaymentMethodConfiguration() : EntityConfiguration<SubscriptionPaymentMethod>("subscription_payment_methods")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionPaymentMethod> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionPaymentMethod.CustomerProfileId),"customer_profile_id");
        Mapping.String(b,nameof(SubscriptionPaymentMethod.ProviderCode),"provider_code",50,nullable:true,unicode:false);
        Mapping.String(b,nameof(SubscriptionPaymentMethod.PaymentMethodTypeCode),"payment_method_type_code",30,unicode:false);
        Mapping.String(b,nameof(SubscriptionPaymentMethod.ExternalTokenReference),"external_token_reference",1000,nullable:true,unicode:false);
        Mapping.String(b,nameof(SubscriptionPaymentMethod.MaskedDisplayText),"masked_display_text",100,nullable:true);
        Mapping.String(b,nameof(SubscriptionPaymentMethod.StatusCode),"status_code",20,unicode:false); Mapping.Bool(b,nameof(SubscriptionPaymentMethod.IsDefault),"is_default",false);
        Mapping.DateTime(b,nameof(SubscriptionPaymentMethod.RegisteredAt),"registered_at"); Mapping.DateTime(b,nameof(SubscriptionPaymentMethod.DisabledAt),"disabled_at",nullable:true); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionPaymentMethod,CustomerProfile>(b,nameof(SubscriptionPaymentMethod.CustomerProfileId));
        b.HasIndex(x=>new{x.CustomerProfileId,x.IsDefault}); b.HasIndex(x=>new{x.CustomerProfileId,x.StatusCode});
        b.ToTable("subscription_payment_methods",t=>t.HasCheckConstraint("CK_subscription_payment_methods_status","[status_code] IN ('ACTIVE','DISABLED')"));
    }
}

internal sealed class SubscriptionPaymentRequestConfiguration() : EntityConfiguration<SubscriptionPaymentRequest>("subscription_payment_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionPaymentRequest> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionPaymentRequest.SubscriptionContractId),"subscription_contract_id"); Mapping.Long(b,nameof(SubscriptionPaymentRequest.CustomerProfileId),"customer_profile_id"); Mapping.NullableLong(b,nameof(SubscriptionPaymentRequest.PaymentMethodId),"payment_method_id");
        Mapping.Date(b,nameof(SubscriptionPaymentRequest.BillingPeriodStart),"billing_period_start"); Mapping.Date(b,nameof(SubscriptionPaymentRequest.BillingPeriodEnd),"billing_period_end"); Mapping.Decimal(b,nameof(SubscriptionPaymentRequest.RequestedAmount),"requested_amount"); Mapping.String(b,nameof(SubscriptionPaymentRequest.CurrencyCode),"currency_code",3,unicode:false,fixedLength:true);
        Mapping.String(b,nameof(SubscriptionPaymentRequest.StatusCode),"status_code",30,unicode:false); Mapping.DateTime(b,nameof(SubscriptionPaymentRequest.RequestedAt),"requested_at"); Mapping.DateTime(b,nameof(SubscriptionPaymentRequest.AuthorizedAt),"authorized_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionPaymentRequest.CompletedAt),"completed_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionPaymentRequest.FailedAt),"failed_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionPaymentRequest.CancelledAt),"cancelled_at",nullable:true);
        Mapping.String(b,nameof(SubscriptionPaymentRequest.FailureCode),"failure_code",100,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionPaymentRequest.FailureReason),"failure_reason",1000,nullable:true); Mapping.String(b,nameof(SubscriptionPaymentRequest.ExternalPaymentReference),"external_payment_reference",200,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionPaymentRequest.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.Int(b,nameof(SubscriptionPaymentRequest.GatewayAttemptNo),"gateway_attempt_no",1); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionPaymentRequest,SubscriptionContract>(b,nameof(SubscriptionPaymentRequest.SubscriptionContractId)); Mapping.Fk<SubscriptionPaymentRequest,CustomerProfile>(b,nameof(SubscriptionPaymentRequest.CustomerProfileId)); Mapping.Fk<SubscriptionPaymentRequest,SubscriptionPaymentMethod>(b,nameof(SubscriptionPaymentRequest.PaymentMethodId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SubscriptionContractId,x.BillingPeriodStart,x.BillingPeriodEnd}).IsUnique(); b.HasIndex(x=>new{x.StatusCode,x.RequestedAt});
        b.ToTable("subscription_payment_requests",t=>{t.HasCheckConstraint("CK_subscription_payment_requests_amount","[requested_amount] > 0");t.HasCheckConstraint("CK_subscription_payment_requests_period","[billing_period_end] >= [billing_period_start]");t.HasCheckConstraint("CK_subscription_payment_requests_status","[status_code] IN ('REQUESTED','PROCESSING','COMPLETED','FAILED','CANCELLED','PARTIALLY_REFUNDED','REFUNDED')");});
    }
}

internal sealed class SubscriptionPaymentLedgerConfiguration() : EntityConfiguration<SubscriptionPaymentLedgerEntry>("subscription_payment_ledger")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionPaymentLedgerEntry> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionPaymentLedgerEntry.SubscriptionContractId),"subscription_contract_id"); Mapping.NullableLong(b,nameof(SubscriptionPaymentLedgerEntry.PaymentRequestId),"payment_request_id"); Mapping.String(b,nameof(SubscriptionPaymentLedgerEntry.EntryTypeCode),"entry_type_code",30,unicode:false); Mapping.Decimal(b,nameof(SubscriptionPaymentLedgerEntry.Amount),"amount"); Mapping.String(b,nameof(SubscriptionPaymentLedgerEntry.CurrencyCode),"currency_code",3,unicode:false,fixedLength:true); Mapping.Decimal(b,nameof(SubscriptionPaymentLedgerEntry.BalanceAfter),"balance_after",nullable:true);
        Mapping.String(b,nameof(SubscriptionPaymentLedgerEntry.ReferenceType),"reference_type",50,unicode:false); Mapping.Guid(b,nameof(SubscriptionPaymentLedgerEntry.ReferencePublicId),"reference_public_id",nullable:true); Mapping.String(b,nameof(SubscriptionPaymentLedgerEntry.ReasonText),"reason_text",1000,nullable:true); Mapping.DateTime(b,nameof(SubscriptionPaymentLedgerEntry.OccurredAt),"occurred_at"); Mapping.NullableLong(b,nameof(SubscriptionPaymentLedgerEntry.ProcessedByUserId),"processed_by_user_id"); Mapping.String(b,nameof(SubscriptionPaymentLedgerEntry.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.DateTime(b,nameof(SubscriptionPaymentLedgerEntry.CreatedAt),"created_at",utcDefault:true);
        Mapping.Fk<SubscriptionPaymentLedgerEntry,SubscriptionContract>(b,nameof(SubscriptionPaymentLedgerEntry.SubscriptionContractId)); Mapping.Fk<SubscriptionPaymentLedgerEntry,SubscriptionPaymentRequest>(b,nameof(SubscriptionPaymentLedgerEntry.PaymentRequestId)); Mapping.Fk<SubscriptionPaymentLedgerEntry,User>(b,nameof(SubscriptionPaymentLedgerEntry.ProcessedByUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SubscriptionContractId,x.OccurredAt});
        b.ToTable("subscription_payment_ledger",t=>{t.HasCheckConstraint("CK_subscription_payment_ledger_type","[entry_type_code] IN ('PAYMENT','REFUND','ADJUSTMENT','REVERSAL')");t.HasCheckConstraint("CK_subscription_payment_ledger_amount","[amount] <> 0");});
    }
}

internal sealed class SubscriptionSettlementItemConfiguration() : EntityConfiguration<SubscriptionSettlementItem>("subscription_settlement_items")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionSettlementItem> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionSettlementItem.SubscriptionVisitScheduleId),"subscription_visit_schedule_id"); Mapping.Long(b,nameof(SubscriptionSettlementItem.SubscriptionContractId),"subscription_contract_id"); Mapping.Long(b,nameof(SubscriptionSettlementItem.ProviderProfileId),"provider_profile_id"); Mapping.Date(b,nameof(SubscriptionSettlementItem.SettlementMonth),"settlement_month"); Mapping.Decimal(b,nameof(SubscriptionSettlementItem.GrossAmount),"gross_amount",nullable:true); Mapping.String(b,nameof(SubscriptionSettlementItem.FeePolicyCode),"fee_policy_code",30,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionSettlementItem.FeePolicySnapshotJson),"fee_policy_snapshot_json",null,nullable:true); Mapping.Decimal(b,nameof(SubscriptionSettlementItem.CalculatedFeeAmount),"calculated_fee_amount",nullable:true); Mapping.Decimal(b,nameof(SubscriptionSettlementItem.AdjustmentAmount),"adjustment_amount",defaultValue:0m); Mapping.Decimal(b,nameof(SubscriptionSettlementItem.NetAmount),"net_amount",nullable:true); Mapping.String(b,nameof(SubscriptionSettlementItem.StatusCode),"status_code",30,unicode:false); Mapping.String(b,nameof(SubscriptionSettlementItem.HoldReason),"hold_reason",1000,nullable:true); Mapping.NullableLong(b,nameof(SubscriptionSettlementItem.MonthlySettlementId),"monthly_settlement_id"); Mapping.FullAudit(b);
        Mapping.String(b,nameof(SubscriptionSettlementItem.IdempotencyKey),"idempotency_key",180,unicode:false); Mapping.Fk<SubscriptionSettlementItem,SubscriptionVisitSchedule>(b,nameof(SubscriptionSettlementItem.SubscriptionVisitScheduleId)); Mapping.Fk<SubscriptionSettlementItem,SubscriptionContract>(b,nameof(SubscriptionSettlementItem.SubscriptionContractId)); Mapping.Fk<SubscriptionSettlementItem,ProviderProfile>(b,nameof(SubscriptionSettlementItem.ProviderProfileId)); Mapping.Fk<SubscriptionSettlementItem,MonthlySettlement>(b,nameof(SubscriptionSettlementItem.MonthlySettlementId));
        b.HasIndex(x=>x.SubscriptionVisitScheduleId).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.ProviderProfileId,x.SettlementMonth,x.StatusCode});
        b.ToTable("subscription_settlement_items",t=>{t.HasCheckConstraint("CK_subscription_settlement_items_status","[status_code] IN ('CALCULATION_PENDING','POLICY_PENDING','READY','HOLD','SETTLED','CANCELLED')");t.HasCheckConstraint("CK_subscription_settlement_items_amounts","[gross_amount] IS NULL OR [gross_amount] >= 0");});
    }
}

internal sealed class MonthlySettlementConfiguration() : EntityConfiguration<MonthlySettlement>("monthly_settlements")
{
    protected override void ConfigureEntity(EntityTypeBuilder<MonthlySettlement> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(MonthlySettlement.ProviderProfileId),"provider_profile_id"); Mapping.Int(b,nameof(MonthlySettlement.SettlementYear),"settlement_year"); Mapping.Int(b,nameof(MonthlySettlement.SettlementMonth),"settlement_month"); Mapping.String(b,nameof(MonthlySettlement.StatusCode),"status_code",30,unicode:false); Mapping.Decimal(b,nameof(MonthlySettlement.GrossTotal),"gross_total",defaultValue:0m); Mapping.Decimal(b,nameof(MonthlySettlement.FeeTotal),"fee_total",defaultValue:0m); Mapping.Decimal(b,nameof(MonthlySettlement.AdjustmentTotal),"adjustment_total",defaultValue:0m); Mapping.Decimal(b,nameof(MonthlySettlement.NetTotal),"net_total",defaultValue:0m); Mapping.Int(b,nameof(MonthlySettlement.ItemCount),"item_count",0); Mapping.String(b,nameof(MonthlySettlement.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.DateTime(b,nameof(MonthlySettlement.CreatedAt),"created_at",utcDefault:true); Mapping.DateTime(b,nameof(MonthlySettlement.PreparedAt),"prepared_at",nullable:true); Mapping.DateTime(b,nameof(MonthlySettlement.ApprovedAt),"approved_at",nullable:true); Mapping.NullableLong(b,nameof(MonthlySettlement.ApprovedByUserId),"approved_by_user_id"); Mapping.DateTime(b,nameof(MonthlySettlement.PaidAt),"paid_at",nullable:true); Mapping.DateTime(b,nameof(MonthlySettlement.UpdatedAt),"updated_at",utcDefault:true); Mapping.NullableLong(b,nameof(MonthlySettlement.UpdatedByUserId),"updated_by_user_id"); Mapping.RowVersion(b);
        Mapping.Fk<MonthlySettlement,ProviderProfile>(b,nameof(MonthlySettlement.ProviderProfileId)); Mapping.Fk<MonthlySettlement,User>(b,nameof(MonthlySettlement.ApprovedByUserId)); Mapping.Fk<MonthlySettlement,User>(b,nameof(MonthlySettlement.UpdatedByUserId));
        b.HasIndex(x=>new{x.ProviderProfileId,x.SettlementYear,x.SettlementMonth}).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.StatusCode,x.SettlementYear,x.SettlementMonth});
        b.ToTable("monthly_settlements",t=>{t.HasCheckConstraint("CK_monthly_settlements_month","[settlement_month] BETWEEN 1 AND 12");t.HasCheckConstraint("CK_monthly_settlements_status","[status_code] IN ('DRAFT','REVIEW','APPROVED','PAYMENT_PENDING','PAID','CANCELLED')");});
    }
}

internal sealed class SubscriptionPayoutConfiguration() : EntityConfiguration<SubscriptionPayout>("subscription_payouts")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionPayout> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionPayout.MonthlySettlementId),"monthly_settlement_id"); Mapping.Long(b,nameof(SubscriptionPayout.ProviderProfileId),"provider_profile_id"); Mapping.Decimal(b,nameof(SubscriptionPayout.RequestedAmount),"requested_amount"); Mapping.Decimal(b,nameof(SubscriptionPayout.ApprovedAmount),"approved_amount",nullable:true); Mapping.String(b,nameof(SubscriptionPayout.StatusCode),"status_code",30,unicode:false); Mapping.DateTime(b,nameof(SubscriptionPayout.RequestedAt),"requested_at"); Mapping.DateTime(b,nameof(SubscriptionPayout.ApprovedAt),"approved_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionPayout.CompletedAt),"completed_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionPayout.FailedAt),"failed_at",nullable:true); Mapping.String(b,nameof(SubscriptionPayout.ExternalPayoutReference),"external_payout_reference",200,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionPayout.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionPayout,MonthlySettlement>(b,nameof(SubscriptionPayout.MonthlySettlementId)); Mapping.Fk<SubscriptionPayout,ProviderProfile>(b,nameof(SubscriptionPayout.ProviderProfileId)); b.HasIndex(x=>x.MonthlySettlementId).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.ToTable("subscription_payouts",t=>{t.HasCheckConstraint("CK_subscription_payouts_amount","[requested_amount] >= 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)");t.HasCheckConstraint("CK_subscription_payouts_status","[status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','FAILED','CANCELLED')");});
    }
}

internal sealed class SubscriptionPayoutEventConfiguration() : EntityConfiguration<SubscriptionPayoutEvent>("subscription_payout_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionPayoutEvent> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionPayoutEvent.SubscriptionPayoutId),"subscription_payout_id"); Mapping.String(b,nameof(SubscriptionPayoutEvent.EventTypeCode),"event_type_code",40,unicode:false); Mapping.String(b,nameof(SubscriptionPayoutEvent.EventDataJson),"event_data_json",null,nullable:true); Mapping.DateTime(b,nameof(SubscriptionPayoutEvent.OccurredAt),"occurred_at"); Mapping.NullableLong(b,nameof(SubscriptionPayoutEvent.ActorUserId),"actor_user_id"); Mapping.String(b,nameof(SubscriptionPayoutEvent.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.Fk<SubscriptionPayoutEvent,SubscriptionPayout>(b,nameof(SubscriptionPayoutEvent.SubscriptionPayoutId)); Mapping.Fk<SubscriptionPayoutEvent,User>(b,nameof(SubscriptionPayoutEvent.ActorUserId)); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SubscriptionPayoutId,x.OccurredAt});
    }
}

internal sealed class SubscriptionRefundAdjustmentConfiguration() : EntityConfiguration<SubscriptionRefundAdjustment>("subscription_refund_adjustments")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionRefundAdjustment> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionRefundAdjustment.SubscriptionContractId),"subscription_contract_id"); Mapping.NullableLong(b,nameof(SubscriptionRefundAdjustment.PaymentRequestId),"payment_request_id"); Mapping.NullableLong(b,nameof(SubscriptionRefundAdjustment.VisitScheduleId),"visit_schedule_id"); Mapping.String(b,nameof(SubscriptionRefundAdjustment.TypeCode),"type_code",20,unicode:false); Mapping.Decimal(b,nameof(SubscriptionRefundAdjustment.RequestedAmount),"requested_amount"); Mapping.Decimal(b,nameof(SubscriptionRefundAdjustment.ApprovedAmount),"approved_amount",nullable:true); Mapping.String(b,nameof(SubscriptionRefundAdjustment.Reason),"reason",1000); Mapping.String(b,nameof(SubscriptionRefundAdjustment.CalculationJson),"calculation_json",null,nullable:true); Mapping.Decimal(b,nameof(SubscriptionRefundAdjustment.ProviderAdjustmentAmount),"provider_adjustment_amount",defaultValue:0m); Mapping.String(b,nameof(SubscriptionRefundAdjustment.ExternalRefundReference),"external_refund_reference",200,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionRefundAdjustment.FailureCode),"failure_code",100,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionRefundAdjustment.FailureReason),"failure_reason",1000,nullable:true); Mapping.String(b,nameof(SubscriptionRefundAdjustment.StatusCode),"status_code",30,unicode:false); Mapping.DateTime(b,nameof(SubscriptionRefundAdjustment.RequestedAt),"requested_at"); Mapping.DateTime(b,nameof(SubscriptionRefundAdjustment.ApprovedAt),"approved_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionRefundAdjustment.CompletedAt),"completed_at",nullable:true); Mapping.NullableLong(b,nameof(SubscriptionRefundAdjustment.ProcessedByUserId),"processed_by_user_id"); Mapping.String(b,nameof(SubscriptionRefundAdjustment.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionRefundAdjustment,SubscriptionContract>(b,nameof(SubscriptionRefundAdjustment.SubscriptionContractId)); Mapping.Fk<SubscriptionRefundAdjustment,SubscriptionPaymentRequest>(b,nameof(SubscriptionRefundAdjustment.PaymentRequestId)); Mapping.Fk<SubscriptionRefundAdjustment,SubscriptionVisitSchedule>(b,nameof(SubscriptionRefundAdjustment.VisitScheduleId)); Mapping.Fk<SubscriptionRefundAdjustment,User>(b,nameof(SubscriptionRefundAdjustment.ProcessedByUserId)); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SubscriptionContractId,x.StatusCode});
        b.ToTable("subscription_refund_adjustments",t=>{t.HasCheckConstraint("CK_subscription_refund_adjustments_type","[type_code] IN ('REFUND','ADJUSTMENT')");t.HasCheckConstraint("CK_subscription_refund_adjustments_amount","[requested_amount] > 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)");t.HasCheckConstraint("CK_subscription_refund_adjustments_status","[status_code] IN ('REQUESTED','WAITING_CASES','MANUAL_REQUIRED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')");});
    }
}
