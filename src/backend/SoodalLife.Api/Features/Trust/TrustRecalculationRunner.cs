using SoodalLife.Api.Features.Admin;

namespace SoodalLife.Api.Features.Trust;

public sealed class TrustRecalculationRunner(TrustCalculationService calculation,ILogger<TrustRecalculationRunner> logger)
{
    public async Task RunAsync(Guid providerId,string sourceType,Guid? sourceId,string idempotencyKey,CancellationToken token)
    {
        try
        {
            await calculation.CalculateActiveAsync(providerId,sourceType,sourceId,idempotencyKey,null,token);
        }
        catch(TrustCalculationException exception) when(exception.BusinessCode=="ACTIVE_TRUST_POLICY_NOT_FOUND")
        {
            logger.LogInformation("Trust recalculation deferred because no active policy exists for {ProviderId}.",providerId);
        }
        catch(Exception exception)
        {
            logger.LogError(exception,"Trust recalculation failed for {ProviderId} from {SourceType}.",providerId,sourceType);
        }
    }
}
