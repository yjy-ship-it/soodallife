using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Work;

public sealed class TransactionDirectPaymentService(
    SoodalLifeDbContext db,
    IPrivateFileStorage fileStorage,
    ICrossDomainFilePublicationResolver publication)
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMethods = ["BANK_TRANSFER", "ON_SITE_CARD", "CASH", "OTHER"];
    private static readonly HashSet<string> AllowedStates = ["IN_PROGRESS", "COMPLETION_SUBMITTED", "REVISION_REQUESTED", "COMPLETED"];
    private static readonly IReadOnlyDictionary<string, (string Extension, byte[][] Signatures)> AllowedImages =
        new Dictionary<string, (string, byte[][])>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", [[0xFF, 0xD8, 0xFF]]),
            ["image/png"] = (".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ["image/webp"] = (".webp", [Encoding.ASCII.GetBytes("RIFF")]),
        };

    public async Task<DirectPaymentContextResponse> GetAsync(ClaimsPrincipal principal, Guid transactionId, CancellationToken token)
    {
        var access = await AccessAsync(principal, transactionId, tracking: false, token);
        var payment = await db.TransactionDirectPayments.AsNoTracking().SingleOrDefaultAsync(x => x.TransactionId == access.Transaction.Id, token);
        return new(access.Transaction.AgreedAmount, access.Transaction.CurrencyCode, access.Transaction.StatusCode,
            Convert.ToBase64String(access.Transaction.RowVersion), payment is null ? null : await ResponseAsync(payment, access.UserId, token));
    }

    public async Task<TransactionDirectPaymentResponse> RegisterAsync(ClaimsPrincipal principal, Guid transactionId, RegisterDirectPaymentInput input, CancellationToken token)
    {
        ValidateRegistration(input);
        var access = await AccessAsync(principal, transactionId, tracking: true, token);
        if (!AllowedStates.Contains(access.Transaction.StatusCode))
            throw Conflict("DIRECT_PAYMENT_STATE_BLOCKED", "작업 시작 이후의 진행 중 또는 완료 거래에서만 지급 사실을 등록할 수 있습니다.");
        if (input.Amount != access.Transaction.AgreedAmount)
            throw Invalid("DIRECT_PAYMENT_AMOUNT_MISMATCH", "지급 확인 금액은 채택 견적의 합의금액과 같아야 합니다.", "amount");
        ApplyTransactionConcurrency(access.Transaction, input.TransactionRowVersion);

        var existingByKey = await db.TransactionDirectPayments.AsNoTracking().SingleOrDefaultAsync(x => x.RegistrationIdempotencyKey == input.IdempotencyKey, token);
        if (existingByKey is not null)
        {
            if (existingByKey.TransactionId != access.Transaction.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 거래에 사용된 요청 키입니다.");
            return await ResponseAsync(existingByKey, access.UserId, token);
        }
        if (await db.TransactionDirectPayments.AnyAsync(x => x.TransactionId == access.Transaction.Id, token))
            throw Conflict("DIRECT_PAYMENT_ALREADY_REGISTERED", "이 거래의 지급 사실이 이미 등록되어 있습니다.");

        var now = DateTime.UtcNow;
        StoredFile? stored = null;
        string? storageKey = null;
        if (input.Evidence is not null)
        {
            var validated = await ValidateImageAsync(input.Evidence, token);
            storageKey = $"direct-payment/{access.Transaction.PublicId:N}/{Guid.NewGuid():N}{validated.Extension}";
            stored = new StoredFile
            {
                PurposeCode = "DIRECT_PAYMENT_EVIDENCE", StorageContainer = "development-private", StorageKey = storageKey,
                StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)), OriginalFileName = validated.Name,
                ContentType = validated.ContentType, SizeBytes = input.Evidence.Length, Sha256Hex = validated.Hash,
                StatusCode = "PENDING", MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
                PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated, SanitizationStatusCode = FilePrivacyCodes.NotIntegrated,
                UploadedByUserId = access.UserId, CreatedAt = now,
            };
            db.Files.Add(stored);
            await db.SaveChangesAsync(token);
            try
            {
                await using var stream = input.Evidence.OpenReadStream();
                await fileStorage.SaveAsync(storageKey, stream, token);
                stored.StatusCode = "ACTIVE"; stored.ActivatedAt = now; stored.ScanResultText = FilePrivacyCodes.NotIntegrated;
            }
            catch { await fileStorage.DeleteIfExistsAsync(storageKey, token); throw; }
        }

        var payment = new TransactionDirectPayment
        {
            TransactionId = access.Transaction.Id, RegisteredByUserId = access.UserId, RegisteredByRoleCode = access.Role,
            Amount = input.Amount, CurrencyCode = access.Transaction.CurrencyCode, PaymentMethodCode = input.PaymentMethod.Trim().ToUpperInvariant(),
            PaidAt = input.PaidAt, NoteText = NullIfEmpty(input.Note), EvidenceFileId = stored?.Id, RegisteredAt = now,
            RegistrationIdempotencyKey = input.IdempotencyKey.Trim(), CreatedAt = now, CreatedByUserId = access.UserId,
            UpdatedAt = now, UpdatedByUserId = access.UserId,
        };
        db.TransactionDirectPayments.Add(payment);
        AddAudit(access, payment, "DIRECT_PAYMENT_REGISTERED", now, null);
        await AddOutboxIfTemplateAsync(access.Transaction, payment, "DIRECT_PAYMENT_REGISTERED", access.UserId, now, token);
        try { await db.SaveChangesAsync(token); }
        catch { if (storageKey is not null) await fileStorage.DeleteIfExistsAsync(storageKey, token); throw; }
        return await ResponseAsync(payment, access.UserId, token);
    }

    public async Task<TransactionDirectPaymentResponse> DecideAsync(ClaimsPrincipal principal, Guid transactionId, Guid paymentId, DecideDirectPaymentInput input, CancellationToken token)
    {
        ValidateDecision(input);
        var access = await AccessAsync(principal, transactionId, tracking: true, token);
        if (!AllowedStates.Contains(access.Transaction.StatusCode)) throw Conflict("DIRECT_PAYMENT_STATE_BLOCKED", "현재 거래 상태에서는 지급 확인 결정을 변경할 수 없습니다.");
        var payment = await db.TransactionDirectPayments.SingleOrDefaultAsync(x => x.PublicId == paymentId && x.TransactionId == access.Transaction.Id, token)
            ?? throw NotFound("DIRECT_PAYMENT_NOT_FOUND");
        var existingByKey = await db.TransactionDirectPayments.AsNoTracking().SingleOrDefaultAsync(x => x.DecisionIdempotencyKey == input.IdempotencyKey, token);
        if (existingByKey is not null)
        {
            if (existingByKey.Id != payment.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 지급 확인에 사용된 요청 키입니다.");
            return await ResponseAsync(existingByKey, access.UserId, token);
        }
        if (payment.RegisteredByUserId == access.UserId) throw new WorkBusinessException("SELF_CONFIRMATION_BLOCKED", "지급 사실 등록자는 직접 확인할 수 없습니다.", 403);
        if (payment.StatusCode != "REGISTERED") throw Conflict("DIRECT_PAYMENT_ALREADY_DECIDED", "이미 처리된 지급 확인입니다.");
        ApplyPaymentConcurrency(payment, input.RowVersion);
        var decision = input.Decision.Trim().ToUpperInvariant();
        var before = payment.StatusCode;
        var now = DateTime.UtcNow;
        payment.StatusCode = decision == "CONFIRM" ? "COUNTERPART_CONFIRMED" : "REJECTED";
        payment.DecidedByUserId = access.UserId; payment.DecidedAt = now;
        payment.RejectionReason = decision == "REJECT" ? input.Reason!.Trim() : null;
        payment.DecisionIdempotencyKey = input.IdempotencyKey.Trim(); payment.UpdatedAt = now; payment.UpdatedByUserId = access.UserId;
        AddAudit(access, payment, payment.StatusCode == "COUNTERPART_CONFIRMED" ? "DIRECT_PAYMENT_CONFIRMED" : "DIRECT_PAYMENT_REJECTED", now, before);
        await AddOutboxIfTemplateAsync(access.Transaction, payment, payment.StatusCode == "COUNTERPART_CONFIRMED" ? "DIRECT_PAYMENT_CONFIRMED" : "DIRECT_PAYMENT_REJECTED", access.UserId, now, token);
        await db.SaveChangesAsync(token);
        if (payment.StatusCode == "COUNTERPART_CONFIRMED") await AddHistorySnapshotAsync(access.Transaction, payment, token);
        return await ResponseAsync(payment, access.UserId, token);
    }

    public async Task<DirectPaymentFileResult> OpenEvidenceAsync(ClaimsPrincipal principal, Guid transactionId, Guid fileId, CancellationToken token)
    {
        var access = await AccessAsync(principal, transactionId, tracking: false, token);
        var row = await (from payment in db.TransactionDirectPayments.AsNoTracking()
                         join file in db.Files.AsNoTracking() on payment.EvidenceFileId equals file.Id
                         where payment.TransactionId == access.Transaction.Id && file.PublicId == fileId && file.StatusCode == "ACTIVE"
                         select file).SingleOrDefaultAsync(token) ?? throw NotFound("FILE_NOT_FOUND");
        var result = await publication.ResolveAsync(row, access.UserId, true, true, token);
        if (!result.Allowed) throw new WorkBusinessException("FILE_PRIVACY_BLOCKED", result.Message, 403);
        var published = result.PublishedFile!;
        return new(await fileStorage.OpenReadAsync(published.StorageKey, token), published.ContentType, published.OriginalFileName);
    }

    private async Task<TransactionDirectPaymentResponse> ResponseAsync(TransactionDirectPayment payment, long viewerUserId, CancellationToken token)
    {
        var transactionPublicId = await db.Transactions.AsNoTracking().Where(x => x.Id == payment.TransactionId).Select(x => x.PublicId).SingleAsync(token);
        DirectPaymentEvidenceResponse? evidence = null;
        if (payment.EvidenceFileId.HasValue)
        {
            var file = await db.Files.AsNoTracking().SingleAsync(x => x.Id == payment.EvidenceFileId, token);
            var result = await publication.ResolveAsync(file, viewerUserId, true, true, token);
            var published = result.PublishedFile ?? file;
            evidence = new(result.Allowed ? file.PublicId : null, result.Allowed ? published.OriginalFileName : "evidence",
                published.ContentType, published.SizeBytes, result.Allowed ? $"/api/v1/transactions/{transactionPublicId}/direct-payments/files/{file.PublicId}" : null,
                result.StatusCode, result.Allowed ? null : result.Message);
        }
        return new(payment.PublicId, payment.StatusCode, payment.Amount, payment.CurrencyCode, payment.PaymentMethodCode,
            payment.PaidAt, payment.NoteText, payment.RegisteredByRoleCode, payment.RegisteredAt, payment.DecidedAt,
            payment.RejectionReason, payment.StatusCode == "REGISTERED" && payment.RegisteredByUserId != viewerUserId,
            Convert.ToBase64String(payment.RowVersion), evidence);
    }

    private async Task<Access> AccessAsync(ClaimsPrincipal principal, Guid transactionId, bool tracking, CancellationToken token)
    {
        var publicId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : Guid.Empty;
        if (!principal.IsInRole(RoleCodes.Customer) && !principal.IsInRole(RoleCodes.Provider))
            throw new WorkBusinessException("DIRECT_PAYMENT_ACCESS_DENIED", "거래 당사자만 접근할 수 있습니다.", 403);
        var query = tracking ? db.Transactions.AsQueryable() : db.Transactions.AsNoTracking();
        var row = await (from transaction in query
                         join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                         join customerUser in db.Users.AsNoTracking() on customer.UserId equals customerUser.Id
                         join providerUser in db.Users.AsNoTracking() on provider.UserId equals providerUser.Id
                         where transaction.PublicId == transactionId
                         select new { transaction, customerUser, providerUser }).SingleOrDefaultAsync(token);
        if (row is null) throw NotFound("TRANSACTION_NOT_FOUND");
        if (principal.IsInRole(RoleCodes.Customer) && row.customerUser.PublicId == publicId) return new(row.transaction, row.customerUser.Id, RoleCodes.Customer);
        if (principal.IsInRole(RoleCodes.Provider) && row.providerUser.PublicId == publicId) return new(row.transaction, row.providerUser.Id, RoleCodes.Provider);
        throw NotFound("TRANSACTION_NOT_FOUND");
    }

    private async Task AddHistorySnapshotAsync(TransactionRecord transaction, TransactionDirectPayment payment, CancellationToken token)
    {
        var history = await db.ServiceHistoryEntries.SingleOrDefaultAsync(x => x.TransactionId == transaction.Id && x.EventTypeCode == "COMPLETION", token);
        if (history is null) return;
        var root = JsonNode.Parse(history.SnapshotJson)?.AsObject() ?? [];
        root["directPayment"] = JsonSerializer.SerializeToNode(new { paymentId = payment.PublicId, payment.StatusCode, payment.Amount, payment.CurrencyCode, payment.PaymentMethodCode, payment.PaidAt, payment.DecidedAt });
        history.SnapshotJson = root.ToJsonString();
        await db.SaveChangesAsync(token);
    }

    private void AddAudit(Access access, TransactionDirectPayment payment, string action, DateTime now, string? before) =>
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = access.UserId, ActorRoleCode = access.Role,
            ActionCode = action, EntityType = "TRANSACTION_DIRECT_PAYMENT", EntityPublicId = payment.PublicId, ResultCode = "SUCCESS",
            BeforeJson = before is null ? null : JsonSerializer.Serialize(new { status = before }),
            AfterJson = JsonSerializer.Serialize(new { payment.StatusCode, payment.Amount, payment.CurrencyCode, payment.PaymentMethodCode }) });

    private async Task AddOutboxIfTemplateAsync(TransactionRecord transaction, TransactionDirectPayment payment, string type, long userId, DateTime now, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == type && x.IsActive, token)) return;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = "Transaction", AggregatePublicId = transaction.PublicId, EventType = type,
            PayloadJson = JsonSerializer.Serialize(new { transactionId = transaction.PublicId, directPaymentId = payment.PublicId, status = payment.StatusCode }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"{type.ToLowerInvariant()}:{payment.PublicId:N}", CreatedByUserId = userId });
    }

    private static void ValidateRegistration(RegisterDirectPaymentInput input)
    {
        if (input.Amount <= 0) throw Invalid("DIRECT_PAYMENT_AMOUNT_INVALID", "지급 금액을 확인해 주세요.", "amount");
        if (!AllowedMethods.Contains(input.PaymentMethod.Trim().ToUpperInvariant())) throw Invalid("DIRECT_PAYMENT_METHOD_INVALID", "지급수단을 확인해 주세요.", "paymentMethod");
        if (input.PaidAt == default || input.PaidAt > DateTime.UtcNow.AddMinutes(5)) throw Invalid("DIRECT_PAYMENT_DATE_INVALID", "실제 지급 일시를 확인해 주세요.", "paidAt");
        if (input.Note?.Length > 1000) throw Invalid("DIRECT_PAYMENT_NOTE_TOO_LONG", "메모는 1000자 이하여야 합니다.", "note");
        ValidateKey(input.IdempotencyKey);
    }

    private static void ValidateDecision(DecideDirectPaymentInput input)
    {
        var decision = input.Decision.Trim().ToUpperInvariant();
        if (decision is not ("CONFIRM" or "REJECT")) throw Invalid("DIRECT_PAYMENT_DECISION_INVALID", "확인 또는 거절을 선택해 주세요.", "decision");
        if (decision == "REJECT" && string.IsNullOrWhiteSpace(input.Reason)) throw Invalid("DIRECT_PAYMENT_REJECTION_REASON_REQUIRED", "거절 사유를 입력해 주세요.", "reason");
        if (input.Reason?.Length > 1000) throw Invalid("DIRECT_PAYMENT_REASON_TOO_LONG", "사유는 1000자 이하여야 합니다.", "reason");
        ValidateKey(input.IdempotencyKey);
    }

    private static void ValidateKey(string key) { if (string.IsNullOrWhiteSpace(key) || key.Length > 100) throw Invalid("IDEMPOTENCY_KEY_INVALID", "유효한 요청 키가 필요합니다.", "idempotencyKey"); }
    private void ApplyTransactionConcurrency(TransactionRecord row, string? value) { var expected = DecodeRowVersion(value, row.RowVersion.Length == 0); if (!row.RowVersion.SequenceEqual(expected)) throw Conflict("ROW_VERSION_CONFLICT", "거래 정보가 변경되었습니다. 새로고침 후 다시 시도해 주세요."); db.Entry(row).Property(x => x.RowVersion).OriginalValue = expected; }
    private void ApplyPaymentConcurrency(TransactionDirectPayment row, string? value) { var expected = DecodeRowVersion(value, row.RowVersion.Length == 0); if (!row.RowVersion.SequenceEqual(expected)) throw Conflict("ROW_VERSION_CONFLICT", "지급 확인 정보가 변경되었습니다. 새로고침 후 다시 시도해 주세요."); db.Entry(row).Property(x => x.RowVersion).OriginalValue = expected; }
    private static byte[] DecodeRowVersion(string? value, bool allowEmpty) { if (allowEmpty && string.IsNullOrEmpty(value)) return []; try { return Convert.FromBase64String(value ?? string.Empty); } catch { throw Invalid("ROW_VERSION_INVALID", "화면을 새로고침한 뒤 다시 시도해 주세요.", "rowVersion"); } }

    private static async Task<(string ContentType, string Extension, string Hash, string Name)> ValidateImageAsync(IFormFile file, CancellationToken token)
    {
        if (file.Length <= 0 || file.Length > MaximumFileSize) throw Invalid("FILE_SIZE_INVALID", "이미지는 10MB 이하여야 합니다.", "evidence");
        if (!AllowedImages.TryGetValue(file.ContentType, out var rule)) throw Invalid("FILE_TYPE_INVALID", "JPEG, PNG 또는 WebP 이미지만 등록할 수 있습니다.", "evidence");
        var name = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(name) || name != file.FileName || name.Length > 255) throw Invalid("FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.", "evidence");
        await using var stream = file.OpenReadStream(); using var memory = new MemoryStream(); await stream.CopyToAsync(memory, token); var bytes = memory.ToArray();
        var valid = rule.Signatures.Any(signature => bytes.AsSpan().StartsWith(signature));
        if (file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase)) valid &= bytes.Length >= 12 && bytes.AsSpan(8, 4).SequenceEqual(Encoding.ASCII.GetBytes("WEBP"));
        if (!valid) throw Invalid("FILE_SIGNATURE_INVALID", "파일 내용과 이미지 형식이 일치하지 않습니다.", "evidence");
        return (file.ContentType.ToLowerInvariant(), rule.Extension, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), name);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException NotFound(string code) => new(code, "대상을 찾을 수 없습니다.", 404);
    private static WorkBusinessException Conflict(string code, string message) => new(code, message);
    private static WorkBusinessException Invalid(string code, string message, string field) => new(code, message, 400, new Dictionary<string, string[]> { [field] = [message] });
    private sealed record Access(TransactionRecord Transaction, long UserId, string Role);
}
