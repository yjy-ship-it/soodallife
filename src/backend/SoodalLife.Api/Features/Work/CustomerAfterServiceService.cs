using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Work;

public sealed class CustomerAfterServiceService(SoodalLifeDbContext db, IPrivateFileStorage storage)
{
    private const int MaximumFileSize = 10 * 1024 * 1024;

    public async Task<IReadOnlyList<ServiceHistoryListItem>> History(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var rows = await db.ServiceHistoryEntries.AsNoTracking()
            .Where(x => x.CustomerProfileId == identity.ProfileId && x.EventTypeCode == "COMPLETION" && x.TransactionId != null)
            .OrderByDescending(x => x.CompletedAtSnapshot).ThenByDescending(x => x.OccurredAt).ToListAsync(token);
        var result = new List<ServiceHistoryListItem>();
        foreach (var row in rows)
        {
            var transactionId = await db.Transactions.AsNoTracking().Where(x => x.Id == row.TransactionId).Select(x => x.PublicId).SingleAsync(token);
            var hasAs = await db.AfterServiceCases.AsNoTracking().AnyAsync(x => x.TransactionId == row.TransactionId, token);
            var hasReview = await db.Reviews.AsNoTracking().AnyAsync(x => x.TransactionId == row.TransactionId, token);
            var hasDispute = await db.DisputeCases.AsNoTracking().AnyAsync(x => x.TransactionId == row.TransactionId, token);
            result.Add(new(row.PublicId, transactionId, row.CategoryNameSnapshot ?? row.Title, row.CategoryNameSnapshot,
                row.ProviderNameSnapshot ?? "공급자", row.CompletedAtSnapshot, row.TotalAmountSnapshot, row.CurrencyCode,
                row.WarrantyEndDate, Warranty(row.WarrantyEndDate), hasAs, hasReview, hasDispute));
        }
        return result;
    }

    public async Task<ServiceHistoryDetail> HistoryDetail(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var row = await db.ServiceHistoryEntries.AsNoTracking().SingleOrDefaultAsync(
            x => x.PublicId == id && x.CustomerProfileId == identity.ProfileId && x.EventTypeCode == "COMPLETION", token) ?? throw NotFound("SERVICE_HISTORY_NOT_FOUND", "서비스 이력을 찾을 수 없습니다.");
        if (!row.TransactionId.HasValue) throw NotFound("SERVICE_HISTORY_NOT_FOUND", "서비스 이력을 찾을 수 없습니다.");
        var transaction = await db.Transactions.AsNoTracking().SingleAsync(x => x.Id == row.TransactionId.Value, token);
        var lines = await db.ServiceHistoryItems.AsNoTracking().Where(x => x.ServiceHistoryEntryId == row.Id).OrderBy(x => x.LineNo)
            .Select(x => new ServiceHistoryLine(x.LineNo, x.ItemName, x.Description, x.Quantity, x.UnitText, x.Amount, x.CurrencyCode)).ToListAsync(token);
        var evidence = row.SourceCompletionRevisionId.HasValue
            ? await (from link in db.CompletionEvidenceFiles.AsNoTracking()
                     join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                     join role in db.CompletionPhotoRoles.AsNoTracking() on link.PhotoRoleId equals role.Id
                     where link.CompletionRevisionId == row.SourceCompletionRevisionId && file.StatusCode == "ACTIVE"
                     orderby link.DisplayOrder
                     select new ServiceHistoryEvidence(file.PublicId, file.OriginalFileName, file.ContentType, role.Name, link.Description, "/api/v1/files/" + file.PublicId)).ToListAsync(token)
            : [];
        var assets = await (from link in db.TransactionAssetLinks.AsNoTracking()
                            join asset in db.ServiceAssets.AsNoTracking() on link.ServiceAssetId equals asset.Id
                            where link.TransactionId == transaction.Id && link.StatusCode == "ACTIVE" && asset.CustomerProfileId == identity.ProfileId
                            select new ServiceHistoryAsset(asset.PublicId, asset.AssetTypeCode, asset.Name, asset.Manufacturer, asset.ModelName)).ToListAsync(token);
        var after = await db.AfterServiceCases.AsNoTracking().Where(x => x.TransactionId == transaction.Id).OrderByDescending(x => x.ReceivedAt)
            .Select(x => new WorkRelatedCase(x.PublicId, x.StatusCode, AfterServiceDisplay(x.StatusCode), x.ReceivedAt)).FirstOrDefaultAsync(token);
        var dispute = await db.DisputeCases.AsNoTracking().Where(x => x.TransactionId == transaction.Id).OrderByDescending(x => x.ReceivedAt)
            .Select(x => new WorkRelatedCase(x.PublicId, x.StatusCode, DisputeDisplay(x.StatusCode), x.ReceivedAt)).FirstOrDefaultAsync(token);
        var review = await db.Reviews.AsNoTracking().Where(x => x.TransactionId == transaction.Id).Select(x => new { x.PublicId, x.VisibilityStatusCode }).SingleOrDefaultAsync(token);
        return new(row.PublicId, transaction.PublicId, row.Title, row.Summary, row.CategoryNameSnapshot ?? row.Title,
            row.CategoryNameSnapshot, row.ProviderNameSnapshot ?? "공급자", row.CompletedAtSnapshot, row.TotalAmountSnapshot,
            row.CurrencyCode, row.WarrantyStartDate, row.WarrantyEndDate, Warranty(row.WarrantyEndDate), lines, evidence, assets,
            after, dispute, new(review?.PublicId, false, review is not null, review?.VisibilityStatusCode));
    }

    public async Task<IReadOnlyList<CustomerAfterServiceResponse>> List(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var ids = await db.AfterServiceCases.AsNoTracking().Where(x => x.CustomerProfileId == identity.ProfileId)
            .OrderByDescending(x => x.ReceivedAt).Select(x => x.Id).ToListAsync(token);
        var result = new List<CustomerAfterServiceResponse>();
        foreach (var id in ids) result.Add(await Build(id, identity.UserId, identity.ProfileId, false, token));
        return result;
    }

    public async Task<CustomerAfterServiceResponse> Detail(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var access = await Access(principal, id, token);
        return await Build(access.CaseId, access.UserId, access.CustomerProfileId, access.IsAdmin, token);
    }

    public async Task<CustomerAfterServiceResponse> Create(ClaimsPrincipal principal, Guid transactionId, CreateAfterServiceInput input, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var subject = Required(input.Subject, 200); var description = Required(input.Description, 4000); var key = Required(input.IdempotencyKey, 150);
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.PublicId == transactionId && x.CustomerProfileId == identity.ProfileId, token) ?? throw NotFound("TRANSACTION_NOT_FOUND", "거래를 찾을 수 없습니다.");
        if (transaction.StatusCode != "COMPLETED") throw Invalid("AFTER_SERVICE_REQUIRES_COMPLETION", "완료된 거래만 A/S를 신청할 수 있습니다.", 409);
        var existing = await db.AfterServiceCases.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, token);
        if (existing is not null)
        {
            if (existing.CustomerProfileId != identity.ProfileId) throw Invalid("IDEMPOTENCY_KEY_CONFLICT", "이미 사용된 요청 키입니다.", 409);
            return await Build(existing.Id, identity.UserId, identity.ProfileId, false, token);
        }
        var history = await db.ServiceHistoryEntries.AsNoTracking().Where(x => x.TransactionId == transaction.Id && x.EventTypeCode == "COMPLETION").OrderByDescending(x => x.OccurredAt).FirstOrDefaultAsync(token);
        if (history is null) throw Invalid("SERVICE_HISTORY_REQUIRED", "완료 서비스 이력이 생성된 거래만 A/S를 신청할 수 있습니다.", 409);
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var now = DateTime.UtcNow;
        var item = new AfterServiceCase
        {
            TransactionId = transaction.Id, CustomerProfileId = identity.ProfileId, ProviderProfileId = transaction.ProviderProfileId,
            ReportedByUserId = identity.UserId, StatusCode = "RECEIVED", Subject = subject, Description = description,
            RequestDetails = Clean(input.RequestDetails), ReceivedAt = now, WarrantyStartDate = history.WarrantyStartDate,
            WarrantyEndDate = history.WarrantyEndDate, IsWithinWarranty = history.WarrantyEndDate.HasValue ? today <= history.WarrantyEndDate.Value : null,
            LastActionAt = now, IdempotencyKey = key, CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId,
        };
        db.AfterServiceCases.Add(item); await db.SaveChangesAsync(token);
        db.AfterServiceActions.Add(new AfterServiceAction { AfterServiceCaseId = item.Id, ToStatusCode = "RECEIVED", ActionTypeCode = "RECEIVED", ActionNote = "고객 A/S 접수", ScheduledAt = input.DesiredVisitAt?.ToUniversalTime(), OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = $"after-service-received:{item.PublicId:N}" });
        var providerUser = await db.ProviderProfiles.Where(x => x.Id == transaction.ProviderProfileId).Select(x => x.UserId).SingleAsync(token);
        AddOutbox("AfterService", item.PublicId, "AFTER_SERVICE_OPENED", providerUser, transaction.PublicId, item.PublicId, identity.UserId, now);
        await db.SaveChangesAsync(token);
        return await Build(item.Id, identity.UserId, identity.ProfileId, false, token);
    }

    public async Task<AfterServiceEvidenceResponse> Upload(ClaimsPrincipal principal, Guid id, string? role, string? description, IFormFile upload, CancellationToken token)
    {
        var access = await Access(principal, id, token);
        var validated = await Validate(upload, token); var now = DateTime.UtcNow;
        var storageKey = $"after-service/{id:N}/{Guid.NewGuid():N}{validated.Extension}";
        var file = NewFile("AFTER_SERVICE", storageKey, upload, validated.Bytes, access.UserId, now);
        db.Files.Add(file); await db.SaveChangesAsync(token);
        try
        {
            await using var stream = new MemoryStream(validated.Bytes); await storage.SaveAsync(storageKey, stream, token);
            file.StatusCode = "ACTIVE"; file.ActivatedAt = now; file.ScanResultText = "NOT_INTEGRATED";
            db.AfterServiceFiles.Add(new AfterServiceFile { AfterServiceCaseId = access.CaseId, FileId = file.Id, RoleCode = Clean(role), Description = Clean(description), CreatedAt = now, CreatedByUserId = access.UserId });
            await db.SaveChangesAsync(token);
            return new(file.PublicId, file.OriginalFileName, file.ContentType, file.SizeBytes, Clean(role), Clean(description), $"/api/v1/after-services/{id}/files/{file.PublicId}");
        }
        catch { await storage.DeleteIfExistsAsync(storageKey, token); throw; }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenFile(ClaimsPrincipal principal, Guid id, Guid fileId, CancellationToken token)
    {
        var access = await Access(principal, id, token);
        var file = await (from link in db.AfterServiceFiles.AsNoTracking() join f in db.Files.AsNoTracking() on link.FileId equals f.Id
                          where link.AfterServiceCaseId == access.CaseId && f.PublicId == fileId && f.StatusCode == "ACTIVE" select f).SingleOrDefaultAsync(token)
                   ?? throw NotFound("AFTER_SERVICE_FILE_NOT_FOUND", "A/S 증빙 파일을 찾을 수 없습니다.");
        return (await storage.OpenReadAsync(file.StorageKey, token), file.ContentType, file.OriginalFileName);
    }

    public async Task<CustomerDisputeResponse> ConvertToDispute(ClaimsPrincipal principal, Guid id, ConvertAfterServiceToDisputeInput input, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var after = await db.AfterServiceCases.SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.ProfileId, token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다.");
        var existing = await db.DisputeCases.AsNoTracking().SingleOrDefaultAsync(x => x.AfterServiceCaseId == after.Id, token);
        if (existing is not null) return await BuildDispute(existing.Id, identity.UserId, token);
        var transaction = after.TransactionId.HasValue ? await db.Transactions.SingleAsync(x => x.Id == after.TransactionId.Value, token) : null;
        var interior = after.InteriorProjectId.HasValue ? await db.InteriorProjects.SingleAsync(x => x.Id == after.InteriorProjectId.Value && x.CustomerProfileId == identity.ProfileId, token) : null;
        if (transaction is null && interior is null) throw Invalid("AFTER_SERVICE_SOURCE_UNSUPPORTED", "분쟁으로 전환할 A/S 원본을 확인할 수 없습니다.", 409);
        var providerUser = await db.ProviderProfiles.Where(x => x.Id == after.ProviderProfileId).Select(x => x.UserId).SingleAsync(token);
        var now = DateTime.UtcNow; var key = Required(input.IdempotencyKey, 100);
        var dispute = new DisputeCase { TransactionId = transaction?.Id, InteriorProjectId = interior?.Id, AfterServiceCaseId = after.Id, ApplicantUserId = identity.UserId,
            CounterpartyUserId = providerUser, Subject = Required(input.Subject, 200), Description = ComposeDispute(input.Reason, input.RequestedResolution),
            StatusCode = "OPEN", ReceivedAt = now, LastActionAt = now, CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId };
        db.DisputeCases.Add(dispute); await db.SaveChangesAsync(token);
        db.DisputeActions.Add(new DisputeAction { DisputeCaseId = dispute.Id, ActionTypeCode = "CREATED", ToStatusCode = "OPEN", ActionNote = "A/S에서 분쟁 전환", Reason = Required(input.Reason, 3000), RelatedReferenceType = "AFTER_SERVICE", RelatedReferencePublicId = after.PublicId, OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = key });
        var previousStatus = after.StatusCode;
        after.StatusCode = "CONVERTED_TO_DISPUTE"; after.ConvertedToDisputeAt = now; after.LastActionAt = now; after.UpdatedAt = now; after.UpdatedByUserId = identity.UserId;
        db.AfterServiceActions.Add(new AfterServiceAction { AfterServiceCaseId = after.Id, FromStatusCode = previousStatus, ToStatusCode = "CONVERTED_TO_DISPUTE", ActionTypeCode = "DISPUTE_CONVERSION", ActionNote = "고객 요청으로 분쟁 전환", OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = $"after-service-dispute:{after.PublicId:N}" });
        if (transaction is not null) AddOutbox("AfterService", after.PublicId, "AFTER_SERVICE_DISPUTE_CONVERTED", providerUser, transaction.PublicId, after.PublicId, identity.UserId, now);
        else db.OutboxEvents.Add(new OutboxEvent { AggregateType = "AfterService", AggregatePublicId = after.PublicId, EventType = "AFTER_SERVICE_DISPUTE_CONVERTED", PayloadJson = JsonSerializer.Serialize(new { recipientUserId = providerUser, interiorProjectId = interior!.PublicId, sourceId = after.PublicId }), StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"after-service-dispute-converted:{after.PublicId:N}", CreatedByUserId = identity.UserId });
        await db.SaveChangesAsync(token); return await BuildDispute(dispute.Id, identity.UserId, token);
    }

    private async Task<CustomerAfterServiceResponse> Build(long id, long userId, long? customerProfileId, bool isAdmin, CancellationToken token)
    {
        var item = await db.AfterServiceCases.AsNoTracking().SingleAsync(x => x.Id == id, token);
        var transactionId = await db.Transactions.AsNoTracking().Where(x => x.Id == item.TransactionId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token);
        var interiorProjectId = await db.InteriorProjects.AsNoTracking().Where(x => x.Id == item.InteriorProjectId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token);
        var historyId = await db.ServiceHistoryEntries.AsNoTracking().Where(x => x.TransactionId == item.TransactionId && x.EventTypeCode == "COMPLETION").OrderByDescending(x => x.OccurredAt).Select(x => (Guid?)x.PublicId).FirstOrDefaultAsync(token);
        var disputeId = await db.DisputeCases.AsNoTracking().Where(x => x.AfterServiceCaseId == item.Id).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token);
        var actionRows = await db.AfterServiceActions.AsNoTracking().Where(x => x.AfterServiceCaseId == id).OrderBy(x => x.OccurredAt).ToListAsync(token);
        var publicNoteTypes = new HashSet<string>(StringComparer.Ordinal) { "RECEIVED", "PROVIDER_CONFIRMATION", "VISIT_SCHEDULED", "VISIT", "REVISIT", "TREATMENT", "RESOLUTION", "UNRESOLVED_CLOSURE", "DISPUTE_CONVERSION" };
        var timeline = actionRows.Select(x => new AfterServiceTimelineItem(x.ToStatusCode, AfterServiceDisplay(x.ToStatusCode), x.ActionTypeCode,
            isAdmin || publicNoteTypes.Contains(x.ActionTypeCode) ? x.ActionNote : null, x.ScheduledAt, x.PerformedAt, x.OccurredAt)).ToList();
        var files = await (from link in db.AfterServiceFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                           where link.AfterServiceCaseId == id && file.StatusCode == "ACTIVE" orderby link.CreatedAt
                           select new AfterServiceEvidenceResponse(file.PublicId, file.OriginalFileName, file.ContentType, file.SizeBytes, link.RoleCode, link.Description, $"/api/v1/after-services/{item.PublicId}/files/{file.PublicId}")).ToListAsync(token);
        return new(item.PublicId, transactionId, interiorProjectId, historyId, item.Subject, item.Description, item.RequestDetails, item.StatusCode,
            AfterServiceDisplay(item.StatusCode), item.ReceivedAt, item.WarrantyStartDate, item.WarrantyEndDate, item.IsWithinWarranty,
            Warranty(item.WarrantyEndDate), item.DueAt, item.ProviderConfirmedAt, item.ProviderResponseText, item.VisitRequired,
            item.StartedAt, item.CompletedAt, item.ResolutionSummary, item.UnresolvedReason, item.RecurrenceOccurred, disputeId, timeline, files);
    }

    private async Task<CustomerDisputeResponse> BuildDispute(long id, long customerUserId, CancellationToken token)
    {
        var row = await db.DisputeCases.AsNoTracking().SingleAsync(x => x.Id == id && x.ApplicantUserId == customerUserId, token);
        var transactionId = await db.Transactions.AsNoTracking().Where(x => x.Id == row.TransactionId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token);
        var interiorProjectId = await db.InteriorProjects.AsNoTracking().Where(x => x.Id == row.InteriorProjectId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token);
        var split = SplitDispute(row.Description);
        var evidence = await (from e in db.DisputeEvidence.AsNoTracking() join f0 in db.Files.AsNoTracking() on e.FileId equals f0.Id into fs from f in fs.DefaultIfEmpty() where e.DisputeCaseId == id && e.StatusCode == "ACTIVE" orderby e.SubmittedAt select new CustomerDisputeEvidenceResponse(e.PublicId, f == null ? null : f.PublicId, e.SourceTypeCode, e.Description, e.SubmittedAt, f == null ? null : "/api/v1/customers/me/dispute-files/" + f.PublicId)).ToListAsync(token);
        var resolution = await db.DisputeResolutions.AsNoTracking().Where(x => x.DisputeCaseId == id && x.IsCurrent).Select(x => new { x.ResultSummary, x.FollowUpAction }).SingleOrDefaultAsync(token);
        var updates = await db.DisputeActions.AsNoTracking().Where(x => x.DisputeCaseId == id && x.ToStatusCode != null && (x.ActionTypeCode == "CREATED" || x.ActionTypeCode == "STATUS_CHANGE" || x.ActionTypeCode == "RESOLUTION"))
            .OrderBy(x => x.OccurredAt).Select(x => new CustomerDisputeUpdate(DisputeDisplay(x.ToStatusCode!), x.OccurredAt)).ToListAsync(token);
        return new(row.PublicId, transactionId, interiorProjectId, row.Subject, split.Reason, split.Requested, row.StatusCode, DisputeDisplay(row.StatusCode), row.ReceivedAt, row.ResolvedAt, resolution?.ResultSummary, resolution?.FollowUpAction, evidence, updates);
    }

    private async Task<AccessInfo> Access(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var publicUserId = PrincipalId(principal);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == publicUserId && x.StatusCode == "ACTIVE", token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다.");
        var roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
        var query = db.AfterServiceCases.AsNoTracking().Where(x => x.PublicId == id);
        if (roles.Contains(RoleCodes.Admin)) return new((await query.Select(x => (long?)x.Id).SingleOrDefaultAsync(token)) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다."), user.Id, null, true);
        if (roles.Contains(RoleCodes.Customer))
        {
            var profile = await db.CustomerProfiles.AsNoTracking().Where(x => x.UserId == user.Id).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
            var caseId = await query.Where(x => x.CustomerProfileId == profile).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
            return new(caseId ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다."), user.Id, profile, false);
        }
        if (roles.Contains(RoleCodes.Provider))
        {
            var profile = await db.ProviderProfiles.AsNoTracking().Where(x => x.UserId == user.Id).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
            var caseId = await query.Where(x => x.ProviderProfileId == profile).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
            return new(caseId ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다."), user.Id, null, false);
        }
        throw NotFound("AFTER_SERVICE_NOT_FOUND", "A/S를 찾을 수 없습니다.");
    }

    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        var id = PrincipalId(principal);
        var value = await (from user in db.Users.AsNoTracking() join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId where user.PublicId == id && user.StatusCode == "ACTIVE" select new { user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return value is null ? throw Invalid("CUSTOMER_PROFILE_REQUIRED", "고객 프로필이 필요합니다.", 403) : (value.Id, value.ProfileId);
    }

    private void AddOutbox(string aggregate, Guid aggregateId, string type, long recipient, Guid transactionId, Guid sourceId, long actor, DateTime now)
    {
        var key = $"{type.ToLowerInvariant()}:{aggregateId:N}";
        if (db.OutboxEvents.Local.Any(x => x.IdempotencyKey == key)) return;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = aggregate, AggregatePublicId = aggregateId, EventType = type,
            PayloadJson = JsonSerializer.Serialize(new { recipientUserId = recipient, transactionId, sourceId, source_no = sourceId.ToString("N")[..8].ToUpperInvariant() }), StatusCode = "PENDING",
            OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }

    private static async Task<ValidatedUpload> Validate(IFormFile upload, CancellationToken token)
    {
        if (upload.Length <= 0 || upload.Length > MaximumFileSize) throw Invalid("CASE_FILE_SIZE_INVALID", "파일은 10MB 이하여야 합니다.");
        var rules = new Dictionary<string, (string Extension, byte[] Signature)>(StringComparer.OrdinalIgnoreCase) { ["image/jpeg"] = (".jpg", [0xff, 0xd8, 0xff]), ["image/png"] = (".png", [0x89, 0x50, 0x4e, 0x47]), ["application/pdf"] = (".pdf", Encoding.ASCII.GetBytes("%PDF")) };
        if (!rules.TryGetValue(upload.ContentType, out var rule)) throw Invalid("CASE_FILE_TYPE_INVALID", "JPEG, PNG, PDF 파일만 첨부할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName); if (string.IsNullOrWhiteSpace(name) || name != upload.FileName) throw Invalid("CASE_FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.");
        await using var source = upload.OpenReadStream(); using var memory = new MemoryStream(); await source.CopyToAsync(memory, token); var bytes = memory.ToArray();
        if (!bytes.AsSpan().StartsWith(rule.Signature)) throw Invalid("CASE_FILE_SIGNATURE_INVALID", "파일 내용과 형식이 일치하지 않습니다.");
        return new(bytes, rule.Extension);
    }

    private static StoredFile NewFile(string purpose, string key, IFormFile upload, byte[] bytes, long actor, DateTime now) => new()
    { PurposeCode = purpose, StorageContainer = "development-private", StorageKey = key, StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = Path.GetFileName(upload.FileName), ContentType = upload.ContentType.ToLowerInvariant(), SizeBytes = bytes.Length, Sha256Hex = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), StatusCode = "PENDING", UploadedByUserId = actor, CreatedAt = now };
    private static string Required(string? value, int max) { var result = value?.Trim(); if (string.IsNullOrWhiteSpace(result) || result.Length > max) throw Invalid("CASE_INPUT_INVALID", "필수 입력값을 확인해 주세요."); return result; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Guid PrincipalId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Authenticated user identifier is invalid.");
    private static string Warranty(DateOnly? end) => !end.HasValue ? "보증정보 확인 필요" : DateOnly.FromDateTime(DateTime.UtcNow) <= end.Value ? $"보증기간 {end:yyyy-MM-dd}까지" : "보증기간 경과 - 비용 발생 여부 확인 필요";
    private static string AfterServiceDisplay(string status) => status switch { "RECEIVED" => "접수", "PROVIDER_CONFIRMED" => "공급자 확인", "VISIT_SCHEDULED" => "일정 조율", "IN_PROGRESS" => "처리 중", "RESOLVED" => "처리 완료", "UNRESOLVED_CLOSED" => "미해결", "CONVERTED_TO_DISPUTE" => "분쟁 전환", _ => "진행 중" };
    private static string DisputeDisplay(string status) => status switch { "OPEN" => "접수", "UNDER_REVIEW" => "검토 중", "WAITING_CUSTOMER" => "고객 확인 대기", "WAITING_PROVIDER" => "공급자 확인 대기", "RESOLVED" => "처리 완료", "CLOSED" => "종료", _ => "진행 중" };
    private static string ComposeDispute(string reason, string requested) => $"{Required(reason, 3000)}\n\n[REQUESTED_RESOLUTION]\n{Required(requested, 2000)}";
    private static (string Reason, string Requested) SplitDispute(string value) { var parts = value.Split("\n\n[REQUESTED_RESOLUTION]\n", 2); if (parts.Length == 1) parts = value.Split("\n\n[?붽뎄?ы빆]\n", 2); return (parts[0], parts.Length > 1 ? parts[1] : ""); }
    private static WorkBusinessException NotFound(string code, string message) => new(code, message, 404);
    private static WorkBusinessException Invalid(string code, string message, int status = 400) => new(code, message, status);
    private sealed record ValidatedUpload(byte[] Bytes, string Extension);
    private sealed record AccessInfo(long CaseId, long UserId, long? CustomerProfileId, bool IsAdmin);
}
