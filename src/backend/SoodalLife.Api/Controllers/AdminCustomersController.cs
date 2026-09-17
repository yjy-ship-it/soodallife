using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/customers")]
public sealed class AdminCustomersController(AdminCustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminCustomerListResponse>> Search(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] bool? hasRequests,
        [FromQuery] bool? hasTransactions,
        [FromQuery] DateOnly? joinedFrom,
        [FromQuery] DateOnly? joinedTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.SearchAsync(search, status, hasRequests, hasTransactions, joinedFrom, joinedTo, page, pageSize, cancellationToken));
        }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<AdminCustomerDetailResponse>> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await service.GetDetailAsync(customerId, cancellationToken);
        return customer is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_CUSTOMER_NOT_FOUND", "고객을 찾을 수 없습니다."))
            : Ok(customer);
    }

    [HttpPost("{customerId:guid}/requests/{requestId:guid}/abuse-exclusion")]
    public async Task<IActionResult> SetRequestAbuseExclusion(
        Guid customerId,
        Guid requestId,
        AdminCustomerRequestAbuseExclusionRequest input,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await service.SetRequestAbuseExclusionAsync(customerId, requestId, input, actor, cancellationToken);
            return NoContent();
        }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
