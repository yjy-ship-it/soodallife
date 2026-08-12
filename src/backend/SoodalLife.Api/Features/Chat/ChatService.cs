using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Chat;

public sealed class ChatService(
    SoodalLifeDbContext db,
    IPrivateFileStorage storage,
    IFilePrivacyContract filePrivacy,
    IHubContext<ChatHub> hub,
    ILogger<ChatService> logger)
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RoomLocks = new();
    private const int PageMaximum = 50;
    private const long FileMaximum = 10 * 1024 * 1024;

    public async Task<ChatRoom> EnsureTransactionRoomAsync(TransactionRecord transaction, DateTime now, CancellationToken token)
    {
        var roomLock = RoomLocks.GetOrAdd(transaction.PublicId, _ => new SemaphoreSlim(1, 1));
        await roomLock.WaitAsync(token);
        try
        {
            var tracked = db.ChatRooms.Local.FirstOrDefault(x => x.ResourceTypeCode == "TRANSACTION" &&
                x.ResourcePublicId == transaction.PublicId && x.RoomTypeCode == "DIRECT");
            var room = tracked ?? await db.ChatRooms.SingleOrDefaultAsync(x => x.ResourceTypeCode == "TRANSACTION" &&
                x.ResourcePublicId == transaction.PublicId && x.RoomTypeCode == "DIRECT", token);
            var customerUserId = await db.CustomerProfiles.Where(x => x.Id == transaction.CustomerProfileId)
                .Select(x => x.UserId).SingleAsync(token);
            var providerUserId = await db.ProviderProfiles.Where(x => x.Id == transaction.ProviderProfileId)
                .Select(x => x.UserId).SingleAsync(token);
            if (room is null)
            {
                room = new ChatRoom
                {
                    ResourceTypeCode = "TRANSACTION", ResourcePublicId = transaction.PublicId, RoomTypeCode = "DIRECT",
                    StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now,
                };
                db.ChatRooms.Add(room);
                await db.SaveChangesAsync(token);
            }
            var existingUsers = (await db.ChatParticipants.AsNoTracking().Where(x => x.ChatRoomId == room.Id)
                .Select(x => x.UserId).ToListAsync(token)).ToHashSet();
            if (!existingUsers.Contains(customerUserId)) db.ChatParticipants.Add(Participant(room.Id, customerUserId, "CUSTOMER", now));
            if (!existingUsers.Contains(providerUserId)) db.ChatParticipants.Add(Participant(room.Id, providerUserId, "PROVIDER", now));
            if (db.ChangeTracker.Entries<ChatParticipant>().Any(x => x.State == EntityState.Added)) await db.SaveChangesAsync(token);
            return room;
        }
        finally
        {
            roomLock.Release();
        }
    }

    public async Task<ChatRoomDetail> EnsureForTransactionAsync(ClaimsPrincipal principal, Guid transactionId, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var transaction = await AuthorizedTransaction(identity.UserId, transactionId, token);
        var room = await EnsureTransactionRoomAsync(transaction, DateTime.UtcNow, token);
        return await Detail(room.PublicId, principal, token);
    }

    public async Task<IReadOnlyList<ChatRoomListItem>> Mine(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var rows = (await AuthorizedRooms(identity.UserId).ToListAsync(token))
            .OrderByDescending(x => x.Room.UpdatedAt).Take(200).ToList();
        var result = new List<ChatRoomListItem>(rows.Count);
        foreach (var row in rows)
        {
            var last = await db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == row.Room.Id)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync(token);
            var unread = await UnreadInRoom(row.Room.Id, identity.UserId, token);
            result.Add(new(row.Room.PublicId, row.Transaction.PublicId, row.Room.StatusCode,
                row.CounterpartyRole, row.CounterpartyName, row.ServiceName, TransactionNumber(row.Transaction.PublicId),
                Preview(last), last?.CreatedAt, unread));
        }
        return result.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ToList();
    }

    public async Task<ChatRoomDetail> Detail(Guid roomId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var row = await AuthorizedRooms(identity.UserId, roomId).SingleOrDefaultAsync(token)
            ?? throw NotFound();
        return new(row.Room.PublicId, row.Transaction.PublicId, row.Room.StatusCode, row.CounterpartyRole,
            row.CounterpartyName, row.ServiceName, TransactionNumber(row.Transaction.PublicId),
            Convert.ToBase64String(row.Room.RowVersion));
    }

    public async Task<ChatMessagePage> Messages(Guid roomId, Guid? before, int pageSize, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        var take = Math.Clamp(pageSize, 1, PageMaximum);
        long? beforeId = null;
        if (before.HasValue)
            beforeId = await db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id && x.PublicId == before)
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw NotFound();
        var query = db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id);
        if (beforeId.HasValue) query = query.Where(x => x.Id < beforeId);
        var rows = await query.OrderByDescending(x => x.Id).Take(take + 1).ToListAsync(token);
        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        var mapped = new List<ChatMessageResponse>(rows.Count);
        foreach (var row in rows) mapped.Add(await Message(row, identity.UserId, token));
        mapped.Reverse();
        return new(mapped, hasMore ? rows[^1].PublicId : null, hasMore);
    }

    public async Task<ChatMessageResponse> SendText(Guid roomId, SendChatTextInput input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        EnsureWritable(access.Room);
        var body = input.Body.Trim();
        if (body.Length is < 1 or > 4000) throw Bad("CHAT_MESSAGE_BODY_INVALID", "메시지는 1자 이상 4,000자 이하로 입력해 주세요.");
        ValidateKey(input.IdempotencyKey);
        var existing = await db.ChatMessages.SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (existing is not null)
        {
            if (existing.SenderParticipantId != access.Participant.Id || existing.ChatRoomId != access.Room.Id) throw Conflict("CHAT_IDEMPOTENCY_CONFLICT", "이미 사용된 요청 식별자입니다.");
            return await Message(existing, identity.UserId, token);
        }
        var message = await PersistMessage(access, "TEXT", body, input.IdempotencyKey, null, identity.UserId, token);
        await Broadcast(access.Room.PublicId, message, token);
        return message;
    }

    public async Task<ChatMessageResponse> SendFile(Guid roomId, IFormFile upload, string idempotencyKey, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        EnsureWritable(access.Room);
        ValidateKey(idempotencyKey);
        var existing = await db.ChatMessages.SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, token);
        if (existing is not null)
        {
            if (existing.SenderParticipantId != access.Participant.Id || existing.ChatRoomId != access.Room.Id) throw Conflict("CHAT_IDEMPOTENCY_CONFLICT", "이미 사용된 요청 식별자입니다.");
            return await Message(existing, identity.UserId, token);
        }
        var validated = await ValidateFile(upload, token);
        var now = DateTime.UtcNow;
        var storageKey = $"chat/{access.Room.PublicId:N}/{Guid.NewGuid():N}{validated.Extension}";
        var stored = new StoredFile
        {
            PurposeCode = "CHAT_ATTACHMENT", StorageContainer = "development-private", StorageKey = storageKey,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)), OriginalFileName = validated.Name,
            ContentType = validated.ContentType, SizeBytes = upload.Length, Sha256Hex = validated.Sha256,
            StatusCode = "PENDING", MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
            PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated, SanitizationStatusCode = FilePrivacyCodes.NotIntegrated,
            UploadedByUserId = identity.UserId, CreatedAt = now,
        };
        db.Files.Add(stored);
        await db.SaveChangesAsync(token);
        try
        {
            await using var stream = upload.OpenReadStream();
            await storage.SaveAsync(storageKey, stream, token);
            stored.StatusCode = "ACTIVE";
            stored.ActivatedAt = now;
            stored.ScanResultText = "Malware and privacy inspection are not integrated. Counterparty access remains fail closed.";
            var message = await PersistMessage(access, "FILE", null, idempotencyKey, stored, identity.UserId, token);
            await Broadcast(access.Room.PublicId, message, token);
            return message;
        }
        catch
        {
            await storage.DeleteIfExistsAsync(storageKey, token);
            throw;
        }
    }

    public async Task MarkRead(Guid roomId, MarkChatReadInput input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        var target = await db.ChatMessages.AsNoTracking().SingleOrDefaultAsync(x => x.ChatRoomId == access.Room.Id && x.PublicId == input.MessageId, token)
            ?? throw NotFound();
        var existing = await db.ChatMessageReads.Where(x => x.ChatRoomId == access.Room.Id && x.UserId == identity.UserId && x.ChatMessageId <= target.Id)
            .Select(x => x.ChatMessageId).ToListAsync(token);
        var existingIds = existing.ToHashSet();
        var ids = await db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id && x.Id <= target.Id &&
                !db.ChatParticipants.Any(p => p.Id == x.SenderParticipantId && p.UserId == identity.UserId))
            .Select(x => x.Id).ToListAsync(token);
        var now = DateTime.UtcNow;
        db.ChatMessageReads.AddRange(ids.Where(x => !existingIds.Contains(x)).Select(x => new ChatMessageRead
        {
            ChatRoomId = access.Room.Id, ChatMessageId = x, UserId = identity.UserId, ReadAt = now, CreatedAt = now,
        }));
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException) { /* A concurrent duplicate read is already the requested final state. */ }
        try { await hub.Clients.Group(Group(roomId)).SendAsync("messagesRead", new { roomId, throughMessageId = input.MessageId }, token); }
        catch (Exception exception) { logger.LogWarning(exception, "Chat read realtime delivery failed for room {RoomId}.", roomId); }
    }

    public async Task<ChatUnreadCountResponse> Unread(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var roomIds = (await AuthorizedRooms(identity.UserId).ToListAsync(token)).Select(x => x.Room.Id).ToArray();
        var count = await db.ChatMessages.AsNoTracking().CountAsync(message => roomIds.Contains(message.ChatRoomId) &&
            !db.ChatParticipants.Any(p => p.Id == message.SenderParticipantId && p.UserId == identity.UserId) &&
            !db.ChatMessageReads.Any(read => read.ChatMessageId == message.Id && read.UserId == identity.UserId), token);
        return new(count);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenAttachment(Guid roomId, Guid attachmentId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        var row = await (from attachment in db.ChatAttachments.AsNoTracking()
                         join message in db.ChatMessages.AsNoTracking() on attachment.ChatMessageId equals message.Id
                         join file in db.Files.AsNoTracking() on attachment.FileId equals file.Id
                         where attachment.PublicId == attachmentId && message.ChatRoomId == access.Room.Id
                         select new { attachment, message, file }).SingleOrDefaultAsync(token) ?? throw NotFound();
        StoredFile published;
        if (row.file.UploadedByUserId == identity.UserId) published = row.file;
        else
        {
            var derivative = await Sanitized(row.file.Id, token);
            var decision = filePrivacy.Evaluate(row.file, derivative, FileAccessAudience.SelectedProvider);
            if (!decision.Allowed || decision.PublishedFile is null) throw NotFound();
            published = decision.PublishedFile;
        }
        return (await storage.OpenReadAsync(published.StorageKey, token), published.ContentType, SafeName(published));
    }

    public async Task<bool> CanJoin(Guid roomId, ClaimsPrincipal principal, CancellationToken token)
    {
        try { var identity = await Identity(principal, token); _ = await Access(roomId, identity.UserId, token); return true; }
        catch (ChatBusinessException) { return false; }
    }

    private IQueryable<AuthorizedRoomRow> AuthorizedRooms(long userId, Guid? roomId = null) =>
        from room in db.ChatRooms.AsNoTracking()
        join participant in db.ChatParticipants.AsNoTracking() on room.Id equals participant.ChatRoomId
        join transaction in db.Transactions.AsNoTracking() on room.ResourcePublicId equals transaction.PublicId
        join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
        join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
        join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
        where (!roomId.HasValue || room.PublicId == roomId.Value) && room.ResourceTypeCode == "TRANSACTION" && room.RoomTypeCode == "DIRECT" && participant.UserId == userId &&
              participant.StatusCode == "ACTIVE" && participant.AccessStartedAt <= DateTime.UtcNow &&
              (participant.AccessEndedAt == null || participant.AccessEndedAt > DateTime.UtcNow) &&
              (customer.UserId == userId || provider.UserId == userId)
        select new AuthorizedRoomRow(room, participant, transaction, category.Name,
            customer.UserId == userId ? "PROVIDER" : "CUSTOMER",
            customer.UserId == userId ? provider.BusinessName : customer.DisplayName);

    private async Task<RoomAccess> Access(Guid roomId, long userId, CancellationToken token)
    {
        var row = await AuthorizedRooms(userId, roomId).SingleOrDefaultAsync(token) ?? throw NotFound();
        var room = await db.ChatRooms.SingleAsync(x => x.Id == row.Room.Id, token);
        var participant = await db.ChatParticipants.SingleAsync(x => x.Id == row.Participant.Id, token);
        return new(room, participant);
    }

    private async Task<TransactionRecord> AuthorizedTransaction(long userId, Guid transactionId, CancellationToken token) =>
        await (from transaction in db.Transactions
               join customer in db.CustomerProfiles on transaction.CustomerProfileId equals customer.Id
               join provider in db.ProviderProfiles on transaction.ProviderProfileId equals provider.Id
               where transaction.PublicId == transactionId && (customer.UserId == userId || provider.UserId == userId)
               select transaction).SingleOrDefaultAsync(token) ?? throw NotFound();

    private async Task<ChatMessageResponse> PersistMessage(RoomAccess access, string type, string? body, string key, StoredFile? file, long userId, CancellationToken token)
    {
        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational() && db.Database.CurrentTransaction is null)
            transaction = await db.Database.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ChatRoomId = access.Room.Id, SenderParticipantId = access.Participant.Id, MessageTypeCode = type,
            Body = body, IdempotencyKey = key.Trim(), CreatedAt = now,
        };
        db.ChatMessages.Add(message);
        try
        {
            await db.SaveChangesAsync(token);
            if (file is not null)
            {
                db.ChatAttachments.Add(new ChatAttachment
                {
                    ChatMessageId = message.Id, FileId = file.Id, CreatedAt = now, CreatedByUserId = userId,
                });
            }
            db.OutboxEvents.Add(new OutboxEvent
            {
                AggregateType = "ChatRoom", AggregatePublicId = access.Room.PublicId, EventType = "CHAT.MESSAGE.CREATED",
                PayloadJson = JsonSerializer.Serialize(new { roomId = access.Room.PublicId, messageId = message.PublicId, messageType = type }),
                StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"chat-message-created:{message.PublicId:N}",
                CreatedByUserId = userId,
            });
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return await Message(message, userId, token);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(token);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<ChatMessageResponse> Message(ChatMessage message, long currentUserId, CancellationToken token)
    {
        var sender = await (from participant in db.ChatParticipants.AsNoTracking()
                            join user in db.Users.AsNoTracking() on participant.UserId equals user.Id
                            where participant.Id == message.SenderParticipantId
                            select new { participant.UserId, participant.ParticipantRoleCode }).SingleAsync(token);
        var display = sender.ParticipantRoleCode == "CUSTOMER"
            ? await db.CustomerProfiles.AsNoTracking().Where(x => x.UserId == sender.UserId).Select(x => x.DisplayName).SingleAsync(token)
            : await db.ProviderProfiles.AsNoTracking().Where(x => x.UserId == sender.UserId).Select(x => x.BusinessName).SingleAsync(token);
        var read = await db.ChatMessageReads.AsNoTracking().AnyAsync(x => x.ChatMessageId == message.Id && x.UserId != sender.UserId, token);
        ChatAttachmentResponse? attachmentResponse = null;
        var attachment = await (from link in db.ChatAttachments.AsNoTracking()
                                join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                                where link.ChatMessageId == message.Id
                                select new { link, file }).SingleOrDefaultAsync(token);
        if (attachment is not null)
        {
            var owner = attachment.file.UploadedByUserId == currentUserId;
            var decision = owner
                ? new FilePrivacyPublicationDecision(true, attachment.file, "OWNER_ORIGINAL", "OWNER")
                : filePrivacy.Evaluate(attachment.file, await Sanitized(attachment.file.Id, token), FileAccessAudience.SelectedProvider);
            var roomPublicId = await db.ChatRooms.AsNoTracking().Where(x => x.Id == message.ChatRoomId).Select(x => x.PublicId).SingleAsync(token);
            var publishedName = owner ? SafeName(attachment.file) : decision.Allowed && decision.PublishedFile is not null
                ? SafeName(decision.PublishedFile) : "안전검사 중인 첨부파일";
            attachmentResponse = new(attachment.link.PublicId, publishedName, attachment.file.ContentType,
                attachment.file.SizeBytes, decision.Allowed, decision.Allowed ? $"/api/v1/chat/rooms/{message.ChatRoomId}/attachments/{attachment.link.PublicId}" : null,
                decision.Allowed ? decision.PublicationMode : decision.ReasonCode);
            if (decision.Allowed)
                attachmentResponse = attachmentResponse with { DownloadUrl = $"/api/v1/chat/rooms/{roomPublicId}/attachments/{attachment.link.PublicId}" };
        }
        return new(message.PublicId, message.MessageTypeCode, message.Body, sender.ParticipantRoleCode, display,
            sender.UserId == currentUserId, message.CreatedAt, read, attachmentResponse, Convert.ToBase64String(message.RowVersion));
    }

    private async Task<StoredFile?> Sanitized(long originalId, CancellationToken token) =>
        await (from derivative in db.FileDerivatives.AsNoTracking()
               join file in db.Files.AsNoTracking() on derivative.DerivedFileId equals file.Id
               where derivative.OriginalFileId == originalId && derivative.DerivativeTypeCode == FilePrivacyCodes.PrivacySanitized
               orderby derivative.CreatedAt descending select file).FirstOrDefaultAsync(token);

    private async Task Broadcast(Guid roomId, ChatMessageResponse message, CancellationToken token)
    {
        try { await hub.Clients.Group(Group(roomId)).SendAsync("messageCreated", message, token); }
        catch (Exception exception) { logger.LogWarning(exception, "Chat realtime delivery failed for room {RoomId}; the saved message remains authoritative.", roomId); }
    }

    private async Task<int> UnreadInRoom(long roomId, long userId, CancellationToken token) =>
        await db.ChatMessages.AsNoTracking().CountAsync(message => message.ChatRoomId == roomId &&
            !db.ChatParticipants.Any(p => p.Id == message.SenderParticipantId && p.UserId == userId) &&
            !db.ChatMessageReads.Any(read => read.ChatMessageId == message.Id && read.UserId == userId), token);

    private async Task<UserIdentity> Identity(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
            throw new ChatBusinessException("AUTHENTICATION_REQUIRED", "로그인이 필요합니다.", 401);
        return await db.Users.AsNoTracking().Where(x => x.PublicId == publicId && x.StatusCode == "ACTIVE")
            .Select(x => new UserIdentity(x.Id)).SingleOrDefaultAsync(token)
            ?? throw new ChatBusinessException("ACTIVE_USER_REQUIRED", "활성 계정을 확인할 수 없습니다.", 401);
    }

    private static ChatParticipant Participant(long roomId, long userId, string role, DateTime now) => new()
    {
        ChatRoomId = roomId, UserId = userId, ParticipantRoleCode = role, StatusCode = "ACTIVE",
        JoinedAt = now, AccessStartedAt = now, CreatedAt = now,
    };

    private static void EnsureWritable(ChatRoom room)
    {
        if (room.StatusCode != "ACTIVE") throw Conflict("CHAT_ROOM_NOT_WRITABLE", "현재 채팅방에서는 새 메시지를 보낼 수 없습니다.");
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120) throw Bad("IDEMPOTENCY_KEY_INVALID", "안전한 요청 식별자가 필요합니다.");
    }

    private static async Task<ValidatedFile> ValidateFile(IFormFile file, CancellationToken token)
    {
        if (file.Length is <= 0 or > FileMaximum) throw Bad("CHAT_FILE_SIZE_INVALID", "10MB 이하의 파일을 선택해 주세요.");
        var name = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(name) || name != file.FileName || name.Length > 255) throw Bad("CHAT_FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var expected = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".pdf" => "application/pdf", _ => null };
        if (expected is null || !string.Equals(file.ContentType, expected, StringComparison.OrdinalIgnoreCase))
            throw Bad("CHAT_FILE_TYPE_INVALID", "JPG, PNG, PDF 파일만 첨부할 수 있습니다.");
        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), token);
        var signatureValid = expected switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            "image/png" => read >= 8 && header.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "application/pdf" => read >= 5 && Encoding.ASCII.GetString(header, 0, 5) == "%PDF-",
            _ => false,
        };
        if (!signatureValid) throw Bad("CHAT_FILE_SIGNATURE_INVALID", "파일 내용과 형식이 일치하지 않습니다.");
        stream.Position = 0;
        var hash = await SHA256.HashDataAsync(stream, token);
        return new(name, expected, extension == ".jpeg" ? ".jpg" : extension, Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static string SafeName(StoredFile file) => Path.GetFileName(file.OriginalFileName);
    private static string TransactionNumber(Guid id) => $"TR-{id.ToString("N")[..10].ToUpperInvariant()}";
    private static string? Preview(ChatMessage? message) => message is null ? null : message.MessageTypeCode == "FILE" ? "파일을 보냈습니다." : message.Body?.Length > 80 ? message.Body[..80] : message.Body;
    public static string Group(Guid roomId) => $"chat-room:{roomId:N}";
    private static ChatBusinessException NotFound() => new("CHAT_ROOM_NOT_FOUND", "채팅방을 찾을 수 없습니다.", 404);
    private static ChatBusinessException Bad(string code, string message) => new(code, message, 400);
    private static ChatBusinessException Conflict(string code, string message) => new(code, message, 409);

    private sealed record UserIdentity(long UserId);
    private sealed record RoomAccess(ChatRoom Room, ChatParticipant Participant);
    private sealed record ValidatedFile(string Name, string ContentType, string Extension, string Sha256);
    private sealed record AuthorizedRoomRow(ChatRoom Room, ChatParticipant Participant, TransactionRecord Transaction,
        string ServiceName, string CounterpartyRole, string CounterpartyName);
}
