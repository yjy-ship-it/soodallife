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

    [HttpPut]
    public Task<ActionResult> Update(UpdateProviderProfileInput input, CancellationToken token) =>
        Run(async () => await providerService.UpdateProfileAsync(User, input, token));

    [HttpGet("onboarding-dashboard")]
    public Task<ActionResult> Dashboard(CancellationToken token) =>
        Run(async () => await providerService.GetDashboardAsync(User, token));

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

    [HttpPost("service-categories/{categoryId:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid categoryId, CancellationToken token)
    {
        try { await providerService.ResubmitServiceAsync(User, categoryId, token); return NoContent(); }
        catch (ProviderConfigurationException exception) { return Error(exception); }
    }

    [HttpGet("requirements")]
    public Task<ActionResult> Requirements(CancellationToken token) => Run(async () => await providerService.GetRequirementsAsync(User, token));

    [HttpGet("document-types")]
    public Task<ActionResult> DocumentTypes(CancellationToken token) => Run(async () => await providerService.GetDocumentTypesAsync(token));

    [HttpGet("documents")]
    public Task<ActionResult> Documents(CancellationToken token) => Run(async () => await providerService.GetDocumentsAsync(User, token));

    [HttpPost("documents")]
    [RequestSizeLimit(10_485_760)]
    public Task<ActionResult> UploadDocument([FromForm] RegisterProviderDocumentInput input, IFormFile file, CancellationToken token) =>
        Run(async () => await providerService.UploadDocumentAsync(User, input, file, token));

    [HttpGet("documents/{fileId:guid}/content")]
    public async Task<IActionResult> DownloadDocument(Guid fileId, CancellationToken token)
    {
        try { var result = await providerService.OpenDocumentAsync(User, fileId, token); return File(result.Content, result.ContentType, result.FileName); }
        catch (ProviderConfigurationException exception) { return Error(exception); }
    }

    [HttpPut("requirements/{verificationId:guid}/evidence")]
    public Task<ActionResult> LinkEvidence(Guid verificationId, LinkProviderEvidenceInput input, CancellationToken token) =>
        Run(async () => await providerService.LinkEvidenceAsync(User, verificationId, input, token));

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

    private async Task<ActionResult> Run(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (ProviderConfigurationException exception) { return Error(exception); }
    }

    private ObjectResult Error(ProviderConfigurationException exception) =>
        StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
}
