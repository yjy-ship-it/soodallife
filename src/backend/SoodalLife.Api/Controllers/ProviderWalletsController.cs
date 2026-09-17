using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/wallet")]
public sealed class ProviderWalletsController(ProviderWalletService service, TossWalletTopUpService topUps) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderWalletDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetDashboardAsync(User, cancellationToken)); }
        catch (WalletOperationException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }

    [HttpPost("top-ups/prepare")]
    public async Task<ActionResult<PrepareWalletTopUpResponse>> Prepare(PrepareWalletTopUpRequest input, CancellationToken cancellationToken) =>
        await RunTopUp(() => topUps.PrepareAsync(User, input, cancellationToken));

    [HttpPost("top-ups/confirm")]
    public async Task<ActionResult<ConfirmWalletTopUpResponse>> Confirm(ConfirmWalletTopUpRequest input, CancellationToken cancellationToken) =>
        await RunTopUp(() => topUps.ConfirmAsync(User, input, cancellationToken));

    private async Task<ActionResult<T>> RunTopUp<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (WalletOperationException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
