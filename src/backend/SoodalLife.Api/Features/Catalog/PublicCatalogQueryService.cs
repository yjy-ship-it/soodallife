using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
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
            rows = rows.Where(x => x.Service.Name.Contains(text) || x.Middle.Name.Contains(text) || x.Major.Name.Contains(text));
        if (majorId.HasValue) rows = rows.Where(x => x.Major.PublicId == majorId.Value);
        if (middleId.HasValue) rows = rows.Where(x => x.Middle.PublicId == middleId.Value);

        var values = await rows.OrderBy(x => x.Major.SortOrder).ThenBy(x => x.Middle.SortOrder)
            .ThenBy(x => x.Service.SortOrder).ThenBy(x => x.Service.Name).Take(take).ToListAsync(token);
        var result = new List<PublicServiceSummaryResponse>(values.Count);
        foreach (var value in values)
        {
            result.Add(new PublicServiceSummaryResponse(
                value.Service.PublicId, value.Service.ExternalCode, value.Service.Name,
                value.Major.PublicId, value.Major.Name, value.Middle.PublicId, value.Middle.Name,
                await DescriptionAsync(value.Service.Id, token), await PriceAsync(value.Service.Id, token)));
        }
        return result;
    }

    public async Task<PublicServiceDetailResponse?> GetServiceAsync(Guid id, CancellationToken token)
    {
        var row = await (from service in db.ServiceCategories.AsNoTracking()
                         join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                         join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                         where service.PublicId == id && service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
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
        var fields = await PublicFieldsAsync(row.Service.Id, row.Middle.Id, token);

        return new PublicServiceDetailResponse(
            row.Service.PublicId, row.Service.ExternalCode, row.Service.Name,
            row.Major.PublicId, row.Major.Name, row.Middle.PublicId, row.Middle.Name,
            await DescriptionAsync(row.Service.Id, token), await PriceAsync(row.Service.Id, token),
            operation?.OnsiteRequirementText ?? legacy?.OnsiteRequirementText ?? "안내 준비 중",
            operation?.IsEmergencyAllowed ?? legacy?.IsEmergencyAllowed ?? false,
            string.Equals(operation?.SubscriptionOptionText ?? legacy?.SubscriptionOptionText, "허용", StringComparison.Ordinal),
            operation?.DefaultWarrantyDays ?? legacy?.DefaultWarrantyDays ?? 0,
            operation?.RequestFieldSummaryText ?? legacy?.RequestFieldSummaryText ?? string.Join(", ", fields.Select(x => x.Label)),
            EmptyToNull(operation?.RequiredQualificationSummaryText ?? legacy?.RequiredQualificationSummaryText), fields);
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
        var label = method switch { "예약가" => "예약가", "견적형" => "견적 상담", _ => method };
        return new PublicPriceSummaryResponse(
            method, label, price.BaseAmount > 0 ? price.BaseAmount : null,
            price.RecommendedMinAmount, price.RecommendedMaxAmount, EmptyToNull(price.UnitText),
            price.CurrencyCode, price.LegacyVatDisplayRuleText,
            "표시 금액은 예상·참고 금액이며 현장 조건과 실제 견적에 따라 달라질 수 있습니다.");
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

    private async Task<IReadOnlyList<PublicServiceRequestFieldResponse>> PublicFieldsAsync(long serviceId, long middleId, CancellationToken token)
    {
        var rows = await (from assignment in db.CategoryFieldAssignments.AsNoTracking()
                          join field in db.CategoryFieldDefinitions.AsNoTracking() on assignment.FieldDefinitionId equals field.Id
                          where assignment.IsActive && field.StatusCode == "ACTIVE" &&
                                (assignment.TargetCategoryId == serviceId ||
                                 (assignment.TargetCategoryId == middleId && !db.CategoryFieldAssignments.Any(overrideItem =>
                                     overrideItem.FieldDefinitionId == field.Id && overrideItem.TargetCategoryId == serviceId)))
                          orderby assignment.DisplayOrder, field.Id
                          select new { Field = field, Assignment = assignment }).ToListAsync(token);
        return rows
            .Where(row => !CustomerRequestFieldPolicy.IsRetiredStructuralDuplicate(row.Field))
            .Select(row => new PublicServiceRequestFieldResponse(
                row.Field.PublicId, row.Field.Label, row.Field.FieldTypeCode, row.Assignment.IsRequired,
                row.Field.UnitText, row.Assignment.DisplayOrder)).ToArray();
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
