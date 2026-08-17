using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Emergency;

public sealed class EmergencyWorkflowService(SoodalLifeDbContext db,ChatService chat)
{
    private static readonly ConcurrentDictionary<Guid,SemaphoreSlim> SelectionLocks=new();

    public async Task<IReadOnlyList<EmergencyProviderRequestItem>> ProviderRequests(ClaimsPrincipal principal,CancellationToken token)
    {
        var p=await Provider(principal,token);var now=DateTime.UtcNow;
        return await(from d in db.RequestDispatches.AsNoTracking() join r in db.ServiceRequests.AsNoTracking() on d.ServiceRequestId equals r.Id
            join c in db.ServiceCategories.AsNoTracking() on r.CategoryId equals c.Id join a in db.AdministrativeAreas.AsNoTracking() on r.AdministrativeAreaId equals a.Id
            join er0 in db.EmergencyResponses.AsNoTracking() on d.Id equals er0.RequestDispatchId into responseJoin from er in responseJoin.DefaultIfEmpty()
            where d.ProviderProfileId==p.ProviderId&&r.IsUrgent&&r.StatusCode=="OPEN"&&d.StatusCode!="EXPIRED"&&d.ExpiresAt>now&&!db.UserRelationshipBlocks.Any(b=>b.CustomerProfileId==r.CustomerProfileId&&b.ProviderProfileId==p.ProviderId&&b.StatusCode=="ACTIVE")
            orderby d.AvailableAt descending select new EmergencyProviderRequestItem(r.PublicId,c.Name,a.AreaName,r.Title,r.Description,r.OpenedAt??r.CreatedAt,d.ExpiresAt,d.StatusCode,er==null?null:er.StatusCode,true)).ToListAsync(token);
    }

    public async Task<IReadOnlyList<EmergencyProviderAssignmentItem>> ProviderAssignments(ClaimsPrincipal principal,CancellationToken token)
    {
        var p=await Provider(principal,token);
        var rows=await(from transaction in db.Transactions.AsNoTracking()
            join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
            join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
            join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
            where transaction.ProviderProfileId==p.ProviderId&&request.IsUrgent&&transaction.StatusCode!="CANCELLED"
            orderby transaction.CreatedAt descending
            select new{transaction.Id,transaction.PublicId,transaction.StatusCode,transaction.CreatedAt,category.Name,area.AreaName,request.Title}).ToListAsync(token);
        var transactionIds=rows.Select(x=>x.Id).ToArray();
        var events=await db.EmergencyProgressEvents.AsNoTracking().Where(x=>transactionIds.Contains(x.TransactionId))
            .OrderByDescending(x=>x.OccurredAt).ToListAsync(token);
        return rows.Select(x=>new EmergencyProviderAssignmentItem(x.PublicId,x.Name,x.AreaName,x.Title,x.StatusCode,
            events.FirstOrDefault(e=>e.TransactionId==x.Id)?.EventTypeCode??"DISPATCH_CONFIRMED",x.CreatedAt)).ToArray();
    }

    public async Task<EmergencyProviderResponseResult> Respond(ClaimsPrincipal principal,Guid requestId,SubmitEmergencyResponseInput input,CancellationToken token)
    {
        var p=await Provider(principal,token);var row=await(from d in db.RequestDispatches join r in db.ServiceRequests on d.ServiceRequestId equals r.Id where r.PublicId==requestId&&r.IsUrgent&&d.ProviderProfileId==p.ProviderId&&!db.UserRelationshipBlocks.Any(b=>b.CustomerProfileId==r.CustomerProfileId&&b.ProviderProfileId==p.ProviderId&&b.StatusCode=="ACTIVE") select new{Dispatch=d,Request=r}).SingleOrDefaultAsync(token)??throw NotFound();
        var now=DateTime.UtcNow;if(row.Request.StatusCode!="OPEN"||row.Dispatch.ExpiresAt<=now)throw Conflict("EMERGENCY_RESPONSE_EXPIRED","긴급출동 응답기한이 지났습니다.");
        if(input.IsAvailable&&(!input.EtaMinutes.HasValue&&!input.EstimatedArrivalAt.HasValue))throw Invalid("EMERGENCY_ETA_REQUIRED","출동 가능 응답에는 ETA가 필요합니다.");
        if(input.EtaMinutes is <=0 or >1440)throw Invalid("EMERGENCY_ETA_INVALID","ETA는 1분에서 1440분 사이여야 합니다.");
        if(string.IsNullOrWhiteSpace(input.IdempotencyKey))throw Invalid("IDEMPOTENCY_KEY_REQUIRED","중복 방지 키가 필요합니다.");
        var keyed=await db.EmergencyResponses.AsNoTracking().SingleOrDefaultAsync(x=>x.IdempotencyKey==input.IdempotencyKey.Trim(),token);
        if(keyed is not null){if(keyed.RequestDispatchId!=row.Dispatch.Id)throw Conflict("IDEMPOTENCY_KEY_CONFLICT","이미 다른 응답에 사용된 키입니다.");return Map(keyed);}
        var response=await db.EmergencyResponses.SingleOrDefaultAsync(x=>x.RequestDispatchId==row.Dispatch.Id,token);
        if(response is null){response=new(){ServiceRequestId=row.Request.Id,RequestDispatchId=row.Dispatch.Id,ProviderProfileId=p.ProviderId,CreatedAt=now,CreatedByUserId=p.UserId};db.EmergencyResponses.Add(response);}
        else ApplyVersion(response.RowVersion,input.RowVersion);
        if(response.StatusCode is "SELECTED" or "NOT_SELECTED")throw Conflict("EMERGENCY_RESPONSE_FINALIZED","선택이 완료된 응답은 변경할 수 없습니다.");
        response.StatusCode=input.IsAvailable?"AVAILABLE":"UNAVAILABLE";response.EtaMinutes=input.IsAvailable?input.EtaMinutes:null;response.EstimatedArrivalAt=input.IsAvailable?input.EstimatedArrivalAt?.ToUniversalTime():null;response.ConditionsText=Clean(input.Conditions,1000);response.RespondedAt=now;response.ExpiresAt=row.Dispatch.ExpiresAt;response.IdempotencyKey=input.IdempotencyKey.Trim();response.UpdatedAt=now;response.UpdatedByUserId=p.UserId;row.Dispatch.StatusCode="RESPONDED";row.Dispatch.RespondedAt=now;
        AddAudit(p.UserId,"PROVIDER","EMERGENCY_RESPONSE_SUBMITTED","EmergencyResponse",response.PublicId,JsonSerializer.Serialize(new{response.StatusCode,response.EtaMinutes,response.EstimatedArrivalAt}));
        await AddOutboxIfTemplate("EMERGENCY.RESPONSE.SUBMITTED","EmergencyResponse",response.PublicId,new{requestId,responseId=response.PublicId},$"emergency-response:{response.PublicId:N}",p.UserId,now,token);
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw Conflict("ROW_VERSION_CONFLICT","다른 화면에서 긴급 응답이 변경되었습니다.");}return Map(response);
    }

    public async Task<IReadOnlyList<EmergencyCustomerResponseItem>> CustomerResponses(ClaimsPrincipal principal,Guid requestId,CancellationToken token)
    {
        var c=await Customer(principal,token);var request=await db.ServiceRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.PublicId==requestId&&x.CustomerProfileId==c.ProfileId&&x.IsUrgent,token)??throw NotFound();var now=DateTime.UtcNow;
        var rows=await(from er in db.EmergencyResponses.AsNoTracking() join p in db.ProviderProfiles.AsNoTracking() on er.ProviderProfileId equals p.Id
            join trust in db.ProviderTrustScoreCurrent.AsNoTracking() on p.Id equals trust.ProviderProfileId into trustJoin from trust in trustJoin.DefaultIfEmpty()
            where er.ServiceRequestId==request.Id&&er.StatusCode=="AVAILABLE"&&er.ExpiresAt>now&&!db.UserRelationshipBlocks.Any(b=>b.CustomerProfileId==request.CustomerProfileId&&b.ProviderProfileId==er.ProviderProfileId&&b.StatusCode=="ACTIVE")
            select new{er,p,Trust=trust}).ToListAsync(token);
        var providerIds=rows.Select(x=>x.p.Id).ToArray();var reviewCounts=await db.Reviews.AsNoTracking().Where(x=>providerIds.Contains(x.ProviderProfileId)&&x.VisibilityStatusCode=="PUBLIC").GroupBy(x=>x.ProviderProfileId).Select(g=>new{Id=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count,token);
        return rows.OrderByDescending(x=>x.Trust!=null&&x.Trust.EvaluationStatusCode=="CALCULATED"?x.Trust.Score:null).ThenBy(x=>x.er.EtaMinutes??int.MaxValue).Select(x=>new EmergencyCustomerResponseItem(x.er.PublicId,x.p.PublicId,x.p.BusinessName,x.er.StatusCode,x.er.EtaMinutes,x.er.EstimatedArrivalAt,x.er.ConditionsText,x.Trust?.EvaluationStatusCode=="CALCULATED"?x.Trust.Score:null,x.Trust?.GradeCode,reviewCounts.GetValueOrDefault(x.p.Id),x.p.ApprovalStatusCode=="APPROVED"&&x.p.ActivityStatusCode=="ACTIVE",true,Convert.ToBase64String(x.er.RowVersion))).ToArray();
    }

    public async Task<EmergencySelectionResult> Select(ClaimsPrincipal principal,Guid requestId,SelectEmergencyProviderInput input,CancellationToken token)
    {
        var detailAddress=Clean(input.DetailAddress,500);if(detailAddress is null)throw Invalid("DETAIL_ADDRESS_REQUIRED","공급자를 선택하려면 상세주소를 입력해 주세요.");var c=await Customer(principal,token);if(string.IsNullOrWhiteSpace(input.IdempotencyKey))throw Invalid("IDEMPOTENCY_KEY_REQUIRED","중복 방지 키가 필요합니다.");var gate=SelectionLocks.GetOrAdd(requestId,_=>new(1,1));await gate.WaitAsync(token);IDbContextTransaction? tx=null;
        try{tx=await Begin(token);var request=await db.ServiceRequests.SingleOrDefaultAsync(x=>x.PublicId==requestId&&x.CustomerProfileId==c.ProfileId&&x.IsUrgent,token)??throw NotFound();var existing=await db.Transactions.SingleOrDefaultAsync(x=>x.ServiceRequestId==request.Id,token);if(existing is not null){var room=await chat.EnsureTransactionRoomAsync(existing,DateTime.UtcNow,token);return new(existing.PublicId,(await db.ProviderProfiles.Where(x=>x.Id==existing.ProviderProfileId).Select(x=>x.PublicId).SingleAsync(token)),room.PublicId,existing.StatusCode,"POLICY_REQUIRED",false);}if(request.StatusCode!="OPEN")throw Conflict("EMERGENCY_REQUEST_NOT_OPEN","선택할 수 있는 긴급요청 상태가 아닙니다.");
            var response=await db.EmergencyResponses.SingleOrDefaultAsync(x=>x.PublicId==input.ResponseId&&x.ServiceRequestId==request.Id,token)??throw NotFound();ApplyVersion(response.RowVersion,input.RowVersion);var now=DateTime.UtcNow;if(response.StatusCode!="AVAILABLE"||response.ExpiresAt<=now)throw Conflict("EMERGENCY_RESPONSE_NOT_AVAILABLE","현재 선택 가능한 출동 응답이 아닙니다.");var provider=await db.ProviderProfiles.SingleAsync(x=>x.Id==response.ProviderProfileId,token);if(provider.ApprovalStatusCode!="APPROVED"||provider.ActivityStatusCode!="ACTIVE")throw Conflict("PROVIDER_NOT_AVAILABLE","공급자의 승인 또는 활동 상태가 변경되었습니다.");
            if(await db.UserRelationshipBlocks.AsNoTracking().AnyAsync(b=>b.CustomerProfileId==request.CustomerProfileId&&b.ProviderProfileId==provider.Id&&b.StatusCode=="ACTIVE",token))throw Conflict("USER_RELATIONSHIP_BLOCKED","차단된 공급자는 선택할 수 없습니다.");
            var quote=new Quote{ServiceRequestId=request.Id,ProviderProfileId=provider.Id,RequestDispatchId=response.RequestDispatchId,StatusCode="ACCEPTED",SubmittedAt=response.RespondedAt,AcceptedAt=now,ExpiresAt=response.ExpiresAt,CreatedAt=now,CreatedByUserId=c.UserId,UpdatedAt=now,UpdatedByUserId=c.UserId};db.Quotes.Add(quote);await db.SaveChangesAsync(token);
            var revision=new QuoteRevision{QuoteId=quote.Id,RevisionNo=1,Summary="긴급출동 응답",Terms="출동비·작업비 정책 확인 필요",SubtotalAmount=0,VatAmount=0,TotalAmount=0,CurrencyCode="KRW",EstimatedDurationText=response.EtaMinutes.HasValue?$"ETA {response.EtaMinutes}분":null,AvailableStartAt=response.EstimatedArrivalAt,ValidUntil=response.ExpiresAt,SubmittedAt=response.RespondedAt,SubmittedByUserId=(await db.ProviderProfiles.Where(x=>x.Id==provider.Id).Select(x=>x.UserId).SingleAsync(token)),IdempotencyKey=$"emergency-quote:{response.PublicId:N}",RevisionPurposeCode="EMERGENCY_RESPONSE"};db.QuoteRevisions.Add(revision);await db.SaveChangesAsync(token);
            var policy=await db.CategoryPolicies.AsNoTracking().SingleAsync(x=>x.Id==request.CategoryPolicyId,token);var transaction=new TransactionRecord{ServiceRequestId=request.Id,AcceptedQuoteRevisionId=revision.Id,CustomerProfileId=c.ProfileId,ProviderProfileId=provider.Id,CategoryId=request.CategoryId,CategoryFeePolicyId=null,StatusCode="CREATED",AgreedAmount=0,CurrencyCode="KRW",QuoteSnapshotJson=JsonSerializer.Serialize(new{responseId=response.PublicId,pricingStatus="POLICY_REQUIRED",response.EtaMinutes,response.EstimatedArrivalAt,response.ConditionsText}),CategoryPolicySnapshotJson=request.PolicySnapshotJson,CompletionPolicySnapshotJson=JsonSerializer.Serialize(new{policyId=policy.PublicId,policy.PolicyVersion,totalRequiredPhotoCount=policy.RequiredCompletionPhotoCount,policy.CompletionEvidenceRuleText}),FeePolicySnapshotJson=JsonSerializer.Serialize(new{status="POLICY_REQUIRED",sourceConflict="EMERGENCY_FEE_POLICY"}),FeePolicyVersionSnapshot="POLICY_REQUIRED",FeePolicyKindSnapshot="EMERGENCY",FeeTransactionTypeSnapshot="EMERGENCY",FeeCalculationMethodSnapshot="POLICY_REQUIRED",CalculatedFeeAmount=null,ActualChargedFeeAmount=null,FeeCurrencyCode="KRW",FeeChargeTimingSnapshot="POLICY_REQUIRED",FeeRestoreRuleSnapshot="POLICY_REQUIRED",WarrantyDaysSnapshot=policy.DefaultWarrantyDays,ProviderTrustScoreSnapshot=await db.ProviderTrustScoreCurrent.AsNoTracking().Where(x=>x.ProviderProfileId==provider.Id&&x.EvaluationStatusCode=="CALCULATED").Select(x=>(decimal?)x.Score).SingleOrDefaultAsync(token),CreatedAt=now,CreatedByUserId=c.UserId,UpdatedAt=now,UpdatedByUserId=c.UserId};db.Transactions.Add(transaction);await db.SaveChangesAsync(token);
            var roomResult=await chat.EnsureTransactionRoomAsync(transaction,now,token);response.StatusCode="SELECTED";response.SelectedAt=now;response.UpdatedAt=now;response.UpdatedByUserId=c.UserId;var others=await db.EmergencyResponses.Where(x=>x.ServiceRequestId==request.Id&&x.Id!=response.Id&&x.StatusCode=="AVAILABLE").ToListAsync(token);foreach(var other in others){other.StatusCode="NOT_SELECTED";other.UpdatedAt=now;other.UpdatedByUserId=c.UserId;}request.StatusCode="ACCEPTED";request.DetailAddress=detailAddress;request.AcceptedAt=now;request.UpdatedAt=now;request.UpdatedByUserId=c.UserId;db.EmergencyProgressEvents.Add(new(){TransactionId=transaction.Id,ActorUserId=c.UserId,EventTypeCode="DISPATCH_CONFIRMED",OccurredAt=now,IdempotencyKey=$"emergency-selected:{request.PublicId:N}"});AddAudit(c.UserId,"CUSTOMER","EMERGENCY_PROVIDER_SELECTED","Transaction",transaction.PublicId,JsonSerializer.Serialize(new{requestId,providerId=provider.PublicId,responseId=response.PublicId}));await AddOutboxIfTemplate("EMERGENCY.PROVIDER.SELECTED","Transaction",transaction.PublicId,new{transactionId=transaction.PublicId,requestId,providerId=provider.PublicId},$"emergency-selected:{request.PublicId:N}",c.UserId,now,token);await db.SaveChangesAsync(token);if(tx is not null)await tx.CommitAsync(token);return new(transaction.PublicId,provider.PublicId,roomResult.PublicId,transaction.StatusCode,"POLICY_REQUIRED",false);
        }catch{if(tx is not null)await tx.RollbackAsync(token);throw;}finally{if(tx is not null)await tx.DisposeAsync();gate.Release();}
    }

    public async Task<EmergencyProgressResponse> AddProgress(ClaimsPrincipal principal,Guid transactionId,EmergencyProgressInput input,CancellationToken token)
    {
        var p=await Provider(principal,token);var transaction=await db.Transactions.SingleOrDefaultAsync(x=>x.PublicId==transactionId&&x.ProviderProfileId==p.ProviderId,token)??throw NotFound();if(!await db.ServiceRequests.AsNoTracking().AnyAsync(x=>x.Id==transaction.ServiceRequestId&&x.IsUrgent,token))throw NotFound();var type=input.EventType.Trim().ToUpperInvariant();if(type is not("DEPARTED" or "EN_ROUTE" or "ARRIVED"))throw Invalid("EMERGENCY_PROGRESS_INVALID","허용되지 않은 출동 상태입니다.");var prior=await db.EmergencyProgressEvents.AsNoTracking().Where(x=>x.TransactionId==transaction.Id).OrderByDescending(x=>x.OccurredAt).FirstOrDefaultAsync(token);var expected=type switch{"DEPARTED"=>"DISPATCH_CONFIRMED","EN_ROUTE"=>"DEPARTED","ARRIVED"=>"EN_ROUTE",_=>""};if(prior?.EventTypeCode==type)return await Progress(transaction);if(prior?.EventTypeCode!=expected)throw Conflict("EMERGENCY_PROGRESS_SEQUENCE_INVALID","출동 진행 순서를 확인해 주세요.");if(await db.EmergencyProgressEvents.AnyAsync(x=>x.IdempotencyKey==input.IdempotencyKey,token))return await Progress(transaction);var now=DateTime.UtcNow;db.EmergencyProgressEvents.Add(new(){TransactionId=transaction.Id,ActorUserId=p.UserId,EventTypeCode=type,Note=Clean(input.Note,1000),OccurredAt=now,IdempotencyKey=input.IdempotencyKey.Trim()});if(type=="ARRIVED"&&!await db.TransactionAppointments.AnyAsync(x=>x.TransactionId==transaction.Id&&x.StatusCode=="CONFIRMED",token))db.TransactionAppointments.Add(new(){TransactionId=transaction.Id,ScheduledStartAt=now,StatusCode="CONFIRMED",ProposalIdempotencyKey=$"emergency-arrival:{transaction.PublicId:N}",ConfirmedAt=now,CreatedAt=now,CreatedByUserId=p.UserId,UpdatedAt=now,UpdatedByUserId=p.UserId});AddAudit(p.UserId,"PROVIDER",$"EMERGENCY_{type}","Transaction",transaction.PublicId,null);await AddOutboxIfTemplate($"EMERGENCY.{type}","Transaction",transaction.PublicId,new{transactionId,type},$"emergency-progress:{transaction.PublicId:N}:{type}",p.UserId,now,token);await db.SaveChangesAsync(token);return await Progress(transaction);
    }

    public async Task<EmergencyProgressResponse> GetProgress(ClaimsPrincipal principal,Guid transactionId,CancellationToken token){var id=Principal(principal);var transaction=await(from t in db.Transactions.AsNoTracking() join customer in db.CustomerProfiles.AsNoTracking() on t.CustomerProfileId equals customer.Id join provider in db.ProviderProfiles.AsNoTracking() on t.ProviderProfileId equals provider.Id join cu in db.Users.AsNoTracking() on customer.UserId equals cu.Id join pu in db.Users.AsNoTracking() on provider.UserId equals pu.Id join r in db.ServiceRequests.AsNoTracking() on t.ServiceRequestId equals r.Id where t.PublicId==transactionId&&r.IsUrgent&&(cu.PublicId==id||pu.PublicId==id) select t).SingleOrDefaultAsync(token)??throw NotFound();return await Progress(transaction);}
    private async Task<EmergencyProgressResponse> Progress(TransactionRecord t){var events=await db.EmergencyProgressEvents.AsNoTracking().Where(x=>x.TransactionId==t.Id).OrderBy(x=>x.OccurredAt).Select(x=>new EmergencyProgressItem(x.PublicId,x.EventTypeCode,x.Note,x.OccurredAt)).ToListAsync();return new(t.PublicId,t.StatusCode,events.LastOrDefault()?.EventType??"DISPATCH_CONFIRMED",events);}
    private async Task AddOutboxIfTemplate(string eventType,string aggregate,Guid id,object payload,string key,long user,DateTime now,CancellationToken token){if(await db.NotificationTemplates.AsNoTracking().AnyAsync(x=>x.EventTypeCode==eventType&&x.IsActive,token)&&!await db.OutboxEvents.AnyAsync(x=>x.IdempotencyKey==key,token))db.OutboxEvents.Add(new(){AggregateType=aggregate,AggregatePublicId=id,EventType=eventType,PayloadJson=JsonSerializer.Serialize(payload),StatusCode="PENDING",OccurredAt=now,AvailableAt=now,IdempotencyKey=key,CreatedByUserId=user});}
    private void AddAudit(long user,string role,string action,string entity,Guid id,string? after)=>db.AuditLogs.Add(new(){OccurredAt=DateTime.UtcNow,ActorUserId=user,ActorRoleCode=role,ActionCode=action,EntityType=entity,EntityPublicId=id,ResultCode="SUCCESS",AfterJson=after});
    private async Task<(long UserId,long ProviderId)> Provider(ClaimsPrincipal p,CancellationToken token)=>await(from u in db.Users join x in db.ProviderProfiles on u.Id equals x.UserId where u.PublicId==Principal(p)&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,x.Id)).SingleOrDefaultAsync(token) is var value&&value!=default?value:throw Forbidden();
    private async Task<(long UserId,long ProfileId)> Customer(ClaimsPrincipal p,CancellationToken token)=>await(from u in db.Users join x in db.CustomerProfiles on u.Id equals x.UserId where u.PublicId==Principal(p)&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,x.Id)).SingleOrDefaultAsync(token) is var value&&value!=default?value:throw Forbidden();
    private static Guid Principal(ClaimsPrincipal p)=>Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw Forbidden();
    private async Task<IDbContextTransaction?> Begin(CancellationToken token)=>db.Database.IsRelational()&&db.Database.CurrentTransaction is null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token):null;
    private static EmergencyProviderResponseResult Map(EmergencyResponse x)=>new(x.PublicId,x.StatusCode,x.EtaMinutes,x.EstimatedArrivalAt,x.ConditionsText,x.RespondedAt,Convert.ToBase64String(x.RowVersion));
    private static void ApplyVersion(byte[] current,string value){byte[] expected;try{expected=Convert.FromBase64String(value);}catch{throw Invalid("ROW_VERSION_INVALID","변경 버전이 올바르지 않습니다.");}if(!current.SequenceEqual(expected))throw Conflict("ROW_VERSION_CONFLICT","다른 화면에서 응답이 변경되었습니다.");}
    private static string? Clean(string? value,int max){value=string.IsNullOrWhiteSpace(value)?null:value.Trim();if(value?.Length>max)throw Invalid("TEXT_TOO_LONG","입력값이 너무 깁니다.");return value;}
    private static EmergencyWorkflowException Invalid(string code,string message)=>new(code,message,StatusCodes.Status400BadRequest);
    private static EmergencyWorkflowException Conflict(string code,string message)=>new(code,message,StatusCodes.Status409Conflict);
    private static EmergencyWorkflowException NotFound()=>new("EMERGENCY_NOT_FOUND","긴급출동 업무를 찾을 수 없습니다.",StatusCodes.Status404NotFound);
    private static EmergencyWorkflowException Forbidden()=>new("EMERGENCY_FORBIDDEN","긴급출동 업무에 접근할 수 없습니다.",StatusCodes.Status403Forbidden);
}
