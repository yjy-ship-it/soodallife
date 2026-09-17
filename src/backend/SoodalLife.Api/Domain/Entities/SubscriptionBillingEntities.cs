namespace SoodalLife.Api.Domain.Entities;

public sealed class SubscriptionPaymentMethod
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public string? ProviderCode { get; set; }
    public string PaymentMethodTypeCode { get; set; } = string.Empty;
    public string? ExternalTokenReference { get; set; }
    public string? MaskedDisplayText { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public bool IsDefault { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionPaymentRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionContractId { get; set; }
    public long CustomerProfileId { get; set; }
    public long? PaymentMethodId { get; set; }
    public DateOnly BillingPeriodStart { get; set; }
    public DateOnly BillingPeriodEnd { get; set; }
    public decimal RequestedAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalPaymentReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int GatewayAttemptNo { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionPaymentLedgerEntry
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionContractId { get; set; }
    public long? PaymentRequestId { get; set; }
    public string EntryTypeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public decimal? BalanceAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferencePublicId { get; set; }
    public string? ReasonText { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class SubscriptionSettlementItem
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionVisitScheduleId { get; set; }
    public long SubscriptionContractId { get; set; }
    public long ProviderProfileId { get; set; }
    public DateOnly SettlementMonth { get; set; }
    public decimal? GrossAmount { get; set; }
    public string? FeePolicyCode { get; set; }
    public string? FeePolicySnapshotJson { get; set; }
    public decimal? CalculatedFeeAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public string StatusCode { get; set; } = "CALCULATION_PENDING";
    public string? HoldReason { get; set; }
    public long? MonthlySettlementId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class MonthlySettlement
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public int SettlementYear { get; set; }
    public int SettlementMonth { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public decimal GrossTotal { get; set; }
    public decimal FeeTotal { get; set; }
    public decimal AdjustmentTotal { get; set; }
    public decimal NetTotal { get; set; }
    public int ItemCount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PreparedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionPayout
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MonthlySettlementId { get; set; }
    public long ProviderProfileId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ExternalPayoutReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionPayoutEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionPayoutId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? ActorUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class SubscriptionRefundAdjustment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SubscriptionContractId { get; set; }
    public long? PaymentRequestId { get; set; }
    public long? VisitScheduleId { get; set; }
    public string TypeCode { get; set; } = "REFUND";
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CalculationJson { get; set; }
    public decimal ProviderAdjustmentAmount { get; set; }
    public string? ExternalRefundReference { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ProcessedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
