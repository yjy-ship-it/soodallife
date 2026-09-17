using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Reviews;

namespace SoodalLife.Api.Controllers;

[ApiController,Route("api/v1")]
public sealed class ReviewConversationController(ReviewConversationService service):ControllerBase
{
    [Authorize(Roles=RoleCodes.Customer),HttpGet("customers/me/review-conversations")] public Task<ActionResult<IReadOnlyList<ReviewConversationResponse>>> CustomerList(CancellationToken token)=>Run(()=>service.CustomerList(User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpGet("customers/me/review-conversations/unread-count")] public Task<ActionResult<ReviewConversationUnreadCountResponse>> CustomerUnread(CancellationToken token)=>Run(()=>service.CustomerUnread(User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("customers/me/review-conversations/read")] public Task<ActionResult> CustomerRead(CancellationToken token)=>RunEmpty(()=>service.CustomerReadAll(User,token));
    [Authorize(Roles=RoleCodes.Customer),HttpPost("customers/me/reviews/{reviewId:guid}/comments")] public Task<ActionResult<ReviewCommentResponse>> CustomerPost(Guid reviewId,CreateReviewCommentRequest input,CancellationToken token)=>Run(()=>service.CustomerPost(User,reviewId,input,token));
    [Authorize(Roles=RoleCodes.Provider),HttpGet("providers/me/review-conversations")] public Task<ActionResult<IReadOnlyList<ReviewConversationResponse>>> ProviderList(CancellationToken token)=>Run(()=>service.ProviderList(User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpGet("providers/me/review-conversations/unread-count")] public Task<ActionResult<ReviewConversationUnreadCountResponse>> ProviderUnread(CancellationToken token)=>Run(()=>service.ProviderUnread(User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("providers/me/review-conversations/read")] public Task<ActionResult> ProviderRead(CancellationToken token)=>RunEmpty(()=>service.ProviderReadAll(User,token));
    [Authorize(Roles=RoleCodes.Provider),HttpPost("providers/me/reviews/{reviewId:guid}/comments")] public Task<ActionResult<ReviewCommentResponse>> ProviderPost(Guid reviewId,CreateReviewCommentRequest input,CancellationToken token)=>Run(()=>service.ProviderPost(User,reviewId,input,token));
    [Authorize(Roles=RoleCodes.Admin),HttpGet("admin/reviews/{reviewId:guid}/comments")] public Task<ActionResult<IReadOnlyList<ReviewCommentResponse>>> AdminList(Guid reviewId,CancellationToken token)=>Run(()=>service.AdminList(reviewId,token));
    [Authorize(Roles=RoleCodes.Admin),HttpPost("admin/reviews/{reviewId:guid}/comments/{commentId:guid}/moderation")] public Task<ActionResult<ReviewCommentResponse>> Moderate(Guid reviewId,Guid commentId,ModerateReviewCommentRequest input,CancellationToken token)=>Run(()=>service.Moderate(User,reviewId,commentId,input,token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(ReviewBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message,field=e.Field});}}
    private async Task<ActionResult> RunEmpty(Func<Task> action){try{await action();return NoContent();}catch(ReviewBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message,field=e.Field});}}
}
