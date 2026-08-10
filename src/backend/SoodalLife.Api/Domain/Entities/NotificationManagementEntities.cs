namespace SoodalLife.Api.Domain.Entities;

public sealed class NotificationTemplate
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudienceTypeCode { get; set; } = string.Empty;
    public string EventTypeCode { get; set; } = string.Empty;
    public string ChannelCode { get; set; } = "WEB";
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string AllowedVariablesJson { get; set; } = "[]";
    public bool IsRequiredBusinessNotice { get; set; }
    public bool IsMarketing { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationRecipient
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public string RecipientRoleCode { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationDeliveryAttempt
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long NotificationDeliveryId { get; set; }
    public int AttemptNo { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string? ProviderResponseCode { get; set; }
    public string? FailureReason { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationPreference
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string EventGroupCode { get; set; } = "BUSINESS";
    public bool WebEnabled { get; set; } = true;
    public bool KakaoEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long NotificationId { get; set; }
    public long? NotificationRecipientId { get; set; }
    public long? NotificationDeliveryId { get; set; }
    public long? ActorUserId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? EventDataJson { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
