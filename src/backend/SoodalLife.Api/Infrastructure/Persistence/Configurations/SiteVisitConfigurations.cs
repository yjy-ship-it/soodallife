using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class SiteVisitProposalConfiguration() : EntityConfiguration<SiteVisitProposal>("site_visit_proposals")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SiteVisitProposal> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b,nameof(SiteVisitProposal.ServiceRequestId),"service_request_id");
        Mapping.Long(b,nameof(SiteVisitProposal.RequestDispatchId),"request_dispatch_id");
        Mapping.Long(b,nameof(SiteVisitProposal.ProviderProfileId),"provider_profile_id");
        Mapping.String(b,nameof(SiteVisitProposal.StatusCode),"status_code",30,unicode:false,defaultValue:"PROPOSED");
        Mapping.DateTime(b,nameof(SiteVisitProposal.ScheduledAt),"scheduled_at");
        b.Property(x=>x.EstimatedDurationMinutes).HasColumnName("estimated_duration_minutes").HasColumnType("int").HasDefaultValue(30);
        Mapping.Decimal(b,nameof(SiteVisitProposal.VisitFeeAmount),"visit_fee_amount");
        Mapping.String(b,nameof(SiteVisitProposal.PaymentModeCode),"payment_mode_code",30,unicode:false,defaultValue:"NO_FEE");
        Mapping.String(b,nameof(SiteVisitProposal.PaymentStatusCode),"payment_status_code",30,unicode:false,defaultValue:"NOT_REQUIRED");
        Mapping.Bool(b,nameof(SiteVisitProposal.DeductFromWorkAmount),"deduct_from_work_amount",false);
        Mapping.String(b,nameof(SiteVisitProposal.TermsText),"terms_text",2000,nullable:true);
        Mapping.String(b,nameof(SiteVisitProposal.PaymentInstructionProtected),"payment_instruction_protected",2000,nullable:true);
        b.Property(x=>x.NoShowWaitMinutes).HasColumnName("no_show_wait_minutes").HasColumnType("int").HasDefaultValue(10);
        Mapping.DateTime(b,nameof(SiteVisitProposal.ExpiresAt),"expires_at");
        Mapping.DateTime(b,nameof(SiteVisitProposal.AcceptedAt),"accepted_at",nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitProposal.PaymentReportedAt),"payment_reported_at",nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitProposal.PaymentConfirmedAt),"payment_confirmed_at",nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitProposal.DepartedAt),"departed_at",nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitProposal.ArrivedAt),"arrived_at",nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitProposal.CompletedAt),"completed_at",nullable:true);
        Mapping.String(b,nameof(SiteVisitProposal.NoShowStatusCode),"no_show_status_code",30,nullable:true,unicode:false);
        Mapping.DateTime(b,nameof(SiteVisitProposal.NoShowReportedAt),"no_show_reported_at",nullable:true);
        Mapping.String(b,nameof(SiteVisitProposal.IdempotencyKey),"idempotency_key",100,unicode:false);
        Mapping.FullAudit(b);
        Mapping.Fk<SiteVisitProposal,ServiceRequest>(b,nameof(SiteVisitProposal.ServiceRequestId));
        Mapping.Fk<SiteVisitProposal,RequestDispatch>(b,nameof(SiteVisitProposal.RequestDispatchId));
        Mapping.Fk<SiteVisitProposal,ProviderProfile>(b,nameof(SiteVisitProposal.ProviderProfileId));
        b.HasIndex(x=>x.RequestDispatchId).IsUnique();
        b.HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.HasIndex(x=>new{x.ServiceRequestId,x.StatusCode});
        b.ToTable("site_visit_proposals",t=>{
            t.HasCheckConstraint("CK_site_visit_status","[status_code] IN ('PROPOSED','ACCEPTED','DEPARTED','ARRIVED','COMPLETED','REJECTED','CANCELLED','EXPIRED','NO_SHOW','DISPUTED')");
            t.HasCheckConstraint("CK_site_visit_payment_mode","[payment_mode_code] IN ('NO_FEE','ON_SITE','TRANSFER_REPORTED','TRANSFER_CONFIRMED')");
            t.HasCheckConstraint("CK_site_visit_payment_status","[payment_status_code] IN ('NOT_REQUIRED','ON_SITE_PENDING','AWAITING_TRANSFER','REPORTED','CONFIRMED','REJECTED')");
            t.HasCheckConstraint("CK_site_visit_amount","[visit_fee_amount] >= 0");
            t.HasCheckConstraint("CK_site_visit_duration","[estimated_duration_minutes] BETWEEN 10 AND 480");
            t.HasCheckConstraint("CK_site_visit_wait","[no_show_wait_minutes] BETWEEN 5 AND 60");
        });
    }
}

internal sealed class SiteVisitEventConfiguration() : EntityConfiguration<SiteVisitEvent>("site_visit_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<SiteVisitEvent> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b,nameof(SiteVisitEvent.SiteVisitProposalId),"site_visit_proposal_id");
        Mapping.Long(b,nameof(SiteVisitEvent.ActorUserId),"actor_user_id");
        Mapping.String(b,nameof(SiteVisitEvent.EventTypeCode),"event_type_code",40,unicode:false);
        Mapping.String(b,nameof(SiteVisitEvent.Note),"note",1000,nullable:true);
        Mapping.DateTime(b,nameof(SiteVisitEvent.OccurredAt),"occurred_at");
        Mapping.String(b,nameof(SiteVisitEvent.IdempotencyKey),"idempotency_key",100,unicode:false);
        Mapping.Fk<SiteVisitEvent,SiteVisitProposal>(b,nameof(SiteVisitEvent.SiteVisitProposalId));
        Mapping.Fk<SiteVisitEvent,User>(b,nameof(SiteVisitEvent.ActorUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.HasIndex(x=>new{x.SiteVisitProposalId,x.OccurredAt});
    }
}
