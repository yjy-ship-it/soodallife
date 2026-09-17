using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpRoomAndSuggestionBoxV163R2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "help_posts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_user_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    administrative_area_id = table.Column<long>(type: "bigint", nullable: true),
                    region_disclosure_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "HIDDEN"),
                    purpose_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    intent_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    intent_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    converted_service_request_id = table.Column<long>(type: "bigint", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_help_posts", x => x.id);
                    table.CheckConstraint("CK_help_posts_intent", "[intent_code] IN ('ADVICE','QUOTE_RECOMMENDED','DANGEROUS')");
                    table.CheckConstraint("CK_help_posts_region", "[region_disclosure_code] IN ('HIDDEN','SIGUNGU')");
                    table.CheckConstraint("CK_help_posts_status", "[status_code] IN ('PUBLISHED','RESOLVED','CONVERTED','HIDDEN')");
                    table.ForeignKey(
                        name: "FK_help_posts_administrative_areas_administrative_area_id",
                        column: x => x.administrative_area_id,
                        principalTable: "administrative_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_posts_service_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_posts_service_requests_converted_service_request_id",
                        column: x => x.converted_service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_posts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_posts_users_customer_user_id",
                        column: x => x.customer_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_posts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_suggestions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    visibility_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PRIVATE"),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    page_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    device_info = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    app_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    assigned_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    admin_reply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    release_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    content_hash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_suggestions_users_assigned_admin_user_id",
                        column: x => x.assigned_admin_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_suggestions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_suggestions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_suggestions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "help_post_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    help_post_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_help_post_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_help_post_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_post_files_help_posts_help_post_id",
                        column: x => x.help_post_id,
                        principalTable: "help_posts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "help_room_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    help_post_id = table.Column<long>(type: "bigint", nullable: false),
                    author_user_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: true),
                    author_role_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    entry_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cause_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    check_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    diy_steps_text = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    risk_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    next_step_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    requires_professional = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    safety_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_help_room_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_help_room_entries_help_posts_help_post_id",
                        column: x => x.help_post_id,
                        principalTable: "help_posts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_room_entries_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_room_entries_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_suggestion_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_suggestion_id = table.Column<long>(type: "bigint", nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    action_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_suggestion_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_suggestion_events_user_suggestions_user_suggestion_id",
                        column: x => x.user_suggestion_id,
                        principalTable: "user_suggestions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_suggestion_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "help_post_resolutions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    help_post_id = table.Column<long>(type: "bigint", nullable: false),
                    resolved_by_customer_user_id = table.Column<long>(type: "bigint", nullable: false),
                    resolution_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    helpful_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_help_post_resolutions", x => x.id);
                    table.ForeignKey(
                        name: "FK_help_post_resolutions_help_posts_help_post_id",
                        column: x => x.help_post_id,
                        principalTable: "help_posts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_post_resolutions_help_room_entries_helpful_entry_id",
                        column: x => x.helpful_entry_id,
                        principalTable: "help_room_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_help_post_resolutions_users_resolved_by_customer_user_id",
                        column: x => x.resolved_by_customer_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_help_post_files_file_id",
                table: "help_post_files",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_post_files_help_post_id_display_order",
                table: "help_post_files",
                columns: new[] { "help_post_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_help_post_files_help_post_id_file_id",
                table: "help_post_files",
                columns: new[] { "help_post_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_help_post_files_public_id",
                table: "help_post_files",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_help_post_resolutions_help_post_id",
                table: "help_post_resolutions",
                column: "help_post_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_help_post_resolutions_helpful_entry_id",
                table: "help_post_resolutions",
                column: "helpful_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_post_resolutions_public_id",
                table: "help_post_resolutions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_help_post_resolutions_resolved_by_customer_user_id",
                table: "help_post_resolutions",
                column: "resolved_by_customer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_administrative_area_id",
                table: "help_posts",
                column: "administrative_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_category_id",
                table: "help_posts",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_converted_service_request_id",
                table: "help_posts",
                column: "converted_service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_created_by_user_id",
                table: "help_posts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_customer_user_id",
                table: "help_posts",
                column: "customer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_public_id",
                table: "help_posts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_status_code_created_at",
                table: "help_posts",
                columns: new[] { "status_code", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_help_posts_updated_by_user_id",
                table: "help_posts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_room_entries_author_user_id",
                table: "help_room_entries",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_room_entries_help_post_id_created_at",
                table: "help_room_entries",
                columns: new[] { "help_post_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_help_room_entries_provider_profile_id",
                table: "help_room_entries",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_help_room_entries_public_id",
                table: "help_room_entries",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestion_events_actor_user_id",
                table: "user_suggestion_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestion_events_public_id",
                table: "user_suggestion_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestion_events_user_suggestion_id_occurred_at",
                table: "user_suggestion_events",
                columns: new[] { "user_suggestion_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_assigned_admin_user_id",
                table: "user_suggestions",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_created_by_user_id",
                table: "user_suggestions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_idempotency_key",
                table: "user_suggestions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_public_id",
                table: "user_suggestions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_updated_by_user_id",
                table: "user_suggestions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_user_id_content_hash_created_at",
                table: "user_suggestions",
                columns: new[] { "user_id", "content_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_suggestions_user_id_created_at",
                table: "user_suggestions",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "help_post_files");

            migrationBuilder.DropTable(
                name: "help_post_resolutions");

            migrationBuilder.DropTable(
                name: "user_suggestion_events");

            migrationBuilder.DropTable(
                name: "help_room_entries");

            migrationBuilder.DropTable(
                name: "user_suggestions");

            migrationBuilder.DropTable(
                name: "help_posts");
        }
    }
}
