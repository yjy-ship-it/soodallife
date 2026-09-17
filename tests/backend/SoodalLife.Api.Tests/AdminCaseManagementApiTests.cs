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

public sealed class AdminCaseManagementApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanAccessReportSanctionAndMasterManagement()
    {using var client=Client();await Login(client,RoleCodes.Admin);Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/admin/reports")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/admin/sanctions")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/admin/case-masters/report-types")).StatusCode);}

    [Fact]
    public async Task Admin_CanSafelyInitializeDefaultReportTypesMoreThanOnce()
    {
        using var client=Client();await Login(client,RoleCodes.Admin);
        var first=await client.PostAsync("/api/v1/admin/case-masters/report-types/initialize",null);
        var second=await client.PostAsync("/api/v1/admin/case-masters/report-types/initialize",null);
        Assert.Equal(HttpStatusCode.OK,first.StatusCode);Assert.Equal(HttpStatusCode.OK,second.StatusCode);
        var values=await second.Content.ReadFromJsonAsync<List<CaseMasterResponse>>();Assert.NotNull(values);
        var defaults=values!.Where(x=>new[]{"TRANSACTION_BREACH","QUALITY_DEFECT","PAYMENT_CHARGE_ISSUE","NO_SHOW_COMMUNICATION","SAFETY_PROPERTY_DAMAGE","HARASSMENT_ABUSE","FRAUD_MISREPRESENTATION","PRIVACY_VIOLATION","INAPPROPRIATE_CONTENT","OTHER"}.Contains(x.Code)).ToList();
        Assert.Equal(10,defaults.Count);Assert.Equal(10,defaults.Select(x=>x.Code).Distinct().Count());Assert.All(defaults,x=>Assert.True(x.IsActive));Assert.Contains(defaults,x=>x.Code=="QUALITY_DEFECT"&&x.Name=="작업 품질·하자"&&x.DisplayOrder==20);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(10,await db.ReportTypes.CountAsync(x=>new[]{"TRANSACTION_BREACH","QUALITY_DEFECT","PAYMENT_CHARGE_ISSUE","NO_SHOW_COMMUNICATION","SAFETY_PROPERTY_DAMAGE","HARASSMENT_ABUSE","FRAUD_MISREPRESENTATION","PRIVACY_VIOLATION","INAPPROPRIATE_CONTENT","OTHER"}.Contains(x.Code)));
    }

    [Theory,InlineData(RoleCodes.Customer),InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessCaseManagement(string role)
    {using var client=Client();await Login(client,role);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/reports")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/sanctions")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/case-masters/sanction-types")).StatusCode);}

    [Fact]
    public async Task Admin_CanCreateReportWithoutAutomaticSanctionOrTrustChange()
    {Guid reporter,reported,typeId;int sanctions,events;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var customer=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Customer].LoginId);var provider=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var type=new ReportType{Code=$"TEST_{Guid.NewGuid():N}",Name="테스트 신고유형",IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.ReportTypes.Add(type);await db.SaveChangesAsync();reporter=customer.PublicId;reported=provider.PublicId;typeId=type.PublicId;sanctions=await db.Sanctions.CountAsync();events=await db.TrustScoreEvents.CountAsync();}using var client=Client();await Login(client,RoleCodes.Admin);var response=await client.PostAsJsonAsync("/api/v1/admin/reports",new{reporterUserId=reporter,reportedUserId=reported,reportTypeId=typeId,description="거래 외 신고 접수 검증",idempotencyKey=$"report-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var verify=factory.Services.CreateScope();var check=verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(sanctions,await check.Sanctions.CountAsync());Assert.Equal(events,await check.TrustScoreEvents.CountAsync());}

    [Fact]
    public async Task Admin_CanDecideReleaseAndReviewAppeal_WithoutTrustAutomation()
    {Guid reportId,targetId,sanctionTypeId;int trustEvents;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var admin=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId);var customer=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Customer].LoginId);var target=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var reportType=new ReportType{Code=$"REPORT_{Guid.NewGuid():N}",Name="제재 근거 신고",IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};var sanctionType=new SanctionType{Code=$"SANCTION_{Guid.NewGuid():N}",Name="승인된 제재유형",IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.AddRange(reportType,sanctionType);await db.SaveChangesAsync();var report=new Report{ReporterUserId=customer.Id,ReportedUserId=target.Id,ReportTypeId=reportType.Id,Description="제재 근거",ReceivedAt=DateTime.UtcNow,IdempotencyKey=$"source-{Guid.NewGuid():N}",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,CreatedByUserId=admin.Id,UpdatedByUserId=admin.Id};db.Reports.Add(report);await db.SaveChangesAsync();reportId=report.PublicId;targetId=target.PublicId;sanctionTypeId=sanctionType.PublicId;trustEvents=await db.TrustScoreEvents.CountAsync();}using var client=Client();await Login(client,RoleCodes.Admin);var create=await client.PostAsJsonAsync("/api/v1/admin/sanctions",new{targetUserId=targetId,targetRoleCode=(string?)null,providerId=(Guid?)null,providerServiceId=(Guid?)null,sanctionTypeId,reason="관리자 근거 검토 완료",startAt=DateTime.UtcNow.AddMinutes(-1),endAt=(DateTime?)null,sources=new[]{new{reportId=(Guid?)reportId,disputeCaseId=(Guid?)null,reviewId=(Guid?)null,afterServiceCaseId=(Guid?)null}},idempotencyKey=$"sanction-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,create.StatusCode);var sanction=await create.Content.ReadFromJsonAsync<AdminSanctionDetail>();Assert.NotNull(sanction);var appealResponse=await client.PostAsJsonAsync($"/api/v1/admin/sanctions/{sanction.Id}/appeals",new{applicantUserId=targetId,previousAppealId=(Guid?)null,statement="결정 재검토 요청",idempotencyKey=$"appeal-{Guid.NewGuid():N}"});Assert.Equal(HttpStatusCode.OK,appealResponse.StatusCode);sanction=await appealResponse.Content.ReadFromJsonAsync<AdminSanctionDetail>();var appeal=Assert.Single(sanction!.Appeals);var assigned=await client.PostAsJsonAsync($"/api/v1/admin/sanctions/{sanction.Id}/appeals/{appeal.Id}/assignment",new{adminUserId=(Guid?)null,reason="이의신청 검토 시작",idempotencyKey=$"assign-{Guid.NewGuid():N}",appeal.RowVersion});Assert.Equal(HttpStatusCode.OK,assigned.StatusCode);sanction=await assigned.Content.ReadFromJsonAsync<AdminSanctionDetail>();appeal=Assert.Single(sanction!.Appeals);var decision=await client.PostAsJsonAsync($"/api/v1/admin/sanctions/{sanction.Id}/appeals/{appeal.Id}/decision",new{decisionCode="REJECTED",decisionReason="제출 근거 재검토 결과",idempotencyKey=$"decision-{Guid.NewGuid():N}",appeal.RowVersion});Assert.Equal(HttpStatusCode.OK,decision.StatusCode);sanction=await decision.Content.ReadFromJsonAsync<AdminSanctionDetail>();var release=await client.PostAsJsonAsync($"/api/v1/admin/sanctions/{sanction!.Id}/status",new{actionCode="RELEASE",reason="운영자 해제 결정",idempotencyKey=$"release-{Guid.NewGuid():N}",sanction.RowVersion});Assert.Equal(HttpStatusCode.OK,release.StatusCode);using var verify=factory.Services.CreateScope();var check=verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Equal(trustEvents,await check.TrustScoreEvents.CountAsync());Assert.Equal("ACTIVE",(await check.Users.SingleAsync(x=>x.PublicId==targetId)).StatusCode);}

    [Fact]
    public async Task SanctionEvent_IsAppendOnlyAtApplicationLevel()
    {using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var admin=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId);var target=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var type=new SanctionType{Code=$"TEST_{Guid.NewGuid():N}",Name="테스트 제재유형",IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.SanctionTypes.Add(type);await db.SaveChangesAsync();var sanction=new Sanction{TargetUserId=target.Id,SanctionTypeId=type.Id,StatusCode="DECIDED",Reason="append-only 검증",StartAt=DateTime.UtcNow,DecidedAt=DateTime.UtcNow,DecidedByUserId=admin.Id,IdempotencyKey=$"sanction-{Guid.NewGuid():N}",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.Sanctions.Add(sanction);await db.SaveChangesAsync();var item=new SanctionEvent{SanctionId=sanction.Id,ActionTypeCode="DECIDED",ToStatusCode="DECIDED",ActorUserId=admin.Id,Reason="최초 결정",SnapshotJson="{}",OccurredAt=DateTime.UtcNow,CreatedAt=DateTime.UtcNow,IdempotencyKey=$"event-{Guid.NewGuid():N}"};db.SanctionEvents.Add(item);await db.SaveChangesAsync();item.Reason="수정 시도";var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());Assert.Contains("수정하거나 삭제할 수 없습니다",error.Message);}

    [Fact]
    public void SanctionEvent_IdempotencyIndex_IsPresent()
    {using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var eventEntity=db.Model.FindEntityType(typeof(SanctionEvent));Assert.NotNull(eventEntity);Assert.Contains(eventEntity.GetIndexes(),x=>x.IsUnique&&x.Properties.Single().Name==nameof(SanctionEvent.IdempotencyKey));}

    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});
}
