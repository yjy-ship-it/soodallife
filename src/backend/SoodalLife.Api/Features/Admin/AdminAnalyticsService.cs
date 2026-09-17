using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminAnalyticsService(SoodalLifeDbContext db)
{
    private static readonly TimeSpan KoreaOffset = TimeSpan.FromHours(9);
    private static readonly string[] InProgressTransactions = ["CREATED", "IN_PROGRESS", "COMPLETION_SUBMITTED", "REVISION_REQUESTED"];
    private static readonly string[] OpenAfterService = ["RECEIVED", "PROVIDER_CONFIRMED", "VISIT_SCHEDULED", "IN_PROGRESS"];
    private static readonly string[] OpenDisputes = ["OPEN", "UNDER_REVIEW", "WAITING_CUSTOMER", "WAITING_PROVIDER"];

    public async Task<AdminAnalyticsDashboardResponse> GetDashboardAsync(AdminAnalyticsQuery input, CancellationToken token)
    {
        var range = ResolveRange(input);
        var start = ToUtc(range.From);
        var end = ToUtc(range.To.AddDays(1));
        var previousStart = ToUtc(range.PreviousFrom);
        var previousEnd = ToUtc(range.PreviousTo.AddDays(1));

        var categories = await db.ServiceCategories.AsNoTracking()
            .Where(x => x.StatusCode == "ACTIVE")
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryRow(x.Id, x.PublicId, x.ParentId, x.LevelCode, x.Name))
            .ToListAsync(token);
        var categoryIds = ResolveCategoryIds(categories, input.CategoryId);
        var selectedCategory = input.CategoryId.HasValue ? categories.SingleOrDefault(x => x.PublicId == input.CategoryId) : null;

        var areas = await db.AdministrativeAreas.AsNoTracking()
            .Where(x => x.IsActive && x.AreaLevelCode == "SIGUNGU")
            .OrderBy(x => x.AreaName)
            .Select(x => new AreaRow(x.Id, x.PublicId, x.AreaName))
            .ToListAsync(token);
        var selectedArea = input.AreaId.HasValue ? areas.SingleOrDefault(x => x.PublicId == input.AreaId) : null;
        long? providerId = input.ProviderId.HasValue
            ? await db.ProviderProfiles.AsNoTracking().Where(x => x.PublicId == input.ProviderId).Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? -1L
            : null;

        var requests = FilterRequests(db.ServiceRequests.AsNoTracking(), start, end, categoryIds, selectedArea?.Id, providerId);
        var previousRequests = FilterRequests(db.ServiceRequests.AsNoTracking(), previousStart, previousEnd, categoryIds, selectedArea?.Id, providerId);
        var requestIds = requests.Select(x => x.Id);
        var previousRequestIds = previousRequests.Select(x => x.Id);

        var quotes = db.Quotes.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && requestIds.Contains(x.ServiceRequestId));
        var transactions = db.Transactions.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && requestIds.Contains(x.ServiceRequestId));
        if (providerId.HasValue)
        {
            quotes = quotes.Where(x => x.ProviderProfileId == providerId.Value);
            transactions = transactions.Where(x => x.ProviderProfileId == providerId.Value);
        }

        var previousTransactions = db.Transactions.AsNoTracking()
            .Where(x => x.CreatedAt >= previousStart && x.CreatedAt < previousEnd && previousRequestIds.Contains(x.ServiceRequestId));
        if (providerId.HasValue) previousTransactions = previousTransactions.Where(x => x.ProviderProfileId == providerId.Value);

        var totalCustomers = await db.CustomerProfiles.AsNoTracking().LongCountAsync(token);
        var activeCustomers = await (from c in db.CustomerProfiles.AsNoTracking()
                                     join u in db.Users.AsNoTracking() on c.UserId equals u.Id
                                     where u.StatusCode == "ACTIVE"
                                     select c.Id).LongCountAsync(token);
        var newCustomers = await db.CustomerProfiles.AsNoTracking().LongCountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, token);
        var previousNewCustomers = await db.CustomerProfiles.AsNoTracking().LongCountAsync(x => x.CreatedAt >= previousStart && x.CreatedAt < previousEnd, token);
        var providers = db.ProviderProfiles.AsNoTracking();
        var totalProviders = await providers.LongCountAsync(token);
        var approvedProviders = await providers.LongCountAsync(x => x.ApprovalStatusCode == "APPROVED", token);
        var pendingProviders = await providers.LongCountAsync(x => x.ApprovalStatusCode == "PENDING", token);
        var inactiveProviders = await providers.LongCountAsync(x => x.ActivityStatusCode != "ACTIVE", token);
        var newProviders = await providers.LongCountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, token);
        var previousNewProviders = await providers.LongCountAsync(x => x.CreatedAt >= previousStart && x.CreatedAt < previousEnd, token);

        var requestCount = await requests.LongCountAsync(token);
        var previousRequestCount = await previousRequests.LongCountAsync(token);
        var requestStatuses = await requests.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var requestsByCategory = await (from request in requests
                                        join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
                                        group request by new { category.ExternalCode, category.Name } into values
                                        orderby values.LongCount() descending
                                        select new AdminAnalyticsBreakdownResponse(values.Key.ExternalCode ?? values.Key.Name, values.Key.Name, values.LongCount(), "건"))
            .Take(20).ToListAsync(token);
        var requestsByRegion = await (from request in requests
                                      join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                                      group request by new { area.AreaCode, area.AreaName } into values
                                      orderby values.LongCount() descending
                                      select new AdminAnalyticsBreakdownResponse(values.Key.AreaCode, values.Key.AreaName, values.LongCount(), "건"))
            .Take(20).ToListAsync(token);

        var submittedQuotes = await quotes.LongCountAsync(x => x.SubmittedAt != null, token);
        var acceptedQuotes = await quotes.LongCountAsync(x => x.StatusCode == "ACCEPTED", token);
        var notSelectedQuotes = await quotes.LongCountAsync(x => x.StatusCode == "NOT_SELECTED", token);
        var quotedRequestCount = await quotes.Where(x => x.SubmittedAt != null).Select(x => x.ServiceRequestId).Distinct().LongCountAsync(token);
        var averageQuotes = quotedRequestCount == 0 ? 0m : decimal.Divide(submittedQuotes, quotedRequestCount);
        var acceptanceRate = submittedQuotes == 0 ? 0m : decimal.Divide(acceptedQuotes, submittedQuotes) * 100m;

        var transactionCount = await transactions.LongCountAsync(token);
        var previousTransactionCount = await previousTransactions.LongCountAsync(token);
        var transactionStatuses = await transactions.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var agreedAmount = await transactions.SumAsync(x => (decimal?)x.AgreedAmount, token) ?? 0m;

        var walletQuery = from wallet in db.ProviderWallets.AsNoTracking()
                          where !providerId.HasValue || wallet.ProviderProfileId == providerId.Value
                          select wallet;
        var currentWalletBalance = await walletQuery.SumAsync(x => (decimal?)(x.AvailableBalance + x.ReservedBalance), token) ?? 0m;
        var walletIds = walletQuery.Select(x => x.Id);
        var ledger = db.WalletLedgerEntries.AsNoTracking().Where(x => walletIds.Contains(x.WalletId) && x.OccurredAt >= start && x.OccurredAt < end);
        var ledgerTotals = await ledger.GroupBy(x => x.EntryTypeCode).Select(x => new AmountTotal(x.Key, x.Sum(v => v.Amount))).ToListAsync(token);
        var feeCharges = db.FeeCharges.AsNoTracking().Where(x => x.ChargedAt >= start && x.ChargedAt < end && transactions.Select(t => t.Id).Contains(x.TransactionId));
        var feeChargedAmount = await feeCharges.SumAsync(x => (decimal?)x.FeeAmount, token) ?? 0m;
        var previousFeeAmount = await db.FeeCharges.AsNoTracking()
            .Where(x => x.ChargedAt >= previousStart && x.ChargedAt < previousEnd && previousTransactions.Select(t => t.Id).Contains(x.TransactionId))
            .SumAsync(x => (decimal?)x.FeeAmount, token) ?? 0m;
        var feeChargeIds = feeCharges.Select(x => x.Id);
        var restoredAmount = await (from restore in db.FeeRestores.AsNoTracking()
                                    join entry in db.WalletLedgerEntries.AsNoTracking() on restore.LedgerEntryId equals entry.Id
                                    where feeChargeIds.Contains(restore.FeeChargeId)
                                    select (decimal?)entry.Amount).SumAsync(token) ?? 0m;

        var reviews = db.Reviews.AsNoTracking().Where(x => x.SubmittedAt >= start && x.SubmittedAt < end);
        if (providerId.HasValue) reviews = reviews.Where(x => x.ProviderProfileId == providerId.Value);
        var reviewCount = await reviews.LongCountAsync(token);
        var publicReviews = await reviews.LongCountAsync(x => x.VisibilityStatusCode == "PUBLIC", token);
        var hiddenReviews = await reviews.LongCountAsync(x => x.VisibilityStatusCode == "HIDDEN", token);
        var verifiedReviews = await reviews.LongCountAsync(x => x.VerificationStatusCode == "VERIFIED_TRANSACTION" || x.VerificationStatusCode == "VERIFIED_SUBSCRIPTION_VISIT", token);
        var reviewIds = reviews.Select(x => x.Id);
        var ratingAverages = await (from rating in db.ReviewRatings.AsNoTracking()
                                    join item in db.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals item.Id
                                    where reviewIds.Contains(rating.ReviewId)
                                    group rating by new { item.Code, item.Name } into values
                                    orderby values.Key.Name
                                    select new AdminAnalyticsMetricResponse("rating_" + values.Key.Code, values.Key.Name + " 평균", values.Average(x => x.RatingValue), "점", null, null, "평가항목별 평균"))
            .ToListAsync(token);

        var afterServices = db.AfterServiceCases.AsNoTracking().Where(x => x.ReceivedAt >= start && x.ReceivedAt < end);
        if (providerId.HasValue) afterServices = afterServices.Where(x => x.ProviderProfileId == providerId.Value);
        var afterServiceStatuses = await afterServices.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var afterServiceCount = afterServiceStatuses.Sum(x => x.Count);
        var convertedAfterService = await afterServices.LongCountAsync(x => x.ConvertedToDisputeAt != null, token);
        var unresolvedAfterService = await afterServices.LongCountAsync(x => OpenAfterService.Contains(x.StatusCode), token);

        var disputes = db.DisputeCases.AsNoTracking().Where(x => x.ReceivedAt >= start && x.ReceivedAt < end);
        if (providerId.HasValue)
        {
            var providerTransactions = db.Transactions.AsNoTracking().Where(x => x.ProviderProfileId == providerId.Value).Select(x => x.Id);
            var providerVisits = db.SubscriptionVisitSchedules.AsNoTracking().Where(x => x.ProviderProfileId == providerId.Value).Select(x => x.Id);
            disputes = disputes.Where(x => (x.TransactionId != null && providerTransactions.Contains(x.TransactionId.Value)) || (x.SubscriptionVisitScheduleId != null && providerVisits.Contains(x.SubscriptionVisitScheduleId.Value)));
        }
        var disputeStatuses = await disputes.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var disputeIds = disputes.Select(x => x.Id);
        var liabilityTotals = await (from resolution in db.DisputeResolutions.AsNoTracking()
                                     join liability in db.DisputeLiabilityTypes.AsNoTracking() on resolution.LiabilityTypeId equals liability.Id
                                     where resolution.IsCurrent && disputeIds.Contains(resolution.DisputeCaseId)
                                     group resolution by new { liability.Code, liability.Name } into values
                                     select new AdminAnalyticsMetricResponse("liability_" + values.Key.Code, values.Key.Name, values.LongCount(), "건", null, null, "구조화된 최종 귀책판정만 집계"))
            .ToListAsync(token);
        var unresolvedDisputes = disputeStatuses.Where(x => OpenDisputes.Contains(x.Status)).Sum(x => x.Count);

        var reports = db.Reports.AsNoTracking().Where(x => x.ReceivedAt >= start && x.ReceivedAt < end);
        var reportStatuses = await reports.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var sanctions = db.Sanctions.AsNoTracking().Where(x => x.DecidedAt >= start && x.DecidedAt < end);
        if (providerId.HasValue) sanctions = sanctions.Where(x => x.ProviderProfileId == providerId.Value);
        var sanctionStatuses = await sanctions.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var appeals = db.SanctionAppeals.AsNoTracking().Where(x => x.SubmittedAt >= start && x.SubmittedAt < end);
        var appealStatuses = await appeals.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);

        var trustCurrent = db.ProviderTrustScoreCurrent.AsNoTracking();
        if (providerId.HasValue) trustCurrent = trustCurrent.Where(x => x.ProviderProfileId == providerId.Value);
        var trustStatuses = await trustCurrent.GroupBy(x => x.EvaluationStatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var trustGrades = await trustCurrent.Where(x => x.GradeCode != null).GroupBy(x => x.GradeCode!).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var policyStatuses = await db.TrustPolicies.AsNoTracking().GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token);
        var simulations = await db.ProviderTrustCalculationResults.AsNoTracking().LongCountAsync(x => x.CalculationModeCode == "SIMULATION" && x.CalculatedAt >= start && x.CalculatedAt < end, token);

        var subscriptionRequests = db.SubscriptionRequests.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        if (categoryIds is not null) subscriptionRequests = subscriptionRequests.Where(x => categoryIds.Contains(x.ServiceCategoryId));
        if (selectedArea is not null) subscriptionRequests = subscriptionRequests.Where(x => x.AdministrativeAreaId == selectedArea.Id);
        var subscriptionRequestIds = subscriptionRequests.Select(x => x.Id);
        var subscriptionApplications = db.SubscriptionApplications.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && subscriptionRequestIds.Contains(x.SubscriptionRequestId));
        if (providerId.HasValue) subscriptionApplications = subscriptionApplications.Where(x => x.ProviderProfileId == providerId.Value);
        var subscriptionContracts = db.SubscriptionContracts.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        if (categoryIds is not null) subscriptionContracts = subscriptionContracts.Where(x => categoryIds.Contains(x.ServiceCategoryId));
        if (providerId.HasValue) subscriptionContracts = subscriptionContracts.Where(x => x.ProviderProfileId == providerId.Value);
        var subscriptionContractIds = subscriptionContracts.Select(x => x.Id);
        var visits = db.SubscriptionVisitSchedules.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && subscriptionContractIds.Contains(x.SubscriptionContractId));
        var payments = db.SubscriptionPaymentRequests.AsNoTracking().Where(x => x.RequestedAt >= start && x.RequestedAt < end && subscriptionContractIds.Contains(x.SubscriptionContractId));
        var settlements = db.SubscriptionSettlementItems.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && subscriptionContractIds.Contains(x.SubscriptionContractId));
        var monthlySettlements = db.MonthlySettlements.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        var payouts = db.SubscriptionPayouts.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        var refunds = db.SubscriptionRefundAdjustments.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end && subscriptionContractIds.Contains(x.SubscriptionContractId));
        if (providerId.HasValue)
        {
            monthlySettlements = monthlySettlements.Where(x => x.ProviderProfileId == providerId.Value);
            payouts = payouts.Where(x => x.ProviderProfileId == providerId.Value);
        }
        List<AdminAnalyticsMetricResponse> subscriptionMetrics =
        [
            M("subscription_requests", "구독 요청", await subscriptionRequests.LongCountAsync(token)),
            M("subscription_applications", "전문가 신청", await subscriptionApplications.LongCountAsync(token)),
            M("subscription_contracts", "계약", await subscriptionContracts.LongCountAsync(token)),
            M("subscription_active", "활성 계약", await subscriptionContracts.LongCountAsync(x => x.StatusCode == "ACTIVE", token)),
            M("subscription_paused", "정지 계약", await subscriptionContracts.LongCountAsync(x => x.StatusCode == "PAUSED", token)),
            M("subscription_terminated", "해지 계약", await subscriptionContracts.LongCountAsync(x => x.StatusCode == "TERMINATED", token)),
            M("visits_scheduled", "예정 회차", await visits.LongCountAsync(x => x.StatusCode == "SCHEDULED", token)),
            M("visits_completed", "완료 회차", await visits.LongCountAsync(x => x.WorkCompletedAt != null, token)),
            M("visits_confirmed", "고객 확인 회차", await visits.LongCountAsync(x => x.CustomerConfirmedAt != null, token)),
            M("visits_hold", "HOLD 회차", await visits.LongCountAsync(x => x.SettlementStatusCode == "HOLD", token)),
            M("payment_requested", "내부 결제 요청", await payments.LongCountAsync(token), note: "실제 PG 매출이 아닌 내부 결제 Workflow"),
            M("payment_completed", "내부 결제 성공", await payments.LongCountAsync(x => x.StatusCode == "COMPLETED", token), note: "PG 미연동 상태"),
            M("payment_failed", "내부 결제 실패", await payments.LongCountAsync(x => x.StatusCode == "FAILED", token)),
            M("settlement_ready", "정산 준비", await settlements.LongCountAsync(x => x.StatusCode == "READY", token)),
            M("settlement_hold", "정산 HOLD", await settlements.LongCountAsync(x => x.StatusCode == "HOLD" || x.StatusCode == "POLICY_PENDING", token)),
            ..StatusMetrics("monthly_settlement", await monthlySettlements.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token)),
            ..StatusMetrics("payout", await payouts.GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token)),
            ..StatusMetrics("refund", await refunds.Where(x => x.TypeCode == "REFUND").GroupBy(x => x.StatusCode).Select(x => new StatusTotal(x.Key, x.LongCount())).ToListAsync(token)),
        ];

        var interiors = db.InteriorProjects.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        if (categoryIds is not null) interiors = interiors.Where(x => categoryIds.Contains(x.ServiceCategoryId));
        if (providerId.HasValue) interiors = interiors.Where(x => x.SelectedSiteVisitProviderId == providerId.Value || x.SelectedContractorProviderId == providerId.Value);
        var interiorIds = interiors.Select(x => x.Id);
        var interiorContractIds = db.InteriorContracts.AsNoTracking().Where(x => interiorIds.Contains(x.InteriorProjectId)).Select(x => x.Id);
        var interiorWorkStageIds = db.InteriorWorkStages.AsNoTracking().Where(x => interiorIds.Contains(x.InteriorProjectId)).Select(x => x.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Add(KoreaOffset));
        var interiorMetrics = new List<AdminAnalyticsMetricResponse>
        {
            M("interior_projects", "프로젝트", await interiors.LongCountAsync(token)),
            M("interior_site_visit_scheduled", "실측 예정", await db.InteriorSiteVisits.AsNoTracking().LongCountAsync(x => interiorIds.Contains(x.InteriorProjectId) && x.StatusCode == "CONFIRMED" && x.CompletedAt == null, token)),
            M("interior_site_visit_completed", "실측 완료", await db.InteriorSiteVisits.AsNoTracking().LongCountAsync(x => interiorIds.Contains(x.InteriorProjectId) && x.StatusCode == "COMPLETED", token)),
            M("interior_contracts", "계약", await db.InteriorContracts.AsNoTracking().LongCountAsync(x => interiorIds.Contains(x.InteriorProjectId), token)),
            M("interior_stages_active", "진행 공정", await db.InteriorWorkStages.AsNoTracking().LongCountAsync(x => interiorIds.Contains(x.InteriorProjectId) && x.StatusCode != "COMPLETED" && x.ActualStartAt != null, token)),
            M("interior_delayed", "지연 프로젝트", await interiors.LongCountAsync(x => x.ExpectedCompletionDate < today && x.StatusCode != "COMPLETED" && x.StatusCode != "CANCELLED", token)),
            M("interior_changes", "변경 요청", await db.InteriorContractChanges.AsNoTracking().LongCountAsync(x => interiorContractIds.Contains(x.InteriorContractId) && x.RequestedAt >= start && x.RequestedAt < end, token)),
            M("interior_inspections", "검사·보완", await db.InteriorStageInspections.AsNoTracking().LongCountAsync(x => interiorWorkStageIds.Contains(x.WorkStageId) && x.InspectedAt >= start && x.InspectedAt < end, token)),
            M("interior_completed", "완료", await interiors.LongCountAsync(x => x.StatusCode == "COMPLETED", token)),
            M("interior_defects", "하자/A/S", await db.InteriorDefects.AsNoTracking().LongCountAsync(x => interiorIds.Contains(x.InteriorProjectId) && x.CreatedAt >= start && x.CreatedAt < end, token)),
            M("interior_disputes", "분쟁", await db.DisputeCases.AsNoTracking().LongCountAsync(x => x.InteriorProjectId != null && interiorIds.Contains(x.InteriorProjectId.Value) && x.ReceivedAt >= start && x.ReceivedAt < end, token)),
            new("interior_fee_revenue", "인테리어 수수료 매출", null, "원", null, null, "POLICY_PENDING은 확정 매출에서 제외"),
        };

        var notifications = db.Notifications.AsNoTracking().Where(x => x.RecordedAt >= start && x.RecordedAt < end);
        var notificationIds = notifications.Select(x => x.Id);
        var recipients = db.NotificationRecipients.AsNoTracking().Where(x => notificationIds.Contains(x.NotificationId));
        var deliveries = db.NotificationDeliveries.AsNoTracking().Where(x => notificationIds.Contains(x.NotificationId));
        var notificationMetrics = new List<AdminAnalyticsMetricResponse>
        {
            M("notifications_created", "생성 알림", await notifications.LongCountAsync(token)),
            M("notifications_read", "읽음", await recipients.LongCountAsync(x => x.ReadAt != null, token)),
            M("notifications_unread", "미읽음", await recipients.LongCountAsync(x => x.ReadAt == null && x.ArchivedAt == null, token)),
            M("deliveries_web", "WEB Delivery", await deliveries.LongCountAsync(x => x.ChannelCode == "WEB" || x.ChannelCode == "IN_APP", token), note: "내부 WEB 알림"),
            M("deliveries_external", "외부 채널 Delivery", await deliveries.LongCountAsync(x => x.ChannelCode != "WEB" && x.ChannelCode != "IN_APP", token), note: "카카오·SMS·Email·Push 미연동"),
            M("deliveries_success", "성공/전송", await deliveries.LongCountAsync(x => x.StatusCode == "SENT" || x.StatusCode == "DELIVERED", token)),
            M("deliveries_failed", "실패", await deliveries.LongCountAsync(x => x.StatusCode == "FAILED", token)),
            M("deliveries_pending", "대기", await deliveries.LongCountAsync(x => x.StatusCode == "PENDING" || x.StatusCode == "PROCESSING", token)),
            M("delivery_retries", "재시도", await db.NotificationDeliveryAttempts.AsNoTracking().LongCountAsync(x => deliveries.Select(d => d.Id).Contains(x.NotificationDeliveryId) && x.AttemptNo > 1, token)),
        };

        var trend = await BuildTrend(start, end, requests, transactions, feeCharges, token);
        var failedPayments = await payments.LongCountAsync(x => x.StatusCode == "FAILED", token);
        var holdSettlements = await settlements.LongCountAsync(x => x.StatusCode == "HOLD" || x.StatusCode == "POLICY_PENDING", token);
        var delayedInteriors = (long)(interiorMetrics.Single(x => x.Code == "interior_delayed").Value ?? 0m);
        var failedDeliveries = await deliveries.LongCountAsync(x => x.StatusCode == "FAILED", token);
        var pendingReports = reportStatuses.Where(x => x.Status is "RECEIVED" or "UNDER_REVIEW" or "EVIDENCE_REQUESTED").Sum(x => x.Count);
        var analyticsEvents=db.AnalyticsEvents.AsNoTracking().Where(x=>x.OccurredAt>=start&&x.OccurredAt<end);
        var discoveryVisitors=await analyticsEvents.Where(x=>x.EventTypeCode=="SERVICE_DISCOVERY_VIEWED").Select(x=>x.VisitorId).Distinct().LongCountAsync(token);
        var requestVisitors=await analyticsEvents.Where(x=>x.EventTypeCode=="SERVICE_REQUEST_CREATED").Select(x=>x.VisitorId).Distinct().LongCountAsync(token);
        var quoteViews=await analyticsEvents.LongCountAsync(x=>x.EventTypeCode=="QUOTE_VIEWED",token);
        var apiRequests=analyticsEvents.Where(x=>x.EventTypeCode=="API_REQUEST");var apiRequestCount=await apiRequests.LongCountAsync(token);var apiErrors=await apiRequests.LongCountAsync(x=>x.StatusCode>=500,token);var apiAverage=apiRequestCount==0?(decimal?)null:(decimal?)await apiRequests.AverageAsync(x=>x.DurationMs??0,token);
        var quoteResponsePairs=await(from request in requests join quote in quotes.Where(x=>x.SubmittedAt!=null) on request.Id equals quote.ServiceRequestId select new{request.CreatedAt,SubmittedAt=quote.SubmittedAt!.Value}).ToListAsync(token);
        var firstResponseHours=quoteResponsePairs.Count==0?(decimal?)null:(decimal)quoteResponsePairs.GroupBy(x=>x.CreatedAt).Select(g=>(g.Min(x=>x.SubmittedAt)-g.Key).TotalHours).Average();
        var quoteSlaRate=quoteResponsePairs.Count==0?(decimal?)null:(decimal)quoteResponsePairs.GroupBy(x=>x.CreatedAt).Count(g=>(g.Min(x=>x.SubmittedAt)-g.Key)<=TimeSpan.FromHours(24))/quoteResponsePairs.GroupBy(x=>x.CreatedAt).Count()*100;
        var eventMetrics=new List<AdminAnalyticsMetricResponse>{M("service_discovery_visitors","서비스 탐색 방문자",discoveryVisitors,"명"),MN("request_conversion_rate","탐색→요청 전환율",discoveryVisitors==0?null:Math.Round((decimal)requestVisitors/discoveryVisitors*100,2),"%","익명 방문자 쿠키와 로그인 사용자를 중복 제거"),M("quote_views","견적 열람 이벤트",quoteViews),MN("first_quote_hours","첫 견적 평균 도착시간",firstResponseHours is null?null:Math.Round(firstResponseHours.Value,1),"시간"),MN("quote_response_sla","24시간 내 첫 견적률",quoteSlaRate is null?null:Math.Round(quoteSlaRate.Value,1),"%"),MN("api_error_rate","API 5xx 오류율",apiRequestCount==0?null:Math.Round((decimal)apiErrors/apiRequestCount*100,2),"%"),MN("api_average_ms","API 평균 응답시간",apiAverage is null?null:Math.Round(apiAverage.Value,1),"ms")};

        var sections = new List<AdminAnalyticsSectionResponse>
        {
            S("members", "회원", M("customers_total", "전체 고객", totalCustomers, "명"), M("customers_active", "활성 고객", activeCustomers, "명"), M("customers_new", "기간 내 신규 고객", newCustomers, "명", previousNewCustomers), M("providers_total", "전체 전문가", totalProviders, "곳"), M("providers_approved", "승인 전문가", approvedProviders, "곳"), M("providers_pending", "승인대기 전문가", pendingProviders, "곳"), M("providers_inactive", "정지·비활성 전문가", inactiveProviders, "곳"), M("providers_new", "기간 내 신규 전문가", newProviders, "곳", previousNewProviders)),
            S("requests", "서비스 요청", [M("requests_total", "신규 요청", requestCount, previous: previousRequestCount), ..StatusMetrics("request", requestStatuses)]),
            S("quotes", "견적", M("quotes_submitted", "제출 견적", submittedQuotes), M("quotes_average", "요청당 평균 견적", averageQuotes, "건"), M("quotes_accepted", "채택 견적", acceptedQuotes), M("quotes_not_selected", "미채택 견적", notSelectedQuotes), M("quote_acceptance_rate", "견적 채택률", acceptanceRate, "%", note: "제출시각이 있는 견적 중 ACCEPTED 비율")),
            S("transactions", "거래", M("transactions_created", "생성 거래", transactionCount, previous: previousTransactionCount), M("transactions_in_progress", "진행 거래", Sum(transactionStatuses, InProgressTransactions)), M("transactions_completed", "완료 거래", Sum(transactionStatuses, "COMPLETED")), M("transactions_cancelled", "취소 거래", Sum(transactionStatuses, "CANCELLED")), M("transactions_disputed", "분쟁 거래", Sum(transactionStatuses, "DISPUTED")), M("transactions_agreed_amount", "확정 약정금액", agreedAmount, "원", note: "거래의 AgreedAmount 합계이며 본사 매출이 아님")),
            S("wallet", "Wallet·수수료", M("wallet_balance", "현재 잔액", currentWalletBalance, "원"), M("wallet_charge", "결제", Positive(ledgerTotals, "CHARGE"), "원"), M("wallet_use", "차감", Abs(ledgerTotals, "USE"), "원"), M("wallet_refund", "환불", Abs(ledgerTotals, "REFUND"), "원"), M("wallet_adjust", "조정", SumAmount(ledgerTotals, "ADJUST"), "원"), M("fee_charged", "수수료 발생액", feeChargedAmount, "원", previousFeeAmount), M("fee_restored", "복원액", Math.Abs(restoredAmount), "원")),
            S("reviews", "리뷰", [M("reviews_total", "전체 리뷰", reviewCount), M("reviews_public", "공개 리뷰", publicReviews), M("reviews_hidden", "숨김 리뷰", hiddenReviews), M("reviews_verified", "검증된 거래·회차 리뷰", verifiedReviews), ..ratingAverages, new("overall_rating", "임의 종합평점", null, "점", null, null, "종합평점 산식 미확정")]),
            S("after_service", "A/S", M("after_service_received", "접수", afterServiceCount), M("after_service_in_progress", "진행", Sum(afterServiceStatuses, OpenAfterService)), M("after_service_resolved", "해결", Sum(afterServiceStatuses, "RESOLVED")), M("after_service_unresolved", "미해결", unresolvedAfterService), M("after_service_dispute", "분쟁전환", convertedAfterService, note: "A/S 접수 자체를 전문가 귀책으로 해석하지 않음")),
            S("disputes", "분쟁", [M("disputes_received", "접수", disputeStatuses.Sum(x => x.Count)), M("disputes_reviewing", "검토 중", Sum(disputeStatuses, ["OPEN", "UNDER_REVIEW", "WAITING_CUSTOMER", "WAITING_PROVIDER"])), M("disputes_closed", "해결·종결", Sum(disputeStatuses, ["RESOLVED", "CLOSED"])), ..liabilityTotals, new("dispute_liability_warning", "분쟁 발생=귀책", null, "", null, null, "구조화된 최종 판정과 분리")]),
            S("reports_sanctions", "신고·제재", [..StatusMetrics("report", reportStatuses), ..StatusMetrics("sanction", sanctionStatuses), ..StatusMetrics("appeal", appealStatuses)]),
            S("trust", "Trust", [..StatusMetrics("trust", trustStatuses), ..trustGrades.Select(x => M("trust_grade_" + x.Status, "등급 " + x.Status, x.Count, "곳")), ..policyStatuses.Select(x => M("trust_policy_" + x.Status, "정책 " + x.Status, x.Count)), M("trust_simulations", "Simulation", simulations)]),
            new("subscriptions", "수달 케어·정기구독", subscriptionMetrics),
            new("interior", "인테리어", interiorMetrics),
            new("notifications", "알림", notificationMetrics),
            new("funnel_sla", "전환·열람·SLA", eventMetrics),
        };

        var kpis = new List<AdminAnalyticsMetricResponse>
        {
            M("kpi_new_requests", "신규 요청", requestCount, previous: previousRequestCount),
            M("kpi_completed_transactions", "완료 거래", Sum(transactionStatuses, "COMPLETED")),
            M("kpi_fee_charged", "수수료 발생액", feeChargedAmount, "원", previousFeeAmount),
            M("kpi_pending_actions", "처리 필요", pendingProviders + pendingReports + unresolvedDisputes + unresolvedAfterService + failedPayments + holdSettlements + delayedInteriors + failedDeliveries),
        };
        var attention = new List<AdminAnalyticsAttentionResponse>
        {
            A("pending_providers", "승인대기 전문가", pendingProviders, "WARNING", "/admin/providers", "심사 대기"),
            A("pending_reports", "처리대기 신고", pendingReports, "WARNING", "/admin/reports", "신고 발생 자체는 제재·귀책이 아님"),
            A("unresolved_disputes", "미해결 분쟁", unresolvedDisputes, "CRITICAL", "/admin/disputes", "최종 귀책판정 전 사건 포함"),
            A("unresolved_after_service", "미해결 A/S", unresolvedAfterService, "WARNING", "/admin/disputes", "A/S 접수 자체는 전문가 귀책이 아님"),
            A("failed_payments", "실패 결제 Workflow", failedPayments, "CRITICAL", "/admin/subscriptions", "실제 PG 매출과 구분"),
            A("hold_settlements", "HOLD·정책대기 정산", holdSettlements, "CRITICAL", "/admin/subscriptions", "정산 확정 전"),
            A("delayed_interiors", "지연 인테리어", delayedInteriors, "WARNING", "/admin/interior", "예정 완료일 경과"),
            A("failed_deliveries", "실패 Notification Delivery", failedDeliveries, "WARNING", "/admin/notifications", "WEB과 외부 채널을 구분 확인"),
        };

        return new(
            new(range.Range, range.From, range.To, range.PreviousFrom, range.PreviousTo, input.CategoryId, selectedCategory?.Name, input.AreaId, selectedArea?.Name, input.ProviderId),
            kpis, trend, requestsByCategory, requestsByRegion, sections, attention,
            categories.Select(x => new AdminAnalyticsFilterOptionResponse(x.PublicId, CategoryLabel(x.LevelCode, x.Name), ParentName(categories, x.ParentId))).ToArray(),
            areas.Select(x => new AdminAnalyticsFilterOptionResponse(x.PublicId, x.Name)).ToArray(),
            await providers.OrderBy(x=>x.BusinessName).Take(1000).Select(x=>new AdminAnalyticsFilterOptionResponse(x.PublicId,x.BusinessName)).ToArrayAsync(token),
            [
                "AI 자동작성 수용률·추천 정확도: AI 평가 이벤트 없음",
                "일정·금액 준수율·재접수율: 업무별 확정 판정 정책 보완 필요",
                "서버 가용성: 단일 인스턴스 내부 지표 외 외부 가용성 감시는 인프라 연동 필요",
                "인테리어 수수료 매출: FeeAssessmentStatusCode=POLICY_PENDING 제외",
                "외부 알림 성공률: 카카오·SMS·Email·Push 미연동",
                "리뷰 종합평점: 산식 미확정",
            ],
            DateTime.UtcNow);
    }

    private IQueryable<ServiceRequest> FilterRequests(IQueryable<ServiceRequest> query, DateTime start, DateTime end, IReadOnlyCollection<long>? categoryIds, long? areaId, long? providerId)
    {
        query = query.Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        if (categoryIds is not null) query = query.Where(x => categoryIds.Contains(x.CategoryId));
        if (areaId.HasValue) query = query.Where(x => x.AdministrativeAreaId == areaId.Value);
        if (providerId.HasValue) query = query.Where(x => db.Quotes.Any(q => q.ServiceRequestId == x.Id && q.ProviderProfileId == providerId.Value) || db.Transactions.Any(t => t.ServiceRequestId == x.Id && t.ProviderProfileId == providerId.Value));
        return query;
    }

    private async Task<IReadOnlyList<AdminAnalyticsTrendPointResponse>> BuildTrend(DateTime start, DateTime end, IQueryable<ServiceRequest> requests, IQueryable<TransactionRecord> transactions, IQueryable<FeeCharge> fees, CancellationToken token)
    {
        var customerDates = await db.CustomerProfiles.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end).Select(x => x.CreatedAt).ToListAsync(token);
        var providerDates = await db.ProviderProfiles.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < end).Select(x => x.CreatedAt).ToListAsync(token);
        var requestDates = await requests.Select(x => x.CreatedAt).ToListAsync(token);
        var transactionDates = await transactions.Select(x => x.CreatedAt).ToListAsync(token);
        var feeRows = await fees.Select(x => new { x.ChargedAt, x.FeeAmount }).ToListAsync(token);
        var from = ToKoreaDate(start);
        var to = ToKoreaDate(end.AddTicks(-1));
        var result = new List<AdminAnalyticsTrendPointResponse>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            result.Add(new(date,
                customerDates.LongCount(x => ToKoreaDate(x) == date),
                providerDates.LongCount(x => ToKoreaDate(x) == date),
                requestDates.LongCount(x => ToKoreaDate(x) == date),
                transactionDates.LongCount(x => ToKoreaDate(x) == date),
                feeRows.Where(x => ToKoreaDate(x.ChargedAt) == date).Sum(x => x.FeeAmount)));
        }
        return result;
    }

    private static RangeRow ResolveRange(AdminAnalyticsQuery input)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Add(KoreaOffset));
        var code = (input.Range ?? "LAST_30_DAYS").Trim().ToUpperInvariant();
        var (from, to) = code switch
        {
            "TODAY" => (today, today),
            "LAST_7_DAYS" => (today.AddDays(-6), today),
            "THIS_MONTH" => (new DateOnly(today.Year, today.Month, 1), today),
            "LAST_MONTH" => PreviousMonth(today),
            "CUSTOM" when input.From.HasValue && input.To.HasValue => (input.From.Value, input.To.Value),
            _ => (today.AddDays(-29), today),
        };
        if (from > to) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber > 365) from = to.AddDays(-365);
        var days = to.DayNumber - from.DayNumber + 1;
        return new(code, from, to, from.AddDays(-days), from.AddDays(-1));
    }

    private static (DateOnly, DateOnly) PreviousMonth(DateOnly today)
    {
        var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        return (first, first.AddMonths(1).AddDays(-1));
    }

    private static IReadOnlyCollection<long>? ResolveCategoryIds(IReadOnlyList<CategoryRow> rows, Guid? publicId)
    {
        if (!publicId.HasValue) return null;
        var selected = rows.SingleOrDefault(x => x.PublicId == publicId.Value);
        if (selected is null) return [-1];
        var ids = new HashSet<long> { selected.Id };
        var added = true;
        while (added)
        {
            added = false;
            foreach (var row in rows.Where(x => x.ParentId.HasValue && ids.Contains(x.ParentId.Value))) added |= ids.Add(row.Id);
        }
        return ids;
    }

    private static string? ParentName(IReadOnlyList<CategoryRow> rows, long? parentId) => parentId.HasValue ? rows.SingleOrDefault(x => x.Id == parentId.Value)?.Name : null;
    private static DateTime ToUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue) - KoreaOffset, DateTimeKind.Utc);
    private static DateOnly ToKoreaDate(DateTime value) => DateOnly.FromDateTime((value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc)).Add(KoreaOffset));
    private static string CategoryLabel(string level, string name) => level switch { "MAJOR" => $"대분류 · {name}", "MIDDLE" => $"중분류 · {name}", _ => name };
    private static AdminAnalyticsSectionResponse S(string code, string title, params AdminAnalyticsMetricResponse[] metrics) => new(code, title, metrics);
    private static AdminAnalyticsMetricResponse M(string code, string label, decimal value, string unit = "건", decimal? previous = null, string? note = null) => new(code, label, value, unit, previous, Change(value, previous), note);
    private static AdminAnalyticsMetricResponse MN(string code,string label,decimal? value,string unit,string? note=null)=>new(code,label,value,unit,null,null,note);
    private static decimal? Change(decimal value, decimal? previous) => previous is > 0 ? Math.Round((value - previous.Value) / previous.Value * 100m, 1) : null;
    private static AdminAnalyticsAttentionResponse A(string code, string label, long count, string severity, string path, string description) => new(code, label, count, severity, path, description);
    private static long Sum(IEnumerable<StatusTotal> values, params string[] statuses) => values.Where(x => statuses.Contains(x.Status)).Sum(x => x.Count);
    private static decimal SumAmount(IEnumerable<AmountTotal> values, string status) => values.Where(x => x.Type == status).Sum(x => x.Amount);
    private static decimal Positive(IEnumerable<AmountTotal> values, string status) => Math.Max(0, SumAmount(values, status));
    private static decimal Abs(IEnumerable<AmountTotal> values, string status) => Math.Abs(SumAmount(values, status));
    private static IReadOnlyList<AdminAnalyticsMetricResponse> StatusMetrics(string prefix, IEnumerable<StatusTotal> values) => values.OrderBy(x => x.Status).Select(x => M(prefix + "_" + x.Status.ToLowerInvariant(), StatusLabel(x.Status), x.Count)).ToArray();
    private static string StatusLabel(string status) => status switch
    {
        "DRAFT" => "작성중", "OPEN" => "접수·공개", "ACCEPTED" => "채택", "EXPIRED" => "만료", "CANCELLED" => "취소",
        "RECEIVED" => "접수", "UNDER_REVIEW" => "검토 중", "EVIDENCE_REQUESTED" => "증빙 요청", "RESOLVED" => "해결", "CLOSED" => "종결",
        "DECIDED" => "결정", "ACTIVE" => "활성", "RELEASED" => "해제", "APPROVED" => "승인", "REJECTED" => "거절",
        "NEW_OR_EVALUATING" => "신규·평가중", "CALCULATED" => "계산 완료", "LEGACY_UNKNOWN_POLICY" => "정책 불명", "RETIRED" => "종료 정책",
        _ => status,
    };

    private sealed record CategoryRow(long Id, Guid PublicId, long? ParentId, string LevelCode, string Name);
    private sealed record AreaRow(long Id, Guid PublicId, string Name);
    private sealed record StatusTotal(string Status, long Count);
    private sealed record AmountTotal(string Type, decimal Amount);
    private sealed record RangeRow(string Range, DateOnly From, DateOnly To, DateOnly PreviousFrom, DateOnly PreviousTo);
}
