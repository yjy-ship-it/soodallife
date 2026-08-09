using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Provider)]
[Route("api/v1/providers/me")]
public sealed class ProvidersController(ProviderConfigurationService providerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderProfileResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await providerService.GetProfileAsync(User, cancellationToken));

    [HttpGet("service-categories")]
    public async Task<ActionResult<IReadOnlyList<ProviderServiceCategoryResponse>>> GetServiceCategories(CancellationToken cancellationToken) =>
        Ok(await providerService.GetServiceCategoriesAsync(User, cancellationToken));

    [HttpPut("service-categories")]
    public async Task<ActionResult<IReadOnlyList<ProviderServiceCategoryResponse>>> ReplaceServiceCategories(
        ReplaceProviderServiceCategoriesInput input,
        CancellationToken cancellationToken) =>
        await Execute(() => providerService.ReplaceServiceCategoriesAsync(User, input, cancellationToken));

    [HttpGet("service-areas")]
    public async Task<ActionResult<IReadOnlyList<ProviderServiceAreaResponse>>> GetServiceAreas(CancellationToken cancellationToken) =>
        Ok(await providerService.GetServiceAreasAsync(User, cancellationToken));

    [HttpPut("service-areas")]
    public async Task<ActionResult<IReadOnlyList<ProviderServiceAreaResponse>>> ReplaceServiceAreas(
        ReplaceProviderServiceAreasInput input,
        CancellationToken cancellationToken) =>
        await Execute(() => providerService.ReplaceServiceAreasAsync(User, input, cancellationToken));

    private async Task<ActionResult<IReadOnlyList<T>>> Execute<T>(Func<Task<IReadOnlyList<T>>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
