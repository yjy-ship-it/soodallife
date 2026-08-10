namespace SoodalLife.Api.Domain.Entities;

public sealed class ReviewRatingItem
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Review
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public decimal? OverallRating { get; set; }
    public string VerificationStatusCode { get; set; } = "VERIFIED_TRANSACTION";
    public string VisibilityStatusCode { get; set; } = "PUBLIC";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? HiddenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ReviewRating
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public long RatingItemId { get; set; }
    public decimal RatingValue { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ReviewFile
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public long FileId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ReviewProviderReply
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ReviewId { get; set; }
    public long ProviderProfileId { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
