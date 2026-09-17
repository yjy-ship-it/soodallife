using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicActivityReadModelV191 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "public_activity_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_request_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    source_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    source_entity_id = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_activity_events", x => x.id);
                    table.CheckConstraint("CK_public_activity_events_type", "[event_type_code] IN ('REQUEST_OPENED','QUOTE_RECEIVED','PROVIDER_SELECTED','WORK_STARTED','WORK_COMPLETED','REVIEW_PUBLISHED')");
                    table.ForeignKey(
                        name: "FK_public_activity_events_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_public_activity_events_occurred_at_event_type_code_service_request_id",
                table: "public_activity_events",
                columns: new[] { "occurred_at", "event_type_code", "service_request_id" },
                descending: new[] { true, false, false });

            migrationBuilder.CreateIndex(
                name: "IX_public_activity_events_service_request_id",
                table: "public_activity_events",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_public_activity_events_source_type_code_source_entity_id_event_type_code",
                table: "public_activity_events",
                columns: new[] { "source_type_code", "source_entity_id", "event_type_code" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT id,'REQUEST_OPENED','SERVICE_REQUEST',id,opened_at
                FROM service_requests
                WHERE opened_at >= DATEADD(DAY,-30,SYSUTCDATETIME());

                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT id,'PROVIDER_SELECTED','SERVICE_REQUEST',id,accepted_at
                FROM service_requests
                WHERE accepted_at >= DATEADD(DAY,-30,SYSUTCDATETIME());

                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT service_request_id,'QUOTE_RECEIVED','QUOTE',id,submitted_at
                FROM quotes
                WHERE submitted_at >= DATEADD(DAY,-30,SYSUTCDATETIME()) AND status_code IN ('SUBMITTED','ACCEPTED');

                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT service_request_id,'WORK_STARTED','TRANSACTION',id,started_at
                FROM transactions
                WHERE started_at >= DATEADD(DAY,-30,SYSUTCDATETIME()) AND status_code NOT IN ('DISPUTED','CANCELLED');

                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT service_request_id,'WORK_COMPLETED','TRANSACTION',id,completed_at
                FROM transactions
                WHERE completed_at >= DATEADD(DAY,-30,SYSUTCDATETIME()) AND status_code='COMPLETED';

                INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                SELECT t.service_request_id,'REVIEW_PUBLISHED','REVIEW',r.id,r.published_at
                FROM reviews r INNER JOIN transactions t ON t.id=r.transaction_id
                WHERE r.published_at >= DATEADD(DAY,-30,SYSUTCDATETIME())
                  AND r.visibility_status_code='PUBLIC' AND r.verification_status_code='VERIFIED_TRANSACTION';
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER TRIGGER TR_public_activity_service_requests
                ON service_requests AFTER INSERT,UPDATE AS
                BEGIN
                  SET NOCOUNT ON;
                  DELETE e FROM public_activity_events e INNER JOIN inserted i ON i.id=e.source_entity_id
                    WHERE e.source_type_code=''SERVICE_REQUEST'' AND e.event_type_code IN (''REQUEST_OPENED'',''PROVIDER_SELECTED'');
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT id,''REQUEST_OPENED'',''SERVICE_REQUEST'',id,opened_at FROM inserted WHERE opened_at IS NOT NULL;
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT id,''PROVIDER_SELECTED'',''SERVICE_REQUEST'',id,accepted_at FROM inserted WHERE accepted_at IS NOT NULL;
                END')
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER TRIGGER TR_public_activity_quotes
                ON quotes AFTER INSERT,UPDATE AS
                BEGIN
                  SET NOCOUNT ON;
                  DELETE e FROM public_activity_events e INNER JOIN inserted i ON i.id=e.source_entity_id
                    WHERE e.source_type_code=''QUOTE'' AND e.event_type_code=''QUOTE_RECEIVED'';
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT service_request_id,''QUOTE_RECEIVED'',''QUOTE'',id,submitted_at FROM inserted
                    WHERE submitted_at IS NOT NULL AND status_code IN (''SUBMITTED'',''ACCEPTED'');
                END')
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER TRIGGER TR_public_activity_transactions
                ON transactions AFTER INSERT,UPDATE AS
                BEGIN
                  SET NOCOUNT ON;
                  DELETE e FROM public_activity_events e INNER JOIN inserted i ON i.id=e.source_entity_id
                    WHERE e.source_type_code=''TRANSACTION'' AND e.event_type_code IN (''WORK_STARTED'',''WORK_COMPLETED'');
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT service_request_id,''WORK_STARTED'',''TRANSACTION'',id,started_at FROM inserted
                    WHERE started_at IS NOT NULL AND status_code NOT IN (''DISPUTED'',''CANCELLED'');
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT service_request_id,''WORK_COMPLETED'',''TRANSACTION'',id,completed_at FROM inserted
                    WHERE completed_at IS NOT NULL AND status_code=''COMPLETED'';
                END')
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER TRIGGER TR_public_activity_reviews
                ON reviews AFTER INSERT,UPDATE AS
                BEGIN
                  SET NOCOUNT ON;
                  DELETE e FROM public_activity_events e INNER JOIN inserted i ON i.id=e.source_entity_id
                    WHERE e.source_type_code=''REVIEW'' AND e.event_type_code=''REVIEW_PUBLISHED'';
                  INSERT INTO public_activity_events(service_request_id,event_type_code,source_type_code,source_entity_id,occurred_at)
                    SELECT t.service_request_id,''REVIEW_PUBLISHED'',''REVIEW'',i.id,i.published_at
                    FROM inserted i INNER JOIN transactions t ON t.id=i.transaction_id
                    WHERE i.published_at IS NOT NULL AND i.visibility_status_code=''PUBLIC''
                      AND i.verification_status_code=''VERIFIED_TRANSACTION'';
                END')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_public_activity_reviews");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_public_activity_transactions");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_public_activity_quotes");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_public_activity_service_requests");
            migrationBuilder.DropTable(
                name: "public_activity_events");
        }
    }
}
