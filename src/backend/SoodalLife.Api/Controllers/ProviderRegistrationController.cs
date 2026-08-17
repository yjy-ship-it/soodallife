using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/provider-registration")]
public sealed class PublicProviderRegistrationController(ProviderRegistrationService service) : ControllerBase
{
    [HttpGet("availability/login-id")]
    public Task<ActionResult> LoginId([FromQuery] string value, CancellationToken token) => Run(async () =>
    {
        var normalizedValue = value.Trim();
        return new { available = await service.LoginIdAvailableAsync(normalizedValue, token), normalizedValue };
    });
    [HttpGet("availability/business-registration-number")]
    public Task<ActionResult> BusinessRegistrationNumber([FromQuery] string value, CancellationToken token) =>
        Run(async () => await service.BusinessRegistrationNumberAvailabilityAsync(value, token));
    [HttpGet("legal-documents")]
    public Task<ActionResult> LegalDocuments(CancellationToken token) => Run(async () => await service.ActiveDocumentsAsync(token));
    [HttpPost]
    public Task<ActionResult> Register(RegisterProviderRequest input, CancellationToken token) => Run(async () => await service.RegisterAsync(input, HttpContext, token));

    private async Task<ActionResult> Run(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (ProviderConfigurationException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}

[ApiController, Authorize, Route("api/v1/provider-registration")]
public sealed class ProviderRegistrationController(ProviderRegistrationService service) : ControllerBase
{
    [HttpPost("role")]
    public async Task<ActionResult> AddRole(AddProviderRoleRequest input, CancellationToken token)
    {
        try
        {
            var result = await service.AddRoleAsync(User, input, HttpContext, token);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new(ClaimTypes.Name, result.LoginId),
            };
            claims.AddRange(result.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationConstants.Scheme));
            await HttpContext.SignInAsync(AuthenticationConstants.Scheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });
            return Ok(result);
        }
        catch (ProviderConfigurationException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
