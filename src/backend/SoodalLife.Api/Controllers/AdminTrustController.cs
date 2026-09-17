using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/trust")]
public sealed class AdminTrustController(AdminTrustService service,TrustCalculationService calculation,TrustReferenceDataService referenceData,TrustReferenceDataInitializer initializer,ILogger<AdminTrustController> logger) : ControllerBase
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
        return value is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_TRUST_PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.")) : Ok(value);
    }

    [HttpGet("policies")]
    public Task<ActionResult> Policies(CancellationToken token)=>Run(async()=>await calculation.GetPoliciesAsync(token));
    [HttpPut("policies/{id:guid}")]
    public Task<ActionResult> UpdatePolicy(Guid id,TrustPolicyRequest request,CancellationToken token)=>Run(async()=>await calculation.UpdatePolicyAsync(id,request,Actor(),token));
    [HttpPost("policies/{id:guid}/save")]
    public Task<ActionResult> SavePolicy(Guid id,TrustPolicyRequest request,CancellationToken token)=>Run(async()=>await calculation.UpdatePolicyAsync(id,request,Actor(),token));
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
    [HttpPost("{providerId:guid}/recalculate")]
    public Task<ActionResult> Recalculate(Guid providerId,TrustRecalculationRequest request,CancellationToken token)=>Run(async()=>await calculation.RecalculateActiveAsync(providerId,request.IdempotencyKey,Actor(),token));
    [HttpPost("{providerId:guid}/adjustment")]
    public Task<ActionResult> Adjust(Guid providerId,TrustManualAdjustmentRequest request,CancellationToken token)=>Run(async()=>await calculation.AdjustScoreAsync(providerId,request,Actor(),token));

    [HttpGet("reference-data")]
    public Task<ActionResult> ReferenceData(CancellationToken token)=>Run(async()=>await referenceData.GetAsync(token));
    [HttpPost("reference-data/initialize")]
    public Task<ActionResult> InitializeReferenceData(CancellationToken token)=>Run(async()=>{await initializer.InitializeAsync(token);return await referenceData.GetAsync(token);});
    [HttpPost("reference-data/rating-items")]
    public Task<ActionResult> CreateRatingItem(CreateTrustRatingItemRequest request,CancellationToken token)=>Run(async()=>await referenceData.CreateRatingItemAsync(request,await ActorDatabaseId(token),token));
    [HttpPut("reference-data/rating-items/{id:guid}")]
    public Task<ActionResult> UpdateRatingItem(Guid id,UpdateTrustRatingItemRequest request,CancellationToken token)=>Run(async()=>await referenceData.UpdateRatingItemAsync(id,request,await ActorDatabaseId(token),token));

    private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor)?actor:throw new TrustCalculationException("ADMIN_IDENTITY_INVALID","관리자 로그인 정보를 확인할 수 없습니다.",401);
    private async Task<long> ActorDatabaseId(CancellationToken token)=>await HttpContext.RequestServices.GetRequiredService<SoodalLife.Api.Infrastructure.Persistence.SoodalLifeDbContext>().Users.Where(x=>x.PublicId==Actor()).Select(x=>x.Id).SingleAsync(token);
    private async Task<ActionResult> Run(Func<Task<object>> action)
    {
        try{return Ok(await action());}
        catch(TrustCalculationException exception){return StatusCode(exception.StatusCode,ApiErrorResponse.Create(HttpContext,exception.BusinessCode,exception.Message));}
        catch(Exception exception)
        {
            logger.LogError(exception,"Admin trust operation failed: {Method} {Path} TraceId={TraceId}",Request.Method,Request.Path,HttpContext.TraceIdentifier);
            var cause=exception.GetBaseException();
            var diagnostic=cause is Microsoft.Data.SqlClient.SqlException sql
                ?$"DB-{sql.Number}"
                :cause.GetType().Name;
            return StatusCode(500,ApiErrorResponse.Create(HttpContext,"ADMIN_TRUST_OPERATION_FAILED",$"신뢰도 정책 처리 중 서버 오류가 발생했습니다. 오류 코드: {diagnostic}, 추적번호: {HttpContext.TraceIdentifier}"));
        }
    }
}
