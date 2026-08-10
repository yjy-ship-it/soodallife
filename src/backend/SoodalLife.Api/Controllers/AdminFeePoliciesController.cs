using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/service-categories/services/{serviceId:guid}/fee-policies")]
public sealed class AdminFeePoliciesController(AdminFeePolicyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminFeePolicyListResponse>> GetPolicies(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await service.GetPoliciesAsync(serviceId, cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다."))
            : Ok(result);
    }

    [HttpGet("effective")]
    public async Task<ActionResult<EffectiveFeePolicyResult>> GetEffective(
        Guid serviceId,
        [FromQuery] DateOnly? effectiveOn,
        [FromQuery] string transactionType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionType))
            return BadRequest(ApiErrorResponse.Create(HttpContext, "ADMIN_FEE_POLICY_TRANSACTION_TYPE_REQUIRED", "거래유형을 입력해 주세요."));
        var result = await service.ResolveEffectiveAsync(
            serviceId,
            effectiveOn ?? DateOnly.FromDateTime(DateTime.UtcNow),
            transactionType,
            cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_FEE_POLICY_NOT_FOUND", "기준일에 적용되는 수수료정책이 없습니다."))
            : Ok(result);
    }

    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<AdminFeePolicyResponse>> GetPolicy(Guid serviceId, Guid policyId, CancellationToken cancellationToken)
    {
        var result = await service.GetPolicyAsync(serviceId, policyId, cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_FEE_POLICY_NOT_FOUND", "수수료정책을 찾을 수 없습니다."))
            : Ok(result);
    }

    [HttpPost]
    public Task<ActionResult<AdminFeePolicyResponse>> Create(Guid serviceId, SaveAdminFeePolicyRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateVersionAsync(serviceId, request, ActorId(), cancellationToken), StatusCodes.Status201Created);

    [HttpPut("{policyId:guid}")]
    public Task<ActionResult<AdminFeePolicyResponse>> Update(Guid serviceId, Guid policyId, SaveAdminFeePolicyRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.UpdateFutureVersionAsync(serviceId, policyId, request, ActorId(), cancellationToken));

    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<ActionResult<AdminFeePolicyResponse>> ExecuteAsync(
        Func<Task<AdminFeePolicyResponse>> action,
        int statusCode = StatusCodes.Status200OK)
    {
        try { return StatusCode(statusCode, await action()); }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
