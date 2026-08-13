using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProviderExitWalletRefundClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_exit_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    wallet_refund_request_id = table.Column<long>(type: "bigint", nullable: true),
                    request_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    status_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false, defaultValue: "REQUESTED"),
                    review_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_exit_requests", x => x.id);
                    table.CheckConstraint("CK_provider_exit_requests_review", "[review_status_code] IN ('PENDING','UNDER_REVIEW','APPROVED','REJECTED')");
                    table.CheckConstraint("CK_provider_exit_requests_status", "[status_code] IN ('REQUESTED','UNDER_REVIEW','REFUND_REQUIRED','BLOCKED_BY_ACTIVE_WORK','READY_TO_COMPLETE','COMPLETED','REJECTED','CANCELLED')");
                    table.CheckConstraint("CK_provider_exit_requests_type", "[request_type_code] IN ('PROVIDER_ROLE_EXIT','ACCOUNT_WITHDRAWAL')");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_exit_requests_wallet_refund_requests_wallet_refund_request_id",
                        column: x => x.wallet_refund_request_id,
                        principalTable: "wallet_refund_requests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_created_by_user_id",
                table: "provider_exit_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_idempotency_key",
                table: "provider_exit_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_provider_profile_id",
                table: "provider_exit_requests",
                column: "provider_profile_id",
                unique: true,
                filter: "[status_code] <> 'COMPLETED' AND [status_code] <> 'REJECTED' AND [status_code] <> 'CANCELLED'");

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_provider_profile_id_status_code",
                table: "provider_exit_requests",
                columns: new[] { "provider_profile_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_public_id",
                table: "provider_exit_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_reviewed_by_user_id",
                table: "provider_exit_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_updated_by_user_id",
                table: "provider_exit_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_user_id",
                table: "provider_exit_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_exit_requests_wallet_refund_request_id",
                table: "provider_exit_requests",
                column: "wallet_refund_request_id",
                unique: true,
                filter: "[wallet_refund_request_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_exit_requests");
        }
    }
}
