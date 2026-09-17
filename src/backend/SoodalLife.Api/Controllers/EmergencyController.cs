using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Emergency;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.Matching;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles=RoleCodes.Provider)]
[Route("api/v1/providers/me/emergency-availability")]
public sealed class ProviderEmergencyAvailabilityController(ProviderEmergencyAvailabilityService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<EmergencyAvailabilityResponse>> Get(CancellationToken token)=>Run(()=>service.GetAsync(User,token));
    [HttpPut] public Task<ActionResult<EmergencyAvailabilityResponse>> Update(UpdateEmergencyAvailabilityInput input,CancellationToken token)=>Run(()=>service.UpdateAsync(User,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Provider)]
[Route("api/v1/providers/me/emergency-requests")]
public sealed class ProviderEmergencyRequestsController(EmergencyWorkflowService service,RequestMatchingService matching):ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<EmergencyProviderRequestItem>>> List(CancellationToken token)=>Run(()=>service.ProviderRequests(User,token));
    [HttpGet("assignments")] public Task<ActionResult<IReadOnlyList<EmergencyProviderAssignmentItem>>> Assignments(CancellationToken token)=>Run(()=>service.ProviderAssignments(User,token));
    [HttpGet("{requestId:guid}")] public async Task<ActionResult<ProviderMatchedRequestDetail>> Detail(Guid requestId,CancellationToken token)
    {
        var detail=await matching.GetInboxDetailAsync(User,requestId,token);
        return detail is null||!detail.IsUrgent?NotFound():Ok(detail);
    }
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Provider)]
[Route("api/v1/emergency-requests/{requestId:guid}/responses")]
public sealed class ProviderEmergencyResponsesController(EmergencyWorkflowService service):ControllerBase
{
    [HttpPost] public Task<ActionResult<EmergencyProviderResponseResult>> Respond(Guid requestId,SubmitEmergencyResponseInput input,CancellationToken token)=>Run(()=>service.Respond(User,requestId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Customer)]
[Route("api/v1/emergency-requests")]
public sealed class CustomerEmergencyController(CustomerServiceRequestService requests,EmergencyWorkflowService emergency):ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ServiceRequestCreatedResponse>> Create(CreateServiceRequestInput input,CancellationToken token)
    {
        try{var emergencyInput=input with{IsUrgent=true};var result=await requests.CreateAsync(User,emergencyInput,token);return Created($"/api/v1/requests/{result.Id}",result);}catch(RequestValidationException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,e.FieldErrors,HttpContext.TraceIdentifier));}
    }
    [HttpPost("{requestId:guid}/publish")]
    public async Task<ActionResult<PublishServiceRequestResponse>> Publish(Guid requestId,CancellationToken token){try{return Ok(await requests.PublishAsync(User,requestId,token));}catch(RequestValidationException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,e.FieldErrors,HttpContext.TraceIdentifier));}}
    [HttpGet("{requestId:guid}/responses")] public Task<ActionResult<IReadOnlyList<EmergencyCustomerResponseItem>>> Responses(Guid requestId,CancellationToken token)=>Run(()=>emergency.CustomerResponses(User,requestId,token));
    [HttpPost("{requestId:guid}/select")] public Task<ActionResult<EmergencySelectionResult>> Select(Guid requestId,SelectEmergencyProviderInput input,CancellationToken token)=>Run(()=>emergency.Select(User,requestId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider)]
[Route("api/v1/emergency-transactions/{transactionId:guid}/progress")]
public sealed class EmergencyProgressController(EmergencyWorkflowService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<EmergencyProgressResponse>> Get(Guid transactionId,CancellationToken token)=>Run(()=>service.GetProgress(User,transactionId,token));
    [HttpPost][Authorize(Roles=RoleCodes.Provider)] public Task<ActionResult<EmergencyProgressResponse>> Add(Guid transactionId,EmergencyProgressInput input,CancellationToken token)=>Run(()=>service.AddProgress(User,transactionId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}

[ApiController]
[Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider)]
[Route("api/v1/emergency-transactions/{transactionId:guid}")]
public sealed class EmergencyTransactionActionsController(EmergencyWorkflowService service):ControllerBase
{
    [HttpPost("payment-report")][Authorize(Roles=RoleCodes.Customer)] public Task<ActionResult<EmergencyProgressResponse>> ReportPayment(Guid transactionId,EmergencyPaymentReportInput input,CancellationToken token)=>Run(()=>service.ReportPayment(User,transactionId,input,token));
    [HttpPost("payment-decision")][Authorize(Roles=RoleCodes.Provider)] public Task<ActionResult<EmergencyProgressResponse>> DecidePayment(Guid transactionId,EmergencyPaymentDecisionInput input,CancellationToken token)=>Run(()=>service.DecidePayment(User,transactionId,input,token));
    [HttpPost("no-show")] public Task<ActionResult<EmergencyProgressResponse>> ReportNoShow(Guid transactionId,EmergencyNoShowInput input,CancellationToken token)=>Run(()=>service.ReportNoShow(User,transactionId,input,token));
    [HttpPost("no-show/dispute")] public Task<ActionResult<EmergencyProgressResponse>> DisputeNoShow(Guid transactionId,EmergencyNoShowDisputeInput input,CancellationToken token)=>Run(()=>service.DisputeNoShow(User,transactionId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work){try{return Ok(await work());}catch(EmergencyWorkflowException e){return StatusCode(e.StatusCode,new ApiErrorResponse(e.BusinessCode,e.Message,null,HttpContext.TraceIdentifier));}}
}
