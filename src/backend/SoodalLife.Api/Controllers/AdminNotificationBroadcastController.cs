using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Notifications;

namespace SoodalLife.Api.Controllers;
[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/notification-broadcasts")]
public sealed class AdminNotificationBroadcastController(NotificationBroadcastService service):ControllerBase
{
    [HttpGet]public Task<ActionResult<PagedNotificationBroadcasts>> Search([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(()=>service.Search(page,pageSize,token));
    [HttpGet("{id:guid}")]public Task<ActionResult<NotificationBroadcastItem>> Detail(Guid id,CancellationToken token)=>Run(()=>service.Detail(id,token));
    [HttpPost]public Task<ActionResult<NotificationBroadcastItem>> Create(SaveNotificationBroadcastRequest input,CancellationToken token)=>Run(()=>service.Create(input,User,token));
    [HttpPut("{id:guid}")]public Task<ActionResult<NotificationBroadcastItem>> Update(Guid id,SaveNotificationBroadcastRequest input,CancellationToken token)=>Run(()=>service.Update(id,input,User,token));
    [HttpPost("{id:guid}/preview")]public Task<ActionResult<NotificationBroadcastPreview>> Preview(Guid id,CancellationToken token)=>Run(()=>service.Preview(id,User,token));
    [HttpPost("{id:guid}/confirm")]public Task<ActionResult<NotificationBroadcastItem>> Confirm(Guid id,ConfirmNotificationBroadcastRequest input,CancellationToken token)=>Run(()=>service.Confirm(id,input,User,Request.Headers["X-Admin-Reauth-Token"].FirstOrDefault(),token));
    [HttpPost("{id:guid}/cancel")]public Task<ActionResult<NotificationBroadcastItem>> Cancel(Guid id,CancelNotificationBroadcastRequest input,CancellationToken token)=>Run(()=>service.Cancel(id,input,User,Request.Headers["X-Admin-Reauth-Token"].FirstOrDefault(),token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(NotificationBusinessException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.Code,e.Message));}catch(AdminSystemException e){return StatusCode(e.StatusCode,ApiErrorResponse.Create(HttpContext,e.BusinessCode,e.Message));}}
}
