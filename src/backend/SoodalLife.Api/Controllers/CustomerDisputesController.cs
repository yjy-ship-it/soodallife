using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;
[ApiController,Route("api/v1/customers/me"),Authorize(Roles=RoleCodes.Customer)]
public sealed class CustomerDisputesController(CustomerDisputeService service):ControllerBase
{
    [HttpGet("disputes")]public Task<ActionResult<IReadOnlyList<CustomerDisputeResponse>>> List(CancellationToken t)=>Run(()=>service.List(User,t));
    [HttpGet("disputes/{id:guid}")]public Task<ActionResult<CustomerDisputeResponse>> Detail(Guid id,CancellationToken t)=>Run(()=>service.Detail(User,id,t));
    [HttpPost("transactions/{transactionId:guid}/disputes")]public Task<ActionResult<CustomerDisputeResponse>> Create(Guid transactionId,CreateCustomerDisputeInput input,CancellationToken t)=>Run(()=>service.Create(User,transactionId,input,t));
    [HttpPost("disputes/{id:guid}/evidence"),RequestSizeLimit(10*1024*1024+64*1024)]public Task<ActionResult<CustomerDisputeEvidenceResponse>> Upload(Guid id,[FromForm]string? description,[FromForm]IFormFile file,CancellationToken t)=>Run(()=>service.Upload(User,id,description,file,t));
    [HttpGet("dispute-files/{id:guid}")]public async Task<IActionResult> File(Guid id,CancellationToken t){try{var f=await service.Open(User,id,t);return File(f.Item1,f.Item2,f.Item3);}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
}
