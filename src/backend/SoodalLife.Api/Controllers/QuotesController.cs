using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Quotes;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class QuotesController(QuoteService quoteService) : ControllerBase
{
    [Authorize(Roles = RoleCodes.Provider)]
    [HttpGet("providers/me/requests/{requestId:guid}/quote")]
    public async Task<ActionResult<QuoteDetailResponse>> GetMine(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await quoteService.GetProviderQuoteAsync(User, requestId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpGet("providers/me/requests/{requestId:guid}/quote-submission-readiness")]
    public Task<ActionResult<QuoteSubmissionReadinessResponse>> GetSubmissionReadiness(Guid requestId, CancellationToken cancellationToken) =>
        Execute(() => quoteService.GetSubmissionReadinessAsync(User, requestId, cancellationToken));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("requests/{requestId:guid}/quotes")]
    public Task<ActionResult<QuoteDetailResponse>> Create(Guid requestId, SaveQuoteRevisionInput input, CancellationToken cancellationToken) =>
        Execute(() => quoteService.CreateQuoteAsync(User, requestId, input, cancellationToken));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("quotes/{quoteId:guid}/revisions")]
    public Task<ActionResult<QuoteDetailResponse>> AddRevision(Guid quoteId, SaveQuoteRevisionInput input, CancellationToken cancellationToken) =>
        Execute(() => quoteService.AddRevisionAsync(User, quoteId, input, cancellationToken));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpPost("quotes/{quoteId:guid}/submit")]
    public Task<ActionResult<QuoteDetailResponse>> Submit(Guid quoteId, CancellationToken cancellationToken) =>
        Execute(() => quoteService.SubmitAsync(User, quoteId, cancellationToken));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpGet("requests/{requestId:guid}/quotes")]
    public Task<ActionResult<IReadOnlyList<QuoteListItemResponse>>> GetForRequest(Guid requestId, CancellationToken cancellationToken) =>
        Execute(() => quoteService.GetCustomerQuotesAsync(User, requestId, cancellationToken));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpGet("quotes/{quoteId:guid}")]
    public Task<ActionResult<QuoteDetailResponse>> GetDetail(Guid quoteId, CancellationToken cancellationToken) =>
        Execute(() => quoteService.GetCustomerQuoteDetailAsync(User, quoteId, cancellationToken));

    [Authorize(Roles = RoleCodes.Customer)]
    [HttpPost("quotes/{quoteId:guid}/accept")]
    public Task<ActionResult<AcceptQuoteResponse>> Accept(Guid quoteId, CancellationToken cancellationToken) =>
        Execute(() => quoteService.AcceptAsync(User, quoteId, cancellationToken));

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (QuoteBusinessException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorResponse(
                exception.BusinessCode,
                exception.Message,
                exception.FieldErrors,
                HttpContext.TraceIdentifier));
        }
    }
}
