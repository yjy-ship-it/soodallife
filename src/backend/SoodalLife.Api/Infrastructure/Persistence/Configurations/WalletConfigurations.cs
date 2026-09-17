using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProviderWalletConfiguration() : EntityConfiguration<ProviderWallet>("wallets")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderWallet> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderWallet.ProviderProfileId), "provider_profile_id");
        Mapping.String(b, nameof(ProviderWallet.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.Decimal(b, nameof(ProviderWallet.AvailableBalance), "available_balance", defaultValue: 0);
        Mapping.Decimal(b, nameof(ProviderWallet.ReservedBalance), "reserved_balance", defaultValue: 0);
        Mapping.String(b, nameof(ProviderWallet.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderWallet, ProviderProfile>(b, nameof(ProviderWallet.ProviderProfileId));
        b.HasIndex(x => new { x.ProviderProfileId, x.CurrencyCode }).IsUnique();
        b.HasIndex(x => x.StatusCode);
        b.ToTable("wallets", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_wallets_balances", "[available_balance] >= 0 AND [reserved_balance] >= 0");
            t.HasCheckConstraint("CK_wallets_status", "[status_code] IN ('ACTIVE','FROZEN','CLOSED')");
        });
    }
}

internal sealed class WalletLedgerEntryConfiguration() : EntityConfiguration<WalletLedgerEntry>("wallet_ledger")
{
    protected override void ConfigureEntity(EntityTypeBuilder<WalletLedgerEntry> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(WalletLedgerEntry.WalletId), "wallet_id");
        Mapping.NullableLong(b, nameof(WalletLedgerEntry.TransactionId), "transaction_id");
        Mapping.String(b, nameof(WalletLedgerEntry.EntryTypeCode), "entry_type_code", 20, unicode: false);
        Mapping.Decimal(b, nameof(WalletLedgerEntry.Amount), "amount");
        Mapping.Decimal(b, nameof(WalletLedgerEntry.SupplyAmount), "supply_amount", defaultValue: 0);
        Mapping.Decimal(b, nameof(WalletLedgerEntry.VatAmount), "vat_amount", defaultValue: 0);
        Mapping.String(b, nameof(WalletLedgerEntry.TaxTreatmentCode), "tax_treatment_code", 30, unicode: false, defaultValue: "DEPOSIT");
        Mapping.Decimal(b, nameof(WalletLedgerEntry.BalanceAfter), "balance_after");
        Mapping.String(b, nameof(WalletLedgerEntry.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.String(b, nameof(WalletLedgerEntry.Reason), "reason", 1000);
        Mapping.String(b, nameof(WalletLedgerEntry.ReferenceType), "reference_type", 50, nullable: true, unicode: false);
        Mapping.Guid(b, nameof(WalletLedgerEntry.ReferencePublicId), "reference_public_id", nullable: true);
        Mapping.String(b, nameof(WalletLedgerEntry.PaymentMethodCode), "payment_method_code", 40, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(WalletLedgerEntry.OccurredAt), "occurred_at", utcDefault: true);
        Mapping.CreatedAudit(b);
        Mapping.Fk<WalletLedgerEntry, ProviderWallet>(b, nameof(WalletLedgerEntry.WalletId));
        Mapping.Fk<WalletLedgerEntry, TransactionRecord>(b, nameof(WalletLedgerEntry.TransactionId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.WalletId, x.OccurredAt }).IsDescending(false, true);
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => new { x.ReferenceType, x.ReferencePublicId });
        b.ToTable("wallet_ledger", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_wallet_ledger_entry_type", "[entry_type_code] IN ('CHARGE','RESERVE','RELEASE','USE','RESTORE','REFUND','ADJUST')");
            t.HasCheckConstraint("CK_wallet_ledger_amount", "[amount] <> 0");
            t.HasCheckConstraint("CK_wallet_ledger_balance", "[balance_after] >= 0");
            t.HasCheckConstraint("CK_wallet_ledger_tax_treatment", "[tax_treatment_code] IN ('DEPOSIT','EXPECTED_VAT_INCLUDED','EXPECTED_REVERSED','TAXABLE_VAT_INCLUDED','TAX_REVERSED','NON_TAXABLE','LEGACY_UNSPLIT')");
        });
    }
}

internal sealed class QuoteFeeReservationConfiguration() : EntityConfiguration<QuoteFeeReservation>("quote_fee_reservations")
{
    protected override void ConfigureEntity(EntityTypeBuilder<QuoteFeeReservation> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(QuoteFeeReservation.QuoteId), "quote_id");
        Mapping.Long(b, nameof(QuoteFeeReservation.WalletId), "wallet_id");
        Mapping.Long(b, nameof(QuoteFeeReservation.CategoryFeePolicyId), "category_fee_policy_id");
        Mapping.Decimal(b, nameof(QuoteFeeReservation.Amount), "amount");
        Mapping.Decimal(b, nameof(QuoteFeeReservation.ExpectedSupplyAmount), "expected_supply_amount", defaultValue: 0);
        Mapping.Decimal(b, nameof(QuoteFeeReservation.ExpectedVatAmount), "expected_vat_amount", defaultValue: 0);
        Mapping.String(b, nameof(QuoteFeeReservation.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(QuoteFeeReservation.StatusCode), "status_code", 20, unicode: false, defaultValue: "RESERVED");
        Mapping.Long(b, nameof(QuoteFeeReservation.ReserveLedgerEntryId), "reserve_ledger_entry_id");
        Mapping.NullableLong(b, nameof(QuoteFeeReservation.CaptureLedgerEntryId), "capture_ledger_entry_id");
        Mapping.NullableLong(b, nameof(QuoteFeeReservation.ReleaseLedgerEntryId), "release_ledger_entry_id");
        Mapping.DateTime(b, nameof(QuoteFeeReservation.ReservedAt), "reserved_at", utcDefault: true);
        Mapping.DateTime(b, nameof(QuoteFeeReservation.CapturedAt), "captured_at", nullable: true);
        Mapping.DateTime(b, nameof(QuoteFeeReservation.ReleasedAt), "released_at", nullable: true);
        Mapping.String(b, nameof(QuoteFeeReservation.ReleaseReasonCode), "release_reason_code", 40, unicode: false, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<QuoteFeeReservation, Quote>(b, nameof(QuoteFeeReservation.QuoteId));
        Mapping.Fk<QuoteFeeReservation, ProviderWallet>(b, nameof(QuoteFeeReservation.WalletId));
        Mapping.Fk<QuoteFeeReservation, CategoryFeePolicy>(b, nameof(QuoteFeeReservation.CategoryFeePolicyId));
        Mapping.Fk<QuoteFeeReservation, WalletLedgerEntry>(b, nameof(QuoteFeeReservation.ReserveLedgerEntryId));
        Mapping.Fk<QuoteFeeReservation, WalletLedgerEntry>(b, nameof(QuoteFeeReservation.CaptureLedgerEntryId));
        Mapping.Fk<QuoteFeeReservation, WalletLedgerEntry>(b, nameof(QuoteFeeReservation.ReleaseLedgerEntryId));
        b.HasIndex(x => x.QuoteId).IsUnique();
        b.HasIndex(x => x.ReserveLedgerEntryId).IsUnique();
        b.HasIndex(x => x.CaptureLedgerEntryId).IsUnique().HasFilter("[capture_ledger_entry_id] IS NOT NULL");
        b.HasIndex(x => x.ReleaseLedgerEntryId).IsUnique().HasFilter("[release_ledger_entry_id] IS NOT NULL");
        b.HasIndex(x => new { x.StatusCode, x.ReservedAt });
        b.ToTable("quote_fee_reservations", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_quote_fee_reservations_amount", "[amount] > 0");
            t.HasCheckConstraint("CK_quote_fee_reservations_status", "[status_code] IN ('RESERVED','CAPTURED','RELEASED')");
        });
    }
}

internal sealed class WalletChargeRequestConfiguration() : EntityConfiguration<WalletChargeRequest>("wallet_charge_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<WalletChargeRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(WalletChargeRequest.WalletId), "wallet_id");
        Mapping.Decimal(b, nameof(WalletChargeRequest.RequestedAmount), "requested_amount");
        Mapping.String(b, nameof(WalletChargeRequest.PaymentMethodCode), "payment_method_code", 40, unicode: false);
        Mapping.String(b, nameof(WalletChargeRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "REQUESTED");
        Mapping.DateTime(b, nameof(WalletChargeRequest.RequestedAt), "requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(WalletChargeRequest.CompletedAt), "completed_at", nullable: true);
        Mapping.DateTime(b, nameof(WalletChargeRequest.FailedAt), "failed_at", nullable: true);
        Mapping.String(b, nameof(WalletChargeRequest.FailureReason), "failure_reason", 1000, nullable: true);
        Mapping.String(b, nameof(WalletChargeRequest.ExternalPaymentReference), "external_payment_reference", 200, nullable: true, unicode: false);
        Mapping.String(b, nameof(WalletChargeRequest.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.NullableLong(b, nameof(WalletChargeRequest.LedgerEntryId), "ledger_entry_id");
        Mapping.String(b, nameof(WalletChargeRequest.RequestReason), "request_reason", 1000, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<WalletChargeRequest, ProviderWallet>(b, nameof(WalletChargeRequest.WalletId));
        Mapping.Fk<WalletChargeRequest, WalletLedgerEntry>(b, nameof(WalletChargeRequest.LedgerEntryId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.WalletId, x.RequestedAt }).IsDescending(false, true);
        b.HasIndex(x => x.StatusCode);
        b.ToTable("wallet_charge_requests", t =>
        {
            t.HasCheckConstraint("CK_wallet_charge_requests_amount", "[requested_amount] > 0");
            t.HasCheckConstraint("CK_wallet_charge_requests_status", "[status_code] IN ('REQUESTED','PROCESSING','SUCCEEDED','FAILED','CANCELLED')");
        });
    }
}

internal sealed class FeeChargeConfiguration() : EntityConfiguration<FeeCharge>("fee_charges")
{
    protected override void ConfigureEntity(EntityTypeBuilder<FeeCharge> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(FeeCharge.TransactionId), "transaction_id");
        Mapping.Long(b, nameof(FeeCharge.CategoryFeePolicyId), "category_fee_policy_id");
        Mapping.Long(b, nameof(FeeCharge.WalletId), "wallet_id");
        Mapping.Long(b, nameof(FeeCharge.LedgerEntryId), "ledger_entry_id");
        Mapping.Decimal(b, nameof(FeeCharge.FeeAmount), "fee_amount");
        Mapping.Decimal(b, nameof(FeeCharge.SupplyAmount), "supply_amount", defaultValue: 0);
        Mapping.Decimal(b, nameof(FeeCharge.VatAmount), "vat_amount", defaultValue: 0);
        Mapping.Bool(b, nameof(FeeCharge.IsVatIncluded), "is_vat_included", true);
        Mapping.DateTime(b, nameof(FeeCharge.ChargedAt), "charged_at", utcDefault: true);
        Mapping.String(b, nameof(FeeCharge.RestoreStatusCode), "restore_status_code", 20, unicode: false, defaultValue: "NOT_RESTORED");
        Mapping.NullableLong(b, nameof(FeeCharge.RestoreLedgerEntryId), "restore_ledger_entry_id");
        Mapping.FullAudit(b);
        Mapping.Fk<FeeCharge, TransactionRecord>(b, nameof(FeeCharge.TransactionId));
        Mapping.Fk<FeeCharge, CategoryFeePolicy>(b, nameof(FeeCharge.CategoryFeePolicyId));
        Mapping.Fk<FeeCharge, ProviderWallet>(b, nameof(FeeCharge.WalletId));
        Mapping.Fk<FeeCharge, WalletLedgerEntry>(b, nameof(FeeCharge.LedgerEntryId));
        Mapping.Fk<FeeCharge, WalletLedgerEntry>(b, nameof(FeeCharge.RestoreLedgerEntryId));
        b.HasIndex(x => new { x.TransactionId, x.CategoryFeePolicyId }).IsUnique();
        b.HasIndex(x => x.LedgerEntryId).IsUnique();
        b.HasIndex(x => x.RestoreLedgerEntryId).IsUnique().HasFilter("[restore_ledger_entry_id] IS NOT NULL");
        b.HasIndex(x => new { x.WalletId, x.ChargedAt }).IsDescending(false, true);
        b.ToTable("fee_charges", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_fee_charges_amount", "[fee_amount] > 0");
            t.HasCheckConstraint("CK_fee_charges_restore_status", "[restore_status_code] IN ('NOT_RESTORED','RESTORED')");
        });
    }
}

internal sealed class FeeRestoreConfiguration() : EntityConfiguration<FeeRestore>("fee_restores")
{
    protected override void ConfigureEntity(EntityTypeBuilder<FeeRestore> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(FeeRestore.FeeChargeId), "fee_charge_id");
        Mapping.Long(b, nameof(FeeRestore.LedgerEntryId), "ledger_entry_id");
        Mapping.String(b, nameof(FeeRestore.ReasonCode), "reason_code", 40, unicode: false);
        Mapping.String(b, nameof(FeeRestore.Reason), "reason", 1000);
        Mapping.String(b, nameof(FeeRestore.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.DateTime(b, nameof(FeeRestore.RestoredAt), "restored_at", utcDefault: true);
        Mapping.Long(b, nameof(FeeRestore.RestoredByUserId), "restored_by_user_id");
        Mapping.CreatedAudit(b);
        Mapping.Fk<FeeRestore, FeeCharge>(b, nameof(FeeRestore.FeeChargeId));
        Mapping.Fk<FeeRestore, WalletLedgerEntry>(b, nameof(FeeRestore.LedgerEntryId));
        Mapping.Fk<FeeRestore, User>(b, nameof(FeeRestore.RestoredByUserId));
        b.HasIndex(x => x.FeeChargeId).IsUnique();
        b.HasIndex(x => x.LedgerEntryId).IsUnique();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.ToTable("fee_restores", t => t.HasCheckConstraint("CK_fee_restores_reason_code", "[reason_code] IN ('SYSTEM_ERROR','DUPLICATE_ACCEPTANCE','FALSE_REQUEST','HEAD_OFFICE_APPROVAL')"));
    }
}

internal sealed class WalletRefundRequestConfiguration() : EntityConfiguration<WalletRefundRequest>("wallet_refund_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<WalletRefundRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(WalletRefundRequest.WalletId), "wallet_id");
        Mapping.Decimal(b, nameof(WalletRefundRequest.RequestedAmount), "requested_amount");
        Mapping.String(b, nameof(WalletRefundRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "REQUESTED");
        Mapping.String(b, nameof(WalletRefundRequest.RequestReason), "request_reason", 1000);
        Mapping.DateTime(b, nameof(WalletRefundRequest.RequestedAt), "requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(WalletRefundRequest.ReviewedAt), "reviewed_at", nullable: true);
        Mapping.NullableLong(b, nameof(WalletRefundRequest.ReviewedByUserId), "reviewed_by_user_id");
        Mapping.String(b, nameof(WalletRefundRequest.ReviewReason), "review_reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(WalletRefundRequest.CompletedAt), "completed_at", nullable: true);
        Mapping.DateTime(b, nameof(WalletRefundRequest.FailedAt), "failed_at", nullable: true);
        Mapping.String(b, nameof(WalletRefundRequest.FailureReason), "failure_reason", 1000, nullable: true);
        Mapping.String(b, nameof(WalletRefundRequest.ExternalRefundReference), "external_refund_reference", 200, nullable: true, unicode: false);
        Mapping.NullableLong(b, nameof(WalletRefundRequest.LedgerEntryId), "ledger_entry_id");
        Mapping.String(b, nameof(WalletRefundRequest.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.FullAudit(b);
        Mapping.Fk<WalletRefundRequest, ProviderWallet>(b, nameof(WalletRefundRequest.WalletId));
        Mapping.Fk<WalletRefundRequest, User>(b, nameof(WalletRefundRequest.ReviewedByUserId));
        Mapping.Fk<WalletRefundRequest, WalletLedgerEntry>(b, nameof(WalletRefundRequest.LedgerEntryId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.WalletId, x.RequestedAt }).IsDescending(false, true);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.LedgerEntryId).IsUnique().HasFilter("[ledger_entry_id] IS NOT NULL");
        b.ToTable("wallet_refund_requests", t =>
        {
            t.HasCheckConstraint("CK_wallet_refund_requests_amount", "[requested_amount] > 0");
            t.HasCheckConstraint("CK_wallet_refund_requests_status", "[status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')");
        });
    }
}
