using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Catalog;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
[Route("api/v1/categories")]
public sealed class CatalogController(CatalogQueryService catalogQueryService) : ControllerBase
{
    [HttpGet("majors")]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetMajors(
        [FromQuery] bool emergencyOnly,
        [FromQuery] bool includeSubscription,
        CancellationToken cancellationToken) =>
        Ok(await catalogQueryService.GetMajorCategoriesAsync(emergencyOnly, includeSubscription, cancellationToken));

    [HttpGet("{majorId:guid}/middles")]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetMiddles(
        Guid majorId,
        [FromQuery] bool emergencyOnly,
        [FromQuery] bool includeSubscription,
        CancellationToken cancellationToken)
    {
        var categories = await catalogQueryService.GetMiddleCategoriesAsync(majorId, emergencyOnly, includeSubscription, cancellationToken);
        return categories is null ? NotFound() : Ok(categories);
    }

    [HttpGet("{middleId:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetServices(
        Guid middleId,
        [FromQuery] bool emergencyOnly,
        [FromQuery] bool includeSubscription,
        CancellationToken cancellationToken)
    {
        var categories = await catalogQueryService.GetServicesAsync(middleId, emergencyOnly, includeSubscription, cancellationToken);
        return categories is null ? NotFound() : Ok(categories);
    }

    [HttpGet("{serviceId:guid}/request-fields")]
    public async Task<ActionResult<IReadOnlyList<RequestFieldResponse>>> GetRequestFields(Guid serviceId, CancellationToken cancellationToken)
    {
        var fields = await catalogQueryService.GetRequestFieldsAsync(serviceId, cancellationToken);
        return fields is null ? NotFound() : Ok(fields);
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/v1/administrative-areas")]
public sealed class AdministrativeAreasController(CatalogQueryService catalogQueryService) : ControllerBase
{
    [HttpGet("sidos")]
    public async Task<ActionResult<IReadOnlyList<AdministrativeAreaResponse>>> GetSidos(CancellationToken cancellationToken) =>
        Ok(await catalogQueryService.GetActiveSidoAsync(cancellationToken));

    [HttpGet("sigungu")]
    public async Task<ActionResult<IReadOnlyList<AdministrativeAreaResponse>>> GetSigungu(
        [FromQuery] Guid? parentId,
        CancellationToken cancellationToken) =>
        Ok(await catalogQueryService.GetActiveSigunguAsync(parentId, cancellationToken));
}
