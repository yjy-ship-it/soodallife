using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Work;

public sealed class CustomerReportService(SoodalLifeDbContext db, IPrivateFileStorage storage)
{
    public async Task<IReadOnlyList<CustomerReportType>> Types(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        return await db.ReportTypes.AsNoTracking().Where(x => x.IsActive && (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= now) && (!x.EffectiveTo.HasValue || x.EffectiveTo > now))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Select(x => new CustomerReportType(x.PublicId, x.Code, x.Name, x.Description)).ToListAsync(token);
    }

    public async Task<IReadOnlyList<CustomerReportResponse>> List(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var ids = await db.Reports.AsNoTracking().Where(x => x.ReporterUserId == identity.UserId).OrderByDescending(x => x.ReceivedAt).Select(x => x.Id).ToListAsync(token);
        var result = new List<CustomerReportResponse>(); foreach (var id in ids) result.Add(await Build(id, identity.UserId, token)); return result;
    }

    public async Task<CustomerReportResponse> Detail(ClaimsPrincipal principal, Guid id, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var reportId = await db.Reports.AsNoTracking().Where(x => x.PublicId == id && x.ReporterUserId == identity.UserId).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
        return reportId.HasValue ? await Build(reportId.Value, identity.UserId, token) : throw NotFound();
    }

    public async Task<CustomerReportResponse> Create(ClaimsPrincipal principal, CreateCustomerReportInput input, CancellationToken token)
    {
        var identity = await Customer(principal, token); var targetType = Required(input.TargetType, 40).ToUpperInvariant(); var key = Required(input.IdempotencyKey, 150);
        var existing = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, token); if (existing is not null) return existing.ReporterUserId == identity.UserId ? await Build(existing.Id, identity.UserId, token) : throw NotFound();
        var now = DateTime.UtcNow;
        var type = await db.ReportTypes.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == input.ReportTypeId && x.IsActive && (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= now) && (!x.EffectiveTo.HasValue || x.EffectiveTo > now), token) ?? throw Invalid("REPORT_TYPE_NOT_FOUND", "사용 가능한 신고 유형을 선택해 주세요.");
        var links = await ResolveTarget(identity, targetType, input.TargetId, token);
        var report = new Report { ReporterUserId = identity.UserId, ReportedUserId = links.ReportedUserId, ReportTypeId = type.Id,
            ServiceRequestId = links.ServiceRequestId, TransactionId = links.TransactionId, ReviewId = links.ReviewId,
            AfterServiceCaseId = links.AfterServiceCaseId, DisputeCaseId = links.DisputeCaseId, Description = Required(input.Description, 4000),
            StatusCode = "RECEIVED", ReceivedAt = now, IdempotencyKey = key, CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId };
        db.Reports.Add(report); await db.SaveChangesAsync(token);
        db.ReportActions.Add(new ReportAction { ReportId = report.Id, ActionTypeCode = "CREATED", ToStatusCode = "RECEIVED", ActorUserId = identity.UserId, Reason = "고객 신고 접수", OccurredAt = now, IdempotencyKey = $"report-created:{report.PublicId:N}" });
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = "Report", AggregatePublicId = report.PublicId, EventType = "REPORT_RECEIVED",
            PayloadJson = JsonSerializer.Serialize(new { recipientUserId = identity.UserId, reportId = report.PublicId, sourceId = report.PublicId, source_no = Number(report.PublicId) }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"report-received:{report.PublicId:N}", CreatedByUserId = identity.UserId });
        await db.SaveChangesAsync(token); return await Build(report.Id, identity.UserId, token);
    }

    public async Task<CustomerReportEvidence> Upload(ClaimsPrincipal principal, Guid id, string? description, IFormFile upload, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var reportId = await db.Reports.AsNoTracking().Where(x => x.PublicId == id && x.ReporterUserId == identity.UserId).Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw NotFound();
        var validated = await Validate(upload, token); var now = DateTime.UtcNow; var key = $"report/{id:N}/{Guid.NewGuid():N}{validated.Extension}";
        var file = new StoredFile { PurposeCode = "REPORT_EVIDENCE", StorageContainer = "development-private", StorageKey = key,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = Path.GetFileName(upload.FileName), ContentType = upload.ContentType.ToLowerInvariant(),
            SizeBytes = validated.Bytes.Length, Sha256Hex = Convert.ToHexString(SHA256.HashData(validated.Bytes)).ToLowerInvariant(), StatusCode = "PENDING", UploadedByUserId = identity.UserId, CreatedAt = now };
        db.Files.Add(file); await db.SaveChangesAsync(token);
        try
        {
            await using var stream = new MemoryStream(validated.Bytes); await storage.SaveAsync(key, stream, token); file.StatusCode = "ACTIVE"; file.ActivatedAt = now; file.ScanResultText = "NOT_INTEGRATED";
            db.ReportEvidence.Add(new ReportEvidence { ReportId = reportId, FileId = file.Id, SubmittedByUserId = identity.UserId, Description = Clean(description), StatusCode = "ACTIVE", SubmittedAt = now, CreatedAt = now, CreatedByUserId = identity.UserId });
            db.ReportActions.Add(new ReportAction { ReportId = reportId, ActionTypeCode = "EVIDENCE_ADDED", ActorUserId = identity.UserId, Reason = "고객 증빙 추가", OccurredAt = now, IdempotencyKey = $"report-evidence:{file.PublicId:N}" });
            await db.SaveChangesAsync(token); return new(file.PublicId, file.OriginalFileName, file.ContentType, file.SizeBytes, Clean(description), $"/api/v1/customers/me/reports/{id}/files/{file.PublicId}");
        }
        catch { await storage.DeleteIfExistsAsync(key, token); throw; }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenFile(ClaimsPrincipal principal, Guid id, Guid fileId, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var file = await (from report in db.Reports.AsNoTracking() join evidence in db.ReportEvidence.AsNoTracking() on report.Id equals evidence.ReportId join stored in db.Files.AsNoTracking() on evidence.FileId equals stored.Id
                          where report.PublicId == id && report.ReporterUserId == identity.UserId && stored.PublicId == fileId && stored.StatusCode == "ACTIVE" select stored).SingleOrDefaultAsync(token) ?? throw NotFound();
        return (await storage.OpenReadAsync(file.StorageKey, token), file.ContentType, file.OriginalFileName);
    }

    private async Task<CustomerReportResponse> Build(long id, long userId, CancellationToken token)
    {
        var report = await db.Reports.AsNoTracking().SingleAsync(x => x.Id == id && x.ReporterUserId == userId, token);
        var type = await db.ReportTypes.AsNoTracking().SingleAsync(x => x.Id == report.ReportTypeId, token);
        var target = await Target(report, token);
        var evidence = await (from link in db.ReportEvidence.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                              where link.ReportId == report.Id && link.StatusCode == "ACTIVE" && file.StatusCode == "ACTIVE" orderby link.SubmittedAt
                              select new CustomerReportEvidence(file.PublicId, file.OriginalFileName, file.ContentType, file.SizeBytes, link.Description, $"/api/v1/customers/me/reports/{report.PublicId}/files/{file.PublicId}")).ToListAsync(token);
        var hasSanction = await (from source in db.SanctionSources.AsNoTracking() join sanction in db.Sanctions.AsNoTracking() on source.SanctionId equals sanction.Id where source.ReportId == report.Id && sanction.StatusCode != "CANCELLED" select source.Id).AnyAsync(token);
        var publicResult = report.StatusCode == "RESOLVED" ? report.ResultSummary ?? (hasSanction ? "검토 결과 필요한 조치가 완료되었습니다." : "검토가 완료되었습니다.") : null;
        return new(report.PublicId, Number(report.PublicId), type.PublicId, type.Code, type.Name, target.Type, target.Id, target.Display,
            report.Description, report.StatusCode, Display(report.StatusCode), report.ReceivedAt, report.ResolvedAt, publicResult, evidence);
    }

    private async Task<TargetLinks> ResolveTarget((long UserId, long ProfileId) identity, string type, Guid id, CancellationToken token)
    {
        if (type == "TRANSACTION")
        {
            var row = await db.Transactions.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.ProfileId, token) ?? throw TargetNotFound();
            return new(await ProviderUser(row.ProviderProfileId, token), null, row.Id, null, null, null);
        }
        if (type == "REVIEW")
        {
            var row = await (from review in db.Reviews.AsNoTracking() join tx in db.Transactions.AsNoTracking() on review.TransactionId equals tx.Id where review.PublicId == id && review.CustomerProfileId == identity.ProfileId select new { review.Id, tx.ProviderProfileId }).SingleOrDefaultAsync(token) ?? throw TargetNotFound();
            return new(await ProviderUser(row.ProviderProfileId, token), null, null, row.Id, null, null);
        }
        if (type == "AFTER_SERVICE")
        {
            var row = await db.AfterServiceCases.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.ProfileId, token) ?? throw TargetNotFound();
            return new(await ProviderUser(row.ProviderProfileId, token), null, null, null, row.Id, null);
        }
        if (type == "DISPUTE")
        {
            var row = await (from dispute in db.DisputeCases.AsNoTracking() join tx in db.Transactions.AsNoTracking() on dispute.TransactionId equals tx.Id where dispute.PublicId == id && tx.CustomerProfileId == identity.ProfileId select new { dispute.Id, dispute.CounterpartyUserId }).SingleOrDefaultAsync(token) ?? throw TargetNotFound();
            return new(row.CounterpartyUserId, null, null, null, null, row.Id);
        }
        if (type == "REQUEST")
        {
            var row = await db.ServiceRequests.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.ProfileId, token) ?? throw TargetNotFound();
            return new(null, row.Id, null, null, null, null);
        }
        throw Invalid("REPORT_TARGET_TYPE_INVALID", "지원되는 신고 대상을 선택해 주세요.");
    }

    private async Task<TargetDisplay> Target(Report report, CancellationToken token)
    {
        if (report.TransactionId.HasValue) return new("TRANSACTION", await db.Transactions.Where(x => x.Id == report.TransactionId).Select(x => x.PublicId).SingleAsync(token), "거래");
        if (report.ReviewId.HasValue) return new("REVIEW", await db.Reviews.Where(x => x.Id == report.ReviewId).Select(x => x.PublicId).SingleAsync(token), "리뷰");
        if (report.AfterServiceCaseId.HasValue) return new("AFTER_SERVICE", await db.AfterServiceCases.Where(x => x.Id == report.AfterServiceCaseId).Select(x => x.PublicId).SingleAsync(token), "A/S");
        if (report.DisputeCaseId.HasValue) return new("DISPUTE", await db.DisputeCases.Where(x => x.Id == report.DisputeCaseId).Select(x => x.PublicId).SingleAsync(token), "분쟁");
        if (report.ServiceRequestId.HasValue) return new("REQUEST", await db.ServiceRequests.Where(x => x.Id == report.ServiceRequestId).Select(x => x.PublicId).SingleAsync(token), "서비스 요청");
        return new("USER", report.ReportedUserId.HasValue ? await db.Users.Where(x => x.Id == report.ReportedUserId).Select(x => x.PublicId).SingleAsync(token) : Guid.Empty, "사용자");
    }

    private async Task<long> ProviderUser(long providerId, CancellationToken token) => await db.ProviderProfiles.Where(x => x.Id == providerId).Select(x => x.UserId).SingleAsync(token);
    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        var id = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var value = await (from user in db.Users.AsNoTracking() join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId where user.PublicId == id && user.StatusCode == "ACTIVE" select new { user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return value is null ? throw Invalid("CUSTOMER_PROFILE_REQUIRED", "고객 프로필이 필요합니다.", 403) : (value.Id, value.ProfileId);
    }

    private static async Task<ValidatedUpload> Validate(IFormFile upload, CancellationToken token)
    {
        if (upload.Length <= 0 || upload.Length > 10 * 1024 * 1024) throw Invalid("REPORT_FILE_SIZE_INVALID", "파일은 10MB 이하여야 합니다.");
        var rules = new Dictionary<string, (string, byte[])>(StringComparer.OrdinalIgnoreCase) { ["image/jpeg"] = (".jpg", [0xff, 0xd8, 0xff]), ["image/png"] = (".png", [0x89, 0x50, 0x4e, 0x47]), ["application/pdf"] = (".pdf", Encoding.ASCII.GetBytes("%PDF")) };
        if (!rules.TryGetValue(upload.ContentType, out var rule)) throw Invalid("REPORT_FILE_TYPE_INVALID", "JPEG, PNG, PDF 파일만 첨부할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName); if (string.IsNullOrWhiteSpace(name) || name != upload.FileName) throw Invalid("REPORT_FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.");
        await using var source = upload.OpenReadStream(); using var memory = new MemoryStream(); await source.CopyToAsync(memory, token); var bytes = memory.ToArray();
        if (!bytes.AsSpan().StartsWith(rule.Item2)) throw Invalid("REPORT_FILE_SIGNATURE_INVALID", "파일 내용과 형식이 일치하지 않습니다."); return new(bytes, rule.Item1);
    }

    private static string Number(Guid id) => "RPT-" + id.ToString("N")[..8].ToUpperInvariant();
    private static string Display(string status) => status switch { "RECEIVED" => "접수", "UNDER_REVIEW" => "검토 중", "EVIDENCE_REQUESTED" => "증빙 요청", "RESOLVED" => "처리 완료", "CANCELLED" => "취소", _ => "진행 중" };
    private static string Required(string? value, int max) { var result = value?.Trim(); if (string.IsNullOrWhiteSpace(result) || result.Length > max) throw Invalid("REPORT_INPUT_INVALID", "필수 입력값을 확인해 주세요."); return result; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException NotFound() => new("REPORT_NOT_FOUND", "신고를 찾을 수 없습니다.", 404);
    private static WorkBusinessException TargetNotFound() => new("REPORT_TARGET_NOT_FOUND", "신고 대상을 찾을 수 없습니다.", 404);
    private static WorkBusinessException Invalid(string code, string message, int status = 400) => new(code, message, status);
    private sealed record ValidatedUpload(byte[] Bytes, string Extension);
    private sealed record TargetLinks(long? ReportedUserId, long? ServiceRequestId, long? TransactionId, long? ReviewId, long? AfterServiceCaseId, long? DisputeCaseId);
    private sealed record TargetDisplay(string Type, Guid Id, string Display);
}
