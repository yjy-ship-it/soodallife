using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ReviewRatingItemConfiguration() : EntityConfiguration<ReviewRatingItem>("review_rating_items")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReviewRatingItem> b)
    {
        Mapping.PublicId(b); Mapping.String(b,nameof(ReviewRatingItem.Code),"code",50,unicode:false); Mapping.String(b,nameof(ReviewRatingItem.Name),"name",100);
        Mapping.String(b,nameof(ReviewRatingItem.Description),"description",1000,nullable:true); Mapping.Decimal(b,nameof(ReviewRatingItem.MinValue),"min_value",precision:9,scale:4);
        Mapping.Decimal(b,nameof(ReviewRatingItem.MaxValue),"max_value",precision:9,scale:4); Mapping.Int(b,nameof(ReviewRatingItem.DisplayOrder),"display_order");
        Mapping.Bool(b,nameof(ReviewRatingItem.IsRequired),"is_required",true); Mapping.Bool(b,nameof(ReviewRatingItem.IsActive),"is_active",true);
        Mapping.DateTime(b,nameof(ReviewRatingItem.EffectiveFrom),"effective_from",nullable:true); Mapping.DateTime(b,nameof(ReviewRatingItem.EffectiveTo),"effective_to",nullable:true);
        Mapping.FullAudit(b); b.HasIndex(x=>x.Code).IsUnique(); b.HasIndex(x=>new{x.IsActive,x.DisplayOrder});
        b.ToTable("review_rating_items",t=>{t.HasCheckConstraint("CK_review_rating_items_range","[max_value] > [min_value]");t.HasCheckConstraint("CK_review_rating_items_period","[effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]");});
    }
}

internal sealed class ReviewConfiguration() : EntityConfiguration<Review>("reviews")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Review> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(Review.TransactionId),"transaction_id"); Mapping.Long(b,nameof(Review.CustomerProfileId),"customer_profile_id"); Mapping.Long(b,nameof(Review.ProviderProfileId),"provider_profile_id");
        Mapping.String(b,nameof(Review.BodyText),"body_text",4000); Mapping.Decimal(b,nameof(Review.OverallRating),"overall_rating",nullable:true,precision:9,scale:4);
        Mapping.String(b,nameof(Review.VerificationStatusCode),"verification_status_code",30,unicode:false); Mapping.String(b,nameof(Review.VisibilityStatusCode),"visibility_status_code",20,unicode:false);
        Mapping.String(b,nameof(Review.IdempotencyKey),"idempotency_key",150,unicode:false); Mapping.DateTime(b,nameof(Review.SubmittedAt),"submitted_at"); Mapping.DateTime(b,nameof(Review.PublishedAt),"published_at",nullable:true); Mapping.DateTime(b,nameof(Review.HiddenAt),"hidden_at",nullable:true);
        Mapping.DateTime(b,nameof(Review.CreatedAt),"created_at",utcDefault:true); Mapping.DateTime(b,nameof(Review.UpdatedAt),"updated_at",utcDefault:true); Mapping.RowVersion(b);
        Mapping.Fk<Review,TransactionRecord>(b,nameof(Review.TransactionId)); Mapping.Fk<Review,CustomerProfile>(b,nameof(Review.CustomerProfileId)); Mapping.Fk<Review,ProviderProfile>(b,nameof(Review.ProviderProfileId));
        b.HasIndex(x=>x.TransactionId).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.ProviderProfileId,x.VisibilityStatusCode,x.SubmittedAt}).IsDescending(false,false,true); b.HasIndex(x=>x.CustomerProfileId);
        b.ToTable("reviews",t=>{t.HasCheckConstraint("CK_reviews_verification","[verification_status_code] = 'VERIFIED_TRANSACTION'");t.HasCheckConstraint("CK_reviews_visibility","[visibility_status_code] IN ('PUBLIC','HIDDEN')");});
    }
}

internal sealed class ReviewRatingConfiguration() : EntityConfiguration<ReviewRating>("review_ratings")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReviewRating> b)
    {
        Mapping.Long(b,nameof(ReviewRating.ReviewId),"review_id"); Mapping.Long(b,nameof(ReviewRating.RatingItemId),"rating_item_id"); Mapping.Decimal(b,nameof(ReviewRating.RatingValue),"rating_value",precision:9,scale:4); Mapping.Int(b,nameof(ReviewRating.DisplayOrder),"display_order"); Mapping.DateTime(b,nameof(ReviewRating.CreatedAt),"created_at",utcDefault:true);
        Mapping.Fk<ReviewRating,Review>(b,nameof(ReviewRating.ReviewId)); Mapping.Fk<ReviewRating,ReviewRatingItem>(b,nameof(ReviewRating.RatingItemId)); b.HasIndex(x=>new{x.ReviewId,x.RatingItemId}).IsUnique(); b.HasIndex(x=>x.RatingItemId);
    }
}

internal sealed class ReviewFileConfiguration() : EntityConfiguration<ReviewFile>("review_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReviewFile> b)
    {
        Mapping.Long(b,nameof(ReviewFile.ReviewId),"review_id"); Mapping.Long(b,nameof(ReviewFile.FileId),"file_id"); Mapping.Int(b,nameof(ReviewFile.DisplayOrder),"display_order"); Mapping.DateTime(b,nameof(ReviewFile.CreatedAt),"created_at",utcDefault:true);
        Mapping.Fk<ReviewFile,Review>(b,nameof(ReviewFile.ReviewId)); Mapping.Fk<ReviewFile,StoredFile>(b,nameof(ReviewFile.FileId)); b.HasIndex(x=>new{x.ReviewId,x.FileId}).IsUnique(); b.HasIndex(x=>x.FileId).IsUnique();
    }
}

internal sealed class ReviewProviderReplyConfiguration() : EntityConfiguration<ReviewProviderReply>("review_provider_replies")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReviewProviderReply> b)
    {
        Mapping.PublicId(b); Mapping.Long(b,nameof(ReviewProviderReply.ReviewId),"review_id"); Mapping.Long(b,nameof(ReviewProviderReply.ProviderProfileId),"provider_profile_id"); Mapping.String(b,nameof(ReviewProviderReply.BodyText),"body_text",2000); Mapping.String(b,nameof(ReviewProviderReply.IdempotencyKey),"idempotency_key",150,unicode:false);
        Mapping.DateTime(b,nameof(ReviewProviderReply.SubmittedAt),"submitted_at"); Mapping.DateTime(b,nameof(ReviewProviderReply.CreatedAt),"created_at",utcDefault:true); Mapping.DateTime(b,nameof(ReviewProviderReply.UpdatedAt),"updated_at",utcDefault:true); Mapping.RowVersion(b);
        Mapping.Fk<ReviewProviderReply,Review>(b,nameof(ReviewProviderReply.ReviewId)); Mapping.Fk<ReviewProviderReply,ProviderProfile>(b,nameof(ReviewProviderReply.ProviderProfileId)); b.HasIndex(x=>x.ReviewId).IsUnique(); b.HasIndex(x=>x.IdempotencyKey).IsUnique();
    }
}
