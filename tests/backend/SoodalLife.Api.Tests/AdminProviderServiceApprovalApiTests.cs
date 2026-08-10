using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
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
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotUseServiceApprovalApi(string role)
    { var target = await EnsureApprovalAsync(); using var client = Client(); await Login(client, factory.Credentials[role]); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Base(target.ProviderId))).StatusCode); }

    private async Task<Target> EnsureApprovalAsync()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.FirstAsync(value => value.BusinessName == "Service Mismatch"); var link = await db.ProviderServiceCategories.SingleAsync(value => value.ProviderProfileId == provider.Id); var serviceId = await db.ServiceCategories.Where(value => value.Id == link.CategoryId).Select(value => value.PublicId).SingleAsync();
        var approval = await db.ProviderServiceApprovals.SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == link.Id);
        if (approval is null) db.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = link.Id, ApprovalStatusCode = "PENDING", ApprovalRequestedAt = link.ActivatedAt, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        else { approval.ApprovalStatusCode = "PENDING"; approval.ApprovalDecidedAt = null; approval.ApprovalDecidedByUserId = null; approval.DecisionReason = null; db.ProviderServiceApprovalEvents.RemoveRange(db.ProviderServiceApprovalEvents.Where(value => value.ProviderServiceCategoryId == link.Id)); }
        await db.SaveChangesAsync();
        var otherLink = await db.ProviderServiceCategories.FirstAsync(value => value.Id != link.Id); if (!await db.ProviderServiceApprovals.AnyAsync(value => value.ProviderServiceCategoryId == otherLink.Id)) { db.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = otherLink.Id, ApprovalStatusCode = "PENDING", ApprovalRequestedAt = otherLink.ActivatedAt, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(); }
        return new(provider.PublicId, serviceId, link.Id);
    }
    private static string Base(Guid providerId) => $"/api/v1/admin/providers/{providerId}/service-approvals";
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private sealed record Target(Guid ProviderId, Guid ServiceId, long LinkId);
}
