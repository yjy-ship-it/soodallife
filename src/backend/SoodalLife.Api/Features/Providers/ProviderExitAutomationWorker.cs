namespace SoodalLife.Api.Features.Providers;

public sealed class ProviderExitAutomationWorker(IServiceScopeFactory scopes,ILogger<ProviderExitAutomationWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(30));
        while(await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope=scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ProviderExitService>().RefreshActiveAsync(stoppingToken);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception exception){logger.LogError(exception,"Provider exit readiness automation failed.");}
        }
    }
}
