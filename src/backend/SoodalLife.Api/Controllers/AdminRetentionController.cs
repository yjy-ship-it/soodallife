using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/system/retention")]
public sealed class AdminRetentionController(DataRetentionService service):ControllerBase
{
    [HttpPut("{id:guid}")] public async Task<ActionResult> Update(Guid id,AdminRetentionPolicyUpdateRequest request,CancellationToken token){try{await service.UpdateAsync(Actor(),id,Reauth(),request,token);return NoContent();}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
    [HttpPost("{id:guid}/assess")] public async Task<ActionResult<AdminRetentionPolicyItem>> Assess(Guid id,CancellationToken token)=>await Run(()=>service.RunAssessmentAsync(Actor(),id,Reauth(),token));
    private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new AdminSystemException("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);
    private string Reauth()=>Request.Headers["X-Admin-Reauth-Token"].FirstOrDefault()??string.Empty;
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
