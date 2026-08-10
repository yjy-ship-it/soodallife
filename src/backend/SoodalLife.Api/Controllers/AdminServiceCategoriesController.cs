using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/service-categories")]
public sealed class AdminServiceCategoriesController(AdminServiceCategoryService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminCategorySummaryResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await service.GetSummaryAsync(cancellationToken));

    [HttpGet("majors")]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryOptionResponse>>> GetMajors(CancellationToken cancellationToken) =>
        Ok(await service.GetMajorsAsync(cancellationToken));

    [HttpGet("majors/{majorId:guid}/middles")]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryOptionResponse>>> GetMiddles(
        Guid majorId,
        CancellationToken cancellationToken)
    {
        var categories = await service.GetMiddlesAsync(majorId, cancellationToken);
        return categories is null ? NotFound() : Ok(categories);
    }

    [HttpGet("services")]
    public async Task<ActionResult<AdminServiceCategoryListResponse>> SearchServices(
        [FromQuery] string? search,
        [FromQuery] Guid? majorId,
        [FromQuery] Guid? middleId,
        [FromQuery] string? status,
        [FromQuery] decimal? feeAmount,
        [FromQuery] string? feeStatus,
        [FromQuery] DateOnly? feeEffectiveFrom,
        [FromQuery] DateOnly? feeEffectiveTo,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.SearchServicesAsync(search, majorId, middleId, status, feeAmount, feeStatus, feeEffectiveFrom, feeEffectiveTo, cancellationToken));
        }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }

    [HttpGet("services/{serviceId:guid}")]
    public async Task<ActionResult<AdminServiceCategoryDetailResponse>> GetService(
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var category = await service.GetServiceAsync(serviceId, cancellationToken);
        return category is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다."))
            : Ok(category);
    }

    [HttpPut("services/{serviceId:guid}")]
    public async Task<ActionResult<AdminServiceCategoryDetailResponse>> UpdateService(
        Guid serviceId,
        UpdateAdminServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorPublicId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await service.UpdateServiceAsync(serviceId, request, actorPublicId, cancellationToken));
        }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
