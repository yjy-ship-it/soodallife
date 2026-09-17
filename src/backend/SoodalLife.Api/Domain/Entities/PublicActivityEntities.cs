namespace SoodalLife.Api.Domain.Entities;

public sealed class PublicActivityEvent
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string SourceTypeCode { get; set; } = string.Empty;
    public long SourceEntityId { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
