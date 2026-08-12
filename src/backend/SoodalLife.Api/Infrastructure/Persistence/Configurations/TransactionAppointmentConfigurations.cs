using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class TransactionAppointmentConfiguration() : EntityConfiguration<TransactionAppointment>("transaction_appointments")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionAppointment> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(TransactionAppointment.TransactionId), "transaction_id");
        Mapping.DateTime(b, nameof(TransactionAppointment.ScheduledStartAt), "scheduled_start_at");
        Mapping.DateTime(b, nameof(TransactionAppointment.ScheduledEndAt), "scheduled_end_at", nullable: true);
        b.Property(x => x.EstimatedDurationMinutes).HasColumnName("estimated_duration_minutes");
        Mapping.String(b, nameof(TransactionAppointment.CustomerMemo), "customer_memo", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionAppointment.ProviderMemo), "provider_memo", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionAppointment.StatusCode), "status_code", 20, unicode: false, defaultValue: "PROPOSED");
        Mapping.String(b, nameof(TransactionAppointment.ProposalIdempotencyKey), "proposal_idempotency_key", 100, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(TransactionAppointment.ConfirmedAt), "confirmed_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointment.CancelledAt), "cancelled_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<TransactionAppointment, TransactionRecord>(b, nameof(TransactionAppointment.TransactionId));
        b.HasIndex(x => x.TransactionId).IsUnique();
        b.HasIndex(x => x.ScheduledStartAt);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.ProposalIdempotencyKey).IsUnique().HasFilter("[proposal_idempotency_key] IS NOT NULL");
        b.ToTable("transaction_appointments", t =>
        {
            t.HasCheckConstraint("CK_transaction_appointments_status", "[status_code] IN ('PROPOSED','CONFIRMED','REJECTED','CANCELLED','COMPLETED')");
            t.HasCheckConstraint("CK_transaction_appointments_period", "[scheduled_end_at] IS NULL OR [scheduled_end_at] > [scheduled_start_at]");
            t.HasCheckConstraint("CK_transaction_appointments_duration", "[estimated_duration_minutes] IS NULL OR [estimated_duration_minutes] > 0");
        });
    }
}

internal sealed class TransactionAppointmentChangeRequestConfiguration() : EntityConfiguration<TransactionAppointmentChangeRequest>("transaction_appointment_change_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionAppointmentChangeRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(TransactionAppointmentChangeRequest.TransactionAppointmentId), "transaction_appointment_id");
        Mapping.Long(b, nameof(TransactionAppointmentChangeRequest.RequestedByUserId), "requested_by_user_id");
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.ChangeTypeCode), "change_type_code", 20, unicode: false, defaultValue: "RESCHEDULE");
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedStartAt), "requested_start_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedEndAt), "requested_end_at", nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.Reason), "reason", 1000);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "REQUESTED");
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedAt), "requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.ProcessedAt), "processed_at", nullable: true);
        Mapping.NullableLong(b, nameof(TransactionAppointmentChangeRequest.ProcessedByUserId), "processed_by_user_id");
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.ProcessingNote), "processing_note", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.CreatedAt), "created_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<TransactionAppointmentChangeRequest, TransactionAppointment>(b, nameof(TransactionAppointmentChangeRequest.TransactionAppointmentId));
        Mapping.Fk<TransactionAppointmentChangeRequest, User>(b, nameof(TransactionAppointmentChangeRequest.RequestedByUserId));
        Mapping.Fk<TransactionAppointmentChangeRequest, User>(b, nameof(TransactionAppointmentChangeRequest.ProcessedByUserId));
        b.HasIndex(x => x.TransactionAppointmentId);
        b.HasIndex(x => x.RequestedByUserId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.RequestedAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.ToTable("transaction_appointment_change_requests", t =>
        {
            t.HasCheckConstraint("CK_transaction_appointment_changes_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
            t.HasCheckConstraint("CK_transaction_appointment_changes_type", "[change_type_code] IN ('RESCHEDULE','CANCEL')");
            t.HasCheckConstraint("CK_transaction_appointment_changes_period", "([change_type_code] = 'CANCEL' AND [requested_start_at] IS NULL AND [requested_end_at] IS NULL) OR ([change_type_code] = 'RESCHEDULE' AND [requested_start_at] IS NOT NULL AND ([requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]))");
        });
    }
}

internal sealed class TransactionAppointmentEventConfiguration() : EntityConfiguration<TransactionAppointmentEvent>("transaction_appointment_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionAppointmentEvent> b)
    {
        Mapping.PublicId(b); Mapping.Long(b, nameof(TransactionAppointmentEvent.TransactionAppointmentId), "transaction_appointment_id");
        Mapping.Long(b, nameof(TransactionAppointmentEvent.ActorUserId), "actor_user_id");
        Mapping.String(b, nameof(TransactionAppointmentEvent.EventTypeCode), "event_type_code", 40, unicode: false);
        Mapping.String(b, nameof(TransactionAppointmentEvent.BeforeStatusCode), "before_status_code", 20, nullable: true, unicode: false);
        Mapping.String(b, nameof(TransactionAppointmentEvent.AfterStatusCode), "after_status_code", 20, unicode: false);
        Mapping.DateTime(b, nameof(TransactionAppointmentEvent.BeforeStartAt), "before_start_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentEvent.BeforeEndAt), "before_end_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentEvent.AfterStartAt), "after_start_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentEvent.AfterEndAt), "after_end_at", nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentEvent.Reason), "reason", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentEvent.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.DateTime(b, nameof(TransactionAppointmentEvent.OccurredAt), "occurred_at", utcDefault: true);
        Mapping.Fk<TransactionAppointmentEvent, TransactionAppointment>(b, nameof(TransactionAppointmentEvent.TransactionAppointmentId));
        Mapping.Fk<TransactionAppointmentEvent, User>(b, nameof(TransactionAppointmentEvent.ActorUserId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique(); b.HasIndex(x => new { x.TransactionAppointmentId, x.OccurredAt });
    }
}

internal sealed class TransactionCancellationRequestConfiguration() : EntityConfiguration<TransactionCancellationRequest>("transaction_cancellation_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<TransactionCancellationRequest> b)
    {
        Mapping.PublicId(b); Mapping.Long(b, nameof(TransactionCancellationRequest.TransactionId), "transaction_id");
        Mapping.Long(b, nameof(TransactionCancellationRequest.RequestedByUserId), "requested_by_user_id");
        Mapping.String(b, nameof(TransactionCancellationRequest.Reason), "reason", 1000);
        Mapping.String(b, nameof(TransactionCancellationRequest.StatusCode), "status_code", 30, unicode: false, defaultValue: "REQUESTED");
        Mapping.DateTime(b, nameof(TransactionCancellationRequest.RequestedAt), "requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(TransactionCancellationRequest.ProcessedAt), "processed_at", nullable: true);
        Mapping.NullableLong(b, nameof(TransactionCancellationRequest.ProcessedByUserId), "processed_by_user_id");
        Mapping.String(b, nameof(TransactionCancellationRequest.ProcessingNote), "processing_note", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionCancellationRequest.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.String(b, nameof(TransactionCancellationRequest.DecisionIdempotencyKey), "decision_idempotency_key", 100, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(TransactionCancellationRequest.CreatedAt), "created_at", utcDefault: true); Mapping.RowVersion(b);
        Mapping.Fk<TransactionCancellationRequest, TransactionRecord>(b, nameof(TransactionCancellationRequest.TransactionId));
        Mapping.Fk<TransactionCancellationRequest, User>(b, nameof(TransactionCancellationRequest.RequestedByUserId));
        Mapping.Fk<TransactionCancellationRequest, User>(b, nameof(TransactionCancellationRequest.ProcessedByUserId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique(); b.HasIndex(x => x.DecisionIdempotencyKey).IsUnique().HasFilter("[decision_idempotency_key] IS NOT NULL"); b.HasIndex(x => new { x.TransactionId, x.RequestedAt }); b.HasIndex(x => x.StatusCode);
        b.ToTable("transaction_cancellation_requests", t => t.HasCheckConstraint("CK_transaction_cancellation_requests_status", "[status_code] IN ('REQUESTED','ADMIN_REVIEW_REQUIRED','APPROVED','REJECTED','CANCELLED')"));
    }
}
