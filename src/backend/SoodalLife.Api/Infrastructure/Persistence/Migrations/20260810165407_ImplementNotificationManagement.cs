using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementNotificationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_notification_deliveries_channel",
                table: "notification_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notification_deliveries_status",
                table: "notification_deliveries");

            migrationBuilder.AddColumn<long>(
                name: "after_service_case_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "dispute_case_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "notifications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "interior_project_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority_code",
                table: "notifications",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "NORMAL");

            migrationBuilder.AddColumn<long>(
                name: "quote_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "review_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "sanction_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "service_request_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_public_id",
                table: "notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type_code",
                table: "notifications",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "subscription_contract_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "subscription_visit_schedule_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_public_id",
                table: "notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_type_code",
                table: "notifications",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_code_snapshot",
                table: "notifications",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "template_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "transaction_id",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "delivered_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_provider_code",
                table: "notification_deliveries",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "failed_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "notification_recipient_id",
                table: "notification_deliveries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "notification_deliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "notification_deliveries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "notification_deliveries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "notification_deliveries",
                type: "datetime2(7)",
                precision: 7,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "notification_delivery_attempts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notification_delivery_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_no = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    result_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    provider_response_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_attempts_notification_deliveries_notification_delivery_id",
                        column: x => x.notification_delivery_id,
                        principalTable: "notification_deliveries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    event_group_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    web_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    kakao_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sms_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    email_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    push_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_recipients",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    recipient_role_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    archived_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_recipients", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_recipients_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_recipients_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    audience_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    channel_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    title_template = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body_template = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    allowed_variables_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_required_business_notice = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_marketing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_notification_templates", x => x.id);
                    table.CheckConstraint("CK_notification_templates_audience", "[audience_type_code] IN ('CUSTOMER','PROVIDER','ADMIN','ALL')");
                    table.CheckConstraint("CK_notification_templates_channel", "[channel_code] IN ('WEB','KAKAO','SMS','EMAIL','PUSH')");
                    table.CheckConstraint("CK_notification_templates_period", "[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_notification_templates_variables_json", "ISJSON([allowed_variables_json]) = 1");
                    table.ForeignKey(
                        name: "FK_notification_templates_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_templates_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    notification_recipient_id = table.Column<long>(type: "bigint", nullable: true),
                    notification_delivery_id = table.Column<long>(type: "bigint", nullable: true),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(180)", unicode: false, maxLength: 180, nullable: false),
                    event_data_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_events", x => x.id);
                    table.CheckConstraint("CK_notification_events_data_json", "[event_data_json] IS NULL OR ISJSON([event_data_json]) = 1");
                    table.ForeignKey(
                        name: "FK_notification_events_notification_deliveries_notification_delivery_id",
                        column: x => x.notification_delivery_id,
                        principalTable: "notification_deliveries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_events_notification_recipients_notification_recipient_id",
                        column: x => x.notification_recipient_id,
                        principalTable: "notification_recipients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_events_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.Sql(
                """
                UPDATE [notification_deliveries]
                SET [public_id] = NEWID()
                WHERE [public_id] IS NULL;

                INSERT INTO [notification_recipients]
                    ([public_id], [notification_id], [user_id], [recipient_role_code], [read_at], [archived_at], [created_at])
                SELECT
                    NEWID(),
                    n.[id],
                    n.[recipient_user_id],
                    COALESCE((
                        SELECT TOP (1) r.[code]
                        FROM [user_roles] ur
                        INNER JOIN [roles] r ON r.[id] = ur.[role_id]
                        WHERE ur.[user_id] = n.[recipient_user_id]
                          AND ur.[revoked_at] IS NULL
                          AND r.[is_active] = 1
                        ORDER BY CASE r.[code]
                            WHEN 'ADMIN' THEN 1
                            WHEN 'PROVIDER' THEN 2
                            WHEN 'CUSTOMER' THEN 3
                            ELSE 4
                        END
                    ), 'CUSTOMER'),
                    n.[read_at],
                    NULL,
                    n.[recorded_at]
                FROM [notifications] n
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [notification_recipients] nr
                    WHERE nr.[notification_id] = n.[id]
                      AND nr.[user_id] = n.[recipient_user_id]
                );

                UPDATE d
                SET d.[notification_recipient_id] = nr.[id]
                FROM [notification_deliveries] d
                INNER JOIN [notifications] n ON n.[id] = d.[notification_id]
                INNER JOIN [notification_recipients] nr
                    ON nr.[notification_id] = n.[id]
                   AND nr.[user_id] = n.[recipient_user_id]
                WHERE d.[notification_recipient_id] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                table: "notification_deliveries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_after_service_case_id",
                table: "notifications",
                column: "after_service_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_dispute_case_id",
                table: "notifications",
                column: "dispute_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_interior_project_id",
                table: "notifications",
                column: "interior_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_quote_id",
                table: "notifications",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_review_id",
                table: "notifications",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_sanction_id",
                table: "notifications",
                column: "sanction_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_service_request_id",
                table: "notifications",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_subscription_contract_id",
                table: "notifications",
                column: "subscription_contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_subscription_visit_schedule_id",
                table: "notifications",
                column: "subscription_visit_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_template_id",
                table: "notifications",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_transaction_id",
                table: "notifications",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_notification_recipient_id",
                table: "notification_deliveries",
                column: "notification_recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_public_id",
                table: "notification_deliveries",
                column: "public_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_notification_deliveries_channel",
                table: "notification_deliveries",
                sql: "[channel_code] IN ('WEB','IN_APP','KAKAO','ALIMTALK','SMS','EMAIL','PUSH')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notification_deliveries_status",
                table: "notification_deliveries",
                sql: "[status_code] IN ('PENDING','PROCESSING','SENT','DELIVERED','FAILED','CANCELLED','SKIPPED')");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_notification_delivery_id_attempt_no",
                table: "notification_delivery_attempts",
                columns: new[] { "notification_delivery_id", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_public_id",
                table: "notification_delivery_attempts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_actor_user_id",
                table: "notification_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_idempotency_key",
                table: "notification_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_delivery_id",
                table: "notification_events",
                column: "notification_delivery_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_id_occurred_at",
                table: "notification_events",
                columns: new[] { "notification_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_recipient_id",
                table: "notification_events",
                column: "notification_recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_public_id",
                table: "notification_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_public_id",
                table: "notification_preferences",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id_event_group_code",
                table: "notification_preferences",
                columns: new[] { "user_id", "event_group_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_notification_id_user_id",
                table: "notification_recipients",
                columns: new[] { "notification_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_public_id",
                table: "notification_recipients",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_user_id_archived_at_read_at_created_at",
                table: "notification_recipients",
                columns: new[] { "user_id", "archived_at", "read_at", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_created_by_user_id",
                table: "notification_templates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_event_type_code_is_active",
                table: "notification_templates",
                columns: new[] { "event_type_code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_public_id",
                table: "notification_templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_template_code_channel_code",
                table: "notification_templates",
                columns: new[] { "template_code", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_updated_by_user_id",
                table: "notification_templates",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_deliveries_notification_recipients_notification_recipient_id",
                table: "notification_deliveries",
                column: "notification_recipient_id",
                principalTable: "notification_recipients",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_after_service_cases_after_service_case_id",
                table: "notifications",
                column: "after_service_case_id",
                principalTable: "after_service_cases",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_dispute_cases_dispute_case_id",
                table: "notifications",
                column: "dispute_case_id",
                principalTable: "dispute_cases",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_interior_projects_interior_project_id",
                table: "notifications",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_notification_templates_template_id",
                table: "notifications",
                column: "template_id",
                principalTable: "notification_templates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_quotes_quote_id",
                table: "notifications",
                column: "quote_id",
                principalTable: "quotes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_reviews_review_id",
                table: "notifications",
                column: "review_id",
                principalTable: "reviews",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_sanctions_sanction_id",
                table: "notifications",
                column: "sanction_id",
                principalTable: "sanctions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_service_requests_service_request_id",
                table: "notifications",
                column: "service_request_id",
                principalTable: "service_requests",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_subscription_contracts_subscription_contract_id",
                table: "notifications",
                column: "subscription_contract_id",
                principalTable: "subscription_contracts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "notifications",
                column: "subscription_visit_schedule_id",
                principalTable: "subscription_visit_schedules",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_transactions_transaction_id",
                table: "notifications",
                column: "transaction_id",
                principalTable: "transactions",
                principalColumn: "id");

            migrationBuilder.Sql(
                """
                EXEC(N'CREATE TRIGGER [TR_notification_events_append_only]
                ON [notification_events]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''notification_events is append-only.'', 1;
                END;')
                """);

            migrationBuilder.Sql(
                """
                EXEC(N'CREATE TRIGGER [TR_notification_delivery_attempts_append_only]
                ON [notification_delivery_attempts]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''notification_delivery_attempts is append-only.'', 1;
                END;')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_notification_delivery_attempts_append_only];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_notification_events_append_only];");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_deliveries_notification_recipients_notification_recipient_id",
                table: "notification_deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_after_service_cases_after_service_case_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_dispute_cases_dispute_case_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_interior_projects_interior_project_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_notification_templates_template_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_quotes_quote_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_reviews_review_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_sanctions_sanction_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_service_requests_service_request_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_subscription_contracts_subscription_contract_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_subscription_visit_schedules_subscription_visit_schedule_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_transactions_transaction_id",
                table: "notifications");

            migrationBuilder.DropTable(
                name: "notification_delivery_attempts");

            migrationBuilder.DropTable(
                name: "notification_events");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "notification_recipients");

            migrationBuilder.DropIndex(
                name: "IX_notifications_after_service_case_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_dispute_case_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_interior_project_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_quote_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_review_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_sanction_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_service_request_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_subscription_contract_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_subscription_visit_schedule_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_template_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_transaction_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notification_deliveries_notification_recipient_id",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "IX_notification_deliveries_public_id",
                table: "notification_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notification_deliveries_channel",
                table: "notification_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notification_deliveries_status",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "after_service_case_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "dispute_case_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "interior_project_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "priority_code",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "quote_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "review_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "sanction_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "service_request_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "source_public_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "source_type_code",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "subscription_contract_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "subscription_visit_schedule_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "target_public_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "target_type_code",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "template_code_snapshot",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "template_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "external_provider_code",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "notification_recipient_id",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_deliveries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notification_deliveries_channel",
                table: "notification_deliveries",
                sql: "[channel_code] IN ('IN_APP','ALIMTALK')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notification_deliveries_status",
                table: "notification_deliveries",
                sql: "[status_code] IN ('PENDING','SENT','FAILED','SKIPPED')");
        }
    }
}
