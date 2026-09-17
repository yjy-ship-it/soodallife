using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/providers/me"), Authorize(Roles = RoleCodes.Provider)]
public sealed class ProviderAftercareController(ProviderAftercareService service) : ControllerBase
{
    [HttpGet("after-services")] public Task<ActionResult<IReadOnlyList<ProviderAfterServiceListItem>>> AfterServices(CancellationToken token) => Run(() => service.AfterServices(User, token));
    [HttpGet("after-services/{id:guid}")] public Task<ActionResult<ProviderAfterServiceDetail>> AfterService(Guid id, CancellationToken token) => Run(() => service.AfterService(User, id, token));
    [HttpPost("after-services/{id:guid}/confirm")] public Task<ActionResult<ProviderAfterServiceDetail>> Confirm(Guid id, ProviderConfirmAfterServiceInput input, CancellationToken token) => Run(() => service.Confirm(User, id, input, token));
    [HttpPost("after-services/{id:guid}/visit-schedule")] public Task<ActionResult<ProviderAfterServiceDetail>> Schedule(Guid id, ProviderScheduleAfterServiceInput input, CancellationToken token) => Run(() => service.Schedule(User, id, input, token));
    [HttpPost("after-services/{id:guid}/actions")] public Task<ActionResult<ProviderAfterServiceDetail>> Action(Guid id, ProviderAfterServiceActionInput input, CancellationToken token) => Run(() => service.Action(User, id, input, token));
    [HttpPost("after-services/{id:guid}/completion-report")] public Task<ActionResult<ProviderAfterServiceDetail>> Complete(Guid id, ProviderCompleteAfterServiceInput input, CancellationToken token) => Run(() => service.Complete(User, id, input, token));
    [HttpPost("after-services/{id:guid}/evidence"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<ProviderCaseFile>> AfterServiceEvidence(Guid id, [FromForm] string? role, [FromForm] string? description, [FromForm] IFormFile file, CancellationToken token) => Run(() => service.UploadAfterServiceEvidence(User, id, role, description, file, token));
    [HttpPost("after-services/{id:guid}/evidence-content"), RequestSizeLimit(15 * 1024 * 1024)]
    public Task<ActionResult<ProviderCaseFile>> AfterServiceEvidenceContent(Guid id, ProviderAfterServiceEvidenceContentInput input, CancellationToken token) => Run(() => service.UploadAfterServiceEvidenceContent(User, id, input, token));
    [HttpGet("after-services/{id:guid}/files/{fileId:guid}")] public Task<IActionResult> AfterServiceFile(Guid id, Guid fileId, CancellationToken token) => Download(() => service.OpenAfterServiceFile(User, id, fileId, token));

    [HttpGet("disputes")] public Task<ActionResult<IReadOnlyList<ProviderDisputeListItem>>> Disputes(CancellationToken token) => Run(() => service.Disputes(User, token));
    [HttpGet("disputes/{id:guid}")] public Task<ActionResult<ProviderDisputeDetail>> Dispute(Guid id, CancellationToken token) => Run(() => service.Dispute(User, id, token));
    [HttpPost("disputes/{id:guid}/responses")] public Task<ActionResult<ProviderDisputeDetail>> Respond(Guid id, ProviderDisputeResponseInput input, CancellationToken token) => Run(() => service.Respond(User, id, input, token));
    [HttpPost("disputes/{id:guid}/evidence"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<ProviderCaseFile>> DisputeEvidence(Guid id, [FromForm] string? description, [FromForm] IFormFile file, CancellationToken token) => Run(() => service.UploadDisputeEvidence(User, id, description, file, token));
    [HttpGet("disputes/{id:guid}/files/{fileId:guid}")] public Task<IActionResult> DisputeFile(Guid id, Guid fileId, CancellationToken token) => Download(() => service.OpenDisputeFile(User, id, fileId, token));

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (WorkBusinessException e) { return StatusCode(e.StatusCode, new { code = e.BusinessCode, message = e.Message, fieldErrors = e.FieldErrors }); } }
    private async Task<IActionResult> Download(Func<Task<(Stream Stream, string ContentType, string FileName)>> action) { try { var file = await action(); return File(file.Stream, file.ContentType, file.FileName); } catch (WorkBusinessException e) { return StatusCode(e.StatusCode, new { code = e.BusinessCode, message = e.Message }); } }
}
