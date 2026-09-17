using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminProviderServiceApprovalApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanReviewApproveAndRejectService_WithHistoryAuditIsolationAndConcurrency()
    {
        var target = await EnsureApprovalAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        var list = await client.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId));
        var initial = Assert.Single(list!); Assert.Equal("PENDING", initial.ApprovalStatusCode);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Equal("ACTIVE", (await db.ProviderServiceCategories.SingleAsync(value => value.Id == target.LinkId)).StatusCode); }

        var approvedResponse = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions", new { ActionCode = "APPROVE", DecisionReason = "제출자료 확인 완료", initial.RowVersion });
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode); var approved = await approvedResponse.Content.ReadFromJsonAsync<AdminProviderServiceApprovalDecisionResponse>(); Assert.Equal("APPROVED", approved!.ApprovalStatusCode); Assert.Single(approved.History);
        var missingReason = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions", new { ActionCode = "REJECT", DecisionReason = "", approved.RowVersion }); Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);
        var stale = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions", new { ActionCode = "REJECT", DecisionReason = "증빙 보완 필요", RowVersion = "AQ==" }); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var rejectedResponse = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions", new { ActionCode = "REJECT", DecisionReason = "증빙 보완 필요", approved.RowVersion }); Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
        var rejected = await rejectedResponse.Content.ReadFromJsonAsync<AdminProviderServiceApprovalDecisionResponse>(); Assert.Equal("REJECTED", rejected!.ApprovalStatusCode); Assert.Equal("증빙 보완 필요", rejected.DecisionReason); Assert.Equal(2, rejected.History.Count);
        using var verifyScope = factory.Services.CreateScope(); var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Equal("ACTIVE", (await verifyDb.ProviderServiceCategories.SingleAsync(value => value.Id == target.LinkId)).StatusCode);
        Assert.True(await verifyDb.AuditLogs.AnyAsync(value => value.ActionCode == "PROVIDER_SERVICE_APPROVED")); Assert.True(await verifyDb.AuditLogs.AnyAsync(value => value.ActionCode == "PROVIDER_SERVICE_REJECTED"));
        Assert.True(await verifyDb.ProviderServiceApprovals.AnyAsync(value => value.ProviderServiceCategoryId != target.LinkId && value.ApprovalStatusCode == "PENDING"));
    }

    [Fact]
    public async Task ProviderDetail_ShowsApprovalRequirementsAndAreas_WithoutAutomaticApproval()
    {
        var target = await EnsureApprovalAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        var detail = await client.GetFromJsonAsync<AdminProviderDetailResponse>($"/api/v1/admin/providers/{target.ProviderId}"); var review = Assert.Single(detail!.ServiceReviews);
        Assert.Equal("PENDING", review.ApprovalStatusCode); Assert.False(review.StructuredRequirementsConfigured); Assert.Equal("필수 자격 확인", review.LegacyQualificationText); Assert.NotEmpty(detail.Areas);
        Assert.All(detail.Areas, area => { Assert.False(string.IsNullOrWhiteSpace(area.ProvinceName)); Assert.False(string.IsNullOrWhiteSpace(area.DistrictName)); });
    }

    [Fact]
    public async Task Admin_CanApproveAndRejectAllProviderServices_InSingleRequest()
    {
        var target = await EnsureApprovalAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        var initial = (await client.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId)))!;
        var approvedResponse = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/bulk-decisions", new { ActionCode = "APPROVE", DecisionReason = "전체 서비스 확인 완료", Services = initial.Select(value => new { value.ServiceId, value.RowVersion }) });
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        var approved = (await approvedResponse.Content.ReadFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>())!;
        Assert.All(approved, value => Assert.Equal("APPROVED", value.ApprovalStatusCode));
        var missingReason = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/bulk-decisions", new { ActionCode = "REJECT", DecisionReason = "", Services = approved.Select(value => new { value.ServiceId, value.RowVersion }) });
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);
        var rejectedResponse = await client.PostAsJsonAsync($"{Base(target.ProviderId)}/bulk-decisions", new { ActionCode = "REJECT", DecisionReason = "공통 증빙 보완", Services = approved.Select(value => new { value.ServiceId, value.RowVersion }) });
        Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
        var rejected = (await rejectedResponse.Content.ReadFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>())!;
        Assert.All(rejected, value => { Assert.Equal("REJECTED", value.ApprovalStatusCode); Assert.Equal("공통 증빙 보완", value.DecisionReason); });
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.True(await db.AuditLogs.AnyAsync(value => value.ActionCode == "PROVIDER_SERVICE_BULK_APPROVED"));
        Assert.True(await db.AuditLogs.AnyAsync(value => value.ActionCode == "PROVIDER_SERVICE_BULK_REJECTED"));
    }

    [Fact]
    public async Task Admin_ApprovesServicesThenProvider_AndActivatesTradingStatus()
    {
        var target=await EnsureApprovalAsync();using var client=Client();await Login(client,factory.Credentials[RoleCodes.Admin]);
        var service=Assert.Single((await client.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId)))!);
        var serviceResponse=await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions",new{ActionCode="APPROVE",DecisionReason="서비스 검증 완료",service.RowVersion});Assert.Equal(HttpStatusCode.OK,serviceResponse.StatusCode);
        var detail=await client.GetFromJsonAsync<AdminProviderDetailResponse>($"/api/v1/admin/providers/{target.ProviderId}");
        var response=await client.PostAsJsonAsync($"/api/v1/admin/providers/{target.ProviderId}/approval-decisions",new{ActionCode="APPROVE",Reason="전체 심사 완료",detail!.Basic.RowVersion});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var decision=await response.Content.ReadFromJsonAsync<AdminProviderApprovalDecisionResponse>();Assert.Equal("APPROVED",decision!.ApprovalStatusCode);Assert.Equal("ACTIVE",decision.ActivityStatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var provider=await db.ProviderProfiles.SingleAsync(x=>x.PublicId==target.ProviderId);Assert.Equal("APPROVED",provider.ApprovalStatusCode);Assert.Equal("ACTIVE",provider.ActivityStatusCode);Assert.True(await db.ProviderApprovalEvents.AnyAsync(x=>x.ProviderProfileId==provider.Id&&x.ToStatusCode=="APPROVED"));Assert.True(await db.AuditLogs.AnyAsync(x=>x.EntityPublicId==target.ProviderId&&x.ActionCode=="PROVIDER_APPROVED"));
    }

    [Fact]
    public async Task PrelaunchDuplicateCheckedProvider_CanBeApprovedWithoutIdentityVerification()
    {
        using var application=factory.WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,configuration)=>configuration.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Prelaunch:AllowPhoneDuplicateCheckOnly"]="true",
        })));
        var target=await EnsureApprovalAsync(application.Services,"NOT_INTEGRATED");using var client=application.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});await Login(client,factory.Credentials[RoleCodes.Admin]);
        var service=Assert.Single((await client.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId)))!);
        var serviceResponse=await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions",new{ActionCode="APPROVE",DecisionReason="사전운영 서비스 검증 완료",service.RowVersion});Assert.Equal(HttpStatusCode.OK,serviceResponse.StatusCode);
        var detail=await client.GetFromJsonAsync<AdminProviderDetailResponse>($"/api/v1/admin/providers/{target.ProviderId}");
        var response=await client.PostAsJsonAsync($"/api/v1/admin/providers/{target.ProviderId}/approval-decisions",new{ActionCode="APPROVE",Reason="사전운영 전체 심사 완료",detail!.Basic.RowVersion});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var decision=await response.Content.ReadFromJsonAsync<AdminProviderApprovalDecisionResponse>();Assert.Equal("APPROVED",decision!.ApprovalStatusCode);Assert.Equal("ACTIVE",decision.ActivityStatusCode);
    }

    [Fact]
    public async Task IdentityVerificationRemainsRequiredWhenPrelaunchDuplicateCheckIsDisabled()
    {
        var target=await EnsureApprovalAsync(phoneVerificationStatus:"NOT_INTEGRATED");using var client=Client();await Login(client,factory.Credentials[RoleCodes.Admin]);
        var service=Assert.Single((await client.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId)))!);
        (await client.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions",new{ActionCode="APPROVE",DecisionReason="서비스 검증 완료",service.RowVersion})).EnsureSuccessStatusCode();
        var detail=await client.GetFromJsonAsync<AdminProviderDetailResponse>($"/api/v1/admin/providers/{target.ProviderId}");
        var response=await client.PostAsJsonAsync($"/api/v1/admin/providers/{target.ProviderId}/approval-decisions",new{ActionCode="APPROVE",Reason="전체 심사 완료",detail!.Basic.RowVersion});
        Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
    }

    [Fact]
    public async Task RejectedProvider_CanResubmitSupplement_AndAdminCanFilterIt()
    {
        var target = await EnsureApprovalAsync();
        using var adminClient = Client(); await Login(adminClient, factory.Credentials[RoleCodes.Admin]);
        var service = Assert.Single((await adminClient.GetFromJsonAsync<IReadOnlyList<AdminProviderServiceApprovalDecisionResponse>>(Base(target.ProviderId)))!);
        var serviceRejected = await adminClient.PostAsJsonAsync($"{Base(target.ProviderId)}/{target.ServiceId}/decisions", new { ActionCode = "REJECT", DecisionReason = "면허 증빙을 교체해 주세요.", service.RowVersion });
        Assert.Equal(HttpStatusCode.OK, serviceRejected.StatusCode);
        var detail = await adminClient.GetFromJsonAsync<AdminProviderDetailResponse>($"/api/v1/admin/providers/{target.ProviderId}");
        var overallRejected = await adminClient.PostAsJsonAsync($"/api/v1/admin/providers/{target.ProviderId}/approval-decisions", new { ActionCode = "REJECT", Reason = "사업자 증빙을 보완해 주세요.", detail!.Basic.RowVersion });
        Assert.Equal(HttpStatusCode.OK, overallRejected.StatusCode);
        using var providerClient = Client(); await Login(providerClient, factory.ServiceMismatchProviderCredential);
        var profile = await providerClient.GetFromJsonAsync<ProviderProfileResponse>("/api/v1/providers/me");
        Assert.Equal("사업자 증빙을 보완해 주세요.", profile!.RejectionReason);
        var providerServices = await providerClient.GetFromJsonAsync<IReadOnlyList<ProviderServiceCategoryResponse>>("/api/v1/providers/me/service-categories");
        var rejectedService = Assert.Single(providerServices!); Assert.Equal("면허 증빙을 교체해 주세요.", rejectedService.DecisionReason);
        Assert.Equal(HttpStatusCode.NoContent, (await providerClient.PostAsync($"/api/v1/providers/me/service-categories/{target.ServiceId}/resubmit", null)).StatusCode);
        var submitted = await providerClient.PostAsJsonAsync("/api/v1/providers/me/approval/resubmit", new { Note = "사업자 증빙 교체 완료", profile.ConcurrencyToken });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var filtered = await adminClient.GetFromJsonAsync<AdminProviderListResponse>("/api/v1/admin/providers?approvalStatus=SUPPLEMENTED");
        Assert.Contains(filtered!.Items, x => x.Id == target.ProviderId && x.ReviewStageCode == "SUPPLEMENTED");
        using var verifyScope = factory.Services.CreateScope(); var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var saved = await verifyDb.ProviderProfiles.SingleAsync(x => x.PublicId == target.ProviderId);
        Assert.Equal("PENDING", saved.ApprovalStatusCode);
        Assert.True(await verifyDb.ProviderApprovalEvents.AnyAsync(x => x.ProviderProfileId == saved.Id && x.ActionCode == "RESUBMIT"));
        Assert.True(await verifyDb.ProviderServiceApprovalEvents.AnyAsync(x => x.ProviderServiceCategoryId == target.LinkId && x.ActionCode == "RESUBMIT"));
        Assert.True(await verifyDb.AuditLogs.AnyAsync(x => x.EntityPublicId == target.ProviderId && x.ActionCode == "PROVIDER_APPROVAL_RESUBMITTED"));
    }

    [Fact]
    public void ProviderApprovalEvent_ConstraintAllowsResubmitAction()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProviderApprovalEvent));
        var constraint = Assert.Single(entity!.GetCheckConstraints(), value => value.Name == "CK_provider_approval_events_action");
        Assert.Contains("'RESUBMIT'", constraint.Sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotUseServiceApprovalApi(string role)
    { var target = await EnsureApprovalAsync(); using var client = Client(); await Login(client, factory.Credentials[role]); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Base(target.ProviderId))).StatusCode); }

    private async Task<Target> EnsureApprovalAsync(IServiceProvider? services = null, string phoneVerificationStatus = "VERIFIED")
    {
        using var scope = (services ?? factory.Services).CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.FirstAsync(value => value.BusinessName == "Service Mismatch"); provider.ApprovalStatusCode="PENDING";provider.ActivityStatusCode="INACTIVE";provider.ApprovalDecidedAt=null;provider.ApprovalDecidedByUserId=null;provider.Introduction="전문가 승인 심사용 서비스 소개입니다.";var providerUser=await db.Users.SingleAsync(value=>value.Id==provider.UserId);providerUser.Phone="01012345678";providerUser.PhoneVerificationStatusCode=phoneVerificationStatus;db.ProviderApprovalEvents.RemoveRange(db.ProviderApprovalEvents.Where(x=>x.ProviderProfileId==provider.Id)); var link = await db.ProviderServiceCategories.SingleAsync(value => value.ProviderProfileId == provider.Id); var serviceId = await db.ServiceCategories.Where(value => value.Id == link.CategoryId).Select(value => value.PublicId).SingleAsync();
        var approval = await db.ProviderServiceApprovals.SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == link.Id);
        if (approval is null) db.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = link.Id, ApprovalStatusCode = "PENDING", ApprovalRequestedAt = link.ActivatedAt, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        else { approval.ApprovalStatusCode = "PENDING"; approval.ApprovalDecidedAt = null; approval.ApprovalDecidedByUserId = null; approval.DecisionReason = null; db.ProviderServiceApprovalEvents.RemoveRange(db.ProviderServiceApprovalEvents.Where(value => value.ProviderServiceCategoryId == link.Id)); }
        await db.SaveChangesAsync();
        var otherLink = await db.ProviderServiceCategories.FirstAsync(value => value.Id != link.Id); var otherApproval=await db.ProviderServiceApprovals.SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == otherLink.Id); if (otherApproval is null) db.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = otherLink.Id, ApprovalStatusCode = "PENDING", ApprovalRequestedAt = otherLink.ActivatedAt, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); else otherApproval.ApprovalStatusCode="PENDING"; await db.SaveChangesAsync();
        return new(provider.PublicId, serviceId, link.Id);
    }
    private static string Base(Guid providerId) => $"/api/v1/admin/providers/{providerId}/service-approvals";
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private sealed record Target(Guid ProviderId, Guid ServiceId, long LinkId);
}
