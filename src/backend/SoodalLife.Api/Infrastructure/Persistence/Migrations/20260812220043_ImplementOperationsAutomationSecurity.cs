using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementOperationsAutomationSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_outbox_events_status",
                table: "outbox_events");

            migrationBuilder.CreateTable(
                name: "scheduled_job_leases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    configuration_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    lease_owner = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_succeeded_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_failed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_error_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    next_scheduled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processing_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    failed_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_leases", x => x.id);
                    table.CheckConstraint("CK_scheduled_job_leases_configuration", "[configuration_status_code] IN ('ENABLED','DISABLED','NOT_CONFIGURED')");
                });

            migrationBuilder.CreateTable(
                name: "scheduled_job_runs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    instance_id = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    failed_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    error_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_runs", x => x.id);
                    table.CheckConstraint("CK_scheduled_job_runs_status", "[status_code] IN ('RUNNING','SUCCEEDED','FAILED','SKIPPED')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_outbox_events_status",
                table: "outbox_events",
                sql: "[status_code] IN ('PENDING','PROCESSING','PUBLISHED','FAILED','DEAD')");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_leases_configuration_status_code_next_scheduled_at",
                table: "scheduled_job_leases",
                columns: new[] { "configuration_status_code", "next_scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_leases_job_name",
                table: "scheduled_job_leases",
                column: "job_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_leases_lease_expires_at",
                table: "scheduled_job_leases",
                column: "lease_expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_leases_public_id",
                table: "scheduled_job_leases",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_runs_job_name_started_at",
                table: "scheduled_job_runs",
                columns: new[] { "job_name", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_runs_public_id",
                table: "scheduled_job_runs",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_runs_status_code",
                table: "scheduled_job_runs",
                column: "status_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_job_leases");

            migrationBuilder.DropTable(
                name: "scheduled_job_runs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_outbox_events_status",
                table: "outbox_events");

            migrationBuilder.AddCheckConstraint(
                name: "CK_outbox_events_status",
                table: "outbox_events",
                sql: "[status_code] IN ('PENDING','PROCESSING','PUBLISHED','FAILED')");
        }
    }
}
