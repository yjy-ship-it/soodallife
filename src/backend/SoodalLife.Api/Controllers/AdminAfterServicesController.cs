using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/after-services")]
public sealed class AdminAfterServicesController(AfterServiceDisputeService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<AdminAfterServiceListResponse>> Search([FromQuery]string? search,[FromQuery]string? status,[FromQuery]bool? withinWarranty,[FromQuery]bool? providerUnconfirmed,[FromQuery]bool? visitScheduled,[FromQuery]bool? inProgress,[FromQuery]bool? convertedToDispute,[FromQuery]DateOnly? receivedFrom,[FromQuery]DateOnly? receivedTo,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(()=>service.SearchAfterServicesAsync(search,status,withinWarranty,providerUnconfirmed,visitScheduled,inProgress,convertedToDispute,receivedFrom,receivedTo,page,pageSize,token));
    [HttpGet("{id:guid}")] public async Task<ActionResult<AdminAfterServiceDetail>> Get(Guid id,CancellationToken token){var value=await service.GetAfterServiceAsync(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"AFTER_SERVICE_NOT_FOUND","A/S를 찾을 수 없습니다.")):Ok(value);}
    [HttpPost] public Task<ActionResult<AdminAfterServiceDetail>> Create(CreateAfterServiceRequest request,CancellationToken token)=>Run(()=>service.CreateAfterServiceAsync(request,Actor(),token));
    [HttpPost("{id:guid}/status")] public Task<ActionResult<AdminAfterServiceDetail>> ChangeStatus(Guid id,ChangeAfterServiceStatusRequest request,CancellationToken token)=>Run(()=>service.ChangeAfterServiceStatusAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/actions")] public Task<ActionResult<AdminAfterServiceDetail>> AddAction(Guid id,AddAfterServiceActionRequest request,CancellationToken token)=>Run(()=>service.AddAfterServiceActionAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/evidence")] public Task<ActionResult<AdminAfterServiceDetail>> AddEvidence(Guid id,AddAfterServiceFileRequest request,CancellationToken token)=>Run(()=>service.AddAfterServiceFileAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/convert-to-dispute")] public Task<ActionResult<AdminDisputeDetail>> Convert(Guid id,ConvertAfterServiceToDisputeRequest request,CancellationToken token)=>Run(()=>service.ConvertToDisputeAsync(id,request,Actor(),token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(AfterServiceDisputeException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
