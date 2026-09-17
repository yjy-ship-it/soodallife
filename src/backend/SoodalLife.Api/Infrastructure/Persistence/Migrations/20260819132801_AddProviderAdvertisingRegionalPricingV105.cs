using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAdvertisingRegionalPricingV105 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_rate_amount",
                table: "provider_advertising_rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_app_amount",
                table: "provider_advertising_applications");

            migrationBuilder.AddColumn<decimal>(
                name: "district_unit_amount",
                table: "provider_advertising_rate_policies",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 2000m);

            migrationBuilder.AddColumn<decimal>(
                name: "province_unit_amount",
                table: "provider_advertising_rate_policies",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 10000m);

            migrationBuilder.AddColumn<decimal>(
                name: "regional_fee_cap_amount",
                table: "provider_advertising_rate_policies",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 30000m);

            migrationBuilder.AddColumn<decimal>(
                name: "base_fee_amount",
                table: "provider_advertising_applications",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "district_target_count",
                table: "provider_advertising_applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "province_target_count",
                table: "provider_advertising_applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "regional_fee_amount",
                table: "provider_advertising_applications",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE [provider_advertising_applications] SET [base_fee_amount] = [fee_amount] WHERE [base_fee_amount] = 0");

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 4L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 5L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.UpdateData(
                table: "provider_advertising_rate_policies",
                keyColumn: "id",
                keyValue: 6L,
                columns: new[] { "district_unit_amount", "province_unit_amount", "regional_fee_cap_amount" },
                values: new object[] { 2000m, 10000m, 30000m });

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_rate_amount",
                table: "provider_advertising_rate_policies",
                sql: "[fixed_amount] > 0 AND [province_unit_amount] >= 0 AND [district_unit_amount] >= 0 AND [regional_fee_cap_amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_app_amount",
                table: "provider_advertising_applications",
                sql: "[fee_amount] > 0 AND [base_fee_amount] >= 0 AND [regional_fee_amount] >= 0 AND [province_target_count] >= 0 AND [district_target_count] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_rate_amount",
                table: "provider_advertising_rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_app_amount",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "district_unit_amount",
                table: "provider_advertising_rate_policies");

            migrationBuilder.DropColumn(
                name: "province_unit_amount",
                table: "provider_advertising_rate_policies");

            migrationBuilder.DropColumn(
                name: "regional_fee_cap_amount",
                table: "provider_advertising_rate_policies");

            migrationBuilder.DropColumn(
                name: "base_fee_amount",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "district_target_count",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "province_target_count",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "regional_fee_amount",
                table: "provider_advertising_applications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_rate_amount",
                table: "provider_advertising_rate_policies",
                sql: "[fixed_amount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_app_amount",
                table: "provider_advertising_applications",
                sql: "[fee_amount] > 0");
        }
    }
}
