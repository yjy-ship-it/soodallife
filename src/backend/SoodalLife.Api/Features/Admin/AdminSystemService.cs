using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed partial class AdminSystemService(SoodalLifeDbContext db)
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
        var withdrawalCandidates = await db.CustomerWithdrawalRequests.AsNoTracking().LongCountAsync(x => x.StatusCode == "APPROVED", token);

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
            [
                Integration("KAKAO_ALIMTALK", "카카오 알림톡"), Integration("SMS", "SMS"), Integration("EMAIL", "Email"),
                Integration("PUSH", "Push"), Integration("PG", "PG")
            ],
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
                "현재 권한은 ADMIN 단일 역할이며 원 설계의 세부 관리자 역할은 미구현입니다.",
                "관리자 MFA, IP·기기 정책, 재인증 및 대량 다운로드 경보는 미구현입니다.",
                "Outbox 재처리는 멱등성과 중복 실행 안전성이 검증되지 않아 제공하지 않습니다.",
                "개발 파일 저장소는 시그니처 검증만 수행하며 악성코드 검사 연동은 미구현입니다.",
            ],
            jobRows.Select(x => new AdminAutomationJobStatus(x.JobName, x.ConfigurationStatusCode, x.ConfigurationStatusCode == "ENABLED",
                x.LastStartedAt, x.LastSucceededAt, x.LastFailedAt, x.LastErrorCode, x.NextScheduledAt, x.ProcessingCount, x.FailedCount)).ToArray(),
            [
                new("PASSWORD_RESET", expiredPasswordResets, "REPORT_ONLY", "RETENTION_PERIOD_POLICY_REQUIRED"),
                new("CUSTOMER_WITHDRAWAL", withdrawalCandidates, "REPORT_ONLY", "RETENTION_AND_LEGAL_HOLD_POLICY_REQUIRED"),
                new("FILE", 0, "NOT_CONFIGURED", "FILE_RETENTION_POLICY_REQUIRED"),
                new("PERSONAL_DATA", 0, "NOT_CONFIGURED", "DOMAIN_RETENTION_POLICY_REQUIRED"),
            ]);
    }

    public async Task RequestOutboxRetryAsync(Guid id, Guid actorPublicId, CancellationToken token)
    {
        var actor = await (from user in db.Users join link in db.UserRoles on user.Id equals link.UserId join role in db.Roles on link.RoleId equals role.Id
                           where user.PublicId == actorPublicId && user.StatusCode == "ACTIVE" && link.RevokedAt == null && role.Code == RoleCodes.Admin
                           select (long?)user.Id).SingleOrDefaultAsync(token) ?? throw new AdminSystemException("ADMIN_REQUIRED", "관리자 권한이 필요합니다.", 403);
        var row = await db.OutboxEvents.SingleOrDefaultAsync(x => x.PublicId == id, token) ?? throw new AdminSystemException("OUTBOX_NOT_FOUND", "Outbox Event를 찾을 수 없습니다.", 404);
        if (row.StatusCode is not ("FAILED" or "DEAD")) throw new AdminSystemException("OUTBOX_RETRY_STATE_INVALID", "실패 또는 중단 상태의 Event만 재처리할 수 있습니다.", 409);
        row.StatusCode = "PENDING"; row.AvailableAt = DateTime.UtcNow; row.ErrorMessage = null;
        db.AuditLogs.Add(new() { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin,
            ActionCode = "OUTBOX_RETRY_REQUESTED", EntityType = "OUTBOX_EVENT", EntityPublicId = row.PublicId, ResultCode = "SUCCESS",
            AfterJson = JsonSerializer.Serialize(new { status = "PENDING", existingEvent = true }) });
        await db.SaveChangesAsync(token);
    }

    private static AdminSystemMetric Metric(string code, string label, long count, string severity, string path) => new(code, label, count, severity, path);
    private static AdminIntegrationStatus Integration(string code, string label) => new(code, label, "UNINTEGRATED", "외부 연동 미구현");
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
