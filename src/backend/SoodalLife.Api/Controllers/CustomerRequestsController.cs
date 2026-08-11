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
            return StatusCode(exception.StatusCode, new ApiErrorResponse(
                exception.BusinessCode,
                exception.Message,
                exception.FieldErrors,
                HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("{requestId:guid}")]
    public async Task<ActionResult<ServiceRequestDetailResponse>> Update(
        Guid requestId,
        UpdateServiceRequestDraftInput input,
        CancellationToken cancellationToken)
    {
        try { return Ok(await requestService.UpdateDraftAsync(User, requestId, input, cancellationToken)); }
        catch (RequestValidationException exception) { return Error(exception); }
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
            return StatusCode(exception.StatusCode, new ApiErrorResponse(
                exception.BusinessCode,
                exception.Message,
                exception.FieldErrors,
                HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("{requestId:guid}/cancel")]
    public async Task<ActionResult<ServiceRequestDetailResponse>> Cancel(
        Guid requestId,
        CancelServiceRequestInput input,
        CancellationToken cancellationToken)
    {
        try { return Ok(await requestService.CancelAsync(User, requestId, input, cancellationToken)); }
        catch (RequestValidationException exception) { return Error(exception); }
    }

    [HttpPost("{requestId:guid}/files")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public async Task<ActionResult<ServiceRequestFileResponse>> UploadFile(
        Guid requestId,
        [FromForm] Guid? requestFieldId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        try { return Ok(await requestService.UploadFileAsync(User, requestId, requestFieldId, file, cancellationToken)); }
        catch (RequestValidationException exception) { return Error(exception); }
    }

    [HttpDelete("{requestId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid requestId, Guid fileId, CancellationToken cancellationToken)
    {
        try { await requestService.DeleteFileAsync(User, requestId, fileId, cancellationToken); return NoContent(); }
        catch (RequestValidationException exception) { return Error(exception); }
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<ServiceRequestDetailResponse>> GetById(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requestService.GetMineByIdAsync(User, requestId, cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }

    private ObjectResult Error(RequestValidationException exception) => StatusCode(exception.StatusCode, new ApiErrorResponse(
        exception.BusinessCode,
        exception.Message,
        exception.FieldErrors,
        HttpContext.TraceIdentifier));
}

[ApiController]
[Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
[Route("api/v1/requests")]
public sealed class RequestFilesController(CustomerServiceRequestService requestService) : ControllerBase
{
    [HttpGet("{requestId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> Download(Guid requestId, Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await requestService.OpenFileAsync(User, requestId, fileId, cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
        }
        catch (RequestValidationException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponse(
                exception.BusinessCode, exception.Message, exception.FieldErrors, HttpContext.TraceIdentifier));
        }
    }
}
