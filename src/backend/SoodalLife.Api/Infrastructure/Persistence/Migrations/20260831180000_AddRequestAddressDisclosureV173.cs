using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260831180000_AddRequestAddressDisclosureV173")]
public sealed class AddRequestAddressDisclosureV173 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "detail_address_disclosure_code",
            table: "service_requests",
            type: "varchar(30)",
            unicode: false,
            maxLength: 30,
            nullable: false,
            defaultValue: "AFTER_SELECTION");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "detail_address_disclosure_code", table: "service_requests");
}
