using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateQuoteAcceptanceAndWalletFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "actual_charged_fee_amount",
                table: "transactions",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "calculated_fee_amount",
                table: "transactions",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "category_fee_policy_id",
                table: "transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_calculation_method_snapshot",
                table: "transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_charge_timing_snapshot",
                table: "transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_currency_code",
                table: "transactions",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_policy_kind_snapshot",
                table: "transactions",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_policy_snapshot_json",
                table: "transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_policy_version_snapshot",
                table: "transactions",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_restore_rule_snapshot",
                table: "transactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_transaction_type_snapshot",
                table: "transactions",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "wallet_ledger_entry_id",
                table: "transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_category_fee_policy_id",
                table: "transactions",
                column: "category_fee_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_wallet_ledger_entry_id",
                table: "transactions",
                column: "wallet_ledger_entry_id",
                unique: true,
                filter: "[wallet_ledger_entry_id] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transactions_fee_amounts",
                table: "transactions",
                sql: "([calculated_fee_amount] IS NULL OR [calculated_fee_amount] >= 0) AND ([actual_charged_fee_amount] IS NULL OR [actual_charged_fee_amount] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transactions_fee_policy_json",
                table: "transactions",
                sql: "[fee_policy_snapshot_json] IS NULL OR ISJSON([fee_policy_snapshot_json]) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_category_fee_policies_category_fee_policy_id",
                table: "transactions",
                column: "category_fee_policy_id",
                principalTable: "category_fee_policies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_wallet_ledger_wallet_ledger_entry_id",
                table: "transactions",
                column: "wallet_ledger_entry_id",
                principalTable: "wallet_ledger",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_category_fee_policies_category_fee_policy_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_wallet_ledger_wallet_ledger_entry_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_category_fee_policy_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_wallet_ledger_entry_id",
                table: "transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transactions_fee_amounts",
                table: "transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transactions_fee_policy_json",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "actual_charged_fee_amount",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "calculated_fee_amount",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "category_fee_policy_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_calculation_method_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_charge_timing_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_currency_code",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_policy_kind_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_policy_snapshot_json",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_policy_version_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_restore_rule_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee_transaction_type_snapshot",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "wallet_ledger_entry_id",
                table: "transactions");
        }
    }
}
