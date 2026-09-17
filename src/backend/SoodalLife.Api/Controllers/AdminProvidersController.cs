using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/providers")]
public sealed class AdminProvidersController(AdminProviderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminProviderListResponse>> Search([FromQuery] string? search, [FromQuery] string? accountStatus,
        [FromQuery] string? approvalStatus, [FromQuery] string? activityStatus, [FromQuery] string? providerType, [FromQuery] Guid? serviceId, [FromQuery] Guid? areaId,
        [FromQuery] string? documentStatus, [FromQuery] DateOnly? joinedFrom, [FromQuery] DateOnly? joinedTo,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    { try { return Ok(await service.SearchAsync(search, accountStatus, approvalStatus, activityStatus, providerType, serviceId, areaId, documentStatus, joinedFrom, joinedTo, page, pageSize, cancellationToken)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpGet("{providerId:guid}")]
    public async Task<ActionResult<AdminProviderDetailResponse>> Get(Guid providerId, CancellationToken cancellationToken)
    { var provider = await service.GetAsync(providerId, cancellationToken); return provider is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.")) : Ok(provider); }
    [HttpPost("{providerId:guid}/approval-decisions")]
    public async Task<ActionResult<AdminProviderApprovalDecisionResponse>> Decide(Guid providerId, AdminProviderApprovalDecisionRequest request, CancellationToken cancellationToken)
    { try { return Ok(await service.DecideApprovalAsync(providerId, request, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpPut("{providerId:guid}")]
    public async Task<ActionResult<AdminProviderDetailResponse>> Update(Guid providerId, AdminUpdateProviderRequest request, CancellationToken token)
    { try { return Ok(await service.UpdateAsync(providerId, request, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), token)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpPut("{providerId:guid}/service-categories")]
    public async Task<ActionResult<AdminProviderDetailResponse>> ReplaceServices(Guid providerId, AdminReplaceProviderServicesRequest request, CancellationToken token)
    { try { return Ok(await service.ReplaceServicesAsync(providerId, request, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), token)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpPut("{providerId:guid}/service-areas")]
    public async Task<ActionResult<AdminProviderDetailResponse>> ReplaceAreas(Guid providerId, AdminReplaceProviderAreasRequest request, CancellationToken token)
    { try { return Ok(await service.ReplaceAreasAsync(providerId, request, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), token)); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
    [HttpGet("{providerId:guid}/documents/{fileId:guid}")]
    public async Task<IActionResult> Download(Guid providerId, Guid fileId, [FromQuery] bool inline, CancellationToken token)
    { try { var file = await service.OpenDocumentAsync(providerId, fileId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), token); return inline ? File(file.Content, file.ContentType) : File(file.Content, file.ContentType, file.FileName); }
      catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); } }
}
