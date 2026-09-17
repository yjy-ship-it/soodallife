using SoodalLife.Api.Features.Notifications;
namespace SoodalLife.Api.Features.Automation;
public sealed class NotificationBroadcastQueueJob(NotificationBroadcastService service):IAutomationJob
{
    public string Name=>"NOTIFICATION_BROADCAST_QUEUE";public AutomationConfigurationStatus ConfigurationStatus=>AutomationConfigurationStatus.Enabled;public TimeSpan Interval=>TimeSpan.FromSeconds(5);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token){var result=await service.PrepareBatch(token);await service.RefreshCompleted(token);return new(result.Processed,0);}
}
