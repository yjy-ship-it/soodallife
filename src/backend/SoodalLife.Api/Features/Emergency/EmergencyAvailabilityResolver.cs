using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Emergency;

public interface IEmergencyAvailabilityResolver
{
    Task<EmergencyAvailabilityDecision> EvaluateAsync(long providerProfileId, long categoryId, long administrativeAreaId,
        DateTime nowUtc, CancellationToken token);
}

public sealed class EmergencyAvailabilityResolver(SoodalLifeDbContext db) : IEmergencyAvailabilityResolver
{
    public async Task<EmergencyAvailabilityDecision> EvaluateAsync(long providerProfileId, long categoryId, long administrativeAreaId,
        DateTime nowUtc, CancellationToken token)
    {
        var provider = await db.ProviderProfiles.AsNoTracking().Where(x=>x.Id==providerProfileId)
            .Select(x=>new{x.UserId,x.ActivityStatusCode,UserActive=db.Users.Any(u=>u.Id==x.UserId&&u.StatusCode=="ACTIVE")}).SingleOrDefaultAsync(token);
        if (provider is null || provider.ActivityStatusCode!="ACTIVE" || !provider.UserActive)
            return No("PROVIDER_NOT_ACTIVE");
        if(await db.ProviderExitRequests.AsNoTracking().AnyAsync(x=>x.ProviderProfileId==providerProfileId&&new[]{"REQUESTED","UNDER_REVIEW","REFUND_REQUIRED","BLOCKED_BY_ACTIVE_WORK","READY_TO_COMPLETE"}.Contains(x.StatusCode),token))
            return No("PROVIDER_EXIT_IN_PROGRESS");
        var today=DateOnly.FromDateTime(nowUtc);
        if(!await db.CategoryPolicies.AsNoTracking().AnyAsync(x=>x.CategoryId==categoryId&&x.IsEmergencyAllowed&&
                (x.TransactionTypeCode=="ONE_TIME"||x.TransactionTypeCode=="PROJECT")&&x.EffectiveFrom<=today&&
                (x.EffectiveTo==null||x.EffectiveTo>today),token)) return No("EMERGENCY_CATEGORY_NOT_ALLOWED");

        var service = await db.ProviderServiceCategories.AsNoTracking().FirstOrDefaultAsync(psc=>
            psc.ProviderProfileId==providerProfileId&&psc.CategoryId==categoryId&&psc.StatusCode=="ACTIVE",token);
        if (service is null) return No("EMERGENCY_SERVICE_NOT_REGISTERED");
        if (!await db.ProviderServiceAreas.AsNoTracking().AnyAsync(x=>x.ProviderServiceCategoryId==service.Id &&
                x.AdministrativeAreaId==administrativeAreaId && x.StatusCode=="ACTIVE",token)) return No("EMERGENCY_AREA_MISMATCH");

        var setting = await db.ProviderEmergencySettings.AsNoTracking().SingleOrDefaultAsync(x=>x.ProviderProfileId==providerProfileId,token);
        if (setting is null || !setting.IsEnabled) return No("EMERGENCY_DISABLED");
        if (setting.TemporarilyUnavailableUntil.HasValue && setting.TemporarilyUnavailableUntil.Value>nowUtc)
            return No("EMERGENCY_TEMPORARILY_UNAVAILABLE");
        if (await db.ProviderEmergencyExceptions.AsNoTracking().AnyAsync(x=>x.ProviderEmergencySettingId==setting.Id &&
                x.StartsAt<=nowUtc && x.EndsAt>nowUtc,token)) return No("EMERGENCY_EXCEPTION_ACTIVE");

        var serviceSetting = await db.ProviderEmergencyServiceSettings.AsNoTracking().SingleOrDefaultAsync(x=>
            x.ProviderEmergencySettingId==setting.Id && x.ProviderServiceCategoryId==service.Id && x.IsEnabled,token);
        if (serviceSetting is null) return No("EMERGENCY_SERVICE_DISABLED");
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc,DateTimeKind.Utc),TimeZoneInfo.Local);
        var day=(byte)local.DayOfWeek; var time=TimeOnly.FromDateTime(local);
        var available=await db.ProviderEmergencyAvailabilitySlots.AsNoTracking().AnyAsync(x=>
            x.ProviderEmergencyServiceSettingId==serviceSetting.Id && x.DayOfWeek==day &&
            (x.Is24Hours || (x.StartTime<=time && x.EndTime>time)),token);
        return available ? new(true,"AVAILABLE",service.Id) : No("OUTSIDE_EMERGENCY_HOURS");
    }

    private static EmergencyAvailabilityDecision No(string reason)=>new(false,reason);
}
