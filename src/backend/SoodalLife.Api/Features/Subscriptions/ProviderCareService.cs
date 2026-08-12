using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class ProviderCareService(
    SoodalLifeDbContext db,
    ProviderTradingEligibilityService eligibility,
    CareSubscriptionService core,
    IPrivacyContract privacy,
    IPrivateFileStorage storage)
{
    private const int MaximumFileSize = 10 * 1024 * 1024;
    private static readonly string[] ContactVisitStates = ["SCHEDULED", "RESCHEDULED", "IN_PROGRESS", "PROVIDER_COMPLETED"];

    public async Task<ProviderCareDashboardResponse> Dashboard(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var open = await OpenRequests(identity, token);
        var applicationCount = await db.SubscriptionApplications.CountAsync(x => x.ProviderProfileId == identity.ProviderId, token);
        var waiting = await db.SubscriptionApplications.CountAsync(x => x.ProviderProfileId == identity.ProviderId && x.StatusCode == "SUBMITTED", token);
        var activeContracts = await db.SubscriptionContracts.CountAsync(x => x.ProviderProfileId == identity.ProviderId && x.StatusCode == "ACTIVE", token);
        var todayVisits = await db.SubscriptionVisitSchedules.CountAsync(x => x.ProviderProfileId == identity.ProviderId && x.ScheduledStartAt >= today && x.ScheduledStartAt < tomorrow && x.StatusCode != "CANCELLED" && x.StatusCode != "SKIPPED", token);
        var pendingChanges = await IncomingScheduleChanges(identity, token);
        var waitingConfirmation = await db.SubscriptionVisitSchedules.CountAsync(x => x.ProviderProfileId == identity.ProviderId && x.StatusCode == "PROVIDER_COMPLETED", token);
        var afterServices = await db.AfterServiceCases.CountAsync(x => x.ProviderProfileId == identity.ProviderId && x.StatusCode != "RESOLVED" && x.StatusCode != "UNRESOLVED_CLOSED" && x.StatusCode != "CONVERTED_TO_DISPUTE", token);
        var disputes = await db.DisputeCases.CountAsync(x => x.CounterpartyUserId == identity.UserId && x.StatusCode != "RESOLVED" && x.StatusCode != "CLOSED", token);
        var recent = await VisitQuery(identity.ProviderId).Where(x => x.visit.StatusCode == "COMPLETED").OrderByDescending(x => x.visit.CustomerConfirmedAt).Take(5).ToListAsync(token);
        return new(open.Count, applicationCount, waiting, activeContracts, todayVisits, pendingChanges.Count,
            waitingConfirmation, afterServices, disputes, recent.Select(MapVisit).ToArray());
    }

    public async Task<IReadOnlyList<ProviderCareRequestItem>> OpenRequests(ClaimsPrincipal principal, CancellationToken token) =>
        await OpenRequests(await Provider(principal, token), token);

    public Task<SubscriptionApplicationResponse> Apply(Guid requestId, SubmitSubscriptionApplicationRequest input, ClaimsPrincipal principal, CancellationToken token) =>
        core.Apply(requestId, input, principal, token);

    public async Task<IReadOnlyList<ProviderCareApplicationItem>> Applications(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var rows = await (from app in db.SubscriptionApplications.AsNoTracking()
                          join request in db.SubscriptionRequests.AsNoTracking() on app.SubscriptionRequestId equals request.Id
                          join service in db.ServiceCategories.AsNoTracking() on request.ServiceCategoryId equals service.Id
                          join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                          where app.ProviderProfileId == identity.ProviderId
                          orderby app.SubmittedAt descending
                          select new { app, request, service, area }).ToListAsync(token);
        var applicationIds = rows.Select(x => x.app.Id).ToArray();
        var contracts = await db.SubscriptionContracts.AsNoTracking().Where(x => applicationIds.Contains(x.SubscriptionApplicationId))
            .ToDictionaryAsync(x => x.SubscriptionApplicationId, x => x.PublicId, token);
        return rows.Select(x => new ProviderCareApplicationItem(x.app.PublicId, x.request.PublicId, Number("SR", x.request.PublicId),
            x.service.Name, x.area.AreaName, x.app.ProposedScopeText, x.app.ProposedMonthlyAmount, x.app.ProposedVisitAmount,
            x.app.AvailableScheduleText, x.app.StatusCode, ApplicationStatus(x.app.StatusCode), x.app.SubmittedAt,
            contracts.GetValueOrDefault(x.app.Id), Version(x.app.RowVersion))).ToArray();
    }

    public async Task<IReadOnlyList<ProviderCareContractListItem>> Contracts(ClaimsPrincipal principal, string? status, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var query = ContractQuery(identity.ProviderId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.contract.StatusCode == status.Trim().ToUpperInvariant());
        var rows = await query.OrderByDescending(x => x.contract.StartedAt).ToListAsync(token);
        var result = new List<ProviderCareContractListItem>();
        foreach (var row in rows) result.Add(await MapContract(row, token));
        return result;
    }

    public async Task<ProviderCareContractDetail> Contract(Guid id, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var row = await ContractQuery(identity.ProviderId).SingleOrDefaultAsync(x => x.contract.PublicId == id, token)
            ?? throw NotFound("SUBSCRIPTION_CONTRACT_NOT_FOUND", "구독 계약을 찾을 수 없습니다.");
        var contract = await MapContract(row, token);
        var activeAssignment = row.contract.StatusCode == "ACTIVE" && await db.SubscriptionVisitSchedules.AsNoTracking().AnyAsync(x =>
            x.SubscriptionContractId == row.contract.Id && x.ProviderProfileId == identity.ProviderId && ContactVisitStates.Contains(x.StatusCode), token);
        var allowed = privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.Phone, activeAssignment).CanAccess &&
                      privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.DetailAddress, activeAssignment).CanAccess;
        return new(contract, row.contract.ServiceScopeSnapshotJson, row.contract.RecurrenceSnapshotJson,
            row.contract.ProviderTrustScoreSnapshot, BillingDisplay(row.contract.BillingStatusCode), "정산 기능 준비 중",
            allowed, allowed ? row.user.Phone : null, allowed ? row.request.DetailAddress : null,
            allowed ? "현재 계약 공급자와 유효 회차에 한해 업무 연락처가 공개됩니다." : "업무 배정이 유효하지 않아 연락처와 상세주소가 비공개입니다.");
    }

    public async Task<IReadOnlyList<ProviderCareVisitListItem>> Visits(ClaimsPrincipal principal, string? filter, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var query = VisitQuery(identity.ProviderId);
        var code = filter?.Trim().ToUpperInvariant();
        var today = DateTime.UtcNow.Date;
        query = code switch
        {
            "TODAY" => query.Where(x => x.visit.ScheduledStartAt >= today && x.visit.ScheduledStartAt < today.AddDays(1)),
            "UPCOMING" => query.Where(x => x.visit.ScheduledStartAt >= DateTime.UtcNow && (x.visit.StatusCode == "SCHEDULED" || x.visit.StatusCode == "RESCHEDULED")),
            "IN_PROGRESS" => query.Where(x => x.visit.StatusCode == "IN_PROGRESS"),
            "WAITING_CUSTOMER" => query.Where(x => x.visit.StatusCode == "PROVIDER_COMPLETED"),
            "COMPLETED" => query.Where(x => x.visit.StatusCode == "COMPLETED"),
            "SKIPPED" => query.Where(x => x.visit.StatusCode == "SKIPPED"),
            "CANCELLED" => query.Where(x => x.visit.StatusCode == "CANCELLED"),
            "DISPUTED" => query.Where(x => x.visit.StatusCode == "DISPUTED"),
            _ => query
        };
        return (await query.OrderByDescending(x => x.visit.ScheduledStartAt).Take(300).ToListAsync(token)).Select(MapVisit).ToArray();
    }

    public async Task<ProviderCareVisitDetail> Visit(Guid id, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var row = await VisitQuery(identity.ProviderId).SingleOrDefaultAsync(x => x.visit.PublicId == id, token)
            ?? throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "구독 회차를 찾을 수 없습니다.");
        var currentAssignment = row.contract.ProviderProfileId == identity.ProviderId && row.contract.StatusCode == "ACTIVE" && ContactVisitStates.Contains(row.visit.StatusCode);
        var allowed = privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.Phone, currentAssignment).CanAccess &&
                      privacy.Decide(PrivacyAudience.AssignedProvider, PrivacyField.DetailAddress, currentAssignment).CanAccess;
        var policy = CompletionPolicy(row.contract.CompletionPolicySnapshotJson);
        var files = await (from link in db.SubscriptionVisitFiles.AsNoTracking()
                           join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                           where link.SubscriptionVisitScheduleId == row.visit.Id && file.StatusCode == "ACTIVE"
                           orderby link.DisplayOrder
                           select new ProviderCareVisitFile(file.PublicId, SafeFileName(file.ContentType), file.ContentType, file.SizeBytes,
                               file.MalwareScanStatusCode ?? FilePrivacyCodes.NotIntegrated, file.PrivacyInspectionStatusCode ?? FilePrivacyCodes.NotIntegrated,
                               file.SanitizationStatusCode ?? FilePrivacyCodes.NotIntegrated,
                               $"/api/v1/providers/me/care/visits/{row.visit.PublicId}/files/{file.PublicId}")).ToListAsync(token);
        return new(MapVisit(row), row.contract.ServiceScopeSnapshotJson, policy.EvidenceRule, policy.RequiredPhotoCount,
            policy.RequiresChecklist, policy.RequiresVisitVerification, row.visit.GpsVerificationStatusCode,
            row.visit.PossessionVerificationStatusCode, row.visit.VisitVerificationStatusCode, row.visit.WorkStartedAt,
            row.visit.WorkCompletedAt, row.visit.CompletionChecklistJson, row.visit.CompletionNote, allowed,
            allowed ? row.user.Phone : null, allowed ? row.request.DetailAddress : null,
            allowed ? "현재 계약 공급자와 유효 회차에 한해 공개됩니다." : "교체·종료 또는 비유효 회차이므로 개인정보가 비공개입니다.",
            files, await db.AfterServiceCases.AsNoTracking().Where(x => x.SubscriptionVisitScheduleId == row.visit.Id).Select(x => (Guid?)x.PublicId).FirstOrDefaultAsync(token),
            await db.DisputeCases.AsNoTracking().Where(x => x.SubscriptionVisitScheduleId == row.visit.Id).Select(x => (Guid?)x.PublicId).FirstOrDefaultAsync(token),
            row.visit.StatusCode switch { "PROVIDER_COMPLETED" => "고객 확인 대기", "COMPLETED" => "고객 확인 완료", "DISPUTED" => "분쟁 전환", _ => "완료보고 전" });
    }

    public async Task<IReadOnlyList<ProviderCareScheduleChangeItem>> ScheduleChanges(ClaimsPrincipal principal, CancellationToken token) =>
        await IncomingScheduleChanges(await Provider(principal, token), token);

    public Task<SubscriptionScheduleChangeResponse> RequestScheduleChange(Guid visitId, RequestScheduleChangeRequest input, ClaimsPrincipal principal, CancellationToken token) =>
        core.RequestScheduleChange(visitId, input, principal, token);

    public Task<SubscriptionScheduleChangeResponse> DecideScheduleChange(Guid id, DecideScheduleChangeRequest input, ClaimsPrincipal principal, CancellationToken token) =>
        core.DecideScheduleChange(id, input, principal, token);

    public async Task<ProviderCareVisitDetail> Start(Guid id, StartProviderCareVisitRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var row = await (from visit in db.SubscriptionVisitSchedules
                         join contract in db.SubscriptionContracts on visit.SubscriptionContractId equals contract.Id
                         where visit.PublicId == id && visit.ProviderProfileId == identity.ProviderId
                         select new { visit, contract }).SingleOrDefaultAsync(token)
            ?? throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "구독 회차를 찾을 수 없습니다.");
        var existing = await db.SubscriptionEvents.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (existing is not null)
        {
            if (existing.SubscriptionVisitScheduleId != row.visit.Id || existing.EventTypeCode != "VISIT_STARTED")
                throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 업무에 사용된 중복 방지 키입니다.");
            return await Visit(id, principal, token);
        }
        if (row.contract.StatusCode != "ACTIVE") throw Conflict("SUBSCRIPTION_CONTRACT_NOT_ACTIVE", "활성 구독 계약의 회차만 시작할 수 있습니다.");
        if (row.contract.ProviderProfileId != identity.ProviderId) throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "현재 계약 공급자의 회차만 시작할 수 있습니다.");
        if (row.visit.StatusCode is not ("SCHEDULED" or "RESCHEDULED")) throw Conflict("SUBSCRIPTION_VISIT_START_INVALID", "예정 또는 변경 확정 회차만 시작할 수 있습니다.");
        if (row.visit.ScheduledEndAt.HasValue && row.visit.ScheduledEndAt <= row.visit.ScheduledStartAt) throw Conflict("SUBSCRIPTION_VISIT_SCHEDULE_INVALID", "유효한 일정이 없는 회차는 시작할 수 없습니다.");
        ApplyVersion(row.visit, input.RowVersion);
        var now = DateTime.UtcNow;
        var before = row.visit.StatusCode;
        row.visit.StatusCode = "IN_PROGRESS";
        row.visit.WorkStartedAt = now;
        row.visit.UpdatedAt = now;
        row.visit.UpdatedByUserId = identity.UserId;
        db.SubscriptionEvents.Add(new SubscriptionEvent { SubscriptionRequestId = row.contract.SubscriptionRequestId,
            SubscriptionContractId = row.contract.Id, SubscriptionVisitScheduleId = row.visit.Id, EventTypeCode = "VISIT_STARTED",
            EventDataJson = JsonSerializer.Serialize(new { from = before, to = row.visit.StatusCode, startedAt = now }), OccurredAt = now,
            ActorUserId = identity.UserId, IdempotencyKey = input.IdempotencyKey.Trim() });
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = identity.UserId, ActorRoleCode = RoleCodes.Provider,
            ActionCode = "SUBSCRIPTION_VISIT_STARTED", EntityType = "SubscriptionVisitSchedule", EntityPublicId = row.visit.PublicId,
            ResultCode = "SUCCESS", BeforeJson = JsonSerializer.Serialize(new { status = before }),
            AfterJson = JsonSerializer.Serialize(new { status = row.visit.StatusCode, startedAt = now }) });
        await AddOutboxIfTemplate(row.visit.PublicId, "SubscriptionVisitSchedule", "SUBSCRIPTION_VISIT_STARTED",
            await db.CustomerProfiles.Where(x => x.Id == row.contract.CustomerProfileId).Select(x => x.UserId).SingleAsync(token),
            identity.UserId, input.IdempotencyKey, now, token);
        await SaveConcurrent(token);
        return await Visit(id, principal, token);
    }

    public Task<SubscriptionVisitResponse> Complete(Guid id, CompleteSubscriptionVisitRequest input, ClaimsPrincipal principal, CancellationToken token) =>
        core.CompleteVisit(id, input, principal, token);

    public async Task<ProviderCareUploadResponse> Upload(Guid visitId, ClaimsPrincipal principal, IFormFile upload, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var visit = await (from item in db.SubscriptionVisitSchedules.AsNoTracking()
                           join contract in db.SubscriptionContracts.AsNoTracking() on item.SubscriptionContractId equals contract.Id
                           where item.PublicId == visitId && item.ProviderProfileId == identity.ProviderId && contract.ProviderProfileId == identity.ProviderId && contract.StatusCode == "ACTIVE"
                           select item).SingleOrDefaultAsync(token) ?? throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "구독 회차를 찾을 수 없습니다.");
        if (visit.StatusCode != "IN_PROGRESS") throw Conflict("SUBSCRIPTION_COMPLETION_UPLOAD_INVALID", "진행 중인 회차에만 완료 증빙을 등록할 수 있습니다.");
        var validated = await ValidateUpload(upload, token);
        var now = DateTime.UtcNow;
        var key = $"subscription-completion/{visit.PublicId:N}/{Guid.NewGuid():N}{validated.Extension}";
        var file = new StoredFile { PurposeCode = "SUBSCRIPTION_COMPLETION", StorageContainer = "development-private", StorageKey = key,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = Path.GetFileName(upload.FileName),
            ContentType = upload.ContentType.ToLowerInvariant(), SizeBytes = validated.Bytes.Length,
            Sha256Hex = Convert.ToHexString(SHA256.HashData(validated.Bytes)).ToLowerInvariant(), StatusCode = "PENDING",
            UploadedByUserId = identity.UserId, MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
            PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated, SanitizationStatusCode = FilePrivacyCodes.NotIntegrated,
            ScanResultText = "NOT_INTEGRATED", CreatedAt = now };
        db.Files.Add(file);
        await db.SaveChangesAsync(token);
        try
        {
            await using var stream = new MemoryStream(validated.Bytes);
            await storage.SaveAsync(key, stream, token);
            file.StatusCode = "ACTIVE";
            file.ActivatedAt = now;
            db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = identity.UserId, ActorRoleCode = RoleCodes.Provider,
                ActionCode = "SUBSCRIPTION_COMPLETION_FILE_UPLOADED", EntityType = "StoredFile", EntityPublicId = file.PublicId,
                ResultCode = "SUCCESS", AfterJson = JsonSerializer.Serialize(new { visitId, fileId = file.PublicId, file.SizeBytes, file.ContentType }) });
            await db.SaveChangesAsync(token);
            return new(file.PublicId, SafeFileName(file.ContentType), file.ContentType, file.SizeBytes,
                file.MalwareScanStatusCode, file.PrivacyInspectionStatusCode, file.SanitizationStatusCode);
        }
        catch
        {
            await storage.DeleteIfExistsAsync(key, token);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenFile(Guid visitId, Guid fileId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var file = await (from visit in db.SubscriptionVisitSchedules.AsNoTracking()
                          join link in db.SubscriptionVisitFiles.AsNoTracking() on visit.Id equals link.SubscriptionVisitScheduleId
                          join value in db.Files.AsNoTracking() on link.FileId equals value.Id
                          where visit.PublicId == visitId && visit.ProviderProfileId == identity.ProviderId && value.PublicId == fileId && value.StatusCode == "ACTIVE" && value.UploadedByUserId == identity.UserId
                          select value).SingleOrDefaultAsync(token) ?? throw NotFound("SUBSCRIPTION_VISIT_FILE_NOT_FOUND", "회차 증빙파일을 찾을 수 없습니다.");
        return (await storage.OpenReadAsync(file.StorageKey, token), file.ContentType, SafeFileName(file.ContentType));
    }

    private async Task<IReadOnlyList<ProviderCareRequestItem>> OpenRequests(ProviderIdentity identity, CancellationToken token)
    {
        var rows = await (from request in db.SubscriptionRequests.AsNoTracking()
                          join service in db.ServiceCategories.AsNoTracking() on request.ServiceCategoryId equals service.Id
                          join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                          join rule in db.SubscriptionRecurrenceRules.AsNoTracking() on request.Id equals rule.SubscriptionRequestId
                          where request.StatusCode == "OPEN"
                          orderby request.CreatedAt descending
                          select new { request, service, area, rule }).Take(300).ToListAsync(token);
        var result = new List<ProviderCareRequestItem>();
        foreach (var row in rows)
        {
            var decision = await eligibility.EvaluateAsync(identity.ProviderId, row.request.ServiceCategoryId, row.request.AdministrativeAreaId, token);
            if (!decision.IsEligible) continue;
            result.Add(new(row.request.PublicId, Number("SR", row.request.PublicId), row.service.PublicId, row.service.Name, row.area.AreaName,
                row.request.RequestTypeCode, row.request.RequestedScopeText, row.request.PreferredStartDate, MapRule(row.rule), row.request.StatusCode,
                await db.SubscriptionApplications.AsNoTracking().AnyAsync(x => x.SubscriptionRequestId == row.request.Id && x.ProviderProfileId == identity.ProviderId, token), row.request.CreatedAt));
        }
        return result;
    }

    private async Task<IReadOnlyList<ProviderCareScheduleChangeItem>> IncomingScheduleChanges(ProviderIdentity identity, CancellationToken token)
    {
        var rows = await (from change in db.SubscriptionScheduleChanges.AsNoTracking()
                          join visit in db.SubscriptionVisitSchedules.AsNoTracking() on change.SubscriptionVisitScheduleId equals visit.Id
                          join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id
                          join request in db.SubscriptionRequests.AsNoTracking() on contract.SubscriptionRequestId equals request.Id
                          join service in db.ServiceCategories.AsNoTracking() on contract.ServiceCategoryId equals service.Id
                          join customer in db.CustomerProfiles.AsNoTracking() on contract.CustomerProfileId equals customer.Id
                          where visit.ProviderProfileId == identity.ProviderId && (contract.ProviderProfileId == identity.ProviderId || visit.ScheduledStartAt < DateTime.UtcNow)
                          orderby change.RequestedAt descending
                          select new { change, visit, contract, service, customer.UserId }).Take(300).ToListAsync(token);
        return rows.Select(x =>
        {
            var old = JsonSerializer.Deserialize<ScheduleValue>(x.change.OldScheduleJson)!;
            var next = JsonSerializer.Deserialize<ScheduleValue>(x.change.NewScheduleJson)!;
            return new ProviderCareScheduleChangeItem(x.change.PublicId, x.visit.PublicId, Number("SC", x.contract.PublicId), x.service.Name,
                old.start, old.end, next.start, next.end, x.change.Reason, x.change.StatusCode,
                x.change.StatusCode == "REQUESTED" && x.change.RequestedByUserId == x.UserId, x.change.RequestedAt, Version(x.change.RowVersion));
        }).ToArray();
    }

    private IQueryable<ContractRow> ContractQuery(long providerId) =>
        from contract in db.SubscriptionContracts.AsNoTracking()
        join request in db.SubscriptionRequests.AsNoTracking() on contract.SubscriptionRequestId equals request.Id
        join customer in db.CustomerProfiles.AsNoTracking() on contract.CustomerProfileId equals customer.Id
        join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
        join service in db.ServiceCategories.AsNoTracking() on contract.ServiceCategoryId equals service.Id
        join application in db.SubscriptionApplications.AsNoTracking() on contract.SubscriptionApplicationId equals application.Id
        where contract.ProviderProfileId == providerId
        select new ContractRow { contract = contract, request = request, customer = customer, user = user, service = service, application = application };

    private IQueryable<VisitRow> VisitQuery(long providerId) =>
        from visit in db.SubscriptionVisitSchedules.AsNoTracking()
        join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id
        join request in db.SubscriptionRequests.AsNoTracking() on contract.SubscriptionRequestId equals request.Id
        join customer in db.CustomerProfiles.AsNoTracking() on contract.CustomerProfileId equals customer.Id
        join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
        join service in db.ServiceCategories.AsNoTracking() on contract.ServiceCategoryId equals service.Id
        where visit.ProviderProfileId == providerId
        select new VisitRow { visit = visit, contract = contract, request = request, customer = customer, user = user, service = service };

    private async Task<ProviderCareContractListItem> MapContract(ContractRow row, CancellationToken token)
    {
        var next = await db.SubscriptionVisitSchedules.AsNoTracking().Where(x => x.SubscriptionContractId == row.contract.Id && x.ProviderProfileId == row.contract.ProviderProfileId && x.ScheduledStartAt >= DateTime.UtcNow && x.StatusCode != "CANCELLED" && x.StatusCode != "SKIPPED")
            .OrderBy(x => x.ScheduledStartAt).Select(x => (DateTime?)x.ScheduledStartAt).FirstOrDefaultAsync(token);
        return new(row.contract.PublicId, Number("SC", row.contract.PublicId), row.service.Name, row.customer.DisplayName,
            row.contract.StatusCode, ContractStatus(row.contract.StatusCode), row.contract.StartedAt, row.contract.EndedAt,
            row.contract.TerminationRequestedAt, row.application.ProposedMonthlyAmount, row.application.ProposedVisitAmount,
            next, Version(row.contract.RowVersion));
    }

    private ProviderCareVisitListItem MapVisit(VisitRow row) => new(row.visit.PublicId, row.contract.PublicId,
        Number("SC", row.contract.PublicId), row.service.Name, row.visit.VisitNo, row.visit.ScheduledStartAt,
        row.visit.ScheduledEndAt, row.visit.StatusCode, VisitStatus(row.visit.StatusCode),
        db.SubscriptionScheduleChanges.Any(x => x.SubscriptionVisitScheduleId == row.visit.Id && x.StatusCode == "REQUESTED"),
        row.visit.VisitVerificationStatusCode, row.visit.ProviderCompletionSubmittedAt.HasValue,
        row.visit.CustomerConfirmedAt.HasValue, Version(row.visit.RowVersion));

    private async Task AddOutboxIfTemplate(Guid aggregateId, string aggregate, string eventType, long recipient, long actor, string sourceKey, DateTime now, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token)) return;
        var key = $"provider-care:{eventType.ToLowerInvariant()}:{sourceKey.Trim()}";
        if (await db.OutboxEvents.AsNoTracking().AnyAsync(x => x.IdempotencyKey == key, token)) return;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = aggregate, AggregatePublicId = aggregateId, EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(new { recipientUserId = recipient, sourceId = aggregateId, source_no = aggregateId.ToString("N")[..8].ToUpperInvariant() }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }

    private async Task<ProviderIdentity> Provider(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) throw Forbidden("AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");
        return await (from user in db.Users.AsNoTracking()
                      join provider in db.ProviderProfiles.AsNoTracking() on user.Id equals provider.UserId
                      where user.PublicId == id && user.StatusCode == "ACTIVE"
                      select new ProviderIdentity(user.Id, provider.Id)).SingleOrDefaultAsync(token)
               ?? throw Forbidden("PROVIDER_PROFILE_REQUIRED", "공급자 프로필이 필요합니다.");
    }

    private void ApplyVersion(SubscriptionVisitSchedule entity, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (entity.RowVersion.Length > 0) throw Bad("ROW_VERSION_REQUIRED", "최신 변경 버전 값이 필요합니다.");
            return;
        }
        try { db.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); }
        catch (FormatException) { throw Bad("ROW_VERSION_INVALID", "변경 버전 값이 올바르지 않습니다."); }
    }

    private async Task SaveConcurrent(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw Conflict("ROW_VERSION_CONFLICT", "다른 사용자가 먼저 변경했습니다. 새로고침 후 다시 시도해 주세요."); }
    }

    private static async Task<ValidatedUpload> ValidateUpload(IFormFile upload, CancellationToken token)
    {
        if (upload.Length is <= 0 or > MaximumFileSize) throw Bad("SUBSCRIPTION_COMPLETION_FILE_SIZE_INVALID", "완료 증빙은 10MB 이하만 등록할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(name) || name != upload.FileName || name.Length > 255) throw Bad("SUBSCRIPTION_COMPLETION_FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var validType = (upload.ContentType.ToLowerInvariant(), extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("application/pdf", ".pdf") => true,
            _ => false
        };
        if (!validType) throw Bad("SUBSCRIPTION_COMPLETION_FILE_TYPE_INVALID", "JPG, PNG, PDF 파일만 등록할 수 있습니다.");
        await using var input = upload.OpenReadStream();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory, token);
        var bytes = memory.ToArray();
        var validSignature = upload.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "application/pdf" => bytes.Length >= 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-",
            _ => false
        };
        if (!validSignature) throw Bad("SUBSCRIPTION_COMPLETION_FILE_SIGNATURE_INVALID", "파일 확장자와 실제 형식이 일치하지 않습니다.");
        return new(bytes, extension);
    }

    private static CompletionPolicyValue CompletionPolicy(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new(root.TryGetProperty("requiredPhotoCount", out var photos) ? photos.GetInt32() : 0,
                root.TryGetProperty("requiresChecklist", out var checklist) && checklist.GetBoolean(),
                root.TryGetProperty("requiresVisitVerification", out var verification) && verification.GetBoolean(),
                root.TryGetProperty("evidenceRule", out var evidence) && evidence.ValueKind == JsonValueKind.String ? evidence.GetString() ?? "정책 미설정" : "정책 미설정");
        }
        catch { return new(0, false, false, "정책 미설정"); }
    }

    private static SubscriptionRecurrenceResponse MapRule(SubscriptionRecurrenceRule value)
    {
        IReadOnlyList<int> weekdays;
        try { weekdays = string.IsNullOrWhiteSpace(value.WeekdaysJson) ? [] : JsonSerializer.Deserialize<int[]>(value.WeekdaysJson) ?? []; }
        catch { weekdays = []; }
        return new(value.FrequencyTypeCode, value.IntervalValue, value.VisitsPerPeriod, weekdays,
            value.PreferredTimeFrom, value.PreferredTimeTo, value.ExpectedDurationMinutes, value.StartDate, value.EndDate);
    }

    private static string ContractStatus(string value) => value switch { "ACTIVE" => "이용 중", "PAUSED" => "일시정지", "TERMINATION_REQUESTED" => "해지 처리 대기", "TERMINATED" => "해지 완료", _ => value };
    private static string ApplicationStatus(string value) => value switch { "SUBMITTED" => "고객 선택 대기", "SELECTED" => "선택됨", "NOT_SELECTED" => "미선택", "WITHDRAWN" => "종료", _ => value };
    private static string VisitStatus(string value) => value switch { "SCHEDULED" => "방문 예정", "RESCHEDULED" => "변경 일정 확정", "IN_PROGRESS" => "진행 중", "PROVIDER_COMPLETED" => "고객 확인 대기", "COMPLETED" => "완료", "SKIPPED" => "건너뜀", "PAUSED" => "일시정지", "CANCELLED" => "취소", "DISPUTED" => "분쟁", _ => value };
    private static string BillingDisplay(string? value) => value switch { "PAID" => "결제 확인", "FAILED" => "결제 확인 필요", null or "" => "결제 연동 준비 중", _ => "결제 상태 확인 중" };
    private static string SafeFileName(string contentType) => "completion-evidence" + (contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", "application/pdf" => ".pdf", _ => ".bin" });
    private static string Number(string prefix, Guid id) => $"{prefix}-{id.ToString("N")[..8].ToUpperInvariant()}";
    private static string Version(byte[] value) => value.Length == 0 ? string.Empty : Convert.ToBase64String(value);
    private static SubscriptionBusinessException Bad(string code, string message) => new(400, code, message);
    private static SubscriptionBusinessException Forbidden(string code, string message) => new(403, code, message);
    private static SubscriptionBusinessException NotFound(string code, string message) => new(404, code, message);
    private static SubscriptionBusinessException Conflict(string code, string message) => new(409, code, message);

    private sealed record ProviderIdentity(long UserId, long ProviderId);
    private sealed record ScheduleValue(DateTime start, DateTime? end);
    private sealed record CompletionPolicyValue(int RequiredPhotoCount, bool RequiresChecklist, bool RequiresVisitVerification, string EvidenceRule);
    private sealed record ValidatedUpload(byte[] Bytes, string Extension);
    private sealed class ContractRow { public required SubscriptionContract contract { get; init; } public required SubscriptionRequest request { get; init; } public required CustomerProfile customer { get; init; } public required User user { get; init; } public required ServiceCategory service { get; init; } public required SubscriptionApplication application { get; init; } }
    private sealed class VisitRow { public required SubscriptionVisitSchedule visit { get; init; } public required SubscriptionContract contract { get; init; } public required SubscriptionRequest request { get; init; } public required CustomerProfile customer { get; init; } public required User user { get; init; } public required ServiceCategory service { get; init; } }
}
