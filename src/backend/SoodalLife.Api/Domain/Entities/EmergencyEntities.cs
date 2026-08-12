namespace SoodalLife.Api.Domain.Entities;

public sealed class ProviderEmergencySetting
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? TemporarilyUnavailableUntil { get; set; }
    public string? TemporaryUnavailableReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderEmergencyServiceSetting
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderEmergencySettingId { get; set; }
    public long ProviderServiceCategoryId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderEmergencyAvailabilitySlot
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderEmergencyServiceSettingId { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool Is24Hours { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ProviderEmergencyException
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderEmergencySettingId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class EmergencyResponse
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long RequestDispatchId { get; set; }
    public long ProviderProfileId { get; set; }
    public string StatusCode { get; set; } = "PENDING";
    public int? EtaMinutes { get; set; }
    public DateTime? EstimatedArrivalAt { get; set; }
    public string? ConditionsText { get; set; }
    public DateTime RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? SelectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class EmergencyProgressEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long TransactionId { get; set; }
    public long ActorUserId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
