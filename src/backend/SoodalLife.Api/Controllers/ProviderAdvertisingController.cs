using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Advertising;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Provider),Route("api/v1/providers/me/advertising-campaigns")]
public sealed class ProviderAdvertisingController(ProviderAdvertisingService service,ProviderAdvertisingRenewalService renewals):ControllerBase
{
    [HttpGet("rates")] public async Task<ActionResult> Rates(CancellationToken token)=>Ok(await service.GetRatesAsync(false,token));
    [HttpGet("areas")] public async Task<ActionResult> Areas(CancellationToken token)=>Ok(await service.GetAreasAsync(token));
    [HttpGet("policy-guide")] public ActionResult Guide()=>Ok(ProviderAdvertisingService.Guide());
    [HttpGet] public async Task<ActionResult> List(CancellationToken token)=>Ok(await service.ProviderListAsync(User,token));
    [HttpGet("{id:guid}")] public async Task<ActionResult> Detail(Guid id,CancellationToken token){var value=await service.ProviderDetailAsync(User,id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"PROVIDER_AD_APPLICATION_NOT_FOUND","광고 신청을 찾을 수 없습니다.")):Ok(value);}
    [HttpPost] public Task<ActionResult> Apply(SaveProviderAdvertisingApplicationRequest request,CancellationToken token)=>Execute(()=>service.ApplyAsync(User,request,token));
    [HttpPut("{id:guid}/resubmit")] public Task<ActionResult> Resubmit(Guid id,SaveProviderAdvertisingApplicationRequest request,CancellationToken token)=>Execute(()=>service.ResubmitAsync(User,id,request,token));
    [HttpPost("{id:guid}/cancel")] public Task<ActionResult> Cancel(Guid id,CancellationToken token)=>Execute(()=>service.CancelAsync(User,id,token));
    [HttpPatch("{id:guid}/auto-renew")] public Task<ActionResult> AutoRenew(Guid id,SetProviderAdvertisingAutoRenewRequest request,CancellationToken token)=>Execute(()=>renewals.SetAsync(User,id,request.Enabled,token));
    [HttpPost("{id:guid}/renewal-consent")] public Task<ActionResult> RenewalConsent(Guid id,CancellationToken token)=>Execute(()=>renewals.ConsentAsync(User,id,token));
    [HttpGet("{id:guid}/renewals")] public async Task<ActionResult> RenewalHistory(Guid id,CancellationToken token)=>Ok(await renewals.HistoryAsync(User,id,token));
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action){try{return Ok(await action());}catch(ProviderAdvertisingException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/provider-campaigns")]
public sealed class AdminProviderCampaignController(ProviderAdvertisingService service):ControllerBase
{
    [HttpGet("rates")] public async Task<ActionResult> Rates(CancellationToken token)=>Ok(await service.GetRatesAsync(true,token));
    [HttpPut("rates/{id:guid}")] public Task<ActionResult> UpdateRate(Guid id,UpdateProviderAdvertisingRateRequest request,CancellationToken token)=>Execute(()=>service.UpdateRateAsync(id,request,token));
    [HttpGet("policy-guide")] public ActionResult Guide()=>Ok(ProviderAdvertisingService.Guide());
    [HttpGet] public async Task<ActionResult> List([FromQuery]string? status,[FromQuery]string? search,CancellationToken token)=>Ok(await service.AdminListAsync(status,search,token));
    [HttpGet("{id:guid}")] public async Task<ActionResult> Detail(Guid id,CancellationToken token){var value=await service.AdminDetailAsync(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"PROVIDER_AD_APPLICATION_NOT_FOUND","광고 신청을 찾을 수 없습니다.")):Ok(value);}
    [HttpPost("{id:guid}/review")] public Task<ActionResult> Review(Guid id,ProviderAdvertisingReviewRequest request,CancellationToken token)=>Execute(()=>service.ReviewAsync(Actor(),id,request,token));
    [HttpPost("{id:guid}/publish")] public Task<ActionResult> Publish(Guid id,ProviderAdvertisingPublishRequest request,CancellationToken token)=>Execute(()=>service.PublishAsync(Actor(),id,request,token));
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action){try{return Ok(await action());}catch(ProviderAdvertisingException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
