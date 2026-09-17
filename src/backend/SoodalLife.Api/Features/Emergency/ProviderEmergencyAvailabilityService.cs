using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Emergency;

public sealed class ProviderEmergencyAvailabilityService(SoodalLifeDbContext db, IEmergencyAvailabilityResolver resolver, IEmergencyPaymentInstructionProtector? paymentProtector = null)
{
    public async Task<EmergencyAvailabilityResponse> GetAsync(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Identity(principal,token); var setting=await db.ProviderEmergencySettings.AsNoTracking().SingleOrDefaultAsync(x=>x.ProviderProfileId==identity.ProviderId,token);
        if(setting is null) return new(Guid.Empty,false,null,null,false,"NOT_CONFIGURED",await Services(identity.ProviderId,null,token),[],[],string.Empty);
        return await Map(identity.ProviderId,setting,token);
    }

    public async Task<EmergencyAvailabilityResponse> UpdateAsync(ClaimsPrincipal principal,UpdateEmergencyAvailabilityInput input,CancellationToken token)
    {
        var identity=await Identity(principal,token); var now=DateTime.UtcNow;
        var setting=await db.ProviderEmergencySettings.SingleOrDefaultAsync(x=>x.ProviderProfileId==identity.ProviderId,token);
        if(setting is null){setting=new(){ProviderProfileId=identity.ProviderId,CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};db.ProviderEmergencySettings.Add(setting);await db.SaveChangesAsync(token);}
        else ApplyVersion(setting.RowVersion,input.RowVersion);
        var previousPauseUntil=setting.TemporarilyUnavailableUntil;var previousPauseReason=setting.TemporaryUnavailableReason;
        var nextPauseUntil=input.TemporarilyUnavailableUntil?.ToUniversalTime();var nextPauseReason=nextPauseUntil.HasValue?Clean(input.TemporaryUnavailableReason,500):null;
        setting.IsEnabled=input.IsEnabled;setting.TemporarilyUnavailableUntil=nextPauseUntil;setting.TemporaryUnavailableReason=nextPauseReason;setting.UpdatedAt=now;setting.UpdatedByUserId=identity.UserId;
        if(previousPauseUntil!=nextPauseUntil||previousPauseReason!=nextPauseReason){var action=!previousPauseUntil.HasValue&&nextPauseUntil.HasValue?"EMERGENCY_PAUSE_SET":previousPauseUntil.HasValue&&!nextPauseUntil.HasValue?"EMERGENCY_PAUSE_RELEASED":"EMERGENCY_PAUSE_UPDATED";db.AuditLogs.Add(new(){OccurredAt=now,ActorUserId=identity.UserId,ActorRoleCode="PROVIDER",ActionCode=action,EntityType="PROVIDER_EMERGENCY_SETTING",EntityPublicId=setting.PublicId,ResultCode="SUCCESS",Reason=nextPauseReason,BeforeJson=JsonSerializer.Serialize(new PauseAuditSnapshot(previousPauseUntil,previousPauseReason)),AfterJson=JsonSerializer.Serialize(new PauseAuditSnapshot(nextPauseUntil,nextPauseReason))});}

        var today=DateOnly.FromDateTime(now);var registered=await (from psc in db.ProviderServiceCategories
                            where psc.ProviderProfileId==identity.ProviderId && psc.StatusCode=="ACTIVE"
                            && db.CategoryPolicies.Any(policy=>policy.CategoryId==psc.CategoryId&&policy.IsEmergencyAllowed&&
                                (policy.TransactionTypeCode=="ONE_TIME"||policy.TransactionTypeCode=="PROJECT")&&policy.EffectiveFrom<=today&&
                                (policy.EffectiveTo==null||policy.EffectiveTo>today))
                            select new{psc.Id,psc.IsNationwide,CategoryId=db.ServiceCategories.Where(c=>c.Id==psc.CategoryId).Select(c=>c.PublicId).Single()}).ToListAsync(token);
        var existing=await db.ProviderEmergencyServiceSettings.Where(x=>x.ProviderEmergencySettingId==setting.Id).ToListAsync(token);
        if(input.Services.Any(item=>item.IsEnabled&&registered.Any(source=>source.CategoryId==item.ProviderServiceCategoryId&&source.IsNationwide)))
            throw Invalid("EMERGENCY_ACTUAL_AREA_REQUIRED","긴급출동은 전국 서비스로 설정할 수 없습니다. 실제 이동 가능한 시·군·구를 등록해 주세요.");
        foreach(var item in input.Services){var source=registered.SingleOrDefault(x=>x.CategoryId==item.ProviderServiceCategoryId)??throw Invalid("EMERGENCY_SERVICE_NOT_REGISTERED","전문가가 등록한 서비스만 긴급출동에 사용할 수 있습니다.");ValidateCommercialTerms(item);var mode=item.PaymentMode.Trim().ToUpperInvariant();var target=existing.SingleOrDefault(x=>x.ProviderServiceCategoryId==source.Id);if(target is null){target=new(){ProviderEmergencySettingId=setting.Id,ProviderServiceCategoryId=source.Id,CreatedAt=now,CreatedByUserId=identity.UserId};db.ProviderEmergencyServiceSettings.Add(target);existing.Add(target);}target.IsEnabled=item.IsEnabled;target.BaseDispatchFeeAmount=mode=="NO_FEE"?0:item.BaseDispatchFeeAmount;target.PaymentModeCode=mode;target.NoShowFeeAmount=mode=="NO_FEE"?0:item.NoShowFeeAmount;target.NoShowWaitMinutes=item.NoShowWaitMinutes;target.WorkFeeSeparate=item.WorkFeeSeparate;target.AdditionalFeeText=Clean(item.AdditionalFeeText,1000);if(mode.StartsWith("TRANSFER",StringComparison.Ordinal)){var instruction=Clean(item.PaymentInstruction,500)??throw Invalid("EMERGENCY_PAYMENT_INSTRUCTION_REQUIRED","계좌이체를 선택하면 전문가가 직접 관리하는 결제 안내를 입력해 주세요.");target.PaymentInstructionProtected=Protect(instruction);}else target.PaymentInstructionProtected=null;target.UpdatedAt=now;target.UpdatedByUserId=identity.UserId;await db.SaveChangesAsync(token);var slots=await db.ProviderEmergencyAvailabilitySlots.Where(x=>x.ProviderEmergencyServiceSettingId==target.Id).ToListAsync(token);db.ProviderEmergencyAvailabilitySlots.RemoveRange(slots);foreach(var slot in item.Slots){ValidateSlot(slot);db.ProviderEmergencyAvailabilitySlots.Add(new(){ProviderEmergencyServiceSettingId=target.Id,DayOfWeek=slot.DayOfWeek,StartTime=slot.Is24Hours?null:slot.StartTime,EndTime=slot.Is24Hours?null:slot.EndTime,Is24Hours=slot.Is24Hours,CreatedAt=now,CreatedByUserId=identity.UserId});}}
        var oldExceptions=await db.ProviderEmergencyExceptions.Where(x=>x.ProviderEmergencySettingId==setting.Id).ToListAsync(token);db.ProviderEmergencyExceptions.RemoveRange(oldExceptions);
        foreach(var exception in input.Exceptions){var start=exception.StartsAt.ToUniversalTime();var end=exception.EndsAt.ToUniversalTime();if(end<=start)throw Invalid("EMERGENCY_EXCEPTION_PERIOD_INVALID","예외 휴무 종료시간은 시작시간 이후여야 합니다.");db.ProviderEmergencyExceptions.Add(new(){ProviderEmergencySettingId=setting.Id,StartsAt=start,EndsAt=end,Reason=Clean(exception.Reason,500),CreatedAt=now,CreatedByUserId=identity.UserId});}
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw new EmergencyWorkflowException("ROW_VERSION_CONFLICT","다른 화면에서 긴급출동 설정이 변경되었습니다.",StatusCodes.Status409Conflict);}return await Map(identity.ProviderId,setting,token);
    }

    private async Task<EmergencyAvailabilityResponse> Map(long providerId,ProviderEmergencySetting setting,CancellationToken token){var services=await Services(providerId,setting.Id,token);var exceptions=await db.ProviderEmergencyExceptions.AsNoTracking().Where(x=>x.ProviderEmergencySettingId==setting.Id).OrderBy(x=>x.StartsAt).Select(x=>new EmergencyExceptionResponse(x.PublicId,x.StartsAt,x.EndsAt,x.Reason)).ToListAsync(token);var auditRows=await db.AuditLogs.AsNoTracking().Where(x=>x.EntityType=="PROVIDER_EMERGENCY_SETTING"&&x.EntityPublicId==setting.PublicId&&(x.ActionCode=="EMERGENCY_PAUSE_SET"||x.ActionCode=="EMERGENCY_PAUSE_UPDATED"||x.ActionCode=="EMERGENCY_PAUSE_RELEASED")).OrderByDescending(x=>x.OccurredAt).Take(30).Select(x=>new{x.OccurredAt,x.ActionCode,x.AfterJson,x.Reason}).ToListAsync(token);var history=auditRows.Select(x=>{var snapshot=string.IsNullOrWhiteSpace(x.AfterJson)?null:JsonSerializer.Deserialize<PauseAuditSnapshot>(x.AfterJson);return new EmergencyPauseHistoryResponse(x.OccurredAt,x.ActionCode,snapshot?.TemporarilyUnavailableUntil,snapshot?.TemporaryUnavailableReason??x.Reason);}).ToArray();var enabledCategories=services.Where(x=>x.IsEnabled).Select(x=>x.CategoryId).ToArray();var decision=await ResolveAnyEnabledService(providerId,enabledCategories,token);return new(setting.PublicId,setting.IsEnabled,setting.TemporarilyUnavailableUntil,setting.TemporaryUnavailableReason,decision.IsAvailable,decision.ReasonCode,services,exceptions,history,Convert.ToBase64String(setting.RowVersion));}
    private async Task<EmergencyAvailabilityDecision> ResolveAnyEnabledService(long providerId,IReadOnlyList<Guid> categoryIds,CancellationToken token){if(categoryIds.Count==0)return new(false,"NO_ENABLED_SERVICE");var decisions=new List<EmergencyAvailabilityDecision>();foreach(var categoryId in categoryIds){var decision=await ResolveAny(providerId,categoryId,token);if(decision.IsAvailable)return decision;decisions.Add(decision);}return decisions.FirstOrDefault(x=>x.ReasonCode is not("AREA_NOT_CONFIGURED" or "EMERGENCY_AREA_MISMATCH"))??decisions[0];}
    private async Task<EmergencyAvailabilityDecision> ResolveAny(long providerId,Guid categoryId,CancellationToken token){var row=await(from c in db.ServiceCategories where c.PublicId==categoryId from a in db.ProviderServiceAreas where a.StatusCode=="ACTIVE"&&db.ProviderServiceCategories.Any(s=>s.Id==a.ProviderServiceCategoryId&&s.ProviderProfileId==providerId&&s.CategoryId==c.Id&&s.StatusCode=="ACTIVE") select new{c.Id,a.AdministrativeAreaId}).FirstOrDefaultAsync(token);return row is null?new(false,"AREA_NOT_CONFIGURED"):await resolver.EvaluateAsync(providerId,row.Id,row.AdministrativeAreaId,DateTime.UtcNow,token);}
    private async Task<IReadOnlyList<EmergencyServiceSettingResponse>> Services(long providerId,long? settingId,CancellationToken token)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow);
        var rows=await(from psc in db.ProviderServiceCategories.AsNoTracking()
            join category in db.ServiceCategories.AsNoTracking() on psc.CategoryId equals category.Id
            where psc.ProviderProfileId==providerId&&psc.StatusCode=="ACTIVE"
                &&db.CategoryPolicies.Any(policy=>policy.CategoryId==psc.CategoryId&&policy.IsEmergencyAllowed&&
                    (policy.TransactionTypeCode=="ONE_TIME"||policy.TransactionTypeCode=="PROJECT")&&policy.EffectiveFrom<=today&&
                    (policy.EffectiveTo==null||policy.EffectiveTo>today))
            select new{psc,category}).ToListAsync(token);
        var settingIds=settingId.HasValue
            ? await db.ProviderEmergencyServiceSettings.AsNoTracking().Where(x=>x.ProviderEmergencySettingId==settingId).ToListAsync(token)
            : new List<ProviderEmergencyServiceSetting>();
        var ids=settingIds.Select(x=>x.Id).ToArray();
        var slots=await db.ProviderEmergencyAvailabilitySlots.AsNoTracking().Where(x=>ids.Contains(x.ProviderEmergencyServiceSettingId)).ToListAsync(token);
        return rows.Select(x=>
        {
            var s=settingIds.SingleOrDefault(y=>y.ProviderServiceCategoryId==x.psc.Id);
            return new EmergencyServiceSettingResponse(s?.PublicId??Guid.Empty,x.category.PublicId,x.category.PublicId,x.category.Name,
                true,true,s?.IsEnabled??false,s is null?[]:
                slots.Where(y=>y.ProviderEmergencyServiceSettingId==s.Id).OrderBy(y=>y.DayOfWeek)
                    .Select(y=>new EmergencyAvailabilitySlotResponse(y.PublicId,y.DayOfWeek,y.StartTime,y.EndTime,y.Is24Hours)).ToArray(),
                s?.BaseDispatchFeeAmount??0,s?.PaymentModeCode??"ON_SITE",s?.NoShowFeeAmount??0,s?.NoShowWaitMinutes??10,
                s?.WorkFeeSeparate??true,s?.AdditionalFeeText,Unprotect(s?.PaymentInstructionProtected));
        }).ToArray();
    }
    private async Task<(long UserId,long ProviderId)> Identity(ClaimsPrincipal p,CancellationToken token){if(!Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id))throw Invalid("IDENTITY_INVALID","로그인 정보를 확인할 수 없습니다.");return await(from u in db.Users join provider in db.ProviderProfiles on u.Id equals provider.UserId where u.PublicId==id&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,provider.Id)).SingleAsync(token);}
    private static void ValidateSlot(EmergencyAvailabilitySlotInput x){if(x.DayOfWeek>6)throw Invalid("EMERGENCY_DAY_INVALID","요일 값이 올바르지 않습니다.");if(!x.Is24Hours&&(!x.StartTime.HasValue||!x.EndTime.HasValue||x.EndTime<=x.StartTime))throw Invalid("EMERGENCY_TIME_INVALID","가능시간을 올바르게 입력해 주세요.");}
    private static void ValidateCommercialTerms(EmergencyServiceSettingInput x){var mode=x.PaymentMode.Trim().ToUpperInvariant();if(mode is not("NO_FEE" or "ON_SITE" or "TRANSFER_REPORTED" or "TRANSFER_CONFIRMED"))throw Invalid("EMERGENCY_PAYMENT_MODE_INVALID","결제 방식을 확인해 주세요.");if(x.BaseDispatchFeeAmount<0||x.BaseDispatchFeeAmount>10000000)throw Invalid("EMERGENCY_BASE_FEE_INVALID","기본 출동비를 확인해 주세요.");if(x.NoShowFeeAmount<0||x.NoShowFeeAmount>x.BaseDispatchFeeAmount)throw Invalid("EMERGENCY_NO_SHOW_FEE_INVALID","노쇼 비용은 기본 출동비 이내로 설정해 주세요.");if(x.NoShowWaitMinutes is <5 or >60)throw Invalid("EMERGENCY_NO_SHOW_WAIT_INVALID","노쇼 대기시간은 5분에서 60분 사이여야 합니다.");}
    private string Protect(string value)=>paymentProtector?.Protect(value)??value;
    private string? Unprotect(string? value){if(string.IsNullOrWhiteSpace(value))return null;try{return paymentProtector?.Unprotect(value)??value;}catch{return null;}}
    private static void ApplyVersion(byte[] current,string? value){if(string.IsNullOrWhiteSpace(value))return;byte[] expected;try{expected=Convert.FromBase64String(value);}catch{throw Invalid("ROW_VERSION_INVALID","변경 버전이 올바르지 않습니다.");}if(!current.SequenceEqual(expected))throw new EmergencyWorkflowException("ROW_VERSION_CONFLICT","다른 화면에서 긴급출동 설정이 변경되었습니다.");}
    private static string? Clean(string? value,int max){value=string.IsNullOrWhiteSpace(value)?null:value.Trim();if(value?.Length>max)throw Invalid("TEXT_TOO_LONG","입력값이 너무 깁니다.");return value;}
    private static EmergencyWorkflowException Invalid(string code,string message)=>new(code,message,StatusCodes.Status400BadRequest);
    private sealed record PauseAuditSnapshot(DateTime? TemporarilyUnavailableUntil,string? TemporaryUnavailableReason);
}
