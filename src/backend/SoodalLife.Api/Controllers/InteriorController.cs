using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Interior;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/interior")]
public sealed class AdminInteriorController(InteriorProjectService service):ControllerBase
{
    [HttpGet("eligible-services")]public Task<ActionResult<IReadOnlyList<InteriorServiceResponse>>> Services(CancellationToken token)=>Run(()=>service.Services(token));
    [HttpGet("projects")]public Task<ActionResult<List<InteriorProjectListItem>>> Projects([FromQuery]string? search,[FromQuery]string? status,[FromQuery]Guid? providerId,[FromQuery]Guid? areaId,[FromQuery]DateOnly? from,[FromQuery]DateOnly? to,CancellationToken token)=>Run(()=>service.List(search,status,providerId,areaId,from,to,token));
    [HttpGet("projects/{id:guid}")]public Task<ActionResult<InteriorProjectDetail>> Project(Guid id,CancellationToken token)=>Run(()=>service.Detail(id,token));
    // 정상 거래는 고객과 전문가가 직접 진행합니다. 본사 관리자는 조회와 별도 분쟁·제재 업무만 수행합니다.
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> call){try{return Ok(await call());}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
}

[ApiController,Route("api/v1/interior")]
public sealed class InteriorController(InteriorProjectService service):ControllerBase
{
    [Authorize(Roles=RoleCodes.Provider),HttpGet("site-visits/{id:guid}")]public Task<ActionResult<ProviderSiteVisitPrivateResponse>> Visit(Guid id,CancellationToken token)=>Run(()=>service.ProviderSiteVisit(id,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpGet("providers/me/projects/{id:guid}")]public Task<ActionResult<ProviderInteriorProjectPrivateResponse>> ProjectContact(Guid id,CancellationToken token)=>Run(()=>service.ProviderProjectContact(id,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("projects/{id:guid}/completion-report")]public Task<ActionResult<InteriorProjectDetail>> CompletionReport(Guid id,SubmitInteriorProjectCompletionRequest input,CancellationToken token)=>Run(()=>service.SubmitProjectCompletion(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("contracts/{id:guid}/agreement")]public Task<ActionResult<InteriorContractResponse>> Agree(Guid id,AgreeInteriorContractRequest input,CancellationToken token)=>Run(()=>service.AgreeContract(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("payment-plans/{id:guid}/confirmations")]public Task<ActionResult<Guid>> Payment(Guid id,ConfirmInteriorPaymentRequest input,CancellationToken token)=>Run(()=>service.ConfirmPayment(id,input,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("stages/{id:guid}/updates")]public Task<ActionResult<InteriorWorkStageResponse>> Update(Guid id,AddInteriorWorkUpdateRequest input,CancellationToken token)=>Run(()=>service.UpdateStage(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("contracts/{id:guid}/changes")]public Task<ActionResult<InteriorChangeResponse>> Change(Guid id,CreateInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.RequestChange(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("changes/{id:guid}/decision")]public Task<ActionResult<InteriorChangeResponse>> Decide(Guid id,DecideInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.DecideChange(id,input,User,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> call){try{return Ok(await call());}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
}
