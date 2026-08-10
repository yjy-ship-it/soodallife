using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;

namespace SoodalLife.Api.Controllers;

[ApiController,Route("api/v1/subscriptions")]
public sealed class CareSubscriptionsController(CareSubscriptionService service):ControllerBase
{
    [Authorize(Roles=RoleCodes.Customer),HttpPost("requests")]
    public Task<ActionResult<SubscriptionRequestResponse>> CreateRequest(CreateSubscriptionRequest input,CancellationToken token)=>Run(()=>service.CreateRequest(input,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("requests/{id:guid}/applications")]
    public Task<ActionResult<SubscriptionApplicationResponse>> Apply(Guid id,SubmitSubscriptionApplicationRequest input,CancellationToken token)=>Run(()=>service.Apply(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpGet("requests/{id:guid}/applications")]
    public Task<ActionResult<IReadOnlyList<SubscriptionApplicationResponse>>> Applications(Guid id,CancellationToken token)=>Run(()=>service.GetApplications(id,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("requests/{id:guid}/selection")]
    public Task<ActionResult<SubscriptionContractResponse>> Select(Guid id,SelectSubscriptionProviderRequest input,CancellationToken token)=>Run(()=>service.SelectProvider(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Provider),HttpPost("visits/{id:guid}/schedule-changes")]
    public Task<ActionResult<SubscriptionScheduleChangeResponse>> ScheduleChange(Guid id,RequestScheduleChangeRequest input,CancellationToken token)=>Run(()=>service.RequestScheduleChange(id,input,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("visits/{id:guid}/completion")]
    public Task<ActionResult<SubscriptionVisitResponse>> Complete(Guid id,CompleteSubscriptionVisitRequest input,CancellationToken token)=>Run(()=>service.CompleteVisit(id,input,User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpGet("providers/me/contracts/{id:guid}")]
    public Task<ActionResult<ProviderSubscriptionContractDetail>> ProviderContract(Guid id,CancellationToken token)=>Run(()=>service.GetProviderContract(id,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("visits/{id:guid}/confirmation")]
    public Task<ActionResult<SubscriptionVisitResponse>> Confirm(Guid id,ConfirmSubscriptionVisitRequest input,CancellationToken token)=>Run(()=>service.ConfirmVisit(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("visits/{id:guid}/review")]
    public Task<ActionResult<Guid>> Review(Guid id,CreateSubscriptionReviewRequest input,CancellationToken token)=>Run(()=>service.CreateReview(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("visits/{id:guid}/after-service")]
    public Task<ActionResult<Guid>> AfterService(Guid id,CreateSubscriptionCaseRequest input,CancellationToken token)=>Run(()=>service.CreateAfterService(id,input,User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("visits/{id:guid}/dispute")]
    public Task<ActionResult<Guid>> Dispute(Guid id,CreateSubscriptionCaseRequest input,CancellationToken token)=>Run(()=>service.CreateDispute(id,input,User,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(SubscriptionBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/subscriptions")]
public sealed class AdminSubscriptionsController(CareSubscriptionService service):ControllerBase
{
    [HttpGet("eligible-services")]public Task<ActionResult<IReadOnlyList<SubscriptionServiceItem>>> Services(CancellationToken token)=>Run(()=>service.GetEligibleServices(token));
    [HttpGet("products")]public Task<ActionResult<IReadOnlyList<CareProductResponse>>> Products(CancellationToken token)=>Run(()=>service.GetProducts(token));
    [HttpPost("products")]public Task<ActionResult<CareProductResponse>> CreateProduct(CreateCareProductRequest input,CancellationToken token)=>Run(()=>service.CreateProduct(input,Actor(),token));
    [HttpPut("products/{id:guid}")]public Task<ActionResult<CareProductResponse>> UpdateProduct(Guid id,CreateCareProductRequest input,CancellationToken token)=>Run(()=>service.UpdateProduct(id,input,Actor(),token));
    [HttpGet("requests")]public Task<ActionResult<List<SubscriptionRequestResponse>>> Requests(CancellationToken token)=>Run(()=>service.AdminRequests(token));
    [HttpGet("applications")]public Task<ActionResult<List<AdminSubscriptionApplicationResponse>>> Applications(CancellationToken token)=>Run(()=>service.AdminApplications(token));
    [HttpGet("contracts")]public Task<ActionResult<List<SubscriptionContractResponse>>> Contracts(CancellationToken token)=>Run(()=>service.AdminContracts(token));
    [HttpGet("visits")]public Task<ActionResult<List<SubscriptionVisitResponse>>> Visits([FromQuery]DateOnly? from,[FromQuery]DateOnly? to,[FromQuery]string? status,CancellationToken token)=>Run(()=>service.AdminVisits(from,to,status,token));
    [HttpGet("schedule-changes")]public Task<ActionResult<List<SubscriptionScheduleChangeResponse>>> ScheduleChanges(CancellationToken token)=>Run(()=>service.AdminScheduleChanges(token));
    [HttpGet("contracts/{id:guid}/events")]public Task<ActionResult<IReadOnlyList<SubscriptionEventResponse>>> Events(Guid id,CancellationToken token)=>Run(()=>service.Events(id,token));
    [HttpPost("contracts/{id:guid}/pause")]public Task<ActionResult<SubscriptionContractResponse>> Pause(Guid id,ChangeContractStateRequest input,CancellationToken token)=>Run(()=>service.ChangeState(id,"PAUSE",input,Actor(),token));
    [HttpPost("contracts/{id:guid}/resume")]public Task<ActionResult<SubscriptionContractResponse>> Resume(Guid id,ChangeContractStateRequest input,CancellationToken token)=>Run(()=>service.ChangeState(id,"RESUME",input,Actor(),token));
    [HttpPost("contracts/{id:guid}/terminate")]public Task<ActionResult<SubscriptionContractResponse>> Terminate(Guid id,ChangeContractStateRequest input,CancellationToken token)=>Run(()=>service.ChangeState(id,"TERMINATE",input,Actor(),token));
    [HttpPost("contracts/{id:guid}/provider")]public Task<ActionResult<SubscriptionContractResponse>> Replace(Guid id,ReplaceSubscriptionProviderRequest input,CancellationToken token)=>Run(()=>service.ReplaceProvider(id,input,Actor(),token));
    [HttpPost("schedule-changes/{id:guid}/decision")]public Task<ActionResult<SubscriptionScheduleChangeResponse>> Decide(Guid id,DecideScheduleChangeRequest input,CancellationToken token)=>Run(()=>service.DecideScheduleChange(id,input,Actor(),token));
    [HttpPost("visits/{id:guid}/skip")]public Task<ActionResult<SubscriptionVisitResponse>> Skip(Guid id,[FromBody]Dictionary<string,string> input,CancellationToken token)=>Run(()=>service.SkipVisit(id,input.GetValueOrDefault("idempotencyKey")??string.Empty,Actor(),token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(SubscriptionBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
