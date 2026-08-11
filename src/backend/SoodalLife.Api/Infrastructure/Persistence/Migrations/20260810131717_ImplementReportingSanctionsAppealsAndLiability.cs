using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementReportingSanctionsAppealsAndLiability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.AddColumn<long>(
                name: "liability_type_id",
                table: "dispute_resolutions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dispute_liability_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_liability_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_dispute_liability_types_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_liability_types_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_types", x => x.id);
                    table.CheckConstraint("CK_report_types_period", "[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]");
                    table.ForeignKey(
                        name: "FK_report_types_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_types_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_types", x => x.id);
                    table.CheckConstraint("CK_sanction_types_period", "[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]");
                    table.ForeignKey(
                        name: "FK_sanction_types_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_types_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reporter_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reported_user_id = table.Column<long>(type: "bigint", nullable: true),
                    report_type_id = table.Column<long>(type: "bigint", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: true),
                    transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    review_id = table.Column<long>(type: "bigint", nullable: true),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: true),
                    dispute_case_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "RECEIVED"),
                    assigned_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    result_summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    received_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    resolved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                    table.CheckConstraint("CK_reports_status", "[status_code] IN ('RECEIVED','UNDER_REVIEW','EVIDENCE_REQUESTED','RESOLVED','CANCELLED')");
                    table.CheckConstraint("CK_reports_target", "[reported_user_id] IS NOT NULL OR [service_request_id] IS NOT NULL OR [transaction_id] IS NOT NULL OR [review_id] IS NOT NULL OR [after_service_case_id] IS NOT NULL OR [dispute_case_id] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_reports_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_dispute_cases_dispute_case_id",
                        column: x => x.dispute_case_id,
                        principalTable: "dispute_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_report_types_report_type_id",
                        column: x => x.report_type_id,
                        principalTable: "report_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_users_assigned_admin_user_id",
                        column: x => x.assigned_admin_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_users_reported_user_id",
                        column: x => x.reported_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_users_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reports_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanctions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    target_user_id = table.Column<long>(type: "bigint", nullable: false),
                    target_role_id = table.Column<long>(type: "bigint", nullable: true),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: true),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: true),
                    sanction_type_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "DECIDED"),
                    reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    released_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    released_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    release_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanctions", x => x.id);
                    table.CheckConstraint("CK_sanctions_period", "[end_at] IS NULL OR [end_at] > [start_at]");
                    table.CheckConstraint("CK_sanctions_scope", "[provider_service_category_id] IS NULL OR [provider_profile_id] IS NOT NULL");
                    table.CheckConstraint("CK_sanctions_status", "[status_code] IN ('DECIDED','ACTIVE','RELEASED','EXPIRED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_sanctions_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_roles_target_role_id",
                        column: x => x.target_role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_sanction_types_sanction_type_id",
                        column: x => x.sanction_type_id,
                        principalTable: "sanction_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_users_released_by_user_id",
                        column: x => x.released_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanctions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    report_id = table.Column<long>(type: "bigint", nullable: false),
                    action_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_actions", x => x.id);
                    table.CheckConstraint("CK_report_actions_type", "[action_type_code] IN ('CREATED','ASSIGNED','STATUS_CHANGED','EVIDENCE_REQUESTED','EVIDENCE_ADDED','RESOLVED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_report_actions_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_actions_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_evidence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    report_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    withdrawn_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    withdrawn_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    withdrawal_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_evidence", x => x.id);
                    table.CheckConstraint("CK_report_evidence_status", "[status_code] IN ('ACTIVE','WITHDRAWN')");
                    table.ForeignKey(
                        name: "FK_report_evidence_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_evidence_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_evidence_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_evidence_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_evidence_users_withdrawn_by_user_id",
                        column: x => x.withdrawn_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_appeals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sanction_id = table.Column<long>(type: "bigint", nullable: false),
                    applicant_user_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_appeal_id = table.Column<long>(type: "bigint", nullable: true),
                    statement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "RECEIVED"),
                    assigned_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decision_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    decision_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_appeals", x => x.id);
                    table.CheckConstraint("CK_sanction_appeals_decision", "[decision_code] IS NULL OR [decision_code] IN ('APPROVED','REJECTED')");
                    table.CheckConstraint("CK_sanction_appeals_status", "[status_code] IN ('RECEIVED','UNDER_REVIEW','APPROVED','REJECTED','CLOSED')");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_sanction_appeals_previous_appeal_id",
                        column: x => x.previous_appeal_id,
                        principalTable: "sanction_appeals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_sanctions_sanction_id",
                        column: x => x.sanction_id,
                        principalTable: "sanctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_users_applicant_user_id",
                        column: x => x.applicant_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_users_assigned_admin_user_id",
                        column: x => x.assigned_admin_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeals_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sanction_id = table.Column<long>(type: "bigint", nullable: false),
                    action_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_events", x => x.id);
                    table.CheckConstraint("CK_sanction_events_action", "[action_type_code] IN ('DECIDED','ACTIVATED','STATUS_CHANGED','RELEASED','EXPIRED','CANCELLED')");
                    table.CheckConstraint("CK_sanction_events_snapshot", "ISJSON([snapshot_json]) = 1");
                    table.ForeignKey(
                        name: "FK_sanction_events_sanctions_sanction_id",
                        column: x => x.sanction_id,
                        principalTable: "sanctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_sources",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sanction_id = table.Column<long>(type: "bigint", nullable: false),
                    report_id = table.Column<long>(type: "bigint", nullable: true),
                    dispute_case_id = table.Column<long>(type: "bigint", nullable: true),
                    review_id = table.Column<long>(type: "bigint", nullable: true),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_sources", x => x.id);
                    table.CheckConstraint("CK_sanction_sources_one_source", "(CASE WHEN [report_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [dispute_case_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [review_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [after_service_case_id] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_sanction_sources_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_sources_dispute_cases_dispute_case_id",
                        column: x => x.dispute_case_id,
                        principalTable: "dispute_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_sources_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_sources_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_sources_sanctions_sanction_id",
                        column: x => x.sanction_id,
                        principalTable: "sanctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_sources_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_appeal_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sanction_appeal_id = table.Column<long>(type: "bigint", nullable: false),
                    action_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_appeal_actions", x => x.id);
                    table.CheckConstraint("CK_sanction_appeal_actions_type", "[action_type_code] IN ('CREATED','ASSIGNED','REVIEW_STARTED','DECIDED','CLOSED','EVIDENCE_ADDED')");
                    table.ForeignKey(
                        name: "FK_sanction_appeal_actions_sanction_appeals_sanction_appeal_id",
                        column: x => x.sanction_appeal_id,
                        principalTable: "sanction_appeals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeal_actions_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sanction_appeal_evidence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sanction_appeal_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_appeal_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_sanction_appeal_evidence_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeal_evidence_sanction_appeals_sanction_appeal_id",
                        column: x => x.sanction_appeal_id,
                        principalTable: "sanction_appeals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeal_evidence_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sanction_appeal_evidence_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE')");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_liability_type_id",
                table: "dispute_resolutions",
                column: "liability_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_liability_types_code",
                table: "dispute_liability_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_liability_types_created_by_user_id",
                table: "dispute_liability_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_liability_types_is_active_display_order",
                table: "dispute_liability_types",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_dispute_liability_types_public_id",
                table: "dispute_liability_types",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_liability_types_updated_by_user_id",
                table: "dispute_liability_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_actions_actor_user_id",
                table: "report_actions",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_actions_idempotency_key",
                table: "report_actions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_actions_public_id",
                table: "report_actions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_actions_report_id_occurred_at",
                table: "report_actions",
                columns: new[] { "report_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_created_by_user_id",
                table: "report_evidence",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_file_id",
                table: "report_evidence",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_public_id",
                table: "report_evidence",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_report_id_file_id",
                table: "report_evidence",
                columns: new[] { "report_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_status_code",
                table: "report_evidence",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_submitted_by_user_id",
                table: "report_evidence",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_withdrawn_by_user_id",
                table: "report_evidence",
                column: "withdrawn_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_types_code",
                table: "report_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_types_created_by_user_id",
                table: "report_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_types_is_active_display_order",
                table: "report_types",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_report_types_public_id",
                table: "report_types",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_types_updated_by_user_id",
                table: "report_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_after_service_case_id",
                table: "reports",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_assigned_admin_user_id_status_code_received_at",
                table: "reports",
                columns: new[] { "assigned_admin_user_id", "status_code", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_created_by_user_id",
                table: "reports",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_dispute_case_id",
                table: "reports",
                column: "dispute_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_idempotency_key",
                table: "reports",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_public_id",
                table: "reports",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_report_type_id",
                table: "reports",
                column: "report_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_reported_user_id",
                table: "reports",
                column: "reported_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_reporter_user_id",
                table: "reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_review_id",
                table: "reports",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_service_request_id",
                table: "reports",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_status_code_received_at",
                table: "reports",
                columns: new[] { "status_code", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_reports_transaction_id",
                table: "reports",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_updated_by_user_id",
                table: "reports",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_actions_actor_user_id",
                table: "sanction_appeal_actions",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_actions_idempotency_key",
                table: "sanction_appeal_actions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_actions_public_id",
                table: "sanction_appeal_actions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_actions_sanction_appeal_id_occurred_at",
                table: "sanction_appeal_actions",
                columns: new[] { "sanction_appeal_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_evidence_created_by_user_id",
                table: "sanction_appeal_evidence",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_evidence_file_id",
                table: "sanction_appeal_evidence",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_evidence_sanction_appeal_id_file_id",
                table: "sanction_appeal_evidence",
                columns: new[] { "sanction_appeal_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeal_evidence_submitted_by_user_id",
                table: "sanction_appeal_evidence",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_applicant_user_id",
                table: "sanction_appeals",
                column: "applicant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_assigned_admin_user_id",
                table: "sanction_appeals",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_created_by_user_id",
                table: "sanction_appeals",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_idempotency_key",
                table: "sanction_appeals",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_previous_appeal_id",
                table: "sanction_appeals",
                column: "previous_appeal_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_public_id",
                table: "sanction_appeals",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_sanction_id_submitted_at",
                table: "sanction_appeals",
                columns: new[] { "sanction_id", "submitted_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_status_code_submitted_at",
                table: "sanction_appeals",
                columns: new[] { "status_code", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sanction_appeals_updated_by_user_id",
                table: "sanction_appeals",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_events_actor_user_id",
                table: "sanction_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_events_idempotency_key",
                table: "sanction_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_events_public_id",
                table: "sanction_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_events_sanction_id_occurred_at",
                table: "sanction_events",
                columns: new[] { "sanction_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_after_service_case_id",
                table: "sanction_sources",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_created_by_user_id",
                table: "sanction_sources",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_dispute_case_id",
                table: "sanction_sources",
                column: "dispute_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_report_id",
                table: "sanction_sources",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_review_id",
                table: "sanction_sources",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_sanction_id_after_service_case_id",
                table: "sanction_sources",
                columns: new[] { "sanction_id", "after_service_case_id" },
                unique: true,
                filter: "[after_service_case_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_sanction_id_dispute_case_id",
                table: "sanction_sources",
                columns: new[] { "sanction_id", "dispute_case_id" },
                unique: true,
                filter: "[dispute_case_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_sanction_id_report_id",
                table: "sanction_sources",
                columns: new[] { "sanction_id", "report_id" },
                unique: true,
                filter: "[report_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_sources_sanction_id_review_id",
                table: "sanction_sources",
                columns: new[] { "sanction_id", "review_id" },
                unique: true,
                filter: "[review_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_types_code",
                table: "sanction_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_types_created_by_user_id",
                table: "sanction_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanction_types_is_active_display_order",
                table: "sanction_types",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_sanction_types_public_id",
                table: "sanction_types",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanction_types_updated_by_user_id",
                table: "sanction_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_created_by_user_id",
                table: "sanctions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_decided_by_user_id",
                table: "sanctions",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_idempotency_key",
                table: "sanctions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_provider_profile_id",
                table: "sanctions",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_provider_service_category_id",
                table: "sanctions",
                column: "provider_service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_public_id",
                table: "sanctions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_released_by_user_id",
                table: "sanctions",
                column: "released_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_sanction_type_id",
                table: "sanctions",
                column: "sanction_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_status_code_start_at_end_at",
                table: "sanctions",
                columns: new[] { "status_code", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_target_role_id",
                table: "sanctions",
                column: "target_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_target_user_id",
                table: "sanctions",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_target_user_id_status_code_start_at",
                table: "sanctions",
                columns: new[] { "target_user_id", "status_code", "start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_updated_by_user_id",
                table: "sanctions",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_resolutions_dispute_liability_types_liability_type_id",
                table: "dispute_resolutions",
                column: "liability_type_id",
                principalTable: "dispute_liability_types",
                principalColumn: "id");

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [TR_sanction_events_append_only]
                ON [sanction_events]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''sanction_events is append-only.'', 1;
                END')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_sanction_events_append_only]");

            migrationBuilder.DropForeignKey(
                name: "FK_dispute_resolutions_dispute_liability_types_liability_type_id",
                table: "dispute_resolutions");

            migrationBuilder.DropTable(
                name: "dispute_liability_types");

            migrationBuilder.DropTable(
                name: "report_actions");

            migrationBuilder.DropTable(
                name: "report_evidence");

            migrationBuilder.DropTable(
                name: "sanction_appeal_actions");

            migrationBuilder.DropTable(
                name: "sanction_appeal_evidence");

            migrationBuilder.DropTable(
                name: "sanction_events");

            migrationBuilder.DropTable(
                name: "sanction_sources");

            migrationBuilder.DropTable(
                name: "sanction_appeals");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "sanctions");

            migrationBuilder.DropTable(
                name: "report_types");

            migrationBuilder.DropTable(
                name: "sanction_types");

            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_dispute_resolutions_liability_type_id",
                table: "dispute_resolutions");

            migrationBuilder.DropColumn(
                name: "liability_type_id",
                table: "dispute_resolutions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','REVIEW')");
        }
    }
}
