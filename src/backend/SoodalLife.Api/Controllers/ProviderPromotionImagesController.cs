using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/provider-promotion-images")]
public sealed class ProviderPromotionImagesController(ProviderConfigurationService providerService) : ControllerBase
{
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Open(Guid fileId, CancellationToken token)
    {
        try
        {
            var result = await providerService.OpenPromotionImageAsync(fileId, token);
            return File(result.Content, result.ContentType, enableRangeProcessing: true);
        }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(exception.StatusCode,
                ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
