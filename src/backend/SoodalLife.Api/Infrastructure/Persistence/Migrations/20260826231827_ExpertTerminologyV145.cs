using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpertTerminologyV145 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "advertising_placements",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "description", "name" },
                values: new object[] { "전문가 역할 홈 화면", "전문가 홈" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "advertising_placements",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "description", "name" },
                values: new object[] { "공급자 역할 홈 화면", "공급자 홈" });
        }
    }
}
