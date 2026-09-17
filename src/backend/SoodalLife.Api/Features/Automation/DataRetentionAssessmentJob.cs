using SoodalLife.Api.Features.Admin;
namespace SoodalLife.Api.Features.Automation;
public sealed class DataRetentionAssessmentJob(DataRetentionService service):IAutomationJob
{
    public string Name=>"PRIVACY_RETENTION_SCAN";public AutomationConfigurationStatus ConfigurationStatus=>AutomationConfigurationStatus.Enabled;public TimeSpan Interval=>TimeSpan.FromHours(24);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token)=>new(await service.AssessEnabledAsync(token));
}
