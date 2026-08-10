using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderWalletAndLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    available_balance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    reserved_balance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.CheckConstraint("CK_wallets_balances", "[available_balance] >= 0 AND [reserved_balance] >= 0");
                    table.CheckConstraint("CK_wallets_status", "[status_code] IN ('ACTIVE','FROZEN','CLOSED')");
                    table.ForeignKey(
                        name: "FK_wallets_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallets_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallets_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wallet_ledger",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    entry_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    balance_after = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    reference_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    reference_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    payment_method_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_ledger", x => x.id);
                    table.CheckConstraint("CK_wallet_ledger_amount", "[amount] <> 0");
                    table.CheckConstraint("CK_wallet_ledger_balance", "[balance_after] >= 0");
                    table.CheckConstraint("CK_wallet_ledger_entry_type", "[entry_type_code] IN ('CHARGE','USE','RESTORE','REFUND','ADJUST')");
                    table.ForeignKey(
                        name: "FK_wallet_ledger_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_ledger_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_ledger_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fee_charges",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    category_fee_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    ledger_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    charged_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    restore_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "NOT_RESTORED"),
                    restore_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_charges", x => x.id);
                    table.CheckConstraint("CK_fee_charges_amount", "[fee_amount] > 0");
                    table.CheckConstraint("CK_fee_charges_restore_status", "[restore_status_code] IN ('NOT_RESTORED','RESTORED')");
                    table.ForeignKey(
                        name: "FK_fee_charges_category_fee_policies_category_fee_policy_id",
                        column: x => x.category_fee_policy_id,
                        principalTable: "category_fee_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_wallet_ledger_ledger_entry_id",
                        column: x => x.ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_wallet_ledger_restore_ledger_entry_id",
                        column: x => x.restore_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_charges_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wallet_charge_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    payment_method_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "REQUESTED"),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_payment_reference = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    request_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_charge_requests", x => x.id);
                    table.CheckConstraint("CK_wallet_charge_requests_amount", "[requested_amount] > 0");
                    table.CheckConstraint("CK_wallet_charge_requests_status", "[status_code] IN ('REQUESTED','PROCESSING','SUCCEEDED','FAILED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_wallet_charge_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_charge_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_charge_requests_wallet_ledger_ledger_entry_id",
                        column: x => x.ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_charge_requests_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wallet_refund_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "REQUESTED"),
                    request_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    reviewed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    review_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_refund_reference = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_refund_requests", x => x.id);
                    table.CheckConstraint("CK_wallet_refund_requests_amount", "[requested_amount] > 0");
                    table.CheckConstraint("CK_wallet_refund_requests_status", "[status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')");
                    table.ForeignKey(
                        name: "FK_wallet_refund_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_refund_requests_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_refund_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_refund_requests_wallet_ledger_ledger_entry_id",
                        column: x => x.ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_refund_requests_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fee_restores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fee_charge_id = table.Column<long>(type: "bigint", nullable: false),
                    ledger_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    restored_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    restored_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_restores", x => x.id);
                    table.CheckConstraint("CK_fee_restores_reason_code", "[reason_code] IN ('SYSTEM_ERROR','DUPLICATE_ACCEPTANCE','FALSE_REQUEST','HEAD_OFFICE_APPROVAL')");
                    table.ForeignKey(
                        name: "FK_fee_restores_fee_charges_fee_charge_id",
                        column: x => x.fee_charge_id,
                        principalTable: "fee_charges",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_restores_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_restores_users_restored_by_user_id",
                        column: x => x.restored_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_restores_wallet_ledger_ledger_entry_id",
                        column: x => x.ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_category_fee_policy_id",
                table: "fee_charges",
                column: "category_fee_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_created_by_user_id",
                table: "fee_charges",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_ledger_entry_id",
                table: "fee_charges",
                column: "ledger_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_public_id",
                table: "fee_charges",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_restore_ledger_entry_id",
                table: "fee_charges",
                column: "restore_ledger_entry_id",
                unique: true,
                filter: "[restore_ledger_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_transaction_id_category_fee_policy_id",
                table: "fee_charges",
                columns: new[] { "transaction_id", "category_fee_policy_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_updated_by_user_id",
                table: "fee_charges",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_charges_wallet_id_charged_at",
                table: "fee_charges",
                columns: new[] { "wallet_id", "charged_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_created_by_user_id",
                table: "fee_restores",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_fee_charge_id",
                table: "fee_restores",
                column: "fee_charge_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_idempotency_key",
                table: "fee_restores",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_ledger_entry_id",
                table: "fee_restores",
                column: "ledger_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_public_id",
                table: "fee_restores",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_restores_restored_by_user_id",
                table: "fee_restores",
                column: "restored_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_created_by_user_id",
                table: "wallet_charge_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_idempotency_key",
                table: "wallet_charge_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_ledger_entry_id",
                table: "wallet_charge_requests",
                column: "ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_public_id",
                table: "wallet_charge_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_status_code",
                table: "wallet_charge_requests",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_updated_by_user_id",
                table: "wallet_charge_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_charge_requests_wallet_id_requested_at",
                table: "wallet_charge_requests",
                columns: new[] { "wallet_id", "requested_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_created_by_user_id",
                table: "wallet_ledger",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_idempotency_key",
                table: "wallet_ledger",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_public_id",
                table: "wallet_ledger",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_reference_type_reference_public_id",
                table: "wallet_ledger",
                columns: new[] { "reference_type", "reference_public_id" });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_transaction_id",
                table: "wallet_ledger",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_wallet_id_occurred_at",
                table: "wallet_ledger",
                columns: new[] { "wallet_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_created_by_user_id",
                table: "wallet_refund_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_idempotency_key",
                table: "wallet_refund_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_ledger_entry_id",
                table: "wallet_refund_requests",
                column: "ledger_entry_id",
                unique: true,
                filter: "[ledger_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_public_id",
                table: "wallet_refund_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_reviewed_by_user_id",
                table: "wallet_refund_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_status_code",
                table: "wallet_refund_requests",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_updated_by_user_id",
                table: "wallet_refund_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_refund_requests_wallet_id_requested_at",
                table: "wallet_refund_requests",
                columns: new[] { "wallet_id", "requested_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_wallets_created_by_user_id",
                table: "wallets",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_provider_profile_id_currency_code",
                table: "wallets",
                columns: new[] { "provider_profile_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallets_public_id",
                table: "wallets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallets_status_code",
                table: "wallets",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_updated_by_user_id",
                table: "wallets",
                column: "updated_by_user_id");

            migrationBuilder.Sql(
                """
                INSERT INTO wallets
                    (public_id, provider_profile_id, currency_code, available_balance, reserved_balance, status_code,
                     created_at, created_by_user_id, updated_at, updated_by_user_id)
                SELECT NEWID(), provider.id, 'KRW', 0, 0, 'ACTIVE',
                       SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL
                FROM provider_profiles provider
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM wallets wallet
                    WHERE wallet.provider_profile_id = provider.id
                      AND wallet.currency_code = 'KRW'
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER TR_wallet_ledger_append_only
                ON wallet_ledger
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, N'Wallet 원장은 수정하거나 삭제할 수 없습니다. 반대 방향의 새 원장 항목을 생성해 주세요.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_restores");

            migrationBuilder.DropTable(
                name: "wallet_charge_requests");

            migrationBuilder.DropTable(
                name: "wallet_refund_requests");

            migrationBuilder.DropTable(
                name: "fee_charges");

            migrationBuilder.DropTable(
                name: "wallet_ledger");

            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
