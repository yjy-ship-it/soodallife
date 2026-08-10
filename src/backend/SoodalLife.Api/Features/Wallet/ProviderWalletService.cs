using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Wallet;

public sealed class ProviderWalletService(SoodalLifeDbContext dbContext)
{
    public async Task<WalletBalanceResponse?> GetBalanceAsync(Guid providerId, decimal requiredAmount, CancellationToken cancellationToken)
    {
        if (requiredAmount < 0) throw Error("WALLET_REQUIRED_AMOUNT_INVALID", "확인할 금액은 0원 이상이어야 합니다.");
        var row = await (from provider in dbContext.ProviderProfiles.AsNoTracking()
                         join wallet in dbContext.ProviderWallets.AsNoTracking() on provider.Id equals wallet.ProviderProfileId
                         where provider.PublicId == providerId && wallet.CurrencyCode == "KRW"
                         select new { ProviderId = provider.PublicId, Wallet = wallet }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new(row.Wallet.PublicId, row.ProviderId, row.Wallet.CurrencyCode.Trim(), row.Wallet.AvailableBalance,
            row.Wallet.ReservedBalance, row.Wallet.StatusCode, row.Wallet.StatusCode == "ACTIVE" && row.Wallet.AvailableBalance >= requiredAmount,
            Convert.ToBase64String(row.Wallet.RowVersion));
    }

    public async Task<bool> HasSufficientBalanceAsync(Guid providerId, decimal requiredAmount, CancellationToken cancellationToken) =>
        (await GetBalanceAsync(providerId, requiredAmount, cancellationToken))?.HasSufficientBalance == true;

    public async Task<WalletOperationResponse> DebitFeeAsync(DebitFeeCommand command, long? actorUserId, CancellationToken cancellationToken)
    {
        ValidatePositive(command.Amount, "수수료");
        ValidateKey(command.IdempotencyKey);
        var reason = RequiredReason(command.Reason);
        var row = await (from provider in dbContext.ProviderProfiles
                         join wallet in dbContext.ProviderWallets on provider.Id equals wallet.ProviderProfileId
                         join transaction in dbContext.Transactions on provider.Id equals transaction.ProviderProfileId
                         join policy in dbContext.CategoryFeePolicies on transaction.CategoryId equals policy.CategoryId
                         where provider.PublicId == command.ProviderId && transaction.PublicId == command.TransactionId &&
                               policy.PublicId == command.CategoryFeePolicyId && wallet.CurrencyCode == transaction.CurrencyCode &&
                               wallet.CurrencyCode == policy.CurrencyCode
                         select new { Wallet = wallet, Transaction = transaction, Policy = policy }).SingleOrDefaultAsync(cancellationToken)
            ?? throw Error("WALLET_FEE_CONTEXT_INVALID", "공급자, 거래, 수수료정책 또는 통화 연결을 확인할 수 없습니다.", StatusCodes.Status404NotFound);
        var existing = await ExistingOperationAsync(command.IdempotencyKey, row.Wallet.Id, "USE", -command.Amount, row.Transaction.PublicId, cancellationToken);
        if (existing is not null) return existing;
        if (row.Wallet.StatusCode != "ACTIVE") throw Error("WALLET_NOT_ACTIVE", "사용 가능한 Wallet 상태가 아닙니다.", StatusCodes.Status409Conflict);
        if (await dbContext.FeeCharges.AnyAsync(value => value.TransactionId == row.Transaction.Id && value.CategoryFeePolicyId == row.Policy.Id, cancellationToken))
            throw Error("WALLET_FEE_ALREADY_CHARGED", "해당 거래의 수수료가 이미 차감되었습니다.", StatusCodes.Status409Conflict);
        if (row.Wallet.AvailableBalance < command.Amount) throw Error("WALLET_INSUFFICIENT_BALANCE", "충전금 잔액이 부족합니다.", StatusCodes.Status409Conflict);

        await using var dbTransaction = await BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        row.Wallet.AvailableBalance -= command.Amount;
        row.Wallet.UpdatedAt = now;
        row.Wallet.UpdatedByUserId = actorUserId;
        var ledger = AddLedger(row.Wallet, row.Transaction.Id, "USE", -command.Amount, command.IdempotencyKey, reason,
            "FEE_CHARGE", row.Transaction.PublicId, null, now, actorUserId);
        await SaveWithConcurrencyAsync(cancellationToken);
        dbContext.FeeCharges.Add(new FeeCharge
        {
            TransactionId = row.Transaction.Id, CategoryFeePolicyId = row.Policy.Id, WalletId = row.Wallet.Id,
            LedgerEntryId = ledger.Id, FeeAmount = command.Amount, ChargedAt = now, RestoreStatusCode = "NOT_RESTORED",
            CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId,
        });
        await SaveWithConcurrencyAsync(cancellationToken);
        if (dbTransaction is not null) await dbTransaction.CommitAsync(cancellationToken);
        return ToOperation(row.Wallet, ledger);
    }

    internal WalletLedgerEntry AddLedger(ProviderWallet wallet, long? transactionId, string entryType, decimal amount,
        string idempotencyKey, string reason, string? referenceType, Guid? referencePublicId, string? paymentMethod,
        DateTime occurredAt, long? actorUserId)
    {
        var entry = new WalletLedgerEntry
        {
            WalletId = wallet.Id, TransactionId = transactionId, EntryTypeCode = entryType, Amount = amount,
            BalanceAfter = wallet.AvailableBalance, IdempotencyKey = idempotencyKey.Trim(), Reason = reason,
            ReferenceType = referenceType, ReferencePublicId = referencePublicId, PaymentMethodCode = paymentMethod,
            OccurredAt = occurredAt, CreatedAt = occurredAt, CreatedByUserId = actorUserId,
        };
        dbContext.WalletLedgerEntries.Add(entry);
        return entry;
    }

    internal async Task<WalletOperationResponse?> ExistingOperationAsync(string idempotencyKey, long walletId, string entryType,
        decimal amount, Guid? referencePublicId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.WalletLedgerEntries.AsNoTracking().SingleOrDefaultAsync(value => value.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);
        if (entry is null) return null;
        if (entry.WalletId != walletId || entry.EntryTypeCode != entryType || entry.Amount != amount || entry.ReferencePublicId != referencePublicId)
            throw Error("WALLET_IDEMPOTENCY_CONFLICT", "같은 멱등성 키가 다른 금전 작업에 사용되었습니다.", StatusCodes.Status409Conflict);
        var wallet = await dbContext.ProviderWallets.AsNoTracking().SingleAsync(value => value.Id == entry.WalletId, cancellationToken);
        return ToOperation(wallet, entry);
    }

    internal async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

    internal async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw Error("WALLET_CONCURRENCY_CONFLICT", "다른 작업이 먼저 잔액을 변경했습니다. 최신 상태를 다시 확인해 주세요.", StatusCodes.Status409Conflict); }
        catch (DbUpdateException) { throw Error("WALLET_DUPLICATE_OPERATION", "이미 처리된 금전 작업입니다.", StatusCodes.Status409Conflict); }
    }

    internal static WalletOperationResponse ToOperation(ProviderWallet wallet, WalletLedgerEntry entry) =>
        new(wallet.PublicId, wallet.AvailableBalance, wallet.ReservedBalance, entry.PublicId, entry.EntryTypeCode,
            entry.Amount, entry.BalanceAfter, Convert.ToBase64String(wallet.RowVersion));
    internal static string RequiredReason(string? value) => string.IsNullOrWhiteSpace(value)
        ? throw Error("WALLET_REASON_REQUIRED", "처리 사유를 입력해 주세요.") : value.Trim().Length > 1000
            ? throw Error("WALLET_REASON_TOO_LONG", "처리 사유는 1,000자 이하여야 합니다.") : value.Trim();
    internal static void ValidatePositive(decimal amount, string label) { if (amount <= 0) throw Error("WALLET_AMOUNT_INVALID", $"{label} 금액은 0원보다 커야 합니다."); }
    internal static void ValidateKey(string value) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 150) throw Error("WALLET_IDEMPOTENCY_KEY_INVALID", "멱등성 키를 확인해 주세요."); }
    internal static WalletOperationException Error(string code, string message, int status = StatusCodes.Status400BadRequest) => new(code, message, status);
}
