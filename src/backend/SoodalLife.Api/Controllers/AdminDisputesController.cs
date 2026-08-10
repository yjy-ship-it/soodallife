using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/disputes")]
public sealed class AdminDisputesController(AfterServiceDisputeService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<AdminDisputeListResponse>> Search([FromQuery]string? search,[FromQuery]string? status,[FromQuery]Guid? assignedAdminId,[FromQuery]bool? hasAfterService,[FromQuery]DateOnly? receivedFrom,[FromQuery]DateOnly? receivedTo,[FromQuery]bool? unresolved,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(()=>service.SearchDisputesAsync(search,status,assignedAdminId,hasAfterService,receivedFrom,receivedTo,unresolved,page,pageSize,token));
    [HttpGet("{id:guid}")] public async Task<ActionResult<AdminDisputeDetail>> Get(Guid id,CancellationToken token){var value=await service.GetDisputeAsync(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"DISPUTE_NOT_FOUND","분쟁을 찾을 수 없습니다.")):Ok(value);}
    [HttpPost] public Task<ActionResult<AdminDisputeDetail>> Create(CreateDisputeRequest request,CancellationToken token)=>Run(()=>service.CreateDisputeAsync(request,Actor(),token));
    [HttpPost("{id:guid}/status")] public Task<ActionResult<AdminDisputeDetail>> ChangeStatus(Guid id,ChangeDisputeStatusRequest request,CancellationToken token)=>Run(()=>service.ChangeDisputeStatusAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/assignment")] public Task<ActionResult<AdminDisputeDetail>> Assign(Guid id,AssignDisputeRequest request,CancellationToken token)=>Run(()=>service.AssignDisputeAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/evidence")] public Task<ActionResult<AdminDisputeDetail>> AddEvidence(Guid id,AddDisputeEvidenceRequest request,CancellationToken token)=>Run(()=>service.AddDisputeEvidenceAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/resolution")] public Task<ActionResult<AdminDisputeDetail>> Resolve(Guid id,ResolveDisputeRequest request,CancellationToken token)=>Run(()=>service.ResolveDisputeAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/fee-restore-links")] public Task<ActionResult<AdminDisputeDetail>> LinkFeeRestore(Guid id,LinkFeeRestoreRequest request,CancellationToken token)=>Run(()=>service.LinkFeeRestoreAsync(id,request,Actor(),token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(AfterServiceDisputeException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
