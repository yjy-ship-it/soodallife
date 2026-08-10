using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAfterServiceAndDisputeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_cases_status",
                table: "after_service_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_actions_from_status",
                table: "after_service_actions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_actions_to_status",
                table: "after_service_actions");

            migrationBuilder.AddColumn<long>(
                name: "assigned_admin_user_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "converted_to_dispute_at",
                table: "after_service_cases",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "due_at",
                table: "after_service_cases",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_within_warranty",
                table: "after_service_cases",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_action_at",
                table: "after_service_cases",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "provider_confirmed_at",
                table: "after_service_cases",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_response_text",
                table: "after_service_cases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "recurrence_occurred",
                table: "after_service_cases",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reported_by_user_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_details",
                table: "after_service_cases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_summary",
                table: "after_service_cases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unresolved_reason",
                table: "after_service_cases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "visit_required",
                table: "after_service_cases",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_end_date",
                table: "after_service_cases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_start_date",
                table: "after_service_cases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "action_type_code",
                table: "after_service_actions",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "STATE_CHANGE");

            migrationBuilder.AddColumn<string>(
                name: "materials_text",
                table: "after_service_actions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "performed_at",
                table: "after_service_actions",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "provider_profile_id",
                table: "after_service_actions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "after_service_actions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "recurrence_occurred",
                table: "after_service_actions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result_text",
                table: "after_service_actions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "after_service_actions",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "visit_occurred",
                table: "after_service_actions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "dispute_cases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: true),
                    applicant_user_id = table.Column<long>(type: "bigint", nullable: false),
                    counterparty_user_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "OPEN"),
                    received_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    due_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_action_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_cases", x => x.id);
                    table.CheckConstraint("CK_dispute_cases_status", "[status_code] IN ('OPEN','UNDER_REVIEW','WAITING_CUSTOMER','WAITING_PROVIDER','RESOLVED','CLOSED')");
                    table.ForeignKey(
                        name: "FK_dispute_cases_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_users_applicant_user_id",
                        column: x => x.applicant_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_users_assigned_admin_user_id",
                        column: x => x.assigned_admin_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_users_counterparty_user_id",
                        column: x => x.counterparty_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_cases_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "dispute_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dispute_case_id = table.Column<long>(type: "bigint", nullable: false),
                    action_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    action_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    related_reference_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    related_reference_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_actions", x => x.id);
                    table.CheckConstraint("CK_dispute_actions_type", "[action_type_code] IN ('CREATED','STATUS_CHANGE','ASSIGNMENT','EVIDENCE_ADDED','EVIDENCE_REQUEST','NOTE','RESOLUTION','FEE_RESTORE_LINK')");
                    table.ForeignKey(
                        name: "FK_dispute_actions_dispute_cases_dispute_case_id",
                        column: x => x.dispute_case_id,
                        principalTable: "dispute_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_actions_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "dispute_evidence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dispute_case_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    source_type_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    source_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_dispute_evidence", x => x.id);
                    table.CheckConstraint("CK_dispute_evidence_source", "[file_id] IS NOT NULL OR [source_public_id] IS NOT NULL");
                    table.CheckConstraint("CK_dispute_evidence_status", "[status_code] IN ('ACTIVE','WITHDRAWN')");
                    table.ForeignKey(
                        name: "FK_dispute_evidence_dispute_cases_dispute_case_id",
                        column: x => x.dispute_case_id,
                        principalTable: "dispute_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_evidence_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_evidence_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_evidence_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_evidence_users_withdrawn_by_user_id",
                        column: x => x.withdrawn_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "dispute_resolutions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dispute_case_id = table.Column<long>(type: "bigint", nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    result_summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    decision_details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    basis_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    follow_up_action = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    is_current = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_resolutions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dispute_resolutions_dispute_cases_dispute_case_id",
                        column: x => x.dispute_case_id,
                        principalTable: "dispute_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_resolutions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispute_resolutions_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_assigned_admin_user_id",
                table: "after_service_cases",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_due_at",
                table: "after_service_cases",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_last_action_at",
                table: "after_service_cases",
                column: "last_action_at");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_reported_by_user_id",
                table: "after_service_cases",
                column: "reported_by_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_cases_status",
                table: "after_service_cases",
                sql: "[status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_action_type_code",
                table: "after_service_actions",
                column: "action_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_provider_profile_id",
                table: "after_service_actions",
                column: "provider_profile_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_actions_from_status",
                table: "after_service_actions",
                sql: "[from_status_code] IS NULL OR [from_status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_actions_to_status",
                table: "after_service_actions",
                sql: "[to_status_code] IN ('RECEIVED','PROVIDER_CONFIRMED','VISIT_SCHEDULED','IN_PROGRESS','RESOLVED','UNRESOLVED_CLOSED','CONVERTED_TO_DISPUTE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_actions_type",
                table: "after_service_actions",
                sql: "[action_type_code] IN ('RECEIVED','PROVIDER_CONFIRMATION','VISIT_SCHEDULED','VISIT','REVISIT','TREATMENT','STATE_CHANGE','RESOLUTION','UNRESOLVED_CLOSURE','DISPUTE_CONVERSION','ADMIN_OVERRIDE')");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_action_type_code",
                table: "dispute_actions",
                column: "action_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_actor_user_id",
                table: "dispute_actions",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_dispute_case_id",
                table: "dispute_actions",
                column: "dispute_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_dispute_case_id_occurred_at",
                table: "dispute_actions",
                columns: new[] { "dispute_case_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_idempotency_key",
                table: "dispute_actions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_occurred_at",
                table: "dispute_actions",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_actions_public_id",
                table: "dispute_actions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_after_service_case_id",
                table: "dispute_cases",
                column: "after_service_case_id",
                unique: true,
                filter: "[after_service_case_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_applicant_user_id",
                table: "dispute_cases",
                column: "applicant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_assigned_admin_user_id",
                table: "dispute_cases",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_counterparty_user_id",
                table: "dispute_cases",
                column: "counterparty_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_created_by_user_id",
                table: "dispute_cases",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_due_at",
                table: "dispute_cases",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_public_id",
                table: "dispute_cases",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_received_at",
                table: "dispute_cases",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_status_code",
                table: "dispute_cases",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_status_code_received_at",
                table: "dispute_cases",
                columns: new[] { "status_code", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_transaction_id",
                table: "dispute_cases",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_cases_updated_by_user_id",
                table: "dispute_cases",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_created_by_user_id",
                table: "dispute_evidence",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_dispute_case_id",
                table: "dispute_evidence",
                column: "dispute_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_dispute_case_id_source_type_code_source_public_id",
                table: "dispute_evidence",
                columns: new[] { "dispute_case_id", "source_type_code", "source_public_id" },
                unique: true,
                filter: "[source_public_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_file_id",
                table: "dispute_evidence",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_public_id",
                table: "dispute_evidence",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_source_public_id",
                table: "dispute_evidence",
                column: "source_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_source_type_code",
                table: "dispute_evidence",
                column: "source_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_status_code",
                table: "dispute_evidence",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_submitted_by_user_id",
                table: "dispute_evidence",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_evidence_withdrawn_by_user_id",
                table: "dispute_evidence",
                column: "withdrawn_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_created_by_user_id",
                table: "dispute_resolutions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_decided_by_user_id",
                table: "dispute_resolutions",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_dispute_case_id",
                table: "dispute_resolutions",
                column: "dispute_case_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_dispute_case_id_version_no",
                table: "dispute_resolutions",
                columns: new[] { "dispute_case_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_resolutions_public_id",
                table: "dispute_resolutions",
                column: "public_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_after_service_actions_provider_profiles_provider_profile_id",
                table: "after_service_actions",
                column: "provider_profile_id",
                principalTable: "provider_profiles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_after_service_cases_users_assigned_admin_user_id",
                table: "after_service_cases",
                column: "assigned_admin_user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_after_service_cases_users_reported_by_user_id",
                table: "after_service_cases",
                column: "reported_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_after_service_actions_provider_profiles_provider_profile_id",
                table: "after_service_actions");

            migrationBuilder.DropForeignKey(
                name: "FK_after_service_cases_users_assigned_admin_user_id",
                table: "after_service_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_after_service_cases_users_reported_by_user_id",
                table: "after_service_cases");

            migrationBuilder.DropTable(
                name: "dispute_actions");

            migrationBuilder.DropTable(
                name: "dispute_evidence");

            migrationBuilder.DropTable(
                name: "dispute_resolutions");

            migrationBuilder.DropTable(
                name: "dispute_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_assigned_admin_user_id",
                table: "after_service_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_due_at",
                table: "after_service_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_last_action_at",
                table: "after_service_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_reported_by_user_id",
                table: "after_service_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_cases_status",
                table: "after_service_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_actions_action_type_code",
                table: "after_service_actions");

            migrationBuilder.DropIndex(
                name: "IX_after_service_actions_provider_profile_id",
                table: "after_service_actions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_actions_from_status",
                table: "after_service_actions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_actions_to_status",
                table: "after_service_actions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_actions_type",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "assigned_admin_user_id",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "converted_to_dispute_at",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "due_at",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "is_within_warranty",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "last_action_at",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "provider_confirmed_at",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "provider_response_text",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "recurrence_occurred",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "reported_by_user_id",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "request_details",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "resolution_summary",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "unresolved_reason",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "visit_required",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "warranty_end_date",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "warranty_start_date",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "action_type_code",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "materials_text",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "performed_at",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "provider_profile_id",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "recurrence_occurred",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "result_text",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "after_service_actions");

            migrationBuilder.DropColumn(
                name: "visit_occurred",
                table: "after_service_actions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_cases_status",
                table: "after_service_cases",
                sql: "[status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_actions_from_status",
                table: "after_service_actions",
                sql: "[from_status_code] IS NULL OR [from_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_actions_to_status",
                table: "after_service_actions",
                sql: "[to_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");
        }
    }
}
