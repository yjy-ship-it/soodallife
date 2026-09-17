namespace SoodalLife.Api.Features.Providers;

public sealed record PublicProviderProfileResponse(
    Guid Id,
    string BusinessName,
    string? IntroductionHtml,
    string? LogoUrl,
    IReadOnlyList<string> PhotoUrls,
    string? BlogUrl,
    string? WebsiteUrl,
    decimal? TrustScore,
    string? TrustGrade,
    string TrustDisplay,
    int CompletedServiceCount,
    int PublicReviewCount,
    IReadOnlyList<PublicProviderRatingResponse> RatingItems,
    IReadOnlyList<string> ActiveServices,
    IReadOnlyList<PublicProviderPerformanceResponse> Performance,
    IReadOnlyList<PublicProviderReviewResponse> RecentReviews,
    PublicProviderCampaignResponse? Campaign);

public sealed record PublicProviderRatingResponse(string Name, decimal Average, int Count, decimal MinValue, decimal MaxValue);
public sealed record PublicProviderPerformanceResponse(string ServiceName, int CompletedCount);
public sealed record PublicProviderReviewResponse(Guid Id, string BodyText, DateTime SubmittedAt, decimal? AverageRating, string? ProviderReply);
public sealed record PublicProviderCampaignResponse(Guid Id, string Title, string? Subtitle, string? BodyText, DateTime StartAt, DateTime? EndAt);

public sealed class PublicProviderProfileException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string BusinessCode { get; } = code;
    public int StatusCode { get; } = statusCode;
}
