using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/settlement-operations")]
public sealed class AdminSettlementOperationsController(AdminSettlementOperationsService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<AdminSettlementOperationsDashboardResponse> Dashboard(CancellationToken token) => service.Dashboard(token);

    [HttpGet("ledger")]
    public Task<AdminUnifiedLedgerResponse> Ledger([FromQuery] string? source, [FromQuery] string? search,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30, CancellationToken token = default) =>
        service.Ledger(source, search, from, to, page, pageSize, token);
}
