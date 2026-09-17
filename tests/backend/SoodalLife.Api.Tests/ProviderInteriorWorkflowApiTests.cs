using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Interior;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderInteriorWorkflowApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task AssignedProviderSeesOnlyOwnRoleProject_AndDtoDoesNotExposeInternalData()
    {
        var flow=await CreateProject("DESIGN");using var other=Client();await Login(other,factory.AreaMismatchProviderCredential);
        var list=await flow.Provider.GetFromJsonAsync<List<ProviderInteriorProjectItem>>("/api/v1/providers/me/interior/projects?role=DESIGN");Assert.Contains(list!,x=>x.Id==flow.ProjectId&&x.Roles.Contains("DESIGN"));
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/api/v1/providers/me/interior/projects/{flow.ProjectId}")).StatusCode);
        var json=await (await flow.Provider.GetAsync($"/api/v1/providers/me/interior/projects/{flow.ProjectId}")).Content.ReadAsStringAsync();Assert.DoesNotContain("storageKey",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("wallet",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("feeCharge",json,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SiteSurveyPrivacy_IsAvailableOnlyUntilCompletion()
    {
        var flow=await CreateProject("SITE_SURVEY",withVisit:true);var detail=await flow.Provider.GetFromJsonAsync<ProviderInteriorDetail>($"/api/v1/providers/me/interior/projects/{flow.ProjectId}");Assert.True(detail!.ContactAvailable);Assert.NotNull(detail.CustomerPhone);Assert.NotNull(detail.DetailAddress);var visit=Assert.Single(detail.SiteVisits);
        Assert.Equal(HttpStatusCode.OK,(await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/site-visits/{visit.Id}/start",new{idempotencyKey=$"start-{Guid.NewGuid():N}",rowVersion=visit.RowVersion})).StatusCode);
        var current=await flow.Provider.GetFromJsonAsync<ProviderInteriorDetail>($"/api/v1/providers/me/interior/projects/{flow.ProjectId}");visit=Assert.Single(current!.SiteVisits);
        var complete=await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/site-visits/{visit.Id}/completion",new{measurementSummary="현장 실측 완료",constraint="엘리베이터 시간 확인",riskNote="없음",measurements=new[]{new{key="width",value=4.2m,text=(string?)null,unit="m",location="거실",note=(string?)null,additionalDataJson=(string?)null}},fileIds=Array.Empty<Guid>(),idempotencyKey=$"complete-{Guid.NewGuid():N}",rowVersion=visit.RowVersion});Assert.Equal(HttpStatusCode.OK,complete.StatusCode);
        var closed=await complete.Content.ReadFromJsonAsync<ProviderInteriorDetail>();Assert.False(closed!.ContactAvailable);Assert.Null(closed.CustomerPhone);Assert.Null(closed.DetailAddress);
    }

    [Fact]
    public async Task DesignRoleCreatesImmutableVersions_ButCannotSeeContact()
    {
        var flow=await CreateProject("DESIGN");var first=await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/projects/{flow.ProjectId}/designs",new{title="1차 설계",description="평면 계획",fileIds=Array.Empty<Guid>(),idempotencyKey=$"design-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,first.StatusCode);
        var second=await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/projects/{flow.ProjectId}/designs",new{title="2차 설계",description="수정 계획",fileIds=Array.Empty<Guid>(),idempotencyKey=$"design-{Guid.NewGuid():N}"});var detail=await second.Content.ReadFromJsonAsync<ProviderInteriorDetail>();Assert.Equal(2,detail!.Designs.Count);Assert.Equal(new[]{2,1},detail.Designs.Select(x=>x.VersionNo));Assert.False(detail.ContactAvailable);
    }

    [Fact]
    public async Task TradeProviderCanUpdateOnlyExplicitlyAssignedStage()
    {
        var flow=await CreateProject("TRADE_CONTRACTOR",withStages:true);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var project=await db.InteriorProjects.SingleAsync(x=>x.PublicId==flow.ProjectId);var participant=await db.InteriorProjectParticipants.SingleAsync(x=>x.InteriorProjectId==project.Id&&x.RoleCode=="TRADE_CONTRACTOR");var stages=await db.InteriorWorkStages.Where(x=>x.InteriorProjectId==project.Id).OrderBy(x=>x.SequenceNo).ToListAsync();var now=DateTime.UtcNow;db.InteriorWorkStageAssignments.Add(new(){WorkStageId=stages[0].Id,ProjectParticipantId=participant.Id,EffectiveFrom=now.AddMinutes(-1),StatusCode="ACTIVE",CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync();
        var visible=await flow.Provider.GetFromJsonAsync<ProviderInteriorDetail>($"/api/v1/providers/me/interior/projects/{flow.ProjectId}");Assert.Single(visible!.Stages);Assert.Equal(stages[0].PublicId,visible.Stages[0].Id);
        var ok=await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/stages/{stages[0].PublicId}/updates",new{progressPercent=20,updateText="배정 공정 진행",fileIds=Array.Empty<Guid>(),idempotencyKey=$"stage-{Guid.NewGuid():N}",rowVersion=(string?)null});Assert.Equal(HttpStatusCode.OK,ok.StatusCode);
        var blocked=await flow.Provider.PostAsJsonAsync($"/api/v1/providers/me/interior/stages/{stages[1].PublicId}/updates",new{progressPercent=20,updateText="미배정 공정",fileIds=Array.Empty<Guid>(),idempotencyKey=$"stage-{Guid.NewGuid():N}",rowVersion=(string?)null});Assert.Equal(HttpStatusCode.Forbidden,blocked.StatusCode);
    }

    [Fact]
    public async Task UploadIsFailClosed_AndProtectedLedgersRemainUnchanged()
    {
        var flow=await CreateProject("DESIGN");using var before=factory.Services.CreateScope();var db=before.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var wallets=await db.ProviderWallets.AsNoTracking().Select(x=>new{x.Id,x.AvailableBalance,x.ReservedBalance}).ToListAsync();var ledger=await db.WalletLedgerEntries.CountAsync();var fees=await db.FeeCharges.CountAsync();var trust=await db.TrustScoreEvents.CountAsync();
        using var form=new MultipartFormDataContent();using var bytes=new ByteArrayContent(TestFileSamples.ValidJpeg());bytes.Headers.ContentType=new("image/jpeg");form.Add(bytes,"file","survey.jpg");var upload=await flow.Provider.PostAsync($"/api/v1/providers/me/interior/projects/{flow.ProjectId}/files",form);Assert.Equal(HttpStatusCode.OK,upload.StatusCode);var value=await upload.Content.ReadFromJsonAsync<ProviderInteriorUpload>();Assert.Equal("NOT_INTEGRATED",value!.MalwareScanStatus);Assert.Equal("NOT_INTEGRATED",value.PrivacyInspectionStatus);Assert.Equal("NOT_INTEGRATED",value.SanitizationStatus);
        db.ChangeTracker.Clear();Assert.Equal(wallets,await db.ProviderWallets.AsNoTracking().Select(x=>new{x.Id,x.AvailableBalance,x.ReservedBalance}).ToListAsync());Assert.Equal(ledger,await db.WalletLedgerEntries.CountAsync());Assert.Equal(fees,await db.FeeCharges.CountAsync());Assert.Equal(trust,await db.TrustScoreEvents.CountAsync());
    }

    private async Task<Flow> CreateProject(string role,bool withVisit=false,bool withStages=false)
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var service=await db.ServiceCategories.SingleAsync(x=>x.PublicId==factory.Catalog.ServiceId);service.ExternalCode="INT-PROVIDER-TEST";var customer=await(from u in db.Users join c in db.CustomerProfiles on u.Id equals c.UserId where u.LoginId==factory.Credentials[RoleCodes.Customer].LoginId select new{u,c}).SingleAsync();customer.u.Phone="010-1234-5678";var provider=await(from u in db.Users join p in db.ProviderProfiles on u.Id equals p.UserId where u.LoginId==factory.Credentials[RoleCodes.Provider].LoginId select new{u,p}).SingleAsync();var area=await db.AdministrativeAreas.SingleAsync(x=>x.PublicId==factory.Catalog.AreaId);var policy=await db.CategoryPolicies.FirstAsync(x=>x.CategoryId==service.Id);var now=DateTime.UtcNow;var request=new ServiceRequest{CustomerProfileId=customer.c.Id,CategoryId=service.Id,CategoryPolicyId=policy.Id,AdministrativeAreaId=area.Id,DetailAddress="101동 1203호",Title="인테리어 전문가 테스트",Description="전문가 역할별 업무",StatusCode="OPEN",PolicySnapshotJson="{}",OpenedAt=now,CreatedAt=now,UpdatedAt=now};db.ServiceRequests.Add(request);await db.SaveChangesAsync();var project=new InteriorProject{ServiceRequestId=request.Id,CustomerProfileId=customer.c.Id,ServiceCategoryId=service.Id,StatusCode=role=="SITE_SURVEY"?"SITE_VISIT_SCHEDULED":"CONSTRUCTION",SelectedSiteVisitProviderId=role=="SITE_SURVEY"?provider.p.Id:null,SelectedContractorProviderId=role=="PRIMARY_CONTRACTOR"?provider.p.Id:null,CreatedAt=now,UpdatedAt=now};db.InteriorProjects.Add(project);await db.SaveChangesAsync();db.InteriorProjectParticipants.Add(new(){InteriorProjectId=project.Id,ProviderProfileId=provider.p.Id,RoleCode=role,EffectiveFrom=now.AddMinutes(-1),StatusCode="ACTIVE",ScopeText="테스트 역할",CreatedAt=now,UpdatedAt=now});if(withVisit)db.InteriorSiteVisits.Add(new(){InteriorProjectId=project.Id,ProviderProfileId=provider.p.Id,StatusCode="CONFIRMED",ScheduledStartAt=now.AddHours(1),ConfirmedAt=now,CreatedAt=now,UpdatedAt=now});if(withStages){db.InteriorWorkStages.Add(new(){InteriorProjectId=project.Id,SequenceNo=1,StageName="철거",PlannedStartDate=DateOnly.FromDateTime(DateTime.Today),PlannedEndDate=DateOnly.FromDateTime(DateTime.Today.AddDays(1)),CreatedAt=now,UpdatedAt=now});db.InteriorWorkStages.Add(new(){InteriorProjectId=project.Id,SequenceNo=2,StageName="도장",PlannedStartDate=DateOnly.FromDateTime(DateTime.Today.AddDays(2)),PlannedEndDate=DateOnly.FromDateTime(DateTime.Today.AddDays(3)),CreatedAt=now,UpdatedAt=now});}await db.SaveChangesAsync();var client=Client();await Login(client,factory.Credentials[RoleCodes.Provider]);return new(client,project.PublicId);
    }
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private static async Task Login(HttpClient client,TestCredential credential)=>Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=credential.LoginId,credential.Password})).StatusCode);
    private sealed record Flow(HttpClient Provider,Guid ProjectId);
}
