using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal abstract class CaseMasterConfiguration<TEntity>(string tableName) : EntityConfiguration<TEntity>(tableName) where TEntity : class
{
    protected void ConfigureMaster(EntityTypeBuilder<TEntity> b)
    {
        Mapping.PublicId(b); Mapping.String(b,"Code","code",50,unicode:false); Mapping.String(b,"Name","name",100);
        Mapping.String(b,"Description","description",1000,nullable:true); Mapping.Bool(b,"IsActive","is_active",true);
        Mapping.Int(b,"DisplayOrder","display_order",0); Mapping.FullAudit(b);
        b.HasIndex("Code").IsUnique(); b.HasIndex("IsActive","DisplayOrder");
    }
}

internal sealed class ReportTypeConfiguration() : CaseMasterConfiguration<ReportType>("report_types")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReportType> b)
    {
        ConfigureMaster(b); Mapping.DateTime(b,nameof(ReportType.EffectiveFrom),"effective_from",nullable:true);
        Mapping.DateTime(b,nameof(ReportType.EffectiveTo),"effective_to",nullable:true);
        b.ToTable("report_types",t=>t.HasCheckConstraint("CK_report_types_period","[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]"));
    }
}

internal sealed class SanctionTypeConfiguration() : CaseMasterConfiguration<SanctionType>("sanction_types")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionType> b)
    {
        ConfigureMaster(b); Mapping.DateTime(b,nameof(SanctionType.EffectiveFrom),"effective_from",nullable:true);
        Mapping.DateTime(b,nameof(SanctionType.EffectiveTo),"effective_to",nullable:true);
        b.ToTable("sanction_types",t=>t.HasCheckConstraint("CK_sanction_types_period","[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]"));
    }
}

internal sealed class DisputeLiabilityTypeConfiguration() : CaseMasterConfiguration<DisputeLiabilityType>("dispute_liability_types")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DisputeLiabilityType> b)=>ConfigureMaster(b);
}

internal sealed class ReportConfiguration() : EntityConfiguration<Report>("reports")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Report> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(Report.ReporterUserId),"reporter_user_id"); Mapping.NullableLong(b,nameof(Report.ReportedUserId),"reported_user_id");
        Mapping.Long(b,nameof(Report.ReportTypeId),"report_type_id"); Mapping.NullableLong(b,nameof(Report.ServiceRequestId),"service_request_id"); Mapping.NullableLong(b,nameof(Report.TransactionId),"transaction_id");
        Mapping.NullableLong(b,nameof(Report.ReviewId),"review_id"); Mapping.NullableLong(b,nameof(Report.AfterServiceCaseId),"after_service_case_id"); Mapping.NullableLong(b,nameof(Report.DisputeCaseId),"dispute_case_id");
        Mapping.String(b,nameof(Report.Description),"description",4000); Mapping.String(b,nameof(Report.StatusCode),"status_code",30,unicode:false,defaultValue:"RECEIVED");
        Mapping.NullableLong(b,nameof(Report.AssignedAdminUserId),"assigned_admin_user_id"); Mapping.String(b,nameof(Report.ResultSummary),"result_summary",2000,nullable:true);
        Mapping.DateTime(b,nameof(Report.ReceivedAt),"received_at",utcDefault:true); Mapping.DateTime(b,nameof(Report.ResolvedAt),"resolved_at",nullable:true); Mapping.DateTime(b,nameof(Report.CancelledAt),"cancelled_at",nullable:true);
        Mapping.String(b,nameof(Report.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<Report,User>(b,nameof(Report.ReporterUserId)); Mapping.Fk<Report,User>(b,nameof(Report.ReportedUserId)); Mapping.Fk<Report,ReportType>(b,nameof(Report.ReportTypeId));
        Mapping.Fk<Report,ServiceRequest>(b,nameof(Report.ServiceRequestId)); Mapping.Fk<Report,TransactionRecord>(b,nameof(Report.TransactionId)); Mapping.Fk<Report,Review>(b,nameof(Report.ReviewId));
        Mapping.Fk<Report,AfterServiceCase>(b,nameof(Report.AfterServiceCaseId)); Mapping.Fk<Report,DisputeCase>(b,nameof(Report.DisputeCaseId)); Mapping.Fk<Report,User>(b,nameof(Report.AssignedAdminUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>x.ReporterUserId); b.HasIndex(x=>x.ReportedUserId); b.HasIndex(x=>x.ReportTypeId); b.HasIndex(x=>x.ServiceRequestId); b.HasIndex(x=>x.TransactionId); b.HasIndex(x=>x.ReviewId); b.HasIndex(x=>x.AfterServiceCaseId); b.HasIndex(x=>x.DisputeCaseId);
        b.HasIndex(x=>new{x.StatusCode,x.ReceivedAt}).IsDescending(false,true); b.HasIndex(x=>new{x.AssignedAdminUserId,x.StatusCode,x.ReceivedAt});
        b.ToTable("reports",t=>{t.HasCheckConstraint("CK_reports_status","[status_code] IN ('RECEIVED','UNDER_REVIEW','EVIDENCE_REQUESTED','RESOLVED','CANCELLED')");t.HasCheckConstraint("CK_reports_target","[reported_user_id] IS NOT NULL OR [service_request_id] IS NOT NULL OR [transaction_id] IS NOT NULL OR [review_id] IS NOT NULL OR [after_service_case_id] IS NOT NULL OR [dispute_case_id] IS NOT NULL");});
    }
}

internal sealed class ReportEvidenceConfiguration() : EntityConfiguration<ReportEvidence>("report_evidence")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReportEvidence> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ReportEvidence.ReportId),"report_id"); Mapping.Long(b,nameof(ReportEvidence.FileId),"file_id"); Mapping.Long(b,nameof(ReportEvidence.SubmittedByUserId),"submitted_by_user_id");
        Mapping.String(b,nameof(ReportEvidence.Description),"description",1000,nullable:true); Mapping.String(b,nameof(ReportEvidence.StatusCode),"status_code",20,unicode:false,defaultValue:"ACTIVE"); Mapping.DateTime(b,nameof(ReportEvidence.SubmittedAt),"submitted_at",utcDefault:true);
        Mapping.DateTime(b,nameof(ReportEvidence.WithdrawnAt),"withdrawn_at",nullable:true); Mapping.NullableLong(b,nameof(ReportEvidence.WithdrawnByUserId),"withdrawn_by_user_id"); Mapping.String(b,nameof(ReportEvidence.WithdrawalReason),"withdrawal_reason",1000,nullable:true); Mapping.CreatedAudit(b);
        Mapping.Fk<ReportEvidence,Report>(b,nameof(ReportEvidence.ReportId)); Mapping.Fk<ReportEvidence,StoredFile>(b,nameof(ReportEvidence.FileId)); Mapping.Fk<ReportEvidence,User>(b,nameof(ReportEvidence.SubmittedByUserId)); Mapping.Fk<ReportEvidence,User>(b,nameof(ReportEvidence.WithdrawnByUserId));
        b.HasIndex(x=>new{x.ReportId,x.FileId}).IsUnique(); b.HasIndex(x=>x.FileId); b.HasIndex(x=>x.StatusCode);
        b.ToTable("report_evidence",t=>t.HasCheckConstraint("CK_report_evidence_status","[status_code] IN ('ACTIVE','WITHDRAWN')"));
    }
}

internal sealed class ReportActionConfiguration() : EntityConfiguration<ReportAction>("report_actions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReportAction> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ReportAction.ReportId),"report_id"); Mapping.String(b,nameof(ReportAction.ActionTypeCode),"action_type_code",30,unicode:false);
        Mapping.String(b,nameof(ReportAction.FromStatusCode),"from_status_code",30,nullable:true,unicode:false); Mapping.String(b,nameof(ReportAction.ToStatusCode),"to_status_code",30,nullable:true,unicode:false);
        Mapping.Long(b,nameof(ReportAction.ActorUserId),"actor_user_id"); Mapping.String(b,nameof(ReportAction.Reason),"reason",1000,nullable:true); Mapping.String(b,nameof(ReportAction.Note),"note",2000,nullable:true);
        Mapping.DateTime(b,nameof(ReportAction.OccurredAt),"occurred_at",utcDefault:true); Mapping.String(b,nameof(ReportAction.IdempotencyKey),"idempotency_key",150,unicode:false);
        Mapping.Fk<ReportAction,Report>(b,nameof(ReportAction.ReportId)); Mapping.Fk<ReportAction,User>(b,nameof(ReportAction.ActorUserId)); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.ReportId,x.OccurredAt}).IsDescending(false,true);
        b.ToTable("report_actions",t=>t.HasCheckConstraint("CK_report_actions_type","[action_type_code] IN ('CREATED','ASSIGNED','STATUS_CHANGED','EVIDENCE_REQUESTED','EVIDENCE_ADDED','RESOLVED','CANCELLED')"));
    }
}

internal sealed class SanctionConfiguration() : EntityConfiguration<Sanction>("sanctions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Sanction> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(Sanction.TargetUserId),"target_user_id"); Mapping.NullableLong(b,nameof(Sanction.TargetRoleId),"target_role_id"); Mapping.NullableLong(b,nameof(Sanction.ProviderProfileId),"provider_profile_id"); Mapping.NullableLong(b,nameof(Sanction.ProviderServiceCategoryId),"provider_service_category_id"); Mapping.Long(b,nameof(Sanction.SanctionTypeId),"sanction_type_id");
        Mapping.String(b,nameof(Sanction.StatusCode),"status_code",20,unicode:false,defaultValue:"DECIDED"); Mapping.String(b,nameof(Sanction.Reason),"reason",2000); Mapping.DateTime(b,nameof(Sanction.StartAt),"start_at"); Mapping.DateTime(b,nameof(Sanction.EndAt),"end_at",nullable:true); Mapping.DateTime(b,nameof(Sanction.DecidedAt),"decided_at",utcDefault:true); Mapping.Long(b,nameof(Sanction.DecidedByUserId),"decided_by_user_id");
        Mapping.DateTime(b,nameof(Sanction.ReleasedAt),"released_at",nullable:true); Mapping.NullableLong(b,nameof(Sanction.ReleasedByUserId),"released_by_user_id"); Mapping.String(b,nameof(Sanction.ReleaseReason),"release_reason",1000,nullable:true); Mapping.String(b,nameof(Sanction.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<Sanction,User>(b,nameof(Sanction.TargetUserId)); Mapping.Fk<Sanction,Role>(b,nameof(Sanction.TargetRoleId)); Mapping.Fk<Sanction,ProviderProfile>(b,nameof(Sanction.ProviderProfileId)); Mapping.Fk<Sanction,ProviderServiceCategory>(b,nameof(Sanction.ProviderServiceCategoryId)); Mapping.Fk<Sanction,SanctionType>(b,nameof(Sanction.SanctionTypeId)); Mapping.Fk<Sanction,User>(b,nameof(Sanction.DecidedByUserId)); Mapping.Fk<Sanction,User>(b,nameof(Sanction.ReleasedByUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>x.TargetUserId); b.HasIndex(x=>x.TargetRoleId); b.HasIndex(x=>x.ProviderProfileId); b.HasIndex(x=>x.ProviderServiceCategoryId); b.HasIndex(x=>x.SanctionTypeId); b.HasIndex(x=>new{x.StatusCode,x.StartAt,x.EndAt}); b.HasIndex(x=>new{x.TargetUserId,x.StatusCode,x.StartAt});
        b.ToTable("sanctions",t=>{t.HasCheckConstraint("CK_sanctions_status","[status_code] IN ('DECIDED','ACTIVE','RELEASED','EXPIRED','CANCELLED')");t.HasCheckConstraint("CK_sanctions_period","[end_at] IS NULL OR [end_at] > [start_at]");t.HasCheckConstraint("CK_sanctions_scope","[provider_service_category_id] IS NULL OR [provider_profile_id] IS NOT NULL");});
    }
}

internal sealed class SanctionSourceConfiguration() : EntityConfiguration<SanctionSource>("sanction_sources")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionSource> b)
    {
        Mapping.Long(b,nameof(SanctionSource.SanctionId),"sanction_id"); Mapping.NullableLong(b,nameof(SanctionSource.ReportId),"report_id"); Mapping.NullableLong(b,nameof(SanctionSource.DisputeCaseId),"dispute_case_id"); Mapping.NullableLong(b,nameof(SanctionSource.ReviewId),"review_id"); Mapping.NullableLong(b,nameof(SanctionSource.AfterServiceCaseId),"after_service_case_id"); Mapping.CreatedAudit(b);
        Mapping.Fk<SanctionSource,Sanction>(b,nameof(SanctionSource.SanctionId)); Mapping.Fk<SanctionSource,Report>(b,nameof(SanctionSource.ReportId)); Mapping.Fk<SanctionSource,DisputeCase>(b,nameof(SanctionSource.DisputeCaseId)); Mapping.Fk<SanctionSource,Review>(b,nameof(SanctionSource.ReviewId)); Mapping.Fk<SanctionSource,AfterServiceCase>(b,nameof(SanctionSource.AfterServiceCaseId));
        b.HasIndex(x=>new{x.SanctionId,x.ReportId}).IsUnique().HasFilter("[report_id] IS NOT NULL"); b.HasIndex(x=>new{x.SanctionId,x.DisputeCaseId}).IsUnique().HasFilter("[dispute_case_id] IS NOT NULL"); b.HasIndex(x=>new{x.SanctionId,x.ReviewId}).IsUnique().HasFilter("[review_id] IS NOT NULL"); b.HasIndex(x=>new{x.SanctionId,x.AfterServiceCaseId}).IsUnique().HasFilter("[after_service_case_id] IS NOT NULL");
        b.ToTable("sanction_sources",t=>t.HasCheckConstraint("CK_sanction_sources_one_source","(CASE WHEN [report_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [dispute_case_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [review_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [after_service_case_id] IS NULL THEN 0 ELSE 1 END) = 1"));
    }
}

internal sealed class SanctionEventConfiguration() : EntityConfiguration<SanctionEvent>("sanction_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionEvent> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SanctionEvent.SanctionId),"sanction_id"); Mapping.String(b,nameof(SanctionEvent.ActionTypeCode),"action_type_code",30,unicode:false); Mapping.String(b,nameof(SanctionEvent.FromStatusCode),"from_status_code",20,nullable:true,unicode:false); Mapping.String(b,nameof(SanctionEvent.ToStatusCode),"to_status_code",20,unicode:false); Mapping.Long(b,nameof(SanctionEvent.ActorUserId),"actor_user_id"); Mapping.String(b,nameof(SanctionEvent.Reason),"reason",1000); Mapping.String(b,nameof(SanctionEvent.SnapshotJson),"snapshot_json",null); Mapping.DateTime(b,nameof(SanctionEvent.OccurredAt),"occurred_at",utcDefault:true); Mapping.String(b,nameof(SanctionEvent.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.DateTime(b,nameof(SanctionEvent.CreatedAt),"created_at",utcDefault:true);
        Mapping.Fk<SanctionEvent,Sanction>(b,nameof(SanctionEvent.SanctionId)); Mapping.Fk<SanctionEvent,User>(b,nameof(SanctionEvent.ActorUserId)); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SanctionId,x.OccurredAt}).IsDescending(false,true);
        b.ToTable("sanction_events",t=>{t.HasCheckConstraint("CK_sanction_events_action","[action_type_code] IN ('DECIDED','ACTIVATED','STATUS_CHANGED','RELEASED','EXPIRED','CANCELLED')");t.HasCheckConstraint("CK_sanction_events_snapshot","ISJSON([snapshot_json]) = 1");});
    }
}

internal sealed class SanctionAppealConfiguration() : EntityConfiguration<SanctionAppeal>("sanction_appeals")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionAppeal> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SanctionAppeal.SanctionId),"sanction_id"); Mapping.Long(b,nameof(SanctionAppeal.ApplicantUserId),"applicant_user_id"); Mapping.NullableLong(b,nameof(SanctionAppeal.PreviousAppealId),"previous_appeal_id"); Mapping.String(b,nameof(SanctionAppeal.Statement),"statement",4000); Mapping.String(b,nameof(SanctionAppeal.StatusCode),"status_code",20,unicode:false,defaultValue:"RECEIVED"); Mapping.NullableLong(b,nameof(SanctionAppeal.AssignedAdminUserId),"assigned_admin_user_id"); Mapping.String(b,nameof(SanctionAppeal.DecisionCode),"decision_code",20,nullable:true,unicode:false); Mapping.String(b,nameof(SanctionAppeal.DecisionReason),"decision_reason",2000,nullable:true); Mapping.DateTime(b,nameof(SanctionAppeal.SubmittedAt),"submitted_at",utcDefault:true); Mapping.DateTime(b,nameof(SanctionAppeal.DecidedAt),"decided_at",nullable:true); Mapping.String(b,nameof(SanctionAppeal.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.FullAudit(b);
        Mapping.Fk<SanctionAppeal,Sanction>(b,nameof(SanctionAppeal.SanctionId)); Mapping.Fk<SanctionAppeal,User>(b,nameof(SanctionAppeal.ApplicantUserId)); Mapping.Fk<SanctionAppeal,SanctionAppeal>(b,nameof(SanctionAppeal.PreviousAppealId)); Mapping.Fk<SanctionAppeal,User>(b,nameof(SanctionAppeal.AssignedAdminUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SanctionId,x.SubmittedAt}).IsDescending(false,true); b.HasIndex(x=>new{x.StatusCode,x.SubmittedAt}); b.HasIndex(x=>x.ApplicantUserId); b.HasIndex(x=>x.PreviousAppealId);
        b.ToTable("sanction_appeals",t=>{t.HasCheckConstraint("CK_sanction_appeals_status","[status_code] IN ('RECEIVED','UNDER_REVIEW','APPROVED','REJECTED','CLOSED')");t.HasCheckConstraint("CK_sanction_appeals_decision","[decision_code] IS NULL OR [decision_code] IN ('APPROVED','REJECTED')");});
    }
}

internal sealed class SanctionAppealEvidenceConfiguration() : EntityConfiguration<SanctionAppealEvidence>("sanction_appeal_evidence")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionAppealEvidence> b)
    {
        Mapping.Long(b,nameof(SanctionAppealEvidence.SanctionAppealId),"sanction_appeal_id"); Mapping.Long(b,nameof(SanctionAppealEvidence.FileId),"file_id"); Mapping.Long(b,nameof(SanctionAppealEvidence.SubmittedByUserId),"submitted_by_user_id"); Mapping.String(b,nameof(SanctionAppealEvidence.Description),"description",1000,nullable:true); Mapping.CreatedAudit(b);
        Mapping.Fk<SanctionAppealEvidence,SanctionAppeal>(b,nameof(SanctionAppealEvidence.SanctionAppealId)); Mapping.Fk<SanctionAppealEvidence,StoredFile>(b,nameof(SanctionAppealEvidence.FileId)); Mapping.Fk<SanctionAppealEvidence,User>(b,nameof(SanctionAppealEvidence.SubmittedByUserId)); b.HasIndex(x=>new{x.SanctionAppealId,x.FileId}).IsUnique(); b.HasIndex(x=>x.FileId);
    }
}

internal sealed class SanctionAppealActionConfiguration() : EntityConfiguration<SanctionAppealAction>("sanction_appeal_actions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SanctionAppealAction> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(SanctionAppealAction.SanctionAppealId),"sanction_appeal_id"); Mapping.String(b,nameof(SanctionAppealAction.ActionTypeCode),"action_type_code",30,unicode:false); Mapping.String(b,nameof(SanctionAppealAction.FromStatusCode),"from_status_code",20,nullable:true,unicode:false); Mapping.String(b,nameof(SanctionAppealAction.ToStatusCode),"to_status_code",20,unicode:false); Mapping.Long(b,nameof(SanctionAppealAction.ActorUserId),"actor_user_id"); Mapping.String(b,nameof(SanctionAppealAction.Reason),"reason",1000); Mapping.DateTime(b,nameof(SanctionAppealAction.OccurredAt),"occurred_at",utcDefault:true); Mapping.String(b,nameof(SanctionAppealAction.IdempotencyKey),"idempotency_key",150,unicode:false);
        Mapping.Fk<SanctionAppealAction,SanctionAppeal>(b,nameof(SanctionAppealAction.SanctionAppealId)); Mapping.Fk<SanctionAppealAction,User>(b,nameof(SanctionAppealAction.ActorUserId)); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.SanctionAppealId,x.OccurredAt}).IsDescending(false,true);
        b.ToTable("sanction_appeal_actions",t=>t.HasCheckConstraint("CK_sanction_appeal_actions_type","[action_type_code] IN ('CREATED','ASSIGNED','REVIEW_STARTED','DECIDED','CLOSED','EVIDENCE_ADDED')"));
    }
}
