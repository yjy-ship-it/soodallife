using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Emergency;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.SiteVisits;

public sealed class SiteVisitService(SoodalLifeDbContext db,IEmergencyPaymentInstructionProtector protector)
{
    private static readonly string[] FinalStatuses=["COMPLETED","REJECTED","CANCELLED","EXPIRED"];

    public async Task<IReadOnlyList<SiteVisitProposalResponse>> ProviderList(ClaimsPrincipal principal,Guid requestId,CancellationToken token)
    {
        var actor=await Provider(principal,token);
        var rows=await(from proposal in db.SiteVisitProposals.AsNoTracking()
            join request in db.ServiceRequests.AsNoTracking() on proposal.ServiceRequestId equals request.Id
            where request.PublicId==requestId&&proposal.ProviderProfileId==actor.ProfileId&&!db.CustomerProfiles.Any(customer=>customer.Id==request.CustomerProfileId&&customer.UserId==actor.UserId)
            orderby proposal.CreatedAt descending select new{proposal,request}).ToListAsync(token);
        var result=new List<SiteVisitProposalResponse>();foreach(var row in rows)result.Add(await Map(row.proposal,row.request,true,token));return result;
    }

    public async Task<IReadOnlyList<SiteVisitProposalResponse>> CustomerList(ClaimsPrincipal principal,Guid requestId,CancellationToken token)
    {
        var actor=await Customer(principal,token);
        var request=await db.ServiceRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==requestId&&x.CustomerProfileId==actor.ProfileId&&!x.IsUrgent,token)??throw NotFound();
        var rows=await db.SiteVisitProposals.AsNoTracking().Where(x=>x.ServiceRequestId==request.Id&&x.StatusCode!="CANCELLED"&&!db.ProviderProfiles.Any(provider=>provider.Id==x.ProviderProfileId&&provider.UserId==actor.UserId))
            .OrderBy(x=>x.ScheduledAt).ToListAsync(token);
        var result=new List<SiteVisitProposalResponse>();foreach(var row in rows)result.Add(await Map(row,request,false,token));return result;
    }

    public async Task<SiteVisitProposalResponse> Save(ClaimsPrincipal principal,Guid requestId,SaveSiteVisitProposalInput input,CancellationToken token)
    {
        var actor=await Provider(principal,token);var row=await(from dispatch in db.RequestDispatches
            join request in db.ServiceRequests on dispatch.ServiceRequestId equals request.Id
            join provider in db.ProviderProfiles on dispatch.ProviderProfileId equals provider.Id
            join customer in db.CustomerProfiles on request.CustomerProfileId equals customer.Id
            where request.PublicId==requestId&&dispatch.ProviderProfileId==actor.ProfileId&&dispatch.StatusCode!="EXPIRED"
            select new{dispatch,request,provider,CustomerUserId=customer.UserId}).SingleOrDefaultAsync(token)??throw Forbidden();
        if(row.CustomerUserId==actor.UserId)throw SelfRequest();
        if(row.request.IsUrgent)throw Conflict("SITE_VISIT_USE_EMERGENCY","긴급 요청은 긴급출동 기능을 이용해 주세요.");
        if(row.request.StatusCode!="OPEN"||row.request.ExpiresAt<=DateTime.UtcNow)throw Conflict("SITE_VISIT_REQUEST_CLOSED","공개 중인 요청에만 방문견적을 제안할 수 있습니다.");
        if(row.provider.ActivityStatusCode!="ACTIVE")throw Conflict("PROVIDER_NOT_ACTIVE","활동 중인 전문가만 방문견적을 제안할 수 있습니다.");
        Validate(input,row.request.ExpiresAt!.Value);
        var key=input.IdempotencyKey.Trim();var keyed=await db.SiteVisitProposals.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==key,token);
        if(keyed is not null){if(keyed.RequestDispatchId!=row.dispatch.Id)throw Conflict("IDEMPOTENCY_KEY_CONFLICT","이미 다른 방문견적에 사용된 중복 방지 키입니다.");return await Map(keyed,row.request,true,token);}
        var now=DateTime.UtcNow;var proposal=await db.SiteVisitProposals.SingleOrDefaultAsync(x=>x.RequestDispatchId==row.dispatch.Id,token);
        if(proposal is null){proposal=new(){ServiceRequestId=row.request.Id,RequestDispatchId=row.dispatch.Id,ProviderProfileId=actor.ProfileId,CreatedAt=now,CreatedByUserId=actor.UserId};db.SiteVisitProposals.Add(proposal);}
        else throw Conflict("SITE_VISIT_ALREADY_SUBMITTED","방문견적 제안 제출이 완료되어 다시 제출할 수 없습니다.");
        proposal.StatusCode="PROPOSED";proposal.ScheduledAt=input.ScheduledAt.ToUniversalTime();proposal.EstimatedDurationMinutes=input.EstimatedDurationMinutes;
        proposal.VisitFeeAmount=input.VisitFeeAmount;proposal.PaymentModeCode=input.VisitFeeAmount==0?"NO_FEE":input.PaymentMode.Trim().ToUpperInvariant();
        proposal.PaymentStatusCode=proposal.PaymentModeCode=="NO_FEE"?"NOT_REQUIRED":proposal.PaymentModeCode=="ON_SITE"?"ON_SITE_PENDING":"AWAITING_TRANSFER";
        proposal.DeductFromWorkAmount=input.DeductFromWorkAmount;proposal.TermsText=Clean(input.Terms,2000);
        proposal.PaymentInstructionProtected=proposal.PaymentModeCode.StartsWith("TRANSFER")&&!string.IsNullOrWhiteSpace(input.PaymentInstruction)?protector.Protect(input.PaymentInstruction.Trim()):null;
        proposal.NoShowWaitMinutes=input.NoShowWaitMinutes;proposal.ExpiresAt=input.ExpiresAt.ToUniversalTime();proposal.IdempotencyKey=key;proposal.UpdatedAt=now;proposal.UpdatedByUserId=actor.UserId;
        row.dispatch.StatusCode="RESPONDED";row.dispatch.RespondedAt=now;
        if(proposal.Id==0)await db.SaveChangesAsync(token);
        await AddEvent(proposal,actor.UserId,"PROPOSED",proposal.TermsText,$"site-visit-proposed:{proposal.PublicId:N}",now,token);
        Audit(actor.UserId,"PROVIDER","SITE_VISIT_PROPOSED",proposal.PublicId,new{requestId,proposal.ScheduledAt,proposal.VisitFeeAmount,proposal.PaymentModeCode});
        await Notify("SITE_VISIT.PROPOSED",proposal.PublicId,new{requestId,proposalId=proposal.PublicId},$"site-visit-proposed:{proposal.PublicId:N}",actor.UserId,now,token);
        await db.SaveChangesAsync(token);return await Map(proposal,row.request,true,token);
    }

    public async Task<SiteVisitProposalResponse> Accept(ClaimsPrincipal principal,Guid proposalId,AcceptSiteVisitInput input,CancellationToken token)
    {
        var actor=await Customer(principal,token);if(!input.TermsAccepted)throw Invalid("SITE_VISIT_TERMS_REQUIRED","방문비·결제·노쇼 조건에 동의해 주세요.");
        var address=Clean(input.DetailAddress,500)??throw Invalid("DETAIL_ADDRESS_REQUIRED","방문할 상세주소를 입력해 주세요.");
        var row=await(from proposal in db.SiteVisitProposals join request in db.ServiceRequests on proposal.ServiceRequestId equals request.Id
            where proposal.PublicId==proposalId&&request.CustomerProfileId==actor.ProfileId&&!request.IsUrgent select new{proposal,request}).SingleOrDefaultAsync(token)??throw NotFound();
        if(await db.ProviderProfiles.AsNoTracking().AnyAsync(provider=>provider.Id==row.proposal.ProviderProfileId&&provider.UserId==actor.UserId,token))throw SelfRequest();
        Version(row.proposal.RowVersion,input.RowVersion);var now=DateTime.UtcNow;
        if(row.proposal.StatusCode!="PROPOSED"||row.proposal.ExpiresAt<=now||row.proposal.ScheduledAt<=now)throw Conflict("SITE_VISIT_NOT_AVAILABLE","현재 수락할 수 없는 방문견적입니다.");
        if(await db.SiteVisitProposals.AnyAsync(x=>x.ServiceRequestId==row.request.Id&&x.Id!=row.proposal.Id&&!FinalStatuses.Contains(x.StatusCode)&&x.StatusCode!="PROPOSED",token))throw Conflict("SITE_VISIT_ALREADY_SCHEDULED","진행 중인 방문견적을 먼저 완료하거나 취소해 주세요.");
        row.proposal.StatusCode="ACCEPTED";row.proposal.AcceptedAt=now;row.proposal.UpdatedAt=now;row.proposal.UpdatedByUserId=actor.UserId;
        row.request.DetailAddress=address;row.request.UpdatedAt=now;row.request.UpdatedByUserId=actor.UserId;
        await AddEvent(row.proposal,actor.UserId,"ACCEPTED",null,input.IdempotencyKey,now,token);
        Audit(actor.UserId,"CUSTOMER","SITE_VISIT_ACCEPTED",row.proposal.PublicId,new{row.proposal.ScheduledAt,row.proposal.VisitFeeAmount});
        await Notify("SITE_VISIT.ACCEPTED",row.proposal.PublicId,new{requestId=row.request.PublicId,proposalId},$"site-visit-accepted:{proposalId:N}",actor.UserId,now,token);
        await db.SaveChangesAsync(token);return await Map(row.proposal,row.request,false,token);
    }

    public Task<SiteVisitProposalResponse> Reject(ClaimsPrincipal principal,Guid proposalId,SiteVisitActionInput input,CancellationToken token)=>CustomerFinal(principal,proposalId,input,"REJECTED",token);
    public Task<SiteVisitProposalResponse> Cancel(ClaimsPrincipal principal,Guid proposalId,SiteVisitActionInput input,CancellationToken token)=>ParticipantFinal(principal,proposalId,input,"CANCELLED",token);

    public async Task<SiteVisitProposalResponse> Progress(ClaimsPrincipal principal,Guid proposalId,SiteVisitActionInput input,CancellationToken token)
    {
        var actor=await Provider(principal,token);var row=await OwnedProvider(proposalId,actor.ProfileId,token);Version(row.Proposal.RowVersion,input.RowVersion);
        await EnsureQuoteAllowsVisit(row.Proposal, token);
        if(await db.SiteVisitEvents.AnyAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token))return await Map(row.Proposal,row.Request,true,token);
        var action=input.Action.Trim().ToUpperInvariant();var now=DateTime.UtcNow;
        if(action=="DEPARTED"&&row.Proposal.StatusCode=="ACCEPTED"){if(!CanDepart(row.Proposal))throw Conflict("SITE_VISIT_PAYMENT_PENDING","약정한 방문비 결제 확인 후 출발할 수 있습니다.");row.Proposal.StatusCode="DEPARTED";row.Proposal.DepartedAt=now;}
        else if(action=="ARRIVED"&&row.Proposal.StatusCode is "ACCEPTED" or "DEPARTED"){row.Proposal.StatusCode="ARRIVED";row.Proposal.ArrivedAt=now;}
        else if(action=="COMPLETED"&&row.Proposal.StatusCode is "ACCEPTED" or "DEPARTED" or "ARRIVED"){row.Proposal.StatusCode="COMPLETED";row.Proposal.CompletedAt=now;}
        else throw Conflict("SITE_VISIT_PROGRESS_INVALID","현재 단계에서 선택할 수 없는 방문 상태입니다.");
        row.Proposal.UpdatedAt=now;row.Proposal.UpdatedByUserId=actor.UserId;await AddEvent(row.Proposal,actor.UserId,action,Clean(input.Note,1000),input.IdempotencyKey,now,token);
        Audit(actor.UserId,"PROVIDER",$"SITE_VISIT_{action}",row.Proposal.PublicId,new{input.Note});await Notify($"SITE_VISIT.{action}",row.Proposal.PublicId,new{proposalId},$"site-visit:{proposalId:N}:{action}",actor.UserId,now,token);
        await db.SaveChangesAsync(token);return await Map(row.Proposal,row.Request,true,token);
    }

    public async Task<SiteVisitProposalResponse> ReportPayment(ClaimsPrincipal principal,Guid proposalId,SiteVisitPaymentReportInput input,CancellationToken token)
    {
        var actor=await Customer(principal,token);var row=await OwnedCustomer(proposalId,actor.ProfileId,token);Version(row.Proposal.RowVersion,input.RowVersion);
        if(row.Proposal.StatusCode!="ACCEPTED"||!row.Proposal.PaymentModeCode.StartsWith("TRANSFER"))throw Conflict("SITE_VISIT_PAYMENT_REPORT_INVALID","계좌이체 약정 방문견적만 이체 사실을 알릴 수 있습니다.");
        var now=DateTime.UtcNow;row.Proposal.PaymentStatusCode="REPORTED";row.Proposal.PaymentReportedAt=now;row.Proposal.UpdatedAt=now;row.Proposal.UpdatedByUserId=actor.UserId;
        await AddEvent(row.Proposal,actor.UserId,"PAYMENT_REPORTED",Clean(input.Memo,500),input.IdempotencyKey,now,token);await db.SaveChangesAsync(token);return await Map(row.Proposal,row.Request,false,token);
    }

    public async Task<SiteVisitProposalResponse> DecidePayment(ClaimsPrincipal principal,Guid proposalId,SiteVisitPaymentDecisionInput input,CancellationToken token)
    {
        var actor=await Provider(principal,token);var row=await OwnedProvider(proposalId,actor.ProfileId,token);Version(row.Proposal.RowVersion,input.RowVersion);
        var decision=input.Decision.Trim().ToUpperInvariant();if(decision is not("CONFIRM" or "REJECT"))throw Invalid("SITE_VISIT_PAYMENT_DECISION_INVALID","입금 확인 또는 미확인을 선택해 주세요.");
        var now=DateTime.UtcNow;row.Proposal.PaymentStatusCode=decision=="CONFIRM"?"CONFIRMED":"REJECTED";row.Proposal.PaymentConfirmedAt=decision=="CONFIRM"?now:null;row.Proposal.UpdatedAt=now;row.Proposal.UpdatedByUserId=actor.UserId;
        await AddEvent(row.Proposal,actor.UserId,decision=="CONFIRM"?"PAYMENT_CONFIRMED":"PAYMENT_REJECTED",Clean(input.Reason,500),input.IdempotencyKey,now,token);await db.SaveChangesAsync(token);return await Map(row.Proposal,row.Request,true,token);
    }

    public async Task<SiteVisitProposalResponse> NoShow(ClaimsPrincipal principal,Guid proposalId,SiteVisitNoShowInput input,CancellationToken token)
    {
        var participant=await Participant(principal,proposalId,token);Version(participant.Proposal.RowVersion,input.RowVersion);var subject=input.SubjectRole.Trim().ToUpperInvariant();
        if(subject==participant.Role||subject is not("CUSTOMER" or "PROVIDER"))throw Invalid("SITE_VISIT_NO_SHOW_ROLE_INVALID","노쇼 대상을 확인해 주세요.");if(input.ContactAttempts<2)throw Invalid("SITE_VISIT_CONTACT_REQUIRED","전화 또는 채팅 연락을 두 번 이상 시도해 주세요.");
        var now=DateTime.UtcNow;if(subject=="CUSTOMER"&&(participant.Proposal.ArrivedAt is null||now<participant.Proposal.ArrivedAt.Value.AddMinutes(participant.Proposal.NoShowWaitMinutes)))throw Conflict("SITE_VISIT_WAIT_REQUIRED",$"도착 후 {participant.Proposal.NoShowWaitMinutes}분 대기해 주세요.");
        if(subject=="PROVIDER"&&now<participant.Proposal.ScheduledAt.AddMinutes(15))throw Conflict("SITE_VISIT_PROVIDER_GRACE_REQUIRED","예약시간부터 15분이 지난 뒤 전문가 노쇼를 기록할 수 있습니다.");
        participant.Proposal.StatusCode="NO_SHOW";participant.Proposal.NoShowStatusCode=subject+"_NO_SHOW";participant.Proposal.NoShowReportedAt=now;participant.Proposal.UpdatedAt=now;participant.Proposal.UpdatedByUserId=participant.UserId;
        await AddEvent(participant.Proposal,participant.UserId,participant.Proposal.NoShowStatusCode,Clean(input.EvidenceNote,1000),input.IdempotencyKey,now,token);await db.SaveChangesAsync(token);return await Map(participant.Proposal,participant.Request,participant.Role=="PROVIDER",token);
    }

    public async Task<SiteVisitProposalResponse> Dispute(ClaimsPrincipal principal,Guid proposalId,SiteVisitDisputeInput input,CancellationToken token)
    {
        var participant=await Participant(principal,proposalId,token);Version(participant.Proposal.RowVersion,input.RowVersion);if(participant.Proposal.StatusCode!="NO_SHOW")throw Conflict("SITE_VISIT_NO_SHOW_NOT_FOUND","이의를 제기할 노쇼 기록이 없습니다.");
        var reason=Clean(input.Reason,1000)??throw Invalid("SITE_VISIT_DISPUTE_REASON_REQUIRED","이의 사유를 입력해 주세요.");var now=DateTime.UtcNow;participant.Proposal.StatusCode="DISPUTED";participant.Proposal.NoShowStatusCode="DISPUTED";participant.Proposal.UpdatedAt=now;participant.Proposal.UpdatedByUserId=participant.UserId;
        await AddEvent(participant.Proposal,participant.UserId,"NO_SHOW_DISPUTED",reason,input.IdempotencyKey,now,token);await db.SaveChangesAsync(token);return await Map(participant.Proposal,participant.Request,participant.Role=="PROVIDER",token);
    }

    private async Task<SiteVisitProposalResponse> CustomerFinal(ClaimsPrincipal principal,Guid id,SiteVisitActionInput input,string status,CancellationToken token){var a=await Customer(principal,token);var row=await OwnedCustomer(id,a.ProfileId,token);return await Final(row.Proposal,row.Request,a.UserId,"CUSTOMER",input,status,false,token);}
    private async Task<SiteVisitProposalResponse> ParticipantFinal(ClaimsPrincipal principal,Guid id,SiteVisitActionInput input,string status,CancellationToken token)
    {
        var p=await Participant(principal,id,token);
        if(p.Role=="PROVIDER")await EnsureQuoteAllowsVisit(p.Proposal,token);
        return await Final(p.Proposal,p.Request,p.UserId,p.Role,input,status,p.Role=="PROVIDER",token);
    }
    private async Task<SiteVisitProposalResponse> Final(SiteVisitProposal proposal,ServiceRequest request,long userId,string role,SiteVisitActionInput input,string status,bool providerView,CancellationToken token){Version(proposal.RowVersion,input.RowVersion);if(FinalStatuses.Contains(proposal.StatusCode))return await Map(proposal,request,providerView,token);var now=DateTime.UtcNow;proposal.StatusCode=status;proposal.UpdatedAt=now;proposal.UpdatedByUserId=userId;await AddEvent(proposal,userId,status,Clean(input.Note,1000),input.IdempotencyKey,now,token);Audit(userId,role,$"SITE_VISIT_{status}",proposal.PublicId,new{input.Note});await db.SaveChangesAsync(token);return await Map(proposal,request,providerView,token);}

    private async Task<(SiteVisitProposal Proposal,ServiceRequest Request)> OwnedProvider(Guid id,long providerId,CancellationToken token)=>await(from p in db.SiteVisitProposals join r in db.ServiceRequests on p.ServiceRequestId equals r.Id where p.PublicId==id&&p.ProviderProfileId==providerId select new ValueTuple<SiteVisitProposal,ServiceRequest>(p,r)).SingleOrDefaultAsync(token) is var x&&x!=default?x:throw NotFound();
    private async Task<(SiteVisitProposal Proposal,ServiceRequest Request)> OwnedCustomer(Guid id,long customerId,CancellationToken token)=>await(from p in db.SiteVisitProposals join r in db.ServiceRequests on p.ServiceRequestId equals r.Id where p.PublicId==id&&r.CustomerProfileId==customerId select new ValueTuple<SiteVisitProposal,ServiceRequest>(p,r)).SingleOrDefaultAsync(token) is var x&&x!=default?x:throw NotFound();
    private async Task<(SiteVisitProposal Proposal,ServiceRequest Request,long UserId,string Role)> Participant(ClaimsPrincipal principal,Guid id,CancellationToken token){var publicId=Principal(principal);return await(from p in db.SiteVisitProposals join r in db.ServiceRequests on p.ServiceRequestId equals r.Id join c in db.CustomerProfiles on r.CustomerProfileId equals c.Id join cu in db.Users on c.UserId equals cu.Id join provider in db.ProviderProfiles on p.ProviderProfileId equals provider.Id join pu in db.Users on provider.UserId equals pu.Id where p.PublicId==id&&(cu.PublicId==publicId||pu.PublicId==publicId) select new ValueTuple<SiteVisitProposal,ServiceRequest,long,string>(p,r,cu.PublicId==publicId?cu.Id:pu.Id,cu.PublicId==publicId?"CUSTOMER":"PROVIDER")).SingleOrDefaultAsync(token) is var x&&x!=default?x:throw NotFound();}

    private async Task EnsureQuoteAllowsVisit(SiteVisitProposal proposal,CancellationToken token)
    {
        if(await db.Quotes.AsNoTracking().AnyAsync(x=>x.ServiceRequestId==proposal.ServiceRequestId&&x.ProviderProfileId==proposal.ProviderProfileId&&x.StatusCode=="NOT_SELECTED",token))
            throw Conflict("SITE_VISIT_QUOTE_NOT_SELECTED","미채택된 견적의 방문 업무는 진행할 수 없습니다.");
    }

    private async Task<SiteVisitProposalResponse> Map(SiteVisitProposal p,ServiceRequest r,bool providerView,CancellationToken token)
    {
        var provider=await db.ProviderProfiles.AsNoTracking().Where(x=>x.Id==p.ProviderProfileId).Select(x=>new{x.PublicId,x.BusinessName}).SingleAsync(token);
        var events=await db.SiteVisitEvents.AsNoTracking().Where(x=>x.SiteVisitProposalId==p.Id).OrderBy(x=>x.OccurredAt).Select(x=>new SiteVisitEventResponse(x.PublicId,x.EventTypeCode,x.Note,x.OccurredAt)).ToListAsync(token);
        string? instruction=null;if((providerView||p.StatusCode!="PROPOSED")&&!string.IsNullOrWhiteSpace(p.PaymentInstructionProtected)){try{instruction=protector.Unprotect(p.PaymentInstructionProtected);}catch{instruction=null;}}
        var active=!FinalStatuses.Contains(p.StatusCode)&&p.StatusCode!="NO_SHOW"&&p.StatusCode!="DISPUTED";
        return new(p.PublicId,r.PublicId,provider.PublicId,provider.BusinessName,p.StatusCode,p.ScheduledAt,p.EstimatedDurationMinutes,p.VisitFeeAmount,p.PaymentModeCode,p.PaymentStatusCode,p.DeductFromWorkAmount,p.TermsText,instruction,p.NoShowWaitMinutes,p.ExpiresAt,p.AcceptedAt,p.DepartedAt,p.ArrivedAt,p.CompletedAt,p.NoShowStatusCode,p.AcceptedAt.HasValue?r.DetailAddress:null,!providerView&&p.StatusCode=="PROPOSED"&&p.ExpiresAt>DateTime.UtcNow,providerView&&p.StatusCode=="PROPOSED",providerView&&active,events,Convert.ToBase64String(p.RowVersion));
    }
    private static bool CanDepart(SiteVisitProposal p)=>p.PaymentModeCode switch{"NO_FEE"=>true,"ON_SITE"=>true,"TRANSFER_REPORTED"=>p.PaymentStatusCode is "REPORTED" or "CONFIRMED","TRANSFER_CONFIRMED"=>p.PaymentStatusCode=="CONFIRMED",_=>false};
    private static void Validate(SaveSiteVisitProposalInput i,DateTime requestExpiry){var mode=i.PaymentMode.Trim().ToUpperInvariant();if(i.ScheduledAt.ToUniversalTime()<=DateTime.UtcNow)throw Invalid("SITE_VISIT_TIME_INVALID","방문 예정일시는 현재 이후로 입력해 주세요.");if(i.ExpiresAt.ToUniversalTime()<=DateTime.UtcNow||i.ExpiresAt.ToUniversalTime()>requestExpiry||i.ExpiresAt>=i.ScheduledAt)throw Invalid("SITE_VISIT_EXPIRY_INVALID","수락 기한은 현재 이후, 요청 마감 이전, 방문 예정일시 이전이어야 합니다.");if(i.EstimatedDurationMinutes is <10 or >480)throw Invalid("SITE_VISIT_DURATION_INVALID","예상 방문시간은 10~480분으로 입력해 주세요.");if(i.VisitFeeAmount<0||i.VisitFeeAmount>10_000_000)throw Invalid("SITE_VISIT_FEE_INVALID","방문비를 확인해 주세요.");if(i.VisitFeeAmount>0&&mode is not("ON_SITE" or "TRANSFER_REPORTED" or "TRANSFER_CONFIRMED"))throw Invalid("SITE_VISIT_PAYMENT_MODE_INVALID","유료 방문견적의 결제 방식을 선택해 주세요.");if(i.NoShowWaitMinutes is <5 or >60)throw Invalid("SITE_VISIT_WAIT_INVALID","노쇼 대기시간은 5~60분으로 입력해 주세요.");if(string.IsNullOrWhiteSpace(i.IdempotencyKey)||i.IdempotencyKey.Length>100)throw Invalid("IDEMPOTENCY_KEY_REQUIRED","중복 방지 키가 필요합니다.");}
    private async Task AddEvent(SiteVisitProposal p,long user,string type,string? note,string key,DateTime now,CancellationToken token){if(string.IsNullOrWhiteSpace(key))throw Invalid("IDEMPOTENCY_KEY_REQUIRED","중복 방지 키가 필요합니다.");if(!await db.SiteVisitEvents.AnyAsync(x=>x.IdempotencyKey==key,token))db.SiteVisitEvents.Add(new(){SiteVisitProposalId=p.Id,ActorUserId=user,EventTypeCode=type,Note=note,OccurredAt=now,IdempotencyKey=key.Trim()});}
    private async Task Notify(string type,Guid id,object payload,string key,long user,DateTime now,CancellationToken token){if(await db.NotificationTemplates.AsNoTracking().AnyAsync(x=>x.EventTypeCode==type&&x.IsActive,token)&&!await db.OutboxEvents.AnyAsync(x=>x.IdempotencyKey==key,token))db.OutboxEvents.Add(new(){AggregateType="SiteVisitProposal",AggregatePublicId=id,EventType=type,PayloadJson=JsonSerializer.Serialize(payload),StatusCode="PENDING",OccurredAt=now,AvailableAt=now,IdempotencyKey=key,CreatedByUserId=user});}
    private void Audit(long user,string role,string action,Guid id,object value)=>db.AuditLogs.Add(new(){OccurredAt=DateTime.UtcNow,ActorUserId=user,ActorRoleCode=role,ActionCode=action,EntityType="SiteVisitProposal",EntityPublicId=id,ResultCode="SUCCESS",AfterJson=JsonSerializer.Serialize(value)});
    private async Task<(long UserId,long ProfileId)> Provider(ClaimsPrincipal p,CancellationToken token)=>await(from u in db.Users join profile in db.ProviderProfiles on u.Id equals profile.UserId where u.PublicId==Principal(p)&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,profile.Id)).SingleOrDefaultAsync(token) is var result&&result!=default?result:throw Forbidden();
    private async Task<(long UserId,long ProfileId)> Customer(ClaimsPrincipal p,CancellationToken token)=>await(from u in db.Users join profile in db.CustomerProfiles on u.Id equals profile.UserId where u.PublicId==Principal(p)&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,profile.Id)).SingleOrDefaultAsync(token) is var result&&result!=default?result:throw Forbidden();
    private static Guid Principal(ClaimsPrincipal p)=>Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw Forbidden();
    private static void Version(byte[] current,string? encoded){if(current.Length==0)return;if(string.IsNullOrWhiteSpace(encoded))throw Conflict("ROW_VERSION_REQUIRED","화면을 새로고침한 후 다시 시도해 주세요.");try{if(!current.SequenceEqual(Convert.FromBase64String(encoded)))throw Conflict("ROW_VERSION_CONFLICT","다른 화면에서 방문견적이 변경되었습니다.");}catch(FormatException){throw Invalid("ROW_VERSION_INVALID","변경 버전이 올바르지 않습니다.");}}
    private static string? Clean(string? value,int max){value=string.IsNullOrWhiteSpace(value)?null:value.Trim();if(value?.Length>max)throw Invalid("TEXT_TOO_LONG","입력값이 너무 깁니다.");return value;}
    private static SiteVisitWorkflowException Invalid(string code,string message)=>new(code,message,400);
    private static SiteVisitWorkflowException Conflict(string code,string message)=>new(code,message,409);
    private static SiteVisitWorkflowException NotFound()=>new("SITE_VISIT_NOT_FOUND","방문견적을 찾을 수 없습니다.",404);
    private static SiteVisitWorkflowException Forbidden()=>new("SITE_VISIT_FORBIDDEN","방문견적에 접근할 수 없습니다.",403);
    private static SiteVisitWorkflowException SelfRequest()=>new("SELF_REQUEST_NOT_ALLOWED","본인이 등록한 요청에는 전문가로 방문견적을 제안하거나 수락할 수 없습니다.",403);
}
