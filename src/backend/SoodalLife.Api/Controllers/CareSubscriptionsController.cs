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

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/subscription-accounting")]
public sealed class AdminSubscriptionAccountingController(SubscriptionBillingService service):ControllerBase
{
    [HttpGet("dashboard")] public Task<ActionResult<SubscriptionAccountingDashboard>> Dashboard(CancellationToken token)=>Run(()=>service.Dashboard(token));
    [HttpGet("payment-methods")] public Task<ActionResult<List<SubscriptionPaymentMethodResponse>>> PaymentMethods(CancellationToken token)=>Run(()=>service.PaymentMethods(token));
    [HttpPost("payment-methods")] public Task<ActionResult<SubscriptionPaymentMethodResponse>> RegisterPaymentMethod(RegisterSubscriptionPaymentMethodRequest input,CancellationToken token)=>Run(()=>service.RegisterPaymentMethod(input,Actor(),token));
    [HttpGet("payments")] public Task<ActionResult<List<SubscriptionPaymentResponse>>> Payments(CancellationToken token)=>Run(()=>service.Payments(token));
    [HttpPost("payments")] public Task<ActionResult<SubscriptionPaymentResponse>> CreatePayment(CreateSubscriptionPaymentRequest input,CancellationToken token)=>Run(()=>service.CreatePayment(input,Actor(),token));
    [HttpPost("payments/{id:guid}/development-confirmation")] public Task<ActionResult<SubscriptionPaymentResponse>> ConfirmPayment(Guid id,DevelopmentPaymentConfirmationRequest input,CancellationToken token)=>Run(()=>service.ConfirmPayment(id,input,Actor(),token));
    [HttpPost("payments/{id:guid}/cancel")] public Task<ActionResult<SubscriptionPaymentResponse>> CancelPayment(Guid id,PaymentStateRequest input,CancellationToken token)=>Run(()=>service.CancelPayment(id,input,Actor(),token));
    [HttpGet("payment-ledger")] public Task<ActionResult<List<SubscriptionPaymentLedgerResponse>>> PaymentLedger(CancellationToken token)=>Run(()=>service.PaymentLedger(token));
    [HttpGet("settlement-items")] public Task<ActionResult<List<SubscriptionSettlementItemResponse>>> SettlementItems([FromQuery]int? year,[FromQuery]int? month,CancellationToken token)=>Run(()=>service.SettlementItems(year,month,token));
    [HttpPost("settlement-items/prepare")] public Task<ActionResult<List<SubscriptionSettlementItemResponse>>> PrepareSettlementItems(PrepareSettlementItemsRequest input,CancellationToken token)=>Run(()=>service.PrepareSettlementItems(input,Actor(),token));
    [HttpPost("settlement-items/{id:guid}/hold")] public Task<ActionResult<SubscriptionSettlementItemResponse>> Hold(Guid id,SettlementDecisionRequest input,CancellationToken token)=>Run(()=>service.HoldSettlementItem(id,input,false,Actor(),token));
    [HttpPost("settlement-items/{id:guid}/release")] public Task<ActionResult<SubscriptionSettlementItemResponse>> Release(Guid id,SettlementDecisionRequest input,CancellationToken token)=>Run(()=>service.HoldSettlementItem(id,input,true,Actor(),token));
    [HttpGet("monthly-settlements")] public Task<ActionResult<List<MonthlySettlementResponse>>> MonthlySettlements(CancellationToken token)=>Run(()=>service.MonthlySettlements(token));
    [HttpPost("monthly-settlements")] public Task<ActionResult<MonthlySettlementResponse>> CreateMonthlySettlement(CreateMonthlySettlementRequest input,CancellationToken token)=>Run(()=>service.CreateMonthlySettlement(input,Actor(),token));
    [HttpPost("monthly-settlements/{id:guid}/approve")] public Task<ActionResult<MonthlySettlementResponse>> ApproveMonthlySettlement(Guid id,SettlementDecisionRequest input,CancellationToken token)=>Run(()=>service.ApproveMonthlySettlement(id,input,Actor(),token));
    [HttpGet("payouts")] public Task<ActionResult<List<SubscriptionPayoutResponse>>> Payouts(CancellationToken token)=>Run(()=>service.Payouts(token));
    [HttpPost("payouts")] public Task<ActionResult<SubscriptionPayoutResponse>> CreatePayout(CreateSubscriptionPayoutRequest input,CancellationToken token)=>Run(()=>service.CreatePayout(input,Actor(),token));
    [HttpPost("payouts/{id:guid}/approve")] public Task<ActionResult<SubscriptionPayoutResponse>> ApprovePayout(Guid id,PayoutDecisionRequest input,CancellationToken token)=>Run(()=>service.ApprovePayout(id,input,Actor(),token));
    [HttpPost("payouts/{id:guid}/development-completion")] public Task<ActionResult<SubscriptionPayoutResponse>> CompletePayout(Guid id,PayoutDecisionRequest input,CancellationToken token)=>Run(()=>service.CompletePayout(id,input,Actor(),token));
    [HttpGet("refund-adjustments")] public Task<ActionResult<List<SubscriptionRefundAdjustmentResponse>>> Refunds(CancellationToken token)=>Run(()=>service.Refunds(token));
    [HttpPost("refund-adjustments")] public Task<ActionResult<SubscriptionRefundAdjustmentResponse>> CreateRefund(CreateRefundAdjustmentRequest input,CancellationToken token)=>Run(()=>service.CreateRefund(input,Actor(),token));
    [HttpPost("refund-adjustments/{id:guid}/approve")] public Task<ActionResult<SubscriptionRefundAdjustmentResponse>> ApproveRefund(Guid id,RefundAdjustmentDecisionRequest input,CancellationToken token)=>Run(()=>service.ApproveRefund(id,input,Actor(),token));
    [HttpPost("refund-adjustments/{id:guid}/development-completion")] public Task<ActionResult<SubscriptionRefundAdjustmentResponse>> CompleteRefund(Guid id,RefundAdjustmentDecisionRequest input,CancellationToken token)=>Run(()=>service.CompleteRefund(id,input,Actor(),token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(SubscriptionBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
