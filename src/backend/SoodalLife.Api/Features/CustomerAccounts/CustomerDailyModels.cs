namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed record MySoodalAddressSummary(Guid Id, string AddressName, string RoadAddress, string DetailAddress);
public sealed record MySoodalHistorySummary(Guid Id, string Title, string? CategoryName, DateTime? CompletedAt);
public sealed record MySoodalSummaryResponse(
    string Name,
    string LoginId,
    MySoodalAddressSummary? DefaultAddress,
    int OngoingRequestCount,
    int OngoingTransactionCount,
    int UnreadNotificationCount,
    IReadOnlyList<MySoodalHistorySummary> RecentServiceHistory,
    int ReviewCount,
    int ActiveAfterServiceCount,
    int ActiveDisputeCount,
    int ActiveReportCount);

public sealed record ConsentHistoryResponse(
    Guid LegalDocumentVersionId,
    string Code,
    string RequirementCode,
    string Title,
    int VersionNo,
    string ConsentStatus,
    DateTime? ConsentedAt,
    DateTime? WithdrawnAt,
    string SourceCode,
    bool IsPlaceholder);

public sealed record WithdrawalBlockerResponse(string Code, string Label, int Count);
public sealed record WithdrawalReadinessResponse(
    bool RequiresReview,
    bool HasMultipleActiveRoles,
    IReadOnlyList<WithdrawalBlockerResponse> Blockers,
    string Guidance);
