using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/providers")]
public sealed class AdminProvidersController(AdminProviderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminProviderListResponse>> Search([FromQuery] string? search, [FromQuery] string? accountStatus,
        [FromQuery] string? approvalStatus, [FromQuery] string? activityStatus, [FromQuery] Guid? serviceId, [FromQuery] Guid? areaId,
        [FromQuery] string? documentStatus, [FromQuery] DateOnly? joinedFrom, [FromQuery] DateOnly? joinedTo,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    { try { return Ok(await service.SearchAsync(search, accountStatus, approvalStatus, activityStatus, serviceId, areaId, documentStatus, joinedFrom, joinedTo, page, pageSize, cancellationToken)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpGet("{providerId:guid}")]
    public async Task<ActionResult<AdminProviderDetailResponse>> Get(Guid providerId, CancellationToken cancellationToken)
    { var provider = await service.GetAsync(providerId, cancellationToken); return provider is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PROVIDER_NOT_FOUND", "공급자를 찾을 수 없습니다.")) : Ok(provider); }
}

