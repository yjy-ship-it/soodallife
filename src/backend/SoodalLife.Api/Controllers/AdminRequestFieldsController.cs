using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/request-fields")]
public sealed class AdminRequestFieldSummaryController(AdminRequestFieldService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminRequestFieldSummaryResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await service.GetSummaryAsync(cancellationToken));
}

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/service-categories/services/{serviceId:guid}/request-fields")]
public sealed class AdminRequestFieldsController(AdminRequestFieldService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminRequestFieldResponse>>> GetFields(
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var fields = await service.GetFieldsAsync(serviceId, cancellationToken);
        return fields is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_SERVICE_NOT_FOUND", "서비스를 찾을 수 없습니다."))
            : Ok(fields);
    }

    [HttpGet("{fieldId:guid}")]
    public async Task<ActionResult<AdminRequestFieldResponse>> GetField(
        Guid serviceId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        var field = await service.GetFieldAsync(serviceId, fieldId, cancellationToken);
        return field is null
            ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_REQUEST_FIELD_NOT_FOUND", "요청항목을 찾을 수 없습니다."))
            : Ok(field);
    }

    [HttpPut("{fieldId:guid}/definition")]
    public async Task<ActionResult<AdminRequestFieldResponse>> UpdateDefinition(
        Guid serviceId,
        Guid fieldId,
        UpdateAdminRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => service.UpdateDefinitionAsync(
            serviceId,
            fieldId,
            request,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken));

    [HttpPut("{fieldId:guid}/assignment")]
    public async Task<ActionResult<AdminRequestFieldResponse>> UpdateAssignment(
        Guid serviceId,
        Guid fieldId,
        UpdateAdminRequestFieldAssignmentRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => service.UpdateAssignmentAsync(
            serviceId,
            fieldId,
            request,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken));

    [HttpPut("{fieldId:guid}/options/{optionId:guid}")]
    public async Task<ActionResult<AdminRequestFieldResponse>> UpdateOption(
        Guid serviceId,
        Guid fieldId,
        Guid optionId,
        UpdateAdminRequestFieldOptionRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => service.UpdateOptionAsync(
            serviceId,
            fieldId,
            optionId,
            request,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken));

    private async Task<ActionResult<AdminRequestFieldResponse>> ExecuteAsync(Func<Task<AdminRequestFieldResponse>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (AdminServiceCategoryException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}
