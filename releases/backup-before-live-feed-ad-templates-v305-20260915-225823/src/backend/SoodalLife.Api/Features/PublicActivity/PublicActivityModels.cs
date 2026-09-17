namespace SoodalLife.Api.Features.PublicActivity;

public sealed record PublicActivityFeedResponse(
    IReadOnlyList<PublicActivityItemResponse> Items,
    DateTime ServerTime,
    int NextRefreshSeconds,
    string PrivacyNotice);

public sealed record PublicActivityItemResponse(
    string Id,
    string EventTypeCode,
    string StatusLabel,
    Guid ServiceId,
    string ServiceName,
    string? ServiceCode,
    string RegionName,
    DateTime OccurredAt,
    DateTime? RequestedAt,
    string RequestTitle,
    string? RequestSummary,
    IReadOnlyList<PublicRequestDetailResponse> RequestDetails,
    int? QuoteCount,
    IReadOnlyList<PublicQuoteAmountResponse> QuoteAmounts,
    string? ServicePath,
    IReadOnlyList<string> ProgressSteps,
    int ActiveStep);

public sealed record PublicRequestDetailResponse(
    string Label,
    string Value);

public sealed record PublicQuoteAmountResponse(
    decimal Amount,
    string CurrencyCode,
    DateTime SubmittedAt);
