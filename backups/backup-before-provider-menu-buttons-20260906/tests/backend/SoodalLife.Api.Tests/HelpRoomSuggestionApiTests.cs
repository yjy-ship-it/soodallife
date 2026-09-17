using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.HelpRoom;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class HelpRoomSuggestionApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task HelpRoom_CustomerCreatesMemberVisiblePost_WithoutExactLocation()
    {
        using var client=Client();await Login(client,RoleCodes.Customer);
        var create=await client.PostAsJsonAsync("/api/v1/help-room",new{categoryId=factory.Catalog.ServiceId,administrativeAreaId=factory.Catalog.AreaId,revealRegion=true,purposeCode="DIY",title="주방 조명 상태를 확인하고 싶어요",body="사진을 보고 안전하게 먼저 확인할 수 있는 순서를 알려주세요."});
        Assert.Equal(HttpStatusCode.OK,create.StatusCode);var detail=await create.Content.ReadFromJsonAsync<HelpPostDetail>();Assert.NotNull(detail);Assert.NotNull(detail.RegionName);Assert.DoesNotContain("주소",detail.Body);Assert.StartsWith("수달회원-",detail.AuthorAlias);
        var list=await client.GetAsync("/api/v1/help-room");Assert.Equal(HttpStatusCode.OK,list.StatusCode);
    }

    [Fact]
    public async Task SuggestionBox_SixthSameDaySubmission_IsRateLimited()
    {
        using var client=Client();await Login(client,RoleCodes.Customer);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var credential=factory.Credentials[RoleCodes.Customer];var user=await db.Users.SingleAsync(x=>x.LoginId==credential.LoginId);var start=DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9).Date.AddHours(-9),DateTimeKind.Utc);var count=await db.UserSuggestions.CountAsync(x=>x.UserId==user.Id&&x.CreatedAt>=start);for(var i=count;i<5;i++){var now=DateTime.UtcNow.AddMinutes(-10-i);db.UserSuggestions.Add(new UserSuggestion{UserId=user.Id,TypeCode="BUG",VisibilityCode="PRIVATE",StatusCode="RECEIVED",Title=$"제한 검증 {i}",Body=$"일일 제한 검증용 서로 다른 내용 {i}",ContentHash=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32),IdempotencyKey=Guid.NewGuid().ToString("N"),CreatedAt=now,CreatedByUserId=user.Id,UpdatedAt=now,UpdatedByUserId=user.Id});}await db.SaveChangesAsync();}
        var sixth=await client.PostAsJsonAsync("/api/v1/suggestions",new{typeCode="UX",title="오늘 여섯 번째 제안입니다",body="하루 등록 제한이 서버에서 동작하는지 확인합니다.",pageUrl="/test",deviceInfo="test",appVersion="v163",idempotencyKey=Guid.NewGuid().ToString("N")});Assert.Equal(HttpStatusCode.TooManyRequests,sixth.StatusCode);
    }

    [Fact]
    public async Task HelpRoom_ProviderFollowUpRequiresCustomerQuestion_AndBlocksCooldownDuplicateAndClosedPost()
    {
        using var customer=Client();using var provider=Client();await Login(customer,RoleCodes.Customer);await Login(provider,RoleCodes.Provider);
        var createdResponse=await customer.PostAsJsonAsync("/api/v1/help-room",new{categoryId=factory.Catalog.ServiceId,administrativeAreaId=(Guid?)null,revealRegion=false,purposeCode="DIAGNOSIS",title="세면대 물 흐름을 확인하고 싶어요",body="물이 천천히 내려가는데 먼저 확인할 안전한 항목을 알려주세요."});Assert.Equal(HttpStatusCode.OK,createdResponse.StatusCode);var post=await createdResponse.Content.ReadFromJsonAsync<HelpPostDetail>();Assert.NotNull(post);
        object first=new{body="배수구 거름망부터 확인해 주세요.",cause="이물질 가능성",check="거름망 상태",diySteps="장갑을 끼고 보이는 이물질만 제거",risk="약품 혼합 금지",nextStep="계속되면 전문가 점검",requiresProfessional=false};
        var firstResponse=await provider.PostAsJsonAsync($"/api/v1/help-room/{post!.Id}/advice",first);Assert.Equal(HttpStatusCode.OK,firstResponse.StatusCode);
        var cooldown=await provider.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/advice",new{body="다른 확인 방법을 안내합니다.",cause="배관 흐름",check="배수 속도",diySteps="물을 조금씩 흘려 확인",risk="분해 금지",nextStep="추가 질문을 남겨주세요",requiresProfessional=false});Assert.Equal(HttpStatusCode.TooManyRequests,cooldown.StatusCode);
        await AgeLatestProviderAdvice(post.Id);
        var noQuestion=await provider.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/advice",new{body="후속 확인 방법입니다.",cause="배관 흐름",check="배수 속도",diySteps="물을 조금씩 흘려 확인",risk="분해 금지",nextStep="추가 질문을 남겨주세요",requiresProfessional=false});Assert.Equal(HttpStatusCode.Conflict,noQuestion.StatusCode);
        var question=await customer.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/follow-ups",new{body="거름망을 청소했는데도 계속 느립니다. 다음은 무엇을 확인할까요?"});Assert.Equal(HttpStatusCode.OK,question.StatusCode);
        var followUp=await provider.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/advice",new{body="배수 트랩 외부 누수 여부만 확인해 주세요.",cause="트랩 막힘 가능성",check="외부 누수",diySteps="눈으로만 확인",risk="직접 분해하지 마세요",nextStep="지속되면 방문 점검",requiresProfessional=true});Assert.Equal(HttpStatusCode.OK,followUp.StatusCode);
        await AgeLatestProviderAdvice(post.Id);
        var duplicate=await provider.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/advice",first);Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);
        var resolved=await customer.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/resolve",new{resolutionCode="SELF_RESOLVED",summary="안내에 따라 확인하고 문제를 해결했습니다.",helpfulEntryId=(Guid?)null});Assert.Equal(HttpStatusCode.OK,resolved.StatusCode);
        var closed=await provider.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/advice",new{body="종료 후 답변 시도입니다.",cause="",check="",diySteps="",risk="",nextStep="",requiresProfessional=false});Assert.Equal(HttpStatusCode.NotFound,closed.StatusCode);
    }

    [Fact]
    public async Task HelpRoom_ProviderMode_ShowsOnlyEligibleOpenPostsAndActionState()
    {
        using var customer=Client();using var provider=Client();await Login(customer,RoleCodes.Customer);await Login(provider,RoleCodes.Provider);
        var created=await customer.PostAsJsonAsync("/api/v1/help-room",new{categoryId=factory.Catalog.ServiceId,administrativeAreaId=(Guid?)null,revealRegion=false,purposeCode="DIAGNOSIS",title="전문가 답변 목록 상태 확인",body="승인된 전문가 화면에서 답변 가능 상태가 올바르게 표시되는지 확인합니다."});
        Assert.Equal(HttpStatusCode.OK,created.StatusCode);var post=await created.Content.ReadFromJsonAsync<HelpPostDetail>();Assert.NotNull(post);
        var initial=(await provider.GetFromJsonAsync<IReadOnlyList<HelpPostListItem>>("/api/v1/help-room?mode=provider"))!;Assert.Equal("ANSWERABLE",Assert.Single(initial,x=>x.Id==post!.Id).ProviderAction);
        var advice=await provider.PostAsJsonAsync($"/api/v1/help-room/{post!.Id}/advice",new{body="먼저 전원을 끄고 눈으로 상태를 확인해 주세요.",cause="연결 상태 가능성",check="외관 상태",diySteps="분해 없이 외관만 확인",risk="전원이 켜진 상태에서 만지지 마세요",nextStep="이상이 있으면 전문가 점검",requiresProfessional=false});Assert.Equal(HttpStatusCode.OK,advice.StatusCode);
        var waiting=(await provider.GetFromJsonAsync<IReadOnlyList<HelpPostListItem>>("/api/v1/help-room?mode=provider"))!;Assert.Equal("WAITING_CUSTOMER",Assert.Single(waiting,x=>x.Id==post.Id).ProviderAction);
        Assert.Equal(HttpStatusCode.OK,(await customer.PostAsJsonAsync($"/api/v1/help-room/{post.Id}/follow-ups",new{body="외관을 확인했습니다. 다음 단계는 무엇인가요?"})).StatusCode);
        var followUp=(await provider.GetFromJsonAsync<IReadOnlyList<HelpPostListItem>>("/api/v1/help-room?mode=provider"))!;Assert.Equal("FOLLOW_UP_AVAILABLE",Assert.Single(followUp,x=>x.Id==post.Id).ProviderAction);
        var customerList=(await customer.GetFromJsonAsync<IReadOnlyList<HelpPostListItem>>("/api/v1/help-room"))!;Assert.Null(Assert.Single(customerList,x=>x.Id==post.Id).ProviderAction);
    }

    [Fact]
    public async Task HelpRoom_Photo_IsStructurallyValidatedSanitizedPrivatelyNamedAndDeduplicated()
    {
        using var customer=Client();await Login(customer,RoleCodes.Customer);
        var created=await customer.PostAsJsonAsync("/api/v1/help-room",new{categoryId=factory.Catalog.ServiceId,administrativeAreaId=(Guid?)null,revealRegion=false,purposeCode="DIAGNOSIS",title="사진 안전처리 확인용 도움 요청",body="업로드한 사진의 위치정보와 파일 안전처리가 되는지 확인합니다."});
        var post=await created.Content.ReadFromJsonAsync<HelpPostDetail>();Assert.NotNull(post);
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        async Task<HttpResponseMessage> Upload(byte[] bytes,string name,string type){using var form=new MultipartFormDataContent();form.Add(new ByteArrayContent(bytes){Headers={ContentType=new(type)}},"file",name);return await customer.PostAsync($"/api/v1/help-room/{post!.Id}/files",form);}
        var uploaded=await Upload(png,"gps-location-original.png","image/png");Assert.Equal(HttpStatusCode.OK,uploaded.StatusCode);var photo=await uploaded.Content.ReadFromJsonAsync<HelpFileResponse>();Assert.NotNull(photo);Assert.True(photo.PubliclySafe);Assert.Equal("help-photo-1.png",photo.FileName);
        var preview=await customer.GetAsync(photo.DownloadUrl);Assert.Equal(HttpStatusCode.OK,preview.StatusCode);Assert.Equal("image/png",preview.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Conflict,(await Upload(png,"same-again.png","image/png")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Upload(png,"disguised.exe","image/png")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Upload(System.Text.Encoding.UTF8.GetBytes("not an image"),"fake.png","image/png")).StatusCode);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var stored=await db.Files.SingleAsync(x=>x.PublicId==photo.Id);Assert.Equal("private",stored.StorageContainer);Assert.Equal("CLEAN",stored.MalwareScanStatusCode);Assert.Equal("SAFE",stored.PrivacyInspectionStatusCode);Assert.Equal("COMPLETED",stored.SanitizationStatusCode);Assert.DoesNotContain("gps-location",stored.OriginalFileName,StringComparison.OrdinalIgnoreCase);Assert.Contains("METADATA_REMOVED",stored.ScanResultText);
    }

    private async Task AgeLatestProviderAdvice(Guid postId){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var post=await db.HelpPosts.SingleAsync(x=>x.PublicId==postId);var entry=await db.HelpRoomEntries.Where(x=>x.HelpPostId==post.Id&&x.AuthorRoleCode=="PROVIDER").OrderByDescending(x=>x.CreatedAt).FirstAsync();entry.CreatedAt=DateTime.UtcNow.AddMinutes(-2);await db.SaveChangesAsync();}

    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private async Task Login(HttpClient client,string role){var c=factory.Credentials[role];var result=await client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=c.LoginId,c.Password});Assert.Equal(HttpStatusCode.OK,result.StatusCode);}
}
