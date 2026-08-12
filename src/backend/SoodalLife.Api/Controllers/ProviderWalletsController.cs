using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/wallet")]
public sealed class ProviderWalletsController(ProviderWalletService service) : ControllerBase
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
}
