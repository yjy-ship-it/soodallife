using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderTrustManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trust_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policy_version = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    policy_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    target_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    scope_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    rules_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    effective_to = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_policies", x => x.id);
                    table.CheckConstraint("CK_trust_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
                    table.CheckConstraint("CK_trust_policies_rules_json", "ISJSON([rules_json]) = 1");
                    table.ForeignKey(
                        name: "FK_trust_policies_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_policies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_policies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trust_score_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    trust_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    source_type_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    source_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    score_before = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    score_delta = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    score_after = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    grade_before = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    grade_after = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    decision_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    reason_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_score_events", x => x.id);
                    table.CheckConstraint("CK_trust_score_events_policy_json", "[policy_snapshot_json] IS NULL OR ISJSON([policy_snapshot_json]) = 1");
                    table.CheckConstraint("CK_trust_score_events_score_range", "([score_before] IS NULL OR ([score_before] >= 0 AND [score_before] <= 100)) AND ([score_after] IS NULL OR ([score_after] >= 0 AND [score_after] <= 100))");
                    table.CheckConstraint("CK_trust_score_events_source_json", "[source_snapshot_json] IS NULL OR ISJSON([source_snapshot_json]) = 1");
                    table.ForeignKey(
                        name: "FK_trust_score_events_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_score_events_trust_policies_trust_policy_id",
                        column: x => x.trust_policy_id,
                        principalTable: "trust_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_score_events_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_trust_score_current",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    trust_policy_id = table.Column<long>(type: "bigint", nullable: true),
                    score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    grade_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    evaluation_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "NEW_OR_EVALUATING"),
                    calculated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    last_event_id = table.Column<long>(type: "bigint", nullable: true),
                    source_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_trust_score_current", x => x.id);
                    table.CheckConstraint("CK_provider_trust_score_current_score", "[score] IS NULL OR ([score] >= 0 AND [score] <= 100)");
                    table.CheckConstraint("CK_provider_trust_score_current_state", "([evaluation_status_code] = 'NEW_OR_EVALUATING' AND [score] IS NULL AND [grade_code] IS NULL) OR [evaluation_status_code] <> 'NEW_OR_EVALUATING'");
                    table.CheckConstraint("CK_provider_trust_score_current_status", "[evaluation_status_code] IN ('NEW_OR_EVALUATING','CALCULATED','LEGACY_UNKNOWN_POLICY')");
                    table.ForeignKey(
                        name: "FK_provider_trust_score_current_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_trust_score_current_trust_policies_trust_policy_id",
                        column: x => x.trust_policy_id,
                        principalTable: "trust_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_trust_score_current_trust_score_events_last_event_id",
                        column: x => x.last_event_id,
                        principalTable: "trust_score_events",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_current_evaluation_status_code_score",
                table: "provider_trust_score_current",
                columns: new[] { "evaluation_status_code", "score" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_current_last_event_id",
                table: "provider_trust_score_current",
                column: "last_event_id",
                unique: true,
                filter: "[last_event_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_current_provider_profile_id",
                table: "provider_trust_score_current",
                column: "provider_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_current_public_id",
                table: "provider_trust_score_current",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_current_trust_policy_id",
                table: "provider_trust_score_current",
                column: "trust_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_approved_by_user_id",
                table: "trust_policies",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_created_by_user_id",
                table: "trust_policies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_policy_version",
                table: "trust_policies",
                column: "policy_version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_public_id",
                table: "trust_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_target_type_code_scope_type_code_status_code_effective_from",
                table: "trust_policies",
                columns: new[] { "target_type_code", "scope_type_code", "status_code", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_trust_policies_updated_by_user_id",
                table: "trust_policies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_idempotency_key",
                table: "trust_score_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_processed_by_user_id",
                table: "trust_score_events",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_provider_profile_id_occurred_at",
                table: "trust_score_events",
                columns: new[] { "provider_profile_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_public_id",
                table: "trust_score_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_source_type_code_source_public_id",
                table: "trust_score_events",
                columns: new[] { "source_type_code", "source_public_id" });

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_events_trust_policy_id",
                table: "trust_score_events",
                column: "trust_policy_id");

            migrationBuilder.Sql("""
                INSERT INTO provider_trust_score_current
                    (public_id, provider_profile_id, trust_policy_id, score, grade_code, evaluation_status_code,
                     calculated_at, last_event_id, source_type_code, created_at, updated_at)
                SELECT NEWID(), p.id, NULL, p.trust_score, NULL,
                       CASE WHEN p.trust_score IS NULL THEN 'NEW_OR_EVALUATING' ELSE 'LEGACY_UNKNOWN_POLICY' END,
                       NULL, NULL, 'LEGACY_PROFILE', SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM provider_profiles p;

                IF (SELECT COUNT_BIG(*) FROM provider_trust_score_current) <> (SELECT COUNT_BIG(*) FROM provider_profiles)
                    THROW 51000, '공급자 신뢰도 Current 초기화 건수가 일치하지 않습니다.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM provider_profiles p
                    JOIN provider_trust_score_current c ON c.provider_profile_id = p.id
                    WHERE (p.trust_score <> c.score)
                       OR (p.trust_score IS NULL AND c.score IS NOT NULL)
                       OR (p.trust_score IS NOT NULL AND c.score IS NULL))
                    THROW 51000, '기존 공급자 신뢰점수 보존 검증에 실패했습니다.', 1;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_trust_score_events_append_only
                ON trust_score_events
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, '신뢰도 변경이력은 수정하거나 삭제할 수 없습니다.', 1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_trust_policies_no_overlapping_period
                ON trust_policies
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN trust_policies p
                          ON p.id <> i.id
                         AND p.target_type_code = i.target_type_code
                         AND p.scope_type_code = i.scope_type_code
                         AND i.effective_from < ISNULL(p.effective_to, CONVERT(datetime2, '9999-12-31'))
                         AND p.effective_from < ISNULL(i.effective_to, CONVERT(datetime2, '9999-12-31')))
                        THROW 51000, '동일 대상과 범위의 신뢰도 정책 적용기간은 중복될 수 없습니다.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_trust_policies_no_overlapping_period;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_trust_score_events_append_only;");

            migrationBuilder.DropTable(
                name: "provider_trust_score_current");

            migrationBuilder.DropTable(
                name: "trust_score_events");

            migrationBuilder.DropTable(
                name: "trust_policies");
        }
    }
}
