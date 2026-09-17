using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class AdminSecurityProfileConfiguration() : EntityConfiguration<AdminSecurityProfile>("admin_security_profiles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdminSecurityProfile> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(AdminSecurityProfile.UserId),"user_id");
        Mapping.String(b,nameof(AdminSecurityProfile.DetailRoleCode),"detail_role_code",40,unicode:false);
        Mapping.Bool(b,nameof(AdminSecurityProfile.MfaEnabled),"mfa_enabled",false);
        Mapping.String(b,nameof(AdminSecurityProfile.TotpSecretProtected),"totp_secret_protected",2000,nullable:true);
        Mapping.DateTime(b,nameof(AdminSecurityProfile.MfaConfirmedAt),"mfa_confirmed_at",nullable:true);
        Mapping.Int(b,nameof(AdminSecurityProfile.FailedMfaAttempts),"failed_mfa_attempts",0);
        Mapping.DateTime(b,nameof(AdminSecurityProfile.LockedUntil),"locked_until",nullable:true);
        Mapping.FullAudit(b); Mapping.Fk<AdminSecurityProfile,User>(b,nameof(AdminSecurityProfile.UserId));
        b.HasIndex(x=>x.UserId).IsUnique(); b.HasIndex(x=>x.DetailRoleCode);
    }
}

internal sealed class AdminReauthenticationSessionConfiguration() : EntityConfiguration<AdminReauthenticationSession>("admin_reauthentication_sessions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdminReauthenticationSession> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(AdminReauthenticationSession.UserId),"user_id");
        Mapping.Binary(b,nameof(AdminReauthenticationSession.TokenHash),"token_hash",32);
        Mapping.DateTime(b,nameof(AdminReauthenticationSession.CreatedAt),"created_at",utcDefault:true);
        Mapping.DateTime(b,nameof(AdminReauthenticationSession.ExpiresAt),"expires_at");
        Mapping.DateTime(b,nameof(AdminReauthenticationSession.RevokedAt),"revoked_at",nullable:true);
        Mapping.String(b,nameof(AdminReauthenticationSession.PurposeCode),"purpose_code",50,unicode:false);
        Mapping.Fk<AdminReauthenticationSession,User>(b,nameof(AdminReauthenticationSession.UserId));
        b.HasIndex(x=>x.TokenHash).IsUnique(); b.HasIndex(x=>new{x.UserId,x.ExpiresAt});
    }
}

internal sealed class OutboxRetryRequestConfiguration() : EntityConfiguration<OutboxRetryRequest>("outbox_retry_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<OutboxRetryRequest> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(OutboxRetryRequest.OutboxEventId),"outbox_event_id");
        Mapping.Int(b,nameof(OutboxRetryRequest.AttemptSnapshot),"attempt_snapshot");
        Mapping.String(b,nameof(OutboxRetryRequest.IdempotencyKey),"idempotency_key",160,unicode:false);
        Mapping.String(b,nameof(OutboxRetryRequest.Reason),"reason",1000);
        Mapping.String(b,nameof(OutboxRetryRequest.StatusCode),"status_code",20,unicode:false);
        Mapping.Long(b,nameof(OutboxRetryRequest.RequestedByUserId),"requested_by_user_id");
        Mapping.DateTime(b,nameof(OutboxRetryRequest.RequestedAt),"requested_at",utcDefault:true);
        Mapping.DateTime(b,nameof(OutboxRetryRequest.ConsumedAt),"consumed_at",nullable:true);
        Mapping.Fk<OutboxRetryRequest,OutboxEvent>(b,nameof(OutboxRetryRequest.OutboxEventId));
        Mapping.Fk<OutboxRetryRequest,User>(b,nameof(OutboxRetryRequest.RequestedByUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.OutboxEventId,x.AttemptSnapshot}).IsUnique();
    }
}

internal sealed class DataRetentionPolicyConfiguration() : EntityConfiguration<DataRetentionPolicy>("data_retention_policies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DataRetentionPolicy> b)
    {
        Mapping.PublicId(b); Mapping.String(b,nameof(DataRetentionPolicy.DomainCode),"domain_code",50,unicode:false);
        Mapping.String(b,nameof(DataRetentionPolicy.ActionCode),"action_code",20,unicode:false);
        Mapping.Int(b,nameof(DataRetentionPolicy.RetentionDays),"retention_days");
        Mapping.Int(b,nameof(DataRetentionPolicy.LegalHoldDays),"legal_hold_days");
        Mapping.Bool(b,nameof(DataRetentionPolicy.IsEnabled),"is_enabled",false); Mapping.Bool(b,nameof(DataRetentionPolicy.DryRun),"dry_run",true);
        Mapping.FullAudit(b); b.HasIndex(x=>x.DomainCode).IsUnique();
    }
}

internal sealed class DataRetentionExecutionConfiguration() : EntityConfiguration<DataRetentionExecution>("data_retention_executions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DataRetentionExecution> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(DataRetentionExecution.PolicyId),"policy_id");
        Mapping.String(b,nameof(DataRetentionExecution.StatusCode),"status_code",20,unicode:false);
        Mapping.Bool(b,nameof(DataRetentionExecution.DryRun),"dry_run");
        Mapping.Int(b,nameof(DataRetentionExecution.CandidateCount),"candidate_count"); Mapping.Int(b,nameof(DataRetentionExecution.ProcessedCount),"processed_count");
        Mapping.Int(b,nameof(DataRetentionExecution.SkippedLegalHoldCount),"skipped_legal_hold_count");
        Mapping.String(b,nameof(DataRetentionExecution.ErrorCode),"error_code",100,nullable:true,unicode:false);
        Mapping.DateTime(b,nameof(DataRetentionExecution.StartedAt),"started_at",utcDefault:true); Mapping.DateTime(b,nameof(DataRetentionExecution.CompletedAt),"completed_at",nullable:true);
        Mapping.NullableLong(b,nameof(DataRetentionExecution.ExecutedByUserId),"executed_by_user_id");
        Mapping.Fk<DataRetentionExecution,DataRetentionPolicy>(b,nameof(DataRetentionExecution.PolicyId)); Mapping.Fk<DataRetentionExecution,User>(b,nameof(DataRetentionExecution.ExecutedByUserId));
        b.HasIndex(x=>new{x.PolicyId,x.StartedAt});
    }
}

internal sealed class AnalyticsEventConfiguration() : EntityConfiguration<AnalyticsEvent>("analytics_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AnalyticsEvent> b)
    {
        Mapping.PublicId(b); Mapping.String(b,nameof(AnalyticsEvent.EventTypeCode),"event_type_code",80,unicode:false);
        Mapping.Guid(b,nameof(AnalyticsEvent.VisitorId),"visitor_id"); Mapping.NullableLong(b,nameof(AnalyticsEvent.UserId),"user_id");
        Mapping.Guid(b,nameof(AnalyticsEvent.ProviderPublicId),"provider_public_id",nullable:true); Mapping.Guid(b,nameof(AnalyticsEvent.SourcePublicId),"source_public_id",nullable:true);
        Mapping.String(b,nameof(AnalyticsEvent.SourceTypeCode),"source_type_code",60,nullable:true,unicode:false);
        Mapping.String(b,nameof(AnalyticsEvent.RouteTemplate),"route_template",300,nullable:true,unicode:false); Mapping.String(b,nameof(AnalyticsEvent.HttpMethod),"http_method",10,nullable:true,unicode:false);
        b.Property(x=>x.StatusCode).HasColumnName("status_code").HasColumnType("int").IsRequired(false); b.Property(x=>x.DurationMs).HasColumnName("duration_ms").HasColumnType("int").IsRequired(false);
        Mapping.String(b,nameof(AnalyticsEvent.OutcomeCode),"outcome_code",50,nullable:true,unicode:false); Mapping.DateTime(b,nameof(AnalyticsEvent.OccurredAt),"occurred_at",utcDefault:true);
        Mapping.String(b,nameof(AnalyticsEvent.MetadataJson),"metadata_json",null,nullable:true); Mapping.Fk<AnalyticsEvent,User>(b,nameof(AnalyticsEvent.UserId));
        b.HasIndex(x=>new{x.EventTypeCode,x.OccurredAt}); b.HasIndex(x=>new{x.VisitorId,x.OccurredAt}); b.HasIndex(x=>x.ProviderPublicId);
    }
}
