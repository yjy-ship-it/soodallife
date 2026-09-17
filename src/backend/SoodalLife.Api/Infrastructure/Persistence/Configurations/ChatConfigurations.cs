using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ChatRoomConfiguration() : EntityConfiguration<ChatRoom>("chat_rooms")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ChatRoom> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(ChatRoom.ResourceTypeCode), "resource_type", 30, unicode: false);
        Mapping.Guid(b, nameof(ChatRoom.ResourcePublicId), "resource_public_id");
        Mapping.String(b, nameof(ChatRoom.RoomTypeCode), "room_type", 30, unicode: false, defaultValue: "DIRECT");
        Mapping.String(b, nameof(ChatRoom.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.DateTime(b, nameof(ChatRoom.CreatedAt), "created_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ChatRoom.UpdatedAt), "updated_at", utcDefault: true);
        Mapping.RowVersion(b);
        b.HasIndex(x => new { x.ResourceTypeCode, x.ResourcePublicId, x.RoomTypeCode }).IsUnique();
        b.HasIndex(x => x.StatusCode);
        b.ToTable("chat_rooms", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_chat_rooms_resource_type", "[resource_type] IN ('TRANSACTION','SUBSCRIPTION','INTERIOR','AFTER_SERVICE','PROVIDER_CONSULTATION')");
            t.HasCheckConstraint("CK_chat_rooms_room_type", "[room_type] IN ('DIRECT','PRIMARY_CONTRACTOR','SITE_SURVEY')");
            t.HasCheckConstraint("CK_chat_rooms_status", "[status_code] IN ('ACTIVE','READ_ONLY','CLOSED')");
        });
    }
}

internal sealed class ChatParticipantConfiguration() : EntityConfiguration<ChatParticipant>("chat_participants")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ChatParticipant> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ChatParticipant.ChatRoomId), "chat_room_id");
        Mapping.Long(b, nameof(ChatParticipant.UserId), "user_id");
        Mapping.String(b, nameof(ChatParticipant.ParticipantRoleCode), "participant_role", 20, unicode: false);
        Mapping.String(b, nameof(ChatParticipant.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.DateTime(b, nameof(ChatParticipant.JoinedAt), "joined_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ChatParticipant.AccessStartedAt), "access_started_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ChatParticipant.AccessEndedAt), "access_ended_at", nullable: true);
        Mapping.DateTime(b, nameof(ChatParticipant.CreatedAt), "created_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<ChatParticipant, ChatRoom>(b, nameof(ChatParticipant.ChatRoomId));
        Mapping.Fk<ChatParticipant, User>(b, nameof(ChatParticipant.UserId));
        b.HasIndex(x => new { x.ChatRoomId, x.UserId }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.StatusCode });
        b.ToTable("chat_participants", t =>
        {
            t.UseSqlOutputClause(false);
            t.HasCheckConstraint("CK_chat_participants_role", "[participant_role] IN ('CUSTOMER','PROVIDER')");
            t.HasCheckConstraint("CK_chat_participants_status", "[status_code] IN ('ACTIVE','ENDED')");
            t.HasCheckConstraint("CK_chat_participants_access", "[access_ended_at] IS NULL OR [access_ended_at] >= [access_started_at]");
        });
    }
}

internal sealed class ChatMessageConfiguration() : EntityConfiguration<ChatMessage>("chat_messages")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ChatMessage> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ChatMessage.ChatRoomId), "chat_room_id");
        Mapping.Long(b, nameof(ChatMessage.SenderParticipantId), "sender_participant_id");
        Mapping.String(b, nameof(ChatMessage.MessageTypeCode), "message_type", 20, unicode: false);
        Mapping.String(b, nameof(ChatMessage.Body), "body", 4000, nullable: true);
        Mapping.String(b, nameof(ChatMessage.IdempotencyKey), "idempotency_key", 120, unicode: false);
        Mapping.DateTime(b, nameof(ChatMessage.CreatedAt), "created_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<ChatMessage, ChatRoom>(b, nameof(ChatMessage.ChatRoomId));
        Mapping.Fk<ChatMessage, ChatParticipant>(b, nameof(ChatMessage.SenderParticipantId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.ChatRoomId, x.Id }).IsDescending(false, true);
        b.ToTable("chat_messages", t =>
        {
            t.HasCheckConstraint("CK_chat_messages_type", "[message_type] IN ('TEXT','FILE')");
            t.HasCheckConstraint("CK_chat_messages_body", "([message_type] = 'TEXT' AND [body] IS NOT NULL AND LEN(LTRIM(RTRIM([body]))) > 0) OR [message_type] = 'FILE'");
        });
    }
}

internal sealed class ChatMessageReadConfiguration() : EntityConfiguration<ChatMessageRead>("chat_message_reads")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ChatMessageRead> b)
    {
        Mapping.Long(b, nameof(ChatMessageRead.ChatRoomId), "chat_room_id");
        Mapping.Long(b, nameof(ChatMessageRead.ChatMessageId), "chat_message_id");
        Mapping.Long(b, nameof(ChatMessageRead.UserId), "user_id");
        Mapping.DateTime(b, nameof(ChatMessageRead.ReadAt), "read_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ChatMessageRead.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<ChatMessageRead, ChatRoom>(b, nameof(ChatMessageRead.ChatRoomId));
        Mapping.Fk<ChatMessageRead, ChatMessage>(b, nameof(ChatMessageRead.ChatMessageId));
        Mapping.Fk<ChatMessageRead, User>(b, nameof(ChatMessageRead.UserId));
        b.HasIndex(x => new { x.ChatMessageId, x.UserId }).IsUnique();
        b.HasIndex(x => new { x.ChatRoomId, x.UserId, x.ReadAt });
    }
}

internal sealed class ChatAttachmentConfiguration() : EntityConfiguration<ChatAttachment>("chat_attachments")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ChatAttachment> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ChatAttachment.ChatMessageId), "chat_message_id");
        Mapping.Long(b, nameof(ChatAttachment.FileId), "file_id");
        Mapping.DateTime(b, nameof(ChatAttachment.CreatedAt), "created_at", utcDefault: true);
        Mapping.Long(b, nameof(ChatAttachment.CreatedByUserId), "created_by_user_id");
        Mapping.Fk<ChatAttachment, ChatMessage>(b, nameof(ChatAttachment.ChatMessageId));
        Mapping.Fk<ChatAttachment, StoredFile>(b, nameof(ChatAttachment.FileId));
        Mapping.Fk<ChatAttachment, User>(b, nameof(ChatAttachment.CreatedByUserId));
        b.HasIndex(x => x.FileId).IsUnique();
        b.HasIndex(x => new { x.ChatMessageId, x.FileId }).IsUnique();
    }
}
