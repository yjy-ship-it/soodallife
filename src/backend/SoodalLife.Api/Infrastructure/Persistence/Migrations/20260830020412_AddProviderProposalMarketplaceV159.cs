using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderProposalMarketplaceV159 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_proposal_area_interests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    source_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_proposal_area_interests", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_proposal_area_interests_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_proposal_area_interests_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_proposal_category_interests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    source_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_proposal_category_interests", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_proposal_category_interests_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_proposal_category_interests_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_proposal_signals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    signal_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_proposal_signals", x => x.id);
                    table.CheckConstraint("CK_customer_proposal_signal_type", "[signal_type_code] IN ('SERVICE_DETAIL','SERVICE_SEARCH','SESSION_CATEGORY')");
                    table.ForeignKey(
                        name: "FK_customer_proposal_signals_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_proposal_signals_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_proposal_campaigns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    proposal_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    scope_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    normal_price_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    offer_price_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    minimum_participants = table.Column<int>(type: "int", nullable: false),
                    maximum_participants = table.Column<int>(type: "int", nullable: false),
                    confirmed_participants = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    service_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    minimum_failure_policy_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    cancellation_policy_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    fee_per_participant = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    reserved_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    captured_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    fee_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    reserve_ledger_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    release_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    published_notification_queued_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    midpoint_notification_queued_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    closing_notification_queued_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_proposal_campaigns", x => x.id);
                    table.CheckConstraint("CK_provider_proposal_amounts", "[offer_price_amount] >= 0 AND ([normal_price_amount] IS NULL OR [normal_price_amount] >= [offer_price_amount]) AND [fee_per_participant] > 0 AND [reserved_fee_amount] >= 0 AND [captured_fee_amount] >= 0");
                    table.CheckConstraint("CK_provider_proposal_fee_status", "[fee_status_code] IN ('RESERVED','PARTIALLY_CAPTURED','CAPTURED','RELEASED','RESTORED')");
                    table.CheckConstraint("CK_provider_proposal_participants", "[minimum_participants] > 0 AND [maximum_participants] >= [minimum_participants] AND [confirmed_participants] >= 0 AND [confirmed_participants] <= [maximum_participants]");
                    table.CheckConstraint("CK_provider_proposal_period", "[end_at] > [start_at]");
                    table.CheckConstraint("CK_provider_proposal_scope", "[scope_code] IN ('LOCAL','NATIONWIDE')");
                    table.CheckConstraint("CK_provider_proposal_status", "[status_code] IN ('PUBLISHED','MINIMUM_MET','FULL','EXPIRED','CANCELLED')");
                    table.CheckConstraint("CK_provider_proposal_type", "[proposal_type_code] IN ('DISCOUNT_SERVICE','GROUP_BUY','GROUP_LESSON')");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_wallet_ledger_release_ledger_entry_id",
                        column: x => x.release_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_wallet_ledger_reserve_ledger_entry_id",
                        column: x => x.reserve_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_campaigns_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_proposal_applications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    captured_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    capture_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    applied_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    declined_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_proposal_applications", x => x.id);
                    table.CheckConstraint("CK_provider_proposal_application_status", "[status_code] IN ('APPLIED','CONFIRMED','CANCELLED','DECLINED')");
                    table.ForeignKey(
                        name: "FK_provider_proposal_applications_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_applications_provider_proposal_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "provider_proposal_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_applications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_applications_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_applications_wallet_ledger_capture_ledger_entry_id",
                        column: x => x.capture_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_proposal_areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_proposal_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_proposal_areas_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_areas_provider_proposal_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "provider_proposal_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_proposal_areas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_area_interests_administrative_area_id_is_active",
                table: "customer_proposal_area_interests",
                columns: new[] { "administrative_area_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_area_interests_customer_profile_id_administrative_area_id",
                table: "customer_proposal_area_interests",
                columns: new[] { "customer_profile_id", "administrative_area_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_category_interests_category_id_is_active",
                table: "customer_proposal_category_interests",
                columns: new[] { "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_category_interests_customer_profile_id_category_id",
                table: "customer_proposal_category_interests",
                columns: new[] { "customer_profile_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_signals_category_id",
                table: "customer_proposal_signals",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_proposal_signals_customer_profile_id_category_id_expires_at",
                table: "customer_proposal_signals",
                columns: new[] { "customer_profile_id", "category_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_campaign_id_customer_profile_id",
                table: "provider_proposal_applications",
                columns: new[] { "campaign_id", "customer_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_campaign_id_status_code",
                table: "provider_proposal_applications",
                columns: new[] { "campaign_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_capture_ledger_entry_id",
                table: "provider_proposal_applications",
                column: "capture_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_created_by_user_id",
                table: "provider_proposal_applications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_customer_profile_id",
                table: "provider_proposal_applications",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_public_id",
                table: "provider_proposal_applications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_applications_updated_by_user_id",
                table: "provider_proposal_applications",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_areas_administrative_area_id",
                table: "provider_proposal_areas",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_areas_campaign_id_administrative_area_id",
                table: "provider_proposal_areas",
                columns: new[] { "campaign_id", "administrative_area_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_areas_created_by_user_id",
                table: "provider_proposal_areas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_created_by_user_id",
                table: "provider_proposal_campaigns",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_provider_profile_id_status_code",
                table: "provider_proposal_campaigns",
                columns: new[] { "provider_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_provider_service_category_id_status_code",
                table: "provider_proposal_campaigns",
                columns: new[] { "provider_service_category_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_public_id",
                table: "provider_proposal_campaigns",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_release_ledger_entry_id",
                table: "provider_proposal_campaigns",
                column: "release_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_reserve_ledger_entry_id",
                table: "provider_proposal_campaigns",
                column: "reserve_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_status_code_start_at_end_at",
                table: "provider_proposal_campaigns",
                columns: new[] { "status_code", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_updated_by_user_id",
                table: "provider_proposal_campaigns",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_proposal_campaigns_wallet_id",
                table: "provider_proposal_campaigns",
                column: "wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_proposal_area_interests");

            migrationBuilder.DropTable(
                name: "customer_proposal_category_interests");

            migrationBuilder.DropTable(
                name: "customer_proposal_signals");

            migrationBuilder.DropTable(
                name: "provider_proposal_applications");

            migrationBuilder.DropTable(
                name: "provider_proposal_areas");

            migrationBuilder.DropTable(
                name: "provider_proposal_campaigns");
        }
    }
}
