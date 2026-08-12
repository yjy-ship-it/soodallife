namespace SoodalLife.Api.Features.Providers;

public sealed record ProviderHubMetric(string Key, string Label, int Count, string Route, string Tone);
public sealed record ProviderHubMetricGroup(string Key, string Title, IReadOnlyList<ProviderHubMetric> Items);

public sealed record ProviderHubWorkItem(
    string Type,
    Guid PublicId,
    string Title,
    string Description,
    string Status,
    string PriorityGroup,
    DateTime? ScheduledAt,
    string Badge,
    string Route,
    string NextAction,
    string Domain);

public sealed record ProviderHubInboxPage(
    IReadOnlyList<ProviderHubWorkItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProviderHubScheduleItem(
    string Type,
    Guid PublicId,
    string Title,
    string Status,
    DateTime ScheduledAt,
    DateTime? ScheduledEndAt,
    string Route,
    string Domain);

public sealed record ProviderHubChatItem(
    Guid RoomId,
    string CounterpartyDisplayName,
    string ServiceName,
    DateTime? LastMessageAt,
    int UnreadCount,
    string Route);

public sealed record ProviderHubEmergencySummary(
    bool IsEnabled,
    bool IsCurrentlyAvailable,
    string AvailabilityReason,
    int NewRequestCount,
    int WaitingResponseCount,
    int ActiveAssignmentCount,
    string TodayAvailability,
    string Route);

public sealed record ProviderHubWalletSummary(
    decimal AvailableBalance,
    decimal ReservedBalance,
    string CurrencyCode,
    string Status,
    string? LatestEntryType,
    decimal? LatestEntryAmount,
    DateTime? LatestEntryAt,
    bool RequiresAttention,
    string Route);

public sealed record ProviderHubApprovalSummary(
    string ApprovalStatus,
    string ActivityStatus,
    int PendingServiceCount,
    int RejectedServiceCount,
    int MissingEvidenceCount,
    int RejectedEvidenceCount,
    int ExpiredEvidenceCount,
    IReadOnlyList<string> NextActions,
    string Route);

public sealed record ProviderOperationsHubResponse(
    DateTime GeneratedAt,
    IReadOnlyList<ProviderHubMetricGroup> Summary,
    ProviderHubInboxPage Inbox,
    IReadOnlyList<ProviderHubScheduleItem> Schedule,
    IReadOnlyList<ProviderHubChatItem> RecentChats,
    ProviderHubEmergencySummary Emergency,
    ProviderHubWalletSummary Wallet,
    ProviderHubApprovalSummary Approval);
