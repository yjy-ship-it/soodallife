using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class TransactionRecordConfiguration() : EntityConfiguration<TransactionRecord>("transactions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionRecord> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(TransactionRecord.ServiceRequestId), "service_request_id");
        Mapping.Long(b, nameof(TransactionRecord.AcceptedQuoteRevisionId), "accepted_quote_revision_id");
        Mapping.Long(b, nameof(TransactionRecord.CustomerProfileId), "customer_profile_id");
        Mapping.Long(b, nameof(TransactionRecord.ProviderProfileId), "provider_profile_id");
        Mapping.Long(b, nameof(TransactionRecord.CategoryId), "category_id");
        Mapping.NullableLong(b, nameof(TransactionRecord.CategoryFeePolicyId), "category_fee_policy_id");
        Mapping.NullableLong(b, nameof(TransactionRecord.WalletLedgerEntryId), "wallet_ledger_entry_id");
        Mapping.String(b, nameof(TransactionRecord.StatusCode), "status_code", 30, unicode: false, defaultValue: "CREATED");
        Mapping.Decimal(b, nameof(TransactionRecord.AgreedAmount), "agreed_amount");
        Mapping.String(b, nameof(TransactionRecord.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(TransactionRecord.QuoteSnapshotJson), "quote_snapshot_json", null);
        Mapping.String(b, nameof(TransactionRecord.CategoryPolicySnapshotJson), "category_policy_snapshot_json", null);
        Mapping.String(b, nameof(TransactionRecord.CompletionPolicySnapshotJson), "completion_policy_snapshot_json", null);
        Mapping.String(b, nameof(TransactionRecord.FeePolicySnapshotJson), "fee_policy_snapshot_json", null, nullable: true);
        Mapping.String(b, nameof(TransactionRecord.FeePolicyVersionSnapshot), "fee_policy_version_snapshot", 100, nullable: true, unicode: false);
        Mapping.String(b, nameof(TransactionRecord.FeePolicyKindSnapshot), "fee_policy_kind_snapshot", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(TransactionRecord.FeeTransactionTypeSnapshot), "fee_transaction_type_snapshot", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(TransactionRecord.FeeCalculationMethodSnapshot), "fee_calculation_method_snapshot", 100, nullable: true);
        Mapping.Decimal(b, nameof(TransactionRecord.CalculatedFeeAmount), "calculated_fee_amount", nullable: true);
        Mapping.Decimal(b, nameof(TransactionRecord.ActualChargedFeeAmount), "actual_charged_fee_amount", nullable: true);
        Mapping.String(b, nameof(TransactionRecord.FeeCurrencyCode), "fee_currency_code", 3, nullable: true, unicode: false, fixedLength: true);
        Mapping.String(b, nameof(TransactionRecord.FeeChargeTimingSnapshot), "fee_charge_timing_snapshot", 200, nullable: true);
        Mapping.String(b, nameof(TransactionRecord.FeeRestoreRuleSnapshot), "fee_restore_rule_snapshot", 1000, nullable: true);
        Mapping.Short(b, nameof(TransactionRecord.WarrantyDaysSnapshot), "warranty_days_snapshot", 0);
        Mapping.Decimal(b, nameof(TransactionRecord.ProviderTrustScoreSnapshot), "provider_trust_score_snapshot", nullable: true, precision: 9, scale: 4);
        Mapping.DateTime(b, nameof(TransactionRecord.StartedAt), "started_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionRecord.CompletedAt), "completed_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionRecord.CancelledAt), "cancelled_at", nullable: true);
        Mapping.String(b, nameof(TransactionRecord.CancellationReason), "cancellation_reason", 1000, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<TransactionRecord, ServiceRequest>(b, nameof(TransactionRecord.ServiceRequestId));
        Mapping.Fk<TransactionRecord, QuoteRevision>(b, nameof(TransactionRecord.AcceptedQuoteRevisionId));
        Mapping.Fk<TransactionRecord, CustomerProfile>(b, nameof(TransactionRecord.CustomerProfileId));
        Mapping.Fk<TransactionRecord, ProviderProfile>(b, nameof(TransactionRecord.ProviderProfileId));
        Mapping.Fk<TransactionRecord, ServiceCategory>(b, nameof(TransactionRecord.CategoryId));
        Mapping.Fk<TransactionRecord, CategoryFeePolicy>(b, nameof(TransactionRecord.CategoryFeePolicyId));
        Mapping.Fk<TransactionRecord, WalletLedgerEntry>(b, nameof(TransactionRecord.WalletLedgerEntryId));
        b.HasIndex(x => x.ServiceRequestId).IsUnique();
        b.HasIndex(x => x.AcceptedQuoteRevisionId).IsUnique();
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.CategoryFeePolicyId);
        b.HasIndex(x => x.WalletLedgerEntryId).IsUnique().HasFilter("[wallet_ledger_entry_id] IS NOT NULL");
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.CompletedAt);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => new { x.CustomerProfileId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.ProviderProfileId, x.StatusCode, x.CreatedAt }).IsDescending(false, false, true);
        b.ToTable("transactions", t =>
        {
            t.HasCheckConstraint("CK_transactions_status", "[status_code] IN ('CREATED','IN_PROGRESS','COMPLETION_SUBMITTED','REVISION_REQUESTED','COMPLETED','DISPUTED','CANCELLED')");
            t.HasCheckConstraint("CK_transactions_quote_json", "ISJSON([quote_snapshot_json]) = 1");
            t.HasCheckConstraint("CK_transactions_category_policy_json", "ISJSON([category_policy_snapshot_json]) = 1");
            t.HasCheckConstraint("CK_transactions_completion_policy_json", "ISJSON([completion_policy_snapshot_json]) = 1");
            t.HasCheckConstraint("CK_transactions_fee_policy_json", "[fee_policy_snapshot_json] IS NULL OR ISJSON([fee_policy_snapshot_json]) = 1");
            t.HasCheckConstraint("CK_transactions_fee_amounts", "([calculated_fee_amount] IS NULL OR [calculated_fee_amount] >= 0) AND ([actual_charged_fee_amount] IS NULL OR [actual_charged_fee_amount] >= 0)");
            t.HasCheckConstraint("CK_transactions_warranty_days", "[warranty_days_snapshot] >= 0");
        });
    }
}

internal sealed class WorkCompletionConfiguration() : EntityConfiguration<WorkCompletion>("work_completions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkCompletion> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(WorkCompletion.TransactionId), "transaction_id");
        Mapping.NullableLong(b, nameof(WorkCompletion.InteriorProjectId), "interior_project_id");
        Mapping.String(b, nameof(WorkCompletion.StatusCode), "status_code", 30, unicode: false, defaultValue: "DRAFT");
        Mapping.Int(b, nameof(WorkCompletion.LatestRevisionNo), "latest_revision_no", 0);
        Mapping.DateTime(b, nameof(WorkCompletion.FirstSubmittedAt), "first_submitted_at", nullable: true);
        Mapping.DateTime(b, nameof(WorkCompletion.ConfirmedAt), "confirmed_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<WorkCompletion, TransactionRecord>(b, nameof(WorkCompletion.TransactionId));
        Mapping.Fk<WorkCompletion, InteriorProject>(b, nameof(WorkCompletion.InteriorProjectId));
        b.HasIndex(x => x.TransactionId).IsUnique();
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.ConfirmedAt);
        b.ToTable("work_completions", t => t.HasCheckConstraint("CK_work_completions_status", "[status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')"));
    }
}

internal sealed class WorkCompletionRevisionConfiguration() : EntityConfiguration<WorkCompletionRevision>("work_completion_revisions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkCompletionRevision> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(WorkCompletionRevision.WorkCompletionId), "work_completion_id");
        Mapping.Int(b, nameof(WorkCompletionRevision.RevisionNo), "revision_no");
        Mapping.String(b, nameof(WorkCompletionRevision.StatusCode), "status_code", 30, unicode: false, defaultValue: "SUBMITTED");
        Mapping.String(b, nameof(WorkCompletionRevision.WorkSummary), "work_summary", null);
        Mapping.String(b, nameof(WorkCompletionRevision.ChecklistJson), "checklist_json", null, nullable: true);
        Mapping.DateTime(b, nameof(WorkCompletionRevision.ProviderAttestationAt), "provider_attestation_at");
        Mapping.DateTime(b, nameof(WorkCompletionRevision.SubmittedAt), "submitted_at", utcDefault: true);
        Mapping.Long(b, nameof(WorkCompletionRevision.SubmittedByUserId), "submitted_by_user_id");
        Mapping.String(b, nameof(WorkCompletionRevision.RevisionReason), "revision_reason", 1000, nullable: true);
        Mapping.String(b, nameof(WorkCompletionRevision.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.Fk<WorkCompletionRevision, WorkCompletion>(b, nameof(WorkCompletionRevision.WorkCompletionId));
        Mapping.Fk<WorkCompletionRevision, User>(b, nameof(WorkCompletionRevision.SubmittedByUserId));
        b.HasIndex(x => x.WorkCompletionId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.SubmittedAt);
        b.HasIndex(x => x.SubmittedByUserId);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.WorkCompletionId, x.RevisionNo }).IsUnique().IsDescending(false, true);
        b.HasIndex(x => new { x.WorkCompletionId, x.SubmittedAt }).IsDescending(false, true);
        b.ToTable("work_completion_revisions", t =>
        {
            t.HasCheckConstraint("CK_work_completion_revisions_status", "[status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')");
            t.HasCheckConstraint("CK_work_completion_revisions_checklist_json", "[checklist_json] IS NULL OR ISJSON([checklist_json]) = 1");
        });
    }
}

internal sealed class CompletionEvidenceFileConfiguration() : EntityConfiguration<CompletionEvidenceFile>("completion_evidence_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CompletionEvidenceFile> b)
    {
        Mapping.Long(b, nameof(CompletionEvidenceFile.CompletionRevisionId), "completion_revision_id");
        Mapping.Long(b, nameof(CompletionEvidenceFile.FileId), "file_id");
        Mapping.Long(b, nameof(CompletionEvidenceFile.PhotoRoleId), "photo_role_id");
        Mapping.Int(b, nameof(CompletionEvidenceFile.DisplayOrder), "display_order", 0);
        Mapping.String(b, nameof(CompletionEvidenceFile.Description), "description", 500, nullable: true);
        Mapping.CreatedAudit(b);
        Mapping.Fk<CompletionEvidenceFile, WorkCompletionRevision>(b, nameof(CompletionEvidenceFile.CompletionRevisionId));
        Mapping.Fk<CompletionEvidenceFile, StoredFile>(b, nameof(CompletionEvidenceFile.FileId));
        Mapping.Fk<CompletionEvidenceFile, CompletionPhotoRole>(b, nameof(CompletionEvidenceFile.PhotoRoleId));
        b.HasIndex(x => x.CompletionRevisionId);
        b.HasIndex(x => x.FileId);
        b.HasIndex(x => x.PhotoRoleId);
        b.HasIndex(x => new { x.CompletionRevisionId, x.FileId }).IsUnique();
        b.HasIndex(x => new { x.CompletionRevisionId, x.PhotoRoleId });
    }
}

internal sealed class CustomerConfirmationConfiguration() : EntityConfiguration<CustomerConfirmation>("customer_confirmations")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerConfirmation> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CustomerConfirmation.TransactionId), "transaction_id");
        Mapping.Long(b, nameof(CustomerConfirmation.CompletionRevisionId), "completion_revision_id");
        Mapping.String(b, nameof(CustomerConfirmation.ResultCode), "result_code", 30, unicode: false);
        Mapping.String(b, nameof(CustomerConfirmation.Comment), "comment", 2000, nullable: true);
        Mapping.DateTime(b, nameof(CustomerConfirmation.ConfirmedAt), "confirmed_at", utcDefault: true);
        Mapping.Long(b, nameof(CustomerConfirmation.ConfirmedByUserId), "confirmed_by_user_id");
        Mapping.String(b, nameof(CustomerConfirmation.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.Fk<CustomerConfirmation, TransactionRecord>(b, nameof(CustomerConfirmation.TransactionId));
        Mapping.Fk<CustomerConfirmation, WorkCompletionRevision>(b, nameof(CustomerConfirmation.CompletionRevisionId));
        Mapping.Fk<CustomerConfirmation, User>(b, nameof(CustomerConfirmation.ConfirmedByUserId));
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.CompletionRevisionId);
        b.HasIndex(x => x.ResultCode);
        b.HasIndex(x => x.ConfirmedAt);
        b.HasIndex(x => x.ConfirmedByUserId);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => x.TransactionId).IsUnique().HasFilter("[result_code] = 'COMPLETED'");
        b.HasIndex(x => new { x.TransactionId, x.ConfirmedAt }).IsDescending(false, true);
        b.ToTable("customer_confirmations", t => t.HasCheckConstraint("CK_customer_confirmations_result", "[result_code] IN ('COMPLETED','REVISION_REQUESTED','DISPUTED')"));
    }
}

internal sealed class ServiceHistoryEntryConfiguration() : EntityConfiguration<ServiceHistoryEntry>("service_history_entries")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceHistoryEntry> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ServiceHistoryEntry.CustomerProfileId), "customer_profile_id");
        Mapping.NullableLong(b, nameof(ServiceHistoryEntry.TransactionId), "transaction_id");
        Mapping.NullableLong(b, nameof(ServiceHistoryEntry.SubscriptionVisitScheduleId), "subscription_visit_schedule_id");
        Mapping.NullableLong(b, nameof(ServiceHistoryEntry.InteriorProjectId), "interior_project_id");
        Mapping.NullableLong(b, nameof(ServiceHistoryEntry.SourceCompletionRevisionId), "source_completion_revision_id");
        Mapping.NullableLong(b, nameof(ServiceHistoryEntry.AfterServiceCaseId), "after_service_case_id");
        Mapping.String(b, nameof(ServiceHistoryEntry.EventTypeCode), "event_type_code", 40, unicode: false);
        Mapping.String(b, nameof(ServiceHistoryEntry.Title), "title", 200);
        Mapping.String(b, nameof(ServiceHistoryEntry.Summary), "summary", 2000);
        Mapping.String(b, nameof(ServiceHistoryEntry.ProviderNameSnapshot), "provider_name_snapshot", 200, nullable: true);
        Mapping.String(b, nameof(ServiceHistoryEntry.CategoryNameSnapshot), "category_name_snapshot", 500, nullable: true);
        Mapping.Decimal(b, nameof(ServiceHistoryEntry.TotalAmountSnapshot), "total_amount_snapshot", nullable: true);
        Mapping.String(b, nameof(ServiceHistoryEntry.CurrencyCode), "currency_code", 3, nullable: true, unicode: false, fixedLength: true);
        Mapping.DateTime(b, nameof(ServiceHistoryEntry.CompletedAtSnapshot), "completed_at_snapshot", nullable: true);
        Mapping.Date(b, nameof(ServiceHistoryEntry.WarrantyStartDate), "warranty_start_date", nullable: true);
        Mapping.Date(b, nameof(ServiceHistoryEntry.WarrantyEndDate), "warranty_end_date", nullable: true);
        Mapping.String(b, nameof(ServiceHistoryEntry.SnapshotJson), "snapshot_json", null);
        Mapping.DateTime(b, nameof(ServiceHistoryEntry.OccurredAt), "occurred_at");
        Mapping.String(b, nameof(ServiceHistoryEntry.IdempotencyKey), "idempotency_key", 120, unicode: false);
        Mapping.CreatedAudit(b);
        Mapping.Fk<ServiceHistoryEntry, CustomerProfile>(b, nameof(ServiceHistoryEntry.CustomerProfileId));
        Mapping.Fk<ServiceHistoryEntry, TransactionRecord>(b, nameof(ServiceHistoryEntry.TransactionId));
        Mapping.Fk<ServiceHistoryEntry, SubscriptionVisitSchedule>(b, nameof(ServiceHistoryEntry.SubscriptionVisitScheduleId));
        Mapping.Fk<ServiceHistoryEntry, InteriorProject>(b, nameof(ServiceHistoryEntry.InteriorProjectId));
        Mapping.Fk<ServiceHistoryEntry, WorkCompletionRevision>(b, nameof(ServiceHistoryEntry.SourceCompletionRevisionId));
        Mapping.Fk<ServiceHistoryEntry, AfterServiceCase>(b, nameof(ServiceHistoryEntry.AfterServiceCaseId));
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.SubscriptionVisitScheduleId).IsUnique().HasFilter("[subscription_visit_schedule_id] IS NOT NULL");
        b.HasIndex(x => x.InteriorProjectId);
        b.HasIndex(x => x.SourceCompletionRevisionId);
        b.HasIndex(x => x.AfterServiceCaseId);
        b.HasIndex(x => x.EventTypeCode);
        b.HasIndex(x => x.WarrantyEndDate);
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.CustomerProfileId, x.OccurredAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.TransactionId, x.EventTypeCode });
        b.ToTable("service_history_entries", t =>
        {
            t.HasCheckConstraint("CK_service_history_entries_event_type", "[event_type_code] IN ('COMPLETION','AFTER_SERVICE_RECEIVED','AFTER_SERVICE_STARTED','AFTER_SERVICE_COMPLETED','ASSET_LINKED','ASSET_CORRECTED')");
            t.HasCheckConstraint("CK_service_history_entries_snapshot_json", "ISJSON([snapshot_json]) = 1");
        });
    }
}

internal sealed class ServiceHistoryItemConfiguration() : EntityConfiguration<ServiceHistoryItem>("service_history_items")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceHistoryItem> b)
    {
        Mapping.Long(b, nameof(ServiceHistoryItem.ServiceHistoryEntryId), "service_history_entry_id");
        Mapping.Int(b, nameof(ServiceHistoryItem.LineNo), "line_no");
        Mapping.String(b, nameof(ServiceHistoryItem.ItemName), "item_name", 200);
        Mapping.String(b, nameof(ServiceHistoryItem.Description), "description", 1000, nullable: true);
        Mapping.Decimal(b, nameof(ServiceHistoryItem.Quantity), "quantity", nullable: true);
        Mapping.String(b, nameof(ServiceHistoryItem.UnitText), "unit_text", 50, nullable: true);
        Mapping.Decimal(b, nameof(ServiceHistoryItem.Amount), "amount", nullable: true);
        Mapping.String(b, nameof(ServiceHistoryItem.CurrencyCode), "currency_code", 3, nullable: true, unicode: false, fixedLength: true);
        Mapping.Fk<ServiceHistoryItem, ServiceHistoryEntry>(b, nameof(ServiceHistoryItem.ServiceHistoryEntryId));
        b.HasIndex(x => x.ServiceHistoryEntryId);
        b.HasIndex(x => new { x.ServiceHistoryEntryId, x.LineNo }).IsUnique();
    }
}

internal sealed class ServiceAssetConfiguration() : EntityConfiguration<ServiceAsset>("service_assets")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceAsset> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ServiceAsset.CustomerProfileId), "customer_profile_id");
        Mapping.String(b, nameof(ServiceAsset.AssetTypeCode), "asset_type_code", 50, unicode: false);
        Mapping.String(b, nameof(ServiceAsset.Name), "name", 200);
        Mapping.String(b, nameof(ServiceAsset.Manufacturer), "manufacturer", 200, nullable: true);
        Mapping.String(b, nameof(ServiceAsset.ModelName), "model_name", 200, nullable: true);
        Mapping.String(b, nameof(ServiceAsset.SerialNumber), "serial_number", 200, nullable: true);
        Mapping.Date(b, nameof(ServiceAsset.InstalledAt), "installed_at", nullable: true);
        Mapping.String(b, nameof(ServiceAsset.AttributesJson), "attributes_json", null, nullable: true);
        Mapping.String(b, nameof(ServiceAsset.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.FullAudit(b);
        Mapping.Fk<ServiceAsset, CustomerProfile>(b, nameof(ServiceAsset.CustomerProfileId));
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => x.AssetTypeCode);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => new { x.CustomerProfileId, x.StatusCode, x.Name });
        b.ToTable("service_assets", t =>
        {
            t.HasCheckConstraint("CK_service_assets_status", "[status_code] IN ('ACTIVE','INACTIVE')");
            t.HasCheckConstraint("CK_service_assets_attributes_json", "[attributes_json] IS NULL OR ISJSON([attributes_json]) = 1");
        });
    }
}

internal sealed class TransactionAssetLinkConfiguration() : EntityConfiguration<TransactionAssetLink>("transaction_asset_links")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionAssetLink> b)
    {
        Mapping.Long(b, nameof(TransactionAssetLink.TransactionId), "transaction_id");
        Mapping.Long(b, nameof(TransactionAssetLink.ServiceAssetId), "service_asset_id");
        Mapping.NullableLong(b, nameof(TransactionAssetLink.SourceHistoryEntryId), "source_history_entry_id");
        Mapping.String(b, nameof(TransactionAssetLink.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.String(b, nameof(TransactionAssetLink.CorrectionReason), "correction_reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(TransactionAssetLink.LinkedAt), "linked_at", utcDefault: true);
        Mapping.NullableLong(b, nameof(TransactionAssetLink.LinkedByUserId), "linked_by_user_id");
        Mapping.DateTime(b, nameof(TransactionAssetLink.CorrectedAt), "corrected_at", nullable: true);
        Mapping.NullableLong(b, nameof(TransactionAssetLink.CorrectedByUserId), "corrected_by_user_id");
        Mapping.Fk<TransactionAssetLink, TransactionRecord>(b, nameof(TransactionAssetLink.TransactionId));
        Mapping.Fk<TransactionAssetLink, ServiceAsset>(b, nameof(TransactionAssetLink.ServiceAssetId));
        Mapping.Fk<TransactionAssetLink, ServiceHistoryEntry>(b, nameof(TransactionAssetLink.SourceHistoryEntryId));
        Mapping.Fk<TransactionAssetLink, User>(b, nameof(TransactionAssetLink.LinkedByUserId));
        Mapping.Fk<TransactionAssetLink, User>(b, nameof(TransactionAssetLink.CorrectedByUserId));
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.ServiceAssetId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.LinkedAt);
        b.HasIndex(x => new { x.TransactionId, x.StatusCode });
        b.HasIndex(x => new { x.ServiceAssetId, x.LinkedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.TransactionId, x.ServiceAssetId }).IsUnique().HasFilter("[status_code] = 'ACTIVE'");
        b.ToTable("transaction_asset_links", t => t.HasCheckConstraint("CK_transaction_asset_links_status", "[status_code] IN ('ACTIVE','CORRECTED')"));
    }
}

internal sealed class AfterServiceCaseConfiguration() : EntityConfiguration<AfterServiceCase>("after_service_cases")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AfterServiceCase> b)
    {
        Mapping.PublicId(b);
        Mapping.NullableLong(b, nameof(AfterServiceCase.TransactionId), "transaction_id");
        Mapping.NullableLong(b, nameof(AfterServiceCase.SubscriptionVisitScheduleId), "subscription_visit_schedule_id");
        Mapping.Long(b, nameof(AfterServiceCase.CustomerProfileId), "customer_profile_id");
        Mapping.Long(b, nameof(AfterServiceCase.ProviderProfileId), "provider_profile_id");
        Mapping.NullableLong(b, nameof(AfterServiceCase.ReportedByUserId), "reported_by_user_id");
        Mapping.NullableLong(b, nameof(AfterServiceCase.AssignedAdminUserId), "assigned_admin_user_id");
        Mapping.String(b, nameof(AfterServiceCase.StatusCode), "status_code", 20, unicode: false, defaultValue: "RECEIVED");
        Mapping.String(b, nameof(AfterServiceCase.Subject), "subject", 200);
        Mapping.String(b, nameof(AfterServiceCase.Description), "description", null);
        Mapping.String(b, nameof(AfterServiceCase.RequestDetails), "request_details", 2000, nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceCase.ReceivedAt), "received_at", utcDefault: true);
        Mapping.Date(b, nameof(AfterServiceCase.WarrantyStartDate), "warranty_start_date", nullable: true);
        Mapping.Date(b, nameof(AfterServiceCase.WarrantyEndDate), "warranty_end_date", nullable: true);
        b.Property<bool?>(nameof(AfterServiceCase.IsWithinWarranty)).HasColumnName("is_within_warranty").HasColumnType("bit").IsRequired(false);
        Mapping.DateTime(b, nameof(AfterServiceCase.DueAt), "due_at", nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceCase.ProviderConfirmedAt), "provider_confirmed_at", nullable: true);
        Mapping.String(b, nameof(AfterServiceCase.ProviderResponseText), "provider_response_text", 2000, nullable: true);
        b.Property<bool?>(nameof(AfterServiceCase.VisitRequired)).HasColumnName("visit_required").HasColumnType("bit").IsRequired(false);
        Mapping.DateTime(b, nameof(AfterServiceCase.StartedAt), "started_at", nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceCase.CompletedAt), "completed_at", nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceCase.LastActionAt), "last_action_at", nullable: true);
        Mapping.String(b, nameof(AfterServiceCase.ResolutionSummary), "resolution_summary", 2000, nullable: true);
        Mapping.String(b, nameof(AfterServiceCase.UnresolvedReason), "unresolved_reason", 2000, nullable: true);
        b.Property<bool?>(nameof(AfterServiceCase.RecurrenceOccurred)).HasColumnName("recurrence_occurred").HasColumnType("bit").IsRequired(false);
        Mapping.DateTime(b, nameof(AfterServiceCase.ConvertedToDisputeAt), "converted_to_dispute_at", nullable: true);
        Mapping.String(b, nameof(AfterServiceCase.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.FullAudit(b);
        Mapping.Fk<AfterServiceCase, TransactionRecord>(b, nameof(AfterServiceCase.TransactionId));
        Mapping.Fk<AfterServiceCase, SubscriptionVisitSchedule>(b, nameof(AfterServiceCase.SubscriptionVisitScheduleId));
        Mapping.Fk<AfterServiceCase, CustomerProfile>(b, nameof(AfterServiceCase.CustomerProfileId));
        Mapping.Fk<AfterServiceCase, ProviderProfile>(b, nameof(AfterServiceCase.ProviderProfileId));
        Mapping.Fk<AfterServiceCase, User>(b, nameof(AfterServiceCase.ReportedByUserId));
        Mapping.Fk<AfterServiceCase, User>(b, nameof(AfterServiceCase.AssignedAdminUserId));
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.SubscriptionVisitScheduleId);
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.ReportedByUserId);
        b.HasIndex(x => x.AssignedAdminUserId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.ReceivedAt);
        b.HasIndex(x => x.CompletedAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.TransactionId, x.ReceivedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.ProviderProfileId, x.StatusCode, x.ReceivedAt });
        b.HasIndex(x => x.DueAt);
        b.HasIndex(x => x.LastActionAt);
        b.ToTable("after_service_cases", t => { t.HasCheckConstraint("CK_after_service_cases_source", "([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL)"); t.HasCheckConstraint("CK_after_service_cases_status", "[status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')"); });
    }
}

internal sealed class AfterServiceActionConfiguration() : EntityConfiguration<AfterServiceAction>("after_service_actions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AfterServiceAction> b)
    {
        Mapping.Long(b, nameof(AfterServiceAction.AfterServiceCaseId), "after_service_case_id");
        Mapping.String(b, nameof(AfterServiceAction.FromStatusCode), "from_status_code", 20, nullable: true, unicode: false);
        Mapping.String(b, nameof(AfterServiceAction.ToStatusCode), "to_status_code", 20, unicode: false);
        Mapping.String(b, nameof(AfterServiceAction.ActionTypeCode), "action_type_code", 30, unicode: false, defaultValue: "STATE_CHANGE");
        Mapping.String(b, nameof(AfterServiceAction.ActionNote), "action_note", 2000, nullable: true);
        Mapping.String(b, nameof(AfterServiceAction.Reason), "reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceAction.ScheduledAt), "scheduled_at", nullable: true);
        Mapping.DateTime(b, nameof(AfterServiceAction.PerformedAt), "performed_at", nullable: true);
        Mapping.NullableLong(b, nameof(AfterServiceAction.ProviderProfileId), "provider_profile_id");
        Mapping.Bool(b, nameof(AfterServiceAction.VisitOccurred), "visit_occurred", false);
        Mapping.String(b, nameof(AfterServiceAction.MaterialsText), "materials_text", 2000, nullable: true);
        Mapping.String(b, nameof(AfterServiceAction.ResultText), "result_text", 2000, nullable: true);
        b.Property<bool?>(nameof(AfterServiceAction.RecurrenceOccurred)).HasColumnName("recurrence_occurred").HasColumnType("bit").IsRequired(false);
        Mapping.DateTime(b, nameof(AfterServiceAction.OccurredAt), "occurred_at", utcDefault: true);
        Mapping.NullableLong(b, nameof(AfterServiceAction.ActorUserId), "actor_user_id");
        Mapping.String(b, nameof(AfterServiceAction.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.Fk<AfterServiceAction, AfterServiceCase>(b, nameof(AfterServiceAction.AfterServiceCaseId));
        Mapping.Fk<AfterServiceAction, User>(b, nameof(AfterServiceAction.ActorUserId));
        Mapping.Fk<AfterServiceAction, ProviderProfile>(b, nameof(AfterServiceAction.ProviderProfileId));
        b.HasIndex(x => x.AfterServiceCaseId);
        b.HasIndex(x => x.ToStatusCode);
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.ActorUserId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.ActionTypeCode);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.AfterServiceCaseId, x.OccurredAt });
        b.ToTable("after_service_actions", t =>
        {
            t.HasCheckConstraint("CK_after_service_actions_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')");
            t.HasCheckConstraint("CK_after_service_actions_to_status", "[to_status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')");
            t.HasCheckConstraint("CK_after_service_actions_type", "[action_type_code] IN ('RECEIVED','PROVIDER_CONFIRMATION','VISIT_SCHEDULED','VISIT','REVISIT','TREATMENT','STATE_CHANGE','RESOLUTION','UNRESOLVED_CLOSURE','DISPUTE_CONVERSION','ADMIN_OVERRIDE')");
        });
    }
}

internal sealed class AfterServiceFileConfiguration() : EntityConfiguration<AfterServiceFile>("after_service_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AfterServiceFile> b)
    {
        Mapping.Long(b, nameof(AfterServiceFile.AfterServiceCaseId), "after_service_case_id");
        Mapping.NullableLong(b, nameof(AfterServiceFile.AfterServiceActionId), "after_service_action_id");
        Mapping.Long(b, nameof(AfterServiceFile.FileId), "file_id");
        Mapping.String(b, nameof(AfterServiceFile.RoleCode), "role_code", 50, nullable: true, unicode: false);
        Mapping.String(b, nameof(AfterServiceFile.Description), "description", 500, nullable: true);
        Mapping.CreatedAudit(b);
        Mapping.Fk<AfterServiceFile, AfterServiceCase>(b, nameof(AfterServiceFile.AfterServiceCaseId));
        Mapping.Fk<AfterServiceFile, AfterServiceAction>(b, nameof(AfterServiceFile.AfterServiceActionId));
        Mapping.Fk<AfterServiceFile, StoredFile>(b, nameof(AfterServiceFile.FileId));
        b.HasIndex(x => x.AfterServiceCaseId);
        b.HasIndex(x => x.AfterServiceActionId);
        b.HasIndex(x => x.FileId);
        b.HasIndex(x => x.RoleCode);
        b.HasIndex(x => new { x.AfterServiceCaseId, x.FileId }).IsUnique();
    }
}

internal sealed class AuditLogConfiguration() : EntityConfiguration<AuditLog>("audit_logs")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> b)
    {
        Mapping.DateTime(b, nameof(AuditLog.OccurredAt), "occurred_at", utcDefault: true);
        Mapping.NullableLong(b, nameof(AuditLog.ActorUserId), "actor_user_id");
        Mapping.String(b, nameof(AuditLog.ActorRoleCode), "actor_role_code", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(AuditLog.ActionCode), "action_code", 50, unicode: false);
        Mapping.String(b, nameof(AuditLog.EntityType), "entity_type", 100, unicode: false);
        Mapping.Guid(b, nameof(AuditLog.EntityPublicId), "entity_public_id", nullable: true);
        Mapping.String(b, nameof(AuditLog.ResultCode), "result_code", 20, unicode: false, defaultValue: "SUCCESS");
        Mapping.Guid(b, nameof(AuditLog.CorrelationId), "correlation_id", nullable: true);
        Mapping.String(b, nameof(AuditLog.IpAddress), "ip_address", 45, nullable: true, unicode: false);
        Mapping.String(b, nameof(AuditLog.UserAgent), "user_agent", 1000, nullable: true);
        Mapping.String(b, nameof(AuditLog.Reason), "reason", 1000, nullable: true);
        Mapping.String(b, nameof(AuditLog.BeforeJson), "before_json", null, nullable: true);
        Mapping.String(b, nameof(AuditLog.AfterJson), "after_json", null, nullable: true);
        Mapping.String(b, nameof(AuditLog.MetadataJson), "metadata_json", null, nullable: true);
        Mapping.Fk<AuditLog, User>(b, nameof(AuditLog.ActorUserId));
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.ActorUserId);
        b.HasIndex(x => x.ActorRoleCode);
        b.HasIndex(x => x.ActionCode);
        b.HasIndex(x => x.EntityType);
        b.HasIndex(x => x.EntityPublicId);
        b.HasIndex(x => x.ResultCode);
        b.HasIndex(x => x.CorrelationId);
        b.HasIndex(x => new { x.EntityType, x.EntityPublicId, x.OccurredAt }).IsDescending(false, false, true);
        b.HasIndex(x => new { x.ActorUserId, x.OccurredAt }).IsDescending(false, true);
        b.ToTable("audit_logs", t =>
        {
            t.HasCheckConstraint("CK_audit_logs_result", "[result_code] IN ('SUCCESS','FAILURE')");
            t.HasCheckConstraint("CK_audit_logs_before_json", "[before_json] IS NULL OR ISJSON([before_json]) = 1");
            t.HasCheckConstraint("CK_audit_logs_after_json", "[after_json] IS NULL OR ISJSON([after_json]) = 1");
            t.HasCheckConstraint("CK_audit_logs_metadata_json", "[metadata_json] IS NULL OR ISJSON([metadata_json]) = 1");
        });
    }
}

internal sealed class OutboxEventConfiguration() : EntityConfiguration<OutboxEvent>("outbox_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<OutboxEvent> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(OutboxEvent.AggregateType), "aggregate_type", 100, unicode: false);
        Mapping.Guid(b, nameof(OutboxEvent.AggregatePublicId), "aggregate_public_id");
        Mapping.String(b, nameof(OutboxEvent.EventType), "event_type", 100, unicode: false);
        Mapping.String(b, nameof(OutboxEvent.PayloadJson), "payload_json", null);
        Mapping.String(b, nameof(OutboxEvent.StatusCode), "status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.DateTime(b, nameof(OutboxEvent.OccurredAt), "occurred_at", utcDefault: true);
        Mapping.DateTime(b, nameof(OutboxEvent.AvailableAt), "available_at", utcDefault: true);
        Mapping.Int(b, nameof(OutboxEvent.AttemptCount), "attempt_count", 0);
        Mapping.DateTime(b, nameof(OutboxEvent.LastAttemptAt), "last_attempt_at", nullable: true);
        Mapping.DateTime(b, nameof(OutboxEvent.ProcessedAt), "processed_at", nullable: true);
        Mapping.String(b, nameof(OutboxEvent.ErrorMessage), "error_message", 2000, nullable: true);
        Mapping.String(b, nameof(OutboxEvent.IdempotencyKey), "idempotency_key", 120, unicode: false);
        Mapping.NullableLong(b, nameof(OutboxEvent.CreatedByUserId), "created_by_user_id");
        Mapping.RowVersion(b);
        Mapping.Fk<OutboxEvent, User>(b, nameof(OutboxEvent.CreatedByUserId));
        b.HasIndex(x => x.AggregateType);
        b.HasIndex(x => x.AggregatePublicId);
        b.HasIndex(x => x.EventType);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.AvailableAt);
        b.HasIndex(x => x.ProcessedAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.StatusCode, x.AvailableAt, x.Id });
        b.ToTable("outbox_events", t =>
        {
            t.HasCheckConstraint("CK_outbox_events_status", "[status_code] IN ('PENDING','PROCESSING','PUBLISHED','FAILED')");
            t.HasCheckConstraint("CK_outbox_events_payload_json", "ISJSON([payload_json]) = 1");
        });
    }
}
