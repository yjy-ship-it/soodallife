using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed class CustomerDailyService(SoodalLifeDbContext db)
{
    public async Task<MySoodalSummaryResponse> Summary(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var address = await db.CustomerAddresses.AsNoTracking()
            .Where(x => x.CustomerProfileId == identity.ProfileId && x.IsActive && x.IsDefault)
            .Select(x => new MySoodalAddressSummary(x.PublicId, x.AddressName, x.RoadAddress, x.DetailAddress))
            .SingleOrDefaultAsync(token);
        var recent = await db.ServiceHistoryEntries.AsNoTracking()
            .Where(x => x.CustomerProfileId == identity.ProfileId)
            .OrderByDescending(x => x.CompletedAtSnapshot ?? x.OccurredAt)
            .Take(3)
            .Select(x => new MySoodalHistorySummary(x.PublicId, x.Title, x.CategoryNameSnapshot, x.CompletedAtSnapshot))
            .ToListAsync(token);

        return new(
            identity.Name,
            identity.LoginId,
            address,
            await db.ServiceRequests.CountAsync(x => x.CustomerProfileId == identity.ProfileId && (x.StatusCode == "OPEN" || x.StatusCode == "ACCEPTED"), token),
            await db.Transactions.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "COMPLETED" && x.StatusCode != "CANCELLED", token),
            await db.NotificationRecipients.CountAsync(x => x.UserId == identity.UserId && x.ReadAt == null && x.ArchivedAt == null, token),
            recent,
            await db.Reviews.CountAsync(x => x.CustomerProfileId == identity.ProfileId, token),
            await db.AfterServiceCases.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "RESOLVED" && x.StatusCode != "UNRESOLVED_CLOSED" && x.StatusCode != "CONVERTED_TO_DISPUTE", token),
            await db.DisputeCases.CountAsync(x => x.ApplicantUserId == identity.UserId && x.StatusCode != "RESOLVED" && x.StatusCode != "CLOSED", token),
            await db.Reports.CountAsync(x => x.ReporterUserId == identity.UserId && x.StatusCode != "RESOLVED" && x.StatusCode != "CANCELLED", token));
    }

    public async Task<IReadOnlyList<ConsentHistoryResponse>> ConsentHistory(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var current = await (from consent in db.UserConsents.AsNoTracking()
                             join version in db.LegalDocumentVersions.AsNoTracking() on consent.LegalDocumentVersionId equals version.Id
                             join document in db.LegalDocuments.AsNoTracking() on version.LegalDocumentId equals document.Id
                             where consent.UserId == identity.UserId
                             orderby consent.CreatedAt descending, version.VersionNo descending
                             select new ConsentHistoryResponse(version.PublicId, document.Code, document.RequirementCode, version.Title,
                                 version.VersionNo, consent.ConsentStatusCode, consent.ConsentedAt, consent.WithdrawnAt,
                                 consent.SourceCode, document.IsPlaceholder || version.IsPlaceholder)).ToListAsync(token);
        var audits = await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == identity.UserId && x.EntityType == "LEGAL_DOCUMENT_VERSION" &&
                (x.ActionCode == "CUSTOMER_CONSENT_GRANTED" || x.ActionCode == "CUSTOMER_CONSENT_WITHDRAWN") && x.EntityPublicId != null)
            .OrderByDescending(x => x.OccurredAt).Select(x => new { VersionId = x.EntityPublicId!.Value, x.ActionCode, x.OccurredAt }).ToListAsync(token);
        if (audits.Count == 0) return current;
        var versionIds = audits.Select(x => x.VersionId).Distinct().ToArray();
        var documents = await (from version in db.LegalDocumentVersions.AsNoTracking()
                               join document in db.LegalDocuments.AsNoTracking() on version.LegalDocumentId equals document.Id
                               where versionIds.Contains(version.PublicId)
                               select new { version.PublicId, document.Code, document.RequirementCode, version.Title, version.VersionNo,
                                   IsPlaceholder = document.IsPlaceholder || version.IsPlaceholder }).ToDictionaryAsync(x => x.PublicId, token);
        var events = audits.Where(x => documents.ContainsKey(x.VersionId)).Select(x =>
        {
            var document = documents[x.VersionId]; var withdrawn = x.ActionCode == "CUSTOMER_CONSENT_WITHDRAWN";
            return new ConsentHistoryResponse(x.VersionId, document.Code, document.RequirementCode, document.Title, document.VersionNo,
                withdrawn ? "WITHDRAWN" : "CONSENTED", withdrawn ? null : x.OccurredAt, withdrawn ? x.OccurredAt : null,
                "MY_SOODAL", document.IsPlaceholder);
        });
        return events.Concat(current.Where(x => audits.All(a => a.VersionId != x.LegalDocumentVersionId))).ToArray();
    }

    public async Task<WithdrawalReadinessResponse> WithdrawalReadiness(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var blockers = new List<WithdrawalBlockerResponse>();
        Add(blockers, "REQUEST", "진행 중 요청", await db.ServiceRequests.CountAsync(x => x.CustomerProfileId == identity.ProfileId && (x.StatusCode == "OPEN" || x.StatusCode == "ACCEPTED"), token));
        Add(blockers, "TRANSACTION", "진행 중 거래", await db.Transactions.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "COMPLETED" && x.StatusCode != "CANCELLED", token));
        Add(blockers, "AFTER_SERVICE", "진행 중 A/S", await db.AfterServiceCases.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "RESOLVED" && x.StatusCode != "UNRESOLVED_CLOSED" && x.StatusCode != "CONVERTED_TO_DISPUTE", token));
        Add(blockers, "DISPUTE", "진행 중 분쟁", await db.DisputeCases.CountAsync(x => x.ApplicantUserId == identity.UserId && x.StatusCode != "RESOLVED" && x.StatusCode != "CLOSED", token));
        Add(blockers, "SUBSCRIPTION", "확인할 정기구독", await db.SubscriptionContracts.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "TERMINATED", token));
        Add(blockers, "INTERIOR", "진행 중 인테리어", await db.InteriorProjects.CountAsync(x => x.CustomerProfileId == identity.ProfileId && x.StatusCode != "COMPLETED" && x.StatusCode != "CANCELLED", token));
        var activeRoles = await (from userRole in db.UserRoles.AsNoTracking()
                                 join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                 where userRole.UserId == identity.UserId && userRole.RevokedAt == null && role.IsActive
                                 select role.Code).CountAsync(token);
        return new(blockers.Count > 0, activeRoles > 1, blockers,
            blockers.Count > 0 ? "진행 중 서비스 확인이 필요합니다." : "탈퇴 신청은 즉시 계정 삭제가 아니며 운영 확인 후 처리됩니다.");
    }

    private async Task<(long UserId, long ProfileId, string Name, string LoginId)> Identity(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
            throw new CustomerAccountException(401, "AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");
        var row = await (from user in db.Users.AsNoTracking()
                         join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId
                         where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                         select new { user.Id, ProfileId = profile.Id, profile.DisplayName, user.LoginId }).SingleOrDefaultAsync(token);
        return row is null
            ? throw new CustomerAccountException(404, "CUSTOMER_PROFILE_NOT_FOUND", "고객 프로필을 찾을 수 없습니다.")
            : (row.Id, row.ProfileId, row.DisplayName, row.LoginId);
    }

    private static void Add(List<WithdrawalBlockerResponse> values, string code, string label, int count)
    {
        if (count > 0) values.Add(new(code, label, count));
    }
}
