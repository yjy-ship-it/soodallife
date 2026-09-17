using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Advertising;

public sealed class ProviderAdvertisingRenewalService(SoodalLifeDbContext db,ProviderWalletService wallets)
{
    public async Task<ProviderAdvertisingRenewalStateResponse> SetAsync(ClaimsPrincipal principal,Guid id,bool enabled,CancellationToken token)
    {
        var access=await Access(principal,id,token);var app=access.App;var campaign=access.Campaign;var now=DateTime.UtcNow;
        if(app.StatusCode!="PUBLISHED"||app.DurationDays!=30)throw Error("PROVIDER_AD_AUTO_RENEW_NOT_AVAILABLE","게시 중인 30일 광고에서만 매월 자동 갱신을 설정할 수 있습니다.",409);
        if(!enabled){app.AutoRenewEnabled=false;app.AutoRenewStatusCode="OFF";app.NextRenewalAt=null;app.RenewalConsentRequired=false;app.AutoRenewDisabledAt=now;await CancelPending(app,"전문가가 자동 갱신을 해지했습니다.",now,token);}
        else{var quote=await Quote(app,token);app.AutoRenewEnabled=true;app.AutoRenewStatusCode="ACTIVE";app.NextRenewalAt=campaign.EndAt;app.RenewalConsentRequired=false;app.RenewalConsentAt=now;app.RenewalConsentFeeAmount=quote.Total;app.RenewalConsentPolicyFingerprint=quote.Fingerprint;app.AutoRenewDisabledAt=null;}
        app.UpdatedAt=now;app.UpdatedByUserId=access.UserId;await db.SaveChangesAsync(token);return State(app);
    }

    public async Task<ProviderAdvertisingRenewalStateResponse> ConsentAsync(ClaimsPrincipal principal,Guid id,CancellationToken token)
    {
        var access=await Access(principal,id,token);var app=access.App;if(!app.AutoRenewEnabled||app.StatusCode!="PUBLISHED")throw Error("PROVIDER_AD_AUTO_RENEW_NOT_ACTIVE","자동 갱신 중인 게시 광고가 아닙니다.",409);var quote=await Quote(app,token);var now=DateTime.UtcNow;app.RenewalConsentRequired=false;app.RenewalConsentAt=now;app.RenewalConsentFeeAmount=quote.Total;app.RenewalConsentPolicyFingerprint=quote.Fingerprint;app.AutoRenewStatusCode="ACTIVE";app.UpdatedAt=now;app.UpdatedByUserId=access.UserId;if(access.Campaign.StatusCode=="PAUSED"&&app.NextRenewalAt>DateTime.UtcNow)access.Campaign.StatusCode="ACTIVE";await db.SaveChangesAsync(token);return State(app);
    }

    public async Task<IReadOnlyList<ProviderAdvertisingRenewalHistoryResponse>> HistoryAsync(ClaimsPrincipal principal,Guid id,CancellationToken token)
    {
        var access=await Access(principal,id,token);return await db.ProviderAdvertisingRenewalHistories.AsNoTracking().Where(x=>x.ProviderAdvertisingApplicationId==access.App.Id).OrderByDescending(x=>x.CycleNo).Select(x=>new ProviderAdvertisingRenewalHistoryResponse(x.PublicId,x.CycleNo,x.DueAt,x.BaseFeeAmount,x.RegionalFeeAmount,x.FeeAmount,x.CurrencyCode,x.StatusCode,x.NoticeSentAt,x.ProcessedAt,x.FailureReason)).ToListAsync(token);
    }

    public async Task<int> ProcessDueAsync(CancellationToken token)
    {
        var now=DateTime.UtcNow;var ids=await db.ProviderAdvertisingApplications.AsNoTracking().Where(x=>x.AutoRenewEnabled&&x.StatusCode=="PUBLISHED"&&x.NextRenewalAt.HasValue&&x.NextRenewalAt<=now.AddDays(7)).OrderBy(x=>x.NextRenewalAt).Select(x=>x.Id).Take(100).ToListAsync(token);var count=0;foreach(var id in ids){try{count+=await ProcessOne(id,now,token);}catch(Exception){/* 개별 실패는 다음 자동 실행에서 재시도 */}}return count;
    }

    private async Task<int> ProcessOne(long id,DateTime now,CancellationToken token)
    {
        var app=await db.ProviderAdvertisingApplications.SingleAsync(x=>x.Id==id,token);var campaign=await db.AdvertisingCampaigns.SingleAsync(x=>x.Id==app.CampaignId,token);var providerUser=await db.ProviderProfiles.Where(x=>x.Id==app.ProviderProfileId).Select(x=>x.UserId).SingleAsync(token);var due=app.NextRenewalAt!.Value;var cycle=app.RenewalCycleNo+1;var quote=await Quote(app,token);var history=await db.ProviderAdvertisingRenewalHistories.SingleOrDefaultAsync(x=>x.ProviderAdvertisingApplicationId==app.Id&&x.CycleNo==cycle,token);
        if(history==null){history=new(){ProviderAdvertisingApplicationId=app.Id,CycleNo=cycle,DueAt=due,BaseFeeAmount=quote.Base,RegionalFeeAmount=quote.Region,FeeAmount=quote.Total,CurrencyCode=app.CurrencyCode,PolicyFingerprint=quote.Fingerprint,CreatedAt=now,UpdatedAt=now};db.ProviderAdvertisingRenewalHistories.Add(history);}
        var policyChanged=!string.Equals(app.RenewalConsentPolicyFingerprint,quote.Fingerprint,StringComparison.Ordinal)||app.RenewalConsentFeeAmount!=quote.Total;
        if(policyChanged){app.RenewalConsentRequired=true;app.AutoRenewStatusCode="CONSENT_REQUIRED";history.StatusCode="CONSENT_REQUIRED";history.FailureReason="노출 지역·위치 또는 요금 정책이 변경되어 전문가 재동의가 필요합니다.";history.UpdatedAt=now;if(due<=now)campaign.StatusCode="PAUSED";await Notify("PROVIDER_AD_RENEWAL_RECONSENT_REQUIRED",app,providerUser,cycle,quote.Total,now,token);await db.SaveChangesAsync(token);return 1;}
        if(due>now){if(history.NoticeSentAt==null){history.StatusCode="NOTICE_SENT";history.NoticeSentAt=now;history.UpdatedAt=now;app.RenewalNoticeSentAt=now;await Notify("PROVIDER_AD_RENEWAL_NOTICE",app,providerUser,cycle,quote.Total,now,token);await db.SaveChangesAsync(token);return 1;}return 0;}
        var wallet=await db.ProviderWallets.SingleAsync(x=>x.Id==app.WalletId,token);if(wallet.StatusCode!="ACTIVE"||wallet.AvailableBalance<quote.Total){app.AutoRenewStatusCode="PAUSED_INSUFFICIENT";campaign.StatusCode="PAUSED";history.StatusCode="PAUSED_INSUFFICIENT";history.ProcessedAt=now;history.FailureReason="예상 광고비보다 이용료 잔액이 부족합니다.";history.UpdatedAt=now;await Notify("PROVIDER_AD_RENEWAL_PAUSED_INSUFFICIENT",app,providerUser,cycle,quote.Total,now,token);await db.SaveChangesAsync(token);return 1;}
        await using var tx=await wallets.BeginTransactionAsync(token);wallet.AvailableBalance-=quote.Total;wallet.ReservedBalance+=quote.Total;wallet.UpdatedAt=now;wallet.UpdatedByUserId=providerUser;var reserve=wallets.AddLedger(wallet,null,"RESERVE",-quote.Total,$"provider-ad-renew-reserve:{app.PublicId:N}:{cycle}",$"광고 자동 갱신 {cycle}회차 광고비 예약","PROVIDER_ADVERTISING_APPLICATION",app.PublicId,null,now,providerUser);await wallets.SaveWithConcurrencyAsync(token);wallet.ReservedBalance-=quote.Total;wallet.AvailableBalance+=quote.Total;wallets.AddLedger(wallet,null,"RELEASE",quote.Total,$"provider-ad-renew-release:{app.PublicId:N}:{cycle}",$"광고 자동 갱신 {cycle}회차 예약 확정 전환","PROVIDER_ADVERTISING_APPLICATION",app.PublicId,null,now,providerUser);wallet.AvailableBalance-=quote.Total;var capture=wallets.AddLedger(wallet,null,"USE",-quote.Total,$"provider-ad-renew-capture:{app.PublicId:N}:{cycle}",$"광고 자동 갱신 {cycle}회차 확정 차감","PROVIDER_ADVERTISING_APPLICATION",app.PublicId,null,now,providerUser);history.ReserveLedgerEntryId=reserve.Id;history.StatusCode="RENEWED";history.ProcessedAt=now;history.FailureReason=null;history.UpdatedAt=now;app.BaseFeeAmount=quote.Base;app.RegionalFeeAmount=quote.Region;app.FeeAmount=quote.Total;app.RenewalCycleNo=cycle;app.LastRenewedAt=now;app.NextRenewalAt=due.AddMonths(1);app.RenewalNoticeSentAt=null;app.AutoRenewStatusCode="ACTIVE";campaign.EndAt=app.NextRenewalAt;campaign.StatusCode="ACTIVE";app.UpdatedAt=now;campaign.UpdatedAt=now;await wallets.SaveWithConcurrencyAsync(token);history.CaptureLedgerEntryId=capture.Id;await Notify("PROVIDER_AD_RENEWED",app,providerUser,cycle,quote.Total,now,token);await wallets.SaveWithConcurrencyAsync(token);if(tx!=null)await tx.CommitAsync(token);return 1;
    }

    private async Task<RenewalQuote> Quote(ProviderAdvertisingApplication app,CancellationToken token)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow);var placementId=await db.AdvertisingCampaignPlacements.Where(x=>x.CampaignId==app.CampaignId).Select(x=>x.PlacementId).SingleAsync(token);var rate=await db.ProviderAdvertisingRatePolicies.Where(x=>x.PlacementId==placementId&&x.DurationDays==30&&x.IsActive&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=today)).OrderByDescending(x=>x.EffectiveFrom).FirstOrDefaultAsync(token)??throw Error("PROVIDER_AD_RENEWAL_RATE_NOT_FOUND","현재 적용할 수 있는 30일 광고 요금이 없습니다.",409);var areas=await(from link in db.AdvertisingCampaignAreas where link.CampaignId==app.CampaignId join area in db.AdministrativeAreas on link.AdministrativeAreaId equals area.Id select new{area.Id,area.AreaLevelCode}).ToListAsync(token);var p=areas.Count(x=>x.AreaLevelCode=="SIDO");var d=areas.Count(x=>x.AreaLevelCode=="SIGUNGU");var raw=p*rate.ProvinceUnitAmount+d*rate.DistrictUnitAmount;var region=rate.RegionalFeeCapAmount>0?Math.Min(raw,rate.RegionalFeeCapAmount):raw;var areaIds=areas.Select(x=>x.Id).Order().ToArray();var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{rate.PublicId:N}|{rate.FixedAmount}|{rate.ProvinceUnitAmount}|{rate.DistrictUnitAmount}|{rate.RegionalFeeCapAmount}|{string.Join(',',areaIds)}")));return new(rate.FixedAmount,region,rate.FixedAmount+region,fingerprint);
    }
    private async Task Notify(string eventType,ProviderAdvertisingApplication app,long user,int cycle,decimal fee,DateTime now,CancellationToken token){if(!await db.NotificationTemplates.AsNoTracking().AnyAsync(x=>x.EventTypeCode==eventType&&x.IsActive,token))return;var key=$"provider-ad-renewal:{eventType.ToLowerInvariant()}:{app.PublicId:N}:{cycle}";if(await db.OutboxEvents.AnyAsync(x=>x.IdempotencyKey==key,token))return;db.OutboxEvents.Add(new(){AggregateType="ProviderAdvertisingApplication",AggregatePublicId=app.PublicId,EventType=eventType,PayloadJson=JsonSerializer.Serialize(new{recipientUserId=user,source_no=app.PublicId.ToString("N")[..10].ToUpperInvariant(),amount=fee,cycle}),StatusCode="PENDING",OccurredAt=now,AvailableAt=now,IdempotencyKey=key,CreatedByUserId=user});}
    private async Task CancelPending(ProviderAdvertisingApplication app,string reason,DateTime now,CancellationToken token){var pending=new[]{"PENDING","NOTICE_SENT","CONSENT_REQUIRED","PAUSED_INSUFFICIENT"};var rows=await db.ProviderAdvertisingRenewalHistories.Where(x=>x.ProviderAdvertisingApplicationId==app.Id&&pending.Contains(x.StatusCode)).ToListAsync(token);foreach(var x in rows){x.StatusCode="CANCELLED";x.FailureReason=reason;x.ProcessedAt=now;x.UpdatedAt=now;}}
    private async Task<(long UserId,ProviderAdvertisingApplication App,AdvertisingCampaign Campaign)> Access(ClaimsPrincipal principal,Guid id,CancellationToken token){if(!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var uid))throw Error("AUTH_REQUIRED","로그인이 필요합니다.",401);var row=await(from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId join app in db.ProviderAdvertisingApplications on profile.Id equals app.ProviderProfileId join campaign in db.AdvertisingCampaigns on app.CampaignId equals campaign.Id where user.PublicId==uid&&app.PublicId==id select new{user.Id,app,campaign}).SingleOrDefaultAsync(token)??throw Error("PROVIDER_AD_APPLICATION_NOT_FOUND","광고 신청을 찾을 수 없습니다.",404);return(row.Id,row.app,row.campaign);}
    private static ProviderAdvertisingRenewalStateResponse State(ProviderAdvertisingApplication x)=>new(x.PublicId,x.AutoRenewEnabled,x.AutoRenewStatusCode,x.NextRenewalAt,x.RenewalConsentRequired,x.RenewalConsentFeeAmount,x.RenewalCycleNo);
    private static ProviderAdvertisingException Error(string code,string message,int status=400)=>new(code,message,status);
    private sealed record RenewalQuote(decimal Base,decimal Region,decimal Total,string Fingerprint);
}
