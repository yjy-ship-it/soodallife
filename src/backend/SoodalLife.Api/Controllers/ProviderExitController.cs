using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/exit")]
public sealed class ProviderExitController(ProviderExitService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<ProviderExitDashboardResponse>> Dashboard(CancellationToken token) => Run(() => service.DashboardAsync(User, token));

    [HttpPost("requests")]
    public Task<ActionResult<ProviderExitRequestResponse>> Submit(CreateProviderExitRequest input, CancellationToken token) => Run(() => service.RequestAsync(User, input, token));

    [HttpPost("requests/{id:guid}/cancel")]
    public Task<ActionResult<ProviderExitRequestResponse>> Cancel(Guid id, CancelProviderExitRequest input, CancellationToken token) => Run(() => service.CancelAsync(User, id, input, token));

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (ProviderExitException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/provider-exits")]
public sealed class AdminProviderExitController(ProviderExitService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<AdminProviderExitListResponse>> Search([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default) =>
        Run(() => service.SearchAdminAsync(status, page, pageSize, token));

    [HttpGet("{id:guid}")]
    public Task<ActionResult<AdminProviderExitDetailResponse>> Detail(Guid id, CancellationToken token) => Run(() => service.AdminDetailAsync(id, token));

    [HttpPost("{id:guid}/recheck")]
    public Task<ActionResult<AdminProviderExitDetailResponse>> Recheck(Guid id, AdminProviderExitDecisionRequest input, CancellationToken token) =>
        Run(() => service.RecheckAsync(id, input, Actor(), token));

    [HttpPost("{id:guid}/complete")]
    public Task<ActionResult<AdminProviderExitDetailResponse>> Complete(Guid id, AdminProviderExitDecisionRequest input, CancellationToken token) =>
        Run(() => service.CompleteAsync(id, input, Actor(), token));

    [HttpPost("{id:guid}/reject")]
    public Task<ActionResult<AdminProviderExitDetailResponse>> Reject(Guid id, AdminProviderExitDecisionRequest input, CancellationToken token) =>
        Run(() => service.RejectAsync(id, input, Actor(), token));

    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (ProviderExitException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
