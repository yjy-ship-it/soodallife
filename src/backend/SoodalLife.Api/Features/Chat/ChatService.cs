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
    IChatResourceAuthorizationResolver authorizationResolver,
    IHubContext<ChatHub> hub,
    ILogger<ChatService> logger)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RoomLocks = new();
    private const int PageMaximum = 50;
    private const long FileMaximum = SafeImageUploadPolicy.MaximumBytes;

    public async Task<ChatRoom> EnsureTransactionRoomAsync(TransactionRecord transaction, DateTime now, CancellationToken token)
    {
        var authorization = await authorizationResolver.ResolveAsync(ChatResourceTypes.Transaction, transaction.PublicId, "DIRECT", token)
            ?? throw NotFound();
        return await EnsureResourceRoomAsync(authorization, now, token);
    }

    private async Task<ChatRoom> EnsureResourceRoomAsync(ChatResourceAuthorization authorization, DateTime now, CancellationToken token)
    {
        var lockKey = $"{authorization.ResourceTypeCode}:{authorization.ResourceId:N}:{authorization.RoomTypeCode}";
        var roomLock = RoomLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await roomLock.WaitAsync(token);
        try
        {
            var tracked = db.ChatRooms.Local.FirstOrDefault(x => x.ResourceTypeCode == authorization.ResourceTypeCode &&
                x.ResourcePublicId == authorization.ResourceId && x.RoomTypeCode == authorization.RoomTypeCode);
            var room = tracked ?? await db.ChatRooms.SingleOrDefaultAsync(x => x.ResourceTypeCode == authorization.ResourceTypeCode &&
                x.ResourcePublicId == authorization.ResourceId && x.RoomTypeCode == authorization.RoomTypeCode, token);
            if (room is null)
            {
                if (!authorization.CanCreateRoom)
                    throw Conflict("CHAT_ROOM_CREATION_POLICY_REQUIRED", "종료된 업무의 신규 채팅방 생성 정책이 확정되지 않았습니다.");
                room = new ChatRoom
                {
                    ResourceTypeCode = authorization.ResourceTypeCode, ResourcePublicId = authorization.ResourceId,
                    RoomTypeCode = authorization.RoomTypeCode,
                    StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now,
                };
                db.ChatRooms.Add(room);
                try { await db.SaveChangesAsync(token); }
                catch (DbUpdateException) when (db.Database.IsRelational())
                {
                    db.Entry(room).State = EntityState.Detached;
                    room = await db.ChatRooms.SingleAsync(x => x.ResourceTypeCode == authorization.ResourceTypeCode &&
                        x.ResourcePublicId == authorization.ResourceId && x.RoomTypeCode == authorization.RoomTypeCode, token);
                }
            }
            await SynchronizeParticipants(room, authorization, now, token);
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
        var authorization = await AuthorizedResource(ChatResourceTypes.Transaction, transactionId, "DIRECT", identity.UserId, token);
        var room = await EnsureResourceRoomAsync(authorization, DateTime.UtcNow, token);
        return await Detail(room.PublicId, principal, token);
    }

    public async Task<ChatRoomDetail> EnsureForResourceAsync(ClaimsPrincipal principal, string resourceType, Guid resourceId, string roomType, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var authorization = await AuthorizedResource(resourceType, resourceId, roomType, identity.UserId, token);
        var room = await EnsureResourceRoomAsync(authorization, DateTime.UtcNow, token);
        return await Detail(room.PublicId, principal, token);
    }

    public async Task<ChatRoomDetail> EnsureForSubscriptionVisitAsync(ClaimsPrincipal principal, Guid visitId, CancellationToken token)
    {
        var contractId = await (from visit in db.SubscriptionVisitSchedules.AsNoTracking()
                                join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id
                                where visit.PublicId == visitId select (Guid?)contract.PublicId).SingleOrDefaultAsync(token) ?? throw NotFound();
        return await EnsureForResourceAsync(principal, ChatResourceTypes.Subscription, contractId, "DIRECT", token);
    }

    public async Task<ChatRoomDetail> EnsureProviderConsultationAsync(ClaimsPrincipal principal, Guid providerId, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var customer = await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == identity.UserId, token)
            ?? throw new ChatBusinessException("CUSTOMER_PROFILE_REQUIRED", "고객 계정으로 로그인해 주세요.", 403);
        var provider = await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == providerId &&
            x.ApprovalStatusCode == "APPROVED" && x.ActivityStatusCode == "ACTIVE", token) ?? throw NotFound();
        if (await db.UserRelationshipBlocks.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customer.Id &&
            x.ProviderProfileId == provider.Id && x.StatusCode == "ACTIVE", token))
            throw Conflict("USER_RELATIONSHIP_BLOCKED", "차단된 전문가에게는 채팅 상담을 신청할 수 없습니다.");
        var resourceId = ConsultationResourceId(customer.PublicId, provider.PublicId);
        var existed = await db.ChatRooms.AsNoTracking().AnyAsync(x => x.ResourceTypeCode == ChatResourceTypes.ProviderConsultation &&
            x.ResourcePublicId == resourceId && x.RoomTypeCode == "DIRECT", token);
        var now = DateTime.UtcNow;
        var authorization = new ChatResourceAuthorization(ChatResourceTypes.ProviderConsultation, resourceId, "DIRECT",
            customer.UserId, provider.UserId, $"{provider.BusinessName} 상담", $"CS-{resourceId.ToString("N")[..10].ToUpperInvariant()}", now, now, true);
        var room = await EnsureResourceRoomAsync(authorization, now, token);
        var eventType = "CUSTOMER_CONSULTATION_REQUESTED";
        var outboxKey = $"provider-consultation-requested:{resourceId:N}";
        if (!existed && await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token) &&
            !await db.OutboxEvents.AnyAsync(x => x.IdempotencyKey == outboxKey, token))
        {
            db.OutboxEvents.Add(new OutboxEvent
            {
                AggregateType = "ChatRoom", AggregatePublicId = room.PublicId, EventType = eventType,
                PayloadJson = JsonSerializer.Serialize(new { recipientUserId = provider.UserId, customer_name = customer.DisplayName,
                    provider_name = provider.BusinessName, service_name = "광고 상담", roomId = room.PublicId,
                    targetRoute = $"/provider/messages/{room.PublicId}" }),
                StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = outboxKey, CreatedByUserId = identity.UserId,
            });
            await db.SaveChangesAsync(token);
        }
        return await Detail(room.PublicId, principal, token);
    }

    public async Task<IReadOnlyList<ChatRoomListItem>> Mine(ClaimsPrincipal principal, string? resourceType, int page, int pageSize, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var normalizedType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType.Trim().ToUpperInvariant();
        if (normalizedType is not null && normalizedType is not (ChatResourceTypes.Transaction or ChatResourceTypes.Subscription or ChatResourceTypes.Interior or ChatResourceTypes.AfterService or ChatResourceTypes.ProviderConsultation))
            throw Bad("CHAT_RESOURCE_TYPE_INVALID", "지원하지 않는 채팅 업무 유형입니다.");
        var safePage = Math.Max(1, page); var safeSize = Math.Clamp(pageSize, 1, 200);
        var rows = (await AuthorizedRooms(identity.UserId, null, token, normalizedType, Math.Min(1000, safePage * safeSize)))
            .Skip((safePage - 1) * safeSize).Take(safeSize).ToList();
        var result = new List<ChatRoomListItem>(rows.Count);
        foreach (var row in rows)
        {
            var last = await db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == row.Room.Id && x.CreatedAt >= row.Participant.AccessStartedAt)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync(token);
            var unread = await UnreadInRoom(row.Room.Id, identity.UserId, row.Participant.AccessStartedAt, token);
            result.Add(new(row.Room.PublicId, row.Authorization.ResourceTypeCode, row.Authorization.ResourceId,
                row.Authorization.ResourceTypeCode == ChatResourceTypes.Transaction ? row.Authorization.ResourceId : null,
                row.Authorization.RoomTypeCode, row.Room.StatusCode, row.CounterpartyRole, row.CounterpartyName,
                row.Authorization.ServiceName, row.Authorization.ResourceNumber,
                Preview(last), last?.CreatedAt, unread));
        }
        return result.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ToList();
    }

    public Task<IReadOnlyList<ChatRoomListItem>> Mine(ClaimsPrincipal principal, CancellationToken token) =>
        Mine(principal, null, 1, 200, token);

    public async Task<ChatRoomDetail> Detail(Guid roomId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var row = (await AuthorizedRooms(identity.UserId, roomId, token)).SingleOrDefault() ?? throw NotFound();
        return new(row.Room.PublicId, row.Authorization.ResourceTypeCode, row.Authorization.ResourceId,
            row.Authorization.ResourceTypeCode == ChatResourceTypes.Transaction ? row.Authorization.ResourceId : null,
            row.Authorization.RoomTypeCode, row.Room.StatusCode, row.CounterpartyRole,
            row.CounterpartyName, row.Authorization.ServiceName, row.Authorization.ResourceNumber,
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
        var query = db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id && x.CreatedAt >= access.Participant.AccessStartedAt);
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
        if (body.Length is < 1 or > 4000) throw Bad("CHAT_MESSAGE_BODY_INVALID", "채팅 내용은 1자 이상 4,000자 이하로 입력해 주세요.");
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
            ContentType = validated.ContentType, SizeBytes = validated.Bytes.LongLength, Sha256Hex = validated.Sha256,
            StatusCode = "PENDING", MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
            PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated, SanitizationStatusCode = FilePrivacyCodes.NotIntegrated,
            UploadedByUserId = identity.UserId, CreatedAt = now,
        };
        db.Files.Add(stored);
        await db.SaveChangesAsync(token);
        try
        {
            await using var stream = new MemoryStream(validated.Bytes, writable: false);
            await storage.SaveAsync(storageKey, stream, token);
            stored.StatusCode = "ACTIVE";
            stored.ActivatedAt = now;
            stored.ScanResultText = "Malware and privacy inspection are not integrated. Counterparty access remains fail closed.";
            var message = await PersistMessage(access, "FILE", null, idempotencyKey, stored, identity.UserId, token);
            await Broadcast(access.Room.PublicId, message, token);
            return message;
        }
        catch (Exception exception)
        {
            await storage.DeleteIfExistsAsync(storageKey, token);
            try
            {
                db.Files.Remove(stored);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogWarning(cleanupException, "Failed to remove incomplete chat attachment {FileId}.", stored.PublicId);
            }
            logger.LogError(exception, "Chat attachment storage failed for room {RoomId} and file {FileName}.", roomId, validated.Name);
            throw new ChatBusinessException("CHAT_FILE_STORAGE_FAILED", "첨부파일을 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.", 503);
        }
    }

    public async Task<ChatMessageResponse> SendFileData(Guid roomId, SendChatFileInput input, ClaimsPrincipal principal, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(input.Base64Data) || input.Base64Data.Length > 14 * 1024 * 1024)
            throw Bad("CHAT_FILE_SIZE_INVALID", "10MB 이하의 파일을 선택해 주세요.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(input.Base64Data); }
        catch (FormatException) { throw Bad("CHAT_FILE_ENCODING_INVALID", "첨부파일 전송 형식을 확인해 주세요."); }
        if (bytes.LongLength is <= 0 or > FileMaximum)
            throw Bad("CHAT_FILE_SIZE_INVALID", "10MB 이하의 파일을 선택해 주세요.");
        await using var source = new MemoryStream(bytes, writable: false);
        var upload = new FormFile(source, 0, bytes.LongLength, "file", input.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = input.ContentType,
        };
        return await SendFile(roomId, upload, input.IdempotencyKey, principal, token);
    }

    public async Task<ChatFileChunkResponse> SendFileChunk(Guid roomId, SendChatFileChunkInput input,
        ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        EnsureWritable(access.Room);
        ValidateKey(input.IdempotencyKey);
        if (input.UploadId == Guid.Empty || input.TotalChunks is < 1 or > 320 ||
            input.ChunkIndex < 0 || input.ChunkIndex >= input.TotalChunks ||
            string.IsNullOrWhiteSpace(input.Base64Chunk) || input.Base64Chunk.Length > 45_000)
            throw Bad("CHAT_FILE_CHUNK_INVALID", "첨부파일 조각 정보를 확인해 주세요.");

        byte[] chunk;
        try { chunk = Convert.FromBase64String(input.Base64Chunk); }
        catch (FormatException) { throw Bad("CHAT_FILE_CHUNK_INVALID", "첨부파일 조각을 읽지 못했습니다."); }
        if (chunk.Length is < 1 or > 32_768)
            throw Bad("CHAT_FILE_CHUNK_INVALID", "첨부파일 조각의 크기가 올바르지 않습니다.");

        var chunkParent = $"chat-upload/{identity.UserId}/{access.Room.PublicId:N}";
        var chunkPrefix = $"{chunkParent}/chunks-{input.UploadId:N}";
        try { await storage.PrepareBoundedUploadDirectoryAsync(chunkParent, chunkPrefix, token); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ChatBusinessException("CHAT_FILE_UPLOAD_LIMIT", "처리 중인 첨부파일이 많습니다. 잠시 후 다시 시도해 주세요.", 429);
        }

        var chunkKey = $"{chunkPrefix}/{input.ChunkIndex:D3}.part";
        try
        {
            await using var content = new MemoryStream(chunk, writable: false);
            await storage.SaveAsync(chunkKey, content, token);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try
            {
                await using var existing = await storage.OpenReadAsync(chunkKey, token);
                using var memory = new MemoryStream();
                await existing.CopyToAsync(memory, token);
                if (!memory.ToArray().AsSpan().SequenceEqual(chunk))
                    throw Conflict("CHAT_FILE_CHUNK_CONFLICT", "같은 첨부파일의 전송 정보가 변경되었습니다. 파일을 다시 선택해 주세요.");
            }
            catch (ChatBusinessException) { throw; }
            catch (Exception readException) when (readException is IOException or UnauthorizedAccessException)
            {
                throw new ChatBusinessException("CHAT_FILE_CHUNK_UNAVAILABLE", "첨부파일 조각을 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.", 503);
            }
        }

        if (input.ChunkIndex != input.TotalChunks - 1) return new(false, null);

        using var assembled = new MemoryStream();
        try
        {
            for (var index = 0; index < input.TotalChunks; index++)
            {
                await using var part = await storage.OpenReadAsync($"{chunkPrefix}/{index:D3}.part", token);
                await part.CopyToAsync(assembled, token);
                if (assembled.Length > FileMaximum)
                    throw Bad("CHAT_FILE_SIZE_INVALID", "10MB 이하의 파일을 선택해 주세요.");
            }
        }
        catch (ChatBusinessException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Conflict("CHAT_FILE_CHUNK_INCOMPLETE", "첨부파일 전송이 완료되지 않았습니다. 다시 보내 주세요.");
        }

        await using var source = new MemoryStream(assembled.ToArray(), writable: false);
        var upload = new FormFile(source, 0, source.Length, "file", input.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = input.ContentType,
        };
        var message = await SendFile(roomId, upload, input.IdempotencyKey, principal, token);
        try { await storage.DeleteDirectoryIfExistsAsync(chunkPrefix, CancellationToken.None); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return new(true, message);
    }

    public async Task MarkRead(Guid roomId, MarkChatReadInput input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        var target = await db.ChatMessages.AsNoTracking().SingleOrDefaultAsync(x => x.ChatRoomId == access.Room.Id && x.PublicId == input.MessageId && x.CreatedAt >= access.Participant.AccessStartedAt, token)
            ?? throw NotFound();
        var existing = await db.ChatMessageReads.Where(x => x.ChatRoomId == access.Room.Id && x.UserId == identity.UserId && x.ChatMessageId <= target.Id)
            .Select(x => x.ChatMessageId).ToListAsync(token);
        var existingIds = existing.ToHashSet();
        var ids = await db.ChatMessages.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id && x.Id <= target.Id && x.CreatedAt >= access.Participant.AccessStartedAt &&
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
        var rooms = await AuthorizedRooms(identity.UserId, null, token, null, 1000);
        var count = 0;
        foreach (var room in rooms) count += await UnreadInRoom(room.Room.Id, identity.UserId, room.Participant.AccessStartedAt, token);
        return new(count);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenAttachment(Guid roomId, Guid attachmentId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var access = await Access(roomId, identity.UserId, token);
        var row = await (from attachment in db.ChatAttachments.AsNoTracking()
                         join message in db.ChatMessages.AsNoTracking() on attachment.ChatMessageId equals message.Id
                         join file in db.Files.AsNoTracking() on attachment.FileId equals file.Id
                         where attachment.PublicId == attachmentId && message.ChatRoomId == access.Room.Id && message.CreatedAt >= access.Participant.AccessStartedAt
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

    private async Task<List<AuthorizedRoomRow>> AuthorizedRooms(long userId, Guid? roomId, CancellationToken token, string? resourceType = null, int limit = 200)
    {
        var now = DateTime.UtcNow;
        var candidates = await (from room in db.ChatRooms.AsNoTracking()
                                join participant in db.ChatParticipants.AsNoTracking() on room.Id equals participant.ChatRoomId
                                where (!roomId.HasValue || room.PublicId == roomId.Value) && (resourceType == null || room.ResourceTypeCode == resourceType) && participant.UserId == userId &&
                                      participant.StatusCode == "ACTIVE" && (room.ResourceTypeCode == ChatResourceTypes.ProviderConsultation || participant.AccessStartedAt <= now) &&
                                      (participant.AccessEndedAt == null || participant.AccessEndedAt > now)
                                orderby room.UpdatedAt descending
                                select new { Room = room, Participant = participant }).Take(limit).ToListAsync(token);
        var rows = new List<AuthorizedRoomRow>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var authorization = await authorizationResolver.ResolveAsync(candidate.Room.ResourceTypeCode, candidate.Room.ResourcePublicId, candidate.Room.RoomTypeCode, token);
            if (authorization is null || userId != authorization.CustomerUserId && userId != authorization.ProviderUserId) continue;
            var counterpartyRole = userId == authorization.CustomerUserId ? "PROVIDER" : "CUSTOMER";
            var counterpartyUserId = userId == authorization.CustomerUserId ? authorization.ProviderUserId : authorization.CustomerUserId;
            var counterpartyName = counterpartyRole == "PROVIDER"
                ? await db.ProviderProfiles.AsNoTracking().Where(x => x.UserId == counterpartyUserId).Select(x => x.BusinessName).SingleAsync(token)
                : await db.CustomerProfiles.AsNoTracking().Where(x => x.UserId == counterpartyUserId).Select(x => x.DisplayName).SingleAsync(token);
            rows.Add(new(candidate.Room, candidate.Participant, authorization, counterpartyRole, counterpartyName));
        }
        return rows;
    }

    private async Task<RoomAccess> Access(Guid roomId, long userId, CancellationToken token)
    {
        var row = (await AuthorizedRooms(userId, roomId, token)).SingleOrDefault() ?? throw NotFound();
        var room = await db.ChatRooms.SingleAsync(x => x.Id == row.Room.Id, token);
        var participant = await db.ChatParticipants.SingleAsync(x => x.Id == row.Participant.Id, token);
        await SynchronizeParticipants(room, row.Authorization, DateTime.UtcNow, token);
        return new(room, participant);
    }

    private async Task<ChatResourceAuthorization> AuthorizedResource(string type, Guid id, string roomType, long userId, CancellationToken token)
    {
        var authorization = await authorizationResolver.ResolveAsync(type, id, roomType, token) ?? throw NotFound();
        if (userId != authorization.CustomerUserId && userId != authorization.ProviderUserId) throw NotFound();
        return authorization;
    }

    private async Task SynchronizeParticipants(ChatRoom room, ChatResourceAuthorization authorization, DateTime now, CancellationToken token)
    {
        var desired = new Dictionary<long, (string Role, DateTime StartedAt)>
        {
            [authorization.CustomerUserId] = ("CUSTOMER", authorization.CustomerAccessStartedAt),
            [authorization.ProviderUserId] = ("PROVIDER", authorization.ProviderAccessStartedAt),
        };
        var existing = await db.ChatParticipants.Where(x => x.ChatRoomId == room.Id).ToListAsync(token);
        foreach (var participant in existing)
        {
            if (!desired.TryGetValue(participant.UserId, out var value))
            {
                if (participant.StatusCode == "ACTIVE") { participant.StatusCode = "ENDED"; participant.AccessEndedAt = now; }
                continue;
            }
            desired.Remove(participant.UserId);
            if (participant.StatusCode != "ACTIVE")
            {
                participant.StatusCode = "ACTIVE";
                participant.ParticipantRoleCode = value.Role;
                participant.AccessStartedAt = now > value.StartedAt ? now : value.StartedAt;
                participant.AccessEndedAt = null;
                participant.JoinedAt = now;
            }
            else if (authorization.ResourceTypeCode == ChatResourceTypes.Subscription && participant.ParticipantRoleCode == "PROVIDER" &&
                     value.StartedAt > participant.AccessStartedAt)
            {
                participant.AccessStartedAt = value.StartedAt;
                participant.JoinedAt = value.StartedAt;
            }
        }
        foreach (var entry in desired)
            db.ChatParticipants.Add(Participant(room.Id, entry.Key, entry.Value.Role, entry.Value.StartedAt > now ? now : entry.Value.StartedAt));
        await db.SaveChangesAsync(token);
    }

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
        access.Room.UpdatedAt = now;
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
            var recipient = await db.ChatParticipants.AsNoTracking().Where(x => x.ChatRoomId == access.Room.Id && x.UserId != userId && x.StatusCode == "ACTIVE")
                .Select(x => new { x.UserId, x.ParticipantRoleCode }).SingleOrDefaultAsync(token);
            if (recipient is not null)
            {
                db.OutboxEvents.Add(new OutboxEvent
                {
                    AggregateType = "ChatRoom", AggregatePublicId = access.Room.PublicId, EventType = "CHAT.MESSAGE.CREATED",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        recipientUserId = recipient.UserId, roomId = access.Room.PublicId, messageId = message.PublicId, messageType = type,
                        resourceType = access.Room.ResourceTypeCode, resourceId = access.Room.ResourcePublicId,
                        targetRoute = recipient.ParticipantRoleCode == "CUSTOMER"
                            ? $"/customer/messages/{access.Room.PublicId}" : $"/provider/messages/{access.Room.PublicId}",
                    }),
                    StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"chat-message-created:{message.PublicId:N}",
                    CreatedByUserId = userId,
                });
            }
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

    private async Task<int> UnreadInRoom(long roomId, long userId, DateTime accessStartedAt, CancellationToken token) =>
        await db.ChatMessages.AsNoTracking().CountAsync(message => message.ChatRoomId == roomId && message.CreatedAt >= accessStartedAt &&
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
        if (room.StatusCode != "ACTIVE") throw Conflict("CHAT_ROOM_NOT_WRITABLE", "현재 채팅방에서는 새 채팅을 보낼 수 없습니다.");
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120) throw Bad("IDEMPOTENCY_KEY_INVALID", "안전한 요청 식별자가 필요합니다.");
    }

    private static async Task<ValidatedFile> ValidateFile(IFormFile file, CancellationToken token)
    {
        try
        {
            var safe = await SafeImageUploadPolicy.ProcessAsync(file, token);
            return new(safe.FileName, safe.ContentType, safe.Extension, safe.Sha256Hex, safe.Bytes);
        }
        catch (SafeImageUploadException error) { throw Bad(error.Code, error.Message); }
    }

    private static string SafeName(StoredFile file) => Path.GetFileName(file.OriginalFileName);
    private static string? Preview(ChatMessage? message) => message is null ? null : message.MessageTypeCode == "FILE" ? "파일을 보냈습니다." : message.Body?.Length > 80 ? message.Body[..80] : message.Body;
    private static Guid ConsultationResourceId(Guid customerId, Guid providerId)
    {
        var value = Encoding.UTF8.GetBytes($"soodal-provider-consultation:{customerId:N}:{providerId:N}");
        var hash = SHA256.HashData(value);
        return new Guid(hash.AsSpan(0, 16));
    }
    public static string Group(Guid roomId) => $"chat-room:{roomId:N}";
    private static ChatBusinessException NotFound() => new("CHAT_ROOM_NOT_FOUND", "채팅방을 찾을 수 없습니다.", 404);
    private static ChatBusinessException Bad(string code, string message) => new(code, message, 400);
    private static ChatBusinessException Conflict(string code, string message) => new(code, message, 409);

    private sealed record UserIdentity(long UserId);
    private sealed record RoomAccess(ChatRoom Room, ChatParticipant Participant);
    private sealed record ValidatedFile(string Name, string ContentType, string Extension, string Sha256, byte[] Bytes);
    private sealed record AuthorizedRoomRow(ChatRoom Room, ChatParticipant Participant, ChatResourceAuthorization Authorization,
        string CounterpartyRole, string CounterpartyName);
}
