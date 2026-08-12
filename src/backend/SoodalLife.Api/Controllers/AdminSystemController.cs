using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using System.Security.Claims;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin")]
public sealed class AdminSystemController(AdminSystemService service) : ControllerBase
{
    [HttpGet("audit-logs")]
    public Task<ActionResult<AdminAuditLogResponse>> AuditLogs([FromQuery] AdminAuditLogQuery query, CancellationToken token) =>
        Execute(() => service.GetAuditLogsAsync(query, token));

    [HttpGet("system/status")]
    public Task<ActionResult<AdminSystemStatusResponse>> Status(CancellationToken token) =>
        Execute(() => service.GetSystemStatusAsync(token));

    [HttpPost("system/outbox/{id:guid}/retry")]
    public async Task<ActionResult> RetryOutbox(Guid id, CancellationToken token)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
            await service.RequestOutboxRetryAsync(id, actor, token);
            return NoContent();
        }
        catch (AdminSystemException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (AdminSystemException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
