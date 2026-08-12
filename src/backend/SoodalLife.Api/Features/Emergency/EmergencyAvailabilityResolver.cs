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
            .Select(x=>new{x.ApprovalStatusCode,x.ActivityStatusCode}).SingleOrDefaultAsync(token);
        if (provider is null || provider.ApprovalStatusCode!="APPROVED" || provider.ActivityStatusCode!="ACTIVE")
            return No("PROVIDER_NOT_APPROVED_ACTIVE");

        var service = await (from psc in db.ProviderServiceCategories.AsNoTracking()
                             join approval in db.ProviderServiceApprovals.AsNoTracking() on psc.Id equals approval.ProviderServiceCategoryId
                             join policy in db.CategoryPolicies.AsNoTracking() on psc.CategoryId equals policy.CategoryId
                             where psc.ProviderProfileId==providerProfileId && psc.CategoryId==categoryId && psc.StatusCode=="ACTIVE" &&
                                   approval.ApprovalStatusCode=="APPROVED" && policy.IsEmergencyAllowed
                             select psc).FirstOrDefaultAsync(token);
        if (service is null) return No("EMERGENCY_SERVICE_NOT_APPROVED");
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
