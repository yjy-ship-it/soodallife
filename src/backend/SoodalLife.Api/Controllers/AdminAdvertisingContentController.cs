using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/advertising-content")]
public sealed class AdminAdvertisingContentController(AdvertisingContentService service) : ControllerBase
{
    [HttpGet("placements")]
    public async Task<ActionResult<IReadOnlyList<AdvertisingPlacementResponse>>> GetPlacements(CancellationToken token) =>
        Ok(await service.GetPlacementsAsync(token));

    [HttpGet("campaigns")]
    public async Task<ActionResult<AdminAdvertisingCampaignListResponse>> SearchCampaigns(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? audience,
        [FromQuery] string? placement, [FromQuery] Guid? categoryId, [FromQuery] DateOnly? activeFrom, [FromQuery] DateOnly? activeTo,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default) =>
        await Execute(() => service.SearchCampaignsAsync(search, status, audience, placement, categoryId, activeFrom, activeTo, page, pageSize, token));

    [HttpGet("campaigns/{campaignId:guid}")]
    public async Task<ActionResult<AdminAdvertisingCampaignDetail>> GetCampaign(Guid campaignId, CancellationToken token)
    {
        var result = await service.GetCampaignAsync(campaignId, token);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADVERTISING_CAMPAIGN_NOT_FOUND", "광고 캠페인을 찾을 수 없습니다.")) : Ok(result);
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<AdminAdvertisingCampaignDetail>> CreateCampaign(SaveAdvertisingCampaignRequest request, CancellationToken token) =>
        await Execute(() => service.CreateCampaignAsync(request, ActorId(), token));

    [HttpPut("campaigns/{campaignId:guid}")]
    public async Task<ActionResult<AdminAdvertisingCampaignDetail>> UpdateCampaign(Guid campaignId, SaveAdvertisingCampaignRequest request, CancellationToken token) =>
        await Execute(() => service.UpdateCampaignAsync(campaignId, request, ActorId(), token));

    [HttpPost("campaigns/{campaignId:guid}/review")]
    public async Task<ActionResult<AdminAdvertisingCampaignDetail>> ReviewCampaign(Guid campaignId, AdvertisingReviewRequest request, CancellationToken token) =>
        await Execute(() => service.ReviewCampaignAsync(campaignId, request, ActorId(), token));

    [HttpPost("campaigns/{campaignId:guid}/pause")]
    public async Task<ActionResult<AdminAdvertisingCampaignDetail>> PauseCampaign(Guid campaignId, AdvertisingPauseRequest request, CancellationToken token) =>
        await Execute(() => service.PauseCampaignAsync(campaignId, request, ActorId(), token));

    [HttpPost("campaigns/{campaignId:guid}/creatives")]
    public async Task<ActionResult<AdvertisingCreativeResponse>> CreateCreative(Guid campaignId, SaveAdvertisingCreativeRequest request, CancellationToken token) =>
        await Execute(() => service.CreateCreativeAsync(campaignId, request, ActorId(), token));

    [HttpPut("campaigns/{campaignId:guid}/creatives/{creativeId:guid}")]
    public async Task<ActionResult<AdvertisingCreativeResponse>> UpdateCreative(Guid campaignId, Guid creativeId, SaveAdvertisingCreativeRequest request, CancellationToken token) =>
        await Execute(() => service.UpdateCreativeAsync(campaignId, creativeId, request, ActorId(), token));

    [HttpGet("contents")]
    public async Task<ActionResult<AdminManagedContentListResponse>> SearchContents(
        [FromQuery] string? search, [FromQuery] string? type, [FromQuery] string? status, [FromQuery] string? audience,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default) =>
        await Execute(() => service.SearchContentsAsync(search, type, status, audience, page, pageSize, token));

    [HttpGet("contents/{contentId:guid}")]
    public async Task<ActionResult<AdminManagedContentDetail>> GetContent(Guid contentId, CancellationToken token)
    {
        var result = await service.GetContentAsync(contentId, token);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "MANAGED_CONTENT_NOT_FOUND", "콘텐츠를 찾을 수 없습니다.")) : Ok(result);
    }

    [HttpPost("contents")]
    public async Task<ActionResult<AdminManagedContentDetail>> CreateContent(SaveManagedContentRequest request, CancellationToken token) =>
        await Execute(() => service.CreateContentAsync(request, ActorId(), token));

    [HttpPut("contents/{contentId:guid}")]
    public async Task<ActionResult<AdminManagedContentDetail>> UpdateContent(Guid contentId, SaveManagedContentRequest request, CancellationToken token) =>
        await Execute(() => service.UpdateContentAsync(contentId, request, ActorId(), token));

    [HttpPost("contents/{contentId:guid}/review")]
    public async Task<ActionResult<AdminManagedContentDetail>> ReviewContent(Guid contentId, AdvertisingReviewRequest request, CancellationToken token) =>
        await Execute(() => service.ReviewContentAsync(contentId, request, ActorId(), token));

    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (AdvertisingContentException exception)
        { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
