using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceFieldIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_field_key",
                table: "category_field_definitions");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_field_key",
                table: "category_field_definitions",
                columns: new[] { "owner_middle_category_id", "field_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_field_key",
                table: "category_field_definitions");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_field_key",
                table: "category_field_definitions",
                columns: new[] { "owner_middle_category_id", "field_key" },
                unique: true);
        }
    }
}
