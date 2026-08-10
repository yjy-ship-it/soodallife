using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/service-categories/services/{serviceId:guid}/price-policies")]
public sealed class AdminPricePoliciesController(AdminPricePolicyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminPricePolicyListResponse>> GetPolicies(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await service.GetPoliciesAsync(serviceId, cancellationToken);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다.")) : Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<AdminPricePolicyResponse>> GetCurrent(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await service.GetCurrentAsync(serviceId, cancellationToken);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PRICE_POLICY_NOT_FOUND", "현재 적용 중인 가격정책이 없습니다.")) : Ok(result);
    }

    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<AdminPricePolicyResponse>> GetPolicy(Guid serviceId, Guid policyId, CancellationToken cancellationToken)
    {
        var result = await service.GetPolicyAsync(serviceId, policyId, cancellationToken);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PRICE_POLICY_NOT_FOUND", "가격정책을 찾을 수 없습니다.")) : Ok(result);
    }

    [HttpPost]
    public Task<ActionResult<AdminPricePolicyResponse>> Create(Guid serviceId, SaveAdminPricePolicyRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateVersionAsync(serviceId, request, ActorId(), cancellationToken), StatusCodes.Status201Created);

    [HttpPut("{policyId:guid}")]
    public Task<ActionResult<AdminPricePolicyResponse>> Update(Guid serviceId, Guid policyId, SaveAdminPricePolicyRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.UpdateFutureVersionAsync(serviceId, policyId, request, ActorId(), cancellationToken));

    [HttpPost("{policyId:guid}/options")]
    public Task<ActionResult<AdminPricePolicyResponse>> CreateOption(Guid serviceId, Guid policyId, SaveAdminPricePolicyOptionRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateOptionAsync(serviceId, policyId, request, ActorId(), cancellationToken), StatusCodes.Status201Created);

    [HttpPut("{policyId:guid}/options/{optionId:guid}")]
    public Task<ActionResult<AdminPricePolicyResponse>> UpdateOption(Guid serviceId, Guid policyId, Guid optionId, SaveAdminPricePolicyOptionRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.UpdateOptionAsync(serviceId, policyId, optionId, request, ActorId(), cancellationToken));

    [HttpPost("{policyId:guid}/surcharges")]
    public Task<ActionResult<AdminPricePolicyResponse>> CreateSurcharge(Guid serviceId, Guid policyId, SaveAdminPricePolicySurchargeRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.CreateSurchargeAsync(serviceId, policyId, request, ActorId(), cancellationToken), StatusCodes.Status201Created);

    [HttpPut("{policyId:guid}/surcharges/{surchargeId:guid}")]
    public Task<ActionResult<AdminPricePolicyResponse>> UpdateSurcharge(Guid serviceId, Guid policyId, Guid surchargeId, SaveAdminPricePolicySurchargeRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => service.UpdateSurchargeAsync(serviceId, policyId, surchargeId, request, ActorId(), cancellationToken));

    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<ActionResult<AdminPricePolicyResponse>> ExecuteAsync(Func<Task<AdminPricePolicyResponse>> action, int statusCode = StatusCodes.Status200OK)
    {
        try { return StatusCode(statusCode, await action()); }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
