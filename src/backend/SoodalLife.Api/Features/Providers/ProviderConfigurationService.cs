using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public sealed class ProviderConfigurationService(SoodalLifeDbContext dbContext)
{
    public async Task<ProviderProfileResponse> GetProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        return new ProviderProfileResponse(
            identity.Profile.PublicId,
            identity.Profile.BusinessName,
            identity.Profile.ApprovalStatusCode,
            identity.Profile.ActivityStatusCode);
    }

    public async Task<IReadOnlyList<ProviderServiceCategoryResponse>> GetServiceCategoriesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        return await QueryServiceCategories(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceCategoryResponse>> ReplaceServiceCategoriesAsync(
        ClaimsPrincipal principal,
        ReplaceProviderServiceCategoriesInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetWritableIdentityAsync(principal, cancellationToken);
        var categoryIds = input.CategoryIds ?? [];
        if (categoryIds.Count != categoryIds.Distinct().Count())
        {
            throw Invalid("PROVIDER_SERVICE_DUPLICATE", "동일한 서비스 카테고리를 중복 선택할 수 없습니다.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = await dbContext.ServiceCategories
            .Where(category => categoryIds.Contains(category.PublicId) &&
                               category.LevelCode == "SERVICE" && category.StatusCode == "ACTIVE" &&
                               dbContext.CategoryPolicies.Any(policy =>
                                   policy.CategoryId == category.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                                   policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today)))
            .ToListAsync(cancellationToken);
        if (categories.Count != categoryIds.Count)
        {
            throw Invalid("PROVIDER_SERVICE_INVALID", "현재 제공 가능한 하위 서비스만 선택할 수 있습니다.");
        }

        var now = DateTime.UtcNow;
        var existing = await dbContext.ProviderServiceCategories
            .Where(item => item.ProviderProfileId == identity.Profile.Id)
            .ToListAsync(cancellationToken);
        var desiredInternalIds = categories.Select(category => category.Id).ToHashSet();
        foreach (var item in existing)
        {
            var active = desiredInternalIds.Contains(item.CategoryId);
            var wasActive = item.StatusCode == "ACTIVE";
            item.StatusCode = active ? "ACTIVE" : "INACTIVE";
            item.ActivatedAt = active && !wasActive ? now : item.ActivatedAt;
            item.DeactivatedAt = active ? null : item.DeactivatedAt ?? now;
            item.UpdatedAt = now;
            item.UpdatedByUserId = identity.UserId;
        }

        foreach (var category in categories.Where(category => existing.All(item => item.CategoryId != category.Id)))
        {
            dbContext.ProviderServiceCategories.Add(new ProviderServiceCategory
            {
                ProviderProfileId = identity.Profile.Id,
                CategoryId = category.Id,
                StatusCode = "ACTIVE",
                ActivatedAt = now,
                CreatedAt = now,
                CreatedByUserId = identity.UserId,
                UpdatedAt = now,
                UpdatedByUserId = identity.UserId,
            });
        }

        var deactivatedIds = existing
            .Where(item => item.StatusCode == "INACTIVE")
            .Select(item => item.Id)
            .ToArray();
        if (deactivatedIds.Length > 0)
        {
            var areas = await dbContext.ProviderServiceAreas
                .Where(area => deactivatedIds.Contains(area.ProviderServiceCategoryId) && area.StatusCode == "ACTIVE")
                .ToListAsync(cancellationToken);
            foreach (var area in areas)
            {
                area.StatusCode = "INACTIVE";
                area.DeactivatedAt = now;
                area.UpdatedAt = now;
                area.UpdatedByUserId = identity.UserId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await QueryServiceCategories(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceAreaResponse>> GetServiceAreasAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        return await QueryServiceAreas(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceAreaResponse>> ReplaceServiceAreasAsync(
        ClaimsPrincipal principal,
        ReplaceProviderServiceAreasInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetWritableIdentityAsync(principal, cancellationToken);
        var selections = input.Services ?? [];
        if (selections.Count != selections.Select(selection => selection.ServiceCategoryId).Distinct().Count() ||
            selections.Any(selection => selection.AdministrativeAreaIds.Count != selection.AdministrativeAreaIds.Distinct().Count()))
        {
            throw Invalid("PROVIDER_AREA_DUPLICATE", "동일한 서비스 또는 출장지역을 중복 선택할 수 없습니다.");
        }

        var serviceCategoryPublicIds = selections.Select(selection => selection.ServiceCategoryId).ToArray();
        var providerServices = await (
                from providerService in dbContext.ProviderServiceCategories
                join category in dbContext.ServiceCategories on providerService.CategoryId equals category.Id
                where providerService.ProviderProfileId == identity.Profile.Id && providerService.StatusCode == "ACTIVE" &&
                      serviceCategoryPublicIds.Contains(category.PublicId)
                select new { ProviderService = providerService, Category = category })
            .ToListAsync(cancellationToken);
        if (providerServices.Count != serviceCategoryPublicIds.Length)
        {
            throw Invalid("PROVIDER_AREA_SERVICE_INVALID", "본인의 활성 제공 서비스에 대해서만 출장지역을 설정할 수 있습니다.");
        }

        var areaPublicIds = selections.SelectMany(selection => selection.AdministrativeAreaIds).Distinct().ToArray();
        var areas = await dbContext.AdministrativeAreas
            .Where(area => areaPublicIds.Contains(area.PublicId) && area.AreaLevelCode == "SIGUNGU" && area.IsActive)
            .ToListAsync(cancellationToken);
        if (areas.Count != areaPublicIds.Length)
        {
            throw Invalid("PROVIDER_AREA_INVALID", "활성 시·군·구 지역만 선택할 수 있습니다.");
        }

        var providerServiceIds = await dbContext.ProviderServiceCategories
            .Where(item => item.ProviderProfileId == identity.Profile.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var existing = await dbContext.ProviderServiceAreas
            .Where(item => providerServiceIds.Contains(item.ProviderServiceCategoryId))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var item in existing.Where(item => item.StatusCode == "ACTIVE"))
        {
            item.StatusCode = "INACTIVE";
            item.DeactivatedAt = now;
            item.UpdatedAt = now;
            item.UpdatedByUserId = identity.UserId;
        }

        var areaByPublicId = areas.ToDictionary(area => area.PublicId);
        var serviceByPublicId = providerServices.ToDictionary(item => item.Category.PublicId);
        foreach (var selection in selections)
        {
            var providerService = serviceByPublicId[selection.ServiceCategoryId].ProviderService;
            foreach (var areaPublicId in selection.AdministrativeAreaIds)
            {
                var area = areaByPublicId[areaPublicId];
                var item = existing.SingleOrDefault(candidate =>
                    candidate.ProviderServiceCategoryId == providerService.Id && candidate.AdministrativeAreaId == area.Id);
                if (item is null)
                {
                    item = new ProviderServiceArea
                    {
                        ProviderServiceCategoryId = providerService.Id,
                        AdministrativeAreaId = area.Id,
                        CreatedAt = now,
                        CreatedByUserId = identity.UserId,
                    };
                    dbContext.ProviderServiceAreas.Add(item);
                    existing.Add(item);
                }

                item.StatusCode = "ACTIVE";
                item.ActivatedAt = now;
                item.DeactivatedAt = null;
                item.UpdatedAt = now;
                item.UpdatedByUserId = identity.UserId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await QueryServiceAreas(identity.Profile.Id, cancellationToken);
    }

    private async Task<List<ProviderServiceCategoryResponse>> QueryServiceCategories(long providerProfileId, CancellationToken cancellationToken) =>
        await (
                from providerService in dbContext.ProviderServiceCategories.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on providerService.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                where providerService.ProviderProfileId == providerProfileId && providerService.StatusCode == "ACTIVE"
                orderby major.SortOrder, middle.SortOrder, service.SortOrder
                select new ProviderServiceCategoryResponse(
                    service.PublicId,
                    major.Name + " > " + middle.Name + " > " + service.Name,
                    providerService.StatusCode))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ProviderServiceAreaResponse>> QueryServiceAreas(long providerProfileId, CancellationToken cancellationToken)
    {
        var services = await (
                from providerService in dbContext.ProviderServiceCategories.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on providerService.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                where providerService.ProviderProfileId == providerProfileId && providerService.StatusCode == "ACTIVE"
                orderby major.SortOrder, middle.SortOrder, service.SortOrder
                select new
                {
                    ProviderServiceId = providerService.Id,
                    service.PublicId,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                })
            .ToListAsync(cancellationToken);
        var providerServiceIds = services.Select(service => service.ProviderServiceId).ToArray();
        var areas = await (
                from providerArea in dbContext.ProviderServiceAreas.AsNoTracking()
                join area in dbContext.AdministrativeAreas.AsNoTracking() on providerArea.AdministrativeAreaId equals area.Id
                where providerServiceIds.Contains(providerArea.ProviderServiceCategoryId) && providerArea.StatusCode == "ACTIVE"
                orderby area.AreaName
                select new
                {
                    providerArea.ProviderServiceCategoryId,
                    Area = new ProviderAreaResponse(area.PublicId, area.AreaName, area.AreaCode),
                })
            .ToListAsync(cancellationToken);

        return services.Select(service => new ProviderServiceAreaResponse(
            service.PublicId,
            service.Path,
            areas.Where(area => area.ProviderServiceCategoryId == service.ProviderServiceId)
                .Select(area => area.Area)
                .ToArray())).ToArray();
    }

    private async Task<ProviderIdentity> GetWritableIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        if (identity.Profile.ApprovalStatusCode != "APPROVED" || identity.Profile.ActivityStatusCode != "ACTIVE")
        {
            throw new ProviderConfigurationException(
                "PROVIDER_NOT_ELIGIBLE",
                "승인되고 활성 상태인 공급자만 공급 범위를 설정할 수 있습니다.",
                StatusCodes.Status403Forbidden);
        }

        return identity;
    }

    private async Task<ProviderIdentity> GetIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
        {
            throw new InvalidOperationException("Authenticated user identifier is invalid.");
        }

        var identity = await (
                from user in dbContext.Users
                join provider in dbContext.ProviderProfiles on user.Id equals provider.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new ProviderIdentity(user.Id, provider))
            .SingleOrDefaultAsync(cancellationToken);
        return identity ?? throw new InvalidOperationException("The authenticated PROVIDER role has no provider profile.");
    }

    private static ProviderConfigurationException Invalid(string code, string message) => new(code, message);
    private sealed record ProviderIdentity(long UserId, ProviderProfile Profile);
}
