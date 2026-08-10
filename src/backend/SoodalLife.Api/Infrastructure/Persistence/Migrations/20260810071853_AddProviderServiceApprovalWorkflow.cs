using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderServiceApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_service_approval_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    to_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    action_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    decision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_approval_events", x => x.id);
                    table.CheckConstraint("CK_provider_service_approval_events_action", "[action_code] IN ('APPROVE','REJECT','SUSPEND','REOPEN')");
                    table.CheckConstraint("CK_provider_service_approval_events_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.CheckConstraint("CK_provider_service_approval_events_to_status", "[to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.ForeignKey(
                        name: "FK_provider_service_approval_events_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_approval_events_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_service_approvals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    approval_status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    approval_requested_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    approval_decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approval_decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decision_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_approvals", x => x.id);
                    table.CheckConstraint("CK_provider_service_approvals_status", "[approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
                    table.ForeignKey(
                        name: "FK_provider_service_approvals_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_approvals_users_approval_decided_by_user_id",
                        column: x => x.approval_decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_approvals_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_service_approvals_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.Sql(
                """
                INSERT INTO provider_service_approvals
                    (public_id, provider_service_category_id, approval_status_code, approval_requested_at,
                     approval_decided_at, approval_decided_by_user_id, decision_reason,
                     created_at, created_by_user_id, updated_at, updated_by_user_id)
                SELECT NEWID(), id, 'PENDING', activated_at,
                       NULL, NULL, NULL,
                       SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL
                FROM provider_service_categories;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approval_events_decided_by_user_id",
                table: "provider_service_approval_events",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approval_events_provider_service_category_id",
                table: "provider_service_approval_events",
                column: "provider_service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approval_events_provider_service_category_id_decided_at",
                table: "provider_service_approval_events",
                columns: new[] { "provider_service_category_id", "decided_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approval_events_public_id",
                table: "provider_service_approval_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_approval_decided_by_user_id",
                table: "provider_service_approvals",
                column: "approval_decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_approval_requested_at",
                table: "provider_service_approvals",
                column: "approval_requested_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_approval_status_code",
                table: "provider_service_approvals",
                column: "approval_status_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_created_by_user_id",
                table: "provider_service_approvals",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_provider_service_category_id",
                table: "provider_service_approvals",
                column: "provider_service_category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_public_id",
                table: "provider_service_approvals",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_approvals_updated_by_user_id",
                table: "provider_service_approvals",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_service_approval_events");

            migrationBuilder.DropTable(
                name: "provider_service_approvals");
        }
    }
}
