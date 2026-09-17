using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminWalletApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanListWallets_WithNonnegativeUniqueKrwBalancesAndReconciliationStatus()
    {
        var baselineProviderIds = await EnsureBaselineWallets(); using var client = Client(); await Login(client, RoleCodes.Admin);
        var result = await client.GetFromJsonAsync<AdminWalletListResponse>(BasePath);
        Assert.NotNull(result); Assert.True(result.TotalCount >= 4); Assert.True(result.Summary.DevelopmentManualConfirmationAvailable);
        var baselineItems = result.Items.Where(item => baselineProviderIds.Contains(item.ProviderId)).ToList(); Assert.Equal(baselineProviderIds.Count, baselineItems.Count);
        Assert.All(baselineItems, item => { Assert.True(item.AvailableBalance >= 0); Assert.True(item.ReservedBalance >= 0); });
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var duplicates = await db.ProviderWallets.GroupBy(value => new { value.ProviderProfileId, value.CurrencyCode }).Where(value => value.Count() > 1).CountAsync(); Assert.Equal(0, duplicates);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessAdminWalletApi(string role)
    { using var client = Client(); await Login(client, role); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath)).StatusCode); }

    [Fact]
    public async Task DevelopmentCharge_IsConfirmedOnce_WithLedgerBalanceAuditAndNoExistingTradeMutation()
    {
        var provider = await CreateDedicatedWallet(); var (quotesBefore, transactionsBefore) = await TradeCounts(); using var client = Client(); await Login(client, RoleCodes.Admin);
        var request = new { Amount = 100000m, Reason = "개발환경 결제 프로세스 검증", IdempotencyKey = $"charge-{Guid.NewGuid():N}" };
        var first = await (await client.PostAsJsonAsync($"{BasePath}/{provider.ProviderId}/development-charges", request)).Content.ReadFromJsonAsync<AdminChargeRequestResponse>();
        var retried = await (await client.PostAsJsonAsync($"{BasePath}/{provider.ProviderId}/development-charges", request)).Content.ReadFromJsonAsync<AdminChargeRequestResponse>();
        Assert.Equal(first!.Id, retried!.Id);
        var confirmed = await (await client.PostAsJsonAsync($"{BasePath}/{provider.ProviderId}/development-charges/{first.Id}/confirm", new { first.RowVersion })).Content.ReadFromJsonAsync<WalletOperationResponse>();
        var repeated = await (await client.PostAsJsonAsync($"{BasePath}/{provider.ProviderId}/development-charges/{first.Id}/confirm", new { first.RowVersion })).Content.ReadFromJsonAsync<WalletOperationResponse>();
        Assert.Equal(100000m, confirmed!.AvailableBalance); Assert.Equal(confirmed.LedgerEntryId, repeated!.LedgerEntryId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var wallet = await db.ProviderWallets.SingleAsync(value => value.PublicId == provider.WalletId);
        Assert.Equal(100000m, wallet.AvailableBalance); Assert.Single(await db.WalletLedgerEntries.Where(value => value.WalletId == wallet.Id && value.EntryTypeCode == "CHARGE").ToListAsync());
        Assert.True(await db.AuditLogs.AnyAsync(value => value.EntityPublicId == wallet.PublicId && value.ActionCode == "WALLET_DEVELOPMENT_CHARGE_CONFIRMED"));
        Assert.Equal((quotesBefore, transactionsBefore), await TradeCounts());
    }

    [Fact]
    public async Task Adjustment_RequiresReasonSupportsBothDirectionsConcurrencyAndDoesNotAffectOtherWallet()
    {
        var target = await CreateDedicatedWallet(); var other = await CreateDedicatedWallet(); using var client = Client(); await Login(client, RoleCodes.Admin);
        var missing = await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/adjustments", new { DirectionCode="INCREASE", Amount=1000, Reason="", IdempotencyKey=$"adjust-{Guid.NewGuid():N}", target.RowVersion }); Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var stale = await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/adjustments", new { DirectionCode="INCREASE", Amount=1000, Reason="이용료 잔액 조정", IdempotencyKey=$"adjust-{Guid.NewGuid():N}", RowVersion="AQ==" }); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var increaseKey=$"adjust-{Guid.NewGuid():N}"; var increased = await PostOperation(client,$"{BasePath}/{target.ProviderId}/adjustments",new {DirectionCode="INCREASE",Amount=50000,Reason="재무 승인 조정",IdempotencyKey=increaseKey,target.RowVersion});
        var retried = await PostOperation(client,$"{BasePath}/{target.ProviderId}/adjustments",new {DirectionCode="INCREASE",Amount=50000,Reason="재무 승인 조정",IdempotencyKey=increaseKey,target.RowVersion}); Assert.Equal(increased.LedgerEntryId,retried.LedgerEntryId);
        var crossProvider = await client.PostAsJsonAsync($"{BasePath}/{other.ProviderId}/adjustments",new {DirectionCode="INCREASE",Amount=50000,Reason="다른 전문가 재사용 시도",IdempotencyKey=increaseKey,other.RowVersion}); Assert.Equal(HttpStatusCode.Conflict,crossProvider.StatusCode);
        var decreased = await PostOperation(client,$"{BasePath}/{target.ProviderId}/adjustments",new {DirectionCode="DECREASE",Amount=20000,Reason="오입금 정정",IdempotencyKey=$"adjust-{Guid.NewGuid():N}",RowVersion=increased.RowVersion}); Assert.Equal(30000m,decreased.AvailableBalance);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(0,(await db.ProviderWallets.SingleAsync(v=>v.PublicId==other.WalletId)).AvailableBalance);Assert.Equal(2,await db.WalletLedgerEntries.CountAsync(v=>v.WalletId==target.InternalWalletId));
    }

    [Fact]
    public async Task DebitFee_IsAtomicChecksBalanceIdempotencyAndDuplicateFeeCharge()
    {
        var target=await CreateDedicatedWallet();var transaction=await CreateTransaction(target.ProviderId);await FundByService(target,5000);using var scope=factory.Services.CreateScope();var service=scope.ServiceProvider.GetRequiredService<ProviderWalletService>();
        var command=new DebitFeeCommand(target.ProviderId,transaction.TransactionId,transaction.PolicyId,3000,$"fee-{Guid.NewGuid():N}","수요자 견적 채택 수수료");var first=await service.DebitFeeAsync(command,null,default);var retry=await service.DebitFeeAsync(command,null,default);Assert.Equal(first.LedgerEntryId,retry.LedgerEntryId);Assert.Equal(2000m,first.AvailableBalance);
        var duplicate=await Assert.ThrowsAsync<WalletOperationException>(()=>service.DebitFeeAsync(command with { IdempotencyKey=$"fee-{Guid.NewGuid():N}" },null,default));Assert.Equal("WALLET_FEE_ALREADY_CHARGED",duplicate.BusinessCode);
        var second=await CreateTransaction(target.ProviderId);var insufficient=await Assert.ThrowsAsync<WalletOperationException>(()=>service.DebitFeeAsync(new(target.ProviderId,second.TransactionId,second.PolicyId,3000,$"fee-{Guid.NewGuid():N}","수수료 차감"),null,default));Assert.Equal("WALLET_INSUFFICIENT_BALANCE",insufficient.BusinessCode);
        var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Single(await db.FeeCharges.Where(v=>v.WalletId==target.InternalWalletId).ToListAsync());Assert.Single(await db.WalletLedgerEntries.Where(v=>v.WalletId==target.InternalWalletId&&v.EntryTypeCode=="USE").ToListAsync());
    }

    [Fact]
    public async Task FeeRestore_IsReverseLedgerAndCannotRestoreTwice()
    {
        var target=await CreateDedicatedWallet();var transaction=await CreateTransaction(target.ProviderId);await FundByService(target,5000);Guid feeId;string feeRowVersion;
        using(var scope=factory.Services.CreateScope()){var service=scope.ServiceProvider.GetRequiredService<ProviderWalletService>();await service.DebitFeeAsync(new(target.ProviderId,transaction.TransactionId,transaction.PolicyId,3000,$"fee-{Guid.NewGuid():N}","채택 수수료"),null,default);var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var fee=await db.FeeCharges.SingleAsync(v=>v.WalletId==target.InternalWalletId);feeId=fee.PublicId;feeRowVersion=Convert.ToBase64String(fee.RowVersion);}
        using var client=Client();await Login(client,RoleCodes.Admin);var idempotency=$"restore-{Guid.NewGuid():N}";var restored=await PostOperation(client,$"{BasePath}/{target.ProviderId}/fee-charges/{feeId}/restore",new{ReasonCode="SYSTEM_ERROR",Reason="시스템 오류로 수수료 복원",IdempotencyKey=idempotency,RowVersion=feeRowVersion});var retry=await PostOperation(client,$"{BasePath}/{target.ProviderId}/fee-charges/{feeId}/restore",new{ReasonCode="SYSTEM_ERROR",Reason="시스템 오류로 수수료 복원",IdempotencyKey=idempotency,RowVersion=feeRowVersion});Assert.Equal(restored.LedgerEntryId,retry.LedgerEntryId);Assert.Equal(5000m,restored.AvailableBalance);
        var duplicate=await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/fee-charges/{feeId}/restore",new{ReasonCode="SYSTEM_ERROR",Reason="중복 복원 시도",IdempotencyKey=$"restore-{Guid.NewGuid():N}",RowVersion=feeRowVersion});Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);
    }

    [Fact]
    public async Task RefundWorkflow_ValidatesAmountCreatesRefundLedgerAndWithdrawalDecision()
    {
        var target=await CreateDedicatedWallet();var other=await CreateDedicatedWallet();await FundByService(target,40000);using var client=Client();await Login(client,RoleCodes.Admin);
        var excessive=await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/refunds",new{Amount=40001,Reason="잔여 이용료 환불",IdempotencyKey=$"refund-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.Conflict,excessive.StatusCode);
        var created=await (await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/refunds",new{Amount=40000,Reason="탈퇴 전 잔여 이용료 환불",IdempotencyKey=$"refund-{Guid.NewGuid():N}"})).Content.ReadFromJsonAsync<AdminRefundRequestResponse>();Assert.Equal("REQUESTED",created!.StatusCode);
        var approved=await (await client.PostAsJsonAsync($"{BasePath}/{target.ProviderId}/refunds/{created.Id}/approve",new{Reason="환불 검토 승인",created.RowVersion})).Content.ReadFromJsonAsync<AdminRefundRequestResponse>();Assert.Equal("APPROVED",approved!.StatusCode);
        var completed=await PostOperation(client,$"{BasePath}/{target.ProviderId}/refunds/{created.Id}/complete-development",new{Reason="개발용 환불 완료 확인",approved.RowVersion});Assert.Equal(0,completed.AvailableBalance);Assert.Equal("REFUND",completed.EntryTypeCode);
        var detail=await client.GetFromJsonAsync<AdminWalletDetailResponse>($"{BasePath}/{target.ProviderId}");Assert.False(detail!.Balance.WithdrawalRefundRequired);Assert.True(detail.Balance.LedgerBalanceMatches);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(0,(await db.ProviderWallets.SingleAsync(v=>v.PublicId==other.WalletId)).AvailableBalance);Assert.True(await db.AuditLogs.AnyAsync(v=>v.EntityPublicId==target.WalletId&&v.ActionCode=="WALLET_REFUND_APPROVED"));
    }

    [Fact]
    public async Task WalletLedger_IsAppendOnly()
    {
        var target=await CreateDedicatedWallet();await FundByService(target,1000);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var ledger=await db.WalletLedgerEntries.SingleAsync(v=>v.WalletId==target.InternalWalletId);ledger.Reason="기존 원장 수정 시도";var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());Assert.Contains("수정하거나 삭제할 수 없습니다",error.Message);
    }

    private async Task<HashSet<Guid>> EnsureBaselineWallets(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var providerRole=await db.Roles.SingleAsync(v=>v.Code==RoleCodes.Provider);var userIds=await db.UserRoles.Where(v=>v.RoleId==providerRole.Id&&v.RevokedAt==null).Select(v=>v.UserId).ToListAsync();var providers=await db.ProviderProfiles.Where(v=>userIds.Contains(v.UserId)).ToListAsync();foreach(var provider in providers)if(!await db.ProviderWallets.AnyAsync(v=>v.ProviderProfileId==provider.Id&&v.CurrencyCode=="KRW"))db.ProviderWallets.Add(new(){ProviderProfileId=provider.Id,CurrencyCode="KRW",StatusCode="ACTIVE",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});await db.SaveChangesAsync();return providers.Select(v=>v.PublicId).ToHashSet();}
    private async Task<TestWallet> CreateDedicatedWallet(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var user=new User{LoginId=$"wallet-provider-{Guid.NewGuid():N}",NormalizedLoginId=$"WALLET-{Guid.NewGuid():N}",PasswordHash="테스트 전용 해시",StatusCode="ACTIVE",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.Users.Add(user);await db.SaveChangesAsync();var provider=new ProviderProfile{UserId=user.Id,BusinessName="수달 테스트 전문가",ApprovalStatusCode="APPROVED",ActivityStatusCode="ACTIVE",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.ProviderProfiles.Add(provider);await db.SaveChangesAsync();var wallet=new ProviderWallet{ProviderProfileId=provider.Id,CurrencyCode="KRW",StatusCode="ACTIVE",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.ProviderWallets.Add(wallet);await db.SaveChangesAsync();return new(provider.PublicId,wallet.PublicId,wallet.Id,Convert.ToBase64String(wallet.RowVersion));}
    private async Task FundByService(TestWallet target,decimal amount){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var wallet=await db.ProviderWallets.SingleAsync(v=>v.Id==target.InternalWalletId);wallet.AvailableBalance=amount;wallet.UpdatedAt=DateTime.UtcNow;db.WalletLedgerEntries.Add(new(){WalletId=wallet.Id,EntryTypeCode="ADJUST",Amount=amount,BalanceAfter=amount,IdempotencyKey=$"test-fund-{Guid.NewGuid():N}",Reason="전용 테스트 Wallet 초기화",OccurredAt=DateTime.UtcNow,CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();}
    private async Task<TestTransaction> CreateTransaction(Guid providerId){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var provider=await db.ProviderProfiles.SingleAsync(v=>v.PublicId==providerId);var customer=await db.CustomerProfiles.FirstAsync();var category=await db.ServiceCategories.SingleAsync(v=>v.PublicId==factory.Catalog.ServiceId);var policy=await db.CategoryPolicies.FirstAsync(v=>v.CategoryId==category.Id);var feePolicy=await db.CategoryFeePolicies.FirstAsync(v=>v.CategoryId==category.Id);var area=await db.AdministrativeAreas.FirstAsync();var now=DateTime.UtcNow;var request=new ServiceRequest{CustomerProfileId=customer.Id,CategoryId=category.Id,CategoryPolicyId=policy.Id,AdministrativeAreaId=area.Id,Title="Wallet 수수료 테스트 요청",StatusCode="ACCEPTED",PolicySnapshotJson="{}",CreatedAt=now,UpdatedAt=now};db.ServiceRequests.Add(request);await db.SaveChangesAsync();var candidate=new DispatchCandidate{ServiceRequestId=request.Id,ProviderProfileId=provider.Id,StatusCode="DISPATCHED",CategoryMatch=true,AreaMatch=true,ApprovalMatch=true,EvaluatedAt=now,CreatedAt=now};db.DispatchCandidates.Add(candidate);await db.SaveChangesAsync();var dispatch=new RequestDispatch{ServiceRequestId=request.Id,ProviderProfileId=provider.Id,CandidateId=candidate.Id,StatusCode="RESPONDED",AvailableAt=now,ExpiresAt=now.AddHours(1),IdempotencyKey=$"wallet-dispatch-{Guid.NewGuid():N}",CreatedAt=now};db.RequestDispatches.Add(dispatch);await db.SaveChangesAsync();var quote=new Quote{ServiceRequestId=request.Id,ProviderProfileId=provider.Id,RequestDispatchId=dispatch.Id,StatusCode="ACCEPTED",SubmittedAt=now,AcceptedAt=now,CreatedAt=now,UpdatedAt=now};db.Quotes.Add(quote);await db.SaveChangesAsync();var revision=new QuoteRevision{QuoteId=quote.Id,RevisionNo=1,Summary="Wallet 수수료 검증",TotalAmount=50000,CurrencyCode="KRW",ValidUntil=now.AddDays(1),SubmittedAt=now,SubmittedByUserId=provider.UserId,IdempotencyKey=$"wallet-revision-{Guid.NewGuid():N}"};db.QuoteRevisions.Add(revision);await db.SaveChangesAsync();var transaction=new TransactionRecord{ServiceRequestId=request.Id,AcceptedQuoteRevisionId=revision.Id,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,CategoryId=category.Id,StatusCode="CREATED",AgreedAmount=50000,CurrencyCode="KRW",QuoteSnapshotJson="{}",CategoryPolicySnapshotJson="{}",CompletionPolicySnapshotJson="{}",CreatedAt=now,UpdatedAt=now};db.Transactions.Add(transaction);await db.SaveChangesAsync();return new(transaction.PublicId,feePolicy.PublicId);}
    private async Task<(int Quotes,int Transactions)> TradeCounts(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();return(await db.Quotes.CountAsync(),await db.Transactions.CountAsync());}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});
    private static async Task<WalletOperationResponse> PostOperation(HttpClient client,string path,object body){var response=await client.PostAsJsonAsync(path,body);response.EnsureSuccessStatusCode();return (await response.Content.ReadFromJsonAsync<WalletOperationResponse>())!;}
    private const string BasePath="/api/v1/admin/wallets";private sealed record TestWallet(Guid ProviderId,Guid WalletId,long InternalWalletId,string RowVersion);private sealed record TestTransaction(Guid TransactionId,Guid PolicyId);
}
