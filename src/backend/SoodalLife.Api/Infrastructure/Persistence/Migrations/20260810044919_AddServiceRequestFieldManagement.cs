using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestFieldManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unit_text",
                table: "category_field_definitions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "category_field_assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "category_field_assignments",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "category_field_options",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    field_definition_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    label = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_options", x => x.id);
                    table.CheckConstraint("CK_category_field_options_display_order", "[display_order] >= 0");
                    table.ForeignKey(
                        name: "FK_category_field_options_category_field_definitions_field_definition_id",
                        column: x => x.field_definition_id,
                        principalTable: "category_field_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_options_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_options_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT source_field_id FROM category_field_definitions
                    GROUP BY source_field_id HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate source_field_id prevents request-field migration.', 1;

                IF EXISTS (
                    SELECT field_definition_id, target_category_id FROM category_field_assignments
                    GROUP BY field_definition_id, target_category_id HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate field assignment prevents request-field migration.', 1;

                IF EXISTS (
                    SELECT 1 FROM category_field_assignments a
                    LEFT JOIN category_field_definitions f ON f.id = a.field_definition_id
                    LEFT JOIN service_categories c ON c.id = a.target_category_id
                    WHERE f.id IS NULL OR c.id IS NULL)
                    THROW 51000, 'Invalid assignment foreign key prevents request-field migration.', 1;

                CREATE TABLE #parsed_options (
                    field_definition_id bigint NOT NULL,
                    option_value nvarchar(2000) NOT NULL,
                    display_order int NOT NULL
                );

                ;WITH option_parts AS (
                    SELECT id AS field_definition_id,
                           CAST(options_or_unit_text AS nvarchar(2000)) AS raw_value,
                           1 AS display_order,
                           1 AS start_position,
                           CHARINDEX('/', options_or_unit_text) AS slash_position
                    FROM category_field_definitions
                    WHERE field_type_code = 'SELECT' AND NULLIF(LTRIM(RTRIM(options_or_unit_text)), '') IS NOT NULL
                    UNION ALL
                    SELECT field_definition_id, raw_value, display_order + 1, slash_position + 1,
                           CHARINDEX('/', raw_value, slash_position + 1)
                    FROM option_parts
                    WHERE slash_position > 0
                )
                INSERT INTO #parsed_options (field_definition_id, option_value, display_order)
                SELECT field_definition_id,
                       LTRIM(RTRIM(SUBSTRING(raw_value, start_position,
                           CASE WHEN slash_position = 0 THEN LEN(raw_value) + 1 ELSE slash_position END - start_position))),
                       display_order
                FROM option_parts
                OPTION (MAXRECURSION 0);

                IF EXISTS (SELECT 1 FROM #parsed_options WHERE option_value = '')
                    THROW 51000, 'Empty SELECT option prevents request-field migration.', 1;
                IF EXISTS (SELECT 1 FROM #parsed_options WHERE LEN(option_value) > 450)
                    THROW 51000, 'SELECT option longer than 450 characters prevents request-field migration.', 1;
                IF EXISTS (
                    SELECT field_definition_id, option_value FROM #parsed_options
                    GROUP BY field_definition_id, option_value HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate SELECT option prevents request-field migration.', 1;

                UPDATE a
                SET a.is_required = f.is_required,
                    a.display_order = f.display_order
                FROM category_field_assignments a
                INNER JOIN category_field_definitions f ON f.id = a.field_definition_id;

                UPDATE category_field_definitions
                SET unit_text = options_or_unit_text
                WHERE field_type_code <> 'SELECT';

                INSERT INTO category_field_options
                    (public_id, field_definition_id, value, label, display_order, is_active, created_at, updated_at)
                SELECT NEWID(), field_definition_id, option_value, option_value, display_order, 1,
                       SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM #parsed_options;

                IF EXISTS (SELECT 1 FROM category_field_assignments WHERE is_required IS NULL OR display_order IS NULL)
                    THROW 51000, 'Assignment backfill validation failed.', 1;
                IF (SELECT COUNT(*) FROM category_field_options) <> (SELECT COUNT(*) FROM #parsed_options)
                    THROW 51000, 'SELECT option conversion validation failed.', 1;
                IF (SELECT COUNT(*) FROM category_field_definitions
                    WHERE source_field_id IN (
                        'FLD-00046','FLD-00057','FLD-00088','FLD-00119','FLD-00190','FLD-00201',
                        'FLD-00212','FLD-00223','FLD-00234','FLD-00245','FLD-00256','FLD-00267',
                        'FLD-00278','FLD-00289','FLD-00300','FLD-00311','FLD-00362','FLD-00664')
                    AND field_type_code = 'TEXT'
                    AND NULLIF(LTRIM(RTRIM(options_or_unit_text)), '') IS NULL) <> 18
                    THROW 51000, 'The 18 approved empty-option exceptions were not preserved.', 1;

                DROP TABLE #parsed_options;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "display_order",
                table: "category_field_assignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_required",
                table: "category_field_assignments",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_target_category_id_is_active_display_order",
                table: "category_field_assignments",
                columns: new[] { "target_category_id", "is_active", "display_order" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_category_field_assignments_display_order",
                table: "category_field_assignments",
                sql: "[display_order] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_created_by_user_id",
                table: "category_field_options",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_field_definition_id_display_order",
                table: "category_field_options",
                columns: new[] { "field_definition_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_field_definition_id_is_active_display_order",
                table: "category_field_options",
                columns: new[] { "field_definition_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_field_definition_id_value",
                table: "category_field_options",
                columns: new[] { "field_definition_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_public_id",
                table: "category_field_options",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_options_updated_by_user_id",
                table: "category_field_options",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_field_options");

            migrationBuilder.DropIndex(
                name: "IX_category_field_assignments_target_category_id_is_active_display_order",
                table: "category_field_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_category_field_assignments_display_order",
                table: "category_field_assignments");

            migrationBuilder.DropColumn(
                name: "unit_text",
                table: "category_field_definitions");

            migrationBuilder.DropColumn(
                name: "display_order",
                table: "category_field_assignments");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "category_field_assignments");
        }
    }
}
