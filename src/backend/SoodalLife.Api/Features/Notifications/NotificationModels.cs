using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Notifications;

public sealed record CreateNotificationTemplateRequest([param:Required,StringLength(100)]string TemplateCode,[param:Required,StringLength(200)]string Name,string? Description,[param:Required]string AudienceTypeCode,[param:Required,StringLength(100)]string EventTypeCode,[param:Required]string ChannelCode,[param:Required,StringLength(300)]string TitleTemplate,[param:Required,StringLength(3000)]string BodyTemplate,bool IsRequiredBusinessNotice,bool IsMarketing,DateTime? EffectiveFrom,DateTime? EffectiveTo);
public sealed record UpdateNotificationTemplateRequest([param:Required,StringLength(100)]string TemplateCode,[param:Required,StringLength(200)]string Name,string? Description,[param:Required]string AudienceTypeCode,[param:Required,StringLength(100)]string EventTypeCode,[param:Required]string ChannelCode,[param:Required,StringLength(300)]string TitleTemplate,[param:Required,StringLength(3000)]string BodyTemplate,bool IsRequiredBusinessNotice,bool IsMarketing,DateTime? EffectiveFrom,DateTime? EffectiveTo,string? RowVersion);
public sealed record SetNotificationTemplateStatusRequest(bool IsActive,string? RowVersion);
public sealed record UpdateNotificationPreferenceRequest(string EventGroupCode,bool WebEnabled,bool KakaoEnabled,bool SmsEnabled,bool EmailEnabled,bool PushEnabled);
public sealed record NotificationTemplateResponse(Guid Id,string TemplateCode,string Name,string? Description,string AudienceTypeCode,string EventTypeCode,string ChannelCode,string TitleTemplate,string BodyTemplate,IReadOnlyList<string> Variables,bool IsRequiredBusinessNotice,bool IsMarketing,bool IsActive,DateTime? EffectiveFrom,DateTime? EffectiveTo,string RowVersion);
public sealed record NotificationListItem(Guid Id,string EventTypeCode,string Title,string Body,string PriorityCode,string StatusCode,string? TargetTypeCode,Guid? TargetPublicId,DateTime CreatedAt,DateTime? ReadAt,bool IsArchived);
public sealed record NotificationUnreadCountResponse(int Count);
public sealed record NotificationPreferenceResponse(string EventGroupCode,bool WebEnabled,bool KakaoEnabled,bool SmsEnabled,bool EmailEnabled,bool PushEnabled,string RowVersion);
public sealed record NotificationAdminSummary(int CreatedToday,int Pending,int SentOrDelivered,int Failed,int WebUnread);
public sealed record NotificationDeliveryAdminItem(Guid Id,string Recipient,string RecipientRole,string ChannelCode,string EventTypeCode,string StatusCode,DateTime? ScheduledAt,DateTime CreatedAt,string? FailureCode,string? FailureReason,int RetryCount,DateTime? LastAttemptAt);
public sealed record ProcessOutboxNotificationResponse(Guid OutboxEventId,int NotificationsCreated,int RecipientsCreated,int DeliveriesCreated);
public sealed record NotificationChannelSendResult(bool Success,string ResultCode,string? ProviderResponseCode=null,string? ExternalMessageId=null,string? FailureReason=null);
public sealed record NotificationChannelMessage(Guid NotificationId,Guid DeliveryId,string ChannelCode,string Title,string Body);

public interface INotificationChannelSender
{
    bool Supports(string channelCode);
    Task<NotificationChannelSendResult> SendAsync(NotificationChannelMessage message,CancellationToken token);
}

public sealed class WebNotificationChannelSender:INotificationChannelSender
{
    public bool Supports(string channelCode)=>channelCode=="WEB";
    public Task<NotificationChannelSendResult> SendAsync(NotificationChannelMessage message,CancellationToken token)=>Task.FromResult(new NotificationChannelSendResult(true,"DELIVERED"));
}

public sealed class UnavailableExternalNotificationChannelSender:INotificationChannelSender
{
    public bool Supports(string channelCode)=>channelCode is "KAKAO" or "SMS" or "EMAIL" or "PUSH";
    public Task<NotificationChannelSendResult> SendAsync(NotificationChannelMessage message,CancellationToken token)=>Task.FromResult(new NotificationChannelSendResult(false,"CHANNEL_NOT_CONFIGURED",FailureReason:"외부 발송 채널이 연동되지 않았습니다."));
}

public sealed class NotificationBusinessException(int statusCode,string code,string message):Exception(message){public int StatusCode{get;}=statusCode;public string Code{get;}=code;}
