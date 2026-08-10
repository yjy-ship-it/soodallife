using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;

namespace SoodalLife.Api.Controllers;

[ApiController, Authorize(Roles = RoleCodes.Admin), Route("api/v1/admin/wallets")]
public sealed class AdminWalletsController(AdminWalletService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<AdminWalletListResponse>> Search([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] bool? hasBalance, [FromQuery] bool? hasRefund, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => Execute(() => service.SearchAsync(search, status, hasBalance, hasRefund, page, pageSize, cancellationToken));

    [HttpGet("{providerId:guid}")]
    public async Task<ActionResult<AdminWalletDetailResponse>> Get(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(providerId, cancellationToken);
        return result is null ? NotFound(ApiErrorResponse.Create(HttpContext, "ADMIN_WALLET_NOT_FOUND", "공급자 Wallet을 찾을 수 없습니다.")) : Ok(result);
    }

    [HttpPost("{providerId:guid}/development-charges")]
    public Task<ActionResult<AdminChargeRequestResponse>> CreateDevelopmentCharge(Guid providerId, AdminDevelopmentChargeRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.CreateDevelopmentChargeAsync(providerId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/development-charges/{chargeId:guid}/confirm")]
    public Task<ActionResult<WalletOperationResponse>> ConfirmDevelopmentCharge(Guid providerId, Guid chargeId, AdminChargeConfirmationRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.ConfirmDevelopmentChargeAsync(providerId, chargeId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/adjustments")]
    public Task<ActionResult<WalletOperationResponse>> Adjust(Guid providerId, AdminWalletAdjustmentRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.AdjustAsync(providerId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/fee-charges/{feeChargeId:guid}/restore")]
    public Task<ActionResult<WalletOperationResponse>> RestoreFee(Guid providerId, Guid feeChargeId, AdminFeeRestoreRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.RestoreFeeAsync(providerId, feeChargeId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/refunds")]
    public Task<ActionResult<AdminRefundRequestResponse>> RequestRefund(Guid providerId, AdminWalletRefundCreateRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.RequestRefundAsync(providerId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/refunds/{refundId:guid}/approve")]
    public Task<ActionResult<AdminRefundRequestResponse>> ApproveRefund(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.ApproveRefundAsync(providerId, refundId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/refunds/{refundId:guid}/complete-development")]
    public Task<ActionResult<WalletOperationResponse>> CompleteDevelopmentRefund(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.CompleteDevelopmentRefundAsync(providerId, refundId, request, ActorId(), cancellationToken));

    [HttpPost("{providerId:guid}/refunds/{refundId:guid}/cancel")]
    public Task<ActionResult<AdminRefundRequestResponse>> CancelRefund(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.CancelRefundAsync(providerId, refundId, request, ActorId(), cancellationToken));

    [HttpPatch("{providerId:guid}/status")]
    public Task<ActionResult<AdminWalletBalanceSummaryResponse>> ChangeStatus(Guid providerId, AdminWalletStatusRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.ChangeStatusAsync(providerId, request, ActorId(), cancellationToken));

    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (WalletOperationException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
