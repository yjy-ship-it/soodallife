using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSubscriptionBillingAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_status_code",
                table: "subscription_contracts",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_billing_at",
                table: "subscription_contracts",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "monthly_settlements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    settlement_year = table.Column<int>(type: "int", nullable: false),
                    settlement_month = table.Column<int>(type: "int", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    gross_total = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    fee_total = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    adjustment_total = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    net_total = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    item_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    prepared_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_settlements", x => x.id);
                    table.CheckConstraint("CK_monthly_settlements_month", "[settlement_month] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_monthly_settlements_status", "[status_code] IN ('DRAFT','REVIEW','APPROVED','PAYMENT_PENDING','PAID','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_monthly_settlements_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_monthly_settlements_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_monthly_settlements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_methods",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    payment_method_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    external_token_reference = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    masked_display_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    registered_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    disabled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payment_methods", x => x.id);
                    table.CheckConstraint("CK_subscription_payment_methods_status", "[status_code] IN ('ACTIVE','DISABLED')");
                    table.ForeignKey(
                        name: "FK_subscription_payment_methods_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_methods_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_methods_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_payouts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    monthly_settlement_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    approved_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    external_payout_reference = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payouts", x => x.id);
                    table.CheckConstraint("CK_subscription_payouts_amount", "[requested_amount] >= 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)");
                    table.CheckConstraint("CK_subscription_payouts_status", "[status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','FAILED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_subscription_payouts_monthly_settlements_monthly_settlement_id",
                        column: x => x.monthly_settlement_id,
                        principalTable: "monthly_settlements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payouts_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payouts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payouts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_settlement_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_visit_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    settlement_month = table.Column<DateOnly>(type: "date", nullable: false),
                    gross_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    fee_policy_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    fee_policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    calculated_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    adjustment_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    net_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    hold_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    monthly_settlement_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(180)", unicode: false, maxLength: 180, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_settlement_items", x => x.id);
                    table.CheckConstraint("CK_subscription_settlement_items_amounts", "[gross_amount] IS NULL OR [gross_amount] >= 0");
                    table.CheckConstraint("CK_subscription_settlement_items_status", "[status_code] IN ('CALCULATION_PENDING','POLICY_PENDING','READY','HOLD','SETTLED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_monthly_settlements_monthly_settlement_id",
                        column: x => x.monthly_settlement_id,
                        principalTable: "monthly_settlements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_subscription_visit_schedules_subscription_visit_schedule_id",
                        column: x => x.subscription_visit_schedule_id,
                        principalTable: "subscription_visit_schedules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_settlement_items_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    billing_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    billing_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    authorized_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failure_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    external_payment_reference = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payment_requests", x => x.id);
                    table.CheckConstraint("CK_subscription_payment_requests_amount", "[requested_amount] > 0");
                    table.CheckConstraint("CK_subscription_payment_requests_period", "[billing_period_end] >= [billing_period_start]");
                    table.CheckConstraint("CK_subscription_payment_requests_status", "[status_code] IN ('REQUESTED','PROCESSING','COMPLETED','FAILED','CANCELLED','PARTIALLY_REFUNDED','REFUNDED')");
                    table.ForeignKey(
                        name: "FK_subscription_payment_requests_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_requests_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_requests_subscription_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "subscription_payment_methods",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_payout_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_payout_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    event_data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payout_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_payout_events_subscription_payouts_subscription_payout_id",
                        column: x => x.subscription_payout_id,
                        principalTable: "subscription_payouts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payout_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_ledger",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_request_id = table.Column<long>(type: "bigint", nullable: true),
                    entry_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    balance_after = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    reference_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    reference_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reason_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payment_ledger", x => x.id);
                    table.CheckConstraint("CK_subscription_payment_ledger_amount", "[amount] <> 0");
                    table.CheckConstraint("CK_subscription_payment_ledger_type", "[entry_type_code] IN ('PAYMENT','REFUND','ADJUSTMENT','REVERSAL')");
                    table.ForeignKey(
                        name: "FK_subscription_payment_ledger_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_ledger_subscription_payment_requests_payment_request_id",
                        column: x => x.payment_request_id,
                        principalTable: "subscription_payment_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_payment_ledger_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subscription_refund_adjustments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_request_id = table.Column<long>(type: "bigint", nullable: true),
                    visit_schedule_id = table.Column<long>(type: "bigint", nullable: true),
                    type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    approved_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_refund_adjustments", x => x.id);
                    table.CheckConstraint("CK_subscription_refund_adjustments_amount", "[requested_amount] > 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)");
                    table.CheckConstraint("CK_subscription_refund_adjustments_status", "[status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')");
                    table.CheckConstraint("CK_subscription_refund_adjustments_type", "[type_code] IN ('REFUND','ADJUSTMENT')");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_subscription_contracts_subscription_contract_id",
                        column: x => x.subscription_contract_id,
                        principalTable: "subscription_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_subscription_payment_requests_payment_request_id",
                        column: x => x.payment_request_id,
                        principalTable: "subscription_payment_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_subscription_visit_schedules_visit_schedule_id",
                        column: x => x.visit_schedule_id,
                        principalTable: "subscription_visit_schedules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_refund_adjustments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_approved_by_user_id",
                table: "monthly_settlements",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_idempotency_key",
                table: "monthly_settlements",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_provider_profile_id_settlement_year_settlement_month",
                table: "monthly_settlements",
                columns: new[] { "provider_profile_id", "settlement_year", "settlement_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_public_id",
                table: "monthly_settlements",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_status_code_settlement_year_settlement_month",
                table: "monthly_settlements",
                columns: new[] { "status_code", "settlement_year", "settlement_month" });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_settlements_updated_by_user_id",
                table: "monthly_settlements",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_ledger_idempotency_key",
                table: "subscription_payment_ledger",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_ledger_payment_request_id",
                table: "subscription_payment_ledger",
                column: "payment_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_ledger_processed_by_user_id",
                table: "subscription_payment_ledger",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_ledger_public_id",
                table: "subscription_payment_ledger",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_ledger_subscription_contract_id_occurred_at",
                table: "subscription_payment_ledger",
                columns: new[] { "subscription_contract_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_methods_created_by_user_id",
                table: "subscription_payment_methods",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_methods_customer_profile_id_is_default",
                table: "subscription_payment_methods",
                columns: new[] { "customer_profile_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_methods_customer_profile_id_status_code",
                table: "subscription_payment_methods",
                columns: new[] { "customer_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_methods_public_id",
                table: "subscription_payment_methods",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_methods_updated_by_user_id",
                table: "subscription_payment_methods",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_created_by_user_id",
                table: "subscription_payment_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_customer_profile_id",
                table: "subscription_payment_requests",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_idempotency_key",
                table: "subscription_payment_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_payment_method_id",
                table: "subscription_payment_requests",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_public_id",
                table: "subscription_payment_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_status_code_requested_at",
                table: "subscription_payment_requests",
                columns: new[] { "status_code", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_subscription_contract_id_billing_period_start_billing_period_end",
                table: "subscription_payment_requests",
                columns: new[] { "subscription_contract_id", "billing_period_start", "billing_period_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_requests_updated_by_user_id",
                table: "subscription_payment_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payout_events_actor_user_id",
                table: "subscription_payout_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payout_events_idempotency_key",
                table: "subscription_payout_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payout_events_public_id",
                table: "subscription_payout_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payout_events_subscription_payout_id_occurred_at",
                table: "subscription_payout_events",
                columns: new[] { "subscription_payout_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_created_by_user_id",
                table: "subscription_payouts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_idempotency_key",
                table: "subscription_payouts",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_monthly_settlement_id",
                table: "subscription_payouts",
                column: "monthly_settlement_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_provider_profile_id",
                table: "subscription_payouts",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_public_id",
                table: "subscription_payouts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payouts_updated_by_user_id",
                table: "subscription_payouts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_created_by_user_id",
                table: "subscription_refund_adjustments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_idempotency_key",
                table: "subscription_refund_adjustments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_payment_request_id",
                table: "subscription_refund_adjustments",
                column: "payment_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_processed_by_user_id",
                table: "subscription_refund_adjustments",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_public_id",
                table: "subscription_refund_adjustments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_subscription_contract_id_status_code",
                table: "subscription_refund_adjustments",
                columns: new[] { "subscription_contract_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_updated_by_user_id",
                table: "subscription_refund_adjustments",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_refund_adjustments_visit_schedule_id",
                table: "subscription_refund_adjustments",
                column: "visit_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_created_by_user_id",
                table: "subscription_settlement_items",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_idempotency_key",
                table: "subscription_settlement_items",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_monthly_settlement_id",
                table: "subscription_settlement_items",
                column: "monthly_settlement_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_provider_profile_id_settlement_month_status_code",
                table: "subscription_settlement_items",
                columns: new[] { "provider_profile_id", "settlement_month", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_public_id",
                table: "subscription_settlement_items",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_subscription_contract_id",
                table: "subscription_settlement_items",
                column: "subscription_contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_subscription_visit_schedule_id",
                table: "subscription_settlement_items",
                column: "subscription_visit_schedule_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_settlement_items_updated_by_user_id",
                table: "subscription_settlement_items",
                column: "updated_by_user_id");

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_subscription_payment_ledger_append_only]
                ON [subscription_payment_ledger]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, N'구독 결제 원장은 수정하거나 삭제할 수 없습니다.', 1;
                END
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_subscription_payout_events_append_only]
                ON [subscription_payout_events]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51001, N'구독 지급 이력은 수정하거나 삭제할 수 없습니다.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_payment_ledger");

            migrationBuilder.DropTable(
                name: "subscription_payout_events");

            migrationBuilder.DropTable(
                name: "subscription_refund_adjustments");

            migrationBuilder.DropTable(
                name: "subscription_settlement_items");

            migrationBuilder.DropTable(
                name: "subscription_payouts");

            migrationBuilder.DropTable(
                name: "subscription_payment_requests");

            migrationBuilder.DropTable(
                name: "monthly_settlements");

            migrationBuilder.DropTable(
                name: "subscription_payment_methods");

            migrationBuilder.DropColumn(
                name: "billing_status_code",
                table: "subscription_contracts");

            migrationBuilder.DropColumn(
                name: "next_billing_at",
                table: "subscription_contracts");
        }
    }
}
