using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/internal/requests")]
public sealed class MatchingController(RequestMatchingService matchingService) : ControllerBase
{
    [HttpPost("{requestId:guid}/match-and-dispatch")]
    public async Task<ActionResult<MatchAndDispatchResult>> MatchAndDispatch(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await matchingService.MatchAndDispatchAsync(requestId, cancellationToken));
        }
        catch (MatchingException exception)
        {
            return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
        }
    }
}

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin/requests")]
public sealed class AdminMatchingController(RequestMatchingService matchingService) : ControllerBase
{
    [HttpGet("{requestId:guid}/candidates")]
    public async Task<ActionResult<IReadOnlyList<DispatchCandidateResponse>>> GetCandidates(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await matchingService.GetCandidatesAsync(requestId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}

[ApiController]
[Authorize(Roles = RoleCodes.Provider)]
[Route("api/v1/providers/me/matched-requests")]
public sealed class ProviderMatchedRequestsController(RequestMatchingService matchingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderMatchedRequestListItem>>> Get(CancellationToken cancellationToken) =>
        Ok(await matchingService.GetInboxAsync(User, cancellationToken));

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<ProviderMatchedRequestDetail>> GetDetail(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await matchingService.GetInboxDetailAsync(User, requestId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
