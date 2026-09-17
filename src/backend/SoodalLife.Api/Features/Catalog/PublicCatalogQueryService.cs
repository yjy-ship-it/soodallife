using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Catalog;

public sealed class PublicCatalogQueryService(SoodalLifeDbContext db)
{
    public Task<List<PublicCategoryResponse>> GetMajorsAsync(CancellationToken token) =>
        CategoryQuery("MAJOR", null).ToListAsync(token);

    public async Task<List<PublicCategoryResponse>?> GetChildrenAsync(Guid parentId, CancellationToken token)
    {
        var parent = await db.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PublicId == parentId && x.StatusCode == "ACTIVE", token);
        if (parent is null || parent.LevelCode is not ("MAJOR" or "MIDDLE")) return null;
        var childLevel = parent.LevelCode == "MAJOR" ? "MIDDLE" : "SERVICE";
        return await CategoryQuery(childLevel, parent.Id).ToListAsync(token);
    }

    public async Task<List<PublicServiceSummaryResponse>> SearchServicesAsync(
        string? query, Guid? majorId, Guid? middleId, int take, CancellationToken token)
    {
        take = Math.Clamp(take, 1, 100);
        var text = query?.Trim();
        if (text?.Length > 100) text = text[..100];

        var rows = from service in db.ServiceCategories.AsNoTracking()
                   join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                   join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                   where service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
                         middle.StatusCode == "ACTIVE" && major.StatusCode == "ACTIVE"
                   select new { Service = service, Middle = middle, Major = major };

        if (!string.IsNullOrWhiteSpace(text))
            rows = rows.Where(x => x.Service.Name.Contains(text) ||
                (x.Service.SearchKeywordsText != null && x.Service.SearchKeywordsText.Contains(text)) ||
                x.Middle.Name.Contains(text) || x.Major.Name.Contains(text));
        if (majorId.HasValue) rows = rows.Where(x => x.Major.PublicId == majorId.Value);
        if (middleId.HasValue) rows = rows.Where(x => x.Middle.PublicId == middleId.Value);

        var values = await rows.OrderBy(x => x.Major.SortOrder).ThenBy(x => x.Middle.SortOrder)
            .ThenBy(x => x.Service.SortOrder).ThenBy(x => x.Service.Name).Take(take).ToListAsync(token);
        var result = new List<PublicServiceSummaryResponse>(values.Count);
        foreach (var value in values)
        {
            result.Add(new PublicServiceSummaryResponse(
                value.Service.PublicId, value.Service.ExternalCode, value.Service.SearchSlug, value.Service.Name,
                value.Major.PublicId, value.Major.Name, value.Middle.PublicId, value.Middle.Name,
                await DescriptionAsync(value.Service.Id, token), await PriceAsync(value.Service.Id, token)));
        }
        return result;
    }

    public async Task<List<PublicServiceSummaryResponse>> GetRepresentativeServicesAsync(int take, int maxPerMiddle, CancellationToken token)
    {
        take = Math.Clamp(take, 1, 24);
        maxPerMiddle = Math.Clamp(maxPerMiddle, 1, 2);
        var rows = await (from service in db.ServiceCategories.AsNoTracking()
                          join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                          join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                          where service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
                                middle.StatusCode == "ACTIVE" && major.StatusCode == "ACTIVE"
                          orderby major.SortOrder, middle.SortOrder, service.SortOrder, service.Name
                          select new { Service = service, Middle = middle, Major = major }).ToListAsync(token);

        // Keep the same representative set for the whole Korean calendar day while
        // rotating the first service inside each category on the next day.
        var rotationDay = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9)).DayNumber;
        var selected = new List<dynamic>(take);
        var middleCounts = new Dictionary<long, int>();
        var majorGroups = rows.GroupBy(value => value.Major.Id).Select(group =>
        {
            var values = group.ToList();
            var offset = (int)(((long)rotationDay + group.Key * 31L) % values.Count);
            return values.Skip(offset).Concat(values.Take(offset)).ToList();
        }).ToList();

        foreach (var major in majorGroups)
        {
            var candidate = major.FirstOrDefault();
            if (candidate is null) continue;
            selected.Add(candidate);
            middleCounts[candidate.Middle.Id] = 1;
            if (selected.Count == take) break;
        }

        while (selected.Count < take)
        {
            var added = false;
            foreach (var major in majorGroups)
            {
                var candidate = major.FirstOrDefault(value =>
                    selected.All(current => current.Service.Id != value.Service.Id) &&
                    middleCounts.GetValueOrDefault(value.Middle.Id) == 0)
                    ?? major.FirstOrDefault(value => selected.All(current => current.Service.Id != value.Service.Id) &&
                        middleCounts.GetValueOrDefault(value.Middle.Id) < maxPerMiddle);
                if (candidate is null) continue;
                selected.Add(candidate);
                middleCounts[candidate.Middle.Id] = middleCounts.GetValueOrDefault(candidate.Middle.Id) + 1;
                added = true;
                if (selected.Count == take) break;
            }
            if (!added) break;
        }

        var result = new List<PublicServiceSummaryResponse>(selected.Count);
        foreach (var value in selected)
            result.Add(new PublicServiceSummaryResponse(
                value.Service.PublicId, value.Service.ExternalCode, value.Service.SearchSlug, value.Service.Name,
                value.Major.PublicId, value.Major.Name, value.Middle.PublicId, value.Middle.Name,
                await DescriptionAsync(value.Service.Id, token), await PriceAsync(value.Service.Id, token)));
        return result;
    }

    public Task<PublicServiceDetailResponse?> GetServiceAsync(Guid id, CancellationToken token) =>
        GetServiceAsync(id, null, token);

    public Task<PublicServiceDetailResponse?> GetServiceBySlugAsync(string slug, CancellationToken token) =>
        GetServiceAsync(null, slug.Trim().ToLowerInvariant(), token);

    private async Task<PublicServiceDetailResponse?> GetServiceAsync(Guid? id, string? slug, CancellationToken token)
    {
        var row = await (from service in db.ServiceCategories.AsNoTracking()
                         join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                         join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                         where (id.HasValue ? service.PublicId == id.Value : service.SearchSlug == slug) && service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
                               middle.StatusCode == "ACTIVE" && major.StatusCode == "ACTIVE"
                         select new { Service = service, Middle = middle, Major = major }).SingleOrDefaultAsync(token);
        if (row is null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var operation = await db.CategoryOperationPolicies.AsNoTracking()
            .Where(x => x.CategoryId == row.Service.Id && x.IsActive && x.EffectiveFrom <= today &&
                        (x.EffectiveTo == null || x.EffectiveTo > today))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).FirstOrDefaultAsync(token);
        var legacy = operation is null
            ? await db.CategoryPolicies.AsNoTracking().Where(x => x.CategoryId == row.Service.Id && x.EffectiveFrom <= today &&
                    (x.EffectiveTo == null || x.EffectiveTo > today))
                .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).FirstOrDefaultAsync(token)
            : null;
        var emergencyAllowed = await db.CategoryPolicies.AsNoTracking().AnyAsync(x =>
            x.CategoryId == row.Service.Id && x.IsEmergencyAllowed &&
            (x.TransactionTypeCode == "ONE_TIME" || x.TransactionTypeCode == "PROJECT") &&
            x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo > today), token);
        var coverageTypeCode = operation?.CoverageTypeCode ?? ProviderCoveragePolicy.LocalOnly;
        var fields = await PublicFieldsAsync(row.Service.Id, row.Middle.Id, coverageTypeCode, token);

        return new PublicServiceDetailResponse(
            row.Service.PublicId, row.Service.ExternalCode, row.Service.SearchSlug, row.Service.Name,
            row.Major.PublicId, row.Major.Name, row.Middle.PublicId, row.Middle.Name,
            await DescriptionAsync(row.Service.Id, token), await PriceAsync(row.Service.Id, token),
            operation?.OnsiteRequirementText ?? legacy?.OnsiteRequirementText ?? "안내 준비 중",
            coverageTypeCode,
            ProviderCoveragePolicy.RequiresServiceAddress(coverageTypeCode),
            emergencyAllowed,
            string.Equals(operation?.SubscriptionOptionText ?? legacy?.SubscriptionOptionText, "허용", StringComparison.Ordinal),
            operation?.DefaultWarrantyDays ?? legacy?.DefaultWarrantyDays ?? 0,
            operation?.RequestFieldSummaryText ?? legacy?.RequestFieldSummaryText ?? string.Join(", ", fields.Select(x => x.Label)),
            EmptyToNull(operation?.RequiredQualificationSummaryText ?? legacy?.RequiredQualificationSummaryText),
            EmptyToNull(row.Service.SeoTitle) ?? $"{row.Service.Name} 견적 비교 | 수달 라이프",
            EmptyToNull(row.Service.SeoDescription) ?? $"{row.Major.Name} {row.Middle.Name}의 {row.Service.Name} 서비스 범위와 가격을 확인하고 검증된 전문가의 견적을 비교해 보세요.",
            EmptyToNull(row.Service.SearchKeywordsText), fields);
    }

    private IQueryable<PublicCategoryResponse> CategoryQuery(string level, long? parentId) =>
        db.ServiceCategories.AsNoTracking()
            .Where(x => x.LevelCode == level && x.StatusCode == "ACTIVE" && x.ParentId == parentId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new PublicCategoryResponse(
                x.PublicId, x.ExternalCode, x.Name, x.LevelCode,
                x.ParentId == null ? null : db.ServiceCategories.Where(p => p.Id == x.ParentId).Select(p => (Guid?)p.PublicId).FirstOrDefault(),
                x.SortOrder, db.ServiceCategories.Count(child => child.ParentId == x.Id && child.StatusCode == "ACTIVE")));

    private async Task<PublicPriceSummaryResponse?> PriceAsync(long categoryId, CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var price = await db.CategoryPricePolicies.AsNoTracking()
            .Where(x => x.CategoryId == categoryId && x.IsActive && x.EffectiveFrom <= today &&
                        (x.EffectiveTo == null || x.EffectiveTo > today))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).FirstOrDefaultAsync(token);
        if (price is null) return null;

        var method = price.LegacyPriceMethodText;
        var label = method switch { "예약가" => "기본 작업 참고가", "견적형" => "견적 상담 참고가", _ => method };
        var vatPolicy = NormalizeVatPolicy(price.VatPolicyCode, price.LegacyVatDisplayRuleText);
        var referenceAmount = price.BaseAmount > 0 ? price.BaseAmount : (decimal?)null;
        var vatAmount = vatPolicy == "EXCLUDED" && referenceAmount.HasValue ? decimal.Round(referenceAmount.Value * 0.1m, 0) : (decimal?)null;
        var totalAmount = referenceAmount.HasValue ? referenceAmount + (vatAmount ?? 0) : null;
        var vatText = vatPolicy switch
        {
            "INCLUDED" => "VAT 포함 참고가",
            "EXCLUDED" => "VAT 별도 참고가",
            "EXEMPT" => "면세·비과세 참고가",
            _ => "VAT 여부는 전문가 견적에서 확인",
        };
        return new PublicPriceSummaryResponse(
            method, label, referenceAmount,
            price.RecommendedMinAmount, price.RecommendedMaxAmount, EmptyToNull(price.UnitText),
            price.CurrencyCode, vatPolicy, vatText, vatAmount, totalAmount,
            "기본 작업 기준의 참고 금액이며 확정 가격이 아닙니다. 자재·수량·현장 상태·출장 조건과 실제 작업 범위에 따라 달라질 수 있습니다.");
    }

    private static string NormalizeVatPolicy(string? code, string? legacy)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (value is "INCLUDED" or "VAT_INCLUDED" or "TAXABLE_VAT_INCLUDED") return "INCLUDED";
        if (value is "EXCLUDED" or "VAT_EXCLUDED") return "EXCLUDED";
        if (value is "EXEMPT" or "NON_TAXABLE") return "EXEMPT";
        var text = legacy ?? string.Empty;
        if (text.Contains("포함") && !text.Contains("별도")) return "INCLUDED";
        if (text.Contains("별도") && !text.Contains("포함")) return "EXCLUDED";
        if (text.Contains("면세") || text.Contains("비과세")) return "EXEMPT";
        return "UNDETERMINED";
    }

    private async Task<string?> DescriptionAsync(long categoryId, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        return await (from content in db.ManagedContents.AsNoTracking()
                      join link in db.ManagedContentCategories.AsNoTracking() on content.Id equals link.ContentId
                      join version in db.ManagedContentVersions.AsNoTracking()
                          on new { ContentId = content.Id, VersionNo = content.CurrentVersionNo }
                          equals new { version.ContentId, version.VersionNo }
                      where link.CategoryId == categoryId && content.ContentTypeCode == "CATEGORY_GUIDE" &&
                            content.StatusCode == "ACTIVE" && content.ReviewStatusCode == "APPROVED" &&
                            content.StartAt <= now && (content.EndAt == null || content.EndAt > now) &&
                            (content.AudienceTypeCode == "ALL" || content.AudienceTypeCode == "CUSTOMER")
                      orderby content.DisplayOrder, content.Id
                      select version.BodyText).FirstOrDefaultAsync(token);
    }

    private async Task<IReadOnlyList<PublicServiceRequestFieldResponse>> PublicFieldsAsync(long serviceId, long middleId, string coverageTypeCode, CancellationToken token)
    {
        var rows = await (from assignment in db.CategoryFieldAssignments.AsNoTracking()
                          join field in db.CategoryFieldDefinitions.AsNoTracking() on assignment.FieldDefinitionId equals field.Id
                          where assignment.IsActive && field.StatusCode == "ACTIVE" &&
                                (assignment.TargetCategoryId == serviceId ||
                                 (assignment.TargetCategoryId == middleId && !db.CategoryFieldAssignments.Any(overrideItem =>
                                     overrideItem.FieldDefinitionId == field.Id && overrideItem.TargetCategoryId == serviceId)))
                          orderby assignment.DisplayOrder, field.Id
                          select new { Field = field, Assignment = assignment }).ToListAsync(token);
        var firstDateTimeFieldId = rows.FirstOrDefault(row => row.Field.FieldTypeCode == "DATETIME")?.Field.Id;
        return rows
            .Where(row => !CustomerRequestFieldPolicy.IsRetiredStructuralDuplicate(row.Field) && !CustomerRequestFieldPolicy.IsIncompatibleWithCoverage(row.Field, coverageTypeCode))
            .Select(row => new PublicServiceRequestFieldResponse(
                row.Field.PublicId, row.Field.Label, row.Field.FieldTypeCode,
                row.Assignment.IsRequired && (row.Field.FieldTypeCode != "DATETIME" || row.Field.Id == firstDateTimeFieldId),
                row.Field.UnitText, row.Assignment.DisplayOrder)).ToArray();
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
