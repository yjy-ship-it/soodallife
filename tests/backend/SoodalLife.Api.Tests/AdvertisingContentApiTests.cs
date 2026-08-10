using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdvertisingContentApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private const string AdminRoot = "/api/v1/admin/advertising-content";

    [Fact]
    public async Task Admin_CanReadConfirmedPlacements()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var placements = await client.GetFromJsonAsync<List<AdvertisingPlacementResponse>>($"{AdminRoot}/placements");
        Assert.Contains(placements!, item => item.Code == "CUSTOMER_HOME" && item.RouteHint == "/customer");
        Assert.Contains(placements!, item => item.Code == "PROVIDER_HOME" && item.RouteHint == "/provider");
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessAdvertisingManagement(string role)
    {
        using var client = Client(); await Login(client, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{AdminRoot}/campaigns")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{AdminRoot}/contents")).StatusCode);
    }

    [Fact]
    public async Task Campaign_RequiresSafeDestination_AndApproval_ThenMatchesAudienceAndPlacement()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var unsafeResponse = await client.PostAsJsonAsync($"{AdminRoot}/campaigns", CampaignInput("안전하지 않은 연결 검증", "CUSTOMER", "CUSTOMER_HOME", "EXTERNAL_URL", "javascript:alert(1)"));
        Assert.Equal(HttpStatusCode.BadRequest, unsafeResponse.StatusCode);

        var campaign = await CreateCampaign(client, "고객 안전수칙 안내", "CUSTOMER", "CUSTOMER_HOME");
        Assert.Equal("DRAFT", campaign.ReviewStatusCode);
        var creativeResponse = await client.PostAsJsonAsync($"{AdminRoot}/campaigns/{campaign.Id}/creatives", new
        {
            Title = "서비스 요청 전 안전수칙", Subtitle = (string?)null, BodyText = "작업 범위와 방문 일정을 먼저 확인해 주세요.", FileId = (Guid?)null,
            AltText = (string?)null, ButtonText = "요청 확인", DestinationTypeCode = "INTERNAL_PATH", DestinationValue = "/customer/requests",
            DisplayOrder = 0, StatusCode = "ACTIVE"
        });
        Assert.Equal(HttpStatusCode.OK, creativeResponse.StatusCode);
        var approved = await ReviewCampaign(client, campaign.Id, "APPROVE", null);
        Assert.Equal("APPROVED", approved.ReviewStatusCode);

        var customerItems = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>("/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME");
        Assert.Contains(customerItems!, item => item.CampaignId == campaign.Id && item.Title == "서비스 요청 전 안전수칙");
        var providerItems = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>("/api/v1/public/advertising?audience=PROVIDER&placement=CUSTOMER_HOME");
        Assert.DoesNotContain(providerItems!, item => item.CampaignId == campaign.Id);
        var wrongPlacement = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>("/api/v1/public/advertising?audience=CUSTOMER&placement=PROVIDER_HOME");
        Assert.DoesNotContain(wrongPlacement!, item => item.CampaignId == campaign.Id);
    }

    [Fact]
    public async Task Campaign_CategoryAndAreaTargets_AndScheduleAreAppliedWithoutAffectingOtherCampaign()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var targeted = await CreateCampaign(client, "지역 서비스 이용 안내", "ALL", "CUSTOMER_HOME", factory.Catalog.ServiceId, factory.Catalog.AreaId);
        await AddCreative(client, targeted.Id, "해당 지역 서비스 안내"); await ReviewCampaign(client, targeted.Id, "APPROVE", null);
        var other = await CreateCampaign(client, "별도 공급자 안내", "PROVIDER", "PROVIDER_HOME"); await AddCreative(client, other.Id, "공급자 운영 안내"); await ReviewCampaign(client, other.Id, "APPROVE", null);

        var matched = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>($"/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME&categoryId={factory.Catalog.ServiceId}&areaId={factory.Catalog.AreaId}");
        Assert.Contains(matched!, item => item.CampaignId == targeted.Id);
        var mismatched = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>($"/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME&categoryId={factory.Catalog.OtherServiceId}&areaId={factory.Catalog.OtherAreaId}");
        Assert.DoesNotContain(mismatched!, item => item.CampaignId == targeted.Id);
        var otherDetail = await client.GetFromJsonAsync<AdminAdvertisingCampaignDetail>($"{AdminRoot}/campaigns/{other.Id}");
        Assert.Equal("APPROVED", otherDetail!.ReviewStatusCode);
    }

    [Fact]
    public async Task Campaign_CanBeUpdatedRejectedWithReasonAndPreviewedWithoutPublishing()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var campaign = await CreateCampaign(client, "검수 전 운영 안내", "ALL", "CUSTOMER_HOME");
        var update = await client.PutAsJsonAsync($"{AdminRoot}/campaigns/{campaign.Id}", CampaignInput("수정된 운영 안내", "CUSTOMER", "CUSTOMER_HOME", "INTERNAL_PATH", "/customer/requests"));
        update.EnsureSuccessStatusCode(); var updated = (await update.Content.ReadFromJsonAsync<AdminAdvertisingCampaignDetail>())!;
        Assert.Equal("수정된 운영 안내", updated.CampaignName); Assert.Equal("DRAFT", updated.ReviewStatusCode);
        var noReason = await client.PostAsJsonAsync($"{AdminRoot}/campaigns/{campaign.Id}/review", new { ActionCode = "REJECT", Reason = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        var rejected = await ReviewCampaign(client, campaign.Id, "REJECT", "연결 문구를 다시 확인해 주세요.");
        Assert.Equal("REJECTED", rejected.ReviewStatusCode); Assert.Equal("연결 문구를 다시 확인해 주세요.", rejected.RejectionReason);
        var preview = await client.GetFromJsonAsync<AdminAdvertisingCampaignDetail>($"{AdminRoot}/campaigns/{campaign.Id}");
        Assert.Equal("수정된 운영 안내", preview!.CampaignName); Assert.Equal("/customer/requests", preview.DestinationValue);
    }

    [Fact]
    public async Task FutureAndExpiredCampaigns_AreNotPubliclyVisible()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var futureResponse = await client.PostAsJsonAsync($"{AdminRoot}/campaigns", CampaignInputAt("게시 예정 안내", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)));
        futureResponse.EnsureSuccessStatusCode(); var future = (await futureResponse.Content.ReadFromJsonAsync<AdminAdvertisingCampaignDetail>())!; await AddCreative(client, future.Id, "게시 예정 배너"); await ReviewCampaign(client, future.Id, "APPROVE", null);
        var expiredResponse = await client.PostAsJsonAsync($"{AdminRoot}/campaigns", CampaignInputAt("게시 종료 안내", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1)));
        expiredResponse.EnsureSuccessStatusCode(); var expired = (await expiredResponse.Content.ReadFromJsonAsync<AdminAdvertisingCampaignDetail>())!; await AddCreative(client, expired.Id, "게시 종료 배너"); await ReviewCampaign(client, expired.Id, "APPROVE", null);
        var visible = await client.GetFromJsonAsync<List<PublicAdvertisingCreative>>("/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_HOME");
        Assert.DoesNotContain(visible!, item => item.CampaignId == future.Id); Assert.DoesNotContain(visible!, item => item.CampaignId == expired.Id);
        Assert.Equal("SCHEDULED", (await client.GetFromJsonAsync<AdminAdvertisingCampaignDetail>($"{AdminRoot}/campaigns/{future.Id}"))!.PublicationStatus);
        Assert.Equal("EXPIRED", (await client.GetFromJsonAsync<AdminAdvertisingCampaignDetail>($"{AdminRoot}/campaigns/{expired.Id}"))!.PublicationStatus);
    }

    [Fact]
    public async Task ImpressionAndClick_AreRecordedOnlyForEligibleCreative()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var campaign = await CreateCampaign(client, "고객 홈 이용 안내", "CUSTOMER", "CUSTOMER_HOME");
        var creative = await AddCreative(client, campaign.Id, "수달 라이프 이용 안내"); await ReviewCampaign(client, campaign.Id, "APPROVE", null);
        var eventBody = new { AudienceTypeCode = "CUSTOMER", PlacementCode = "CUSTOMER_HOME", CategoryId = (Guid?)null, AreaId = (Guid?)null };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/v1/public/advertising/creatives/{creative.Id}/events/IMPRESSION", eventBody)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/v1/public/advertising/creatives/{creative.Id}/events/CLICK", eventBody)).StatusCode);
        var detail = await client.GetFromJsonAsync<AdminAdvertisingCampaignDetail>($"{AdminRoot}/campaigns/{campaign.Id}");
        Assert.Equal(1, detail!.Metrics.Impressions); Assert.Equal(1, detail.Metrics.Clicks);
    }

    [Fact]
    public async Task Notice_CanBeVersionedApprovedScheduledAndFilteredForPublicAudience()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var createdResponse = await client.PostAsJsonAsync($"{AdminRoot}/contents", ContentInput("NOTICE", "CUSTOMER", "서비스 이용 안내", "서비스 요청 전 내용을 확인해 주세요."));
        createdResponse.EnsureSuccessStatusCode(); var created = (await createdResponse.Content.ReadFromJsonAsync<AdminManagedContentDetail>())!;
        var updatedResponse = await client.PutAsJsonAsync($"{AdminRoot}/contents/{created.Id}", ContentInput("NOTICE", "CUSTOMER", "서비스 이용 안내", "서비스 요청 전 일정과 작업 범위를 확인해 주세요.", "안내 문구 명확화"));
        updatedResponse.EnsureSuccessStatusCode(); var updated = (await updatedResponse.Content.ReadFromJsonAsync<AdminManagedContentDetail>())!;
        Assert.Equal(2, updated.CurrentVersionNo); Assert.Equal(2, updated.Versions.Count);
        var reviewed = await client.PostAsJsonAsync($"{AdminRoot}/contents/{created.Id}/review", new { ActionCode = "APPROVE", Reason = (string?)null }); reviewed.EnsureSuccessStatusCode();
        var customer = await client.GetFromJsonAsync<List<PublicManagedContent>>("/api/v1/public/contents?type=NOTICE&audience=CUSTOMER");
        Assert.Contains(customer!, item => item.Id == created.Id && item.VersionNo == 2);
        var provider = await client.GetFromJsonAsync<List<PublicManagedContent>>("/api/v1/public/contents?type=NOTICE&audience=PROVIDER");
        Assert.DoesNotContain(provider!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task FaqAndSafetyGuide_AreManagedSeparatelyAndRespectScheduleAndExpiration()
    {
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var faqResponse = await client.PostAsJsonAsync($"{AdminRoot}/contents", new { ContentTypeCode="FAQ",AudienceTypeCode="ALL",Title="서비스 요청 FAQ",BodyText=(string?)null,QuestionText="요청을 수정할 수 있나요?",AnswerText="견적 채택 전 요청 상세에서 확인해 주세요.",FileId=(Guid?)null,LinkText=(string?)null,DestinationTypeCode="NONE",DestinationValue=(string?)null,StartAt=DateTime.UtcNow.AddMinutes(-5),EndAt=DateTime.UtcNow.AddDays(7),DisplayOrder=1,CategoryIds=Array.Empty<Guid>(),AreaIds=Array.Empty<Guid>(),ChangeReason=(string?)null });
        faqResponse.EnsureSuccessStatusCode(); var faq=(await faqResponse.Content.ReadFromJsonAsync<AdminManagedContentDetail>())!; (await client.PostAsJsonAsync($"{AdminRoot}/contents/{faq.Id}/review",new{ActionCode="APPROVE",Reason=(string?)null})).EnsureSuccessStatusCode();
        var safetyResponse = await client.PostAsJsonAsync($"{AdminRoot}/contents", ContentInputAt("SAFETY_GUIDE", "ALL", "방문 작업 안전 안내", "작업 전 이동 경로와 주변 물품을 확인해 주세요.", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1)));
        safetyResponse.EnsureSuccessStatusCode(); var safety=(await safetyResponse.Content.ReadFromJsonAsync<AdminManagedContentDetail>())!; (await client.PostAsJsonAsync($"{AdminRoot}/contents/{safety.Id}/review",new{ActionCode="APPROVE",Reason=(string?)null})).EnsureSuccessStatusCode();
        var faqPublic=await client.GetFromJsonAsync<List<PublicManagedContent>>("/api/v1/public/contents?type=FAQ&audience=CUSTOMER"); Assert.Contains(faqPublic!,item=>item.Id==faq.Id&&item.QuestionText=="요청을 수정할 수 있나요?");
        var safetyPublic=await client.GetFromJsonAsync<List<PublicManagedContent>>("/api/v1/public/contents?type=SAFETY_GUIDE&audience=CUSTOMER"); Assert.DoesNotContain(safetyPublic!,item=>item.Id==safety.Id);
        Assert.Equal("EXPIRED",(await client.GetFromJsonAsync<AdminManagedContentDetail>($"{AdminRoot}/contents/{safety.Id}"))!.PublicationStatus);
    }

    [Fact]
    public async Task AdvertisingManagement_WritesAuditButDoesNotWriteWalletOrTradeData()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var ledgerBefore = await db.WalletLedgerEntries.CountAsync(); var transactionsBefore = await db.Transactions.CountAsync();
        using var client = Client(); await Login(client, RoleCodes.Admin);
        var campaign = await CreateCampaign(client, "본사 운영 공지 배너", "ALL", "CUSTOMER_HOME"); await AddCreative(client, campaign.Id, "운영 공지 확인"); await ReviewCampaign(client, campaign.Id, "APPROVE", null);
        Assert.Equal(ledgerBefore, await db.WalletLedgerEntries.CountAsync()); Assert.Equal(transactionsBefore, await db.Transactions.CountAsync());
        Assert.True(await db.AuditLogs.AnyAsync(item => item.EntityPublicId == campaign.Id && item.ActionCode == "ADVERTISING_CAMPAIGN_APPROVED"));
    }

    private async Task<AdminAdvertisingCampaignDetail> CreateCampaign(HttpClient client, string name, string audience, string placement, Guid? category = null, Guid? area = null)
    { var response = await client.PostAsJsonAsync($"{AdminRoot}/campaigns", CampaignInput(name, audience, placement, "NONE", null, category, area)); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AdminAdvertisingCampaignDetail>())!; }
    private static object CampaignInput(string name, string audience, string placement, string destinationType, string? destination, Guid? category = null, Guid? area = null) => new { CampaignName=name, CampaignTypeCode="BANNER", AudienceTypeCode=audience, OwnerTypeCode="HEAD_OFFICE", OwnerDisplayName="수달 라이프 본사", StartAt=DateTime.UtcNow.AddMinutes(-5), EndAt=DateTime.UtcNow.AddDays(7), Priority=10, DestinationTypeCode=destinationType, DestinationValue=destination, PlacementCodes=new[]{placement}, CategoryIds=category.HasValue?new[]{category.Value}:Array.Empty<Guid>(), AreaIds=area.HasValue?new[]{area.Value}:Array.Empty<Guid>() };
    private static object CampaignInputAt(string name,DateTime start,DateTime end)=>new{CampaignName=name,CampaignTypeCode="BANNER",AudienceTypeCode="CUSTOMER",OwnerTypeCode="HEAD_OFFICE",OwnerDisplayName="수달 라이프 본사",StartAt=start,EndAt=end,Priority=10,DestinationTypeCode="NONE",DestinationValue=(string?)null,PlacementCodes=new[]{"CUSTOMER_HOME"},CategoryIds=Array.Empty<Guid>(),AreaIds=Array.Empty<Guid>()};
    private static object ContentInput(string type,string audience,string title,string body,string? reason=null)=>new{ContentTypeCode=type,AudienceTypeCode=audience,Title=title,BodyText=body,QuestionText=(string?)null,AnswerText=(string?)null,FileId=(Guid?)null,LinkText=(string?)null,DestinationTypeCode="NONE",DestinationValue=(string?)null,StartAt=DateTime.UtcNow.AddMinutes(-5),EndAt=DateTime.UtcNow.AddDays(7),DisplayOrder=0,CategoryIds=Array.Empty<Guid>(),AreaIds=Array.Empty<Guid>(),ChangeReason=reason};
    private static object ContentInputAt(string type,string audience,string title,string body,DateTime start,DateTime end)=>new{ContentTypeCode=type,AudienceTypeCode=audience,Title=title,BodyText=body,QuestionText=(string?)null,AnswerText=(string?)null,FileId=(Guid?)null,LinkText=(string?)null,DestinationTypeCode="NONE",DestinationValue=(string?)null,StartAt=start,EndAt=end,DisplayOrder=0,CategoryIds=Array.Empty<Guid>(),AreaIds=Array.Empty<Guid>(),ChangeReason=(string?)null};
    private async Task<AdvertisingCreativeResponse> AddCreative(HttpClient client,Guid campaignId,string title){var response=await client.PostAsJsonAsync($"{AdminRoot}/campaigns/{campaignId}/creatives",new{Title=title,Subtitle=(string?)null,BodyText=(string?)null,FileId=(Guid?)null,AltText=(string?)null,ButtonText=(string?)null,DestinationTypeCode=(string?)null,DestinationValue=(string?)null,DisplayOrder=0,StatusCode="ACTIVE"});response.EnsureSuccessStatusCode();return(await response.Content.ReadFromJsonAsync<AdvertisingCreativeResponse>())!;}
    private async Task<AdminAdvertisingCampaignDetail> ReviewCampaign(HttpClient client,Guid id,string action,string? reason){var response=await client.PostAsJsonAsync($"{AdminRoot}/campaigns/{id}/review",new{ActionCode=action,Reason=reason});response.EnsureSuccessStatusCode();return(await response.Content.ReadFromJsonAsync<AdminAdvertisingCampaignDetail>())!;}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});
}
