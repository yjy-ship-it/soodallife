using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCustomerAccountWithdrawalClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_withdrawal_requests_user_id_scope_code",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_customer_withdrawal_status",
                table: "customer_withdrawal_requests");

            migrationBuilder.AddColumn<string>(
                name: "decision_idempotency_key",
                table: "customer_withdrawal_requests",
                type: "varchar(150)",
                unicode: false,
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "customer_withdrawal_requests",
                type: "varchar(150)",
                unicode: false,
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_decision_idempotency_key",
                table: "customer_withdrawal_requests",
                column: "decision_idempotency_key",
                unique: true,
                filter: "[decision_idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_idempotency_key",
                table: "customer_withdrawal_requests",
                column: "idempotency_key",
                unique: true,
                filter: "[idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_user_id_scope_code",
                table: "customer_withdrawal_requests",
                columns: new[] { "user_id", "scope_code" },
                unique: true,
                filter: "[status_code] IN ('REQUESTED','UNDER_REVIEW','BLOCKED_BY_ACTIVE_WORK','READY_TO_COMPLETE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_customer_withdrawal_status",
                table: "customer_withdrawal_requests",
                sql: "[status_code] IN ('REQUESTED','UNDER_REVIEW','BLOCKED_BY_ACTIVE_WORK','READY_TO_COMPLETE','APPROVED','COMPLETED','REJECTED','CANCELLED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_withdrawal_requests_decision_idempotency_key",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropIndex(
                name: "IX_customer_withdrawal_requests_idempotency_key",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropIndex(
                name: "IX_customer_withdrawal_requests_user_id_scope_code",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_customer_withdrawal_status",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "decision_idempotency_key",
                table: "customer_withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "customer_withdrawal_requests");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_user_id_scope_code",
                table: "customer_withdrawal_requests",
                columns: new[] { "user_id", "scope_code" },
                unique: true,
                filter: "[status_code] = 'REQUESTED'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_customer_withdrawal_status",
                table: "customer_withdrawal_requests",
                sql: "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
        }
    }
}
