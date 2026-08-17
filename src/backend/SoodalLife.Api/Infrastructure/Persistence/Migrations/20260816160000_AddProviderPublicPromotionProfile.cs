using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260816160000_AddProviderPublicPromotionProfile")]
public partial class AddProviderPublicPromotionProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "public_introduction_html", table: "provider_profiles", type: "nvarchar(max)", maxLength: 8000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_phone", table: "provider_profiles", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_email", table: "provider_profiles", type: "nvarchar(320)", maxLength: 320, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_address", table: "provider_profiles", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_blog_url", table: "provider_profiles", type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_website_url", table: "provider_profiles", type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_logo_url", table: "provider_profiles", type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "public_photo_urls_json", table: "provider_profiles", type: "nvarchar(max)", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "public_introduction_html", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_phone", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_email", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_address", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_blog_url", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_website_url", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_logo_url", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "public_photo_urls_json", table: "provider_profiles");
    }
}
