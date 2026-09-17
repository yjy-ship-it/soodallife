using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.PublicActivity;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/activity")]
public sealed class PublicActivityController(PublicActivityFeedService service) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration=15,Location=ResponseCacheLocation.Any)]
    public Task<PublicActivityFeedResponse> Get(
        [FromQuery] string? region,
        [FromQuery] string? eventType,
        [FromQuery] int take = 20,
        CancellationToken token = default) => service.GetAsync(region, eventType, take, token);
}
