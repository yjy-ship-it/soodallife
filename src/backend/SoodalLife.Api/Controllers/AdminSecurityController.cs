using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/security")]
public sealed class AdminSecurityController(AdminSecurityService service):ControllerBase
{
    [HttpGet("accounts")] public Task<IReadOnlyList<AdminSecurityAccountResponse>> Accounts(CancellationToken token)=>service.AccountsAsync(token);
    [HttpPost("mfa/enrollment")] public async Task<ActionResult<AdminMfaEnrollmentResponse>> BeginMfa(CancellationToken token)=>await Run(()=>service.BeginMfaAsync(Actor(),token));
    [HttpPost("mfa/confirm")] public async Task<ActionResult> ConfirmMfa(AdminMfaConfirmRequest request,CancellationToken token){try{await service.ConfirmMfaAsync(Actor(),request.Code,token);return NoContent();}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
    [HttpPost("reauthenticate")] public async Task<ActionResult<AdminReauthenticateResponse>> Reauthenticate(AdminReauthenticateRequest request,CancellationToken token)=>await Run(()=>service.ReauthenticateAsync(Actor(),request,token));
    [HttpPost("accounts")] public async Task<ActionResult<AdminSecurityAccountResponse>> Create(AdminAccountCreateRequest request,CancellationToken token)=>await Run(()=>service.CreateAccountAsync(Actor(),Reauth(),request,token));
    [HttpPut("accounts/{id:guid}")] public async Task<ActionResult> Update(Guid id,AdminAccountUpdateRequest request,CancellationToken token){try{await service.UpdateAccountAsync(Actor(),id,Reauth(),request,token);return NoContent();}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
    private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new AdminSystemException("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);
    private string Reauth()=>Request.Headers["X-Admin-Reauth-Token"].FirstOrDefault()??string.Empty;
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
