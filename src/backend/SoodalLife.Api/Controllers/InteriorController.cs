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
    [HttpPost("projects")]public Task<ActionResult<InteriorProjectDetail>> Create(CreateInteriorProjectRequest input,CancellationToken token)=>Run(()=>service.Create(input,Actor(),token));
    [HttpPost("projects/{id:guid}/site-visits")]public Task<ActionResult<InteriorSiteVisitResponse>> AddVisit(Guid id,CreateInteriorSiteVisitRequest input,CancellationToken token)=>Run(()=>service.AddSiteVisit(id,input,Actor(),token));
    [HttpPost("projects/{id:guid}/site-visits/{visitId:guid}/confirmation")]public Task<ActionResult<InteriorSiteVisitResponse>> ConfirmVisit(Guid id,Guid visitId,ConfirmInteriorSiteVisitRequest input,CancellationToken token)=>Run(()=>service.ConfirmSiteVisit(id,visitId,input,Actor(),token));
    [HttpPost("projects/{id:guid}/site-visits/{visitId:guid}/completion")]public Task<ActionResult<InteriorSiteVisitResponse>> CompleteVisit(Guid id,Guid visitId,CompleteInteriorSiteVisitRequest input,CancellationToken token)=>Run(()=>service.CompleteSiteVisit(id,visitId,input,Actor(),token));
    [HttpPost("projects/{id:guid}/designs")]public Task<ActionResult<InteriorDesignResponse>> Design(Guid id,CreateInteriorDesignRequest input,CancellationToken token)=>Run(()=>service.AddDesign(id,input,Actor(),token));
    [HttpPost("projects/{id:guid}/contracts")]public Task<ActionResult<InteriorContractResponse>> Contract(Guid id,CreateInteriorContractRequest input,CancellationToken token)=>Run(()=>service.CreateContract(id,input,Actor(),token));
    [HttpPost("projects/{id:guid}/stages")]public Task<ActionResult<InteriorWorkStageResponse>> Stage(Guid id,CreateInteriorWorkStageRequest input,CancellationToken token)=>Run(()=>service.AddStage(id,input,Actor(),token));
    [HttpPost("stages/{id:guid}/inspections")]public Task<ActionResult<Guid>> Inspect(Guid id,InspectInteriorStageRequest input,CancellationToken token)=>Run(()=>service.Inspect(id,input,Actor(),token));
    [HttpPost("changes/{id:guid}/decision")]public Task<ActionResult<InteriorChangeResponse>> Decide(Guid id,DecideInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.DecideChange(id,input,User,token));
    [HttpPost("projects/{id:guid}/completion")]public Task<ActionResult<InteriorProjectDetail>> Complete(Guid id,CompleteInteriorProjectRequest input,CancellationToken token)=>Run(()=>service.CompleteProject(id,input,Actor(),token));
    [HttpPost("projects/{id:guid}/defects")]public Task<ActionResult<Guid>> Defect(Guid id,LinkInteriorDefectRequest input,CancellationToken token)=>Run(()=>service.LinkDefect(id,input,Actor(),token));
    [HttpPost("projects/{id:guid}/disputes")]public Task<ActionResult<Guid>> Dispute(Guid id,LinkInteriorDisputeRequest input,CancellationToken token)=>Run(()=>service.LinkDispute(id,input,Actor(),token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> call){try{return Ok(await call());}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
}

[ApiController,Route("api/v1/interior")]
public sealed class InteriorController(InteriorProjectService service):ControllerBase
{
    [Authorize(Roles=RoleCodes.Provider),HttpGet("site-visits/{id:guid}")]public Task<ActionResult<ProviderSiteVisitPrivateResponse>> Visit(Guid id,CancellationToken token)=>Run(()=>service.ProviderSiteVisit(id,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("contracts/{id:guid}/agreement")]public Task<ActionResult<InteriorContractResponse>> Agree(Guid id,AgreeInteriorContractRequest input,CancellationToken token)=>Run(()=>service.AgreeContract(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("payment-plans/{id:guid}/confirmations")]public Task<ActionResult<Guid>> Payment(Guid id,ConfirmInteriorPaymentRequest input,CancellationToken token)=>Run(()=>service.ConfirmPayment(id,input,User,token));
    [Authorize(Roles=RoleCodes.Provider+","+RoleCodes.Admin),HttpPost("stages/{id:guid}/updates")]public Task<ActionResult<InteriorWorkStageResponse>> Update(Guid id,AddInteriorWorkUpdateRequest input,CancellationToken token)=>Run(()=>service.UpdateStage(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider+","+RoleCodes.Admin),HttpPost("contracts/{id:guid}/changes")]public Task<ActionResult<InteriorChangeResponse>> Change(Guid id,CreateInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.RequestChange(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("changes/{id:guid}/decision")]public Task<ActionResult<InteriorChangeResponse>> Decide(Guid id,DecideInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.DecideChange(id,input,User,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> call){try{return Ok(await call());}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
}
