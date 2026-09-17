using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ServiceCategoryConfiguration() : EntityConfiguration<ServiceCategory>("service_categories")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceCategory> b)
    {
        Mapping.PublicId(b);
        Mapping.NullableLong(b, nameof(ServiceCategory.ParentId), "parent_id");
        Mapping.String(b, nameof(ServiceCategory.LevelCode), "level_code", 20, unicode: false);
        Mapping.String(b, nameof(ServiceCategory.ExternalCode), "external_code", 50, nullable: true, unicode: false);
        Mapping.String(b, nameof(ServiceCategory.SourceRecordId), "source_record_id", 50, nullable: true, unicode: false);
        Mapping.String(b, nameof(ServiceCategory.Name), "name", 200);
        Mapping.String(b, nameof(ServiceCategory.SearchKeywordsText), "search_keywords_text", 2000, nullable: true);
        Mapping.String(b, nameof(ServiceCategory.SearchSlug), "search_slug", 220, nullable: true, unicode: false);
        Mapping.String(b, nameof(ServiceCategory.SeoTitle), "seo_title", 200, nullable: true);
        Mapping.String(b, nameof(ServiceCategory.SeoDescription), "seo_description", 500, nullable: true);
        Mapping.Bool(b, nameof(ServiceCategory.IsSearchIndexable), "is_search_indexable", true);
        Mapping.String(b, nameof(ServiceCategory.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.Int(b, nameof(ServiceCategory.SortOrder), "sort_order", 0);
        Mapping.FullAudit(b);
        Mapping.Fk<ServiceCategory, ServiceCategory>(b, nameof(ServiceCategory.ParentId));
        b.HasIndex(x => x.ParentId);
        b.HasIndex(x => x.LevelCode);
        b.HasIndex(x => x.ExternalCode).IsUnique().HasFilter("[external_code] IS NOT NULL");
        b.HasIndex(x => x.SourceRecordId).IsUnique().HasFilter("[source_record_id] IS NOT NULL");
        b.HasIndex(x => x.SearchSlug).IsUnique().HasFilter("[search_slug] IS NOT NULL");
        b.HasIndex(x => new { x.ParentId, x.Name }).IsUnique().HasFilter(null);
        b.HasIndex(x => new { x.ParentId, x.StatusCode, x.SortOrder });
        b.ToTable("service_categories", t =>
        {
            t.HasCheckConstraint("CK_service_categories_level", "[level_code] IN ('MAJOR','MIDDLE','SERVICE')");
            t.HasCheckConstraint("CK_service_categories_status", "[status_code] IN ('ACTIVE','PAUSED','REVIEW')");
        });
    }
}

internal sealed class CategoryPolicyConfiguration() : EntityConfiguration<CategoryPolicy>("category_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryPolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryPolicy.CategoryId), "category_id");
        Mapping.String(b, nameof(CategoryPolicy.PolicyVersion), "policy_version", 30, unicode: false);
        Mapping.String(b, nameof(CategoryPolicy.TransactionTypeCode), "transaction_type_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryPolicy.RequestMethodText), "request_method_text", 300);
        Mapping.String(b, nameof(CategoryPolicy.OnsiteRequirementText), "onsite_requirement_text", 30);
        Mapping.Bool(b, nameof(CategoryPolicy.IsEmergencyAllowed), "is_emergency_allowed", false);
        Mapping.String(b, nameof(CategoryPolicy.SubscriptionOptionText), "subscription_option_text", 30);
        Mapping.String(b, nameof(CategoryPolicy.StandardWorkUnitText), "standard_work_unit_text", 100);
        Mapping.Decimal(b, nameof(CategoryPolicy.BasePriceAmount), "base_price_amount");
        Mapping.String(b, nameof(CategoryPolicy.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(CategoryPolicy.PriceMethodText), "price_method_text", 30);
        Mapping.String(b, nameof(CategoryPolicy.VatDisplayRuleText), "vat_display_rule_text", 100);
        Mapping.Decimal(b, nameof(CategoryPolicy.MinimumBudgetAmount), "minimum_budget_amount");
        Mapping.Short(b, nameof(CategoryPolicy.MaxQuoteCount), "max_quote_count");
        Mapping.Int(b, nameof(CategoryPolicy.QuoteValidityMinutes), "quote_validity_minutes");
        Mapping.Long(b, nameof(CategoryPolicy.FeePolicyId), "fee_policy_id");
        Mapping.Decimal(b, nameof(CategoryPolicy.EstimatedQuoteFeeAmount), "estimated_quote_fee_amount");
        Mapping.String(b, nameof(CategoryPolicy.FeeChargeTimingText), "fee_charge_timing_text", 200);
        Mapping.String(b, nameof(CategoryPolicy.FeeRestoreConditionText), "fee_restore_condition_text", 500);
        Mapping.String(b, nameof(CategoryPolicy.MatchingAreaRuleText), "matching_area_rule_text", 200);
        Mapping.String(b, nameof(CategoryPolicy.NotificationTargetRuleText), "notification_target_rule_text", 300);
        Mapping.Int(b, nameof(CategoryPolicy.ProviderResponseDeadlineMinutes), "provider_response_deadline_minutes");
        Mapping.String(b, nameof(CategoryPolicy.RequestFieldSummaryText), "request_field_summary_text", 1000);
        Mapping.Short(b, nameof(CategoryPolicy.RequiredCompletionPhotoCount), "required_completion_photo_count", 0);
        Mapping.String(b, nameof(CategoryPolicy.RequiredQualificationSummaryText), "required_qualification_summary_text", 1000);
        Mapping.String(b, nameof(CategoryPolicy.InsuranceRequirementText), "insurance_requirement_text", 100);
        Mapping.String(b, nameof(CategoryPolicy.SafetyGradeCode), "safety_grade_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryPolicy.CompletionEvidenceRuleText), "completion_evidence_rule_text", 1000);
        Mapping.Short(b, nameof(CategoryPolicy.DefaultWarrantyDays), "default_warranty_days", 0);
        Mapping.String(b, nameof(CategoryPolicy.TrustScoreDisplayText), "trust_score_display_text", 100);
        Mapping.String(b, nameof(CategoryPolicy.DefaultSortCode), "default_sort_code", 30, unicode: false);
        Mapping.String(b, nameof(CategoryPolicy.ServiceAreaLevelCode), "service_area_level_code", 20, unicode: false, defaultValue: "SIGUNGU");
        Mapping.String(b, nameof(CategoryPolicy.ReferenceUrl), "reference_url", 2048);
        Mapping.String(b, nameof(CategoryPolicy.AdminNote), "admin_note", 2000);
        Mapping.Date(b, nameof(CategoryPolicy.EffectiveFrom), "effective_from");
        Mapping.Date(b, nameof(CategoryPolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryPolicy, ServiceCategory>(b, nameof(CategoryPolicy.CategoryId));
        Mapping.Fk<CategoryPolicy, FeePolicy>(b, nameof(CategoryPolicy.FeePolicyId));
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.FeePolicyId);
        b.HasIndex(x => x.TransactionTypeCode);
        b.HasIndex(x => x.IsEmergencyAllowed);
        b.HasIndex(x => x.SafetyGradeCode);
        b.HasIndex(x => x.EffectiveFrom);
        b.HasIndex(x => x.EffectiveTo);
        b.HasIndex(x => new { x.CategoryId, x.PolicyVersion }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.EffectiveFrom }).IsDescending(false, true);
        b.ToTable("category_policies", t =>
        {
            t.HasCheckConstraint("CK_category_policies_transaction_type", "[transaction_type_code] IN ('ONE_TIME','SUBSCRIPTION','PROJECT')");
            t.HasCheckConstraint("CK_category_policies_safety_grade", "[safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')");
            t.HasCheckConstraint("CK_category_policies_photo_count", "[required_completion_photo_count] >= 0");
            t.HasCheckConstraint("CK_category_policies_warranty_days", "[default_warranty_days] >= 0");
            t.HasCheckConstraint("CK_category_policies_max_quotes", "[max_quote_count] > 0");
            t.HasCheckConstraint("CK_category_policies_quote_validity", "[quote_validity_minutes] > 0");
            t.HasCheckConstraint("CK_category_policies_response_deadline", "[provider_response_deadline_minutes] > 0");
        });
    }
}

internal sealed class CategoryPricePolicyConfiguration() : EntityConfiguration<CategoryPricePolicy>("category_price_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryPricePolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryPricePolicy.CategoryId), "category_id");
        Mapping.NullableLong(b, nameof(CategoryPricePolicy.LegacyCategoryPolicyId), "legacy_category_policy_id");
        Mapping.String(b, nameof(CategoryPricePolicy.PolicyVersion), "policy_version", 30, unicode: false);
        Mapping.String(b, nameof(CategoryPricePolicy.LegacyPriceMethodText), "legacy_price_method_text", 30);
        Mapping.String(b, nameof(CategoryPricePolicy.PriceTypeCode), "price_type_code", 30, nullable: true, unicode: false);
        Mapping.Decimal(b, nameof(CategoryPricePolicy.BaseAmount), "base_amount");
        Mapping.Decimal(b, nameof(CategoryPricePolicy.MinimumBudgetAmount), "minimum_budget_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryPricePolicy.RecommendedMinAmount), "recommended_min_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryPricePolicy.RecommendedMaxAmount), "recommended_max_amount", nullable: true);
        Mapping.String(b, nameof(CategoryPricePolicy.UnitText), "unit_text", 100, nullable: true);
        Mapping.Decimal(b, nameof(CategoryPricePolicy.UnitPriceAmount), "unit_price_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryPricePolicy.MinimumChargeAmount), "minimum_charge_amount", nullable: true);
        Mapping.String(b, nameof(CategoryPricePolicy.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(CategoryPricePolicy.LegacyVatDisplayRuleText), "legacy_vat_display_rule_text", 100);
        Mapping.String(b, nameof(CategoryPricePolicy.VatPolicyCode), "vat_policy_code", 20, nullable: true, unicode: false);
        Mapping.Date(b, nameof(CategoryPricePolicy.EffectiveFrom), "effective_from");
        Mapping.Date(b, nameof(CategoryPricePolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(CategoryPricePolicy.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryPricePolicy, ServiceCategory>(b, nameof(CategoryPricePolicy.CategoryId));
        Mapping.Fk<CategoryPricePolicy, CategoryPolicy>(b, nameof(CategoryPricePolicy.LegacyCategoryPolicyId));
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => x.LegacyCategoryPolicyId).IsUnique().HasFilter("[legacy_category_policy_id] IS NOT NULL");
        b.HasIndex(x => new { x.CategoryId, x.PolicyVersion }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        b.ToTable("category_price_policies", t =>
        {
            t.HasCheckConstraint("CK_category_price_policies_amounts", "[base_amount] >= 0 AND ([minimum_budget_amount] IS NULL OR [minimum_budget_amount] >= 0) AND ([recommended_min_amount] IS NULL OR [recommended_min_amount] >= 0) AND ([recommended_max_amount] IS NULL OR [recommended_max_amount] >= 0) AND ([unit_price_amount] IS NULL OR [unit_price_amount] >= 0) AND ([minimum_charge_amount] IS NULL OR [minimum_charge_amount] >= 0)");
            t.HasCheckConstraint("CK_category_price_policies_range", "[recommended_min_amount] IS NULL OR [recommended_max_amount] IS NULL OR [recommended_min_amount] <= [recommended_max_amount]");
            t.HasCheckConstraint("CK_category_price_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
        });
    }
}

internal sealed class CategoryFeePolicyConfiguration() : EntityConfiguration<CategoryFeePolicy>("category_fee_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryFeePolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryFeePolicy.CategoryId), "category_id");
        Mapping.NullableLong(b, nameof(CategoryFeePolicy.LegacyCategoryPolicyId), "legacy_category_policy_id");
        Mapping.NullableLong(b, nameof(CategoryFeePolicy.SourceFeePolicyId), "source_fee_policy_id");
        Mapping.String(b, nameof(CategoryFeePolicy.PolicyVersion), "policy_version", 30, unicode: false);
        Mapping.String(b, nameof(CategoryFeePolicy.PolicyKindCode), "policy_kind_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryFeePolicy.TransactionTypeCode), "transaction_type_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryFeePolicy.CalculationMethodText), "calculation_method_text", 100, nullable: true);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.FeeAmount), "fee_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.MinBaseAmount), "min_base_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.MaxBaseAmount), "max_base_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.Rate), "rate", nullable: true, precision: 9, scale: 6);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.MonthlyAmount), "monthly_amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryFeePolicy.PerVisitAmount), "per_visit_amount", nullable: true);
        Mapping.String(b, nameof(CategoryFeePolicy.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(CategoryFeePolicy.ChargeTimingText), "charge_timing_text", 300);
        Mapping.String(b, nameof(CategoryFeePolicy.RestoreRuleText), "restore_rule_text", 1000, nullable: true);
        Mapping.Date(b, nameof(CategoryFeePolicy.EffectiveFrom), "effective_from");
        Mapping.Date(b, nameof(CategoryFeePolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(CategoryFeePolicy.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryFeePolicy, ServiceCategory>(b, nameof(CategoryFeePolicy.CategoryId));
        Mapping.Fk<CategoryFeePolicy, CategoryPolicy>(b, nameof(CategoryFeePolicy.LegacyCategoryPolicyId));
        Mapping.Fk<CategoryFeePolicy, FeePolicy>(b, nameof(CategoryFeePolicy.SourceFeePolicyId));
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => x.LegacyCategoryPolicyId).IsUnique().HasFilter("[legacy_category_policy_id] IS NOT NULL");
        b.HasIndex(x => new { x.CategoryId, x.PolicyVersion }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        b.ToTable("category_fee_policies", t =>
        {
            t.HasCheckConstraint("CK_category_fee_policies_amounts", "([fee_amount] IS NULL OR [fee_amount] >= 0) AND ([min_base_amount] IS NULL OR [min_base_amount] >= 0) AND ([max_base_amount] IS NULL OR [max_base_amount] >= 0) AND ([monthly_amount] IS NULL OR [monthly_amount] >= 0) AND ([per_visit_amount] IS NULL OR [per_visit_amount] >= 0)");
            t.HasCheckConstraint("CK_category_fee_policies_rate", "[rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)");
            t.HasCheckConstraint("CK_category_fee_policies_base_range", "[min_base_amount] IS NULL OR [max_base_amount] IS NULL OR [min_base_amount] <= [max_base_amount]");
            t.HasCheckConstraint("CK_category_fee_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
        });
    }
}

internal sealed class CategoryOperationPolicyConfiguration() : EntityConfiguration<CategoryOperationPolicy>("category_operation_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryOperationPolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryOperationPolicy.CategoryId), "category_id");
        Mapping.NullableLong(b, nameof(CategoryOperationPolicy.LegacyCategoryPolicyId), "legacy_category_policy_id");
        Mapping.String(b, nameof(CategoryOperationPolicy.PolicyVersion), "policy_version", 30, unicode: false);
        Mapping.String(b, nameof(CategoryOperationPolicy.RequestMethodText), "request_method_text", 300);
        Mapping.String(b, nameof(CategoryOperationPolicy.OnsiteRequirementText), "onsite_requirement_text", 30);
        Mapping.String(b, nameof(CategoryOperationPolicy.CoverageTypeCode), "coverage_type_code", 30, unicode: false, defaultValue: "LOCAL_ONLY");
        Mapping.Bool(b, nameof(CategoryOperationPolicy.IsEmergencyAllowed), "is_emergency_allowed", false);
        Mapping.String(b, nameof(CategoryOperationPolicy.SubscriptionOptionText), "subscription_option_text", 30);
        Mapping.Short(b, nameof(CategoryOperationPolicy.MaxQuoteCount), "max_quote_count");
        Mapping.Int(b, nameof(CategoryOperationPolicy.QuoteValidityMinutes), "quote_validity_minutes");
        Mapping.String(b, nameof(CategoryOperationPolicy.MatchingAreaRuleText), "matching_area_rule_text", 200);
        Mapping.String(b, nameof(CategoryOperationPolicy.NotificationTargetRuleText), "notification_target_rule_text", 300);
        Mapping.Int(b, nameof(CategoryOperationPolicy.ProviderResponseDeadlineMinutes), "provider_response_deadline_minutes");
        Mapping.String(b, nameof(CategoryOperationPolicy.RequestFieldSummaryText), "request_field_summary_text", 1000);
        Mapping.Short(b, nameof(CategoryOperationPolicy.RequiredCompletionPhotoCount), "required_completion_photo_count", 0);
        Mapping.String(b, nameof(CategoryOperationPolicy.RequiredQualificationSummaryText), "required_qualification_summary_text", 1000);
        Mapping.String(b, nameof(CategoryOperationPolicy.InsuranceRequirementText), "insurance_requirement_text", 100);
        Mapping.String(b, nameof(CategoryOperationPolicy.SafetyGradeCode), "safety_grade_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryOperationPolicy.CompletionEvidenceRuleText), "completion_evidence_rule_text", 1000);
        Mapping.Short(b, nameof(CategoryOperationPolicy.DefaultWarrantyDays), "default_warranty_days", 0);
        Mapping.String(b, nameof(CategoryOperationPolicy.TrustScoreDisplayText), "trust_score_display_text", 100);
        Mapping.String(b, nameof(CategoryOperationPolicy.DefaultSortCode), "default_sort_code", 30, unicode: false);
        Mapping.String(b, nameof(CategoryOperationPolicy.ServiceAreaLevelCode), "service_area_level_code", 20, unicode: false);
        Mapping.String(b, nameof(CategoryOperationPolicy.ReferenceUrl), "reference_url", 2048);
        Mapping.String(b, nameof(CategoryOperationPolicy.AdminNote), "admin_note", 2000);
        Mapping.Date(b, nameof(CategoryOperationPolicy.EffectiveFrom), "effective_from");
        Mapping.Date(b, nameof(CategoryOperationPolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(CategoryOperationPolicy.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryOperationPolicy, ServiceCategory>(b, nameof(CategoryOperationPolicy.CategoryId));
        Mapping.Fk<CategoryOperationPolicy, CategoryPolicy>(b, nameof(CategoryOperationPolicy.LegacyCategoryPolicyId));
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => x.LegacyCategoryPolicyId).IsUnique().HasFilter("[legacy_category_policy_id] IS NOT NULL");
        b.HasIndex(x => new { x.CategoryId, x.PolicyVersion }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        b.ToTable("category_operation_policies", t =>
        {
            t.HasCheckConstraint("CK_category_operation_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
            t.HasCheckConstraint("CK_category_operation_policies_counts", "[max_quote_count] > 0 AND [quote_validity_minutes] > 0 AND [provider_response_deadline_minutes] > 0 AND [required_completion_photo_count] >= 0 AND [default_warranty_days] >= 0");
            t.HasCheckConstraint("CK_category_operation_policies_coverage_type", "[coverage_type_code] IN ('LOCAL_ONLY','NATIONWIDE_REMOTE','NATIONWIDE_DELIVERY','NATIONWIDE_NETWORK','FLEXIBLE')");
        });
    }
}

internal sealed class CategoryPricePolicyOptionConfiguration() : EntityConfiguration<CategoryPricePolicyOption>("category_price_policy_options")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryPricePolicyOption> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryPricePolicyOption.PricePolicyId), "price_policy_id");
        Mapping.String(b, nameof(CategoryPricePolicyOption.OptionName), "option_name", 200);
        Mapping.Decimal(b, nameof(CategoryPricePolicyOption.AdditionalAmount), "additional_amount", defaultValue: 0m);
        Mapping.Int(b, nameof(CategoryPricePolicyOption.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(CategoryPricePolicyOption.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryPricePolicyOption, CategoryPricePolicy>(b, nameof(CategoryPricePolicyOption.PricePolicyId));
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => new { x.PricePolicyId, x.IsActive, x.DisplayOrder });
        b.ToTable("category_price_policy_options", t => t.HasCheckConstraint("CK_category_price_policy_options_order", "[display_order] >= 0"));
    }
}

internal sealed class CategoryPricePolicySurchargeConfiguration() : EntityConfiguration<CategoryPricePolicySurcharge>("category_price_policy_surcharges")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryPricePolicySurcharge> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryPricePolicySurcharge.PricePolicyId), "price_policy_id");
        Mapping.String(b, nameof(CategoryPricePolicySurcharge.SurchargeName), "surcharge_name", 200);
        Mapping.String(b, nameof(CategoryPricePolicySurcharge.CalculationTypeCode), "calculation_type_code", 20, unicode: false);
        Mapping.Decimal(b, nameof(CategoryPricePolicySurcharge.Amount), "amount", nullable: true);
        Mapping.Decimal(b, nameof(CategoryPricePolicySurcharge.Rate), "rate", nullable: true, precision: 9, scale: 6);
        Mapping.Int(b, nameof(CategoryPricePolicySurcharge.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(CategoryPricePolicySurcharge.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryPricePolicySurcharge, CategoryPricePolicy>(b, nameof(CategoryPricePolicySurcharge.PricePolicyId));
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => new { x.PricePolicyId, x.IsActive, x.DisplayOrder });
        b.ToTable("category_price_policy_surcharges", t =>
        {
            t.HasCheckConstraint("CK_category_price_policy_surcharges_value", "(([amount] IS NOT NULL AND [amount] >= 0 AND [rate] IS NULL) OR ([amount] IS NULL AND [rate] IS NOT NULL AND [rate] >= 0 AND [rate] <= 1))");
            t.HasCheckConstraint("CK_category_price_policy_surcharges_order", "[display_order] >= 0");
        });
    }
}

internal sealed class CompletionPhotoRoleConfiguration() : EntityConfiguration<CompletionPhotoRole>("completion_photo_roles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CompletionPhotoRole> b)
    {
        Mapping.String(b, nameof(CompletionPhotoRole.Code), "code", 50, unicode: false);
        Mapping.String(b, nameof(CompletionPhotoRole.Name), "name", 100);
        Mapping.String(b, nameof(CompletionPhotoRole.Description), "description", 1000);
        Mapping.Bool(b, nameof(CompletionPhotoRole.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.IsActive);
    }
}

internal sealed class CategoryCompletionPhotoRequirementConfiguration() : EntityConfiguration<CategoryCompletionPhotoRequirement>("category_completion_photo_requirements")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryCompletionPhotoRequirement> b)
    {
        Mapping.Long(b, nameof(CategoryCompletionPhotoRequirement.CategoryPolicyId), "category_policy_id");
        Mapping.Long(b, nameof(CategoryCompletionPhotoRequirement.PhotoRoleId), "photo_role_id");
        Mapping.Short(b, nameof(CategoryCompletionPhotoRequirement.MinimumCount), "minimum_count");
        Mapping.Int(b, nameof(CategoryCompletionPhotoRequirement.DisplayOrder), "display_order", 0);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryCompletionPhotoRequirement, CategoryPolicy>(b, nameof(CategoryCompletionPhotoRequirement.CategoryPolicyId));
        Mapping.Fk<CategoryCompletionPhotoRequirement, CompletionPhotoRole>(b, nameof(CategoryCompletionPhotoRequirement.PhotoRoleId));
        b.HasIndex(x => x.CategoryPolicyId);
        b.HasIndex(x => x.PhotoRoleId);
        b.HasIndex(x => x.DisplayOrder);
        b.HasIndex(x => new { x.CategoryPolicyId, x.PhotoRoleId }).IsUnique();
        b.ToTable("category_completion_photo_requirements", t => t.HasCheckConstraint("CK_category_completion_photo_requirements_minimum", "[minimum_count] >= 0"));
    }
}

internal sealed class CategoryFieldDefinitionConfiguration() : EntityConfiguration<CategoryFieldDefinition>("category_field_definitions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryFieldDefinition> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(CategoryFieldDefinition.SourceFieldId), "source_field_id", 50, unicode: false);
        Mapping.Long(b, nameof(CategoryFieldDefinition.OwnerMiddleCategoryId), "owner_middle_category_id");
        Mapping.String(b, nameof(CategoryFieldDefinition.FieldKey), "field_key", 100, unicode: false);
        Mapping.String(b, nameof(CategoryFieldDefinition.Label), "label", 200);
        Mapping.String(b, nameof(CategoryFieldDefinition.FieldTypeCode), "field_type_code", 20, unicode: false);
        Mapping.Bool(b, nameof(CategoryFieldDefinition.IsRequired), "is_required", false);
        Mapping.String(b, nameof(CategoryFieldDefinition.OptionsOrUnitText), "options_or_unit_text", 2000, nullable: true);
        Mapping.String(b, nameof(CategoryFieldDefinition.UnitText), "unit_text", 2000, nullable: true);
        Mapping.String(b, nameof(CategoryFieldDefinition.ProviderVisibilityCode), "provider_visibility_code", 20, unicode: false, defaultValue: "FULL");
        Mapping.String(b, nameof(CategoryFieldDefinition.PreAcceptMaskingCode), "pre_accept_masking_code", 30, unicode: false, defaultValue: "NONE");
        Mapping.String(b, nameof(CategoryFieldDefinition.ValidationRuleText), "validation_rule_text", 1000);
        Mapping.Int(b, nameof(CategoryFieldDefinition.DisplayOrder), "display_order", 0);
        Mapping.String(b, nameof(CategoryFieldDefinition.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryFieldDefinition, ServiceCategory>(b, nameof(CategoryFieldDefinition.OwnerMiddleCategoryId));
        b.HasIndex(x => x.SourceFieldId).IsUnique();
        b.HasIndex(x => x.OwnerMiddleCategoryId);
        b.HasIndex(x => x.FieldTypeCode);
        b.HasIndex(x => x.IsRequired);
        b.HasIndex(x => new { x.OwnerMiddleCategoryId, x.FieldKey });
        b.HasIndex(x => new { x.OwnerMiddleCategoryId, x.StatusCode, x.DisplayOrder });
        b.ToTable("category_field_definitions", t =>
        {
            t.HasCheckConstraint("CK_category_field_definitions_type", "[field_type_code] IN ('LONG_TEXT','FILE','DATETIME','MONEY','TEXT','ADDRESS','SELECT','NUMBER','PERIOD','RECURRENCE')");
            t.HasCheckConstraint("CK_category_field_definitions_visibility", "[provider_visibility_code] IN ('FULL','AREA_ONLY')");
            t.HasCheckConstraint("CK_category_field_definitions_masking", "[pre_accept_masking_code] IN ('NONE','DETAIL_ADDRESS')");
            t.HasCheckConstraint("CK_category_field_definitions_status", "[status_code] IN ('ACTIVE','INACTIVE')");
        });
    }
}

internal sealed class CategoryFieldAssignmentConfiguration() : EntityConfiguration<CategoryFieldAssignment>("category_field_assignments")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryFieldAssignment> b)
    {
        Mapping.Long(b, nameof(CategoryFieldAssignment.FieldDefinitionId), "field_definition_id");
        Mapping.Long(b, nameof(CategoryFieldAssignment.TargetCategoryId), "target_category_id");
        Mapping.String(b, nameof(CategoryFieldAssignment.ScopeCode), "scope_code", 20, unicode: false);
        Mapping.Bool(b, nameof(CategoryFieldAssignment.IsRequired), "is_required", false);
        Mapping.Int(b, nameof(CategoryFieldAssignment.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(CategoryFieldAssignment.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryFieldAssignment, CategoryFieldDefinition>(b, nameof(CategoryFieldAssignment.FieldDefinitionId));
        Mapping.Fk<CategoryFieldAssignment, ServiceCategory>(b, nameof(CategoryFieldAssignment.TargetCategoryId));
        b.HasIndex(x => x.FieldDefinitionId);
        b.HasIndex(x => x.TargetCategoryId);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => new { x.TargetCategoryId, x.IsActive, x.DisplayOrder });
        b.HasIndex(x => new { x.FieldDefinitionId, x.TargetCategoryId }).IsUnique();
        b.ToTable("category_field_assignments", t =>
        {
            t.HasCheckConstraint("CK_category_field_assignments_scope", "[scope_code] IN ('MIDDLE','SERVICE')");
            t.HasCheckConstraint("CK_category_field_assignments_display_order", "[display_order] >= 0");
        });
    }
}

internal sealed class CategoryFieldOptionConfiguration() : EntityConfiguration<CategoryFieldOption>("category_field_options")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryFieldOption> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryFieldOption.FieldDefinitionId), "field_definition_id");
        Mapping.String(b, nameof(CategoryFieldOption.Value), "value", 450);
        Mapping.String(b, nameof(CategoryFieldOption.Label), "label", 1000);
        Mapping.Int(b, nameof(CategoryFieldOption.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(CategoryFieldOption.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryFieldOption, CategoryFieldDefinition>(b, nameof(CategoryFieldOption.FieldDefinitionId));
        b.HasIndex(x => new { x.FieldDefinitionId, x.Value }).IsUnique();
        b.HasIndex(x => new { x.FieldDefinitionId, x.DisplayOrder });
        b.HasIndex(x => new { x.FieldDefinitionId, x.IsActive, x.DisplayOrder });
        b.ToTable("category_field_options", t =>
            t.HasCheckConstraint("CK_category_field_options_display_order", "[display_order] >= 0"));
    }
}

internal sealed class FeePolicyConfiguration() : EntityConfiguration<FeePolicy>("fee_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<FeePolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(FeePolicy.Code), "code", 50, unicode: false);
        Mapping.String(b, nameof(FeePolicy.PolicyKindCode), "policy_kind_code", 20, unicode: false);
        Mapping.String(b, nameof(FeePolicy.TransactionTypeCode), "transaction_type_code", 20, unicode: false);
        Mapping.String(b, nameof(FeePolicy.AppliesToText), "applies_to_text", 200);
        Mapping.String(b, nameof(FeePolicy.CalculationMethodText), "calculation_method_text", 100, nullable: true);
        Mapping.Decimal(b, nameof(FeePolicy.MinBaseAmount), "min_base_amount", nullable: true);
        Mapping.Decimal(b, nameof(FeePolicy.MaxBaseAmount), "max_base_amount", nullable: true);
        Mapping.Decimal(b, nameof(FeePolicy.DisplayFeeAmount), "display_fee_amount", nullable: true);
        Mapping.Decimal(b, nameof(FeePolicy.Rate), "rate", nullable: true, precision: 9, scale: 6);
        Mapping.Decimal(b, nameof(FeePolicy.MonthlyAmount), "monthly_amount", nullable: true);
        Mapping.Decimal(b, nameof(FeePolicy.PerVisitAmount), "per_visit_amount", nullable: true);
        Mapping.String(b, nameof(FeePolicy.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(FeePolicy.ChargeTimingText), "charge_timing_text", 300);
        Mapping.String(b, nameof(FeePolicy.RestoreRuleText), "restore_rule_text", 1000, nullable: true);
        Mapping.String(b, nameof(FeePolicy.Note), "note", 1000, nullable: true);
        Mapping.Date(b, nameof(FeePolicy.EffectiveFrom), "effective_from", nullable: true);
        Mapping.Date(b, nameof(FeePolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(FeePolicy.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.PolicyKindCode);
        b.HasIndex(x => x.TransactionTypeCode);
        b.HasIndex(x => x.MinBaseAmount);
        b.HasIndex(x => x.MaxBaseAmount);
        b.HasIndex(x => x.EffectiveFrom);
        b.HasIndex(x => x.EffectiveTo);
        b.HasIndex(x => x.IsActive);
        b.ToTable("fee_policies", t =>
        {
            t.HasCheckConstraint("CK_fee_policies_kind", "[policy_kind_code] IN ('QUOTE','SUPPORT','PROJECT','SUBSCRIPTION')");
            t.HasCheckConstraint("CK_fee_policies_transaction_type", "[transaction_type_code] IN ('ONE_TIME','PROJECT','SUBSCRIPTION')");
            t.HasCheckConstraint("CK_fee_policies_rate", "[rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)");
        });
    }
}

internal sealed class QualificationPolicyConfiguration() : EntityConfiguration<QualificationPolicy>("qualification_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<QualificationPolicy> b)
    {
        Mapping.String(b, nameof(QualificationPolicy.SourcePolicyId), "source_policy_id", 50, unicode: false);
        Mapping.Long(b, nameof(QualificationPolicy.MiddleCategoryId), "middle_category_id");
        Mapping.String(b, nameof(QualificationPolicy.IdentityVerificationRuleText), "identity_verification_rule_text", 500);
        Mapping.String(b, nameof(QualificationPolicy.BusinessRegistrationRuleText), "business_registration_rule_text", 500);
        Mapping.String(b, nameof(QualificationPolicy.RequiredLicenseText), "required_license_text", 2000);
        Mapping.String(b, nameof(QualificationPolicy.InsuranceRuleText), "insurance_rule_text", 1000);
        Mapping.String(b, nameof(QualificationPolicy.EquipmentFacilityRuleText), "equipment_facility_rule_text", 1000);
        Mapping.String(b, nameof(QualificationPolicy.BackgroundCheckRuleText), "background_check_rule_text", 1000);
        Mapping.String(b, nameof(QualificationPolicy.SafetyGradeCode), "safety_grade_code", 20, unicode: false);
        Mapping.String(b, nameof(QualificationPolicy.EmergencyRuleText), "emergency_rule_text", 1000);
        Mapping.String(b, nameof(QualificationPolicy.ReviewCycleText), "review_cycle_text", 500);
        Mapping.String(b, nameof(QualificationPolicy.AdminChecklistText), "admin_checklist_text", 2000);
        Mapping.Date(b, nameof(QualificationPolicy.EffectiveFrom), "effective_from", nullable: true);
        Mapping.Date(b, nameof(QualificationPolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<QualificationPolicy, ServiceCategory>(b, nameof(QualificationPolicy.MiddleCategoryId));
        b.HasIndex(x => x.SourcePolicyId).IsUnique();
        b.HasIndex(x => x.MiddleCategoryId);
        b.HasIndex(x => x.SafetyGradeCode);
        b.HasIndex(x => x.EffectiveFrom);
        b.HasIndex(x => x.EffectiveTo);
        b.HasIndex(x => new { x.MiddleCategoryId, x.SourcePolicyId }).IsUnique();
        b.ToTable("qualification_policies", t => t.HasCheckConstraint("CK_qualification_policies_safety_grade", "[safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')"));
    }
}

internal sealed class ProviderRequirementTypeConfiguration : IEntityTypeConfiguration<ProviderRequirementType>
{
    public void Configure(EntityTypeBuilder<ProviderRequirementType> b)
    {
        b.ToTable("provider_requirement_types");
        b.HasKey(x => x.Code);
        Mapping.String(b, nameof(ProviderRequirementType.Code), "code", 30, unicode: false);
        Mapping.String(b, nameof(ProviderRequirementType.Name), "name", 100);
        Mapping.Bool(b, nameof(ProviderRequirementType.IsActive), "is_active", true);
        b.HasData(
            new ProviderRequirementType { Code = "QUALIFICATION", Name = "자격", IsActive = true },
            new ProviderRequirementType { Code = "LICENSE", Name = "면허", IsActive = true },
            new ProviderRequirementType { Code = "INSURANCE", Name = "보험", IsActive = true },
            new ProviderRequirementType { Code = "SAFETY", Name = "안전", IsActive = true },
            new ProviderRequirementType { Code = "EVIDENCE_VALIDITY", Name = "증빙 유효성", IsActive = true });
    }
}

internal sealed class ProviderRequirementDefinitionConfiguration() : EntityConfiguration<ProviderRequirementDefinition>("provider_requirement_definitions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderRequirementDefinition> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(ProviderRequirementDefinition.RequirementTypeCode), "requirement_type_code", 30, unicode: false);
        Mapping.String(b, nameof(ProviderRequirementDefinition.RequirementCode), "requirement_code", 50, unicode: false);
        Mapping.String(b, nameof(ProviderRequirementDefinition.Name), "name", 200);
        Mapping.String(b, nameof(ProviderRequirementDefinition.Description), "description", 1000, nullable: true);
        Mapping.Bool(b, nameof(ProviderRequirementDefinition.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderRequirementDefinition, ProviderRequirementType>(b, nameof(ProviderRequirementDefinition.RequirementTypeCode));
        b.HasIndex(x => x.RequirementCode).IsUnique();
        b.HasIndex(x => new { x.RequirementTypeCode, x.IsActive, x.Name });
    }
}

internal sealed class ProviderDocumentTypeConfiguration() : EntityConfiguration<ProviderDocumentType>("provider_document_types")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderDocumentType> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(ProviderDocumentType.Code), "code", 50, unicode: false);
        Mapping.String(b, nameof(ProviderDocumentType.Name), "name", 200);
        Mapping.Bool(b, nameof(ProviderDocumentType.SupportsExpiry), "supports_expiry", false);
        Mapping.Bool(b, nameof(ProviderDocumentType.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.Name });
    }
}

internal sealed class CategoryProviderRequirementAssignmentConfiguration() : EntityConfiguration<CategoryProviderRequirementAssignment>("category_provider_requirement_assignments")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryProviderRequirementAssignment> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryProviderRequirementAssignment.CategoryOperationPolicyId), "category_operation_policy_id");
        Mapping.Long(b, nameof(CategoryProviderRequirementAssignment.RequirementDefinitionId), "requirement_definition_id");
        Mapping.Bool(b, nameof(CategoryProviderRequirementAssignment.IsRequired), "is_required", true);
        Mapping.Bool(b, nameof(CategoryProviderRequirementAssignment.VerificationRequired), "verification_required", true);
        Mapping.Bool(b, nameof(CategoryProviderRequirementAssignment.ExpiryCheckRequired), "expiry_check_required", false);
        b.Property(x => x.MinimumValidDays).HasColumnName("minimum_valid_days").HasColumnType("smallint").IsRequired(false);
        Mapping.Int(b, nameof(CategoryProviderRequirementAssignment.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(CategoryProviderRequirementAssignment.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryProviderRequirementAssignment, CategoryOperationPolicy>(b, nameof(CategoryProviderRequirementAssignment.CategoryOperationPolicyId));
        Mapping.Fk<CategoryProviderRequirementAssignment, ProviderRequirementDefinition>(b, nameof(CategoryProviderRequirementAssignment.RequirementDefinitionId));
        b.HasIndex(x => new { x.CategoryOperationPolicyId, x.RequirementDefinitionId }).IsUnique();
        b.HasIndex(x => new { x.CategoryOperationPolicyId, x.IsActive, x.DisplayOrder });
        b.ToTable("category_provider_requirement_assignments", t =>
        {
            t.HasCheckConstraint("CK_category_provider_requirement_assignments_minimum_valid_days", "[minimum_valid_days] IS NULL OR [minimum_valid_days] >= 0");
            t.HasCheckConstraint("CK_category_provider_requirement_assignments_display_order", "[display_order] >= 0");
        });
    }
}

internal sealed class CategoryProviderRequirementEvidenceTypeConfiguration() : EntityConfiguration<CategoryProviderRequirementEvidenceType>("category_provider_requirement_evidence_types")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CategoryProviderRequirementEvidenceType> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CategoryProviderRequirementEvidenceType.RequirementAssignmentId), "requirement_assignment_id");
        Mapping.Long(b, nameof(CategoryProviderRequirementEvidenceType.DocumentTypeId), "document_type_id");
        Mapping.Bool(b, nameof(CategoryProviderRequirementEvidenceType.IsRequired), "is_required", true);
        Mapping.Int(b, nameof(CategoryProviderRequirementEvidenceType.DisplayOrder), "display_order", 0);
        Mapping.FullAudit(b);
        Mapping.Fk<CategoryProviderRequirementEvidenceType, CategoryProviderRequirementAssignment>(b, nameof(CategoryProviderRequirementEvidenceType.RequirementAssignmentId));
        Mapping.Fk<CategoryProviderRequirementEvidenceType, ProviderDocumentType>(b, nameof(CategoryProviderRequirementEvidenceType.DocumentTypeId));
        b.HasIndex(x => new { x.RequirementAssignmentId, x.DocumentTypeId }).IsUnique();
        b.HasIndex(x => new { x.RequirementAssignmentId, x.DisplayOrder });
        b.ToTable("category_provider_requirement_evidence_types", t =>
            t.HasCheckConstraint("CK_category_provider_requirement_evidence_types_display_order", "[display_order] >= 0"));
    }
}

internal sealed class AdministrativeAreaConfiguration() : EntityConfiguration<AdministrativeArea>("administrative_areas")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdministrativeArea> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(AdministrativeArea.SourceSystemCode), "source_system_code", 30, unicode: false, defaultValue: "MOIS_STANDARD_CODE");
        Mapping.String(b, nameof(AdministrativeArea.AreaCode), "area_code", 20, unicode: false);
        Mapping.String(b, nameof(AdministrativeArea.AreaName), "area_name", 100);
        Mapping.String(b, nameof(AdministrativeArea.AreaLevelCode), "area_level_code", 20, unicode: false);
        Mapping.NullableLong(b, nameof(AdministrativeArea.ParentAreaId), "parent_area_id");
        Mapping.String(b, nameof(AdministrativeArea.SourceParentAreaCode), "source_parent_area_code", 20, nullable: true, unicode: false);
        Mapping.Date(b, nameof(AdministrativeArea.SourceCreatedDate), "source_created_date", nullable: true);
        Mapping.Date(b, nameof(AdministrativeArea.SourceAbolishedDate), "source_abolished_date", nullable: true);
        Mapping.String(b, nameof(AdministrativeArea.AbolitionTypeCode), "abolition_type_code", 30, nullable: true, unicode: false);
        Mapping.Date(b, nameof(AdministrativeArea.EffectiveFrom), "effective_from");
        Mapping.Date(b, nameof(AdministrativeArea.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(AdministrativeArea.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<AdministrativeArea, AdministrativeArea>(b, nameof(AdministrativeArea.ParentAreaId));
        b.HasIndex(x => x.SourceSystemCode);
        b.HasIndex(x => x.AreaCode);
        b.HasIndex(x => x.AreaName);
        b.HasIndex(x => x.AreaLevelCode);
        b.HasIndex(x => x.ParentAreaId);
        b.HasIndex(x => x.SourceParentAreaCode);
        b.HasIndex(x => x.AbolitionTypeCode);
        b.HasIndex(x => x.EffectiveFrom);
        b.HasIndex(x => x.EffectiveTo);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => new { x.AreaCode, x.EffectiveFrom }).IsUnique();
        b.HasIndex(x => x.AreaCode).IsUnique().HasFilter("[is_active] = CAST(1 AS bit)");
        b.HasIndex(x => new { x.ParentAreaId, x.AreaLevelCode, x.IsActive, x.AreaName });
        b.ToTable("administrative_areas", t => t.HasCheckConstraint("CK_administrative_areas_level", "[area_level_code] IN ('SIDO','SIGUNGU')"));
    }
}
