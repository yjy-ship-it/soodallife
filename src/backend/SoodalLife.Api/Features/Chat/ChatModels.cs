namespace SoodalLife.Api.Features.Chat;

public sealed record ChatRoomListItem(
    Guid Id, Guid TransactionId, string StatusCode, string CounterpartyRoleCode, string CounterpartyDisplayName,
    string ServiceName, string TransactionNumber, string? LastMessagePreview, DateTime? LastMessageAt, int UnreadCount);

public sealed record ChatRoomDetail(
    Guid Id, Guid TransactionId, string StatusCode, string CounterpartyRoleCode, string CounterpartyDisplayName,
    string ServiceName, string TransactionNumber, string RowVersion);

public sealed record ChatAttachmentResponse(
    Guid Id, string FileName, string ContentType, long SizeBytes, bool Available, string? DownloadUrl, string PublicationStatus);

public sealed record ChatMessageResponse(
    Guid Id, string MessageTypeCode, string? Body, string SenderRoleCode, string SenderDisplayName,
    bool IsMine, DateTime CreatedAt, bool IsReadByCounterparty, ChatAttachmentResponse? Attachment, string RowVersion);

public sealed record ChatMessagePage(IReadOnlyList<ChatMessageResponse> Items, Guid? NextCursor, bool HasMore);
public sealed record ChatUnreadCountResponse(int Count);
public sealed record SendChatTextInput(string Body, string IdempotencyKey);
public sealed record MarkChatReadInput(Guid MessageId);

public sealed class ChatBusinessException(string code, string message, int statusCode = 409) : Exception(message)
{
    public string BusinessCode { get; } = code;
    public int StatusCode { get; } = statusCode;
}
