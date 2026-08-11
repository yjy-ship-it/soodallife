using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementInteriorProjectCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "interior_project_id",
                table: "work_completions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "interior_project_id",
                table: "service_history_entries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revision_purpose_code",
                table: "quote_revisions",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_category_code",
                table: "quote_items",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "labor_note_text",
                table: "quote_items",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "material_spec_text",
                table: "quote_items",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "space_text",
                table: "quote_items",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_trade_text",
                table: "quote_items",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "interior_contract_change_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "interior_project_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "interior_work_stage_id",
                table: "dispute_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "interior_contract_changes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    change_no = table.Column<int>(type: "int", nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    change_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    reason_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    scope_change_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    amount_delta = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    schedule_impact_days = table.Column<int>(type: "int", nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    customer_decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_contract_changes", x => x.id);
                    table.CheckConstraint("CK_interior_contract_changes_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_interior_contract_changes_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contract_changes_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contract_changes_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_contract_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    interior_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    source_contract_change_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    scope_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    schedule_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    payment_plan_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warranty_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_agreed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    provider_agreed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_contract_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_contract_versions_interior_contract_changes_source_contract_change_id",
                        column: x => x.source_contract_change_id,
                        principalTable: "interior_contract_changes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contract_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_contracts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    selected_quote_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    scope_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quote_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    schedule_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warranty_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    provider_trust_score_snapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_completion_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_agreed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    provider_agreed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    effective_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    terminated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    termination_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_contracts", x => x.id);
                    table.CheckConstraint("CK_interior_contracts_amount", "[contract_amount] >= 0");
                    table.CheckConstraint("CK_interior_contracts_period", "[planned_completion_date] >= [planned_start_date]");
                    table.ForeignKey(
                        name: "FK_interior_contracts_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contracts_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contracts_quote_revisions_selected_quote_revision_id",
                        column: x => x.selected_quote_revision_id,
                        principalTable: "quote_revisions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contracts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contracts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_payment_plans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_no = table.Column<int>(type: "int", nullable: false),
                    payment_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    planned_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    planned_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    condition_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_payment_plans", x => x.id);
                    table.CheckConstraint("CK_interior_payment_plans_amount", "[planned_amount] >= 0");
                    table.ForeignKey(
                        name: "FK_interior_payment_plans_interior_contracts_interior_contract_id",
                        column: x => x.interior_contract_id,
                        principalTable: "interior_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_payment_plans_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_payment_plans_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_projects",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    selected_site_visit_provider_id = table.Column<long>(type: "bigint", nullable: true),
                    selected_contractor_provider_id = table.Column<long>(type: "bigint", nullable: true),
                    site_visit_selected_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    contractor_selected_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    current_quote_revision_id = table.Column<long>(type: "bigint", nullable: true),
                    current_contract_id = table.Column<long>(type: "bigint", nullable: true),
                    site_visit_provider_trust_score_snapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    contractor_trust_score_snapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    fee_assessment_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "POLICY_PENDING"),
                    project_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expected_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_projects", x => x.id);
                    table.CheckConstraint("CK_interior_projects_fee", "[fee_assessment_status_code] IN ('POLICY_PENDING','NOT_APPLICABLE','ASSESSED')");
                    table.CheckConstraint("CK_interior_projects_status", "[status_code] IN ('CONSULTATION','SITE_VISIT_SELECTION','SITE_VISIT_SCHEDULED','SITE_VISIT_COMPLETED','ESTIMATE_IN_PROGRESS','ESTIMATE_READY','CONTRACT_PENDING','CONTRACTED','CONSTRUCTION','INSPECTION','COMPLETED','DEFECT_MANAGEMENT','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_interior_projects_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_interior_contracts_current_contract_id",
                        column: x => x.current_contract_id,
                        principalTable: "interior_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_provider_profiles_selected_contractor_provider_id",
                        column: x => x.selected_contractor_provider_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_provider_profiles_selected_site_visit_provider_id",
                        column: x => x.selected_site_visit_provider_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_quote_revisions_current_quote_revision_id",
                        column: x => x.current_quote_revision_id,
                        principalTable: "quote_revisions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_projects_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_payment_confirmations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    confirmed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    confirmation_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    confirmed_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    evidence_file_id = table.Column<long>(type: "bigint", nullable: true),
                    note_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_payment_confirmations", x => x.id);
                    table.CheckConstraint("CK_interior_payment_confirmations_amount", "[confirmed_amount] >= 0");
                    table.ForeignKey(
                        name: "FK_interior_payment_confirmations_files_evidence_file_id",
                        column: x => x.evidence_file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_payment_confirmations_interior_payment_plans_payment_plan_id",
                        column: x => x.payment_plan_id,
                        principalTable: "interior_payment_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_payment_confirmations_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_design_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    customer_approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_design_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_design_versions_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_design_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_design_versions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_project_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    event_data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_project_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_project_events_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_project_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_site_visits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    proposed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    scheduled_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    scheduled_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    access_condition_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    visited_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    measurement_summary_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    constraint_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    risk_note_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    customer_confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_site_visits", x => x.id);
                    table.CheckConstraint("CK_interior_site_visits_status", "[status_code] IN ('PROPOSED','CONFIRMED','COMPLETED','CANCELLED','NO_SHOW')");
                    table.ForeignKey(
                        name: "FK_interior_site_visits_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visits_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visits_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visits_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_work_stages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_no = table.Column<int>(type: "int", nullable: false),
                    stage_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    actual_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    actual_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    provider_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_work_stages", x => x.id);
                    table.CheckConstraint("CK_interior_work_stages_period", "[planned_end_date] >= [planned_start_date]");
                    table.CheckConstraint("CK_interior_work_stages_progress", "[progress_percent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_interior_work_stages_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_stages_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_stages_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_design_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    design_version_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_design_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_design_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_design_files_interior_design_versions_design_version_id",
                        column: x => x.design_version_id,
                        principalTable: "interior_design_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_design_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_site_visit_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    site_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_site_visit_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_site_visit_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visit_files_interior_site_visits_site_visit_id",
                        column: x => x.site_visit_id,
                        principalTable: "interior_site_visits",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visit_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_site_visit_measurements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    site_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    measurement_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    measurement_value = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    measurement_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    unit_text = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    location_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    note_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    additional_data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_site_visit_measurements", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_site_visit_measurements_interior_site_visits_site_visit_id",
                        column: x => x.site_visit_id,
                        principalTable: "interior_site_visits",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_site_visit_measurements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_defects",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: false),
                    work_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_version = table.Column<int>(type: "int", nullable: true),
                    defect_location_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    defect_description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_defects", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_defects_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_defects_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_defects_interior_work_stages_work_stage_id",
                        column: x => x.work_stage_id,
                        principalTable: "interior_work_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_defects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_stage_inspections",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    inspection_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    inspected_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    result_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    requested_correction_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    inspected_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_stage_inspections", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_stage_inspections_interior_work_stages_work_stage_id",
                        column: x => x.work_stage_id,
                        principalTable: "interior_work_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_stage_inspections_users_inspected_by_user_id",
                        column: x => x.inspected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_work_updates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    update_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    issue_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_work_updates", x => x.id);
                    table.CheckConstraint("CK_interior_work_updates_progress", "[progress_percent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_interior_work_updates_interior_work_stages_work_stage_id",
                        column: x => x.work_stage_id,
                        principalTable: "interior_work_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_updates_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_work_update_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    work_update_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_work_update_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_work_update_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_update_files_interior_work_updates_work_update_id",
                        column: x => x.work_update_id,
                        principalTable: "interior_work_updates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_update_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_interior_project_id",
                table: "work_completions",
                column: "interior_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_interior_project_id",
                table: "service_history_entries",
                column: "interior_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_interior_contract_change_id",
                table: "dispute_cases",
                column: "interior_contract_change_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_interior_project_id",
                table: "dispute_cases",
                column: "interior_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_interior_work_stage_id",
                table: "dispute_cases",
                column: "interior_work_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_changes_created_by_user_id",
                table: "interior_contract_changes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_changes_interior_contract_id_change_no",
                table: "interior_contract_changes",
                columns: new[] { "interior_contract_id", "change_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_changes_public_id",
                table: "interior_contract_changes",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_changes_requested_by_user_id",
                table: "interior_contract_changes",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_changes_updated_by_user_id",
                table: "interior_contract_changes",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_versions_created_by_user_id",
                table: "interior_contract_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_versions_interior_contract_id_version_no",
                table: "interior_contract_versions",
                columns: new[] { "interior_contract_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_versions_source_contract_change_id",
                table: "interior_contract_versions",
                column: "source_contract_change_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_created_by_user_id",
                table: "interior_contracts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_customer_profile_id",
                table: "interior_contracts",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_interior_project_id_contract_version",
                table: "interior_contracts",
                columns: new[] { "interior_project_id", "contract_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_provider_profile_id",
                table: "interior_contracts",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_public_id",
                table: "interior_contracts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_selected_quote_revision_id",
                table: "interior_contracts",
                column: "selected_quote_revision_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contracts_updated_by_user_id",
                table: "interior_contracts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_defects_after_service_case_id",
                table: "interior_defects",
                column: "after_service_case_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_defects_created_by_user_id",
                table: "interior_defects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_defects_interior_project_id",
                table: "interior_defects",
                column: "interior_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_defects_public_id",
                table: "interior_defects",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_defects_work_stage_id",
                table: "interior_defects",
                column: "work_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_files_created_by_user_id",
                table: "interior_design_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_files_design_version_id_file_id",
                table: "interior_design_files",
                columns: new[] { "design_version_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_files_file_id",
                table: "interior_design_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_versions_created_by_user_id",
                table: "interior_design_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_versions_interior_project_id_version_no",
                table: "interior_design_versions",
                columns: new[] { "interior_project_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_versions_public_id",
                table: "interior_design_versions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_design_versions_updated_by_user_id",
                table: "interior_design_versions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_confirmations_confirmed_by_user_id",
                table: "interior_payment_confirmations",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_confirmations_evidence_file_id",
                table: "interior_payment_confirmations",
                column: "evidence_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_confirmations_payment_plan_id_confirmation_type_code_confirmed_by_user_id",
                table: "interior_payment_confirmations",
                columns: new[] { "payment_plan_id", "confirmation_type_code", "confirmed_by_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_confirmations_public_id",
                table: "interior_payment_confirmations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_plans_created_by_user_id",
                table: "interior_payment_plans",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_plans_interior_contract_id_sequence_no",
                table: "interior_payment_plans",
                columns: new[] { "interior_contract_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_plans_public_id",
                table: "interior_payment_plans",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_payment_plans_updated_by_user_id",
                table: "interior_payment_plans",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_events_actor_user_id",
                table: "interior_project_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_events_idempotency_key",
                table: "interior_project_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_events_interior_project_id_occurred_at",
                table: "interior_project_events",
                columns: new[] { "interior_project_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_events_public_id",
                table: "interior_project_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_created_by_user_id",
                table: "interior_projects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_current_contract_id",
                table: "interior_projects",
                column: "current_contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_current_quote_revision_id",
                table: "interior_projects",
                column: "current_quote_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_customer_profile_id",
                table: "interior_projects",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_public_id",
                table: "interior_projects",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_selected_contractor_provider_id",
                table: "interior_projects",
                column: "selected_contractor_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_selected_site_visit_provider_id",
                table: "interior_projects",
                column: "selected_site_visit_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_service_category_id",
                table: "interior_projects",
                column: "service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_service_request_id",
                table: "interior_projects",
                column: "service_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_status_code_updated_at",
                table: "interior_projects",
                columns: new[] { "status_code", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_updated_by_user_id",
                table: "interior_projects",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visit_files_created_by_user_id",
                table: "interior_site_visit_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visit_files_file_id",
                table: "interior_site_visit_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visit_files_site_visit_id_file_id",
                table: "interior_site_visit_files",
                columns: new[] { "site_visit_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visit_measurements_created_by_user_id",
                table: "interior_site_visit_measurements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visit_measurements_site_visit_id_measurement_key",
                table: "interior_site_visit_measurements",
                columns: new[] { "site_visit_id", "measurement_key" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_created_by_user_id",
                table: "interior_site_visits",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_interior_project_id_provider_profile_id_scheduled_start_at",
                table: "interior_site_visits",
                columns: new[] { "interior_project_id", "provider_profile_id", "scheduled_start_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_provider_profile_id",
                table: "interior_site_visits",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_public_id",
                table: "interior_site_visits",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_status_code_scheduled_start_at",
                table: "interior_site_visits",
                columns: new[] { "status_code", "scheduled_start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_site_visits_updated_by_user_id",
                table: "interior_site_visits",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspections_inspected_by_user_id",
                table: "interior_stage_inspections",
                column: "inspected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspections_public_id",
                table: "interior_stage_inspections",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspections_work_stage_id_inspected_at",
                table: "interior_stage_inspections",
                columns: new[] { "work_stage_id", "inspected_at" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stages_created_by_user_id",
                table: "interior_work_stages",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stages_interior_project_id_sequence_no",
                table: "interior_work_stages",
                columns: new[] { "interior_project_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stages_public_id",
                table: "interior_work_stages",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stages_updated_by_user_id",
                table: "interior_work_stages",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_update_files_created_by_user_id",
                table: "interior_work_update_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_update_files_file_id",
                table: "interior_work_update_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_update_files_work_update_id_file_id",
                table: "interior_work_update_files",
                columns: new[] { "work_update_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_updates_created_by_user_id",
                table: "interior_work_updates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_updates_public_id",
                table: "interior_work_updates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_updates_work_stage_id_created_at",
                table: "interior_work_updates",
                columns: new[] { "work_stage_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_cases_interior_contract_changes_interior_contract_change_id",
                table: "dispute_cases",
                column: "interior_contract_change_id",
                principalTable: "interior_contract_changes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_cases_interior_projects_interior_project_id",
                table: "dispute_cases",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_cases_interior_work_stages_interior_work_stage_id",
                table: "dispute_cases",
                column: "interior_work_stage_id",
                principalTable: "interior_work_stages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_service_history_entries_interior_projects_interior_project_id",
                table: "service_history_entries",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_work_completions_interior_projects_interior_project_id",
                table: "work_completions",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interior_contract_changes_interior_contracts_interior_contract_id",
                table: "interior_contract_changes",
                column: "interior_contract_id",
                principalTable: "interior_contracts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interior_contract_versions_interior_contracts_interior_contract_id",
                table: "interior_contract_versions",
                column: "interior_contract_id",
                principalTable: "interior_contracts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interior_contracts_interior_projects_interior_project_id",
                table: "interior_contracts",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.Sql(
                """
                EXEC(N'CREATE TRIGGER [dbo].[TR_interior_project_events_append_only]
                ON [dbo].[interior_project_events]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''interior_project_events is append-only.'', 1;
                END')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_interior_project_events_append_only];");

            migrationBuilder.DropForeignKey(
                name: "FK_dispute_cases_interior_contract_changes_interior_contract_change_id",
                table: "dispute_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_dispute_cases_interior_projects_interior_project_id",
                table: "dispute_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_dispute_cases_interior_work_stages_interior_work_stage_id",
                table: "dispute_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_service_history_entries_interior_projects_interior_project_id",
                table: "service_history_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_work_completions_interior_projects_interior_project_id",
                table: "work_completions");

            migrationBuilder.DropForeignKey(
                name: "FK_interior_projects_interior_contracts_current_contract_id",
                table: "interior_projects");

            migrationBuilder.DropTable(
                name: "interior_contract_versions");

            migrationBuilder.DropTable(
                name: "interior_defects");

            migrationBuilder.DropTable(
                name: "interior_design_files");

            migrationBuilder.DropTable(
                name: "interior_payment_confirmations");

            migrationBuilder.DropTable(
                name: "interior_project_events");

            migrationBuilder.DropTable(
                name: "interior_site_visit_files");

            migrationBuilder.DropTable(
                name: "interior_site_visit_measurements");

            migrationBuilder.DropTable(
                name: "interior_stage_inspections");

            migrationBuilder.DropTable(
                name: "interior_work_update_files");

            migrationBuilder.DropTable(
                name: "interior_contract_changes");

            migrationBuilder.DropTable(
                name: "interior_design_versions");

            migrationBuilder.DropTable(
                name: "interior_payment_plans");

            migrationBuilder.DropTable(
                name: "interior_site_visits");

            migrationBuilder.DropTable(
                name: "interior_work_updates");

            migrationBuilder.DropTable(
                name: "interior_work_stages");

            migrationBuilder.DropTable(
                name: "interior_contracts");

            migrationBuilder.DropTable(
                name: "interior_projects");

            migrationBuilder.DropIndex(
                name: "IX_work_completions_interior_project_id",
                table: "work_completions");

            migrationBuilder.DropIndex(
                name: "IX_service_history_entries_interior_project_id",
                table: "service_history_entries");

            migrationBuilder.DropIndex(
                name: "IX_dispute_cases_interior_contract_change_id",
                table: "dispute_cases");

            migrationBuilder.DropIndex(
                name: "IX_dispute_cases_interior_project_id",
                table: "dispute_cases");

            migrationBuilder.DropIndex(
                name: "IX_dispute_cases_interior_work_stage_id",
                table: "dispute_cases");

            migrationBuilder.DropColumn(
                name: "interior_project_id",
                table: "work_completions");

            migrationBuilder.DropColumn(
                name: "interior_project_id",
                table: "service_history_entries");

            migrationBuilder.DropColumn(
                name: "revision_purpose_code",
                table: "quote_revisions");

            migrationBuilder.DropColumn(
                name: "item_category_code",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "labor_note_text",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "material_spec_text",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "space_text",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "work_trade_text",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "interior_contract_change_id",
                table: "dispute_cases");

            migrationBuilder.DropColumn(
                name: "interior_project_id",
                table: "dispute_cases");

            migrationBuilder.DropColumn(
                name: "interior_work_stage_id",
                table: "dispute_cases");
        }
    }
}
