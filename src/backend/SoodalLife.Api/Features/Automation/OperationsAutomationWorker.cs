namespace SoodalLife.Api.Features.Automation;

using SoodalLife.Api.Infrastructure.Security;

public sealed class OperationsAutomationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OperationsAutomationWorker> logger,
    Microsoft.Extensions.Options.IOptions<AutomationOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ScheduledJobRunner>().RunDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError("Operations automation loop failed with {ErrorCode}.", SecurityTextSanitizer.ErrorCode(exception));
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
