namespace SoodalLife.Api.Features.Admin;

public sealed record AdminDashboardSummaryResponse(
    AdminMetricResponse TotalCustomers,
    AdminMetricResponse TotalProviders,
    AdminMetricResponse PendingProviders,
    AdminMetricResponse ActiveRequests,
    AdminMetricResponse ActiveTransactions,
    AdminMetricResponse UnresolvedAfterServiceCases);

public sealed record AdminMetricResponse(long? Value, string? UnavailableReason = null);
