using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Proposals;

public sealed partial class ProposalService
{
    private const decimal FeePerPerson=5000;
    private const int MaximumLocalAreas=5;
    private const int MaximumActiveCampaigns=5;
    private readonly SoodalLifeDbContext db;
    private readonly ProviderWalletService wallets;
    private readonly ProposalOptions options;
    public ProposalService(SoodalLifeDbContext db,ProviderWalletService wallets,Microsoft.Extensions.Options.IOptions<ProposalOptions> options){this.db=db;this.wallets=wallets;this.options=options.Value;}

    public static ProposalPolicyGuideResponse Guide()=>new(
        "모집 정원 × 5,000원을 이용료 잔액에서 예약하고 참여 확정 인원당 5,000원을 확정 차감합니다.",
        "프로그램 피드·WEB 알림·PUSH", "프로그램 피드·WEB 알림·PUSH·SMS",
        "최종 마케팅 약관과 채널 설정이 활성화된 고객에게만 외부 발송하며 PUSH는 PWA 활성화 전 운영 게이트로 차단합니다.",
        ["기존 광고와 별도 운영","현재 활성·승인된 전문가 서비스만 선택","지역모집은 실제 활동지역 중 최대 5개 시·군·구","전국모집은 전국 서비스로 설정한 원격·온라인·택배형 서비스만 허용","모집기간 1일 이상 30일 이하","카탈로그 밖 자유입력 차단"]);

    public async Task<ProviderProposalSetupResponse> Setup(ClaimsPrincipal principal,CancellationToken token)
    {
        var provider=await Provider(principal,token);
        var rows=await(from ps in db.ProviderServiceCategories.AsNoTracking()
            join category in db.ServiceCategories.AsNoTracking() on ps.CategoryId equals category.Id
            join middle in db.ServiceCategories.AsNoTracking() on category.ParentId equals middle.Id
            join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
            join approval in db.ProviderServiceApprovals.AsNoTracking() on ps.Id equals approval.ProviderServiceCategoryId
            where ps.ProviderProfileId==provider.ProfileId&&ps.StatusCode=="ACTIVE"&&category.StatusCode=="ACTIVE"&&category.LevelCode=="SERVICE"&&approval.ApprovalStatusCode=="APPROVED"
            orderby major.SortOrder,middle.SortOrder,category.SortOrder
            select new{ProviderServiceId=ps.Id,category.PublicId,category.Name,Path=major.Name+" › "+middle.Name+" › "+category.Name,ps.IsNationwide}).ToListAsync(token);
        var ids=rows.Select(x=>x.ProviderServiceId).ToArray();
        var areas=await(from link in db.ProviderServiceAreas.AsNoTracking()
            join area in db.AdministrativeAreas.AsNoTracking() on link.AdministrativeAreaId equals area.Id
            join parent in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parent.Id into parents from parent in parents.DefaultIfEmpty()
            where ids.Contains(link.ProviderServiceCategoryId)&&link.StatusCode=="ACTIVE"&&area.AreaLevelCode=="SIGUNGU"
            select new{link.ProviderServiceCategoryId,area.PublicId,area.AreaName,Parent=parent==null?null:parent.AreaName}).ToListAsync(token);
        var wallet=await db.ProviderWallets.AsNoTracking().Where(x=>x.ProviderProfileId==provider.ProfileId&&x.CurrencyCode=="KRW").Select(x=>new{x.AvailableBalance,x.StatusCode}).SingleOrDefaultAsync(token);
        return new(rows.Select(x=>new ProposalServiceOptionResponse(x.PublicId,x.Name,x.Path,x.IsNationwide,
            areas.Where(a=>a.ProviderServiceCategoryId==x.ProviderServiceId).Select(a=>new ProposalAreaResponse(a.PublicId,a.AreaName,a.Parent)).ToArray())).ToArray(),FeePerPerson,MaximumLocalAreas,MaximumActiveCampaigns,wallet?.AvailableBalance??0,wallet?.StatusCode??"NOT_FOUND");
    }

    public async Task<ProposalCampaignListResponse> ProviderList(ClaimsPrincipal principal,CancellationToken token)
    {var p=await Provider(principal,token);var ids=await db.ProviderProposalCampaigns.AsNoTracking().Where(x=>x.ProviderProfileId==p.ProfileId).OrderByDescending(x=>x.CreatedAt).Select(x=>x.Id).Take(100).ToListAsync(token);return new(await BuildMany(ids,null,token));}

    public async Task<ProposalApplicationListResponse> Applications(ClaimsPrincipal principal,Guid campaignId,CancellationToken token)
    {var p=await Provider(principal,token);var campaign=await db.ProviderProposalCampaigns.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==campaignId&&x.ProviderProfileId==p.ProfileId,token)??throw Error("PROPOSAL_NOT_FOUND","모집을 찾을 수 없습니다.",404);var rows=await(from a in db.ProviderProposalApplications.AsNoTracking() join c in db.CustomerProfiles.AsNoTracking() on a.CustomerProfileId equals c.Id where a.CampaignId==campaign.Id orderby a.AppliedAt select new ProposalApplicationResponse(a.PublicId,campaignId,c.PublicId,c.DisplayName,a.StatusCode,a.AppliedAt,a.ConfirmedAt)).ToListAsync(token);return new(rows);}

    public async Task<ProposalCampaignListResponse> PublicList(Guid? categoryId,Guid? areaId,int take,ClaimsPrincipal? principal,CancellationToken token)
    {
        var now=DateTime.UtcNow;take=Math.Clamp(take,1,50);
        var query=db.ProviderProposalCampaigns.AsNoTracking().Where(x=>x.StartAt<=now&&x.EndAt>now&&(x.StatusCode=="PUBLISHED"||x.StatusCode=="MINIMUM_MET"||x.StatusCode=="FULL"));
        if(categoryId.HasValue)query=query.Where(x=>db.ProviderServiceCategories.Any(ps=>ps.Id==x.ProviderServiceCategoryId&&db.ServiceCategories.Any(c=>c.Id==ps.CategoryId&&c.PublicId==categoryId)));
        if(areaId.HasValue)query=query.Where(x=>x.ScopeCode=="NATIONWIDE"||db.ProviderProposalAreas.Any(a=>a.CampaignId==x.Id&&db.AdministrativeAreas.Any(area=>area.Id==a.AdministrativeAreaId&&area.PublicId==areaId)));
        var ids=await query.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Select(x=>x.Id).Take(take).ToListAsync(token);long? customer=null;
        if(principal?.Identity?.IsAuthenticated==true&&principal.IsInRole(RoleCodes.Customer))customer=(await Customer(principal,token)).ProfileId;
        return new(await BuildMany(ids,customer,token));
    }

    public async Task<ProposalCampaignResponse?> PublicDetail(Guid id,ClaimsPrincipal? principal,CancellationToken token)
    {var cid=await db.ProviderProposalCampaigns.AsNoTracking().Where(x=>x.PublicId==id).Select(x=>(long?)x.Id).SingleOrDefaultAsync(token);if(cid==null)return null;long? customer=null;if(principal?.Identity?.IsAuthenticated==true&&principal.IsInRole(RoleCodes.Customer))customer=(await Customer(principal,token)).ProfileId;return await Build(cid.Value,customer,token);}

    private async Task<ProposalApplicationResponse> Application(long id,Guid campaignId,CancellationToken token)=>await(from a in db.ProviderProposalApplications.AsNoTracking() join c in db.CustomerProfiles.AsNoTracking() on a.CustomerProfileId equals c.Id where a.Id==id select new ProposalApplicationResponse(a.PublicId,campaignId,c.PublicId,c.DisplayName,a.StatusCode,a.AppliedAt,a.ConfirmedAt)).SingleAsync(token);
    private async Task<List<ProposalCampaignResponse>> BuildMany(IEnumerable<long> ids,long? customer,CancellationToken token){var result=new List<ProposalCampaignResponse>();foreach(var id in ids)result.Add(await Build(id,customer,token));return result;}
    private async Task<ProposalCampaignResponse> Build(long id,long? customer,CancellationToken token)
    {
        var row=await(from c in db.ProviderProposalCampaigns.AsNoTracking() join p in db.ProviderProfiles.AsNoTracking() on c.ProviderProfileId equals p.Id join u in db.Users.AsNoTracking() on p.UserId equals u.Id join ps in db.ProviderServiceCategories.AsNoTracking() on c.ProviderServiceCategoryId equals ps.Id join category in db.ServiceCategories.AsNoTracking() on ps.CategoryId equals category.Id join middle in db.ServiceCategories.AsNoTracking() on category.ParentId equals middle.Id join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id where c.Id==id select new{c,p,u,category,Path=major.Name+" › "+middle.Name+" › "+category.Name}).SingleAsync(token);
        var areas=await(from x in db.ProviderProposalAreas.AsNoTracking() join area in db.AdministrativeAreas.AsNoTracking() on x.AdministrativeAreaId equals area.Id join parent in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parent.Id into parents from parent in parents.DefaultIfEmpty() where x.CampaignId==id select new ProposalAreaResponse(area.PublicId,area.AreaName,parent==null?null:parent.AreaName)).ToListAsync(token);
        var my=customer.HasValue?await db.ProviderProposalApplications.AsNoTracking().Where(x=>x.CampaignId==id&&x.CustomerProfileId==customer).Select(x=>x.StatusCode).SingleOrDefaultAsync(token):null;
        var can=(row.c.StatusCode is "PUBLISHED" or "MINIMUM_MET")&&row.c.StartAt<=DateTime.UtcNow&&row.c.EndAt>DateTime.UtcNow&&row.c.ConfirmedParticipants<row.c.MaximumParticipants;
        var publicProviderName=string.IsNullOrWhiteSpace(row.p.BusinessName)||row.p.BusinessName.Equals(row.u.LoginId,StringComparison.OrdinalIgnoreCase)?"수달 전문가":row.p.BusinessName;
        return new(row.c.PublicId,row.c.ProposalTypeCode,row.c.ScopeCode,row.c.Title,row.c.Summary,row.category.PublicId,row.category.Name,row.Path,row.p.PublicId,publicProviderName,row.c.NormalPriceAmount,row.c.OfferPriceAmount,row.c.MinimumParticipants,row.c.MaximumParticipants,row.c.ConfirmedParticipants,row.c.StartAt,row.c.EndAt,row.c.ServiceAt,row.c.CancellationPolicyText,row.c.StatusCode,row.c.FeePerParticipant,row.c.ReservedFeeAmount,row.c.CapturedFeeAmount,row.c.FeeStatusCode,areas,can,my);
    }

    private static void Validate(SaveProviderProposalRequest x,bool allowStarted=false)
    {var type=x.ProposalTypeCode.Trim().ToUpperInvariant();if(type is not("DISCOUNT_SERVICE" or "GROUP_BUY" or "GROUP_LESSON"))throw Error("PROPOSAL_TYPE_INVALID","할인 서비스·공동구매·그룹 레슨만 등록할 수 있습니다.");var scope=x.ScopeCode.Trim().ToUpperInvariant();if(scope is not("LOCAL" or "NATIONWIDE"))throw Error("PROPOSAL_SCOPE_INVALID","전국 또는 지역 모집을 선택해 주세요.");if(string.IsNullOrWhiteSpace(x.Title)||string.IsNullOrWhiteSpace(x.Summary)||string.IsNullOrWhiteSpace(x.CancellationPolicyText))throw Error("PROPOSAL_REQUIRED_TEXT","제목·상세설명·취소 조건을 입력해 주세요.");if(x.OfferPriceAmount<=0||x.NormalPriceAmount.HasValue&&x.NormalPriceAmount<x.OfferPriceAmount)throw Error("PROPOSAL_PRICE_INVALID","제안가는 1원 이상이어야 하며 정상가는 제안가보다 작을 수 없습니다.");if(x.MinimumParticipants<1||x.MaximumParticipants<x.MinimumParticipants||x.MaximumParticipants>100)throw Error("PROPOSAL_PARTICIPANT_INVALID","모집 인원은 최소 1명, 최대 100명 범위에서 설정해 주세요.");var start=x.StartAt.ToUniversalTime();var end=x.EndAt.ToUniversalTime();var duration=end-start;if((!allowStarted&&start<DateTime.UtcNow.AddMinutes(-5))||end<=DateTime.UtcNow||duration<TimeSpan.FromDays(1)||duration>TimeSpan.FromDays(30))throw Error("PROPOSAL_PERIOD_INVALID","모집기간은 현재 이후부터 1일 이상 30일 이하로 설정해 주세요.");if(x.ServiceAt.HasValue&&x.ServiceAt.Value.ToUniversalTime()<end)throw Error("PROPOSAL_SERVICE_AT_INVALID","서비스 예정일은 모집 마감 이후로 설정해 주세요.");}
    private async Task<(long UserId,long ProfileId)> Provider(ClaimsPrincipal p,CancellationToken token){var user=await User(p,RoleCodes.Provider,token);var id=await db.ProviderProfiles.Where(x=>x.UserId==user&&x.ApprovalStatusCode=="APPROVED"&&x.ActivityStatusCode=="ACTIVE").Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw Error("PROPOSAL_PROVIDER_NOT_ACTIVE","승인되어 활동 중인 전문가만 제안·공동모집을 등록할 수 있습니다.",403);return(user,id);}
    private async Task<(long UserId,long ProfileId)> Customer(ClaimsPrincipal p,CancellationToken token){var user=await User(p,RoleCodes.Customer,token);var profile=await db.CustomerProfiles.Where(x=>x.UserId==user).Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw Error("CUSTOMER_PROFILE_NOT_FOUND","고객 정보를 확인할 수 없습니다.",404);return(user,profile);}
    private async Task<long> User(ClaimsPrincipal p,string role,CancellationToken token){if(!Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id))throw Error("AUTH_REQUIRED","로그인이 필요합니다.",401);var user=await(from u in db.Users join ur in db.UserRoles on u.Id equals ur.UserId join r in db.Roles on ur.RoleId equals r.Id where u.PublicId==id&&u.StatusCode=="ACTIVE"&&r.Code==role&&r.IsActive&&ur.RevokedAt==null select (long?)u.Id).SingleOrDefaultAsync(token);return user??throw Error("ROLE_REQUIRED","권한을 확인할 수 없습니다.",403);}
    private static ProposalException Error(string code,string message,int status=400)=>new(code,message,status);
}
