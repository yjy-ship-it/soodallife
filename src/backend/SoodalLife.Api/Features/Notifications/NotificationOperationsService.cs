using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Notifications;

public sealed partial class NotificationOperationsService(SoodalLifeDbContext db,IEnumerable<INotificationChannelSender> senders)
{
    private static readonly string[] Channels=["WEB","KAKAO","SMS","EMAIL","PUSH"];
    private static readonly IReadOnlyDictionary<string,string> Names=new Dictionary<string,string>{{"WEB","WEB 내부 알림"},{"KAKAO","카카오 알림톡"},{"SMS","SMS 문자"},{"EMAIL","이메일"},{"PUSH","Push 알림"}};

    public async Task<PagedNotificationResponse<NotificationTemplateResponse>> SearchTemplates(string? search,string? eventType,string? audience,string? channel,bool? active,int page,int pageSize,CancellationToken token)
    {
        (page,pageSize)=Page(page,pageSize);var q=db.NotificationTemplates.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim();q=q.Where(x=>x.TemplateCode.Contains(s)||x.Name.Contains(s)||x.TitleTemplate.Contains(s));}
        if(!string.IsNullOrWhiteSpace(eventType)){var value=eventType.Trim().ToUpperInvariant();q=q.Where(x=>x.EventTypeCode.Contains(value));}
        if(!string.IsNullOrWhiteSpace(audience)){var value=audience.Trim().ToUpperInvariant();q=q.Where(x=>x.AudienceTypeCode==value);}
        if(!string.IsNullOrWhiteSpace(channel)){var value=channel.Trim().ToUpperInvariant();q=q.Where(x=>x.ChannelCode==value);}
        if(active.HasValue)q=q.Where(x=>x.IsActive==active.Value);var count=await q.CountAsync(token);
        var rows=await q.OrderBy(x=>x.EventTypeCode).ThenBy(x=>x.TemplateCode).ThenBy(x=>x.ChannelCode).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(Template).ToArray(),page,pageSize,count,Pages(count,pageSize));
    }

    public NotificationTemplatePreviewResponse Preview(NotificationTemplatePreviewRequest input)
    {
        var values=input.Variables??new Dictionary<string,string>();var variables=Variables(input.TitleTemplate).Concat(Variables(input.BodyTemplate)).Distinct().ToArray();
        string Render(string value)=>VariableRegex().Replace(value,m=>values.TryGetValue(m.Groups[1].Value,out var replacement)&&!string.IsNullOrWhiteSpace(replacement)?replacement:$"[{m.Groups[1].Value}]");
        return new(Render(input.TitleTemplate),Render(input.BodyTemplate),variables);
    }

    public async Task<PagedNotificationResponse<NotificationDeliveryAdminItem>> SearchDeliveries(string? search,string? status,string? channel,string? eventType,DateTime? from,DateTime? to,int page,int pageSize,CancellationToken token)
    {
        (page,pageSize)=Page(page,pageSize);var q=from d in db.NotificationDeliveries.AsNoTracking()join n in db.Notifications.AsNoTracking()on d.NotificationId equals n.Id join r in db.NotificationRecipients.AsNoTracking()on d.NotificationRecipientId equals(long?)r.Id join u in db.Users.AsNoTracking()on r.UserId equals u.Id select new{d,n,r,u};
        if(!string.IsNullOrWhiteSpace(status)){var value=status.Trim().ToUpperInvariant();q=q.Where(x=>x.d.StatusCode==value);}
        if(!string.IsNullOrWhiteSpace(channel)){var value=channel.Trim().ToUpperInvariant();q=q.Where(x=>x.d.ChannelCode==value);}
        if(!string.IsNullOrWhiteSpace(eventType)){var value=eventType.Trim().ToUpperInvariant();q=q.Where(x=>x.n.TypeCode.Contains(value));}
        if(from.HasValue)q=q.Where(x=>x.d.CreatedAt>=from.Value.ToUniversalTime());if(to.HasValue)q=q.Where(x=>x.d.CreatedAt<to.Value.ToUniversalTime().AddDays(1));
        if(!string.IsNullOrWhiteSpace(search)){var value=search.Trim();q=q.Where(x=>x.u.LoginId.Contains(value)||x.n.Title.Contains(value)||(x.n.TemplateCodeSnapshot!=null&&x.n.TemplateCodeSnapshot.Contains(value)));}
        var count=await q.CountAsync(token);var rows=await q.OrderByDescending(x=>x.d.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(x=>Item(x.d,x.n,x.r,x.u)).ToArray(),page,pageSize,count,Pages(count,pageSize));
    }

    public async Task<NotificationDeliveryDetailResponse> Delivery(Guid id,CancellationToken token)
    {
        var row=await(from d in db.NotificationDeliveries.AsNoTracking()join n in db.Notifications.AsNoTracking()on d.NotificationId equals n.Id join r in db.NotificationRecipients.AsNoTracking()on d.NotificationRecipientId equals(long?)r.Id join u in db.Users.AsNoTracking()on r.UserId equals u.Id where d.PublicId==id select new{d,n,r,u}).SingleOrDefaultAsync(token)??throw Error(404,"DELIVERY_NOT_FOUND","발송 건을 찾을 수 없습니다.");
        var attempts=await db.NotificationDeliveryAttempts.AsNoTracking().Where(x=>x.NotificationDeliveryId==row.d.Id).OrderByDescending(x=>x.AttemptNo).Select(x=>new NotificationDeliveryAttemptItem(x.AttemptNo,x.StartedAt,x.CompletedAt,x.ResultCode,x.ProviderResponseCode,x.FailureReason)).ToListAsync(token);
        return new(Item(row.d,row.n,row.r,row.u),row.n.Title,row.n.Body,row.n.TemplateCodeSnapshot,attempts);
    }

    public async Task<IReadOnlyList<NotificationChannelSettingResponse>> ChannelsAsync(CancellationToken token)
    {
        await EnsureChannels(token);var rows=await db.NotificationChannelSettings.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(token);return rows.Select(Channel).ToArray();
    }

    public async Task<NotificationChannelSettingResponse> UpdateChannel(string channel,UpdateNotificationChannelSettingRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var code=Normalize(channel);await EnsureChannels(token);var row=await db.NotificationChannelSettings.SingleAsync(x=>x.ChannelCode==code,token);var actor=await UserId(principal,token);var mode=input.OperationModeCode.Trim().ToUpperInvariant();if(mode is not("DISABLED"or"TEST"or"PRODUCTION"))throw Error(409,"CHANNEL_MODE_INVALID","채널 운영 모드를 확인해 주세요.");
        if(input.BatchSize is<1 or>500||input.MaxAttempts is<1 or>20||input.BaseRetrySeconds<5||input.MaxRetrySeconds<input.BaseRetrySeconds||input.SendWindowStartHour is<0 or>23||input.SendWindowEndHour is<1 or>24)throw Error(409,"CHANNEL_SETTING_INVALID","배치·재시도·발송시간 설정을 확인해 주세요.");
        var configured=senders.Any(x=>x.Supports(code)&&x.IsConfigured(code));if(input.IsEnabled&&!configured)throw Error(409,"CHANNEL_ADAPTER_NOT_CONFIGURED","외부 업체 계약과 서버 인증키 등록 후 채널을 활성화할 수 있습니다.");
        if(!string.IsNullOrWhiteSpace(input.RowVersion)){try{db.Entry(row).Property(x=>x.RowVersion).OriginalValue=Convert.FromBase64String(input.RowVersion);}catch(FormatException){throw Error(409,"ROW_VERSION_INVALID","변경 버전 값이 올바르지 않습니다.");}}
        var before=new{row.IsEnabled,row.OperationModeCode,row.ProviderCode,row.SenderIdentity,row.BatchSize,row.MaxAttempts};row.IsEnabled=input.IsEnabled;row.OperationModeCode=input.IsEnabled?mode:"DISABLED";row.ProviderCode=Clean(input.ProviderCode,50);row.SenderIdentity=Clean(input.SenderIdentity,200);row.ReplyTo=Clean(input.ReplyTo,300);row.BatchSize=input.BatchSize;row.MaxAttempts=input.MaxAttempts;row.BaseRetrySeconds=input.BaseRetrySeconds;row.MaxRetrySeconds=input.MaxRetrySeconds;row.SendWindowStartHour=input.SendWindowStartHour;row.SendWindowEndHour=input.SendWindowEndHour;row.UpdatedAt=DateTime.UtcNow;row.UpdatedByUserId=actor;
        db.AuditLogs.Add(new(){OccurredAt=DateTime.UtcNow,ActorUserId=actor,ActorRoleCode="ADMIN",ActionCode="NOTIFICATION_CHANNEL_SETTING_UPDATED",EntityType="NotificationChannelSetting",EntityPublicId=row.PublicId,ResultCode="SUCCESS",BeforeJson=System.Text.Json.JsonSerializer.Serialize(before),AfterJson=System.Text.Json.JsonSerializer.Serialize(new{row.IsEnabled,row.OperationModeCode,row.ProviderCode,row.SenderIdentity,row.BatchSize,row.MaxAttempts})});
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw Error(409,"CONCURRENCY_CONFLICT","다른 관리자가 설정을 변경했습니다. 새로고침 후 다시 시도해 주세요.");}return Channel(row);
    }

    private NotificationChannelSettingResponse Channel(NotificationChannelSetting x){var configured=senders.Any(s=>s.Supports(x.ChannelCode)&&s.IsConfigured(x.ChannelCode));var status=x.ChannelCode=="WEB"?"사용 가능":configured?x.IsEnabled?"사용 중":"연동됨·중지":"업체 계약·인증키 대기";return new(x.ChannelCode,x.DisplayName,x.IsEnabled,x.OperationModeCode,x.ProviderCode,x.SenderIdentity,x.ReplyTo,x.BatchSize,x.MaxAttempts,x.BaseRetrySeconds,x.MaxRetrySeconds,x.SendWindowStartHour,x.SendWindowEndHour,configured,status,Convert.ToBase64String(x.RowVersion));}
    private async Task EnsureChannels(CancellationToken token){if(await db.NotificationChannelSettings.AnyAsync(token))return;var now=DateTime.UtcNow;foreach(var code in Channels)db.NotificationChannelSettings.Add(new(){ChannelCode=code,DisplayName=Names[code],IsEnabled=code=="WEB",OperationModeCode=code=="WEB"?"PRODUCTION":"DISABLED",BatchSize=50,MaxAttempts=5,BaseRetrySeconds=30,MaxRetrySeconds=900,SendWindowStartHour=0,SendWindowEndHour=24,CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync(token);}
    private static NotificationDeliveryAdminItem Item(NotificationDelivery d,Notification n,NotificationRecipient r,User u)=>new(d.PublicId,Mask(u.LoginId),r.RecipientRoleCode,d.ChannelCode,n.TypeCode,d.StatusCode,d.ScheduledAt,d.CreatedAt,d.ErrorCode,d.ErrorMessage,d.RetryCount,d.AttemptedAt);
    private static NotificationTemplateResponse Template(NotificationTemplate x)=>new(x.PublicId,x.TemplateCode,x.Name,x.Description,x.AudienceTypeCode,x.EventTypeCode,x.ChannelCode,x.TitleTemplate,x.BodyTemplate,System.Text.Json.JsonSerializer.Deserialize<string[]>(x.AllowedVariablesJson)??[],x.IsRequiredBusinessNotice,x.IsMarketing,x.IsActive,x.EffectiveFrom,x.EffectiveTo,Convert.ToBase64String(x.RowVersion));
    private static(int,int)Page(int page,int size)=>(Math.Max(1,page),Math.Clamp(size,10,100));private static int Pages(int count,int size)=>Math.Max(1,(int)Math.Ceiling(count/(double)size));private static string Mask(string value)=>value.Length<=2?"**":value[..2]+new string('*',Math.Min(6,value.Length-2));private static string Normalize(string value)=>value.Trim().ToUpperInvariant()is var code&&Channels.Contains(code)?code:throw Error(409,"CHANNEL_INVALID","알림 채널을 확인해 주세요.");private static string?Clean(string?value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim()[..Math.Min(value.Trim().Length,max)];
    private async Task<long>UserId(ClaimsPrincipal p,CancellationToken token){if(!Guid.TryParse(p.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id))throw Error(401,"AUTHENTICATION_REQUIRED","로그인이 필요합니다.");return await db.Users.Where(x=>x.PublicId==id&&x.StatusCode=="ACTIVE").Select(x=>x.Id).SingleAsync(token);}private static NotificationBusinessException Error(int status,string code,string message)=>new(status,code,message);
    private static IEnumerable<string>Variables(string value)=>VariableRegex().Matches(value).Select(x=>x.Groups[1].Value);[GeneratedRegex(@"\{\{\s*([a-z0-9_]+)\s*\}\}",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]private static partial Regex VariableRegex();
}

public sealed class NotificationDeliveryProcessor(SoodalLifeDbContext db,IEnumerable<INotificationChannelSender> senders,SoodalLife.Api.Infrastructure.Security.IPersonalDataReader personalDataReader,NotificationBroadcastService broadcasts)
{
    public async Task<(int Processed,int Failed)> ProcessBatch(CancellationToken token)
    {
        var now=DateTime.UtcNow;var settings=await db.NotificationChannelSettings.AsNoTracking().Where(x=>x.IsEnabled&&x.ChannelCode!="WEB").ToDictionaryAsync(x=>x.ChannelCode,token);if(settings.Count==0)return(0,0);var maxBatch=Math.Clamp(settings.Values.Max(x=>x.BatchSize),1,500);
        var ids=await db.NotificationDeliveries.AsNoTracking().Where(x=>(x.StatusCode=="PENDING"||x.StatusCode=="FAILED")&&(x.ScheduledAt==null||x.ScheduledAt<=now)&&settings.Keys.Contains(x.ChannelCode)).OrderBy(x=>x.ScheduledAt).ThenBy(x=>x.Id).Select(x=>x.Id).Take(maxBatch).ToListAsync(token);var processed=0;var failed=0;
        foreach(var id in ids){var row=await(from d in db.NotificationDeliveries join n in db.Notifications on d.NotificationId equals n.Id join r in db.NotificationRecipients on d.NotificationRecipientId equals (long?)r.Id join u in db.Users on r.UserId equals u.Id where d.Id==id select new{d,n,u}).SingleAsync(token);var setting=settings[row.d.ChannelCode];if(row.d.RetryCount>=setting.MaxAttempts)continue;if(!await broadcasts.CanDeliverNotificationAsync(row.n,row.u.Id,row.d.ChannelCode,now,token)){row.d.StatusCode="SKIPPED";row.d.ErrorCode="CHANNEL_OR_CONSENT_DISABLED";row.d.ErrorMessage="발송 직전 수신동의, 연락처 인증 또는 발송 상태 재검증에서 제외되었습니다.";row.d.ScheduledAt=null;row.d.CompletedAt=now;row.d.UpdatedAt=now;await db.SaveChangesAsync(token);continue;}var sender=senders.FirstOrDefault(x=>x.Supports(row.d.ChannelCode)&&x.IsConfigured(row.d.ChannelCode));if(sender==null)continue;var phone=personalDataReader.Read(row.u.PhoneEncrypted,row.u.Phone);var email=personalDataReader.Read(row.u.EmailEncrypted,row.u.Email);var recipient=row.d.ChannelCode switch{"EMAIL"=>email,"PUSH"=>row.u.PublicId.ToString("N"),_=>phone};row.d.StatusCode="PROCESSING";row.d.UpdatedAt=now;await db.SaveChangesAsync(token);var attempt=row.d.RetryCount+1;NotificationChannelSendResult result;var started=DateTime.UtcNow;try{result=await sender.SendAsync(new(row.n.PublicId,row.d.PublicId,row.d.ChannelCode,row.n.Title,row.n.Body,recipient,row.n.TemplateCodeSnapshot),token);}catch(Exception ex){result=new(false,"CHANNEL_SEND_EXCEPTION",FailureReason:ex.GetType().Name);}
            db.NotificationDeliveryAttempts.Add(new(){NotificationDeliveryId=row.d.Id,AttemptNo=attempt,StartedAt=started,CompletedAt=DateTime.UtcNow,ResultCode=result.ResultCode,ProviderResponseCode=result.ProviderResponseCode,FailureReason=result.FailureReason,CorrelationId=Guid.NewGuid(),CreatedAt=started});row.d.RetryCount=attempt;row.d.AttemptNo=(short)attempt;row.d.AttemptedAt=started;row.d.CompletedAt=DateTime.UtcNow;row.d.UpdatedAt=DateTime.UtcNow;if(result.Success){row.d.StatusCode=result.ResultCode=="DELIVERED"?"DELIVERED":"SENT";row.d.SentAt=DateTime.UtcNow;row.d.DeliveredAt=result.ResultCode=="DELIVERED"?DateTime.UtcNow:null;row.d.ProviderMessageId=result.ExternalMessageId;row.d.ErrorCode=null;row.d.ErrorMessage=null;row.d.ScheduledAt=null;processed++;}else{row.d.StatusCode="FAILED";row.d.FailedAt=DateTime.UtcNow;row.d.ErrorCode=result.ResultCode;row.d.ErrorMessage=result.FailureReason;var seconds=Math.Min(setting.MaxRetrySeconds,setting.BaseRetrySeconds*Math.Pow(2,Math.Max(0,attempt-1)));row.d.ScheduledAt=attempt<setting.MaxAttempts?DateTime.UtcNow.AddSeconds(seconds):null;failed++;}await db.SaveChangesAsync(token);}
        return(processed,failed);
    }
}
