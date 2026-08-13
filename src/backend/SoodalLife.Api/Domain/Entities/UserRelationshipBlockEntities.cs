namespace SoodalLife.Api.Domain.Entities;

public sealed class UserRelationshipBlock
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerProfileId { get; set; }
    public long ProviderProfileId { get; set; }
    public string DirectionCode { get; set; } = "CUSTOMER_TO_PROVIDER";
    public string StatusCode { get; set; } = "ACTIVE";
    public string? ReasonCode { get; set; }
    public string? PrivateMemo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public long? ReleasedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ReleaseIdempotencyKey { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
