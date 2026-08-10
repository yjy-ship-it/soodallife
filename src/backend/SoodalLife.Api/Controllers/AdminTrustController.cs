using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/trust")]
public sealed class AdminTrustController(AdminTrustService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminTrustListResponse>> Search([FromQuery] string? search, [FromQuery] string? evaluationStatus,
        [FromQuery] string? grade, [FromQuery] string? approvalStatus, [FromQuery] string? activityStatus,
        [FromQuery] bool? hasExpiredEvidence, [FromQuery] bool? hasAfterService, [FromQuery] bool? hasDispute,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try { return Ok(await service.SearchAsync(search, evaluationStatus, grade, approvalStatus, activityStatus, hasExpiredEvidence, hasAfterService, hasDispute, page, pageSize, cancellationToken)); }
        catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    [HttpGet("{providerId:guid}")]
    public async Task<ActionResult<AdminTrustDetailResponse>> Get(Guid providerId, CancellationToken cancellationToken)
    {
        var value = await service.GetAsync(providerId, cancellationToken);
        return value is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_TRUST_PROVIDER_NOT_FOUND", "공급자를 찾을 수 없습니다.")) : Ok(value);
    }
}
