using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/provider-requirement-standards")]
public sealed class AdminProviderRequirementStandardsController(AdminProviderRequirementStandardService service) : ControllerBase
{
    [HttpGet]
    public Task<AdminProviderRequirementStandardsResponse> Get(CancellationToken cancellationToken) => service.GetAsync(cancellationToken);
    [HttpPost("types")]
    public Task<ActionResult<AdminProviderRequirementTypeResponse>> CreateType(SaveAdminProviderRequirementTypeRequest request, CancellationToken cancellationToken) => Execute(() => service.CreateTypeAsync(request, ActorId(), cancellationToken), StatusCodes.Status201Created);
    [HttpPut("types/{code}")]
    public Task<ActionResult<AdminProviderRequirementTypeResponse>> UpdateType(string code, SaveAdminProviderRequirementTypeRequest request, CancellationToken cancellationToken) => Execute(() => service.UpdateTypeAsync(code, request, ActorId(), cancellationToken));
    [HttpPost("definitions")]
    public Task<ActionResult<AdminProviderRequirementDefinitionResponse>> CreateDefinition(SaveAdminProviderRequirementDefinitionRequest request, CancellationToken cancellationToken) => Execute(() => service.CreateDefinitionAsync(request, ActorId(), cancellationToken), StatusCodes.Status201Created);
    [HttpPut("definitions/{id:guid}")]
    public Task<ActionResult<AdminProviderRequirementDefinitionResponse>> UpdateDefinition(Guid id, SaveAdminProviderRequirementDefinitionRequest request, CancellationToken cancellationToken) => Execute(() => service.UpdateDefinitionAsync(id, request, ActorId(), cancellationToken));
    [HttpPost("document-types")]
    public Task<ActionResult<AdminProviderDocumentTypeResponse>> CreateDocumentType(SaveAdminProviderDocumentTypeRequest request, CancellationToken cancellationToken) => Execute(() => service.CreateDocumentTypeAsync(request, ActorId(), cancellationToken), StatusCodes.Status201Created);
    [HttpPut("document-types/{id:guid}")]
    public Task<ActionResult<AdminProviderDocumentTypeResponse>> UpdateDocumentType(Guid id, SaveAdminProviderDocumentTypeRequest request, CancellationToken cancellationToken) => Execute(() => service.UpdateDocumentTypeAsync(id, request, ActorId(), cancellationToken));
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action, int status = StatusCodes.Status200OK)
    {
        try { return StatusCode(status, await action()); }
        catch (AdminServiceCategoryException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
