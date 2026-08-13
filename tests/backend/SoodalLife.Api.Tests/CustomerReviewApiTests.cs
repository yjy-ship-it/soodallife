using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Reviews;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CustomerReviewApiTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact] public async Task CompletedTransactionOwner_CanCreateVerifiedReview_WithDynamicRatingsAndFile()
    {var data=await Prepare(true,true);using var client=Client();await Login(client,RoleCodes.Customer);var response=await Post(client,data);response.EnsureSuccessStatusCode();var review=(await response.Content.ReadFromJsonAsync<ReviewResponse>())!;Assert.Equal("VERIFIED_TRANSACTION",review.VerificationStatusCode);Assert.Equal("PUBLIC",review.VisibilityStatusCode);Assert.Null(review.OverallRating);Assert.Single(review.Ratings);Assert.Equal(7,review.Ratings[0].MaxValue);Assert.Single(review.Files);}

    [Fact] public async Task IncompleteTransaction_IsRejected()
    {var data=await Prepare(false);using var client=Client();await Login(client,RoleCodes.Customer);Assert.Equal(HttpStatusCode.Conflict,(await Post(client,data)).StatusCode);}

    [Fact] public async Task OtherCustomer_CannotReviewTransaction()
    {var data=await Prepare(true);using var client=Client();await client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.OtherCustomerCredential.LoginId,factory.OtherCustomerCredential.Password});Assert.Equal(HttpStatusCode.Forbidden,(await Post(client,data)).StatusCode);}

    [Theory][InlineData(RoleCodes.Provider)][InlineData(RoleCodes.Admin)] public async Task NonCustomer_CannotCreateCustomerReview(string role)
    {var data=await Prepare(true);using var client=Client();await Login(client,role);Assert.Equal(HttpStatusCode.Forbidden,(await Post(client,data)).StatusCode);}

    [Fact] public async Task TransactionAllowsOnlyOneReview_ButSameIdempotencyKeyReturnsOriginal()
    {var data=await Prepare(true);using var client=Client();await Login(client,RoleCodes.Customer);var first=await Post(client,data,"same-review-key");first.EnsureSuccessStatusCode();var original=(await first.Content.ReadFromJsonAsync<ReviewResponse>())!;var retry=await Post(client,data,"same-review-key");retry.EnsureSuccessStatusCode();Assert.Equal(original.Id,(await retry.Content.ReadFromJsonAsync<ReviewResponse>())!.Id);Assert.Equal(HttpStatusCode.Conflict,(await Post(client,data,"different-review-key")).StatusCode);}

    [Fact] public async Task UnknownRatingItemAndOutOfRangeValue_AreRejectedAtomically()
    {var data=await Prepare(true);using var client=Client();await Login(client,RoleCodes.Customer);var unknown=await client.PostAsJsonAsync(Path(data.TransactionId),new{BodyText="실제 거래에 대한 검증 리뷰입니다.",Ratings=new[]{new{RatingItemId=Guid.NewGuid(),RatingValue=4m}},FileIds=Array.Empty<Guid>(),IdempotencyKey=Key()});Assert.Equal(HttpStatusCode.BadRequest,unknown.StatusCode);var range=await client.PostAsJsonAsync(Path(data.TransactionId),new{BodyText="실제 거래에 대한 검증 리뷰입니다.",Ratings=new[]{new{RatingItemId=data.RatingItemId,RatingValue=8m}},FileIds=Array.Empty<Guid>(),IdempotencyKey=Key()});Assert.Equal(HttpStatusCode.BadRequest,range.StatusCode);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.False(await db.Reviews.AnyAsync(x=>x.TransactionId==data.InternalTransactionId));Assert.False(await db.ReviewRatings.AnyAsync(x=>db.Reviews.Where(r=>r.TransactionId==data.InternalTransactionId).Select(r=>r.Id).Contains(x.ReviewId)));}

    [Fact] public async Task InvalidFile_RollsBackReviewAndRatings()
    {var data=await Prepare(true);using var client=Client();await Login(client,RoleCodes.Customer);var response=await client.PostAsJsonAsync(Path(data.TransactionId),new{BodyText="첨부파일 원자성 검증 리뷰입니다.",Ratings=new[]{new{RatingItemId=data.RatingItemId,RatingValue=6m}},FileIds=new[]{Guid.NewGuid()},IdempotencyKey=Key()});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();Assert.False(await db.Reviews.AnyAsync(x=>x.TransactionId==data.InternalTransactionId));}

    [Fact] public async Task PublicReview_ReturnsOnlyPublicData_AndProviderStatisticsAreNotTrustScore()
    {var data=await Prepare(true);using var customer=Client();await Login(customer,RoleCodes.Customer);(await Post(customer,data)).EnsureSuccessStatusCode();using var publicClient=Client();var list=await publicClient.GetFromJsonAsync<PublicReviewListResponse>($"/api/v1/providers/{data.ProviderId}/reviews");Assert.NotNull(list);var review=Assert.Single(list.Items,x=>x.TransactionId==data.TransactionId);Assert.Contains("*",review.CustomerDisplayName);var raw=await (await publicClient.GetAsync($"/api/v1/providers/{data.ProviderId}/reviews")).Content.ReadAsStringAsync();Assert.DoesNotContain(factory.Credentials[RoleCodes.Customer].LoginId,raw);var stats=await publicClient.GetFromJsonAsync<ProviderReviewStatisticsResponse>($"/api/v1/providers/{data.ProviderId}/review-statistics");Assert.NotNull(stats);Assert.True(stats.PublicReviewCount>0);Assert.Single(stats.RatingItemAverages,x=>x.ItemId==data.RatingItemId);}

    [Theory]
    [InlineData(null,"SAFE")]
    [InlineData("NOT_INTEGRATED","SAFE")]
    [InlineData("PENDING","SAFE")]
    [InlineData("PROCESSING","SAFE")]
    [InlineData("FAILED","SAFE")]
    [InlineData("INFECTED","SAFE")]
    [InlineData("CLEAN",null)]
    [InlineData("CLEAN","NOT_INTEGRATED")]
    [InlineData("CLEAN","PENDING")]
    [InlineData("CLEAN","PROCESSING")]
    [InlineData("CLEAN","FAILED")]
    [InlineData("CLEAN","SENSITIVE_DETECTED")]
    public async Task PublicReview_FailsClosed_ForUnsafeOrIncompleteAttachment(string? malware,string? privacy)
    {
        const string originalName="01012345678_서울시강남구상세주소.jpg";
        var value=await CreateAttachedReview(originalName);await SetSafety(value.FileId,malware,privacy,null);
        using var client=Client();var response=await client.GetAsync($"/api/v1/providers/{value.Data.ProviderId}/reviews");response.EnsureSuccessStatusCode();
        var raw=await response.Content.ReadAsStringAsync();var list=(await response.Content.ReadFromJsonAsync<PublicReviewListResponse>())!;var review=Assert.Single(list.Items,x=>x.Id==value.ReviewId);
        Assert.Empty(review.Files);Assert.DoesNotContain(originalName,raw);Assert.DoesNotContain(value.FileId.ToString(),raw,StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public async Task PublicReview_PublishesOnlyCleanSafeAttachment_WithFriendlyMetadataAndProtectedDownload()
    {
        const string originalName="01012345678_우리집사진.jpg";var value=await CreateAttachedReview(originalName);await SetSafety(value.FileId,"CLEAN","SAFE",null);
        using var publicClient=Client();var list=(await publicClient.GetFromJsonAsync<PublicReviewListResponse>($"/api/v1/providers/{value.Data.ProviderId}/reviews"))!;var review=Assert.Single(list.Items,x=>x.Id==value.ReviewId);var file=Assert.Single(review.Files);
        Assert.Equal(value.FileId,file.FileId);Assert.Equal("리뷰 첨부 이미지",file.FileName);Assert.Equal("PRIVACY_SAFE_ORIGINAL",file.PublicationMode);Assert.NotNull(file.DownloadUrl);Assert.DoesNotContain(originalName,file.FileName);
        var download=await publicClient.GetAsync(file.DownloadUrl);download.EnsureSuccessStatusCode();Assert.Equal("image/jpeg",download.Content.Headers.ContentType?.MediaType);Assert.DoesNotContain(originalName,download.Content.Headers.ContentDisposition?.FileName??string.Empty);
    }

    [Fact] public async Task PublicReview_PrefersSanitizedDerivative_AndBlocksOriginalIdentifier()
    {
        const string originalName="서울시강남구상세주소.jpg";var value=await CreateAttachedReview(originalName);var derivativeId=await AddSanitizedDerivative(value.FileId);
        using var client=Client();var response=await client.GetAsync($"/api/v1/providers/{value.Data.ProviderId}/reviews");var raw=await response.Content.ReadAsStringAsync();var list=(await response.Content.ReadFromJsonAsync<PublicReviewListResponse>())!;var file=Assert.Single(Assert.Single(list.Items,x=>x.Id==value.ReviewId).Files);
        Assert.Equal(derivativeId,file.FileId);Assert.Equal("PRIVACY_SANITIZED_DERIVATIVE",file.PublicationMode);Assert.DoesNotContain(originalName,raw);Assert.DoesNotContain(value.FileId.ToString(),raw,StringComparison.OrdinalIgnoreCase);
        (await client.GetAsync(file.DownloadUrl)).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/providers/{value.Data.ProviderId}/reviews/{value.ReviewId}/files/{value.FileId}")).StatusCode);
    }

    [Fact] public async Task ReviewOwner_RetainsOriginalAccess_ButOtherCustomerIsDenied()
    {
        const string originalName="01012345678_소유자사진.jpg";var value=await CreateAttachedReview(originalName);
        using var owner=Client();await Login(owner,RoleCodes.Customer);var mine=(await owner.GetFromJsonAsync<List<ReviewResponse>>("/api/v1/customers/me/reviews"))!;var file=Assert.Single(Assert.Single(mine,x=>x.Id==value.ReviewId).Files);Assert.Equal(originalName,file.FileName);Assert.Equal("OWNER_ORIGINAL",file.PublicationMode);(await owner.GetAsync(file.DownloadUrl)).EnsureSuccessStatusCode();
        using var other=Client();await other.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.OtherCustomerCredential.LoginId,factory.OtherCustomerCredential.Password});Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(file.DownloadUrl)).StatusCode);
    }

    [Fact] public async Task ProviderPublicReviewSurface_DoesNotBypassAttachmentPrivacy()
    {
        var value=await CreateAttachedReview("01012345678_공급자에게숨김.jpg");await SetSafety(value.FileId,"NOT_INTEGRATED","NOT_INTEGRATED",null);
        using var provider=Client();await Login(provider,RoleCodes.Provider);var list=(await provider.GetFromJsonAsync<PublicReviewListResponse>($"/api/v1/providers/{value.Data.ProviderId}/reviews"))!;Assert.Empty(Assert.Single(list.Items,x=>x.Id==value.ReviewId).Files);
    }

    [Fact] public async Task PublicReviewDownload_BlocksHiddenReviewCrossReviewAndCrossResourceIds()
    {
        var first=await CreateAttachedReview("first-private-name.jpg");await SetSafety(first.FileId,"CLEAN","SAFE",null);var second=await CreateAttachedReview("second-private-name.jpg");await SetSafety(second.FileId,"CLEAN","SAFE",null);
        using var client=Client();Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/providers/{first.Data.ProviderId}/reviews/{first.ReviewId}/files/{second.FileId}")).StatusCode);
        var unrelated=await AddUnrelatedSafeFile();Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/providers/{first.Data.ProviderId}/reviews/{first.ReviewId}/files/{unrelated}")).StatusCode);
        await HideReview(first.ReviewId);Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/providers/{first.Data.ProviderId}/reviews/{first.ReviewId}/files/{first.FileId}")).StatusCode);
    }

    [Fact] public async Task PublicReviewMetadata_DoesNotExposeCustomerOrStorageSecrets()
    {
        const string originalName="01012345678_서울시강남구상세주소.jpg";var value=await CreateAttachedReview(originalName);await SetSafety(value.FileId,"NOT_INTEGRATED","NOT_INTEGRATED",null);
        using var client=Client();var raw=await (await client.GetAsync($"/api/v1/providers/{value.Data.ProviderId}/reviews")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("01012345678",raw);Assert.DoesNotContain("서울시강남구상세주소",raw);Assert.DoesNotContain("customer@sudal.example.kr",raw);Assert.DoesNotContain("storageKey",raw,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("storageContainer",raw,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("internalFile",raw,StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public async Task Admin_CanListAndInspectReview_ButHasNoContentMutationApi()
    {var data=await Prepare(true);using var customer=Client();await Login(customer,RoleCodes.Customer);var created=(await (await Post(customer,data)).Content.ReadFromJsonAsync<ReviewResponse>())!;using var admin=Client();await Login(admin,RoleCodes.Admin);var list=await admin.GetFromJsonAsync<AdminReviewListResponse>($"/api/v1/admin/reviews?search={Uri.EscapeDataString("수달")}&verificationStatus=VERIFIED_TRANSACTION&visibilityStatus=PUBLIC&page=1&pageSize=20");Assert.NotNull(list);var item=Assert.Single(list.Items,x=>x.Id==created.Id);var numbered=await admin.GetFromJsonAsync<AdminReviewListResponse>($"/api/v1/admin/reviews?search={item.ReviewNumber}");Assert.Contains(numbered!.Items,x=>x.Id==created.Id);var detail=await admin.GetFromJsonAsync<AdminReviewDetailResponse>($"/api/v1/admin/reviews/{created.Id}");Assert.NotNull(detail);Assert.Equal("실제 거래에 대한 검증 리뷰입니다.",detail.BodyText);Assert.Equal(HttpStatusCode.MethodNotAllowed,(await admin.PutAsJsonAsync($"/api/v1/admin/reviews/{created.Id}",new{BodyText="관리자 변경"})).StatusCode);}

    [Theory][InlineData(RoleCodes.Customer)][InlineData(RoleCodes.Provider)] public async Task NonAdmin_CannotAccessAdminReviewApi(string role)
    {using var client=Client();await Login(client,role);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/reviews")).StatusCode);}

    [Fact] public async Task AdminVisibilityChange_PreservesOriginalAndWritesAudit()
    {var data=await Prepare(true);using var customer=Client();await Login(customer,RoleCodes.Customer);var created=(await (await Post(customer,data)).Content.ReadFromJsonAsync<ReviewResponse>())!;using var admin=Client();await Login(admin,RoleCodes.Admin);var detail=(await admin.GetFromJsonAsync<AdminReviewDetailResponse>($"/api/v1/admin/reviews/{created.Id}"))!;var hidden=await admin.PostAsJsonAsync($"/api/v1/admin/reviews/{created.Id}/visibility",new{TargetStatusCode="HIDDEN",Reason="운영정책 검토를 위한 숨김",detail.RowVersion});hidden.EnsureSuccessStatusCode();var changed=(await hidden.Content.ReadFromJsonAsync<AdminReviewDetailResponse>())!;Assert.Equal("HIDDEN",changed.VisibilityStatusCode);Assert.Equal(detail.BodyText,changed.BodyText);Assert.Contains(changed.Audit,x=>x.ActionCode=="REVIEW_HIDE"&&x.Reason=="운영정책 검토를 위한 숨김");var restored=await admin.PostAsJsonAsync($"/api/v1/admin/reviews/{created.Id}/visibility",new{TargetStatusCode="PUBLIC",Reason="검토 완료 후 공개 복원",changed.RowVersion});restored.EnsureSuccessStatusCode();Assert.Equal("PUBLIC",(await restored.Content.ReadFromJsonAsync<AdminReviewDetailResponse>())!.VisibilityStatusCode);}

    [Fact] public async Task ProviderCanReplyOnlyToOwnReview()
    {var data=await Prepare(true);using var customer=Client();await Login(customer,RoleCodes.Customer);var created=(await (await Post(customer,data)).Content.ReadFromJsonAsync<ReviewResponse>())!;using var provider=Client();await Login(provider,RoleCodes.Provider);var response=await provider.PostAsJsonAsync($"/api/v1/providers/me/reviews/{created.Id}/reply",new{BodyText="이용해 주셔서 감사합니다.",IdempotencyKey=Key()});response.EnsureSuccessStatusCode();Assert.Equal("수달 리뷰공급자",(await response.Content.ReadFromJsonAsync<ReviewReplyResponse>())!.ProviderName);}

    [Fact] public async Task ReviewCreation_DoesNotCreateServiceHistoryOrChangeTrust()
    {var data=await Prepare(true);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var provider=await db.ProviderProfiles.SingleAsync(x=>x.PublicId==data.ProviderId);var historyBefore=await db.ServiceHistoryEntries.CountAsync();var eventsBefore=await db.TrustScoreEvents.CountAsync();var scoreBefore=provider.TrustScore;var currentBefore=await db.ProviderTrustScoreCurrent.Where(x=>x.ProviderProfileId==provider.Id).Select(x=>x.Score).SingleOrDefaultAsync();using var customer=Client();await Login(customer,RoleCodes.Customer);(await Post(customer,data)).EnsureSuccessStatusCode();db.ChangeTracker.Clear();provider=await db.ProviderProfiles.SingleAsync(x=>x.PublicId==data.ProviderId);Assert.Equal(historyBefore,await db.ServiceHistoryEntries.CountAsync());Assert.Equal(eventsBefore,await db.TrustScoreEvents.CountAsync());Assert.Equal(scoreBefore,provider.TrustScore);Assert.Equal(currentBefore,await db.ProviderTrustScoreCurrent.Where(x=>x.ProviderProfileId==provider.Id).Select(x=>x.Score).SingleOrDefaultAsync());}

    private async Task<AttachedReview> CreateAttachedReview(string fileName)
    {
        var data=await Prepare(true);using var client=Client();await Login(client,RoleCodes.Customer);
        using var content=new MultipartFormDataContent();using var bytes=new ByteArrayContent([0xff,0xd8,0xff,0xd9]);bytes.Headers.ContentType=new MediaTypeHeaderValue("image/jpeg");content.Add(bytes,"file",fileName);
        var uploadResponse=await client.PostAsync("/api/v1/customers/me/review-files",content);uploadResponse.EnsureSuccessStatusCode();var upload=(await uploadResponse.Content.ReadFromJsonAsync<ReviewUploadResponse>())!;
        data=data with{FileId=upload.FileId};var response=await Post(client,data);response.EnsureSuccessStatusCode();var review=(await response.Content.ReadFromJsonAsync<ReviewResponse>())!;return new(data,review.Id,upload.FileId);
    }

    private async Task SetSafety(Guid fileId,string? malware,string? privacy,string? sanitization)
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var file=await db.Files.SingleAsync(x=>x.PublicId==fileId);file.MalwareScanStatusCode=malware;file.PrivacyInspectionStatusCode=privacy;file.SanitizationStatusCode=sanitization;await db.SaveChangesAsync();
    }

    private async Task<Guid> AddSanitizedDerivative(Guid originalId)
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var storage=scope.ServiceProvider.GetRequiredService<IPrivateFileStorage>();var original=await db.Files.SingleAsync(x=>x.PublicId==originalId);original.MalwareScanStatusCode="CLEAN";original.PrivacyInspectionStatusCode="SENSITIVE_DETECTED";original.SanitizationStatusCode="COMPLETED";
        var key=$"review/sanitized/{Guid.NewGuid():N}.jpg";var derivative=new StoredFile{PurposeCode="REVIEW",StorageContainer="development-private",StorageKey=key,StorageKeyHash=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)),OriginalFileName="sanitized-review-image.jpg",ContentType="image/jpeg",SizeBytes=4,Sha256Hex=new string('b',64),StatusCode="ACTIVE",MalwareScanStatusCode="CLEAN",PrivacyInspectionStatusCode="SAFE",ActivatedAt=DateTime.UtcNow,UploadedByUserId=original.UploadedByUserId,CreatedAt=DateTime.UtcNow};db.Files.Add(derivative);await db.SaveChangesAsync();db.FileDerivatives.Add(new StoredFileDerivative{OriginalFileId=original.Id,DerivedFileId=derivative.Id,DerivativeTypeCode=FilePrivacyCodes.PrivacySanitized,CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();await using var stream=new MemoryStream([0xff,0xd8,0xff,0xd9]);await storage.SaveAsync(key,stream,default);return derivative.PublicId;
    }

    private async Task<Guid> AddUnrelatedSafeFile()
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var user=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Customer].LoginId);var key=$"review/unrelated/{Guid.NewGuid():N}.jpg";var file=new StoredFile{PurposeCode="REVIEW",StorageContainer="development-private",StorageKey=key,StorageKeyHash=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)),OriginalFileName="unrelated.jpg",ContentType="image/jpeg",SizeBytes=4,Sha256Hex=new string('c',64),StatusCode="ACTIVE",MalwareScanStatusCode="CLEAN",PrivacyInspectionStatusCode="SAFE",ActivatedAt=DateTime.UtcNow,UploadedByUserId=user.Id,CreatedAt=DateTime.UtcNow};db.Files.Add(file);await db.SaveChangesAsync();return file.PublicId;
    }

    private async Task HideReview(Guid reviewId)
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var review=await db.Reviews.SingleAsync(x=>x.PublicId==reviewId);review.VisibilityStatusCode="HIDDEN";await db.SaveChangesAsync();
    }

    private async Task<TestData> Prepare(bool completed,bool withFile=false)
    {using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var customerUser=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Customer].LoginId);customerUser.Phone="01012345678";customerUser.Email="customer@sudal.example.kr";var customer=await db.CustomerProfiles.SingleAsync(x=>x.UserId==customerUser.Id);customer.DisplayName="김수달";var providerUser=await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Provider].LoginId);var provider=await db.ProviderProfiles.SingleAsync(x=>x.UserId==providerUser.Id);provider.BusinessName="수달 리뷰공급자";var item=await db.ReviewRatingItems.SingleOrDefaultAsync(x=>x.Code=="INTEGRATION_TEST_ITEM");if(item is null){item=new ReviewRatingItem{Code="INTEGRATION_TEST_ITEM",Name="통합테스트 평가항목",Description="Master별 범위 검증용",MinValue=1,MaxValue=7,DisplayOrder=1,IsRequired=true,IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.ReviewRatingItems.Add(item);await db.SaveChangesAsync();}var category=await db.ServiceCategories.SingleAsync(x=>x.PublicId==factory.Catalog.ServiceId);var policy=await db.CategoryPolicies.FirstAsync(x=>x.CategoryId==category.Id);var area=await db.AdministrativeAreas.FirstAsync();var now=DateTime.UtcNow;var request=new ServiceRequest{CustomerProfileId=customer.Id,CategoryId=category.Id,CategoryPolicyId=policy.Id,AdministrativeAreaId=area.Id,Title="고객 리뷰 검증용 생활서비스",StatusCode="ACCEPTED",PolicySnapshotJson="{}",CreatedAt=now,UpdatedAt=now};db.ServiceRequests.Add(request);await db.SaveChangesAsync();var quote=new Quote{ServiceRequestId=request.Id,ProviderProfileId=provider.Id,StatusCode="ACCEPTED",SubmittedAt=now,AcceptedAt=now,CreatedAt=now,UpdatedAt=now};db.Quotes.Add(quote);await db.SaveChangesAsync();var revision=new QuoteRevision{QuoteId=quote.Id,RevisionNo=1,Summary="서비스 완료",TotalAmount=50000,CurrencyCode="KRW",ValidUntil=now.AddDays(1),SubmittedAt=now,SubmittedByUserId=provider.UserId,IdempotencyKey=Key()};db.QuoteRevisions.Add(revision);await db.SaveChangesAsync();var transaction=new TransactionRecord{ServiceRequestId=request.Id,AcceptedQuoteRevisionId=revision.Id,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,CategoryId=category.Id,StatusCode=completed?"COMPLETED":"IN_PROGRESS",AgreedAmount=50000,CurrencyCode="KRW",QuoteSnapshotJson="{}",CategoryPolicySnapshotJson="{}",CompletionPolicySnapshotJson="{}",CompletedAt=completed?now:null,CreatedAt=now,UpdatedAt=now};db.Transactions.Add(transaction);await db.SaveChangesAsync();Guid? fileId=null;if(withFile){var file=new StoredFile{PurposeCode="REVIEW",StorageContainer="테스트",StorageKey=$"reviews/{Guid.NewGuid():N}.jpg",StorageKeyHash=System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray()),OriginalFileName="서비스완료사진.jpg",ContentType="image/jpeg",SizeBytes=100,Sha256Hex=new string('a',64),StatusCode="ACTIVE",ActivatedAt=now,UploadedByUserId=customerUser.Id,CreatedAt=now};db.Files.Add(file);await db.SaveChangesAsync();fileId=file.PublicId;}return new(transaction.PublicId,transaction.Id,provider.PublicId,item.PublicId,fileId);}
    private Task<HttpResponseMessage> Post(HttpClient client,TestData data,string? key=null)=>client.PostAsJsonAsync(Path(data.TransactionId),new{BodyText="실제 거래에 대한 검증 리뷰입니다.",Ratings=new[]{new{RatingItemId=data.RatingItemId,RatingValue=6m}},FileIds=data.FileId.HasValue?new[]{data.FileId.Value}:Array.Empty<Guid>(),IdempotencyKey=key??Key()});
    private static string Path(Guid transactionId)=>$"/api/v1/customers/me/transactions/{transactionId}/review";private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});private static string Key()=>$"review-{Guid.NewGuid():N}";private sealed record TestData(Guid TransactionId,long InternalTransactionId,Guid ProviderId,Guid RatingItemId,Guid? FileId);private sealed record AttachedReview(TestData Data,Guid ReviewId,Guid FileId);
}
