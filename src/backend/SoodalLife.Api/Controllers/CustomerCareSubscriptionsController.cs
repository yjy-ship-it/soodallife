using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/public/care")]
public sealed class PublicCareController(CustomerCareSubscriptionService service) : ControllerBase
{
    [HttpGet("services")] public Task<IReadOnlyList<SubscriptionServiceItem>> Services(CancellationToken token) => service.EligibleServices(token);
    [HttpGet("products")] public Task<IReadOnlyList<CustomerCareProductResponse>> Products([FromQuery] Guid? serviceId, CancellationToken token) => service.Products(serviceId, token);
}

[ApiController, Authorize(Roles = RoleCodes.Customer), Route("api/v1/customers/me/care")]
public sealed class CustomerCareSubscriptionsController(CustomerCareSubscriptionService service) : ControllerBase
{
    [HttpGet("home")] public Task<ActionResult<CustomerCareHomeResponse>> Home(CancellationToken token) => Run(() => service.Home(User, token));
    [HttpGet("requests")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionRequestResponse>>> Requests(CancellationToken token) => Run(() => service.Requests(User, token));
    [HttpGet("requests/{id:guid}")] public Task<ActionResult<CustomerSubscriptionRequestResponse>> RequestDetail(Guid id, CancellationToken token) => Run(() => service.Request(id, User, token));
    [HttpPost("requests/{id:guid}/cancel")] public Task<ActionResult<CustomerSubscriptionRequestResponse>> CancelRequest(Guid id, CancelSubscriptionRequestRequest input, CancellationToken token) => Run(() => service.CancelRequest(id, input, User, token));
    [HttpGet("requests/{id:guid}/applications")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionApplicationResponse>>> Applications(Guid id, CancellationToken token) => Run(() => service.Applications(id, User, token));
    [HttpGet("contracts")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionContractResponse>>> Contracts([FromQuery] string? status, CancellationToken token) => Run(() => service.Contracts(User, status, token));
    [HttpGet("contracts/{id:guid}")] public Task<ActionResult<CustomerSubscriptionContractResponse>> Contract(Guid id, CancellationToken token) => Run(() => service.Contract(id, User, token));
    [HttpPost("contracts/{id:guid}/pause")] public Task<ActionResult<CustomerSubscriptionContractResponse>> Pause(Guid id, CustomerContractActionRequest input, CancellationToken token) => Run(() => service.ChangeContract(id, "PAUSE", input, User, token));
    [HttpPost("contracts/{id:guid}/resume")] public Task<ActionResult<CustomerSubscriptionContractResponse>> Resume(Guid id, CustomerContractActionRequest input, CancellationToken token) => Run(() => service.ChangeContract(id, "RESUME", input, User, token));
    [HttpPost("contracts/{id:guid}/terminate")] public Task<ActionResult<CustomerSubscriptionContractResponse>> Terminate(Guid id, CustomerContractActionRequest input, CancellationToken token) => Run(() => service.ChangeContract(id, "TERMINATE", input, User, token));
    [HttpGet("visits")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionVisitListItem>>> Visits([FromQuery] Guid? contractId, [FromQuery] string? status, CancellationToken token) => Run(() => service.Visits(User, contractId, status, token));
    [HttpGet("visits/{id:guid}")] public Task<ActionResult<CustomerSubscriptionVisitDetailResponse>> Visit(Guid id, CancellationToken token) => Run(() => service.Visit(id, User, token));
    [HttpPost("visits/{id:guid}/skip")] public Task<ActionResult<CustomerSubscriptionVisitListItem>> Skip(Guid id, CustomerSkipVisitRequest input, CancellationToken token) => Run(() => service.Skip(id, input, User, token));
    [HttpPost("schedule-changes/{id:guid}/cancel")] public Task<ActionResult<SubscriptionScheduleChangeResponse>> Cancel(Guid id, CustomerCancelScheduleChangeRequest input, CancellationToken token) => Run(() => service.CancelScheduleChange(id, input, User, token));
    [HttpGet("payment-methods")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionPaymentMethodResponse>>> PaymentMethods(CancellationToken token) => Run(() => service.PaymentMethods(User, token));
    [HttpGet("billing-registration")] public Task<ActionResult<SubscriptionBillingRegistrationResponse>> BillingRegistration(CancellationToken token)=>Run(()=>service.BillingRegistration(User,token));
    [HttpPost("billing-authorizations")] public Task<ActionResult<CustomerSubscriptionPaymentMethodResponse>> CompleteBillingAuthorization(CompleteBillingAuthorizationRequest input,CancellationToken token)=>Run(()=>service.CompleteBillingAuthorization(input,User,token));
    [HttpGet("payments")] public Task<ActionResult<IReadOnlyList<CustomerSubscriptionPaymentHistoryResponse>>> Payments(CancellationToken token) => Run(() => service.Payments(User, token));
    [HttpPost("contracts/{id:guid}/recurring-payment-consent")] public Task<ActionResult<CustomerSubscriptionContractResponse>> ConsentRecurringPayment(Guid id, CustomerRecurringPaymentConsentRequest input, CancellationToken token) => Run(() => service.ConsentRecurringPayment(id, input, User, token));
    [HttpGet("visits/{visitId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> File(Guid visitId, Guid fileId, CancellationToken token)
    {
        try { var result = await service.OpenVisitFile(visitId, fileId, User, token); return File(result.Stream, result.ContentType, result.FileName); }
        catch (SubscriptionBusinessException error) { return StatusCode(error.StatusCode, ApiErrorResponse.Create(HttpContext, error.BusinessCode, error.Message)); }
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (SubscriptionBusinessException error) { return StatusCode(error.StatusCode, ApiErrorResponse.Create(HttpContext, error.BusinessCode, error.Message)); }
    }
}
