using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/requests")]
public sealed class AdminRequestsController(AdminRequestTransactionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminRequestListResponse>> Search([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] bool? accepted, [FromQuery] bool? hasTransaction, [FromQuery] DateOnly? requestedFrom,
        [FromQuery] DateOnly? requestedTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default)
    {
        try { return Ok(await service.SearchRequestsAsync(search, status, accepted, hasTransaction, requestedFrom, requestedTo, page, pageSize, token)); }
        catch (AdminServiceCategoryException ex) { return StatusCode(ex.StatusCode, ApiErrorResponse.Create(HttpContext, ex.BusinessCode, ex.Message)); }
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<AdminRequestDetailResponse>> Get(Guid requestId, CancellationToken token)
    {
        var result = await service.GetRequestAsync(requestId, token);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_REQUEST_NOT_FOUND", "요청을 찾을 수 없습니다.")) : Ok(result);
    }
}
