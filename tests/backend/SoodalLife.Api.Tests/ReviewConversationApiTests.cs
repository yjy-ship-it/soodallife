using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Reviews;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ReviewConversationApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task CustomerAndProviderCanContinueNestedReviewConversation()
    {
        var review=await SeedReview();using var customer=Client();using var provider=Client();await Login(customer,factory.Credentials[RoleCodes.Customer]);await Login(provider,factory.Credentials[RoleCodes.Provider]);
        var first=await Post(customer,$"/api/v1/customers/me/reviews/{review}/comments","고객 첫 댓글",null);var second=await Post(provider,$"/api/v1/providers/me/reviews/{review}/comments","전문가 답글",first.Id);var third=await Post(customer,$"/api/v1/customers/me/reviews/{review}/comments","고객의 추가 답글",second.Id);
        Assert.Equal(0,first.Depth);Assert.Equal(1,second.Depth);Assert.Equal(first.Id,second.ParentCommentId);Assert.Equal(2,third.Depth);Assert.Equal(second.Id,third.ParentCommentId);
        var customerList=(await customer.GetFromJsonAsync<ReviewConversationResponse[]>("/api/v1/customers/me/review-conversations"))!;var thread=Assert.Single(customerList,x=>x.ReviewId==review);Assert.Equal(3,thread.Comments.Count);
        var providerList=(await provider.GetFromJsonAsync<ReviewConversationResponse[]>("/api/v1/providers/me/review-conversations"))!;Assert.Equal(3,Assert.Single(providerList,x=>x.ReviewId==review).Comments.Count);
    }

    [Fact]
    public async Task UnrelatedCustomerAndProviderCannotJoinReviewConversation()
    {
        var review=await SeedReview();using var otherCustomer=Client();await Login(otherCustomer,factory.OtherCustomerCredential);Assert.Equal(HttpStatusCode.NotFound,(await otherCustomer.PostAsJsonAsync($"/api/v1/customers/me/reviews/{review}/comments",Body("접근 불가",null))).StatusCode);
        using var otherProvider=Client();await Login(otherProvider,factory.ServiceMismatchProviderCredential);Assert.Equal(HttpStatusCode.NotFound,(await otherProvider.PostAsJsonAsync($"/api/v1/providers/me/reviews/{review}/comments",Body("접근 불가",null))).StatusCode);
    }

    [Fact]
    public async Task AdminCanHideCommentAndParticipantsSeePlaceholder()
    {
        var review=await SeedReview();using var customer=Client();await Login(customer,factory.Credentials[RoleCodes.Customer]);var comment=await Post(customer,$"/api/v1/customers/me/reviews/{review}/comments","숨김 대상 원문",null);
        using var admin=Client();await Login(admin,factory.Credentials[RoleCodes.Admin]);var response=await admin.PostAsJsonAsync($"/api/v1/admin/reviews/{review}/comments/{comment.Id}/moderation",new{targetStatus="HIDDEN",reason="개인정보 포함",rowVersion=comment.RowVersion});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var list=(await customer.GetFromJsonAsync<ReviewConversationResponse[]>("/api/v1/customers/me/review-conversations"))!;var hidden=Assert.Single(Assert.Single(list,x=>x.ReviewId==review).Comments);Assert.Equal("HIDDEN",hidden.Status);Assert.DoesNotContain("숨김 대상 원문",hidden.BodyText);Assert.Contains("숨김 처리",hidden.BodyText);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.Contains(await db.AuditLogs.Where(x=>x.EntityPublicId==comment.Id).ToListAsync(),x=>x.ActionCode=="REVIEW_COMMENT_HIDE"&&x.Reason=="개인정보 포함");
    }

    [Fact]
    public async Task HiddenReviewRejectsNewComments()
    {
        var review=await SeedReview("HIDDEN");using var customer=Client();await Login(customer,factory.Credentials[RoleCodes.Customer]);Assert.Equal(HttpStatusCode.Conflict,(await customer.PostAsJsonAsync($"/api/v1/customers/me/reviews/{review}/comments",Body("등록 불가",null))).StatusCode);
    }

    [Fact]
    public async Task NewCommentCreatesUnreadNoticeAndBlocksDuplicateOrRapidConsecutivePost()
    {
        var review=await SeedReview();using var customer=Client();using var provider=Client();await Login(customer,factory.Credentials[RoleCodes.Customer]);await Login(provider,factory.Credentials[RoleCodes.Provider]);
        Assert.Equal(HttpStatusCode.NoContent,(await provider.PostAsJsonAsync("/api/v1/providers/me/review-conversations/read",new{})).StatusCode);
        await Post(customer,$"/api/v1/customers/me/reviews/{review}/comments","알림 검증 댓글",null);
        var unread=(await provider.GetFromJsonAsync<ReviewConversationUnreadCountResponse>("/api/v1/providers/me/review-conversations/unread-count"))!;Assert.Equal(1,unread.Count);
        var list=(await provider.GetFromJsonAsync<ReviewConversationResponse[]>("/api/v1/providers/me/review-conversations"))!;Assert.Equal(1,Assert.Single(list,x=>x.ReviewId==review).UnreadCommentCount);
        Assert.Equal(HttpStatusCode.Conflict,(await customer.PostAsJsonAsync($"/api/v1/customers/me/reviews/{review}/comments",Body("알림 검증 댓글",null))).StatusCode);
        Assert.Equal((HttpStatusCode)429,(await customer.PostAsJsonAsync($"/api/v1/customers/me/reviews/{review}/comments",Body("너무 빠른 연속 댓글",null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await provider.PostAsJsonAsync("/api/v1/providers/me/review-conversations/read",new{})).StatusCode);
        Assert.Equal(0,(await provider.GetFromJsonAsync<ReviewConversationUnreadCountResponse>("/api/v1/providers/me/review-conversations/unread-count"))!.Count);
    }

    private async Task<Guid> SeedReview(string visibility="PUBLIC")
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var customerUser=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Customer].LoginId);var providerUser=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var customer=await db.CustomerProfiles.SingleAsync(x=>x.UserId==customerUser.Id);var provider=await db.ProviderProfiles.SingleAsync(x=>x.UserId==providerUser.Id);var now=DateTime.UtcNow;var transaction=new TransactionRecord{ServiceRequestId=0,AcceptedQuoteRevisionId=0,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,CategoryId=0,StatusCode="COMPLETED",AgreedAmount=10000,CurrencyCode="KRW",QuoteSnapshotJson="{}",CategoryPolicySnapshotJson="{}",CompletionPolicySnapshotJson="{}",CompletedAt=now,CreatedAt=now,UpdatedAt=now};db.Transactions.Add(transaction);await db.SaveChangesAsync();var review=new Review{TransactionId=transaction.Id,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,BodyText="다단계 댓글 검증 리뷰",VerificationStatusCode="VERIFIED_TRANSACTION",VisibilityStatusCode=visibility,IdempotencyKey=$"review-thread-{Guid.NewGuid():N}",SubmittedAt=now,PublishedAt=visibility=="PUBLIC"?now:null,CreatedAt=now,UpdatedAt=now};db.Reviews.Add(review);await db.SaveChangesAsync();return review.PublicId;
    }
    private async Task<ReviewCommentResponse> Post(HttpClient client,string path,string text,Guid? parent){var response=await client.PostAsJsonAsync(path,Body(text,parent));Assert.Equal(HttpStatusCode.OK,response.StatusCode);return(await response.Content.ReadFromJsonAsync<ReviewCommentResponse>())!;}
    private static object Body(string text,Guid? parent)=>new{bodyText=text,parentCommentId=parent,idempotencyKey=$"comment-{Guid.NewGuid():N}"};
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    private static Task<HttpResponseMessage> Login(HttpClient client,TestCredential credential)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=credential.LoginId,credential.Password});
}
