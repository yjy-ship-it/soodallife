using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Emergency;

public sealed class ProviderEmergencyAvailabilityService(SoodalLifeDbContext db, IEmergencyAvailabilityResolver resolver)
{
    public async Task<EmergencyAvailabilityResponse> GetAsync(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Identity(principal,token); var setting=await db.ProviderEmergencySettings.AsNoTracking().SingleOrDefaultAsync(x=>x.ProviderProfileId==identity.ProviderId,token);
        if(setting is null) return new(Guid.Empty,false,null,null,false,"NOT_CONFIGURED",await Services(identity.ProviderId,null,token),[],string.Empty);
        return await Map(identity.ProviderId,setting,token);
    }

    public async Task<EmergencyAvailabilityResponse> UpdateAsync(ClaimsPrincipal principal,UpdateEmergencyAvailabilityInput input,CancellationToken token)
    {
        var identity=await Identity(principal,token); var now=DateTime.UtcNow;
        var setting=await db.ProviderEmergencySettings.SingleOrDefaultAsync(x=>x.ProviderProfileId==identity.ProviderId,token);
        if(setting is null){setting=new(){ProviderProfileId=identity.ProviderId,CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};db.ProviderEmergencySettings.Add(setting);await db.SaveChangesAsync(token);}
        else ApplyVersion(setting.RowVersion,input.RowVersion);
        setting.IsEnabled=input.IsEnabled; setting.TemporarilyUnavailableUntil=input.TemporarilyUnavailableUntil?.ToUniversalTime();
        setting.TemporaryUnavailableReason=Clean(input.TemporaryUnavailableReason,500); setting.UpdatedAt=now; setting.UpdatedByUserId=identity.UserId;

        var approved=await (from psc in db.ProviderServiceCategories
                            join approval in db.ProviderServiceApprovals on psc.Id equals approval.ProviderServiceCategoryId
                            join policy in db.CategoryPolicies on psc.CategoryId equals policy.CategoryId
                            where psc.ProviderProfileId==identity.ProviderId && psc.StatusCode=="ACTIVE" && approval.ApprovalStatusCode=="APPROVED"
                            select new{psc.Id,CategoryId=db.ServiceCategories.Where(c=>c.Id==psc.CategoryId).Select(c=>c.PublicId).Single(),policy.IsEmergencyAllowed}).ToListAsync(token);
        var existing=await db.ProviderEmergencyServiceSettings.Where(x=>x.ProviderEmergencySettingId==setting.Id).ToListAsync(token);
        foreach(var item in input.Services){var source=approved.SingleOrDefault(x=>x.CategoryId==item.ProviderServiceCategoryId)??throw Invalid("EMERGENCY_SERVICE_NOT_APPROVED","승인된 공급 서비스만 긴급출동을 설정할 수 있습니다.");if(item.IsEnabled&&!source.IsEmergencyAllowed)throw Invalid("EMERGENCY_NOT_ALLOWED","본사 정책상 긴급출동이 허용되지 않은 서비스입니다.");var target=existing.SingleOrDefault(x=>x.ProviderServiceCategoryId==source.Id);if(target is null){target=new(){ProviderEmergencySettingId=setting.Id,ProviderServiceCategoryId=source.Id,CreatedAt=now,CreatedByUserId=identity.UserId};db.ProviderEmergencyServiceSettings.Add(target);existing.Add(target);}target.IsEnabled=item.IsEnabled;target.UpdatedAt=now;target.UpdatedByUserId=identity.UserId;await db.SaveChangesAsync(token);var slots=await db.ProviderEmergencyAvailabilitySlots.Where(x=>x.ProviderEmergencyServiceSettingId==target.Id).ToListAsync(token);db.ProviderEmergencyAvailabilitySlots.RemoveRange(slots);foreach(var slot in item.Slots){ValidateSlot(slot);db.ProviderEmergencyAvailabilitySlots.Add(new(){ProviderEmergencyServiceSettingId=target.Id,DayOfWeek=slot.DayOfWeek,StartTime=slot.Is24Hours?null:slot.StartTime,EndTime=slot.Is24Hours?null:slot.EndTime,Is24Hours=slot.Is24Hours,CreatedAt=now,CreatedByUserId=identity.UserId});}}
        var oldExceptions=await db.ProviderEmergencyExceptions.Where(x=>x.ProviderEmergencySettingId==setting.Id).ToListAsync(token);db.ProviderEmergencyExceptions.RemoveRange(oldExceptions);
        foreach(var exception in input.Exceptions){var start=exception.StartsAt.ToUniversalTime();var end=exception.EndsAt.ToUniversalTime();if(end<=start)throw Invalid("EMERGENCY_EXCEPTION_PERIOD_INVALID","예외 휴무 종료시간은 시작시간 이후여야 합니다.");db.ProviderEmergencyExceptions.Add(new(){ProviderEmergencySettingId=setting.Id,StartsAt=start,EndsAt=end,Reason=Clean(exception.Reason,500),CreatedAt=now,CreatedByUserId=identity.UserId});}
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw new EmergencyWorkflowException("ROW_VERSION_CONFLICT","다른 화면에서 긴급출동 설정이 변경되었습니다.",StatusCodes.Status409Conflict);}return await Map(identity.ProviderId,setting,token);
    }

    private async Task<EmergencyAvailabilityResponse> Map(long providerId,ProviderEmergencySetting setting,CancellationToken token){var services=await Services(providerId,setting.Id,token);var exceptions=await db.ProviderEmergencyExceptions.AsNoTracking().Where(x=>x.ProviderEmergencySettingId==setting.Id).OrderBy(x=>x.StartsAt).Select(x=>new EmergencyExceptionResponse(x.PublicId,x.StartsAt,x.EndsAt,x.Reason)).ToListAsync(token);var current=services.Where(x=>x.IsEnabled).Select(x=>x.CategoryId).FirstOrDefault();EmergencyAvailabilityDecision decision=current==Guid.Empty?new(false,"NO_ENABLED_SERVICE"):await ResolveAny(providerId,current,token);return new(setting.PublicId,setting.IsEnabled,setting.TemporarilyUnavailableUntil,setting.TemporaryUnavailableReason,decision.IsAvailable,decision.ReasonCode,services,exceptions,Convert.ToBase64String(setting.RowVersion));}
    private async Task<EmergencyAvailabilityDecision> ResolveAny(long providerId,Guid categoryId,CancellationToken token){var row=await(from c in db.ServiceCategories where c.PublicId==categoryId from a in db.ProviderServiceAreas where db.ProviderServiceCategories.Any(s=>s.Id==a.ProviderServiceCategoryId&&s.ProviderProfileId==providerId&&s.CategoryId==c.Id) select new{c.Id,a.AdministrativeAreaId}).FirstOrDefaultAsync(token);return row is null?new(false,"AREA_NOT_CONFIGURED"):await resolver.EvaluateAsync(providerId,row.Id,row.AdministrativeAreaId,DateTime.UtcNow,token);}
    private async Task<IReadOnlyList<EmergencyServiceSettingResponse>> Services(long providerId,long? settingId,CancellationToken token)
    {
        var rows=await(from psc in db.ProviderServiceCategories.AsNoTracking()
            join category in db.ServiceCategories.AsNoTracking() on psc.CategoryId equals category.Id
            join approval in db.ProviderServiceApprovals.AsNoTracking() on psc.Id equals approval.ProviderServiceCategoryId
            join policy in db.CategoryPolicies.AsNoTracking() on category.Id equals policy.CategoryId
            where psc.ProviderProfileId==providerId&&psc.StatusCode=="ACTIVE"
            select new{psc,category,approval.ApprovalStatusCode,policy.IsEmergencyAllowed}).ToListAsync(token);
        var settingIds=settingId.HasValue
            ? await db.ProviderEmergencyServiceSettings.AsNoTracking().Where(x=>x.ProviderEmergencySettingId==settingId).ToListAsync(token)
            : new List<ProviderEmergencyServiceSetting>();
        var ids=settingIds.Select(x=>x.Id).ToArray();
        var slots=await db.ProviderEmergencyAvailabilitySlots.AsNoTracking().Where(x=>ids.Contains(x.ProviderEmergencyServiceSettingId)).ToListAsync(token);
        return rows.Select(x=>
        {
            var s=settingIds.SingleOrDefault(y=>y.ProviderServiceCategoryId==x.psc.Id);
            return new EmergencyServiceSettingResponse(s?.PublicId??Guid.Empty,x.category.PublicId,x.category.PublicId,x.category.Name,
                x.ApprovalStatusCode=="APPROVED",x.IsEmergencyAllowed,s?.IsEnabled??false,s is null?[]:
                slots.Where(y=>y.ProviderEmergencyServiceSettingId==s.Id).OrderBy(y=>y.DayOfWeek)
                    .Select(y=>new EmergencyAvailabilitySlotResponse(y.PublicId,y.DayOfWeek,y.StartTime,y.EndTime,y.Is24Hours)).ToArray());
        }).ToArray();
    }
    private async Task<(long UserId,long ProviderId)> Identity(ClaimsPrincipal p,CancellationToken token){if(!Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier),out var id))throw Invalid("IDENTITY_INVALID","로그인 정보를 확인할 수 없습니다.");return await(from u in db.Users join provider in db.ProviderProfiles on u.Id equals provider.UserId where u.PublicId==id&&u.StatusCode=="ACTIVE" select new ValueTuple<long,long>(u.Id,provider.Id)).SingleAsync(token);}
    private static void ValidateSlot(EmergencyAvailabilitySlotInput x){if(x.DayOfWeek>6)throw Invalid("EMERGENCY_DAY_INVALID","요일 값이 올바르지 않습니다.");if(!x.Is24Hours&&(!x.StartTime.HasValue||!x.EndTime.HasValue||x.EndTime<=x.StartTime))throw Invalid("EMERGENCY_TIME_INVALID","가능시간을 올바르게 입력해 주세요.");}
    private static void ApplyVersion(byte[] current,string? value){if(string.IsNullOrWhiteSpace(value))return;byte[] expected;try{expected=Convert.FromBase64String(value);}catch{throw Invalid("ROW_VERSION_INVALID","변경 버전이 올바르지 않습니다.");}if(!current.SequenceEqual(expected))throw new EmergencyWorkflowException("ROW_VERSION_CONFLICT","다른 화면에서 긴급출동 설정이 변경되었습니다.");}
    private static string? Clean(string? value,int max){value=string.IsNullOrWhiteSpace(value)?null:value.Trim();if(value?.Length>max)throw Invalid("TEXT_TOO_LONG","입력값이 너무 깁니다.");return value;}
    private static EmergencyWorkflowException Invalid(string code,string message)=>new(code,message,StatusCodes.Status400BadRequest);
}
