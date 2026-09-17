using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public sealed class PublicProviderProfileService(SoodalLifeDbContext db)
{
    public async Task<PublicProviderProfileResponse> GetAsync(Guid providerId, Guid? campaignId, CancellationToken token)
    {
        var provider = await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == providerId &&
            x.ApprovalStatusCode == "APPROVED" && x.ActivityStatusCode == "ACTIVE", token)
            ?? throw new PublicProviderProfileException("PUBLIC_PROVIDER_NOT_FOUND", "현재 공개 중인 전문가 소개를 찾을 수 없습니다.", 404);

        var services = await (from link in db.ProviderServiceCategories.AsNoTracking()
                              join category in db.ServiceCategories.AsNoTracking() on link.CategoryId equals category.Id
                              where link.ProviderProfileId == provider.Id && link.StatusCode == "ACTIVE" && category.StatusCode == "ACTIVE"
                              orderby category.Name
                              select category.Name).Distinct().ToListAsync(token);
        var completed = await db.Transactions.AsNoTracking().CountAsync(x => x.ProviderProfileId == provider.Id && x.StatusCode == "COMPLETED", token);
        var performance = await (from transaction in db.Transactions.AsNoTracking()
                                 join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                                 where transaction.ProviderProfileId == provider.Id && transaction.StatusCode == "COMPLETED"
                                 group transaction by category.Name into rows
                                 orderby rows.Count() descending, rows.Key
                                 select new PublicProviderPerformanceResponse(rows.Key, rows.Count())).Take(12).ToListAsync(token);
        var reviewQuery = db.Reviews.AsNoTracking().Where(x => x.ProviderProfileId == provider.Id &&
            x.VisibilityStatusCode == "PUBLIC" && x.VerificationStatusCode == "VERIFIED_TRANSACTION");
        var publicReviewCount = await reviewQuery.CountAsync(token);
        var ratingItems = await (from rating in db.ReviewRatings.AsNoTracking()
                                 join review in reviewQuery on rating.ReviewId equals review.Id
                                 join item in db.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals item.Id
                                 group rating by new { item.Name, item.MinValue, item.MaxValue, item.DisplayOrder } into rows
                                 orderby rows.Key.DisplayOrder
                                 select new PublicProviderRatingResponse(rows.Key.Name, rows.Average(x => x.RatingValue), rows.Count(), rows.Key.MinValue, rows.Key.MaxValue)).ToListAsync(token);
        var recentRows = await reviewQuery.OrderByDescending(x => x.SubmittedAt).Take(10).ToListAsync(token);
        var recentReviews = new List<PublicProviderReviewResponse>(recentRows.Count);
        foreach (var review in recentRows)
        {
            var average = await db.ReviewRatings.AsNoTracking().Where(x => x.ReviewId == review.Id).Select(x => (decimal?)x.RatingValue).AverageAsync(token);
            var reply = await db.ReviewProviderReplies.AsNoTracking().Where(x => x.ReviewId == review.Id).Select(x => x.BodyText).SingleOrDefaultAsync(token);
            recentReviews.Add(new(review.PublicId, review.BodyText, review.SubmittedAt, average, reply));
        }
        var trust = await db.ProviderTrustScoreCurrent.AsNoTracking().SingleOrDefaultAsync(x => x.ProviderProfileId == provider.Id, token);
        var campaign = campaignId.HasValue ? await Campaign(provider.Id, campaignId.Value, token) : null;
        var trustScore = trust?.EvaluationStatusCode == "CALCULATED" ? trust.Score : null;
        return new(provider.PublicId, provider.BusinessName, provider.PublicIntroductionHtml, provider.PublicLogoUrl,
            Photos(provider.PublicPhotoUrlsJson), provider.PublicBlogUrl, provider.PublicWebsiteUrl, trustScore,
            trustScore.HasValue ? trust?.GradeCode : null, trustScore.HasValue ? $"{trustScore:0.#}점 · {trust?.GradeCode ?? "등급 산정 중"}" : "신뢰도 산정 중",
            completed, publicReviewCount, ratingItems, services, performance, recentReviews, campaign);
    }

    private async Task<PublicProviderCampaignResponse?> Campaign(long providerId, Guid campaignId, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        return await (from application in db.ProviderAdvertisingApplications.AsNoTracking()
                      join campaign in db.AdvertisingCampaigns.AsNoTracking() on application.CampaignId equals campaign.Id
                      join creative in db.AdvertisingCreatives.AsNoTracking() on campaign.Id equals creative.CampaignId
                      where application.ProviderProfileId == providerId && campaign.PublicId == campaignId &&
                            application.StatusCode == "PUBLISHED" && campaign.StatusCode == "ACTIVE" &&
                            creative.StatusCode == "ACTIVE" && campaign.StartAt <= now && (!campaign.EndAt.HasValue || campaign.EndAt > now)
                      orderby creative.DisplayOrder
                      select new PublicProviderCampaignResponse(campaign.PublicId, creative.Title, creative.Subtitle, creative.BodyText, campaign.StartAt, campaign.EndAt))
            .FirstOrDefaultAsync(token);
    }

    private static IReadOnlyList<string> Photos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
