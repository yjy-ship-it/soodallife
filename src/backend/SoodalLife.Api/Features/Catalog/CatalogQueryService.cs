using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Catalog;

public sealed class CatalogQueryService(SoodalLifeDbContext dbContext)
{
    public async Task<IReadOnlyList<CategoryResponse>> GetMajorCategoriesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await dbContext.ServiceCategories.AsNoTracking()
            .Where(major => major.LevelCode == "MAJOR" && major.StatusCode == "ACTIVE")
            .Where(major => dbContext.ServiceCategories.Any(middle =>
                middle.ParentId == major.Id && middle.StatusCode == "ACTIVE" &&
                dbContext.ServiceCategories.Any(service =>
                    service.ParentId == middle.Id && service.StatusCode == "ACTIVE" &&
                    dbContext.CategoryPolicies.Any(policy =>
                        policy.CategoryId == service.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                        policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today)))))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(category.PublicId, category.Name, category.LevelCode, category.ExternalCode, category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryResponse>?> GetMiddleCategoriesAsync(Guid majorPublicId, CancellationToken cancellationToken)
    {
        var major = await dbContext.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.PublicId == majorPublicId && category.LevelCode == "MAJOR" && category.StatusCode == "ACTIVE", cancellationToken);
        if (major is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await dbContext.ServiceCategories.AsNoTracking()
            .Where(middle => middle.ParentId == major.Id && middle.LevelCode == "MIDDLE" && middle.StatusCode == "ACTIVE")
            .Where(middle => dbContext.ServiceCategories.Any(service =>
                service.ParentId == middle.Id && service.StatusCode == "ACTIVE" &&
                dbContext.CategoryPolicies.Any(policy =>
                    policy.CategoryId == service.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                    policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today))))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(category.PublicId, category.Name, category.LevelCode, category.ExternalCode, category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryResponse>?> GetServicesAsync(Guid middlePublicId, CancellationToken cancellationToken)
    {
        var middle = await dbContext.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.PublicId == middlePublicId && category.LevelCode == "MIDDLE" && category.StatusCode == "ACTIVE", cancellationToken);
        if (middle is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await dbContext.ServiceCategories.AsNoTracking()
            .Where(service => service.ParentId == middle.Id && service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE")
            .Where(service => dbContext.CategoryPolicies.Any(policy =>
                policy.CategoryId == service.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today)))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(category.PublicId, category.Name, category.LevelCode, category.ExternalCode, category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestFieldResponse>?> GetRequestFieldsAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var service = await dbContext.ServiceCategories.AsNoTracking()
            .SingleOrDefaultAsync(category =>
                category.PublicId == servicePublicId && category.LevelCode == "SERVICE" && category.StatusCode == "ACTIVE" &&
                dbContext.CategoryPolicies.Any(policy =>
                    policy.CategoryId == category.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                    policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today)),
                cancellationToken);
        if (service?.ParentId is null)
        {
            return null;
        }

        var fields = await (
                from assignment in dbContext.CategoryFieldAssignments.AsNoTracking()
                join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on assignment.FieldDefinitionId equals field.Id
                where assignment.IsActive && field.StatusCode == "ACTIVE" &&
                      (assignment.TargetCategoryId == service.Id || assignment.TargetCategoryId == service.ParentId)
                orderby field.DisplayOrder, field.Id
                select field)
            .ToListAsync(cancellationToken);

        return fields.Select(field => new RequestFieldResponse(
            field.PublicId,
            field.FieldKey,
            field.Label,
            field.FieldTypeCode,
            field.IsRequired,
            null,
            ParseOptions(field.FieldTypeCode, field.OptionsOrUnitText),
            field.OptionsOrUnitText,
            field.ValidationRuleText,
            field.DisplayOrder)).ToArray();
    }

    public Task<List<AdministrativeAreaResponse>> GetActiveSidoAsync(CancellationToken cancellationToken) =>
        dbContext.AdministrativeAreas.AsNoTracking()
            .Where(area => area.AreaLevelCode == "SIDO" && area.IsActive)
            .OrderBy(area => area.AreaName)
            .Select(area => new AdministrativeAreaResponse(area.PublicId, area.AreaName, area.AreaCode, null, null))
            .ToListAsync(cancellationToken);

    public async Task<List<AdministrativeAreaResponse>> GetActiveSigunguAsync(Guid? parentPublicId, CancellationToken cancellationToken)
    {
        long? parentId = null;
        if (parentPublicId.HasValue)
        {
            parentId = await dbContext.AdministrativeAreas.AsNoTracking()
                .Where(area => area.PublicId == parentPublicId && area.AreaLevelCode == "SIDO" && area.IsActive)
                .Select(area => (long?)area.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (parentId is null) return [];
        }

        return await (
                from area in dbContext.AdministrativeAreas.AsNoTracking()
                join parent in dbContext.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parent.Id into parentGroup
                from parent in parentGroup.DefaultIfEmpty()
                where area.AreaLevelCode == "SIGUNGU" && area.IsActive && (parentId == null || area.ParentAreaId == parentId)
                orderby parent.AreaName, area.AreaName
                select new AdministrativeAreaResponse(
                    area.PublicId,
                    area.AreaName,
                    area.AreaCode,
                    parent == null ? null : parent.PublicId,
                    parent == null ? null : parent.AreaName))
            .ToListAsync(cancellationToken);
    }

    public static IReadOnlyList<string> ParseOptions(string fieldType, string? rawOptions)
    {
        if (fieldType is not ("SELECT" or "RADIO" or "CHECKBOX") || string.IsNullOrWhiteSpace(rawOptions))
        {
            return [];
        }

        return rawOptions.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
