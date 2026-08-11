using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/customers/me"), Authorize(Roles = RoleCodes.Customer)]
public sealed class CustomerReportsController(CustomerReportService service) : ControllerBase
{
    [HttpGet("report-types")] public Task<ActionResult<IReadOnlyList<CustomerReportType>>> Types(CancellationToken token) => Run(() => service.Types(token));
    [HttpGet("reports")] public Task<ActionResult<IReadOnlyList<CustomerReportResponse>>> List(CancellationToken token) => Run(() => service.List(User, token));
    [HttpGet("reports/{id:guid}")] public Task<ActionResult<CustomerReportResponse>> Detail(Guid id, CancellationToken token) => Run(() => service.Detail(User, id, token));
    [HttpPost("reports")] public Task<ActionResult<CustomerReportResponse>> Create(CreateCustomerReportInput input, CancellationToken token) => Run(() => service.Create(User, input, token));
    [HttpPost("reports/{id:guid}/evidence"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<CustomerReportEvidence>> Upload(Guid id, [FromForm] string? description, [FromForm] IFormFile file, CancellationToken token) => Run(() => service.Upload(User, id, description, file, token));
    [HttpGet("reports/{id:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> File(Guid id, Guid fileId, CancellationToken token)
    {
        try { var result = await service.OpenFile(User, id, fileId, token); return File(result.Stream, result.ContentType, result.FileName); }
        catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message, fieldErrors = error.FieldErrors }); }
    }
}
