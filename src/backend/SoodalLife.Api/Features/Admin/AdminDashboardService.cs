using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminDashboardService(SoodalLifeDbContext dbContext)
{
    private const string AggregationCriteriaPending = "집계 기준 미확정";

    public async Task<AdminDashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalCustomers = await dbContext.CustomerProfiles.AsNoTracking().LongCountAsync(cancellationToken);
        var totalProviders = await dbContext.ProviderProfiles.AsNoTracking().LongCountAsync(cancellationToken);
        var pendingProviders = await dbContext.ProviderProfiles
            .AsNoTracking()
            .LongCountAsync(provider => provider.ApprovalStatusCode == "PENDING", cancellationToken);

        return new AdminDashboardSummaryResponse(
            new AdminMetricResponse(totalCustomers),
            new AdminMetricResponse(totalProviders),
            new AdminMetricResponse(pendingProviders),
            new AdminMetricResponse(null, AggregationCriteriaPending),
            new AdminMetricResponse(null, AggregationCriteriaPending),
            new AdminMetricResponse(null, AggregationCriteriaPending));
    }
}
