namespace SoodalLife.Api.Domain.Entities;

public sealed class NotificationBroadcast
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? NotificationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AudienceCode { get; set; } = "ALL";
    public string KindCode { get; set; } = "BUSINESS_NOTICE";
    public string ChannelsJson { get; set; } = "[]";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public int EstimatedAudienceCount { get; set; }
    public int RecipientsProcessedCount { get; set; }
    public int DeliveriesCreatedCount { get; set; }
    public long? LastProcessedUserId { get; set; }
    public DateTime? PreviewedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public long? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
