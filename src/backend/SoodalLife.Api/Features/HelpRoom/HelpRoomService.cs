using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.HelpRoom;

public sealed class HelpRoomService(SoodalLifeDbContext db, CustomerServiceRequestService requests, IPrivateFileStorage storage, ICrossDomainFilePublicationResolver publication)
{
    private static readonly string[] DangerWords=["누전","분전반","감전","가스 누출","가스배관","구조벽","철거","붕괴","화재","고압","고소 작업","석면"];
    private static readonly string[] QuoteWords=["견적","비용","가격","얼마","방문","와주세요","수리해","공사","업체","예약"];

    public async Task<IReadOnlyList<HelpPostListItem>> List(ClaimsPrincipal principal,string? category,string? mode,CancellationToken token)
    {
        var user=await User(principal,token);var providerMode=string.Equals(mode,"provider",StringComparison.OrdinalIgnoreCase);long? providerId=null;
        var query=db.HelpPosts.AsNoTracking().Where(x=>x.StatusCode!="HIDDEN");
        if(providerMode)
        {
            var provider=await Provider(principal,token);providerId=provider.ProviderId;
            query=query.Where(post=>post.StatusCode=="PUBLISHED"&&post.CustomerUserId!=user.Id&&
                db.ProviderServiceCategories.Any(service=>service.ProviderProfileId==provider.ProviderId&&service.CategoryId==post.CategoryId&&service.StatusCode=="ACTIVE"&&
                    db.ProviderServiceApprovals.Any(approval=>approval.ProviderServiceCategoryId==service.Id&&approval.ApprovalStatusCode=="APPROVED")));
        }
        if(Guid.TryParse(category,out var categoryId))query=from post in query join cat in db.ServiceCategories on post.CategoryId equals cat.Id where cat.PublicId==categoryId select post;
        var rows=await(from post in query join cat in db.ServiceCategories on post.CategoryId equals cat.Id join area0 in db.AdministrativeAreas on post.AdministrativeAreaId equals area0.Id into areas from area in areas.DefaultIfEmpty()
            join parent0 in db.AdministrativeAreas on area!.ParentAreaId equals (long?)parent0.Id into parents from parent in parents.DefaultIfEmpty()
            orderby post.CreatedAt descending select new{post.PublicId,post.Title,CategoryId=cat.PublicId,CategoryName=cat.Name,RegionName=post.RegionDisclosureCode=="SIGUNGU"?(parent==null?area!.AreaName:parent.AreaName+" "+area!.AreaName):null,post.StatusCode,post.IntentCode,post.CreatedAt,AnswerCount=db.HelpRoomEntries.Count(x=>x.HelpPostId==post.Id&&x.AuthorRoleCode=="PROVIDER"&&x.StatusCode=="PUBLISHED"),post.CustomerUserId,
                LastProviderAdviceAt=providerId.HasValue?db.HelpRoomEntries.Where(x=>x.HelpPostId==post.Id&&x.ProviderProfileId==providerId&&x.AuthorRoleCode=="PROVIDER"&&x.StatusCode=="PUBLISHED").Max(x=>(DateTime?)x.CreatedAt):null,
                LatestCustomerQuestionAt=providerId.HasValue?db.HelpRoomEntries.Where(x=>x.HelpPostId==post.Id&&x.AuthorRoleCode=="CUSTOMER"&&x.EntryTypeCode=="CUSTOMER_FOLLOW_UP"&&x.StatusCode=="PUBLISHED").Max(x=>(DateTime?)x.CreatedAt):null}).Take(100).ToListAsync(token);
        return rows.Select(x=>new HelpPostListItem(x.PublicId,x.Title,x.CategoryId,x.CategoryName,x.RegionName,x.StatusCode,x.IntentCode,x.CreatedAt,x.AnswerCount,Alias(x.CustomerUserId),providerMode?ProviderAction(x.LastProviderAdviceAt,x.LatestCustomerQuestionAt):null))
            .OrderBy(x=>x.ProviderAction switch{"ANSWERABLE"=>0,"FOLLOW_UP_AVAILABLE"=>1,"WAITING_CUSTOMER"=>2,_=>3}).ThenByDescending(x=>x.CreatedAt).ToList();
    }

    public async Task<HelpPostDetail> Detail(ClaimsPrincipal principal,Guid id,CancellationToken token)
    {
        var actor=await User(principal,token);var post=await db.HelpPosts.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==id,token)??throw Error("HELP_POST_NOT_FOUND","도움방 글을 찾을 수 없습니다.",404);
        if(post.StatusCode=="HIDDEN"&&post.CustomerUserId!=actor.Id&&!actor.IsAdmin)throw Error("HELP_POST_NOT_FOUND","도움방 글을 찾을 수 없습니다.",404);
        var cat=await db.ServiceCategories.AsNoTracking().SingleAsync(x=>x.Id==post.CategoryId,token);var region=post.RegionDisclosureCode=="SIGUNGU"&&post.AdministrativeAreaId.HasValue?await(from area in db.AdministrativeAreas.AsNoTracking() join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)parent0.Id into parents from parent in parents.DefaultIfEmpty() where area.Id==post.AdministrativeAreaId select parent==null?area.AreaName:parent.AreaName+" "+area.AreaName).SingleOrDefaultAsync(token):null;
        var entryRows=await db.HelpRoomEntries.AsNoTracking().Where(x=>x.HelpPostId==post.Id&&x.StatusCode=="PUBLISHED").OrderBy(x=>x.CreatedAt).ToListAsync(token);
        var entries=entryRows.Select(x=>new HelpEntryResponse(x.PublicId,x.AuthorRoleCode,x.AuthorRoleCode=="PROVIDER"?"승인 전문가":Alias(x.AuthorUserId),x.Body,x.CauseText,x.CheckText,x.DiyStepsText,x.RiskText,x.NextStepText,x.RequiresProfessional,x.SafetyCode,x.CreatedAt)).ToList();
        var resolution=await db.HelpPostResolutions.AsNoTracking().Where(x=>x.HelpPostId==post.Id).Select(x=>new HelpResolutionResponse(x.ResolutionCode,x.Summary,x.CreatedAt)).SingleOrDefaultAsync(token);
        var files=await(from link in db.HelpPostFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.HelpPostId==post.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new HelpFileResponse(file.PublicId,file.OriginalFileName,file.ContentType,file.SizeBytes,$"/api/v1/help-room/{post.PublicId}/files/{file.PublicId}",file.MalwareScanStatusCode==FilePrivacyCodes.Clean&&file.PrivacyInspectionStatusCode==FilePrivacyCodes.Safe)).ToListAsync(token);
        string? providerAction=null;
        if(principal.IsInRole("PROVIDER")&&post.StatusCode=="PUBLISHED"&&post.CustomerUserId!=actor.Id)
        {
            var providerId=await db.ProviderProfiles.AsNoTracking().Where(x=>x.UserId==actor.Id&&x.ApprovalStatusCode=="APPROVED"&&x.ActivityStatusCode=="ACTIVE").Select(x=>(long?)x.Id).SingleOrDefaultAsync(token);
            if(providerId.HasValue&&await(from service in db.ProviderServiceCategories.AsNoTracking() join approval in db.ProviderServiceApprovals.AsNoTracking() on service.Id equals approval.ProviderServiceCategoryId where service.ProviderProfileId==providerId&&service.CategoryId==post.CategoryId&&service.StatusCode=="ACTIVE"&&approval.ApprovalStatusCode=="APPROVED" select service.Id).AnyAsync(token))
            {
                var lastAdvice=entryRows.Where(x=>x.ProviderProfileId==providerId&&x.AuthorRoleCode=="PROVIDER").Select(x=>(DateTime?)x.CreatedAt).Max();
                var latestQuestion=entryRows.Where(x=>x.AuthorRoleCode=="CUSTOMER"&&x.EntryTypeCode=="CUSTOMER_FOLLOW_UP").Select(x=>(DateTime?)x.CreatedAt).Max();
                providerAction=ProviderAction(lastAdvice,latestQuestion);
            }
        }
        return new(post.PublicId,post.Title,post.Body,cat.PublicId,cat.Name,region,post.RegionDisclosureCode,post.PurposeCode,post.StatusCode,post.IntentCode,post.IntentReason,post.ConvertedServiceRequestId.HasValue,post.CustomerUserId==actor.Id,Alias(post.CustomerUserId),post.CreatedAt,entries,files,resolution,providerAction);
    }

    public async Task<HelpPostDetail> Create(ClaimsPrincipal principal,CreateHelpPostInput input,CancellationToken token)
    {
        var actor=await Customer(principal,token);var category=await db.ServiceCategories.SingleOrDefaultAsync(x=>x.PublicId==input.CategoryId&&x.StatusCode=="ACTIVE"&&x.LevelCode=="SERVICE",token)??throw Error("HELP_CATEGORY_INVALID","사용 가능한 하위 서비스 카테고리를 선택해 주세요.");
        long? area=null;if(input.RevealRegion){if(!input.AdministrativeAreaId.HasValue)throw Error("HELP_REGION_REQUIRED","공개할 시·군·구를 선택해 주세요.");area=await db.AdministrativeAreas.Where(x=>x.PublicId==input.AdministrativeAreaId&&x.IsActive&&x.AreaLevelCode=="SIGUNGU").Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw Error("HELP_REGION_INVALID","사용 가능한 시·군·구를 선택해 주세요.");}
        var title=Required(input.Title,5,200);var body=Required(input.Body,10,5000);var intent=Intent(title+" "+body);var now=DateTime.UtcNow;
        var post=new HelpPost{CustomerUserId=actor.Id,CategoryId=category.Id,AdministrativeAreaId=area,RegionDisclosureCode=input.RevealRegion?"SIGUNGU":"HIDDEN",PurposeCode=Allowed(input.PurposeCode,["DIAGNOSIS","DIY","MATERIALS","PROFESSIONAL_DECISION","INTERIOR_IDEA"],"DIAGNOSIS"),Title=title,Body=body,StatusCode="PUBLISHED",IntentCode=intent.Code,IntentReason=intent.Reason,CreatedAt=now,CreatedByUserId=actor.Id,UpdatedAt=now,UpdatedByUserId=actor.Id};db.HelpPosts.Add(post);await db.SaveChangesAsync(token);return await Detail(principal,post.PublicId,token);
    }

    public async Task<HelpPostDetail> FollowUp(ClaimsPrincipal principal,Guid id,AddHelpEntryInput input,CancellationToken token)
    {var actor=await Customer(principal,token);var post=await Owned(id,actor.Id,token);EnsureOpen(post);var now=DateTime.UtcNow;db.HelpRoomEntries.Add(new HelpRoomEntry{HelpPostId=post.Id,AuthorUserId=actor.Id,AuthorRoleCode="CUSTOMER",EntryTypeCode="CUSTOMER_FOLLOW_UP",Body=Required(input.Body,2,5000),SafetyCode="NORMAL",StatusCode="PUBLISHED",CreatedAt=now,CreatedByUserId=actor.Id});await db.SaveChangesAsync(token);return await Detail(principal,id,token);}

    public async Task<HelpFileResponse> Upload(ClaimsPrincipal principal,Guid id,IFormFile upload,CancellationToken token)
    {
        var actor=await Customer(principal,token);var post=await Owned(id,actor.Id,token);EnsureOpen(post);
        await using var transaction=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token):null;
        if(await db.HelpPostFiles.CountAsync(x=>x.HelpPostId==post.Id,token)>=8)throw Error("HELP_FILE_LIMIT","사진은 글당 최대 8장까지 등록할 수 있습니다.");
        var validated=await Validate(upload,token);
        if(await(from link in db.HelpPostFiles join existing in db.Files on link.FileId equals existing.Id where link.HelpPostId==post.Id&&existing.Sha256Hex==validated.Sha256Hex select link.Id).AnyAsync(token))throw Error("HELP_FILE_DUPLICATE","같은 사진은 다시 등록할 수 없습니다.",409);
        var now=DateTime.UtcNow;var key=$"help-room/{post.PublicId:N}/safe/{Guid.NewGuid():N}{validated.Extension}";var order=await db.HelpPostFiles.CountAsync(x=>x.HelpPostId==post.Id,token);var publicName=$"help-photo-{order+1}{validated.Extension}";
        var file=new StoredFile{PurposeCode="HELP_ROOM_PHOTO",StorageContainer="private",StorageKey=key,StorageKeyHash=SHA256.HashData(Encoding.UTF8.GetBytes(key)),OriginalFileName=publicName,ContentType=validated.ContentType,SizeBytes=validated.Bytes.Length,Sha256Hex=validated.Sha256Hex,StatusCode="PENDING",MalwareScanStatusCode=FilePrivacyCodes.Clean,PrivacyInspectionStatusCode=FilePrivacyCodes.Safe,PrivacyInspectedAt=now,PrivacyAdapterVersion="LOCAL_IMAGE_SAFETY_V1",PrivacyDetectionTypesJson="[]",SanitizationStatusCode=FilePrivacyCodes.SanitizationCompleted,SanitizationCompletedAt=now,UploadedByUserId=actor.Id,CreatedAt=now};
        db.Files.Add(file);await db.SaveChangesAsync(token);
        try{await using var content=new MemoryStream(validated.Bytes);await storage.SaveAsync(key,content,token);file.StatusCode="ACTIVE";file.ActivatedAt=now;file.ScanResultText=$"LOCAL_BASIC_CLEAN;METADATA_REMOVED;{validated.Width}x{validated.Height}";db.HelpPostFiles.Add(new HelpPostFile{HelpPostId=post.Id,FileId=file.Id,DisplayOrder=order,CreatedAt=now,CreatedByUserId=actor.Id});await db.SaveChangesAsync(token);if(transaction is not null)await transaction.CommitAsync(token);return new(file.PublicId,publicName,file.ContentType,file.SizeBytes,$"/api/v1/help-room/{post.PublicId}/files/{file.PublicId}",true);}catch{await storage.DeleteIfExistsAsync(key,token);throw;}
    }

    public async Task<(Stream Stream,string ContentType,string FileName)> OpenFile(ClaimsPrincipal principal,Guid postId,Guid fileId,CancellationToken token)
    {var actor=await User(principal,token);var row=await(from post in db.HelpPosts.AsNoTracking() join link in db.HelpPostFiles.AsNoTracking() on post.Id equals link.HelpPostId join file in db.Files.AsNoTracking() on link.FileId equals file.Id where post.PublicId==postId&&file.PublicId==fileId&&file.StatusCode=="ACTIVE" select new{post,file}).SingleOrDefaultAsync(token)??throw Error("HELP_FILE_NOT_FOUND","사진을 찾을 수 없습니다.",404);if(row.post.StatusCode=="HIDDEN"&&row.post.CustomerUserId!=actor.Id&&!actor.IsAdmin)throw Error("HELP_FILE_NOT_FOUND","사진을 찾을 수 없습니다.",404);var result=await publication.ResolveAsync(row.file,actor.Id,true,true,token);if(!result.Allowed)throw Error("HELP_FILE_PRIVACY_BLOCKED",result.Message,403);var published=result.PublishedFile!;return(await storage.OpenReadAsync(published.StorageKey,token),published.ContentType,published.OriginalFileName);}

    public async Task ReportFile(ClaimsPrincipal principal,Guid postId,Guid fileId,string? reason,CancellationToken token)
    {var actor=await User(principal,token);var row=await(from post in db.HelpPosts join link in db.HelpPostFiles on post.Id equals link.HelpPostId join file in db.Files on link.FileId equals file.Id where post.PublicId==postId&&file.PublicId==fileId&&file.StatusCode=="ACTIVE" select new{post,file}).SingleOrDefaultAsync(token)??throw Error("HELP_FILE_NOT_FOUND","신고할 사진을 찾을 수 없습니다.",404);if(row.file.UploadedByUserId==actor.Id)throw Error("HELP_FILE_SELF_REPORT","본인이 등록한 사진은 신고할 수 없습니다.",409);row.file.StatusCode="QUARANTINED";row.file.ScanResultText=$"MEMBER_REPORTED:{actor.Id}:{Clean(reason)??"사유 미입력"}";await db.SaveChangesAsync(token);}

    public async Task<IReadOnlyList<HelpFileReviewItem>> ReviewQueue(ClaimsPrincipal principal,CancellationToken token)
    {var admin=await User(principal,token);if(!admin.IsAdmin)throw Error("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);return await(from post in db.HelpPosts.AsNoTracking() join link in db.HelpPostFiles.AsNoTracking() on post.Id equals link.HelpPostId join file in db.Files.AsNoTracking() on link.FileId equals file.Id where file.PurposeCode=="HELP_ROOM_PHOTO"&&file.StatusCode=="QUARANTINED" orderby file.CreatedAt select new HelpFileReviewItem(post.PublicId,file.PublicId,post.Title,file.OriginalFileName,file.ContentType,file.SizeBytes,file.ScanResultText,file.CreatedAt)).ToListAsync(token);}

    public async Task<(Stream Stream,string ContentType,string FileName)> OpenReviewFile(ClaimsPrincipal principal,Guid postId,Guid fileId,CancellationToken token)
    {var admin=await User(principal,token);if(!admin.IsAdmin)throw Error("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);var file=await(from post in db.HelpPosts.AsNoTracking() join link in db.HelpPostFiles.AsNoTracking() on post.Id equals link.HelpPostId join value in db.Files.AsNoTracking() on link.FileId equals value.Id where post.PublicId==postId&&value.PublicId==fileId&&value.StatusCode=="QUARANTINED" select value).SingleOrDefaultAsync(token)??throw Error("HELP_FILE_NOT_FOUND","검수할 사진을 찾을 수 없습니다.",404);return(await storage.OpenReadAsync(file.StorageKey,token),file.ContentType,file.OriginalFileName);}

    public async Task ReviewFile(ClaimsPrincipal principal,Guid postId,Guid fileId,bool approved,CancellationToken token)
    {var admin=await User(principal,token);if(!admin.IsAdmin)throw Error("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);var file=await(from post in db.HelpPosts join link in db.HelpPostFiles on post.Id equals link.HelpPostId join value in db.Files on link.FileId equals value.Id where post.PublicId==postId&&value.PublicId==fileId&&value.StatusCode=="QUARANTINED" select value).SingleOrDefaultAsync(token)??throw Error("HELP_FILE_NOT_FOUND","검수할 사진을 찾을 수 없습니다.",404);file.StatusCode=approved?"ACTIVE":"DELETED";file.DeletedAt=approved?null:DateTime.UtcNow;file.ScanResultText=approved?"ADMIN_REVIEW_APPROVED":"ADMIN_REVIEW_REJECTED";await db.SaveChangesAsync(token);}

    public async Task<HelpPostDetail> Advise(ClaimsPrincipal principal,Guid id,AddProviderAdviceInput input,CancellationToken token)
    {
        var actor=await Provider(principal,token);
        await using var transaction=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token):null;
        var post=await db.HelpPosts.SingleOrDefaultAsync(x=>x.PublicId==id&&x.StatusCode=="PUBLISHED",token)??throw Error("HELP_POST_NOT_FOUND","답변 가능한 도움방 글을 찾을 수 없습니다.",404);
        var approved=await(from service in db.ProviderServiceCategories join approval in db.ProviderServiceApprovals on service.Id equals approval.ProviderServiceCategoryId where service.ProviderProfileId==actor.ProviderId&&service.CategoryId==post.CategoryId&&service.StatusCode=="ACTIVE"&&approval.ApprovalStatusCode=="APPROVED" select service.Id).AnyAsync(token);
        if(!approved)throw Error("HELP_CATEGORY_NOT_APPROVED","본사 승인된 자신의 서비스 카테고리에만 조언할 수 있습니다.",403);
        var dangerous=post.IntentCode=="DANGEROUS";if(dangerous&&!string.IsNullOrWhiteSpace(input.DiySteps))throw Error("DANGEROUS_DIY_BLOCKED","위험 작업에는 DIY 상세 절차를 제공할 수 없습니다.");
        var body=Required(input.Body,2,5000);var cause=Clean(input.Cause);var check=Clean(input.Check);var diy=dangerous?null:Clean(input.DiySteps);var risk=Clean(input.Risk);var next=Clean(input.NextStep);var requires=dangerous||input.RequiresProfessional;var now=DateTime.UtcNow;
        var previous=await db.HelpRoomEntries.Where(x=>x.HelpPostId==post.Id&&x.ProviderProfileId==actor.ProviderId&&x.AuthorRoleCode=="PROVIDER"&&x.StatusCode=="PUBLISHED").OrderBy(x=>x.CreatedAt).ToListAsync(token);
        var signature=AdviceSignature(body,cause,check,diy,risk,next,requires);if(previous.Any(x=>AdviceSignature(x.Body,x.CauseText,x.CheckText,x.DiyStepsText,x.RiskText,x.NextStepText,x.RequiresProfessional)==signature))throw Error("HELP_ADVICE_DUPLICATE","같은 내용의 조언은 다시 등록할 수 없습니다.",409);
        var last=previous.LastOrDefault();if(last is not null){if(last.CreatedAt>now.AddMinutes(-1))throw Error("HELP_ADVICE_COOLDOWN","같은 글에는 마지막 답변 후 1분이 지나야 다시 답변할 수 있습니다.",429);var hasNewQuestion=await db.HelpRoomEntries.AnyAsync(x=>x.HelpPostId==post.Id&&x.AuthorRoleCode=="CUSTOMER"&&x.EntryTypeCode=="CUSTOMER_FOLLOW_UP"&&x.StatusCode=="PUBLISHED"&&x.CreatedAt>last.CreatedAt,token);if(!hasNewQuestion)throw Error("HELP_FOLLOW_UP_REQUIRED","고객이 추가 질문을 등록한 후에만 후속 답변을 작성할 수 있습니다.",409);}
        db.HelpRoomEntries.Add(new HelpRoomEntry{HelpPostId=post.Id,AuthorUserId=actor.UserId,ProviderProfileId=actor.ProviderId,AuthorRoleCode="PROVIDER",EntryTypeCode="PROVIDER_ADVICE",Body=body,CauseText=cause,CheckText=check,DiyStepsText=diy,RiskText=risk,NextStepText=next,RequiresProfessional=requires,SafetyCode=dangerous?"PROFESSIONAL_ONLY":requires?"CAUTION":"NORMAL",StatusCode="PUBLISHED",CreatedAt=now,CreatedByUserId=actor.UserId});await db.SaveChangesAsync(token);if(transaction is not null)await transaction.CommitAsync(token);return await Detail(principal,id,token);
    }

    public async Task<HelpPostDetail> Resolve(ClaimsPrincipal principal,Guid id,ResolveHelpPostInput input,CancellationToken token)
    {var actor=await Customer(principal,token);var post=await Owned(id,actor.Id,token);EnsureOpen(post);var code=Allowed(input.ResolutionCode,["SELF_RESOLVED","PROFESSIONAL_NEEDED"],"SELF_RESOLVED");long? helpful=null;if(input.HelpfulEntryId.HasValue)helpful=await db.HelpRoomEntries.Where(x=>x.PublicId==input.HelpfulEntryId&&x.HelpPostId==post.Id&&x.StatusCode=="PUBLISHED").Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw Error("HELPFUL_ENTRY_INVALID","도움이 된 답변을 찾을 수 없습니다.");var now=DateTime.UtcNow;db.HelpPostResolutions.Add(new HelpPostResolution{HelpPostId=post.Id,ResolvedByCustomerUserId=actor.Id,ResolutionCode=code,Summary=Required(input.Summary,2,3000),HelpfulEntryId=helpful,CreatedAt=now,CreatedByUserId=actor.Id});post.StatusCode="RESOLVED";post.ResolvedAt=now;post.UpdatedAt=now;post.UpdatedByUserId=actor.Id;await db.SaveChangesAsync(token);return await Detail(principal,id,token);}

    public async Task<ConvertHelpPostResponse> Convert(ClaimsPrincipal principal,Guid id,CancellationToken token)
    {var actor=await Customer(principal,token);var post=await Owned(id,actor.Id,token);EnsureOpen(post);var category=await db.ServiceCategories.Where(x=>x.Id==post.CategoryId).Select(x=>x.PublicId).SingleAsync(token);var area=post.AdministrativeAreaId.HasValue?await db.AdministrativeAreas.Where(x=>x.Id==post.AdministrativeAreaId).Select(x=>(Guid?)x.PublicId).SingleAsync(token):null;var created=await requests.CreateAsync(principal,new(category,area,post.Title,post.Body,null,null,false,$"help:{post.PublicId:N}",[]),token);post.ConvertedServiceRequestId=await db.ServiceRequests.Where(x=>x.PublicId==created.Id).Select(x=>x.Id).SingleAsync(token);post.StatusCode="CONVERTED";post.ResolvedAt=DateTime.UtcNow;post.UpdatedAt=DateTime.UtcNow;post.UpdatedByUserId=actor.Id;db.HelpPostResolutions.Add(new HelpPostResolution{HelpPostId=post.Id,ResolvedByCustomerUserId=actor.Id,ResolutionCode="QUOTE_CONVERTED",Summary="고객 확인 후 견적요청 초안으로 전환했습니다.",CreatedAt=DateTime.UtcNow,CreatedByUserId=actor.Id});await db.SaveChangesAsync(token);return new(created.Id,"DRAFT",$"/customer/requests/new?draft={created.Id}");}

    public async Task Hide(ClaimsPrincipal principal,Guid id,bool hidden,CancellationToken token){var admin=await User(principal,token);if(!admin.IsAdmin)throw Error("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);var post=await db.HelpPosts.SingleOrDefaultAsync(x=>x.PublicId==id,token)??throw Error("HELP_POST_NOT_FOUND","도움방 글을 찾을 수 없습니다.",404);post.StatusCode=hidden?"HIDDEN":"PUBLISHED";post.UpdatedAt=DateTime.UtcNow;post.UpdatedByUserId=admin.Id;await db.SaveChangesAsync(token);}

    private async Task<HelpPost> Owned(Guid id,long userId,CancellationToken token)=>await db.HelpPosts.SingleOrDefaultAsync(x=>x.PublicId==id&&x.CustomerUserId==userId,token)??throw Error("HELP_POST_NOT_FOUND","내 도움방 글을 찾을 수 없습니다.",404);
    private static void EnsureOpen(HelpPost post){if(post.StatusCode!="PUBLISHED")throw Error("HELP_POST_CLOSED","이미 해결 또는 전환된 글입니다.",409);}
    private async Task<(long Id,bool IsAdmin)> User(ClaimsPrincipal p,CancellationToken token){if(!Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id))throw Error("AUTH_REQUIRED","로그인이 필요합니다.",401);var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==id&&x.StatusCode=="ACTIVE",token)??throw Error("AUTH_REQUIRED","로그인이 필요합니다.",401);return(user.Id,p.IsInRole("ADMIN"));}
    private async Task<(long Id,long ProfileId)> Customer(ClaimsPrincipal p,CancellationToken token){var user=await User(p,token);var profile=await db.CustomerProfiles.AsNoTracking().Where(x=>x.UserId==user.Id).Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw Error("CUSTOMER_PROFILE_REQUIRED","고객 프로필이 필요합니다.",403);return(user.Id,profile);}
    private async Task<(long UserId,long ProviderId)> Provider(ClaimsPrincipal p,CancellationToken token){var user=await User(p,token);var profile=await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==user.Id&&x.ApprovalStatusCode=="APPROVED"&&x.ActivityStatusCode=="ACTIVE",token)??throw Error("APPROVED_PROVIDER_REQUIRED","본사 승인 후 활동 중인 전문가만 조언할 수 있습니다.",403);return(user.Id,profile.Id);}
    private static string ProviderAction(DateTime? lastAdvice,DateTime? latestQuestion)=>!lastAdvice.HasValue?"ANSWERABLE":latestQuestion.HasValue&&latestQuestion>lastAdvice?"FOLLOW_UP_AVAILABLE":"WAITING_CUSTOMER";
    private static (string Code,string? Reason) Intent(string text){if(DangerWords.Any(text.Contains))return("DANGEROUS","감전·가스·구조·화재 등 위험 가능성이 감지되어 DIY 상세안내를 제한합니다.");if(QuoteWords.Any(text.Contains))return("QUOTE_RECOMMENDED","비용·방문·수리 의도가 감지되었습니다. 고객 확인 후 견적요청으로 전환할 수 있습니다.");return("ADVICE",null);}
    private static string AdviceSignature(string? body,string? cause,string? check,string? diy,string? risk,string? next,bool requires)=>NormalizeAdvice(string.Join('|',body,cause,check,diy,risk,next,requires?"1":"0"));
    private static string NormalizeAdvice(string value)=>string.Join(' ',value.Trim().ToLowerInvariant().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
    private static async Task<SafeHelpRoomImage> Validate(IFormFile upload,CancellationToken token){if(upload.Length<=0||upload.Length>5*1024*1024)throw Error("HELP_FILE_SIZE_INVALID","사진은 장당 5MB 이하여야 합니다.");var type=upload.ContentType.ToLowerInvariant();if(type is not("image/jpeg" or "image/png"))throw Error("HELP_FILE_TYPE_INVALID","JPEG 또는 PNG 사진만 등록할 수 있습니다.");var name=Path.GetFileName(upload.FileName);var extension=Path.GetExtension(name).ToLowerInvariant();var extensionMatches=type=="image/jpeg"?extension is ".jpg" or ".jpeg":extension==".png";if(string.IsNullOrWhiteSpace(name)||name!=upload.FileName||!extensionMatches)throw Error("HELP_FILE_EXTENSION_INVALID","파일명 확장자와 사진 형식이 일치해야 합니다.");await using var source=upload.OpenReadStream();using var memory=new MemoryStream();await source.CopyToAsync(memory,token);try{return HelpRoomImageSafety.Sanitize(memory.ToArray(),type);}catch(InvalidDataException e){throw Error("HELP_FILE_UNSAFE",e.Message);}}
    private static string Alias(long id)=>$"수달회원-{id%10000:D4}";private static string Required(string? value,int min,int max){var clean=value?.Trim();if(string.IsNullOrWhiteSpace(clean)||clean.Length<min||clean.Length>max)throw Error("HELP_INPUT_INVALID",$"내용은 {min}~{max}자로 입력해 주세요.");return clean;}private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();private static string Allowed(string? value,string[] values,string fallback){var clean=value?.Trim().ToUpperInvariant()??fallback;return values.Contains(clean)?clean:fallback;}private static WorkBusinessException Error(string code,string message,int status=400)=>new(code,message,status);
}

public sealed record CreateHelpPostInput(Guid CategoryId,Guid? AdministrativeAreaId,bool RevealRegion,string PurposeCode,string Title,string Body);
public sealed record AddHelpEntryInput(string Body);
public sealed record AddProviderAdviceInput(string Body,string? Cause,string? Check,string? DiySteps,string? Risk,string? NextStep,bool RequiresProfessional);
public sealed record ResolveHelpPostInput(string ResolutionCode,string Summary,Guid? HelpfulEntryId);
public sealed record HelpPostListItem(Guid Id,string Title,Guid CategoryId,string CategoryName,string? RegionName,string Status,string Intent,DateTime CreatedAt,int AnswerCount,string AuthorAlias,string? ProviderAction);
public sealed record HelpEntryResponse(Guid Id,string AuthorRole,string AuthorDisplay,string Body,string? Cause,string? Check,string? DiySteps,string? Risk,string? NextStep,bool RequiresProfessional,string Safety,DateTime CreatedAt);
public sealed record HelpResolutionResponse(string Code,string Summary,DateTime CreatedAt);
public sealed record HelpFileResponse(Guid Id,string FileName,string ContentType,long SizeBytes,string DownloadUrl,bool PubliclySafe);
public sealed record HelpPostDetail(Guid Id,string Title,string Body,Guid CategoryId,string CategoryName,string? RegionName,string RegionDisclosure,string Purpose,string Status,string Intent,string? IntentReason,bool Converted,bool IsOwner,string AuthorAlias,DateTime CreatedAt,IReadOnlyList<HelpEntryResponse> Entries,IReadOnlyList<HelpFileResponse> Files,HelpResolutionResponse? Resolution,string? ProviderAction);
public sealed record ConvertHelpPostResponse(Guid RequestId,string Status,string ContinuePath);
public sealed record HelpFileReviewItem(Guid PostId,Guid FileId,string PostTitle,string FileName,string ContentType,long SizeBytes,string? ReportReason,DateTime CreatedAt);
