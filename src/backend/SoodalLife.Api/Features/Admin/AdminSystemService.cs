using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;
using System.Diagnostics;

namespace SoodalLife.Api.Features.Admin;

public sealed partial class AdminSystemService(SoodalLifeDbContext db,AdminSecurityService security,IOptions<SubscriptionPaymentGatewayOptions> paymentOptions)
{
    private static readonly TimeSpan KoreaOffset = TimeSpan.FromHours(9);
    private static readonly string[] SensitiveKeys =
    [
        "password", "passwd", "token", "secret", "apikey", "api_key", "authorization", "cookie",
        "card", "cvc", "accountnumber", "account_number", "accountpassword", "account_password",
        "phone", "email", "detailaddress", "detail_address", "businessregistration", "business_registration",
        "documentnumber", "document_number", "storagekey", "storage_key"
    ];

    public async Task<AdminAuditLogResponse> GetAuditLogsAsync(AdminAuditLogQuery input, CancellationToken token)
    {
        if (input.Page < 1 || input.PageSize is < 1 or > 100)
            throw new AdminSystemException("AUDIT_PAGE_INVALID", "페이지 값은 1 이상, 페이지 크기는 1~100이어야 합니다.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Add(KoreaOffset));
        var from = input.From ?? today.AddDays(-29);
        var to = input.To ?? today;
        if (to < from || to.DayNumber - from.DayNumber > 366)
            throw new AdminSystemException("AUDIT_PERIOD_INVALID", "조회 기간은 최대 366일이며 종료일은 시작일보다 빠를 수 없습니다.");

        var start = from.ToDateTime(TimeOnly.MinValue).Subtract(KoreaOffset);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue).Subtract(KoreaOffset);
        long? actorId = null;
        if (input.AdminId.HasValue)
        {
            actorId = await db.Users.AsNoTracking().Where(x => x.PublicId == input.AdminId).Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
            if (!actorId.HasValue) actorId = -1;
        }

        var query = db.AuditLogs.AsNoTracking().Where(x => x.OccurredAt >= start && x.OccurredAt < end);
        if (actorId.HasValue) query = query.Where(x => x.ActorUserId == actorId);
        if (!string.IsNullOrWhiteSpace(input.Area)) query = query.Where(x => x.EntityType == input.Area.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(input.Action)) query = query.Where(x => x.ActionCode == input.Action.Trim().ToUpperInvariant());

        var total = await query.CountAsync(token);
        var rows = await (from audit in query
                          join actor0 in db.Users.AsNoTracking() on audit.ActorUserId equals actor0.Id into actors
                          from actor in actors.DefaultIfEmpty()
                          orderby audit.OccurredAt descending, audit.Id descending
                          select new
                          {
                              audit.OccurredAt,
                              AdminId = actor == null ? (Guid?)null : actor.PublicId,
                              AdminLoginId = actor == null ? "SYSTEM" : actor.LoginId,
                              audit.ActorRoleCode,
                              audit.EntityType,
                              audit.ActionCode,
                              audit.EntityPublicId,
                              audit.Reason,
                              audit.CorrelationId,
                              audit.ResultCode,
                              audit.BeforeJson,
                              audit.AfterJson,
                          })
            .Skip((input.Page - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync(token);

        var administrators = await (from user in db.Users.AsNoTracking()
                                    join link in db.UserRoles.AsNoTracking() on user.Id equals link.UserId
                                    join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                                    where role.Code == RoleCodes.Admin && link.RevokedAt == null
                                    orderby user.LoginId
                                    select new AdminAuditFilterOption(user.PublicId.ToString(), user.LoginId)).ToListAsync(token);
        var areaValues = await db.AuditLogs.AsNoTracking().Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync(token);
        var actionValues = await db.AuditLogs.AsNoTracking().Select(x => x.ActionCode).Distinct().OrderBy(x => x).ToListAsync(token);
        var areas = areaValues.Select(x => new AdminAuditFilterOption(x, x)).ToArray();
        var actions = actionValues.Select(x => new AdminAuditFilterOption(x, x)).ToArray();

        return new(total, input.Page, input.PageSize,
            rows.Select(x => new AdminAuditLogItem(x.OccurredAt, x.AdminId, x.AdminLoginId, x.ActorRoleCode,
                AreaLabel(x.EntityType), x.ActionCode, x.EntityType, x.EntityPublicId, MaskText(x.Reason),
                x.CorrelationId, x.ResultCode, MaskJson(x.BeforeJson), MaskJson(x.AfterJson))).ToArray(),
            administrators, areas, actions);
    }

    public async Task<AdminSystemStatusResponse> GetSystemStatusAsync(CancellationToken token)
    {
        var canConnect = await db.Database.CanConnectAsync(token);
        IReadOnlyList<string> pendingMigrations;
        string migrationStatus;
        if (db.Database.IsRelational())
        {
            pendingMigrations = (await db.Database.GetPendingMigrationsAsync(token)).ToArray();
            migrationStatus = pendingMigrations.Count == 0 ? "CURRENT" : "PENDING";
        }
        else
        {
            pendingMigrations = [];
            migrationStatus = "NOT_APPLICABLE";
        }

        var outboxRows = await db.OutboxEvents.AsNoTracking().GroupBy(x => x.StatusCode)
            .Select(x => new { Status = x.Key, Count = x.LongCount() }).ToListAsync(token);
        var outboxStatuses = outboxRows.OrderBy(x => x.Status).Select(x => new AdminOutboxStatus(x.Status, x.Count)).ToArray();
        var failures = await db.OutboxEvents.AsNoTracking().Where(x => x.StatusCode == "FAILED")
            .OrderByDescending(x => x.LastAttemptAt ?? x.OccurredAt).Take(20)
            .Select(x => new { x.PublicId, x.AggregateType, x.EventType, x.AttemptCount, x.LastAttemptAt, x.ErrorMessage })
            .ToListAsync(token);

        var failedNotifications = await db.NotificationDeliveries.AsNoTracking().LongCountAsync(x => x.StatusCode == "FAILED", token);
        var failedPayments = await db.SubscriptionPaymentRequests.AsNoTracking().LongCountAsync(x => x.StatusCode == "FAILED", token);
        var heldSettlements = await db.SubscriptionSettlementItems.AsNoTracking().LongCountAsync(x => x.StatusCode == "HOLD" || x.StatusCode == "POLICY_PENDING", token);
        var pendingReports = await db.Reports.AsNoTracking().LongCountAsync(x => x.StatusCode == "RECEIVED" || x.StatusCode == "UNDER_REVIEW" || x.StatusCode == "EVIDENCE_REQUESTED", token);
        var pendingDisputes = await db.DisputeCases.AsNoTracking().LongCountAsync(x => x.StatusCode == "OPEN" || x.StatusCode == "UNDER_REVIEW" || x.StatusCode == "WAITING_CUSTOMER" || x.StatusCode == "WAITING_PROVIDER", token);
        var failedOutbox = outboxStatuses.Where(x => x.Status == "FAILED").Sum(x => x.Count);
        var pendingOutbox = outboxStatuses.Where(x => x.Status == "PENDING" || x.Status == "PROCESSING").Sum(x => x.Count);

        var admins = await (from user in db.Users.AsNoTracking()
                            join link in db.UserRoles.AsNoTracking() on user.Id equals link.UserId
                            join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                            where role.Code == RoleCodes.Admin && link.RevokedAt == null
                            orderby user.LoginId
                            select new { user.Id, user.PublicId, user.LoginId, user.StatusCode, user.CreatedAt, user.LastLoginAt }).ToListAsync(token);
        var adminIds = admins.Select(x => x.Id).ToArray();
        var roleRows = await (from link in db.UserRoles.AsNoTracking()
                              join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                              where adminIds.Contains(link.UserId) && link.RevokedAt == null && role.IsActive
                              select new { link.UserId, role.Code }).ToListAsync(token);
        var jobRows = await db.ScheduledJobLeases.AsNoTracking().OrderBy(x => x.JobName).ToListAsync(token);
        var expiredPasswordResets = await db.PasswordResetRequests.AsNoTracking().LongCountAsync(x => x.UsedAt == null && x.ExpiresAt <= DateTime.UtcNow, token);
        var withdrawalCandidates = await db.CustomerWithdrawalRequests.AsNoTracking().LongCountAsync(x => x.StatusCode == "APPROVED" || x.StatusCode == "COMPLETED", token);
        var providerExitCandidates = await db.ProviderExitRequests.AsNoTracking().LongCountAsync(x => x.StatusCode == "COMPLETED", token);
        var integrations=await IntegrationStatuses(token);
        var process=Process.GetCurrentProcess();
        var telemetrySince=DateTime.UtcNow.AddHours(-24);
        var telemetry=await db.AnalyticsEvents.AsNoTracking().Where(x=>x.EventTypeCode=="API_REQUEST"&&x.OccurredAt>=telemetrySince).GroupBy(_=>1).Select(g=>new{Count=g.LongCount(),Errors=g.LongCount(x=>x.StatusCode>=500),Average=g.Average(x=>(double?)(x.DurationMs??0))}).SingleOrDefaultAsync(token);
        long diskFree=0;try{var root=Path.GetPathRoot(AppContext.BaseDirectory);if(!string.IsNullOrWhiteSpace(root))diskFree=new DriveInfo(root).AvailableFreeSpace/1024/1024;}catch(IOException){}
        var runtime=new AdminRuntimeStatus(process.WorkingSet64/1024/1024,GC.GetTotalMemory(false)/1024/1024,process.Threads.Count,(DateTime.UtcNow-process.StartTime.ToUniversalTime()).TotalHours,diskFree,telemetry?.Count??0,telemetry is null||telemetry.Count==0?null:Math.Round((decimal)telemetry.Errors/telemetry.Count*100,2),telemetry?.Average is null?null:Math.Round((decimal)telemetry.Average.Value,1));
        var recentRuns=await db.ScheduledJobRuns.AsNoTracking().OrderByDescending(x=>x.StartedAt).Take(100).Select(x=>new AdminJobRunItem(x.PublicId,x.JobName,x.StatusCode,x.StartedAt,x.CompletedAt,x.ProcessedCount,x.FailedCount,x.ErrorCode)).ToListAsync(token);
        var retentionPolicies=await db.DataRetentionPolicies.AsNoTracking().OrderBy(x=>x.DomainCode).Select(x=>new AdminRetentionPolicyItem(x.PublicId,x.DomainCode,x.ActionCode,x.RetentionDays,x.LegalHoldDays,x.IsEnabled,x.DryRun,x.UpdatedAt)).ToListAsync(token);

        return new(DateTime.UtcNow,
            new(canConnect ? "CONNECTED" : "UNAVAILABLE", migrationStatus, pendingMigrations.Count, pendingMigrations),
            [
                Metric("outbox_pending", "Outbox 처리대기", pendingOutbox, pendingOutbox > 0 ? "WARNING" : "NORMAL", "/admin/system"),
                Metric("outbox_failed", "Outbox 실패", failedOutbox, failedOutbox > 0 ? "CRITICAL" : "NORMAL", "/admin/system"),
                Metric("notification_failed", "Notification 실패", failedNotifications, failedNotifications > 0 ? "WARNING" : "NORMAL", "/admin/notifications"),
                Metric("payment_failed", "내부 결제 Workflow 실패", failedPayments, failedPayments > 0 ? "CRITICAL" : "NORMAL", "/admin/subscriptions"),
                Metric("settlement_hold", "정산 HOLD·정책대기", heldSettlements, heldSettlements > 0 ? "CRITICAL" : "NORMAL", "/admin/subscriptions"),
                Metric("reports_pending", "처리대기 신고", pendingReports, pendingReports > 0 ? "WARNING" : "NORMAL", "/admin/reports"),
                Metric("disputes_pending", "미해결 분쟁", pendingDisputes, pendingDisputes > 0 ? "CRITICAL" : "NORMAL", "/admin/disputes"),
            ],
            outboxStatuses,
            failures.Select(x => new AdminOutboxFailure(x.PublicId, x.AggregateType, x.EventType, x.AttemptCount, x.LastAttemptAt, MaskText(x.ErrorMessage))).ToArray(),
            integrations,
            [
                new("가격정책", "CategoryPricePolicy", "/admin/pricing", "기존 버전형 가격정책 사용"),
                new("수수료정책", "CategoryFeePolicy", "/admin/pricing", "기존 수수료정책 사용"),
                new("Trust 정책", "TrustPolicy", "/admin/trust", "기존 정책·Simulation 관리 사용"),
                new("알림 Template", "NotificationTemplate", "/admin/notifications", "기존 Template 관리 사용"),
                new("구독상품", "CareProduct", "/admin/subscriptions", "기존 Care Product 사용"),
            ],
            admins.Select(x => new AdminAccountSummary(x.PublicId, x.LoginId, x.StatusCode,
                roleRows.Where(role => role.UserId == x.Id).Select(role => role.Code).OrderBy(value => value).ToArray(), x.CreatedAt, x.LastLoginAt)).ToArray(),
            [
                "세부 관리자 역할은 중요 작업에 적용되며 기존 업무별 API는 단계적으로 세부 권한 정책을 확대 적용합니다.",
                "관리자 MFA와 중요 작업 재인증이 적용됩니다. IP·기기 정책과 대량 다운로드 경보는 후속 보안 연동 대상입니다.",
                "Outbox 재처리는 실패 회차별 고유 요청과 재인증·사유·감사로그를 사용해 중복 요청을 차단합니다.",
                "개발 파일 저장소는 시그니처 검증만 수행하며 악성코드 검사 연동은 미구현입니다.",
            ],
            jobRows.Select(x => new AdminAutomationJobStatus(x.JobName, x.ConfigurationStatusCode, x.ConfigurationStatusCode == "ENABLED",
                x.LastStartedAt, x.LastSucceededAt, x.LastFailedAt, x.LastErrorCode, x.NextScheduledAt, x.ProcessingCount, x.FailedCount)).ToArray(),
            [
                new("PASSWORD_RESET", expiredPasswordResets, "REPORT_ONLY", "RETENTION_PERIOD_POLICY_REQUIRED"),
                new("CUSTOMER_WITHDRAWAL", withdrawalCandidates, "REPORT_ONLY", "RETENTION_AND_LEGAL_HOLD_POLICY_REQUIRED"),
                new("PROVIDER_EXIT", providerExitCandidates, "REPORT_ONLY", "RETENTION_AND_LEGAL_HOLD_POLICY_REQUIRED"),
                new("FILE", 0, "NOT_CONFIGURED", "FILE_RETENTION_POLICY_REQUIRED"),
                new("PERSONAL_DATA", 0, retentionPolicies.Any(x=>x.DomainCode=="PERSONAL_DATA")?"POLICY_CONFIGURED":"NOT_CONFIGURED", "LEGAL_HOLD_AWARE_AUTOMATION"),
            ],runtime,recentRuns,retentionPolicies);
    }

    public async Task RequestOutboxRetryAsync(Guid id, Guid actorPublicId,string? reauthToken,string reason, CancellationToken token)
    {
        var actor=await security.RequireSensitiveAccessAsync(actorPublicId,reauthToken,AdminDetailRoles.SuperAdmin,AdminDetailRoles.Security,AdminDetailRoles.Operations);
        if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length<5)throw new AdminSystemException("OUTBOX_RETRY_REASON_REQUIRED","재처리 사유를 5자 이상 입력해 주세요.");
        var row = await db.OutboxEvents.SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw new AdminSystemException("OUTBOX_NOT_FOUND", "Outbox Event를 찾을 수 없습니다.", 404);
        var retryKey=$"outbox-retry:{row.PublicId:N}:{row.AttemptCount}";
        if(await db.OutboxRetryRequests.AnyAsync(x=>x.IdempotencyKey==retryKey,token))return;
        if (row.StatusCode is not ("FAILED" or "DEAD")) throw new AdminSystemException("OUTBOX_RETRY_STATE_INVALID", "실패 또는 중단 상태의 Event만 재처리할 수 있습니다.", 409);
        if(string.IsNullOrWhiteSpace(row.IdempotencyKey))throw new AdminSystemException("OUTBOX_IDEMPOTENCY_KEY_REQUIRED","멱등성 키가 없는 Event는 안전하게 재처리할 수 없습니다.",409);
        db.OutboxRetryRequests.Add(new(){OutboxEventId=row.Id,AttemptSnapshot=row.AttemptCount,IdempotencyKey=retryKey,Reason=reason.Trim(),StatusCode="REQUESTED",RequestedByUserId=actor,RequestedAt=DateTime.UtcNow});
        row.StatusCode = "PENDING"; row.AvailableAt = DateTime.UtcNow; row.ErrorMessage = null;
        db.AuditLogs.Add(new() { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin,
            ActionCode = "OUTBOX_RETRY_REQUESTED", EntityType = "OUTBOX_EVENT", EntityPublicId = row.PublicId, ResultCode = "SUCCESS",
            Reason=reason.Trim(),AfterJson = JsonSerializer.Serialize(new { status = "PENDING", existingEvent = true, retryKey, attemptSnapshot=row.AttemptCount }) });
        await db.SaveChangesAsync(token);
    }

    private static AdminSystemMetric Metric(string code, string label, long count, string severity, string path) => new(code, label, count, severity, path);
    private async Task<IReadOnlyList<AdminIntegrationStatus>> IntegrationStatuses(CancellationToken token)
    {
        var now=DateTime.UtcNow;var since=now.AddDays(-30);var settings=await db.NotificationChannelSettings.AsNoTracking().ToListAsync(token);var deliveries=await db.NotificationDeliveries.AsNoTracking().Where(x=>x.CreatedAt>=since&&x.ChannelCode!="WEB").GroupBy(x=>x.ChannelCode).Select(g=>new{Code=g.Key,Delivered=g.LongCount(x=>x.StatusCode=="DELIVERED"),Failed=g.LongCount(x=>x.StatusCode=="FAILED"),LastSuccess=g.Where(x=>x.DeliveredAt!=null).Max(x=>(DateTime?)x.DeliveredAt),LastFailure=g.Where(x=>x.StatusCode=="FAILED").Max(x=>(DateTime?)x.CompletedAt)}).ToListAsync(token);
        var result=new List<AdminIntegrationStatus>();foreach(var pair in new[]{("KAKAO_ALIMTALK","KAKAO","카카오 알림톡"),("SMS","SMS","SMS"),("EMAIL","EMAIL","Email"),("PUSH","PUSH","Push")}){var setting=settings.FirstOrDefault(x=>x.ChannelCode==pair.Item2);var stat=deliveries.FirstOrDefault(x=>x.Code==pair.Item2);var total=(stat?.Delivered??0)+(stat?.Failed??0);var configured=setting is not null&&setting.IsEnabled&&setting.OperationModeCode=="PRODUCTION"&&!string.IsNullOrWhiteSpace(setting.ProviderCode);result.Add(new(pair.Item1,pair.Item3,configured?"CONFIGURED":"UNINTEGRATED",configured?"최근 30일 실제 발송 결과":"업체 계약·자격정보·PRODUCTION 설정이 모두 필요합니다.",stat?.Delivered??0,stat?.Failed??0,total==0?null:Math.Round((decimal)(stat?.Delivered??0)/total*100,2),stat?.LastSuccess,stat?.LastFailure));}
        var pg=paymentOptions.Value;var pgConfigured=pg.Enabled&&!string.IsNullOrWhiteSpace(pg.ClientKey)&&!string.IsNullOrWhiteSpace(pg.SecretKey);var payments=await db.SubscriptionPaymentRequests.AsNoTracking().Where(x=>x.CreatedAt>=since).GroupBy(_=>1).Select(g=>new{Delivered=g.LongCount(x=>x.StatusCode=="COMPLETED"),Failed=g.LongCount(x=>x.StatusCode=="FAILED"),LastSuccess=g.Where(x=>x.CompletedAt!=null).Max(x=>(DateTime?)x.CompletedAt),LastFailure=g.Where(x=>x.StatusCode=="FAILED").Max(x=>(DateTime?)x.UpdatedAt)}).SingleOrDefaultAsync(token);var paymentTotal=(payments?.Delivered??0)+(payments?.Failed??0);result.Add(new("PG","PG",pgConfigured?"CONFIGURED":"UNINTEGRATED",pgConfigured?$"{pg.ProviderCode} 설정 및 최근 30일 결과":"Enabled, ClientKey, SecretKey가 모두 필요합니다.",payments?.Delivered??0,payments?.Failed??0,paymentTotal==0?null:Math.Round((decimal)(payments?.Delivered??0)/paymentTotal*100,2),payments?.LastSuccess,payments?.LastFailure));return result;
    }
    private static string AreaLabel(string entityType) => entityType switch
    {
        "USER" or "CUSTOMER_PROFILE" or "PROVIDER_PROFILE" => "회원·권한",
        "WALLET" or "FEE_POLICY" or "PRICE_POLICY" => "재무·정책",
        "REPORT" or "SANCTION" or "DISPUTE_CASE" or "AFTER_SERVICE_CASE" => "사건·분쟁",
        "TRUST_POLICY" or "PROVIDER_TRUST" => "Trust",
        "SUBSCRIPTION_CONTRACT" or "MONTHLY_SETTLEMENT" => "Subscription",
        "INTERIOR_PROJECT" or "INTERIOR_CONTRACT" => "Interior",
        "NOTIFICATION_TEMPLATE" or "NOTIFICATION" => "Notification",
        _ => "운영·기타",
    };

    internal static string? MaskAuditJson(string? value) => MaskJson(value);
    internal static string? MaskAuditText(string? value) => MaskText(value);

    private static string? MaskJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var node = JsonNode.Parse(value);
            MaskNode(node);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return MaskText(value);
        }
    }

    private static void MaskNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj.ToArray())
            {
                var key = pair.Key.Replace("-", "").Replace("_", "").ToLowerInvariant();
                if (SensitiveKeys.Any(value => key.Contains(value.Replace("_", ""), StringComparison.Ordinal))) obj[pair.Key] = "***";
                else MaskNode(pair.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) MaskNode(item);
        }
    }

    private static string? MaskText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var masked = SecretAssignmentRegex().Replace(value, "$1=***");
        masked = BearerTokenRegex().Replace(masked, "Bearer ***");
        masked = JwtRegex().Replace(masked, "***");
        masked = EmailRegex().Replace(masked, "***@***");
        return PhoneRegex().Replace(masked, "***-****-****");
    }

    [GeneratedRegex(@"(?i)\b(password|passwd|token|secret|api[_-]?key|authorization|cookie|card(?:number)?|cvc|account(?:number|password)?)\b\s*[:=]\s*[^\s,;]+", RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)01[016789][- ]?\d{3,4}[- ]?\d{4}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
