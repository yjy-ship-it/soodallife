using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.PublicActivity;

public sealed class PublicActivityFeedService(SoodalLifeDbContext db,IMemoryCache cache,ILogger<PublicActivityFeedService> logger)
{
    // One cold rebuild at a time protects SQL Server when many anonymous users
    // arrive just after the shared feed cache expires.
    private static readonly SemaphoreSlim BuildGate=new(1,1);
    private static readonly string[] Steps =
        ["견적 요청", "전문가 매칭", "견적 도착", "전문가 선택", "일정·작업 진행", "작업 완료", "후기 등록"];
    private const string PrivacyNotice = "개인 식별정보·상세주소·전문가 정보는 제외하고 10분 지연된 실제 요청 정보와 견적금액만 표시합니다.";

    public async Task<PublicActivityFeedResponse> GetAsync(string? region, string? eventType, int take, CancellationToken token)
    {
        var stopwatch=Stopwatch.StartNew();
        var normalizedRegion=string.IsNullOrWhiteSpace(region)?null:region.Trim();if(normalizedRegion?.Length>50)normalizedRegion=normalizedRegion[..50];
        var normalizedType=Normalize(eventType);var normalizedTake=Math.Clamp(take,5,30);
        var key=$"public-activity:v305:{normalizedRegion?.ToUpperInvariant()??"ALL"}:{normalizedType??"ALL"}:{normalizedTake}";
        if(cache.TryGetValue<PublicActivityFeedResponse>(key,out var cached)&&cached is not null){stopwatch.Stop();logger.LogInformation("Public activity feed completed. CacheHit={CacheHit} ItemCount={ItemCount} DurationMs={DurationMs}",true,cached.Items.Count,stopwatch.Elapsed.TotalMilliseconds);return cached;}
        await BuildGate.WaitAsync(token);
        PublicActivityFeedResponse response;
        try
        {
            if(cache.TryGetValue<PublicActivityFeedResponse>(key,out cached)&&cached is not null)return cached;
            response=await BuildAsync(normalizedRegion,normalizedType,normalizedTake,token);
            cache.Set(key,response,TimeSpan.FromSeconds(90));
        }
        finally{BuildGate.Release();}
        stopwatch.Stop();
        logger.LogInformation("Public activity feed completed. CacheHit={CacheHit} ItemCount={ItemCount} DurationMs={DurationMs}",false,response.Items.Count,stopwatch.Elapsed.TotalMilliseconds);
        return response;
    }

    private async Task<PublicActivityFeedResponse> BuildAsync(string? region, string? eventType, int take, CancellationToken token)
    {
        if(!db.Database.IsRelational())return await BuildLegacyAsync(region,eventType,take,token);
        var now=DateTime.UtcNow;var visibleBefore=now.AddMinutes(-10);var cutoff=now.AddDays(-30);var requestedType=Normalize(eventType);
        var query=from activity in db.PublicActivityEvents.AsNoTracking()
                  join request in db.ServiceRequests.AsNoTracking() on activity.ServiceRequestId equals request.Id
                  join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
                  join area0 in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals (long?)area0.Id into areaRows
                  from area in areaRows.DefaultIfEmpty()
                  join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)parent0.Id into parentRows
                  from parent in parentRows.DefaultIfEmpty()
                  where activity.OccurredAt>=cutoff&&activity.OccurredAt<=visibleBefore&&request.StatusCode!="CANCELLED"&&category.StatusCode=="ACTIVE"&&
                        (requestedType==null||activity.EventTypeCode==requestedType)&&
                        !db.Transactions.Any(x=>x.ServiceRequestId==request.Id&&(x.StatusCode=="DISPUTED"||x.StatusCode=="CANCELLED"))
                  orderby activity.OccurredAt descending
                  select new{activity.ServiceRequestId,activity.EventTypeCode,activity.OccurredAt,request.Title,request.Description,request.OpenedAt,request.IsUrgent,category.PublicId,category.Name,category.ExternalCode,category.SearchSlug,IsInterior=db.InteriorProjects.Any(project=>project.ServiceRequestId==request.Id),IsCare=db.CareProducts.Any(product=>product.ServiceCategoryId==category.Id&&product.IsActive),RegionName=area==null?"전국":parent==null||parent.AreaName==area.AreaName?area.AreaName:parent.AreaName+" "+area.AreaName};
        var rows=await query.Take(300).ToListAsync(token);
        var requestIds=rows.Select(x=>x.ServiceRequestId).Distinct().ToArray();
        var quoteAmounts=await LoadQuoteAmountsAsync(requestIds,token);
        var requestDetails=await LoadRequestDetailsAsync(requestIds,token);
        var items=rows.GroupBy(x=>new{x.ServiceRequestId,x.EventTypeCode,x.Title,x.Description,x.OpenedAt,x.IsUrgent,x.IsInterior,x.IsCare,x.PublicId,x.Name,x.ExternalCode,x.SearchSlug,x.RegionName})
            .Select(group=>new{Candidate=new ActivityCandidate(group.Key.ServiceRequestId,group.Key.EventTypeCode,group.Max(x=>x.OccurredAt),group.Key.EventTypeCode=="QUOTE_RECEIVED"?group.Count():null),Metadata=new RequestMetadata(group.Key.ServiceRequestId,group.Key.PublicId,group.Key.Name,group.Key.ExternalCode,group.Key.SearchSlug,group.Key.RegionName,group.Key.Title,group.Key.Description,group.Key.OpenedAt,group.Key.IsUrgent,group.Key.IsInterior,group.Key.IsCare)})
            .Select(x=>Map(x.Candidate,x.Metadata,requestDetails.GetValueOrDefault(x.Metadata.RequestId,[]),quoteAmounts.GetValueOrDefault(x.Metadata.RequestId,[]))).Where(x=>string.IsNullOrWhiteSpace(region)||x.RegionName.Contains(region.Trim(),StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x=>x.OccurredAt).ThenBy(x=>x.Id).Take(Math.Clamp(take,5,30)).ToArray();
        return new(items,now,60,PrivacyNotice);
    }

    private async Task<PublicActivityFeedResponse> BuildLegacyAsync(string? region, string? eventType, int take, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var visibleBefore = now.AddMinutes(-10);
        var cutoff = now.AddDays(-30);
        var events = new List<ActivityCandidate>();

        var requestRows = await db.ServiceRequests.AsNoTracking()
            .Where(x => x.OpenedAt >= cutoff && x.OpenedAt <= visibleBefore)
            .OrderByDescending(x => x.OpenedAt).Select(x => new { x.Id, x.OpenedAt })
            .Take(100).ToListAsync(token);
        events.AddRange(requestRows.Select(x => new ActivityCandidate(x.Id, "REQUEST_OPENED", x.OpenedAt!.Value, null)));

        // Aggregate after materialization. Keeping GroupBy out of SQL avoids the
        // production SQL Server translation failure that caused the V133 HTTP 500.
        var quoteRows = await db.Quotes.AsNoTracking()
            .Where(x => x.SubmittedAt >= cutoff && x.SubmittedAt <= visibleBefore &&
                        (x.StatusCode == "SUBMITTED" || x.StatusCode == "ACCEPTED"))
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new { x.ServiceRequestId, x.SubmittedAt })
            .Take(500).ToListAsync(token);
        events.AddRange(quoteRows.GroupBy(x => x.ServiceRequestId)
            .Select(x => new ActivityCandidate(x.Key, "QUOTE_RECEIVED", x.Max(q => q.SubmittedAt)!.Value, x.Count()))
            .OrderByDescending(x => x.OccurredAt).Take(100));

        var selectedRows = await db.ServiceRequests.AsNoTracking()
            .Where(x => x.AcceptedAt >= cutoff && x.AcceptedAt <= visibleBefore)
            .OrderByDescending(x => x.AcceptedAt).Select(x => new { x.Id, x.AcceptedAt })
            .Take(100).ToListAsync(token);
        events.AddRange(selectedRows.Select(x => new ActivityCandidate(x.Id, "PROVIDER_SELECTED", x.AcceptedAt!.Value, null)));

        var startedRows = await db.Transactions.AsNoTracking()
            .Where(x => x.StartedAt >= cutoff && x.StartedAt <= visibleBefore)
            .OrderByDescending(x => x.StartedAt).Select(x => new { x.ServiceRequestId, x.StartedAt })
            .Take(100).ToListAsync(token);
        events.AddRange(startedRows.Select(x => new ActivityCandidate(x.ServiceRequestId, "WORK_STARTED", x.StartedAt!.Value, null)));

        var completedRows = await db.Transactions.AsNoTracking()
            .Where(x => x.StatusCode == "COMPLETED" && x.CompletedAt >= cutoff && x.CompletedAt <= visibleBefore)
            .OrderByDescending(x => x.CompletedAt).Select(x => new { x.ServiceRequestId, x.CompletedAt })
            .Take(100).ToListAsync(token);
        events.AddRange(completedRows.Select(x => new ActivityCandidate(x.ServiceRequestId, "WORK_COMPLETED", x.CompletedAt!.Value, null)));

        var reviewRows = await db.Reviews.AsNoTracking()
            .Where(x => x.TransactionId != null && x.VisibilityStatusCode == "PUBLIC" &&
                        x.VerificationStatusCode == "VERIFIED_TRANSACTION" &&
                        x.PublishedAt >= cutoff && x.PublishedAt <= visibleBefore)
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new { TransactionId = x.TransactionId!.Value, x.PublishedAt })
            .Take(100).ToListAsync(token);
        var reviewTransactionIds = reviewRows.Select(x => x.TransactionId).Distinct().ToArray();
        var reviewTransactions = reviewTransactionIds.Length == 0
            ? new Dictionary<long, long>()
            : await db.Transactions.AsNoTracking().Where(x => reviewTransactionIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.ServiceRequestId, token);
        events.AddRange(reviewRows.Where(x => reviewTransactions.ContainsKey(x.TransactionId))
            .Select(x => new ActivityCandidate(reviewTransactions[x.TransactionId], "REVIEW_PUBLISHED", x.PublishedAt!.Value, null))
            .OrderByDescending(x => x.OccurredAt).Take(100));

        var requestedType = Normalize(eventType);
        if (requestedType is not null) events = events.Where(x => x.EventTypeCode == requestedType).ToList();
        var requestIds = events.Select(x => x.ServiceRequestId).Distinct().ToArray();
        if (requestIds.Length == 0) return Empty(now);

        var requests = await db.ServiceRequests.AsNoTracking()
            .Where(x => requestIds.Contains(x.Id) && x.StatusCode != "CANCELLED")
            .Select(x => new { x.Id, x.CategoryId, x.AdministrativeAreaId, x.Title, x.Description, x.OpenedAt, x.IsUrgent }).ToListAsync(token);
        var categoryIds = requests.Select(x => x.CategoryId).Distinct().ToArray();
        var categories = await db.ServiceCategories.AsNoTracking()
            .Where(x => categoryIds.Contains(x.Id) && x.StatusCode == "ACTIVE")
            .Select(x => new { x.Id, x.PublicId, x.Name, x.ExternalCode, x.SearchSlug }).ToDictionaryAsync(x => x.Id, token);
        var excluded = (await db.Transactions.AsNoTracking()
            .Where(x => requestIds.Contains(x.ServiceRequestId) && (x.StatusCode == "DISPUTED" || x.StatusCode == "CANCELLED"))
            .Select(x => x.ServiceRequestId).Distinct().ToListAsync(token)).ToHashSet();
        var interiorRequestIds=(await db.InteriorProjects.AsNoTracking().Where(x=>requestIds.Contains(x.ServiceRequestId)).Select(x=>x.ServiceRequestId).Distinct().ToListAsync(token)).ToHashSet();
        var careCategoryIds=(await db.CareProducts.AsNoTracking().Where(x=>categoryIds.Contains(x.ServiceCategoryId)&&x.IsActive).Select(x=>x.ServiceCategoryId).Distinct().ToListAsync(token)).ToHashSet();

        var areaIds = requests.Where(x => x.AdministrativeAreaId.HasValue).Select(x => x.AdministrativeAreaId!.Value).Distinct().ToArray();
        var areas = (await db.AdministrativeAreas.AsNoTracking().Where(x => areaIds.Contains(x.Id))
            .Select(x => new { x.Id, x.AreaName, x.ParentAreaId }).ToListAsync(token))
            .ToDictionary(x => x.Id, x => new AreaMetadata(x.Id, x.AreaName, x.ParentAreaId));
        var parentIds = areas.Values.Where(x => x.ParentAreaId.HasValue).Select(x => x.ParentAreaId!.Value).Distinct().ToArray();
        var parentNames = parentIds.Length == 0
            ? new Dictionary<long, string>()
            : await db.AdministrativeAreas.AsNoTracking().Where(x => parentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.AreaName, token);

        var metadata = new Dictionary<long, RequestMetadata>();
        foreach (var request in requests)
        {
            if (excluded.Contains(request.Id) || !categories.TryGetValue(request.CategoryId, out var category)) continue;
            var regionName = "전국";
            if (request.AdministrativeAreaId.HasValue && areas.TryGetValue(request.AdministrativeAreaId.Value, out var area))
            {
                regionName = area.AreaName;
                if (area.ParentAreaId.HasValue && parentNames.TryGetValue(area.ParentAreaId.Value, out var parentName) && parentName != area.AreaName)
                    regionName = $"{parentName} {area.AreaName}";
            }
            metadata[request.Id] = new RequestMetadata(request.Id, category.PublicId, category.Name, category.ExternalCode, category.SearchSlug, regionName, request.Title, request.Description, request.OpenedAt, request.IsUrgent, interiorRequestIds.Contains(request.Id), careCategoryIds.Contains(request.CategoryId));
        }

        var regionValue = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        var quoteAmounts = await LoadQuoteAmountsAsync(requestIds, token);
        var requestDetails = await LoadRequestDetailsAsync(requestIds, token);
        var items = events.Where(x => metadata.ContainsKey(x.ServiceRequestId))
            .Select(x => Map(x, metadata[x.ServiceRequestId], requestDetails.GetValueOrDefault(x.ServiceRequestId, []), quoteAmounts.GetValueOrDefault(x.ServiceRequestId, [])))
            .Where(x => regionValue is null || x.RegionName.Contains(regionValue, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
            .Take(Math.Clamp(take, 5, 30)).ToArray();
        return new PublicActivityFeedResponse(items, now, 60, PrivacyNotice);
    }

    private static PublicActivityFeedResponse Empty(DateTime now) => new([], now, 60, PrivacyNotice);

    private async Task<Dictionary<long, IReadOnlyList<PublicRequestDetailResponse>>> LoadRequestDetailsAsync(long[] requestIds, CancellationToken token)
    {
        if (requestIds.Length == 0) return [];
        var rows = await (from answer in db.RequestAnswers.AsNoTracking()
                          join field in db.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                          where requestIds.Contains(answer.ServiceRequestId)
                          select new RequestAnswerCandidate(answer.ServiceRequestId, field.FieldKey, field.Label, field.FieldTypeCode,
                              field.UnitText, field.DisplayOrder, answer.ValueText, answer.ValueNumber, answer.ValueBoolean,
                              answer.ValueDate, answer.ValueDateTime, answer.ValueJson, answer.ValueCurrencyCode)).ToListAsync(token);
        return rows.Select(row => new { row.ServiceRequestId, Label = PublicDetailLabel(row.FieldKey, row.Label), Value = PublicAnswer(row), row.DisplayOrder })
            .Where(row => row.Label is not null && !string.IsNullOrWhiteSpace(row.Value))
            .GroupBy(row => row.ServiceRequestId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PublicRequestDetailResponse>)group
                .GroupBy(row => row.Label!)
                .Select(values => values.OrderBy(value => value.DisplayOrder).First())
                .OrderBy(value => PublicDetailOrder(value.Label!))
                .Select(value => new PublicRequestDetailResponse(value.Label!, value.Value!)).ToArray());
    }

    private static string? PublicDetailLabel(string fieldKey, string label)
    {
        var value = $"{fieldKey} {label}".Replace(" ", string.Empty).ToLowerInvariant();
        if (value.Contains("평수") || value.Contains("면적") || value.Contains("area_size")) return "평수";
        if (value.Contains("환경") || value.Contains("environment")) return "환경";
        if (value.Contains("희망금액") || value.Contains("희망비용") || value.Contains("예산") || value.Contains("budget") || value.Contains("desired_amount")) return "희망금액";
        if (value.Contains("현장조건") || value.Contains("현장상태") || value.Contains("site_condition") || value.Contains("onsite_condition")) return "현장조건";
        if (value.Contains("수량") || value.Contains("건수") || value.Contains("개수") || value.Contains("quantity") || value.Contains("count")) return "수량";
        return null;
    }

    private static int PublicDetailOrder(string label) => label switch
    {
        "평수" => 0, "환경" => 1, "희망금액" => 2, "현장조건" => 3, "수량" => 4, _ => 9
    };

    private static string? PublicAnswer(RequestAnswerCandidate row)
    {
        if (PublicDetailLabel(row.FieldKey, row.Label) == "희망금액" && IsZeroBudgetAnswer(row)) return "견적상담 후 결정";
        if (!string.IsNullOrWhiteSpace(row.ValueText)) return PublicText(row.ValueText, 100);
        if (row.ValueNumber.HasValue)
        {
            if (row.FieldTypeCode == "MONEY" || !string.IsNullOrWhiteSpace(row.ValueCurrencyCode))
                return $"{row.ValueNumber.Value.ToString("N0", CultureInfo.GetCultureInfo("ko-KR"))}원";
            var number = row.ValueNumber.Value.ToString("0.####", CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(row.UnitText) ? number : $"{number}{row.UnitText}";
        }
        if (row.ValueBoolean.HasValue) return row.ValueBoolean.Value ? "예" : "아니요";
        if (row.ValueDate.HasValue) return row.ValueDate.Value.ToString("yyyy. M. d.", CultureInfo.GetCultureInfo("ko-KR"));
        if (row.ValueDateTime.HasValue) return row.ValueDateTime.Value.ToString("yyyy. M. d. tt h:mm", CultureInfo.GetCultureInfo("ko-KR"));
        if (string.IsNullOrWhiteSpace(row.ValueJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(row.ValueJson);
            return PublicText(document.RootElement.ValueKind == JsonValueKind.String ? document.RootElement.GetString() : document.RootElement.ToString(), 100);
        }
        catch (JsonException) { return null; }
    }

    private static bool IsZeroBudgetAnswer(RequestAnswerCandidate row)
    {
        if (row.ValueNumber == 0) return true;
        if (string.IsNullOrWhiteSpace(row.ValueText)) return false;
        var digits = Regex.Replace(row.ValueText, @"[^0-9.-]", string.Empty);
        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value == 0;
    }

    private async Task<Dictionary<long, IReadOnlyList<PublicQuoteAmountResponse>>> LoadQuoteAmountsAsync(long[] requestIds, CancellationToken token)
    {
        if (requestIds.Length == 0) return [];
        var quotes = await db.Quotes.AsNoTracking()
            .Where(x => requestIds.Contains(x.ServiceRequestId) && x.SubmittedAt.HasValue &&
                        (x.StatusCode == "SUBMITTED" || x.StatusCode == "ACCEPTED" || x.StatusCode == "NOT_SELECTED"))
            .Select(x => new { x.Id, x.ServiceRequestId, x.SubmittedAt }).ToListAsync(token);
        var quoteIds = quotes.Select(x => x.Id).ToArray();
        if (quoteIds.Length == 0) return [];
        var revisions = await db.QuoteRevisions.AsNoTracking().Where(x => quoteIds.Contains(x.QuoteId))
            .Select(x => new { x.QuoteId, x.RevisionNo, x.TotalAmount, x.CurrencyCode, x.SubmittedAt }).ToListAsync(token);
        var latest = revisions.GroupBy(x => x.QuoteId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.RevisionNo).First());
        return quotes.Where(x => latest.ContainsKey(x.Id)).GroupBy(x => x.ServiceRequestId).ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<PublicQuoteAmountResponse>)x.Select(q => latest[q.Id])
                .Select(r => new PublicQuoteAmountResponse(r.TotalAmount, r.CurrencyCode, r.SubmittedAt))
                .OrderByDescending(r => r.SubmittedAt).ToArray());
    }

    private static PublicActivityItemResponse Map(ActivityCandidate value, RequestMetadata metadata, IReadOnlyList<PublicRequestDetailResponse> requestDetails, IReadOnlyList<PublicQuoteAmountResponse> quoteAmounts)
    {
        var quoteCount = quoteAmounts.Count > 0 ? quoteAmounts.Count : value.QuoteCount ?? 0;
        var (label, activeStep) = value.EventTypeCode switch
        {
            "REQUEST_OPENED" => ("견적 요청 접수", 0),
            "QUOTE_RECEIVED" => (quoteCount > 1 ? $"견적 {quoteCount}건 도착" : "견적 도착", 2),
            "PROVIDER_SELECTED" => ("전문가 선택 완료", 3),
            "WORK_STARTED" => ("서비스 진행 중", 4),
            "WORK_COMPLETED" => ("서비스 완료", 5),
            "REVIEW_PUBLISHED" => ("검증된 후기 등록", 6),
            _ => ("거래 진행", 0)
        };
        var requestTitle = PublicText(metadata.Title, 80);
        var requestSummary = PublicText(metadata.Description, 220);
        var (domainCode,domainLabel)=metadata.IsUrgent?("EMERGENCY","긴급출동"):metadata.IsInterior?("INTERIOR","수달 인테리어"):metadata.IsCare?("CARE","수달 케어"):("GENERAL","일반 서비스");
        return new PublicActivityItemResponse(Id(value), value.EventTypeCode, label, domainCode, domainLabel, metadata.ServiceId, metadata.ServiceName, metadata.ServiceCode,
            metadata.RegionName, value.OccurredAt, metadata.OpenedAt,
            string.IsNullOrWhiteSpace(requestTitle) ? metadata.ServiceName : requestTitle,
            string.IsNullOrWhiteSpace(requestSummary) ? null : requestSummary, requestDetails,
            quoteCount, quoteAmounts,
            string.IsNullOrWhiteSpace(metadata.SearchSlug) ? null : $"/services/{metadata.SearchSlug}", Steps, activeStep);
    }

    private static string PublicText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = Regex.Replace(value, @"https?://\S+|[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}|(?:01[016789]|0\d{1,2})[-.\s]?\d{3,4}[-.\s]?\d{4}", "[개인정보 제외]", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
    }

    private static string Id(ActivityCandidate value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{value.EventTypeCode}:{value.ServiceRequestId}:{value.OccurredAt:O}")))[..20].ToLowerInvariant();

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "REQUEST_OPENED" or "QUOTE_RECEIVED" or "PROVIDER_SELECTED" or
            "WORK_STARTED" or "WORK_COMPLETED" or "REVIEW_PUBLISHED" ? normalized : null;
    }

    private sealed record ActivityCandidate(long ServiceRequestId, string EventTypeCode, DateTime OccurredAt, int? QuoteCount);
    private sealed record RequestMetadata(long RequestId, Guid ServiceId, string ServiceName, string? ServiceCode, string? SearchSlug, string RegionName, string Title, string? Description, DateTime? OpenedAt, bool IsUrgent, bool IsInterior, bool IsCare);
    private sealed record AreaMetadata(long Id, string AreaName, long? ParentAreaId);
    private sealed record RequestAnswerCandidate(long ServiceRequestId, string FieldKey, string Label, string FieldTypeCode,
        string? UnitText, int DisplayOrder, string? ValueText, decimal? ValueNumber, bool? ValueBoolean,
        DateOnly? ValueDate, DateTime? ValueDateTime, string? ValueJson, string? ValueCurrencyCode);
}
