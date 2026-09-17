using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.RelationshipBlocks;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Features.Trust;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Interior;

public sealed class CustomerInteriorService(SoodalLifeDbContext db,IPrivateFileStorage storage,InteriorProjectService core,InteriorContractWorkspaceService contractWorkspace,ICrossDomainFilePublicationResolver filePublication,ProviderTradingEligibilityService eligibility,IUserRelationshipBlockPolicy relationshipBlocks,ProviderWalletService walletService,InteriorContractExpiryService contractExpiry,TrustRecalculationRunner trustRecalculation)
{
    public async Task<IReadOnlyList<InteriorServiceResponse>> Services(CancellationToken token)
    {
        return await (from service in db.ServiceCategories.AsNoTracking()
                      join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                      join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                      where service.LevelCode=="SERVICE"&&service.StatusCode=="ACTIVE"&&((service.ExternalCode??string.Empty).StartsWith("INT-")||major.Name=="인테리어")
                      orderby major.SortOrder,middle.SortOrder,service.SortOrder
                      select new InteriorServiceResponse(service.PublicId,service.ExternalCode??string.Empty,$"{major.Name} > {middle.Name} > {service.Name}")).ToListAsync(token);
    }

    public async Task<CustomerInteriorHomeResponse> Home(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var all=await List(principal,token);
        var active=all.Where(x=>!x.Completed).Take(4).ToArray();var completed=all.Where(x=>x.Completed).Take(4).ToArray();
        var ids=await db.InteriorProjects.AsNoTracking().Where(x=>x.CustomerProfileId==identity.ProfileId).Select(x=>x.Id).ToArrayAsync(token);
        return new((await Services(token)).Count,all.Count,all.Count(x=>!x.Completed),all.Count(x=>x.Completed),
            await db.InteriorDefects.CountAsync(x=>ids.Contains(x.InteriorProjectId),token),
            await db.DisputeCases.CountAsync(x=>x.InteriorProjectId!=null&&ids.Contains(x.InteriorProjectId.Value),token),active,completed);
    }

    public async Task<IReadOnlyList<CustomerInteriorRequestCandidate>> RequestCandidates(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);
        return await (from request in db.ServiceRequests.AsNoTracking()
                      join service in db.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                      join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                      join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                      join area0 in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area0.Id into areas
                      from area in areas.DefaultIfEmpty()
                      join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)parent0.Id into parents
                      from parent in parents.DefaultIfEmpty()
                      where request.CustomerProfileId==identity.ProfileId&&((service.ExternalCode??string.Empty).StartsWith("INT-")||major.Name=="인테리어")
                      orderby request.CreatedAt descending
                      select new CustomerInteriorRequestCandidate(request.PublicId,request.Title,service.Name,area==null?"지역 미정":parent==null||parent.AreaName==area.AreaName?area.AreaName:parent.AreaName+" "+area.AreaName,request.StatusCode,request.CreatedAt,
                          db.InteriorProjects.Any(x=>x.ServiceRequestId==request.Id))).ToListAsync(token);
    }

    public async Task<CustomerInteriorProjectDetail> Create(CustomerCreateInteriorProjectRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);
        var existing=await db.InteriorProjectEvents.AsNoTracking().Where(x=>x.IdempotencyKey==input.IdempotencyKey)
            .Join(db.InteriorProjects.AsNoTracking(),x=>x.InteriorProjectId,x=>x.Id,(e,p)=>p.PublicId).SingleOrDefaultAsync(token);
        if(existing!=Guid.Empty)return await Detail(existing,principal,token);
        var request=await (from value in db.ServiceRequests
                           join service in db.ServiceCategories on value.CategoryId equals service.Id
                           join middle in db.ServiceCategories on service.ParentId equals middle.Id
                           join major in db.ServiceCategories on middle.ParentId equals major.Id
                           where value.PublicId==input.ServiceRequestId&&value.CustomerProfileId==identity.ProfileId&&((service.ExternalCode??string.Empty).StartsWith("INT-")||major.Name=="인테리어")
                           select new{value,service,major}).SingleOrDefaultAsync(token)??throw NotFound("INTERIOR_REQUEST_NOT_FOUND","인테리어 요청을 찾을 수 없습니다.");
        if(request.value.StatusCode is "DRAFT" or "CANCELLED")throw Conflict("INTERIOR_REQUEST_NOT_READY","공개된 인테리어 요청만 프로젝트로 시작할 수 있습니다.");
        var duplicate=await db.InteriorProjects.AsNoTracking().Where(x=>x.ServiceRequestId==request.value.Id).Select(x=>(Guid?)x.PublicId).SingleOrDefaultAsync(token);
        if(duplicate.HasValue)return await Detail(duplicate.Value,principal,token);
        var now=DateTime.UtcNow;var project=new InteriorProject{ServiceRequestId=request.value.Id,CustomerProfileId=identity.ProfileId,ServiceCategoryId=request.value.CategoryId,StatusCode="CONSULTATION",FeeAssessmentStatusCode="PENDING_SELECTION",CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};
        db.InteriorProjects.Add(project);await db.SaveChangesAsync(token);Event(project,"PROJECT_CREATED",input.IdempotencyKey,identity.UserId,new{requestId=input.ServiceRequestId},now);Audit(identity.UserId,"INTERIOR_PROJECT_CREATED",project.PublicId,new{requestId=input.ServiceRequestId},now);Outbox(project,"INTERIOR_PROJECT_CREATED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);
        return await Detail(project.PublicId,principal,token);
    }

    public async Task<IReadOnlyList<CustomerInteriorProjectListItem>> List(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var rows=await (from project in db.InteriorProjects.AsNoTracking()
            join request in db.ServiceRequests.AsNoTracking() on project.ServiceRequestId equals request.Id
            join service in db.ServiceCategories.AsNoTracking() on project.ServiceCategoryId equals service.Id
            join area0 in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area0.Id into areas from area in areas.DefaultIfEmpty()
            join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)parent0.Id into parents from parent in parents.DefaultIfEmpty()
            where project.CustomerProfileId==identity.ProfileId orderby project.UpdatedAt descending select new{project,service,area,parent}).ToListAsync(token);
        var result=new List<CustomerInteriorProjectListItem>();
        foreach(var row in rows)
        {
            var stages=await db.InteriorWorkStages.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderBy(x=>x.SequenceNo).Select(x=>new CustomerInteriorStageProgress(x.PublicId,x.SequenceNo,x.StageName,x.StatusCode,x.ProgressPercent)).ToListAsync(token);
            var visit=await db.InteriorSiteVisits.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderByDescending(x=>x.UpdatedAt).Select(x=>x.StatusCode).FirstOrDefaultAsync(token)??"NOT_SCHEDULED";
            var contract=await db.InteriorContracts.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderByDescending(x=>x.ContractVersion).Select(x=>x.StatusCode).FirstOrDefaultAsync(token)??"NOT_CREATED";
            var change=await (from x in db.InteriorContractChanges.AsNoTracking() join c in db.InteriorContracts.AsNoTracking() on x.InteriorContractId equals c.Id where c.InteriorProjectId==row.project.Id orderby x.RequestedAt descending select x.StatusCode).FirstOrDefaultAsync(token)??"NONE";
            var inspection=await (from x in db.InteriorStageInspections.AsNoTracking() join s in db.InteriorWorkStages.AsNoTracking() on x.WorkStageId equals s.Id where s.InteriorProjectId==row.project.Id orderby x.InspectedAt descending select x.InspectionStatusCode).FirstOrDefaultAsync(token)??"NONE";
            result.Add(new(row.project.PublicId,Number(row.project.PublicId),row.service.Name,row.area==null?"지역 미정":row.parent==null||row.parent.AreaName==row.area.AreaName?row.area.AreaName:$"{row.parent.AreaName} {row.area.AreaName}",row.project.StatusCode,Status(row.project.StatusCode),visit,contract,stages,change,inspection,row.project.ActualCompletionDate.HasValue,
                await db.InteriorDefects.CountAsync(x=>x.InteriorProjectId==row.project.Id,token),await db.DisputeCases.CountAsync(x=>x.InteriorProjectId==row.project.Id,token),row.project.UpdatedAt));
        }
        return result;
    }

    public async Task<CustomerInteriorProjectDetail> Detail(Guid id,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var row=await (from project in db.InteriorProjects.AsNoTracking()
            join request in db.ServiceRequests.AsNoTracking() on project.ServiceRequestId equals request.Id join service in db.ServiceCategories.AsNoTracking() on project.ServiceCategoryId equals service.Id
            join area0 in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area0.Id into areas from area in areas.DefaultIfEmpty()
            join parent0 in db.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)parent0.Id into parents from parent in parents.DefaultIfEmpty()
            where project.PublicId==id&&project.CustomerProfileId==identity.ProfileId select new{project,request,service,area,parent}).SingleOrDefaultAsync(token)??throw NotFound("INTERIOR_PROJECT_NOT_FOUND","인테리어 프로젝트를 찾을 수 없습니다.");
        var visits=new List<CustomerInteriorSiteVisit>();foreach(var visit in await db.InteriorSiteVisits.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderBy(x=>x.ScheduledStartAt).ToListAsync(token)){if(visit.StatusCode=="PROPOSED"&&row.project.SelectedSiteVisitProviderId!=visit.ProviderProfileId&&await relationshipBlocks.IsBlockedAsync(row.project.CustomerProfileId,visit.ProviderProfileId,token))continue;visits.Add(await MapVisit(row.project,visit,identity.UserId,token));}
        var quotes=await Quotes(row.project,token);var designs=await Designs(row.project,identity.UserId,token);var contract=await Contract(row.project,identity.UserId,token);var stages=await Stages(row.project,identity.UserId,token);
        var changes=await Changes(row.project,identity.UserId,token);var defects=await Defects(row.project,identity.UserId,token);var disputes=await Disputes(row.project,token);
        var events=await db.InteriorProjectEvents.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderByDescending(x=>x.OccurredAt).Select(x=>new CustomerInteriorEvent(x.PublicId,x.EventTypeCode,EventDisplay(x.EventTypeCode),x.OccurredAt)).ToListAsync(token);
        var history=await db.ServiceHistoryEntries.AsNoTracking().Where(x=>x.InteriorProjectId==row.project.Id).OrderByDescending(x=>x.OccurredAt).Select(x=>new{x.Title,x.Summary,x.OccurredAt}).FirstOrDefaultAsync(token);
        var tx=await db.Transactions.AsNoTracking().Where(x=>x.ServiceRequestId==row.project.ServiceRequestId&&x.CustomerProfileId==identity.ProfileId).Select(x=>(Guid?)x.PublicId).FirstOrDefaultAsync(token);
        CustomerInteriorProviderSelection? selection=null;
        if(row.project.CurrentQuoteRevisionId.HasValue&&row.project.ContractorSelectedAt.HasValue)
        {
            selection=await(from revision in db.QuoteRevisions.AsNoTracking() join quote in db.Quotes.AsNoTracking() on revision.QuoteId equals quote.Id join provider in db.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id where revision.Id==row.project.CurrentQuoteRevisionId&&provider.Id==row.project.SelectedContractorProviderId select new CustomerInteriorProviderSelection(provider.PublicId,provider.BusinessName,revision.PublicId,revision.RevisionNo,revision.TotalAmount,revision.CurrencyCode,revision.Summary,revision.Terms,row.project.ContractorSelectedAt.Value)).SingleOrDefaultAsync(token);
        }
        return new(row.project.PublicId,Number(row.project.PublicId),row.service.Name,row.request.Title,row.request.Description,row.area==null?"지역 미정":row.parent==null||row.parent.AreaName==row.area.AreaName?row.area.AreaName:$"{row.parent.AreaName} {row.area.AreaName}",row.request.DetailAddress,row.project.StatusCode,Status(row.project.StatusCode),row.project.SiteVisitProviderTrustScoreSnapshot,row.project.ContractorTrustScoreSnapshot,row.project.ProjectStartDate,row.project.ExpectedCompletionDate,
            row.project.ActualCompletionDate.HasValue?new(row.project.ActualCompletionDate.Value,history?.Title,history?.Summary,history?.OccurredAt):null,selection,
            row.project.ContractActionDueAt.HasValue&&row.project.ContractExpiryPhaseCode!=null?new(row.project.ContractExpiryPhaseCode,row.project.ContractActionDueAt.Value,row.project.ContractExpiryPausedAt.HasValue,row.project.ContractExpiryPauseReasonCode,row.project.ContractExpiryReminderSentAt):null,
            Version(row.project.RowVersion),visits,quotes,designs,contract,stages,changes,defects,disputes,events,tx.HasValue,tx);
    }

    public async Task<CustomerInteriorProjectDetail> SelectVisit(Guid projectId,Guid visitId,CustomerSelectSiteVisitRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);
        if(await db.InteriorProjectEvents.AnyAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token))return await Detail(projectId,principal,token);
        var visit=await db.InteriorSiteVisits.SingleOrDefaultAsync(x=>x.PublicId==visitId&&x.InteriorProjectId==project.Id,token)??throw NotFound("SITE_VISIT_NOT_FOUND","실측 후보를 찾을 수 없습니다.");
        if(visit.StatusCode!="PROPOSED"&&visit.StatusCode!="CONFIRMED")throw Conflict("SITE_VISIT_STATE_CONFLICT","제안 상태의 실측 일정만 선택할 수 있습니다.");
        await relationshipBlocks.EnsureAllowedAsync(project.CustomerProfileId,visit.ProviderProfileId,token);
        var now=DateTime.UtcNow;
        var request=await db.ServiceRequests.SingleAsync(x=>x.Id==project.ServiceRequestId,token);
        request.DetailAddress=input.DetailAddress.Trim();request.UpdatedAt=now;request.UpdatedByUserId=identity.UserId;
        foreach(var other in await db.InteriorSiteVisits.Where(x=>x.InteriorProjectId==project.Id&&x.ProviderProfileId!=visit.ProviderProfileId&&x.StatusCode=="CONFIRMED"&&x.CompletedAt==null).ToListAsync(token)){other.StatusCode="CANCELLED";other.UpdatedAt=now;other.UpdatedByUserId=identity.UserId;}
        foreach(var participant in await db.InteriorProjectParticipants.Where(x=>x.InteriorProjectId==project.Id&&x.RoleCode=="SITE_SURVEY"&&x.ProviderProfileId!=visit.ProviderProfileId&&x.StatusCode=="ACTIVE").ToListAsync(token)){participant.StatusCode="ENDED";participant.EffectiveTo=now;participant.IsPrimary=false;participant.UpdatedAt=now;participant.UpdatedByUserId=identity.UserId;}
        var selectedParticipant=await db.InteriorProjectParticipants.SingleOrDefaultAsync(x=>x.InteriorProjectId==project.Id&&x.ProviderProfileId==visit.ProviderProfileId&&x.RoleCode=="SITE_SURVEY",token);
        if(selectedParticipant is null){db.InteriorProjectParticipants.Add(new(){InteriorProjectId=project.Id,ProviderProfileId=visit.ProviderProfileId,RoleCode="SITE_SURVEY",EffectiveFrom=now,StatusCode="ACTIVE",IsPrimary=true,ScopeText="고객이 선택한 현장 실측",CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId});}
        else{selectedParticipant.StatusCode="ACTIVE";selectedParticipant.EffectiveFrom=now;selectedParticipant.EffectiveTo=null;selectedParticipant.IsPrimary=true;selectedParticipant.ScopeText="고객이 선택한 현장 실측";selectedParticipant.UpdatedAt=now;selectedParticipant.UpdatedByUserId=identity.UserId;}
        visit.StatusCode="CONFIRMED";visit.ConfirmedAt??=now;visit.CustomerConfirmedAt??=now;visit.UpdatedAt=now;visit.UpdatedByUserId=identity.UserId;
        project.SelectedSiteVisitProviderId=visit.ProviderProfileId;project.SiteVisitSelectedAt=now;project.SiteVisitProviderTrustScoreSnapshot=await CurrentTrust(visit.ProviderProfileId,token);project.StatusCode="SITE_VISIT_SCHEDULED";project.UpdatedAt=now;project.UpdatedByUserId=identity.UserId;
        Event(project,"SITE_VISIT_CONFIRMED",input.IdempotencyKey,identity.UserId,new{visitId},now);Audit(identity.UserId,"INTERIOR_SITE_VISIT_CONFIRMED",project.PublicId,new{visitId},now);Outbox(project,"INTERIOR_SITE_VISIT_CONFIRMED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);return await Detail(projectId,principal,token);
    }

    public async Task<CustomerInteriorProjectDetail> SelectProvider(Guid projectId,CustomerSelectInteriorProviderRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);
        var key=input.IdempotencyKey.Trim();
        var duplicate=await db.InteriorProjectEvents.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==key,token);
        if(duplicate is not null)
        {
            if(duplicate.InteriorProjectId!=await db.InteriorProjects.Where(x=>x.PublicId==projectId&&x.CustomerProfileId==identity.ProfileId).Select(x=>x.Id).SingleOrDefaultAsync(token)||
               duplicate.EventTypeCode!="CONTRACTOR_SELECTED"||!SelectionMatches(duplicate.EventDataJson,input.QuoteRevisionId))
                throw Conflict("IDEMPOTENCY_KEY_REUSED","동일한 중복 실행 방지 키를 다른 전문가 선택에 사용할 수 없습니다.");
            return await Detail(projectId,principal,token);
        }

        IDbContextTransaction? transaction=null;
        try
        {
            if(db.Database.IsRelational())transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var project=await OwnedProject(projectId,identity.ProfileId,token);
            if(project.SelectedContractorProviderId.HasValue||project.CurrentQuoteRevisionId.HasValue)
                throw Conflict("INTERIOR_CONTRACTOR_ALREADY_SELECTED","최종 시공 전문가는 다시 선택할 수 없습니다.");
            if(project.CurrentContractId.HasValue||project.StatusCode is "CONTRACT_PENDING" or "CONTRACTED" or "CONSTRUCTION" or "INSPECTION" or "COMPLETED" or "DEFECT_MANAGEMENT" or "CANCELLED")
                throw Conflict("INTERIOR_PROVIDER_SELECTION_CLOSED","현재 프로젝트 단계에서는 최종 시공 전문가를 선택할 수 없습니다.");
            if(project.StatusCode is not ("SITE_VISIT_COMPLETED" or "ESTIMATE_IN_PROGRESS" or "ESTIMATE_READY"))
                throw Conflict("INTERIOR_PROVIDER_SELECTION_NOT_READY","실측 완료 후 유효한 상세견적을 비교하여 선택할 수 있습니다.");

            ApplyVersion(project,input.RowVersion);
            var now=DateTime.UtcNow;
            var candidate=await(from revision in db.QuoteRevisions
                                join quote in db.Quotes on revision.QuoteId equals quote.Id
                                join provider in db.ProviderProfiles on quote.ProviderProfileId equals provider.Id
                                join request in db.ServiceRequests on quote.ServiceRequestId equals request.Id
                                where revision.PublicId==input.QuoteRevisionId&&quote.ServiceRequestId==project.ServiceRequestId
                                select new{Revision=revision,Quote=quote,Provider=provider,Request=request}).SingleOrDefaultAsync(token)
                          ??throw NotFound("INTERIOR_QUOTE_NOT_FOUND","이 프로젝트에서 선택할 수 있는 견적을 찾을 수 없습니다.");
            if(candidate.Quote.StatusCode!="SUBMITTED"||candidate.Quote.WithdrawnAt.HasValue||(candidate.Quote.ExpiresAt.HasValue&&candidate.Quote.ExpiresAt<=now)||candidate.Revision.ValidUntil<=now||candidate.Revision.RevisionPurposeCode is not ("POST_SITE_VISIT" or "CONTRACT_ESTIMATE"))
                throw Conflict("INTERIOR_QUOTE_NOT_SELECTABLE","철회·만료되었거나 계약 선택용이 아닌 견적은 선택할 수 없습니다.");
            var latest=await db.QuoteRevisions.Where(x=>x.QuoteId==candidate.Quote.Id).MaxAsync(x=>x.RevisionNo,token);
            if(candidate.Revision.RevisionNo!=latest)
                throw Conflict("INTERIOR_LATEST_QUOTE_REQUIRED","해당 전문가의 최신 견적 Version만 선택할 수 있습니다.");
            if(!candidate.Request.AdministrativeAreaId.HasValue)throw Conflict("REQUEST_AREA_REQUIRED","서비스 지역이 없는 프로젝트는 전문가를 선택할 수 없습니다.");
            await relationshipBlocks.EnsureAllowedAsync(project.CustomerProfileId,candidate.Provider.Id,token);
            var eligibilityResult=await eligibility.EvaluateAsync(candidate.Provider.Id,project.ServiceCategoryId,candidate.Request.AdministrativeAreaId.Value,token);
            if(!eligibilityResult.IsEligible)throw Conflict(eligibilityResult.ReasonCode??"PROVIDER_NOT_ELIGIBLE","현재 승인·서비스·지역·증빙 요건을 충족한 전문가만 선택할 수 있습니다.");

            var effectiveDate=DateOnly.FromDateTime(now);
            var feePolicy=await(from policy in db.CategoryFeePolicies.AsNoTracking()
                                join source in db.FeePolicies.AsNoTracking() on policy.SourceFeePolicyId equals source.Id
                                where policy.CategoryId==project.ServiceCategoryId&&policy.IsActive&&policy.TransactionTypeCode=="PROJECT"&&source.Code=="FEE-I1"&&source.IsActive&&
                                      policy.EffectiveFrom<=effectiveDate&&(!policy.EffectiveTo.HasValue||policy.EffectiveTo>=effectiveDate)
                                orderby policy.EffectiveFrom descending,policy.Id descending
                                select new{Policy=policy,Source=source}).FirstOrDefaultAsync(token)
                          ??throw Conflict("INTERIOR_FEE_POLICY_NOT_FOUND","적용 가능한 인테리어 전문가 수수료 정책 FEE-I1을 찾을 수 없습니다.");
            var feeTier=await db.FeePolicies.AsNoTracking()
                .Where(x=>x.IsActive&&x.Code.StartsWith("FEE-Q")&&x.CurrencyCode==candidate.Revision.CurrencyCode&&
                          x.MinBaseAmount.HasValue&&x.MaxBaseAmount.HasValue&&x.DisplayFeeAmount.HasValue&&
                          x.MinBaseAmount<=candidate.Revision.TotalAmount&&x.MaxBaseAmount>=candidate.Revision.TotalAmount&&
                          (!x.EffectiveFrom.HasValue||x.EffectiveFrom<=effectiveDate)&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=effectiveDate))
                .OrderBy(x=>x.MinBaseAmount).FirstOrDefaultAsync(token)
                ??throw Conflict("INTERIOR_FEE_TIER_NOT_FOUND","선택 견적금액에 적용할 FEE-Q1~FEE-Q7 수수료 구간을 찾을 수 없습니다.");
            var feeAmount=feeTier.DisplayFeeAmount!.Value;
            var balance=await walletService.GetBalanceAsync(candidate.Provider.PublicId,feeAmount,token);
            if(balance is null)throw Conflict("WALLET_NOT_FOUND","전문가 Wallet을 찾을 수 없습니다.");
            if(balance.StatusCode!="ACTIVE")throw Conflict("WALLET_NOT_ACTIVE","현재 사용할 수 없는 전문가 Wallet입니다.");
            if(!balance.HasSufficientBalance)throw Conflict("WALLET_INSUFFICIENT_BALANCE","최종 시공업체 선택 수수료를 차감할 Wallet 잔액이 부족합니다.");

            var categoryPolicy=await db.CategoryPolicies.AsNoTracking().SingleAsync(x=>x.Id==candidate.Request.CategoryPolicyId,token);
            var quoteItems=await db.QuoteItems.AsNoTracking().Where(x=>x.QuoteRevisionId==candidate.Revision.Id).OrderBy(x=>x.LineNo).ToListAsync(token);
            var trust=await CurrentTrust(candidate.Provider.Id,token);
            var transactionRecord=new TransactionRecord
            {
                ServiceRequestId=candidate.Request.Id,AcceptedQuoteRevisionId=candidate.Revision.Id,CustomerProfileId=project.CustomerProfileId,ProviderProfileId=candidate.Provider.Id,CategoryId=project.ServiceCategoryId,CategoryFeePolicyId=feePolicy.Policy.Id,
                StatusCode="CREATED",AgreedAmount=candidate.Revision.TotalAmount,CurrencyCode=candidate.Revision.CurrencyCode,
                QuoteSnapshotJson=JsonSerializer.Serialize(new{quoteId=candidate.Quote.PublicId,revisionId=candidate.Revision.PublicId,candidate.Revision.RevisionNo,candidate.Revision.Summary,candidate.Revision.Terms,candidate.Revision.SubtotalAmount,candidate.Revision.VatAmount,candidate.Revision.TotalAmount,candidate.Revision.CurrencyCode,candidate.Revision.EstimatedDurationText,candidate.Revision.ValidUntil,providerId=candidate.Provider.PublicId,candidate.Provider.BusinessName,items=quoteItems.Select(x=>new{x.LineNo,x.ItemName,x.Description,x.Quantity,x.UnitText,x.UnitPriceAmount,x.LineTotalAmount,x.CurrencyCode})}),
                CategoryPolicySnapshotJson=candidate.Request.PolicySnapshotJson,
                CompletionPolicySnapshotJson=JsonSerializer.Serialize(new{policyId=categoryPolicy.PublicId,categoryPolicy.PolicyVersion,categoryPolicy.RequiredCompletionPhotoCount,categoryPolicy.CompletionEvidenceRuleText}),
                FeePolicySnapshotJson=JsonSerializer.Serialize(new{policyId=feePolicy.Policy.PublicId,sourcePolicyCode=feePolicy.Source.Code,feePolicy.Policy.PolicyVersion,feePolicy.Policy.PolicyKindCode,feePolicy.Policy.TransactionTypeCode,calculationMethod="SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7",tierCode=feeTier.Code,minBaseAmount=feeTier.MinBaseAmount,maxBaseAmount=feeTier.MaxBaseAmount,calculatedFeeAmount=feeAmount,feePolicy.Policy.CurrencyCode,feePolicy.Policy.ChargeTimingText,feePolicy.Policy.RestoreRuleText,feePolicy.Policy.EffectiveFrom,feePolicy.Policy.EffectiveTo}),
                FeePolicyVersionSnapshot=feePolicy.Policy.PolicyVersion,FeePolicyKindSnapshot=feePolicy.Policy.PolicyKindCode,FeeTransactionTypeSnapshot=feePolicy.Policy.TransactionTypeCode,FeeCalculationMethodSnapshot="SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7",CalculatedFeeAmount=feeAmount,FeeCurrencyCode=feePolicy.Policy.CurrencyCode,FeeChargeTimingSnapshot=feePolicy.Policy.ChargeTimingText,FeeRestoreRuleSnapshot=feePolicy.Policy.RestoreRuleText,WarrantyDaysSnapshot=categoryPolicy.DefaultWarrantyDays,ProviderTrustScoreSnapshot=trust,
                CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId
            };
            db.Transactions.Add(transactionRecord);
            await db.SaveChangesAsync(token);
            var wallet=await db.ProviderWallets.SingleAsync(x=>x.ProviderProfileId==candidate.Provider.Id&&x.CurrencyCode==feePolicy.Policy.CurrencyCode,token);
            if(wallet.StatusCode!="ACTIVE")throw Conflict("WALLET_NOT_ACTIVE","현재 사용할 수 없는 전문가 Wallet입니다.");
            if(wallet.AvailableBalance<feeAmount)throw Conflict("WALLET_INSUFFICIENT_BALANCE","예상 수수료보다 잔액이 부족합니다.");
            wallet.AvailableBalance-=feeAmount;wallet.ReservedBalance+=feeAmount;wallet.UpdatedAt=now;wallet.UpdatedByUserId=identity.UserId;
            var reserveLedger=walletService.AddLedger(wallet,transactionRecord.Id,"RESERVE",-feeAmount,$"interior-fee-reserve:{project.PublicId:N}:{transactionRecord.PublicId:N}","인테리어 계약 성사 예상 수수료 예약","INTERIOR_FEE_RESERVATION",project.PublicId,null,now,identity.UserId);
            await db.SaveChangesAsync(token);
            project.ReservedFeeAmount=feeAmount;project.FeeReservationWalletId=wallet.Id;project.FeeReservationLedgerEntryId=reserveLedger.Id;project.FeeReservedAt=now;

            var otherPrimary=await db.InteriorProjectParticipants.AnyAsync(x=>x.InteriorProjectId==project.Id&&x.RoleCode=="PRIMARY_CONTRACTOR"&&x.ProviderProfileId!=candidate.Provider.Id&&x.StatusCode=="ACTIVE"&&(x.EffectiveTo==null||x.EffectiveTo>now),token);
            if(otherPrimary)throw Conflict("INTERIOR_PRIMARY_CONTRACTOR_CONFLICT","이미 다른 주 시공 참여자가 활성 상태입니다.");
            var participant=await db.InteriorProjectParticipants.SingleOrDefaultAsync(x=>x.InteriorProjectId==project.Id&&x.ProviderProfileId==candidate.Provider.Id&&x.RoleCode=="PRIMARY_CONTRACTOR",token);
            if(participant is null)
            {
                participant=new InteriorProjectParticipant{InteriorProjectId=project.Id,ProviderProfileId=candidate.Provider.Id,RoleCode="PRIMARY_CONTRACTOR",EffectiveFrom=now,StatusCode="ACTIVE",IsPrimary=true,ScopeText="고객 최종 선택 주 시공 전문가",CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};
                db.InteriorProjectParticipants.Add(participant);
            }
            else
            {
                participant.StatusCode="ACTIVE";participant.IsPrimary=true;participant.EffectiveFrom=now;participant.EffectiveTo=null;participant.ScopeText="고객 최종 선택 주 시공 전문가";participant.UpdatedAt=now;participant.UpdatedByUserId=identity.UserId;
            }
            project.SelectedContractorProviderId=candidate.Provider.Id;project.ContractorSelectedAt=now;project.CurrentQuoteRevisionId=candidate.Revision.Id;project.ContractorTrustScoreSnapshot=trust;project.FeeAssessmentStatusCode="RESERVED_PENDING_CONTRACT";project.StatusCode="ESTIMATE_READY";
            project.ContractExpiryPhaseCode="PROVIDER_SUBMISSION";project.ContractActionDueAt=now.AddDays(InteriorContractExpiryService.ConfirmationDays);project.ContractExpiryReminderSentAt=null;project.ContractExpiryPausedAt=null;project.ContractExpiryPauseReasonCode=null;project.ContractExpiredAt=null;project.UpdatedAt=now;project.UpdatedByUserId=identity.UserId;
            Event(project,"CONTRACTOR_SELECTED",key,identity.UserId,new{providerId=candidate.Provider.PublicId,quoteRevisionId=candidate.Revision.PublicId,candidate.Revision.RevisionNo,candidate.Revision.TotalAmount,candidate.Revision.CurrencyCode},now);
            Audit(identity.UserId,"INTERIOR_CONTRACTOR_SELECTED",project.PublicId,new{providerId=candidate.Provider.PublicId,quoteRevisionId=candidate.Revision.PublicId,candidate.Revision.RevisionNo},now);
            if(await db.NotificationTemplates.AsNoTracking().AnyAsync(x=>x.EventTypeCode=="CONTRACTOR_SELECTED"&&x.IsActive,token))
                db.OutboxEvents.Add(new(){AggregateType="InteriorProject",AggregatePublicId=project.PublicId,EventType="CONTRACTOR_SELECTED",PayloadJson=JsonSerializer.Serialize(new{projectId=project.PublicId,providerId=candidate.Provider.PublicId}),OccurredAt=now,AvailableAt=now,IdempotencyKey=$"interior:{key}",CreatedByUserId=identity.UserId});
            await db.SaveChangesAsync(token);
            if(transaction is not null)await transaction.CommitAsync(token);
            return await Detail(projectId,principal,token);
        }
        catch(DbUpdateConcurrencyException)
        {
            if(transaction is not null)await transaction.RollbackAsync(token);
            throw Conflict("INTERIOR_PROVIDER_SELECTION_CONFLICT","프로젝트가 변경되었습니다. 최신 내용을 확인한 뒤 다시 선택해 주세요.");
        }
        finally
        {
            if(transaction is not null)await transaction.DisposeAsync();
        }
    }

    public async Task<CustomerInteriorProjectDetail> Agree(Guid projectId,Guid contractId,CustomerAgreeInteriorContractRequest input,ClaimsPrincipal principal,CancellationToken token)
    {await contractWorkspace.ReviewByCustomer(projectId,contractId,new ReviewInteriorContractRecordRequest(true,null,input.IdempotencyKey),principal,token);return await Detail(projectId,principal,token);}

    public async Task<CustomerInteriorProjectDetail> ReleaseProviderSelection(Guid projectId,CustomerReleaseInteriorProviderSelectionRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);
        if(await db.InteriorProjectEvents.AnyAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token))return await Detail(projectId,principal,token);
        if(project.FeeAssessmentStatusCode!="RESERVED_PENDING_CONTRACT"||!project.SelectedContractorProviderId.HasValue)throw Conflict("INTERIOR_PROVIDER_SELECTION_NOT_RELEASABLE","예약 중인 전문가 선택이 없습니다.");
        if(await db.DisputeCases.AnyAsync(x=>x.InteriorProjectId==project.Id&&x.StatusCode!="RESOLVED"&&x.StatusCode!="CLOSED",token))throw Conflict("INTERIOR_ACTIVE_DISPUTE_REQUIRES_REVIEW","진행 중인 분쟁이 있어 전문가 선택을 즉시 해제할 수 없습니다.");
        Event(project,"PROVIDER_SELECTION_RELEASE_REQUESTED",input.IdempotencyKey,identity.UserId,new{input.CancelProject},DateTime.UtcNow);
        await contractExpiry.ReleaseSelectionAsync(project,input.CancelProject?"CUSTOMER_PROJECT_CANCELLED":"CUSTOMER_PROVIDER_SELECTION_RELEASED",input.CancelProject,identity.UserId,DateTime.UtcNow,token);
        return await Detail(projectId,principal,token);
    }

    public async Task<CustomerInteriorProjectDetail> ReviewContract(Guid projectId,Guid contractId,ReviewInteriorContractRecordRequest input,ClaimsPrincipal principal,CancellationToken token)
    {await contractWorkspace.ReviewByCustomer(projectId,contractId,input,principal,token);return await Detail(projectId,principal,token);}
    public async Task<Guid> ConfirmPayment(Guid projectId,Guid planId,CustomerConfirmInteriorPaymentRequest input,ClaimsPrincipal principal,CancellationToken token)
    {var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);if(!await(from plan in db.InteriorPaymentPlans.AsNoTracking() join contract in db.InteriorContracts.AsNoTracking() on plan.InteriorContractId equals contract.Id where plan.PublicId==planId&&contract.InteriorProjectId==project.Id select plan.Id).AnyAsync(token))throw NotFound("PAYMENT_PLAN_NOT_FOUND","지급계획을 찾을 수 없습니다.");if(input.EvidenceFileId.HasValue&&!await db.Files.AsNoTracking().AnyAsync(x=>x.PublicId==input.EvidenceFileId&&x.UploadedByUserId==identity.UserId&&x.StatusCode=="ACTIVE",token))throw NotFound("INTERIOR_FILE_NOT_FOUND","사용할 수 없는 지급 증빙파일입니다.");return await core.ConfirmPayment(planId,new ConfirmInteriorPaymentRequest(input.ConfirmationTypeCode,input.Amount,input.ConfirmedAt,input.EvidenceFileId,input.Note,input.IdempotencyKey),principal,token);}
    public async Task<CustomerInteriorProjectDetail> DecideChange(Guid projectId,Guid changeId,CustomerDecideInteriorChangeRequest input,ClaimsPrincipal principal,CancellationToken token)
    {var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);if(!await(from change in db.InteriorContractChanges.AsNoTracking() join contract in db.InteriorContracts.AsNoTracking() on change.InteriorContractId equals contract.Id where change.PublicId==changeId&&contract.InteriorProjectId==project.Id select change.Id).AnyAsync(token))throw NotFound("CONTRACT_CHANGE_NOT_FOUND","계약 변경을 찾을 수 없습니다.");await core.DecideChange(changeId,new DecideInteriorChangeRequest(input.Approve,input.IdempotencyKey,null),principal,token);return await Detail(projectId,principal,token);}

    public async Task<InteriorInspectionAcknowledgementResponse> AcknowledgeInspection(Guid projectId,Guid inspectionId,AcknowledgeInteriorInspectionRequest input,ClaimsPrincipal principal,CancellationToken token)
    {var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);var inspection=await(from value in db.InteriorStageInspections join stage in db.InteriorWorkStages on value.WorkStageId equals stage.Id where value.PublicId==inspectionId&&stage.InteriorProjectId==project.Id select value).SingleOrDefaultAsync(token)??throw NotFound("INTERIOR_INSPECTION_NOT_FOUND","검사결과를 찾을 수 없습니다.");var duplicate=await db.InteriorStageInspectionAcknowledgements.SingleOrDefaultAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token);if(duplicate is not null)return new(duplicate.PublicId,inspection.PublicId,duplicate.AcknowledgedAt,duplicate.Comment,Version(duplicate.RowVersion));ApplyVersion(inspection,input.RowVersion);var now=DateTime.UtcNow;var item=new InteriorStageInspectionAcknowledgement{StageInspectionId=inspection.Id,CustomerProfileId=identity.ProfileId,AcknowledgedAt=now,Comment=Clean(input.Comment),IdempotencyKey=input.IdempotencyKey.Trim(),CreatedAt=now,CreatedByUserId=identity.UserId};db.InteriorStageInspectionAcknowledgements.Add(item);Event(project,"INSPECTION_RESULT_ACKNOWLEDGED",input.IdempotencyKey,identity.UserId,new{inspectionId,meaning="RESULT_RECEIVED_ONLY"},now);Audit(identity.UserId,"INTERIOR_INSPECTION_RESULT_ACKNOWLEDGED",project.PublicId,new{inspectionId,meaning="RESULT_RECEIVED_ONLY"},now);Outbox(project,"INTERIOR_INSPECTION_RESULT_ACKNOWLEDGED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);return new(item.PublicId,inspection.PublicId,item.AcknowledgedAt,item.Comment,Version(item.RowVersion));}

    public async Task<CustomerInteriorProjectDetail> AcknowledgeCompletion(Guid projectId,AcknowledgeInteriorProjectCompletionRequest input,ClaimsPrincipal principal,CancellationToken token)
        => await AcknowledgeCompletionLegacy(projectId,input,principal,token);

    private async Task<CustomerInteriorProjectDetail> AcknowledgeCompletionLegacy(Guid projectId,AcknowledgeInteriorProjectCompletionRequest input,ClaimsPrincipal principal,CancellationToken token)
    {var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);if(await db.InteriorProjectEvents.AnyAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token))return await Detail(projectId,principal,token);if(!project.ProviderCompletionSubmittedAt.HasValue)throw Conflict("INTERIOR_PROVIDER_COMPLETION_REQUIRED","주 시공 전문가 완료보고 후 확인할 수 있습니다.");var stages=await db.InteriorWorkStages.Where(x=>x.InteriorProjectId==project.Id).OrderBy(x=>x.SequenceNo).ToListAsync(token);if(stages.Count==0||stages.Any(x=>x.StatusCode!="COMPLETED"||x.ProgressPercent!=100))throw Conflict("INTERIOR_WORK_STAGES_INCOMPLETE","모든 필수 공정이 완료되어야 합니다.");var finalStage=stages[^1];if(!await db.InteriorStageInspections.AnyAsync(x=>x.WorkStageId==finalStage.Id&&x.InspectionStatusCode=="PASSED",token))throw Conflict("INTERIOR_FINAL_INSPECTION_REQUIRED","최종검사 완료 후 확인할 수 있습니다.");ApplyVersion(project,input.RowVersion);var now=DateTime.UtcNow;project.CustomerCompletionAcknowledgedAt=now;project.CustomerCompletionAcknowledgedByUserId=identity.UserId;project.CustomerCompletionComment=Clean(input.Comment);project.StatusCode="COMPLETED";project.ActualCompletionDate=DateOnly.FromDateTime(now);project.UpdatedAt=now;project.UpdatedByUserId=identity.UserId;var contract=await db.InteriorContracts.AsNoTracking().Where(x=>x.InteriorProjectId==project.Id).OrderByDescending(x=>x.ContractVersion).FirstOrDefaultAsync(token);var provider=contract==null?null:await db.ProviderProfiles.AsNoTracking().SingleAsync(x=>x.Id==contract.ProviderProfileId,token);var category=await db.ServiceCategories.AsNoTracking().SingleAsync(x=>x.Id==project.ServiceCategoryId,token);var historyKey=$"interior-completed:{project.PublicId:N}";if(!await db.ServiceHistoryEntries.AnyAsync(x=>x.IdempotencyKey==historyKey,token))db.ServiceHistoryEntries.Add(new(){CustomerProfileId=project.CustomerProfileId,InteriorProjectId=project.Id,EventTypeCode="INTERIOR_COMPLETED",Title="인테리어 공사 완료",Summary=Clean(input.Comment)??project.ProviderCompletionSummary??"고객 완료 확인",ProviderNameSnapshot=provider?.BusinessName,CategoryNameSnapshot=category.Name,TotalAmountSnapshot=contract?.ContractAmount,CurrencyCode=contract?.CurrencyCode,CompletedAtSnapshot=now,SnapshotJson=JsonSerializer.Serialize(new{projectId=project.PublicId,contractVersion=contract?.ContractVersion,contractAmount=contract?.ContractAmount,stages=stages.Select(x=>new{x.StageName,x.ProgressPercent,x.StatusCode})}),OccurredAt=now,IdempotencyKey=historyKey,CreatedAt=now,CreatedByUserId=identity.UserId});foreach(var participant in await db.InteriorProjectParticipants.Where(x=>x.InteriorProjectId==project.Id&&x.StatusCode=="ACTIVE"&&x.RoleCode!="AFTER_SERVICE").ToListAsync(token)){participant.StatusCode="ENDED";participant.EffectiveTo??=now;participant.UpdatedAt=now;participant.UpdatedByUserId=identity.UserId;}Event(project,"CUSTOMER_COMPLETION_ACKNOWLEDGED",input.IdempotencyKey,identity.UserId,new{meaning="CUSTOMER_CONFIRMED_AND_PROJECT_COMPLETED"},now);Audit(identity.UserId,"INTERIOR_PROJECT_COMPLETED_BY_PARTIES",project.PublicId,new{project.StatusCode,project.ActualCompletionDate},now);Outbox(project,"INTERIOR_PROJECT_COMPLETED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);if(provider is not null)await trustRecalculation.RunAsync(provider.PublicId,"INTERIOR_COMPLETED",project.PublicId,$"trust-interior-completed:{project.PublicId:N}",token);return await Detail(projectId,principal,token);}

    public async Task<CustomerInteriorProjectDetail> CreateDefect(Guid projectId,CustomerCreateInteriorDefectRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);if(!project.SelectedContractorProviderId.HasValue)throw Conflict("INTERIOR_CONTRACTOR_REQUIRED","시공 전문가가 확정된 프로젝트만 하자를 접수할 수 있습니다.");
        var existing=await db.AfterServiceCases.AsNoTracking().Where(x=>x.IdempotencyKey==input.IdempotencyKey&&x.InteriorProjectId==project.Id).Select(x=>x.PublicId).SingleOrDefaultAsync(token);if(existing!=Guid.Empty)return await Detail(projectId,principal,token);
        long? stage=null;if(input.WorkStageId.HasValue)stage=await db.InteriorWorkStages.Where(x=>x.PublicId==input.WorkStageId&&x.InteriorProjectId==project.Id).Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw NotFound("WORK_STAGE_NOT_FOUND","공정을 찾을 수 없습니다.");
        var now=DateTime.UtcNow;var warranty=await db.InteriorContracts.AsNoTracking().Where(x=>x.InteriorProjectId==project.Id).OrderByDescending(x=>x.ContractVersion).Select(x=>x.WarrantySnapshotJson).FirstOrDefaultAsync(token);
        var item=new AfterServiceCase{InteriorProjectId=project.Id,CustomerProfileId=project.CustomerProfileId,ProviderProfileId=project.SelectedContractorProviderId.Value,ReportedByUserId=identity.UserId,StatusCode="RECEIVED",Subject=input.Subject.Trim(),Description=input.Description.Trim(),RequestDetails=input.Location.Trim(),ReceivedAt=now,WarrantyStartDate=project.ActualCompletionDate,IsWithinWarranty=null,LastActionAt=now,IdempotencyKey=input.IdempotencyKey.Trim(),CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};
        db.AfterServiceCases.Add(item);await db.SaveChangesAsync(token);db.AfterServiceActions.Add(new(){AfterServiceCaseId=item.Id,ToStatusCode="RECEIVED",ActionTypeCode="RECEIVED",ActionNote="고객 인테리어 하자 접수",OccurredAt=now,ActorUserId=identity.UserId,IdempotencyKey=$"{input.IdempotencyKey}:received"});
        var defect=new InteriorDefect{InteriorProjectId=project.Id,AfterServiceCaseId=item.Id,WorkStageId=stage,ContractVersion=input.ContractVersion,DefectLocationText=input.Location.Trim(),DefectDescription=input.Description.Trim(),CreatedAt=now,CreatedByUserId=identity.UserId};db.InteriorDefects.Add(defect);
        foreach(var file in await OwnedFiles(input.FileIds,identity.UserId,token))db.AfterServiceFiles.Add(new(){AfterServiceCaseId=item.Id,FileId=file.Id,RoleCode="DEFECT_EVIDENCE",Description="고객 하자 증빙",CreatedAt=now,CreatedByUserId=identity.UserId});
        project.StatusCode="DEFECT_MANAGEMENT";project.UpdatedAt=now;project.UpdatedByUserId=identity.UserId;Event(project,"DEFECT_REPORTED",input.IdempotencyKey,identity.UserId,new{afterServiceId=item.PublicId,defectId=defect.PublicId,warrantySnapshotPresent=!string.IsNullOrWhiteSpace(warranty)},now);Audit(identity.UserId,"INTERIOR_DEFECT_REPORTED",project.PublicId,new{afterServiceId=item.PublicId},now);Outbox(project,"INTERIOR_DEFECT_REPORTED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);return await Detail(projectId,principal,token);
    }

    public async Task<CustomerInteriorProjectDetail> CreateDispute(Guid projectId,CustomerCreateInteriorDisputeRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);if(!project.SelectedContractorProviderId.HasValue)throw Conflict("INTERIOR_CONTRACTOR_REQUIRED","시공 전문가가 확정된 프로젝트만 분쟁을 접수할 수 있습니다.");
        var duplicate=await db.DisputeActions.AsNoTracking().Where(x=>x.IdempotencyKey==input.IdempotencyKey).Join(db.DisputeCases.AsNoTracking(),x=>x.DisputeCaseId,x=>x.Id,(a,d)=>d.PublicId).SingleOrDefaultAsync(token);if(duplicate!=Guid.Empty)return await Detail(projectId,principal,token);
        long? change=null;if(input.ContractChangeId.HasValue)change=await (from x in db.InteriorContractChanges join c in db.InteriorContracts on x.InteriorContractId equals c.Id where x.PublicId==input.ContractChangeId&&c.InteriorProjectId==project.Id select(long?)x.Id).SingleOrDefaultAsync(token)??throw NotFound("CONTRACT_CHANGE_NOT_FOUND","계약 변경을 찾을 수 없습니다.");
        long? stage=null;if(input.WorkStageId.HasValue)stage=await db.InteriorWorkStages.Where(x=>x.PublicId==input.WorkStageId&&x.InteriorProjectId==project.Id).Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)??throw NotFound("WORK_STAGE_NOT_FOUND","공정을 찾을 수 없습니다.");
        var providerUser=await db.ProviderProfiles.Where(x=>x.Id==project.SelectedContractorProviderId).Select(x=>x.UserId).SingleAsync(token);var now=DateTime.UtcNow;
        var dispute=new DisputeCase{InteriorProjectId=project.Id,InteriorContractChangeId=change,InteriorWorkStageId=stage,ApplicantUserId=identity.UserId,CounterpartyUserId=providerUser,Subject=input.Subject.Trim(),Description=input.Description.Trim(),StatusCode="OPEN",ReceivedAt=now,LastActionAt=now,CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};db.DisputeCases.Add(dispute);await db.SaveChangesAsync(token);
        db.DisputeActions.Add(new(){DisputeCaseId=dispute.Id,ActionTypeCode="CREATED",ToStatusCode="OPEN",ActionNote="고객 인테리어 분쟁 접수",OccurredAt=now,ActorUserId=identity.UserId,IdempotencyKey=input.IdempotencyKey.Trim()});
        foreach(var file in await OwnedFiles(input.FileIds,identity.UserId,token))db.DisputeEvidence.Add(new(){DisputeCaseId=dispute.Id,FileId=file.Id,SubmittedByUserId=identity.UserId,SourceTypeCode="OTHER",Description="고객 제출 증빙",StatusCode="ACTIVE",SubmittedAt=now,CreatedAt=now,CreatedByUserId=identity.UserId});
        Event(project,"DISPUTE_REPORTED",input.IdempotencyKey,identity.UserId,new{disputeId=dispute.PublicId},now);Audit(identity.UserId,"INTERIOR_DISPUTE_REPORTED",project.PublicId,new{disputeId=dispute.PublicId},now);Outbox(project,"INTERIOR_DISPUTE_REPORTED",input.IdempotencyKey,identity.UserId,now);await db.SaveChangesAsync(token);return await Detail(projectId,principal,token);
    }

    public async Task<(Stream Stream,string ContentType,string FileName)> OpenFile(Guid projectId,Guid fileId,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var project=await OwnedProject(projectId,identity.ProfileId,token);
        var linked=await ProjectFileIds(project.Id).ContainsAsync(fileId,token);if(!linked)throw NotFound("INTERIOR_FILE_NOT_FOUND","프로젝트 파일을 찾을 수 없습니다.");
        var file=await db.Files.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==fileId&&x.StatusCode=="ACTIVE",token)??throw NotFound("INTERIOR_FILE_NOT_FOUND","프로젝트 파일을 찾을 수 없습니다.");
        var publication=await filePublication.ResolveAsync(file,identity.UserId,true,true,token);if(!publication.Allowed)throw new InteriorBusinessException(403,"INTERIOR_FILE_PRIVACY_BLOCKED",publication.Message);var published=publication.PublishedFile!;
        return(await storage.OpenReadAsync(published.StorageKey,token),published.ContentType,published.OriginalFileName);
    }

    public async Task<CustomerInteriorUploadResponse> UploadEvidence(Guid projectId,IFormFile upload,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);await OwnedProject(projectId,identity.ProfileId,token);if(upload.Length is <=0 or >10485760)throw Conflict("INTERIOR_FILE_SIZE_INVALID","파일은 10MB 이하만 등록할 수 있습니다.");
        var name=Path.GetFileName(upload.FileName);if(string.IsNullOrWhiteSpace(name)||name!=upload.FileName||name.Length>255)throw Conflict("INTERIOR_FILE_NAME_INVALID","안전한 파일명을 사용해 주세요.");var ext=Path.GetExtension(name).ToLowerInvariant();
        var allowed=(upload.ContentType.ToLowerInvariant(),ext) switch{("application/pdf",".pdf")=>true,("image/jpeg",".jpg" or ".jpeg")=>true,("image/png",".png")=>true,_=>false};if(!allowed)throw Conflict("INTERIOR_FILE_TYPE_INVALID","PDF, JPG, PNG 파일만 등록할 수 있습니다.");
        await using var source=upload.OpenReadStream();using var memory=new MemoryStream();await source.CopyToAsync(memory,token);var bytes=memory.ToArray();if(!ValidSignature(upload.ContentType,bytes))throw Conflict("INTERIOR_FILE_SIGNATURE_INVALID","파일 형식과 실제 내용이 일치하지 않습니다.");
        var now=DateTime.UtcNow;var key=$"interior/{projectId:N}/{Guid.NewGuid():N}{ext}";var file=new StoredFile{PurposeCode="INTERIOR_EVIDENCE",StorageContainer="development-private",StorageKey=key,StorageKeyHash=SHA256.HashData(Encoding.UTF8.GetBytes(key)),OriginalFileName=name,ContentType=upload.ContentType.ToLowerInvariant(),SizeBytes=bytes.Length,Sha256Hex=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),StatusCode="PENDING",MalwareScanStatusCode=FilePrivacyCodes.NotIntegrated,PrivacyInspectionStatusCode=FilePrivacyCodes.NotIntegrated,SanitizationStatusCode=FilePrivacyCodes.NotIntegrated,UploadedByUserId=identity.UserId,CreatedAt=now};db.Files.Add(file);await db.SaveChangesAsync(token);
        try{memory.Position=0;await storage.SaveAsync(key,memory,token);file.StatusCode="ACTIVE";file.ActivatedAt=now;file.ScanResultText=FilePrivacyCodes.NotIntegrated;await db.SaveChangesAsync(token);return new(file.PublicId,file.OriginalFileName,file.ContentType,file.SizeBytes,FilePrivacyCodes.NotIntegrated);}catch{await storage.DeleteIfExistsAsync(key,token);throw;}
    }

    private async Task<CustomerInteriorSiteVisit> MapVisit(InteriorProject project,InteriorSiteVisit visit,long viewerUserId,CancellationToken token)
    {
        var provider=await db.ProviderProfiles.AsNoTracking().SingleAsync(x=>x.Id==visit.ProviderProfileId,token);var trust=await CurrentTrust(provider.Id,token);var reviews=await PublicReviewCount(provider.Id,token);
        var measurements=await db.InteriorSiteVisitMeasurements.AsNoTracking().Where(x=>x.SiteVisitId==visit.Id).OrderBy(x=>x.Id).Select(x=>new CustomerInteriorMeasurement(x.MeasurementKey,x.MeasurementValue,x.MeasurementText,x.UnitText,x.LocationText,x.NoteText)).ToListAsync(token);
        var fileRows=await (from link in db.InteriorSiteVisitFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.SiteVisitId==visit.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose=link.PurposeCode}).ToListAsync(token);var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);
        var proposal=ParseSiteVisitProposal(visit.AccessConditionText);
        return new(visit.PublicId,provider.PublicId,provider.BusinessName,visit.StatusCode,VisitStatus(visit.StatusCode),visit.ScheduledStartAt,visit.ScheduledEndAt,visit.ConfirmedAt,visit.CompletedAt,proposal.Fee,proposal.Terms,trust,Trust(trust),reviews,project.SelectedSiteVisitProviderId==provider.Id,visit.StatusCode=="PROPOSED"&&project.SelectedSiteVisitProviderId==null,visit.MeasurementSummaryText,visit.ConstraintText,visit.RiskNoteText,measurements,files);
    }

    private static (decimal? Fee,string? Terms) ParseSiteVisitProposal(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return(null,null);
        const string prefix="PROPOSED_FEE=";
        if(!value.StartsWith(prefix,StringComparison.Ordinal))return(null,value);
        var separator=value.IndexOf('\n');
        var feeText=separator<0?value[prefix.Length..]:value[prefix.Length..separator];
        var terms=separator<0?null:value[(separator+1)..].Trim();
        return(decimal.TryParse(feeText,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var fee)?fee:null,string.IsNullOrWhiteSpace(terms)?null:terms);
    }

    private async Task<IReadOnlyList<CustomerInteriorQuote>> Quotes(InteriorProject project,CancellationToken token)
    {
        var now=DateTime.UtcNow;var areaId=await db.ServiceRequests.AsNoTracking().Where(x=>x.Id==project.ServiceRequestId).Select(x=>x.AdministrativeAreaId).SingleAsync(token);
        var rows=await (from quote in db.Quotes.AsNoTracking() join revision in db.QuoteRevisions.AsNoTracking() on quote.Id equals revision.QuoteId join provider in db.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id where quote.ServiceRequestId==project.ServiceRequestId&&quote.StatusCode=="SUBMITTED"&&!quote.WithdrawnAt.HasValue&&(quote.ExpiresAt==null||quote.ExpiresAt>now)&&revision.ValidUntil>now&&revision.RevisionNo==db.QuoteRevisions.Where(x=>x.QuoteId==quote.Id).Max(x=>x.RevisionNo) orderby revision.SubmittedAt descending select new{quote,revision,provider}).ToListAsync(token);var result=new List<CustomerInteriorQuote>();
        foreach(var row in rows)
        {
            if(!areaId.HasValue||(await eligibility.EvaluateAsync(row.provider.Id,project.ServiceCategoryId,areaId.Value,token)).IsEligible==false||await relationshipBlocks.IsBlockedAsync(project.CustomerProfileId,row.provider.Id,token))continue;
            var items=await db.QuoteItems.AsNoTracking().Where(x=>x.QuoteRevisionId==row.revision.Id).OrderBy(x=>x.LineNo).Select(x=>new CustomerInteriorQuoteItem(x.LineNo,x.ItemName,x.Description,x.Quantity,x.UnitText,x.UnitPriceAmount,x.LineTotalAmount,x.WorkTradeText,x.SpaceText,x.MaterialSpecText,x.LaborNoteText)).ToListAsync(token);var trust=await CurrentTrust(row.provider.Id,token);
            var design=await(from value in db.InteriorDesignVersions.AsNoTracking() join provider in db.ProviderProfiles.AsNoTracking() on value.CreatedByUserId equals provider.UserId where value.InteriorProjectId==project.Id&&provider.Id==row.provider.Id orderby value.VersionNo descending select new{value.VersionNo,value.StatusCode}).FirstOrDefaultAsync(token);
            var selectable=project.SelectedContractorProviderId==null&&project.StatusCode is ("SITE_VISIT_COMPLETED" or "ESTIMATE_IN_PROGRESS" or "ESTIMATE_READY")&&row.revision.RevisionPurposeCode is ("POST_SITE_VISIT" or "CONTRACT_ESTIMATE");
            result.Add(new(row.quote.PublicId,row.revision.PublicId,row.revision.RevisionNo,row.revision.RevisionPurposeCode??"PRELIMINARY",Purpose(row.revision.RevisionPurposeCode),row.provider.PublicId,row.provider.BusinessName,row.revision.TotalAmount,row.revision.VatAmount,row.revision.CurrencyCode,row.revision.EstimatedDurationText,row.revision.SubmittedAt,row.revision.AvailableStartAt,row.revision.ValidUntil,row.revision.Summary,row.revision.Terms,trust,Trust(trust),await PublicReviewCount(row.provider.Id,token),design?.VersionNo,design?.StatusCode,project.CurrentQuoteRevisionId==row.revision.Id,selectable,items));
        }
        return result;
    }

    private async Task<IReadOnlyList<CustomerInteriorDesign>> Designs(InteriorProject project,long viewerUserId,CancellationToken token)
    {var rows=await db.InteriorDesignVersions.AsNoTracking().Where(x=>x.InteriorProjectId==project.Id).OrderByDescending(x=>x.VersionNo).ToListAsync(token);var result=new List<CustomerInteriorDesign>();foreach(var row in rows){var fileRows=await(from link in db.InteriorDesignFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.DesignVersionId==row.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose=link.PurposeCode}).ToListAsync(token);var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);result.Add(new(row.PublicId,row.VersionNo,row.Title,row.Description,row.StatusCode,row.CustomerApprovedAt,files));}return result;}

    private async Task<CustomerInteriorContract?> Contract(InteriorProject project,long userId,CancellationToken token)
    {
        var row=await db.InteriorContracts.AsNoTracking().Where(x=>x.InteriorProjectId==project.Id).OrderByDescending(x=>x.ContractVersion).FirstOrDefaultAsync(token);if(row is null)return null;var provider=await db.ProviderProfiles.AsNoTracking().Where(x=>x.Id==row.ProviderProfileId).Select(x=>x.BusinessName).SingleAsync(token);
        var versions=await db.InteriorContractVersions.AsNoTracking().Where(x=>x.InteriorContractId==row.Id).OrderBy(x=>x.VersionNo).Select(x=>new CustomerInteriorContractVersion(x.VersionNo,x.ContractAmount,x.CurrencyCode,x.ScopeSnapshotJson,x.ScheduleSnapshotJson,x.PaymentPlanSnapshotJson,x.WarrantySnapshotJson,x.CustomerAgreedAt,x.ProviderAgreedAt,x.CreatedAt)).ToListAsync(token);
        var plans=new List<CustomerInteriorPaymentPlan>();
        foreach(var plan in await db.InteriorPaymentPlans.AsNoTracking().Where(x=>x.InteriorContractId==row.Id).OrderBy(x=>x.SequenceNo).ToListAsync(token))
        {
            var confirms=new List<CustomerInteriorPaymentConfirmation>();
            foreach(var confirmation in await db.InteriorPaymentConfirmations.AsNoTracking().Where(x=>x.PaymentPlanId==plan.Id).OrderBy(x=>x.ConfirmedAt).ToListAsync(token))
            {
                CustomerInteriorFile? evidence=null;
                if(confirmation.EvidenceFileId.HasValue)
                {
                    var file=await db.Files.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==confirmation.EvidenceFileId.Value&&x.StatusCode=="ACTIVE",token);
                    if(file is not null)evidence=(await PublishFiles(project.PublicId,userId,[(file,"PAYMENT_EVIDENCE")],token)).Single();
                }
                confirms.Add(new(confirmation.PublicId,confirmation.ConfirmationTypeCode,confirmation.ConfirmedAmount,confirmation.ConfirmedAt,confirmation.NoteText,evidence));
            }
            plans.Add(new(plan.PublicId,plan.SequenceNo,plan.PaymentName,plan.PlannedAmount,plan.PlannedDueDate,plan.ConditionText,plan.StatusCode,confirms));
        }
        var documentRows=await(from link in db.InteriorContractDocuments.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.InteriorContractId==row.Id&&link.IsCurrent&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose="SIGNED_CONTRACT"}).ToListAsync(token);
        var documents=await PublishFiles(project.PublicId,userId,documentRows.Select(x=>(x.File,x.Purpose)),token);
        return new(row.PublicId,row.ContractVersion,provider,row.StatusCode,row.ContractAmount,row.CurrencyCode,row.ScopeSnapshotJson,row.ScheduleSnapshotJson,row.WarrantySnapshotJson,row.ContractSignedDate,row.PlannedStartDate,row.PlannedCompletionDate,row.CustomerAgreedAt,row.ProviderAgreedAt,row.EffectiveAt,row.CustomerMismatchReason,row.ProviderTrustScoreSnapshot,documents,versions,plans);
    }

    private async Task<IReadOnlyList<CustomerInteriorWorkStage>> Stages(InteriorProject project,long viewerUserId,CancellationToken token)
    {var result=new List<CustomerInteriorWorkStage>();foreach(var stage in await db.InteriorWorkStages.AsNoTracking().Where(x=>x.InteriorProjectId==project.Id).OrderBy(x=>x.SequenceNo).ToListAsync(token)){var updates=new List<CustomerInteriorWorkUpdate>();foreach(var update in await db.InteriorWorkUpdates.AsNoTracking().Where(x=>x.WorkStageId==stage.Id).OrderByDescending(x=>x.CreatedAt).ToListAsync(token)){var fileRows=await(from link in db.InteriorWorkUpdateFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.WorkUpdateId==update.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose=link.PurposeCode}).ToListAsync(token);var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);updates.Add(new(update.PublicId,update.ProgressPercent,update.UpdateText,update.IssueText,update.CreatedAt,files));}var inspections=await Inspections(project,stage.Id,viewerUserId,token);result.Add(new(stage.PublicId,stage.SequenceNo,stage.StageName,stage.PlannedStartDate,stage.PlannedEndDate,stage.ActualStartAt,stage.ActualEndAt,stage.ProgressPercent,stage.StatusCode,stage.ProviderNote,updates,inspections));}return result;}

    private async Task<IReadOnlyList<CustomerInteriorInspection>> Inspections(InteriorProject project,long stageId,long viewerUserId,CancellationToken token)
    {
        var rows=await(from inspection in db.InteriorStageInspections.AsNoTracking() join acknowledgement0 in db.InteriorStageInspectionAcknowledgements.AsNoTracking().Where(x=>x.CustomerProfileId==project.CustomerProfileId) on inspection.Id equals acknowledgement0.StageInspectionId into acknowledgements from acknowledgement in acknowledgements.DefaultIfEmpty() where inspection.WorkStageId==stageId orderby inspection.InspectedAt descending select new{Inspection=inspection,Acknowledgement=acknowledgement}).ToListAsync(token);
        var result=new List<CustomerInteriorInspection>();
        foreach(var row in rows)
        {
            var fileRows=await(from link in db.InteriorStageInspectionFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.StageInspectionId==row.Inspection.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose=link.PurposeCode}).ToListAsync(token);
            var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);
            result.Add(new(row.Inspection.PublicId,row.Inspection.InspectionStatusCode,row.Inspection.ChecklistJson,row.Inspection.ResultText,row.Inspection.RequestedCorrectionText,row.Inspection.InspectedAt,row.Acknowledgement?.AcknowledgedAt,row.Acknowledgement?.Comment,files));
        }
        return result;
    }

    private async Task<IReadOnlyList<CustomerInteriorChange>> Changes(InteriorProject project,long viewerUserId,CancellationToken token)
    {
        var rows=await(from change in db.InteriorContractChanges.AsNoTracking() join contract in db.InteriorContracts.AsNoTracking() on change.InteriorContractId equals contract.Id where contract.InteriorProjectId==project.Id orderby change.RequestedAt descending select change).ToListAsync(token);
        var result=new List<CustomerInteriorChange>();
        foreach(var change in rows)
        {
            var fileRows=await(from link in db.InteriorContractChangeFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.ContractChangeId==change.Id&&file.StatusCode=="ACTIVE" orderby link.DisplayOrder select new{File=file,Purpose=link.PurposeCode}).ToListAsync(token);
            var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);
            result.Add(new(change.PublicId,change.ChangeNo,change.StatusCode,change.ChangeTypeCode,change.ReasonText,change.ScopeChangeText,change.AmountDelta,change.ScheduleImpactDays,change.RequestedAt,change.CustomerDecidedAt,change.StatusCode=="REQUESTED",files));
        }
        return result;
    }
    private async Task<IReadOnlyList<CustomerInteriorDefect>> Defects(InteriorProject project,long viewerUserId,CancellationToken token)
    {
        var rows=await(from defect in db.InteriorDefects.AsNoTracking() join item in db.AfterServiceCases.AsNoTracking() on defect.AfterServiceCaseId equals item.Id join stage0 in db.InteriorWorkStages.AsNoTracking() on defect.WorkStageId equals stage0.Id into stages from stage in stages.DefaultIfEmpty() where defect.InteriorProjectId==project.Id orderby item.ReceivedAt descending select new{defect,item,StageId=stage==null?(Guid?)null:stage.PublicId}).ToListAsync(token);
        var result=new List<CustomerInteriorDefect>();
        foreach(var row in rows)
        {
            var actions=await db.AfterServiceActions.AsNoTracking().Where(x=>x.AfterServiceCaseId==row.item.Id).OrderBy(x=>x.OccurredAt).Select(x=>new CustomerInteriorAfterServiceAction(x.ActionTypeCode,x.FromStatusCode,x.ToStatusCode,x.ActionNote,x.ScheduledAt,x.PerformedAt,x.VisitOccurred,x.ResultText,x.OccurredAt)).ToListAsync(token);
            var fileRows=await(from link in db.AfterServiceFiles.AsNoTracking() join file in db.Files.AsNoTracking() on link.FileId equals file.Id where link.AfterServiceCaseId==row.item.Id&&file.StatusCode=="ACTIVE" orderby link.CreatedAt select new{File=file,Purpose=link.RoleCode??"AFTER_SERVICE_EVIDENCE"}).ToListAsync(token);var files=await PublishFiles(project.PublicId,viewerUserId,fileRows.Select(x=>(x.File,x.Purpose)),token);
            result.Add(new(row.defect.PublicId,row.item.PublicId,row.item.StatusCode,row.item.Subject,row.defect.DefectLocationText,row.defect.DefectDescription,row.StageId,row.defect.ContractVersion,row.item.ReceivedAt,actions,files));
        }
        return result;
    }
    private async Task<IReadOnlyList<CustomerInteriorDispute>> Disputes(InteriorProject project,CancellationToken token)=>await(from item in db.DisputeCases.AsNoTracking() join change0 in db.InteriorContractChanges.AsNoTracking() on item.InteriorContractChangeId equals change0.Id into changes from change in changes.DefaultIfEmpty() join stage0 in db.InteriorWorkStages.AsNoTracking() on item.InteriorWorkStageId equals stage0.Id into stages from stage in stages.DefaultIfEmpty() where item.InteriorProjectId==project.Id orderby item.ReceivedAt descending select new CustomerInteriorDispute(item.PublicId,item.StatusCode,item.Subject,item.Description,change==null?null:change.PublicId,stage==null?null:stage.PublicId,item.ReceivedAt)).ToListAsync(token);

    private IQueryable<Guid> ProjectFileIds(long projectId)=>
        db.InteriorSiteVisitFiles.Where(x=>db.InteriorSiteVisits.Any(v=>v.Id==x.SiteVisitId&&v.InteriorProjectId==projectId)).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First())
        .Concat(db.InteriorDesignFiles.Where(x=>db.InteriorDesignVersions.Any(v=>v.Id==x.DesignVersionId&&v.InteriorProjectId==projectId)).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.InteriorWorkUpdateFiles.Where(x=>db.InteriorWorkUpdates.Any(u=>u.Id==x.WorkUpdateId&&db.InteriorWorkStages.Any(s=>s.Id==u.WorkStageId&&s.InteriorProjectId==projectId))).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.InteriorStageInspectionFiles.Where(x=>db.InteriorStageInspections.Any(i=>i.Id==x.StageInspectionId&&db.InteriorWorkStages.Any(s=>s.Id==i.WorkStageId&&s.InteriorProjectId==projectId))).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.InteriorContractDocuments.Where(x=>x.IsCurrent&&db.InteriorContracts.Any(c=>c.Id==x.InteriorContractId&&c.InteriorProjectId==projectId)).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.InteriorContractChangeFiles.Where(x=>db.InteriorContractChanges.Any(c=>c.Id==x.ContractChangeId&&db.InteriorContracts.Any(k=>k.Id==c.InteriorContractId&&k.InteriorProjectId==projectId))).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.InteriorPaymentConfirmations.Where(x=>x.EvidenceFileId!=null&&db.InteriorPaymentPlans.Any(p=>p.Id==x.PaymentPlanId&&db.InteriorContracts.Any(k=>k.Id==p.InteriorContractId&&k.InteriorProjectId==projectId))).Select(x=>db.Files.Where(f=>f.Id==x.EvidenceFileId).Select(f=>f.PublicId).First()))
        .Concat(db.AfterServiceFiles.Where(x=>db.AfterServiceCases.Any(a=>a.Id==x.AfterServiceCaseId&&a.InteriorProjectId==projectId)).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()))
        .Concat(db.DisputeEvidence.Where(x=>x.FileId!=null&&db.DisputeCases.Any(d=>d.Id==x.DisputeCaseId&&d.InteriorProjectId==projectId)).Select(x=>db.Files.Where(f=>f.Id==x.FileId).Select(f=>f.PublicId).First()));
    private async Task<IReadOnlyList<CustomerInteriorFile>> PublishFiles(Guid projectId,long viewerUserId,IEnumerable<(StoredFile File,string Purpose)> rows,CancellationToken token){var result=new List<CustomerInteriorFile>();foreach(var row in rows){var publication=await filePublication.ResolveAsync(row.File,viewerUserId,true,true,token);var published=publication.PublishedFile??row.File;result.Add(new(row.File.PublicId,publication.Allowed?published.OriginalFileName:"evidence",published.ContentType,published.SizeBytes,row.Purpose,publication.Allowed?$"/api/v1/customers/me/interior/projects/{projectId}/files/{row.File.PublicId}":null,publication.StatusCode,publication.Allowed?null:publication.Message));}return result;}
    private async Task<List<StoredFile>> OwnedFiles(IReadOnlyList<Guid>? ids,long userId,CancellationToken token){if(ids is null||ids.Count==0)return[];var values=ids.Distinct().ToArray();var files=await db.Files.Where(x=>values.Contains(x.PublicId)&&x.StatusCode=="ACTIVE"&&x.UploadedByUserId==userId).ToListAsync(token);if(files.Count!=values.Length)throw NotFound("INTERIOR_FILE_NOT_FOUND","사용할 수 없는 증빙파일이 포함되어 있습니다.");return files;}
    private async Task<int> PublicReviewCount(long providerId,CancellationToken token)=>await db.Reviews.AsNoTracking().CountAsync(x=>x.ProviderProfileId==providerId&&x.VisibilityStatusCode=="PUBLIC"&&(x.VerificationStatusCode=="VERIFIED_TRANSACTION"||x.VerificationStatusCode=="VERIFIED_SUBSCRIPTION_VISIT"),token);
    private async Task<decimal?> CurrentTrust(long providerId,CancellationToken token){var value=await db.ProviderTrustScoreCurrent.AsNoTracking().Where(x=>x.ProviderProfileId==providerId&&x.EvaluationStatusCode=="CALCULATED").Select(x=>(decimal?)x.Score).SingleOrDefaultAsync(token);return value;}
    private async Task<InteriorProject> OwnedProject(Guid id,long profileId,CancellationToken token)=>await db.InteriorProjects.SingleOrDefaultAsync(x=>x.PublicId==id&&x.CustomerProfileId==profileId,token)??throw NotFound("INTERIOR_PROJECT_NOT_FOUND","인테리어 프로젝트를 찾을 수 없습니다.");
    private async Task<(long UserId,long ProfileId)> Customer(ClaimsPrincipal principal,CancellationToken token){if(!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var id))throw Forbidden("AUTHENTICATION_REQUIRED","로그인이 필요합니다.");var value=await(from user in db.Users.AsNoTracking() join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId where user.PublicId==id&&user.StatusCode=="ACTIVE" select new ValueTuple<long,long>(user.Id,profile.Id)).SingleOrDefaultAsync(token);return value.Item1==0?throw Forbidden("CUSTOMER_PROFILE_REQUIRED","고객 프로필이 필요합니다."):value;}
    private void Event(InteriorProject p,string type,string key,long actor,object data,DateTime now)=>db.InteriorProjectEvents.Add(new(){InteriorProjectId=p.Id,EventTypeCode=type,ActorUserId=actor,IdempotencyKey=key.Trim(),EventDataJson=JsonSerializer.Serialize(data),OccurredAt=now,CreatedAt=now});
    private void Audit(long actor,string action,Guid id,object data,DateTime now)=>db.AuditLogs.Add(new(){OccurredAt=now,ActorUserId=actor,ActorRoleCode=RoleCodes.Customer,ActionCode=action,EntityType="InteriorProject",EntityPublicId=id,AfterJson=JsonSerializer.Serialize(data)});
    private void Outbox(InteriorProject p,string type,string key,long actor,DateTime now)=>db.OutboxEvents.Add(new(){AggregateType="InteriorProject",AggregatePublicId=p.PublicId,EventType=type,PayloadJson=JsonSerializer.Serialize(new{projectId=p.PublicId}),OccurredAt=now,AvailableAt=now,IdempotencyKey=$"interior:{key}",CreatedByUserId=actor});
    private void ApplyVersion(object entity,string? value){if(!string.IsNullOrWhiteSpace(value))db.Entry(entity).Property("RowVersion").OriginalValue=Convert.FromBase64String(value);}
    private static bool SelectionMatches(string? data,Guid revisionId){if(string.IsNullOrWhiteSpace(data))return false;try{using var json=JsonDocument.Parse(data);return json.RootElement.TryGetProperty("quoteRevisionId",out var value)&&Guid.TryParse(value.GetString(),out var parsed)&&parsed==revisionId;}catch(JsonException){return false;}}
    private static string Version(byte[] value)=>value.Length==0?string.Empty:Convert.ToBase64String(value);
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string Number(Guid id)=>$"IP-{id:N}"[..15].ToUpperInvariant();
    private static string Status(string code)=>code switch{"CONSULTATION"=>"상담 진행","SITE_VISIT_SCHEDULED"=>"실측 예정","SITE_VISIT_COMPLETED"=>"실측 완료","CONTRACT_PENDING"=>"계약 확인","CONTRACTED"=>"계약 완료","CONSTRUCTION"=>"공사 진행","INSPECTION"=>"검사 진행","COMPLETED"=>"완료","DEFECT_MANAGEMENT"=>"하자/A/S 진행",_=>code};
    private static string VisitStatus(string code)=>code switch{"PROPOSED"=>"일정 제안","CONFIRMED"=>"실측 확정","COMPLETED"=>"실측 완료","CANCELLED"=>"취소",_=>code};
    private static string Purpose(string? code)=>code switch{"POST_SITE_VISIT"=>"실측 후 견적","CONTRACT_ESTIMATE"=>"계약 견적","FINAL_SETTLEMENT"=>"최종 정산견적",_=>"예비견적"};
    private static string Trust(decimal? value)=>value.HasValue?$"{value:0.##}점":"신규·평가중";
    private static bool ValidSignature(string type,byte[] bytes)=>type.ToLowerInvariant() switch{"application/pdf"=>bytes.Length>=5&&bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8),"image/png"=>bytes.Length>=8&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"image/jpeg"=>bytes.Length>=3&&bytes[0]==0xff&&bytes[1]==0xd8&&bytes[2]==0xff,_=>false};
    private static string EventDisplay(string code)=>code switch{"PROJECT_CREATED"=>"상담 프로젝트 시작","SITE_VISIT_CONFIRMED"=>"실측 일정 확정","SITE_VISIT_COMPLETED"=>"실측 완료","DESIGN_VERSION_CREATED"=>"설계안 등록","CONTRACT_CREATED"=>"계약 등록","CONTRACT_AGREED"=>"계약 동의","PAYMENT_CONFIRMED"=>"직접 지급 확인","STAGE_UPDATED"=>"공정 업데이트","CHANGE_REQUESTED"=>"변경 승인 요청","CHANGE_APPROVED"=>"변경 승인","CHANGE_REJECTED"=>"변경 거절","INSPECTION_COMPLETED"=>"단계 검사","PROJECT_COMPLETED"=>"프로젝트 완료","DEFECT_REPORTED"=>"하자 접수","DISPUTE_REPORTED"=>"분쟁 접수",_=>code};
    private static InteriorBusinessException NotFound(string code,string message)=>new(404,code,message);private static InteriorBusinessException Forbidden(string code,string message)=>new(403,code,message);private static InteriorBusinessException Conflict(string code,string message)=>new(409,code,message);
}
