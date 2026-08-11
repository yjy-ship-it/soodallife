using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCustomerRequestFilesAndQuoteComparison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "administrative_area_id",
                table: "service_requests",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "service_request_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    request_field_id = table.Column<long>(type: "bigint", nullable: true),
                    purpose_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "REQUEST_REFERENCE"),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_files", x => x.id);
                    table.CheckConstraint("CK_service_request_files_purpose", "[purpose_code] IN ('REQUEST_REFERENCE','DYNAMIC_FIELD')");
                    table.ForeignKey(
                        name: "FK_service_request_files_category_field_definitions_request_field_id",
                        column: x => x.request_field_id,
                        principalTable: "category_field_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_request_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_request_files_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_request_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_created_by_user_id",
                table: "service_request_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_file_id",
                table: "service_request_files",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_public_id",
                table: "service_request_files",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_request_field_id",
                table: "service_request_files",
                column: "request_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_service_request_id",
                table: "service_request_files",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_service_request_id_display_order",
                table: "service_request_files",
                columns: new[] { "service_request_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_files_service_request_id_file_id",
                table: "service_request_files",
                columns: new[] { "service_request_id", "file_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_request_files");

            migrationBuilder.AlterColumn<long>(
                name: "administrative_area_id",
                table: "service_requests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
