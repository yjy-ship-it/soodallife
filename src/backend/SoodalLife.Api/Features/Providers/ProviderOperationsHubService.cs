using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Features.Emergency;
using SoodalLife.Api.Features.Interior;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public sealed class ProviderOperationsHubService(
    SoodalLifeDbContext db,
    ProviderConfigurationService configuration,
    RequestMatchingService matching,
    WorkService work,
    ProviderCareService care,
    ProviderInteriorService interior,
    EmergencyWorkflowService emergency,
    ProviderEmergencyAvailabilityService emergencyAvailability,
    ChatService chat,
    ProviderAftercareService aftercare,
    ProviderWalletService wallet)
{
    private const int MaximumPageSize = 50;
    private static readonly string[] ClosedTransactions = ["COMPLETED", "CANCELLED", "DISPUTED"];

    public async Task<ProviderOperationsHubResponse> GetAsync(
        ClaimsPrincipal principal, string? group, string? domain, int page, int pageSize, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        var identity = await Identity(principal, token);
        var legacy = await configuration.GetOperationsDashboardAsync(principal, token);
        var onboarding = await configuration.GetDashboardAsync(principal, token);
        var requirements = await configuration.GetRequirementsAsync(principal, token);
        var matched = await matching.GetInboxAsync(principal, token);
        var transactions = await work.GetProviderTransactionsAsync(principal, token);
        var careDashboard = await care.Dashboard(principal, token);
        var careRequests = await care.OpenRequests(principal, token);
        var careContracts = await care.Contracts(principal, null, token);
        var careVisits = await care.Visits(principal, null, token);
        var careChanges = await care.ScheduleChanges(principal, token);
        var interiorDashboard = await interior.Dashboard(principal, token);
        var interiorProjects = await interior.Projects(principal, null, null, token);
        var emergencyState = await emergencyAvailability.GetAsync(principal, token);
        var emergencyRequests = await emergency.ProviderRequests(principal, token);
        var emergencyAssignments = await emergency.ProviderAssignments(principal, token);
        var chats = await chat.Mine(principal, token);
        var afterServices = await aftercare.AfterServices(principal, token);
        var disputes = await aftercare.Disputes(principal, token);
        var walletDashboard = await wallet.GetDashboardAsync(principal, token);

        var emergencyRequestIds = emergencyRequests.Select(x => x.RequestId).ToHashSet();
        var emergencyTransactionIds = emergencyAssignments.Select(x => x.TransactionId).ToHashSet();
        var generalRequests = matched.Where(x => !emergencyRequestIds.Contains(x.RequestId) && x.RequestStatus == "OPEN").ToArray();
        var generalTransactions = transactions.Where(x => !emergencyTransactionIds.Contains(x.Id)).ToArray();
        var items = new List<ProviderHubWorkItem>();
        var schedule = new List<ProviderHubScheduleItem>();

        foreach (var request in generalRequests)
            items.Add(Item("GENERAL_REQUEST", request.RequestId, request.Summary, request.CategoryPath, request.DispatchStatus,
                "NEW", request.DesiredAt, "새 요청", $"/provider/matched-requests/{request.RequestId}", "요청을 확인하고 견적을 제출하세요.", "GENERAL"));

        foreach (var transaction in generalTransactions.Where(x => !ClosedTransactions.Contains(x.Status)))
        {
            var priority = transaction.Status switch
            {
                "COMPLETION_SUBMITTED" => "WAITING_CUSTOMER",
                "REVISION_REQUESTED" => "ACTION_REQUIRED",
                "CREATED" => "ACTION_REQUIRED",
                _ => "IN_PROGRESS"
            };
            var type = transaction.Status == "COMPLETION_SUBMITTED" ? "GENERAL_COMPLETION" : "GENERAL_APPOINTMENT";
            items.Add(Item(type, transaction.Id, transaction.CategoryPath, transaction.AreaName, transaction.Status,
                priority, null, StatusLabel(transaction.Status), $"/provider/work/{transaction.Id}", GeneralAction(transaction.Status), "GENERAL"));
        }

        var appointmentRows = await (from appointment in db.TransactionAppointments.AsNoTracking()
                                     join transaction in db.Transactions.AsNoTracking() on appointment.TransactionId equals transaction.Id
                                     join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                                     join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                                     where transaction.ProviderProfileId == identity.ProviderId && appointment.StatusCode == "CONFIRMED" &&
                                           appointment.ScheduledStartAt >= today && appointment.ScheduledStartAt < tomorrow
                                     select new { appointment, transaction.PublicId, request.IsUrgent, category.Name }).ToListAsync(token);
        foreach (var row in appointmentRows)
        {
            var domainCode = row.IsUrgent ? "EMERGENCY" : "GENERAL";
            schedule.Add(new("GENERAL_APPOINTMENT", row.PublicId, row.Name, row.appointment.StatusCode,
                row.appointment.ScheduledStartAt, row.appointment.ScheduledEndAt, $"/provider/work/{row.PublicId}", domainCode));
        }

        var generalTransactionIds = await (from transaction in db.Transactions.AsNoTracking()
                                           join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                                           where transaction.ProviderProfileId == identity.ProviderId && !request.IsUrgent
                                           select transaction.Id).ToArrayAsync(token);
        var generalAppointmentIds = db.TransactionAppointments.AsNoTracking()
            .Where(x => generalTransactionIds.Contains(x.TransactionId)).Select(x => x.Id);
        var generalAppointmentActions = await db.TransactionAppointments.AsNoTracking().CountAsync(
            x => generalTransactionIds.Contains(x.TransactionId) && x.StatusCode == "PROPOSED" && x.CreatedByUserId != identity.UserId, token);
        generalAppointmentActions += await db.TransactionAppointmentChangeRequests.AsNoTracking().CountAsync(
            x => generalAppointmentIds.Contains(x.TransactionAppointmentId) && x.StatusCode == "REQUESTED" && x.RequestedByUserId != identity.UserId, token);

        foreach (var request in careRequests)
            items.Add(Item("CARE_REQUEST", request.Id, request.ServiceName, request.AreaName, request.StatusCode, "NEW", null,
                request.HasApplied ? "지원 완료" : "모집 중", "/provider/care/requests", request.HasApplied ? "선택 결과를 기다리세요." : "지원 조건을 확인하세요.", "CARE"));
        foreach (var change in careChanges.Where(x => x.RequiresProviderDecision))
            items.Add(Item("CARE_SCHEDULE_CHANGE", change.Id, change.ServiceName, $"{change.OldStartAt:g} → {change.NewStartAt:g}", change.StatusCode,
                "ACTION_REQUIRED", change.NewStartAt, "응답 필요", "/provider/care/schedule-changes", "일정변경을 승인하거나 거절하세요.", "CARE"));
        foreach (var visit in careVisits.Where(x => x.StatusCode is "SCHEDULED" or "RESCHEDULED" or "IN_PROGRESS" or "PROVIDER_COMPLETED"))
        {
            var priority = visit.StatusCode == "PROVIDER_COMPLETED" ? "WAITING_CUSTOMER" : visit.ScheduledStartAt < tomorrow ? "TODAY" : "IN_PROGRESS";
            items.Add(Item("CARE_VISIT", visit.Id, visit.ServiceName, $"{visit.ContractNumber} · {visit.VisitNo}회차", visit.StatusCode,
                priority, visit.ScheduledStartAt, StatusLabel(visit.StatusCode), $"/provider/care/visits/{visit.Id}", CareAction(visit.StatusCode), "CARE"));
            if (visit.StatusCode is not ("CANCELLED" or "SKIPPED"))
                schedule.Add(new("CARE_VISIT", visit.Id, $"{visit.ServiceName} {visit.VisitNo}회차", visit.StatusCode,
                    visit.ScheduledStartAt, visit.ScheduledEndAt, $"/provider/care/visits/{visit.Id}", "CARE"));
        }

        foreach (var project in interiorProjects.Where(x => x.StatusCode is not ("COMPLETED" or "CANCELLED")))
        {
            var type = InteriorType(project.NextAction, project.Roles);
            var priority = project.CustomerActionRequired ? "WAITING_CUSTOMER" : project.NextAt.HasValue && project.NextAt.Value < tomorrow ? "TODAY" : "IN_PROGRESS";
            items.Add(Item(type, project.Id, project.ServiceName, $"{project.AreaName} · {string.Join(" · ", project.Roles)}", project.StatusCode,
                priority, project.NextAt, project.PendingActionCount > 0 ? $"미처리 {project.PendingActionCount}건" : StatusLabel(project.StatusCode),
                $"/provider/interior/projects/{project.Id}", project.NextAction, "INTERIOR"));
            if (project.NextAt.HasValue)
                schedule.Add(new(type, project.Id, project.ServiceName, project.StatusCode, project.NextAt.Value, null,
                    $"/provider/interior/projects/{project.Id}", "INTERIOR"));
        }

        foreach (var request in emergencyRequests.Where(x => x.ResponseStatus is null or "PENDING"))
            items.Add(Item("EMERGENCY_REQUEST", request.RequestId, request.Title, $"{request.CategoryName} · {request.AreaName}", request.DispatchStatus,
                "URGENT", request.ResponseDeadlineAt, "긴급 응답", "/provider/emergency", "출동 가능 여부와 ETA를 응답하세요.", "EMERGENCY"));
        foreach (var assignment in emergencyAssignments.Where(x => !ClosedTransactions.Contains(x.TransactionStatus)))
            items.Add(Item("EMERGENCY_ACTIVE", assignment.TransactionId, assignment.Title, $"{assignment.CategoryName} · {assignment.AreaName}", assignment.EmergencyStatus,
                "URGENT", null, StatusLabel(assignment.EmergencyStatus), $"/provider/emergency/{assignment.TransactionId}", "현재 출동 단계를 확인하세요.", "EMERGENCY"));

        foreach (var room in chats.Where(x => x.UnreadCount > 0))
            items.Add(Item("CHAT_UNREAD", room.Id, room.CounterpartyDisplayName, room.ServiceName, room.StatusCode,
                "ACTION_REQUIRED", room.LastMessageAt, $"새 메시지 {room.UnreadCount}개", $"/provider/messages/{room.Id}", "메시지를 확인하세요.", "CHAT"));

        foreach (var item in afterServices.Where(x => x.Status is not ("RESOLVED" or "UNRESOLVED_CLOSED" or "CONVERTED_TO_DISPUTE")))
        {
            items.Add(Item("AFTER_SERVICE", item.Id, item.Subject, item.CaseNumber, item.Status, "ISSUE", item.ScheduledAt,
                item.DisplayStatus, $"/provider/after-services/{item.Id}", item.Status == "RECEIVED" ? "접수를 확인하세요." : "처리 상태를 확인하세요.", "AFTER_SERVICE"));
            if (item.ScheduledAt.HasValue)
                schedule.Add(new("AFTER_SERVICE", item.Id, item.Subject, item.Status, item.ScheduledAt.Value, null,
                    $"/provider/after-services/{item.Id}", "AFTER_SERVICE"));
        }
        foreach (var item in disputes.Where(x => x.Status is not ("RESOLVED" or "CLOSED")))
            items.Add(Item("DISPUTE", item.Id, item.Subject, item.CaseNumber, item.Status, "ISSUE", item.LastActionAt,
                item.DisplayStatus, $"/provider/disputes/{item.Id}", "분쟁 내용과 소명 필요 여부를 확인하세요.", "DISPUTE"));

        foreach (var requirement in requirements.Where(x => x.IsRequired && (x.DocumentId is null || x.VerificationStatus is "REJECTED" or "EXPIRED")))
            items.Add(Item("VERIFICATION", requirement.VerificationId, requirement.RequirementName, requirement.CategoryPath,
                requirement.VerificationStatus, "OPERATIONS", requirement.ExpiresAt?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), StatusLabel(requirement.VerificationStatus),
                "/provider/documents", requirement.DocumentId is null ? "증빙을 등록하세요." : "증빙을 보완하세요.", "VERIFICATION"));
        if (walletDashboard.StatusCode != "ACTIVE" || walletDashboard.AvailableBalance <= 0 || walletDashboard.WithdrawalRefundRequired)
            items.Add(Item("WALLET_ALERT", walletDashboard.WalletId, "충전금 상태 확인", $"가용잔액 {walletDashboard.AvailableBalance:N0}원",
                walletDashboard.StatusCode, "OPERATIONS", null, walletDashboard.AvailableBalance <= 0 ? "잔액 확인" : StatusLabel(walletDashboard.StatusCode),
                "/provider/wallet", "충전금과 최근 원장을 확인하세요.", "WALLET"));

        var ordered = items.OrderBy(x => Priority(x.PriorityGroup)).ThenBy(x => x.ScheduledAt ?? DateTime.MaxValue).ThenBy(x => x.Title).ToList();
        if (!string.IsNullOrWhiteSpace(group)) ordered = ordered.Where(x => x.PriorityGroup.Equals(group.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(domain)) ordered = ordered.Where(x => x.Domain.Equals(domain.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var total = ordered.Count;
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        var recentChats = chats.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).Take(5)
            .Select(x => new ProviderHubChatItem(x.Id, x.CounterpartyDisplayName, x.ServiceName, x.LastMessageAt, x.UnreadCount, $"/provider/messages/{x.Id}")).ToArray();
        var missingEvidence = requirements.Count(x => x.IsRequired && x.DocumentId is null);
        var rejectedEvidence = requirements.Count(x => x.VerificationStatus == "REJECTED");
        var expiredEvidence = requirements.Count(x => x.VerificationStatus == "EXPIRED");
        var walletLatest = walletDashboard.RecentLedger.FirstOrDefault();

        var summary = BuildSummary(legacy, careDashboard, interiorDashboard, emergencyRequests, emergencyAssignments, chats,
            afterServices, disputes, walletDashboard, onboarding, requirements,
            generalRequests.Length, generalAppointmentActions, generalTransactions.Count(x => x.Status == "IN_PROGRESS"),
            generalTransactions.Count(x => x.Status == "COMPLETION_SUBMITTED"), generalTransactions.Count(x => x.Status == "REVISION_REQUESTED"),
            appointmentRows.Count(x => !x.IsUrgent), interiorProjects.Count(x => x.NextAt >= today && x.NextAt < tomorrow));
        return new(now, summary, new(pageItems, page, pageSize, total, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))),
            schedule.OrderBy(x => x.ScheduledAt).Take(50).ToArray(), recentChats,
            new(emergencyState.IsEnabled, emergencyState.IsCurrentlyAvailable, emergencyState.CurrentAvailabilityReason,
                emergencyRequests.Count(x => x.ResponseStatus is null or "PENDING"), emergencyRequests.Count(x => x.ResponseStatus == "AVAILABLE"),
                emergencyAssignments.Count(x => !ClosedTransactions.Contains(x.TransactionStatus)), TodayAvailability(emergencyState), "/provider/emergency"),
            new(walletDashboard.AvailableBalance, walletDashboard.ReservedBalance, walletDashboard.CurrencyCode, walletDashboard.StatusCode,
                walletLatest?.EntryTypeCode, walletLatest?.Amount, walletLatest?.OccurredAt,
                walletDashboard.StatusCode != "ACTIVE" || walletDashboard.AvailableBalance <= 0 || walletDashboard.WithdrawalRefundRequired, "/provider/wallet"),
            new(onboarding.ApprovalStatus, onboarding.ActivityStatus, onboarding.PendingServiceCount, onboarding.RejectedServiceCount,
                missingEvidence, rejectedEvidence, expiredEvidence, onboarding.NextActions, "/provider/onboarding"));
    }

    private static IReadOnlyList<ProviderHubMetricGroup> BuildSummary(
        ProviderOperationsDashboardResponse general, ProviderCareDashboardResponse care, ProviderInteriorDashboard interior,
        IReadOnlyList<EmergencyProviderRequestItem> emergencyRequests, IReadOnlyList<EmergencyProviderAssignmentItem> assignments,
        IReadOnlyList<ChatRoomListItem> chats, IReadOnlyList<ProviderAfterServiceListItem> afterServices,
        IReadOnlyList<ProviderDisputeListItem> disputes, ProviderWalletDashboardResponse wallet,
        ProviderOnboardingDashboardResponse onboarding, IReadOnlyList<ProviderRequirementResponse> requirements,
        int newGeneral, int generalAppointmentActions, int generalInProgress, int generalWaitingConfirmation,
        int generalRevisions, int todayGeneral, int todayInterior)
    {
        var emergencyActive = assignments.Count(x => !ClosedTransactions.Contains(x.TransactionStatus));
        var unreadChat = chats.Sum(x => x.UnreadCount);
        return
        [
            Group("TODAY", "오늘", M("today-general", "일반 일정", todayGeneral, "/provider/schedule"), M("today-care", "Care 방문", care.TodayVisitCount, "/provider/care/visits"), M("today-interior", "Interior 일정·공정", todayInterior, "/provider/interior/projects"), M("today-emergency", "긴급출동 중", emergencyActive, "/provider/emergency"), M("today-completion", "완료 예정·보고", generalWaitingConfirmation + care.WaitingCustomerConfirmationCount + interior.CompletionPendingCount, "/provider/progress")),
            Group("NEW", "새 업무", M("new-general", "신규 일반 요청", newGeneral, "/provider/matched-requests"), M("new-care", "Care 모집 요청", care.OpenRequestCount, "/provider/care/requests"), M("new-emergency", "긴급 요청", emergencyRequests.Count(x => x.ResponseStatus is null or "PENDING"), "/provider/emergency", "urgent"), M("new-chat", "새 메시지", unreadChat, "/provider/messages")),
            Group("ACTION_REQUIRED", "응답·결정 필요", M("quote-needed", "견적·일정 응답", newGeneral + generalAppointmentActions, "/provider/inbox"), M("care-decision", "Care 지원·일정변경", care.OpenRequestCount + care.PendingScheduleChangeCount, "/provider/care"), M("emergency-response", "긴급출동 응답", emergencyRequests.Count(x => x.ResponseStatus is null or "PENDING"), "/provider/emergency", "urgent"), M("interior-action", "Interior 조치", interior.StageUpdatePendingCount + interior.CompletionPendingCount, "/provider/interior/projects"), M("revision", "고객 보완 요청", generalRevisions, "/provider/work")),
            Group("IN_PROGRESS", "진행 중", M("general-progress", "일반 거래", generalInProgress, "/provider/progress?domain=GENERAL"), M("care-progress", "Care 계약·회차", care.ActiveContractCount, "/provider/progress?domain=CARE"), M("interior-progress", "Interior 프로젝트", interior.ActiveProjectCount, "/provider/progress?domain=INTERIOR"), M("emergency-progress", "긴급출동", emergencyActive, "/provider/progress?domain=EMERGENCY")),
            Group("WAITING_CUSTOMER", "고객 확인 대기", M("general-wait", "일반 완료보고", generalWaitingConfirmation, "/provider/progress?group=WAITING_CUSTOMER"), M("care-wait", "Care 완료보고", care.WaitingCustomerConfirmationCount, "/provider/care/visits"), M("interior-wait", "Interior 완료확인", interior.CompletionPendingCount, "/provider/interior/projects")),
            Group("ISSUE", "A/S·분쟁", M("after-service", "A/S 처리", afterServices.Count(x => x.Status is not ("RESOLVED" or "UNRESOLVED_CLOSED" or "CONVERTED_TO_DISPUTE")), "/provider/after-services"), M("dispute", "분쟁 대응", disputes.Count(x => x.Status is not ("RESOLVED" or "CLOSED")), "/provider/disputes")),
            Group("OPERATIONS", "운영", M("wallet", $"Wallet {wallet.AvailableBalance:N0}원", wallet.StatusCode == "ACTIVE" ? 0 : 1, "/provider/wallet"), M("notification", "미확인 알림", general.UnreadNotificationCount, "/provider/notifications"), M("approval", "승인 보완", onboarding.RejectedServiceCount + onboarding.PendingServiceCount, "/provider/approval"), M("evidence", "증빙 보완", requirements.Count(x => x.IsRequired && (x.DocumentId is null || x.VerificationStatus is "REJECTED" or "EXPIRED")), "/provider/documents"))
        ];
    }

    private async Task<(long UserId, long ProviderId)> Identity(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var userId))
            throw new ProviderConfigurationException("PROVIDER_IDENTITY_INVALID", "공급자 로그인 정보를 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
        return await (from user in db.Users.AsNoTracking()
                      join provider in db.ProviderProfiles.AsNoTracking() on user.Id equals provider.UserId
                      where user.PublicId == userId && user.StatusCode == "ACTIVE"
                      select new ValueTuple<long, long>(user.Id, provider.Id)).SingleOrDefaultAsync(token) is var value && value != default
            ? value
            : throw new ProviderConfigurationException("PROVIDER_NOT_FOUND", "공급자 정보를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    }

    private static ProviderHubWorkItem Item(string type, Guid id, string title, string description, string status,
        string priority, DateTime? scheduledAt, string badge, string route, string action, string domain) =>
        new(type, id, title, description, status, priority, scheduledAt, badge, SafeRoute(route), action, domain);
    private static string SafeRoute(string route) => route.StartsWith("/provider/", StringComparison.Ordinal) &&
        !route.Contains("//", StringComparison.Ordinal) && !route.Contains("\\", StringComparison.Ordinal) &&
        !route.Contains("..", StringComparison.Ordinal) ? route : "/provider";
    private static ProviderHubMetric M(string key, string label, int count, string route, string tone = "default") => new(key, label, count, SafeRoute(route), tone);
    private static ProviderHubMetricGroup Group(string key, string title, params ProviderHubMetric[] items) => new(key, title, items);
    private static int Priority(string value) => value switch { "URGENT" => 0, "TODAY" => 1, "ACTION_REQUIRED" => 2, "WAITING_CUSTOMER" => 3, "IN_PROGRESS" => 4, "ISSUE" => 5, "NEW" => 6, _ => 7 };
    private static string StatusLabel(string value) => value switch
    {
        "PROVIDER_COMPLETED" or "COMPLETION_SUBMITTED" => "완료보고 · 고객 확인 대기",
        "TERMINATION_REQUESTED" => "해지 처리 중",
        "POLICY_PENDING" => "정책 확정 전",
        "REVISION_REQUESTED" => "보완 요청",
        "IN_PROGRESS" => "진행 중",
        "SCHEDULED" => "일정 확정",
        "RESCHEDULED" => "변경 일정 확정",
        "RECEIVED" => "접수 확인 필요",
        "PENDING" => "확인 대기",
        "REJECTED" => "반려 · 보완 필요",
        "EXPIRED" => "만료 · 갱신 필요",
        "DEPARTED" => "출발",
        "EN_ROUTE" => "이동 중",
        "ARRIVED" => "현장 도착",
        _ => value.Replace('_', ' ')
    };
    private static string GeneralAction(string status) => status switch { "CREATED" => "일정을 제안하거나 확정하세요.", "IN_PROGRESS" => "진행 상태와 완료보고를 확인하세요.", "REVISION_REQUESTED" => "고객 보완 요청을 확인하세요.", "COMPLETION_SUBMITTED" => "고객 완료확인을 기다리고 있습니다.", _ => "거래 상세를 확인하세요." };
    private static string CareAction(string status) => status switch { "SCHEDULED" or "RESCHEDULED" => "방문 일정을 확인하세요.", "IN_PROGRESS" => "체크리스트와 완료 증빙을 등록하세요.", "PROVIDER_COMPLETED" => "고객 확인을 기다리고 있습니다.", _ => "회차 상태를 확인하세요." };
    private static string InteriorType(string action, IReadOnlyList<string> roles)
    {
        if (action.Contains("실측", StringComparison.Ordinal) || roles.Contains("SITE_SURVEY")) return "INTERIOR_SITE_SURVEY";
        if (action.Contains("설계", StringComparison.Ordinal) || roles.Contains("DESIGN")) return "INTERIOR_DESIGN";
        if (action.Contains("검사", StringComparison.Ordinal) || roles.Contains("INSPECTION")) return "INTERIOR_INSPECTION";
        if (action.Contains("완료", StringComparison.Ordinal)) return "INTERIOR_COMPLETION";
        return "INTERIOR_PROCESS";
    }
    private static string TodayAvailability(EmergencyAvailabilityResponse value)
    {
        var today = (byte)TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local).DayOfWeek;
        var slots = value.Services.Where(x => x.IsEnabled).SelectMany(x => x.Slots).Where(x => x.DayOfWeek == today).ToArray();
        if (slots.Any(x => x.Is24Hours)) return "24시간 가능";
        return slots.Length == 0 ? "오늘 가능시간 없음" : string.Join(", ", slots.Select(x => $"{x.StartTime:HH\\:mm}~{x.EndTime:HH\\:mm}"));
    }
}
