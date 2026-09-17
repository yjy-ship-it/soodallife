using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAutonomousEmergencyDispatchV131 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_emergency_progress_events_type",
                table: "emergency_progress_events");

            migrationBuilder.AddColumn<string>(
                name: "additional_fee_text",
                table: "provider_emergency_service_settings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_dispatch_fee_amount",
                table: "provider_emergency_service_settings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "no_show_fee_amount",
                table: "provider_emergency_service_settings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "no_show_wait_minutes",
                table: "provider_emergency_service_settings",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "payment_instruction_protected",
                table: "provider_emergency_service_settings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_mode_code",
                table: "provider_emergency_service_settings",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "ON_SITE");

            migrationBuilder.AddColumn<bool>(
                name: "work_fee_separate",
                table: "provider_emergency_service_settings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "additional_fee_text",
                table: "emergency_responses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_dispatch_fee_amount",
                table: "emergency_responses",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "no_show_fee_amount",
                table: "emergency_responses",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "no_show_wait_minutes",
                table: "emergency_responses",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "payment_mode_code",
                table: "emergency_responses",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "ON_SITE");

            migrationBuilder.AddColumn<bool>(
                name: "work_fee_separate",
                table: "emergency_responses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "emergency_dispatch_agreements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    base_dispatch_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    payment_mode_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    payment_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    no_show_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    no_show_wait_minutes = table.Column<int>(type: "int", nullable: false),
                    work_fee_separate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    additional_fee_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    payment_instruction_protected = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    terms_accepted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    payment_reported_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    payment_confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    arrived_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    no_show_wait_until = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    no_show_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    no_show_reported_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    no_show_reported_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergency_dispatch_agreements", x => x.id);
                    table.CheckConstraint("CK_emergency_agreement_no_show", "[no_show_status_code] IS NULL OR [no_show_status_code] IN ('WAITING','CUSTOMER_NO_SHOW','PROVIDER_NO_SHOW','DISPUTED')");
                    table.CheckConstraint("CK_emergency_agreement_payment_mode", "[payment_mode_code] IN ('NO_FEE','ON_SITE','TRANSFER_REPORTED','TRANSFER_CONFIRMED')");
                    table.CheckConstraint("CK_emergency_agreement_payment_status", "[payment_status_code] IN ('NOT_REQUIRED','ON_SITE_PENDING','AWAITING_TRANSFER','REPORTED','CONFIRMED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_emergency_dispatch_agreements_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_dispatch_agreements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_dispatch_agreements_users_no_show_reported_by_user_id",
                        column: x => x.no_show_reported_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_dispatch_agreements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_emergency_service_amounts",
                table: "provider_emergency_service_settings",
                sql: "[base_dispatch_fee_amount] >= 0 AND [no_show_fee_amount] >= 0 AND [no_show_fee_amount] <= [base_dispatch_fee_amount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_emergency_service_payment_mode",
                table: "provider_emergency_service_settings",
                sql: "[payment_mode_code] IN ('NO_FEE','ON_SITE','TRANSFER_REPORTED','TRANSFER_CONFIRMED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_emergency_service_wait",
                table: "provider_emergency_service_settings",
                sql: "[no_show_wait_minutes] BETWEEN 5 AND 60");

            migrationBuilder.AddCheckConstraint(
                name: "CK_emergency_progress_events_type",
                table: "emergency_progress_events",
                sql: "[event_type_code] IN ('DISPATCH_CONFIRMED','PAYMENT_REPORTED','PAYMENT_CONFIRMED','PAYMENT_REJECTED','DEPARTED','ARRIVED','COMPLETED','NO_SHOW_WAITING','CUSTOMER_NO_SHOW','PROVIDER_NO_SHOW','NO_SHOW_DISPUTED')");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_dispatch_agreements_created_by_user_id",
                table: "emergency_dispatch_agreements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_dispatch_agreements_no_show_reported_by_user_id",
                table: "emergency_dispatch_agreements",
                column: "no_show_reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_dispatch_agreements_public_id",
                table: "emergency_dispatch_agreements",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_dispatch_agreements_transaction_id",
                table: "emergency_dispatch_agreements",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_dispatch_agreements_updated_by_user_id",
                table: "emergency_dispatch_agreements",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emergency_dispatch_agreements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_emergency_service_amounts",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_emergency_service_payment_mode",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_emergency_service_wait",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_emergency_progress_events_type",
                table: "emergency_progress_events");

            migrationBuilder.DropColumn(
                name: "additional_fee_text",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "base_dispatch_fee_amount",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "no_show_fee_amount",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "no_show_wait_minutes",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "payment_instruction_protected",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "payment_mode_code",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "work_fee_separate",
                table: "provider_emergency_service_settings");

            migrationBuilder.DropColumn(
                name: "additional_fee_text",
                table: "emergency_responses");

            migrationBuilder.DropColumn(
                name: "base_dispatch_fee_amount",
                table: "emergency_responses");

            migrationBuilder.DropColumn(
                name: "no_show_fee_amount",
                table: "emergency_responses");

            migrationBuilder.DropColumn(
                name: "no_show_wait_minutes",
                table: "emergency_responses");

            migrationBuilder.DropColumn(
                name: "payment_mode_code",
                table: "emergency_responses");

            migrationBuilder.DropColumn(
                name: "work_fee_separate",
                table: "emergency_responses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_emergency_progress_events_type",
                table: "emergency_progress_events",
                sql: "[event_type_code] IN ('DISPATCH_CONFIRMED','DEPARTED','EN_ROUTE','ARRIVED')");
        }
    }
}
