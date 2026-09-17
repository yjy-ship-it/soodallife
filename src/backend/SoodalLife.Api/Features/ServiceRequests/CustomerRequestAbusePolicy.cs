using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.ServiceRequests;

public sealed class CustomerRequestAbusePolicy(SoodalLifeDbContext db)
{
    public const short CurrentVersion = 1;
    public const int SameServicePerDay = 2;
    public const int TotalPerDay = 5;
    public const int TotalPerWeek = 15;
    public const int ConcurrentOpen = 3;
    public const int ConcurrentUrgent = 1;

    public async Task<string> ValidateAsync(ServiceRequest request, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var published = db.ServiceRequests.AsNoTracking().Where(item =>
            item.CustomerProfileId == request.CustomerProfileId && item.Id != request.Id &&
            item.OpenedAt != null && !item.AbuseCountExcluded);

        await CheckProgressiveRestrictionsAsync(request.CustomerProfileId, now, token);

        if (await published.CountAsync(item => item.CategoryId == request.CategoryId && item.OpenedAt >= now.AddHours(-24), token) >= SameServicePerDay)
            throw Limit("REQUEST_SAME_SERVICE_DAILY_LIMIT", "동일 서비스는 24시간 동안 최대 2회까지 요청할 수 있습니다. 기존 요청에 내용을 추가하거나 도착한 견적을 확인해 주세요.");
        if (await published.CountAsync(item => item.OpenedAt >= now.AddHours(-24), token) >= TotalPerDay)
            throw Limit("REQUEST_DAILY_LIMIT", "견적요청은 24시간 동안 최대 5건까지 공개할 수 있습니다. 진행 중인 요청을 먼저 확인해 주세요.");
        if (await published.CountAsync(item => item.OpenedAt >= now.AddDays(-7), token) >= TotalPerWeek)
            throw Limit("REQUEST_WEEKLY_LIMIT", "최근 7일 동안 공개할 수 있는 견적요청 15건을 모두 사용했습니다. 기존 요청을 먼저 확인해 주세요.");
        if (await published.CountAsync(item => item.StatusCode == "OPEN" && item.ExpiresAt > now, token) >= ConcurrentOpen)
            throw Limit("REQUEST_OPEN_LIMIT", "동시에 견적을 모집할 수 있는 요청은 최대 3건입니다. 기존 요청을 완료하거나 모집 종료 후 다시 시도해 주세요.");
        if (request.IsUrgent && await published.AnyAsync(item => item.IsUrgent && item.StatusCode == "OPEN" && item.ExpiresAt > now, token))
            throw Limit("REQUEST_URGENT_OPEN_LIMIT", "긴급출동 요청은 동시에 1건만 진행할 수 있습니다. 기존 긴급 요청을 먼저 확인해 주세요.");

        var fingerprint = await FingerprintAsync(request, token);
        if (await published.AnyAsync(item => item.CategoryId == request.CategoryId &&
                item.AdministrativeAreaId == request.AdministrativeAreaId && item.OpenedAt >= now.AddHours(-24) &&
                item.AbuseFingerprint == fingerprint, token))
            throw Limit("REQUEST_SIMILAR_DUPLICATE", "24시간 안에 같은 서비스·지역·내용으로 공개한 요청이 있습니다. 기존 요청을 확인해 주세요.");
        return fingerprint;
    }

    private async Task CheckProgressiveRestrictionsAsync(long customerId, DateTime now, CancellationToken token)
    {
        var protectedRequests = db.ServiceRequests.AsNoTracking().Where(item =>
            item.CustomerProfileId == customerId && item.AbusePolicyVersion == CurrentVersion && !item.AbuseCountExcluded);
        var recentCancellations = await protectedRequests
            .Where(item => item.CancelledAt >= now.AddDays(-7)).OrderByDescending(item => item.CancelledAt)
            .Select(item => item.CancelledAt).ToListAsync(token);
        if (recentCancellations.Count >= 3 && recentCancellations[0] > now.AddHours(-24))
            throw Limit("REQUEST_REPEATED_CANCELLATION_RESTRICTED", "최근 요청 취소가 반복되어 마지막 취소 시점부터 24시간 동안 새 요청 공개가 제한됩니다.");

        var abandoned = protectedRequests.Where(item => item.ExpiresAt < now && item.ExpiresAt >= now.AddDays(-30) &&
            !db.Transactions.Any(transaction => transaction.ServiceRequestId == item.Id) &&
            db.Quotes.Any(quote => quote.ServiceRequestId == item.Id &&
                (quote.StatusCode == "SUBMITTED" || quote.StatusCode == "NOT_SELECTED" || quote.StatusCode == "EXPIRED")));
        var unviewed = abandoned.Where(item => item.CustomerQuotesViewedAt == null);
        var lastUnviewedAt = await unviewed.MaxAsync(item => (DateTime?)item.ExpiresAt, token);
        if (await unviewed.CountAsync(token) >= 5 && lastUnviewedAt > now.AddHours(-72))
            throw Limit("REQUEST_REPEATED_UNVIEWED_RESTRICTED", "도착한 견적을 확인하지 않은 요청이 반복되어 마지막 모집 종료 후 72시간 동안 새 요청 공개가 제한됩니다.");

        var lastAbandonedAt = await abandoned.MaxAsync(item => (DateTime?)item.ExpiresAt, token);
        if (await abandoned.CountAsync(token) >= 5 && lastAbandonedAt > now.AddHours(-24))
            throw Limit("REQUEST_REPEATED_ABANDONMENT_RESTRICTED", "견적이 도착한 요청을 반복해서 완료하지 않아 마지막 모집 종료 후 24시간 동안 새 요청 공개가 제한됩니다.");
    }

    private async Task<string> FingerprintAsync(ServiceRequest request, CancellationToken token)
    {
        var answers = await db.RequestAnswers.AsNoTracking().Where(item => item.ServiceRequestId == request.Id)
            .OrderBy(item => item.FieldDefinitionId)
            .Select(item => new { item.FieldDefinitionId, item.ValueText, item.ValueNumber, item.ValueBoolean, item.ValueDate, item.ValueDateTime, item.ValueJson, item.ValueCurrencyCode })
            .ToListAsync(token);
        var files = await (from link in db.ServiceRequestFiles.AsNoTracking()
                           join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                           where link.ServiceRequestId == request.Id
                           orderby file.Sha256Hex
                           select file.Sha256Hex).ToListAsync(token);
        var raw = string.Join('|', request.CategoryId, request.AdministrativeAreaId, request.IsUrgent,
            Normalize(request.Title), Normalize(request.Description),
            string.Join(';', answers.Select(item => string.Join(':', item.FieldDefinitionId, Normalize(item.ValueText), item.ValueNumber,
                item.ValueBoolean, item.ValueDate, item.ValueDateTime, Normalize(item.ValueJson), item.ValueCurrencyCode))),
            string.Join(';', files));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static RequestValidationException Limit(string code, string message) =>
        new(code, message, new Dictionary<string, string[]> { ["request"] = [message] }, StatusCodes.Status429TooManyRequests);
}
