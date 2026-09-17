using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/providers")]
public sealed class PublicProvidersController(PublicProviderProfileService service) : ControllerBase
{
    [HttpGet("{providerId:guid}")]
    public async Task<ActionResult<PublicProviderProfileResponse>> Get(Guid providerId, [FromQuery] Guid? campaignId, CancellationToken token)
    {
        try { return Ok(await service.GetAsync(providerId, campaignId, token)); }
        catch (PublicProviderProfileException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }
}
