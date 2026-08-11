using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize]
public sealed class CustomerAfterServicesController(CustomerAfterServiceService service) : ControllerBase
{
    [HttpGet("api/v1/customers/me/service-history"), Authorize(Roles = RoleCodes.Customer)]
    public Task<ActionResult<IReadOnlyList<ServiceHistoryListItem>>> History(CancellationToken token) => Run(() => service.History(User, token));

    [HttpGet("api/v1/customers/me/service-history/{id:guid}"), Authorize(Roles = RoleCodes.Customer)]
    public Task<ActionResult<ServiceHistoryDetail>> HistoryDetail(Guid id, CancellationToken token) => Run(() => service.HistoryDetail(User, id, token));

    [HttpGet("api/v1/customers/me/after-services"), Authorize(Roles = RoleCodes.Customer)]
    public Task<ActionResult<IReadOnlyList<CustomerAfterServiceResponse>>> List(CancellationToken token) => Run(() => service.List(User, token));

    [HttpPost("api/v1/customers/me/transactions/{transactionId:guid}/after-services"), Authorize(Roles = RoleCodes.Customer)]
    public Task<ActionResult<CustomerAfterServiceResponse>> Create(Guid transactionId, CreateAfterServiceInput input, CancellationToken token) => Run(() => service.Create(User, transactionId, input, token));

    [HttpGet("api/v1/after-services/{id:guid}"), Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider + "," + RoleCodes.Admin)]
    public Task<ActionResult<CustomerAfterServiceResponse>> Detail(Guid id, CancellationToken token) => Run(() => service.Detail(User, id, token));

    [HttpPost("api/v1/after-services/{id:guid}/evidence"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024), Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider + "," + RoleCodes.Admin)]
    public Task<ActionResult<AfterServiceEvidenceResponse>> Upload(Guid id, [FromForm] string? role, [FromForm] string? description, [FromForm] IFormFile file, CancellationToken token) => Run(() => service.Upload(User, id, role, description, file, token));

    [HttpGet("api/v1/after-services/{id:guid}/files/{fileId:guid}"), Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider + "," + RoleCodes.Admin)]
    public async Task<IActionResult> File(Guid id, Guid fileId, CancellationToken token)
    {
        try { var result = await service.OpenFile(User, id, fileId, token); return File(result.Stream, result.ContentType, result.FileName); }
        catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }

    [HttpPost("api/v1/customers/me/after-services/{id:guid}/dispute"), Authorize(Roles = RoleCodes.Customer)]
    public Task<ActionResult<CustomerDisputeResponse>> Convert(Guid id, ConvertAfterServiceToDisputeInput input, CancellationToken token) => Run(() => service.ConvertToDispute(User, id, input, token));

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message, fieldErrors = error.FieldErrors }); }
    }
}
