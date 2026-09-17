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

public sealed class AdminOperationsV116ApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task OutboxRetry_RequiresReauthentication_AndDuplicateRequestIsIdempotent()
    {
        using var client=Client();var credential=factory.Credentials[RoleCodes.Admin];await Login(client,credential);var outbox=await SeedSecurityAndFailedOutbox();
        var without=await client.PostAsJsonAsync($"/api/v1/admin/system/outbox/{outbox}/retry",new{reason="관리자 확인 후 안전 재처리"});Assert.Equal((HttpStatusCode)428,without.StatusCode);
        var reauth=await (await client.PostAsJsonAsync("/api/v1/admin/security/reauthenticate",new{password=credential.Password,mfaCode=(string?)null})).Content.ReadFromJsonAsync<AdminReauthenticateResponse>();Assert.NotNull(reauth);
        client.DefaultRequestHeaders.Add("X-Admin-Reauth-Token",reauth.Token);var first=await client.PostAsJsonAsync($"/api/v1/admin/system/outbox/{outbox}/retry",new{reason="관리자 확인 후 안전 재처리"});var second=await client.PostAsJsonAsync($"/api/v1/admin/system/outbox/{outbox}/retry",new{reason="관리자 확인 후 안전 재처리"});Assert.Equal(HttpStatusCode.NoContent,first.StatusCode);Assert.Equal(HttpStatusCode.NoContent,second.StatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Single(await db.OutboxRetryRequests.Where(x=>x.OutboxEventId==db.OutboxEvents.Where(o=>o.PublicId==outbox).Select(o=>o.Id).Single()).ToListAsync());Assert.Equal("PENDING",(await db.OutboxEvents.SingleAsync(x=>x.PublicId==outbox)).StatusCode);
    }

    [Fact]
    public async Task AnalyticsEvents_CompleteConversionQuoteAndApiMetrics()
    {
        using var client=Client();await Login(client,factory.Credentials[RoleCodes.Admin]);using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var now=DateTime.UtcNow;var v1=Guid.NewGuid();var v2=Guid.NewGuid();db.AnalyticsEvents.AddRange(Event("SERVICE_DISCOVERY_VIEWED",v1,now),Event("SERVICE_DISCOVERY_VIEWED",v2,now),Event("SERVICE_REQUEST_CREATED",v1,now),Event("QUOTE_VIEWED",v1,now),new AnalyticsEvent{EventTypeCode="API_REQUEST",VisitorId=v1,HttpMethod="GET",StatusCode=500,DurationMs=800,OccurredAt=now});await db.SaveChangesAsync();}
        var response=await client.GetFromJsonAsync<AdminAnalyticsDashboardResponse>("/api/v1/admin/dashboard/management?range=LAST_30_DAYS");Assert.NotNull(response);Assert.Equal(50m,Metric(response,"request_conversion_rate").Value);Assert.True(Metric(response,"quote_views").Value>=1);Assert.NotNull(Metric(response,"api_error_rate").Value);Assert.NotEmpty(response.Providers);
    }

    [Fact]
    public async Task RetentionPolicy_CannotEnableDestructiveModeWithoutSeparateApproval()
    {
        using var client=Client();var credential=factory.Credentials[RoleCodes.Admin];await Login(client,credential);var policy=await SeedSecurityAndPolicy();var reauth=await (await client.PostAsJsonAsync("/api/v1/admin/security/reauthenticate",new{password=credential.Password})).Content.ReadFromJsonAsync<AdminReauthenticateResponse>();client.DefaultRequestHeaders.Add("X-Admin-Reauth-Token",reauth!.Token);
        var response=await client.PutAsJsonAsync($"/api/v1/admin/system/retention/{policy}",new{actionCode="ANONYMIZE",retentionDays=1825,legalHoldDays=365,isEnabled=true,dryRun=false});Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
    }

    private async Task<Guid> SeedSecurityAndFailedOutbox(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var admin=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId);if(!await db.AdminSecurityProfiles.AnyAsync(x=>x.UserId==admin.Id))db.AdminSecurityProfiles.Add(new(){UserId=admin.Id,DetailRoleCode=AdminDetailRoles.SuperAdmin,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});var row=new OutboxEvent{AggregateType="V116_TEST",AggregatePublicId=Guid.NewGuid(),EventType="V116_SAFE_RETRY",PayloadJson="{}",StatusCode="FAILED",OccurredAt=DateTime.UtcNow,AvailableAt=DateTime.UtcNow,AttemptCount=2,LastAttemptAt=DateTime.UtcNow,ErrorMessage="test",IdempotencyKey=$"v116:{Guid.NewGuid():N}"};db.OutboxEvents.Add(row);await db.SaveChangesAsync();return row.PublicId;}
    private async Task<Guid> SeedSecurityAndPolicy(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var admin=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId);if(!await db.AdminSecurityProfiles.AnyAsync(x=>x.UserId==admin.Id))db.AdminSecurityProfiles.Add(new(){UserId=admin.Id,DetailRoleCode=AdminDetailRoles.SuperAdmin,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});var row=new DataRetentionPolicy{DomainCode=$"TEST_{Guid.NewGuid():N}",ActionCode="ANONYMIZE",RetentionDays=1825,LegalHoldDays=365,IsEnabled=false,DryRun=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.DataRetentionPolicies.Add(row);await db.SaveChangesAsync();return row.PublicId;}
    private static AnalyticsEvent Event(string type,Guid visitor,DateTime at)=>new(){EventTypeCode=type,VisitorId=visitor,OccurredAt=at};
    private static AdminAnalyticsMetricResponse Metric(AdminAnalyticsDashboardResponse value,string code)=>value.Kpis.Concat(value.Sections.SelectMany(x=>x.Metrics)).Single(x=>x.Code==code);
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private static Task<HttpResponseMessage> Login(HttpClient client,TestCredential credential)=>client.PostAsJsonAsync("/api/v1/auth/login",new{loginOrEmail=credential.LoginId,password=credential.Password});
}
