using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Catalog;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1/service-visuals/settings")]
public sealed class ServiceVisualSettingsController(ServiceVisualSettingsService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ServiceVisualSettingsResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(cancellationToken));

    [Authorize(Roles = RoleCodes.Admin)]
    [HttpPut]
    public async Task<ActionResult<ServiceVisualSettingsResponse>> Update(UpdateServiceVisualSettingsRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(request, cancellationToken));
    }
}
