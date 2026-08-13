using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCustomerProviderUserBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_relationship_blocks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    direction_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    reason_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    private_memo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    released_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    released_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    release_idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_relationship_blocks", x => x.id);
                    table.CheckConstraint("CK_user_relationship_blocks_direction", "[direction_code] IN ('CUSTOMER_TO_PROVIDER','PROVIDER_TO_CUSTOMER')");
                    table.CheckConstraint("CK_user_relationship_blocks_status", "[status_code] IN ('ACTIVE','RELEASED')");
                    table.ForeignKey(
                        name: "FK_user_relationship_blocks_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_relationship_blocks_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_relationship_blocks_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_relationship_blocks_users_released_by_user_id",
                        column: x => x.released_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_relationship_blocks_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_created_by_user_id",
                table: "user_relationship_blocks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_customer_profile_id_provider_profile_id_direction_code",
                table: "user_relationship_blocks",
                columns: new[] { "customer_profile_id", "provider_profile_id", "direction_code" },
                unique: true,
                filter: "[status_code] = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_customer_profile_id_status_code_created_at",
                table: "user_relationship_blocks",
                columns: new[] { "customer_profile_id", "status_code", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_idempotency_key",
                table: "user_relationship_blocks",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_provider_profile_id",
                table: "user_relationship_blocks",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_public_id",
                table: "user_relationship_blocks",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_release_idempotency_key",
                table: "user_relationship_blocks",
                column: "release_idempotency_key",
                unique: true,
                filter: "[release_idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_released_by_user_id",
                table: "user_relationship_blocks",
                column: "released_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_relationship_blocks_updated_by_user_id",
                table: "user_relationship_blocks",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_relationship_blocks");
        }
    }
}
