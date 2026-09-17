using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Matching;

public sealed class ProviderDispatchWaveWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProviderDispatchWaveWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expanded = await scope.ServiceProvider.GetRequiredService<RequestMatchingService>()
                    .ExpandDueDispatchWavesAsync(stoppingToken);
                if (expanded > 0)
                    logger.LogInformation("Provider dispatch wave expanded by {DispatchCount} dispatches.", expanded);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError("Provider dispatch wave failed with {ErrorCode}.", SecurityTextSanitizer.ErrorCode(exception));
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
