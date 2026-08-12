using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class DisputeCaseConfiguration() : EntityConfiguration<DisputeCase>("dispute_cases")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DisputeCase> b)
    {
        Mapping.PublicId(b); Mapping.NullableLong(b,nameof(DisputeCase.TransactionId),"transaction_id"); Mapping.NullableLong(b,nameof(DisputeCase.SubscriptionVisitScheduleId),"subscription_visit_schedule_id"); Mapping.NullableLong(b,nameof(DisputeCase.AfterServiceCaseId),"after_service_case_id"); Mapping.NullableLong(b,nameof(DisputeCase.InteriorProjectId),"interior_project_id"); Mapping.NullableLong(b,nameof(DisputeCase.InteriorContractChangeId),"interior_contract_change_id"); Mapping.NullableLong(b,nameof(DisputeCase.InteriorWorkStageId),"interior_work_stage_id"); Mapping.Long(b,nameof(DisputeCase.ApplicantUserId),"applicant_user_id"); Mapping.Long(b,nameof(DisputeCase.CounterpartyUserId),"counterparty_user_id"); Mapping.NullableLong(b,nameof(DisputeCase.AssignedAdminUserId),"assigned_admin_user_id"); Mapping.String(b,nameof(DisputeCase.Subject),"subject",200); Mapping.String(b,nameof(DisputeCase.Description),"description",null); Mapping.String(b,nameof(DisputeCase.StatusCode),"status_code",30,unicode:false,defaultValue:"OPEN"); Mapping.DateTime(b,nameof(DisputeCase.ReceivedAt),"received_at",utcDefault:true); Mapping.DateTime(b,nameof(DisputeCase.DueAt),"due_at",nullable:true); Mapping.DateTime(b,nameof(DisputeCase.ResolvedAt),"resolved_at",nullable:true); Mapping.DateTime(b,nameof(DisputeCase.ClosedAt),"closed_at",nullable:true); Mapping.DateTime(b,nameof(DisputeCase.LastActionAt),"last_action_at",nullable:true); Mapping.FullAudit(b);
        Mapping.Fk<DisputeCase,TransactionRecord>(b,nameof(DisputeCase.TransactionId)); Mapping.Fk<DisputeCase,SubscriptionVisitSchedule>(b,nameof(DisputeCase.SubscriptionVisitScheduleId)); Mapping.Fk<DisputeCase,AfterServiceCase>(b,nameof(DisputeCase.AfterServiceCaseId)); Mapping.Fk<DisputeCase,InteriorProject>(b,nameof(DisputeCase.InteriorProjectId)); Mapping.Fk<DisputeCase,InteriorContractChange>(b,nameof(DisputeCase.InteriorContractChangeId)); Mapping.Fk<DisputeCase,InteriorWorkStage>(b,nameof(DisputeCase.InteriorWorkStageId)); Mapping.Fk<DisputeCase,User>(b,nameof(DisputeCase.ApplicantUserId)); Mapping.Fk<DisputeCase,User>(b,nameof(DisputeCase.CounterpartyUserId)); Mapping.Fk<DisputeCase,User>(b,nameof(DisputeCase.AssignedAdminUserId));
        b.HasIndex(x=>x.TransactionId); b.HasIndex(x=>x.SubscriptionVisitScheduleId); b.HasIndex(x=>x.AfterServiceCaseId).IsUnique().HasFilter("[after_service_case_id] IS NOT NULL"); b.HasIndex(x=>x.ApplicantUserId); b.HasIndex(x=>x.CounterpartyUserId); b.HasIndex(x=>x.AssignedAdminUserId); b.HasIndex(x=>x.StatusCode); b.HasIndex(x=>x.ReceivedAt); b.HasIndex(x=>x.DueAt); b.HasIndex(x=>new{x.StatusCode,x.ReceivedAt});
        b.ToTable("dispute_cases",t=>{t.HasCheckConstraint("CK_dispute_cases_source","([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NOT NULL)");t.HasCheckConstraint("CK_dispute_cases_status","[status_code] IN ('OPEN','UNDER_REVIEW','WAITING_CUSTOMER','WAITING_PROVIDER','RESOLVED','CLOSED')");});
    }
}

internal sealed class DisputeEvidenceConfiguration() : EntityConfiguration<DisputeEvidence>("dispute_evidence")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DisputeEvidence> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(DisputeEvidence.DisputeCaseId),"dispute_case_id"); Mapping.NullableLong(b,nameof(DisputeEvidence.FileId),"file_id"); Mapping.NullableLong(b,nameof(DisputeEvidence.SubmittedByUserId),"submitted_by_user_id"); Mapping.String(b,nameof(DisputeEvidence.SourceTypeCode),"source_type_code",40,unicode:false); Mapping.Guid(b,nameof(DisputeEvidence.SourcePublicId),"source_public_id",nullable:true); Mapping.String(b,nameof(DisputeEvidence.Description),"description",1000,nullable:true); Mapping.String(b,nameof(DisputeEvidence.StatusCode),"status_code",20,unicode:false,defaultValue:"ACTIVE"); Mapping.DateTime(b,nameof(DisputeEvidence.SubmittedAt),"submitted_at",utcDefault:true); Mapping.DateTime(b,nameof(DisputeEvidence.WithdrawnAt),"withdrawn_at",nullable:true); Mapping.NullableLong(b,nameof(DisputeEvidence.WithdrawnByUserId),"withdrawn_by_user_id"); Mapping.String(b,nameof(DisputeEvidence.WithdrawalReason),"withdrawal_reason",1000,nullable:true); Mapping.CreatedAudit(b);
        Mapping.Fk<DisputeEvidence,DisputeCase>(b,nameof(DisputeEvidence.DisputeCaseId)); Mapping.Fk<DisputeEvidence,StoredFile>(b,nameof(DisputeEvidence.FileId)); Mapping.Fk<DisputeEvidence,User>(b,nameof(DisputeEvidence.SubmittedByUserId)); Mapping.Fk<DisputeEvidence,User>(b,nameof(DisputeEvidence.WithdrawnByUserId));
        b.HasIndex(x=>x.DisputeCaseId); b.HasIndex(x=>x.FileId); b.HasIndex(x=>x.SubmittedByUserId); b.HasIndex(x=>x.SourceTypeCode); b.HasIndex(x=>x.SourcePublicId); b.HasIndex(x=>x.StatusCode); b.HasIndex(x=>new{x.DisputeCaseId,x.SourceTypeCode,x.SourcePublicId}).IsUnique().HasFilter("[source_public_id] IS NOT NULL");
        b.ToTable("dispute_evidence",t=>{t.HasCheckConstraint("CK_dispute_evidence_status","[status_code] IN ('ACTIVE','WITHDRAWN')");t.HasCheckConstraint("CK_dispute_evidence_source","[file_id] IS NOT NULL OR [source_public_id] IS NOT NULL");});
    }
}

internal sealed class DisputeActionConfiguration() : EntityConfiguration<DisputeAction>("dispute_actions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DisputeAction> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(DisputeAction.DisputeCaseId),"dispute_case_id"); Mapping.String(b,nameof(DisputeAction.ActionTypeCode),"action_type_code",30,unicode:false); Mapping.String(b,nameof(DisputeAction.FromStatusCode),"from_status_code",30,nullable:true,unicode:false); Mapping.String(b,nameof(DisputeAction.ToStatusCode),"to_status_code",30,nullable:true,unicode:false); Mapping.String(b,nameof(DisputeAction.ActionNote),"action_note",2000,nullable:true); Mapping.String(b,nameof(DisputeAction.Reason),"reason",1000,nullable:true); Mapping.String(b,nameof(DisputeAction.RelatedReferenceType),"related_reference_type",50,nullable:true,unicode:false); Mapping.Guid(b,nameof(DisputeAction.RelatedReferencePublicId),"related_reference_public_id",nullable:true); Mapping.DateTime(b,nameof(DisputeAction.OccurredAt),"occurred_at",utcDefault:true); Mapping.Long(b,nameof(DisputeAction.ActorUserId),"actor_user_id"); Mapping.String(b,nameof(DisputeAction.IdempotencyKey),"idempotency_key",100,unicode:false);
        Mapping.Fk<DisputeAction,DisputeCase>(b,nameof(DisputeAction.DisputeCaseId)); Mapping.Fk<DisputeAction,User>(b,nameof(DisputeAction.ActorUserId)); b.HasIndex(x=>x.DisputeCaseId); b.HasIndex(x=>x.ActionTypeCode); b.HasIndex(x=>x.ActorUserId); b.HasIndex(x=>x.OccurredAt); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.DisputeCaseId,x.OccurredAt});
        b.ToTable("dispute_actions",t=>t.HasCheckConstraint("CK_dispute_actions_type","[action_type_code] IN ('CREATED','STATUS_CHANGE','ASSIGNMENT','EVIDENCE_ADDED','EVIDENCE_REQUEST','NOTE','RESOLUTION','FEE_RESTORE_LINK')"));
    }
}

internal sealed class DisputeResolutionConfiguration() : EntityConfiguration<DisputeResolution>("dispute_resolutions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DisputeResolution> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(DisputeResolution.DisputeCaseId),"dispute_case_id"); Mapping.NullableLong(b,nameof(DisputeResolution.LiabilityTypeId),"liability_type_id"); Mapping.Int(b,nameof(DisputeResolution.VersionNo),"version_no"); Mapping.String(b,nameof(DisputeResolution.ResultSummary),"result_summary",1000); Mapping.String(b,nameof(DisputeResolution.DecisionDetails),"decision_details",null); Mapping.String(b,nameof(DisputeResolution.BasisText),"basis_text",2000); Mapping.String(b,nameof(DisputeResolution.FollowUpAction),"follow_up_action",2000,nullable:true); Mapping.DateTime(b,nameof(DisputeResolution.DecidedAt),"decided_at",utcDefault:true); Mapping.Long(b,nameof(DisputeResolution.DecidedByUserId),"decided_by_user_id"); Mapping.Bool(b,nameof(DisputeResolution.IsCurrent),"is_current",true); Mapping.CreatedAudit(b);
        Mapping.Fk<DisputeResolution,DisputeCase>(b,nameof(DisputeResolution.DisputeCaseId)); Mapping.Fk<DisputeResolution,DisputeLiabilityType>(b,nameof(DisputeResolution.LiabilityTypeId)); Mapping.Fk<DisputeResolution,User>(b,nameof(DisputeResolution.DecidedByUserId)); b.HasIndex(x=>x.DisputeCaseId); b.HasIndex(x=>x.LiabilityTypeId); b.HasIndex(x=>x.DecidedByUserId); b.HasIndex(x=>new{x.DisputeCaseId,x.VersionNo}).IsUnique(); b.HasIndex(x=>x.DisputeCaseId).IsUnique().HasFilter("[is_current] = 1");
    }
}
