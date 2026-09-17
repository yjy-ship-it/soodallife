using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Matching;

public sealed class AdminMatchingOperationsService(SoodalLifeDbContext db)
{
    private static readonly string[] ExitLocks=["REQUESTED","UNDER_REVIEW","REFUND_REQUIRED","BLOCKED_BY_ACTIVE_WORK","READY_TO_COMPLETE"];

    public async Task<AdminMatchingDashboard> Dashboard(CancellationToken token)
    {
        var now=DateTime.UtcNow;
        var requestRows=await(from request in db.ServiceRequests.AsNoTracking()
            join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
            join area0 in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals (long?)area0.Id into areas
            from area in areas.DefaultIfEmpty()
            join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parent0.Id into parents
            from parent in parents.DefaultIfEmpty()
            where request.StatusCode!="DRAFT"
            orderby request.CreatedAt descending
            select new{request,CategoryName=category.Name,Area=area,Parent=parent}).Take(200).ToListAsync(token);
        var requestIds=requestRows.Select(x=>x.request.Id).ToArray();
        var candidateCounts=await db.DispatchCandidates.AsNoTracking().Where(x=>requestIds.Contains(x.ServiceRequestId)).GroupBy(x=>x.ServiceRequestId).Select(g=>new{Id=g.Key,Total=g.Count(),Eligible=g.Count(x=>x.StatusCode=="ELIGIBLE")}).ToDictionaryAsync(x=>x.Id,token);
        var dispatchCounts=await db.RequestDispatches.AsNoTracking().Where(x=>requestIds.Contains(x.ServiceRequestId)&&x.StatusCode!="EXPIRED").GroupBy(x=>x.ServiceRequestId).Select(g=>new{Id=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count,token);
        var quoteCounts=await db.Quotes.AsNoTracking().Where(x=>requestIds.Contains(x.ServiceRequestId)).GroupBy(x=>x.ServiceRequestId).Select(g=>new{Id=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count,token);
        var requests=requestRows.Select(x=>{var counts=candidateCounts.GetValueOrDefault(x.request.Id);return new AdminMatchingRequest(x.request.PublicId,x.Area?.PublicId??Guid.Empty,x.request.Title,x.CategoryName,x.Area==null?"전국":x.Parent?.AreaName??(x.Area.AreaLevelCode=="SIDO"?x.Area.AreaName:"-"),x.Area==null?"온라인":x.Area.AreaLevelCode=="SIDO"?"전체":x.Area.AreaName,x.request.StatusCode,x.request.IsUrgent,x.request.OpenedAt,x.request.ExpiresAt,counts?.Total??0,counts?.Eligible??0,dispatchCounts.GetValueOrDefault(x.request.Id),quoteCounts.GetValueOrDefault(x.request.Id));}).ToArray();
        var coverageRows=await(from link in db.ProviderServiceAreas.AsNoTracking()
            join service in db.ProviderServiceCategories.AsNoTracking() on link.ProviderServiceCategoryId equals service.Id
            join approval in db.ProviderServiceApprovals.AsNoTracking() on service.Id equals approval.ProviderServiceCategoryId
            join provider in db.ProviderProfiles.AsNoTracking() on service.ProviderProfileId equals provider.Id
            join area in db.AdministrativeAreas.AsNoTracking() on link.AdministrativeAreaId equals area.Id
            join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parent0.Id into parents from parent in parents.DefaultIfEmpty()
            where link.StatusCode=="ACTIVE"&&service.StatusCode=="ACTIVE"&&approval.ApprovalStatusCode=="APPROVED"&&provider.ApprovalStatusCode=="APPROVED"&&provider.ActivityStatusCode=="ACTIVE"&&!db.ProviderExitRequests.Any(e=>e.ProviderProfileId==provider.Id&&ExitLocks.Contains(e.StatusCode))
            select new{AreaDbId=area.Id,area.PublicId,area.AreaName,area.AreaLevelCode,Province=parent==null?(area.AreaLevelCode=="SIDO"?area.AreaName:"-"):parent.AreaName,ProviderId=provider.Id,ServiceId=service.Id}).ToListAsync(token);
        var open=requests.Where(x=>x.Status=="OPEN"&&(x.ExpiresAt==null||x.ExpiresAt>now)).ToArray();
        var coverageGroups=coverageRows.GroupBy(x=>new{x.PublicId,x.Province,District=x.AreaLevelCode=="SIDO"?"전체":x.AreaName}).ToDictionary(g=>(g.Key.PublicId,g.Key.Province,g.Key.District),g=>new{Providers=g.Select(x=>x.ProviderId).Distinct().Count(),Services=g.Select(x=>x.ServiceId).Distinct().Count()});
        var regionKeys=coverageGroups.Keys.Concat(open.Select(x=>(x.AreaId,x.ProvinceName,x.DistrictName))).Distinct().ToArray();
        var regions=regionKeys.Select(key=>{var coverage=coverageGroups.GetValueOrDefault(key);return new AdminMatchingRegion(key.Item1,key.Item2,key.Item3,coverage?.Providers??0,coverage?.Services??0,open.Count(x=>x.AreaId==key.Item1),open.Count(x=>x.AreaId==key.Item1&&x.DispatchCount==0));}).OrderBy(x=>x.ProvinceName).ThenBy(x=>x.DistrictName).ToArray();
        var openCount=open.Length;var unmatched=open.Count(x=>x.DispatchCount==0);var activeDispatch=await db.RequestDispatches.AsNoTracking().CountAsync(x=>x.StatusCode=="AVAILABLE"&&x.ExpiresAt>now,token);
        return new(new(openCount,unmatched,activeDispatch,regions.Count(x=>x.ActiveProviderCount>0),coverageRows.Select(x=>x.ProviderId).Distinct().Count()),regions,requests);
    }
}
