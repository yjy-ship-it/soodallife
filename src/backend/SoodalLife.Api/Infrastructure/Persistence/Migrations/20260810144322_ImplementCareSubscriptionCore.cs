using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCareSubscriptionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reviews_transaction_id",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_reviews_verification",
                table: "reviews");

            migrationBuilder.AddColumn<long>(
                name: "subscription_visit_schedule_id",
                table: "service_history_entries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "reviews",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "subscription_visit_schedule_id",
                table: "reviews",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "subscription_visit_schedule_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "subscription_visit_schedule_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "care_products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    product_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    service_scope_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    visits_per_period = table.Column<int>(type: "int", nullable: false),
                    expected_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    billing_period_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    standard_monthly_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    standard_visit_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_care_products", x => x.id);
                    table.CheckConstraint("CK_care_products_amount", "[standard_monthly_amount] IS NULL OR [standard_monthly_amount] >= 0");
                    table.CheckConstraint("CK_care_products_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_care_products_visits", "[visits_per_period] > 0 AND [expected_duration_minutes] > 0");
                    table.ForeignKey(
                        name: "FK_care_products_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_care_products_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_care_products_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_applications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    proposed_scope_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    proposed_monthly_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    proposed_visit_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    available_schedule_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_applications", x => x.id);
                    table.CheckConstraint("CK_subscription_applications_amount", "[proposed_monthly_amount] IS NULL OR [proposed_monthly_amount] >= 0");
                    table.CheckConstraint("CK_subscription_applications_status", "[status_code] IN ('SUBMITTED','SELECTED','NOT_SELECTED','WITHDRAWN')");
                    table.ForeignKey(
                        name: "FK_subscription_applications_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_applications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_applications_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    care_product_id = table.Column<long>(type: "bigint", nullable: true),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    request_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    requested_scope_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    preferred_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    detail_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    selected_application_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_requests", x => x.id);
                    table.CheckConstraint("CK_subscription_requests_product", "([request_type_code] = 'STANDARD' AND [care_product_id] IS NOT NULL) OR [request_type_code] = 'CUSTOM'");
                    table.CheckConstraint("CK_subscription_requests_status", "[status_code] IN ('OPEN','SELECTED','CONTRACTED','CANCELLED','CLOSED')");
                    table.CheckConstraint("CK_subscription_requests_type", "[request_type_code] IN ('STANDARD','CUSTOM')");
                    table.ForeignKey(
                        name: "FK_subscription_requests_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_care_products_care_product_id",
                        column: x => x.care_product_id,
                        principalTable: "care_products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_subscription_applications_selected_application_id",
                        column: x => x.selected_application_id,
                        principalTable: "subscription_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_contracts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    care_product_id = table.Column<long>(type: "bigint", nullable: true),
                    subscription_application_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    pause_started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    resume_planned_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    termination_requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    terminated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    termination_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    price_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fee_policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    service_scope_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    recurrence_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    provider_trust_score_snapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_contracts", x => x.id);
                    table.CheckConstraint("CK_subscription_contracts_status", "[status_code] IN ('ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED')");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_care_products_care_product_id",
                        column: x => x.care_product_id,
                        principalTable: "care_products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_subscription_applications_subscription_application_id",
                        column: x => x.subscription_application_id,
                        principalTable: "subscription_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_subscription_requests_subscription_request_id",
                        column: x => x.subscription_request_id,
                        principalTable: "subscription_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_contracts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_recurrence_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: false),
                    frequency_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    interval_value = table.Column<int>(type: "int", nullable: false),
                    visits_per_period = table.Column<int>(type: "int", nullable: true),
                    weekdays_json = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    preferred_time_from = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    preferred_time_to = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    expected_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    additional_rule_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_recurrence_rules", x => x.id);
                    table.CheckConstraint("CK_subscription_recurrence_frequency", "[frequency_type_code] IN ('WEEKLY','BIWEEKLY','MONTHLY','QUARTERLY','HALF_YEARLY')");
                    table.CheckConstraint("CK_subscription_recurrence_period", "[end_date] IS NULL OR [end_date] >= [start_date]");
                    table.CheckConstraint("CK_subscription_recurrence_values", "[interval_value] > 0 AND [expected_duration_minutes] > 0");
                    table.ForeignKey(
                        name: "FK_subscription_recurrence_rules_subscription_requests_subscription_request_id",
                        column: x => x.subscription_request_id,
                        principalTable: "subscription_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_recurrence_rules_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_recurrence_rules_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_visit_schedules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    visit_no = table.Column<int>(type: "int", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    scheduled_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    visit_verified_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    visit_verification_method_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    visit_verification_result_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    work_started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    work_completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    provider_completion_submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    customer_confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completion_checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completion_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    settlement_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_visit_schedules", x => x.id);
                    table.CheckConstraint("CK_subscription_visits_number", "[visit_no] > 0");
                    table.CheckConstraint("CK_subscription_visits_settlement", "[settlement_status_code] IN ('NOT_READY','READY','HOLD','SETTLED')");
                    table.CheckConstraint("CK_subscription_visits_status", "[status_code] IN ('SCHEDULED','RESCHEDULED','SKIPPED','PAUSED','IN_PROGRESS','PROVIDER_COMPLETED','COMPLETED','CANCELLED','DISPUTED')");
                    table.ForeignKey(
                        name: "FK_subscription_visit_schedules_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_visit_schedules_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_visit_schedules_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_visit_schedules_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: true),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: true),
                    subscription_visit_schedule_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    event_data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_events", x => x.id);
                    table.CheckConstraint("CK_subscription_events_parent", "[subscription_request_id] IS NOT NULL OR [subscription_contract_id] IS NOT NULL OR [subscription_visit_schedule_id] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_subscription_events_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_events_subscription_requests_subscription_request_id",
                        column: x => x.subscription_request_id,
                        principalTable: "subscription_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_events_subscription_visit_schedules_subscription_visit_schedule_id",
                        column: x => x.subscription_visit_schedule_id,
                        principalTable: "subscription_visit_schedules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_schedule_changes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_visit_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    old_schedule_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    new_schedule_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_schedule_changes", x => x.id);
                    table.CheckConstraint("CK_subscription_schedule_changes_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_subscription_schedule_changes_subscription_visit_schedules_subscription_visit_schedule_id",
                        column: x => x.subscription_visit_schedule_id,
                        principalTable: "subscription_visit_schedules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_schedule_changes_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_schedule_changes_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_visit_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subscription_visit_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_visit_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_visit_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_visit_files_subscription_visit_schedules_subscription_visit_schedule_id",
                        column: x => x.subscription_visit_schedule_id,
                        principalTable: "subscription_visit_schedules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_visit_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.Sql("EXEC(N'CREATE UNIQUE INDEX [IX_service_history_entries_subscription_visit_schedule_id] ON [dbo].[service_history_entries] ([subscription_visit_schedule_id]) WHERE [subscription_visit_schedule_id] IS NOT NULL')");

            migrationBuilder.Sql("EXEC(N'CREATE UNIQUE INDEX [IX_reviews_subscription_visit_schedule_id] ON [dbo].[reviews] ([subscription_visit_schedule_id]) WHERE [subscription_visit_schedule_id] IS NOT NULL')");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_transaction_id",
                table: "reviews",
                column: "transaction_id",
                unique: true,
                filter: "[transaction_id] IS NOT NULL");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[reviews] ADD CONSTRAINT [CK_reviews_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_reviews_verification",
                table: "reviews",
                sql: "[verification_status_code] IN ('VERIFIED_TRANSACTION','VERIFIED_SUBSCRIPTION_VISIT')");

            migrationBuilder.Sql("EXEC(N'CREATE INDEX [IX_dispute_cases_subscription_visit_schedule_id] ON [dbo].[dispute_cases] ([subscription_visit_schedule_id])')");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[dispute_cases] ADD CONSTRAINT [CK_dispute_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')");

            migrationBuilder.Sql("EXEC(N'CREATE INDEX [IX_after_service_cases_subscription_visit_schedule_id] ON [dbo].[after_service_cases] ([subscription_visit_schedule_id])')");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[after_service_cases] ADD CONSTRAINT [CK_after_service_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')");

            migrationBuilder.CreateIndex(
                name: "IX_care_products_created_by_user_id",
                table: "care_products",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_care_products_product_name_effective_from",
                table: "care_products",
                columns: new[] { "product_name", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_care_products_public_id",
                table: "care_products",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_care_products_service_category_id_is_active",
                table: "care_products",
                columns: new[] { "service_category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_care_products_updated_by_user_id",
                table: "care_products",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_created_by_user_id",
                table: "subscription_applications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_idempotency_key",
                table: "subscription_applications",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_provider_profile_id",
                table: "subscription_applications",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_public_id",
                table: "subscription_applications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_subscription_request_id_provider_profile_id",
                table: "subscription_applications",
                columns: new[] { "subscription_request_id", "provider_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_subscription_request_id_status_code",
                table: "subscription_applications",
                columns: new[] { "subscription_request_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_applications_updated_by_user_id",
                table: "subscription_applications",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_care_product_id",
                table: "subscription_contracts",
                column: "care_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_created_by_user_id",
                table: "subscription_contracts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_customer_profile_id_status_code",
                table: "subscription_contracts",
                columns: new[] { "customer_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_provider_profile_id_status_code",
                table: "subscription_contracts",
                columns: new[] { "provider_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_public_id",
                table: "subscription_contracts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_service_category_id",
                table: "subscription_contracts",
                column: "service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_subscription_application_id",
                table: "subscription_contracts",
                column: "subscription_application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_subscription_request_id",
                table: "subscription_contracts",
                column: "subscription_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_updated_by_user_id",
                table: "subscription_contracts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_actor_user_id",
                table: "subscription_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_idempotency_key",
                table: "subscription_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_public_id",
                table: "subscription_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_subscription_contract_id_occurred_at",
                table: "subscription_events",
                columns: new[] { "subscription_contract_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_subscription_request_id",
                table: "subscription_events",
                column: "subscription_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_events_subscription_visit_schedule_id_occurred_at",
                table: "subscription_events",
                columns: new[] { "subscription_visit_schedule_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_recurrence_rules_created_by_user_id",
                table: "subscription_recurrence_rules",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_recurrence_rules_public_id",
                table: "subscription_recurrence_rules",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_recurrence_rules_subscription_request_id",
                table: "subscription_recurrence_rules",
                column: "subscription_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_recurrence_rules_updated_by_user_id",
                table: "subscription_recurrence_rules",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_administrative_area_id",
                table: "subscription_requests",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_care_product_id",
                table: "subscription_requests",
                column: "care_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_created_by_user_id",
                table: "subscription_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_customer_profile_id_status_code",
                table: "subscription_requests",
                columns: new[] { "customer_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_public_id",
                table: "subscription_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_selected_application_id",
                table: "subscription_requests",
                column: "selected_application_id",
                unique: true,
                filter: "[selected_application_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_service_category_id_administrative_area_id_status_code",
                table: "subscription_requests",
                columns: new[] { "service_category_id", "administrative_area_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_updated_by_user_id",
                table: "subscription_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_schedule_changes_decided_by_user_id",
                table: "subscription_schedule_changes",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_schedule_changes_idempotency_key",
                table: "subscription_schedule_changes",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_schedule_changes_public_id",
                table: "subscription_schedule_changes",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_schedule_changes_requested_by_user_id",
                table: "subscription_schedule_changes",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_schedule_changes_subscription_visit_schedule_id_status_code",
                table: "subscription_schedule_changes",
                columns: new[] { "subscription_visit_schedule_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_files_created_by_user_id",
                table: "subscription_visit_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_files_file_id",
                table: "subscription_visit_files",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_files_subscription_visit_schedule_id_file_id",
                table: "subscription_visit_files",
                columns: new[] { "subscription_visit_schedule_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_created_by_user_id",
                table: "subscription_visit_schedules",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_provider_profile_id_scheduled_start_at",
                table: "subscription_visit_schedules",
                columns: new[] { "provider_profile_id", "scheduled_start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_public_id",
                table: "subscription_visit_schedules",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_status_code_scheduled_start_at",
                table: "subscription_visit_schedules",
                columns: new[] { "status_code", "scheduled_start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_subscription_contract_id_visit_no",
                table: "subscription_visit_schedules",
                columns: new[] { "subscription_contract_id", "visit_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_updated_by_user_id",
                table: "subscription_visit_schedules",
                column: "updated_by_user_id");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[after_service_cases] ADD CONSTRAINT [FK_after_service_cases_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[dispute_cases] ADD CONSTRAINT [FK_dispute_cases_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[reviews] ADD CONSTRAINT [FK_reviews_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')");

            migrationBuilder.Sql("EXEC(N'ALTER TABLE [dbo].[service_history_entries] ADD CONSTRAINT [FK_service_history_entries_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')");

            migrationBuilder.AddForeignKey(
                name: "FK_subscription_applications_subscription_requests_subscription_request_id",
                table: "subscription_applications",
                column: "subscription_request_id",
                principalTable: "subscription_requests",
                principalColumn: "id");

            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_subscription_events_append_only]
                ON [dbo].[subscription_events]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51003, N''구독 변경이력은 수정하거나 삭제할 수 없습니다.'', 1;
                END;')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_subscription_events_append_only];");

            migrationBuilder.DropForeignKey(
                name: "FK_after_service_cases_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "after_service_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_dispute_cases_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "dispute_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_service_history_entries_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "service_history_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_subscription_applications_subscription_requests_subscription_request_id",
                table: "subscription_applications");

            migrationBuilder.DropTable(
                name: "subscription_events");

            migrationBuilder.DropTable(
                name: "subscription_recurrence_rules");

            migrationBuilder.DropTable(
                name: "subscription_schedule_changes");

            migrationBuilder.DropTable(
                name: "subscription_visit_files");

            migrationBuilder.DropTable(
                name: "subscription_visit_schedules");

            migrationBuilder.DropTable(
                name: "subscription_contracts");

            migrationBuilder.DropTable(
                name: "subscription_requests");

            migrationBuilder.DropTable(
                name: "care_products");

            migrationBuilder.DropTable(
                name: "subscription_applications");

            migrationBuilder.DropIndex(
                name: "IX_service_history_entries_subscription_visit_schedule_id",
                table: "service_history_entries");

            migrationBuilder.DropIndex(
                name: "IX_reviews_subscription_visit_schedule_id",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_transaction_id",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_reviews_source",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_reviews_verification",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_dispute_cases_subscription_visit_schedule_id",
                table: "dispute_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_dispute_cases_source",
                table: "dispute_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_subscription_visit_schedule_id",
                table: "after_service_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_cases_source",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "subscription_visit_schedule_id",
                table: "service_history_entries");

            migrationBuilder.DropColumn(
                name: "subscription_visit_schedule_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "subscription_visit_schedule_id",
                table: "dispute_cases");

            migrationBuilder.DropColumn(
                name: "subscription_visit_schedule_id",
                table: "after_service_cases");

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "reviews",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "transaction_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_transaction_id",
                table: "reviews",
                column: "transaction_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_reviews_verification",
                table: "reviews",
                sql: "[verification_status_code] = 'VERIFIED_TRANSACTION'");
        }
    }
}
