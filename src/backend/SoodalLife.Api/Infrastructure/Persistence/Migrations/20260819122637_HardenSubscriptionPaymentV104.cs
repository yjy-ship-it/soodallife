using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSubscriptionPaymentV104 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gateway_attempt_no",
                table: "subscription_payment_requests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "external_token_reference",
                table: "subscription_payment_methods",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldUnicode: false,
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "billing_anchor_day",
                table: "subscription_contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "payment_method_id",
                table: "subscription_contracts",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE contract
                   SET payment_method_id = COALESCE(
                       (SELECT TOP (1) payment.payment_method_id
                          FROM subscription_payment_requests payment
                         WHERE payment.subscription_contract_id = contract.id
                           AND payment.payment_method_id IS NOT NULL
                         ORDER BY CASE WHEN payment.status_code = 'COMPLETED' THEN 0 ELSE 1 END,
                                  payment.requested_at DESC),
                       (SELECT TOP (1) method.id
                          FROM subscription_payment_methods method
                         WHERE method.customer_profile_id = contract.customer_profile_id
                           AND method.status_code = 'ACTIVE'
                         ORDER BY method.is_default DESC, method.registered_at DESC)),
                       billing_anchor_day = DAY(COALESCE(contract.started_at, contract.created_at))
                  FROM subscription_contracts contract
                 WHERE contract.payment_method_id IS NULL OR contract.billing_anchor_day IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_contracts_billing_anchor_day",
                table: "subscription_contracts",
                sql: "[billing_anchor_day] IS NULL OR [billing_anchor_day] BETWEEN 1 AND 31");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_contracts_payment_method_id",
                table: "subscription_contracts",
                column: "payment_method_id");

            migrationBuilder.AddForeignKey(
                name: "FK_subscription_contracts_subscription_payment_methods_payment_method_id",
                table: "subscription_contracts",
                column: "payment_method_id",
                principalTable: "subscription_payment_methods",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscription_contracts_subscription_payment_methods_payment_method_id",
                table: "subscription_contracts");

            migrationBuilder.DropIndex(
                name: "IX_subscription_contracts_payment_method_id",
                table: "subscription_contracts");

            migrationBuilder.DropColumn(
                name: "gateway_attempt_no",
                table: "subscription_payment_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_contracts_billing_anchor_day",
                table: "subscription_contracts");

            migrationBuilder.DropColumn(
                name: "billing_anchor_day",
                table: "subscription_contracts");

            migrationBuilder.DropColumn(
                name: "payment_method_id",
                table: "subscription_contracts");

            migrationBuilder.AlterColumn<string>(
                name: "external_token_reference",
                table: "subscription_payment_methods",
                type: "varchar(300)",
                unicode: false,
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldUnicode: false,
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
