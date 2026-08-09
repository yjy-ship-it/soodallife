using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.ServiceRequests;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Customer)]
[Route("api/v1/requests")]
public sealed class CustomerRequestsController(CustomerServiceRequestService requestService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ServiceRequestCreatedResponse>> Create(
        CreateServiceRequestInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await requestService.CreateAsync(User, input, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { requestId = response.Id }, response);
        }
        catch (RequestValidationException exception)
        {
            return BadRequest(new ApiErrorResponse(
                exception.BusinessCode,
                exception.Message,
                exception.FieldErrors,
                HttpContext.TraceIdentifier));
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestListItemResponse>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await requestService.GetMineAsync(User, cancellationToken));

    [HttpPost("{requestId:guid}/publish")]
    public async Task<ActionResult<PublishServiceRequestResponse>> Publish(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await requestService.PublishAsync(User, requestId, cancellationToken));
        }
        catch (RequestValidationException exception)
        {
            return BadRequest(new ApiErrorResponse(
                exception.BusinessCode,
                exception.Message,
                exception.FieldErrors,
                HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<ServiceRequestDetailResponse>> GetById(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requestService.GetMineByIdAsync(User, requestId, cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }
}
