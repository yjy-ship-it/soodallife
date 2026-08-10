using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderServiceApprovalService(SoodalLifeDbContext dbContext)
{
    public async Task<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>?> GetListAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var internalProviderId = await dbContext.ProviderProfiles.Where(value => value.PublicId == providerId).Select(value => (long?)value.Id).SingleOrDefaultAsync(cancellationToken);
        if (!internalProviderId.HasValue) return null;
        var rows = await (from link in dbContext.ProviderServiceCategories.AsNoTracking()
                          join category in dbContext.ServiceCategories.AsNoTracking() on link.CategoryId equals category.Id
                          join approval in dbContext.ProviderServiceApprovals.AsNoTracking() on link.Id equals approval.ProviderServiceCategoryId
                          where link.ProviderProfileId == internalProviderId.Value orderby category.Name
                          select new { category.PublicId, Approval = approval }).ToListAsync(cancellationToken);
        var result = new List<AdminProviderServiceApprovalDecisionResponse>();
        foreach (var row in rows) result.Add(await ToResponseAsync(row.PublicId, row.Approval, cancellationToken));
        return result;
    }

    public async Task<AdminProviderServiceApprovalDecisionResponse?> GetAsync(Guid providerId, Guid serviceId, CancellationToken cancellationToken)
    {
        var row = await (from provider in dbContext.ProviderProfiles.AsNoTracking()
                         join link in dbContext.ProviderServiceCategories.AsNoTracking() on provider.Id equals link.ProviderProfileId
                         join category in dbContext.ServiceCategories.AsNoTracking() on link.CategoryId equals category.Id
                         join approval in dbContext.ProviderServiceApprovals.AsNoTracking() on link.Id equals approval.ProviderServiceCategoryId
                         where provider.PublicId == providerId && category.PublicId == serviceId select new { category.PublicId, Approval = approval }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : await ToResponseAsync(row.PublicId, row.Approval, cancellationToken);
    }

    public async Task<AdminProviderServiceApprovalDecisionResponse> DecideAsync(Guid providerId, Guid serviceId, AdminProviderServiceApprovalDecisionRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var action = request.ActionCode.Trim().ToUpperInvariant();
        if (action is not ("APPROVE" or "REJECT")) throw Error("ADMIN_PROVIDER_SERVICE_APPROVAL_ACTION_INVALID", "승인 또는 반려를 선택해 주세요.");
        var reason = string.IsNullOrWhiteSpace(request.DecisionReason) ? null : request.DecisionReason.Trim();
        if (action == "REJECT" && reason is null) throw Error("ADMIN_PROVIDER_SERVICE_REJECTION_REASON_REQUIRED", "반려 사유를 입력해 주세요.");
        if (reason?.Length > 1000) throw Error("ADMIN_PROVIDER_SERVICE_DECISION_REASON_TOO_LONG", "심사 사유는 1,000자 이하여야 합니다.");
        byte[] token; try { token = Convert.FromBase64String(request.RowVersion); } catch (FormatException) { throw Conflict(); }
        var row = await (from provider in dbContext.ProviderProfiles
                         join link in dbContext.ProviderServiceCategories on provider.Id equals link.ProviderProfileId
                         join category in dbContext.ServiceCategories on link.CategoryId equals category.Id
                         join approval in dbContext.ProviderServiceApprovals on link.Id equals approval.ProviderServiceCategoryId
                         where provider.PublicId == providerId && category.PublicId == serviceId select new { Provider = provider, Link = link, Category = category, Approval = approval }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_SERVICE_APPROVAL_NOT_FOUND", "서비스 심사정보를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (!row.Approval.RowVersion.SequenceEqual(token)) throw Conflict();
        var actor = await dbContext.Users.Where(value => value.PublicId == actorId && value.StatusCode == "ACTIVE").Select(value => (long?)value.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
        var before = row.Approval.ApprovalStatusCode; var after = action == "APPROVE" ? "APPROVED" : "REJECTED"; var now = DateTime.UtcNow;
        row.Approval.ApprovalStatusCode = after; row.Approval.ApprovalDecidedAt = now; row.Approval.ApprovalDecidedByUserId = actor;
        row.Approval.DecisionReason = reason; row.Approval.UpdatedAt = now; row.Approval.UpdatedByUserId = actor;
        dbContext.Entry(row.Approval).Property(value => value.RowVersion).OriginalValue = token;
        dbContext.ProviderServiceApprovalEvents.Add(new ProviderServiceApprovalEvent { ProviderServiceCategoryId = row.Link.Id, FromStatusCode = before,
            ToStatusCode = after, ActionCode = action, DecisionReason = reason, DecidedAt = now, DecidedByUserId = actor, CreatedAt = now });
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin,
            ActionCode = action == "APPROVE" ? "PROVIDER_SERVICE_APPROVED" : "PROVIDER_SERVICE_REJECTED", EntityType = "PROVIDER_SERVICE_APPROVAL",
            EntityPublicId = row.Approval.PublicId, ResultCode = "SUCCESS", Reason = reason,
            BeforeJson = JsonSerializer.Serialize(new { ApprovalStatusCode = before }), AfterJson = JsonSerializer.Serialize(new { ApprovalStatusCode = after }),
            MetadataJson = JsonSerializer.Serialize(new { ProviderId = row.Provider.PublicId, ServiceId = row.Category.PublicId, ServiceName = row.Category.Name }) });
        try { await dbContext.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw Conflict(); }
        return await ToResponseAsync(row.Category.PublicId, row.Approval, cancellationToken);
    }

    private async Task<AdminProviderServiceApprovalDecisionResponse> ToResponseAsync(Guid serviceId, ProviderServiceApproval approval, CancellationToken cancellationToken)
    {
        var history = await dbContext.ProviderServiceApprovalEvents.AsNoTracking().Where(value => value.ProviderServiceCategoryId == approval.ProviderServiceCategoryId)
            .OrderByDescending(value => value.DecidedAt).Select(value => new AdminProviderServiceApprovalEventResponse(value.FromStatusCode, value.ToStatusCode, value.ActionCode, value.DecisionReason, value.DecidedAt)).ToListAsync(cancellationToken);
        return new(serviceId, approval.ApprovalStatusCode, approval.ApprovalRequestedAt, approval.ApprovalDecidedAt, approval.DecisionReason, Convert.ToBase64String(approval.RowVersion), history);
    }
    private static AdminServiceCategoryException Error(string code, string message) => new(code, message);
    private static AdminServiceCategoryException Conflict() => new("ADMIN_PROVIDER_SERVICE_APPROVAL_CONFLICT", "다른 관리자가 먼저 심사했습니다. 최신 상태를 다시 확인해 주세요.", StatusCodes.Status409Conflict);
}

