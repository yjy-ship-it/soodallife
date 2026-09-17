using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberNotificationBroadcastV162 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_broadcasts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notification_id = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    audience_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    kind_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    channels_json = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    estimated_audience_count = table.Column<int>(type: "int", nullable: false),
                    recipients_processed_count = table.Column<int>(type: "int", nullable: false),
                    deliveries_created_count = table.Column<int>(type: "int", nullable: false),
                    last_processed_user_id = table.Column<long>(type: "bigint", nullable: true),
                    previewed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    confirmed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    cancelled_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_broadcasts", x => x.id);
                    table.CheckConstraint("CK_notification_broadcast_audience", "[audience_code] IN ('CUSTOMER','PROVIDER','ALL')");
                    table.CheckConstraint("CK_notification_broadcast_channels", "ISJSON([channels_json])=1");
                    table.CheckConstraint("CK_notification_broadcast_kind", "[kind_code] IN ('BUSINESS_NOTICE','MARKETING')");
                    table.CheckConstraint("CK_notification_broadcast_status", "[status_code] IN ('DRAFT','QUEUED','PREPARING','SENDING','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_cancelled_by_user_id",
                table: "notification_broadcasts",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_confirmed_by_user_id",
                table: "notification_broadcasts",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_created_at",
                table: "notification_broadcasts",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_created_by_user_id",
                table: "notification_broadcasts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_notification_id",
                table: "notification_broadcasts",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_public_id",
                table: "notification_broadcasts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_status_code_scheduled_at",
                table: "notification_broadcasts",
                columns: new[] { "status_code", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_updated_by_user_id",
                table: "notification_broadcasts",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_broadcasts");
        }
    }
}
