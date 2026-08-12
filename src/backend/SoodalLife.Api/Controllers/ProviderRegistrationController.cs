using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/provider-registration")]
public sealed class PublicProviderRegistrationController(ProviderRegistrationService service) : ControllerBase
{
    [HttpGet("availability/login-id")]
    public Task<ActionResult> LoginId([FromQuery] string value, CancellationToken token) => Run(async () => new { available = await service.LoginIdAvailableAsync(value, token) });
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
        try { return Ok(await service.AddRoleAsync(User, input, HttpContext, token)); }
        catch (ProviderConfigurationException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
