namespace SoodalLife.Api.Domain.Entities;

public sealed class ChatRoom
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string ResourceTypeCode { get; set; } = "TRANSACTION";
    public Guid ResourcePublicId { get; set; }
    public string RoomTypeCode { get; set; } = "DIRECT";
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ChatParticipant
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ChatRoomId { get; set; }
    public long UserId { get; set; }
    public string ParticipantRoleCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime JoinedAt { get; set; }
    public DateTime AccessStartedAt { get; set; }
    public DateTime? AccessEndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ChatMessage
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ChatRoomId { get; set; }
    public long SenderParticipantId { get; set; }
    public string MessageTypeCode { get; set; } = "TEXT";
    public string? Body { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ChatMessageRead
{
    public long Id { get; set; }
    public long ChatRoomId { get; set; }
    public long ChatMessageId { get; set; }
    public long UserId { get; set; }
    public DateTime ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ChatAttachment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ChatMessageId { get; set; }
    public long FileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
}
