using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementFilePrivacyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "malware_scan_status_code",
                table: "files",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_adapter_version",
                table: "files",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_detection_types_json",
                table: "files",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "privacy_inspected_at",
                table: "files",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_inspection_error_code",
                table: "files",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_inspection_status_code",
                table: "files",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sanitization_completed_at",
                table: "files",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sanitization_status_code",
                table: "files",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "file_derivatives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    original_file_id = table.Column<long>(type: "bigint", nullable: false),
                    derived_file_id = table.Column<long>(type: "bigint", nullable: false),
                    derivative_type_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    adapter_version = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_derivatives", x => x.id);
                    table.CheckConstraint("CK_file_derivatives_distinct", "[original_file_id] <> [derived_file_id]");
                    table.CheckConstraint("CK_file_derivatives_type", "[derivative_type_code] IN ('PRIVACY_SANITIZED')");
                    table.ForeignKey(
                        name: "FK_file_derivatives_files_derived_file_id",
                        column: x => x.derived_file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_file_derivatives_files_original_file_id",
                        column: x => x.original_file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_file_derivatives_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_files_malware_scan_status_code",
                table: "files",
                column: "malware_scan_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_files_privacy_inspection_status_code",
                table: "files",
                column: "privacy_inspection_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_files_sanitization_status_code",
                table: "files",
                column: "sanitization_status_code");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_malware_scan_status",
                table: "files",
                sql: "[malware_scan_status_code] IS NULL OR [malware_scan_status_code] IN ('NOT_INTEGRATED','PENDING','PROCESSING','CLEAN','INFECTED','FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_privacy_inspection_status",
                table: "files",
                sql: "[privacy_inspection_status_code] IS NULL OR [privacy_inspection_status_code] IN ('NOT_INTEGRATED','PENDING','PROCESSING','SAFE','SENSITIVE_DETECTED','FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_sanitization_status",
                table: "files",
                sql: "[sanitization_status_code] IS NULL OR [sanitization_status_code] IN ('NOT_INTEGRATED','NOT_REQUIRED','PENDING','PROCESSING','COMPLETED','FAILED')");

            migrationBuilder.CreateIndex(
                name: "IX_file_derivatives_created_by_user_id",
                table: "file_derivatives",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_derivatives_derived_file_id",
                table: "file_derivatives",
                column: "derived_file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_derivatives_original_file_id",
                table: "file_derivatives",
                column: "original_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_derivatives_original_file_id_derivative_type_code",
                table: "file_derivatives",
                columns: new[] { "original_file_id", "derivative_type_code" });

            migrationBuilder.CreateIndex(
                name: "IX_file_derivatives_public_id",
                table: "file_derivatives",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_derivatives");

            migrationBuilder.DropIndex(
                name: "IX_files_malware_scan_status_code",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_files_privacy_inspection_status_code",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_files_sanitization_status_code",
                table: "files");

            migrationBuilder.DropCheckConstraint(
                name: "CK_files_malware_scan_status",
                table: "files");

            migrationBuilder.DropCheckConstraint(
                name: "CK_files_privacy_inspection_status",
                table: "files");

            migrationBuilder.DropCheckConstraint(
                name: "CK_files_sanitization_status",
                table: "files");

            migrationBuilder.DropColumn(
                name: "malware_scan_status_code",
                table: "files");

            migrationBuilder.DropColumn(
                name: "privacy_adapter_version",
                table: "files");

            migrationBuilder.DropColumn(
                name: "privacy_detection_types_json",
                table: "files");

            migrationBuilder.DropColumn(
                name: "privacy_inspected_at",
                table: "files");

            migrationBuilder.DropColumn(
                name: "privacy_inspection_error_code",
                table: "files");

            migrationBuilder.DropColumn(
                name: "privacy_inspection_status_code",
                table: "files");

            migrationBuilder.DropColumn(
                name: "sanitization_completed_at",
                table: "files");

            migrationBuilder.DropColumn(
                name: "sanitization_status_code",
                table: "files");
        }
    }
}
