using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/transactions")]
public sealed class AdminTransactionsController(AdminRequestTransactionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminTransactionListResponse>> Search([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] DateOnly? createdFrom, [FromQuery] DateOnly? createdTo, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken token = default)
    {
        try { return Ok(await service.SearchTransactionsAsync(search, status, createdFrom, createdTo, page, pageSize, token)); }
        catch (AdminServiceCategoryException ex) { return StatusCode(ex.StatusCode, ApiErrorResponse.Create(HttpContext, ex.BusinessCode, ex.Message)); }
    }

    [HttpGet("{transactionId:guid}")]
    public async Task<ActionResult<AdminTransactionDetailResponse>> Get(Guid transactionId, CancellationToken token)
    {
        var result = await service.GetTransactionAsync(transactionId, token);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_TRANSACTION_NOT_FOUND", "거래를 찾을 수 없습니다.")) : Ok(result);
    }
}
