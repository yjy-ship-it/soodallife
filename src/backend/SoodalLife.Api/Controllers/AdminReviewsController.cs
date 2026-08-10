using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Reviews;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/reviews")]
public sealed class AdminReviewsController(AdminReviewService service):ControllerBase
{
    [HttpGet] public Task<ActionResult<AdminReviewListResponse>> Search([FromQuery]string? search,[FromQuery]string? verificationStatus,[FromQuery]string? visibilityStatus,[FromQuery]DateOnly? submittedFrom,[FromQuery]DateOnly? submittedTo,[FromQuery]Guid? providerId,[FromQuery]Guid? serviceId,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(()=>service.Search(search,verificationStatus,visibilityStatus,submittedFrom,submittedTo,providerId,serviceId,page,pageSize,token));
    [HttpGet("{id:guid}")] public async Task<ActionResult<AdminReviewDetailResponse>> Get(Guid id,CancellationToken token){var value=await service.Get(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"REVIEW_NOT_FOUND","리뷰를 찾을 수 없습니다.")):Ok(value);}
    [HttpPost("{id:guid}/visibility")] public Task<ActionResult<AdminReviewDetailResponse>> Visibility(Guid id,ChangeReviewVisibilityRequest input,CancellationToken token)=>Run(()=>service.ChangeVisibility(id,input,Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(ReviewBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
