using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/customer-withdrawals")]
public sealed class AdminCustomerWithdrawalController(CustomerWithdrawalService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<AdminCustomerWithdrawalListResponse>> Search([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default) =>
        Run(() => service.SearchAdminAsync(status, page, pageSize, token));
    [HttpGet("{id:guid}")]
    public Task<ActionResult<AdminCustomerWithdrawalDetailResponse>> Detail(Guid id, CancellationToken token) => Run(() => service.AdminDetailAsync(id, token));
    [HttpPost("{id:guid}/recheck")]
    public Task<ActionResult<AdminCustomerWithdrawalDetailResponse>> Recheck(Guid id, AdminCustomerWithdrawalDecisionRequest input, CancellationToken token) =>
        Run(() => service.RecheckAsync(id, input, Actor(), token));
    [HttpPost("{id:guid}/complete")]
    public Task<ActionResult<AdminCustomerWithdrawalDetailResponse>> Complete(Guid id, AdminCustomerWithdrawalDecisionRequest input, CancellationToken token) =>
        Run(() => service.CompleteAsync(id, input, Actor(), token));
    [HttpPost("{id:guid}/reject")]
    public Task<ActionResult<AdminCustomerWithdrawalDetailResponse>> Reject(Guid id, AdminCustomerWithdrawalDecisionRequest input, CancellationToken token) =>
        Run(() => service.RejectAsync(id, input, Actor(), token));

    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerWithdrawalException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
