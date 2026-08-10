namespace SoodalLife.Api.Domain.Entities;

public sealed class ProviderWallet
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WalletLedgerEntry
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WalletId { get; set; }
    public long? TransactionId { get; set; }
    public string EntryTypeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferencePublicId { get; set; }
    public string? PaymentMethodCode { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class WalletChargeRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WalletId { get; set; }
    public decimal RequestedAmount { get; set; }
    public string PaymentMethodCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalPaymentReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long? LedgerEntryId { get; set; }
    public string? RequestReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FeeCharge
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long CategoryFeePolicyId { get; set; }
    public long WalletId { get; set; }
    public long LedgerEntryId { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime ChargedAt { get; set; }
    public string RestoreStatusCode { get; set; } = "NOT_RESTORED";
    public long? RestoreLedgerEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FeeRestore
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long FeeChargeId { get; set; }
    public long LedgerEntryId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime RestoredAt { get; set; }
    public long RestoredByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class WalletRefundRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long WalletId { get; set; }
    public decimal RequestedAmount { get; set; }
    public string StatusCode { get; set; } = "REQUESTED";
    public string RequestReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalRefundReference { get; set; }
    public long? LedgerEntryId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
