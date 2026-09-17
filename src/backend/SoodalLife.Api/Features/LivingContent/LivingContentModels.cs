namespace SoodalLife.Api.Features.LivingContent;

public sealed record LivingHomeResponse(
    IReadOnlyList<DailyLivingCheck> DailyChecks,
    IReadOnlyList<LocalServiceInsight> LocalInsights,
    IReadOnlyList<MaintenanceCalendarItem> MaintenanceCalendar,
    IReadOnlyList<PublicWorkStory> WorkStories,
    IReadOnlyList<ExpertQuickAnswer> ExpertAnswers,
    string RegionName,
    bool IsPersonalized,
    DateTime GeneratedAt,
    string PrivacyNotice);

public sealed record DailyLivingCheck(string Id,string SeasonLabel,string Title,string Body,string ActionLabel,string ServicePath);
public sealed record LocalServiceInsight(string ServiceName,string RegionName,int CompletedCount,decimal? TypicalMinAmount,decimal? TypicalMaxAmount,string CurrencyCode,int SamplePeriodDays,string ServicePath,string EvidenceLabel);
public sealed record MaintenanceCalendarItem(string Id,string Title,string Detail,DateOnly DueDate,string StatusCode,string SourceLabel,string ServicePath);
public sealed record PublicWorkStory(Guid Id,string ServiceName,string RegionName,string ProviderName,string ReviewText,decimal? Rating,DateTime CompletedAt,string ServicePath);
public sealed record ExpertQuickAnswer(Guid Id,string Question,string Answer,string ProviderName,string ServiceName,DateTime AnsweredAt,string ServicePath);
