using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Security;

public sealed class PersonalDataProtectionInterceptor(
    IPersonalDataProtector protector,
    IPersonalDataSearchHasher hasher,
    IOptions<PrivacyProtectionOptions> options) : SaveChangesInterceptor
{
    private const short Version = 1;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Protect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Protect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Protect(DbContext? context)
    {
        if (context is null || !options.Value.DualWriteEnabled) return;
        if (!hasher.IsConfigured) throw new InvalidOperationException("PrivacyProtection dual-write requires a configured search hash key.");

        foreach (var entry in context.ChangeTracker.Entries().Where(IsWrite))
        {
            switch (entry.Entity)
            {
                case User user:
                    var userProtected = false;
                    if (Changed(entry, nameof(User.Email)) || Changed(entry, nameof(User.NormalizedEmail)))
                    {
                        user.EmailEncrypted = ProtectNullable(user.Email);
                        user.EmailSearchHash = HashNullable(user.Email, hasher.Email);
                        userProtected = true;
                    }
                    if (Changed(entry, nameof(User.Phone)))
                    {
                        user.PhoneEncrypted = ProtectNullable(user.Phone);
                        user.PhoneSearchHash = HashNullable(user.Phone, hasher.Phone);
                        userProtected = true;
                    }
                    if (userProtected) user.PrivacyProtectionVersion = Version;
                    break;
                case CustomerAddress address:
                    var addressProtected = false;
                    if (Changed(entry, nameof(CustomerAddress.RecipientName))) { address.RecipientNameEncrypted = ProtectNullable(address.RecipientName); addressProtected = true; }
                    if (Changed(entry, nameof(CustomerAddress.RoadAddress))) { address.RoadAddressEncrypted = ProtectNullable(address.RoadAddress); addressProtected = true; }
                    if (Changed(entry, nameof(CustomerAddress.DetailAddress))) { address.DetailAddressEncrypted = ProtectNullable(address.DetailAddress); addressProtected = true; }
                    if (addressProtected) address.PrivacyProtectionVersion = Version;
                    break;
                case ProviderProfile provider:
                    if (Changed(entry, nameof(ProviderProfile.BusinessAddress)))
                    {
                        provider.BusinessAddressEncrypted = ProtectNullable(provider.BusinessAddress);
                        provider.PrivacyProtectionVersion = Version;
                    }
                    break;
                case ServiceRequest request:
                    if (Changed(entry, nameof(ServiceRequest.DetailAddress)))
                    {
                        request.DetailAddressEncrypted = ProtectNullable(request.DetailAddress);
                        request.PrivacyProtectionVersion = Version;
                    }
                    break;
                case SubscriptionRequest request:
                    if (Changed(entry, nameof(SubscriptionRequest.DetailAddress)))
                    {
                        request.DetailAddressEncrypted = ProtectNullable(request.DetailAddress);
                        request.PrivacyProtectionVersion = Version;
                    }
                    break;
            }
        }
    }

    private byte[]? ProtectNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : protector.Protect(PersonalDataNormalizer.Text(value));
    private static byte[]? HashNullable(string? value, Func<string, byte[]> hash) => string.IsNullOrWhiteSpace(value) ? null : hash(value);
    private static bool IsWrite(EntityEntry entry) => entry.State is EntityState.Added or EntityState.Modified;
    private static bool Changed(EntityEntry entry, string property) => entry.State == EntityState.Added || entry.Property(property).IsModified;
}
