using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Trust;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/trust")]
public sealed class ProviderTrustController(ProviderTrustService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderTrustDashboardResponse>> Get(CancellationToken token)
    {
        try { return Ok(await service.GetAsync(User, token)); }
        catch (ProviderTrustException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
