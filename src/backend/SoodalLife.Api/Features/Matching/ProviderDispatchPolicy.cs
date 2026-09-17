namespace SoodalLife.Api.Features.Matching;

public static class ProviderDispatchPolicy
{
    public const int WaveSize = 10;
    public const int MaximumRequestsPerCycle = 50;
    public static readonly TimeSpan ExpansionDelay = TimeSpan.FromMinutes(5);
}
