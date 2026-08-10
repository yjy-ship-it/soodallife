using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Admin)]
[Route("api/v1/admin")]
public sealed class AdminController(AdminDashboardService dashboardService, AdminAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard/summary")]
    [ProducesResponseType<AdminDashboardSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardSummaryResponse>> GetDashboardSummary(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetSummaryAsync(cancellationToken));

    [HttpGet("dashboard/management")]
    [ProducesResponseType<AdminAnalyticsDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAnalyticsDashboardResponse>> GetManagementDashboard(
        [FromQuery] AdminAnalyticsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetDashboardAsync(query, cancellationToken));
}
