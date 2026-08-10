using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class AdvertisingPlacementConfiguration() : EntityConfiguration<AdvertisingPlacement>("advertising_placements")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingPlacement> b)
    {
        Mapping.PublicId(b); Mapping.String(b, nameof(AdvertisingPlacement.Code), "code", 50, unicode: false);
        Mapping.String(b, nameof(AdvertisingPlacement.Name), "name", 100); Mapping.String(b, nameof(AdvertisingPlacement.Description), "description", 500, nullable: true);
        Mapping.String(b, nameof(AdvertisingPlacement.RouteHint), "route_hint", 200, nullable: true, unicode: false); Mapping.Bool(b, nameof(AdvertisingPlacement.IsActive), "is_active", true); Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => new { x.IsActive, x.Name });
        var seededAt = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        b.HasData(
            new AdvertisingPlacement { Id = 1, PublicId = Guid.Parse("11a10000-0000-0000-0000-000000000001"), Code = "CUSTOMER_HOME", Name = "고객 홈", Description = "고객 역할 홈 화면", RouteHint = "/customer", IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new AdvertisingPlacement { Id = 2, PublicId = Guid.Parse("11a10000-0000-0000-0000-000000000002"), Code = "PROVIDER_HOME", Name = "공급자 홈", Description = "공급자 역할 홈 화면", RouteHint = "/provider", IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt });
    }
}

internal sealed class AdvertisingCampaignConfiguration() : EntityConfiguration<AdvertisingCampaign>("advertising_campaigns")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingCampaign> b)
    {
        Mapping.PublicId(b); Mapping.String(b, nameof(AdvertisingCampaign.CampaignName), "campaign_name", 200); Mapping.String(b, nameof(AdvertisingCampaign.CampaignTypeCode), "campaign_type_code", 30, unicode: false);
        Mapping.String(b, nameof(AdvertisingCampaign.AudienceTypeCode), "audience_type_code", 20, unicode: false); Mapping.String(b, nameof(AdvertisingCampaign.OwnerTypeCode), "owner_type_code", 30, unicode: false);
        Mapping.String(b, nameof(AdvertisingCampaign.OwnerDisplayName), "owner_display_name", 200, nullable: true); Mapping.DateTime(b, nameof(AdvertisingCampaign.StartAt), "start_at"); Mapping.DateTime(b, nameof(AdvertisingCampaign.EndAt), "end_at", nullable: true);
        Mapping.String(b, nameof(AdvertisingCampaign.StatusCode), "status_code", 20, unicode: false); Mapping.Int(b, nameof(AdvertisingCampaign.Priority), "priority", 0);
        Mapping.String(b, nameof(AdvertisingCampaign.DestinationTypeCode), "destination_type_code", 30, unicode: false); Mapping.String(b, nameof(AdvertisingCampaign.DestinationValue), "destination_value", 2000, nullable: true, unicode: false);
        Mapping.String(b, nameof(AdvertisingCampaign.ReviewStatusCode), "review_status_code", 20, unicode: false); Mapping.NullableLong(b, nameof(AdvertisingCampaign.ApprovedByUserId), "approved_by_user_id");
        Mapping.DateTime(b, nameof(AdvertisingCampaign.ApprovedAt), "approved_at", nullable: true); Mapping.String(b, nameof(AdvertisingCampaign.RejectionReason), "rejection_reason", 1000, nullable: true); Mapping.FullAudit(b);
        Mapping.Fk<AdvertisingCampaign, User>(b, nameof(AdvertisingCampaign.ApprovedByUserId));
        b.HasIndex(x => new { x.StatusCode, x.ReviewStatusCode, x.StartAt, x.EndAt }); b.HasIndex(x => new { x.AudienceTypeCode, x.StartAt }); b.HasIndex(x => x.CampaignName);
        b.ToTable("advertising_campaigns", t => { t.HasCheckConstraint("CK_advertising_campaigns_type", "[campaign_type_code] IN ('ADVERTISEMENT','PROMOTION','BANNER','POPUP')"); t.HasCheckConstraint("CK_advertising_campaigns_audience", "[audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')"); t.HasCheckConstraint("CK_advertising_campaigns_owner", "[owner_type_code] IN ('HEAD_OFFICE','PLATFORM','EXTERNAL','PROVIDER')"); t.HasCheckConstraint("CK_advertising_campaigns_status", "[status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')"); t.HasCheckConstraint("CK_advertising_campaigns_review", "[review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')"); t.HasCheckConstraint("CK_advertising_campaigns_destination", "[destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')"); t.HasCheckConstraint("CK_advertising_campaigns_period", "[end_at] IS NULL OR [end_at] > [start_at]"); t.HasCheckConstraint("CK_advertising_campaigns_priority", "[priority] >= 0"); });
    }
}

internal sealed class AdvertisingCampaignPlacementConfiguration() : EntityConfiguration<AdvertisingCampaignPlacement>("advertising_campaign_placements")
{ protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingCampaignPlacement> b) { Mapping.Long(b, nameof(AdvertisingCampaignPlacement.CampaignId), "campaign_id"); Mapping.Long(b, nameof(AdvertisingCampaignPlacement.PlacementId), "placement_id"); Mapping.CreatedAudit(b); Mapping.Fk<AdvertisingCampaignPlacement, AdvertisingCampaign>(b, nameof(AdvertisingCampaignPlacement.CampaignId)); Mapping.Fk<AdvertisingCampaignPlacement, AdvertisingPlacement>(b, nameof(AdvertisingCampaignPlacement.PlacementId)); b.HasIndex(x => new { x.CampaignId, x.PlacementId }).IsUnique(); } }
internal sealed class AdvertisingCampaignCategoryConfiguration() : EntityConfiguration<AdvertisingCampaignCategory>("advertising_campaign_categories")
{ protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingCampaignCategory> b) { Mapping.Long(b, nameof(AdvertisingCampaignCategory.CampaignId), "campaign_id"); Mapping.Long(b, nameof(AdvertisingCampaignCategory.CategoryId), "category_id"); Mapping.CreatedAudit(b); Mapping.Fk<AdvertisingCampaignCategory, AdvertisingCampaign>(b, nameof(AdvertisingCampaignCategory.CampaignId)); Mapping.Fk<AdvertisingCampaignCategory, ServiceCategory>(b, nameof(AdvertisingCampaignCategory.CategoryId)); b.HasIndex(x => new { x.CampaignId, x.CategoryId }).IsUnique(); b.HasIndex(x => x.CategoryId); } }
internal sealed class AdvertisingCampaignAreaConfiguration() : EntityConfiguration<AdvertisingCampaignArea>("advertising_campaign_areas")
{ protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingCampaignArea> b) { Mapping.Long(b, nameof(AdvertisingCampaignArea.CampaignId), "campaign_id"); Mapping.Long(b, nameof(AdvertisingCampaignArea.AdministrativeAreaId), "administrative_area_id"); Mapping.CreatedAudit(b); Mapping.Fk<AdvertisingCampaignArea, AdvertisingCampaign>(b, nameof(AdvertisingCampaignArea.CampaignId)); Mapping.Fk<AdvertisingCampaignArea, AdministrativeArea>(b, nameof(AdvertisingCampaignArea.AdministrativeAreaId)); b.HasIndex(x => new { x.CampaignId, x.AdministrativeAreaId }).IsUnique(); b.HasIndex(x => x.AdministrativeAreaId); } }

internal sealed class AdvertisingCreativeConfiguration() : EntityConfiguration<AdvertisingCreative>("advertising_creatives")
{
    protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingCreative> b)
    {
        Mapping.PublicId(b); Mapping.Long(b, nameof(AdvertisingCreative.CampaignId), "campaign_id"); Mapping.String(b, nameof(AdvertisingCreative.Title), "title", 200); Mapping.String(b, nameof(AdvertisingCreative.Subtitle), "subtitle", 300, nullable: true);
        Mapping.String(b, nameof(AdvertisingCreative.BodyText), "body_text", 4000, nullable: true); Mapping.NullableLong(b, nameof(AdvertisingCreative.FileId), "file_id"); Mapping.String(b, nameof(AdvertisingCreative.AltText), "alt_text", 300, nullable: true);
        Mapping.String(b, nameof(AdvertisingCreative.ButtonText), "button_text", 50, nullable: true); Mapping.String(b, nameof(AdvertisingCreative.DestinationTypeCode), "destination_type_code", 30, nullable: true, unicode: false); Mapping.String(b, nameof(AdvertisingCreative.DestinationValue), "destination_value", 2000, nullable: true, unicode: false);
        Mapping.Int(b, nameof(AdvertisingCreative.DisplayOrder), "display_order", 0); Mapping.String(b, nameof(AdvertisingCreative.StatusCode), "status_code", 20, unicode: false); Mapping.FullAudit(b);
        Mapping.Fk<AdvertisingCreative, AdvertisingCampaign>(b, nameof(AdvertisingCreative.CampaignId)); Mapping.Fk<AdvertisingCreative, StoredFile>(b, nameof(AdvertisingCreative.FileId)); b.HasIndex(x => new { x.CampaignId, x.DisplayOrder }); b.HasIndex(x => x.FileId);
        b.ToTable("advertising_creatives", t => { t.HasCheckConstraint("CK_advertising_creatives_status", "[status_code] IN ('ACTIVE','INACTIVE')"); t.HasCheckConstraint("CK_advertising_creatives_destination", "[destination_type_code] IS NULL OR [destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')"); t.HasCheckConstraint("CK_advertising_creatives_order", "[display_order] >= 0"); });
    }
}

internal sealed class AdvertisingEventConfiguration() : EntityConfiguration<AdvertisingEvent>("advertising_events")
{ protected override void ConfigureEntity(EntityTypeBuilder<AdvertisingEvent> b) { Mapping.Long(b, nameof(AdvertisingEvent.CreativeId), "creative_id"); Mapping.Long(b, nameof(AdvertisingEvent.PlacementId), "placement_id"); Mapping.String(b, nameof(AdvertisingEvent.EventTypeCode), "event_type_code", 20, unicode: false); Mapping.DateTime(b, nameof(AdvertisingEvent.OccurredAt), "occurred_at", utcDefault: true); Mapping.Fk<AdvertisingEvent, AdvertisingCreative>(b, nameof(AdvertisingEvent.CreativeId)); Mapping.Fk<AdvertisingEvent, AdvertisingPlacement>(b, nameof(AdvertisingEvent.PlacementId)); b.HasIndex(x => new { x.CreativeId, x.PlacementId, x.EventTypeCode, x.OccurredAt }); b.ToTable("advertising_events", t => t.HasCheckConstraint("CK_advertising_events_type", "[event_type_code] IN ('IMPRESSION','CLICK')")); } }

internal sealed class ManagedContentConfiguration() : EntityConfiguration<ManagedContent>("managed_contents")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ManagedContent> b)
    {
        Mapping.PublicId(b); Mapping.String(b, nameof(ManagedContent.ContentTypeCode), "content_type_code", 30, unicode: false); Mapping.String(b, nameof(ManagedContent.AudienceTypeCode), "audience_type_code", 20, unicode: false);
        Mapping.String(b, nameof(ManagedContent.StatusCode), "status_code", 20, unicode: false); Mapping.String(b, nameof(ManagedContent.ReviewStatusCode), "review_status_code", 20, unicode: false); Mapping.Int(b, nameof(ManagedContent.CurrentVersionNo), "current_version_no", 1); Mapping.Int(b, nameof(ManagedContent.DisplayOrder), "display_order", 0);
        Mapping.DateTime(b, nameof(ManagedContent.StartAt), "start_at"); Mapping.DateTime(b, nameof(ManagedContent.EndAt), "end_at", nullable: true); Mapping.NullableLong(b, nameof(ManagedContent.ApprovedByUserId), "approved_by_user_id"); Mapping.DateTime(b, nameof(ManagedContent.ApprovedAt), "approved_at", nullable: true); Mapping.String(b, nameof(ManagedContent.RejectionReason), "rejection_reason", 1000, nullable: true); Mapping.FullAudit(b); Mapping.Fk<ManagedContent, User>(b, nameof(ManagedContent.ApprovedByUserId));
        b.HasIndex(x => new { x.ContentTypeCode, x.StatusCode, x.ReviewStatusCode, x.StartAt, x.EndAt }); b.HasIndex(x => new { x.AudienceTypeCode, x.StartAt });
        b.ToTable("managed_contents", t => { t.HasCheckConstraint("CK_managed_contents_type", "[content_type_code] IN ('NOTICE','FAQ','SAFETY_GUIDE','CATEGORY_GUIDE','PRICE_REFERENCE')"); t.HasCheckConstraint("CK_managed_contents_audience", "[audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')"); t.HasCheckConstraint("CK_managed_contents_status", "[status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')"); t.HasCheckConstraint("CK_managed_contents_review", "[review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')"); t.HasCheckConstraint("CK_managed_contents_period", "[end_at] IS NULL OR [end_at] > [start_at]"); t.HasCheckConstraint("CK_managed_contents_version", "[current_version_no] > 0"); t.HasCheckConstraint("CK_managed_contents_order", "[display_order] >= 0"); });
    }
}

internal sealed class ManagedContentVersionConfiguration() : EntityConfiguration<ManagedContentVersion>("managed_content_versions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ManagedContentVersion> b)
    {
        Mapping.PublicId(b); Mapping.Long(b, nameof(ManagedContentVersion.ContentId), "content_id"); Mapping.Int(b, nameof(ManagedContentVersion.VersionNo), "version_no"); Mapping.String(b, nameof(ManagedContentVersion.Title), "title", 300); Mapping.String(b, nameof(ManagedContentVersion.BodyText), "body_text", null, nullable: true); Mapping.String(b, nameof(ManagedContentVersion.QuestionText), "question_text", 1000, nullable: true); Mapping.String(b, nameof(ManagedContentVersion.AnswerText), "answer_text", null, nullable: true); Mapping.NullableLong(b, nameof(ManagedContentVersion.FileId), "file_id"); Mapping.String(b, nameof(ManagedContentVersion.LinkText), "link_text", 100, nullable: true); Mapping.String(b, nameof(ManagedContentVersion.DestinationTypeCode), "destination_type_code", 30, unicode: false); Mapping.String(b, nameof(ManagedContentVersion.DestinationValue), "destination_value", 2000, nullable: true, unicode: false); Mapping.String(b, nameof(ManagedContentVersion.ChangeReason), "change_reason", 1000, nullable: true); Mapping.CreatedAudit(b); Mapping.Fk<ManagedContentVersion, ManagedContent>(b, nameof(ManagedContentVersion.ContentId)); Mapping.Fk<ManagedContentVersion, StoredFile>(b, nameof(ManagedContentVersion.FileId)); b.HasIndex(x => new { x.ContentId, x.VersionNo }).IsUnique(); b.HasIndex(x => x.FileId); b.ToTable("managed_content_versions", t => { t.HasCheckConstraint("CK_managed_content_versions_version", "[version_no] > 0"); t.HasCheckConstraint("CK_managed_content_versions_destination", "[destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')"); });
    }
}

internal sealed class ManagedContentCategoryConfiguration() : EntityConfiguration<ManagedContentCategory>("managed_content_categories")
{ protected override void ConfigureEntity(EntityTypeBuilder<ManagedContentCategory> b) { Mapping.Long(b, nameof(ManagedContentCategory.ContentId), "content_id"); Mapping.Long(b, nameof(ManagedContentCategory.CategoryId), "category_id"); Mapping.CreatedAudit(b); Mapping.Fk<ManagedContentCategory, ManagedContent>(b, nameof(ManagedContentCategory.ContentId)); Mapping.Fk<ManagedContentCategory, ServiceCategory>(b, nameof(ManagedContentCategory.CategoryId)); b.HasIndex(x => new { x.ContentId, x.CategoryId }).IsUnique(); b.HasIndex(x => x.CategoryId); } }
internal sealed class ManagedContentAreaConfiguration() : EntityConfiguration<ManagedContentArea>("managed_content_areas")
{ protected override void ConfigureEntity(EntityTypeBuilder<ManagedContentArea> b) { Mapping.Long(b, nameof(ManagedContentArea.ContentId), "content_id"); Mapping.Long(b, nameof(ManagedContentArea.AdministrativeAreaId), "administrative_area_id"); Mapping.CreatedAudit(b); Mapping.Fk<ManagedContentArea, ManagedContent>(b, nameof(ManagedContentArea.ContentId)); Mapping.Fk<ManagedContentArea, AdministrativeArea>(b, nameof(ManagedContentArea.AdministrativeAreaId)); b.HasIndex(x => new { x.ContentId, x.AdministrativeAreaId }).IsUnique(); b.HasIndex(x => x.AdministrativeAreaId); } }
