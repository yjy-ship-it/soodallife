using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Provider)]
[Route("api/v1/providers/me/operations-hub")]
public sealed class ProviderOperationsHubController(ProviderOperationsHubService service, IMemoryCache cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderOperationsHubResponse>> Get(
        [FromQuery] string? group, [FromQuery] string? domain, [FromQuery] bool activeOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken token = default)
    {
        try
        {
            var providerKey = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var normalizedGroup = group?.Trim().ToUpperInvariant() ?? string.Empty;
            var normalizedDomain = domain?.Trim().ToUpperInvariant() ?? string.Empty;
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
            var key = $"provider-hub:v198:{providerKey}:{normalizedGroup}:{normalizedDomain}:{activeOnly}:{normalizedPage}:{normalizedPageSize}";
            var result = await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
                return await service.GetAsync(User, normalizedGroup, normalizedDomain, activeOnly, normalizedPage, normalizedPageSize, token);
            });
            return Ok(result!);
        }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(exception.StatusCode,
                ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }

    [HttpGet("~/api/v1/providers/me/request-status")]
    public async Task<ActionResult<ProviderRequestStatusResponse>> RequestStatus(CancellationToken token = default)
    {
        try
        {
            var providerKey = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var result = await cache.GetOrCreateAsync($"provider-request-status:v190:{providerKey}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
                return await service.GetRequestStatusAsync(User, token);
            });
            return Ok(result!);
        }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(exception.StatusCode,
                ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
