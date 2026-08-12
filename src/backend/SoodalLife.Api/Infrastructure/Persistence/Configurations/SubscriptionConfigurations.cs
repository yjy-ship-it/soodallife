using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class CareProductConfiguration() : EntityConfiguration<CareProduct>("care_products")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CareProduct> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(CareProduct.ServiceCategoryId),"service_category_id");
        Mapping.String(b,nameof(CareProduct.ProductName),"product_name",200); Mapping.String(b,nameof(CareProduct.Description),"description",2000,nullable:true);
        Mapping.String(b,nameof(CareProduct.ServiceScopeText),"service_scope_text",4000); Mapping.Int(b,nameof(CareProduct.VisitsPerPeriod),"visits_per_period");
        Mapping.Int(b,nameof(CareProduct.ExpectedDurationMinutes),"expected_duration_minutes"); Mapping.String(b,nameof(CareProduct.BillingPeriodCode),"billing_period_code",30,unicode:false);
        Mapping.Decimal(b,nameof(CareProduct.StandardMonthlyAmount),"standard_monthly_amount",nullable:true); Mapping.Decimal(b,nameof(CareProduct.StandardVisitAmount),"standard_visit_amount",nullable:true);
        Mapping.Bool(b,nameof(CareProduct.IsActive),"is_active",true); Mapping.Date(b,nameof(CareProduct.EffectiveFrom),"effective_from"); Mapping.Date(b,nameof(CareProduct.EffectiveTo),"effective_to",nullable:true); Mapping.FullAudit(b);
        Mapping.Fk<CareProduct,ServiceCategory>(b,nameof(CareProduct.ServiceCategoryId)); b.HasIndex(x=>new{x.ServiceCategoryId,x.IsActive}); b.HasIndex(x=>new{x.ProductName,x.EffectiveFrom});
        b.ToTable("care_products",t=>{t.HasCheckConstraint("CK_care_products_visits","[visits_per_period] > 0 AND [expected_duration_minutes] > 0");t.HasCheckConstraint("CK_care_products_amount","[standard_monthly_amount] IS NULL OR [standard_monthly_amount] >= 0");t.HasCheckConstraint("CK_care_products_period","[effective_to] IS NULL OR [effective_to] > [effective_from]");});
    }
}

internal sealed class SubscriptionRequestConfiguration() : EntityConfiguration<SubscriptionRequest>("subscription_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionRequest> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionRequest.CustomerProfileId),"customer_profile_id"); Mapping.Long(b,nameof(SubscriptionRequest.ServiceCategoryId),"service_category_id");
        Mapping.NullableLong(b,nameof(SubscriptionRequest.CareProductId),"care_product_id"); Mapping.Long(b,nameof(SubscriptionRequest.AdministrativeAreaId),"administrative_area_id");
        Mapping.String(b,nameof(SubscriptionRequest.RequestTypeCode),"request_type_code",20,unicode:false); Mapping.String(b,nameof(SubscriptionRequest.RequestedScopeText),"requested_scope_text",4000);
        Mapping.Date(b,nameof(SubscriptionRequest.PreferredStartDate),"preferred_start_date"); Mapping.String(b,nameof(SubscriptionRequest.DetailAddress),"detail_address",500,nullable:true);
        Mapping.Binary(b,nameof(SubscriptionRequest.DetailAddressEncrypted),"detail_address_encrypted"); Mapping.NullableShort(b,nameof(SubscriptionRequest.PrivacyProtectionVersion),"privacy_protection_version");
        Mapping.String(b,nameof(SubscriptionRequest.StatusCode),"status_code",30,unicode:false); Mapping.NullableLong(b,nameof(SubscriptionRequest.SelectedApplicationId),"selected_application_id"); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionRequest,CustomerProfile>(b,nameof(SubscriptionRequest.CustomerProfileId)); Mapping.Fk<SubscriptionRequest,ServiceCategory>(b,nameof(SubscriptionRequest.ServiceCategoryId));
        Mapping.Fk<SubscriptionRequest,CareProduct>(b,nameof(SubscriptionRequest.CareProductId)); Mapping.Fk<SubscriptionRequest,AdministrativeArea>(b,nameof(SubscriptionRequest.AdministrativeAreaId));
        Mapping.Fk<SubscriptionRequest,SubscriptionApplication>(b,nameof(SubscriptionRequest.SelectedApplicationId));
        b.HasIndex(x=>new{x.CustomerProfileId,x.StatusCode}); b.HasIndex(x=>new{x.ServiceCategoryId,x.AdministrativeAreaId,x.StatusCode}); b.HasIndex(x=>x.SelectedApplicationId).IsUnique().HasFilter("[selected_application_id] IS NOT NULL");
        b.ToTable("subscription_requests",t=>{t.HasCheckConstraint("CK_subscription_requests_type","[request_type_code] IN ('STANDARD','CUSTOM')");t.HasCheckConstraint("CK_subscription_requests_product","([request_type_code] = 'STANDARD' AND [care_product_id] IS NOT NULL) OR [request_type_code] = 'CUSTOM'");t.HasCheckConstraint("CK_subscription_requests_status","[status_code] IN ('OPEN','SELECTED','CONTRACTED','CANCELLED','CLOSED')");});
    }
}

internal sealed class SubscriptionRecurrenceRuleConfiguration() : EntityConfiguration<SubscriptionRecurrenceRule>("subscription_recurrence_rules")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionRecurrenceRule> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionRecurrenceRule.SubscriptionRequestId),"subscription_request_id"); Mapping.String(b,nameof(SubscriptionRecurrenceRule.FrequencyTypeCode),"frequency_type_code",30,unicode:false);
        Mapping.Int(b,nameof(SubscriptionRecurrenceRule.IntervalValue),"interval_value"); b.Property(x=>x.VisitsPerPeriod).HasColumnName("visits_per_period").HasColumnType("int");
        Mapping.String(b,nameof(SubscriptionRecurrenceRule.WeekdaysJson),"weekdays_json",1000,nullable:true); b.Property(x=>x.PreferredTimeFrom).HasColumnName("preferred_time_from").HasColumnType("time(0)"); b.Property(x=>x.PreferredTimeTo).HasColumnName("preferred_time_to").HasColumnType("time(0)");
        Mapping.Int(b,nameof(SubscriptionRecurrenceRule.ExpectedDurationMinutes),"expected_duration_minutes"); Mapping.Date(b,nameof(SubscriptionRecurrenceRule.StartDate),"start_date"); Mapping.Date(b,nameof(SubscriptionRecurrenceRule.EndDate),"end_date",nullable:true);
        Mapping.String(b,nameof(SubscriptionRecurrenceRule.AdditionalRuleJson),"additional_rule_json",null,nullable:true); Mapping.FullAudit(b); Mapping.Fk<SubscriptionRecurrenceRule,SubscriptionRequest>(b,nameof(SubscriptionRecurrenceRule.SubscriptionRequestId));
        b.HasIndex(x=>x.SubscriptionRequestId).IsUnique(); b.ToTable("subscription_recurrence_rules",t=>{t.HasCheckConstraint("CK_subscription_recurrence_frequency","[frequency_type_code] IN ('WEEKLY','BIWEEKLY','MONTHLY','QUARTERLY','HALF_YEARLY')");t.HasCheckConstraint("CK_subscription_recurrence_values","[interval_value] > 0 AND [expected_duration_minutes] > 0");t.HasCheckConstraint("CK_subscription_recurrence_period","[end_date] IS NULL OR [end_date] >= [start_date]");});
    }
}

internal sealed class SubscriptionApplicationConfiguration() : EntityConfiguration<SubscriptionApplication>("subscription_applications")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionApplication> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionApplication.SubscriptionRequestId),"subscription_request_id"); Mapping.Long(b,nameof(SubscriptionApplication.ProviderProfileId),"provider_profile_id");
        Mapping.String(b,nameof(SubscriptionApplication.ProposedScopeText),"proposed_scope_text",4000); Mapping.Decimal(b,nameof(SubscriptionApplication.ProposedMonthlyAmount),"proposed_monthly_amount",nullable:true); Mapping.Decimal(b,nameof(SubscriptionApplication.ProposedVisitAmount),"proposed_visit_amount",nullable:true);
        Mapping.String(b,nameof(SubscriptionApplication.AvailableScheduleText),"available_schedule_text",2000,nullable:true); Mapping.String(b,nameof(SubscriptionApplication.StatusCode),"status_code",30,unicode:false); Mapping.DateTime(b,nameof(SubscriptionApplication.SubmittedAt),"submitted_at"); Mapping.String(b,nameof(SubscriptionApplication.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionApplication,SubscriptionRequest>(b,nameof(SubscriptionApplication.SubscriptionRequestId)); Mapping.Fk<SubscriptionApplication,ProviderProfile>(b,nameof(SubscriptionApplication.ProviderProfileId));
        b.HasIndex(x=>new{x.SubscriptionRequestId,x.ProviderProfileId}).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SubscriptionRequestId,x.StatusCode});
        b.ToTable("subscription_applications",t=>{t.HasCheckConstraint("CK_subscription_applications_amount","[proposed_monthly_amount] IS NULL OR [proposed_monthly_amount] >= 0");t.HasCheckConstraint("CK_subscription_applications_status","[status_code] IN ('SUBMITTED','SELECTED','NOT_SELECTED','WITHDRAWN')");});
    }
}

internal sealed class SubscriptionContractConfiguration() : EntityConfiguration<SubscriptionContract>("subscription_contracts")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionContract> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionContract.SubscriptionRequestId),"subscription_request_id"); Mapping.Long(b,nameof(SubscriptionContract.CustomerProfileId),"customer_profile_id"); Mapping.Long(b,nameof(SubscriptionContract.ProviderProfileId),"provider_profile_id"); Mapping.Long(b,nameof(SubscriptionContract.ServiceCategoryId),"service_category_id"); Mapping.NullableLong(b,nameof(SubscriptionContract.CareProductId),"care_product_id"); Mapping.Long(b,nameof(SubscriptionContract.SubscriptionApplicationId),"subscription_application_id");
        Mapping.String(b,nameof(SubscriptionContract.StatusCode),"status_code",30,unicode:false); Mapping.DateTime(b,nameof(SubscriptionContract.StartedAt),"started_at"); Mapping.DateTime(b,nameof(SubscriptionContract.EndedAt),"ended_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionContract.PauseStartedAt),"pause_started_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionContract.ResumePlannedAt),"resume_planned_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionContract.TerminationRequestedAt),"termination_requested_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionContract.TerminatedAt),"terminated_at",nullable:true); Mapping.String(b,nameof(SubscriptionContract.TerminationReason),"termination_reason",2000,nullable:true);
        Mapping.String(b,nameof(SubscriptionContract.PriceSnapshotJson),"price_snapshot_json",null); Mapping.String(b,nameof(SubscriptionContract.FeePolicySnapshotJson),"fee_policy_snapshot_json",null,nullable:true); Mapping.String(b,nameof(SubscriptionContract.ServiceScopeSnapshotJson),"service_scope_snapshot_json",null); Mapping.String(b,nameof(SubscriptionContract.RecurrenceSnapshotJson),"recurrence_snapshot_json",null); Mapping.Decimal(b,nameof(SubscriptionContract.ProviderTrustScoreSnapshot),"provider_trust_score_snapshot",nullable:true,precision:9,scale:4); Mapping.String(b,nameof(SubscriptionContract.CurrencyCode),"currency_code",3,unicode:false,fixedLength:true); Mapping.FullAudit(b);
        Mapping.DateTime(b,nameof(SubscriptionContract.NextBillingAt),"next_billing_at",nullable:true); Mapping.String(b,nameof(SubscriptionContract.BillingStatusCode),"billing_status_code",30,nullable:true,unicode:false);
        Mapping.Fk<SubscriptionContract,SubscriptionRequest>(b,nameof(SubscriptionContract.SubscriptionRequestId)); Mapping.Fk<SubscriptionContract,CustomerProfile>(b,nameof(SubscriptionContract.CustomerProfileId)); Mapping.Fk<SubscriptionContract,ProviderProfile>(b,nameof(SubscriptionContract.ProviderProfileId)); Mapping.Fk<SubscriptionContract,ServiceCategory>(b,nameof(SubscriptionContract.ServiceCategoryId)); Mapping.Fk<SubscriptionContract,CareProduct>(b,nameof(SubscriptionContract.CareProductId)); Mapping.Fk<SubscriptionContract,SubscriptionApplication>(b,nameof(SubscriptionContract.SubscriptionApplicationId));
        b.HasIndex(x=>x.SubscriptionRequestId).IsUnique(); b.HasIndex(x=>x.SubscriptionApplicationId).IsUnique(); b.HasIndex(x=>new{x.CustomerProfileId,x.StatusCode}); b.HasIndex(x=>new{x.ProviderProfileId,x.StatusCode});
        b.ToTable("subscription_contracts",t=>t.HasCheckConstraint("CK_subscription_contracts_status","[status_code] IN ('ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED')"));
    }
}

internal sealed class SubscriptionVisitScheduleConfiguration() : EntityConfiguration<SubscriptionVisitSchedule>("subscription_visit_schedules")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionVisitSchedule> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SubscriptionVisitSchedule.SubscriptionContractId),"subscription_contract_id"); Mapping.Int(b,nameof(SubscriptionVisitSchedule.VisitNo),"visit_no"); Mapping.Long(b,nameof(SubscriptionVisitSchedule.ProviderProfileId),"provider_profile_id"); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.ScheduledStartAt),"scheduled_start_at"); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.ScheduledEndAt),"scheduled_end_at",nullable:true); Mapping.String(b,nameof(SubscriptionVisitSchedule.StatusCode),"status_code",30,unicode:false);
        Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.VisitVerifiedAt),"visit_verified_at",nullable:true); Mapping.String(b,nameof(SubscriptionVisitSchedule.VisitVerificationMethodCode),"visit_verification_method_code",30,nullable:true,unicode:false); Mapping.String(b,nameof(SubscriptionVisitSchedule.VisitVerificationResultCode),"visit_verification_result_code",30,nullable:true,unicode:false); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.WorkStartedAt),"work_started_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.WorkCompletedAt),"work_completed_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.ProviderCompletionSubmittedAt),"provider_completion_submitted_at",nullable:true); Mapping.DateTime(b,nameof(SubscriptionVisitSchedule.CustomerConfirmedAt),"customer_confirmed_at",nullable:true); Mapping.String(b,nameof(SubscriptionVisitSchedule.CompletionChecklistJson),"completion_checklist_json",null,nullable:true); Mapping.String(b,nameof(SubscriptionVisitSchedule.CompletionNote),"completion_note",2000,nullable:true); Mapping.String(b,nameof(SubscriptionVisitSchedule.SettlementStatusCode),"settlement_status_code",30,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<SubscriptionVisitSchedule,SubscriptionContract>(b,nameof(SubscriptionVisitSchedule.SubscriptionContractId)); Mapping.Fk<SubscriptionVisitSchedule,ProviderProfile>(b,nameof(SubscriptionVisitSchedule.ProviderProfileId)); b.HasIndex(x=>new{x.SubscriptionContractId,x.VisitNo}).IsUnique(); b.HasIndex(x=>new{x.ProviderProfileId,x.ScheduledStartAt}); b.HasIndex(x=>new{x.StatusCode,x.ScheduledStartAt});
        b.ToTable("subscription_visit_schedules",t=>{t.HasCheckConstraint("CK_subscription_visits_status","[status_code] IN ('SCHEDULED','RESCHEDULED','SKIPPED','PAUSED','IN_PROGRESS','PROVIDER_COMPLETED','COMPLETED','CANCELLED','DISPUTED')");t.HasCheckConstraint("CK_subscription_visits_settlement","[settlement_status_code] IN ('NOT_READY','READY','HOLD','SETTLED')");t.HasCheckConstraint("CK_subscription_visits_number","[visit_no] > 0");});
    }
}

internal sealed class SubscriptionVisitFileConfiguration() : EntityConfiguration<SubscriptionVisitFile>("subscription_visit_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionVisitFile> b){Mapping.Long(b,nameof(SubscriptionVisitFile.SubscriptionVisitScheduleId),"subscription_visit_schedule_id");Mapping.Long(b,nameof(SubscriptionVisitFile.FileId),"file_id");Mapping.String(b,nameof(SubscriptionVisitFile.PurposeCode),"purpose_code",50,unicode:false);Mapping.Int(b,nameof(SubscriptionVisitFile.DisplayOrder),"display_order");Mapping.CreatedAudit(b);Mapping.Fk<SubscriptionVisitFile,SubscriptionVisitSchedule>(b,nameof(SubscriptionVisitFile.SubscriptionVisitScheduleId));Mapping.Fk<SubscriptionVisitFile,StoredFile>(b,nameof(SubscriptionVisitFile.FileId));b.HasIndex(x=>new{x.SubscriptionVisitScheduleId,x.FileId}).IsUnique();b.HasIndex(x=>x.FileId).IsUnique();}
}

internal sealed class SubscriptionScheduleChangeConfiguration() : EntityConfiguration<SubscriptionScheduleChange>("subscription_schedule_changes")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionScheduleChange> b){Mapping.PublicId(b);Mapping.Long(b,nameof(SubscriptionScheduleChange.SubscriptionVisitScheduleId),"subscription_visit_schedule_id");Mapping.Long(b,nameof(SubscriptionScheduleChange.RequestedByUserId),"requested_by_user_id");Mapping.String(b,nameof(SubscriptionScheduleChange.OldScheduleJson),"old_schedule_json",null);Mapping.String(b,nameof(SubscriptionScheduleChange.NewScheduleJson),"new_schedule_json",null);Mapping.String(b,nameof(SubscriptionScheduleChange.Reason),"reason",2000);Mapping.String(b,nameof(SubscriptionScheduleChange.StatusCode),"status_code",30,unicode:false);Mapping.DateTime(b,nameof(SubscriptionScheduleChange.RequestedAt),"requested_at");Mapping.DateTime(b,nameof(SubscriptionScheduleChange.DecidedAt),"decided_at",nullable:true);Mapping.NullableLong(b,nameof(SubscriptionScheduleChange.DecidedByUserId),"decided_by_user_id");Mapping.String(b,nameof(SubscriptionScheduleChange.IdempotencyKey),"idempotency_key",150,unicode:false);Mapping.DateTime(b,nameof(SubscriptionScheduleChange.CreatedAt),"created_at",utcDefault:true);Mapping.DateTime(b,nameof(SubscriptionScheduleChange.UpdatedAt),"updated_at",utcDefault:true);Mapping.RowVersion(b);Mapping.Fk<SubscriptionScheduleChange,SubscriptionVisitSchedule>(b,nameof(SubscriptionScheduleChange.SubscriptionVisitScheduleId));Mapping.Fk<SubscriptionScheduleChange,User>(b,nameof(SubscriptionScheduleChange.RequestedByUserId));Mapping.Fk<SubscriptionScheduleChange,User>(b,nameof(SubscriptionScheduleChange.DecidedByUserId));b.HasIndex(x=>x.IdempotencyKey).IsUnique();b.HasIndex(x=>new{x.SubscriptionVisitScheduleId,x.StatusCode});b.ToTable("subscription_schedule_changes",t=>t.HasCheckConstraint("CK_subscription_schedule_changes_status","[status_code] IN ('REQUESTED','APPROVED','REJECTED')"));}
}

internal sealed class SubscriptionEventConfiguration() : EntityConfiguration<SubscriptionEvent>("subscription_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionEvent> b){Mapping.PublicId(b);Mapping.NullableLong(b,nameof(SubscriptionEvent.SubscriptionRequestId),"subscription_request_id");Mapping.NullableLong(b,nameof(SubscriptionEvent.SubscriptionContractId),"subscription_contract_id");Mapping.NullableLong(b,nameof(SubscriptionEvent.SubscriptionVisitScheduleId),"subscription_visit_schedule_id");Mapping.String(b,nameof(SubscriptionEvent.EventTypeCode),"event_type_code",50,unicode:false);Mapping.String(b,nameof(SubscriptionEvent.EventDataJson),"event_data_json",null,nullable:true);Mapping.DateTime(b,nameof(SubscriptionEvent.OccurredAt),"occurred_at",utcDefault:true);Mapping.NullableLong(b,nameof(SubscriptionEvent.ActorUserId),"actor_user_id");Mapping.String(b,nameof(SubscriptionEvent.IdempotencyKey),"idempotency_key",150,unicode:false);Mapping.Fk<SubscriptionEvent,SubscriptionRequest>(b,nameof(SubscriptionEvent.SubscriptionRequestId));Mapping.Fk<SubscriptionEvent,SubscriptionContract>(b,nameof(SubscriptionEvent.SubscriptionContractId));Mapping.Fk<SubscriptionEvent,SubscriptionVisitSchedule>(b,nameof(SubscriptionEvent.SubscriptionVisitScheduleId));Mapping.Fk<SubscriptionEvent,User>(b,nameof(SubscriptionEvent.ActorUserId));b.HasIndex(x=>x.IdempotencyKey).IsUnique();b.HasIndex(x=>new{x.SubscriptionContractId,x.OccurredAt});b.HasIndex(x=>new{x.SubscriptionVisitScheduleId,x.OccurredAt});b.ToTable("subscription_events",t=>t.HasCheckConstraint("CK_subscription_events_parent","[subscription_request_id] IS NOT NULL OR [subscription_contract_id] IS NOT NULL OR [subscription_visit_schedule_id] IS NOT NULL"));}
}
