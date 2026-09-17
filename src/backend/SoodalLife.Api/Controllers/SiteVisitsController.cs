using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.SiteVisits;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles=RoleCodes.Provider)]
[Route("api/v1/providers/me/requests/{requestId:guid}/site-visits")]
public sealed class ProviderRequestSiteVisitsController(SiteVisitService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<SiteVisitProposalResponse>>> List(Guid requestId,CancellationToken token)=>Run(()=>service.ProviderList(User,requestId,token));
    [HttpPost] public Task<ActionResult<SiteVisitProposalResponse>> Save(Guid requestId,SaveSiteVisitProposalInput input,CancellationToken token)=>Run(()=>service.Save(User,requestId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(SiteVisitWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Customer)]
[Route("api/v1/customer/requests/{requestId:guid}/site-visits")]
public sealed class CustomerRequestSiteVisitsController(SiteVisitService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<SiteVisitProposalResponse>>> List(Guid requestId,CancellationToken token)=>Run(()=>service.CustomerList(User,requestId,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(SiteVisitWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider)]
[Route("api/v1/site-visits/{proposalId:guid}")]
public sealed class SiteVisitActionsController(SiteVisitService service):ControllerBase
{
    [HttpPost("accept")][Authorize(Roles=RoleCodes.Customer)] public Task<ActionResult<SiteVisitProposalResponse>> Accept(Guid proposalId,AcceptSiteVisitInput input,CancellationToken token)=>Run(()=>service.Accept(User,proposalId,input,token));
    [HttpPost("reject")][Authorize(Roles=RoleCodes.Customer)] public Task<ActionResult<SiteVisitProposalResponse>> Reject(Guid proposalId,SiteVisitActionInput input,CancellationToken token)=>Run(()=>service.Reject(User,proposalId,input,token));
    [HttpPost("cancel")] public Task<ActionResult<SiteVisitProposalResponse>> Cancel(Guid proposalId,SiteVisitActionInput input,CancellationToken token)=>Run(()=>service.Cancel(User,proposalId,input,token));
    [HttpPost("progress")][Authorize(Roles=RoleCodes.Provider)] public Task<ActionResult<SiteVisitProposalResponse>> Progress(Guid proposalId,SiteVisitActionInput input,CancellationToken token)=>Run(()=>service.Progress(User,proposalId,input,token));
    [HttpPost("payment-report")][Authorize(Roles=RoleCodes.Customer)] public Task<ActionResult<SiteVisitProposalResponse>> ReportPayment(Guid proposalId,SiteVisitPaymentReportInput input,CancellationToken token)=>Run(()=>service.ReportPayment(User,proposalId,input,token));
    [HttpPost("payment-decision")][Authorize(Roles=RoleCodes.Provider)] public Task<ActionResult<SiteVisitProposalResponse>> DecidePayment(Guid proposalId,SiteVisitPaymentDecisionInput input,CancellationToken token)=>Run(()=>service.DecidePayment(User,proposalId,input,token));
    [HttpPost("no-show")] public Task<ActionResult<SiteVisitProposalResponse>> NoShow(Guid proposalId,SiteVisitNoShowInput input,CancellationToken token)=>Run(()=>service.NoShow(User,proposalId,input,token));
    [HttpPost("no-show/dispute")] public Task<ActionResult<SiteVisitProposalResponse>> Dispute(Guid proposalId,SiteVisitDisputeInput input,CancellationToken token)=>Run(()=>service.Dispute(User,proposalId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(SiteVisitWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}
