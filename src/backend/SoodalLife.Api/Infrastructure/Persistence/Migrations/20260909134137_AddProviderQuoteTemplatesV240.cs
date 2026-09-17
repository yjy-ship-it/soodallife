using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderQuoteTemplatesV240 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_quote_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estimated_duration_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    vat_mode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "EXCLUDED"),
                    items_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_quote_templates", x => x.id);
                    table.CheckConstraint("CK_provider_quote_templates_vat_mode", "[vat_mode] IN ('INCLUDED','EXCLUDED')");
                    table.ForeignKey(
                        name: "FK_provider_quote_templates_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_quote_templates_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_quote_templates_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_quote_templates_created_by_user_id",
                table: "provider_quote_templates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_quote_templates_provider_profile_id",
                table: "provider_quote_templates",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_quote_templates_provider_profile_id_name",
                table: "provider_quote_templates",
                columns: new[] { "provider_profile_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_quote_templates_public_id",
                table: "provider_quote_templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_quote_templates_updated_by_user_id",
                table: "provider_quote_templates",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_quote_templates");
        }
    }
}
