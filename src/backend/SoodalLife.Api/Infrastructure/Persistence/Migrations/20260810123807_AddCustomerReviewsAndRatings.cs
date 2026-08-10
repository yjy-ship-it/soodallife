using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReviewsAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.CreateTable(
                name: "review_rating_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    min_value = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    max_value = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_rating_items", x => x.id);
                    table.CheckConstraint("CK_review_rating_items_period", "[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_review_rating_items_range", "[max_value] > [min_value]");
                    table.ForeignKey(
                        name: "FK_review_rating_items_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_review_rating_items_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    body_text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    overall_rating = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    verification_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    visibility_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    hidden_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id);
                    table.CheckConstraint("CK_reviews_verification", "[verification_status_code] = 'VERIFIED_TRANSACTION'");
                    table.CheckConstraint("CK_reviews_visibility", "[visibility_status_code] IN ('PUBLIC','HIDDEN')");
                    table.ForeignKey(
                        name: "FK_reviews_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reviews_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reviews_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "review_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    review_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_files_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_review_files_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "review_provider_replies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    review_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    body_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_provider_replies", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_provider_replies_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_review_provider_replies_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "review_ratings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    review_id = table.Column<long>(type: "bigint", nullable: false),
                    rating_item_id = table.Column<long>(type: "bigint", nullable: false),
                    rating_value = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_ratings", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_ratings_review_rating_items_rating_item_id",
                        column: x => x.rating_item_id,
                        principalTable: "review_rating_items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_review_ratings_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','REVIEW')");

            migrationBuilder.CreateIndex(
                name: "IX_review_files_file_id",
                table: "review_files",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_files_review_id_file_id",
                table: "review_files",
                columns: new[] { "review_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_provider_replies_idempotency_key",
                table: "review_provider_replies",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_provider_replies_provider_profile_id",
                table: "review_provider_replies",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_provider_replies_public_id",
                table: "review_provider_replies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_provider_replies_review_id",
                table: "review_provider_replies",
                column: "review_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_rating_items_code",
                table: "review_rating_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_rating_items_created_by_user_id",
                table: "review_rating_items",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_rating_items_is_active_display_order",
                table: "review_rating_items",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_review_rating_items_public_id",
                table: "review_rating_items",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_rating_items_updated_by_user_id",
                table: "review_rating_items",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_ratings_rating_item_id",
                table: "review_ratings",
                column: "rating_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_ratings_review_id_rating_item_id",
                table: "review_ratings",
                columns: new[] { "review_id", "rating_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_customer_profile_id",
                table: "reviews",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_idempotency_key",
                table: "reviews",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_provider_profile_id_visibility_status_code_submitted_at",
                table: "reviews",
                columns: new[] { "provider_profile_id", "visibility_status_code", "submitted_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_reviews_public_id",
                table: "reviews",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_transaction_id",
                table: "reviews",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_files");

            migrationBuilder.DropTable(
                name: "review_provider_replies");

            migrationBuilder.DropTable(
                name: "review_ratings");

            migrationBuilder.DropTable(
                name: "review_rating_items");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE')");
        }
    }
}
