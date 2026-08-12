using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProviderPredevelopmentP0DomainFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_schedule_changes_status",
                table: "subscription_schedule_changes");

            migrationBuilder.AddColumn<string>(
                name: "gps_verification_status_code",
                table: "subscription_visit_schedules",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_INTEGRATED");

            migrationBuilder.AddColumn<string>(
                name: "possession_verification_status_code",
                table: "subscription_visit_schedules",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_INTEGRATED");

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_override_at",
                table: "subscription_visit_schedules",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "verification_override_by_user_id",
                table: "subscription_visit_schedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_override_reason",
                table: "subscription_visit_schedules",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visit_verification_status_code",
                table: "subscription_visit_schedules",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_VERIFIED");

            migrationBuilder.AddColumn<string>(
                name: "completion_policy_snapshot_json",
                table: "subscription_contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<DateTime>(
                name: "admin_completed_at",
                table: "interior_projects",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "admin_completed_by_user_id",
                table: "interior_projects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "customer_completion_acknowledged_at",
                table: "interior_projects",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "customer_completion_acknowledged_by_user_id",
                table: "interior_projects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_completion_comment",
                table: "interior_projects",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "provider_completion_submitted_at",
                table: "interior_projects",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "provider_completion_submitted_by_user_id",
                table: "interior_projects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_completion_summary",
                table: "interior_projects",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_final_checklist_json",
                table: "interior_projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "interior_project_participants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interior_project_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    role_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    scope_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_project_participants", x => x.id);
                    table.CheckConstraint("CK_interior_project_participants_period", "[effective_to] IS NULL OR [effective_to] >= [effective_from]");
                    table.CheckConstraint("CK_interior_project_participants_role", "[role_code] IN ('SITE_SURVEY','DESIGN','PRIMARY_CONTRACTOR','TRADE_CONTRACTOR','INSPECTION','AFTER_SERVICE')");
                    table.CheckConstraint("CK_interior_project_participants_status", "[status_code] IN ('ACTIVE','ENDED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_interior_project_participants_interior_projects_interior_project_id",
                        column: x => x.interior_project_id,
                        principalTable: "interior_projects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_project_participants_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_project_participants_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_project_participants_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_stage_inspection_acknowledgements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stage_inspection_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_stage_inspection_acknowledgements", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_acknowledgements_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_acknowledgements_interior_stage_inspections_stage_inspection_id",
                        column: x => x.stage_inspection_id,
                        principalTable: "interior_stage_inspections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_acknowledgements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_visit_schedules_verification_override_by_user_id",
                table: "subscription_visit_schedules",
                column: "verification_override_by_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_visits_verification",
                table: "subscription_visit_schedules",
                sql: "[visit_verification_status_code] IN ('NOT_VERIFIED','PENDING','VERIFIED','OVERRIDE_APPROVED','FAILED','NOT_INTEGRATED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_schedule_changes_status",
                table: "subscription_schedule_changes",
                sql: "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_admin_completed_by_user_id",
                table: "interior_projects",
                column: "admin_completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_customer_completion_acknowledged_by_user_id",
                table: "interior_projects",
                column: "customer_completion_acknowledged_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_projects_provider_completion_submitted_by_user_id",
                table: "interior_projects",
                column: "provider_completion_submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_participants_created_by_user_id",
                table: "interior_project_participants",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_participants_interior_project_id_provider_profile_id_role_code",
                table: "interior_project_participants",
                columns: new[] { "interior_project_id", "provider_profile_id", "role_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_participants_provider_profile_id_status_code_effective_to",
                table: "interior_project_participants",
                columns: new[] { "provider_profile_id", "status_code", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_participants_public_id",
                table: "interior_project_participants",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_project_participants_updated_by_user_id",
                table: "interior_project_participants",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_acknowledgements_created_by_user_id",
                table: "interior_stage_inspection_acknowledgements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_acknowledgements_customer_profile_id",
                table: "interior_stage_inspection_acknowledgements",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_acknowledgements_idempotency_key",
                table: "interior_stage_inspection_acknowledgements",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_acknowledgements_public_id",
                table: "interior_stage_inspection_acknowledgements",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_acknowledgements_stage_inspection_id_customer_profile_id",
                table: "interior_stage_inspection_acknowledgements",
                columns: new[] { "stage_inspection_id", "customer_profile_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_interior_projects_users_admin_completed_by_user_id",
                table: "interior_projects",
                column: "admin_completed_by_user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interior_projects_users_customer_completion_acknowledged_by_user_id",
                table: "interior_projects",
                column: "customer_completion_acknowledged_by_user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interior_projects_users_provider_completion_submitted_by_user_id",
                table: "interior_projects",
                column: "provider_completion_submitted_by_user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_subscription_visit_schedules_users_verification_override_by_user_id",
                table: "subscription_visit_schedules",
                column: "verification_override_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_interior_projects_users_admin_completed_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_interior_projects_users_customer_completion_acknowledged_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_interior_projects_users_provider_completion_submitted_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_subscription_visit_schedules_users_verification_override_by_user_id",
                table: "subscription_visit_schedules");

            migrationBuilder.DropTable(
                name: "interior_project_participants");

            migrationBuilder.DropTable(
                name: "interior_stage_inspection_acknowledgements");

            migrationBuilder.DropIndex(
                name: "IX_subscription_visit_schedules_verification_override_by_user_id",
                table: "subscription_visit_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_visits_verification",
                table: "subscription_visit_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_schedule_changes_status",
                table: "subscription_schedule_changes");

            migrationBuilder.DropIndex(
                name: "IX_interior_projects_admin_completed_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropIndex(
                name: "IX_interior_projects_customer_completion_acknowledged_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropIndex(
                name: "IX_interior_projects_provider_completion_submitted_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "gps_verification_status_code",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "possession_verification_status_code",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "verification_override_at",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "verification_override_by_user_id",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "verification_override_reason",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "visit_verification_status_code",
                table: "subscription_visit_schedules");

            migrationBuilder.DropColumn(
                name: "completion_policy_snapshot_json",
                table: "subscription_contracts");

            migrationBuilder.DropColumn(
                name: "admin_completed_at",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "admin_completed_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "customer_completion_acknowledged_at",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "customer_completion_acknowledged_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "customer_completion_comment",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "provider_completion_submitted_at",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "provider_completion_submitted_by_user_id",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "provider_completion_summary",
                table: "interior_projects");

            migrationBuilder.DropColumn(
                name: "provider_final_checklist_json",
                table: "interior_projects");

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_schedule_changes_status",
                table: "subscription_schedule_changes",
                sql: "[status_code] IN ('REQUESTED','APPROVED','REJECTED')");
        }
    }
}
