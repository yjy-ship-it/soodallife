using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/trust")]
public sealed class AdminTrustController(AdminTrustService service,TrustCalculationService calculation) : ControllerBase
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

    [HttpGet("policies")]
    public Task<ActionResult> Policies(CancellationToken token)=>Run(async()=>await calculation.GetPoliciesAsync(token));
    [HttpPut("policies/{id:guid}")]
    public Task<ActionResult> UpdatePolicy(Guid id,TrustPolicyRequest request,CancellationToken token)=>Run(async()=>await calculation.UpdatePolicyAsync(id,request,Actor(),token));
    [HttpPost("policies/{id:guid}/clone")]
    public Task<ActionResult> ClonePolicy(Guid id,TrustPolicyCloneRequest request,CancellationToken token)=>Run(async()=>await calculation.ClonePolicyAsync(id,request,Actor(),token));
    [HttpPost("policies/{id:guid}/approve")]
    public Task<ActionResult> ApprovePolicy(Guid id,TrustPolicyActionRequest request,CancellationToken token)=>Run(async()=>await calculation.ApprovePolicyAsync(id,request,Actor(),token));
    [HttpPost("policies/{id:guid}/activate")]
    public Task<ActionResult> ActivatePolicy(Guid id,TrustPolicyActionRequest request,CancellationToken token)=>Run(async()=>await calculation.ActivatePolicyAsync(id,request,Actor(),token));
    [HttpPost("policies/{id:guid}/retire")]
    public Task<ActionResult> RetirePolicy(Guid id,TrustPolicyActionRequest request,CancellationToken token)=>Run(async()=>await calculation.RetirePolicyAsync(id,request,Actor(),token));
    [HttpPost("{providerId:guid}/simulation")]
    public Task<ActionResult> Simulate(Guid providerId,TrustSimulationRequest request,CancellationToken token)=>Run(async()=>await calculation.SimulateAsync(providerId,request.PolicyId,request.IdempotencyKey,Actor(),token));

    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult> Run(Func<Task<object>> action){try{return Ok(await action());}catch(TrustCalculationException exception){return StatusCode(exception.StatusCode,ApiErrorResponse.Create(HttpContext,exception.BusinessCode,exception.Message));}}
}
