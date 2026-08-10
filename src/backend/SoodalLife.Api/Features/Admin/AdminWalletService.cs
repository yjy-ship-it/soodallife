using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminWalletService(SoodalLifeDbContext dbContext, ProviderWalletService walletService, IHostEnvironment environment)
{
    private static readonly string[] OpenRefundStatuses = ["REQUESTED", "APPROVED", "PROCESSING"];
    private const int MaximumPageSize = 100;
    private bool DevelopmentConfirmationAvailable => environment.IsDevelopment() || environment.IsEnvironment("Testing");

    public async Task<AdminWalletListResponse> SearchAsync(string? search, string? status, bool? hasBalance, bool? hasRefund,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize) throw WalletOperationException("ADMIN_WALLET_PAGE_INVALID", "페이지 조건을 확인해 주세요.");
        var query = from wallet in dbContext.ProviderWallets.AsNoTracking()
                    join provider in dbContext.ProviderProfiles.AsNoTracking() on wallet.ProviderProfileId equals provider.Id
                    join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                    select new { Wallet = wallet, Provider = provider, User = user };
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var publicId)) query = query.Where(row => row.Provider.PublicId == publicId || row.Wallet.PublicId == publicId || row.User.PublicId == publicId);
            else query = query.Where(row => row.Provider.BusinessName.Contains(term) || (row.User.Phone != null && row.User.Phone.Contains(term)) || (row.User.Email != null && row.User.Email.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.Wallet.StatusCode == status.Trim().ToUpperInvariant());
        if (hasBalance.HasValue) query = query.Where(row => (row.Wallet.AvailableBalance > 0 || row.Wallet.ReservedBalance > 0) == hasBalance.Value);
        if (hasRefund.HasValue) query = query.Where(row => dbContext.WalletRefundRequests.Any(refund => refund.WalletId == row.Wallet.Id && OpenRefundStatuses.Contains(refund.StatusCode)) == hasRefund.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(row => row.Wallet.AvailableBalance).ThenBy(row => row.Provider.BusinessName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = new List<AdminWalletListItemResponse>(rows.Count);
        foreach (var row in rows)
        {
            var latest = await dbContext.WalletLedgerEntries.AsNoTracking().Where(value => value.WalletId == row.Wallet.Id).OrderByDescending(value => value.OccurredAt).ThenByDescending(value => value.Id).FirstOrDefaultAsync(cancellationToken);
            var refundInProgress = await dbContext.WalletRefundRequests.AnyAsync(value => value.WalletId == row.Wallet.Id && OpenRefundStatuses.Contains(value.StatusCode), cancellationToken);
            items.Add(new(row.Provider.PublicId, row.Wallet.PublicId, row.Provider.BusinessName, row.Provider.BusinessName,
                MaskPhone(row.User.Phone), MaskEmail(row.User.Email), row.Wallet.StatusCode, row.Wallet.AvailableBalance, row.Wallet.ReservedBalance,
                await LastLedgerAt(row.Wallet.Id, "CHARGE", cancellationToken), await LastLedgerAt(row.Wallet.Id, "USE", cancellationToken), latest?.OccurredAt,
                refundInProgress, row.Wallet.AvailableBalance > 0 || row.Wallet.ReservedBalance > 0 || refundInProgress,
                latest is null ? row.Wallet.AvailableBalance == 0 : latest.BalanceAfter == row.Wallet.AvailableBalance,
                Convert.ToBase64String(row.Wallet.RowVersion)));
        }
        return new(await SummaryAsync(cancellationToken), totalCount, page, pageSize, items);
    }

    public async Task<AdminWalletDetailResponse?> GetAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var row = await (from wallet in dbContext.ProviderWallets.AsNoTracking()
                         join provider in dbContext.ProviderProfiles.AsNoTracking() on wallet.ProviderProfileId equals provider.Id
                         join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                         where provider.PublicId == providerId && wallet.CurrencyCode == "KRW"
                         select new { Wallet = wallet, Provider = provider, User = user }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var ledgerRows = await (from ledger in dbContext.WalletLedgerEntries.AsNoTracking()
                                join actor in dbContext.Users.AsNoTracking() on ledger.CreatedByUserId equals actor.Id into actors
                                from actor in actors.DefaultIfEmpty()
                                join transaction in dbContext.Transactions.AsNoTracking() on ledger.TransactionId equals transaction.Id into transactions
                                from transaction in transactions.DefaultIfEmpty()
                                where ledger.WalletId == row.Wallet.Id
                                orderby ledger.OccurredAt descending, ledger.Id descending
                                select new AdminWalletLedgerResponse(ledger.PublicId, ledger.OccurredAt, ledger.EntryTypeCode, ledger.Amount, ledger.BalanceAfter,
                                    ledger.Reason, ledger.ReferenceType, ledger.ReferencePublicId, ledger.PaymentMethodCode, transaction == null ? null : transaction.PublicId,
                                    actor == null ? "시스템" : actor.LoginId)).ToListAsync(cancellationToken);
        var charges = await dbContext.WalletChargeRequests.AsNoTracking().Where(value => value.WalletId == row.Wallet.Id).OrderByDescending(value => value.RequestedAt)
            .Select(value => new AdminWalletChargeResponse(value.PublicId, value.RequestedAmount, value.PaymentMethodCode, value.StatusCode, value.RequestedAt,
                value.CompletedAt, value.FailureReason, value.ExternalPaymentReference, Convert.ToBase64String(value.RowVersion))).ToListAsync(cancellationToken);
        var feeCharges = await (from fee in dbContext.FeeCharges.AsNoTracking()
                                join transaction in dbContext.Transactions.AsNoTracking() on fee.TransactionId equals transaction.Id
                                join policy in dbContext.CategoryFeePolicies.AsNoTracking() on fee.CategoryFeePolicyId equals policy.Id
                                join debit in dbContext.WalletLedgerEntries.AsNoTracking() on fee.LedgerEntryId equals debit.Id
                                join restore in dbContext.WalletLedgerEntries.AsNoTracking() on fee.RestoreLedgerEntryId equals restore.Id into restores
                                from restore in restores.DefaultIfEmpty()
                                where fee.WalletId == row.Wallet.Id orderby fee.ChargedAt descending
                                select new AdminFeeChargeResponse(fee.PublicId, transaction.PublicId, policy.PublicId, fee.FeeAmount, fee.ChargedAt,
                                    fee.RestoreStatusCode, debit.PublicId, restore == null ? null : restore.PublicId, Convert.ToBase64String(fee.RowVersion))).ToListAsync(cancellationToken);
        var restoreRows = await (from restore in dbContext.FeeRestores.AsNoTracking()
                                 join fee in dbContext.FeeCharges.AsNoTracking() on restore.FeeChargeId equals fee.Id
                                 join actor in dbContext.Users.AsNoTracking() on restore.RestoredByUserId equals actor.Id
                                 where fee.WalletId == row.Wallet.Id orderby restore.RestoredAt descending
                                 select new AdminFeeRestoreResponse(restore.PublicId, fee.PublicId, restore.ReasonCode, restore.Reason, restore.RestoredAt, actor.LoginId)).ToListAsync(cancellationToken);
        var refunds = await dbContext.WalletRefundRequests.AsNoTracking().Where(value => value.WalletId == row.Wallet.Id).OrderByDescending(value => value.RequestedAt)
            .Select(value => new AdminWalletRefundResponse(value.PublicId, value.RequestedAmount, value.StatusCode, value.RequestReason, value.RequestedAt,
                value.ReviewedAt, value.ReviewReason, value.CompletedAt, value.FailureReason, Convert.ToBase64String(value.RowVersion))).ToListAsync(cancellationToken);
        var audits = await dbContext.AuditLogs.AsNoTracking().Where(value => value.EntityPublicId == row.Wallet.PublicId).OrderByDescending(value => value.OccurredAt)
            .Select(value => new AdminWalletAuditResponse(value.OccurredAt, value.ActionCode, value.ResultCode, value.Reason, value.ActorRoleCode)).ToListAsync(cancellationToken);
        var latest = ledgerRows.FirstOrDefault();
        var inProgress = refunds.Where(value => OpenRefundStatuses.Contains(value.StatusCode)).Sum(value => value.RequestedAmount);
        var refundable = Math.Max(0, row.Wallet.AvailableBalance - inProgress);
        var balance = new AdminWalletBalanceSummaryResponse(row.Wallet.PublicId, row.Wallet.CurrencyCode.Trim(), row.Wallet.AvailableBalance,
            row.Wallet.ReservedBalance, refundable, inProgress, row.Wallet.StatusCode,
            row.Wallet.AvailableBalance > 0 || row.Wallet.ReservedBalance > 0 || inProgress > 0, row.Wallet.ReservedBalance > 0,
            latest is null ? row.Wallet.AvailableBalance == 0 : latest.BalanceAfter == row.Wallet.AvailableBalance, latest?.BalanceAfter,
            Convert.ToBase64String(row.Wallet.RowVersion));
        return new(new(row.Provider.PublicId, row.Provider.BusinessName, row.Provider.BusinessName, MaskPhone(row.User.Phone), MaskEmail(row.User.Email)),
            balance, ledgerRows, charges, feeCharges, restoreRows, refunds, audits, DevelopmentConfirmationAvailable);
    }

    public async Task<AdminChargeRequestResponse> CreateDevelopmentChargeAsync(Guid providerId, AdminDevelopmentChargeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        EnsureDevelopment(); ProviderWalletService.ValidatePositive(request.Amount, "충전"); ProviderWalletService.ValidateKey(request.IdempotencyKey);
        var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken);
        var wallet = await WalletForProvider(providerId, true, cancellationToken);
        var existing = await dbContext.WalletChargeRequests.AsNoTracking().SingleOrDefaultAsync(value => value.IdempotencyKey == request.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.WalletId != wallet.Id || existing.RequestedAmount != request.Amount) throw WalletOperationException("WALLET_IDEMPOTENCY_CONFLICT", "같은 멱등성 키가 다른 충전 요청에 사용되었습니다.", StatusCodes.Status409Conflict);
            return ChargeResponse(existing, wallet.PublicId);
        }
        var now = DateTime.UtcNow;
        var charge = new WalletChargeRequest { WalletId = wallet.Id, RequestedAmount = request.Amount, PaymentMethodCode = "DEVELOPMENT_MANUAL",
            StatusCode = "REQUESTED", RequestedAt = now, IdempotencyKey = request.IdempotencyKey.Trim(), RequestReason = reason,
            CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        dbContext.WalletChargeRequests.Add(charge); await walletService.SaveWithConcurrencyAsync(cancellationToken); return ChargeResponse(charge, wallet.PublicId);
    }

    public async Task<WalletOperationResponse> ConfirmDevelopmentChargeAsync(Guid providerId, Guid chargeId, AdminChargeConfirmationRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        EnsureDevelopment(); var actor = await ActorId(actorPublicId, cancellationToken);
        var row = await (from wallet in dbContext.ProviderWallets join provider in dbContext.ProviderProfiles on wallet.ProviderProfileId equals provider.Id
                         join charge in dbContext.WalletChargeRequests on wallet.Id equals charge.WalletId
                         where provider.PublicId == providerId && charge.PublicId == chargeId select new { Wallet = wallet, Charge = charge }).SingleOrDefaultAsync(cancellationToken)
            ?? throw WalletOperationException("ADMIN_WALLET_CHARGE_NOT_FOUND", "충전 요청을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (row.Charge.StatusCode == "SUCCEEDED" && row.Charge.LedgerEntryId.HasValue)
        { var existing = await dbContext.WalletLedgerEntries.AsNoTracking().SingleAsync(value => value.Id == row.Charge.LedgerEntryId, cancellationToken); return ProviderWalletService.ToOperation(row.Wallet, existing); }
        EnsureRowVersion(row.Charge.RowVersion, request.RowVersion);
        if (row.Charge.StatusCode != "REQUESTED" || row.Wallet.StatusCode != "ACTIVE") throw WalletOperationException("ADMIN_WALLET_CHARGE_STATE_INVALID", "충전을 확정할 수 있는 상태가 아닙니다.", StatusCodes.Status409Conflict);
        await using var transaction = await walletService.BeginTransactionAsync(cancellationToken); var now = DateTime.UtcNow;
        var before = row.Wallet.AvailableBalance; row.Wallet.AvailableBalance += row.Charge.RequestedAmount; row.Wallet.UpdatedAt = now; row.Wallet.UpdatedByUserId = actor;
        var ledger = walletService.AddLedger(row.Wallet, null, "CHARGE", row.Charge.RequestedAmount, $"CHARGE:{row.Charge.PublicId:N}",
            row.Charge.RequestReason ?? "개발용 수동 충전 확인", "CHARGE_REQUEST", row.Charge.PublicId, row.Charge.PaymentMethodCode, now, actor);
        await walletService.SaveWithConcurrencyAsync(cancellationToken);
        row.Charge.StatusCode = "SUCCEEDED"; row.Charge.CompletedAt = now; row.Charge.LedgerEntryId = ledger.Id; row.Charge.UpdatedAt = now; row.Charge.UpdatedByUserId = actor;
        AddAudit(row.Wallet, actor, "WALLET_DEVELOPMENT_CHARGE_CONFIRMED", row.Charge.RequestReason, before, row.Wallet.AvailableBalance, row.Charge.RequestedAmount, row.Charge.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ProviderWalletService.ToOperation(row.Wallet, ledger);
    }

    public async Task<WalletOperationResponse> AdjustAsync(Guid providerId, AdminWalletAdjustmentRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ProviderWalletService.ValidatePositive(request.Amount, "조정"); ProviderWalletService.ValidateKey(request.IdempotencyKey);
        var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken);
        var wallet = await WalletForProvider(providerId, true, cancellationToken);
        var direction = request.DirectionCode.Trim().ToUpperInvariant(); var signed = direction switch { "INCREASE" => request.Amount, "DECREASE" => -request.Amount, _ => throw WalletOperationException("ADMIN_WALLET_ADJUST_DIRECTION_INVALID", "증가 또는 감소를 선택해 주세요.") };
        var existing = await walletService.ExistingOperationAsync(request.IdempotencyKey, wallet.Id, "ADJUST", signed, wallet.PublicId, cancellationToken); if (existing is not null) return existing;
        EnsureRowVersion(wallet.RowVersion, request.RowVersion);
        if (wallet.AvailableBalance + signed < 0) throw WalletOperationException("WALLET_INSUFFICIENT_BALANCE", "조정할 충전금 잔액이 부족합니다.", StatusCodes.Status409Conflict);
        var now = DateTime.UtcNow; var before = wallet.AvailableBalance; wallet.AvailableBalance += signed; wallet.UpdatedAt = now; wallet.UpdatedByUserId = actor;
        var ledger = walletService.AddLedger(wallet, null, "ADJUST", signed, request.IdempotencyKey, reason, "ADMIN_ADJUSTMENT", wallet.PublicId, null, now, actor);
        AddAudit(wallet, actor, "WALLET_ADMIN_ADJUSTED", reason, before, wallet.AvailableBalance, signed, ledger.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); return ProviderWalletService.ToOperation(wallet, ledger);
    }

    public async Task<WalletOperationResponse> RestoreFeeAsync(Guid providerId, Guid feeChargeId, AdminFeeRestoreRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ProviderWalletService.ValidateKey(request.IdempotencyKey); var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken);
        var reasonCode = request.ReasonCode.Trim().ToUpperInvariant(); if (reasonCode is not ("SYSTEM_ERROR" or "DUPLICATE_ACCEPTANCE" or "FALSE_REQUEST" or "HEAD_OFFICE_APPROVAL")) throw WalletOperationException("ADMIN_WALLET_RESTORE_REASON_INVALID", "허용된 복원 사유를 선택해 주세요.");
        var row = await (from wallet in dbContext.ProviderWallets join provider in dbContext.ProviderProfiles on wallet.ProviderProfileId equals provider.Id
                         join fee in dbContext.FeeCharges on wallet.Id equals fee.WalletId
                         where provider.PublicId == providerId && fee.PublicId == feeChargeId select new { Wallet = wallet, Fee = fee }).SingleOrDefaultAsync(cancellationToken)
            ?? throw WalletOperationException("ADMIN_WALLET_FEE_NOT_FOUND", "수수료 차감내역을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var existing = await walletService.ExistingOperationAsync(request.IdempotencyKey, row.Wallet.Id, "RESTORE", row.Fee.FeeAmount, row.Fee.PublicId, cancellationToken); if (existing is not null) return existing;
        EnsureRowVersion(row.Fee.RowVersion, request.RowVersion); if (row.Fee.RestoreStatusCode != "NOT_RESTORED") throw WalletOperationException("ADMIN_WALLET_FEE_ALREADY_RESTORED", "이미 복원된 수수료입니다.", StatusCodes.Status409Conflict);
        await using var transaction = await walletService.BeginTransactionAsync(cancellationToken); var now = DateTime.UtcNow; var before = row.Wallet.AvailableBalance;
        row.Wallet.AvailableBalance += row.Fee.FeeAmount; row.Wallet.UpdatedAt = now; row.Wallet.UpdatedByUserId = actor;
        var ledger = walletService.AddLedger(row.Wallet, row.Fee.TransactionId, "RESTORE", row.Fee.FeeAmount, request.IdempotencyKey, reason, "FEE_RESTORE", row.Fee.PublicId, null, now, actor);
        await walletService.SaveWithConcurrencyAsync(cancellationToken);
        row.Fee.RestoreStatusCode = "RESTORED"; row.Fee.RestoreLedgerEntryId = ledger.Id; row.Fee.UpdatedAt = now; row.Fee.UpdatedByUserId = actor;
        dbContext.FeeRestores.Add(new FeeRestore { FeeChargeId = row.Fee.Id, LedgerEntryId = ledger.Id, ReasonCode = reasonCode, Reason = reason,
            IdempotencyKey = request.IdempotencyKey.Trim(), RestoredAt = now, RestoredByUserId = actor, CreatedAt = now, CreatedByUserId = actor });
        AddAudit(row.Wallet, actor, "WALLET_FEE_RESTORED", reason, before, row.Wallet.AvailableBalance, row.Fee.FeeAmount, row.Fee.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ProviderWalletService.ToOperation(row.Wallet, ledger);
    }

    public async Task<AdminRefundRequestResponse> RequestRefundAsync(Guid providerId, AdminWalletRefundCreateRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ProviderWalletService.ValidatePositive(request.Amount, "환불"); ProviderWalletService.ValidateKey(request.IdempotencyKey); var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken);
        var wallet = await WalletForProvider(providerId, false, cancellationToken); var pending = await PendingRefundAmount(wallet.Id, cancellationToken);
        var existing = await dbContext.WalletRefundRequests.AsNoTracking().SingleOrDefaultAsync(value => value.IdempotencyKey == request.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.WalletId != wallet.Id || existing.RequestedAmount != request.Amount) throw WalletOperationException("WALLET_IDEMPOTENCY_CONFLICT", "같은 멱등성 키가 다른 환불 요청에 사용되었습니다.", StatusCodes.Status409Conflict);
            return RefundResponse(existing, wallet.PublicId);
        }
        if (request.Amount > wallet.AvailableBalance - pending) throw WalletOperationException("ADMIN_WALLET_REFUND_AMOUNT_EXCEEDED", "환불 가능금액을 초과했습니다.", StatusCodes.Status409Conflict);
        var now = DateTime.UtcNow; wallet.UpdatedAt = now; wallet.UpdatedByUserId = actor;
        var refund = new WalletRefundRequest { WalletId = wallet.Id, RequestedAmount = request.Amount, StatusCode = "REQUESTED",
            RequestReason = reason, RequestedAt = now, IdempotencyKey = request.IdempotencyKey.Trim(), CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        dbContext.WalletRefundRequests.Add(refund); await walletService.SaveWithConcurrencyAsync(cancellationToken); return RefundResponse(refund, wallet.PublicId);
    }

    public async Task<AdminRefundRequestResponse> ApproveRefundAsync(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken); var row = await RefundForProvider(providerId, refundId, cancellationToken);
        EnsureRowVersion(row.Refund.RowVersion, request.RowVersion); if (row.Refund.StatusCode != "REQUESTED") throw WalletOperationException("ADMIN_WALLET_REFUND_STATE_INVALID", "검토 대기 중인 환불만 승인할 수 있습니다.", StatusCodes.Status409Conflict);
        var now = DateTime.UtcNow; row.Refund.StatusCode = "APPROVED"; row.Refund.ReviewedAt = now; row.Refund.ReviewedByUserId = actor; row.Refund.ReviewReason = reason; row.Refund.UpdatedAt = now; row.Refund.UpdatedByUserId = actor;
        AddAudit(row.Wallet, actor, "WALLET_REFUND_APPROVED", reason, row.Wallet.AvailableBalance, row.Wallet.AvailableBalance, row.Refund.RequestedAmount, row.Refund.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); return RefundResponse(row.Refund, row.Wallet.PublicId);
    }

    public async Task<WalletOperationResponse> CompleteDevelopmentRefundAsync(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        EnsureDevelopment(); var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken); var row = await RefundForProvider(providerId, refundId, cancellationToken);
        if (row.Refund.StatusCode == "COMPLETED" && row.Refund.LedgerEntryId.HasValue) { var existing = await dbContext.WalletLedgerEntries.AsNoTracking().SingleAsync(value => value.Id == row.Refund.LedgerEntryId, cancellationToken); return ProviderWalletService.ToOperation(row.Wallet, existing); }
        EnsureRowVersion(row.Refund.RowVersion, request.RowVersion); if (row.Refund.StatusCode != "APPROVED") throw WalletOperationException("ADMIN_WALLET_REFUND_STATE_INVALID", "승인된 환불만 완료 확인할 수 있습니다.", StatusCodes.Status409Conflict);
        if (row.Wallet.AvailableBalance < row.Refund.RequestedAmount) throw WalletOperationException("ADMIN_WALLET_REFUND_AMOUNT_EXCEEDED", "현재 잔액이 환불금액보다 적습니다.", StatusCodes.Status409Conflict);
        await using var transaction = await walletService.BeginTransactionAsync(cancellationToken); var now = DateTime.UtcNow; var before = row.Wallet.AvailableBalance;
        row.Wallet.AvailableBalance -= row.Refund.RequestedAmount; row.Wallet.UpdatedAt = now; row.Wallet.UpdatedByUserId = actor;
        var ledger = walletService.AddLedger(row.Wallet, null, "REFUND", -row.Refund.RequestedAmount, $"REFUND:{row.Refund.PublicId:N}", reason,
            "REFUND_REQUEST", row.Refund.PublicId, null, now, actor); await walletService.SaveWithConcurrencyAsync(cancellationToken);
        row.Refund.StatusCode = "COMPLETED"; row.Refund.CompletedAt = now; row.Refund.LedgerEntryId = ledger.Id; row.Refund.UpdatedAt = now; row.Refund.UpdatedByUserId = actor;
        AddAudit(row.Wallet, actor, "WALLET_DEVELOPMENT_REFUND_COMPLETED", reason, before, row.Wallet.AvailableBalance, -row.Refund.RequestedAmount, row.Refund.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ProviderWalletService.ToOperation(row.Wallet, ledger);
    }

    public async Task<AdminRefundRequestResponse> CancelRefundAsync(Guid providerId, Guid refundId, AdminWalletRefundReviewRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken); var row = await RefundForProvider(providerId, refundId, cancellationToken);
        EnsureRowVersion(row.Refund.RowVersion, request.RowVersion); if (!OpenRefundStatuses.Contains(row.Refund.StatusCode)) throw WalletOperationException("ADMIN_WALLET_REFUND_STATE_INVALID", "진행 중인 환불만 취소할 수 있습니다.", StatusCodes.Status409Conflict);
        row.Refund.StatusCode = "CANCELLED"; row.Refund.ReviewedAt = DateTime.UtcNow; row.Refund.ReviewedByUserId = actor; row.Refund.ReviewReason = reason; row.Refund.UpdatedAt = DateTime.UtcNow; row.Refund.UpdatedByUserId = actor;
        AddAudit(row.Wallet, actor, "WALLET_REFUND_CANCELLED", reason, row.Wallet.AvailableBalance, row.Wallet.AvailableBalance, row.Refund.RequestedAmount, row.Refund.PublicId);
        await walletService.SaveWithConcurrencyAsync(cancellationToken); return RefundResponse(row.Refund, row.Wallet.PublicId);
    }

    public async Task<AdminWalletBalanceSummaryResponse> ChangeStatusAsync(Guid providerId, AdminWalletStatusRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var reason = ProviderWalletService.RequiredReason(request.Reason); var actor = await ActorId(actorPublicId, cancellationToken); var wallet = await WalletForProvider(providerId, false, cancellationToken); EnsureRowVersion(wallet.RowVersion, request.RowVersion);
        var status = request.StatusCode.Trim().ToUpperInvariant(); if (status is not ("ACTIVE" or "FROZEN" or "CLOSED")) throw WalletOperationException("ADMIN_WALLET_STATUS_INVALID", "Wallet 상태를 확인해 주세요.");
        if (status == "CLOSED" && (wallet.AvailableBalance > 0 || wallet.ReservedBalance > 0 || await PendingRefundAmount(wallet.Id, cancellationToken) > 0)) throw WalletOperationException("ADMIN_WALLET_CLOSE_BLOCKED", "잔액 또는 진행 중 환불이 있어 Wallet을 종료할 수 없습니다.", StatusCodes.Status409Conflict);
        var before = wallet.StatusCode; wallet.StatusCode = status; wallet.UpdatedAt = DateTime.UtcNow; wallet.UpdatedByUserId = actor;
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "WALLET_STATUS_CHANGED", EntityType = "WALLET", EntityPublicId = wallet.PublicId, ResultCode = "SUCCESS", Reason = reason, BeforeJson = JsonSerializer.Serialize(new { StatusCode = before }), AfterJson = JsonSerializer.Serialize(new { StatusCode = status }) });
        await walletService.SaveWithConcurrencyAsync(cancellationToken); var pending = await PendingRefundAmount(wallet.Id, cancellationToken);
        return new(wallet.PublicId, wallet.CurrencyCode.Trim(), wallet.AvailableBalance, wallet.ReservedBalance, Math.Max(0, wallet.AvailableBalance - pending), pending, wallet.StatusCode,
            wallet.AvailableBalance > 0 || wallet.ReservedBalance > 0 || pending > 0, wallet.ReservedBalance > 0, await BalanceMatches(wallet, cancellationToken), await LatestBalance(wallet.Id, cancellationToken), Convert.ToBase64String(wallet.RowVersion));
    }

    private async Task<AdminWalletSummaryResponse> SummaryAsync(CancellationToken cancellationToken)
    {
        var (from, to) = KoreaTodayUtc();
        return new(await dbContext.ProviderWallets.CountAsync(cancellationToken), await dbContext.ProviderWallets.SumAsync(value => value.AvailableBalance, cancellationToken),
            await LedgerSum("CHARGE", from, to, cancellationToken), -await LedgerSum("USE", from, to, cancellationToken), await LedgerSum("RESTORE", from, to, cancellationToken),
            await dbContext.WalletRefundRequests.Where(value => OpenRefundStatuses.Contains(value.StatusCode)).SumAsync(value => (decimal?)value.RequestedAmount, cancellationToken) ?? 0,
            DevelopmentConfirmationAvailable);
    }
    private async Task<decimal> LedgerSum(string type, DateTime from, DateTime to, CancellationToken token) => await dbContext.WalletLedgerEntries.Where(value => value.EntryTypeCode == type && value.OccurredAt >= from && value.OccurredAt < to).SumAsync(value => (decimal?)value.Amount, token) ?? 0;
    private async Task<DateTime?> LastLedgerAt(long walletId, string type, CancellationToken token) => await dbContext.WalletLedgerEntries.Where(value => value.WalletId == walletId && value.EntryTypeCode == type).MaxAsync(value => (DateTime?)value.OccurredAt, token);
    private async Task<long> ActorId(Guid publicId, CancellationToken token) => await dbContext.Users.Where(value => value.PublicId == publicId && value.StatusCode == "ACTIVE").Select(value => (long?)value.Id).SingleOrDefaultAsync(token) ?? throw WalletOperationException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
    private async Task<ProviderWallet> WalletForProvider(Guid providerId, bool activeOnly, CancellationToken token) => await (from provider in dbContext.ProviderProfiles join wallet in dbContext.ProviderWallets on provider.Id equals wallet.ProviderProfileId where provider.PublicId == providerId && wallet.CurrencyCode == "KRW" && (!activeOnly || wallet.StatusCode == "ACTIVE") select wallet).SingleOrDefaultAsync(token) ?? throw WalletOperationException("ADMIN_WALLET_NOT_FOUND", "사용 가능한 공급자 Wallet을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    private async Task<(ProviderWallet Wallet, WalletRefundRequest Refund)> RefundForProvider(Guid providerId, Guid refundId, CancellationToken token)
    { var row = await (from provider in dbContext.ProviderProfiles join wallet in dbContext.ProviderWallets on provider.Id equals wallet.ProviderProfileId join refund in dbContext.WalletRefundRequests on wallet.Id equals refund.WalletId where provider.PublicId == providerId && refund.PublicId == refundId select new { wallet, refund }).SingleOrDefaultAsync(token) ?? throw WalletOperationException("ADMIN_WALLET_REFUND_NOT_FOUND", "환불 요청을 찾을 수 없습니다.", StatusCodes.Status404NotFound); return (row.wallet, row.refund); }
    private async Task<decimal> PendingRefundAmount(long walletId, CancellationToken token) => await dbContext.WalletRefundRequests.Where(value => value.WalletId == walletId && OpenRefundStatuses.Contains(value.StatusCode)).SumAsync(value => (decimal?)value.RequestedAmount, token) ?? 0;
    private async Task<decimal?> LatestBalance(long walletId, CancellationToken token) => await dbContext.WalletLedgerEntries.Where(value => value.WalletId == walletId).OrderByDescending(value => value.OccurredAt).ThenByDescending(value => value.Id).Select(value => (decimal?)value.BalanceAfter).FirstOrDefaultAsync(token);
    private async Task<bool> BalanceMatches(ProviderWallet wallet, CancellationToken token) { var latest = await LatestBalance(wallet.Id, token); return latest.HasValue ? latest.Value == wallet.AvailableBalance : wallet.AvailableBalance == 0; }
    private static void EnsureRowVersion(byte[] current, string supplied) { byte[] token; try { token = Convert.FromBase64String(supplied); } catch { throw WalletOperationException("WALLET_CONCURRENCY_CONFLICT", "최신 상태를 다시 확인해 주세요.", StatusCodes.Status409Conflict); } if (!current.SequenceEqual(token)) throw WalletOperationException("WALLET_CONCURRENCY_CONFLICT", "다른 작업이 먼저 처리했습니다. 최신 상태를 다시 확인해 주세요.", StatusCodes.Status409Conflict); }
    private void EnsureDevelopment() { if (!DevelopmentConfirmationAvailable) throw WalletOperationException("WALLET_DEVELOPMENT_CONFIRMATION_DISABLED", "개발용 수동 확인 기능은 운영환경에서 사용할 수 없습니다.", StatusCodes.Status404NotFound); }
    private void AddAudit(ProviderWallet wallet, long actor, string action, string? reason, decimal before, decimal after, decimal amount, Guid referenceId) => dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = action, EntityType = "WALLET", EntityPublicId = wallet.PublicId, ResultCode = "SUCCESS", Reason = reason, BeforeJson = JsonSerializer.Serialize(new { AvailableBalance = before }), AfterJson = JsonSerializer.Serialize(new { AvailableBalance = after }), MetadataJson = JsonSerializer.Serialize(new { Amount = amount, ReferenceId = referenceId }) });
    private static AdminChargeRequestResponse ChargeResponse(WalletChargeRequest value, Guid walletId) => new(value.PublicId, walletId, value.RequestedAmount, value.StatusCode, value.RequestedAt, value.CompletedAt, Convert.ToBase64String(value.RowVersion));
    private static AdminRefundRequestResponse RefundResponse(WalletRefundRequest value, Guid walletId) => new(value.PublicId, walletId, value.RequestedAmount, value.StatusCode, value.RequestedAt, value.ReviewedAt, value.CompletedAt, Convert.ToBase64String(value.RowVersion));
    private static string? MaskPhone(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length < 7 ? "연락처 등록됨" : $"{digits[..3]}-****-{digits[^4..]}"; }
    private static string? MaskEmail(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var at = value.IndexOf('@'); return at <= 0 ? "이메일 등록됨" : $"{value[0]}***{value[at..]}"; }
    private static (DateTime From, DateTime To) KoreaTodayUtc() { TimeZoneInfo zone; try { zone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"); } catch { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"); } var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date; return (TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeZoneInfo.ConvertTimeToUtc(local.AddDays(1), zone)); }
    private static WalletOperationException WalletOperationException(string code, string message, int status = StatusCodes.Status400BadRequest) => new(code, message, status);
}
