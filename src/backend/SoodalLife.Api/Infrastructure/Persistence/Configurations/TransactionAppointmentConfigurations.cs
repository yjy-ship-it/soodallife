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
        Mapping.DateTime(b, nameof(TransactionAppointment.ConfirmedAt), "confirmed_at", nullable: true);
        Mapping.DateTime(b, nameof(TransactionAppointment.CancelledAt), "cancelled_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<TransactionAppointment, TransactionRecord>(b, nameof(TransactionAppointment.TransactionId));
        b.HasIndex(x => x.TransactionId).IsUnique();
        b.HasIndex(x => x.ScheduledStartAt);
        b.HasIndex(x => x.StatusCode);
        b.ToTable("transaction_appointments", t =>
        {
            t.HasCheckConstraint("CK_transaction_appointments_status", "[status_code] IN ('PROPOSED','CONFIRMED','CANCELLED')");
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
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedStartAt), "requested_start_at");
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedEndAt), "requested_end_at", nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.Reason), "reason", 1000);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "REQUESTED");
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.RequestedAt), "requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.ProcessedAt), "processed_at", nullable: true);
        Mapping.NullableLong(b, nameof(TransactionAppointmentChangeRequest.ProcessedByUserId), "processed_by_user_id");
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.ProcessingNote), "processing_note", 1000, nullable: true);
        Mapping.String(b, nameof(TransactionAppointmentChangeRequest.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.DateTime(b, nameof(TransactionAppointmentChangeRequest.CreatedAt), "created_at", utcDefault: true);
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
            t.HasCheckConstraint("CK_transaction_appointment_changes_period", "[requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]");
        });
    }
}
