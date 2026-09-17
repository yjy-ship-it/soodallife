using SoodalLife.Api.Features.Notifications;

namespace SoodalLife.Api.Features.Automation;

public sealed class NotificationDeliveryJob(NotificationDeliveryProcessor processor):IAutomationJob
{
    public string Name=>"NOTIFICATION_DELIVERY_PROCESSOR";
    public AutomationConfigurationStatus ConfigurationStatus=>AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval=>TimeSpan.FromSeconds(10);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token){var result=await processor.ProcessBatch(token);return new(result.Processed,result.Failed);}
}
