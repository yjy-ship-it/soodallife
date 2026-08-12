using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProviderAppOnboardingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "business_address",
                table: "provider_profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "business_address_encrypted",
                table: "provider_profiles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_item_text",
                table: "provider_profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_type_text",
                table: "provider_profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                table: "provider_profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "introduction",
                table: "provider_profiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "privacy_protection_version",
                table: "provider_profiles",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "representative_name",
                table: "provider_profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "business_address",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "business_address_encrypted",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "business_item_text",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "business_type_text",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "contact_name",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "introduction",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "privacy_protection_version",
                table: "provider_profiles");

            migrationBuilder.DropColumn(
                name: "representative_name",
                table: "provider_profiles");
        }
    }
}
