using SoodalLife.Api.Features.Proposals;

namespace SoodalLife.Api.Features.Automation;

public sealed class ProposalLifecycleJob(ProposalService service) : IAutomationJob
{
    public string Name => "PROVIDER_PROPOSAL_LIFECYCLE";
    public AutomationConfigurationStatus ConfigurationStatus => AutomationConfigurationStatus.Enabled;
    public TimeSpan Interval => TimeSpan.FromMinutes(5);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => new(await service.ProcessDue(token));
}
