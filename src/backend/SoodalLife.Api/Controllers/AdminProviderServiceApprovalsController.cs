using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/providers/{providerId:guid}/service-approvals")]
public sealed class AdminProviderServiceApprovalsController(AdminProviderServiceApprovalService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>> GetList(Guid providerId, CancellationToken cancellationToken)
    { var result = await service.GetListAsync(providerId, cancellationToken); return result is null ? NotFound() : Ok(result); }
    [HttpGet("{serviceId:guid}")]
    public async Task<ActionResult<AdminProviderServiceApprovalDecisionResponse>> Get(Guid providerId, Guid serviceId, CancellationToken cancellationToken)
    { var result = await service.GetAsync(providerId, serviceId, cancellationToken); return result is null ? NotFound() : Ok(result); }
    [HttpPost("{serviceId:guid}/decisions")]
    public async Task<ActionResult<AdminProviderServiceApprovalDecisionResponse>> Decide(Guid providerId, Guid serviceId, AdminProviderServiceApprovalDecisionRequest request, CancellationToken cancellationToken)
    { try { return Ok(await service.DecideAsync(providerId, serviceId, request, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
}

