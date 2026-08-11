using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminProviderListResponse> SearchAsync(string? search, string? accountStatus, string? approvalStatus, string? activityStatus,
        Guid? serviceId, Guid? areaId, string? documentStatus, DateOnly? joinedFrom, DateOnly? joinedTo, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new AdminServiceCategoryException("ADMIN_PROVIDER_PAGE_INVALID", "페이지와 페이지당 공급자 수를 확인해 주세요.");
        var query = from provider in dbContext.ProviderProfiles.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                    where dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null &&
                        dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                    select new { Provider = provider, User = user };
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var id)) query = query.Where(row => row.Provider.PublicId == id || row.User.PublicId == id);
            else query = query.Where(row => row.Provider.BusinessName.Contains(term) || (row.Provider.BusinessRegistrationNo != null && row.Provider.BusinessRegistrationNo.Contains(term)) ||
                (row.User.Phone != null && row.User.Phone.Contains(term)) || (row.User.Email != null && row.User.Email.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(accountStatus)) query = query.Where(row => row.User.StatusCode == accountStatus.ToUpper());
        if (!string.IsNullOrWhiteSpace(approvalStatus)) query = query.Where(row => row.Provider.ApprovalStatusCode == approvalStatus.ToUpper());
        if (!string.IsNullOrWhiteSpace(activityStatus)) query = query.Where(row => row.Provider.ActivityStatusCode == activityStatus.ToUpper());
        if (joinedFrom.HasValue) query = query.Where(row => row.User.CreatedAt >= joinedFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (joinedTo.HasValue) query = query.Where(row => row.User.CreatedAt < joinedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (serviceId.HasValue) query = query.Where(row => dbContext.ProviderServiceCategories.Any(link => link.ProviderProfileId == row.Provider.Id && dbContext.ServiceCategories.Any(category => category.Id == link.CategoryId && category.PublicId == serviceId)));
        if (areaId.HasValue) query = query.Where(row => dbContext.ProviderServiceCategories.Any(link => link.ProviderProfileId == row.Provider.Id && dbContext.ProviderServiceAreas.Any(area => area.ProviderServiceCategoryId == link.Id && dbContext.AdministrativeAreas.Any(value => value.Id == area.AdministrativeAreaId && value.PublicId == areaId))));
        if (!string.IsNullOrWhiteSpace(documentStatus)) query = query.Where(row => dbContext.ProviderDocuments.Any(document => document.ProviderProfileId == row.Provider.Id && document.VerificationStatusCode == documentStatus.ToUpper()));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(row => row.User.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new { row.Provider, row.User,
                ServiceCount = dbContext.ProviderServiceCategories.Count(link => link.ProviderProfileId == row.Provider.Id),
                DocumentCount = dbContext.ProviderDocuments.Count(document => document.ProviderProfileId == row.Provider.Id),
                LastQuote = dbContext.Quotes.Where(quote => quote.ProviderProfileId == row.Provider.Id).Select(quote => (DateTime?)quote.UpdatedAt).Max(),
                LastTransaction = dbContext.Transactions.Where(transaction => transaction.ProviderProfileId == row.Provider.Id).Select(transaction => (DateTime?)transaction.UpdatedAt).Max() }).ToListAsync(cancellationToken);
        var userIds = rows.Select(row => row.User.Id).ToArray();
        var roles = await (from userRole in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where userIds.Contains(userRole.UserId) && userRole.RevokedAt == null select new { userRole.UserId, role.Code }).ToListAsync(cancellationToken);
        return new(total, page, pageSize, rows.Select(row => new AdminProviderListItemResponse(row.Provider.PublicId, row.Provider.BusinessName, row.Provider.BusinessName,
            MaskPhone(row.User.Phone), MaskEmail(row.User.Email), MaskBusinessNo(row.Provider.BusinessRegistrationNo), row.User.StatusCode,
            row.Provider.ApprovalStatusCode, row.Provider.ActivityStatusCode, row.ServiceCount, null,
            row.DocumentCount == 0 ? "제출 증빙 없음" : $"제출 {row.DocumentCount}건 · 검증 기준 미확정", row.User.CreatedAt,
            Max(row.LastQuote, row.LastTransaction), roles.Where(role => role.UserId == row.User.Id).Select(role => role.Code).OrderBy(RoleOrder).ToArray())).ToArray());
    }

    public async Task<AdminProviderDetailResponse?> GetAsync(Guid providerPublicId, CancellationToken cancellationToken)
    {
        var identity = await (from provider in dbContext.ProviderProfiles.AsNoTracking() join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                              where provider.PublicId == providerPublicId && dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null && dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                              select new { Provider = provider, User = user }).SingleOrDefaultAsync(cancellationToken);
        if (identity is null) return null;
        var roleRows = await (from userRole in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where userRole.UserId == identity.User.Id && userRole.RevokedAt == null select role.Code).ToListAsync(cancellationToken);
        var roles = roleRows.OrderBy(RoleOrder).ToArray();
        var links = await dbContext.ProviderServiceCategories.AsNoTracking().Where(link => link.ProviderProfileId == identity.Provider.Id).OrderBy(link => link.CreatedAt).ToListAsync(cancellationToken);
        var categories = await dbContext.ServiceCategories.AsNoTracking().ToListAsync(cancellationToken);
        var services = links.Select(link => { var service = categories.Single(category => category.Id == link.CategoryId); var middle = categories.SingleOrDefault(category => category.Id == service.ParentId); var major = middle is null ? null : categories.SingleOrDefault(category => category.Id == middle.ParentId);
            return new AdminProviderServiceResponse(link.Id, service.PublicId, major?.Name ?? "대분류 없음", middle?.Name ?? "중분류 없음", service.Name, link.StatusCode, "서비스별 승인상태 구조 없음", link.ActivatedAt, link.DeactivatedAt); }).ToArray();
        var areas = await (from link in dbContext.ProviderServiceCategories.AsNoTracking() join areaLink in dbContext.ProviderServiceAreas.AsNoTracking() on link.Id equals areaLink.ProviderServiceCategoryId
                           join area in dbContext.AdministrativeAreas.AsNoTracking() on areaLink.AdministrativeAreaId equals area.Id join category in dbContext.ServiceCategories.AsNoTracking() on link.CategoryId equals category.Id
                           where link.ProviderProfileId == identity.Provider.Id select new AdminProviderAreaResponse(category.PublicId, category.Name, area.PublicId, area.AreaName, area.AreaLevelCode, areaLink.StatusCode, areaLink.ActivatedAt, areaLink.DeactivatedAt, false)).ToListAsync(cancellationToken);
        var documents = await (from document in dbContext.ProviderDocuments.AsNoTracking() join file in dbContext.Files.AsNoTracking() on document.FileId equals file.Id
                               join typeValue in dbContext.ProviderDocumentTypes.AsNoTracking() on document.DocumentTypeId equals typeValue.Id into types from type in types.DefaultIfEmpty()
                               join verifierValue in dbContext.Users.AsNoTracking() on document.VerifiedByUserId equals verifierValue.Id into verifiers from verifier in verifiers.DefaultIfEmpty()
                               where document.ProviderProfileId == identity.Provider.Id select new AdminProviderDocumentResponse(document.Id, type != null ? type.Name : document.DocumentTypeCode,
                                   document.DocumentTypeCode, AdminPrivacy.DocumentNumber(document.DocumentNumber), document.IssuedAt, document.ExpiresAt, document.VerificationStatusCode, document.VerifiedAt,
                                   verifier != null ? verifier.LoginId : null, file.OriginalFileName, false, "안전한 관리자 파일 열람 API가 아직 없습니다.")).ToListAsync(cancellationToken);
        var reviews = new List<AdminProviderServiceReviewResponse>();
        foreach (var service in services)
        {
            var categoryId = categories.Single(category => category.PublicId == service.ServiceId).Id;
            var policy = await dbContext.CategoryOperationPolicies.AsNoTracking().Where(item => item.CategoryId == categoryId && item.IsActive).OrderByDescending(item => item.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
            if (policy is null) continue;
            var assignments = await (from assignment in dbContext.CategoryProviderRequirementAssignments.AsNoTracking() join definition in dbContext.ProviderRequirementDefinitions.AsNoTracking() on assignment.RequirementDefinitionId equals definition.Id
                                     where assignment.CategoryOperationPolicyId == policy.Id orderby assignment.DisplayOrder select new { Assignment = assignment, Definition = definition }).ToListAsync(cancellationToken);
            var comparisons = new List<AdminProviderRequirementComparisonResponse>();
            foreach (var item in assignments)
            {
                var evidenceNames = await (from link in dbContext.CategoryProviderRequirementEvidenceTypes.AsNoTracking() join type in dbContext.ProviderDocumentTypes.AsNoTracking() on link.DocumentTypeId equals type.Id where link.RequirementAssignmentId == item.Assignment.Id orderby link.DisplayOrder select type.Name).ToListAsync(cancellationToken);
                var verification = await dbContext.ProviderServiceRequirementVerifications.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == service.InternalId && value.RequirementAssignmentId == item.Assignment.Id, cancellationToken);
                var linkedDocument = verification?.ProviderDocumentId is long documentId ? documents.SingleOrDefault(document => document.InternalId == documentId)?.DocumentTypeName : null;
                comparisons.Add(new(item.Assignment.PublicId, item.Definition.Name, item.Assignment.IsRequired, item.Assignment.VerificationRequired, item.Assignment.ExpiryCheckRequired,
                    evidenceNames, verification?.VerificationStatusCode ?? "검증결과 없음", linkedDocument, verification?.VerifiedAt, verification?.ExpiresAt, verification?.RejectionReason));
            }
            var approval = await dbContext.ProviderServiceApprovals.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == service.InternalId, cancellationToken);
            var approvalEvents = await dbContext.ProviderServiceApprovalEvents.AsNoTracking().Where(value => value.ProviderServiceCategoryId == service.InternalId)
                .OrderByDescending(value => value.DecidedAt).Select(value => new AdminProviderServiceApprovalEventResponse(value.FromStatusCode, value.ToStatusCode, value.ActionCode, value.DecisionReason, value.DecidedAt)).ToListAsync(cancellationToken);
            reviews.Add(new(service.ServiceId, service.ServiceName, service.RegistrationStatusCode,
                approval?.ApprovalStatusCode ?? "PENDING", approval?.ApprovalRequestedAt ?? service.ActivatedAt, approval?.ApprovalDecidedAt,
                approval?.DecisionReason, approval is null ? string.Empty : Convert.ToBase64String(approval.RowVersion), approvalEvents,
                assignments.Count > 0, policy.RequiredQualificationSummaryText, policy.InsuranceRequirementText, policy.SafetyGradeCode, comparisons));
        }
        var quoteRows = await (from quote in dbContext.Quotes.AsNoTracking() join request in dbContext.ServiceRequests.AsNoTracking() on quote.ServiceRequestId equals request.Id join category in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id where quote.ProviderProfileId == identity.Provider.Id orderby quote.CreatedAt descending select new { quote, request, category.Name }).ToListAsync(cancellationToken);
        var quoteIds = quoteRows.Select(row => row.quote.Id).ToArray();
        var revisions = await dbContext.QuoteRevisions.AsNoTracking().Where(value => quoteIds.Contains(value.QuoteId)).GroupBy(value => value.QuoteId).Select(group => group.OrderByDescending(value => value.RevisionNo).First()).ToListAsync(cancellationToken);
        var quotes = quoteRows.Select(row => { var revision = revisions.SingleOrDefault(value => value.QuoteId == row.quote.Id); return new AdminProviderQuoteResponse(row.quote.PublicId, row.request.PublicId, row.request.Title, row.Name, row.quote.SubmittedAt, revision?.TotalAmount, revision?.CurrencyCode ?? "KRW", row.quote.StatusCode, row.quote.StatusCode == "ACCEPTED"); }).ToArray();
        var transactions = await (from transaction in dbContext.Transactions.AsNoTracking() join category in dbContext.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id where transaction.ProviderProfileId == identity.Provider.Id orderby transaction.CreatedAt descending select new AdminProviderTransactionResponse(transaction.PublicId, category.Name, transaction.StatusCode, transaction.AgreedAmount, transaction.CurrencyCode, transaction.StartedAt, transaction.CompletedAt)).ToListAsync(cancellationToken);
        var approvalHistory = await dbContext.ProviderApprovalEvents.AsNoTracking().Where(value => value.ProviderProfileId == identity.Provider.Id).OrderByDescending(value => value.DecidedAt).Select(value => new AdminProviderApprovalEventResponse(value.FromStatusCode, value.ToStatusCode, value.ActionCode, value.Reason, value.DecidedAt)).ToListAsync(cancellationToken);
        var audit = await dbContext.AuditLogs.AsNoTracking().Where(value => value.EntityPublicId == identity.Provider.PublicId || value.EntityPublicId == identity.User.PublicId).OrderByDescending(value => value.OccurredAt).Take(100).Select(value => new AdminCustomerAuditResponse(value.OccurredAt, value.ActionCode, value.EntityType, value.ResultCode, value.Reason, value.ActorRoleCode)).ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var soon = today.AddDays(30);
        return new(new(identity.Provider.PublicId, identity.User.PublicId, identity.Provider.BusinessName, AdminPrivacy.Phone(identity.User.Phone), AdminPrivacy.Email(identity.User.Email), identity.User.StatusCode, identity.Provider.ApprovalStatusCode, identity.Provider.ActivityStatusCode, identity.User.CreatedAt, identity.User.LastLoginAt, roles),
            new(services.Length, null, null, documents.Count, null, documents.Count(value => value.ExpiresAt >= today && value.ExpiresAt <= soon), documents.Count(value => value.ExpiresAt < today), quotes.Length, quotes.Count(value => value.IsAccepted), transactions.Count(value => value.StatusCode == "COMPLETED"), Max(quotes.Select(value => value.SubmittedAt).Max(), transactions.Select(value => value.CompletedAt ?? value.StartedAt).Max())),
            new(identity.Provider.BusinessName, AdminPrivacy.BusinessNumber(identity.Provider.BusinessRegistrationNo), false, "대표자명·사업장 주소·업종·업태 구조가 없습니다."), services, areas, documents, reviews, quotes, transactions, approvalHistory, audit);
    }

    private static string? MaskPhone(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length < 7 ? "연락처 등록됨" : $"{digits[..3]}-****-{digits[^4..]}"; }
    private static string? MaskEmail(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var at = value.IndexOf('@'); return at <= 0 ? "이메일 등록됨" : $"{value[0]}***{value[at..]}"; }
    private static string? MaskBusinessNo(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length == 10 ? $"{digits[..3]}-**-{digits[^5..]}" : "사업자등록번호 등록됨"; }
    private static DateTime? Max(DateTime? left, DateTime? right) => left.HasValue && right.HasValue ? (left > right ? left : right) : left ?? right;
    private static int RoleOrder(string role) => role switch { "CUSTOMER" => 0, "PROVIDER" => 1, "ADMIN" => 2, _ => 9 };
}
