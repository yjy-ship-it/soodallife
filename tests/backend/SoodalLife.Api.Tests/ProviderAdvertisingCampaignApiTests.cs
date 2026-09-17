using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Advertising;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Tests;

public sealed class ProviderAdvertisingCampaignApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    private const string ProviderRoot="/api/v1/providers/me/advertising-campaigns",AdminRoot="/api/v1/admin/provider-campaigns";

    [Fact]
    public async Task FixedRateDefaults_AreAvailable_AndExistingContentMenuRemainsAuthorized()
    {
        await EnsureRates();
        using var provider=Client();await Login(provider,RoleCodes.Provider);var rates=await provider.GetFromJsonAsync<List<ProviderAdvertisingRateResponse>>($"{ProviderRoot}/rates");Assert.Equal(9,rates!.Count);Assert.Contains(rates,x=>x.PlacementCode=="CUSTOMER_HOME"&&x.DurationDays==7&&x.FixedAmount==35000&&x.DistrictUnitAmount==2000&&x.ProvinceUnitAmount==10000&&x.RegionalFeeCapAmount==30000);Assert.Contains(rates,x=>x.PlacementCode=="PROVIDER_HOME"&&x.DurationDays==30&&x.FixedAmount==66000);Assert.Contains(rates,x=>x.PlacementCode=="CUSTOMER_LIVE_ACTIVITY_FEED"&&x.DurationDays==30&&x.FixedAmount==88000);Assert.Equal(HttpStatusCode.OK,(await provider.GetAsync($"{ProviderRoot}/areas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await provider.GetAsync(AdminRoot)).StatusCode);
        using var admin=Client();await Login(admin,RoleCodes.Admin);Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync("/api/v1/admin/advertising-content/placements")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync(AdminRoot)).StatusCode);
    }

    [Fact]
    public async Task ApplyRejectResubmitApprovePublish_UsesWalletReservationAndCapture()
    {
        await EnsureRates();
        decimal initial;long walletId;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var user=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var profile=await db.ProviderProfiles.SingleAsync(x=>x.UserId==user.Id);var wallet=await db.ProviderWallets.SingleAsync(x=>x.ProviderProfileId==profile.Id);wallet.AvailableBalance=500000;wallet.ReservedBalance=0;await db.SaveChangesAsync();initial=wallet.AvailableBalance;walletId=wallet.Id;}
        using var provider=Client();await Login(provider,RoleCodes.Provider);var rates=(await provider.GetFromJsonAsync<List<ProviderAdvertisingRateResponse>>($"{ProviderRoot}/rates"))!;var rate=rates.Single(x=>x.PlacementCode=="CUSTOMER_HOME"&&x.DurationDays==7);var payload=Input(rate.Id,"100% 보장 광고",null);var apply=await provider.PostAsJsonAsync(ProviderRoot,payload);apply.EnsureSuccessStatusCode();var app=(await apply.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("SUBMITTED",app.StatusCode);Assert.Equal("RESERVED",app.FeeStatusCode);Assert.Equal(35000,app.BaseFeeAmount);Assert.Equal(2000,app.RegionalFeeAmount);Assert.Equal(37000,app.FeeAmount);await Wallet(initial-37000,37000);
        using var admin=Client();await Login(admin,RoleCodes.Admin);var reject=await admin.PostAsJsonAsync($"{AdminRoot}/{app.Id}/review",new{ActionCode="REJECT",Reason="광고 문구를 구체적으로 보완해 주세요."});reject.EnsureSuccessStatusCode();app=(await reject.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("REJECTED",app.StatusCode);Assert.Equal("RELEASED",app.FeeStatusCode);await Wallet(initial,0);
        var resubmit=await provider.PutAsJsonAsync($"{ProviderRoot}/{app.Id}/resubmit",Input(rate.Id,"첫 전문가 광고 보완","서비스 범위와 혜택 문구를 구체적으로 수정했습니다."));resubmit.EnsureSuccessStatusCode();app=(await resubmit.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("RESUBMITTED",app.StatusCode);await Wallet(initial-37000,37000);
        var approve=await admin.PostAsJsonAsync($"{AdminRoot}/{app.Id}/review",new{ActionCode="APPROVE",Reason=(string?)null});approve.EnsureSuccessStatusCode();app=(await approve.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("APPROVED",app.StatusCode);await Wallet(initial-37000,37000);
        var publish=await admin.PostAsJsonAsync($"{AdminRoot}/{app.Id}/publish",new{Note="게시 승인"});publish.EnsureSuccessStatusCode();app=(await publish.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("PUBLISHED",app.StatusCode);Assert.Equal("CAPTURED",app.FeeStatusCode);await Wallet(initial-37000,0);
        Guid providerId;using(var verify=factory.Services.CreateScope()){var check=verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await check.ProviderAdvertisingApplications.SingleAsync(x=>x.PublicId==app.Id);var campaign=await check.AdvertisingCampaigns.SingleAsync(x=>x.Id==stored.CampaignId);Assert.Equal("ACTIVE",campaign.StatusCode);campaign.StartAt=DateTime.UtcNow.AddMinutes(-1);Assert.Contains(await check.WalletLedgerEntries.Where(x=>x.WalletId==walletId&&x.ReferencePublicId==app.Id).ToListAsync(),x=>x.EntryTypeCode=="USE"&&x.Amount==-37000);providerId=await check.ProviderProfiles.Where(x=>x.Id==stored.ProviderProfileId).Select(x=>x.PublicId).SingleAsync();await check.SaveChangesAsync();}
        using(var anonymous=Client()){var profile=await anonymous.GetFromJsonAsync<PublicProviderProfileResponse>($"/api/v1/public/providers/{providerId}?campaignId={app.CampaignId}");Assert.NotNull(profile?.Campaign);Assert.Equal("방문 일정과 작업 범위를 친절하게 안내합니다.",profile!.Campaign!.BodyText);}

        async Task Wallet(decimal available,decimal reserved){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var wallet=await db.ProviderWallets.AsNoTracking().SingleAsync(x=>x.Id==walletId);Assert.Equal(available,wallet.AvailableBalance);Assert.Equal(reserved,wallet.ReservedBalance);}
    }

    [Fact]
    public async Task MonthlyAutoRenew_NoticesCapturesAndPausesWhenBalanceIsInsufficient()
    {
        await EnsureRates();
        using var provider=Client();await Login(provider,RoleCodes.Provider);var rates=(await provider.GetFromJsonAsync<List<ProviderAdvertisingRateResponse>>($"{ProviderRoot}/rates"))!;var rate=rates.Single(x=>x.PlacementCode=="CUSTOMER_HOME"&&x.DurationDays==30);using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var user=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var profile=await db.ProviderProfiles.SingleAsync(x=>x.UserId==user.Id);var wallet=await db.ProviderWallets.SingleAsync(x=>x.ProviderProfileId==profile.Id);wallet.AvailableBalance=500000;wallet.ReservedBalance=0;await db.SaveChangesAsync();}var apply=await provider.PostAsJsonAsync(ProviderRoot,Input(rate.Id,"자동 갱신 광고",null,true));Assert.True(apply.IsSuccessStatusCode,await apply.Content.ReadAsStringAsync());var app=(await apply.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;
        Assert.Equal("PUBLISHED",app.StatusCode);Assert.Equal("CAPTURED",app.FeeStatusCode);Assert.True(app.AutoRenewEnabled);Assert.Equal("ACTIVE",app.AutoRenewStatusCode);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await db.ProviderAdvertisingApplications.SingleAsync(x=>x.PublicId==app.Id);var campaign=await db.AdvertisingCampaigns.SingleAsync(x=>x.Id==stored.CampaignId);campaign.StartAt=DateTime.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();}
        using(var anonymous=Client()){var publicAds=await anonymous.GetFromJsonAsync<List<PublicAdvertisingCreative>>($"/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME&categoryId={factory.Catalog.ServiceId}&areaId={factory.Catalog.AreaId}");Assert.Contains(publicAds!,x=>x.CampaignId==app.CampaignId&&x.ProviderId.HasValue);}
        long walletId;decimal beforeRenewal;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await db.ProviderAdvertisingApplications.SingleAsync(x=>x.PublicId==app.Id);stored.NextRenewalAt=DateTime.UtcNow.AddMinutes(-1);var campaign=await db.AdvertisingCampaigns.SingleAsync(x=>x.Id==stored.CampaignId);campaign.EndAt=stored.NextRenewalAt.Value;var wallet=await db.ProviderWallets.SingleAsync(x=>x.Id==stored.WalletId);wallet.AvailableBalance=500000;wallet.ReservedBalance=0;walletId=wallet.Id;beforeRenewal=wallet.AvailableBalance;await db.SaveChangesAsync();}
        using(var scope=factory.Services.CreateScope()){var service=scope.ServiceProvider.GetRequiredService<ProviderAdvertisingRenewalService>();Assert.True(await service.ProcessDueAsync(CancellationToken.None)>0);}
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await db.ProviderAdvertisingApplications.SingleAsync(x=>x.PublicId==app.Id);Assert.Equal(1,stored.RenewalCycleNo);Assert.Equal("ACTIVE",stored.AutoRenewStatusCode);var history=await db.ProviderAdvertisingRenewalHistories.SingleAsync(x=>x.ProviderAdvertisingApplicationId==stored.Id&&x.CycleNo==1);Assert.Equal("RENEWED",history.StatusCode);Assert.NotNull(history.ReserveLedgerEntryId);Assert.NotNull(history.CaptureLedgerEntryId);var wallet=await db.ProviderWallets.SingleAsync(x=>x.Id==walletId);Assert.Equal(beforeRenewal-stored.FeeAmount,wallet.AvailableBalance);Assert.Equal(0,wallet.ReservedBalance);stored.NextRenewalAt=DateTime.UtcNow.AddMinutes(-1);wallet.AvailableBalance=0;await db.SaveChangesAsync();}
        using(var scope=factory.Services.CreateScope()){var service=scope.ServiceProvider.GetRequiredService<ProviderAdvertisingRenewalService>();Assert.True(await service.ProcessDueAsync(CancellationToken.None)>0);}
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await db.ProviderAdvertisingApplications.SingleAsync(x=>x.PublicId==app.Id);Assert.Equal("PAUSED_INSUFFICIENT",stored.AutoRenewStatusCode);Assert.Equal("PAUSED",await db.AdvertisingCampaigns.Where(x=>x.Id==stored.CampaignId).Select(x=>x.StatusCode).SingleAsync());Assert.Equal("PAUSED_INSUFFICIENT",await db.ProviderAdvertisingRenewalHistories.Where(x=>x.ProviderAdvertisingApplicationId==stored.Id&&x.CycleNo==2).Select(x=>x.StatusCode).SingleAsync());}
    }

    [Fact]
    public async Task Application_CalculatesEndAt_AndValidatesPreviewDestinationAndButton()
    {
        await EnsureRates();using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var user=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var profile=await db.ProviderProfiles.SingleAsync(x=>x.UserId==user.Id);var wallet=await db.ProviderWallets.SingleAsync(x=>x.ProviderProfileId==profile.Id);wallet.AvailableBalance=500000;wallet.ReservedBalance=0;await db.SaveChangesAsync();}
        using var provider=Client();await Login(provider,RoleCodes.Provider);var rate=(await provider.GetFromJsonAsync<List<ProviderAdvertisingRateResponse>>($"{ProviderRoot}/rates"))!.Single(x=>x.PlacementCode=="CUSTOMER_HOME"&&x.DurationDays==7);var start=DateTime.UtcNow.AddHours(2);
        var payload=new{RatePolicyId=rate.Id,CampaignName="외부 연결 광고",StartAt=start,Title="홈페이지에서 자세히 확인하세요",Subtitle="수달 파트너스 광고",BodyText="외부 서비스 소개 페이지로 이동합니다.",TemplateCode="major-home-repair",ButtonText="자세히 보기",DestinationTypeCode="EXTERNAL_URL",DestinationValue="https://example.com/service",CategoryIds=new[]{factory.Catalog.ServiceId},AreaIds=new[]{factory.Catalog.AreaId},AutoRenewEnabled=false,SupplementNote=(string?)null};
        var response=await provider.PostAsJsonAsync(ProviderRoot,payload);Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());var app=(await response.Content.ReadFromJsonAsync<ProviderAdvertisingApplicationResponse>())!;Assert.Equal("EXTERNAL_URL",app.DestinationTypeCode);Assert.Equal("https://example.com/service",app.DestinationValue);Assert.InRange((app.EndAt-app.StartAt).TotalDays,6.999,7.001);
        var noButton=await provider.PostAsJsonAsync(ProviderRoot,new{payload.RatePolicyId,CampaignName="버튼 없는 광고",StartAt=start,Title="버튼 없음",payload.Subtitle,payload.BodyText,ButtonText="",payload.DestinationTypeCode,payload.DestinationValue,payload.CategoryIds,payload.AreaIds,payload.AutoRenewEnabled,payload.SupplementNote});Assert.Equal(HttpStatusCode.BadRequest,noButton.StatusCode);
        var unsafeInternal=await provider.PostAsJsonAsync(ProviderRoot,new{payload.RatePolicyId,CampaignName="허용되지 않은 내부 경로",StartAt=start,Title="내부 경로",payload.Subtitle,payload.BodyText,ButtonText="자세히 보기",DestinationTypeCode="INTERNAL_PATH",DestinationValue="/admin",payload.CategoryIds,payload.AreaIds,payload.AutoRenewEnabled,payload.SupplementNote});Assert.Equal(HttpStatusCode.BadRequest,unsafeInternal.StatusCode);
    }

    [Fact]
    public async Task ProviderProfile_IsPublic_AndCustomerConsultationCreatesOneReusableRoomAndNotificationEvent()
    {
        Guid providerId;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var user=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);
            var profile=await db.ProviderProfiles.SingleAsync(x=>x.UserId==user.Id);
            profile.BusinessName="우리 동네 수리 전문가";profile.PublicIntroductionHtml="<p>검증된 전문가 소개입니다.</p>";
            providerId=profile.PublicId;
            if(!await db.NotificationTemplates.AnyAsync(x=>x.TemplateCode=="PROVIDER_CONSULTATION_REQUESTED"&&x.ChannelCode=="WEB"))
                db.NotificationTemplates.Add(new NotificationTemplate{TemplateCode="PROVIDER_CONSULTATION_REQUESTED",Name="광고 상담 신청",AudienceTypeCode="PROVIDER",EventTypeCode="CUSTOMER_CONSULTATION_REQUESTED",ChannelCode="WEB",TitleTemplate="새 채팅 상담 신청",BodyTemplate="{{customer_name}} 고객이 상담 신청하였습니다.",AllowedVariablesJson="[\"customer_name\"]",IsRequiredBusinessNotice=true,IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});
            await db.SaveChangesAsync();
        }

        using var anonymous=Client();
        var publicProfile=await anonymous.GetFromJsonAsync<PublicProviderProfileResponse>($"/api/v1/public/providers/{providerId}");
        Assert.NotNull(publicProfile);Assert.Equal("우리 동네 수리 전문가",publicProfile!.BusinessName);

        using var customer=Client();await Login(customer,RoleCodes.Customer);
        var first=await customer.PostAsync($"/api/v1/chat/provider-consultations/{providerId}/room",null);Assert.True(first.IsSuccessStatusCode,await first.Content.ReadAsStringAsync());
        var room=await first.Content.ReadFromJsonAsync<ChatRoomDetail>();Assert.NotNull(room);Assert.Equal("PROVIDER_CONSULTATION",room!.ResourceTypeCode);
        var second=await customer.PostAsync($"/api/v1/chat/provider-consultations/{providerId}/room",null);second.EnsureSuccessStatusCode();
        Assert.Equal(room.Id,(await second.Content.ReadFromJsonAsync<ChatRoomDetail>())!.Id);
        var sent=await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/messages/text",new{Body="안녕하세요. 상담을 신청합니다.",IdempotencyKey=Guid.NewGuid().ToString("N")});
        Assert.True(sent.IsSuccessStatusCode,await sent.Content.ReadAsStringAsync());
        Assert.Equal("안녕하세요. 상담을 신청합니다.",(await sent.Content.ReadFromJsonAsync<ChatMessageResponse>())!.Body);
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.Equal(1,await db.OutboxEvents.CountAsync(x=>x.EventType=="CUSTOMER_CONSULTATION_REQUESTED"&&x.AggregatePublicId==room.Id));
        }
    }

    private object Input(Guid rate,string name,string? note,bool autoRenew=false)=>new{RatePolicyId=rate,CampaignName=name,StartAt=DateTime.UtcNow.AddHours(1),Title="우리 동네 수리 전문가",Subtitle="승인된 수달 파트너스",BodyText="방문 일정과 작업 범위를 친절하게 안내합니다.",ButtonText="프로필 보기",DestinationTypeCode="INTERNAL_PATH",DestinationValue="/provider-profile",CategoryIds=new[]{factory.Catalog.ServiceId},AreaIds=new[]{factory.Catalog.AreaId},AutoRenewEnabled=autoRenew,SupplementNote=note};
    private async Task EnsureRates(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();if(await db.ProviderAdvertisingRatePolicies.AnyAsync())return;var placements=await db.AdvertisingPlacements.OrderBy(x=>x.Id).ToListAsync();var at=DateTime.UtcNow;var from=DateOnly.FromDateTime(at);var amounts=new[]{new[]{35000m,60000m,110000m},new[]{21000m,36000m,66000m},new[]{28000m,48000m,88000m}};var days=new[]{7,14,30};for(var p=0;p<placements.Count;p++)for(var d=0;d<days.Length;d++)db.ProviderAdvertisingRatePolicies.Add(new ProviderAdvertisingRatePolicy{PlacementId=placements[p].Id,DurationDays=days[d],FixedAmount=amounts[p][d],CurrencyCode="KRW",EffectiveFrom=from,IsActive=true,CreatedAt=at,UpdatedAt=at});await db.SaveChangesAsync();}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});
}
