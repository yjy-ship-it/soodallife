using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class WorkController(WorkService workService, TransactionAppointmentService appointmentService) : ControllerBase
{
    [Authorize(Roles = RoleCodes.Provider)]
    [HttpGet("providers/me/transactions")]
    public Task<ActionResult<IReadOnlyList<WorkTransactionListItem>>> ProviderList(CancellationToken token) =>
        Execute(() => workService.GetProviderTransactionsAsync(User, token));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpGet("providers/me/transactions/{transactionId:guid}")]
    public Task<ActionResult<WorkTransactionDetail>> ProviderDetail(Guid transactionId, CancellationToken token) =>
        Execute(() => workService.GetProviderDetailAsync(User, transactionId, token));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpGet("customers/me/transactions")]
    public Task<ActionResult<IReadOnlyList<WorkTransactionListItem>>> CustomerList(CancellationToken token) =>
        Execute(() => workService.GetCustomerTransactionsAsync(User, token));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpGet("customers/me/transactions/{transactionId:guid}")]
    public Task<ActionResult<WorkTransactionDetail>> CustomerDetail(Guid transactionId, CancellationToken token) =>
        Execute(() => workService.GetCustomerDetailAsync(User, transactionId, token));

    [Authorize(Roles = $"{RoleCodes.Customer},{RoleCodes.Provider}")]
    [HttpGet("transactions/{transactionId:guid}/appointment")]
    public Task<ActionResult<TransactionAppointmentResponse?>> Appointment(Guid transactionId, CancellationToken token) =>
        Execute(() => appointmentService.GetAsync(User, transactionId, token));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpPost("customers/me/transactions/{transactionId:guid}/appointment")]
    public Task<ActionResult<TransactionAppointmentResponse>> CreateAppointment(
        Guid transactionId, CreateTransactionAppointmentInput input, CancellationToken token) =>
        Execute(() => appointmentService.CreateAsync(User, transactionId, input, token));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpPost("customers/me/transactions/{transactionId:guid}/appointment-change-requests")]
    public Task<ActionResult<AppointmentChangeResponse>> RequestAppointmentChange(
        Guid transactionId, RequestAppointmentChangeInput input, CancellationToken token) =>
        Execute(() => appointmentService.RequestChangeAsync(User, transactionId, input, token));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("transactions/{transactionId:guid}/start")]
    public Task<ActionResult<WorkTransactionDetail>> Start(Guid transactionId, CancellationToken token) =>
        Execute(() => workService.StartAsync(User, transactionId, token));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("transactions/{transactionId:guid}/completions/drafts")]
    public Task<ActionResult<WorkCompletionRevisionResponse>> SaveDraft(
        Guid transactionId, SaveCompletionDraftInput input, CancellationToken token) =>
        Execute(() => workService.SaveDraftAsync(User, transactionId, input, token));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("transactions/{transactionId:guid}/completion-evidence")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<CompletionEvidenceResponse>> Upload(
        Guid transactionId, [FromForm] string roleCode, [FromForm] string? description, [FromForm] IFormFile file,
        CancellationToken token) => Execute(() => workService.UploadEvidenceAsync(User, transactionId, roleCode, description, file, token));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("transactions/{transactionId:guid}/completions/submit")]
    public Task<ActionResult<WorkCompletionRevisionResponse>> Submit(Guid transactionId, CancellationToken token) =>
        Execute(() => workService.SubmitCompletionAsync(User, transactionId, token));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpPost("transactions/{transactionId:guid}/confirm-completion")]
    public Task<ActionResult<CompletionConfirmationResponse>> Confirm(
        Guid transactionId, ConfirmCompletionInput input, CancellationToken token) =>
        Execute(() => workService.ConfirmAsync(User, transactionId, input, token));

    [Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
    [HttpGet("files/{fileId:guid}")]
    public async Task<IActionResult> Download(Guid fileId, CancellationToken token)
    {
        try
        {
            var result = await workService.OpenEvidenceAsync(User, fileId, token);
            return File(result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
        }
        catch (WorkBusinessException exception)
        {
            return StatusCode(exception.StatusCode, Error(exception));
        }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (WorkBusinessException exception) { return StatusCode(exception.StatusCode, Error(exception)); }
    }

    private ApiErrorResponse Error(WorkBusinessException exception) => new(
        exception.BusinessCode, exception.Message, exception.FieldErrors, HttpContext.TraceIdentifier);
}
