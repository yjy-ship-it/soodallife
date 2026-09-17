using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Notifications;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminNotificationBroadcastApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task MarketingBroadcast_RejectsKakao_AndHasNoExternalContactImport()
    {
        using var admin=Client();await Login(admin);
        var response=await admin.PostAsJsonAsync("/api/v1/admin/notification-broadcasts",new{name="광고 발송",audienceCode="CUSTOMER",kindCode="MARKETING",channels=new[]{"WEB","KAKAO"},title="광고",body="광고 본문",scheduledAt=(DateTime?)null,rowVersion=(string?)null});
        Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        var import=await admin.PostAsJsonAsync("/api/v1/admin/notification-broadcasts/import",new{contacts=new[]{"01012345678"}});
        Assert.Contains(import.StatusCode,new[]{HttpStatusCode.NotFound,HttpStatusCode.MethodNotAllowed});
    }

    [Fact]
    public async Task MemberBroadcast_RequiresPreviewReauthAndSecondConfirmation_ThenBatchesDistinctUsers()
    {
        await AddDualRoleUser();await EnsureAdminSecurityProfile();using var admin=Client();await Login(admin);
        var createdResponse=await admin.PostAsJsonAsync("/api/v1/admin/notification-broadcasts",new{name="가입 회원 공지",audienceCode="ALL",kindCode="BUSINESS_NOTICE",channels=new[]{"WEB"},title="서비스 공지",body="가입 회원에게 보내는 정보성 공지입니다.",scheduledAt=(DateTime?)null,rowVersion=(string?)null});
        Assert.Equal(HttpStatusCode.OK,createdResponse.StatusCode);var created=(await createdResponse.Content.ReadFromJsonAsync<NotificationBroadcastItem>())!;
        var previewResponse=await admin.PostAsync($"/api/v1/admin/notification-broadcasts/{created.Id}/preview",null);Assert.Equal(HttpStatusCode.OK,previewResponse.StatusCode);var preview=(await previewResponse.Content.ReadFromJsonAsync<NotificationBroadcastPreview>())!;Assert.True(preview.DuplicateRolesRemoved>=1);
        var current=Assert.Single((await admin.GetFromJsonAsync<PagedNotificationBroadcasts>("/api/v1/admin/notification-broadcasts?page=1&pageSize=20"))!.Items,x=>x.Id==created.Id);
        var withoutReauth=await admin.PostAsJsonAsync($"/api/v1/admin/notification-broadcasts/{created.Id}/confirm",new{confirmationText="발송 확인",current.RowVersion});Assert.Equal((HttpStatusCode)428,withoutReauth.StatusCode);
        var auth=(await (await admin.PostAsJsonAsync("/api/v1/admin/security/reauthenticate",new{password=factory.Credentials[RoleCodes.Admin].Password,mfaCode=(string?)null})).Content.ReadFromJsonAsync<AdminReauthenticateResponse>())!;
        using(var wrong=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/admin/notification-broadcasts/{created.Id}/confirm")){wrong.Headers.Add("X-Admin-Reauth-Token",auth.Token);wrong.Content=JsonContent.Create(new{confirmationText="확인",current.RowVersion});Assert.Equal(HttpStatusCode.Conflict,(await admin.SendAsync(wrong)).StatusCode);}
        using(var confirm=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/admin/notification-broadcasts/{created.Id}/confirm")){confirm.Headers.Add("X-Admin-Reauth-Token",auth.Token);confirm.Content=JsonContent.Create(new{confirmationText="발송 확인",current.RowVersion});Assert.Equal(HttpStatusCode.OK,(await admin.SendAsync(confirm)).StatusCode);}
        using var scope=factory.Services.CreateScope();var service=scope.ServiceProvider.GetRequiredService<NotificationBroadcastService>();while((await service.PrepareBatch(default)).Processed>0){}await service.RefreshCompleted(default);var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var broadcast=await db.NotificationBroadcasts.SingleAsync(x=>x.PublicId==created.Id);Assert.Equal("COMPLETED",broadcast.StatusCode);var recipients=await db.NotificationRecipients.Where(x=>x.NotificationId==broadcast.NotificationId).Select(x=>x.UserId).ToListAsync();Assert.Equal(recipients.Distinct().Count(),recipients.Count);Assert.Equal(preview.EligibleByChannel["WEB"],recipients.Count);
    }

    private async Task EnsureAdminSecurityProfile(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var admin=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId);if(!await db.AdminSecurityProfiles.AnyAsync(x=>x.UserId==admin.Id)){db.AdminSecurityProfiles.Add(new AdminSecurityProfile{UserId=admin.Id,DetailRoleCode=AdminDetailRoles.Operations,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});await db.SaveChangesAsync();}}
    private async Task AddDualRoleUser(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();if(await db.Users.AnyAsync(x=>x.LoginId=="broadcast-dual-role"))return;var user=new User{LoginId="broadcast-dual-role",NormalizedLoginId="BROADCAST-DUAL-ROLE",StatusCode="ACTIVE",PhoneVerificationStatusCode="VERIFIED"};db.Users.Add(user);await db.SaveChangesAsync();var roles=await db.Roles.Where(x=>x.Code==RoleCodes.Customer||x.Code==RoleCodes.Provider).ToListAsync();db.UserRoles.AddRange(roles.Select(role=>new UserRole{UserId=user.Id,RoleId=role.Id}));await db.SaveChangesAsync();}
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private Task<HttpResponseMessage> Login(HttpClient client)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[RoleCodes.Admin].LoginId,factory.Credentials[RoleCodes.Admin].Password});
}


