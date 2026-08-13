namespace SoodalLife.Api.Domain.Entities;

public sealed class ProviderExitRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long ProviderProfileId { get; set; }
    public long? WalletRefundRequestId { get; set; }
    public string RequestTypeCode { get; set; } = "PROVIDER_ROLE_EXIT";
    public string Reason { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "REQUESTED";
    public string ReviewStatusCode { get; set; } = "PENDING";
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
