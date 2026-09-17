using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Catalog;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/catalog")]
public sealed class PublicCatalogController(PublicCatalogQueryService service) : ControllerBase
{
    [HttpGet("categories/majors")]
    public Task<List<PublicCategoryResponse>> Majors(CancellationToken token) => service.GetMajorsAsync(token);

    [HttpGet("categories/{parentId:guid}/children")]
    public async Task<ActionResult<List<PublicCategoryResponse>>> Children(Guid parentId, CancellationToken token)
    {
        var result = await service.GetChildrenAsync(parentId, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("services")]
    public Task<List<PublicServiceSummaryResponse>> Services(
        [FromQuery] Guid? majorId, [FromQuery] Guid? middleId, [FromQuery] int take = 24, CancellationToken token = default) =>
        service.SearchServicesAsync(null, majorId, middleId, take, token);

    [HttpGet("services/representative")]
    public Task<List<PublicServiceSummaryResponse>> RepresentativeServices(
        [FromQuery] int take = 8, [FromQuery] int maxPerMiddle = 2, CancellationToken token = default) =>
        service.GetRepresentativeServicesAsync(take, maxPerMiddle, token);

    [HttpGet("services/search")]
    public Task<List<PublicServiceSummaryResponse>> Search(
        [FromQuery] string? q, [FromQuery] int take = 60, CancellationToken token = default) =>
        service.SearchServicesAsync(q, null, null, take, token);

    [HttpGet("services/{id:guid}")]
    public async Task<ActionResult<PublicServiceDetailResponse>> Detail(Guid id, CancellationToken token)
    {
        var result = await service.GetServiceAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("services/by-slug/{slug}")]
    public async Task<ActionResult<PublicServiceDetailResponse>> DetailBySlug(string slug, CancellationToken token)
    {
        var result = await service.GetServiceBySlugAsync(slug, token);
        return result is null ? NotFound() : Ok(result);
    }
}
