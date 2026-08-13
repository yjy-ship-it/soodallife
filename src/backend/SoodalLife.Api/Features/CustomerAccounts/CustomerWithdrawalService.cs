using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.CustomerAccounts;

public interface ICustomerWithdrawalReadinessService
{
    Task<CustomerWithdrawalReadinessResponse> EvaluateAsync(long userId, long customerProfileId, string scope, CancellationToken token);
}

public sealed class CustomerWithdrawalReadinessService(SoodalLifeDbContext db) : ICustomerWithdrawalReadinessService
{
    private static readonly string[] ActiveTransactionStatuses = ["CREATED", "IN_PROGRESS", "COMPLETION_SUBMITTED", "REVISION_REQUESTED", "DISPUTED"];
    private static readonly string[] ActiveCareVisitStatuses = ["SCHEDULED", "RESCHEDULED", "PAUSED", "IN_PROGRESS", "PROVIDER_COMPLETED", "DISPUTED"];
    private static readonly string[] OpenAfterServiceStatuses = ["RECEIVED", "PROVIDER_CONFIRMED", "VISIT_SCHEDULED", "IN_PROGRESS"];
    private static readonly string[] OpenDisputeStatuses = ["OPEN", "UNDER_REVIEW", "WAITING_CUSTOMER", "WAITING_PROVIDER"];
    private static readonly string[] PendingCancellationStatuses = ["REQUESTED", "ADMIN_REVIEW_REQUIRED"];
    private static readonly string[] PendingPaymentStatuses = ["REQUESTED", "PROCESSING"];
    private static readonly string[] PendingRefundStatuses = ["REQUESTED", "APPROVED", "PROCESSING"];

    public async Task<CustomerWithdrawalReadinessResponse> EvaluateAsync(long userId, long customerProfileId, string scope, CancellationToken token)
    {
        var generalRequests = await db.ServiceRequests.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId && !x.IsUrgent &&
            (x.StatusCode == "DRAFT" || x.StatusCode == "OPEN" || x.StatusCode == "ACCEPTED"), token);
        var generalTransactions = await db.Transactions.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId &&
            x.FeePolicyKindSnapshot != "EMERGENCY" && ActiveTransactionStatuses.Contains(x.StatusCode), token);
        var cancellationReviews = await (from cancellation in db.TransactionCancellationRequests.AsNoTracking()
                                         join transaction in db.Transactions.AsNoTracking() on cancellation.TransactionId equals transaction.Id
                                         where transaction.CustomerProfileId == customerProfileId && PendingCancellationStatuses.Contains(cancellation.StatusCode)
                                         select cancellation.Id).CountAsync(token);

        var careRequests = await db.SubscriptionRequests.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId &&
            (x.StatusCode == "OPEN" || x.StatusCode == "SELECTED" || x.StatusCode == "CONTRACTED"), token);
        var careContracts = await db.SubscriptionContracts.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId && x.StatusCode != "TERMINATED", token);
        var careVisits = await (from visit in db.SubscriptionVisitSchedules.AsNoTracking()
                               join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id
                               where contract.CustomerProfileId == customerProfileId && ActiveCareVisitStatuses.Contains(visit.StatusCode)
                               select visit.Id).CountAsync(token);

        var interiorProjects = await db.InteriorProjects.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId &&
            x.StatusCode != "COMPLETED" && x.StatusCode != "CANCELLED", token);
        var emergencyRequests = await db.ServiceRequests.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId && x.IsUrgent &&
            (x.StatusCode == "DRAFT" || x.StatusCode == "OPEN" || x.StatusCode == "ACCEPTED"), token);
        var emergencyTransactions = await db.Transactions.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId &&
            x.FeePolicyKindSnapshot == "EMERGENCY" && ActiveTransactionStatuses.Contains(x.StatusCode), token);
        var afterServices = await db.AfterServiceCases.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId && OpenAfterServiceStatuses.Contains(x.StatusCode), token);
        var disputes = await db.DisputeCases.AsNoTracking().CountAsync(x => (x.ApplicantUserId == userId || x.CounterpartyUserId == userId) && OpenDisputeStatuses.Contains(x.StatusCode), token);

        var directPayments = await (from payment in db.TransactionDirectPayments.AsNoTracking()
                                    join transaction in db.Transactions.AsNoTracking() on payment.TransactionId equals transaction.Id
                                    where transaction.CustomerProfileId == customerProfileId && payment.StatusCode == "REGISTERED"
                                    select payment.Id).CountAsync(token);
        var carePayments = await db.SubscriptionPaymentRequests.AsNoTracking().CountAsync(x => x.CustomerProfileId == customerProfileId && PendingPaymentStatuses.Contains(x.StatusCode), token);
        var careRefunds = await (from refund in db.SubscriptionRefundAdjustments.AsNoTracking()
                                join contract in db.SubscriptionContracts.AsNoTracking() on refund.SubscriptionContractId equals contract.Id
                                where contract.CustomerProfileId == customerProfileId && PendingRefundStatuses.Contains(refund.StatusCode)
                                select refund.Id).CountAsync(token);

        var roles = await (from link in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                           where link.UserId == userId && link.RevokedAt == null && role.IsActive
                           orderby role.Code select role.Code).ToListAsync(token);
        var blockers = new List<CustomerWithdrawalBlocker>();
        Add(blockers, "GENERAL_REQUEST", "GENERAL", "진행 중 일반 요청", generalRequests);
        Add(blockers, "GENERAL_TRANSACTION", "GENERAL", "진행 중 일반 거래·완료확인", generalTransactions);
        Add(blockers, "TRANSACTION_CANCELLATION", "GENERAL", "검토 중 거래 취소", cancellationReviews);
        Add(blockers, "CARE_REQUEST", "CARE", "진행 중 수달 케어 요청", careRequests);
        Add(blockers, "CARE_CONTRACT", "CARE", "종료되지 않은 수달 케어 계약", careContracts);
        Add(blockers, "CARE_VISIT", "CARE", "진행 또는 확인 대기 수달 케어 회차", careVisits);
        Add(blockers, "INTERIOR_PROJECT", "INTERIOR", "진행 중 인테리어 프로젝트", interiorProjects);
        Add(blockers, "EMERGENCY_REQUEST", "EMERGENCY", "진행 중 긴급 요청", emergencyRequests);
        Add(blockers, "EMERGENCY_TRANSACTION", "EMERGENCY", "진행 중 긴급출동 거래", emergencyTransactions);
        Add(blockers, "AFTER_SERVICE", "AFTER_SERVICE", "미종결 A/S", afterServices);
        Add(blockers, "DISPUTE", "DISPUTE", "미종결 분쟁", disputes);
        Add(blockers, "DIRECT_PAYMENT", "FINANCIAL", "상대방 확인 대기 직접지급", directPayments);
        Add(blockers, "CARE_PAYMENT", "FINANCIAL", "처리 중 수달 케어 결제", carePayments);
        Add(blockers, "CARE_REFUND", "FINANCIAL", "처리 중 수달 케어 환불·조정", careRefunds);
        if (scope == "ACCOUNT") blockers.Add(new("ACCOUNT_CLOSURE_POLICY", "ACCOUNT", "전체 계정 종료·개인정보 파기정책 확인", 1, true));

        var financial = directPayments + carePayments + careRefunds;
        var activeWork = generalRequests + generalTransactions + cancellationReviews + careRequests + careContracts + careVisits +
            interiorProjects + emergencyRequests + emergencyTransactions + afterServices;
        var canComplete = blockers.All(x => !x.BlocksCompletion);
        return new(canComplete, canComplete ? "READY_TO_COMPLETE" : "BLOCKED_BY_ACTIVE_WORK", activeWork, afterServices, disputes,
            financial, roles.Count > 1, roles, blockers,
            canComplete ? "관리자가 최신 업무 상태를 다시 확인한 뒤 고객 역할 종료를 완료할 수 있습니다."
                : "진행 중 업무·A/S·분쟁 또는 금전 확인사항을 먼저 종결해야 합니다. 탈퇴 신청 자체는 관리자 검토를 위해 접수할 수 있습니다.",
            "POLICY_REQUIRED_REPORT_ONLY", roles.Count == 1 ? "POLICY_REQUIRED" : "NOT_APPLICABLE");
    }

    private static void Add(List<CustomerWithdrawalBlocker> values, string code, string domain, string label, int count)
    {
        if (count > 0) values.Add(new(code, domain, label, count, true));
    }
}

public sealed class CustomerWithdrawalService(SoodalLifeDbContext db, ICustomerWithdrawalReadinessService readiness)
{
    private static readonly string[] ActiveStatuses = ["REQUESTED", "UNDER_REVIEW", "BLOCKED_BY_ACTIVE_WORK", "READY_TO_COMPLETE"];

    public async Task<CustomerWithdrawalDashboardResponse> DashboardAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await CustomerIdentity(principal, token);
        var item = await db.CustomerWithdrawalRequests.AsNoTracking().Where(x => x.UserId == identity.UserId)
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(token);
        var current = await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, item?.ScopeCode ?? "CUSTOMER_ROLE", token);
        return new(item is null ? null : Response(item, current), current);
    }

    public async Task<CustomerWithdrawalRequestResponse> RequestAsync(ClaimsPrincipal principal, CreateCustomerWithdrawalRequest input, CancellationToken token)
    {
        var identity = await CustomerIdentity(principal, token);
        var scope = Scope(input.ScopeCode); var key = string.IsNullOrWhiteSpace(input.IdempotencyKey) ? $"customer-withdrawal:{identity.UserId}:{scope}:{Guid.NewGuid():N}" : Key(input.IdempotencyKey); var reason = Clean(input.Reason);
        var idempotent = await db.CustomerWithdrawalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, token);
        if (idempotent is not null)
        {
            if (idempotent.UserId != identity.UserId || idempotent.ScopeCode != scope) throw Conflict("CUSTOMER_WITHDRAWAL_IDEMPOTENCY_CONFLICT", "같은 중복 방지 키가 다른 요청에 사용되었습니다.");
            return Response(idempotent, await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, scope, token));
        }
        var existing = await db.CustomerWithdrawalRequests.AsNoTracking().Where(x => x.UserId == identity.UserId && ActiveStatuses.Contains(x.StatusCode))
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(token);
        if (existing is not null) return Response(existing, await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, existing.ScopeCode, token));
        var current = await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, scope, token); var now = DateTime.UtcNow;
        var item = new CustomerWithdrawalRequest { UserId = identity.UserId, ScopeCode = scope, StatusCode = current.RecommendedStatus,
            Reason = reason, RequestedAt = now, IdempotencyKey = key };
        db.CustomerWithdrawalRequests.Add(item); await Save(token);
        Audit(identity.UserId, RoleCodes.Customer, "CUSTOMER_WITHDRAWAL_REQUESTED", item, null, new { item.ScopeCode, item.StatusCode });
        await Outbox("CUSTOMER_WITHDRAWAL_REQUESTED", item, identity.UserId, token); await Save(token);
        return Response(item, current);
    }

    public async Task<CustomerWithdrawalRequestResponse> CancelAsync(ClaimsPrincipal principal, Guid id, CancelCustomerWithdrawalRequest input, CancellationToken token)
    {
        var identity = await CustomerIdentity(principal, token);
        var item = await db.CustomerWithdrawalRequests.SingleOrDefaultAsync(x => x.PublicId == id && x.UserId == identity.UserId, token)
            ?? throw NotFound("CUSTOMER_WITHDRAWAL_NOT_FOUND", "탈퇴 신청을 찾을 수 없습니다.");
        var key = Key(input.IdempotencyKey);
        if (item.DecisionIdempotencyKey == key) return Response(item, await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, item.ScopeCode, token));
        if (!ActiveStatuses.Contains(item.StatusCode)) throw Conflict("CUSTOMER_WITHDRAWAL_CANCEL_NOT_ALLOWED", "현재 상태에서는 신청을 취소할 수 없습니다.");
        ApplyVersion(item, input.RowVersion);
        var before = item.StatusCode; item.StatusCode = "CANCELLED"; item.ProcessedAt = DateTime.UtcNow; item.DecisionReason = Required(input.Reason); item.DecisionIdempotencyKey = key;
        Audit(identity.UserId, RoleCodes.Customer, "CUSTOMER_WITHDRAWAL_CANCELLED", item, new { Status = before }, new { item.StatusCode });
        await Outbox("CUSTOMER_WITHDRAWAL_CANCELLED", item, identity.UserId, token); await Save(token);
        return Response(item, await readiness.EvaluateAsync(identity.UserId, identity.ProfileId, item.ScopeCode, token));
    }

    public async Task<AdminCustomerWithdrawalListResponse> SearchAdminAsync(string? status, int page, int pageSize, CancellationToken token)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw Bad("CUSTOMER_WITHDRAWAL_PAGE_INVALID", "페이지 조건을 확인해 주세요.");
        var query = from item in db.CustomerWithdrawalRequests.AsNoTracking()
                    join profile in db.CustomerProfiles.AsNoTracking() on item.UserId equals profile.UserId
                    select new { Item = item, Profile = profile };
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim().ToUpperInvariant(); query = query.Where(x => x.Item.StatusCode == value); }
        var total = await query.CountAsync(token); var rows = await query.OrderByDescending(x => x.Item.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token); var values = new List<AdminCustomerWithdrawalListItem>(rows.Count);
        foreach (var row in rows)
        {
            var current = await readiness.EvaluateAsync(row.Item.UserId, row.Profile.Id, row.Item.ScopeCode, token);
            values.Add(new(row.Item.PublicId, row.Profile.PublicId, row.Profile.DisplayName, row.Item.ScopeCode, row.Item.StatusCode,
                row.Item.RequestedAt, current.ActiveWorkCount, current.OpenAfterServiceCount, current.OpenDisputeCount,
                current.FinancialPendingCount, current.HasMultipleActiveRoles, current.CanComplete));
        }
        return new(total, page, pageSize, values);
    }

    public async Task<AdminCustomerWithdrawalDetailResponse> AdminDetailAsync(Guid id, CancellationToken token)
    {
        var row = await (from item in db.CustomerWithdrawalRequests.AsNoTracking()
                         join profile in db.CustomerProfiles.AsNoTracking() on item.UserId equals profile.UserId
                         where item.PublicId == id select new { Item = item, Profile = profile }).SingleOrDefaultAsync(token)
            ?? throw NotFound("CUSTOMER_WITHDRAWAL_NOT_FOUND", "탈퇴 신청을 찾을 수 없습니다.");
        return Detail(row.Item, row.Profile, await readiness.EvaluateAsync(row.Item.UserId, row.Profile.Id, row.Item.ScopeCode, token));
    }

    public async Task<AdminCustomerWithdrawalDetailResponse> RecheckAsync(Guid id, AdminCustomerWithdrawalDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token); var item = await Item(id, token); ApplyVersion(item, input.RowVersion);
        var key = Key(input.IdempotencyKey); if (item.DecisionIdempotencyKey == key) return await AdminDetailAsync(id, token);
        if (!ActiveStatuses.Contains(item.StatusCode)) throw Conflict("CUSTOMER_WITHDRAWAL_RECHECK_NOT_ALLOWED", "현재 상태에서는 재검증할 수 없습니다.");
        var profile = await db.CustomerProfiles.SingleAsync(x => x.UserId == item.UserId, token);
        var current = await readiness.EvaluateAsync(item.UserId, profile.Id, item.ScopeCode, token); var before = item.StatusCode;
        item.StatusCode = current.RecommendedStatus; item.ProcessedAt = DateTime.UtcNow; item.ProcessedByUserId = actor; item.DecisionReason = Required(input.Reason); item.DecisionIdempotencyKey = key;
        Audit(actor, RoleCodes.Admin, "CUSTOMER_WITHDRAWAL_RECHECKED", item, new { Status = before }, new { item.StatusCode, current.ActiveWorkCount });
        await Outbox("CUSTOMER_WITHDRAWAL_RECHECKED", item, actor, token); await Save(token);
        return Detail(item, profile, current);
    }

    public async Task<AdminCustomerWithdrawalDetailResponse> CompleteAsync(Guid id, AdminCustomerWithdrawalDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token); var item = await Item(id, token); var decisionKey = Key(input.IdempotencyKey);
        if (item.DecisionIdempotencyKey == decisionKey && item.StatusCode == "COMPLETED") return await AdminDetailAsync(id, token);
        ApplyVersion(item, input.RowVersion);
        if (!ActiveStatuses.Contains(item.StatusCode)) throw Conflict("CUSTOMER_WITHDRAWAL_COMPLETE_NOT_ALLOWED", "현재 상태에서는 탈퇴를 완료할 수 없습니다.");
        var profile = await db.CustomerProfiles.SingleAsync(x => x.UserId == item.UserId, token);
        var current = await readiness.EvaluateAsync(item.UserId, profile.Id, item.ScopeCode, token);
        if (!current.CanComplete) throw Conflict("CUSTOMER_WITHDRAWAL_READINESS_CHANGED", "진행 업무 상태가 변경되었습니다. 재검증 후 다시 시도해 주세요.");
        if (item.ScopeCode != "CUSTOMER_ROLE") throw Conflict("ACCOUNT_WITHDRAWAL_POLICY_REQUIRED", "전체 계정 종료정책이 확정되지 않아 고객 역할 종료만 완료할 수 있습니다.");
        await using var transaction = await BeginTransaction(token); var now = DateTime.UtcNow;
        var roleId = await db.Roles.Where(x => x.Code == RoleCodes.Customer).Select(x => x.Id).SingleAsync(token);
        var role = await db.UserRoles.SingleOrDefaultAsync(x => x.UserId == item.UserId && x.RoleId == roleId && x.RevokedAt == null, token)
            ?? throw Conflict("CUSTOMER_ROLE_ALREADY_ENDED", "고객 역할이 이미 종료되었습니다.");
        role.RevokedAt = now; role.RevokedByUserId = actor;
        var chats = await db.ChatParticipants.Where(x => x.UserId == item.UserId && x.ParticipantRoleCode == "CUSTOMER" && x.StatusCode == "ACTIVE").ToListAsync(token);
        foreach (var chat in chats) { chat.StatusCode = "ENDED"; chat.AccessEndedAt = now; }
        item.StatusCode = "COMPLETED"; item.ProcessedAt = now; item.ProcessedByUserId = actor; item.DecisionReason = Required(input.Reason); item.DecisionIdempotencyKey = decisionKey;
        Audit(actor, RoleCodes.Admin, "CUSTOMER_WITHDRAWAL_COMPLETED", item, new { CustomerRole = "ACTIVE" },
            new { CustomerRole = "REVOKED", EndedCustomerChatParticipants = chats.Count, Retention = "REPORT_ONLY" });
        await Outbox("CUSTOMER_WITHDRAWAL_COMPLETED", item, actor, token); await Save(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return Detail(item, profile, await readiness.EvaluateAsync(item.UserId, profile.Id, item.ScopeCode, token));
    }

    public async Task<AdminCustomerWithdrawalDetailResponse> RejectAsync(Guid id, AdminCustomerWithdrawalDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token); var item = await Item(id, token); var key = Key(input.IdempotencyKey);
        if (item.DecisionIdempotencyKey == key && item.StatusCode == "REJECTED") return await AdminDetailAsync(id, token);
        ApplyVersion(item, input.RowVersion);
        if (!ActiveStatuses.Contains(item.StatusCode)) throw Conflict("CUSTOMER_WITHDRAWAL_REJECT_NOT_ALLOWED", "현재 상태에서는 신청을 거절할 수 없습니다.");
        var before = item.StatusCode; item.StatusCode = "REJECTED"; item.ProcessedAt = DateTime.UtcNow; item.ProcessedByUserId = actor; item.DecisionReason = Required(input.Reason); item.DecisionIdempotencyKey = key;
        Audit(actor, RoleCodes.Admin, "CUSTOMER_WITHDRAWAL_REJECTED", item, new { Status = before }, new { item.StatusCode });
        await Outbox("CUSTOMER_WITHDRAWAL_REJECTED", item, actor, token); await Save(token);
        var profile = await db.CustomerProfiles.SingleAsync(x => x.UserId == item.UserId, token);
        return Detail(item, profile, await readiness.EvaluateAsync(item.UserId, profile.Id, item.ScopeCode, token));
    }

    private async Task<(long UserId, long ProfileId)> CustomerIdentity(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) throw Unauthorized();
        var row = await (from user in db.Users.AsNoTracking() join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId
                         where user.PublicId == id && user.StatusCode == "ACTIVE" select new { UserId = user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return row is null ? throw NotFound("CUSTOMER_PROFILE_NOT_FOUND", "고객 프로필을 찾을 수 없습니다.") : (row.UserId, row.ProfileId);
    }
    private async Task<CustomerWithdrawalRequest> Item(Guid id, CancellationToken token) => await db.CustomerWithdrawalRequests.SingleOrDefaultAsync(x => x.PublicId == id, token)
        ?? throw NotFound("CUSTOMER_WITHDRAWAL_NOT_FOUND", "탈퇴 신청을 찾을 수 없습니다.");
    private async Task<long> AdminId(Guid id, CancellationToken token) => await (from user in db.Users join link in db.UserRoles on user.Id equals link.UserId
        join role in db.Roles on link.RoleId equals role.Id where user.PublicId == id && user.StatusCode == "ACTIVE" && link.RevokedAt == null && role.Code == RoleCodes.Admin select (long?)user.Id)
        .SingleOrDefaultAsync(token) ?? throw Unauthorized();
    private CustomerWithdrawalRequestResponse Response(CustomerWithdrawalRequest item, CustomerWithdrawalReadinessResponse current) =>
        new(item.PublicId, item.ScopeCode, item.StatusCode, item.Reason, item.RequestedAt, item.ProcessedAt, item.DecisionReason, Version(item.RowVersion), current);
    private AdminCustomerWithdrawalDetailResponse Detail(CustomerWithdrawalRequest item, CustomerProfile profile, CustomerWithdrawalReadinessResponse current) =>
        new(item.PublicId, profile.PublicId, profile.DisplayName, item.ScopeCode, item.StatusCode, item.Reason, item.RequestedAt, item.ProcessedAt, item.DecisionReason, Version(item.RowVersion), current);
    private void Audit(long actor, string role, string action, CustomerWithdrawalRequest item, object? before, object? after) => db.AuditLogs.Add(new AuditLog
    { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = role, ActionCode = action, EntityType = "CUSTOMER_WITHDRAWAL_REQUEST", EntityPublicId = item.PublicId,
        ResultCode = "SUCCESS", BeforeJson = before is null ? null : JsonSerializer.Serialize(before), AfterJson = after is null ? null : JsonSerializer.Serialize(after) });
    private async Task Outbox(string eventType, CustomerWithdrawalRequest item, long actor, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token)) return;
        var key = $"customer-withdrawal:{item.PublicId:N}:{eventType.ToLowerInvariant()}";
        if (await db.OutboxEvents.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        var now = DateTime.UtcNow; db.OutboxEvents.Add(new OutboxEvent { AggregateType = "CUSTOMER_WITHDRAWAL_REQUEST", AggregatePublicId = item.PublicId,
            EventType = eventType, PayloadJson = JsonSerializer.Serialize(new { sourceId = item.PublicId, customerWithdrawalRequestId = item.PublicId }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }
    private async Task<IDbContextTransaction?> BeginTransaction(CancellationToken token) => db.Database.IsRelational() && db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(token) : null;
    private async Task Save(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw Conflict("CUSTOMER_WITHDRAWAL_CONCURRENCY_CONFLICT", "다른 작업이 먼저 처리했습니다. 최신 상태를 다시 확인해 주세요."); }
        catch (DbUpdateException) { throw Conflict("CUSTOMER_WITHDRAWAL_DUPLICATE_REQUEST", "이미 처리 중인 고객 탈퇴 신청이 있습니다."); }
    }
    private void ApplyVersion(CustomerWithdrawalRequest item, string value) { try { db.Entry(item).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(value); } catch { throw Conflict("CUSTOMER_WITHDRAWAL_CONCURRENCY_CONFLICT", "최신 상태를 다시 확인해 주세요."); } }
    private static string Scope(string value) => value.Trim().ToUpperInvariant() is var scope && scope is "CUSTOMER_ROLE" or "ACCOUNT" ? scope : throw Bad("CUSTOMER_WITHDRAWAL_SCOPE_INVALID", "탈퇴 신청 범위를 확인해 주세요.");
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw Bad("CUSTOMER_WITHDRAWAL_REASON_REQUIRED", "처리 사유를 입력해 주세요.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Key(string value) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > 150 ? throw Bad("CUSTOMER_WITHDRAWAL_IDEMPOTENCY_KEY_INVALID", "중복 방지 키를 확인해 주세요.") : value.Trim();
    private static string Version(byte[] value) => Convert.ToBase64String(value);
    private static CustomerWithdrawalException Bad(string code, string message) => new(400, code, message);
    private static CustomerWithdrawalException Unauthorized() => new(401, "AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");
    private static CustomerWithdrawalException NotFound(string code, string message) => new(404, code, message);
    private static CustomerWithdrawalException Conflict(string code, string message) => new(409, code, message);
}
