using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCustomerAccountProfileAndConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email_verification_status_code",
                table: "users",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_INTEGRATED");

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_verification_status_code",
                table: "users",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_INTEGRATED");

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    address_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    recipient_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    road_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    detail_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_addresses_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_addresses_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_addresses_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_withdrawal_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    scope_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "REQUESTED"),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_withdrawal_requests", x => x.id);
                    table.CheckConstraint("CK_customer_withdrawal_scope", "[scope_code] IN ('CUSTOMER_ROLE','ACCOUNT')");
                    table.CheckConstraint("CK_customer_withdrawal_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_customer_withdrawal_requests_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_withdrawal_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "legal_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    audience_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "CUSTOMER"),
                    requirement_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_placeholder = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_documents", x => x.id);
                    table.CheckConstraint("CK_legal_documents_audience", "[audience_code] IN ('CUSTOMER','PROVIDER','ALL')");
                    table.CheckConstraint("CK_legal_documents_requirement", "[requirement_code] IN ('REQUIRED','OPTIONAL','NOTICE')");
                    table.ForeignKey(
                        name: "FK_legal_documents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_legal_documents_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "password_reset_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    requested_identifier_hash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    requested_identifier_masked = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    token_hash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    delivery_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "NOT_INTEGRATED"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_requests", x => x.id);
                    table.CheckConstraint("CK_password_reset_delivery", "[delivery_status_code] IN ('NOT_INTEGRATED','PENDING','SENT','FAILED')");
                    table.ForeignKey(
                        name: "FK_password_reset_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "legal_document_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legal_document_id = table.Column<long>(type: "bigint", nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_placeholder = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_document_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_legal_document_versions_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "legal_documents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_legal_document_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    legal_document_version_id = table.Column<long>(type: "bigint", nullable: false),
                    consent_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    consented_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    source_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ip_address = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_consents", x => x.id);
                    table.CheckConstraint("CK_user_consents_status", "[consent_status_code] IN ('CONSENTED','WITHDRAWN')");
                    table.ForeignKey(
                        name: "FK_user_consents_legal_document_versions_legal_document_version_id",
                        column: x => x.legal_document_version_id,
                        principalTable: "legal_document_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "[normalized_email] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_email_verification",
                table: "users",
                sql: "[email_verification_status_code] IN ('NOT_INTEGRATED','PENDING','VERIFIED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_phone_verification",
                table: "users",
                sql: "[phone_verification_status_code] IN ('NOT_INTEGRATED','PENDING','VERIFIED')");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_administrative_area_id",
                table: "customer_addresses",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_created_by_user_id",
                table: "customer_addresses",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_profile_id",
                table: "customer_addresses",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_profile_id_is_active_created_at",
                table: "customer_addresses",
                columns: new[] { "customer_profile_id", "is_active", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_profile_id_is_default",
                table: "customer_addresses",
                columns: new[] { "customer_profile_id", "is_default" },
                unique: true,
                filter: "[is_active] = 1 AND [is_default] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_public_id",
                table: "customer_addresses",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_updated_by_user_id",
                table: "customer_addresses",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_processed_by_user_id",
                table: "customer_withdrawal_requests",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_public_id",
                table: "customer_withdrawal_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_user_id_scope_code",
                table: "customer_withdrawal_requests",
                columns: new[] { "user_id", "scope_code" },
                unique: true,
                filter: "[status_code] = 'REQUESTED'");

            migrationBuilder.CreateIndex(
                name: "IX_customer_withdrawal_requests_user_id_status_code",
                table: "customer_withdrawal_requests",
                columns: new[] { "user_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_legal_document_versions_created_by_user_id",
                table: "legal_document_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_legal_document_versions_is_active_effective_from_effective_to",
                table: "legal_document_versions",
                columns: new[] { "is_active", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_legal_document_versions_legal_document_id_version_no",
                table: "legal_document_versions",
                columns: new[] { "legal_document_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_document_versions_public_id",
                table: "legal_document_versions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_audience_code_is_active_display_order",
                table: "legal_documents",
                columns: new[] { "audience_code", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_code",
                table: "legal_documents",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_created_by_user_id",
                table: "legal_documents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_public_id",
                table: "legal_documents",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_updated_by_user_id",
                table: "legal_documents",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_requests_expires_at_used_at",
                table: "password_reset_requests",
                columns: new[] { "expires_at", "used_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_requests_public_id",
                table: "password_reset_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_requests_requested_identifier_hash_created_at",
                table: "password_reset_requests",
                columns: new[] { "requested_identifier_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_requests_token_hash",
                table: "password_reset_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_requests_user_id",
                table: "password_reset_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_legal_document_version_id",
                table: "user_consents",
                column: "legal_document_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_user_id_consent_status_code",
                table: "user_consents",
                columns: new[] { "user_id", "consent_status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_user_id_legal_document_version_id",
                table: "user_consents",
                columns: new[] { "user_id", "legal_document_version_id" },
                unique: true);

            migrationBuilder.InsertData(
                table: "legal_documents",
                columns: new[] { "id", "public_id", "code", "audience_code", "requirement_code", "display_order", "is_active", "is_placeholder" },
                values: new object[,]
                {
                    { 1L, new Guid("10000000-0000-4000-8000-000000000001"), "TERMS_OF_SERVICE", "CUSTOMER", "REQUIRED", 10, true, true },
                    { 2L, new Guid("10000000-0000-4000-8000-000000000002"), "PRIVACY_POLICY", "CUSTOMER", "REQUIRED", 20, true, true },
                    { 3L, new Guid("10000000-0000-4000-8000-000000000003"), "MARKETING_CONSENT", "CUSTOMER", "OPTIONAL", 30, true, true },
                });

            migrationBuilder.InsertData(
                table: "legal_document_versions",
                columns: new[] { "id", "public_id", "legal_document_id", "version_no", "title", "content", "effective_from", "is_active", "is_placeholder" },
                values: new object[,]
                {
                    { 1L, new Guid("20000000-0000-4000-8000-000000000001"), 1L, 1, "[개발용] 수달 라이프 이용약관", "개발 및 화면 검증용 Placeholder입니다. 운영 전 검토·승인된 이용약관으로 교체해야 합니다.", new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc), true, true },
                    { 2L, new Guid("20000000-0000-4000-8000-000000000002"), 2L, 1, "[개발용] 개인정보 필수동의", "개발 및 화면 검증용 Placeholder입니다. 운영 전 검토·승인된 개인정보 문서로 교체해야 합니다.", new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc), true, true },
                    { 3L, new Guid("20000000-0000-4000-8000-000000000003"), 3L, 1, "[선택·개발용] 마케팅 수신 동의", "개발 및 화면 검증용 Placeholder입니다. 실제 메시지를 발송하지 않으며 운영 전 승인 문서로 교체해야 합니다.", new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc), true, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_addresses");

            migrationBuilder.DropTable(
                name: "customer_withdrawal_requests");

            migrationBuilder.DropTable(
                name: "password_reset_requests");

            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropTable(
                name: "legal_document_versions");

            migrationBuilder.DropTable(
                name: "legal_documents");

            migrationBuilder.DropIndex(
                name: "IX_users_normalized_email",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_email_verification",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_phone_verification",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_status_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_verification_status_code",
                table: "users");
        }
    }
}
