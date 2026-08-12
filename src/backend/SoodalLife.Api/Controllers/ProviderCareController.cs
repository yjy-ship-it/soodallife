using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Subscriptions;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/care")]
public sealed class ProviderCareController(ProviderCareService service) : ControllerBase
{
    [HttpGet("home")] public Task<ActionResult<ProviderCareDashboardResponse>> Home(CancellationToken token) => Run(() => service.Dashboard(User, token));
    [HttpGet("requests/open")] public Task<ActionResult<IReadOnlyList<ProviderCareRequestItem>>> OpenRequests(CancellationToken token) => Run(() => service.OpenRequests(User, token));
    [HttpPost("requests/{id:guid}/applications")] public Task<ActionResult<SubscriptionApplicationResponse>> Apply(Guid id, SubmitSubscriptionApplicationRequest input, CancellationToken token) => Run(() => service.Apply(id, input, User, token));
    [HttpGet("applications")] public Task<ActionResult<IReadOnlyList<ProviderCareApplicationItem>>> Applications(CancellationToken token) => Run(() => service.Applications(User, token));
    [HttpGet("contracts")] public Task<ActionResult<IReadOnlyList<ProviderCareContractListItem>>> Contracts([FromQuery] string? status, CancellationToken token) => Run(() => service.Contracts(User, status, token));
    [HttpGet("contracts/{id:guid}")] public Task<ActionResult<ProviderCareContractDetail>> Contract(Guid id, CancellationToken token) => Run(() => service.Contract(id, User, token));
    [HttpGet("visits")] public Task<ActionResult<IReadOnlyList<ProviderCareVisitListItem>>> Visits([FromQuery] string? filter, CancellationToken token) => Run(() => service.Visits(User, filter, token));
    [HttpGet("visits/{id:guid}")] public Task<ActionResult<ProviderCareVisitDetail>> Visit(Guid id, CancellationToken token) => Run(() => service.Visit(id, User, token));
    [HttpGet("schedule-changes")] public Task<ActionResult<IReadOnlyList<ProviderCareScheduleChangeItem>>> ScheduleChanges(CancellationToken token) => Run(() => service.ScheduleChanges(User, token));
    [HttpPost("visits/{id:guid}/schedule-changes")] public Task<ActionResult<SubscriptionScheduleChangeResponse>> RequestScheduleChange(Guid id, RequestScheduleChangeRequest input, CancellationToken token) => Run(() => service.RequestScheduleChange(id, input, User, token));
    [HttpPost("schedule-changes/{id:guid}/decision")] public Task<ActionResult<SubscriptionScheduleChangeResponse>> DecideScheduleChange(Guid id, DecideScheduleChangeRequest input, CancellationToken token) => Run(() => service.DecideScheduleChange(id, input, User, token));
    [HttpPost("visits/{id:guid}/start")] public Task<ActionResult<ProviderCareVisitDetail>> Start(Guid id, StartProviderCareVisitRequest input, CancellationToken token) => Run(() => service.Start(id, input, User, token));
    [HttpPost("visits/{id:guid}/files"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)] public Task<ActionResult<ProviderCareUploadResponse>> Upload(Guid id, [FromForm] IFormFile file, CancellationToken token) => Run(() => service.Upload(id, User, file, token));
    [HttpPost("visits/{id:guid}/completion")] public Task<ActionResult<SubscriptionVisitResponse>> Complete(Guid id, CompleteSubscriptionVisitRequest input, CancellationToken token) => Run(() => service.Complete(id, input, User, token));
    [HttpGet("visits/{visitId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> File(Guid visitId, Guid fileId, CancellationToken token)
    {
        try { var result = await service.OpenFile(visitId, fileId, User, token); return File(result.Stream, result.ContentType, result.FileName); }
        catch (SubscriptionBusinessException error) { return StatusCode(error.StatusCode, ApiErrorResponse.Create(HttpContext, error.BusinessCode, error.Message)); }
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (SubscriptionBusinessException error) { return StatusCode(error.StatusCode, ApiErrorResponse.Create(HttpContext, error.BusinessCode, error.Message)); }
    }
}
