using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Customer), Route("api/v1/customers/me")]
public sealed class CustomerDailyController(CustomerDailyService service, CustomerWithdrawalService withdrawal) : ControllerBase
{
    [HttpGet("my-soodal/summary")]
    public Task<ActionResult> Summary(CancellationToken token) => Run(() => service.Summary(User, token));

    [HttpGet("consent-history")]
    public Task<ActionResult> ConsentHistory(CancellationToken token) => Run(() => service.ConsentHistory(User, token));

    [HttpGet("withdrawal-readiness")]
    public Task<ActionResult> WithdrawalReadiness(CancellationToken token) => WithdrawalRun(async () => (await withdrawal.DashboardAsync(User, token)).Readiness);

    private async Task<ActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerAccountException exception)
        { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    private async Task<ActionResult> WithdrawalRun(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerWithdrawalException exception)
        { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
