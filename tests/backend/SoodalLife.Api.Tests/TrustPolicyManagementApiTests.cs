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

public sealed class TrustPolicyManagementApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact] public async Task DefaultReferenceData_IsRegisteredAndAvailableToAdmin()
    {using var client=Client();await Login(client);var response=await client.PostAsJsonAsync("/api/v1/admin/trust/reference-data/initialize",new{});response.EnsureSuccessStatusCode();var data=(await response.Content.ReadFromJsonAsync<TrustReferenceDataResponse>())!;Assert.Contains(data.RatingItems,x=>x.Code=="SERVICE_QUALITY");Assert.Contains(data.LiabilityTypes,x=>x.Code=="PROVIDER_FULL");Assert.Contains(data.SanctionTypes,x=>x.Code=="SUSPENSION");}

    [Fact] public async Task Admin_CanSaveApproveAndClonePolicyWithoutNetworkFailure()
    {using var client=Client();await Login(client);var policies=(await client.GetFromJsonAsync<List<TrustPolicyListItem>>("/api/v1/admin/trust/policies"))!;var draft=policies.Single(x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);var body=new{draft.PolicyVersion,draft.PolicyName,draft.EffectiveFrom,draft.EffectiveTo,draft.MinimumCompletedTransactions,draft.MinimumVerifiedReviews,components=draft.Components.Select(x=>new{x.Code,x.Weight,x.RuleType,settings=System.Text.Json.JsonSerializer.Deserialize<object>(x.SettingsJson)}),draft.RowVersion};var saved=await client.PostAsJsonAsync($"/api/v1/admin/trust/policies/{draft.Id}/save",body);saved.EnsureSuccessStatusCode();draft=(await saved.Content.ReadFromJsonAsync<TrustPolicyListItem>())!;var approved=await client.PostAsJsonAsync($"/api/v1/admin/trust/policies/{draft.Id}/approve",new{reason="자동 테스트 검토 승인",idempotencyKey=$"approve-{Guid.NewGuid():N}",draft.RowVersion});approved.EnsureSuccessStatusCode();var version=$"clone-test-{Guid.NewGuid():N}";var clone=await client.PostAsJsonAsync($"/api/v1/admin/trust/policies/{draft.Id}/clone",new{newVersion=version,newName="복제 정책 테스트",effectiveFrom=DateTime.UtcNow,effectiveTo=(DateTime?)null});clone.EnsureSuccessStatusCode();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var source=await db.TrustPolicies.SingleAsync(x=>x.PublicId==draft.Id);source.StatusCode="DRAFT";var copied=await db.TrustPolicies.SingleAsync(x=>x.PolicyVersion==version);db.TrustPolicies.Remove(copied);await db.SaveChangesAsync();}

    [Theory,InlineData(RoleCodes.Customer),InlineData(RoleCodes.Provider)] public async Task NonAdmin_CannotManageReferenceData(string role)
    {using var client=Client();await client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/trust/reference-data")).StatusCode);}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});private Task<HttpResponseMessage> Login(HttpClient client)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[RoleCodes.Admin].LoginId,factory.Credentials[RoleCodes.Admin].Password});
}
