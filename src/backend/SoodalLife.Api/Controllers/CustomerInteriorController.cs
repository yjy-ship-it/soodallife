using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Interior;

namespace SoodalLife.Api.Controllers;

[ApiController,Route("api/v1/public/interior")]
public sealed class PublicInteriorController(CustomerInteriorService service):ControllerBase
{
    [HttpGet("services")]public Task<IReadOnlyList<InteriorServiceResponse>> Services(CancellationToken token)=>service.Services(token);
}

[ApiController,Authorize(Roles=RoleCodes.Customer),Route("api/v1/customers/me/interior")]
public sealed class CustomerInteriorController(CustomerInteriorService service):ControllerBase
{
    [HttpGet("home")]public Task<ActionResult<CustomerInteriorHomeResponse>> Home(CancellationToken token)=>Run(()=>service.Home(User,token));
    [HttpGet("request-candidates")]public Task<ActionResult<IReadOnlyList<CustomerInteriorRequestCandidate>>> Candidates(CancellationToken token)=>Run(()=>service.RequestCandidates(User,token));
    [HttpPost("projects")]public Task<ActionResult<CustomerInteriorProjectDetail>> Create(CustomerCreateInteriorProjectRequest input,CancellationToken token)=>Run(()=>service.Create(input,User,token));
    [HttpGet("projects")]public Task<ActionResult<IReadOnlyList<CustomerInteriorProjectListItem>>> Projects(CancellationToken token)=>Run(()=>service.List(User,token));
    [HttpGet("projects/{id:guid}")]public Task<ActionResult<CustomerInteriorProjectDetail>> Project(Guid id,CancellationToken token)=>Run(()=>service.Detail(id,User,token));
    [HttpPost("projects/{id:guid}/site-visits/{visitId:guid}/selection")]public Task<ActionResult<CustomerInteriorProjectDetail>> SelectVisit(Guid id,Guid visitId,CustomerSelectSiteVisitRequest input,CancellationToken token)=>Run(()=>service.SelectVisit(id,visitId,input,User,token));
    [HttpPost("projects/{id:guid}/contracts/{contractId:guid}/agreement")]public Task<ActionResult<CustomerInteriorProjectDetail>> Agree(Guid id,Guid contractId,CustomerAgreeInteriorContractRequest input,CancellationToken token)=>Run(()=>service.Agree(id,contractId,input,User,token));
    [HttpPost("projects/{id:guid}/payment-plans/{planId:guid}/confirmations")]public Task<ActionResult<Guid>> ConfirmPayment(Guid id,Guid planId,CustomerConfirmInteriorPaymentRequest input,CancellationToken token)=>Run(()=>service.ConfirmPayment(id,planId,input,User,token));
    [HttpPost("projects/{id:guid}/changes/{changeId:guid}/decision")]public Task<ActionResult<CustomerInteriorProjectDetail>> DecideChange(Guid id,Guid changeId,CustomerDecideInteriorChangeRequest input,CancellationToken token)=>Run(()=>service.DecideChange(id,changeId,input,User,token));
    [HttpPost("projects/{id:guid}/inspections/{inspectionId:guid}/acknowledgement")]public Task<ActionResult<InteriorInspectionAcknowledgementResponse>> AcknowledgeInspection(Guid id,Guid inspectionId,AcknowledgeInteriorInspectionRequest input,CancellationToken token)=>Run(()=>service.AcknowledgeInspection(id,inspectionId,input,User,token));
    [HttpPost("projects/{id:guid}/completion-acknowledgement")]public Task<ActionResult<CustomerInteriorProjectDetail>> AcknowledgeCompletion(Guid id,AcknowledgeInteriorProjectCompletionRequest input,CancellationToken token)=>Run(()=>service.AcknowledgeCompletion(id,input,User,token));
    [HttpPost("projects/{id:guid}/defects")]public Task<ActionResult<CustomerInteriorProjectDetail>> Defect(Guid id,CustomerCreateInteriorDefectRequest input,CancellationToken token)=>Run(()=>service.CreateDefect(id,input,User,token));
    [HttpPost("projects/{id:guid}/disputes")]public Task<ActionResult<CustomerInteriorProjectDetail>> Dispute(Guid id,CustomerCreateInteriorDisputeRequest input,CancellationToken token)=>Run(()=>service.CreateDispute(id,input,User,token));
    [HttpPost("projects/{id:guid}/evidence-files"),RequestSizeLimit(10*1024*1024+64*1024)]public Task<ActionResult<CustomerInteriorUploadResponse>> Upload(Guid id,[FromForm]IFormFile file,CancellationToken token)=>Run(()=>service.UploadEvidence(id,file,User,token));
    [HttpGet("projects/{id:guid}/files/{fileId:guid}")]public async Task<IActionResult> File(Guid id,Guid fileId,CancellationToken token){try{var value=await service.OpenFile(id,fileId,User,token);return File(value.Stream,value.ContentType,value.FileName);}catch(InteriorBusinessException error){return StatusCode(error.StatusCode,ApiErrorResponse.Create(HttpContext,error.BusinessCode,error.Message));}}
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(InteriorBusinessException error){return StatusCode(error.StatusCode,ApiErrorResponse.Create(HttpContext,error.BusinessCode,error.Message));}}
}
