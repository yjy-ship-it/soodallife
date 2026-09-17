using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRequestAbuseProtectionV155 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "abuse_count_excluded",
                table: "service_requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "abuse_exclusion_reason",
                table: "service_requests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "abuse_fingerprint",
                table: "service_requests",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "abuse_policy_version",
                table: "service_requests",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "customer_quotes_viewed_at",
                table: "service_requests",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_profile_id_abuse_fingerprint_opened_at",
                table: "service_requests",
                columns: new[] { "customer_profile_id", "abuse_fingerprint", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_profile_id_category_id_opened_at",
                table: "service_requests",
                columns: new[] { "customer_profile_id", "category_id", "opened_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_requests_customer_profile_id_abuse_fingerprint_opened_at",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_customer_profile_id_category_id_opened_at",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "abuse_count_excluded",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "abuse_exclusion_reason",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "abuse_fingerprint",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "abuse_policy_version",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "customer_quotes_viewed_at",
                table: "service_requests");
        }
    }
}
