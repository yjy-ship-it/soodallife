using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTransactionAppointmentAndCancellationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transaction_appointments_status",
                table: "transaction_appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transaction_appointment_changes_period",
                table: "transaction_appointment_change_requests");

            migrationBuilder.AddColumn<string>(
                name: "proposal_idempotency_key",
                table: "transaction_appointments",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "requested_start_at",
                table: "transaction_appointment_change_requests",
                type: "datetime2(7)",
                precision: 7,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(7)",
                oldPrecision: 7);

            migrationBuilder.AddColumn<string>(
                name: "change_type_code",
                table: "transaction_appointment_change_requests",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "RESCHEDULE");

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "transaction_appointment_change_requests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "transaction_appointment_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_appointment_id = table.Column<long>(type: "bigint", nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    before_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    after_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    before_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    before_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    after_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    after_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_appointment_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_appointment_events_transaction_appointments_transaction_appointment_id",
                        column: x => x.transaction_appointment_id,
                        principalTable: "transaction_appointments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_appointment_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transaction_cancellation_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "REQUESTED"),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    processing_note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    decision_idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_cancellation_requests", x => x.id);
                    table.CheckConstraint("CK_transaction_cancellation_requests_status", "[status_code] IN ('REQUESTED','ADMIN_REVIEW_REQUIRED','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_transaction_cancellation_requests_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_cancellation_requests_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_cancellation_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_proposal_idempotency_key",
                table: "transaction_appointments",
                column: "proposal_idempotency_key",
                unique: true,
                filter: "[proposal_idempotency_key] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transaction_appointments_status",
                table: "transaction_appointments",
                sql: "[status_code] IN ('PROPOSED','CONFIRMED','REJECTED','CANCELLED','COMPLETED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transaction_appointment_changes_period",
                table: "transaction_appointment_change_requests",
                sql: "([change_type_code] = 'CANCEL' AND [requested_start_at] IS NULL AND [requested_end_at] IS NULL) OR ([change_type_code] = 'RESCHEDULE' AND [requested_start_at] IS NOT NULL AND ([requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transaction_appointment_changes_type",
                table: "transaction_appointment_change_requests",
                sql: "[change_type_code] IN ('RESCHEDULE','CANCEL')");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_events_actor_user_id",
                table: "transaction_appointment_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_events_idempotency_key",
                table: "transaction_appointment_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_events_public_id",
                table: "transaction_appointment_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_events_transaction_appointment_id_occurred_at",
                table: "transaction_appointment_events",
                columns: new[] { "transaction_appointment_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_decision_idempotency_key",
                table: "transaction_cancellation_requests",
                column: "decision_idempotency_key",
                unique: true,
                filter: "[decision_idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_idempotency_key",
                table: "transaction_cancellation_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_processed_by_user_id",
                table: "transaction_cancellation_requests",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_public_id",
                table: "transaction_cancellation_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_requested_by_user_id",
                table: "transaction_cancellation_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_status_code",
                table: "transaction_cancellation_requests",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cancellation_requests_transaction_id_requested_at",
                table: "transaction_cancellation_requests",
                columns: new[] { "transaction_id", "requested_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_appointment_events");

            migrationBuilder.DropTable(
                name: "transaction_cancellation_requests");

            migrationBuilder.DropIndex(
                name: "IX_transaction_appointments_proposal_idempotency_key",
                table: "transaction_appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transaction_appointments_status",
                table: "transaction_appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transaction_appointment_changes_period",
                table: "transaction_appointment_change_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transaction_appointment_changes_type",
                table: "transaction_appointment_change_requests");

            migrationBuilder.DropColumn(
                name: "proposal_idempotency_key",
                table: "transaction_appointments");

            migrationBuilder.DropColumn(
                name: "change_type_code",
                table: "transaction_appointment_change_requests");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "transaction_appointment_change_requests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "requested_start_at",
                table: "transaction_appointment_change_requests",
                type: "datetime2(7)",
                precision: 7,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2(7)",
                oldPrecision: 7,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_transaction_appointments_status",
                table: "transaction_appointments",
                sql: "[status_code] IN ('PROPOSED','CONFIRMED','CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transaction_appointment_changes_period",
                table: "transaction_appointment_change_requests",
                sql: "[requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]");
        }
    }
}
