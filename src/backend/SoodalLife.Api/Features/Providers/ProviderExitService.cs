using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public interface IProviderExitReadinessService
{
    Task<ProviderExitReadinessResponse> EvaluateAsync(long providerProfileId, long providerUserId, string requestType, CancellationToken token);
}

public sealed class ProviderExitReadinessService(SoodalLifeDbContext db) : IProviderExitReadinessService
{
    private static readonly string[] ActiveTransactionStatuses = ["CREATED", "IN_PROGRESS", "COMPLETION_SUBMITTED", "REVISION_REQUESTED", "DISPUTED"];
    private static readonly string[] ActiveVisitStatuses = ["SCHEDULED", "RESCHEDULED", "PAUSED", "IN_PROGRESS", "PROVIDER_COMPLETED", "DISPUTED"];
    private static readonly string[] OpenAfterServiceStatuses = ["RECEIVED", "PROVIDER_CONFIRMED", "VISIT_SCHEDULED", "IN_PROGRESS"];
    private static readonly string[] OpenDisputeStatuses = ["OPEN", "UNDER_REVIEW", "WAITING_CUSTOMER", "WAITING_PROVIDER"];
    private static readonly string[] OpenRefundStatuses = ["REQUESTED", "APPROVED", "PROCESSING"];

    public async Task<ProviderExitReadinessResponse> EvaluateAsync(long providerProfileId, long providerUserId, string requestType, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var transactions = await db.Transactions.AsNoTracking().CountAsync(x => x.ProviderProfileId == providerProfileId &&
            x.FeePolicyKindSnapshot != "EMERGENCY" && ActiveTransactionStatuses.Contains(x.StatusCode), token);
        var appointments = await (from appointment in db.TransactionAppointments.AsNoTracking()
                                  join transaction in db.Transactions.AsNoTracking() on appointment.TransactionId equals transaction.Id
                                  where transaction.ProviderProfileId == providerProfileId && (appointment.StatusCode == "PROPOSED" || appointment.StatusCode == "CONFIRMED")
                                  select appointment.Id).CountAsync(token);
        var completionPending = await db.Transactions.AsNoTracking().CountAsync(x => x.ProviderProfileId == providerProfileId &&
            (x.StatusCode == "COMPLETION_SUBMITTED" || x.StatusCode == "REVISION_REQUESTED"), token);
        var careContracts = await db.SubscriptionContracts.AsNoTracking().CountAsync(x => x.ProviderProfileId == providerProfileId &&
            (x.StatusCode == "ACTIVE" || x.StatusCode == "PAUSED" || x.StatusCode == "TERMINATION_REQUESTED"), token);
        var careVisits = await db.SubscriptionVisitSchedules.AsNoTracking().CountAsync(x => x.ProviderProfileId == providerProfileId && ActiveVisitStatuses.Contains(x.StatusCode), token);
        var interiorParticipants = await (from participant in db.InteriorProjectParticipants.AsNoTracking()
                                          join project in db.InteriorProjects.AsNoTracking() on participant.InteriorProjectId equals project.Id
                                          where participant.ProviderProfileId == providerProfileId && participant.StatusCode == "ACTIVE" &&
                                                participant.EffectiveFrom <= now && (participant.EffectiveTo == null || participant.EffectiveTo > now) &&
                                                project.StatusCode != "COMPLETED" && project.StatusCode != "CANCELLED"
                                          select participant.Id).CountAsync(token);
        var emergency = await db.Transactions.AsNoTracking().CountAsync(x => x.ProviderProfileId == providerProfileId &&
            x.FeePolicyKindSnapshot == "EMERGENCY" && ActiveTransactionStatuses.Contains(x.StatusCode), token);
        var afterServices = await db.AfterServiceCases.AsNoTracking().CountAsync(x => OpenAfterServiceStatuses.Contains(x.StatusCode) &&
            (x.ProviderProfileId == providerProfileId || x.InteriorProjectId != null && db.InteriorProjectParticipants.Any(p =>
                p.InteriorProjectId == x.InteriorProjectId && p.ProviderProfileId == providerProfileId && p.RoleCode == "AFTER_SERVICE" &&
                p.StatusCode == "ACTIVE" && p.EffectiveFrom <= now && (p.EffectiveTo == null || p.EffectiveTo > now))), token);
        var disputes = await db.DisputeCases.AsNoTracking().CountAsync(x => OpenDisputeStatuses.Contains(x.StatusCode) &&
            (x.ApplicantUserId == providerUserId || x.CounterpartyUserId == providerUserId), token);
        var chat = await db.ChatParticipants.AsNoTracking().CountAsync(x => x.UserId == providerUserId && x.ParticipantRoleCode == "PROVIDER" &&
            x.StatusCode == "ACTIVE" && x.AccessStartedAt <= now && (x.AccessEndedAt == null || x.AccessEndedAt > now), token);

        var wallet = await db.ProviderWallets.AsNoTracking().SingleAsync(x => x.ProviderProfileId == providerProfileId && x.CurrencyCode == "KRW", token);
        var pendingCharges = await db.WalletChargeRequests.AsNoTracking().CountAsync(x => x.WalletId == wallet.Id && (x.StatusCode == "REQUESTED" || x.StatusCode == "PROCESSING"), token);
        var pendingRefunds = await db.WalletRefundRequests.AsNoTracking().Where(x => x.WalletId == wallet.Id && OpenRefundStatuses.Contains(x.StatusCode)).ToListAsync(token);
        var pendingRefundAmount = pendingRefunds.Sum(x => x.RequestedAmount);
        const int pendingFeeRestores = 0;

        var blockers = new List<ProviderExitBlocker>();
        Add(blockers, "GENERAL_TRANSACTION", "진행 중 일반 거래", transactions);
        Add(blockers, "APPOINTMENT", "확정 또는 제안 중 일정", appointments);
        Add(blockers, "COMPLETION", "완료 확인 또는 보완 대기", completionPending);
        Add(blockers, "CARE_CONTRACT", "종료되지 않은 수달 케어 계약", careContracts);
        Add(blockers, "CARE_VISIT", "미래 또는 진행 중 수달 케어 회차", careVisits);
        Add(blockers, "INTERIOR", "활성 인테리어 참여 업무", interiorParticipants);
        Add(blockers, "EMERGENCY", "진행 중 긴급출동", emergency);
        Add(blockers, "AFTER_SERVICE", "미종결 A/S", afterServices);
        Add(blockers, "DISPUTE", "관리자 검토가 필요한 미종결 분쟁", disputes);
        Add(blockers, "WALLET_CHARGE", "처리 중 Wallet 충전", pendingCharges);
        if (wallet.ReservedBalance > 0) blockers.Add(new("WALLET_RESERVED", "예약된 Wallet 금액", 1, true));
        var accountPolicy = requestType == "ACCOUNT_WITHDRAWAL";
        if (accountPolicy) blockers.Add(new("ACCOUNT_WITHDRAWAL_POLICY", "전체 계정 탈퇴·개인정보 파기정책 확인", 1, true));

        var activeWorkCount = transactions + appointments + completionPending + careContracts + careVisits + interiorParticipants + emergency + afterServices;
        var hasBlocking = blockers.Any(x => x.BlocksCompletion);
        var refundRequired = wallet.AvailableBalance > 0 || pendingRefunds.Count > 0;
        var status = accountPolicy ? "UNDER_REVIEW" : hasBlocking ? "BLOCKED_BY_ACTIVE_WORK" : refundRequired ? "REFUND_REQUIRED" : "READY_TO_COMPLETE";
        var guidance = accountPolicy
            ? "전체 계정 탈퇴는 다른 역할의 진행 업무와 개인정보 보존·파기정책이 확정된 뒤 별도 검토해야 합니다. 이번 단계에서 계정이나 개인정보를 자동 삭제하지 않습니다."
            : hasBlocking
            ? "진행 중 업무 또는 보류 금액이 있어 공급자 활동 종료를 완료할 수 없습니다. 각 업무를 먼저 종결해 주세요."
            : refundRequired
                ? "충전금 환불과 관리자 지급 확인이 완료된 후 공급자 활동을 종료할 수 있습니다."
                : "현재 공급자 역할 종료를 관리자에게 요청할 수 있습니다.";
        return new(!hasBlocking && !refundRequired, status, blockers,
            new(wallet.PublicId, wallet.CurrencyCode.Trim(), wallet.AvailableBalance, wallet.ReservedBalance, wallet.StatusCode,
                pendingRefundAmount, pendingCharges, pendingRefunds.Count, pendingFeeRestores, refundRequired, wallet.ReservedBalance > 0),
            activeWorkCount, disputes, chat, accountPolicy,
            guidance, "POLICY_REQUIRED");
    }

    private static void Add(List<ProviderExitBlocker> values, string code, string label, int count)
    {
        if (count > 0) values.Add(new(code, label, count, true));
    }
}

public sealed class ProviderExitService(SoodalLifeDbContext db, IProviderExitReadinessService readiness)
{
    private static readonly string[] ActiveExitStatuses = ["REQUESTED", "UNDER_REVIEW", "REFUND_REQUIRED", "BLOCKED_BY_ACTIVE_WORK", "READY_TO_COMPLETE"];

    public async Task<ProviderExitDashboardResponse> DashboardAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await ProviderIdentityAsync(principal, token);
        var request = await db.ProviderExitRequests.AsNoTracking().Where(x => x.ProviderProfileId == identity.Provider.Id)
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(token);
        var requestType = request?.RequestTypeCode ?? "PROVIDER_ROLE_EXIT";
        var current = await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, requestType, token);
        var hasCustomer = await HasRole(identity.User.Id, RoleCodes.Customer, token);
        return new(identity.Provider.BusinessName, identity.Provider.ApprovalStatusCode, identity.Provider.ActivityStatusCode,
            hasCustomer, request is null ? null : await Response(request, current, token), current);
    }

    public async Task<ProviderExitRequestResponse> RequestAsync(ClaimsPrincipal principal, CreateProviderExitRequest input, CancellationToken token)
    {
        var identity = await ProviderIdentityAsync(principal, token);
        var type = NormalizeType(input.RequestType);
        var reason = Required(input.Reason);
        var key = Key(input.IdempotencyKey);
        var idempotent = await db.ProviderExitRequests.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, token);
        if (idempotent is not null)
        {
            if (idempotent.ProviderProfileId != identity.Provider.Id || idempotent.RequestTypeCode != type)
                throw Conflict("PROVIDER_EXIT_IDEMPOTENCY_CONFLICT", "같은 중복 방지 키가 다른 요청에 사용되었습니다.");
            return await Response(idempotent, await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, type, token), token);
        }
        var existing = await db.ProviderExitRequests.AsNoTracking().Where(x => x.ProviderProfileId == identity.Provider.Id && ActiveExitStatuses.Contains(x.StatusCode))
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(token);
        if (existing is not null) return await Response(existing, await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, existing.RequestTypeCode, token), token);

        await using var transaction = await BeginTransaction(token);
        var current = await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, type, token);
        var now = DateTime.UtcNow;
        var item = new ProviderExitRequest
        {
            UserId = identity.User.Id, ProviderProfileId = identity.Provider.Id, RequestTypeCode = type, Reason = reason,
            StatusCode = current.RecommendedStatus, ReviewStatusCode = "PENDING", RequestedAt = now,
            IdempotencyKey = key, CreatedAt = now, CreatedByUserId = identity.User.Id, UpdatedAt = now, UpdatedByUserId = identity.User.Id,
        };
        db.ProviderExitRequests.Add(item);
        await db.SaveChangesAsync(token);
        await EnsureRefundRequest(item, current, identity.User.Id, token);
        AddAudit(identity.User.Id, RoleCodes.Provider, "PROVIDER_EXIT_REQUESTED", item, null,
            new { item.RequestTypeCode, item.StatusCode, refundRequired = current.Wallet.RefundRequired }, reason);
        await AddOutboxIfTemplate("PROVIDER_EXIT_REQUESTED", item, identity.User.Id, token);
        await Save(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return await Response(item, await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, type, token), token);
    }

    public async Task<ProviderExitRequestResponse> CancelAsync(ClaimsPrincipal principal, Guid id, CancelProviderExitRequest input, CancellationToken token)
    {
        var identity = await ProviderIdentityAsync(principal, token);
        var item = await db.ProviderExitRequests.SingleOrDefaultAsync(x => x.PublicId == id && x.ProviderProfileId == identity.Provider.Id, token)
            ?? throw NotFound("PROVIDER_EXIT_NOT_FOUND", "공급자 활동 종료 신청을 찾을 수 없습니다.");
        ApplyVersion(item, input.RowVersion);
        if (!ActiveExitStatuses.Contains(item.StatusCode)) throw Conflict("PROVIDER_EXIT_CANCEL_NOT_ALLOWED", "현재 상태에서는 신청을 취소할 수 없습니다.");
        if (item.WalletRefundRequestId.HasValue)
        {
            var refund = await db.WalletRefundRequests.SingleAsync(x => x.Id == item.WalletRefundRequestId, token);
            if (refund.StatusCode != "REQUESTED") throw Conflict("PROVIDER_EXIT_REFUND_ALREADY_REVIEWED", "환불 검토가 시작되어 관리자 확인 없이 종료 신청을 취소할 수 없습니다.");
            refund.StatusCode = "CANCELLED"; refund.ReviewReason = Required(input.Reason); refund.ReviewedAt = DateTime.UtcNow;
            refund.UpdatedAt = DateTime.UtcNow; refund.UpdatedByUserId = identity.User.Id;
        }
        var before = item.StatusCode;
        item.StatusCode = "CANCELLED"; item.DecisionReason = Required(input.Reason); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = identity.User.Id;
        AddAudit(identity.User.Id, RoleCodes.Provider, "PROVIDER_EXIT_CANCELLED", item, new { StatusCode = before }, new { item.StatusCode }, item.DecisionReason);
        await Save(token);
        return await Response(item, await readiness.EvaluateAsync(identity.Provider.Id, identity.User.Id, item.RequestTypeCode, token), token);
    }

    public async Task<AdminProviderExitListResponse> SearchAdminAsync(string? status, int page, int pageSize, CancellationToken token)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw Bad("PROVIDER_EXIT_PAGE_INVALID", "페이지 조건을 확인해 주세요.");
        var query = from item in db.ProviderExitRequests.AsNoTracking()
                    join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
                    join refund in db.WalletRefundRequests.AsNoTracking() on item.WalletRefundRequestId equals refund.Id into refunds
                    from refund in refunds.DefaultIfEmpty()
                    select new { Item = item, Provider = provider, Refund = refund };
        if (!string.IsNullOrWhiteSpace(status)) { var normalized = status.Trim().ToUpperInvariant(); query = query.Where(x => x.Item.StatusCode == normalized); }
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(x => x.Item.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var values = new List<AdminProviderExitListItem>(rows.Count);
        foreach (var row in rows)
        {
            var current = await readiness.EvaluateAsync(row.Provider.Id, row.Item.UserId, row.Item.RequestTypeCode, token);
            values.Add(new(row.Item.PublicId, row.Provider.PublicId, row.Provider.BusinessName, row.Item.RequestTypeCode,
                row.Item.StatusCode, row.Item.ReviewStatusCode, row.Item.RequestedAt, current.ActiveWorkCount,
                current.OpenDisputeCount, current.Wallet.AvailableBalance, current.Wallet.ReservedBalance,
                current.Wallet.RefundRequired, row.Refund?.StatusCode));
        }
        return new(total, page, pageSize, values);
    }

    public async Task<AdminProviderExitDetailResponse> AdminDetailAsync(Guid id, CancellationToken token)
    {
        var row = await (from item in db.ProviderExitRequests.AsNoTracking()
                         join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
                         join refund in db.WalletRefundRequests.AsNoTracking() on item.WalletRefundRequestId equals refund.Id into refunds
                         from refund in refunds.DefaultIfEmpty()
                         where item.PublicId == id select new { Item = item, Provider = provider, Refund = refund }).SingleOrDefaultAsync(token)
            ?? throw NotFound("PROVIDER_EXIT_NOT_FOUND", "공급자 활동 종료 신청을 찾을 수 없습니다.");
        var current = await readiness.EvaluateAsync(row.Provider.Id, row.Item.UserId, row.Item.RequestTypeCode, token);
        return new(row.Item.PublicId, row.Provider.PublicId, row.Provider.BusinessName, row.Item.RequestTypeCode, row.Item.Reason,
            row.Item.StatusCode, row.Item.ReviewStatusCode, row.Item.RequestedAt, row.Item.ReviewedAt, row.Item.DecisionReason,
            row.Item.CompletedAt, await HasRole(row.Item.UserId, RoleCodes.Customer, token), row.Refund?.PublicId,
            row.Refund?.StatusCode, Version(row.Item.RowVersion), current);
    }

    public async Task<AdminProviderExitDetailResponse> RecheckAsync(Guid id, AdminProviderExitDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token);
        var item = await db.ProviderExitRequests.SingleOrDefaultAsync(x => x.PublicId == id, token)
            ?? throw NotFound("PROVIDER_EXIT_NOT_FOUND", "공급자 활동 종료 신청을 찾을 수 없습니다.");
        ApplyVersion(item, input.RowVersion);
        if (!ActiveExitStatuses.Contains(item.StatusCode)) throw Conflict("PROVIDER_EXIT_RECHECK_NOT_ALLOWED", "현재 상태에서는 재검증할 수 없습니다.");
        var current = await readiness.EvaluateAsync(item.ProviderProfileId, item.UserId, item.RequestTypeCode, token);
        var before = item.StatusCode;
        item.StatusCode = current.RecommendedStatus; item.ReviewStatusCode = "UNDER_REVIEW"; item.ReviewedAt = DateTime.UtcNow;
        item.ReviewedByUserId = actor; item.DecisionReason = Required(input.Reason); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor;
        await EnsureRefundRequest(item, current, actor, token);
        AddAudit(actor, RoleCodes.Admin, "PROVIDER_EXIT_RECHECKED", item, new { StatusCode = before }, new { item.StatusCode }, item.DecisionReason);
        await AddOutboxIfTemplate(item.StatusCode == "REFUND_REQUIRED" ? "PROVIDER_EXIT_REFUND_REQUIRED" : item.StatusCode == "READY_TO_COMPLETE" ? "PROVIDER_EXIT_READY" : "PROVIDER_EXIT_RECHECKED", item, actor, token);
        await Save(token);
        return await AdminDetailAsync(id, token);
    }

    public async Task<AdminProviderExitDetailResponse> CompleteAsync(Guid id, AdminProviderExitDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token);
        var item = await db.ProviderExitRequests.SingleOrDefaultAsync(x => x.PublicId == id, token)
            ?? throw NotFound("PROVIDER_EXIT_NOT_FOUND", "공급자 활동 종료 신청을 찾을 수 없습니다.");
        ApplyVersion(item, input.RowVersion);
        if (!ActiveExitStatuses.Contains(item.StatusCode)) throw Conflict("PROVIDER_EXIT_COMPLETE_NOT_ALLOWED", "현재 상태에서는 활동 종료를 완료할 수 없습니다.");
        var current = await readiness.EvaluateAsync(item.ProviderProfileId, item.UserId, item.RequestTypeCode, token);
        if (!current.CanComplete) throw Conflict("PROVIDER_EXIT_READINESS_CHANGED", "진행 업무 또는 Wallet 상태가 변경되었습니다. 재검증 후 다시 시도해 주세요.");

        await using var transaction = await BeginTransaction(token);
        var now = DateTime.UtcNow;
        var provider = await db.ProviderProfiles.SingleAsync(x => x.Id == item.ProviderProfileId, token);
        var roleId = await db.Roles.Where(x => x.Code == RoleCodes.Provider).Select(x => x.Id).SingleAsync(token);
        var role = await db.UserRoles.SingleOrDefaultAsync(x => x.UserId == item.UserId && x.RoleId == roleId && x.RevokedAt == null, token)
            ?? throw Conflict("PROVIDER_ROLE_ALREADY_ENDED", "공급자 역할이 이미 종료되었습니다.");
        var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == provider.Id && x.CurrencyCode == "KRW", token);
        if (wallet.AvailableBalance != 0 || wallet.ReservedBalance != 0 || await db.WalletRefundRequests.AnyAsync(x => x.WalletId == wallet.Id && (x.StatusCode == "REQUESTED" || x.StatusCode == "APPROVED" || x.StatusCode == "PROCESSING"), token))
            throw Conflict("PROVIDER_EXIT_WALLET_CHANGED", "Wallet 잔액 또는 환불 상태가 변경되었습니다.");

        role.RevokedAt = now; role.RevokedByUserId = actor;
        provider.ActivityStatusCode = "INACTIVE"; provider.UpdatedAt = now; provider.UpdatedByUserId = actor;
        wallet.StatusCode = "CLOSED"; wallet.UpdatedAt = now; wallet.UpdatedByUserId = actor;
        var emergency = await db.ProviderEmergencySettings.SingleOrDefaultAsync(x => x.ProviderProfileId == provider.Id, token);
        if (emergency is not null) { emergency.IsEnabled = false; emergency.UpdatedAt = now; emergency.UpdatedByUserId = actor; }
        var chats = await db.ChatParticipants.Where(x => x.UserId == item.UserId && x.ParticipantRoleCode == "PROVIDER" && x.StatusCode == "ACTIVE").ToListAsync(token);
        foreach (var chat in chats) { chat.StatusCode = "ENDED"; chat.AccessEndedAt = now; }

        item.StatusCode = "COMPLETED"; item.ReviewStatusCode = "APPROVED"; item.ReviewedAt = now;
        item.ReviewedByUserId = actor; item.DecisionReason = Required(input.Reason); item.CompletedAt = now; item.UpdatedAt = now; item.UpdatedByUserId = actor;
        AddAudit(actor, RoleCodes.Admin, "PROVIDER_ROLE_EXIT_COMPLETED", item, new { StatusCode = "READY_TO_COMPLETE" },
            new { item.StatusCode, providerActivity = provider.ActivityStatusCode, walletStatus = wallet.StatusCode, providerRoleEnded = true }, item.DecisionReason);
        await AddOutboxIfTemplate("PROVIDER_EXIT_COMPLETED", item, actor, token);
        await Save(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return await AdminDetailAsync(id, token);
    }

    public async Task<AdminProviderExitDetailResponse> RejectAsync(Guid id, AdminProviderExitDecisionRequest input, Guid actorPublicId, CancellationToken token)
    {
        var actor = await AdminId(actorPublicId, token);
        var item = await db.ProviderExitRequests.SingleOrDefaultAsync(x => x.PublicId == id, token)
            ?? throw NotFound("PROVIDER_EXIT_NOT_FOUND", "공급자 활동 종료 신청을 찾을 수 없습니다.");
        ApplyVersion(item, input.RowVersion);
        if (!ActiveExitStatuses.Contains(item.StatusCode)) throw Conflict("PROVIDER_EXIT_REJECT_NOT_ALLOWED", "현재 상태에서는 신청을 거절할 수 없습니다.");
        if (item.WalletRefundRequestId.HasValue)
        {
            var refund = await db.WalletRefundRequests.SingleAsync(x => x.Id == item.WalletRefundRequestId, token);
            if (refund.StatusCode == "REQUESTED") { refund.StatusCode = "REJECTED"; refund.ReviewedAt = DateTime.UtcNow; refund.ReviewedByUserId = actor; refund.ReviewReason = Required(input.Reason); refund.UpdatedAt = DateTime.UtcNow; refund.UpdatedByUserId = actor; }
        }
        var before = item.StatusCode;
        item.StatusCode = "REJECTED"; item.ReviewStatusCode = "REJECTED"; item.ReviewedAt = DateTime.UtcNow;
        item.ReviewedByUserId = actor; item.DecisionReason = Required(input.Reason); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor;
        AddAudit(actor, RoleCodes.Admin, "PROVIDER_EXIT_REJECTED", item, new { StatusCode = before }, new { item.StatusCode }, item.DecisionReason);
        await AddOutboxIfTemplate("PROVIDER_EXIT_REJECTED", item, actor, token);
        await Save(token);
        return await AdminDetailAsync(id, token);
    }

    private async Task EnsureRefundRequest(ProviderExitRequest item, ProviderExitReadinessResponse current, long actor, CancellationToken token)
    {
        if (item.WalletRefundRequestId.HasValue || current.ActiveWorkCount > 0 || current.OpenDisputeCount > 0 ||
            current.Wallet.ReservedBalance > 0 || current.Wallet.PendingChargeCount > 0 || current.Wallet.AvailableBalance <= 0) return;
        var walletId = await db.ProviderWallets.Where(x => x.PublicId == current.Wallet.WalletId).Select(x => x.Id).SingleAsync(token);
        var open = await db.WalletRefundRequests.Where(x => x.WalletId == walletId && (x.StatusCode == "REQUESTED" || x.StatusCode == "APPROVED" || x.StatusCode == "PROCESSING"))
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(token);
        if (open is null)
        {
            var now = DateTime.UtcNow;
            open = new WalletRefundRequest
            {
                WalletId = walletId, RequestedAmount = current.Wallet.AvailableBalance, StatusCode = "REQUESTED",
                RequestReason = "공급자 활동 종료에 따른 잔여 충전금 환불 요청", RequestedAt = now,
                IdempotencyKey = $"provider-exit-refund:{item.PublicId:N}", CreatedAt = now, CreatedByUserId = actor,
                UpdatedAt = now, UpdatedByUserId = actor,
            };
            db.WalletRefundRequests.Add(open);
            await db.SaveChangesAsync(token);
        }
        item.WalletRefundRequestId = open.Id;
    }

    private async Task<ProviderExitRequestResponse> Response(ProviderExitRequest item, ProviderExitReadinessResponse current, CancellationToken token)
    {
        WalletRefundRequest? refund = null;
        if (item.WalletRefundRequestId.HasValue) refund = await db.WalletRefundRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.WalletRefundRequestId, token);
        return new(item.PublicId, item.RequestTypeCode, item.Reason, item.StatusCode, item.ReviewStatusCode,
            item.RequestedAt, item.ReviewedAt, item.DecisionReason, item.CompletedAt, refund?.PublicId, refund?.StatusCode,
            Version(item.RowVersion), current);
    }

    private async Task<(User User, ProviderProfile Provider)> ProviderIdentityAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) throw Unauthorized();
        var row = await (from user in db.Users join provider in db.ProviderProfiles on user.Id equals provider.UserId
                         where user.PublicId == id && user.StatusCode == "ACTIVE" select new { User = user, Provider = provider }).SingleOrDefaultAsync(token);
        return row is null ? throw Forbidden("PROVIDER_PROFILE_REQUIRED", "공급자 프로필을 확인할 수 없습니다.") : (row.User, row.Provider);
    }

    private async Task<long> AdminId(Guid publicId, CancellationToken token) => await (from user in db.Users
        join link in db.UserRoles on user.Id equals link.UserId join role in db.Roles on link.RoleId equals role.Id
        where user.PublicId == publicId && user.StatusCode == "ACTIVE" && link.RevokedAt == null && role.Code == RoleCodes.Admin
        select (long?)user.Id).SingleOrDefaultAsync(token) ?? throw Unauthorized();
    private Task<bool> HasRole(long userId, string code, CancellationToken token) => (from link in db.UserRoles.AsNoTracking()
        join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id where link.UserId == userId && link.RevokedAt == null && role.IsActive && role.Code == code select link.Id).AnyAsync(token);

    private void AddAudit(long actor, string role, string action, ProviderExitRequest item, object? before, object? after, string? reason) =>
        db.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = role, ActionCode = action,
            EntityType = "PROVIDER_EXIT_REQUEST", EntityPublicId = item.PublicId, ResultCode = "SUCCESS", Reason = reason,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before), AfterJson = after is null ? null : JsonSerializer.Serialize(after) });
    private async Task AddOutboxIfTemplate(string eventType, ProviderExitRequest item, long actor, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token)) return;
        var key = $"provider-exit:{item.PublicId:N}:{eventType.ToLowerInvariant()}";
        if (await db.OutboxEvents.AnyAsync(x => x.IdempotencyKey == key, token)) return;
        var now = DateTime.UtcNow;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = "PROVIDER_EXIT_REQUEST", AggregatePublicId = item.PublicId,
            EventType = eventType, PayloadJson = JsonSerializer.Serialize(new { sourceId = item.PublicId, providerExitRequestId = item.PublicId }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }

    private async Task<IDbContextTransaction?> BeginTransaction(CancellationToken token) => db.Database.IsRelational() && db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(token) : null;
    private async Task Save(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw Conflict("PROVIDER_EXIT_CONCURRENCY_CONFLICT", "다른 작업이 먼저 처리했습니다. 최신 상태를 다시 확인해 주세요."); }
        catch (DbUpdateException) { throw Conflict("PROVIDER_EXIT_DUPLICATE_REQUEST", "이미 처리 중인 공급자 활동 종료 신청이 있습니다."); }
    }
    private void ApplyVersion(ProviderExitRequest item, string value)
    {
        try { db.Entry(item).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(value); }
        catch { throw Conflict("PROVIDER_EXIT_CONCURRENCY_CONFLICT", "최신 상태를 다시 확인해 주세요."); }
    }
    private static string NormalizeType(string value) => value.Trim().ToUpperInvariant() is var type && type is "PROVIDER_ROLE_EXIT" or "ACCOUNT_WITHDRAWAL" ? type : throw Bad("PROVIDER_EXIT_TYPE_INVALID", "신청 유형을 확인해 주세요.");
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw Bad("PROVIDER_EXIT_REASON_REQUIRED", "처리 사유를 입력해 주세요.") : value.Trim();
    private static string Key(string value) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > 150 ? throw Bad("PROVIDER_EXIT_IDEMPOTENCY_KEY_INVALID", "중복 방지 키를 확인해 주세요.") : value.Trim();
    private static string Version(byte[] value) => Convert.ToBase64String(value);
    private static ProviderExitException Bad(string code, string message) => new(400, code, message);
    private static ProviderExitException Unauthorized() => new(401, "AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");
    private static ProviderExitException Forbidden(string code, string message) => new(403, code, message);
    private static ProviderExitException NotFound(string code, string message) => new(404, code, message);
    private static ProviderExitException Conflict(string code, string message) => new(409, code, message);
}
