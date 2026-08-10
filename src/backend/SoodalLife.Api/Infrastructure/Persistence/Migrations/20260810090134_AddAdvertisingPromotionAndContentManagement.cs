using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvertisingPromotionAndContentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "advertising_campaigns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    campaign_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    campaign_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    audience_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    owner_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    owner_display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    destination_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    destination_value = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    review_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_campaigns", x => x.id);
                    table.CheckConstraint("CK_advertising_campaigns_audience", "[audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')");
                    table.CheckConstraint("CK_advertising_campaigns_destination", "[destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')");
                    table.CheckConstraint("CK_advertising_campaigns_owner", "[owner_type_code] IN ('HEAD_OFFICE','PLATFORM','EXTERNAL','PROVIDER')");
                    table.CheckConstraint("CK_advertising_campaigns_period", "[end_at] IS NULL OR [end_at] > [start_at]");
                    table.CheckConstraint("CK_advertising_campaigns_priority", "[priority] >= 0");
                    table.CheckConstraint("CK_advertising_campaigns_review", "[review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')");
                    table.CheckConstraint("CK_advertising_campaigns_status", "[status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')");
                    table.CheckConstraint("CK_advertising_campaigns_type", "[campaign_type_code] IN ('ADVERTISEMENT','PROMOTION','BANNER','POPUP')");
                    table.ForeignKey(
                        name: "FK_advertising_campaigns_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaigns_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaigns_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_placements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    route_hint = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_placements", x => x.id);
                    table.ForeignKey(
                        name: "FK_advertising_placements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_placements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "managed_contents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    audience_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    review_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    current_version_no = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    start_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managed_contents", x => x.id);
                    table.CheckConstraint("CK_managed_contents_audience", "[audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')");
                    table.CheckConstraint("CK_managed_contents_order", "[display_order] >= 0");
                    table.CheckConstraint("CK_managed_contents_period", "[end_at] IS NULL OR [end_at] > [start_at]");
                    table.CheckConstraint("CK_managed_contents_review", "[review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')");
                    table.CheckConstraint("CK_managed_contents_status", "[status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')");
                    table.CheckConstraint("CK_managed_contents_type", "[content_type_code] IN ('NOTICE','FAQ','SAFETY_GUIDE','CATEGORY_GUIDE','PRICE_REFERENCE')");
                    table.CheckConstraint("CK_managed_contents_version", "[current_version_no] > 0");
                    table.ForeignKey(
                        name: "FK_managed_contents_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_contents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_contents_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_campaign_areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_campaign_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_advertising_campaign_areas_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_areas_advertising_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "advertising_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_areas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_campaign_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_campaign_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_advertising_campaign_categories_advertising_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "advertising_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_categories_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_creatives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    body_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    file_id = table.Column<long>(type: "bigint", nullable: true),
                    alt_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    button_text = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    destination_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    destination_value = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_creatives", x => x.id);
                    table.CheckConstraint("CK_advertising_creatives_destination", "[destination_type_code] IS NULL OR [destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')");
                    table.CheckConstraint("CK_advertising_creatives_order", "[display_order] >= 0");
                    table.CheckConstraint("CK_advertising_creatives_status", "[status_code] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_advertising_creatives_advertising_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "advertising_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_creatives_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_creatives_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_creatives_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_campaign_placements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    placement_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_campaign_placements", x => x.id);
                    table.ForeignKey(
                        name: "FK_advertising_campaign_placements_advertising_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "advertising_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_placements_advertising_placements_placement_id",
                        column: x => x.placement_id,
                        principalTable: "advertising_placements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_campaign_placements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "managed_content_areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    content_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managed_content_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_managed_content_areas_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_areas_managed_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "managed_contents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_areas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "managed_content_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    content_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managed_content_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_managed_content_categories_managed_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "managed_contents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_categories_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "managed_content_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content_id = table.Column<long>(type: "bigint", nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    question_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    answer_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_id = table.Column<long>(type: "bigint", nullable: true),
                    link_text = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    destination_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    destination_value = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    change_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managed_content_versions", x => x.id);
                    table.CheckConstraint("CK_managed_content_versions_destination", "[destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')");
                    table.CheckConstraint("CK_managed_content_versions_version", "[version_no] > 0");
                    table.ForeignKey(
                        name: "FK_managed_content_versions_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_versions_managed_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "managed_contents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_managed_content_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "advertising_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    creative_id = table.Column<long>(type: "bigint", nullable: false),
                    placement_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertising_events", x => x.id);
                    table.CheckConstraint("CK_advertising_events_type", "[event_type_code] IN ('IMPRESSION','CLICK')");
                    table.ForeignKey(
                        name: "FK_advertising_events_advertising_creatives_creative_id",
                        column: x => x.creative_id,
                        principalTable: "advertising_creatives",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_advertising_events_advertising_placements_placement_id",
                        column: x => x.placement_id,
                        principalTable: "advertising_placements",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "advertising_placements",
                columns: new[] { "id", "code", "created_at", "created_by_user_id", "description", "is_active", "name", "public_id", "route_hint", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { 1L, "CUSTOMER_HOME", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "고객 역할 홈 화면", true, "고객 홈", new Guid("11a10000-0000-0000-0000-000000000001"), "/customer", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2L, "PROVIDER_HOME", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "공급자 역할 홈 화면", true, "공급자 홈", new Guid("11a10000-0000-0000-0000-000000000002"), "/provider", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_areas_administrative_area_id",
                table: "advertising_campaign_areas",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_areas_campaign_id_administrative_area_id",
                table: "advertising_campaign_areas",
                columns: new[] { "campaign_id", "administrative_area_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_areas_created_by_user_id",
                table: "advertising_campaign_areas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_categories_campaign_id_category_id",
                table: "advertising_campaign_categories",
                columns: new[] { "campaign_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_categories_category_id",
                table: "advertising_campaign_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_categories_created_by_user_id",
                table: "advertising_campaign_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_placements_campaign_id_placement_id",
                table: "advertising_campaign_placements",
                columns: new[] { "campaign_id", "placement_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_placements_created_by_user_id",
                table: "advertising_campaign_placements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaign_placements_placement_id",
                table: "advertising_campaign_placements",
                column: "placement_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_approved_by_user_id",
                table: "advertising_campaigns",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_audience_type_code_start_at",
                table: "advertising_campaigns",
                columns: new[] { "audience_type_code", "start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_campaign_name",
                table: "advertising_campaigns",
                column: "campaign_name");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_created_by_user_id",
                table: "advertising_campaigns",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_public_id",
                table: "advertising_campaigns",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_status_code_review_status_code_start_at_end_at",
                table: "advertising_campaigns",
                columns: new[] { "status_code", "review_status_code", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_campaigns_updated_by_user_id",
                table: "advertising_campaigns",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_creatives_campaign_id_display_order",
                table: "advertising_creatives",
                columns: new[] { "campaign_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_creatives_created_by_user_id",
                table: "advertising_creatives",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_creatives_file_id",
                table: "advertising_creatives",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_creatives_public_id",
                table: "advertising_creatives",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_creatives_updated_by_user_id",
                table: "advertising_creatives",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_events_creative_id_placement_id_event_type_code_occurred_at",
                table: "advertising_events",
                columns: new[] { "creative_id", "placement_id", "event_type_code", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_events_placement_id",
                table: "advertising_events",
                column: "placement_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_placements_code",
                table: "advertising_placements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_placements_created_by_user_id",
                table: "advertising_placements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertising_placements_is_active_name",
                table: "advertising_placements",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_advertising_placements_public_id",
                table: "advertising_placements",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advertising_placements_updated_by_user_id",
                table: "advertising_placements",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_areas_administrative_area_id",
                table: "managed_content_areas",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_areas_content_id_administrative_area_id",
                table: "managed_content_areas",
                columns: new[] { "content_id", "administrative_area_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_areas_created_by_user_id",
                table: "managed_content_areas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_categories_category_id",
                table: "managed_content_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_categories_content_id_category_id",
                table: "managed_content_categories",
                columns: new[] { "content_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_categories_created_by_user_id",
                table: "managed_content_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_versions_content_id_version_no",
                table: "managed_content_versions",
                columns: new[] { "content_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_versions_created_by_user_id",
                table: "managed_content_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_versions_file_id",
                table: "managed_content_versions",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_content_versions_public_id",
                table: "managed_content_versions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_approved_by_user_id",
                table: "managed_contents",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_audience_type_code_start_at",
                table: "managed_contents",
                columns: new[] { "audience_type_code", "start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_content_type_code_status_code_review_status_code_start_at_end_at",
                table: "managed_contents",
                columns: new[] { "content_type_code", "status_code", "review_status_code", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_created_by_user_id",
                table: "managed_contents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_public_id",
                table: "managed_contents",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_managed_contents_updated_by_user_id",
                table: "managed_contents",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advertising_campaign_areas");

            migrationBuilder.DropTable(
                name: "advertising_campaign_categories");

            migrationBuilder.DropTable(
                name: "advertising_campaign_placements");

            migrationBuilder.DropTable(
                name: "advertising_events");

            migrationBuilder.DropTable(
                name: "managed_content_areas");

            migrationBuilder.DropTable(
                name: "managed_content_categories");

            migrationBuilder.DropTable(
                name: "managed_content_versions");

            migrationBuilder.DropTable(
                name: "advertising_creatives");

            migrationBuilder.DropTable(
                name: "advertising_placements");

            migrationBuilder.DropTable(
                name: "managed_contents");

            migrationBuilder.DropTable(
                name: "advertising_campaigns");
        }
    }
}
