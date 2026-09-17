using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdvertisingContentService(SoodalLifeDbContext db)
{
    private const int MaximumPageSize = 100;
    private static readonly string[] Audiences = ["ALL", "CUSTOMER", "PROVIDER"];
    private static readonly string[] CampaignTypes = ["ADVERTISEMENT", "PROMOTION", "BANNER", "POPUP"];
    private static readonly string[] OwnerTypes = ["HEAD_OFFICE", "PLATFORM", "EXTERNAL", "PROVIDER"];
    private static readonly string[] ContentTypes = ["NOTICE", "FAQ", "SAFETY_GUIDE", "CATEGORY_GUIDE", "PRICE_REFERENCE"];
    private static readonly DefaultManagedContent[] DefaultManagedContents =
    [
        new("SAFETY_GUIDE", 10, "방문 작업 전 안전 확인", "전문가 방문 전 작업 장소 주변을 정리하고 작업에 방해가 되는 물건을 치워 주세요. 고객은 어린이·고령자·반려동물이 작업 구역에 접근하지 않도록 보호하고, 전문가는 작업 범위와 위험요소를 고객에게 먼저 설명한 뒤 작업을 시작해야 합니다."),
        new("SAFETY_GUIDE", 20, "전기·가스·수도 작업 안전수칙", "전기·가스·수도 관련 작업은 필요한 자격과 경험을 갖춘 전문가가 수행해야 합니다. 작업 전 차단기·밸브·계량기 위치를 확인하고 필요하면 공급을 차단하세요. 누전, 가스 냄새, 누수 확대 등 즉시 위험한 상황에서는 작업을 중단하고 119 또는 관계기관에 먼저 연락해 주세요."),
        new("SAFETY_GUIDE", 30, "사다리·고소 작업 안전수칙", "사다리와 고소 작업 장비는 평탄하고 미끄럽지 않은 곳에 설치하고, 작업 구역 아래에는 사람이 지나가지 않도록 표시해야 합니다. 비·강풍 등 기상 상태가 좋지 않거나 안전 장비를 갖추지 못한 경우 작업을 진행하지 마세요."),
        new("SAFETY_GUIDE", 40, "작업 중 사진·개인정보 보호", "사진에는 작업 확인에 필요한 범위만 담고 주민등록번호, 금융정보, 현관 비밀번호와 가족의 얼굴 등 불필요한 개인정보가 노출되지 않도록 확인해 주세요. 개인정보가 포함된 자료는 채팅이나 첨부파일로 전달하지 않는 것이 안전합니다."),
        new("SAFETY_GUIDE", 50, "작업 완료 후 안전 점검", "작업이 끝나면 전원·가스·수도 연결 상태, 누수·누전·파손 여부와 작업 주변 정리 상태를 고객과 전문가가 함께 확인해 주세요. 사용 방법과 주의사항, 보증 및 A/S 조건도 확인하고 문제가 있으면 완료 처리 전에 기록을 남겨 주세요."),

        new("CATEGORY_GUIDE", 10, "서비스 요청을 정확하게 작성하는 방법", "필요한 서비스, 증상, 수량과 규모, 현장 조건, 희망 일정과 지역을 구체적으로 작성하면 더 정확한 견적을 받을 수 있습니다. 사진은 전체 모습과 문제 부분이 함께 보이도록 촬영하되 개인정보가 노출되지 않도록 확인해 주세요."),
        new("CATEGORY_GUIDE", 20, "견적 비교와 전문가 선택 안내", "견적 금액만 보지 말고 작업 범위, 포함·제외 항목, 자재, 일정, 보증과 A/S 조건, 전문가 승인 상태와 리뷰를 함께 비교해 주세요. 이해되지 않는 내용은 채팅으로 확인하고 합의한 내용은 기록으로 남기는 것이 좋습니다."),
        new("CATEGORY_GUIDE", 30, "방문 작업 전 확인사항", "방문일시와 주소, 주차·출입 방법, 현장 연락 담당자, 작업 공간과 필요한 전기·수도 사용 가능 여부를 미리 확인해 주세요. 일정이나 작업 범위가 바뀌면 작업 시작 전에 상대방과 다시 합의해야 합니다."),
        new("CATEGORY_GUIDE", 40, "작업 완료 확인과 거래 종료", "작업 결과가 견적과 합의 내용에 맞는지, 추가 비용이 있다면 사전에 동의했는지 확인해 주세요. 이상이 없으면 완료 처리하고, 문제나 미완료 항목이 있으면 사진과 설명을 남긴 뒤 전문가와 보완 일정을 정해 주세요."),
        new("CATEGORY_GUIDE", 50, "A/S·분쟁 신청 안내", "완료 후 문제가 발생하면 마이수달의 A/S·분쟁 메뉴에서 원 거래를 검색해 접수할 수 있습니다. 증상, 발생일시, 요청사항과 사진 등 확인 자료를 등록하고 전문가와 처리 일정을 협의하세요. 합의되지 않으면 분쟁 절차를 이용할 수 있습니다."),

        new("PRICE_REFERENCE", 10, "가격 참고자료 이용 안내", "가격 참고자료는 일반적인 작업 조건을 기준으로 이해를 돕기 위한 정보이며 최종 가격이 아닙니다. 실제 견적은 작업 범위, 현장 상태, 지역, 일정, 자재와 전문가 조건에 따라 달라질 수 있으므로 견적서의 세부 항목을 확인해 주세요."),
        new("PRICE_REFERENCE", 20, "견적 금액에 포함되는 항목", "견적을 받을 때 인건비, 출장비, 자재비, 장비 사용료, 폐기물 처리비, 부가세와 A/S 조건이 포함되었는지 확인해 주세요. 포함되지 않은 항목은 작업 시작 전에 예상 금액과 산정 기준을 확인하는 것이 좋습니다."),
        new("PRICE_REFERENCE", 30, "추가 비용이 발생할 수 있는 경우", "현장 확인 후 숨은 손상이나 추가 작업이 발견되거나 고객 요청으로 범위·자재·일정이 바뀌면 추가 비용이 발생할 수 있습니다. 전문가는 추가 작업 전에 사유와 금액을 안내하고 고객 동의를 받은 뒤 진행해야 합니다."),
        new("PRICE_REFERENCE", 40, "출장비·자재비 확인 방법", "출장비의 적용 지역과 재방문 조건, 자재의 규격·수량·단가와 반품 가능 여부를 견적서에서 확인해 주세요. 고가 자재나 주문 제작품은 발주 전 제품명, 규격, 금액과 취소 조건을 별도로 확인하는 것이 안전합니다."),
        new("PRICE_REFERENCE", 50, "결제·취소·환불 금액 확인", "서비스 대금은 고객이 선택한 전문가에게 직접 지급합니다. 지급 전 최종 작업 범위와 금액을 확인하고 영수증 등 증빙을 보관해 주세요. 취소·환불 금액은 작업 진행 정도, 이미 사용한 자재와 실제 발생 비용, 관계 법령 및 합의 내용에 따라 달라질 수 있습니다.")
    ];

    public Task<List<AdvertisingPlacementResponse>> GetPlacementsAsync(CancellationToken token) => db.AdvertisingPlacements.AsNoTracking()
        .OrderBy(item => item.Id).Select(item => new AdvertisingPlacementResponse(item.PublicId, item.Code, item.Name, item.Description, item.RouteHint, item.IsActive)).ToListAsync(token);

    public async Task<ManagedContentDefaultInitializationResponse> InitializeDefaultContentsAsync(Guid actorId, CancellationToken token)
    {
        var actor = await ActorAsync(actorId, token);
        var now = DateTime.UtcNow;
        var contents = await db.ManagedContents.ToListAsync(token);
        var contentIds = contents.Select(item => item.Id).ToArray();
        var versions = await db.ManagedContentVersions.Where(item => contentIds.Contains(item.ContentId)).ToListAsync(token);
        var createdCount = 0;
        var repairedCount = 0;
        var existingCount = 0;
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        foreach (var item in DefaultManagedContents)
        {
            var publicId = DefaultPublicId("d970", item);
            var content = contents.SingleOrDefault(value => value.PublicId == publicId);
            if (content is not null)
            {
                var version = versions.SingleOrDefault(value => value.ContentId == content.Id && value.VersionNo == content.CurrentVersionNo);
                var needsRepair = version is null || content.ContentTypeCode != item.Type || content.AudienceTypeCode != "ALL" ||
                    content.StatusCode != "ACTIVE" || content.ReviewStatusCode != "APPROVED" || content.DisplayOrder != item.DisplayOrder ||
                    version.Title != item.Title || version.BodyText != item.Body || version.DestinationTypeCode != "NONE";
                if (!needsRepair) { existingCount++; continue; }

                content.ContentTypeCode = item.Type; content.AudienceTypeCode = "ALL"; content.StatusCode = "ACTIVE";
                content.ReviewStatusCode = "APPROVED"; content.DisplayOrder = item.DisplayOrder; content.EndAt = null;
                content.ApprovedByUserId = actor; content.ApprovedAt = now; content.RejectionReason = null;
                content.UpdatedAt = now; content.UpdatedByUserId = actor;
                if (version is null)
                {
                    version = new ManagedContentVersion { PublicId = DefaultPublicId("d971", item), ContentId = content.Id, VersionNo = content.CurrentVersionNo };
                    db.ManagedContentVersions.Add(version); versions.Add(version);
                }
                version.Title = item.Title; version.BodyText = item.Body; version.QuestionText = null; version.AnswerText = null;
                version.FileId = null; version.LinkText = null; version.DestinationTypeCode = "NONE"; version.DestinationValue = null;
                version.ChangeReason = "기본 콘텐츠 한글 인코딩 복구"; version.CreatedAt = now; version.CreatedByUserId = actor;
                AddAudit(actor, "MANAGED_CONTENT_DEFAULT_REPAIRED", "MANAGED_CONTENT", content.PublicId, null, Snapshot(content), "기본 콘텐츠 한글 인코딩 복구", now);
                repairedCount++;
                continue;
            }

            var matchingVersion = versions.FirstOrDefault(value => value.Title == item.Title && contents.Any(owner => owner.Id == value.ContentId && owner.ContentTypeCode == item.Type));
            if (matchingVersion is not null) { existingCount++; continue; }

            content = new ManagedContent
            {
                PublicId = publicId,
                ContentTypeCode = item.Type, AudienceTypeCode = "ALL", StatusCode = "ACTIVE", ReviewStatusCode = "APPROVED",
                CurrentVersionNo = 1, DisplayOrder = item.DisplayOrder, StartAt = now, ApprovedByUserId = actor, ApprovedAt = now,
                CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor
            };
            db.ManagedContents.Add(content);
            await db.SaveChangesAsync(token);
            db.ManagedContentVersions.Add(new ManagedContentVersion
            {
                PublicId = DefaultPublicId("d971", item),
                ContentId = content.Id, VersionNo = 1, Title = item.Title, BodyText = item.Body,
                DestinationTypeCode = "NONE", ChangeReason = "운영 기본 콘텐츠 안전 등록", CreatedAt = now, CreatedByUserId = actor
            });
            AddAudit(actor, "MANAGED_CONTENT_DEFAULT_CREATED", "MANAGED_CONTENT", content.PublicId, null, Snapshot(content), "운영 기본 콘텐츠 안전 등록", now);
            contents.Add(content);
            createdCount++;
        }
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(createdCount, repairedCount, existingCount, DefaultManagedContents.Length);
    }

    public async Task<AdminAdvertisingCampaignListResponse> SearchCampaignsAsync(string? search, string? status, string? audience,
        string? placement, Guid? categoryId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken token)
    {
        ValidatePage(page, pageSize);
        var query = db.AdvertisingCampaigns.AsNoTracking().AsQueryable();
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(item => item.CampaignName.Contains(term) ||
            (item.OwnerDisplayName != null && item.OwnerDisplayName.Contains(term)) || db.AdvertisingCreatives.Any(creative => creative.CampaignId == item.Id && creative.Title.Contains(term)));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.StatusCode == status.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(audience)) query = query.Where(item => item.AudienceTypeCode == audience.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(placement)) query = query.Where(item => db.AdvertisingCampaignPlacements.Any(link => link.CampaignId == item.Id && db.AdvertisingPlacements.Any(slot => slot.Id == link.PlacementId && slot.Code == placement.Trim().ToUpperInvariant())));
        if (categoryId.HasValue) query = query.Where(item => db.AdvertisingCampaignCategories.Any(link => link.CampaignId == item.Id && db.ServiceCategories.Any(category => category.Id == link.CategoryId && category.PublicId == categoryId)));
        if (from.HasValue) query = query.Where(item => item.EndAt == null || item.EndAt >= from.Value.ToDateTime(TimeOnly.MinValue));
        if (to.HasValue) query = query.Where(item => item.StartAt < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(item => item.UpdatedAt).ThenBy(item => item.CampaignName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var ids = rows.Select(item => item.Id).ToArray();
        var placementRows = await (from link in db.AdvertisingCampaignPlacements.AsNoTracking() join slot in db.AdvertisingPlacements.AsNoTracking() on link.PlacementId equals slot.Id where ids.Contains(link.CampaignId) select new { link.CampaignId, slot.Name }).ToListAsync(token);
        var metrics = await (from creative in db.AdvertisingCreatives.AsNoTracking() join metric in db.AdvertisingEvents.AsNoTracking() on creative.Id equals metric.CreativeId where ids.Contains(creative.CampaignId) group metric by creative.CampaignId into values select new { CampaignId = values.Key, Impressions = values.LongCount(item => item.EventTypeCode == "IMPRESSION"), Clicks = values.LongCount(item => item.EventTypeCode == "CLICK") }).ToListAsync(token);
        var now = DateTime.UtcNow;
        return new(total, page, pageSize, rows.Select(item =>
        {
            var metric = metrics.SingleOrDefault(value => value.CampaignId == item.Id);
            return new AdminAdvertisingCampaignListItem(item.PublicId, item.CampaignName, item.CampaignTypeCode, item.AudienceTypeCode,
                item.OwnerDisplayName, placementRows.Where(value => value.CampaignId == item.Id).Select(value => value.Name).ToArray(),
                item.StartAt, item.EndAt, item.StatusCode, item.ReviewStatusCode, Publication(item.StatusCode, item.ReviewStatusCode, item.StartAt, item.EndAt, now), metric?.Impressions ?? 0, metric?.Clicks ?? 0);
        }).ToArray());
    }

    public async Task<AdminAdvertisingCampaignDetail?> GetCampaignAsync(Guid id, CancellationToken token)
    {
        var campaign = await db.AdvertisingCampaigns.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == id, token);
        return campaign is null ? null : await BuildCampaignAsync(campaign, token);
    }

    public async Task<AdminAdvertisingCampaignDetail> CreateCampaignAsync(SaveAdvertisingCampaignRequest request, Guid actorId, CancellationToken token)
    {
        ValidateCampaign(request); await ValidateCampaignTargetsAsync(request,token);var actor = await ActorAsync(actorId, token); var now = DateTime.UtcNow; await using var transaction = db.Database.IsRelational()?await db.Database.BeginTransactionAsync(token):null;
        var campaign = new AdvertisingCampaign { CampaignName = request.CampaignName.Trim(), CampaignTypeCode = Code(request.CampaignTypeCode), AudienceTypeCode = Code(request.AudienceTypeCode), OwnerTypeCode = Code(request.OwnerTypeCode), OwnerDisplayName = Clean(request.OwnerDisplayName), StartAt = request.StartAt.ToUniversalTime(), EndAt = request.EndAt?.ToUniversalTime(), StatusCode = "DRAFT", Priority = request.Priority, DestinationTypeCode = Code(request.DestinationTypeCode), DestinationValue = Clean(request.DestinationValue), ReviewStatusCode = "DRAFT", CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        db.AdvertisingCampaigns.Add(campaign); await db.SaveChangesAsync(token); await ReplaceCampaignTargetsAsync(campaign, request, actor, now, token);
        AddAudit(actor, "ADVERTISING_CAMPAIGN_CREATED", "ADVERTISING_CAMPAIGN", campaign.PublicId, null, Snapshot(campaign), null, now); await db.SaveChangesAsync(token);
        if(transaction is not null)await transaction.CommitAsync(token); return await BuildCampaignAsync(campaign, token);
    }

    public async Task<AdminAdvertisingCampaignDetail> UpdateCampaignAsync(Guid id, SaveAdvertisingCampaignRequest request, Guid actorId, CancellationToken token)
    {
        ValidateCampaign(request); var actor = await ActorAsync(actorId, token); var campaign = await CampaignAsync(id, token); var before = Snapshot(campaign); var now = DateTime.UtcNow;
        campaign.CampaignName = request.CampaignName.Trim(); campaign.CampaignTypeCode = Code(request.CampaignTypeCode); campaign.AudienceTypeCode = Code(request.AudienceTypeCode); campaign.OwnerTypeCode = Code(request.OwnerTypeCode); campaign.OwnerDisplayName = Clean(request.OwnerDisplayName); campaign.StartAt = request.StartAt.ToUniversalTime(); campaign.EndAt = request.EndAt?.ToUniversalTime(); campaign.Priority = request.Priority; campaign.DestinationTypeCode = Code(request.DestinationTypeCode); campaign.DestinationValue = Clean(request.DestinationValue); campaign.StatusCode = "DRAFT"; campaign.ReviewStatusCode = "DRAFT"; campaign.ApprovedAt = null; campaign.ApprovedByUserId = null; campaign.RejectionReason = null; campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor;
        await ReplaceCampaignTargetsAsync(campaign, request, actor, now, token); AddAudit(actor, "ADVERTISING_CAMPAIGN_UPDATED", "ADVERTISING_CAMPAIGN", campaign.PublicId, before, Snapshot(campaign), null, now); await db.SaveChangesAsync(token); return await BuildCampaignAsync(campaign, token);
    }

    public async Task<AdminAdvertisingCampaignDetail> ReviewCampaignAsync(Guid id, AdvertisingReviewRequest request, Guid actorId, CancellationToken token)
    {
        var actor = await ActorAsync(actorId, token); var campaign = await CampaignAsync(id, token); var action = Code(request.ActionCode); var reason = Clean(request.Reason); var now = DateTime.UtcNow; var before = Snapshot(campaign);
        if (action == "APPROVE")
        {
            if (!await db.AdvertisingCampaignPlacements.AnyAsync(item => item.CampaignId == campaign.Id, token) || !await db.AdvertisingCreatives.AnyAsync(item => item.CampaignId == campaign.Id && item.StatusCode == "ACTIVE", token)) throw Error("ADVERTISING_APPROVAL_INCOMPLETE", "노출 위치와 활성 Creative를 등록한 후 승인할 수 있습니다.", StatusCodes.Status409Conflict);
            campaign.ReviewStatusCode = "APPROVED"; campaign.StatusCode = "ACTIVE"; campaign.ApprovedByUserId = actor; campaign.ApprovedAt = now; campaign.RejectionReason = null;
        }
        else if (action == "REJECT")
        {
            if (string.IsNullOrWhiteSpace(reason)) throw Error("ADVERTISING_REJECTION_REASON_REQUIRED", "반려 사유를 입력해 주세요.");
            campaign.ReviewStatusCode = "REJECTED"; campaign.StatusCode = "DRAFT"; campaign.ApprovedByUserId = null; campaign.ApprovedAt = null; campaign.RejectionReason = reason;
        }
        else throw Error("ADVERTISING_REVIEW_ACTION_INVALID", "승인 또는 반려 작업만 가능합니다.");
        campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor; AddAudit(actor, action == "APPROVE" ? "ADVERTISING_CAMPAIGN_APPROVED" : "ADVERTISING_CAMPAIGN_REJECTED", "ADVERTISING_CAMPAIGN", campaign.PublicId, before, Snapshot(campaign), reason, now); await db.SaveChangesAsync(token); return await BuildCampaignAsync(campaign, token);
    }

    public async Task<AdminAdvertisingCampaignDetail> SubmitCampaignAsync(Guid id, AdvertisingOperationRequest request, Guid actorId, CancellationToken token)
    {
        var actor=await ActorAsync(actorId,token);var campaign=await CampaignAsync(id,token);if(campaign.ReviewStatusCode=="PENDING")return await BuildCampaignAsync(campaign,token);if(campaign.ReviewStatusCode is not("DRAFT" or "REJECTED"))throw Error("ADVERTISING_SUBMIT_NOT_ALLOWED","작성 중이거나 반려된 캠페인만 검토 요청할 수 있습니다.",409);if(!await db.AdvertisingCampaignPlacements.AnyAsync(item=>item.CampaignId==campaign.Id,token)||!await db.AdvertisingCreatives.AnyAsync(item=>item.CampaignId==campaign.Id&&item.StatusCode=="ACTIVE",token))throw Error("ADVERTISING_SUBMIT_INCOMPLETE","노출 위치와 활성 소재를 등록한 후 검토 요청해 주세요.",409);var now=DateTime.UtcNow;var before=Snapshot(campaign);campaign.ReviewStatusCode="PENDING";campaign.StatusCode="DRAFT";campaign.RejectionReason=null;campaign.UpdatedAt=now;campaign.UpdatedByUserId=actor;AddAudit(actor,"ADVERTISING_CAMPAIGN_SUBMITTED","ADVERTISING_CAMPAIGN",campaign.PublicId,before,Snapshot(campaign),Clean(request.Reason),now);await db.SaveChangesAsync(token);return await BuildCampaignAsync(campaign,token);
    }

    public async Task<AdminAdvertisingCampaignDetail> PauseCampaignAsync(Guid id, AdvertisingPauseRequest request, Guid actorId, CancellationToken token)
    {
        var reason = Clean(request.Reason); if (string.IsNullOrWhiteSpace(reason)) throw Error("ADVERTISING_PAUSE_REASON_REQUIRED", "중지 사유를 입력해 주세요."); var actor = await ActorAsync(actorId, token); var campaign = await CampaignAsync(id, token); if(campaign.StatusCode!="ACTIVE")throw Error("ADVERTISING_PAUSE_NOT_ALLOWED","운영 중인 캠페인만 중지할 수 있습니다.",409);var before = Snapshot(campaign); var now = DateTime.UtcNow; campaign.StatusCode = "PAUSED"; campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor; AddAudit(actor, "ADVERTISING_CAMPAIGN_PAUSED", "ADVERTISING_CAMPAIGN", campaign.PublicId, before, Snapshot(campaign), reason, now); await db.SaveChangesAsync(token); return await BuildCampaignAsync(campaign, token);
    }

    public async Task<AdminAdvertisingCampaignDetail> ResumeCampaignAsync(Guid id, AdvertisingOperationRequest request, Guid actorId, CancellationToken token)
    {
        var actor=await ActorAsync(actorId,token);var campaign=await CampaignAsync(id,token);if(campaign.StatusCode!="PAUSED"||campaign.ReviewStatusCode!="APPROVED")throw Error("ADVERTISING_RESUME_NOT_ALLOWED","승인 후 중지된 캠페인만 다시 운영할 수 있습니다.",409);var before=Snapshot(campaign);var now=DateTime.UtcNow;campaign.StatusCode="ACTIVE";campaign.UpdatedAt=now;campaign.UpdatedByUserId=actor;AddAudit(actor,"ADVERTISING_CAMPAIGN_RESUMED","ADVERTISING_CAMPAIGN",campaign.PublicId,before,Snapshot(campaign),Clean(request.Reason),now);await db.SaveChangesAsync(token);return await BuildCampaignAsync(campaign,token);
    }

    public async Task<AdvertisingCreativeResponse> CreateCreativeAsync(Guid campaignId, SaveAdvertisingCreativeRequest request, Guid actorId, CancellationToken token)
    {
        ValidateCreative(request); var actor = await ActorAsync(actorId, token); var campaign = await CampaignAsync(campaignId, token); var fileId = await FileIdAsync(request.FileId, token); var now = DateTime.UtcNow;
        var creative = new AdvertisingCreative { CampaignId = campaign.Id, Title = request.Title.Trim(), Subtitle = Clean(request.Subtitle), BodyText = Clean(request.BodyText), FileId = fileId, AltText = Clean(request.AltText), ButtonText = Clean(request.ButtonText), DestinationTypeCode = EmptyCode(request.DestinationTypeCode), DestinationValue = Clean(request.DestinationValue), DisplayOrder = request.DisplayOrder, StatusCode = Code(request.StatusCode), CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        db.AdvertisingCreatives.Add(creative); ResetCampaignReview(campaign, actor, now); AddAudit(actor, "ADVERTISING_CREATIVE_CREATED", "ADVERTISING_CREATIVE", creative.PublicId, null, JsonSerializer.Serialize(new { creative.Title, creative.StatusCode }), null, now); await db.SaveChangesAsync(token); return await CreativeResponseAsync(creative, token);
    }

    public async Task<AdvertisingCreativeResponse> UpdateCreativeAsync(Guid campaignId, Guid creativeId, SaveAdvertisingCreativeRequest request, Guid actorId, CancellationToken token)
    {
        ValidateCreative(request); var actor = await ActorAsync(actorId, token); var campaign = await CampaignAsync(campaignId, token); var creative = await db.AdvertisingCreatives.SingleOrDefaultAsync(item => item.PublicId == creativeId && item.CampaignId == campaign.Id, token) ?? throw Error("ADVERTISING_CREATIVE_NOT_FOUND", "Creative를 찾을 수 없습니다.", 404); var before = JsonSerializer.Serialize(new { creative.Title, creative.StatusCode }); var now = DateTime.UtcNow;
        creative.Title = request.Title.Trim(); creative.Subtitle = Clean(request.Subtitle); creative.BodyText = Clean(request.BodyText); creative.FileId = await FileIdAsync(request.FileId, token); creative.AltText = Clean(request.AltText); creative.ButtonText = Clean(request.ButtonText); creative.DestinationTypeCode = EmptyCode(request.DestinationTypeCode); creative.DestinationValue = Clean(request.DestinationValue); creative.DisplayOrder = request.DisplayOrder; creative.StatusCode = Code(request.StatusCode); creative.UpdatedAt = now; creative.UpdatedByUserId = actor; ResetCampaignReview(campaign, actor, now); AddAudit(actor, "ADVERTISING_CREATIVE_UPDATED", "ADVERTISING_CREATIVE", creative.PublicId, before, JsonSerializer.Serialize(new { creative.Title, creative.StatusCode }), null, now); await db.SaveChangesAsync(token); return await CreativeResponseAsync(creative, token);
    }

    public async Task<AdminManagedContentListResponse> SearchContentsAsync(string? search, string? type, string? status, string? audience, int page, int pageSize, CancellationToken token)
    {
        ValidatePage(page, pageSize); var query = db.ManagedContents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(item => item.ContentTypeCode == Code(type)); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.StatusCode == Code(status)); if (!string.IsNullOrWhiteSpace(audience)) query = query.Where(item => item.AudienceTypeCode == Code(audience)); var term = Clean(search); if (term is not null) query = query.Where(item => db.ManagedContentVersions.Any(version => version.ContentId == item.Id && version.VersionNo == item.CurrentVersionNo && (version.Title.Contains(term) || (version.QuestionText != null && version.QuestionText.Contains(term)))));
        var total = await query.CountAsync(token); var rows = await query.OrderByDescending(item => item.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token); var ids = rows.Select(item => item.Id).ToArray(); var versions = await db.ManagedContentVersions.AsNoTracking().Where(item => ids.Contains(item.ContentId)).ToListAsync(token); var now = DateTime.UtcNow;
        return new(total, page, pageSize, rows.Select(item => new AdminManagedContentListItem(item.PublicId, item.ContentTypeCode, item.AudienceTypeCode, versions.Single(value => value.ContentId == item.Id && value.VersionNo == item.CurrentVersionNo).Title, item.CurrentVersionNo, item.StartAt, item.EndAt, item.StatusCode, item.ReviewStatusCode, Publication(item.StatusCode, item.ReviewStatusCode, item.StartAt, item.EndAt, now))).ToArray());
    }

    public async Task<AdminManagedContentDetail?> GetContentAsync(Guid id, CancellationToken token) { var content = await db.ManagedContents.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == id, token); return content is null ? null : await BuildContentAsync(content, token); }

    public async Task<AdminManagedContentDetail> CreateContentAsync(SaveManagedContentRequest request, Guid actorId, CancellationToken token)
    {
        ValidateContent(request);await ValidateContentTargetsAsync(request,token);var actor = await ActorAsync(actorId, token); var now = DateTime.UtcNow; await using var transaction = db.Database.IsRelational()?await db.Database.BeginTransactionAsync(token):null; var content = new ManagedContent { ContentTypeCode = Code(request.ContentTypeCode), AudienceTypeCode = Code(request.AudienceTypeCode), StatusCode = "DRAFT", ReviewStatusCode = "DRAFT", CurrentVersionNo = 1, DisplayOrder = request.DisplayOrder, StartAt = request.StartAt.ToUniversalTime(), EndAt = request.EndAt?.ToUniversalTime(), CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor };
        db.ManagedContents.Add(content); await db.SaveChangesAsync(token); db.ManagedContentVersions.Add(await VersionAsync(content, request, actor, now, token)); await ReplaceContentTargetsAsync(content, request, actor, now, token); AddAudit(actor, "MANAGED_CONTENT_CREATED", "MANAGED_CONTENT", content.PublicId, null, Snapshot(content), request.ChangeReason, now); await db.SaveChangesAsync(token); if(transaction is not null)await transaction.CommitAsync(token); return await BuildContentAsync(content, token);
    }

    public async Task<AdminManagedContentDetail> UpdateContentAsync(Guid id, SaveManagedContentRequest request, Guid actorId, CancellationToken token)
    {
        ValidateContent(request); var actor = await ActorAsync(actorId, token); var content = await ContentAsync(id, token); var before = Snapshot(content); var now = DateTime.UtcNow; content.ContentTypeCode = Code(request.ContentTypeCode); content.AudienceTypeCode = Code(request.AudienceTypeCode); content.StatusCode = "DRAFT"; content.ReviewStatusCode = "DRAFT"; content.CurrentVersionNo++; content.DisplayOrder = request.DisplayOrder; content.StartAt = request.StartAt.ToUniversalTime(); content.EndAt = request.EndAt?.ToUniversalTime(); content.ApprovedByUserId = null; content.ApprovedAt = null; content.RejectionReason = null; content.UpdatedAt = now; content.UpdatedByUserId = actor; db.ManagedContentVersions.Add(await VersionAsync(content, request, actor, now, token)); await ReplaceContentTargetsAsync(content, request, actor, now, token); AddAudit(actor, "MANAGED_CONTENT_UPDATED", "MANAGED_CONTENT", content.PublicId, before, Snapshot(content), request.ChangeReason, now); await db.SaveChangesAsync(token); return await BuildContentAsync(content, token);
    }

    public async Task<AdminManagedContentDetail> ReviewContentAsync(Guid id, AdvertisingReviewRequest request, Guid actorId, CancellationToken token)
    {
        var actor = await ActorAsync(actorId, token); var content = await ContentAsync(id, token); var action = Code(request.ActionCode); var reason = Clean(request.Reason); var before = Snapshot(content); var now = DateTime.UtcNow;
        if (action == "APPROVE") { content.ReviewStatusCode = "APPROVED"; content.StatusCode = "ACTIVE"; content.ApprovedByUserId = actor; content.ApprovedAt = now; content.RejectionReason = null; }
        else if (action == "REJECT") { if (reason is null) throw Error("CONTENT_REJECTION_REASON_REQUIRED", "반려 사유를 입력해 주세요."); content.ReviewStatusCode = "REJECTED"; content.StatusCode = "DRAFT"; content.ApprovedByUserId = null; content.ApprovedAt = null; content.RejectionReason = reason; }
        else throw Error("CONTENT_REVIEW_ACTION_INVALID", "승인 또는 반려 작업만 가능합니다.");
        content.UpdatedAt = now; content.UpdatedByUserId = actor; AddAudit(actor, action == "APPROVE" ? "MANAGED_CONTENT_APPROVED" : "MANAGED_CONTENT_REJECTED", "MANAGED_CONTENT", content.PublicId, before, Snapshot(content), reason, now); await db.SaveChangesAsync(token); return await BuildContentAsync(content, token);
    }

    public async Task<AdminManagedContentDetail> SubmitContentAsync(Guid id, AdvertisingOperationRequest request, Guid actorId, CancellationToken token)
    {
        var actor=await ActorAsync(actorId,token);var content=await ContentAsync(id,token);if(content.ReviewStatusCode=="PENDING")return await BuildContentAsync(content,token);if(content.ReviewStatusCode is not("DRAFT" or "REJECTED"))throw Error("CONTENT_SUBMIT_NOT_ALLOWED","작성 중이거나 반려된 콘텐츠만 검토 요청할 수 있습니다.",409);var before=Snapshot(content);var now=DateTime.UtcNow;content.ReviewStatusCode="PENDING";content.StatusCode="DRAFT";content.RejectionReason=null;content.UpdatedAt=now;content.UpdatedByUserId=actor;AddAudit(actor,"MANAGED_CONTENT_SUBMITTED","MANAGED_CONTENT",content.PublicId,before,Snapshot(content),Clean(request.Reason),now);await db.SaveChangesAsync(token);return await BuildContentAsync(content,token);
    }

    public async Task<AdminManagedContentDetail> PauseContentAsync(Guid id, AdvertisingPauseRequest request, Guid actorId, CancellationToken token)
    {
        var reason=Clean(request.Reason);if(reason is null)throw Error("CONTENT_PAUSE_REASON_REQUIRED","중지 사유를 입력해 주세요.");var actor=await ActorAsync(actorId,token);var content=await ContentAsync(id,token);if(content.StatusCode!="ACTIVE")throw Error("CONTENT_PAUSE_NOT_ALLOWED","게시 중인 콘텐츠만 중지할 수 있습니다.",409);var before=Snapshot(content);var now=DateTime.UtcNow;content.StatusCode="PAUSED";content.UpdatedAt=now;content.UpdatedByUserId=actor;AddAudit(actor,"MANAGED_CONTENT_PAUSED","MANAGED_CONTENT",content.PublicId,before,Snapshot(content),reason,now);await db.SaveChangesAsync(token);return await BuildContentAsync(content,token);
    }

    public async Task<AdminManagedContentDetail> ResumeContentAsync(Guid id, AdvertisingOperationRequest request, Guid actorId, CancellationToken token)
    {
        var actor=await ActorAsync(actorId,token);var content=await ContentAsync(id,token);if(content.StatusCode!="PAUSED"||content.ReviewStatusCode!="APPROVED")throw Error("CONTENT_RESUME_NOT_ALLOWED","승인 후 중지된 콘텐츠만 다시 게시할 수 있습니다.",409);var before=Snapshot(content);var now=DateTime.UtcNow;content.StatusCode="ACTIVE";content.UpdatedAt=now;content.UpdatedByUserId=actor;AddAudit(actor,"MANAGED_CONTENT_RESUMED","MANAGED_CONTENT",content.PublicId,before,Snapshot(content),Clean(request.Reason),now);await db.SaveChangesAsync(token);return await BuildContentAsync(content,token);
    }

    public async Task<IReadOnlyList<PublicAdvertisingCreative>> GetPublicAdvertisingAsync(string audience, string placementCode, Guid? categoryId, Guid? areaId, CancellationToken token)
    {
        var audienceCode = ValidateAudience(audience); var placement = await db.AdvertisingPlacements.AsNoTracking().SingleOrDefaultAsync(item => item.Code == Code(placementCode) && item.IsActive, token); if (placement is null) return [];
        var categoryScope = await CategoryScopeAsync(categoryId, token); var areaScope = await AreaScopeAsync(areaId, token); var now = DateTime.UtcNow;
        var campaigns = await db.AdvertisingCampaigns.AsNoTracking().Where(item => item.StatusCode == "ACTIVE" && item.ReviewStatusCode == "APPROVED" && item.StartAt <= now && (item.EndAt == null || item.EndAt > now) && (item.AudienceTypeCode == "ALL" || item.AudienceTypeCode == audienceCode) && db.AdvertisingCampaignPlacements.Any(link => link.CampaignId == item.Id && link.PlacementId == placement.Id) && (!db.AdvertisingCampaignCategories.Any(link => link.CampaignId == item.Id) || categoryScope.Length > 0 && db.AdvertisingCampaignCategories.Any(link => link.CampaignId == item.Id && categoryScope.Contains(link.CategoryId))) && (!db.AdvertisingCampaignAreas.Any(link => link.CampaignId == item.Id) || areaScope.Length > 0 && db.AdvertisingCampaignAreas.Any(link => link.CampaignId == item.Id && areaScope.Contains(link.AdministrativeAreaId)))).ToListAsync(token);
        var ids = campaigns.Select(item => item.Id).ToArray();
        var since = now.AddHours(-24);
        var impressionCounts = await (from creative in db.AdvertisingCreatives.AsNoTracking()
                                      join item in db.AdvertisingEvents.AsNoTracking() on creative.Id equals item.CreativeId
                                      where ids.Contains(creative.CampaignId) && item.PlacementId == placement.Id && item.EventTypeCode == "IMPRESSION" && item.OccurredAt >= since
                                      group item by creative.CampaignId into values
                                      select new { CampaignId = values.Key, Count = values.Count() }).ToDictionaryAsync(x => x.CampaignId, x => x.Count, token);
        campaigns = campaigns.OrderByDescending(item => item.Priority).ThenBy(item => impressionCounts.GetValueOrDefault(item.Id)).ThenBy(item => item.Id).ToList();
        var creatives = await db.AdvertisingCreatives.AsNoTracking().Where(item => ids.Contains(item.CampaignId) && item.StatusCode == "ACTIVE").OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id).ToListAsync(token);
        var providerLinks = await (from application in db.ProviderAdvertisingApplications.AsNoTracking()
                                   join provider in db.ProviderProfiles.AsNoTracking() on application.ProviderProfileId equals provider.Id
                                   where ids.Contains(application.CampaignId)
                                   select new { application.CampaignId, provider.PublicId }).ToDictionaryAsync(x => x.CampaignId, x => x.PublicId, token);
        return campaigns.SelectMany(campaign => creatives.Where(item => item.CampaignId == campaign.Id).Select(item => { var destinationType = item.DestinationTypeCode ?? campaign.DestinationTypeCode; var destinationValue = item.DestinationValue ?? campaign.DestinationValue; Guid? providerId = providerLinks.TryGetValue(campaign.Id, out var linkedProviderId) ? linkedProviderId : null; if (destinationType == "INTERNAL_PATH" && providerId.HasValue) destinationValue = $"/providers/{providerId}?campaign={campaign.PublicId}"; return new PublicAdvertisingCreative(campaign.PublicId, providerId, item.PublicId, campaign.CampaignTypeCode, item.Title, item.Subtitle, item.BodyText, item.FileId.HasValue ? db.Files.Where(file => file.Id == item.FileId).Select(file => (Guid?)file.PublicId).FirstOrDefault() : null, item.AltText, item.ButtonText, destinationType, destinationValue, campaign.Priority, item.DisplayOrder); })).ToArray();
    }

    public async Task<IReadOnlyList<PublicManagedContent>> GetPublicContentsAsync(string contentType, string audience, Guid? categoryId, Guid? areaId, CancellationToken token)
    {
        var type = Code(contentType); if (!ContentTypes.Contains(type)) throw Error("CONTENT_TYPE_INVALID", "지원하지 않는 콘텐츠 유형입니다."); var audienceCode = ValidateAudience(audience); var categoryScope = await CategoryScopeAsync(categoryId, token); var areaScope = await AreaScopeAsync(areaId, token); var now = DateTime.UtcNow;
        var contents = await db.ManagedContents.AsNoTracking().Where(item => item.ContentTypeCode == type && item.StatusCode == "ACTIVE" && item.ReviewStatusCode == "APPROVED" && item.StartAt <= now && (item.EndAt == null || item.EndAt > now) && (item.AudienceTypeCode == "ALL" || item.AudienceTypeCode == audienceCode) && (!db.ManagedContentCategories.Any(link => link.ContentId == item.Id) || categoryScope.Length > 0 && db.ManagedContentCategories.Any(link => link.ContentId == item.Id && categoryScope.Contains(link.CategoryId))) && (!db.ManagedContentAreas.Any(link => link.ContentId == item.Id) || areaScope.Length > 0 && db.ManagedContentAreas.Any(link => link.ContentId == item.Id && areaScope.Contains(link.AdministrativeAreaId)))).OrderBy(item => item.DisplayOrder).ThenByDescending(item => item.StartAt).ToListAsync(token);
        var ids = contents.Select(item => item.Id).ToArray(); var versions = await db.ManagedContentVersions.AsNoTracking().Where(item => ids.Contains(item.ContentId)).ToListAsync(token);
        return contents.Select(item => { var version = versions.Single(value => value.ContentId == item.Id && value.VersionNo == item.CurrentVersionNo); return new PublicManagedContent(item.PublicId, item.ContentTypeCode, item.AudienceTypeCode, version.Title, version.BodyText, version.QuestionText, version.AnswerText, version.FileId.HasValue ? db.Files.Where(file => file.Id == version.FileId).Select(file => (Guid?)file.PublicId).FirstOrDefault() : null, version.LinkText, version.DestinationTypeCode, version.DestinationValue, item.DisplayOrder, item.CurrentVersionNo, item.StartAt); }).ToArray();
    }

    public async Task RecordEventAsync(Guid creativeId, string eventType, AdvertisingEventRequest request, CancellationToken token)
    {
        var type = Code(eventType); if (type is not ("IMPRESSION" or "CLICK")) throw Error("ADVERTISING_EVENT_TYPE_INVALID", "지원하지 않는 광고 이벤트입니다."); var eligible = await GetPublicAdvertisingAsync(request.AudienceTypeCode, request.PlacementCode, request.CategoryId, request.AreaId, token); if (!eligible.Any(item => item.CreativeId == creativeId)) throw Error("ADVERTISING_NOT_VISIBLE", "현재 노출 가능한 광고가 아닙니다.", 404); var creative = await db.AdvertisingCreatives.AsNoTracking().SingleAsync(item => item.PublicId == creativeId, token); var placement = await db.AdvertisingPlacements.AsNoTracking().SingleAsync(item => item.Code == Code(request.PlacementCode), token); db.AdvertisingEvents.Add(new AdvertisingEvent { CreativeId = creative.Id, PlacementId = placement.Id, EventTypeCode = type, OccurredAt = DateTime.UtcNow }); await db.SaveChangesAsync(token);
    }

    private async Task ReplaceCampaignTargetsAsync(AdvertisingCampaign campaign, SaveAdvertisingCampaignRequest request, long actor, DateTime now, CancellationToken token)
    {
        EnsureDistinct(request.PlacementCodes, "노출 위치"); EnsureDistinct(request.CategoryIds, "카테고리"); EnsureDistinct(request.AreaIds, "지역"); var placementCodes=request.PlacementCodes.Select(Code).ToArray();var placements = await db.AdvertisingPlacements.Where(item => placementCodes.Contains(item.Code) && item.IsActive).ToListAsync(token); if (placements.Count != placementCodes.Length) throw Error("ADVERTISING_PLACEMENT_INVALID", "사용할 수 없는 노출 위치가 포함되어 있습니다."); var categories = await db.ServiceCategories.Where(item => request.CategoryIds.Contains(item.PublicId) && item.StatusCode == "ACTIVE").ToListAsync(token); if (categories.Count != request.CategoryIds.Count) throw Error("ADVERTISING_CATEGORY_INVALID", "사용할 수 없는 서비스 카테고리가 포함되어 있습니다."); var areas = await db.AdministrativeAreas.Where(item => request.AreaIds.Contains(item.PublicId) && item.IsActive).ToListAsync(token); if (areas.Count != request.AreaIds.Count) throw Error("ADVERTISING_AREA_INVALID", "사용할 수 없는 지역이 포함되어 있습니다."); db.AdvertisingCampaignPlacements.RemoveRange(db.AdvertisingCampaignPlacements.Where(item => item.CampaignId == campaign.Id)); db.AdvertisingCampaignCategories.RemoveRange(db.AdvertisingCampaignCategories.Where(item => item.CampaignId == campaign.Id)); db.AdvertisingCampaignAreas.RemoveRange(db.AdvertisingCampaignAreas.Where(item => item.CampaignId == campaign.Id)); db.AdvertisingCampaignPlacements.AddRange(placements.Select(item => new AdvertisingCampaignPlacement { CampaignId = campaign.Id, PlacementId = item.Id, CreatedAt = now, CreatedByUserId = actor })); db.AdvertisingCampaignCategories.AddRange(categories.Select(item => new AdvertisingCampaignCategory { CampaignId = campaign.Id, CategoryId = item.Id, CreatedAt = now, CreatedByUserId = actor })); db.AdvertisingCampaignAreas.AddRange(areas.Select(item => new AdvertisingCampaignArea { CampaignId = campaign.Id, AdministrativeAreaId = item.Id, CreatedAt = now, CreatedByUserId = actor }));
    }

    private async Task ValidateCampaignTargetsAsync(SaveAdvertisingCampaignRequest request,CancellationToken token)
    {
        EnsureDistinct(request.PlacementCodes,"노출 위치");EnsureDistinct(request.CategoryIds,"카테고리");EnsureDistinct(request.AreaIds,"지역");var codes=request.PlacementCodes.Select(Code).ToArray();if(await db.AdvertisingPlacements.CountAsync(x=>codes.Contains(x.Code)&&x.IsActive,token)!=codes.Length)throw Error("ADVERTISING_PLACEMENT_INVALID","사용할 수 없는 노출 위치가 포함되어 있습니다.");if(await db.ServiceCategories.CountAsync(x=>request.CategoryIds.Contains(x.PublicId)&&x.StatusCode=="ACTIVE",token)!=request.CategoryIds.Count)throw Error("ADVERTISING_CATEGORY_INVALID","사용할 수 없는 서비스 카테고리가 포함되어 있습니다.");if(await db.AdministrativeAreas.CountAsync(x=>request.AreaIds.Contains(x.PublicId)&&x.IsActive,token)!=request.AreaIds.Count)throw Error("ADVERTISING_AREA_INVALID","사용할 수 없는 지역이 포함되어 있습니다.");
    }

    private async Task ReplaceContentTargetsAsync(ManagedContent content, SaveManagedContentRequest request, long actor, DateTime now, CancellationToken token)
    {
        EnsureDistinct(request.CategoryIds, "카테고리"); EnsureDistinct(request.AreaIds, "지역"); var categories = await db.ServiceCategories.Where(item => request.CategoryIds.Contains(item.PublicId) && item.StatusCode == "ACTIVE").ToListAsync(token); if (categories.Count != request.CategoryIds.Count) throw Error("CONTENT_CATEGORY_INVALID", "사용할 수 없는 서비스 카테고리가 포함되어 있습니다."); var areas = await db.AdministrativeAreas.Where(item => request.AreaIds.Contains(item.PublicId) && item.IsActive).ToListAsync(token); if (areas.Count != request.AreaIds.Count) throw Error("CONTENT_AREA_INVALID", "사용할 수 없는 지역이 포함되어 있습니다."); db.ManagedContentCategories.RemoveRange(db.ManagedContentCategories.Where(item => item.ContentId == content.Id)); db.ManagedContentAreas.RemoveRange(db.ManagedContentAreas.Where(item => item.ContentId == content.Id)); db.ManagedContentCategories.AddRange(categories.Select(item => new ManagedContentCategory { ContentId = content.Id, CategoryId = item.Id, CreatedAt = now, CreatedByUserId = actor })); db.ManagedContentAreas.AddRange(areas.Select(item => new ManagedContentArea { ContentId = content.Id, AdministrativeAreaId = item.Id, CreatedAt = now, CreatedByUserId = actor }));
    }

    private async Task ValidateContentTargetsAsync(SaveManagedContentRequest request,CancellationToken token)
    {
        EnsureDistinct(request.CategoryIds,"카테고리");EnsureDistinct(request.AreaIds,"지역");if(await db.ServiceCategories.CountAsync(x=>request.CategoryIds.Contains(x.PublicId)&&x.StatusCode=="ACTIVE",token)!=request.CategoryIds.Count)throw Error("CONTENT_CATEGORY_INVALID","사용할 수 없는 서비스 카테고리가 포함되어 있습니다.");if(await db.AdministrativeAreas.CountAsync(x=>request.AreaIds.Contains(x.PublicId)&&x.IsActive,token)!=request.AreaIds.Count)throw Error("CONTENT_AREA_INVALID","사용할 수 없는 지역이 포함되어 있습니다.");
    }

    private async Task<AdminAdvertisingCampaignDetail> BuildCampaignAsync(AdvertisingCampaign campaign, CancellationToken token)
    {
        var placements = await (from link in db.AdvertisingCampaignPlacements.AsNoTracking() join item in db.AdvertisingPlacements.AsNoTracking() on link.PlacementId equals item.Id where link.CampaignId == campaign.Id orderby item.Id select new AdvertisingPlacementResponse(item.PublicId, item.Code, item.Name, item.Description, item.RouteHint, item.IsActive)).ToListAsync(token); var categories = await (from link in db.AdvertisingCampaignCategories.AsNoTracking() join item in db.ServiceCategories.AsNoTracking() on link.CategoryId equals item.Id where link.CampaignId == campaign.Id orderby item.Name select new AdvertisingTargetResponse(item.PublicId, item.Name)).ToListAsync(token); var areas = await (from link in db.AdvertisingCampaignAreas.AsNoTracking() join item in db.AdministrativeAreas.AsNoTracking() on link.AdministrativeAreaId equals item.Id where link.CampaignId == campaign.Id orderby item.AreaName select new AdvertisingTargetResponse(item.PublicId, item.AreaName)).ToListAsync(token); var creatives = await db.AdvertisingCreatives.AsNoTracking().Where(item => item.CampaignId == campaign.Id).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id).ToListAsync(token); var creativeResponses = new List<AdvertisingCreativeResponse>(); foreach (var creative in creatives) creativeResponses.Add(await CreativeResponseAsync(creative, token)); var creativeIds = creatives.Select(item => item.Id).ToArray(); var impressions = await db.AdvertisingEvents.LongCountAsync(item => creativeIds.Contains(item.CreativeId) && item.EventTypeCode == "IMPRESSION", token); var clicks = await db.AdvertisingEvents.LongCountAsync(item => creativeIds.Contains(item.CreativeId) && item.EventTypeCode == "CLICK", token); return new(campaign.PublicId, campaign.CampaignName, campaign.CampaignTypeCode, campaign.AudienceTypeCode, campaign.OwnerTypeCode, campaign.OwnerDisplayName, campaign.StartAt, campaign.EndAt, campaign.StatusCode, campaign.Priority, campaign.DestinationTypeCode, campaign.DestinationValue, campaign.ReviewStatusCode, campaign.ApprovedAt, campaign.RejectionReason, Publication(campaign.StatusCode, campaign.ReviewStatusCode, campaign.StartAt, campaign.EndAt, DateTime.UtcNow), placements, categories, areas, creativeResponses, new(impressions, clicks), await HistoryAsync("ADVERTISING_CAMPAIGN", campaign.PublicId, token), Convert.ToBase64String(campaign.RowVersion));
    }

    private async Task<AdminManagedContentDetail> BuildContentAsync(ManagedContent content, CancellationToken token)
    {
        var versions = await db.ManagedContentVersions.AsNoTracking().Where(item => item.ContentId == content.Id).OrderByDescending(item => item.VersionNo).ToListAsync(token); var responses = new List<ManagedContentVersionResponse>(); foreach (var version in versions) responses.Add(await VersionResponseAsync(version, token)); var categories = await (from link in db.ManagedContentCategories.AsNoTracking() join item in db.ServiceCategories.AsNoTracking() on link.CategoryId equals item.Id where link.ContentId == content.Id orderby item.Name select new AdvertisingTargetResponse(item.PublicId, item.Name)).ToListAsync(token); var areas = await (from link in db.ManagedContentAreas.AsNoTracking() join item in db.AdministrativeAreas.AsNoTracking() on link.AdministrativeAreaId equals item.Id where link.ContentId == content.Id orderby item.AreaName select new AdvertisingTargetResponse(item.PublicId, item.AreaName)).ToListAsync(token); return new(content.PublicId, content.ContentTypeCode, content.AudienceTypeCode, content.StatusCode, content.ReviewStatusCode, content.CurrentVersionNo, content.DisplayOrder, content.StartAt, content.EndAt, content.ApprovedAt, content.RejectionReason, Publication(content.StatusCode, content.ReviewStatusCode, content.StartAt, content.EndAt, DateTime.UtcNow), responses.Single(item => item.VersionNo == content.CurrentVersionNo), responses, categories, areas, await HistoryAsync("MANAGED_CONTENT", content.PublicId, token), Convert.ToBase64String(content.RowVersion));
    }

    private async Task<AdvertisingCreativeResponse> CreativeResponseAsync(AdvertisingCreative item, CancellationToken token) { var file = item.FileId.HasValue ? await db.Files.AsNoTracking().Where(value => value.Id == item.FileId).Select(value => new { value.PublicId, value.OriginalFileName }).SingleOrDefaultAsync(token) : null; return new(item.PublicId, item.Title, item.Subtitle, item.BodyText, file?.PublicId, file?.OriginalFileName, item.AltText, item.ButtonText, item.DestinationTypeCode, item.DestinationValue, item.DisplayOrder, item.StatusCode, Convert.ToBase64String(item.RowVersion)); }
    private async Task<ManagedContentVersionResponse> VersionResponseAsync(ManagedContentVersion item, CancellationToken token) { var file = item.FileId.HasValue ? await db.Files.AsNoTracking().Where(value => value.Id == item.FileId).Select(value => new { value.PublicId, value.OriginalFileName }).SingleOrDefaultAsync(token) : null; return new(item.PublicId, item.VersionNo, item.Title, item.BodyText, item.QuestionText, item.AnswerText, file?.PublicId, file?.OriginalFileName, item.LinkText, item.DestinationTypeCode, item.DestinationValue, item.ChangeReason, item.CreatedAt); }
    private async Task<ManagedContentVersion> VersionAsync(ManagedContent content, SaveManagedContentRequest request, long actor, DateTime now, CancellationToken token) => new() { ContentId = content.Id, VersionNo = content.CurrentVersionNo, Title = request.Title.Trim(), BodyText = Clean(request.BodyText), QuestionText = Clean(request.QuestionText), AnswerText = Clean(request.AnswerText), FileId = await FileIdAsync(request.FileId, token), LinkText = Clean(request.LinkText), DestinationTypeCode = Code(request.DestinationTypeCode), DestinationValue = Clean(request.DestinationValue), ChangeReason = Clean(request.ChangeReason), CreatedAt = now, CreatedByUserId = actor };
    private async Task<IReadOnlyList<AdvertisingAuditResponse>> HistoryAsync(string type, Guid id, CancellationToken token) => await db.AuditLogs.AsNoTracking().Where(item => item.EntityType == type && item.EntityPublicId == id).OrderByDescending(item => item.OccurredAt).Take(100).Select(item => new AdvertisingAuditResponse(item.OccurredAt, item.ActionCode, item.ResultCode, item.Reason)).ToListAsync(token);
    private async Task<long?> FileIdAsync(Guid? id, CancellationToken token) { if (!id.HasValue) return null; return await db.Files.Where(item => item.PublicId == id && item.StatusCode == "ACTIVE" && item.ContentType.StartsWith("image/")).Select(item => (long?)item.Id).SingleOrDefaultAsync(token) ?? throw Error("ADVERTISING_MEDIA_INVALID", "사용할 수 있는 활성 이미지 파일이 아닙니다."); }
    private async Task<long[]> CategoryScopeAsync(Guid? id, CancellationToken token) { if (!id.HasValue) return []; var category = await db.ServiceCategories.AsNoTracking().Where(item => item.PublicId == id).Select(item => new { item.Id, item.ParentId }).SingleOrDefaultAsync(token); if (category is null) return []; var result = new List<long> { category.Id }; var parent = category.ParentId; while (parent.HasValue) { result.Add(parent.Value); parent = await db.ServiceCategories.AsNoTracking().Where(item => item.Id == parent.Value).Select(item => item.ParentId).SingleOrDefaultAsync(token); } return result.ToArray(); }
    private async Task<long[]> AreaScopeAsync(Guid? id, CancellationToken token) { if (!id.HasValue) return []; var area = await db.AdministrativeAreas.AsNoTracking().Where(item => item.PublicId == id).Select(item => new { item.Id, item.ParentAreaId }).SingleOrDefaultAsync(token); return area is null ? [] : area.ParentAreaId.HasValue ? [area.Id, area.ParentAreaId.Value] : [area.Id]; }
    private async Task<long> ActorAsync(Guid id, CancellationToken token) => await db.Users.Where(item => item.PublicId == id && item.StatusCode == "ACTIVE").Select(item => (long?)item.Id).SingleOrDefaultAsync(token) ?? throw Error("ADMIN_USER_NOT_FOUND", "관리자 계정을 확인할 수 없습니다.", 401);
    private async Task<AdvertisingCampaign> CampaignAsync(Guid id, CancellationToken token) =>
        await db.AdvertisingCampaigns.SingleOrDefaultAsync(item => item.PublicId == id, token)
        ?? throw Error("ADVERTISING_CAMPAIGN_NOT_FOUND", "광고 캠페인을 찾을 수 없습니다.", 404);
    private async Task<ManagedContent> ContentAsync(Guid id, CancellationToken token) =>
        await db.ManagedContents.SingleOrDefaultAsync(item => item.PublicId == id, token)
        ?? throw Error("MANAGED_CONTENT_NOT_FOUND", "콘텐츠를 찾을 수 없습니다.", 404);
    private void AddAudit(long actor, string action, string type, Guid id, string? before, string? after, string? reason, DateTime now) => db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = action, EntityType = type, EntityPublicId = id, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = after, Reason = reason });
    private static void ResetCampaignReview(AdvertisingCampaign campaign, long actor, DateTime now) { campaign.StatusCode = "DRAFT"; campaign.ReviewStatusCode = "DRAFT"; campaign.ApprovedAt = null; campaign.ApprovedByUserId = null; campaign.RejectionReason = null; campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor; }
    private static void ValidateCampaign(SaveAdvertisingCampaignRequest request) { if (string.IsNullOrWhiteSpace(request.CampaignName)) throw Error("ADVERTISING_CAMPAIGN_NAME_REQUIRED", "캠페인명을 입력해 주세요."); if (!CampaignTypes.Contains(Code(request.CampaignTypeCode))) throw Error("ADVERTISING_CAMPAIGN_TYPE_INVALID", "광고 유형을 확인해 주세요."); ValidateAudience(request.AudienceTypeCode); if (!OwnerTypes.Contains(Code(request.OwnerTypeCode))) throw Error("ADVERTISING_OWNER_TYPE_INVALID", "소유자 유형을 확인해 주세요."); ValidatePeriod(request.StartAt, request.EndAt); if (request.Priority < 0) throw Error("ADVERTISING_PRIORITY_INVALID", "우선순위는 0 이상이어야 합니다."); ValidateDestination(request.DestinationTypeCode, request.DestinationValue); if (request.PlacementCodes is null || request.PlacementCodes.Count == 0) throw Error("ADVERTISING_PLACEMENT_REQUIRED", "노출 위치를 한 곳 이상 선택해 주세요.");if(request.CategoryIds is null||request.AreaIds is null)throw Error("ADVERTISING_TARGET_REQUIRED","서비스·지역 대상 값이 누락되었습니다."); }
    private static void ValidateCreative(SaveAdvertisingCreativeRequest request) { if (string.IsNullOrWhiteSpace(request.Title)) throw Error("ADVERTISING_CREATIVE_TITLE_REQUIRED", "Creative 제목을 입력해 주세요."); if (request.DisplayOrder < 0) throw Error("ADVERTISING_CREATIVE_ORDER_INVALID", "표시순서는 0 이상이어야 합니다."); if (Code(request.StatusCode) is not ("ACTIVE" or "INACTIVE")) throw Error("ADVERTISING_CREATIVE_STATUS_INVALID", "Creative 상태를 확인해 주세요."); if (!string.IsNullOrWhiteSpace(request.DestinationTypeCode)) ValidateDestination(request.DestinationTypeCode, request.DestinationValue); }
    private static void ValidateContent(SaveManagedContentRequest request) { var type = Code(request.ContentTypeCode); if (!ContentTypes.Contains(type)) throw Error("CONTENT_TYPE_INVALID", "콘텐츠 유형을 확인해 주세요."); ValidateAudience(request.AudienceTypeCode); if (string.IsNullOrWhiteSpace(request.Title)) throw Error("CONTENT_TITLE_REQUIRED", "제목을 입력해 주세요."); if (type == "FAQ" && (string.IsNullOrWhiteSpace(request.QuestionText) || string.IsNullOrWhiteSpace(request.AnswerText))) throw Error("FAQ_QUESTION_ANSWER_REQUIRED", "FAQ 질문과 답변을 모두 입력해 주세요."); if (type != "FAQ" && string.IsNullOrWhiteSpace(request.BodyText)) throw Error("CONTENT_BODY_REQUIRED", "본문 내용을 입력해 주세요."); if (request.DisplayOrder < 0) throw Error("CONTENT_ORDER_INVALID", "표시순서는 0 이상이어야 합니다."); if(request.CategoryIds is null||request.AreaIds is null)throw Error("CONTENT_TARGET_REQUIRED","서비스·지역 대상 값이 누락되었습니다.");ValidatePeriod(request.StartAt, request.EndAt); ValidateDestination(request.DestinationTypeCode, request.DestinationValue); }
    private static string ValidateAudience(string value) { var code = Code(value); return Audiences.Contains(code) ? code : throw Error("AUDIENCE_INVALID", "대상 사용자를 확인해 주세요."); }
    private static void ValidateDestination(string typeValue, string? value) { var type = Code(typeValue); var destination = Clean(value); if (type == "NONE") { if (destination is not null) throw Error("DESTINATION_VALUE_NOT_ALLOWED", "연결 없음에서는 이동 경로를 입력할 수 없습니다."); return; } if (type == "INTERNAL_PATH") { if (destination is null || !destination.StartsWith('/') || destination.StartsWith("//") || destination.Contains('\\') || destination.Any(char.IsControl)) throw Error("DESTINATION_URL_INVALID", "내부 경로는 /로 시작하는 안전한 경로여야 합니다."); return; } if (type == "EXTERNAL_URL" && destination is not null && Uri.TryCreate(destination, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrWhiteSpace(uri.Host)) return; throw Error("DESTINATION_URL_INVALID", "외부 링크는 안전한 HTTPS 주소만 사용할 수 있습니다."); }
    private static void ValidatePeriod(DateTime start, DateTime? end) { if (start == default || end.HasValue && end.Value <= start) throw Error("PUBLISH_PERIOD_INVALID", "게시 시작·종료일시를 확인해 주세요."); }
    private static void ValidatePage(int page, int size) { if (page < 1 || size is < 1 or > MaximumPageSize) throw Error("PAGE_INVALID", "페이지와 페이지당 조회 건수를 확인해 주세요."); }
    private static void EnsureDistinct<T>(IReadOnlyList<T> values, string label) { if (values is null || values.Count != values.Distinct().Count()) throw Error("TARGET_DUPLICATED", $"{label}을 중복 선택할 수 없습니다."); }
    private static string Publication(string status, string review, DateTime start, DateTime? end, DateTime now) => review != "APPROVED" ? "REVIEW_REQUIRED" : status == "PAUSED" ? "PAUSED" : status != "ACTIVE" ? "DRAFT" : start > now ? "SCHEDULED" : end.HasValue && end <= now ? "EXPIRED" : "PUBLISHED";
    private static string Snapshot(object value) => JsonSerializer.Serialize(value);
    private static string Code(string value) => value.Trim().ToUpperInvariant();
    private static string? EmptyCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : Code(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AdvertisingContentException Error(string code, string message, int status = 400) => new(code, message, status);
    private static Guid DefaultPublicId(string prefix, DefaultManagedContent item)
    {
        var group = item.Type == "SAFETY_GUIDE" ? 0 : item.Type == "CATEGORY_GUIDE" ? 10 : 20;
        var seedId = group + item.DisplayOrder / 10;
        return Guid.Parse($"{prefix}{seedId:D4}-0000-4000-8000-{seedId:D12}");
    }
    private sealed record DefaultManagedContent(string Type, int DisplayOrder, string Title, string Body);
}
