using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Reviews;

public sealed class ReviewService(
    SoodalLifeDbContext db,
    SoodalLife.Api.Features.Work.IPrivateFileStorage fileStorage,
    ICrossDomainFilePublicationResolver filePublication,
    TrustCalculationService trustCalculation,
    ILogger<ReviewService> logger)
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    public async Task<ReviewResponse> CreateAsync(ClaimsPrincipal principal,Guid transactionId,CreateReviewRequest input,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token); var key=Required(input.IdempotencyKey,"idempotencyKey",150);
        var existing=await db.Reviews.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==key,token);
        if(existing is not null){if(existing.CustomerProfileId!=identity.ProfileId||existing.TransactionId!=(await TransactionId(transactionId,token)))throw Conflict("REVIEW_IDEMPOTENCY_CONFLICT","이미 다른 리뷰에 사용된 요청키입니다.");return await Build(existing.Id,identity.ProfileId,false,token);}
        var transaction=await db.Transactions.SingleOrDefaultAsync(x=>x.PublicId==transactionId,token)??throw NotFound("REVIEW_TRANSACTION_NOT_FOUND","거래를 찾을 수 없습니다.");
        if(transaction.CustomerProfileId!=identity.ProfileId)throw Forbidden("REVIEW_TRANSACTION_FORBIDDEN","본인의 거래에만 리뷰를 작성할 수 있습니다.");
        if(transaction.StatusCode!="COMPLETED"||transaction.CompletedAt is null)throw Conflict("REVIEW_TRANSACTION_NOT_COMPLETED","완료된 거래에만 리뷰를 작성할 수 있습니다.");
        if(await db.Reviews.AnyAsync(x=>x.TransactionId==transaction.Id,token))throw Conflict("REVIEW_ALREADY_EXISTS","이 거래에는 이미 리뷰가 등록되어 있습니다.");
        var body=Required(input.BodyText,"bodyText",4000); var now=DateTime.UtcNow;
        var activeItems=await db.ReviewRatingItems.Where(x=>x.IsActive&&(x.EffectiveFrom==null||x.EffectiveFrom<=now)&&(x.EffectiveTo==null||x.EffectiveTo>now)).OrderBy(x=>x.DisplayOrder).ToListAsync(token);
        if(activeItems.Count==0)throw Conflict("REVIEW_RATING_ITEMS_NOT_CONFIGURED","현재 사용할 수 있는 평가항목이 없어 리뷰를 등록할 수 없습니다.");
        if(input.Ratings.GroupBy(x=>x.RatingItemId).Any(x=>x.Count()>1))throw Invalid("REVIEW_RATING_DUPLICATED","같은 평가항목을 중복 제출할 수 없습니다.","ratings");
        var ratingMap=input.Ratings.ToDictionary(x=>x.RatingItemId); var activeIds=activeItems.Select(x=>x.PublicId).ToHashSet();
        if(ratingMap.Keys.Any(x=>!activeIds.Contains(x)))throw Invalid("REVIEW_RATING_ITEM_INVALID","사용할 수 없는 평가항목이 포함되어 있습니다.","ratings");
        if(activeItems.Any(x=>x.IsRequired&&!ratingMap.ContainsKey(x.PublicId)))throw Invalid("REVIEW_REQUIRED_RATING_MISSING","필수 평가항목을 모두 입력해 주세요.","ratings");
        foreach(var item in activeItems.Where(x=>ratingMap.ContainsKey(x.PublicId))){var value=ratingMap[item.PublicId].RatingValue;if(value<item.MinValue||value>item.MaxValue)throw Invalid("REVIEW_RATING_OUT_OF_RANGE",$"{item.Name} 평점은 {item.MinValue}부터 {item.MaxValue}까지 입력할 수 있습니다.","ratings");}
        if(input.FileIds.Distinct().Count()!=input.FileIds.Count)throw Invalid("REVIEW_FILE_DUPLICATED","같은 파일을 중복 연결할 수 없습니다.","fileIds");
        var files=await db.Files.Where(x=>input.FileIds.Contains(x.PublicId)).ToListAsync(token);
        if(files.Count!=input.FileIds.Count||files.Any(x=>x.UploadedByUserId!=identity.UserId||x.PurposeCode!="REVIEW"||x.StatusCode!="ACTIVE")||await db.ReviewFiles.AnyAsync(x=>files.Select(f=>f.Id).Contains(x.FileId),token))throw Invalid("REVIEW_FILE_INVALID","본인이 등록한 사용 가능한 리뷰 파일만 연결할 수 있습니다.","fileIds");
        IDbContextTransaction? databaseTransaction=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(token):null;
        try
        {
            var review=new Review{TransactionId=transaction.Id,CustomerProfileId=transaction.CustomerProfileId,ProviderProfileId=transaction.ProviderProfileId,BodyText=body,OverallRating=null,VerificationStatusCode="VERIFIED_TRANSACTION",VisibilityStatusCode="PUBLIC",IdempotencyKey=key,SubmittedAt=now,PublishedAt=now,CreatedAt=now,UpdatedAt=now};
            db.Reviews.Add(review);await db.SaveChangesAsync(token);
            db.ReviewRatings.AddRange(activeItems.Where(x=>ratingMap.ContainsKey(x.PublicId)).Select(x=>new ReviewRating{ReviewId=review.Id,RatingItemId=x.Id,RatingValue=ratingMap[x.PublicId].RatingValue,DisplayOrder=x.DisplayOrder,CreatedAt=now}));
            db.ReviewFiles.AddRange(files.Select((x,index)=>new ReviewFile{ReviewId=review.Id,FileId=x.Id,DisplayOrder=index+1,CreatedAt=now}));
            db.OutboxEvents.Add(new OutboxEvent{AggregateType="Review",AggregatePublicId=review.PublicId,EventType="REVIEW_CREATED",PayloadJson=JsonSerializer.Serialize(new{reviewId=review.PublicId,transactionId}),StatusCode="PENDING",OccurredAt=now,AvailableAt=now,IdempotencyKey=$"review-created:{review.PublicId:N}",CreatedByUserId=identity.UserId});
            await db.SaveChangesAsync(token);if(databaseTransaction is not null)await databaseTransaction.CommitAsync(token);
            try{var providerId=await db.ProviderProfiles.Where(x=>x.Id==review.ProviderProfileId).Select(x=>x.PublicId).SingleAsync(token);await trustCalculation.CalculateActiveAsync(providerId,"REVIEW_CREATED",review.PublicId,$"trust-review:{review.PublicId:N}",null,token);}
            catch(TrustCalculationException exception) when(exception.BusinessCode=="ACTIVE_TRUST_POLICY_NOT_FOUND"){logger.LogInformation("Review {ReviewId} was saved before an active trust policy was available.",review.PublicId);}
            catch(Exception exception){logger.LogError(exception,"Immediate trust recalculation failed after review {ReviewId}; the review remains saved.",review.PublicId);}
            return await Build(review.Id,identity.ProfileId,false,token);
        }
        catch{if(databaseTransaction is not null)await databaseTransaction.RollbackAsync(token);throw;}finally{if(databaseTransaction is not null)await databaseTransaction.DisposeAsync();}
    }

    public async Task<IReadOnlyList<ReviewRatingItemOption>> RatingItems(ClaimsPrincipal principal,CancellationToken token)
    {
        _=await CustomerIdentity(principal,token);var now=DateTime.UtcNow;
        return await db.ReviewRatingItems.AsNoTracking().Where(x=>x.IsActive&&(x.EffectiveFrom==null||x.EffectiveFrom<=now)&&(x.EffectiveTo==null||x.EffectiveTo>now))
            .OrderBy(x=>x.DisplayOrder).Select(x=>new ReviewRatingItemOption(x.PublicId,x.Code,x.Name,x.Description,x.MinValue,x.MaxValue,x.IsRequired,x.DisplayOrder)).ToListAsync(token);
    }

    public async Task<ReviewResponse> UpdateAsync(ClaimsPrincipal principal,Guid reviewId,UpdateReviewRequest input,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token);var key=Required(input.IdempotencyKey,"idempotencyKey",150);
        var review=await db.Reviews.SingleOrDefaultAsync(x=>x.PublicId==reviewId&&x.CustomerProfileId==identity.ProfileId,token)??throw NotFound("REVIEW_NOT_FOUND","리뷰를 찾을 수 없습니다.");
        var eventKey=$"review-updated:{review.PublicId:N}:{key}";
        if(await db.OutboxEvents.AsNoTracking().AnyAsync(x=>x.IdempotencyKey==eventKey,token))return await Build(review.Id,identity.ProfileId,false,token);
        var body=Required(input.BodyText,"bodyText",4000);var now=DateTime.UtcNow;
        var activeItems=await db.ReviewRatingItems.Where(x=>x.IsActive&&(x.EffectiveFrom==null||x.EffectiveFrom<=now)&&(x.EffectiveTo==null||x.EffectiveTo>now)).OrderBy(x=>x.DisplayOrder).ToListAsync(token);
        if(input.Ratings.GroupBy(x=>x.RatingItemId).Any(x=>x.Count()>1))throw Invalid("REVIEW_RATING_DUPLICATED","같은 평가항목을 중복 제출할 수 없습니다.","ratings");
        var ratingMap=input.Ratings.ToDictionary(x=>x.RatingItemId);var activeIds=activeItems.Select(x=>x.PublicId).ToHashSet();
        if(ratingMap.Keys.Any(x=>!activeIds.Contains(x)))throw Invalid("REVIEW_RATING_ITEM_INVALID","사용할 수 없는 평가항목이 포함되어 있습니다.","ratings");
        if(activeItems.Any(x=>x.IsRequired&&!ratingMap.ContainsKey(x.PublicId)))throw Invalid("REVIEW_REQUIRED_RATING_MISSING","필수 평가항목을 모두 입력해 주세요.","ratings");
        foreach(var item in activeItems.Where(x=>ratingMap.ContainsKey(x.PublicId))){var value=ratingMap[item.PublicId].RatingValue;if(value<item.MinValue||value>item.MaxValue)throw Invalid("REVIEW_RATING_OUT_OF_RANGE",$"{item.Name} 평점은 {item.MinValue}부터 {item.MaxValue}까지 입력할 수 있습니다.","ratings");}
        if(input.FileIds.Distinct().Count()!=input.FileIds.Count)throw Invalid("REVIEW_FILE_DUPLICATED","같은 파일을 중복 연결할 수 없습니다.","fileIds");
        var files=await db.Files.Where(x=>input.FileIds.Contains(x.PublicId)).ToListAsync(token);
        if(files.Count!=input.FileIds.Count||files.Any(x=>x.UploadedByUserId!=identity.UserId||x.PurposeCode!="REVIEW"||x.StatusCode!="ACTIVE")||await db.ReviewFiles.AnyAsync(x=>files.Select(f=>f.Id).Contains(x.FileId)&&x.ReviewId!=review.Id,token))throw Invalid("REVIEW_FILE_INVALID","본인이 등록한 사용 가능한 리뷰 파일만 연결할 수 있습니다.","fileIds");
        IDbContextTransaction? databaseTransaction=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(token):null;
        try
        {
            db.ReviewRatings.RemoveRange(await db.ReviewRatings.Where(x=>x.ReviewId==review.Id).ToListAsync(token));
            db.ReviewFiles.RemoveRange(await db.ReviewFiles.Where(x=>x.ReviewId==review.Id).ToListAsync(token));
            await db.SaveChangesAsync(token);
            review.BodyText=body;review.OverallRating=null;review.UpdatedAt=now;
            db.ReviewRatings.AddRange(activeItems.Where(x=>ratingMap.ContainsKey(x.PublicId)).Select(x=>new ReviewRating{ReviewId=review.Id,RatingItemId=x.Id,RatingValue=ratingMap[x.PublicId].RatingValue,DisplayOrder=x.DisplayOrder,CreatedAt=now}));
            db.ReviewFiles.AddRange(files.Select((x,index)=>new ReviewFile{ReviewId=review.Id,FileId=x.Id,DisplayOrder=index+1,CreatedAt=now}));
            db.OutboxEvents.Add(new OutboxEvent{AggregateType="Review",AggregatePublicId=review.PublicId,EventType="REVIEW_UPDATED",PayloadJson=JsonSerializer.Serialize(new{reviewId=review.PublicId,review.TransactionId}),StatusCode="PENDING",OccurredAt=now,AvailableAt=now,IdempotencyKey=eventKey,CreatedByUserId=identity.UserId});
            await db.SaveChangesAsync(token);if(databaseTransaction is not null)await databaseTransaction.CommitAsync(token);
            try{var providerId=await db.ProviderProfiles.Where(x=>x.Id==review.ProviderProfileId).Select(x=>x.PublicId).SingleAsync(token);await trustCalculation.CalculateActiveAsync(providerId,"REVIEW_UPDATED",review.PublicId,$"trust-review-update:{review.PublicId:N}:{key}",null,token);}catch(Exception exception){logger.LogError(exception,"Immediate trust recalculation failed after review update {ReviewId}; the review remains saved.",review.PublicId);}
            return await Build(review.Id,identity.ProfileId,false,token);
        }
        catch{if(databaseTransaction is not null)await databaseTransaction.RollbackAsync(token);throw;}finally{if(databaseTransaction is not null)await databaseTransaction.DisposeAsync();}
    }

    public async Task<IReadOnlyList<ReviewResponse>> Mine(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token);var ids=await db.Reviews.AsNoTracking().Where(x=>x.CustomerProfileId==identity.ProfileId).OrderByDescending(x=>x.SubmittedAt).Select(x=>x.Id).ToListAsync(token);
        var result=new List<ReviewResponse>();foreach(var id in ids)result.Add(await Build(id,identity.ProfileId,false,token));return result;
    }

    public async Task<ReviewResponse> MineDetail(ClaimsPrincipal principal,Guid reviewId,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token);var id=await db.Reviews.AsNoTracking().Where(x=>x.PublicId==reviewId&&x.CustomerProfileId==identity.ProfileId).Select(x=>x.Id).SingleOrDefaultAsync(token);
        if(id==0)throw NotFound("REVIEW_NOT_FOUND","리뷰를 찾을 수 없습니다.");return await Build(id,identity.ProfileId,false,token);
    }

    public async Task<ReviewUploadResponse> Upload(ClaimsPrincipal principal,IFormFile upload,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token);if(upload.Length<=0||upload.Length>MaximumFileSize)throw Invalid("REVIEW_FILE_SIZE_INVALID","사진은 10MB 이하여야 합니다.","file");
        var rules=new Dictionary<string,(string Ext,byte[] Signature)>(StringComparer.OrdinalIgnoreCase){{"image/jpeg",(".jpg",[0xff,0xd8,0xff])},{"image/png",(".png",[0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a])},{"image/webp",(".webp",Encoding.ASCII.GetBytes("RIFF"))}};
        if(!rules.TryGetValue(upload.ContentType,out var rule))throw Invalid("REVIEW_FILE_TYPE_INVALID","JPEG, PNG, WebP 사진만 첨부할 수 있습니다.","file");
        var original=Path.GetFileName(upload.FileName);if(string.IsNullOrWhiteSpace(original)||original!=upload.FileName||original.Length>255)throw Invalid("REVIEW_FILE_NAME_INVALID","안전한 파일명을 사용해 주세요.","file");
        await using var source=upload.OpenReadStream();using var memory=new MemoryStream();await source.CopyToAsync(memory,token);var bytes=memory.ToArray();
        var valid=bytes.AsSpan().StartsWith(rule.Signature);if(upload.ContentType.Equals("image/webp",StringComparison.OrdinalIgnoreCase))valid&=bytes.Length>=12&&bytes.AsSpan(8,4).SequenceEqual(Encoding.ASCII.GetBytes("WEBP"));
        if(!valid)throw Invalid("REVIEW_FILE_SIGNATURE_INVALID","파일 내용과 이미지 형식이 일치하지 않습니다.","file");
        var now=DateTime.UtcNow;var key=$"review/{Guid.NewGuid():N}{rule.Ext}";var file=new StoredFile{PurposeCode="REVIEW",StorageContainer="development-private",StorageKey=key,StorageKeyHash=SHA256.HashData(Encoding.UTF8.GetBytes(key)),OriginalFileName=original,ContentType=upload.ContentType.ToLowerInvariant(),SizeBytes=bytes.Length,Sha256Hex=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),StatusCode="PENDING",MalwareScanStatusCode=FilePrivacyCodes.Clean,PrivacyInspectionStatusCode=FilePrivacyCodes.Safe,PrivacyInspectedAt=now,PrivacyAdapterVersion="server-image-normalizer-v1",PrivacyDetectionTypesJson="[]",SanitizationStatusCode=FilePrivacyCodes.SanitizationCompleted,SanitizationCompletedAt=now,UploadedByUserId=identity.UserId,CreatedAt=now};
        db.Files.Add(file);await db.SaveChangesAsync(token);try{await using var content=new MemoryStream(bytes);await fileStorage.SaveAsync(key,content,token);file.StatusCode="ACTIVE";file.ActivatedAt=now;file.ScanResultText="SERVER_NORMALIZED";await db.SaveChangesAsync(token);return new(file.PublicId,file.OriginalFileName,file.ContentType,file.SizeBytes,"SAFE");}catch{await fileStorage.DeleteIfExistsAsync(key,token);throw;}
    }

    public async Task<(Stream Stream,string ContentType,string FileName)> OpenFile(ClaimsPrincipal principal,Guid fileId,CancellationToken token)
    {
        var identity=await CustomerIdentity(principal,token);var file=await db.Files.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==fileId&&x.StatusCode=="ACTIVE"&&x.PurposeCode=="REVIEW"&&x.UploadedByUserId==identity.UserId,token)??throw NotFound("REVIEW_FILE_NOT_FOUND","파일을 찾을 수 없습니다.");
        return(await fileStorage.OpenReadAsync(file.StorageKey,token),file.ContentType,file.OriginalFileName);
    }

    public async Task<(Stream Stream,string ContentType,string FileName)> OpenPublicFile(
        Guid providerId,Guid reviewId,Guid fileId,CancellationToken token)
    {
        var originals=await(
            from review in db.Reviews.AsNoTracking()
            join provider in db.ProviderProfiles.AsNoTracking() on review.ProviderProfileId equals provider.Id
            join link in db.ReviewFiles.AsNoTracking() on review.Id equals link.ReviewId
            join file in db.Files.AsNoTracking() on link.FileId equals file.Id
            where provider.PublicId==providerId&&review.PublicId==reviewId&&file.PurposeCode=="REVIEW"&&
                  review.VisibilityStatusCode=="PUBLIC"&&review.VerificationStatusCode=="VERIFIED_TRANSACTION"
            select file).ToListAsync(token);
        foreach(var original in originals)
        {
            var publication=await filePublication.ResolveAsync(original,null,true,true,token);
            if(!publication.Allowed||publication.PublishedFile?.PublicId!=fileId)continue;
            var published=publication.PublishedFile!;
            return(await fileStorage.OpenReadAsync(published.StorageKey,token),published.ContentType,PublicFileName(published.ContentType));
        }
        throw NotFound("REVIEW_FILE_NOT_FOUND","공개 가능한 리뷰 첨부파일을 찾을 수 없습니다.");
    }

    public async Task<PublicReviewListResponse> PublicList(Guid providerId,int page,int pageSize,CancellationToken token)
    {
        if(page<1||pageSize is <1 or >100)throw Invalid("REVIEW_PAGE_INVALID","페이지 정보를 확인해 주세요.");
        var provider=await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==providerId,token)??throw NotFound("REVIEW_PROVIDER_NOT_FOUND","전문가를 찾을 수 없습니다.");
        var query=db.Reviews.AsNoTracking().Where(x=>x.ProviderProfileId==provider.Id&&x.VisibilityStatusCode=="PUBLIC"&&x.VerificationStatusCode=="VERIFIED_TRANSACTION");var total=await query.CountAsync(token);
        var ids=await query.OrderByDescending(x=>x.SubmittedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>x.Id).ToListAsync(token);var items=new List<ReviewResponse>();foreach(var id in ids)items.Add(await Build(id,null,true,token));return new(total,page,pageSize,items);
    }

    public async Task<ProviderReviewStatisticsResponse> Statistics(Guid providerId,CancellationToken token)
    {
        var provider=await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==providerId,token)??throw NotFound("REVIEW_PROVIDER_NOT_FOUND","전문가를 찾을 수 없습니다.");
        var total=await db.Reviews.CountAsync(x=>x.ProviderProfileId==provider.Id,token);var publicCount=await db.Reviews.CountAsync(x=>x.ProviderProfileId==provider.Id&&x.VisibilityStatusCode=="PUBLIC"&&x.VerificationStatusCode=="VERIFIED_TRANSACTION",token);
        var averages=await(from rating in db.ReviewRatings.AsNoTracking() join review in db.Reviews.AsNoTracking() on rating.ReviewId equals review.Id join item in db.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals item.Id where review.ProviderProfileId==provider.Id&&review.VisibilityStatusCode=="PUBLIC"&&review.VerificationStatusCode=="VERIFIED_TRANSACTION" group rating by new{item.PublicId,item.Code,item.Name,item.MinValue,item.MaxValue,item.DisplayOrder} into values orderby values.Key.DisplayOrder select new RatingItemAverageResponse(values.Key.PublicId,values.Key.Code,values.Key.Name,values.Average(x=>x.RatingValue),values.Count(),values.Key.MinValue,values.Key.MaxValue)).ToListAsync(token);
        return new(provider.PublicId,total,publicCount,averages);
    }

    public async Task<ReviewReplyResponse> Reply(ClaimsPrincipal principal,Guid reviewId,CreateProviderReplyRequest input,CancellationToken token)
    {
        var identity=await ProviderIdentity(principal,token);var key=Required(input.IdempotencyKey,"idempotencyKey",150);var existing=await db.ReviewProviderReplies.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==key,token);if(existing is not null)return await ReplyResponse(existing,token);
        var review=await db.Reviews.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==reviewId,token)??throw NotFound("REVIEW_NOT_FOUND","리뷰를 찾을 수 없습니다.");if(review.ProviderProfileId!=identity.ProfileId)throw NotFound("REVIEW_NOT_FOUND","리뷰를 찾을 수 없습니다.");if(await db.ReviewProviderReplies.AnyAsync(x=>x.ReviewId==review.Id,token))throw Conflict("REVIEW_REPLY_EXISTS","이미 답글이 등록되어 있습니다.");var now=DateTime.UtcNow;var reply=new ReviewProviderReply{ReviewId=review.Id,ProviderProfileId=identity.ProfileId,BodyText=Required(input.BodyText,"bodyText",2000),IdempotencyKey=key,SubmittedAt=now,CreatedAt=now,UpdatedAt=now};db.ReviewProviderReplies.Add(reply);await db.SaveChangesAsync(token);return await ReplyResponse(reply,token);
    }

    private async Task<ReviewResponse> Build(long reviewId,long? ownerCustomerId,bool publicView,CancellationToken token)
    {
        var row=await(from review in db.Reviews.AsNoTracking() join transaction in db.Transactions.AsNoTracking() on review.TransactionId equals transaction.Id join provider in db.ProviderProfiles.AsNoTracking() on review.ProviderProfileId equals provider.Id join customer in db.CustomerProfiles.AsNoTracking() on review.CustomerProfileId equals customer.Id where review.Id==reviewId select new{review,transaction,provider,customer}).SingleAsync(token);
        if(ownerCustomerId.HasValue&&row.review.CustomerProfileId!=ownerCustomerId)throw Forbidden("REVIEW_FORBIDDEN","리뷰를 조회할 수 없습니다.");
        var ratings=await(from rating in db.ReviewRatings.AsNoTracking() join item in db.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals item.Id where rating.ReviewId==reviewId orderby rating.DisplayOrder select new ReviewRatingResponse(item.PublicId,item.Code,item.Name,rating.RatingValue,item.MinValue,item.MaxValue,rating.DisplayOrder)).ToListAsync(token);
        var fileRows=await(from link in db.ReviewFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.ReviewId==reviewId orderby link.DisplayOrder select new{link.DisplayOrder,File=file}).ToListAsync(token);
        var files=new List<ReviewFileResponse>(fileRows.Count);
        foreach(var item in fileRows)
        {
            if(!publicView)
            {
                files.Add(new(item.File.PublicId,item.File.OriginalFileName,item.File.ContentType,item.DisplayOrder,$"/api/v1/customers/me/review-files/{item.File.PublicId}","OWNER_ORIGINAL"));
                continue;
            }
            var publication=await filePublication.ResolveAsync(item.File,null,true,true,token);
            if(!publication.Allowed||publication.PublishedFile is null)continue;
            var published=publication.PublishedFile;
            files.Add(new(published.PublicId,PublicFileName(published.ContentType),published.ContentType,item.DisplayOrder,
                $"/api/v1/providers/{row.provider.PublicId}/reviews/{row.review.PublicId}/files/{published.PublicId}",publication.PublicationMode));
        }
        var reply=await db.ReviewProviderReplies.AsNoTracking().SingleOrDefaultAsync(x=>x.ReviewId==reviewId,token);return new(row.review.PublicId,row.transaction.PublicId,$"TR-{row.transaction.Id:D8}",row.provider.PublicId,row.provider.BusinessName,publicView?MaskName(row.customer.DisplayName):row.customer.DisplayName,row.review.BodyText,row.review.OverallRating,row.review.VerificationStatusCode,row.review.VisibilityStatusCode,row.review.SubmittedAt,ratings,files,reply is null?null:await ReplyResponse(reply,token));
    }
    private async Task<ReviewReplyResponse> ReplyResponse(ReviewProviderReply reply,CancellationToken token){var name=await db.ProviderProfiles.Where(x=>x.Id==reply.ProviderProfileId).Select(x=>x.BusinessName).SingleAsync(token);return new(reply.PublicId,name,reply.BodyText,reply.SubmittedAt);}
    private static string PublicFileName(string contentType)=>contentType.StartsWith("image/",StringComparison.OrdinalIgnoreCase)?"리뷰 첨부 이미지":"리뷰 첨부 파일";
    private async Task<(long UserId,long ProfileId)> CustomerIdentity(ClaimsPrincipal p,CancellationToken t){var id=Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);return await(from u in db.Users where u.PublicId==id join c in db.CustomerProfiles on u.Id equals c.UserId select new ValueTuple<long,long>(u.Id,c.Id)).SingleOrDefaultAsync(t) is var x&&x.Item1!=0?x:throw Forbidden("CUSTOMER_PROFILE_REQUIRED","고객 프로필이 필요합니다.");}
    private async Task<(long UserId,long ProfileId)> ProviderIdentity(ClaimsPrincipal p,CancellationToken t){var id=Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);return await(from u in db.Users where u.PublicId==id join c in db.ProviderProfiles on u.Id equals c.UserId select new ValueTuple<long,long>(u.Id,c.Id)).SingleOrDefaultAsync(t) is var x&&x.Item1!=0?x:throw Forbidden("PROVIDER_PROFILE_REQUIRED","전문가 프로필이 필요합니다.");}
    private Task<long> TransactionId(Guid id,CancellationToken t)=>db.Transactions.Where(x=>x.PublicId==id).Select(x=>x.Id).SingleOrDefaultAsync(t);
    private static string Required(string? value,string field,int max=4000){var result=value?.Trim();if(string.IsNullOrWhiteSpace(result)||result.Length>max)throw Invalid("REVIEW_INPUT_INVALID",$"{field} 값을 확인해 주세요.",field);return result;}
    private static string MaskName(string value)=>string.IsNullOrWhiteSpace(value)?"고객":value.Length==1?$"{value}*":$"{value[0]}*{value[^1]}";
    private static ReviewBusinessException Invalid(string c,string m,string? f=null)=>new(400,c,m,f);private static ReviewBusinessException Forbidden(string c,string m)=>new(403,c,m);private static ReviewBusinessException NotFound(string c,string m)=>new(404,c,m);private static ReviewBusinessException Conflict(string c,string m)=>new(409,c,m);
}
