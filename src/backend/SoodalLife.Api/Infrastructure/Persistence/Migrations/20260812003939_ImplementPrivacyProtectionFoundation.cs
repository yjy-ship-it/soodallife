using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementPrivacyProtectionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "email_encrypted",
                table: "users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "email_search_hash",
                table: "users",
                type: "binary(32)",
                fixedLength: true,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "phone_encrypted",
                table: "users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "phone_search_hash",
                table: "users",
                type: "binary(32)",
                fixedLength: true,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "privacy_protection_version",
                table: "users",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "detail_address_encrypted",
                table: "subscription_requests",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "privacy_protection_version",
                table: "subscription_requests",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "detail_address_encrypted",
                table: "service_requests",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "privacy_protection_version",
                table: "service_requests",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "detail_address_encrypted",
                table: "customer_addresses",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "privacy_protection_version",
                table: "customer_addresses",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "recipient_name_encrypted",
                table: "customer_addresses",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "road_address_encrypted",
                table: "customer_addresses",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email_search_hash",
                table: "users",
                column: "email_search_hash",
                unique: true,
                filter: "[email_search_hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_phone_search_hash",
                table: "users",
                column: "phone_search_hash",
                filter: "[phone_search_hash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email_search_hash",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_phone_search_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_encrypted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_search_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_encrypted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_search_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "privacy_protection_version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "detail_address_encrypted",
                table: "subscription_requests");

            migrationBuilder.DropColumn(
                name: "privacy_protection_version",
                table: "subscription_requests");

            migrationBuilder.DropColumn(
                name: "detail_address_encrypted",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "privacy_protection_version",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "detail_address_encrypted",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "privacy_protection_version",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "recipient_name_encrypted",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "road_address_encrypted",
                table: "customer_addresses");
        }
    }
}
