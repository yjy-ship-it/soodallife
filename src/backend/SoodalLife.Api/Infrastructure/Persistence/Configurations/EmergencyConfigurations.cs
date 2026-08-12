using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProviderEmergencySettingConfiguration() : EntityConfiguration<ProviderEmergencySetting>("provider_emergency_settings")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderEmergencySetting> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ProviderEmergencySetting.ProviderProfileId),"provider_profile_id");
        Mapping.Bool(b,nameof(ProviderEmergencySetting.IsEnabled),"is_enabled",false);
        Mapping.DateTime(b,nameof(ProviderEmergencySetting.TemporarilyUnavailableUntil),"temporarily_unavailable_until",nullable:true);
        Mapping.String(b,nameof(ProviderEmergencySetting.TemporaryUnavailableReason),"temporary_unavailable_reason",500,nullable:true);
        Mapping.FullAudit(b); Mapping.Fk<ProviderEmergencySetting,ProviderProfile>(b,nameof(ProviderEmergencySetting.ProviderProfileId));
        b.HasIndex(x=>x.ProviderProfileId).IsUnique();
    }
}

internal sealed class ProviderEmergencyServiceSettingConfiguration() : EntityConfiguration<ProviderEmergencyServiceSetting>("provider_emergency_service_settings")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderEmergencyServiceSetting> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ProviderEmergencyServiceSetting.ProviderEmergencySettingId),"provider_emergency_setting_id");
        Mapping.Long(b,nameof(ProviderEmergencyServiceSetting.ProviderServiceCategoryId),"provider_service_category_id");
        Mapping.Bool(b,nameof(ProviderEmergencyServiceSetting.IsEnabled),"is_enabled",false); Mapping.FullAudit(b);
        Mapping.Fk<ProviderEmergencyServiceSetting,ProviderEmergencySetting>(b,nameof(ProviderEmergencyServiceSetting.ProviderEmergencySettingId));
        Mapping.Fk<ProviderEmergencyServiceSetting,ProviderServiceCategory>(b,nameof(ProviderEmergencyServiceSetting.ProviderServiceCategoryId));
        b.HasIndex(x=>x.ProviderServiceCategoryId).IsUnique();
        b.HasIndex(x=>new{x.ProviderEmergencySettingId,x.IsEnabled});
    }
}

internal sealed class ProviderEmergencyAvailabilitySlotConfiguration() : EntityConfiguration<ProviderEmergencyAvailabilitySlot>("provider_emergency_availability_slots")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderEmergencyAvailabilitySlot> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ProviderEmergencyAvailabilitySlot.ProviderEmergencyServiceSettingId),"provider_emergency_service_setting_id");
        b.Property(x=>x.DayOfWeek).HasColumnName("day_of_week").HasColumnType("tinyint");
        b.Property(x=>x.StartTime).HasColumnName("start_time").HasColumnType("time(0)");
        b.Property(x=>x.EndTime).HasColumnName("end_time").HasColumnType("time(0)");
        Mapping.Bool(b,nameof(ProviderEmergencyAvailabilitySlot.Is24Hours),"is_24_hours",false); Mapping.CreatedAudit(b);
        Mapping.Fk<ProviderEmergencyAvailabilitySlot,ProviderEmergencyServiceSetting>(b,nameof(ProviderEmergencyAvailabilitySlot.ProviderEmergencyServiceSettingId));
        b.HasIndex(x=>new{x.ProviderEmergencyServiceSettingId,x.DayOfWeek});
        b.ToTable("provider_emergency_availability_slots",t=>{
            t.HasCheckConstraint("CK_provider_emergency_availability_day","[day_of_week] BETWEEN 0 AND 6");
            t.HasCheckConstraint("CK_provider_emergency_availability_period","([is_24_hours] = 1 AND [start_time] IS NULL AND [end_time] IS NULL) OR ([is_24_hours] = 0 AND [start_time] IS NOT NULL AND [end_time] IS NOT NULL AND [start_time] < [end_time])");
        });
    }
}

internal sealed class ProviderEmergencyExceptionConfiguration() : EntityConfiguration<ProviderEmergencyException>("provider_emergency_exceptions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderEmergencyException> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ProviderEmergencyException.ProviderEmergencySettingId),"provider_emergency_setting_id");
        Mapping.DateTime(b,nameof(ProviderEmergencyException.StartsAt),"starts_at"); Mapping.DateTime(b,nameof(ProviderEmergencyException.EndsAt),"ends_at");
        Mapping.String(b,nameof(ProviderEmergencyException.Reason),"reason",500,nullable:true); Mapping.CreatedAudit(b);
        Mapping.Fk<ProviderEmergencyException,ProviderEmergencySetting>(b,nameof(ProviderEmergencyException.ProviderEmergencySettingId));
        b.HasIndex(x=>new{x.ProviderEmergencySettingId,x.StartsAt,x.EndsAt});
        b.ToTable("provider_emergency_exceptions",t=>t.HasCheckConstraint("CK_provider_emergency_exceptions_period","[ends_at] > [starts_at]"));
    }
}

internal sealed class EmergencyResponseConfiguration() : EntityConfiguration<EmergencyResponse>("emergency_responses")
{
    protected override void ConfigureEntity(EntityTypeBuilder<EmergencyResponse> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(EmergencyResponse.ServiceRequestId),"service_request_id"); Mapping.Long(b,nameof(EmergencyResponse.RequestDispatchId),"request_dispatch_id"); Mapping.Long(b,nameof(EmergencyResponse.ProviderProfileId),"provider_profile_id");
        Mapping.String(b,nameof(EmergencyResponse.StatusCode),"status_code",20,unicode:false,defaultValue:"PENDING");
        b.Property(x=>x.EtaMinutes).HasColumnName("eta_minutes").HasColumnType("int"); Mapping.DateTime(b,nameof(EmergencyResponse.EstimatedArrivalAt),"estimated_arrival_at",nullable:true);
        Mapping.String(b,nameof(EmergencyResponse.ConditionsText),"conditions_text",1000,nullable:true); Mapping.DateTime(b,nameof(EmergencyResponse.RespondedAt),"responded_at"); Mapping.DateTime(b,nameof(EmergencyResponse.ExpiresAt),"expires_at");
        Mapping.String(b,nameof(EmergencyResponse.IdempotencyKey),"idempotency_key",100,unicode:false); Mapping.DateTime(b,nameof(EmergencyResponse.SelectedAt),"selected_at",nullable:true); Mapping.FullAudit(b);
        Mapping.Fk<EmergencyResponse,ServiceRequest>(b,nameof(EmergencyResponse.ServiceRequestId)); Mapping.Fk<EmergencyResponse,RequestDispatch>(b,nameof(EmergencyResponse.RequestDispatchId)); Mapping.Fk<EmergencyResponse,ProviderProfile>(b,nameof(EmergencyResponse.ProviderProfileId));
        b.HasIndex(x=>x.RequestDispatchId).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.ServiceRequestId,x.StatusCode});
        b.ToTable("emergency_responses",t=>{
            t.HasCheckConstraint("CK_emergency_responses_status","[status_code] IN ('PENDING','AVAILABLE','UNAVAILABLE','EXPIRED','SELECTED','NOT_SELECTED')");
            t.HasCheckConstraint("CK_emergency_responses_eta","([status_code] <> 'AVAILABLE' AND [status_code] <> 'SELECTED') OR ([eta_minutes] IS NOT NULL AND [eta_minutes] > 0) OR [estimated_arrival_at] IS NOT NULL");
        });
    }
}

internal sealed class EmergencyProgressEventConfiguration() : EntityConfiguration<EmergencyProgressEvent>("emergency_progress_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<EmergencyProgressEvent> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(EmergencyProgressEvent.TransactionId),"transaction_id"); Mapping.Long(b,nameof(EmergencyProgressEvent.ActorUserId),"actor_user_id");
        Mapping.String(b,nameof(EmergencyProgressEvent.EventTypeCode),"event_type_code",30,unicode:false); Mapping.String(b,nameof(EmergencyProgressEvent.Note),"note",1000,nullable:true); Mapping.DateTime(b,nameof(EmergencyProgressEvent.OccurredAt),"occurred_at"); Mapping.String(b,nameof(EmergencyProgressEvent.IdempotencyKey),"idempotency_key",100,unicode:false);
        Mapping.Fk<EmergencyProgressEvent,TransactionRecord>(b,nameof(EmergencyProgressEvent.TransactionId)); Mapping.Fk<EmergencyProgressEvent,User>(b,nameof(EmergencyProgressEvent.ActorUserId));
        b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.TransactionId,x.OccurredAt});
        b.ToTable("emergency_progress_events",t=>t.HasCheckConstraint("CK_emergency_progress_events_type","[event_type_code] IN ('DISPATCH_CONFIRMED','DEPARTED','EN_ROUTE','ARRIVED')"));
    }
}
