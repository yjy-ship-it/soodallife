namespace SoodalLife.Api.Features.Admin;

public sealed record AdminAnalyticsQuery(
    string? Range,
    DateOnly? From,
    DateOnly? To,
    Guid? CategoryId,
    Guid? AreaId,
    Guid? ProviderId);

public sealed record AdminAnalyticsRangeResponse(
    string Range,
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    Guid? CategoryId,
    string? CategoryName,
    Guid? AreaId,
    string? AreaName,
    Guid? ProviderId);

public sealed record AdminAnalyticsMetricResponse(
    string Code,
    string Label,
    decimal? Value,
    string Unit,
    decimal? PreviousValue = null,
    decimal? ChangeRate = null,
    string? Note = null);

public sealed record AdminAnalyticsSectionResponse(
    string Code,
    string Title,
    IReadOnlyList<AdminAnalyticsMetricResponse> Metrics);

public sealed record AdminAnalyticsBreakdownResponse(string Code, string Label, decimal Value, string Unit = "건");

public sealed record AdminAnalyticsTrendPointResponse(
    DateOnly Date,
    long NewCustomers,
    long NewProviders,
    long Requests,
    long Transactions,
    decimal FeeChargedAmount);

public sealed record AdminAnalyticsAttentionResponse(
    string Code,
    string Label,
    long Count,
    string Severity,
    string Path,
    string Description);

public sealed record AdminAnalyticsFilterOptionResponse(Guid Id, string Label, string? ParentLabel = null);

public sealed record AdminAnalyticsDashboardResponse(
    AdminAnalyticsRangeResponse AppliedFilter,
    IReadOnlyList<AdminAnalyticsMetricResponse> Kpis,
    IReadOnlyList<AdminAnalyticsTrendPointResponse> Trend,
    IReadOnlyList<AdminAnalyticsBreakdownResponse> RequestsByCategory,
    IReadOnlyList<AdminAnalyticsBreakdownResponse> RequestsByRegion,
    IReadOnlyList<AdminAnalyticsSectionResponse> Sections,
    IReadOnlyList<AdminAnalyticsAttentionResponse> Attention,
    IReadOnlyList<AdminAnalyticsFilterOptionResponse> Categories,
    IReadOnlyList<AdminAnalyticsFilterOptionResponse> Regions,
    IReadOnlyList<string> UnavailableMetrics,
    DateTime GeneratedAt);
