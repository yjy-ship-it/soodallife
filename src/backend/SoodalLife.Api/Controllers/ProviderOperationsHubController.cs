using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Provider)]
[Route("api/v1/providers/me/operations-hub")]
public sealed class ProviderOperationsHubController(ProviderOperationsHubService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderOperationsHubResponse>> Get(
        [FromQuery] string? group, [FromQuery] string? domain, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken token = default)
    {
        try { return Ok(await service.GetAsync(User, group, domain, page, pageSize, token)); }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(exception.StatusCode,
                ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
