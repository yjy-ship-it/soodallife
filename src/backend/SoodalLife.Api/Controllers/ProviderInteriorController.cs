using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Interior;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Provider),Route("api/v1/providers/me/interior")]
public sealed class ProviderInteriorController(ProviderInteriorService service):ControllerBase
{
    [HttpGet("home")] public Task<ActionResult<ProviderInteriorDashboard>> Home(CancellationToken t)=>Run(()=>service.Dashboard(User,t));
    [HttpGet("projects")] public Task<ActionResult<IReadOnlyList<ProviderInteriorProjectItem>>> Projects([FromQuery]string? role,[FromQuery]string? status,CancellationToken t)=>Run(()=>service.Projects(User,role,status,t));
    [HttpGet("projects/{id:guid}")] public Task<ActionResult<ProviderInteriorDetail>> Detail(Guid id,CancellationToken t)=>Run(()=>service.Detail(id,User,t));
    [HttpPost("site-visits/{id:guid}/start")] public Task<ActionResult<ProviderInteriorDetail>> StartVisit(Guid id,StartProviderInteriorVisitRequest input,CancellationToken t)=>Run(()=>service.StartVisit(id,input,User,t));
    [HttpPost("site-visits/{id:guid}/completion")] public Task<ActionResult<ProviderInteriorDetail>> CompleteVisit(Guid id,CompleteInteriorSiteVisitRequest input,CancellationToken t)=>Run(()=>service.CompleteVisit(id,input,User,t));
    [HttpPost("projects/{id:guid}/designs")] public Task<ActionResult<ProviderInteriorDetail>> Design(Guid id,SubmitProviderInteriorDesignRequest input,CancellationToken t)=>Run(()=>service.SubmitDesign(id,input,User,t));
    [HttpPost("contracts/{id:guid}/agreement")] public Task<ActionResult<ProviderInteriorDetail>> Agree(Guid id,AgreeInteriorContractRequest input,CancellationToken t)=>Run(()=>service.Agree(id,input,User,t));
    [HttpPost("payment-plans/{id:guid}/confirmations")] public Task<ActionResult<Guid>> Payment(Guid id,ConfirmInteriorPaymentRequest input,CancellationToken t)=>Run(()=>service.ConfirmPayment(id,input,User,t));
    [HttpPost("stages/{id:guid}/updates")] public Task<ActionResult<ProviderInteriorDetail>> Stage(Guid id,AddInteriorWorkUpdateRequest input,CancellationToken t)=>Run(()=>service.UpdateStage(id,input,User,t));
    [HttpPost("stages/{id:guid}/inspections")] public Task<ActionResult<ProviderInteriorDetail>> Inspect(Guid id,SubmitProviderInteriorInspectionRequest input,CancellationToken t)=>Run(()=>service.Inspect(id,input,User,t));
    [HttpPost("contracts/{id:guid}/changes")] public Task<ActionResult<ProviderInteriorDetail>> Change(Guid id,ProviderInteriorChangeRequest input,CancellationToken t)=>Run(()=>service.RequestChange(id,input,User,t));
    [HttpPost("projects/{id:guid}/completion")] public Task<ActionResult<ProviderInteriorDetail>> Complete(Guid id,SubmitInteriorProjectCompletionRequest input,CancellationToken t)=>Run(()=>service.SubmitCompletion(id,input,User,t));
    [HttpPost("projects/{id:guid}/files"),RequestSizeLimit(10*1024*1024+64*1024)] public Task<ActionResult<ProviderInteriorUpload>> Upload(Guid id,[FromForm]IFormFile file,CancellationToken t)=>Run(()=>service.Upload(id,User,file,t));
    [HttpGet("projects/{projectId:guid}/files/{fileId:guid}")] public async Task<IActionResult> File(Guid projectId,Guid fileId,CancellationToken t){try{var x=await service.OpenFile(projectId,fileId,User,t);return File(x.Stream,x.ContentType,x.FileName);}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> a){try{return Ok(await a());}catch(InteriorBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
