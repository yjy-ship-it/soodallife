using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminRequestTransactionService(SoodalLifeDbContext db)
{
    private const int MaximumPageSize = 100;

    public async Task<AdminRequestListResponse> SearchRequestsAsync(string? search, string? status, bool? accepted,
        bool? hasTransaction, DateOnly? requestedFrom, DateOnly? requestedTo, int page, int pageSize, CancellationToken token)
    {
        ValidatePage(page, pageSize);
        var query = from request in db.ServiceRequests.AsNoTracking()
                    join customer in db.CustomerProfiles.AsNoTracking() on request.CustomerProfileId equals customer.Id
                    join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
                    join service in db.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                    join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                    join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                    join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                    select new { Request = request, Customer = customer, User = user, Service = service, Middle = middle, Major = major, Area = area };
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term)) query = Guid.TryParse(term, out var id)
            ? query.Where(row => row.Request.PublicId == id)
            : query.Where(row => row.Customer.DisplayName.Contains(term) || row.Service.Name.Contains(term) ||
                row.Middle.Name.Contains(term) || row.Major.Name.Contains(term) || row.Area.AreaName.Contains(term));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.Request.StatusCode == status.Trim().ToUpperInvariant());
        if (accepted.HasValue) query = query.Where(row => (row.Request.StatusCode == "ACCEPTED") == accepted.Value);
        if (hasTransaction.HasValue) query = query.Where(row => db.Transactions.Any(item => item.ServiceRequestId == row.Request.Id) == hasTransaction.Value);
        if (requestedFrom.HasValue) query = query.Where(row => row.Request.CreatedAt >= requestedFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (requestedTo.HasValue) query = query.Where(row => row.Request.CreatedAt < requestedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(row => row.Request.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new AdminRequestListItem(row.Request.PublicId, RequestNumber(row.Request.PublicId), row.Customer.DisplayName,
                MaskPhone(row.User.Phone), row.Service.Name, row.Major.Name + " > " + row.Middle.Name + " > " + row.Service.Name,
                row.Area.AreaName, row.Request.CreatedAt, row.Request.StatusCode,
                db.DispatchCandidates.Count(item => item.ServiceRequestId == row.Request.Id && (item.StatusCode == "ELIGIBLE" || item.StatusCode == "DISPATCHED")),
                db.Quotes.Count(item => item.ServiceRequestId == row.Request.Id && item.SubmittedAt != null),
                row.Request.StatusCode == "ACCEPTED", db.Transactions.Any(item => item.ServiceRequestId == row.Request.Id)))
            .ToListAsync(token);
        return new(total, page, pageSize, rows);
    }

    public async Task<AdminRequestDetailResponse?> GetRequestAsync(Guid publicId, CancellationToken token)
    {
        var row = await (from request in db.ServiceRequests.AsNoTracking()
                         join customer in db.CustomerProfiles.AsNoTracking() on request.CustomerProfileId equals customer.Id
                         join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
                         join service in db.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                         join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                         join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                         join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                         where request.PublicId == publicId
                         select new { Request = request, Customer = customer, User = user, Service = service, Middle = middle, Major = major, Area = area }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var answers = await (from answer in db.RequestAnswers.AsNoTracking()
                             join field in db.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                             where answer.ServiceRequestId == row.Request.Id orderby field.DisplayOrder, field.Id
                             select new AdminRequestAnswerItem(field.Label, field.FieldTypeCode, Answer(answer.ValueText, answer.ValueNumber,
                                 answer.ValueBoolean, answer.ValueDate, answer.ValueDateTime, answer.ValueJson), field.IsRequired)).ToListAsync(token);
        var candidates = await (from candidate in db.DispatchCandidates.AsNoTracking()
                                join provider in db.ProviderProfiles.AsNoTracking() on candidate.ProviderProfileId equals provider.Id
                                where candidate.ServiceRequestId == row.Request.Id orderby candidate.EvaluatedAt descending
                                select new AdminRequestCandidateItem(provider.PublicId, provider.BusinessName, candidate.StatusCode,
                                    candidate.ReasonCode, candidate.CategoryMatch, candidate.AreaMatch, candidate.ApprovalMatch)).ToListAsync(token);
        var quoteRows = await (from quote in db.Quotes.AsNoTracking()
                               join provider in db.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id
                               where quote.ServiceRequestId == row.Request.Id orderby quote.CreatedAt
                               select new { Quote = quote, Provider = provider }).ToListAsync(token);
        var quotes = new List<AdminRequestQuoteItem>();
        foreach (var item in quoteRows)
        {
            var revisions = await db.QuoteRevisions.AsNoTracking().Where(value => value.QuoteId == item.Quote.Id).OrderByDescending(value => value.RevisionNo).ToListAsync(token);
            quotes.Add(new(item.Quote.PublicId, item.Provider.PublicId, item.Provider.BusinessName, item.Quote.SubmittedAt,
                revisions.FirstOrDefault()?.TotalAmount, revisions.FirstOrDefault()?.CurrencyCode ?? "KRW", item.Quote.StatusCode,
                revisions.Count, item.Quote.StatusCode == "ACCEPTED", item.Provider.ApprovalStatusCode));
        }
        var transactionId = await db.Transactions.AsNoTracking().Where(item => item.ServiceRequestId == row.Request.Id).Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(token);
        var acceptedQuote = quotes.SingleOrDefault(item => item.IsAccepted)?.Id;
        var history = await HistoryAsync("SERVICE_REQUEST", row.Request.PublicId, token);
        return new(row.Request.PublicId, RequestNumber(row.Request.PublicId), row.Customer.DisplayName, AdminPrivacy.Phone(row.User.Phone), row.Service.Name,
            row.Major.Name + " > " + row.Middle.Name + " > " + row.Service.Name, row.Area.AreaName, AdminPrivacy.DetailAddress(row.Request.DetailAddress),
            row.Request.Title, row.Request.Description, row.Request.CreatedAt, row.Request.StatusCode,
            transactionId.HasValue ? "채택 전문가에게 공개" : "전문가 비공개", answers, candidates, quotes, acceptedQuote, transactionId, history);
    }

    public async Task<AdminTransactionListResponse> SearchTransactionsAsync(string? search, string? status, DateOnly? createdFrom,
        DateOnly? createdTo, int page, int pageSize, CancellationToken token)
    {
        ValidatePage(page, pageSize);
        var query = from transaction in db.Transactions.AsNoTracking()
                    join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                    join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                    join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                    select new { Transaction = transaction, Category = category, Customer = customer, Provider = provider };
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var id)) query = query.Where(row => row.Transaction.PublicId == id);
            else if (term.StartsWith("거래-", StringComparison.OrdinalIgnoreCase) && term.Length > 3)
            {
                var numberPrefix = term[3..].Replace("-", string.Empty).Trim().ToLowerInvariant();
                query = query.Where(row => row.Transaction.PublicId.ToString().StartsWith(numberPrefix));
            }
            else query = query.Where(row => row.Category.Name.Contains(term) || row.Customer.DisplayName.Contains(term) || row.Provider.BusinessName.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.Transaction.StatusCode == status.Trim().ToUpperInvariant());
        if (createdFrom.HasValue) query = query.Where(row => row.Transaction.CreatedAt >= createdFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (createdTo.HasValue) query = query.Where(row => row.Transaction.CreatedAt < createdTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(row => row.Transaction.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new AdminTransactionListItem(row.Transaction.PublicId, TransactionNumber(row.Transaction.PublicId), row.Category.Name,
                row.Customer.DisplayName, row.Provider.BusinessName, row.Transaction.AgreedAmount, row.Transaction.ActualChargedFeeAmount,
                row.Transaction.CurrencyCode, row.Transaction.StatusCode, row.Transaction.StartedAt, row.Transaction.CompletedAt,
                db.AfterServiceCases.Any(item => item.TransactionId == row.Transaction.Id))).ToListAsync(token);
        return new(total, page, pageSize, rows);
    }

    public async Task<AdminTransactionDetailResponse?> GetTransactionAsync(Guid publicId, CancellationToken token)
    {
        var row = await (from transaction in db.Transactions.AsNoTracking()
                         join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                         join service in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals service.Id
                         join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                         join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                         join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                         join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                         join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                         where transaction.PublicId == publicId
                         select new { Transaction = transaction, Request = request, Service = service, Middle = middle, Major = major,
                             Area = area, Customer = customer, User = user, Provider = provider }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var fee = row.Transaction.CategoryFeePolicyId.HasValue ? new AdminAppliedFeePolicy(
            await db.CategoryFeePolicies.AsNoTracking().Where(item => item.Id == row.Transaction.CategoryFeePolicyId).Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(token),
            row.Transaction.FeePolicyVersionSnapshot, row.Transaction.FeePolicyKindSnapshot, row.Transaction.FeeTransactionTypeSnapshot,
            row.Transaction.FeeCalculationMethodSnapshot, row.Transaction.CalculatedFeeAmount, row.Transaction.ActualChargedFeeAmount,
            row.Transaction.FeeCurrencyCode, row.Transaction.FeeChargeTimingSnapshot, row.Transaction.FeeRestoreRuleSnapshot) : null;
        var feeLink = await (from charge in db.FeeCharges.AsNoTracking()
                             join wallet in db.ProviderWallets.AsNoTracking() on charge.WalletId equals wallet.Id
                             join ledger in db.WalletLedgerEntries.AsNoTracking() on charge.LedgerEntryId equals ledger.Id
                             where charge.TransactionId == row.Transaction.Id
                             select new AdminWalletFeeLink(wallet.PublicId, ledger.PublicId, charge.PublicId, ledger.EntryTypeCode,
                                 ledger.Amount, ledger.BalanceAfter, charge.RestoreStatusCode)).SingleOrDefaultAsync(token);
        var items = await db.QuoteItems.AsNoTracking().Where(item => item.QuoteRevisionId == row.Transaction.AcceptedQuoteRevisionId)
            .OrderBy(item => item.LineNo).Select(item => new AdminTransactionQuoteItem(item.LineNo, item.ItemName, item.Description,
                item.Quantity, item.UnitText, item.UnitPriceAmount, item.LineTotalAmount)).ToListAsync(token);
        var completion = await db.WorkCompletions.AsNoTracking().Where(item => item.TransactionId == row.Transaction.Id)
            .Select(item => new AdminCompletionSummary(true, item.StatusCode,
                db.WorkCompletionRevisions.Count(revision => revision.WorkCompletionId == item.Id), item.FirstSubmittedAt, item.ConfirmedAt))
            .SingleOrDefaultAsync(token) ?? new(false, null, 0, null, null);
        var afterServices = await db.AfterServiceCases.AsNoTracking().Where(item => item.TransactionId == row.Transaction.Id)
            .OrderByDescending(item => item.ReceivedAt).Select(item => new AdminAfterServiceSummary(item.PublicId, item.Subject,
                item.StatusCode, item.ReceivedAt, item.CompletedAt)).ToListAsync(token);
        var directPayment = await db.TransactionDirectPayments.AsNoTracking().Where(item => item.TransactionId == row.Transaction.Id)
            .Select(item => new AdminDirectPaymentSummary(item.PublicId, item.StatusCode, item.Amount, item.CurrencyCode,
                item.PaymentMethodCode, item.PaidAt, item.RegisteredByRoleCode, item.RegisteredAt, item.DecidedAt, item.RejectionReason))
            .SingleOrDefaultAsync(token);
        return new(row.Transaction.PublicId, TransactionNumber(row.Transaction.PublicId), row.Transaction.StatusCode, row.Service.Name,
            row.Major.Name + " > " + row.Middle.Name + " > " + row.Service.Name, row.Customer.DisplayName, AdminPrivacy.Phone(row.User.Phone),
            row.Provider.BusinessName, row.Request.Title, row.Area.AreaName, AdminPrivacy.DetailAddress(row.Request.DetailAddress), row.Transaction.AgreedAmount,
            row.Transaction.CurrencyCode, row.Transaction.CreatedAt, row.Transaction.StartedAt, row.Transaction.CompletedAt,
            fee, feeLink, items, completion, directPayment, afterServices, await HistoryAsync("TRANSACTION", row.Transaction.PublicId, token));
    }

    private async Task<IReadOnlyList<AdminOperationHistoryItem>> HistoryAsync(string entityType, Guid id, CancellationToken token) =>
        await db.AuditLogs.AsNoTracking().Where(item => item.EntityType == entityType && item.EntityPublicId == id)
            .OrderByDescending(item => item.OccurredAt).Take(100)
            .Select(item => new AdminOperationHistoryItem(item.OccurredAt, item.ActionCode, item.ResultCode, item.Reason)).ToListAsync(token);
    private static string? Answer(string? text, decimal? number, bool? boolean, DateOnly? date, DateTime? dateTime, string? json) =>
        text ?? number?.ToString("0.####") ?? (boolean.HasValue ? boolean.Value ? "예" : "아니요" : null) ??
        date?.ToString("yyyy-MM-dd") ?? dateTime?.ToString("yyyy-MM-dd HH:mm") ?? JsonAnswer(json);
    private static string? JsonAnswer(string? json) { if (json is null) return null; try { return JsonSerializer.Deserialize<JsonElement>(json).ToString(); } catch { return "확인 필요"; } }
    private static string RequestNumber(Guid id) => "요청-" + id.ToString("N")[..8].ToUpperInvariant();
    private static string TransactionNumber(Guid id) => "거래-" + id.ToString("N")[..8].ToUpperInvariant();
    private static string? MaskPhone(string? phone) => string.IsNullOrWhiteSpace(phone) || phone.Length < 4 ? phone : phone[..Math.Min(3, phone.Length - 4)] + "-****-" + phone[^4..];
    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize)
            throw new AdminServiceCategoryException("ADMIN_PAGE_INVALID", "페이지와 페이지당 조회 건수를 확인해 주세요.");
    }
}
