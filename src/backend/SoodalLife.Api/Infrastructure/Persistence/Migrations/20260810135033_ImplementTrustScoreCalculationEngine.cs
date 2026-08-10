using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTrustScoreCalculationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_trust_calculation_results",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    trust_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    calculation_mode_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    result_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    grade_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    evaluation_status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    insufficiency_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    completed_transaction_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    verified_review_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    policy_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    calculated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    applied_trust_score_event_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_trust_calculation_results", x => x.id);
                    table.CheckConstraint("CK_trust_results_mode", "[calculation_mode_code] IN ('SIMULATION','ACTUAL')");
                    table.CheckConstraint("CK_trust_results_policy_json", "ISJSON([policy_snapshot_json]) = 1");
                    table.CheckConstraint("CK_trust_results_score", "[score] IS NULL OR ([score] >= 0 AND [score] <= 100)");
                    table.CheckConstraint("CK_trust_results_source_json", "ISJSON([source_snapshot_json]) = 1");
                    table.CheckConstraint("CK_trust_results_status", "[result_status_code] IN ('CALCULATED','INSUFFICIENT_DATA','FAILED')");
                    table.ForeignKey(
                        name: "FK_provider_trust_calculation_results_provider_profiles_provider_profile_id",
                        column: x => x.provider_profile_id,
                        principalTable: "provider_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_trust_calculation_results_trust_policies_trust_policy_id",
                        column: x => x.trust_policy_id,
                        principalTable: "trust_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_trust_calculation_results_trust_score_events_applied_trust_score_event_id",
                        column: x => x.applied_trust_score_event_id,
                        principalTable: "trust_score_events",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_trust_calculation_results_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "provider_trust_score_components",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calculation_result_id = table.Column<long>(type: "bigint", nullable: false),
                    component_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    weight = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    raw_value_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    normalized_score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    weighted_score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    sample_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_calculable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    unavailable_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    source_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    calculated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_trust_score_components", x => x.id);
                    table.CheckConstraint("CK_trust_components_raw_json", "ISJSON([raw_value_json]) = 1");
                    table.CheckConstraint("CK_trust_components_score", "([normalized_score] IS NULL OR ([normalized_score] >= 0 AND [normalized_score] <= 100)) AND ([weighted_score] IS NULL OR ([weighted_score] >= 0 AND [weighted_score] <= [weight]))");
                    table.CheckConstraint("CK_trust_components_source_json", "ISJSON([source_snapshot_json]) = 1");
                    table.ForeignKey(
                        name: "FK_provider_trust_score_components_provider_trust_calculation_results_calculation_result_id",
                        column: x => x.calculation_result_id,
                        principalTable: "provider_trust_calculation_results",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "trust_policies",
                columns: new[] { "id", "approved_at", "approved_by_user_id", "created_at", "created_by_user_id", "effective_from", "effective_to", "policy_name", "policy_version", "public_id", "rules_json", "scope_type_code", "status_code", "target_type_code", "updated_at", "updated_by_user_id" },
                values: new object[] { 1L, null, null, new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "TrustScore v1.0 정책 초안", "v1.0-draft", new Guid("f84f8728-8e1e-4ef8-a7ee-4bea94548ff0"), "{\"minimumCompletedTransactions\":3,\"minimumVerifiedReviews\":3,\"components\":[{\"code\":\"EVIDENCE\",\"weight\":15,\"ruleType\":\"EVIDENCE_COMPLETENESS\",\"settings\":{\"approvalRatio\":0.25,\"serviceApprovalRatio\":0.25,\"requiredVerificationRatio\":0.4,\"notExpiredRatio\":0.1}},{\"code\":\"TRANSACTION\",\"weight\":30,\"ruleType\":\"TRANSACTION_COMPLETION_RATE\",\"settings\":{\"completionRateRatio\":0.8,\"completionEvidenceRatio\":0.2}},{\"code\":\"REVIEW\",\"weight\":30,\"ruleType\":\"VERIFIED_PUBLIC_RATING_AVERAGE\",\"settings\":{}},{\"code\":\"AFTER_SERVICE\",\"weight\":10,\"ruleType\":\"FINALIZED_AFTER_SERVICE_OUTCOME\",\"settings\":{\"resolvedValue\":1.0,\"unresolvedValue\":0.0,\"recurrencePenalty\":0.25,\"disputeConversionPenalty\":0.25}},{\"code\":\"DISPUTE\",\"weight\":10,\"ruleType\":\"STRUCTURED_LIABILITY_MAPPING\",\"settings\":{\"liabilityScores\":{}}},{\"code\":\"SANCTION\",\"weight\":5,\"ruleType\":\"DECIDED_SANCTION_MAPPING\",\"settings\":{\"sanctionScores\":{}}}]}", "GLOBAL", "DRAFT", "PROVIDER", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.AddCheckConstraint(
                name: "CK_trust_policies_status",
                table: "trust_policies",
                sql: "[status_code] IN ('DRAFT','APPROVED','ACTIVE','RETIRED')");

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_applied_trust_score_event_id",
                table: "provider_trust_calculation_results",
                column: "applied_trust_score_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_idempotency_key",
                table: "provider_trust_calculation_results",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_provider_profile_id_calculated_at",
                table: "provider_trust_calculation_results",
                columns: new[] { "provider_profile_id", "calculated_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_public_id",
                table: "provider_trust_calculation_results",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_requested_by_user_id",
                table: "provider_trust_calculation_results",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_calculation_results_trust_policy_id_calculation_mode_code_calculated_at",
                table: "provider_trust_calculation_results",
                columns: new[] { "trust_policy_id", "calculation_mode_code", "calculated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_components_calculation_result_id_component_code",
                table: "provider_trust_score_components",
                columns: new[] { "calculation_result_id", "component_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_trust_score_components_public_id",
                table: "provider_trust_score_components",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_trust_score_components");

            migrationBuilder.DropTable(
                name: "provider_trust_calculation_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_trust_policies_status",
                table: "trust_policies");

            migrationBuilder.DeleteData(
                table: "trust_policies",
                keyColumn: "id",
                keyValue: 1L);
        }
    }
}
