using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Provider)]
[Route("api/v1/providers/me")]
public sealed class ProvidersController(ProviderConfigurationService providerService, ILogger<ProvidersController> logger) : ControllerBase
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

    [HttpGet("operations-dashboard")]
    public Task<ActionResult> OperationsDashboard(CancellationToken token) =>
        Run(async () => await providerService.GetOperationsDashboardAsync(User, token));

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

    [HttpPost("approval/resubmit")]
    public async Task<ActionResult> ResubmitApproval(ResubmitProviderApprovalInput input, CancellationToken token)
    {
        try { return Ok(await providerService.ResubmitApprovalAsync(User, input, token)); }
        catch (ProviderConfigurationException exception) { return Error(exception); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider approval resubmission failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.Create(HttpContext,
                "PROVIDER_APPROVAL_RESUBMIT_FAILED", $"보완 제출 처리 중 서버 오류가 발생했습니다. 오류번호: {HttpContext.TraceIdentifier}"));
        }
    }

    [HttpGet("requirements")]
    public Task<ActionResult> Requirements(CancellationToken token) => Run(async () => await providerService.GetRequirementsAsync(User, token));

    [HttpGet("document-types")]
    public Task<ActionResult> DocumentTypes(CancellationToken token) => Run(async () => await providerService.GetDocumentTypesAsync(token));

    [HttpGet("documents")]
    public Task<ActionResult> Documents(CancellationToken token) => Run(async () => await providerService.GetDocumentsAsync(User, token));

    [HttpPost("promotion-images/logo")]
    [RequestSizeLimit(5_500_000)]
    public Task<ActionResult> UploadPromotionLogo(IFormFile file, CancellationToken token) =>
        Run(async () => await providerService.UploadPromotionLogoAsync(User, file, token));

    [HttpPost("promotion-images/logo-content")]
    [RequestSizeLimit(8_000_000)]
    public Task<ActionResult> UploadPromotionLogoContent(ProviderPromotionImageContentInput input, CancellationToken token) =>
        Run(async () => await providerService.UploadPromotionLogoContentAsync(User, input, token));

    [HttpGet("promotion-images/storage-status")]
    public Task<ActionResult> PromotionStorageStatus(CancellationToken token) =>
        Run(async () => await providerService.CheckPromotionStorageAsync(User, token));

    [HttpPost("promotion-images/photos")]
    [RequestSizeLimit(26_500_000)]
    public Task<ActionResult> UploadPromotionPhotos(List<IFormFile> files, CancellationToken token) =>
        Run(async () => await providerService.UploadPromotionPhotosAsync(User, files, token));

    [HttpPost("promotion-images/photo-content")]
    [RequestSizeLimit(8_000_000)]
    public Task<ActionResult> UploadPromotionPhotoContent(ProviderPromotionPhotoContentInput input, CancellationToken token) =>
        Run(async () => await providerService.UploadPromotionPhotoContentAsync(User, input, token));

    [HttpPost("promotion-images/chunks")]
    [RequestSizeLimit(120_000)]
    public Task<ActionResult> UploadPromotionImageChunk(ProviderPromotionImageChunkInput input, CancellationToken token) =>
        RunPromotionUpload(async () => await providerService.UploadPromotionImageChunkAsync(User, input, token));

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

    private async Task<ActionResult> RunPromotionUpload(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (ProviderConfigurationException exception) { return Error(exception); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Provider promotion image upload failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.Create(HttpContext,
                "PROMOTION_IMAGE_UPLOAD_FAILED", $"이미지 저장 처리 중 오류가 발생했습니다. 오류번호: {HttpContext.TraceIdentifier}"));
        }
    }

    private ObjectResult Error(ProviderConfigurationException exception) =>
        StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
}
