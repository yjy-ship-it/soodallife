using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Wallet;

public sealed class TossWalletTopUpOptions
{
    public const string SectionName = "TossWalletTopUp";
    public string Mode { get; set; } = "DISABLED";
    public string BaseUrl { get; set; } = "https://api.tosspayments.com";
    public string ClientKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public decimal[] AllowedAmounts { get; set; } = [10_000, 50_000, 100_000, 300_000, 500_000];
    public decimal MonthlyPurchaseLimit { get; set; } = 1_000_000;
    public int PreparationLifetimeMinutes { get; set; } = 30;
    public string SuccessPath { get; set; } = "/provider/wallet?topUp=success";
    public string FailPath { get; set; } = "/provider/wallet?topUp=fail";
}

public sealed record PrepareWalletTopUpRequest(decimal Amount);
public sealed record PrepareWalletTopUpResponse(string OrderId, decimal Amount, string ClientKey, string CustomerKey, string OrderName, string SuccessPath, string FailPath, string Mode);
public sealed record ConfirmWalletTopUpRequest(string PaymentKey, string OrderId, decimal Amount);
public sealed record ConfirmWalletTopUpResponse(Guid ChargeId, string StatusCode, decimal Amount, decimal AvailableBalance, Guid? LedgerEntryId);

public sealed class TossWalletTopUpService(
    SoodalLifeDbContext db,
    ProviderWalletService walletService,
    HttpClient client,
    IOptions<TossWalletTopUpOptions> configured)
{
    private readonly TossWalletTopUpOptions options = configured.Value;

    public bool IsReady => Mode() is "TEST" or "PRODUCTION" && !string.IsNullOrWhiteSpace(options.ClientKey) && !string.IsNullOrWhiteSpace(options.SecretKey);
    public string ModeCode => Mode();

    public async Task<PrepareWalletTopUpResponse> PrepareAsync(ClaimsPrincipal principal, PrepareWalletTopUpRequest input, CancellationToken token)
    {
        EnsureReady();
        if (!AllowedAmounts().Contains(input.Amount))
            throw ProviderWalletService.Error("WALLET_TOPUP_AMOUNT_INVALID", $"이용료 선결제 상품은 {string.Join(", ", AllowedAmounts().Select(value => $"{value:N0}원"))} 중에서 선택해 주세요.");
        var owner = await OwnerAsync(principal, token);
        if (owner.Wallet.StatusCode != "ACTIVE") throw ProviderWalletService.Error("WALLET_NOT_ACTIVE", "사용 가능한 Wallet 상태가 아닙니다.", StatusCodes.Status409Conflict);
        await EnsureMonthlyLimitAsync(owner.Wallet.Id, input.Amount, null, token);
        var orderId = $"SDW-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var charge = new WalletChargeRequest
        {
            WalletId = owner.Wallet.Id, RequestedAmount = input.Amount, PaymentMethodCode = Mode() == "TEST" ? "TOSS_TEST" : "TOSS",
            StatusCode = "REQUESTED", RequestedAt = now, IdempotencyKey = $"wallet-topup:{orderId}", RequestReason = "전문가 선결제 이용료 결제",
            CreatedAt = now, CreatedByUserId = owner.UserId, UpdatedAt = now, UpdatedByUserId = owner.UserId,
        };
        db.WalletChargeRequests.Add(charge);
        await db.SaveChangesAsync(token);
        return new(orderId, input.Amount, options.ClientKey, owner.ProviderPublicId.ToString("N"), "수달 라이프 전문가 플랫폼 이용료", options.SuccessPath, options.FailPath, Mode());
    }

    public async Task<ConfirmWalletTopUpResponse> ConfirmAsync(ClaimsPrincipal principal, ConfirmWalletTopUpRequest input, CancellationToken token)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(input.PaymentKey) || string.IsNullOrWhiteSpace(input.OrderId))
            throw ProviderWalletService.Error("WALLET_TOPUP_CONFIRMATION_INVALID", "결제 승인 정보를 확인할 수 없습니다.");
        var owner = await OwnerAsync(principal, token);
        var key = $"wallet-topup:{input.OrderId.Trim()}";
        var charge = await db.WalletChargeRequests.SingleOrDefaultAsync(value => value.WalletId == owner.Wallet.Id && value.IdempotencyKey == key, token)
            ?? throw ProviderWalletService.Error("WALLET_TOPUP_NOT_FOUND", "준비된 결제 요청을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (charge.RequestedAmount != input.Amount)
            throw ProviderWalletService.Error("WALLET_TOPUP_AMOUNT_MISMATCH", "요청금액과 결제 승인금액이 일치하지 않습니다.", StatusCodes.Status409Conflict);
        if (!AllowedAmounts().Contains(charge.RequestedAmount))
            throw ProviderWalletService.Error("WALLET_TOPUP_PRODUCT_INVALID", "현재 판매 중인 이용료 선결제 상품이 아닙니다.", StatusCodes.Status409Conflict);
        if (charge.StatusCode == "SUCCEEDED")
            return new(charge.PublicId, charge.StatusCode, charge.RequestedAmount, owner.Wallet.AvailableBalance,
                charge.LedgerEntryId == null ? null : await db.WalletLedgerEntries.Where(x => x.Id == charge.LedgerEntryId).Select(x => (Guid?)x.PublicId).SingleAsync(token));

        await EnsureMonthlyLimitAsync(owner.Wallet.Id, charge.RequestedAmount, charge.Id, token);

        TossConfirmResult approved;
        try { approved = await ApproveAtTossAsync(input, token); }
        catch (TossApprovalException exception)
        {
            charge.StatusCode = "FAILED"; charge.FailedAt = DateTime.UtcNow; charge.FailureReason = exception.SafeMessage; charge.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(token);
            throw ProviderWalletService.Error("TOSS_APPROVAL_FAILED", exception.SafeMessage, StatusCodes.Status402PaymentRequired);
        }
        if (approved.OrderId != input.OrderId || approved.TotalAmount != input.Amount || approved.Status != "DONE")
            throw ProviderWalletService.Error("TOSS_APPROVAL_MISMATCH", "토스 승인 결과가 결제 요청과 일치하지 않습니다.", StatusCodes.Status409Conflict);

        await using var transaction = await walletService.BeginTransactionAsync(token);
        var existing = await db.WalletLedgerEntries.SingleOrDefaultAsync(value => value.IdempotencyKey == key, token);
        if (existing is null)
        {
            owner.Wallet.AvailableBalance += charge.RequestedAmount;
            owner.Wallet.UpdatedAt = DateTime.UtcNow; owner.Wallet.UpdatedByUserId = owner.UserId;
            existing = walletService.AddLedger(owner.Wallet, null, "CHARGE", charge.RequestedAmount, key, "토스페이먼츠 이용료 결제 승인",
                "WALLET_CHARGE", charge.PublicId, charge.PaymentMethodCode, DateTime.UtcNow, owner.UserId);
            await db.SaveChangesAsync(token);
        }
        charge.StatusCode = "SUCCEEDED"; charge.CompletedAt = DateTime.UtcNow; charge.ExternalPaymentReference = approved.PaymentKey;
        charge.LedgerEntryId = existing.Id; charge.FailureReason = null; charge.UpdatedAt = DateTime.UtcNow; charge.UpdatedByUserId = owner.UserId;
        await walletService.SaveWithConcurrencyAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(charge.PublicId, charge.StatusCode, charge.RequestedAmount, owner.Wallet.AvailableBalance, existing.PublicId);
    }

    private async Task<TossConfirmResult> ApproveAtTossAsync(ConfirmWalletTopUpRequest input, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/v1/payments/confirm");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.SecretKey}:")));
        request.Headers.TryAddWithoutValidation("Idempotency-Key", input.OrderId);
        request.Content = JsonContent.Create(new { paymentKey = input.PaymentKey, orderId = input.OrderId, amount = input.Amount });
        try
        {
            using var response = await client.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode) throw new TossApprovalException(SafeTossError(body));
            return JsonSerializer.Deserialize<TossConfirmResult>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new TossApprovalException("토스 승인 응답을 확인할 수 없습니다.");
        }
        catch (HttpRequestException) { throw new TossApprovalException("토스페이먼츠 승인 서버에 연결하지 못했습니다."); }
        catch (TaskCanceledException) { throw new TossApprovalException("토스페이먼츠 승인 시간이 초과되었습니다."); }
        catch (JsonException) { throw new TossApprovalException("토스 승인 응답 형식이 올바르지 않습니다."); }
    }

    private async Task<Owner> OwnerAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
            throw ProviderWalletService.Error("PROVIDER_IDENTITY_INVALID", "전문가 로그인 정보를 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
        return await (from user in db.Users join provider in db.ProviderProfiles on user.Id equals provider.UserId join wallet in db.ProviderWallets on provider.Id equals wallet.ProviderProfileId
                      where user.PublicId == publicId select new Owner(user.Id, provider.PublicId, wallet)).SingleOrDefaultAsync(token)
            ?? throw ProviderWalletService.Error("PROVIDER_WALLET_NOT_FOUND", "전문가 Wallet을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    }
    private async Task EnsureMonthlyLimitAsync(long walletId, decimal requestedAmount, long? currentChargeId, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var koreaNow = now.AddHours(9);
        var monthStartUtc = new DateTime(koreaNow.Year, koreaNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-9);
        var activeRequestStart = now.AddMinutes(-Math.Max(5, options.PreparationLifetimeMinutes));
        var reservedOrPaid = await db.WalletChargeRequests.AsNoTracking()
            .Where(value => value.WalletId == walletId && value.Id != currentChargeId &&
                ((value.StatusCode == "SUCCEEDED" && value.CompletedAt >= monthStartUtc) ||
                 (value.StatusCode == "REQUESTED" && value.RequestedAt >= activeRequestStart)))
            .SumAsync(value => (decimal?)value.RequestedAmount, token) ?? 0;
        if (reservedOrPaid + requestedAmount > options.MonthlyPurchaseLimit)
            throw ProviderWalletService.Error("WALLET_TOPUP_MONTHLY_LIMIT_EXCEEDED", $"월 이용료 선결제 한도는 {options.MonthlyPurchaseLimit:N0}원입니다. 이번 달 결제 및 진행 중인 결제를 확인해 주세요.", StatusCodes.Status409Conflict);
    }
    private void EnsureReady() { if (!IsReady) throw ProviderWalletService.Error("WALLET_TOPUP_NOT_READY", "이용료 결제 기능이 아직 활성화되지 않았습니다.", StatusCodes.Status503ServiceUnavailable); }
    private string Mode() => options.Mode.Trim().ToUpperInvariant();
    private decimal[] AllowedAmounts() => options.AllowedAmounts.Where(value => value > 0 && value == decimal.Truncate(value)).Distinct().Order().ToArray();
    private static string SafeTossError(string body) { try { using var json = JsonDocument.Parse(body); return json.RootElement.TryGetProperty("message", out var value) ? value.GetString() ?? "결제 승인이 거절되었습니다." : "결제 승인이 거절되었습니다."; } catch { return "결제 승인이 거절되었습니다."; } }
    private sealed record Owner(long UserId, Guid ProviderPublicId, ProviderWallet Wallet);
    private sealed record TossConfirmResult(string PaymentKey, string OrderId, decimal TotalAmount, string Status);
    private sealed class TossApprovalException(string safeMessage) : Exception(safeMessage) { public string SafeMessage { get; } = safeMessage; }
}
