using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class PublicActivityEventConfiguration() : EntityConfiguration<PublicActivityEvent>("public_activity_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<PublicActivityEvent> b)
    {
        Mapping.Long(b,nameof(PublicActivityEvent.ServiceRequestId),"service_request_id");
        Mapping.String(b,nameof(PublicActivityEvent.EventTypeCode),"event_type_code",30,unicode:false);
        Mapping.String(b,nameof(PublicActivityEvent.SourceTypeCode),"source_type_code",30,unicode:false);
        Mapping.Long(b,nameof(PublicActivityEvent.SourceEntityId),"source_entity_id");
        Mapping.DateTime(b,nameof(PublicActivityEvent.OccurredAt),"occurred_at");
        Mapping.DateTime(b,nameof(PublicActivityEvent.CreatedAt),"created_at",utcDefault:true);
        Mapping.Fk<PublicActivityEvent,ServiceRequest>(b,nameof(PublicActivityEvent.ServiceRequestId));
        b.HasIndex(x=>new{x.OccurredAt,x.EventTypeCode,x.ServiceRequestId}).IsDescending(true,false,false);
        b.HasIndex(x=>new{x.SourceTypeCode,x.SourceEntityId,x.EventTypeCode}).IsUnique();
        b.ToTable("public_activity_events",t=>t.HasCheckConstraint("CK_public_activity_events_type","[event_type_code] IN ('REQUEST_OPENED','QUOTE_RECEIVED','PROVIDER_SELECTED','WORK_STARTED','WORK_COMPLETED','REVIEW_PUBLISHED')"));
    }
}
