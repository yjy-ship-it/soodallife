using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    // Customer transaction schedule aggregate; no existing rows are rewritten.
    /// <inheritdoc />
    public partial class ImplementCustomerTransactionAppointmentsAndCompletionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transaction_appointments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    scheduled_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "int", nullable: true),
                    customer_memo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    provider_memo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PROPOSED"),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_appointments", x => x.id);
                    table.CheckConstraint("CK_transaction_appointments_duration", "[estimated_duration_minutes] IS NULL OR [estimated_duration_minutes] > 0");
                    table.CheckConstraint("CK_transaction_appointments_period", "[scheduled_end_at] IS NULL OR [scheduled_end_at] > [scheduled_start_at]");
                    table.CheckConstraint("CK_transaction_appointments_status", "[status_code] IN ('PROPOSED','CONFIRMED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_transaction_appointments_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_appointments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_appointments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transaction_appointment_change_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_appointment_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    requested_end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "REQUESTED"),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    processing_note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_appointment_change_requests", x => x.id);
                    table.CheckConstraint("CK_transaction_appointment_changes_period", "[requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]");
                    table.CheckConstraint("CK_transaction_appointment_changes_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_transaction_appointment_change_requests_transaction_appointments_transaction_appointment_id",
                        column: x => x.transaction_appointment_id,
                        principalTable: "transaction_appointments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_appointment_change_requests_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_appointment_change_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_idempotency_key",
                table: "transaction_appointment_change_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_processed_by_user_id",
                table: "transaction_appointment_change_requests",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_public_id",
                table: "transaction_appointment_change_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_requested_at",
                table: "transaction_appointment_change_requests",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_requested_by_user_id",
                table: "transaction_appointment_change_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_status_code",
                table: "transaction_appointment_change_requests",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointment_change_requests_transaction_appointment_id",
                table: "transaction_appointment_change_requests",
                column: "transaction_appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_created_by_user_id",
                table: "transaction_appointments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_public_id",
                table: "transaction_appointments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_scheduled_start_at",
                table: "transaction_appointments",
                column: "scheduled_start_at");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_status_code",
                table: "transaction_appointments",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_transaction_id",
                table: "transaction_appointments",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_appointments_updated_by_user_id",
                table: "transaction_appointments",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_appointment_change_requests");

            migrationBuilder.DropTable(
                name: "transaction_appointments");
        }
    }
}
