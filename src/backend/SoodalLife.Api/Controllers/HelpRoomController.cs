using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.HelpRoom;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController,Route("api/v1/help-room"),Authorize]
public sealed class HelpRoomController(HelpRoomService service):ControllerBase
{
    [HttpGet]public Task<ActionResult<IReadOnlyList<HelpPostListItem>>> List([FromQuery]string? category,[FromQuery]string? mode,CancellationToken token)=>Run(()=>service.List(User,category,mode,token));
    [HttpGet("{id:guid}")]public Task<ActionResult<HelpPostDetail>> Detail(Guid id,CancellationToken token)=>Run(()=>service.Detail(User,id,token));
    [HttpPost,Authorize(Roles=RoleCodes.Customer)]public Task<ActionResult<HelpPostDetail>> Create(CreateHelpPostInput input,CancellationToken token)=>Run(()=>service.Create(User,input,token));
    [HttpPost("{id:guid}/files"),Authorize(Roles=RoleCodes.Customer),RequestSizeLimit(5*1024*1024+64*1024)]public Task<ActionResult<HelpFileResponse>> Upload(Guid id,[FromForm]IFormFile file,CancellationToken token)=>Run(()=>service.Upload(User,id,file,token));
    [HttpGet("{id:guid}/files/{fileId:guid}")]public async Task<IActionResult> File(Guid id,Guid fileId,CancellationToken token){try{var result=await service.OpenFile(User,id,fileId,token);return File(result.Stream,result.ContentType,result.FileName);}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
    [HttpPost("{id:guid}/files/{fileId:guid}/report")]public async Task<IActionResult> ReportFile(Guid id,Guid fileId,HelpFileReportInput input,CancellationToken token){try{await service.ReportFile(User,id,fileId,input.Reason,token);return NoContent();}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
    [HttpPost("{id:guid}/follow-ups"),Authorize(Roles=RoleCodes.Customer)]public Task<ActionResult<HelpPostDetail>> FollowUp(Guid id,AddHelpEntryInput input,CancellationToken token)=>Run(()=>service.FollowUp(User,id,input,token));
    [HttpPost("{id:guid}/advice"),Authorize(Roles=RoleCodes.Provider)]public Task<ActionResult<HelpPostDetail>> Advice(Guid id,AddProviderAdviceInput input,CancellationToken token)=>Run(()=>service.Advise(User,id,input,token));
    [HttpPost("{id:guid}/resolve"),Authorize(Roles=RoleCodes.Customer)]public Task<ActionResult<HelpPostDetail>> Resolve(Guid id,ResolveHelpPostInput input,CancellationToken token)=>Run(()=>service.Resolve(User,id,input,token));
    [HttpPost("{id:guid}/convert-to-request"),Authorize(Roles=RoleCodes.Customer)]public Task<ActionResult<ConvertHelpPostResponse>> Convert(Guid id,CancellationToken token)=>Run(()=>service.Convert(User,id,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message,fieldErrors=e.FieldErrors});}}
}
[ApiController,Route("api/v1/admin/help-room"),Authorize(Roles=RoleCodes.Admin)]
public sealed class AdminHelpRoomController(HelpRoomService service):ControllerBase
{[HttpPut("{id:guid}/visibility")]public async Task<IActionResult> Visibility(Guid id,HelpVisibilityInput input,CancellationToken token){try{await service.Hide(User,id,input.Hidden,token);return NoContent();}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
 [HttpGet("files/review")]public async Task<IActionResult> ReviewQueue(CancellationToken token){try{return Ok(await service.ReviewQueue(User,token));}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
 [HttpGet("{id:guid}/files/{fileId:guid}/review")]public async Task<IActionResult> ReviewFile(Guid id,Guid fileId,CancellationToken token){try{var value=await service.OpenReviewFile(User,id,fileId,token);return File(value.Stream,value.ContentType,value.FileName);}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}
 [HttpPut("{id:guid}/files/{fileId:guid}/review")]public async Task<IActionResult> Review(Guid id,Guid fileId,HelpFileReviewInput input,CancellationToken token){try{await service.ReviewFile(User,id,fileId,input.Approved,token);return NoContent();}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}}
public sealed record HelpVisibilityInput(bool Hidden);
public sealed record HelpFileReportInput(string? Reason);
public sealed record HelpFileReviewInput(bool Approved);
