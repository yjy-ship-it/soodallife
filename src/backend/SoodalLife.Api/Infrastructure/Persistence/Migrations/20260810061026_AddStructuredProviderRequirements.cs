using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredProviderRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "document_type_id",
                table: "provider_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_document_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    supports_expiry = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_document_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_document_types_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_document_types_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_requirement_types",
                columns: table => new
                {
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_requirement_types", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "provider_requirement_definitions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requirement_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    requirement_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_requirement_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_requirement_definitions_provider_requirement_types_requirement_type_code",
                        column: x => x.requirement_type_code,
                        principalTable: "provider_requirement_types",
                        principalColumn: "code");
                    table.ForeignKey(
                        name: "FK_provider_requirement_definitions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_requirement_definitions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_provider_requirement_assignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_operation_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    requirement_definition_id = table.Column<long>(type: "bigint", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    verification_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    expiry_check_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    minimum_valid_days = table.Column<short>(type: "smallint", nullable: true),
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
                    table.PrimaryKey("PK_category_provider_requirement_assignments", x => x.id);
                    table.CheckConstraint("CK_category_provider_requirement_assignments_display_order", "[display_order] >= 0");
                    table.CheckConstraint("CK_category_provider_requirement_assignments_minimum_valid_days", "[minimum_valid_days] IS NULL OR [minimum_valid_days] >= 0");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_assignments_category_operation_policies_category_operation_policy_id",
                        column: x => x.category_operation_policy_id,
                        principalTable: "category_operation_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_assignments_provider_requirement_definitions_requirement_definition_id",
                        column: x => x.requirement_definition_id,
                        principalTable: "provider_requirement_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_assignments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_assignments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_provider_requirement_evidence_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requirement_assignment_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type_id = table.Column<long>(type: "bigint", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_provider_requirement_evidence_types", x => x.id);
                    table.CheckConstraint("CK_category_provider_requirement_evidence_types_display_order", "[display_order] >= 0");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_evidence_types_category_provider_requirement_assignments_requirement_assignment_id",
                        column: x => x.requirement_assignment_id,
                        principalTable: "category_provider_requirement_assignments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_evidence_types_provider_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "provider_document_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_evidence_types_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_provider_requirement_evidence_types_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_service_requirement_verifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    requirement_assignment_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_document_id = table.Column<long>(type: "bigint", nullable: true),
                    verification_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    verified_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_requirement_verifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_category_provider_requirement_assignments_requirement_assignment_id",
                        column: x => x.requirement_assignment_id,
                        principalTable: "category_provider_requirement_assignments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_provider_documents_provider_document_id",
                        column: x => x.provider_document_id,
                        principalTable: "provider_documents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_requirement_verifications_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "provider_requirement_types",
                columns: new[] { "code", "is_active", "name" },
                values: new object[,]
                {
                    { "EVIDENCE_VALIDITY", true, "증빙 유효성" },
                    { "INSURANCE", true, "보험" },
                    { "LICENSE", true, "면허" },
                    { "QUALIFICATION", true, "자격" },
                    { "SAFETY", true, "안전" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_document_type_id",
                table: "provider_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_category_operation_policy_id_is_active_display_order",
                table: "category_provider_requirement_assignments",
                columns: new[] { "category_operation_policy_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_category_operation_policy_id_requirement_definition_id",
                table: "category_provider_requirement_assignments",
                columns: new[] { "category_operation_policy_id", "requirement_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_created_by_user_id",
                table: "category_provider_requirement_assignments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_public_id",
                table: "category_provider_requirement_assignments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_requirement_definition_id",
                table: "category_provider_requirement_assignments",
                column: "requirement_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_assignments_updated_by_user_id",
                table: "category_provider_requirement_assignments",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_created_by_user_id",
                table: "category_provider_requirement_evidence_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_document_type_id",
                table: "category_provider_requirement_evidence_types",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_public_id",
                table: "category_provider_requirement_evidence_types",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_requirement_assignment_id_display_order",
                table: "category_provider_requirement_evidence_types",
                columns: new[] { "requirement_assignment_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_requirement_assignment_id_document_type_id",
                table: "category_provider_requirement_evidence_types",
                columns: new[] { "requirement_assignment_id", "document_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_provider_requirement_evidence_types_updated_by_user_id",
                table: "category_provider_requirement_evidence_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_document_types_code",
                table: "provider_document_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_document_types_created_by_user_id",
                table: "provider_document_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_document_types_is_active_name",
                table: "provider_document_types",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_document_types_public_id",
                table: "provider_document_types",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_document_types_updated_by_user_id",
                table: "provider_document_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_requirement_definitions_created_by_user_id",
                table: "provider_requirement_definitions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_requirement_definitions_public_id",
                table: "provider_requirement_definitions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_requirement_definitions_requirement_code",
                table: "provider_requirement_definitions",
                column: "requirement_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_requirement_definitions_requirement_type_code_is_active_name",
                table: "provider_requirement_definitions",
                columns: new[] { "requirement_type_code", "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_requirement_definitions_updated_by_user_id",
                table: "provider_requirement_definitions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_created_by_user_id",
                table: "provider_service_requirement_verifications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_provider_document_id",
                table: "provider_service_requirement_verifications",
                column: "provider_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_provider_service_category_id_requirement_assignment_id",
                table: "provider_service_requirement_verifications",
                columns: new[] { "provider_service_category_id", "requirement_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_public_id",
                table: "provider_service_requirement_verifications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_requirement_assignment_id",
                table: "provider_service_requirement_verifications",
                column: "requirement_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_updated_by_user_id",
                table: "provider_service_requirement_verifications",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_verification_status_code_expires_at",
                table: "provider_service_requirement_verifications",
                columns: new[] { "verification_status_code", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_requirement_verifications_verified_by_user_id",
                table: "provider_service_requirement_verifications",
                column: "verified_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_provider_documents_provider_document_types_document_type_id",
                table: "provider_documents",
                column: "document_type_id",
                principalTable: "provider_document_types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_provider_documents_provider_document_types_document_type_id",
                table: "provider_documents");

            migrationBuilder.DropTable(
                name: "category_provider_requirement_evidence_types");

            migrationBuilder.DropTable(
                name: "provider_service_requirement_verifications");

            migrationBuilder.DropTable(
                name: "provider_document_types");

            migrationBuilder.DropTable(
                name: "category_provider_requirement_assignments");

            migrationBuilder.DropTable(
                name: "provider_requirement_definitions");

            migrationBuilder.DropTable(
                name: "provider_requirement_types");

            migrationBuilder.DropIndex(
                name: "IX_provider_documents_document_type_id",
                table: "provider_documents");

            migrationBuilder.DropColumn(
                name: "document_type_id",
                table: "provider_documents");
        }
    }
}
