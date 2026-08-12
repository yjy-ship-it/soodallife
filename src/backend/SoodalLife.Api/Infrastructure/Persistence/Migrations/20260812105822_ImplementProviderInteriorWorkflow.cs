using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProviderInteriorWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interior_contract_change_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_change_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_contract_change_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_contract_change_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contract_change_files_interior_contract_changes_contract_change_id",
                        column: x => x.contract_change_id,
                        principalTable: "interior_contract_changes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_contract_change_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_stage_inspection_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    stage_inspection_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_stage_inspection_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_files_interior_stage_inspections_stage_inspection_id",
                        column: x => x.stage_inspection_id,
                        principalTable: "interior_stage_inspections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_stage_inspection_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interior_work_stage_assignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    project_participant_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interior_work_stage_assignments", x => x.id);
                    table.CheckConstraint("CK_interior_work_stage_assignments_period", "[effective_to] IS NULL OR [effective_to] >= [effective_from]");
                    table.CheckConstraint("CK_interior_work_stage_assignments_status", "[status_code] IN ('ACTIVE','ENDED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_interior_work_stage_assignments_interior_project_participants_project_participant_id",
                        column: x => x.project_participant_id,
                        principalTable: "interior_project_participants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_stage_assignments_interior_work_stages_work_stage_id",
                        column: x => x.work_stage_id,
                        principalTable: "interior_work_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_stage_assignments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interior_work_stage_assignments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_change_files_contract_change_id_file_id",
                table: "interior_contract_change_files",
                columns: new[] { "contract_change_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_change_files_created_by_user_id",
                table: "interior_contract_change_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_contract_change_files_file_id",
                table: "interior_contract_change_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_files_created_by_user_id",
                table: "interior_stage_inspection_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_files_file_id",
                table: "interior_stage_inspection_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_stage_inspection_files_stage_inspection_id_file_id",
                table: "interior_stage_inspection_files",
                columns: new[] { "stage_inspection_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stage_assignments_created_by_user_id",
                table: "interior_work_stage_assignments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stage_assignments_project_participant_id_status_code_effective_to",
                table: "interior_work_stage_assignments",
                columns: new[] { "project_participant_id", "status_code", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stage_assignments_public_id",
                table: "interior_work_stage_assignments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stage_assignments_updated_by_user_id",
                table: "interior_work_stage_assignments",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interior_work_stage_assignments_work_stage_id_project_participant_id",
                table: "interior_work_stage_assignments",
                columns: new[] { "work_stage_id", "project_participant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interior_contract_change_files");

            migrationBuilder.DropTable(
                name: "interior_stage_inspection_files");

            migrationBuilder.DropTable(
                name: "interior_work_stage_assignments");
        }
    }
}
