using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminSettlementOperationsService(SoodalLifeDbContext db)
{
    private static readonly string[] CompletedPaymentStatuses = ["COMPLETED", "PAID"];
    private static readonly string[] FailedPaymentStatuses = ["FAILED"];
    private static readonly string[] PendingSettlementStatuses = ["DRAFT", "PREPARED", "APPROVED"];
    private static readonly string[] PendingPayoutStatuses = ["REQUESTED", "APPROVED"];
    private static readonly string[] OpenRefundStatuses = ["REQUESTED", "APPROVED", "PROCESSING"];

    public async Task<AdminSettlementOperationsDashboardResponse> Dashboard(CancellationToken token)
    {
        var walletAvailable = await db.ProviderWallets.AsNoTracking().SumAsync(x => (decimal?)x.AvailableBalance, token) ?? 0;
        var walletReserved = await db.ProviderWallets.AsNoTracking().SumAsync(x => (decimal?)x.ReservedBalance, token) ?? 0;
        var reservedQuoteCount = await db.QuoteFeeReservations.AsNoTracking().CountAsync(x => x.StatusCode == "RESERVED", token);
        var reservedQuoteAmount = await db.QuoteFeeReservations.AsNoTracking().Where(x => x.StatusCode == "RESERVED").SumAsync(x => (decimal?)x.Amount, token) ?? 0;
        var capturedQuoteAmount = await db.QuoteFeeReservations.AsNoTracking().Where(x => x.StatusCode == "CAPTURED").SumAsync(x => (decimal?)x.Amount, token) ?? 0;
        var failedPaymentCount = await db.SubscriptionPaymentRequests.AsNoTracking().CountAsync(x => FailedPaymentStatuses.Contains(x.StatusCode), token);
        var completedPaymentAmount = await db.SubscriptionPaymentRequests.AsNoTracking().Where(x => CompletedPaymentStatuses.Contains(x.StatusCode)).SumAsync(x => (decimal?)x.RequestedAmount, token) ?? 0;
        var pendingSettlementCount = await db.MonthlySettlements.AsNoTracking().CountAsync(x => PendingSettlementStatuses.Contains(x.StatusCode), token);
        var pendingSettlementAmount = await db.MonthlySettlements.AsNoTracking().Where(x => PendingSettlementStatuses.Contains(x.StatusCode)).SumAsync(x => (decimal?)x.NetTotal, token) ?? 0;
        var pendingPayoutCount = await db.SubscriptionPayouts.AsNoTracking().CountAsync(x => PendingPayoutStatuses.Contains(x.StatusCode), token);
        var pendingPayoutAmount = await db.SubscriptionPayouts.AsNoTracking().Where(x => PendingPayoutStatuses.Contains(x.StatusCode)).SumAsync(x => (decimal?)(x.ApprovedAmount ?? x.RequestedAmount), token) ?? 0;
        var pendingAdCount = await db.ProviderAdvertisingApplications.AsNoTracking().CountAsync(x => x.FeeStatusCode == "RESERVED", token);
        var pendingAdAmount = await db.ProviderAdvertisingApplications.AsNoTracking().Where(x => x.FeeStatusCode == "RESERVED").SumAsync(x => (decimal?)x.FeeAmount, token) ?? 0;
        var subscriptionRefundCount = await db.SubscriptionRefundAdjustments.AsNoTracking().CountAsync(x => OpenRefundStatuses.Contains(x.StatusCode), token);
        var subscriptionRefundAmount = await db.SubscriptionRefundAdjustments.AsNoTracking().Where(x => OpenRefundStatuses.Contains(x.StatusCode)).SumAsync(x => (decimal?)(x.ApprovedAmount ?? x.RequestedAmount), token) ?? 0;
        var walletRefundCount = await db.WalletRefundRequests.AsNoTracking().CountAsync(x => OpenRefundStatuses.Contains(x.StatusCode), token);
        var walletRefundAmount = await db.WalletRefundRequests.AsNoTracking().Where(x => OpenRefundStatuses.Contains(x.StatusCode)).SumAsync(x => (decimal?)x.RequestedAmount, token) ?? 0;

        var summary = new AdminSettlementSummaryResponse(walletAvailable, walletReserved, reservedQuoteCount,
            reservedQuoteAmount, capturedQuoteAmount, failedPaymentCount, completedPaymentAmount,
            pendingSettlementCount, pendingSettlementAmount, pendingPayoutCount, pendingPayoutAmount,
            pendingAdCount, pendingAdAmount, subscriptionRefundCount + walletRefundCount,
            subscriptionRefundAmount + walletRefundAmount);

        var quoteFees = await (from reservation in db.QuoteFeeReservations.AsNoTracking()
            join quote in db.Quotes.AsNoTracking() on reservation.QuoteId equals quote.Id
            join provider in db.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id
            orderby reservation.ReservedAt descending
            select new AdminQuoteFeeOperationResponse(reservation.PublicId, quote.PublicId, provider.PublicId,
                provider.BusinessName, reservation.Amount, reservation.CurrencyCode, reservation.StatusCode,
                reservation.ReservedAt, reservation.CapturedAt, reservation.ReleasedAt, reservation.ReleaseReasonCode))
            .Take(200).ToListAsync(token);

        var payments = await (from payment in db.SubscriptionPaymentRequests.AsNoTracking()
            join contract in db.SubscriptionContracts.AsNoTracking() on payment.SubscriptionContractId equals contract.Id
            join provider in db.ProviderProfiles.AsNoTracking() on contract.ProviderProfileId equals provider.Id
            orderby payment.RequestedAt descending
            select new AdminSubscriptionPaymentOperationResponse(payment.PublicId, contract.PublicId,
                provider.BusinessName, payment.BillingPeriodStart, payment.BillingPeriodEnd,
                payment.RequestedAmount, payment.CurrencyCode, payment.StatusCode, payment.RequestedAt,
                payment.CompletedAt, payment.FailureReason)).Take(200).ToListAsync(token);

        var monthly = await (from item in db.MonthlySettlements.AsNoTracking()
            join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
            orderby item.SettlementYear descending, item.SettlementMonth descending, item.CreatedAt descending
            select new AdminMonthlySettlementOperationResponse(item.PublicId, provider.PublicId,
                provider.BusinessName, item.SettlementYear, item.SettlementMonth, item.StatusCode,
                item.GrossTotal, item.FeeTotal, item.AdjustmentTotal, item.NetTotal, item.ItemCount,
                item.ApprovedAt, item.PaidAt)).Take(200).ToListAsync(token);

        var payouts = await (from payout in db.SubscriptionPayouts.AsNoTracking()
            join settlement in db.MonthlySettlements.AsNoTracking() on payout.MonthlySettlementId equals settlement.Id
            join provider in db.ProviderProfiles.AsNoTracking() on payout.ProviderProfileId equals provider.Id
            orderby payout.RequestedAt descending
            select new AdminPayoutOperationResponse(payout.PublicId, settlement.PublicId, provider.PublicId,
                provider.BusinessName, payout.RequestedAmount, payout.ApprovedAmount, payout.StatusCode,
                payout.RequestedAt, payout.CompletedAt, payout.ExternalPayoutReference)).Take(200).ToListAsync(token);

        var ads = await (from application in db.ProviderAdvertisingApplications.AsNoTracking()
            join campaign in db.AdvertisingCampaigns.AsNoTracking() on application.CampaignId equals campaign.Id
            join provider in db.ProviderProfiles.AsNoTracking() on application.ProviderProfileId equals provider.Id
            orderby application.SubmittedAt descending
            select new AdminAdvertisingFeeOperationResponse(application.PublicId, campaign.CampaignName,
                provider.PublicId, provider.BusinessName, application.FeeAmount, application.CurrencyCode,
                application.StatusCode, application.FeeStatusCode, application.AutoRenewEnabled,
                application.SubmittedAt, application.PublishedAt, application.NextRenewalAt)).Take(200).ToListAsync(token);

        var subscriptionRefunds = await (from item in db.SubscriptionRefundAdjustments.AsNoTracking()
            join contract in db.SubscriptionContracts.AsNoTracking() on item.SubscriptionContractId equals contract.Id
            join provider in db.ProviderProfiles.AsNoTracking() on contract.ProviderProfileId equals provider.Id
            orderby item.RequestedAt descending
            select new AdminRefundAdjustmentOperationResponse(item.PublicId, "SUBSCRIPTION", provider.PublicId,
                provider.BusinessName, item.TypeCode, item.RequestedAmount, item.ApprovedAmount,
                item.StatusCode, item.Reason, item.RequestedAt, item.CompletedAt)).Take(150).ToListAsync(token);
        var walletRefunds = await (from item in db.WalletRefundRequests.AsNoTracking()
            join wallet in db.ProviderWallets.AsNoTracking() on item.WalletId equals wallet.Id
            join provider in db.ProviderProfiles.AsNoTracking() on wallet.ProviderProfileId equals provider.Id
            orderby item.RequestedAt descending
            select new AdminRefundAdjustmentOperationResponse(item.PublicId, "WALLET", provider.PublicId,
                provider.BusinessName, "REFUND", item.RequestedAmount, null, item.StatusCode,
                item.RequestReason, item.RequestedAt, item.CompletedAt)).Take(150).ToListAsync(token);
        var refunds = subscriptionRefunds.Concat(walletRefunds).OrderByDescending(x => x.RequestedAt).Take(200).ToList();

        return new(DateTime.UtcNow, summary, quoteFees, payments, monthly, payouts, ads, refunds);
    }

    public async Task<AdminUnifiedLedgerResponse> Ledger(string? source, string? search, DateTime? from,
        DateTime? to, int page, int pageSize, CancellationToken token)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var start = from?.ToUniversalTime() ?? DateTime.UnixEpoch;
        var end = to?.ToUniversalTime() ?? DateTime.UtcNow;
        var normalizedSource = source?.Trim().ToUpperInvariant();
        var walletRows = from entry in db.WalletLedgerEntries.AsNoTracking()
            join wallet in db.ProviderWallets.AsNoTracking() on entry.WalletId equals wallet.Id
            join provider in db.ProviderProfiles.AsNoTracking() on wallet.ProviderProfileId equals provider.Id
            where entry.OccurredAt >= start && entry.OccurredAt <= end
            select new AdminUnifiedLedgerItemResponse("WALLET", entry.PublicId, entry.OccurredAt,
                entry.EntryTypeCode, entry.Amount, "KRW", provider.BusinessName, "RECORDED",
                entry.ReferenceType, entry.ReferencePublicId, entry.Reason, null);
        var subscriptionRows = from entry in db.SubscriptionPaymentLedger.AsNoTracking()
            join contract in db.SubscriptionContracts.AsNoTracking() on entry.SubscriptionContractId equals contract.Id
            join provider in db.ProviderProfiles.AsNoTracking() on contract.ProviderProfileId equals provider.Id
            where entry.OccurredAt >= start && entry.OccurredAt <= end
            select new AdminUnifiedLedgerItemResponse("SUBSCRIPTION", entry.PublicId, entry.OccurredAt,
                entry.EntryTypeCode, entry.Amount, entry.CurrencyCode, provider.BusinessName, "RECORDED",
                entry.ReferenceType, entry.ReferencePublicId, entry.ReasonText, null);
        var auditRows = db.AuditLogs.AsNoTracking()
            .Where(x => x.OccurredAt >= start && x.OccurredAt <= end)
            .Select(x => new AdminUnifiedLedgerItemResponse("AUDIT", x.EntityPublicId, x.OccurredAt,
                x.ActionCode, null, "KRW", x.EntityType, x.ResultCode, x.EntityType,
                x.EntityPublicId, x.Reason, x.ActorRoleCode));

        static IQueryable<AdminUnifiedLedgerItemResponse> ApplySearch(
            IQueryable<AdminUnifiedLedgerItemResponse> query, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return query;
            var q = value.Trim();
            return query.Where(x => x.PartyName.Contains(q) || x.EntryTypeCode.Contains(q)
                || (x.ReferenceType != null && x.ReferenceType.Contains(q))
                || (x.Reason != null && x.Reason.Contains(q)));
        }
        walletRows = ApplySearch(walletRows, search);
        subscriptionRows = ApplySearch(subscriptionRows, search);
        auditRows = ApplySearch(auditRows, search);
        var includeWallet = string.IsNullOrEmpty(normalizedSource) || normalizedSource == "WALLET";
        var includeSubscription = string.IsNullOrEmpty(normalizedSource) || normalizedSource == "SUBSCRIPTION";
        var includeAudit = string.IsNullOrEmpty(normalizedSource) || normalizedSource == "AUDIT";
        var total = 0;
        if (includeWallet) total += await walletRows.CountAsync(token);
        if (includeSubscription) total += await subscriptionRows.CountAsync(token);
        if (includeAudit) total += await auditRows.CountAsync(token);
        var take = checked(page * pageSize);
        var candidates = new List<AdminUnifiedLedgerItemResponse>(Math.Min(total, take * 3));
        if (includeWallet) candidates.AddRange(await walletRows.OrderByDescending(x => x.OccurredAt).Take(take).ToListAsync(token));
        if (includeSubscription) candidates.AddRange(await subscriptionRows.OrderByDescending(x => x.OccurredAt).Take(take).ToListAsync(token));
        if (includeAudit) candidates.AddRange(await auditRows.OrderByDescending(x => x.OccurredAt).Take(take).ToListAsync(token));
        var rows = candidates.OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new(total, page, pageSize, rows);
    }
}
