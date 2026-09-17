using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/service-categories/services/{serviceId:guid}/provider-requirements")]
public sealed class AdminProviderRequirementsController(AdminProviderRequirementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminProviderRequirementListResponse>> GetPolicies(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await service.GetPoliciesAsync(serviceId, cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다."))
            : Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<AdminProviderRequirementResponse>> GetCurrent(Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await service.GetCurrentAsync(serviceId, cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PROVIDER_REQUIREMENT_NOT_FOUND", "현재 적용 중인 전문가 요건이 없습니다."))
            : Ok(result);
    }

    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<AdminProviderRequirementResponse>> GetPolicy(Guid serviceId, Guid policyId, CancellationToken cancellationToken)
    {
        var result = await service.GetPolicyAsync(serviceId, policyId, cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PROVIDER_REQUIREMENT_NOT_FOUND", "전문가 요건 이력을 찾을 수 없습니다."))
            : Ok(result);
    }

    [HttpPost("{policyId:guid}/assignments")]
    public Task<ActionResult<AdminProviderRequirementResponse>> CreateAssignment(Guid serviceId, Guid policyId, SaveAdminCategoryProviderRequirementRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.CreateAssignmentAsync(serviceId, policyId, request, ActorId(), cancellationToken), StatusCodes.Status201Created);

    [HttpPut("{policyId:guid}/assignments/{assignmentId:guid}")]
    public Task<ActionResult<AdminProviderRequirementResponse>> UpdateAssignment(Guid serviceId, Guid policyId, Guid assignmentId, SaveAdminCategoryProviderRequirementRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.UpdateAssignmentAsync(serviceId, policyId, assignmentId, request, ActorId(), cancellationToken));

    [HttpPut("{policyId:guid}/assignments/{assignmentId:guid}/evidence-types")]
    public Task<ActionResult<AdminProviderRequirementResponse>> ReplaceEvidence(Guid serviceId, Guid policyId, Guid assignmentId, ReplaceAdminCategoryProviderRequirementEvidenceRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.ReplaceEvidenceAsync(serviceId, policyId, assignmentId, request, ActorId(), cancellationToken));

    [HttpPut("{policyId:guid}/operation-policy")]
    public Task<ActionResult<AdminProviderRequirementResponse>> UpdateOperationPolicy(Guid serviceId, Guid policyId, SaveAdminOperationPolicyRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.UpdateOperationPolicyAsync(serviceId, policyId, request, ActorId(), cancellationToken));

    [HttpPut("~/api/v1/admin/service-categories/middles/{middleId:guid}/operation-policy")]
    public async Task<ActionResult<IReadOnlyList<AdminProviderRequirementResponse>>> UpdateMiddleOperationPolicies(Guid middleId, SaveAdminMiddleOperationPolicyRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.UpdateMiddleOperationPoliciesAsync(middleId, request, ActorId(), cancellationToken)); }
        catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    [HttpPut("~/api/v1/admin/service-categories/middles/{middleId:guid}/provider-requirements/apply")]
    public async Task<ActionResult<IReadOnlyList<AdminProviderRequirementResponse>>> ApplyMiddleProviderRequirements(Guid middleId, ApplyAdminMiddleProviderRequirementsRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ApplyMiddleProviderRequirementsAsync(middleId, request, ActorId(), cancellationToken)); }
        catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<AdminProviderRequirementResponse>> Execute(Func<Task<AdminProviderRequirementResponse>> action, int status = StatusCodes.Status200OK)
    {
        try { return StatusCode(status, await action()); }
        catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
