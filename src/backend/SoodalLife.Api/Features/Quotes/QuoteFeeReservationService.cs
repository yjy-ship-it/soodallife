using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Quotes;

public sealed class QuoteFeeReservationService(SoodalLifeDbContext db, ProviderWalletService wallets)
{
    public async Task<QuoteFeeReservation> EnsureReservedAsync(Quote quote, CategoryFeePolicy policy, long? actorUserId, CancellationToken token)
    {
        var existing = await db.QuoteFeeReservations.SingleOrDefaultAsync(x => x.QuoteId == quote.Id, token);
        if (existing is not null)
        {
            if (existing.StatusCode == "RESERVED") return existing;
            throw Error("QUOTE_FEE_RESERVATION_FINALIZED", "이미 확정 또는 해제된 견적 수수료 예약입니다.");
        }

        var wallet = await db.ProviderWallets.SingleOrDefaultAsync(x => x.ProviderProfileId == quote.ProviderProfileId && x.CurrencyCode == policy.CurrencyCode, token)
            ?? throw Error("WALLET_NOT_FOUND", "전문가 Wallet을 찾을 수 없습니다.");
        var amount = policy.FeeAmount is > 0 ? policy.FeeAmount.Value : throw Error("FEE_POLICY_NOT_CHARGEABLE", "견적 채택 시 차감할 수수료가 확정되지 않았습니다.");
        if (wallet.StatusCode != "ACTIVE") throw Error("WALLET_NOT_ACTIVE", "현재 사용할 수 없는 전문가 Wallet입니다.");
        if (wallet.AvailableBalance < amount) throw Error("WALLET_INSUFFICIENT_BALANCE", "예상 수수료보다 잔액이 부족합니다.");

        await using var transaction = await wallets.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        wallet.AvailableBalance -= amount;
        wallet.ReservedBalance += amount;
        wallet.UpdatedAt = now;
        wallet.UpdatedByUserId = actorUserId;
        var ledger = wallets.AddLedger(wallet, null, "RESERVE", -amount, $"quote-fee-reserve:{quote.PublicId:N}",
            "견적 제출 예상 수수료 예약", "QUOTE_FEE_RESERVATION", quote.PublicId, null, now, actorUserId);
        await wallets.SaveWithConcurrencyAsync(token);
        var reservation = new QuoteFeeReservation
        {
            QuoteId = quote.Id, WalletId = wallet.Id, CategoryFeePolicyId = policy.Id, Amount = amount,
            ExpectedSupplyAmount = Math.Abs(ledger.SupplyAmount), ExpectedVatAmount = Math.Abs(ledger.VatAmount),
            CurrencyCode = policy.CurrencyCode, StatusCode = "RESERVED", ReserveLedgerEntryId = ledger.Id,
            ReservedAt = now, CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId,
        };
        db.QuoteFeeReservations.Add(reservation);
        await wallets.SaveWithConcurrencyAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return reservation;
    }

    public async Task<WalletOperationResponse> CaptureAsync(Quote quote, TransactionRecord transactionRecord, long? actorUserId, CancellationToken token)
    {
        var row = await (from reservation in db.QuoteFeeReservations
                         join wallet in db.ProviderWallets on reservation.WalletId equals wallet.Id
                         where reservation.QuoteId == quote.Id
                         select new { Reservation = reservation, Wallet = wallet }).SingleOrDefaultAsync(token)
            ?? throw Error("QUOTE_FEE_RESERVATION_NOT_FOUND", "견적 수수료 예약을 찾을 수 없습니다.");
        if (row.Reservation.StatusCode == "CAPTURED")
        {
            var captured = await db.WalletLedgerEntries.AsNoTracking().SingleAsync(x => x.Id == row.Reservation.CaptureLedgerEntryId, token);
            return ProviderWalletService.ToOperation(row.Wallet, captured);
        }
        if (row.Reservation.StatusCode != "RESERVED") throw Error("QUOTE_FEE_RESERVATION_RELEASED", "이미 해제된 견적 수수료 예약입니다.");
        if (row.Wallet.StatusCode != "ACTIVE") throw Error("WALLET_NOT_ACTIVE", "현재 사용할 수 없는 전문가 Wallet입니다.");
        if (row.Wallet.ReservedBalance < row.Reservation.Amount) throw Error("WALLET_RESERVED_BALANCE_INVALID", "예약잔액을 확인할 수 없습니다.");
        if (await db.FeeCharges.AnyAsync(x => x.TransactionId == transactionRecord.Id && x.CategoryFeePolicyId == row.Reservation.CategoryFeePolicyId, token))
            throw Error("WALLET_FEE_ALREADY_CHARGED", "해당 거래의 수수료가 이미 차감되었습니다.");

        await using var dbTransaction = await wallets.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        row.Wallet.ReservedBalance -= row.Reservation.Amount;
        row.Wallet.AvailableBalance += row.Reservation.Amount;
        row.Wallet.UpdatedAt = now;
        row.Wallet.UpdatedByUserId = actorUserId;
        wallets.AddLedger(row.Wallet, transactionRecord.Id, "RELEASE", row.Reservation.Amount,
            $"quote-fee-capture-release:{quote.PublicId:N}", "예약 수수료를 채택 확정 차감으로 전환", "QUOTE_FEE_RESERVATION", quote.PublicId, null, now, actorUserId);
        row.Wallet.AvailableBalance -= row.Reservation.Amount;
        var ledger = wallets.AddLedger(row.Wallet, transactionRecord.Id, "USE", -row.Reservation.Amount,
            $"quote-fee-capture:{quote.PublicId:N}", "고객 견적 채택 수수료 확정", "FEE_CHARGE", transactionRecord.PublicId, null, now, actorUserId);
        await wallets.SaveWithConcurrencyAsync(token);
        db.FeeCharges.Add(new FeeCharge
        {
            TransactionId = transactionRecord.Id, CategoryFeePolicyId = row.Reservation.CategoryFeePolicyId,
            WalletId = row.Wallet.Id, LedgerEntryId = ledger.Id, FeeAmount = row.Reservation.Amount,
            SupplyAmount = Math.Abs(ledger.SupplyAmount), VatAmount = Math.Abs(ledger.VatAmount), IsVatIncluded = true,
            ChargedAt = now, RestoreStatusCode = "NOT_RESTORED", CreatedAt = now, CreatedByUserId = actorUserId,
            UpdatedAt = now, UpdatedByUserId = actorUserId,
        });
        row.Reservation.StatusCode = "CAPTURED";
        row.Reservation.CaptureLedgerEntryId = ledger.Id;
        row.Reservation.CapturedAt = now;
        row.Reservation.UpdatedAt = now;
        row.Reservation.UpdatedByUserId = actorUserId;
        await wallets.SaveWithConcurrencyAsync(token);
        if (dbTransaction is not null) await dbTransaction.CommitAsync(token);
        return ProviderWalletService.ToOperation(row.Wallet, ledger);
    }

    public async Task<bool> ReleaseAsync(Quote quote, string reasonCode, string reason, long? actorUserId, CancellationToken token)
    {
        var row = await (from reservation in db.QuoteFeeReservations
                         join wallet in db.ProviderWallets on reservation.WalletId equals wallet.Id
                         where reservation.QuoteId == quote.Id
                         select new { Reservation = reservation, Wallet = wallet }).SingleOrDefaultAsync(token);
        if (row is null || row.Reservation.StatusCode != "RESERVED") return false;
        if (row.Wallet.ReservedBalance < row.Reservation.Amount) throw Error("WALLET_RESERVED_BALANCE_INVALID", "예약잔액을 확인할 수 없습니다.");
        await using var transaction = await wallets.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        row.Wallet.ReservedBalance -= row.Reservation.Amount;
        row.Wallet.AvailableBalance += row.Reservation.Amount;
        row.Wallet.UpdatedAt = now;
        row.Wallet.UpdatedByUserId = actorUserId;
        var ledger = wallets.AddLedger(row.Wallet, null, "RELEASE", row.Reservation.Amount,
            $"quote-fee-release:{quote.PublicId:N}", reason, "QUOTE_FEE_RESERVATION", quote.PublicId, null, now, actorUserId);
        await wallets.SaveWithConcurrencyAsync(token);
        row.Reservation.StatusCode = "RELEASED";
        row.Reservation.ReleaseLedgerEntryId = ledger.Id;
        row.Reservation.ReleasedAt = now;
        row.Reservation.ReleaseReasonCode = reasonCode;
        row.Reservation.UpdatedAt = now;
        row.Reservation.UpdatedByUserId = actorUserId;
        await wallets.SaveWithConcurrencyAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return true;
    }

    public async Task<int> ExpireDueAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var ids = await (from quote in db.Quotes.AsNoTracking()
                         join reservation in db.QuoteFeeReservations.AsNoTracking() on quote.Id equals reservation.QuoteId
                         where reservation.StatusCode == "RESERVED" &&
                           ((quote.StatusCode == "SUBMITTED" && quote.ExpiresAt <= now) || quote.StatusCode == "NOT_SELECTED" || quote.StatusCode == "EXPIRED" || quote.StatusCode == "WITHDRAWN" || quote.StatusCode == "CANCELLED")
                         select quote.Id).Take(200).ToListAsync(token);
        foreach (var id in ids)
        {
            var quote = await db.Quotes.SingleAsync(x => x.Id == id, token);
            var expired = quote.StatusCode == "SUBMITTED" && quote.ExpiresAt <= now;
            await ReleaseAsync(quote, expired ? "QUOTE_EXPIRED" : "QUOTE_TERMINAL_RECONCILED", expired ? "견적 유효기간 만료로 예상 수수료 예약 해제" : "종료된 견적의 남은 예상 수수료 예약 정합성 복구", null, token);
            if(expired)quote.StatusCode = "EXPIRED";
            quote.UpdatedAt = now;
            await db.SaveChangesAsync(token);
        }
        return ids.Count;
    }

    private static WalletOperationException Error(string code, string message) => new(code, message, StatusCodes.Status409Conflict);
}
