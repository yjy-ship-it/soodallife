using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class TrustPolicyConfiguration() : EntityConfiguration<TrustPolicy>("trust_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TrustPolicy> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(TrustPolicy.PolicyVersion), "policy_version", 50, unicode: false);
        Mapping.String(b, nameof(TrustPolicy.PolicyName), "policy_name", 200);
        Mapping.String(b, nameof(TrustPolicy.TargetTypeCode), "target_type_code", 30, unicode: false);
        Mapping.String(b, nameof(TrustPolicy.ScopeTypeCode), "scope_type_code", 30, unicode: false);
        Mapping.String(b, nameof(TrustPolicy.StatusCode), "status_code", 20, unicode: false);
        Mapping.String(b, nameof(TrustPolicy.RulesJson), "rules_json", null);
        Mapping.DateTime(b, nameof(TrustPolicy.EffectiveFrom), "effective_from");
        Mapping.DateTime(b, nameof(TrustPolicy.EffectiveTo), "effective_to", nullable: true);
        Mapping.DateTime(b, nameof(TrustPolicy.ApprovedAt), "approved_at", nullable: true);
        Mapping.NullableLong(b, nameof(TrustPolicy.ApprovedByUserId), "approved_by_user_id");
        Mapping.FullAudit(b);
        Mapping.Fk<TrustPolicy, User>(b, nameof(TrustPolicy.ApprovedByUserId));
        b.HasIndex(x => x.PolicyVersion).IsUnique();
        b.HasIndex(x => new { x.TargetTypeCode, x.ScopeTypeCode, x.StatusCode, x.EffectiveFrom });
        b.ToTable("trust_policies", t =>
        {
            t.HasCheckConstraint("CK_trust_policies_period", "[effective_to] IS NULL OR [effective_to] > [effective_from]");
            t.HasCheckConstraint("CK_trust_policies_rules_json", "ISJSON([rules_json]) = 1");
        });
    }
}

internal sealed class ProviderTrustScoreCurrentConfiguration() : EntityConfiguration<ProviderTrustScoreCurrent>("provider_trust_score_current")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderTrustScoreCurrent> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderTrustScoreCurrent.ProviderProfileId), "provider_profile_id");
        Mapping.NullableLong(b, nameof(ProviderTrustScoreCurrent.TrustPolicyId), "trust_policy_id");
        Mapping.Decimal(b, nameof(ProviderTrustScoreCurrent.Score), "score", nullable: true, precision: 9, scale: 4);
        Mapping.String(b, nameof(ProviderTrustScoreCurrent.GradeCode), "grade_code", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderTrustScoreCurrent.EvaluationStatusCode), "evaluation_status_code", 30, unicode: false, defaultValue: "NEW_OR_EVALUATING");
        Mapping.DateTime(b, nameof(ProviderTrustScoreCurrent.CalculatedAt), "calculated_at", nullable: true);
        Mapping.NullableLong(b, nameof(ProviderTrustScoreCurrent.LastEventId), "last_event_id");
        Mapping.String(b, nameof(ProviderTrustScoreCurrent.SourceTypeCode), "source_type_code", 30, unicode: false);
        Mapping.DateTime(b, nameof(ProviderTrustScoreCurrent.CreatedAt), "created_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ProviderTrustScoreCurrent.UpdatedAt), "updated_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<ProviderTrustScoreCurrent, ProviderProfile>(b, nameof(ProviderTrustScoreCurrent.ProviderProfileId));
        Mapping.Fk<ProviderTrustScoreCurrent, TrustPolicy>(b, nameof(ProviderTrustScoreCurrent.TrustPolicyId));
        Mapping.Fk<ProviderTrustScoreCurrent, TrustScoreEvent>(b, nameof(ProviderTrustScoreCurrent.LastEventId));
        b.HasIndex(x => x.ProviderProfileId).IsUnique();
        b.HasIndex(x => x.TrustPolicyId);
        b.HasIndex(x => x.LastEventId).IsUnique().HasFilter("[last_event_id] IS NOT NULL");
        b.HasIndex(x => new { x.EvaluationStatusCode, x.Score }).IsDescending(false, true);
        b.ToTable("provider_trust_score_current", t =>
        {
            t.HasCheckConstraint("CK_provider_trust_score_current_score", "[score] IS NULL OR ([score] >= 0 AND [score] <= 100)");
            t.HasCheckConstraint("CK_provider_trust_score_current_status", "[evaluation_status_code] IN ('NEW_OR_EVALUATING','CALCULATED','LEGACY_UNKNOWN_POLICY')");
            t.HasCheckConstraint("CK_provider_trust_score_current_state", "([evaluation_status_code] = 'NEW_OR_EVALUATING' AND [score] IS NULL AND [grade_code] IS NULL) OR [evaluation_status_code] <> 'NEW_OR_EVALUATING'");
        });
    }
}

internal sealed class TrustScoreEventConfiguration() : EntityConfiguration<TrustScoreEvent>("trust_score_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TrustScoreEvent> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(TrustScoreEvent.ProviderProfileId), "provider_profile_id");
        Mapping.NullableLong(b, nameof(TrustScoreEvent.TrustPolicyId), "trust_policy_id");
        Mapping.String(b, nameof(TrustScoreEvent.EventTypeCode), "event_type_code", 50, unicode: false);
        Mapping.String(b, nameof(TrustScoreEvent.SourceTypeCode), "source_type_code", 50, unicode: false);
        Mapping.Guid(b, nameof(TrustScoreEvent.SourcePublicId), "source_public_id", nullable: true);
        Mapping.String(b, nameof(TrustScoreEvent.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.Decimal(b, nameof(TrustScoreEvent.ScoreBefore), "score_before", nullable: true, precision: 9, scale: 4);
        Mapping.Decimal(b, nameof(TrustScoreEvent.ScoreDelta), "score_delta", nullable: true, precision: 9, scale: 4);
        Mapping.Decimal(b, nameof(TrustScoreEvent.ScoreAfter), "score_after", nullable: true, precision: 9, scale: 4);
        Mapping.String(b, nameof(TrustScoreEvent.GradeBefore), "grade_before", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(TrustScoreEvent.GradeAfter), "grade_after", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(TrustScoreEvent.DecisionCode), "decision_code", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(TrustScoreEvent.ReasonText), "reason_text", 2000, nullable: true);
        Mapping.String(b, nameof(TrustScoreEvent.PolicySnapshotJson), "policy_snapshot_json", null, nullable: true);
        Mapping.String(b, nameof(TrustScoreEvent.SourceSnapshotJson), "source_snapshot_json", null, nullable: true);
        Mapping.DateTime(b, nameof(TrustScoreEvent.OccurredAt), "occurred_at");
        Mapping.DateTime(b, nameof(TrustScoreEvent.ProcessedAt), "processed_at", nullable: true);
        Mapping.NullableLong(b, nameof(TrustScoreEvent.ProcessedByUserId), "processed_by_user_id");
        Mapping.DateTime(b, nameof(TrustScoreEvent.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<TrustScoreEvent, ProviderProfile>(b, nameof(TrustScoreEvent.ProviderProfileId));
        Mapping.Fk<TrustScoreEvent, TrustPolicy>(b, nameof(TrustScoreEvent.TrustPolicyId));
        Mapping.Fk<TrustScoreEvent, User>(b, nameof(TrustScoreEvent.ProcessedByUserId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.ProviderProfileId, x.OccurredAt }).IsDescending(false, true);
        b.HasIndex(x => x.TrustPolicyId);
        b.HasIndex(x => new { x.SourceTypeCode, x.SourcePublicId });
        b.ToTable("trust_score_events", t =>
        {
            t.HasCheckConstraint("CK_trust_score_events_score_range", "([score_before] IS NULL OR ([score_before] >= 0 AND [score_before] <= 100)) AND ([score_after] IS NULL OR ([score_after] >= 0 AND [score_after] <= 100))");
            t.HasCheckConstraint("CK_trust_score_events_policy_json", "[policy_snapshot_json] IS NULL OR ISJSON([policy_snapshot_json]) = 1");
            t.HasCheckConstraint("CK_trust_score_events_source_json", "[source_snapshot_json] IS NULL OR ISJSON([source_snapshot_json]) = 1");
        });
    }
}
