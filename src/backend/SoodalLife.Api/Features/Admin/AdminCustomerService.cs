using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminCustomerService(SoodalLifeDbContext dbContext)
{
    private const int MaximumPageSize = 100;

    public async Task<AdminCustomerListResponse> SearchAsync(
        string? search,
        string? status,
        bool? hasRequests,
        bool? hasTransactions,
        DateOnly? joinedFrom,
        DateOnly? joinedTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) throw Invalid("ADMIN_CUSTOMER_PAGE_INVALID", "페이지는 1 이상이어야 합니다.");
        if (pageSize is < 1 or > MaximumPageSize) throw Invalid("ADMIN_CUSTOMER_PAGE_SIZE_INVALID", $"페이지당 고객 수는 1~{MaximumPageSize}명이어야 합니다.");

        var query =
            from customer in dbContext.CustomerProfiles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on customer.UserId equals user.Id
            where dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RevokedAt == null &&
                dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "CUSTOMER"))
            select new { Customer = customer, User = user };

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            if (Guid.TryParse(normalizedSearch, out var publicId))
            {
                query = query.Where(row => row.Customer.PublicId == publicId || row.User.PublicId == publicId);
            }
            else
            {
                query = query.Where(row =>
                    row.Customer.DisplayName.Contains(normalizedSearch) ||
                    (row.User.Phone != null && row.User.Phone.Contains(normalizedSearch)) ||
                    (row.User.Email != null && row.User.Email.Contains(normalizedSearch)));
            }
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.User.StatusCode == status.Trim().ToUpperInvariant());
        if (joinedFrom.HasValue) query = query.Where(row => row.User.CreatedAt >= joinedFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (joinedTo.HasValue) query = query.Where(row => row.User.CreatedAt < joinedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (hasRequests.HasValue) query = query.Where(row => dbContext.ServiceRequests.Any(request => request.CustomerProfileId == row.Customer.Id) == hasRequests.Value);
        if (hasTransactions.HasValue) query = query.Where(row => dbContext.Transactions.Any(transaction => transaction.CustomerProfileId == row.Customer.Id) == hasTransactions.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageRows = await query
            .OrderByDescending(row => row.User.CreatedAt)
            .ThenBy(row => row.Customer.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new CustomerListRow(
                row.Customer.Id,
                row.Customer.PublicId,
                row.Customer.DisplayName,
                row.User.Phone,
                row.User.Email,
                row.User.StatusCode,
                row.User.CreatedAt,
                dbContext.ServiceRequests.Count(request => request.CustomerProfileId == row.Customer.Id),
                dbContext.Transactions.Count(transaction => transaction.CustomerProfileId == row.Customer.Id),
                dbContext.ServiceRequests.Where(request => request.CustomerProfileId == row.Customer.Id).Select(request => (DateTime?)request.UpdatedAt)
                    .Concat(dbContext.Transactions.Where(transaction => transaction.CustomerProfileId == row.Customer.Id).Select(transaction => (DateTime?)transaction.UpdatedAt))
                    .Concat(dbContext.ServiceHistoryEntries.Where(history => history.CustomerProfileId == row.Customer.Id).Select(history => (DateTime?)history.OccurredAt))
                    .Max()))
            .ToListAsync(cancellationToken);

        var customerUserIds = await dbContext.CustomerProfiles.AsNoTracking()
            .Where(customer => pageRows.Select(row => row.InternalCustomerId).Contains(customer.Id))
            .Select(customer => new { customer.Id, customer.UserId })
            .ToListAsync(cancellationToken);
        var userIds = customerUserIds.Select(item => item.UserId).ToArray();
        var roleRows = await (from userRole in dbContext.UserRoles.AsNoTracking()
                              join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where userIds.Contains(userRole.UserId) && userRole.RevokedAt == null
                              select new { userRole.UserId, role.Code }).ToListAsync(cancellationToken);
        var customerToUser = customerUserIds.ToDictionary(item => item.Id, item => item.UserId);

        return new AdminCustomerListResponse(totalCount, page, pageSize, pageRows.Select(row => new AdminCustomerListItemResponse(
            row.Id,
            row.Name,
            MaskPhone(row.Phone),
            MaskEmail(row.Email),
            row.StatusCode,
            row.JoinedAt,
            roleRows.Where(role => role.UserId == customerToUser[row.InternalCustomerId]).Select(role => role.Code).OrderBy(RoleOrder).ToArray(),
            row.RequestCount,
            row.TransactionCount,
            row.LastUsedAt)).ToArray());
    }

    public async Task<AdminCustomerDetailResponse?> GetDetailAsync(Guid customerPublicId, CancellationToken cancellationToken)
    {
        var identity = await (from customer in dbContext.CustomerProfiles.AsNoTracking()
                              join user in dbContext.Users.AsNoTracking() on customer.UserId equals user.Id
                              where customer.PublicId == customerPublicId &&
                                    dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null &&
                                        dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "CUSTOMER"))
                              select new { Customer = customer, User = user }).SingleOrDefaultAsync(cancellationToken);
        if (identity is null) return null;

        var roleHistory = await (from userRole in dbContext.UserRoles.AsNoTracking()
                                 join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                 where userRole.UserId == identity.User.Id
                                 orderby userRole.GrantedAt
                                 select new AdminCustomerRoleHistoryResponse(role.Code, userRole.GrantedAt, userRole.RevokedAt)).ToListAsync(cancellationToken);
        var activeRoles = roleHistory.Where(role => role.RevokedAt == null).Select(role => role.RoleCode).OrderBy(RoleOrder).ToArray();

        var requestRows = await (from request in dbContext.ServiceRequests.AsNoTracking()
                                 join category in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
                                 join area in dbContext.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                                 where request.CustomerProfileId == identity.Customer.Id
                                 orderby request.CreatedAt descending
                                 select new AdminCustomerRequestResponse(
                                     request.PublicId, request.Title, category.Name, request.CreatedAt, request.StatusCode,
                                     area.AreaName, request.DetailAddress,
                                     dbContext.Quotes.Count(quote => quote.ServiceRequestId == request.Id),
                                     dbContext.Quotes.Any(quote => quote.ServiceRequestId == request.Id && quote.StatusCode == "ACCEPTED")))
            .ToListAsync(cancellationToken);

        var quoteBaseRows = await (from quote in dbContext.Quotes.AsNoTracking()
                                   join request in dbContext.ServiceRequests.AsNoTracking() on quote.ServiceRequestId equals request.Id
                                   join provider in dbContext.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id
                                   where request.CustomerProfileId == identity.Customer.Id
                                   orderby quote.CreatedAt descending
                                   select new { Quote = quote, Request = request, ProviderName = provider.BusinessName }).ToListAsync(cancellationToken);
        var quoteIds = quoteBaseRows.Select(row => row.Quote.Id).ToArray();
        var latestRevisions = await dbContext.QuoteRevisions.AsNoTracking().Where(revision => quoteIds.Contains(revision.QuoteId))
            .GroupBy(revision => revision.QuoteId)
            .Select(group => group.OrderByDescending(revision => revision.RevisionNo).Select(revision => new { revision.QuoteId, revision.TotalAmount, revision.CurrencyCode }).First())
            .ToListAsync(cancellationToken);
        var quotes = quoteBaseRows.Select(row =>
        {
            var revision = latestRevisions.SingleOrDefault(item => item.QuoteId == row.Quote.Id);
            return new AdminCustomerQuoteResponse(row.Quote.PublicId, row.Request.PublicId, row.Request.Title, row.ProviderName,
                revision?.TotalAmount, revision?.CurrencyCode ?? "KRW", row.Quote.StatusCode, row.Quote.SubmittedAt,
                row.Quote.StatusCode == "ACCEPTED", row.Quote.AcceptedAt);
        }).ToArray();

        var transactions = await (from transaction in dbContext.Transactions.AsNoTracking()
                                  join request in dbContext.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                                  join category in dbContext.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                                  join provider in dbContext.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                                  where transaction.CustomerProfileId == identity.Customer.Id
                                  orderby transaction.CreatedAt descending
                                  select new AdminCustomerTransactionResponse(transaction.PublicId, request.PublicId, category.Name, provider.BusinessName,
                                      transaction.StatusCode, transaction.AgreedAmount, null, transaction.CurrencyCode,
                                      transaction.StartedAt, transaction.CompletedAt, null)).ToListAsync(cancellationToken);

        var afterServiceBase = await dbContext.AfterServiceCases.AsNoTracking()
            .Where(item => item.CustomerProfileId == identity.Customer.Id).OrderByDescending(item => item.ReceivedAt).ToListAsync(cancellationToken);
        var afterServiceIds = afterServiceBase.Select(item => item.Id).ToArray();
        var lastActions = await dbContext.AfterServiceActions.AsNoTracking().Where(action => afterServiceIds.Contains(action.AfterServiceCaseId))
            .GroupBy(action => action.AfterServiceCaseId)
            .Select(group => group.OrderByDescending(action => action.OccurredAt).Select(action => new { action.AfterServiceCaseId, action.ActionNote }).First())
            .ToListAsync(cancellationToken);
        var transactionPublicIds = await dbContext.Transactions.AsNoTracking()
            .Where(transaction => afterServiceBase.Select(item => item.TransactionId).Contains(transaction.Id))
            .ToDictionaryAsync(transaction => transaction.Id, transaction => transaction.PublicId, cancellationToken);
        var afterServices = afterServiceBase.Select(item => new AdminCustomerAfterServiceResponse(item.PublicId, transactionPublicIds[item.TransactionId], item.Subject,
            item.StatusCode, item.ReceivedAt, item.CompletedAt, lastActions.SingleOrDefault(action => action.AfterServiceCaseId == item.Id)?.ActionNote)).ToArray();

        var historyBase = await dbContext.ServiceHistoryEntries.AsNoTracking().Where(item => item.CustomerProfileId == identity.Customer.Id)
            .OrderByDescending(item => item.OccurredAt).ToListAsync(cancellationToken);
        var historyTransactionIds = historyBase.Where(item => item.TransactionId.HasValue).Select(item => item.TransactionId!.Value).Distinct().ToArray();
        var historyTransactionPublicIds = await dbContext.Transactions.AsNoTracking().Where(item => historyTransactionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.PublicId, cancellationToken);
        var serviceHistory = historyBase.Select(item => new AdminCustomerServiceHistoryResponse(item.PublicId,
            item.TransactionId.HasValue && historyTransactionPublicIds.TryGetValue(item.TransactionId.Value, out var transactionPublicId) ? transactionPublicId : null,
            item.EventTypeCode, item.Title, item.Summary, item.ProviderNameSnapshot, item.CategoryNameSnapshot,
            item.TotalAmountSnapshot, item.CurrencyCode, item.CompletedAtSnapshot, item.OccurredAt, item.WarrantyEndDate)).ToArray();

        var managementHistory = await dbContext.AuditLogs.AsNoTracking()
            .Where(log => log.EntityPublicId == identity.Customer.PublicId || log.EntityPublicId == identity.User.PublicId)
            .OrderByDescending(log => log.OccurredAt)
            .Take(100)
            .Select(log => new AdminCustomerAuditResponse(log.OccurredAt, log.ActionCode, log.EntityType, log.ResultCode, log.Reason, log.ActorRoleCode))
            .ToListAsync(cancellationToken);

        var requestCount = requestRows.Count;
        var completedTransactionCount = transactions.Count(transaction => transaction.StatusCode == "COMPLETED");
        var inProgressAfterServiceCount = afterServices.Count(item => item.StatusCode is "RECEIVED" or "IN_PROGRESS");
        var lastUsedAt = requestRows.Select(item => (DateTime?)item.CreatedAt)
            .Concat(transactions.Select(item => item.CompletedAt ?? item.StartedAt))
            .Concat(serviceHistory.Select(item => (DateTime?)item.OccurredAt)).Max();

        return new AdminCustomerDetailResponse(
            new AdminCustomerBasicResponse(identity.Customer.PublicId, identity.User.PublicId, identity.Customer.DisplayName,
                identity.User.Phone, identity.User.Email, identity.User.StatusCode, identity.User.CreatedAt, identity.User.LastLoginAt,
                false, "본인인증 구조 없음", activeRoles),
            new AdminCustomerUsageSummaryResponse(requestCount, requestRows.Count(request => request.StatusCode is "OPEN" or "ACCEPTED"),
                completedTransactionCount, inProgressAfterServiceCount, lastUsedAt),
            new AdminCustomerAddressSectionResponse(false, "고객 주소록 구조가 없습니다. 요청별 지역과 상세주소는 요청 탭에서 확인할 수 있습니다.", []),
            requestRows, quotes, transactions,
            new AdminCustomerReviewSectionResponse(false, "후기 저장 구조가 아직 없습니다."),
            afterServices, serviceHistory,
            new AdminCustomerConsentSectionResponse(false, "약관·동의 이력 구조가 아직 없습니다."),
            new AdminCustomerStatusResponse(identity.User.StatusCode, roleHistory, false,
                "CUSTOMER 역할 종료와 User 전체 탈퇴를 구분하는 탈퇴 Workflow가 아직 없습니다."),
            managementHistory);
    }

    private static string? MaskPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 7) return "연락처 등록됨";
        return $"{digits[..3]}-****-{digits[^4..]}";
    }

    private static string? MaskEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var at = value.IndexOf('@');
        if (at <= 0 || at == value.Length - 1) return "이메일 등록됨";
        return $"{value[0]}***{value[at..]}";
    }

    private static int RoleOrder(string role) => role switch { "CUSTOMER" => 0, "PROVIDER" => 1, "ADMIN" => 2, _ => 9 };
    private static AdminServiceCategoryException Invalid(string code, string message) => new(code, message);
    private sealed record CustomerListRow(long InternalCustomerId, Guid Id, string Name, string? Phone, string? Email, string StatusCode,
        DateTime JoinedAt, int RequestCount, int TransactionCount, DateTime? LastUsedAt);
}

