using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Features.Proposals;

public sealed partial class ProposalService
{
    public async Task<IReadOnlyList<CustomerProposalParticipationResponse>> GetMyApplications(ClaimsPrincipal principal, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var rows = await (from application in db.ProviderProposalApplications.AsNoTracking()
            join campaign in db.ProviderProposalCampaigns.AsNoTracking() on application.CampaignId equals campaign.Id
            join provider in db.ProviderProfiles.AsNoTracking() on campaign.ProviderProfileId equals provider.Id
            join providerService in db.ProviderServiceCategories.AsNoTracking() on campaign.ProviderServiceCategoryId equals providerService.Id
            join category in db.ServiceCategories.AsNoTracking() on providerService.CategoryId equals category.Id
            where application.CustomerProfileId == customer.ProfileId
            orderby application.AppliedAt descending
            select new { Application = application, Campaign = campaign, Provider = provider, Category = category }).ToListAsync(token);
        return rows.Select(row => new CustomerProposalParticipationResponse(row.Application.PublicId,row.Campaign.PublicId,row.Campaign.Title,row.Provider.PublicId,row.Provider.BusinessName,row.Category.PublicId,row.Category.Name,row.Application.StatusCode,row.Campaign.StatusCode,row.Campaign.OfferPriceAmount,row.Application.AppliedAt,row.Application.ConfirmedAt,row.Application.CancelledAt,row.Application.DeclinedAt,row.Campaign.EndAt,row.Campaign.ServiceAt,row.Application.StatusCode=="APPLIED",NextStep(row.Application.StatusCode,row.Campaign.StatusCode,row.Campaign.ServiceAt))).ToArray();
    }

    private static string NextStep(string applicationStatus,string campaignStatus,DateTime? serviceAt) => applicationStatus switch
    {
        "APPLIED" => "전문가가 신청을 검토 중입니다. 확정 전에는 직접 취소할 수 있습니다.",
        "CONFIRMED" when campaignStatus == "CANCELLED" => "모집이 취소되었습니다. 전문가 안내를 확인해 주세요.",
        "CONFIRMED" when serviceAt.HasValue => $"참여가 확정되었습니다. {serviceAt.Value.ToLocalTime():M월 d일 H시 m분} 일정과 전문가 안내를 확인해 주세요.",
        "CONFIRMED" => "참여가 확정되었습니다. 전문가의 일정 안내를 확인해 주세요.",
        "DECLINED" => "이번 모집에는 참여가 확정되지 않았습니다.",
        "CANCELLED" => "참여 신청이 취소되었습니다.",
        _ => "현재 상태를 확인해 주세요."
    };

    public async Task<IReadOnlyList<InterestedServiceResponse>> GetInterestedServices(ClaimsPrincipal principal, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await (from interest in db.CustomerProposalCategoryInterests.AsNoTracking()
            join service in db.ServiceCategories.AsNoTracking() on interest.CategoryId equals service.Id
            join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
            join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
            where interest.CustomerProfileId == customer.ProfileId && interest.IsActive &&
                  service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
                  middle.StatusCode == "ACTIVE" && major.StatusCode == "ACTIVE"
            orderby interest.UpdatedAt descending, service.Name
            select new InterestedServiceResponse(service.PublicId, service.ExternalCode, service.SearchSlug, service.Name,
                major.Name + " › " + middle.Name + " › " + service.Name,
                db.CategoryOperationPolicies.Any(x => x.CategoryId == service.Id && x.IsActive && x.SubscriptionOptionText == "허용" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo > today)) ||
                db.CategoryPolicies.Any(x => x.CategoryId == service.Id && x.SubscriptionOptionText == "허용" && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo > today)),
                interest.UpdatedAt)).ToListAsync(token);
    }

    public async Task<InterestedServiceStateResponse> GetInterestedServiceState(ClaimsPrincipal principal, Guid serviceId, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var categoryId = await db.ServiceCategories.AsNoTracking()
            .Where(x => x.PublicId == serviceId && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE")
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
        if (categoryId is null) throw Error("INTERESTED_SERVICE_NOT_FOUND", "현재 제공 중인 서비스를 찾을 수 없습니다.", 404);
        var interested = await db.CustomerProposalCategoryInterests.AsNoTracking()
            .AnyAsync(x => x.CustomerProfileId == customer.ProfileId && x.CategoryId == categoryId && x.IsActive, token);
        return new(serviceId, interested);
    }

    public async Task<InterestedServiceStateResponse> SaveInterestedService(ClaimsPrincipal principal, Guid serviceId, bool interested, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var categoryId = await db.ServiceCategories
            .Where(x => x.PublicId == serviceId && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE")
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
        if (categoryId is null) throw Error("INTERESTED_SERVICE_NOT_FOUND", "현재 제공 중인 서비스만 관심 서비스로 저장할 수 있습니다.", 404);
        var now = DateTime.UtcNow;
        var row = await db.CustomerProposalCategoryInterests
            .SingleOrDefaultAsync(x => x.CustomerProfileId == customer.ProfileId && x.CategoryId == categoryId, token);
        if (row is null)
        {
            if (interested) db.CustomerProposalCategoryInterests.Add(new CustomerProposalCategoryInterest
            {
                CustomerProfileId = customer.ProfileId, CategoryId = categoryId.Value, SourceCode = "SERVICE_DETAIL",
                IsActive = true, CreatedAt = now, UpdatedAt = now
            });
        }
        else if (row.IsActive != interested || interested)
        {
            row.IsActive = interested;
            row.SourceCode = "SERVICE_DETAIL";
            row.UpdatedAt = now;
        }
        await db.SaveChangesAsync(token);
        return new(serviceId, interested);
    }

    public async Task RecordSignal(ClaimsPrincipal principal, RecordProposalSignalRequest input, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var type = input.SignalTypeCode.Trim().ToUpperInvariant();
        if (type is not ("SERVICE_DETAIL" or "SERVICE_SEARCH" or "SESSION_CATEGORY")) throw Error("PROPOSAL_SIGNAL_INVALID", "추천 활동 유형을 확인할 수 없습니다.");
        var categoryId = await db.ServiceCategories.Where(x => x.PublicId == input.ServiceCategoryId && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE").Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw Error("PROPOSAL_SIGNAL_CATEGORY_INVALID", "현재 제공 중인 서비스만 추천 기록에 사용할 수 있습니다.");
        var now = DateTime.UtcNow; var since = now.AddHours(-12);
        var existing = await db.CustomerProposalSignals.SingleOrDefaultAsync(x => x.CustomerProfileId == customer.ProfileId && x.CategoryId == categoryId && x.SignalTypeCode == type && x.OccurredAt >= since, token);
        if (existing is null) db.CustomerProposalSignals.Add(new CustomerProposalSignal { CustomerProfileId = customer.ProfileId, CategoryId = categoryId, SignalTypeCode = type, OccurredAt = now, ExpiresAt = now.AddDays(30) });
        else { existing.OccurredAt = now; existing.ExpiresAt = now.AddDays(30); }
        await db.SaveChangesAsync(token);
    }

    public async Task<ProposalApplicationResponse> Apply(ClaimsPrincipal principal, Guid campaignId, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var campaign = await db.ProviderProposalCampaigns.SingleOrDefaultAsync(x => x.PublicId == campaignId, token)
            ?? throw Error("PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.", 404);
        var now = DateTime.UtcNow;
        if (campaign.StatusCode is not ("PUBLISHED" or "MINIMUM_MET") || campaign.StartAt > now || campaign.EndAt <= now || campaign.ConfirmedParticipants >= campaign.MaximumParticipants)
            throw Error("PROPOSAL_APPLICATION_CLOSED", "현재 참여 신청을 받을 수 없는 모집입니다.", 409);

        var existing = await db.ProviderProposalApplications.SingleOrDefaultAsync(x => x.CampaignId == campaign.Id && x.CustomerProfileId == customer.ProfileId, token);
        if (existing is not null)
        {
            if (existing.StatusCode is "APPLIED" or "CONFIRMED") return await Application(existing.Id, campaign.PublicId, token);
            existing.StatusCode = "APPLIED"; existing.AppliedAt = now; existing.ConfirmedAt = null; existing.CancelledAt = null; existing.DeclinedAt = null;
            existing.UpdatedAt = now; existing.UpdatedByUserId = customer.UserId;
        }
        else
        {
            existing = new ProviderProposalApplication
            {
                CampaignId = campaign.Id, CustomerProfileId = customer.ProfileId, StatusCode = "APPLIED", AppliedAt = now,
                CreatedAt = now, CreatedByUserId = customer.UserId, UpdatedAt = now, UpdatedByUserId = customer.UserId
            };
            db.ProviderProposalApplications.Add(existing);
        }
        await db.SaveChangesAsync(token);
        return await Application(existing.Id, campaign.PublicId, token);
    }

    public async Task<ProposalApplicationResponse> CancelApplication(ClaimsPrincipal principal, Guid campaignId, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var campaign = await db.ProviderProposalCampaigns.SingleOrDefaultAsync(x => x.PublicId == campaignId, token)
            ?? throw Error("PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.", 404);
        var application = await db.ProviderProposalApplications.SingleOrDefaultAsync(x => x.CampaignId == campaign.Id && x.CustomerProfileId == customer.ProfileId, token)
            ?? throw Error("PROPOSAL_APPLICATION_NOT_FOUND", "참여 신청을 찾을 수 없습니다.", 404);
        if (application.StatusCode != "APPLIED")
            throw Error("PROPOSAL_APPLICATION_NOT_CANCELLABLE", "전문가가 확정하기 전의 신청만 바로 취소할 수 있습니다.", 409);
        var now = DateTime.UtcNow; application.StatusCode = "CANCELLED"; application.CancelledAt = now;
        application.UpdatedAt = now; application.UpdatedByUserId = customer.UserId;
        await db.SaveChangesAsync(token);
        return await Application(application.Id, campaign.PublicId, token);
    }

    public async Task<ProposalInterestResponse> GetInterests(ClaimsPrincipal principal, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var categories = await (from x in db.CustomerProposalCategoryInterests.AsNoTracking()
            join c in db.ServiceCategories.AsNoTracking() on x.CategoryId equals c.Id
            where x.CustomerProfileId == customer.ProfileId && x.IsActive select c.PublicId).ToListAsync(token);
        var areas = await (from x in db.CustomerProposalAreaInterests.AsNoTracking()
            join a in db.AdministrativeAreas.AsNoTracking() on x.AdministrativeAreaId equals a.Id
            where x.CustomerProfileId == customer.ProfileId && x.IsActive select a.PublicId).ToListAsync(token);
        return new(categories, areas);
    }

    public async Task<ProposalInterestResponse> SaveInterests(ClaimsPrincipal principal, SaveProposalInterestRequest input, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var categoryIds = (input.CategoryIds ?? []).Distinct().ToArray();
        var areaIds = (input.AreaIds ?? []).Distinct().ToArray();
        var validCategories = await db.ServiceCategories.Where(x => categoryIds.Contains(x.PublicId) && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE").ToListAsync(token);
        var validAreas = await db.AdministrativeAreas.Where(x => areaIds.Contains(x.PublicId) && x.AreaLevelCode == "SIGUNGU").ToListAsync(token);
        if (validCategories.Count != categoryIds.Length) throw Error("PROPOSAL_INTEREST_CATEGORY_INVALID", "현재 제공 중인 하위 서비스만 관심 서비스로 저장할 수 있습니다.");
        if (validAreas.Count != areaIds.Length) throw Error("PROPOSAL_INTEREST_AREA_INVALID", "시·군·구 단위의 지역만 우리 동네로 저장할 수 있습니다.");

        var now = DateTime.UtcNow;
        var currentCategories = await db.CustomerProposalCategoryInterests.Where(x => x.CustomerProfileId == customer.ProfileId).ToListAsync(token);
        foreach (var row in currentCategories) { row.IsActive = validCategories.Any(x => x.Id == row.CategoryId); row.UpdatedAt = now; }
        foreach (var category in validCategories.Where(x => currentCategories.All(y => y.CategoryId != x.Id)))
            db.CustomerProposalCategoryInterests.Add(new CustomerProposalCategoryInterest { CustomerProfileId = customer.ProfileId, CategoryId = category.Id, SourceCode = "DIRECT", IsActive = true, CreatedAt = now, UpdatedAt = now });

        var currentAreas = await db.CustomerProposalAreaInterests.Where(x => x.CustomerProfileId == customer.ProfileId).ToListAsync(token);
        foreach (var row in currentAreas) { row.IsActive = validAreas.Any(x => x.Id == row.AdministrativeAreaId); row.UpdatedAt = now; }
        foreach (var area in validAreas.Where(x => currentAreas.All(y => y.AdministrativeAreaId != x.Id)))
            db.CustomerProposalAreaInterests.Add(new CustomerProposalAreaInterest { CustomerProfileId = customer.ProfileId, AdministrativeAreaId = area.Id, SourceCode = "DIRECT", IsActive = true, CreatedAt = now, UpdatedAt = now });

        await db.SaveChangesAsync(token);
        return await GetInterests(principal, token);
    }
}
