using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Notifications;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/notifications")]
public sealed class AdminNotificationController(NotificationManagementService service,NotificationOperationsService operations):ControllerBase
{
    [HttpGet("summary")]public Task<ActionResult<NotificationAdminSummary>> Summary(CancellationToken t)=>Run(()=>service.Summary(t));
    [HttpGet("templates")]public Task<ActionResult<IReadOnlyList<NotificationTemplateResponse>>> Templates([FromQuery]string? search,CancellationToken t)=>Run(()=>service.Templates(search,t));
    [HttpGet("templates/search")]public Task<ActionResult<PagedNotificationResponse<NotificationTemplateResponse>>> SearchTemplates([FromQuery]string? search,[FromQuery]string? eventType,[FromQuery]string? audience,[FromQuery]string? channel,[FromQuery]bool? active,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken t=default)=>Run(()=>operations.SearchTemplates(search,eventType,audience,channel,active,page,pageSize,t));
    [HttpPost("templates/preview")]public ActionResult<NotificationTemplatePreviewResponse> Preview(NotificationTemplatePreviewRequest input)=>Ok(operations.Preview(input));
    [HttpGet("templates/{id:guid}")]public Task<ActionResult<NotificationTemplateResponse>> Template(Guid id,CancellationToken t)=>Run(()=>service.Template(id,t));
    [HttpPost("templates")]public Task<ActionResult<NotificationTemplateResponse>> Create(CreateNotificationTemplateRequest input,CancellationToken t)=>Run(()=>service.CreateTemplate(input,User,t));
    [HttpPut("templates/{id:guid}")]public Task<ActionResult<NotificationTemplateResponse>> Update(Guid id,UpdateNotificationTemplateRequest input,CancellationToken t)=>Run(()=>service.UpdateTemplate(id,input,User,t));
    [HttpPatch("templates/{id:guid}/status")]public Task<ActionResult<NotificationTemplateResponse>> Status(Guid id,SetNotificationTemplateStatusRequest input,CancellationToken t)=>Run(()=>service.SetTemplateStatus(id,input,User,t));
    [HttpGet("deliveries")]public Task<ActionResult<IReadOnlyList<NotificationDeliveryAdminItem>>> Deliveries([FromQuery]string? status,CancellationToken t)=>Run(()=>service.Deliveries(status,t));
    [HttpGet("deliveries/search")]public Task<ActionResult<PagedNotificationResponse<NotificationDeliveryAdminItem>>> SearchDeliveries([FromQuery]string? search,[FromQuery]string? status,[FromQuery]string? channel,[FromQuery]string? eventType,[FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken t=default)=>Run(()=>operations.SearchDeliveries(search,status,channel,eventType,from,to,page,pageSize,t));
    [HttpGet("deliveries/{id:guid}")]public Task<ActionResult<NotificationDeliveryDetailResponse>> Delivery(Guid id,CancellationToken t)=>Run(()=>operations.Delivery(id,t));
    [HttpPost("deliveries/{id:guid}/retry")]public Task<ActionResult<NotificationDeliveryAdminItem>> Retry(Guid id,CancellationToken t)=>Run(()=>service.Retry(id,User,t));
    [HttpPost("outbox/{id:guid}/process")]public Task<ActionResult<ProcessOutboxNotificationResponse>> Process(Guid id,CancellationToken t)=>Run(()=>service.ProcessOutbox(id,t));
    [HttpGet("channels")]public Task<ActionResult<IReadOnlyList<NotificationChannelSettingResponse>>> Channels(CancellationToken t)=>Run(()=>operations.ChannelsAsync(t));
    [HttpPut("channels/{channel}")]public Task<ActionResult<NotificationChannelSettingResponse>> Channel(string channel,UpdateNotificationChannelSettingRequest input,CancellationToken t)=>Run(()=>operations.UpdateChannel(channel,input,User,t));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> f){try{return Ok(await f());}catch(NotificationBusinessException e){return StatusCode(e.StatusCode,new{code=e.Code,message=e.Message});}}
}

[ApiController,Authorize,Route("api/v1/notifications")]
public sealed class NotificationController(NotificationManagementService service):ControllerBase
{
    [HttpGet]public Task<ActionResult<IReadOnlyList<NotificationListItem>>> List([FromQuery]string view="ALL",CancellationToken t=default)=>Run(()=>service.Mine(User,view,t));
    [HttpGet("unread-count")]public Task<ActionResult<NotificationUnreadCountResponse>> Unread(CancellationToken t)=>Run(()=>service.Unread(User,t));
    [HttpGet("{id:guid}")]public Task<ActionResult<NotificationListItem>> Detail(Guid id,CancellationToken t)=>Run(()=>service.Detail(id,User,t));
    [HttpPost("{id:guid}/read")]public Task<ActionResult> Read(Guid id,CancellationToken t)=>RunEmpty(()=>service.Read(id,User,t));
    [HttpPost("read-all")]public Task<ActionResult> ReadAll(CancellationToken t)=>RunEmpty(()=>service.ReadAll(User,t));
    [HttpPost("{id:guid}/archive")]public Task<ActionResult> Archive(Guid id,CancellationToken t)=>RunEmpty(()=>service.Archive(id,User,t));
    [HttpGet("preferences")]public Task<ActionResult<IReadOnlyList<NotificationPreferenceResponse>>> Preferences(CancellationToken t)=>Run(()=>service.Preferences(User,t));
    [HttpPut("preferences")]public Task<ActionResult<NotificationPreferenceResponse>> Preference(UpdateNotificationPreferenceRequest input,CancellationToken t)=>Run(()=>service.SetPreference(input,User,t));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> f){try{return Ok(await f());}catch(NotificationBusinessException e){return StatusCode(e.StatusCode,new{code=e.Code,message=e.Message});}}
    private async Task<ActionResult> RunEmpty(Func<Task> f){try{await f();return NoContent();}catch(NotificationBusinessException e){return StatusCode(e.StatusCode,new{code=e.Code,message=e.Message});}}
}
