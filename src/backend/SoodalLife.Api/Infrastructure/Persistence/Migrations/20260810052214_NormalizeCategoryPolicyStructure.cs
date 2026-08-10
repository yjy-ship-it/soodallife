using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCategoryPolicyStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_fee_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    legacy_category_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    source_fee_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    policy_version = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    policy_kind_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    transaction_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    calculation_method_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    min_base_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    max_base_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    rate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    monthly_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    per_visit_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    charge_timing_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    restore_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_fee_policies", x => x.id);
                    table.CheckConstraint("CK_category_fee_policies_amounts", "([fee_amount] IS NULL OR [fee_amount] >= 0) AND ([min_base_amount] IS NULL OR [min_base_amount] >= 0) AND ([max_base_amount] IS NULL OR [max_base_amount] >= 0) AND ([monthly_amount] IS NULL OR [monthly_amount] >= 0) AND ([per_visit_amount] IS NULL OR [per_visit_amount] >= 0)");
                    table.CheckConstraint("CK_category_fee_policies_base_range", "[min_base_amount] IS NULL OR [max_base_amount] IS NULL OR [min_base_amount] <= [max_base_amount]");
                    table.CheckConstraint("CK_category_fee_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_category_fee_policies_rate", "[rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)");
                    table.ForeignKey(
                        name: "FK_category_fee_policies_category_policies_legacy_category_policy_id",
                        column: x => x.legacy_category_policy_id,
                        principalTable: "category_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_fee_policies_fee_policies_source_fee_policy_id",
                        column: x => x.source_fee_policy_id,
                        principalTable: "fee_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_fee_policies_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_fee_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_fee_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_operation_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    legacy_category_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    policy_version = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    request_method_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    onsite_requirement_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_emergency_allowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    subscription_option_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    max_quote_count = table.Column<short>(type: "smallint", nullable: false),
                    quote_validity_minutes = table.Column<int>(type: "int", nullable: false),
                    matching_area_rule_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    notification_target_rule_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    provider_response_deadline_minutes = table.Column<int>(type: "int", nullable: false),
                    request_field_summary_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    required_completion_photo_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    required_qualification_summary_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    insurance_requirement_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    safety_grade_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    completion_evidence_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    default_warranty_days = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    trust_score_display_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    default_sort_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    service_area_level_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    reference_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    admin_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_operation_policies", x => x.id);
                    table.CheckConstraint("CK_category_operation_policies_counts", "[max_quote_count] > 0 AND [quote_validity_minutes] > 0 AND [provider_response_deadline_minutes] > 0 AND [required_completion_photo_count] >= 0 AND [default_warranty_days] >= 0");
                    table.CheckConstraint("CK_category_operation_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
                    table.ForeignKey(
                        name: "FK_category_operation_policies_category_policies_legacy_category_policy_id",
                        column: x => x.legacy_category_policy_id,
                        principalTable: "category_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_operation_policies_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_operation_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_operation_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_price_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    legacy_category_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    policy_version = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    legacy_price_method_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    price_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    base_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    minimum_budget_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    recommended_min_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    recommended_max_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    unit_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    minimum_charge_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    legacy_vat_display_rule_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    vat_policy_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_price_policies", x => x.id);
                    table.CheckConstraint("CK_category_price_policies_amounts", "[base_amount] >= 0 AND ([minimum_budget_amount] IS NULL OR [minimum_budget_amount] >= 0) AND ([recommended_min_amount] IS NULL OR [recommended_min_amount] >= 0) AND ([recommended_max_amount] IS NULL OR [recommended_max_amount] >= 0) AND ([unit_price_amount] IS NULL OR [unit_price_amount] >= 0) AND ([minimum_charge_amount] IS NULL OR [minimum_charge_amount] >= 0)");
                    table.CheckConstraint("CK_category_price_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_category_price_policies_range", "[recommended_min_amount] IS NULL OR [recommended_max_amount] IS NULL OR [recommended_min_amount] <= [recommended_max_amount]");
                    table.ForeignKey(
                        name: "FK_category_price_policies_category_policies_legacy_category_policy_id",
                        column: x => x.legacy_category_policy_id,
                        principalTable: "category_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policies_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_price_policy_options",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    option_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    additional_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_price_policy_options", x => x.id);
                    table.CheckConstraint("CK_category_price_policy_options_order", "[display_order] >= 0");
                    table.ForeignKey(
                        name: "FK_category_price_policy_options_category_price_policies_price_policy_id",
                        column: x => x.price_policy_id,
                        principalTable: "category_price_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policy_options_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policy_options_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_price_policy_surcharges",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    surcharge_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    calculation_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    rate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_price_policy_surcharges", x => x.id);
                    table.CheckConstraint("CK_category_price_policy_surcharges_order", "[display_order] >= 0");
                    table.CheckConstraint("CK_category_price_policy_surcharges_value", "(([amount] IS NOT NULL AND [amount] >= 0 AND [rate] IS NULL) OR ([amount] IS NULL AND [rate] IS NOT NULL AND [rate] >= 0 AND [rate] <= 1))");
                    table.ForeignKey(
                        name: "FK_category_price_policy_surcharges_category_price_policies_price_policy_id",
                        column: x => x.price_policy_id,
                        principalTable: "category_price_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policy_surcharges_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_price_policy_surcharges_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT category_id, policy_version FROM category_policies GROUP BY category_id, policy_version HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate legacy category policy version prevents policy normalization.', 1;
                IF EXISTS (SELECT 1 FROM category_policies p LEFT JOIN service_categories c ON c.id = p.category_id WHERE c.id IS NULL)
                    THROW 51000, 'Invalid legacy category foreign key prevents policy normalization.', 1;
                IF EXISTS (SELECT 1 FROM category_policies p LEFT JOIN fee_policies f ON f.id = p.fee_policy_id WHERE f.id IS NULL)
                    THROW 51000, 'Invalid legacy fee policy foreign key prevents policy normalization.', 1;
                IF EXISTS (SELECT 1 FROM category_policies p JOIN fee_policies f ON f.id = p.fee_policy_id WHERE p.estimated_quote_fee_amount <> ISNULL(f.display_fee_amount, 0))
                    THROW 51000, 'Legacy estimated fee differs from source fee display amount.', 1;
                IF EXISTS (SELECT 1 FROM category_policies WHERE effective_to IS NOT NULL AND effective_to <= effective_from)
                    THROW 51000, 'Invalid legacy policy period prevents policy normalization.', 1;
                IF EXISTS (SELECT 1 FROM category_policies WHERE base_price_amount < 0 OR minimum_budget_amount < 0 OR max_quote_count <= 0 OR quote_validity_minutes <= 0 OR provider_response_deadline_minutes <= 0 OR required_completion_photo_count < 0 OR default_warranty_days < 0)
                    THROW 51000, 'Invalid legacy policy values prevent policy normalization.', 1;
                IF EXISTS (SELECT 1 FROM category_price_policies) OR EXISTS (SELECT 1 FROM category_fee_policies) OR EXISTS (SELECT 1 FROM category_operation_policies)
                    THROW 51000, 'Policy normalization target tables must be empty.', 1;

                INSERT INTO category_price_policies
                    (public_id, category_id, legacy_category_policy_id, policy_version, legacy_price_method_text, price_type_code,
                     base_amount, minimum_budget_amount, recommended_min_amount, recommended_max_amount, unit_text, unit_price_amount,
                     minimum_charge_amount, currency_code, legacy_vat_display_rule_text, vat_policy_code, effective_from, effective_to,
                     is_active, created_at, created_by_user_id, updated_at, updated_by_user_id)
                SELECT NEWID(), p.category_id, p.id, p.policy_version, p.price_method_text, NULL,
                       p.base_price_amount, p.minimum_budget_amount, NULL, NULL, p.standard_work_unit_text, NULL,
                       NULL, p.currency_code, p.vat_display_rule_text, NULL, p.effective_from, p.effective_to,
                       1, p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
                FROM category_policies p;

                INSERT INTO category_fee_policies
                    (public_id, category_id, legacy_category_policy_id, source_fee_policy_id, policy_version, policy_kind_code,
                     transaction_type_code, calculation_method_text, fee_amount, min_base_amount, max_base_amount, rate,
                     monthly_amount, per_visit_amount, currency_code, charge_timing_text, restore_rule_text, effective_from,
                     effective_to, is_active, created_at, created_by_user_id, updated_at, updated_by_user_id)
                SELECT NEWID(), p.category_id, p.id, f.id, p.policy_version, f.policy_kind_code,
                       f.transaction_type_code, f.calculation_method_text, p.estimated_quote_fee_amount, f.min_base_amount,
                       f.max_base_amount, f.rate, f.monthly_amount, f.per_visit_amount, f.currency_code,
                       p.fee_charge_timing_text, p.fee_restore_condition_text, p.effective_from, p.effective_to,
                       f.is_active, p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
                FROM category_policies p
                INNER JOIN fee_policies f ON f.id = p.fee_policy_id;

                INSERT INTO category_operation_policies
                    (public_id, category_id, legacy_category_policy_id, policy_version, request_method_text,
                     onsite_requirement_text, is_emergency_allowed, subscription_option_text, max_quote_count,
                     quote_validity_minutes, matching_area_rule_text, notification_target_rule_text,
                     provider_response_deadline_minutes, request_field_summary_text, required_completion_photo_count,
                     required_qualification_summary_text, insurance_requirement_text, safety_grade_code,
                     completion_evidence_rule_text, default_warranty_days, trust_score_display_text, default_sort_code,
                     service_area_level_code, reference_url, admin_note, effective_from, effective_to, is_active,
                     created_at, created_by_user_id, updated_at, updated_by_user_id)
                SELECT NEWID(), p.category_id, p.id, p.policy_version, p.request_method_text,
                       p.onsite_requirement_text, p.is_emergency_allowed, p.subscription_option_text, p.max_quote_count,
                       p.quote_validity_minutes, p.matching_area_rule_text, p.notification_target_rule_text,
                       p.provider_response_deadline_minutes, p.request_field_summary_text, p.required_completion_photo_count,
                       p.required_qualification_summary_text, p.insurance_requirement_text, p.safety_grade_code,
                       p.completion_evidence_rule_text, p.default_warranty_days, p.trust_score_display_text, p.default_sort_code,
                       p.service_area_level_code, p.reference_url, p.admin_note, p.effective_from, p.effective_to, 1,
                       p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
                FROM category_policies p;

                DECLARE @legacy_count bigint = (SELECT COUNT_BIG(*) FROM category_policies);
                IF (SELECT COUNT_BIG(*) FROM category_price_policies) <> @legacy_count
                    THROW 51000, 'Price policy row count validation failed.', 1;
                IF (SELECT COUNT_BIG(*) FROM category_fee_policies) <> @legacy_count
                    THROW 51000, 'Fee policy row count validation failed.', 1;
                IF (SELECT COUNT_BIG(*) FROM category_operation_policies) <> @legacy_count
                    THROW 51000, 'Operation policy row count validation failed.', 1;

                IF EXISTS (
                    SELECT id, category_id, policy_version, price_method_text, base_price_amount, minimum_budget_amount,
                           standard_work_unit_text, currency_code, vat_display_rule_text, effective_from, effective_to
                    FROM category_policies
                    EXCEPT
                    SELECT legacy_category_policy_id, category_id, policy_version, legacy_price_method_text, base_amount,
                           minimum_budget_amount, unit_text, currency_code, legacy_vat_display_rule_text, effective_from, effective_to
                    FROM category_price_policies)
                    THROW 51000, 'Price policy value validation failed.', 1;
                IF EXISTS (SELECT 1 FROM category_price_policies WHERE price_type_code IS NOT NULL OR recommended_min_amount IS NOT NULL OR recommended_max_amount IS NOT NULL OR unit_price_amount IS NOT NULL OR minimum_charge_amount IS NOT NULL OR vat_policy_code IS NOT NULL)
                    THROW 51000, 'Unapproved inferred price values were created.', 1;

                IF EXISTS (
                    SELECT p.id, p.category_id, p.policy_version, f.id, f.policy_kind_code, f.transaction_type_code,
                           f.calculation_method_text, p.estimated_quote_fee_amount, f.min_base_amount, f.max_base_amount,
                           f.rate, f.monthly_amount, f.per_visit_amount, f.currency_code, p.fee_charge_timing_text,
                           p.fee_restore_condition_text, p.effective_from, p.effective_to, f.is_active
                    FROM category_policies p INNER JOIN fee_policies f ON f.id = p.fee_policy_id
                    EXCEPT
                    SELECT legacy_category_policy_id, category_id, policy_version, source_fee_policy_id, policy_kind_code,
                           transaction_type_code, calculation_method_text, fee_amount, min_base_amount, max_base_amount,
                           rate, monthly_amount, per_visit_amount, currency_code, charge_timing_text, restore_rule_text,
                           effective_from, effective_to, is_active
                    FROM category_fee_policies)
                    THROW 51000, 'Fee policy value validation failed.', 1;

                IF EXISTS (
                    SELECT p.id, p.category_id, p.policy_version, p.request_method_text, p.onsite_requirement_text,
                           p.is_emergency_allowed, p.subscription_option_text, p.max_quote_count, p.quote_validity_minutes,
                           p.matching_area_rule_text, p.notification_target_rule_text, p.provider_response_deadline_minutes,
                           p.request_field_summary_text, p.required_completion_photo_count, p.required_qualification_summary_text,
                           p.insurance_requirement_text, p.safety_grade_code, p.completion_evidence_rule_text,
                           p.default_warranty_days, p.trust_score_display_text, p.default_sort_code,
                           p.service_area_level_code, p.reference_url, p.admin_note, p.effective_from, p.effective_to
                    FROM category_policies p
                    EXCEPT
                    SELECT legacy_category_policy_id, category_id, policy_version, request_method_text,
                           onsite_requirement_text, is_emergency_allowed, subscription_option_text, max_quote_count,
                           quote_validity_minutes, matching_area_rule_text, notification_target_rule_text,
                           provider_response_deadline_minutes, request_field_summary_text, required_completion_photo_count,
                           required_qualification_summary_text, insurance_requirement_text, safety_grade_code,
                           completion_evidence_rule_text, default_warranty_days, trust_score_display_text, default_sort_code,
                           service_area_level_code, reference_url, admin_note, effective_from, effective_to
                    FROM category_operation_policies)
                    THROW 51000, 'Operation policy value validation failed.', 1;
                IF EXISTS (SELECT 1 FROM category_price_policy_options) OR EXISTS (SELECT 1 FROM category_price_policy_surcharges)
                    THROW 51000, 'Price option or surcharge data must not be inferred.', 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_category_id_is_active_effective_from_effective_to",
                table: "category_fee_policies",
                columns: new[] { "category_id", "is_active", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_category_id_policy_version",
                table: "category_fee_policies",
                columns: new[] { "category_id", "policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_created_by_user_id",
                table: "category_fee_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_legacy_category_policy_id",
                table: "category_fee_policies",
                column: "legacy_category_policy_id",
                unique: true,
                filter: "[legacy_category_policy_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_public_id",
                table: "category_fee_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_source_fee_policy_id",
                table: "category_fee_policies",
                column: "source_fee_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_fee_policies_updated_by_user_id",
                table: "category_fee_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_category_id_is_active_effective_from_effective_to",
                table: "category_operation_policies",
                columns: new[] { "category_id", "is_active", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_category_id_policy_version",
                table: "category_operation_policies",
                columns: new[] { "category_id", "policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_created_by_user_id",
                table: "category_operation_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_legacy_category_policy_id",
                table: "category_operation_policies",
                column: "legacy_category_policy_id",
                unique: true,
                filter: "[legacy_category_policy_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_public_id",
                table: "category_operation_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_operation_policies_updated_by_user_id",
                table: "category_operation_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_category_id_is_active_effective_from_effective_to",
                table: "category_price_policies",
                columns: new[] { "category_id", "is_active", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_category_id_policy_version",
                table: "category_price_policies",
                columns: new[] { "category_id", "policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_created_by_user_id",
                table: "category_price_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_legacy_category_policy_id",
                table: "category_price_policies",
                column: "legacy_category_policy_id",
                unique: true,
                filter: "[legacy_category_policy_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_public_id",
                table: "category_price_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policies_updated_by_user_id",
                table: "category_price_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_options_created_by_user_id",
                table: "category_price_policy_options",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_options_price_policy_id_is_active_display_order",
                table: "category_price_policy_options",
                columns: new[] { "price_policy_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_options_public_id",
                table: "category_price_policy_options",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_options_updated_by_user_id",
                table: "category_price_policy_options",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_surcharges_created_by_user_id",
                table: "category_price_policy_surcharges",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_surcharges_price_policy_id_is_active_display_order",
                table: "category_price_policy_surcharges",
                columns: new[] { "price_policy_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_surcharges_public_id",
                table: "category_price_policy_surcharges",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_price_policy_surcharges_updated_by_user_id",
                table: "category_price_policy_surcharges",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_fee_policies");

            migrationBuilder.DropTable(
                name: "category_operation_policies");

            migrationBuilder.DropTable(
                name: "category_price_policy_options");

            migrationBuilder.DropTable(
                name: "category_price_policy_surcharges");

            migrationBuilder.DropTable(
                name: "category_price_policies");
        }
    }
}
