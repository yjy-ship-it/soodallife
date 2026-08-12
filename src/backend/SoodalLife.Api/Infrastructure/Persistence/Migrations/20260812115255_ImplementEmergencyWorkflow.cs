using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementEmergencyWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emergency_progress_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergency_progress_events", x => x.id);
                    table.CheckConstraint("CK_emergency_progress_events_type", "[event_type_code] IN ('DISPATCH_CONFIRMED','DEPARTED','EN_ROUTE','ARRIVED')");
                    table.ForeignKey(
                        name: "FK_emergency_progress_events_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_progress_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "emergency_responses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    request_dispatch_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    eta_minutes = table.Column<int>(type: "int", nullable: true),
                    estimated_arrival_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    conditions_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    responded_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    selected_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergency_responses", x => x.id);
                    table.CheckConstraint("CK_emergency_responses_eta", "([status_code] <> 'AVAILABLE' AND [status_code] <> 'SELECTED') OR ([eta_minutes] IS NOT NULL AND [eta_minutes] > 0) OR [estimated_arrival_at] IS NOT NULL");
                    table.CheckConstraint("CK_emergency_responses_status", "[status_code] IN ('PENDING','AVAILABLE','UNAVAILABLE','EXPIRED','SELECTED','NOT_SELECTED')");
                    table.ForeignKey(
                        name: "FK_emergency_responses_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_responses_request_dispatches_request_dispatch_id",
                        column: x => x.request_dispatch_id,
                        principalTable: "request_dispatches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_responses_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_responses_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_emergency_responses_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_emergency_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    temporarily_unavailable_until = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    temporary_unavailable_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_emergency_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_emergency_settings_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_settings_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_emergency_exceptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_emergency_setting_id = table.Column<long>(type: "bigint", nullable: false),
                    starts_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ends_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_emergency_exceptions", x => x.id);
                    table.CheckConstraint("CK_provider_emergency_exceptions_period", "[ends_at] > [starts_at]");
                    table.ForeignKey(
                        name: "FK_provider_emergency_exceptions_provider_emergency_settings_provider_emergency_setting_id",
                        column: x => x.provider_emergency_setting_id,
                        principalTable: "provider_emergency_settings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_exceptions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_emergency_service_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_emergency_setting_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_emergency_service_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_emergency_service_settings_provider_emergency_settings_provider_emergency_setting_id",
                        column: x => x.provider_emergency_setting_id,
                        principalTable: "provider_emergency_settings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_service_settings_provider_service_categories_provider_service_category_id",
                        column: x => x.provider_service_category_id,
                        principalTable: "provider_service_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_service_settings_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_service_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_emergency_availability_slots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_emergency_service_setting_id = table.Column<long>(type: "bigint", nullable: false),
                    day_of_week = table.Column<byte>(type: "tinyint", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    is_24_hours = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_emergency_availability_slots", x => x.id);
                    table.CheckConstraint("CK_provider_emergency_availability_day", "[day_of_week] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_provider_emergency_availability_period", "([is_24_hours] = 1 AND [start_time] IS NULL AND [end_time] IS NULL) OR ([is_24_hours] = 0 AND [start_time] IS NOT NULL AND [end_time] IS NOT NULL AND [start_time] < [end_time])");
                    table.ForeignKey(
                        name: "FK_provider_emergency_availability_slots_provider_emergency_service_settings_provider_emergency_service_setting_id",
                        column: x => x.provider_emergency_service_setting_id,
                        principalTable: "provider_emergency_service_settings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_emergency_availability_slots_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_emergency_progress_events_actor_user_id",
                table: "emergency_progress_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_progress_events_idempotency_key",
                table: "emergency_progress_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_progress_events_public_id",
                table: "emergency_progress_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_progress_events_transaction_id_occurred_at",
                table: "emergency_progress_events",
                columns: new[] { "transaction_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_created_by_user_id",
                table: "emergency_responses",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_idempotency_key",
                table: "emergency_responses",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_provider_profile_id",
                table: "emergency_responses",
                column: "provider_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_public_id",
                table: "emergency_responses",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_request_dispatch_id",
                table: "emergency_responses",
                column: "request_dispatch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_service_request_id_status_code",
                table: "emergency_responses",
                columns: new[] { "service_request_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_emergency_responses_updated_by_user_id",
                table: "emergency_responses",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_availability_slots_created_by_user_id",
                table: "provider_emergency_availability_slots",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_availability_slots_provider_emergency_service_setting_id_day_of_week",
                table: "provider_emergency_availability_slots",
                columns: new[] { "provider_emergency_service_setting_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_availability_slots_public_id",
                table: "provider_emergency_availability_slots",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_exceptions_created_by_user_id",
                table: "provider_emergency_exceptions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_exceptions_provider_emergency_setting_id_starts_at_ends_at",
                table: "provider_emergency_exceptions",
                columns: new[] { "provider_emergency_setting_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_exceptions_public_id",
                table: "provider_emergency_exceptions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_service_settings_created_by_user_id",
                table: "provider_emergency_service_settings",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_service_settings_provider_emergency_setting_id_is_enabled",
                table: "provider_emergency_service_settings",
                columns: new[] { "provider_emergency_setting_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_service_settings_provider_service_category_id",
                table: "provider_emergency_service_settings",
                column: "provider_service_category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_service_settings_public_id",
                table: "provider_emergency_service_settings",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_service_settings_updated_by_user_id",
                table: "provider_emergency_service_settings",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_settings_created_by_user_id",
                table: "provider_emergency_settings",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_settings_provider_profile_id",
                table: "provider_emergency_settings",
                column: "provider_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_settings_public_id",
                table: "provider_emergency_settings",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_emergency_settings_updated_by_user_id",
                table: "provider_emergency_settings",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emergency_progress_events");

            migrationBuilder.DropTable(
                name: "emergency_responses");

            migrationBuilder.DropTable(
                name: "provider_emergency_availability_slots");

            migrationBuilder.DropTable(
                name: "provider_emergency_exceptions");

            migrationBuilder.DropTable(
                name: "provider_emergency_service_settings");

            migrationBuilder.DropTable(
                name: "provider_emergency_settings");
        }
    }
}
