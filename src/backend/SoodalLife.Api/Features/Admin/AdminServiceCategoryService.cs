using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminServiceCategoryService(SoodalLifeDbContext dbContext)
{
    private static readonly string[] AllowedStatusCodes = ["ACTIVE", "PAUSED", "REVIEW"];

    public async Task<AdminCategorySummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var categories = dbContext.ServiceCategories.AsNoTracking();
        return new AdminCategorySummaryResponse(
            await categories.LongCountAsync(category => category.LevelCode == "MAJOR", cancellationToken),
            await categories.LongCountAsync(category => category.LevelCode == "MIDDLE", cancellationToken),
            await categories.LongCountAsync(category => category.LevelCode == "SERVICE", cancellationToken),
            await categories.LongCountAsync(category => category.LevelCode == "SERVICE" && category.StatusCode == "ACTIVE", cancellationToken));
    }

    public Task<List<AdminCategoryOptionResponse>> GetMajorsAsync(CancellationToken cancellationToken) =>
        dbContext.ServiceCategories.AsNoTracking()
            .Where(category => category.LevelCode == "MAJOR")
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new AdminCategoryOptionResponse(
                category.PublicId,
                category.Name,
                category.ExternalCode,
                category.StatusCode,
                category.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminCategoryOptionResponse>?> GetMiddlesAsync(
        Guid majorPublicId,
        CancellationToken cancellationToken)
    {
        var majorId = await dbContext.ServiceCategories.AsNoTracking()
            .Where(category => category.PublicId == majorPublicId && category.LevelCode == "MAJOR")
            .Select(category => (long?)category.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (majorId is null)
        {
            return null;
        }

        return await dbContext.ServiceCategories.AsNoTracking()
            .Where(category => category.ParentId == majorId && category.LevelCode == "MIDDLE")
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new AdminCategoryOptionResponse(
                category.PublicId,
                category.Name,
                category.ExternalCode,
                category.StatusCode,
                category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminServiceCategoryListResponse> SearchServicesAsync(
        string? search,
        Guid? majorPublicId,
        Guid? middlePublicId,
        string? statusCode,
        CancellationToken cancellationToken)
    {
        var query =
            from service in dbContext.ServiceCategories.AsNoTracking()
            join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
            join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
            where service.LevelCode == "SERVICE" && middle.LevelCode == "MIDDLE" && major.LevelCode == "MAJOR"
            select new { Service = service, Middle = middle, Major = major };

        if (majorPublicId.HasValue)
        {
            query = query.Where(row => row.Major.PublicId == majorPublicId.Value);
        }

        if (middlePublicId.HasValue)
        {
            query = query.Where(row => row.Middle.PublicId == middlePublicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusCode))
        {
            var normalizedStatus = NormalizeStatus(statusCode);
            query = query.Where(row => row.Service.StatusCode == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row =>
                row.Service.Name.Contains(term) ||
                row.Middle.Name.Contains(term) ||
                row.Major.Name.Contains(term) ||
                (row.Service.ExternalCode != null && row.Service.ExternalCode.Contains(term)) ||
                (row.Middle.ExternalCode != null && row.Middle.ExternalCode.Contains(term)) ||
                (row.Major.ExternalCode != null && row.Major.ExternalCode.Contains(term)));
        }

        var items = await query
            .OrderBy(row => row.Major.SortOrder)
            .ThenBy(row => row.Middle.SortOrder)
            .ThenBy(row => row.Service.SortOrder)
            .ThenBy(row => row.Service.Name)
            .Select(row => new AdminServiceCategoryListItemResponse(
                row.Service.PublicId,
                row.Service.Name,
                row.Service.ExternalCode,
                row.Service.StatusCode,
                row.Service.SortOrder,
                row.Major.PublicId,
                row.Major.Name,
                row.Middle.PublicId,
                row.Middle.Name))
            .ToListAsync(cancellationToken);

        return new AdminServiceCategoryListResponse(items.Count, items);
    }

    public Task<AdminServiceCategoryDetailResponse?> GetServiceAsync(Guid servicePublicId, CancellationToken cancellationToken) =>
        BuildDetailQuery(servicePublicId).SingleOrDefaultAsync(cancellationToken);

    public async Task<AdminServiceCategoryDetailResponse> UpdateServiceAsync(
        Guid servicePublicId,
        UpdateAdminServiceCategoryRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var service = await dbContext.ServiceCategories
            .SingleOrDefaultAsync(
                category => category.PublicId == servicePublicId && category.LevelCode == "SERVICE",
                cancellationToken)
            ?? throw new AdminServiceCategoryException(
                "ADMIN_SERVICE_NOT_FOUND",
                "수정할 서비스를 찾을 수 없습니다.",
                StatusCodes.Status404NotFound);

        var normalizedName = request.Name.Trim();
        var normalizedStatus = NormalizeStatus(request.StatusCode);
        if (await dbContext.ServiceCategories.AnyAsync(
                category => category.Id != service.Id && category.ParentId == service.ParentId && category.Name == normalizedName,
                cancellationToken))
        {
            throw new AdminServiceCategoryException(
                "ADMIN_SERVICE_NAME_DUPLICATED",
                "같은 중분류에 동일한 서비스명이 이미 있습니다.",
                StatusCodes.Status409Conflict);
        }

        var actorUserId = await dbContext.Users
            .Where(user => user.PublicId == actorPublicId && user.StatusCode == "ACTIVE")
            .Select(user => (long?)user.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException(
                "ADMIN_USER_NOT_FOUND",
                "현재 관리자 계정을 확인할 수 없습니다.",
                StatusCodes.Status401Unauthorized);

        var before = JsonSerializer.Serialize(new
        {
            service.Name,
            service.StatusCode,
            service.SortOrder,
        });

        service.Name = normalizedName;
        service.StatusCode = normalizedStatus;
        service.SortOrder = request.SortOrder;
        service.UpdatedAt = DateTime.UtcNow;
        service.UpdatedByUserId = actorUserId;

        dbContext.AuditLogs.Add(new AuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorRoleCode = RoleCodes.Admin,
            ActionCode = "SERVICE_CATEGORY_UPDATED",
            EntityType = "SERVICE_CATEGORY",
            EntityPublicId = service.PublicId,
            ResultCode = "SUCCESS",
            BeforeJson = before,
            AfterJson = JsonSerializer.Serialize(new
            {
                service.Name,
                service.StatusCode,
                service.SortOrder,
            }),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetServiceAsync(servicePublicId, cancellationToken))!;
    }

    private IQueryable<AdminServiceCategoryDetailResponse> BuildDetailQuery(Guid servicePublicId) =>
        from service in dbContext.ServiceCategories.AsNoTracking()
        join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
        join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
        where service.PublicId == servicePublicId && service.LevelCode == "SERVICE" && middle.LevelCode == "MIDDLE" && major.LevelCode == "MAJOR"
        select new AdminServiceCategoryDetailResponse(
            service.PublicId,
            service.Name,
            service.ExternalCode,
            service.StatusCode,
            service.SortOrder,
            major.PublicId,
            major.Name,
            middle.PublicId,
            middle.Name);

    private static string NormalizeStatus(string statusCode)
    {
        var normalizedStatus = statusCode.Trim().ToUpperInvariant();
        return AllowedStatusCodes.Contains(normalizedStatus, StringComparer.Ordinal)
            ? normalizedStatus
            : throw new AdminServiceCategoryException(
                "ADMIN_SERVICE_STATUS_INVALID",
                "운영상태를 확인해 주세요.");
    }
}
