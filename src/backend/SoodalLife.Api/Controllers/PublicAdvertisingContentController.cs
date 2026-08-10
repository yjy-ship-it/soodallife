using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public")]
public sealed class PublicAdvertisingContentController(AdvertisingContentService service) : ControllerBase
{
    [HttpGet("advertising")]
    public async Task<ActionResult<IReadOnlyList<PublicAdvertisingCreative>>> GetAdvertising(
        [FromQuery] string audience, [FromQuery] string placement, [FromQuery] Guid? categoryId,
        [FromQuery] Guid? areaId, CancellationToken token) =>
        await Execute(() => service.GetPublicAdvertisingAsync(audience, placement, categoryId, areaId, token));

    [HttpPost("advertising/creatives/{creativeId:guid}/events/{eventType}")]
    public async Task<IActionResult> RecordEvent(Guid creativeId, string eventType, AdvertisingEventRequest request, CancellationToken token)
    {
        try { await service.RecordEventAsync(creativeId, eventType, request, token); return NoContent(); }
        catch (AdvertisingContentException exception)
        { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }

    [HttpGet("contents")]
    public async Task<ActionResult<IReadOnlyList<PublicManagedContent>>> GetContents(
        [FromQuery] string type, [FromQuery] string audience, [FromQuery] Guid? categoryId,
        [FromQuery] Guid? areaId, CancellationToken token) =>
        await Execute(() => service.GetPublicContentsAsync(type, audience, categoryId, areaId, token));

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (AdvertisingContentException exception)
        { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
