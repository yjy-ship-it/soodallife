using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Work;

public sealed class ProviderAftercareService(
    SoodalLifeDbContext db, IPrivateFileStorage storage, IPrivacyContract privacy,
    IFilePrivacyContract filePrivacy)
{
    private const int MaximumFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> TerminalAfterService = ["RESOLVED", "UNRESOLVED_CLOSED", "CONVERTED_TO_DISPUTE"];
    private static readonly HashSet<string> TerminalDispute = ["RESOLVED", "CLOSED"];

    public async Task<IReadOnlyList<ProviderAfterServiceListItem>> AfterServices(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var ids = await AssignedAfterServices(identity.ProviderId).OrderByDescending(x => x.ReceivedAt).Select(x => x.Id).ToListAsync(token);
        var result = new List<ProviderAfterServiceListItem>();
        foreach (var id in ids)
        {
            var item = await db.AfterServiceCases.AsNoTracking().SingleAsync(x => x.Id == id, token);
            var scheduled = await db.AfterServiceActions.AsNoTracking().Where(x => x.AfterServiceCaseId == id && x.ScheduledAt != null)
                .OrderByDescending(x => x.OccurredAt).Select(x => x.ScheduledAt).FirstOrDefaultAsync(token);
            result.Add(new(item.PublicId, Number("AS", item.PublicId), item.Subject, item.StatusCode, AfterStatus(item.StatusCode),
                SourceType(item.TransactionId, item.SubscriptionVisitScheduleId, item.InteriorProjectId), item.ReceivedAt, scheduled,
                !TerminalAfterService.Contains(item.StatusCode)));
        }
        return result;
    }

    public async Task<ProviderAfterServiceDetail> AfterService(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedAfterServices(identity.ProviderId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "담당 A/S를 찾을 수 없습니다.");
        return await BuildAfterService(item, identity, token);
    }

    public Task<ProviderAfterServiceDetail> Confirm(ClaimsPrincipal principal, Guid id, ProviderConfirmAfterServiceInput input, CancellationToken token) =>
        MutateAfterService(principal, id, input.IdempotencyKey, input.RowVersion, ["RECEIVED"], "PROVIDER_CONFIRMATION", "PROVIDER_CONFIRMED",
            async (item, identity, now) => { item.ProviderConfirmedAt = now; item.ProviderResponseText = Required(input.Response, 2000); item.VisitRequired = input.VisitRequired; await Task.CompletedTask; }, token);

    public Task<ProviderAfterServiceDetail> Schedule(ClaimsPrincipal principal, Guid id, ProviderScheduleAfterServiceInput input, CancellationToken token) =>
        MutateAfterService(principal, id, input.IdempotencyKey, input.RowVersion, ["PROVIDER_CONFIRMED", "VISIT_SCHEDULED", "IN_PROGRESS"], "VISIT_SCHEDULED", "VISIT_SCHEDULED",
            async (item, identity, now) =>
            {
                if (input.ScheduledAt.ToUniversalTime() <= now) throw Bad("AFTER_SERVICE_SCHEDULE_INVALID", "방문 제안 일시는 현재 이후여야 합니다.");
                db.AfterServiceActions.Add(NewAfterAction(item, identity, "VISIT_SCHEDULED", "VISIT_SCHEDULED", Clean(input.Note) ?? "공급자 방문 일정 제안", input.IdempotencyKey, now, input.ScheduledAt.ToUniversalTime()));
                item.VisitRequired = true;
                await Task.CompletedTask;
            }, token, addDefaultAction: false);

    public Task<ProviderAfterServiceDetail> Action(ClaimsPrincipal principal, Guid id, ProviderAfterServiceActionInput input, CancellationToken token)
    {
        var action = input.ActionType.Trim().ToUpperInvariant();
        if (action is not ("VISIT" or "REVISIT" or "TREATMENT")) throw Bad("AFTER_SERVICE_ACTION_INVALID", "방문, 재방문 또는 처리 이력만 등록할 수 있습니다.");
        return MutateAfterService(principal, id, input.IdempotencyKey, input.RowVersion, ["PROVIDER_CONFIRMED", "VISIT_SCHEDULED", "IN_PROGRESS"], action, "IN_PROGRESS",
            async (item, identity, now) =>
            {
                db.AfterServiceActions.Add(new AfterServiceAction { AfterServiceCaseId = item.Id, FromStatusCode = item.StatusCode, ToStatusCode = "IN_PROGRESS",
                    ActionTypeCode = action, ActionNote = Required(input.Note, 2000), PerformedAt = input.PerformedAt?.ToUniversalTime() ?? now,
                    ProviderProfileId = identity.ProviderId, VisitOccurred = input.VisitOccurred, MaterialsText = Limit(input.Materials, 2000), ResultText = Limit(input.Result, 2000),
                    OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = Key(input.IdempotencyKey) });
                item.StartedAt ??= now;
                await Task.CompletedTask;
            }, token, addDefaultAction: false);
    }

    public Task<ProviderAfterServiceDetail> Complete(ClaimsPrincipal principal, Guid id, ProviderCompleteAfterServiceInput input, CancellationToken token) =>
        MutateAfterService(principal, id, input.IdempotencyKey, input.RowVersion, ["IN_PROGRESS"], input.Resolved ? "RESOLUTION" : "UNRESOLVED_CLOSURE",
            input.Resolved ? "RESOLVED" : "UNRESOLVED_CLOSED", async (item, identity, now) =>
            {
                item.ResolutionSummary = Required(input.Summary, 2000);
                item.UnresolvedReason = input.Resolved ? null : Required(input.UnresolvedReason, 1000);
                item.RecurrenceOccurred = input.RecurrenceOccurred; item.CompletedAt = now;
                await Task.CompletedTask;
            }, token);

    public async Task<ProviderCaseFile> UploadAfterServiceEvidence(ClaimsPrincipal principal, Guid id, string? role, string? description, IFormFile upload, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedAfterServices(identity.ProviderId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "담당 A/S를 찾을 수 없습니다.");
        if (TerminalAfterService.Contains(item.StatusCode)) throw Conflict("AFTER_SERVICE_CLOSED", "종료된 A/S에는 새 증빙을 등록할 수 없습니다.");
        var file = await SaveFile(upload, "AFTER_SERVICE", $"after-service/{id:N}", identity.UserId, token);
        var now = DateTime.UtcNow;
        db.AfterServiceFiles.Add(new AfterServiceFile { AfterServiceCaseId = item.Id, FileId = file.Id, RoleCode = Limit(role, 50), Description = Limit(description, 500), CreatedAt = now, CreatedByUserId = identity.UserId });
        await Audit(identity.UserId, "PROVIDER_AFTER_SERVICE_EVIDENCE_ADDED", "AfterService", item.PublicId, new { fileId = file.PublicId }, token);
        return ProviderFile(item.PublicId, file, role, description, "PROVIDER_UPLOAD", "OWNER_ORIGINAL", "after-services");
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenAfterServiceFile(ClaimsPrincipal principal, Guid id, Guid fileId, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedAfterServices(identity.ProviderId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "담당 A/S를 찾을 수 없습니다.");
        var row = await (from link in db.AfterServiceFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                         where link.AfterServiceCaseId == item.Id && file.PublicId == fileId && file.StatusCode == "ACTIVE" select file).SingleOrDefaultAsync(token)
                  ?? throw NotFound("AFTER_SERVICE_FILE_NOT_FOUND", "공개 가능한 A/S 증빙을 찾을 수 없습니다.");
        var published = await PublishedFile(row, identity.UserId, token) ?? throw NotFound("AFTER_SERVICE_FILE_NOT_AVAILABLE", "검사 및 개인정보 보호가 완료된 파일만 열 수 있습니다.");
        return (await storage.OpenReadAsync(published.StorageKey, token), published.ContentType, SafeFileName(published.ContentType));
    }

    public async Task<IReadOnlyList<ProviderDisputeListItem>> Disputes(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var rows = await AssignedDisputes(identity.UserId).OrderByDescending(x => x.ReceivedAt).ToListAsync(token);
        return rows.Select(x => new ProviderDisputeListItem(x.PublicId, Number("DS", x.PublicId), x.Subject, x.StatusCode, DisputeStatus(x.StatusCode),
            SourceType(x.TransactionId, x.SubscriptionVisitScheduleId, x.InteriorProjectId), x.ReceivedAt, x.LastActionAt)).ToArray();
    }

    public async Task<ProviderDisputeDetail> Dispute(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedDisputes(identity.UserId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("DISPUTE_NOT_FOUND", "연결된 분쟁을 찾을 수 없습니다.");
        return await BuildDispute(item, identity, token);
    }

    public async Task<ProviderDisputeDetail> Respond(ClaimsPrincipal principal, Guid id, ProviderDisputeResponseInput input, CancellationToken token)
    {
        var identity = await Provider(principal, token); var key = Key(input.IdempotencyKey);
        var item = await AssignedDisputes(identity.UserId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("DISPUTE_NOT_FOUND", "연결된 분쟁을 찾을 수 없습니다.");
        if (await db.DisputeActions.AsNoTracking().AnyAsync(x => x.IdempotencyKey == key && x.DisputeCaseId == item.Id, token)) return await BuildDispute(item, identity, token);
        if (TerminalDispute.Contains(item.StatusCode)) throw Conflict("DISPUTE_CLOSED", "종료된 분쟁에는 소명을 추가할 수 없습니다.");
        ApplyVersion(item, input.RowVersion); var now = DateTime.UtcNow;
        db.DisputeActions.Add(new DisputeAction { DisputeCaseId = item.Id, ActionTypeCode = "NOTE", ActionNote = Required(input.Statement, 2000),
            OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = key });
        item.LastActionAt = now; item.UpdatedAt = now; item.UpdatedByUserId = identity.UserId;
        await AddOutboxIfTemplate(item.PublicId, "Dispute", "DISPUTE_PROVIDER_RESPONSE_SUBMITTED", item.ApplicantUserId, identity.UserId, now, token);
        AddAudit(identity.UserId, "PROVIDER_DISPUTE_RESPONSE_SUBMITTED", "Dispute", item.PublicId, new { action = "NOTE" }, now);
        await SaveConcurrent(token); return await BuildDispute(item, identity, token);
    }

    public async Task<ProviderCaseFile> UploadDisputeEvidence(ClaimsPrincipal principal, Guid id, string? description, IFormFile upload, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedDisputes(identity.UserId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("DISPUTE_NOT_FOUND", "연결된 분쟁을 찾을 수 없습니다.");
        if (TerminalDispute.Contains(item.StatusCode)) throw Conflict("DISPUTE_CLOSED", "종료된 분쟁에는 증빙을 추가할 수 없습니다.");
        var file = await SaveFile(upload, "DISPUTE_EVIDENCE", $"dispute/{id:N}", identity.UserId, token); var now = DateTime.UtcNow;
        var evidence = new DisputeEvidence { DisputeCaseId = item.Id, FileId = file.Id, SubmittedByUserId = identity.UserId, SourceTypeCode = "PROVIDER_UPLOAD",
            Description = Limit(description, 1000), StatusCode = "ACTIVE", SubmittedAt = now, CreatedAt = now, CreatedByUserId = identity.UserId };
        db.DisputeEvidence.Add(evidence); await db.SaveChangesAsync(token);
        db.DisputeActions.Add(new DisputeAction { DisputeCaseId = item.Id, ActionTypeCode = "EVIDENCE_ADDED", ActionNote = "공급자 증빙 제출",
            RelatedReferenceType = "DISPUTE_EVIDENCE", RelatedReferencePublicId = evidence.PublicId, OccurredAt = now, ActorUserId = identity.UserId,
            IdempotencyKey = $"provider-dispute-evidence:{evidence.PublicId:N}" });
        item.LastActionAt = now; item.UpdatedAt = now; item.UpdatedByUserId = identity.UserId;
        await AddOutboxIfTemplate(item.PublicId, "Dispute", "DISPUTE_PROVIDER_EVIDENCE_SUBMITTED", item.ApplicantUserId, identity.UserId, now, token);
        AddAudit(identity.UserId, "PROVIDER_DISPUTE_EVIDENCE_ADDED", "Dispute", item.PublicId, new { evidenceId = evidence.PublicId }, now);
        await db.SaveChangesAsync(token);
        return ProviderFile(item.PublicId, file, null, description, "PROVIDER_UPLOAD", "OWNER_ORIGINAL", "disputes");
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenDisputeFile(ClaimsPrincipal principal, Guid id, Guid fileId, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var item = await AssignedDisputes(identity.UserId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("DISPUTE_NOT_FOUND", "연결된 분쟁을 찾을 수 없습니다.");
        var file = await (from evidence in db.DisputeEvidence.AsNoTracking() join f in db.Files.AsNoTracking() on evidence.FileId equals f.Id
                          where evidence.DisputeCaseId == item.Id && evidence.StatusCode == "ACTIVE" && f.PublicId == fileId && f.StatusCode == "ACTIVE" select f).SingleOrDefaultAsync(token)
                   ?? throw NotFound("DISPUTE_FILE_NOT_FOUND", "공개 가능한 분쟁 증빙을 찾을 수 없습니다.");
        var published = await PublishedFile(file, identity.UserId, token) ?? throw NotFound("DISPUTE_FILE_NOT_AVAILABLE", "검사 및 개인정보 보호가 완료된 파일만 열 수 있습니다.");
        return (await storage.OpenReadAsync(published.StorageKey, token), published.ContentType, SafeFileName(published.ContentType));
    }

    private async Task<ProviderAfterServiceDetail> MutateAfterService(ClaimsPrincipal principal, Guid id, string rawKey, string rowVersion,
        string[] allowed, string actionType, string targetStatus, Func<AfterServiceCase, ProviderIdentity, DateTime, Task> changes,
        CancellationToken token, bool addDefaultAction = true)
    {
        var identity = await Provider(principal, token); var key = Key(rawKey);
        var item = await AssignedAfterServices(identity.ProviderId).SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw NotFound("AFTER_SERVICE_NOT_FOUND", "담당 A/S를 찾을 수 없습니다.");
        if (await db.AfterServiceActions.AsNoTracking().AnyAsync(x => x.AfterServiceCaseId == item.Id && x.IdempotencyKey == key, token)) return await BuildAfterService(item, identity, token);
        if (!allowed.Contains(item.StatusCode)) throw Conflict("AFTER_SERVICE_TRANSITION_INVALID", "현재 상태에서는 해당 A/S 조치를 수행할 수 없습니다.");
        ApplyVersion(item, rowVersion); var from = item.StatusCode; var now = DateTime.UtcNow;
        await changes(item, identity, now);
        if (addDefaultAction) db.AfterServiceActions.Add(NewAfterAction(item, identity, actionType, targetStatus,
            actionType == "PROVIDER_CONFIRMATION" ? "공급자 접수 확인" : targetStatus == "RESOLVED" ? "공급자 해결 완료 보고" : "공급자 미해결 종료 보고", key, now));
        item.StatusCode = targetStatus; item.LastActionAt = now; item.UpdatedAt = now; item.UpdatedByUserId = identity.UserId;
        await AddOutboxIfTemplate(item.PublicId, "AfterService", actionType switch { "PROVIDER_CONFIRMATION" => "AFTER_SERVICE_PROVIDER_CONFIRMED", "VISIT_SCHEDULED" => "AFTER_SERVICE_VISIT_SCHEDULED", "RESOLUTION" or "UNRESOLVED_CLOSURE" => "AFTER_SERVICE_COMPLETION_REPORTED", _ => "AFTER_SERVICE_ACTION_RECORDED" },
            await CustomerUser(item.CustomerProfileId, token), identity.UserId, now, token);
        AddAudit(identity.UserId, "PROVIDER_AFTER_SERVICE_" + actionType, "AfterService", item.PublicId, new { from, to = targetStatus }, now);
        await SaveConcurrent(token); return await BuildAfterService(item, identity, token);
    }

    private async Task<ProviderAfterServiceDetail> BuildAfterService(AfterServiceCase item, ProviderIdentity identity, CancellationToken token)
    {
        var source = await Source(item.TransactionId, item.SubscriptionVisitScheduleId, item.InteriorProjectId, null, token);
        var customer = await (from profile in db.CustomerProfiles.AsNoTracking() join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                              where profile.Id == item.CustomerProfileId select new { profile.DisplayName, user.Phone }).SingleAsync(token);
        var contactActive = !TerminalAfterService.Contains(item.StatusCode);
        var phoneAllowed = privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.Phone, contactActive).CanAccess;
        var addressAllowed = privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.DetailAddress, contactActive).CanAccess;
        var address = contactActive ? await SourceAddress(item, token) : null;
        var actions = await db.AfterServiceActions.AsNoTracking().Where(x => x.AfterServiceCaseId == item.Id).OrderBy(x => x.OccurredAt).ToListAsync(token);
        var timeline = actions.Select(x => new ProviderAfterServiceTimeline(x.ActionTypeCode, x.ToStatusCode, AfterStatus(x.ToStatusCode), x.ActionNote, x.ScheduledAt, x.PerformedAt, x.OccurredAt)).ToArray();
        var evidence = await AfterFiles(item, identity, token);
        return new(item.PublicId, Number("AS", item.PublicId), source, item.Subject, item.Description, item.RequestDetails, item.StatusCode, AfterStatus(item.StatusCode), item.ReceivedAt,
            item.WarrantyStartDate, item.WarrantyEndDate, item.IsWithinWarranty, item.DueAt, item.ProviderConfirmedAt, item.ProviderResponseText, item.VisitRequired,
            item.StartedAt, item.CompletedAt, item.ResolutionSummary, item.UnresolvedReason, item.RecurrenceOccurred, customer.DisplayName,
            phoneAllowed ? customer.Phone : null, addressAllowed ? address : null, contactActive && phoneAllowed && addressAllowed,
            contactActive ? "담당 A/S 수행 중에만 연락처와 상세주소를 제공합니다." : "A/S 종료 후 신규 연락처·상세주소 조회가 차단되었습니다.",
            Convert.ToBase64String(item.RowVersion), timeline, evidence);
    }

    private async Task<ProviderDisputeDetail> BuildDispute(DisputeCase item, ProviderIdentity identity, CancellationToken token)
    {
        var source = await Source(item.TransactionId, item.SubscriptionVisitScheduleId, item.InteriorProjectId, item.AfterServiceCaseId, token);
        var actions = await db.DisputeActions.AsNoTracking().Where(x => x.DisputeCaseId == item.Id).OrderBy(x => x.OccurredAt).ToListAsync(token);
        var timeline = actions.Select(x => new ProviderDisputeTimeline(x.ActionTypeCode, x.ActionNote, x.Reason, x.OccurredAt, x.ActorUserId == identity.UserId)).ToArray();
        var evidence = await DisputeFiles(item, identity, token);
        return new(item.PublicId, Number("DS", item.PublicId), source, item.Subject, item.Description, item.StatusCode, DisputeStatus(item.StatusCode), item.ReceivedAt,
            item.DueAt, item.ResolvedAt, Convert.ToBase64String(item.RowVersion), timeline, evidence, !TerminalDispute.Contains(item.StatusCode),
            "공급자는 소명과 증빙만 제출할 수 있습니다. 판정·귀책·환불·수수료 복원·TrustScore 결정은 관리자 전용입니다.");
    }

    private async Task<IReadOnlyList<ProviderCaseFile>> AfterFiles(AfterServiceCase item, ProviderIdentity identity, CancellationToken token)
    {
        var rows = await (from link in db.AfterServiceFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                          where link.AfterServiceCaseId == item.Id && file.StatusCode == "ACTIVE" orderby link.CreatedAt select new { link, file }).ToListAsync(token);
        var result = new List<ProviderCaseFile>();
        foreach (var row in rows) { var published = await PublishedFile(row.file, identity.UserId, token); if (published is null) continue;
            result.Add(ProviderFile(item.PublicId, published, row.link.RoleCode, row.link.Description, row.file.UploadedByUserId == identity.UserId ? "PROVIDER_UPLOAD" : "CUSTOMER_EVIDENCE",
                published.Id == row.file.Id ? (row.file.UploadedByUserId == identity.UserId ? "OWNER_ORIGINAL" : "PRIVACY_SAFE_ORIGINAL") : "PRIVACY_SANITIZED_DERIVATIVE", "after-services")); }
        return result;
    }

    private async Task<IReadOnlyList<ProviderCaseFile>> DisputeFiles(DisputeCase item, ProviderIdentity identity, CancellationToken token)
    {
        var rows = await (from evidence in db.DisputeEvidence.AsNoTracking() join file0 in db.Files.AsNoTracking() on evidence.FileId equals file0.Id into files
                          from file in files.DefaultIfEmpty() where evidence.DisputeCaseId == item.Id && evidence.StatusCode == "ACTIVE" orderby evidence.SubmittedAt select new { evidence, file }).ToListAsync(token);
        var result = new List<ProviderCaseFile>();
        foreach (var row in rows) { if (row.file is null) continue; var published = await PublishedFile(row.file, identity.UserId, token); if (published is null) continue;
            result.Add(ProviderFile(item.PublicId, published, null, row.evidence.Description, row.evidence.SourceTypeCode,
                published.Id == row.file.Id ? (row.file.UploadedByUserId == identity.UserId ? "OWNER_ORIGINAL" : "PRIVACY_SAFE_ORIGINAL") : "PRIVACY_SANITIZED_DERIVATIVE", "disputes")); }
        return result;
    }

    private async Task<StoredFile?> PublishedFile(StoredFile original, long providerUserId, CancellationToken token)
    {
        if (original.UploadedByUserId == providerUserId) return original.StatusCode == "ACTIVE" ? original : null;
        var derivative = await (from relation in db.FileDerivatives.AsNoTracking() join file in db.Files.AsNoTracking() on relation.DerivedFileId equals file.Id
                                where relation.OriginalFileId == original.Id && relation.DerivativeTypeCode == FilePrivacyCodes.PrivacySanitized
                                orderby file.CreatedAt descending select file).FirstOrDefaultAsync(token);
        var decision = filePrivacy.Evaluate(original, derivative, FileAccessAudience.SelectedProvider);
        return decision.Allowed ? decision.PublishedFile : null;
    }

    private IQueryable<AfterServiceCase> AssignedAfterServices(long providerId)
    {
        var now = DateTime.UtcNow;
        return db.AfterServiceCases.Where(x => x.ProviderProfileId == providerId || x.InteriorProjectId != null &&
            db.InteriorProjectParticipants.Any(p => p.InteriorProjectId == x.InteriorProjectId && p.ProviderProfileId == providerId && p.RoleCode == "AFTER_SERVICE" && p.StatusCode == "ACTIVE" && p.EffectiveFrom <= now && (p.EffectiveTo == null || p.EffectiveTo > now)));
    }
    private IQueryable<DisputeCase> AssignedDisputes(long userId) => db.DisputeCases.Where(x => x.CounterpartyUserId == userId || x.ApplicantUserId == userId);

    private async Task<ProviderCaseSource> Source(long? tx, long? visit, long? interior, long? after, CancellationToken token) => new(
        tx.HasValue ? await db.Transactions.AsNoTracking().Where(x => x.Id == tx).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token) : null,
        visit.HasValue ? await db.SubscriptionVisitSchedules.AsNoTracking().Where(x => x.Id == visit).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token) : null,
        interior.HasValue ? await db.InteriorProjects.AsNoTracking().Where(x => x.Id == interior).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token) : null,
        after.HasValue ? await db.AfterServiceCases.AsNoTracking().Where(x => x.Id == after).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync(token) : null);

    private async Task<string?> SourceAddress(AfterServiceCase item, CancellationToken token)
    {
        if (item.TransactionId.HasValue) return await (from tx in db.Transactions.AsNoTracking() join request in db.ServiceRequests.AsNoTracking() on tx.ServiceRequestId equals request.Id where tx.Id == item.TransactionId select request.DetailAddress).SingleOrDefaultAsync(token);
        if (item.SubscriptionVisitScheduleId.HasValue) return await (from visit in db.SubscriptionVisitSchedules.AsNoTracking() join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id join request in db.SubscriptionRequests.AsNoTracking() on contract.SubscriptionRequestId equals request.Id where visit.Id == item.SubscriptionVisitScheduleId select request.DetailAddress).SingleOrDefaultAsync(token);
        if (item.InteriorProjectId.HasValue) return await (from project in db.InteriorProjects.AsNoTracking() join request in db.ServiceRequests.AsNoTracking() on project.ServiceRequestId equals request.Id where project.Id == item.InteriorProjectId select request.DetailAddress).SingleOrDefaultAsync(token);
        return null;
    }

    private async Task<StoredFile> SaveFile(IFormFile upload, string purpose, string prefix, long userId, CancellationToken token)
    {
        var validated = await Validate(upload, token); var now = DateTime.UtcNow; var key = $"{prefix}/{Guid.NewGuid():N}{validated.Extension}";
        var file = new StoredFile { PurposeCode = purpose, StorageContainer = "development-private", StorageKey = key, StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)),
            OriginalFileName = Path.GetFileName(upload.FileName), ContentType = upload.ContentType.ToLowerInvariant(), SizeBytes = validated.Bytes.Length,
            Sha256Hex = Convert.ToHexString(SHA256.HashData(validated.Bytes)).ToLowerInvariant(), StatusCode = "PENDING", MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
            PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated, SanitizationStatusCode = FilePrivacyCodes.NotIntegrated, ScanResultText = FilePrivacyCodes.NotIntegrated,
            UploadedByUserId = userId, CreatedAt = now };
        db.Files.Add(file); await db.SaveChangesAsync(token);
        try { await using var stream = new MemoryStream(validated.Bytes); await storage.SaveAsync(key, stream, token); file.StatusCode = "ACTIVE"; file.ActivatedAt = now; await db.SaveChangesAsync(token); return file; }
        catch { await storage.DeleteIfExistsAsync(key, token); throw; }
    }

    private static async Task<ValidatedUpload> Validate(IFormFile upload, CancellationToken token)
    {
        if (upload.Length <= 0 || upload.Length > MaximumFileSize) throw Bad("CASE_FILE_SIZE_INVALID", "파일은 10MB 이하여야 합니다.");
        var rules = new Dictionary<string, (string Extension, byte[] Signature)>(StringComparer.OrdinalIgnoreCase) { ["image/jpeg"] = (".jpg", [0xff, 0xd8, 0xff]), ["image/png"] = (".png", [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), ["application/pdf"] = (".pdf", Encoding.ASCII.GetBytes("%PDF-")) };
        if (!rules.TryGetValue(upload.ContentType, out var rule)) throw Bad("CASE_FILE_TYPE_INVALID", "JPEG, PNG, PDF 파일만 첨부할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName); if (string.IsNullOrWhiteSpace(name) || name != upload.FileName || !string.Equals(Path.GetExtension(name), rule.Extension, StringComparison.OrdinalIgnoreCase)) throw Bad("CASE_FILE_NAME_INVALID", "파일명과 확장자를 확인해 주세요.");
        await using var source = upload.OpenReadStream(); using var memory = new MemoryStream(); await source.CopyToAsync(memory, token); var bytes = memory.ToArray();
        if (!bytes.AsSpan().StartsWith(rule.Signature)) throw Bad("CASE_FILE_SIGNATURE_INVALID", "파일 내용과 형식이 일치하지 않습니다."); return new(bytes, rule.Extension);
    }

    private async Task AddOutboxIfTemplate(Guid id, string aggregate, string eventType, long recipient, long actor, DateTime now, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token)) return;
        var key = $"provider-aftercare:{eventType.ToLowerInvariant()}:{id:N}";
        if (await db.OutboxEvents.AsNoTracking().AnyAsync(x => x.IdempotencyKey == key, token)) return;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = aggregate, AggregatePublicId = id, EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(new { recipientUserId = recipient, sourceId = id, source_no = id.ToString("N")[..8].ToUpperInvariant() }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }

    private async Task Audit(long user, string action, string entity, Guid id, object after, CancellationToken token) { AddAudit(user, action, entity, id, after, DateTime.UtcNow); await db.SaveChangesAsync(token); }
    private void AddAudit(long user, string action, string entity, Guid id, object after, DateTime now) => db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = user, ActorRoleCode = RoleCodes.Provider, ActionCode = action, EntityType = entity, EntityPublicId = id, ResultCode = "SUCCESS", AfterJson = JsonSerializer.Serialize(after) });
    private async Task SaveConcurrent(CancellationToken token) { try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { throw Conflict("ROW_VERSION_CONFLICT", "다른 사용자가 먼저 변경했습니다. 새로고침 후 다시 시도해 주세요."); } }
    private void ApplyVersion(object entity, string value) { try { db.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(Required(value, 500)); } catch (FormatException) { throw Bad("ROW_VERSION_INVALID", "변경 버전 값이 올바르지 않습니다."); } }
    private async Task<ProviderIdentity> Provider(ClaimsPrincipal principal, CancellationToken token) { if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) throw new WorkBusinessException("AUTHENTICATION_REQUIRED", "로그인이 필요합니다.", 401); var value = await (from user in db.Users where user.PublicId == id && user.StatusCode == "ACTIVE" join profile in db.ProviderProfiles on user.Id equals profile.UserId select new ProviderIdentity(user.Id, profile.Id)).SingleOrDefaultAsync(token); return value ?? throw new WorkBusinessException("PROVIDER_PROFILE_REQUIRED", "공급자 프로필이 필요합니다.", 403); }
    private Task<long> CustomerUser(long customer, CancellationToken token) => db.CustomerProfiles.Where(x => x.Id == customer).Select(x => x.UserId).SingleAsync(token);
    private static AfterServiceAction NewAfterAction(AfterServiceCase item, ProviderIdentity identity, string type, string to, string note, string key, DateTime now, DateTime? scheduled = null) => new() { AfterServiceCaseId = item.Id, FromStatusCode = item.StatusCode, ToStatusCode = to, ActionTypeCode = type, ActionNote = note, ScheduledAt = scheduled, ProviderProfileId = identity.ProviderId, OccurredAt = now, ActorUserId = identity.UserId, IdempotencyKey = Key(key) };
    private static ProviderCaseFile ProviderFile(Guid caseId, StoredFile file, string? role, string? description, string source, string mode, string segment) => new(file.PublicId, SafeFileName(file.ContentType), file.ContentType, file.SizeBytes, Clean(role), Clean(description), source, mode, $"/api/v1/providers/me/{segment}/{caseId}/files/{file.PublicId}");
    private static string SafeFileName(string contentType) => "evidence" + (contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", "application/pdf" => ".pdf", _ => ".bin" });
    private static string SourceType(long? tx, long? visit, long? interior) => interior.HasValue ? "INTERIOR" : visit.HasValue ? "SUBSCRIPTION" : tx.HasValue ? "TRANSACTION" : "OTHER";
    private static string Number(string prefix, Guid id) => $"{prefix}-{id.ToString("N")[..8].ToUpperInvariant()}";
    private static string AfterStatus(string value) => value switch { "RECEIVED" => "접수", "PROVIDER_CONFIRMED" => "공급자 확인", "VISIT_SCHEDULED" => "방문 예정", "IN_PROGRESS" => "처리 중", "RESOLVED" => "해결 완료", "UNRESOLVED_CLOSED" => "미해결 종료", "CONVERTED_TO_DISPUTE" => "분쟁 전환", _ => value };
    private static string DisputeStatus(string value) => value switch { "OPEN" => "접수", "UNDER_REVIEW" => "검토 중", "WAITING_CUSTOMER" => "고객 확인 대기", "WAITING_PROVIDER" => "공급자 확인 대기", "RESOLVED" => "처리 완료", "CLOSED" => "종료", _ => value };
    private static string Key(string value) => Required(value, 100);
    private static string Required(string? value, int max) { var result = value?.Trim(); if (string.IsNullOrWhiteSpace(result) || result.Length > max) throw Bad("AFTERCARE_INPUT_INVALID", "필수 입력값과 길이를 확인해 주세요."); return result; }
    private static string? Limit(string? value, int max) { var result = Clean(value); if (result?.Length > max) throw Bad("AFTERCARE_INPUT_INVALID", "입력값 길이를 확인해 주세요."); return result; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException Bad(string code, string message) => new(code, message, 400);
    private static WorkBusinessException Conflict(string code, string message) => new(code, message, 409);
    private static WorkBusinessException NotFound(string code, string message) => new(code, message, 404);
    private sealed record ProviderIdentity(long UserId, long ProviderId);
    private sealed record ValidatedUpload(byte[] Bytes, string Extension);
}
