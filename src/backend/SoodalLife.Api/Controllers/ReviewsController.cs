using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Reviews;

namespace SoodalLife.Api.Controllers;

[ApiController,Route("api/v1")]
public sealed class ReviewsController(ReviewService service):ControllerBase
{
    [Authorize(Roles=RoleCodes.Customer),HttpPost("customers/me/transactions/{transactionId:guid}/review")]
    public Task<ActionResult<ReviewResponse>> Create(Guid transactionId,CreateReviewRequest input,CancellationToken token)=>Run(()=>service.CreateAsync(User,transactionId,input,token));
    [AllowAnonymous,HttpGet("providers/{providerId:guid}/reviews")]
    public Task<ActionResult<PublicReviewListResponse>> Public(Guid providerId,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(()=>service.PublicList(providerId,page,pageSize,token));
    [AllowAnonymous,HttpGet("providers/{providerId:guid}/review-statistics")]
    public Task<ActionResult<ProviderReviewStatisticsResponse>> Statistics(Guid providerId,CancellationToken token)=>Run(()=>service.Statistics(providerId,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("providers/me/reviews/{reviewId:guid}/reply")]
    public Task<ActionResult<ReviewReplyResponse>> Reply(Guid reviewId,CreateProviderReplyRequest input,CancellationToken token)=>Run(()=>service.Reply(User,reviewId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(ReviewBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
