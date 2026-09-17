using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAdvertisingCampaignWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_advertising_rate_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    placement_id = table.Column<long>(type: "bigint", nullable: false),
                    duration_days = table.Column<int>(type: "int", nullable: false),
                    fixed_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_advertising_rate_policies", x => x.id);
                    table.CheckConstraint("CK_provider_ad_rate_amount", "[fixed_amount] > 0");
                    table.CheckConstraint("CK_provider_ad_rate_duration", "[duration_days] IN (7,14,30)");
                    table.CheckConstraint("CK_provider_ad_rate_period", "[effective_to] IS NULL OR [effective_to] >= [effective_from]");
                    table.ForeignKey(
                        name: "FK_provider_advertising_rate_policies_advertising_placements_placement_id",
                        column: x => x.placement_id,
                        principalTable: "advertising_placements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_rate_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_rate_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_advertising_applications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    rate_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    duration_days = table.Column<int>(type: "int", nullable: false),
                    fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    fee_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    reserve_ledger_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    capture_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    release_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    supplement_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    resubmitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_advertising_applications", x => x.id);
                    table.CheckConstraint("CK_provider_ad_app_amount", "[fee_amount] > 0");
                    table.CheckConstraint("CK_provider_ad_app_fee_status", "[fee_status_code] IN ('RESERVED','CAPTURED','RELEASED')");
                    table.CheckConstraint("CK_provider_ad_app_status", "[status_code] IN ('SUBMITTED','REJECTED','RESUBMITTED','APPROVED','PUBLISHED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_advertising_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "advertising_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_provider_advertising_rate_policies_rate_policy_id",
                        column: x => x.rate_policy_id,
                        principalTable: "provider_advertising_rate_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_wallet_ledger_capture_ledger_entry_id",
                        column: x => x.capture_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_wallet_ledger_release_ledger_entry_id",
                        column: x => x.release_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_wallet_ledger_reserve_ledger_entry_id",
                        column: x => x.reserve_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_applications_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "provider_advertising_rate_policies",
                columns: new[] { "id", "created_at", "created_by_user_id", "currency_code", "duration_days", "effective_from", "effective_to", "fixed_amount", "is_active", "placement_id", "public_id", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 7, new DateOnly(2026, 8, 19), null, 35000m, true, 1L, new Guid("11a20000-0000-0000-0000-000000000001"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 14, new DateOnly(2026, 8, 19), null, 60000m, true, 1L, new Guid("11a20000-0000-0000-0000-000000000002"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 30, new DateOnly(2026, 8, 19), null, 110000m, true, 1L, new Guid("11a20000-0000-0000-0000-000000000003"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 7, new DateOnly(2026, 8, 19), null, 21000m, true, 2L, new Guid("11a20000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 5L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 14, new DateOnly(2026, 8, 19), null, 36000m, true, 2L, new Guid("11a20000-0000-0000-0000-000000000005"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 6L, new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 30, new DateOnly(2026, 8, 19), null, 66000m, true, 2L, new Guid("11a20000-0000-0000-0000-000000000006"), new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_campaign_id",
                table: "provider_advertising_applications",
                column: "campaign_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_capture_ledger_entry_id",
                table: "provider_advertising_applications",
                column: "capture_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_created_by_user_id",
                table: "provider_advertising_applications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_provider_profile_id_status_code_submitted_at",
                table: "provider_advertising_applications",
                columns: new[] { "provider_profile_id", "status_code", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_public_id",
                table: "provider_advertising_applications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_rate_policy_id",
                table: "provider_advertising_applications",
                column: "rate_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_release_ledger_entry_id",
                table: "provider_advertising_applications",
                column: "release_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_reserve_ledger_entry_id",
                table: "provider_advertising_applications",
                column: "reserve_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_status_code_updated_at",
                table: "provider_advertising_applications",
                columns: new[] { "status_code", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_updated_by_user_id",
                table: "provider_advertising_applications",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_wallet_id",
                table: "provider_advertising_applications",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_rate_policies_created_by_user_id",
                table: "provider_advertising_rate_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_rate_policies_is_active_effective_from_effective_to",
                table: "provider_advertising_rate_policies",
                columns: new[] { "is_active", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_rate_policies_placement_id_duration_days_effective_from",
                table: "provider_advertising_rate_policies",
                columns: new[] { "placement_id", "duration_days", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_rate_policies_public_id",
                table: "provider_advertising_rate_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_rate_policies_updated_by_user_id",
                table: "provider_advertising_rate_policies",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_advertising_applications");

            migrationBuilder.DropTable(
                name: "provider_advertising_rate_policies");
        }
    }
}
