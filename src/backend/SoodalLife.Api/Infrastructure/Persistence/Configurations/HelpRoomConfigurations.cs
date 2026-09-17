using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class HelpPostConfiguration() : EntityConfiguration<HelpPost>("help_posts")
{
    protected override void ConfigureEntity(EntityTypeBuilder<HelpPost> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(HelpPost.CustomerUserId),"customer_user_id"); Mapping.Long(b,nameof(HelpPost.CategoryId),"category_id"); Mapping.NullableLong(b,nameof(HelpPost.AdministrativeAreaId),"administrative_area_id");
        Mapping.String(b,nameof(HelpPost.RegionDisclosureCode),"region_disclosure_code",20,unicode:false,defaultValue:"HIDDEN"); Mapping.String(b,nameof(HelpPost.PurposeCode),"purpose_code",40,unicode:false); Mapping.String(b,nameof(HelpPost.Title),"title",200); Mapping.String(b,nameof(HelpPost.Body),"body",null); Mapping.String(b,nameof(HelpPost.StatusCode),"status_code",20,unicode:false); Mapping.String(b,nameof(HelpPost.IntentCode),"intent_code",30,unicode:false); Mapping.String(b,nameof(HelpPost.IntentReason),"intent_reason",500,nullable:true); Mapping.NullableLong(b,nameof(HelpPost.ConvertedServiceRequestId),"converted_service_request_id"); Mapping.DateTime(b,nameof(HelpPost.ResolvedAt),"resolved_at",nullable:true); Mapping.FullAudit(b);
        Mapping.Fk<HelpPost,User>(b,nameof(HelpPost.CustomerUserId)); Mapping.Fk<HelpPost,ServiceCategory>(b,nameof(HelpPost.CategoryId)); Mapping.Fk<HelpPost,AdministrativeArea>(b,nameof(HelpPost.AdministrativeAreaId)); Mapping.Fk<HelpPost,ServiceRequest>(b,nameof(HelpPost.ConvertedServiceRequestId));
        b.HasIndex(x=>new{x.StatusCode,x.CreatedAt}).IsDescending(false,true); b.HasIndex(x=>x.CategoryId); b.HasIndex(x=>x.CustomerUserId);
        b.ToTable("help_posts",t=>{t.HasCheckConstraint("CK_help_posts_region","[region_disclosure_code] IN ('HIDDEN','SIGUNGU')");t.HasCheckConstraint("CK_help_posts_status","[status_code] IN ('PUBLISHED','RESOLVED','CONVERTED','HIDDEN')");t.HasCheckConstraint("CK_help_posts_intent","[intent_code] IN ('ADVICE','QUOTE_RECOMMENDED','DANGEROUS')");});
    }
}

internal sealed class HelpRoomEntryConfiguration() : EntityConfiguration<HelpRoomEntry>("help_room_entries")
{
    protected override void ConfigureEntity(EntityTypeBuilder<HelpRoomEntry> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(HelpRoomEntry.HelpPostId),"help_post_id"); Mapping.Long(b,nameof(HelpRoomEntry.AuthorUserId),"author_user_id"); Mapping.NullableLong(b,nameof(HelpRoomEntry.ProviderProfileId),"provider_profile_id"); Mapping.String(b,nameof(HelpRoomEntry.AuthorRoleCode),"author_role_code",20,unicode:false); Mapping.String(b,nameof(HelpRoomEntry.EntryTypeCode),"entry_type_code",30,unicode:false); Mapping.String(b,nameof(HelpRoomEntry.Body),"body",null); Mapping.String(b,nameof(HelpRoomEntry.CauseText),"cause_text",2000,nullable:true); Mapping.String(b,nameof(HelpRoomEntry.CheckText),"check_text",2000,nullable:true); Mapping.String(b,nameof(HelpRoomEntry.DiyStepsText),"diy_steps_text",3000,nullable:true); Mapping.String(b,nameof(HelpRoomEntry.RiskText),"risk_text",2000,nullable:true); Mapping.String(b,nameof(HelpRoomEntry.NextStepText),"next_step_text",2000,nullable:true); Mapping.Bool(b,nameof(HelpRoomEntry.RequiresProfessional),"requires_professional",false); Mapping.String(b,nameof(HelpRoomEntry.SafetyCode),"safety_code",30,unicode:false); Mapping.String(b,nameof(HelpRoomEntry.StatusCode),"status_code",20,unicode:false); Mapping.DateTime(b,nameof(HelpRoomEntry.CreatedAt),"created_at",utcDefault:true); Mapping.NullableLong(b,nameof(HelpRoomEntry.CreatedByUserId),"created_by_user_id");
        Mapping.Fk<HelpRoomEntry,HelpPost>(b,nameof(HelpRoomEntry.HelpPostId)); Mapping.Fk<HelpRoomEntry,User>(b,nameof(HelpRoomEntry.AuthorUserId)); Mapping.Fk<HelpRoomEntry,ProviderProfile>(b,nameof(HelpRoomEntry.ProviderProfileId)); b.HasIndex(x=>new{x.HelpPostId,x.CreatedAt}); b.HasIndex(x=>new{x.HelpPostId,x.ProviderProfileId,x.CreatedAt});
    }
}

internal sealed class HelpPostResolutionConfiguration() : EntityConfiguration<HelpPostResolution>("help_post_resolutions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<HelpPostResolution> b){Mapping.PublicId(b);Mapping.Long(b,nameof(HelpPostResolution.HelpPostId),"help_post_id");Mapping.Long(b,nameof(HelpPostResolution.ResolvedByCustomerUserId),"resolved_by_customer_user_id");Mapping.String(b,nameof(HelpPostResolution.ResolutionCode),"resolution_code",30,unicode:false);Mapping.String(b,nameof(HelpPostResolution.Summary),"summary",3000);Mapping.NullableLong(b,nameof(HelpPostResolution.HelpfulEntryId),"helpful_entry_id");Mapping.DateTime(b,nameof(HelpPostResolution.CreatedAt),"created_at",utcDefault:true);Mapping.NullableLong(b,nameof(HelpPostResolution.CreatedByUserId),"created_by_user_id");Mapping.Fk<HelpPostResolution,HelpPost>(b,nameof(HelpPostResolution.HelpPostId));Mapping.Fk<HelpPostResolution,User>(b,nameof(HelpPostResolution.ResolvedByCustomerUserId));Mapping.Fk<HelpPostResolution,HelpRoomEntry>(b,nameof(HelpPostResolution.HelpfulEntryId));b.HasIndex(x=>x.HelpPostId).IsUnique();}
}

internal sealed class HelpPostFileConfiguration() : EntityConfiguration<HelpPostFile>("help_post_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<HelpPostFile> b){Mapping.PublicId(b);Mapping.Long(b,nameof(HelpPostFile.HelpPostId),"help_post_id");Mapping.Long(b,nameof(HelpPostFile.FileId),"file_id");Mapping.Int(b,nameof(HelpPostFile.DisplayOrder),"display_order",0);Mapping.DateTime(b,nameof(HelpPostFile.CreatedAt),"created_at",utcDefault:true);Mapping.NullableLong(b,nameof(HelpPostFile.CreatedByUserId),"created_by_user_id");Mapping.Fk<HelpPostFile,HelpPost>(b,nameof(HelpPostFile.HelpPostId));Mapping.Fk<HelpPostFile,StoredFile>(b,nameof(HelpPostFile.FileId));b.HasIndex(x=>new{x.HelpPostId,x.DisplayOrder});b.HasIndex(x=>new{x.HelpPostId,x.FileId}).IsUnique();}
}

internal sealed class UserSuggestionConfiguration() : EntityConfiguration<UserSuggestion>("user_suggestions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserSuggestion> b){Mapping.PublicId(b);Mapping.Long(b,nameof(UserSuggestion.UserId),"user_id");Mapping.String(b,nameof(UserSuggestion.TypeCode),"type_code",30,unicode:false);Mapping.String(b,nameof(UserSuggestion.VisibilityCode),"visibility_code",20,unicode:false,defaultValue:"PRIVATE");Mapping.String(b,nameof(UserSuggestion.StatusCode),"status_code",30,unicode:false);Mapping.String(b,nameof(UserSuggestion.Title),"title",200);Mapping.String(b,nameof(UserSuggestion.Body),"body",null);Mapping.String(b,nameof(UserSuggestion.PageUrl),"page_url",1000,nullable:true);Mapping.String(b,nameof(UserSuggestion.DeviceInfo),"device_info",1000,nullable:true);Mapping.String(b,nameof(UserSuggestion.AppVersion),"app_version",100,nullable:true);Mapping.NullableLong(b,nameof(UserSuggestion.AssignedAdminUserId),"assigned_admin_user_id");Mapping.String(b,nameof(UserSuggestion.AdminReply),"admin_reply",null,nullable:true);Mapping.String(b,nameof(UserSuggestion.ReleaseVersion),"release_version",100,nullable:true);b.Property(x=>x.ContentHash).HasColumnName("content_hash").HasColumnType("binary(32)").HasMaxLength(32).IsFixedLength().IsRequired();Mapping.String(b,nameof(UserSuggestion.IdempotencyKey),"idempotency_key",100,unicode:false);Mapping.FullAudit(b);Mapping.Fk<UserSuggestion,User>(b,nameof(UserSuggestion.UserId));Mapping.Fk<UserSuggestion,User>(b,nameof(UserSuggestion.AssignedAdminUserId));b.HasIndex(x=>x.IdempotencyKey).IsUnique();b.HasIndex(x=>new{x.UserId,x.CreatedAt}).IsDescending(false,true);b.HasIndex(x=>new{x.UserId,x.ContentHash,x.CreatedAt});}
}

internal sealed class UserSuggestionEventConfiguration() : EntityConfiguration<UserSuggestionEvent>("user_suggestion_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserSuggestionEvent> b){Mapping.PublicId(b);Mapping.Long(b,nameof(UserSuggestionEvent.UserSuggestionId),"user_suggestion_id");Mapping.Long(b,nameof(UserSuggestionEvent.ActorUserId),"actor_user_id");Mapping.String(b,nameof(UserSuggestionEvent.ActionCode),"action_code",30,unicode:false);Mapping.String(b,nameof(UserSuggestionEvent.FromStatusCode),"from_status_code",30,nullable:true,unicode:false);Mapping.String(b,nameof(UserSuggestionEvent.ToStatusCode),"to_status_code",30,unicode:false);Mapping.String(b,nameof(UserSuggestionEvent.Note),"note",2000,nullable:true);Mapping.DateTime(b,nameof(UserSuggestionEvent.OccurredAt),"occurred_at",utcDefault:true);Mapping.DateTime(b,nameof(UserSuggestionEvent.CreatedAt),"created_at",utcDefault:true);Mapping.Fk<UserSuggestionEvent,UserSuggestion>(b,nameof(UserSuggestionEvent.UserSuggestionId));Mapping.Fk<UserSuggestionEvent,User>(b,nameof(UserSuggestionEvent.ActorUserId));b.HasIndex(x=>new{x.UserSuggestionId,x.OccurredAt});}
}
