using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    normalized_login_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    last_login_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("CK_users_status_code", "[status_code] IN ('ACTIVE','SUSPENDED','WITHDRAWN')");
                    table.ForeignKey(
                        name: "FK_users_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "administrative_areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_system_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "MOIS_STANDARD_CODE"),
                    area_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    area_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    area_level_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    parent_area_id = table.Column<long>(type: "bigint", nullable: true),
                    source_parent_area_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    source_created_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source_abolished_date = table.Column<DateOnly>(type: "date", nullable: true),
                    abolition_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrative_areas", x => x.id);
                    table.CheckConstraint("CK_administrative_areas_level", "[area_level_code] IN ('SIDO','SIGUNGU')");
                    table.ForeignKey(
                        name: "FK_administrative_areas_administrative_areas_parent_area_id",
                        column: x => x.parent_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_administrative_areas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_administrative_areas_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    actor_role_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    action_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    entity_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    result_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "SUCCESS"),
                    correlation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ip_address = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    before_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    after_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.CheckConstraint("CK_audit_logs_after_json", "[after_json] IS NULL OR ISJSON([after_json]) = 1");
                    table.CheckConstraint("CK_audit_logs_before_json", "[before_json] IS NULL OR ISJSON([before_json]) = 1");
                    table.CheckConstraint("CK_audit_logs_metadata_json", "[metadata_json] IS NULL OR ISJSON([metadata_json]) = 1");
                    table.CheckConstraint("CK_audit_logs_result", "[result_code] IN ('SUCCESS','FAILURE')");
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "completion_photo_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completion_photo_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_completion_photo_roles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_completion_photo_roles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_profiles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_profiles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_profiles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fee_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    policy_kind_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    transaction_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    applies_to_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    calculation_method_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    min_base_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    max_base_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    display_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    rate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    monthly_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    per_visit_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    charge_timing_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    restore_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_policies", x => x.id);
                    table.CheckConstraint("CK_fee_policies_kind", "[policy_kind_code] IN ('QUOTE','SUPPORT','PROJECT','SUBSCRIPTION')");
                    table.CheckConstraint("CK_fee_policies_rate", "[rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)");
                    table.CheckConstraint("CK_fee_policies_transaction_type", "[transaction_type_code] IN ('ONE_TIME','PROJECT','SUBSCRIPTION')");
                    table.ForeignKey(
                        name: "FK_fee_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_fee_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    purpose_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    storage_container = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    storage_key = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    storage_key_hash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256_hex = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    scan_result_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    activated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    uploaded_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                    table.CheckConstraint("CK_files_purpose", "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE')");
                    table.CheckConstraint("CK_files_size_bytes", "[size_bytes] >= 0");
                    table.CheckConstraint("CK_files_status", "[status_code] IN ('PENDING','ACTIVE','QUARANTINED','DELETED')");
                    table.ForeignKey(
                        name: "FK_files_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    aggregate_type = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    aggregate_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_type = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    available_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    attempt_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    last_attempt_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                    table.CheckConstraint("CK_outbox_events_payload_json", "ISJSON([payload_json]) = 1");
                    table.CheckConstraint("CK_outbox_events_status", "[status_code] IN ('PENDING','PROCESSING','PUBLISHED','FAILED')");
                    table.ForeignKey(
                        name: "FK_outbox_events_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_profiles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    business_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    business_registration_no = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    approval_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    activity_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "INACTIVE"),
                    trust_score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    approval_decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approval_decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_profiles", x => x.id);
                    table.CheckConstraint("CK_provider_profiles_activity_status", "[activity_status_code] IN ('ACTIVE','INACTIVE')");
                    table.CheckConstraint("CK_provider_profiles_approval_status", "[approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.ForeignKey(
                        name: "FK_provider_profiles_users_approval_decided_by_user_id",
                        column: x => x.approval_decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_profiles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_profiles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_roles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    level_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    external_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    source_record_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.id);
                    table.CheckConstraint("CK_service_categories_level", "[level_code] IN ('MAJOR','MIDDLE','SERVICE')");
                    table.CheckConstraint("CK_service_categories_status", "[status_code] IN ('ACTIVE','PAUSED','REVIEW')");
                    table.ForeignKey(
                        name: "FK_service_categories_service_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_categories_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_assets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    asset_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    model_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    serial_number = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    installed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    attributes_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_assets", x => x.id);
                    table.CheckConstraint("CK_service_assets_attributes_json", "[attributes_json] IS NULL OR ISJSON([attributes_json]) = 1");
                    table.CheckConstraint("CK_service_assets_status", "[status_code] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_service_assets_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_assets_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_assets_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_approval_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    action_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_approval_events", x => x.id);
                    table.CheckConstraint("CK_provider_approval_events_action", "[action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME')");
                    table.CheckConstraint("CK_provider_approval_events_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.CheckConstraint("CK_provider_approval_events_to_status", "[to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.ForeignKey(
                        name: "FK_provider_approval_events_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_approval_events_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    issued_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    verification_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    verified_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_documents_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_documents_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_documents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_documents_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_documents_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    granted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    granted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    revoked_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_roles_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_roles_users_revoked_by_user_id",
                        column: x => x.revoked_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_field_definitions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_field_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    owner_middle_category_id = table.Column<long>(type: "bigint", nullable: false),
                    field_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    field_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    options_or_unit_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    provider_visibility_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "FULL"),
                    pre_accept_masking_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "NONE"),
                    validation_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_definitions", x => x.id);
                    table.CheckConstraint("CK_category_field_definitions_masking", "[pre_accept_masking_code] IN ('NONE','DETAIL_ADDRESS')");
                    table.CheckConstraint("CK_category_field_definitions_status", "[status_code] IN ('ACTIVE','INACTIVE')");
                    table.CheckConstraint("CK_category_field_definitions_type", "[field_type_code] IN ('LONG_TEXT','FILE','DATETIME','MONEY','TEXT','ADDRESS','SELECT','NUMBER','PERIOD','RECURRENCE')");
                    table.CheckConstraint("CK_category_field_definitions_visibility", "[provider_visibility_code] IN ('FULL','AREA_ONLY')");
                    table.ForeignKey(
                        name: "FK_category_field_definitions_service_categories_owner_middle_category_id",
                        column: x => x.owner_middle_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_definitions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_definitions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    policy_version = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    transaction_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    request_method_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    onsite_requirement_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_emergency_allowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    subscription_option_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    standard_work_unit_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    base_price_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    price_method_text = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    vat_display_rule_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    minimum_budget_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    max_quote_count = table.Column<short>(type: "smallint", nullable: false),
                    quote_validity_minutes = table.Column<int>(type: "int", nullable: false),
                    fee_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    estimated_quote_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    fee_charge_timing_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    fee_restore_condition_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    matching_area_rule_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    notification_target_rule_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    provider_response_deadline_minutes = table.Column<int>(type: "int", nullable: false),
                    request_field_summary_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    required_completion_photo_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    required_qualification_summary_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    insurance_requirement_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    safety_grade_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    completion_evidence_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    default_warranty_days = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    trust_score_display_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    default_sort_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    service_area_level_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "SIGUNGU"),
                    reference_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    admin_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_policies", x => x.id);
                    table.CheckConstraint("CK_category_policies_max_quotes", "[max_quote_count] > 0");
                    table.CheckConstraint("CK_category_policies_photo_count", "[required_completion_photo_count] >= 0");
                    table.CheckConstraint("CK_category_policies_quote_validity", "[quote_validity_minutes] > 0");
                    table.CheckConstraint("CK_category_policies_response_deadline", "[provider_response_deadline_minutes] > 0");
                    table.CheckConstraint("CK_category_policies_safety_grade", "[safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')");
                    table.CheckConstraint("CK_category_policies_transaction_type", "[transaction_type_code] IN ('ONE_TIME','SUBSCRIPTION','PROJECT')");
                    table.CheckConstraint("CK_category_policies_warranty_days", "[default_warranty_days] >= 0");
                    table.ForeignKey(
                        name: "FK_category_policies_fee_policies_fee_policy_id",
                        column: x => x.fee_policy_id,
                        principalTable: "fee_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_policies_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_service_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    activated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    deactivated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_categories", x => x.id);
                    table.CheckConstraint("CK_provider_service_categories_status", "[status_code] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_provider_service_categories_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_categories_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_categories_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "qualification_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_policy_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    middle_category_id = table.Column<long>(type: "bigint", nullable: false),
                    identity_verification_rule_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    business_registration_rule_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    required_license_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    insurance_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    equipment_facility_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    background_check_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    safety_grade_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    emergency_rule_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    review_cycle_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    admin_checklist_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qualification_policies", x => x.id);
                    table.CheckConstraint("CK_qualification_policies_safety_grade", "[safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')");
                    table.ForeignKey(
                        name: "FK_qualification_policies_service_categories_middle_category_id",
                        column: x => x.middle_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_qualification_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_qualification_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_field_assignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    field_definition_id = table.Column<long>(type: "bigint", nullable: false),
                    target_category_id = table.Column<long>(type: "bigint", nullable: false),
                    scope_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_field_assignments", x => x.id);
                    table.CheckConstraint("CK_category_field_assignments_scope", "[scope_code] IN ('MIDDLE','SERVICE')");
                    table.ForeignKey(
                        name: "FK_category_field_assignments_category_field_definitions_field_definition_id",
                        column: x => x.field_definition_id,
                        principalTable: "category_field_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_assignments_service_categories_target_category_id",
                        column: x => x.target_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_assignments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_field_assignments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_completion_photo_requirements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    photo_role_id = table.Column<long>(type: "bigint", nullable: false),
                    minimum_count = table.Column<short>(type: "smallint", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_completion_photo_requirements", x => x.id);
                    table.CheckConstraint("CK_category_completion_photo_requirements_minimum", "[minimum_count] >= 0");
                    table.ForeignKey(
                        name: "FK_category_completion_photo_requirements_category_policies_category_policy_id",
                        column: x => x.category_policy_id,
                        principalTable: "category_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_completion_photo_requirements_completion_photo_roles_photo_role_id",
                        column: x => x.photo_role_id,
                        principalTable: "completion_photo_roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_completion_photo_requirements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_category_completion_photo_requirements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    category_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    detail_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    is_urgent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    opened_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    accepted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.id);
                    table.CheckConstraint("CK_service_requests_policy_json", "ISJSON([policy_snapshot_json]) = 1");
                    table.CheckConstraint("CK_service_requests_status", "[status_code] IN ('DRAFT','OPEN','ACCEPTED','EXPIRED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_service_requests_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_requests_category_policies_category_policy_id",
                        column: x => x.category_policy_id,
                        principalTable: "category_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_requests_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_requests_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_service_areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    activated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    deactivated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_areas", x => x.id);
                    table.CheckConstraint("CK_provider_service_areas_status", "[status_code] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_provider_service_areas_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_areas_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_areas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_areas_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "dispatch_candidates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    category_match = table.Column<bool>(type: "bit", nullable: false),
                    area_match = table.Column<bool>(type: "bit", nullable: false),
                    approval_match = table.Column<bool>(type: "bit", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispatch_candidates", x => x.id);
                    table.CheckConstraint("CK_dispatch_candidates_status", "[status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','EXPIRED')");
                    table.ForeignKey(
                        name: "FK_dispatch_candidates_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_dispatch_candidates_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "request_answers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    field_definition_id = table.Column<long>(type: "bigint", nullable: false),
                    value_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    value_number = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    value_boolean = table.Column<bool>(type: "bit", nullable: true),
                    value_date = table.Column<DateOnly>(type: "date", nullable: true),
                    value_datetime = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    value_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    value_currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_answers", x => x.id);
                    table.CheckConstraint("CK_request_answers_value_json", "[value_json] IS NULL OR ISJSON([value_json]) = 1");
                    table.ForeignKey(
                        name: "FK_request_answers_category_field_definitions_field_definition_id",
                        column: x => x.field_definition_id,
                        principalTable: "category_field_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_answers_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_answers_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_answers_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "request_dispatches",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    candidate_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    available_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    viewed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    responded_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_dispatches", x => x.id);
                    table.CheckConstraint("CK_request_dispatches_status", "[status_code] IN ('AVAILABLE','VIEWED','RESPONDED','EXPIRED')");
                    table.ForeignKey(
                        name: "FK_request_dispatches_dispatch_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "dispatch_candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_dispatches_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_dispatches_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "request_answer_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_answer_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_answer_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_answer_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_answer_files_request_answers_request_answer_id",
                        column: x => x.request_answer_id,
                        principalTable: "request_answers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_request_answer_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_user_id = table.Column<long>(type: "bigint", nullable: false),
                    request_dispatch_id = table.Column<long>(type: "bigint", nullable: true),
                    type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_urgent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    recorded_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    read_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.CheckConstraint("CK_notifications_data_json", "[data_json] IS NULL OR ISJSON([data_json]) = 1");
                    table.CheckConstraint("CK_notifications_status", "[status_code] IN ('PENDING','RECORDED','PROCESSING','SENT','PARTIALLY_FAILED','FAILED')");
                    table.ForeignKey(
                        name: "FK_notifications_request_dispatches_request_dispatch_id",
                        column: x => x.request_dispatch_id,
                        principalTable: "request_dispatches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notifications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    request_dispatch_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    accepted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    withdrawn_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.id);
                    table.CheckConstraint("CK_quotes_status", "[status_code] IN ('DRAFT','SUBMITTED','ACCEPTED','NOT_SELECTED','WITHDRAWN','EXPIRED','INVALIDATED')");
                    table.ForeignKey(
                        name: "FK_quotes_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quotes_request_dispatches_request_dispatch_id",
                        column: x => x.request_dispatch_id,
                        principalTable: "request_dispatches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quotes_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quotes_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quotes_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    attempt_no = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    attempted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.CheckConstraint("CK_notification_deliveries_channel", "[channel_code] IN ('IN_APP','ALIMTALK')");
                    table.CheckConstraint("CK_notification_deliveries_status", "[status_code] IN ('PENDING','SENT','FAILED','SKIPPED')");
                    table.ForeignKey(
                        name: "FK_notification_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quote_revisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quote_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_no = table.Column<int>(type: "int", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subtotal_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    vat_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    total_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    estimated_duration_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    available_start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    valid_until = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    revision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_quote_revisions_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_revisions_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quote_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    quote_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 1m),
                    unit_text = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    line_total_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_items", x => x.id);
                    table.CheckConstraint("CK_quote_items_quantity", "[quantity] > 0");
                    table.ForeignKey(
                        name: "FK_quote_items_quote_revisions_quote_revision_id",
                        column: x => x.quote_revision_id,
                        principalTable: "quote_revisions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    accepted_quote_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "CREATED"),
                    agreed_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    quote_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category_policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    completion_policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warranty_days_snapshot = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    provider_trust_score_snapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.CheckConstraint("CK_transactions_category_policy_json", "ISJSON([category_policy_snapshot_json]) = 1");
                    table.CheckConstraint("CK_transactions_completion_policy_json", "ISJSON([completion_policy_snapshot_json]) = 1");
                    table.CheckConstraint("CK_transactions_quote_json", "ISJSON([quote_snapshot_json]) = 1");
                    table.CheckConstraint("CK_transactions_status", "[status_code] IN ('CREATED','IN_PROGRESS','COMPLETION_SUBMITTED','REVISION_REQUESTED','COMPLETED','DISPUTED','CANCELLED')");
                    table.CheckConstraint("CK_transactions_warranty_days", "[warranty_days_snapshot] >= 0");
                    table.ForeignKey(
                        name: "FK_transactions_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_quote_revisions_accepted_quote_revision_id",
                        column: x => x.accepted_quote_revision_id,
                        principalTable: "quote_revisions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transactions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "after_service_cases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "RECEIVED"),
                    subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_after_service_cases", x => x.id);
                    table.CheckConstraint("CK_after_service_cases_status", "[status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");
                    table.ForeignKey(
                        name: "FK_after_service_cases_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_cases_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_cases_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_cases_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_cases_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "work_completions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    latest_revision_no = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    first_submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_completions", x => x.id);
                    table.CheckConstraint("CK_work_completions_status", "[status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')");
                    table.ForeignKey(
                        name: "FK_work_completions_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_completions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_completions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "after_service_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    action_note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_after_service_actions", x => x.id);
                    table.CheckConstraint("CK_after_service_actions_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");
                    table.CheckConstraint("CK_after_service_actions_to_status", "[to_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')");
                    table.ForeignKey(
                        name: "FK_after_service_actions_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_actions_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "work_completion_revisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_completion_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_no = table.Column<int>(type: "int", nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "SUBMITTED"),
                    work_summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    checklist_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    provider_attestation_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_completion_revisions", x => x.id);
                    table.CheckConstraint("CK_work_completion_revisions_checklist_json", "[checklist_json] IS NULL OR ISJSON([checklist_json]) = 1");
                    table.CheckConstraint("CK_work_completion_revisions_status", "[status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')");
                    table.ForeignKey(
                        name: "FK_work_completion_revisions_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_completion_revisions_work_completions_work_completion_id",
                        column: x => x.work_completion_id,
                        principalTable: "work_completions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "after_service_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: false),
                    after_service_action_id = table.Column<long>(type: "bigint", nullable: true),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    role_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_after_service_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_after_service_files_after_service_actions_after_service_action_id",
                        column: x => x.after_service_action_id,
                        principalTable: "after_service_actions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_files_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_after_service_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "completion_evidence_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    completion_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    photo_role_id = table.Column<long>(type: "bigint", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completion_evidence_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_completion_evidence_files_completion_photo_roles_photo_role_id",
                        column: x => x.photo_role_id,
                        principalTable: "completion_photo_roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_completion_evidence_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_completion_evidence_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_completion_evidence_files_work_completion_revisions_completion_revision_id",
                        column: x => x.completion_revision_id,
                        principalTable: "work_completion_revisions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_confirmations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    completion_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    result_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    confirmed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_confirmations", x => x.id);
                    table.CheckConstraint("CK_customer_confirmations_result", "[result_code] IN ('COMPLETED','REVISION_REQUESTED','DISPUTED')");
                    table.ForeignKey(
                        name: "FK_customer_confirmations_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_confirmations_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_confirmations_work_completion_revisions_completion_revision_id",
                        column: x => x.completion_revision_id,
                        principalTable: "work_completion_revisions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_history_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    source_completion_revision_id = table.Column<long>(type: "bigint", nullable: true),
                    after_service_case_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    provider_name_snapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    category_name_snapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    total_amount_snapshot = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    completed_at_snapshot = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    warranty_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    warranty_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_history_entries", x => x.id);
                    table.CheckConstraint("CK_service_history_entries_event_type", "[event_type_code] IN ('COMPLETION','AFTER_SERVICE_RECEIVED','AFTER_SERVICE_STARTED','AFTER_SERVICE_COMPLETED','ASSET_LINKED','ASSET_CORRECTED')");
                    table.CheckConstraint("CK_service_history_entries_snapshot_json", "ISJSON([snapshot_json]) = 1");
                    table.ForeignKey(
                        name: "FK_service_history_entries_after_service_cases_after_service_case_id",
                        column: x => x.after_service_case_id,
                        principalTable: "after_service_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_history_entries_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_history_entries_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_history_entries_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_service_history_entries_work_completion_revisions_source_completion_revision_id",
                        column: x => x.source_completion_revision_id,
                        principalTable: "work_completion_revisions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_history_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_history_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    unit_text = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_history_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_history_items_service_history_entries_service_history_entry_id",
                        column: x => x.service_history_entry_id,
                        principalTable: "service_history_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transaction_asset_links",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    service_asset_id = table.Column<long>(type: "bigint", nullable: false),
                    source_history_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    correction_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    linked_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    linked_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    corrected_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    corrected_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_asset_links", x => x.id);
                    table.CheckConstraint("CK_transaction_asset_links_status", "[status_code] IN ('ACTIVE','CORRECTED')");
                    table.ForeignKey(
                        name: "FK_transaction_asset_links_service_assets_service_asset_id",
                        column: x => x.service_asset_id,
                        principalTable: "service_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_asset_links_service_history_entries_source_history_entry_id",
                        column: x => x.source_history_entry_id,
                        principalTable: "service_history_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_asset_links_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_asset_links_users_corrected_by_user_id",
                        column: x => x.corrected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_asset_links_users_linked_by_user_id",
                        column: x => x.linked_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_abolition_type_code",
                table: "administrative_areas",
                column: "abolition_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_area_code",
                table: "administrative_areas",
                column: "area_code",
                unique: true,
                filter: "[is_active] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_area_code_effective_from",
                table: "administrative_areas",
                columns: new[] { "area_code", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_area_level_code",
                table: "administrative_areas",
                column: "area_level_code");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_area_name",
                table: "administrative_areas",
                column: "area_name");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_created_by_user_id",
                table: "administrative_areas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_effective_from",
                table: "administrative_areas",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_effective_to",
                table: "administrative_areas",
                column: "effective_to");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_is_active",
                table: "administrative_areas",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_parent_area_id",
                table: "administrative_areas",
                column: "parent_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_parent_area_id_area_level_code_is_active_area_name",
                table: "administrative_areas",
                columns: new[] { "parent_area_id", "area_level_code", "is_active", "area_name" });

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_public_id",
                table: "administrative_areas",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_source_parent_area_code",
                table: "administrative_areas",
                column: "source_parent_area_code");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_source_system_code",
                table: "administrative_areas",
                column: "source_system_code");

            migrationBuilder.CreateIndex(
                name: "IX_administrative_areas_updated_by_user_id",
                table: "administrative_areas",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_actor_user_id",
                table: "after_service_actions",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_after_service_case_id",
                table: "after_service_actions",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_after_service_case_id_occurred_at",
                table: "after_service_actions",
                columns: new[] { "after_service_case_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_idempotency_key",
                table: "after_service_actions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_occurred_at",
                table: "after_service_actions",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_actions_to_status_code",
                table: "after_service_actions",
                column: "to_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_completed_at",
                table: "after_service_cases",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_created_by_user_id",
                table: "after_service_cases",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_customer_profile_id",
                table: "after_service_cases",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_idempotency_key",
                table: "after_service_cases",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_provider_profile_id",
                table: "after_service_cases",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_provider_profile_id_status_code_received_at",
                table: "after_service_cases",
                columns: new[] { "provider_profile_id", "status_code", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_public_id",
                table: "after_service_cases",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_received_at",
                table: "after_service_cases",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_status_code",
                table: "after_service_cases",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_transaction_id",
                table: "after_service_cases",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_transaction_id_received_at",
                table: "after_service_cases",
                columns: new[] { "transaction_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_updated_by_user_id",
                table: "after_service_cases",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_after_service_action_id",
                table: "after_service_files",
                column: "after_service_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_after_service_case_id",
                table: "after_service_files",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_after_service_case_id_file_id",
                table: "after_service_files",
                columns: new[] { "after_service_case_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_created_by_user_id",
                table: "after_service_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_file_id",
                table: "after_service_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_after_service_files_role_code",
                table: "after_service_files",
                column: "role_code");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action_code",
                table: "audit_logs",
                column: "action_code");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_role_code",
                table: "audit_logs",
                column: "actor_role_code");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id_occurred_at",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_public_id",
                table: "audit_logs",
                column: "entity_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type",
                table: "audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_public_id_occurred_at",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_public_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_occurred_at",
                table: "audit_logs",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_result_code",
                table: "audit_logs",
                column: "result_code");

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_category_policy_id",
                table: "category_completion_photo_requirements",
                column: "category_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_category_policy_id_photo_role_id",
                table: "category_completion_photo_requirements",
                columns: new[] { "category_policy_id", "photo_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_created_by_user_id",
                table: "category_completion_photo_requirements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_display_order",
                table: "category_completion_photo_requirements",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_photo_role_id",
                table: "category_completion_photo_requirements",
                column: "photo_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_completion_photo_requirements_updated_by_user_id",
                table: "category_completion_photo_requirements",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_created_by_user_id",
                table: "category_field_assignments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_field_definition_id",
                table: "category_field_assignments",
                column: "field_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_field_definition_id_target_category_id",
                table: "category_field_assignments",
                columns: new[] { "field_definition_id", "target_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_is_active",
                table: "category_field_assignments",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_target_category_id",
                table: "category_field_assignments",
                column: "target_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_assignments_updated_by_user_id",
                table: "category_field_assignments",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_created_by_user_id",
                table: "category_field_definitions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_field_type_code",
                table: "category_field_definitions",
                column: "field_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_is_required",
                table: "category_field_definitions",
                column: "is_required");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_owner_middle_category_id",
                table: "category_field_definitions",
                column: "owner_middle_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_field_key",
                table: "category_field_definitions",
                columns: new[] { "owner_middle_category_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_owner_middle_category_id_status_code_display_order",
                table: "category_field_definitions",
                columns: new[] { "owner_middle_category_id", "status_code", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_public_id",
                table: "category_field_definitions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_source_field_id",
                table: "category_field_definitions",
                column: "source_field_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_field_definitions_updated_by_user_id",
                table: "category_field_definitions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_category_id",
                table: "category_policies",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_category_id_effective_from",
                table: "category_policies",
                columns: new[] { "category_id", "effective_from" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_category_id_policy_version",
                table: "category_policies",
                columns: new[] { "category_id", "policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_created_by_user_id",
                table: "category_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_effective_from",
                table: "category_policies",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_effective_to",
                table: "category_policies",
                column: "effective_to");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_fee_policy_id",
                table: "category_policies",
                column: "fee_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_is_emergency_allowed",
                table: "category_policies",
                column: "is_emergency_allowed");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_public_id",
                table: "category_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_safety_grade_code",
                table: "category_policies",
                column: "safety_grade_code");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_transaction_type_code",
                table: "category_policies",
                column: "transaction_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_category_policies_updated_by_user_id",
                table: "category_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_completion_revision_id",
                table: "completion_evidence_files",
                column: "completion_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_completion_revision_id_file_id",
                table: "completion_evidence_files",
                columns: new[] { "completion_revision_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_completion_revision_id_photo_role_id",
                table: "completion_evidence_files",
                columns: new[] { "completion_revision_id", "photo_role_id" });

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_created_by_user_id",
                table: "completion_evidence_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_file_id",
                table: "completion_evidence_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_evidence_files_photo_role_id",
                table: "completion_evidence_files",
                column: "photo_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_photo_roles_code",
                table: "completion_photo_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_completion_photo_roles_created_by_user_id",
                table: "completion_photo_roles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_completion_photo_roles_is_active",
                table: "completion_photo_roles",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_completion_photo_roles_updated_by_user_id",
                table: "completion_photo_roles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_completion_revision_id",
                table: "customer_confirmations",
                column: "completion_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_confirmed_at",
                table: "customer_confirmations",
                column: "confirmed_at");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_confirmed_by_user_id",
                table: "customer_confirmations",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_idempotency_key",
                table: "customer_confirmations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_public_id",
                table: "customer_confirmations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_result_code",
                table: "customer_confirmations",
                column: "result_code");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_transaction_id",
                table: "customer_confirmations",
                column: "transaction_id",
                unique: true,
                filter: "[result_code] = 'COMPLETED'");

            migrationBuilder.CreateIndex(
                name: "IX_customer_confirmations_transaction_id_confirmed_at",
                table: "customer_confirmations",
                columns: new[] { "transaction_id", "confirmed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_created_by_user_id",
                table: "customer_profiles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_public_id",
                table: "customer_profiles",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_updated_by_user_id",
                table: "customer_profiles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_user_id",
                table: "customer_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_evaluated_at",
                table: "dispatch_candidates",
                column: "evaluated_at");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_expires_at",
                table: "dispatch_candidates",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_provider_profile_id",
                table: "dispatch_candidates",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_reason_code",
                table: "dispatch_candidates",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_service_request_id",
                table: "dispatch_candidates",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_service_request_id_provider_profile_id",
                table: "dispatch_candidates",
                columns: new[] { "service_request_id", "provider_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_service_request_id_status_code",
                table: "dispatch_candidates",
                columns: new[] { "service_request_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_candidates_status_code",
                table: "dispatch_candidates",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_code",
                table: "fee_policies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_created_by_user_id",
                table: "fee_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_effective_from",
                table: "fee_policies",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_effective_to",
                table: "fee_policies",
                column: "effective_to");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_is_active",
                table: "fee_policies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_max_base_amount",
                table: "fee_policies",
                column: "max_base_amount");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_min_base_amount",
                table: "fee_policies",
                column: "min_base_amount");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_policy_kind_code",
                table: "fee_policies",
                column: "policy_kind_code");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_public_id",
                table: "fee_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_transaction_type_code",
                table: "fee_policies",
                column: "transaction_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_fee_policies_updated_by_user_id",
                table: "fee_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_content_type",
                table: "files",
                column: "content_type");

            migrationBuilder.CreateIndex(
                name: "IX_files_created_at",
                table: "files",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_files_public_id",
                table: "files",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_purpose_code",
                table: "files",
                column: "purpose_code");

            migrationBuilder.CreateIndex(
                name: "IX_files_sha256_hex",
                table: "files",
                column: "sha256_hex");

            migrationBuilder.CreateIndex(
                name: "IX_files_status_code",
                table: "files",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_files_storage_key_hash",
                table: "files",
                column: "storage_key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_uploaded_by_user_id",
                table: "files",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_attempted_at",
                table: "notification_deliveries",
                column: "attempted_at");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_channel_code",
                table: "notification_deliveries",
                column: "channel_code");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_notification_id",
                table: "notification_deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_notification_id_channel_code_attempt_no",
                table: "notification_deliveries",
                columns: new[] { "notification_id", "channel_code", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_provider_message_id",
                table: "notification_deliveries",
                column: "provider_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_status_code",
                table: "notification_deliveries",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_by_user_id",
                table: "notifications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_idempotency_key",
                table: "notifications",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_urgent",
                table: "notifications",
                column: "is_urgent");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_public_id",
                table: "notifications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_read_at",
                table: "notifications",
                column: "read_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_user_id",
                table: "notifications",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_user_id_read_at_recorded_at",
                table: "notifications",
                columns: new[] { "recipient_user_id", "read_at", "recorded_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recorded_at",
                table: "notifications",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_request_dispatch_id",
                table: "notifications",
                column: "request_dispatch_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_status_code",
                table: "notifications",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_type_code",
                table: "notifications",
                column: "type_code");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_aggregate_public_id",
                table: "outbox_events",
                column: "aggregate_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_aggregate_type",
                table: "outbox_events",
                column: "aggregate_type");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_available_at",
                table: "outbox_events",
                column: "available_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_created_by_user_id",
                table: "outbox_events",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_event_type",
                table: "outbox_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_idempotency_key",
                table: "outbox_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_occurred_at",
                table: "outbox_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_processed_at",
                table: "outbox_events",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_public_id",
                table: "outbox_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_status_code",
                table: "outbox_events",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_status_code_available_at_id",
                table: "outbox_events",
                columns: new[] { "status_code", "available_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_approval_events_correlation_id",
                table: "provider_approval_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_approval_events_decided_by_user_id",
                table: "provider_approval_events",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_approval_events_provider_profile_id",
                table: "provider_approval_events",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_approval_events_provider_profile_id_decided_at",
                table: "provider_approval_events",
                columns: new[] { "provider_profile_id", "decided_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_created_by_user_id",
                table: "provider_documents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_document_type_code",
                table: "provider_documents",
                column: "document_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_expires_at",
                table: "provider_documents",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_file_id",
                table: "provider_documents",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_provider_profile_id",
                table: "provider_documents",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_updated_by_user_id",
                table: "provider_documents",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_verification_status_code",
                table: "provider_documents",
                column: "verification_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_documents_verified_by_user_id",
                table: "provider_documents",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_activity_status_code",
                table: "provider_profiles",
                column: "activity_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_approval_decided_by_user_id",
                table: "provider_profiles",
                column: "approval_decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_approval_status_code",
                table: "provider_profiles",
                column: "approval_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_approval_status_code_activity_status_code_trust_score",
                table: "provider_profiles",
                columns: new[] { "approval_status_code", "activity_status_code", "trust_score" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_business_name",
                table: "provider_profiles",
                column: "business_name");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_business_registration_no",
                table: "provider_profiles",
                column: "business_registration_no",
                unique: true,
                filter: "[business_registration_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_created_by_user_id",
                table: "provider_profiles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_public_id",
                table: "provider_profiles",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_updated_by_user_id",
                table: "provider_profiles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_profiles_user_id",
                table: "provider_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_administrative_area_id",
                table: "provider_service_areas",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_administrative_area_id_status_code_provider_service_category_id",
                table: "provider_service_areas",
                columns: new[] { "administrative_area_id", "status_code", "provider_service_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_created_by_user_id",
                table: "provider_service_areas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_provider_service_category_id",
                table: "provider_service_areas",
                column: "provider_service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_provider_service_category_id_administrative_area_id",
                table: "provider_service_areas",
                columns: new[] { "provider_service_category_id", "administrative_area_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_status_code",
                table: "provider_service_areas",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_areas_updated_by_user_id",
                table: "provider_service_areas",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_category_id",
                table: "provider_service_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_category_id_status_code_provider_profile_id",
                table: "provider_service_categories",
                columns: new[] { "category_id", "status_code", "provider_profile_id" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_created_by_user_id",
                table: "provider_service_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_provider_profile_id",
                table: "provider_service_categories",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_provider_profile_id_category_id",
                table: "provider_service_categories",
                columns: new[] { "provider_profile_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_status_code",
                table: "provider_service_categories",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_categories_updated_by_user_id",
                table: "provider_service_categories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_created_by_user_id",
                table: "qualification_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_effective_from",
                table: "qualification_policies",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_effective_to",
                table: "qualification_policies",
                column: "effective_to");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_middle_category_id",
                table: "qualification_policies",
                column: "middle_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_middle_category_id_source_policy_id",
                table: "qualification_policies",
                columns: new[] { "middle_category_id", "source_policy_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_safety_grade_code",
                table: "qualification_policies",
                column: "safety_grade_code");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_source_policy_id",
                table: "qualification_policies",
                column: "source_policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qualification_policies_updated_by_user_id",
                table: "qualification_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_quote_revision_id",
                table: "quote_items",
                column: "quote_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_quote_revision_id_line_no",
                table: "quote_items",
                columns: new[] { "quote_revision_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_idempotency_key",
                table: "quote_revisions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_public_id",
                table: "quote_revisions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_quote_id",
                table: "quote_revisions",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_quote_id_revision_no",
                table: "quote_revisions",
                columns: new[] { "quote_id", "revision_no" },
                unique: true,
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_quote_id_submitted_at",
                table: "quote_revisions",
                columns: new[] { "quote_id", "submitted_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_submitted_at",
                table: "quote_revisions",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_submitted_by_user_id",
                table: "quote_revisions",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_total_amount",
                table: "quote_revisions",
                column: "total_amount");

            migrationBuilder.CreateIndex(
                name: "IX_quote_revisions_valid_until",
                table: "quote_revisions",
                column: "valid_until");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_created_by_user_id",
                table: "quotes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_expires_at",
                table: "quotes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_provider_profile_id",
                table: "quotes",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_public_id",
                table: "quotes",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotes_request_dispatch_id",
                table: "quotes",
                column: "request_dispatch_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_service_request_id",
                table: "quotes",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_service_request_id_provider_profile_id",
                table: "quotes",
                columns: new[] { "service_request_id", "provider_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotes_service_request_id_status_code_submitted_at",
                table: "quotes",
                columns: new[] { "service_request_id", "status_code", "submitted_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_quotes_status_code",
                table: "quotes",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_submitted_at",
                table: "quotes",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_updated_by_user_id",
                table: "quotes",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answer_files_created_by_user_id",
                table: "request_answer_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answer_files_file_id",
                table: "request_answer_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answer_files_request_answer_id",
                table: "request_answer_files",
                column: "request_answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answer_files_request_answer_id_file_id",
                table: "request_answer_files",
                columns: new[] { "request_answer_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_answers_created_by_user_id",
                table: "request_answers",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answers_field_definition_id",
                table: "request_answers",
                column: "field_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answers_service_request_id",
                table: "request_answers",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_answers_service_request_id_field_definition_id",
                table: "request_answers",
                columns: new[] { "service_request_id", "field_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_answers_updated_by_user_id",
                table: "request_answers",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_available_at",
                table: "request_dispatches",
                column: "available_at");

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_candidate_id",
                table: "request_dispatches",
                column: "candidate_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_expires_at",
                table: "request_dispatches",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_idempotency_key",
                table: "request_dispatches",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_provider_profile_id",
                table: "request_dispatches",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_provider_profile_id_status_code_available_at",
                table: "request_dispatches",
                columns: new[] { "provider_profile_id", "status_code", "available_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_public_id",
                table: "request_dispatches",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_service_request_id",
                table: "request_dispatches",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_service_request_id_provider_profile_id",
                table: "request_dispatches",
                columns: new[] { "service_request_id", "provider_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_dispatches_status_code",
                table: "request_dispatches",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_created_by_user_id",
                table: "roles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_is_active",
                table: "roles",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_roles_updated_by_user_id",
                table: "roles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_asset_type_code",
                table: "service_assets",
                column: "asset_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_created_by_user_id",
                table: "service_assets",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_customer_profile_id",
                table: "service_assets",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_customer_profile_id_status_code_name",
                table: "service_assets",
                columns: new[] { "customer_profile_id", "status_code", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_public_id",
                table: "service_assets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_status_code",
                table: "service_assets",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_service_assets_updated_by_user_id",
                table: "service_assets",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_created_by_user_id",
                table: "service_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_external_code",
                table: "service_categories",
                column: "external_code",
                unique: true,
                filter: "[external_code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_level_code",
                table: "service_categories",
                column: "level_code");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_parent_id",
                table: "service_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_parent_id_name",
                table: "service_categories",
                columns: new[] { "parent_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_parent_id_status_code_sort_order",
                table: "service_categories",
                columns: new[] { "parent_id", "status_code", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_public_id",
                table: "service_categories",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_source_record_id",
                table: "service_categories",
                column: "source_record_id",
                unique: true,
                filter: "[source_record_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_updated_by_user_id",
                table: "service_categories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_after_service_case_id",
                table: "service_history_entries",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_created_by_user_id",
                table: "service_history_entries",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_customer_profile_id",
                table: "service_history_entries",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_customer_profile_id_occurred_at",
                table: "service_history_entries",
                columns: new[] { "customer_profile_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_event_type_code",
                table: "service_history_entries",
                column: "event_type_code");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_idempotency_key",
                table: "service_history_entries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_occurred_at",
                table: "service_history_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_public_id",
                table: "service_history_entries",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_source_completion_revision_id",
                table: "service_history_entries",
                column: "source_completion_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_transaction_id",
                table: "service_history_entries",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_transaction_id_event_type_code",
                table: "service_history_entries",
                columns: new[] { "transaction_id", "event_type_code" });

            migrationBuilder.CreateIndex(
                name: "IX_service_history_entries_warranty_end_date",
                table: "service_history_entries",
                column: "warranty_end_date");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_items_service_history_entry_id",
                table: "service_history_items",
                column: "service_history_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_history_items_service_history_entry_id_line_no",
                table: "service_history_items",
                columns: new[] { "service_history_entry_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_administrative_area_id",
                table: "service_requests",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_category_id",
                table: "service_requests",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_category_id_administrative_area_id_status_code_opened_at",
                table: "service_requests",
                columns: new[] { "category_id", "administrative_area_id", "status_code", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_category_policy_id",
                table: "service_requests",
                column: "category_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_created_at",
                table: "service_requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_created_by_user_id",
                table: "service_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_profile_id",
                table: "service_requests",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_profile_id_created_at",
                table: "service_requests",
                columns: new[] { "customer_profile_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_expires_at",
                table: "service_requests",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_idempotency_key",
                table: "service_requests",
                column: "idempotency_key",
                unique: true,
                filter: "[idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_is_urgent",
                table: "service_requests",
                column: "is_urgent");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_opened_at",
                table: "service_requests",
                column: "opened_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_public_id",
                table: "service_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_status_code",
                table: "service_requests",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_updated_by_user_id",
                table: "service_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_corrected_by_user_id",
                table: "transaction_asset_links",
                column: "corrected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_linked_at",
                table: "transaction_asset_links",
                column: "linked_at");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_linked_by_user_id",
                table: "transaction_asset_links",
                column: "linked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_service_asset_id",
                table: "transaction_asset_links",
                column: "service_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_service_asset_id_linked_at",
                table: "transaction_asset_links",
                columns: new[] { "service_asset_id", "linked_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_source_history_entry_id",
                table: "transaction_asset_links",
                column: "source_history_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_status_code",
                table: "transaction_asset_links",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_transaction_id",
                table: "transaction_asset_links",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_transaction_id_service_asset_id",
                table: "transaction_asset_links",
                columns: new[] { "transaction_id", "service_asset_id" },
                unique: true,
                filter: "[status_code] = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_asset_links_transaction_id_status_code",
                table: "transaction_asset_links",
                columns: new[] { "transaction_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_accepted_quote_revision_id",
                table: "transactions",
                column: "accepted_quote_revision_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_category_id",
                table: "transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_completed_at",
                table: "transactions",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_created_at",
                table: "transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_created_by_user_id",
                table: "transactions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_customer_profile_id",
                table: "transactions",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_customer_profile_id_created_at",
                table: "transactions",
                columns: new[] { "customer_profile_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_provider_profile_id",
                table: "transactions",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_provider_profile_id_status_code_created_at",
                table: "transactions",
                columns: new[] { "provider_profile_id", "status_code", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_public_id",
                table: "transactions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_service_request_id",
                table: "transactions",
                column: "service_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_status_code",
                table: "transactions",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_updated_by_user_id",
                table: "transactions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_granted_by_user_id",
                table: "user_roles",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_revoked_at",
                table: "user_roles",
                column: "revoked_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_revoked_by_user_id",
                table: "user_roles",
                column: "revoked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id_role_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true,
                filter: "[revoked_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_login_id",
                table: "users",
                column: "normalized_login_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "IX_users_public_id",
                table: "users",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_status_code",
                table: "users",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_users_updated_by_user_id",
                table: "users",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_idempotency_key",
                table: "work_completion_revisions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_public_id",
                table: "work_completion_revisions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_status_code",
                table: "work_completion_revisions",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_submitted_at",
                table: "work_completion_revisions",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_submitted_by_user_id",
                table: "work_completion_revisions",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_work_completion_id",
                table: "work_completion_revisions",
                column: "work_completion_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_work_completion_id_revision_no",
                table: "work_completion_revisions",
                columns: new[] { "work_completion_id", "revision_no" },
                unique: true,
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_work_completion_revisions_work_completion_id_submitted_at",
                table: "work_completion_revisions",
                columns: new[] { "work_completion_id", "submitted_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_confirmed_at",
                table: "work_completions",
                column: "confirmed_at");

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_created_by_user_id",
                table: "work_completions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_public_id",
                table: "work_completions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_status_code",
                table: "work_completions",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_transaction_id",
                table: "work_completions",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_completions_updated_by_user_id",
                table: "work_completions",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "after_service_files");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "category_completion_photo_requirements");

            migrationBuilder.DropTable(
                name: "category_field_assignments");

            migrationBuilder.DropTable(
                name: "completion_evidence_files");

            migrationBuilder.DropTable(
                name: "customer_confirmations");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "outbox_events");

            migrationBuilder.DropTable(
                name: "provider_approval_events");

            migrationBuilder.DropTable(
                name: "provider_documents");

            migrationBuilder.DropTable(
                name: "provider_service_areas");

            migrationBuilder.DropTable(
                name: "qualification_policies");

            migrationBuilder.DropTable(
                name: "quote_items");

            migrationBuilder.DropTable(
                name: "request_answer_files");

            migrationBuilder.DropTable(
                name: "service_history_items");

            migrationBuilder.DropTable(
                name: "transaction_asset_links");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "after_service_actions");

            migrationBuilder.DropTable(
                name: "completion_photo_roles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "provider_service_categories");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "request_answers");

            migrationBuilder.DropTable(
                name: "service_assets");

            migrationBuilder.DropTable(
                name: "service_history_entries");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "category_field_definitions");

            migrationBuilder.DropTable(
                name: "after_service_cases");

            migrationBuilder.DropTable(
                name: "work_completion_revisions");

            migrationBuilder.DropTable(
                name: "work_completions");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "quote_revisions");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "request_dispatches");

            migrationBuilder.DropTable(
                name: "dispatch_candidates");

            migrationBuilder.DropTable(
                name: "provider_profiles");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "administrative_areas");

            migrationBuilder.DropTable(
                name: "category_policies");

            migrationBuilder.DropTable(
                name: "customer_profiles");

            migrationBuilder.DropTable(
                name: "fee_policies");

            migrationBuilder.DropTable(
                name: "service_categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
