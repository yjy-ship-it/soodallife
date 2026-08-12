using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Automation;

public sealed record PrivacyBackfillResult(int ScannedCount, int UpdatedCount, int VerifiedCount);
public sealed record PrivacyBackfillPreflightResult(int ExistingCiphertextVerified, int EmailHashesChecked, int PhoneHashesChecked);

public sealed class PrivacyBackfillService(
    SoodalLifeDbContext db,
    IPersonalDataProtector protector,
    IPersonalDataSearchHasher hasher,
    IOptions<PrivacyProtectionOptions> options)
{
    public bool IsReady => hasher.IsConfigured;

    public async Task<PrivacyBackfillPreflightResult> ValidatePreconditionsAsync(CancellationToken token)
    {
        if (!IsReady) throw new InvalidOperationException("PRIVACY_BACKFILL_KEYS_NOT_CONFIGURED");
        var sentinel = "privacy-backfill-preflight";
        if (protector.Unprotect(protector.Protect(sentinel)) != sentinel) throw new InvalidOperationException("PRIVACY_BACKFILL_KEYRING_UNAVAILABLE");
        var users = await db.Users.AsNoTracking().Where(x => x.Email != null || x.Phone != null || x.EmailEncrypted != null || x.PhoneEncrypted != null)
            .Select(x => new { x.Id, x.Email, x.Phone, x.EmailEncrypted, x.PhoneEncrypted }).ToListAsync(token);
        var verified = 0;
        foreach (var user in users)
        {
            if (user.EmailEncrypted is { Length: > 0 }) { _ = protector.Unprotect(user.EmailEncrypted); verified++; }
            if (user.PhoneEncrypted is { Length: > 0 }) { _ = protector.Unprotect(user.PhoneEncrypted); verified++; }
        }
        var emailHashes = users.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => new { x.Id, Hash = Convert.ToHexString(hasher.Email(x.Email!)) }).ToList();
        if (emailHashes.GroupBy(x => x.Hash).Any(x => x.Select(v => v.Id).Distinct().Count() > 1)) throw new InvalidOperationException("PRIVACY_BACKFILL_EMAIL_HASH_COLLISION");
        var phoneHashes = users.Where(x => !string.IsNullOrWhiteSpace(x.Phone)).Select(x => Convert.ToHexString(hasher.Phone(x.Phone!))).ToList();
        return new(verified, emailHashes.Count, phoneHashes.Count);
    }

    public async Task<PrivacyBackfillResult> BackfillBatchAsync(CancellationToken token)
    {
        if (!IsReady) throw new InvalidOperationException("PRIVACY_BACKFILL_KEYS_NOT_CONFIGURED");
        var size = Math.Clamp(options.Value.BackfillBatchSize, 1, 500);
        var scanned = 0; var updated = 0; var verified = 0;

        var users = await db.Users.Where(x =>
                (x.Email != null && (x.EmailEncrypted == null || x.EmailSearchHash == null)) ||
                (x.Phone != null && (x.PhoneEncrypted == null || x.PhoneSearchHash == null)))
            .OrderBy(x => x.Id).Take(size).ToListAsync(token);
        foreach (var user in users)
        {
            scanned++; var changed = false;
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                user.EmailEncrypted ??= protector.Protect(PersonalDataNormalizer.Text(user.Email));
                user.EmailSearchHash ??= hasher.Email(user.Email); changed = true;
                if (PersonalDataNormalizer.Email(protector.Unprotect(user.EmailEncrypted)) != PersonalDataNormalizer.Email(user.Email)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED");
                verified++;
            }
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                user.PhoneEncrypted ??= protector.Protect(PersonalDataNormalizer.Text(user.Phone));
                user.PhoneSearchHash ??= hasher.Phone(user.Phone); changed = true;
                if (PersonalDataNormalizer.Phone(protector.Unprotect(user.PhoneEncrypted)) != PersonalDataNormalizer.Phone(user.Phone)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED");
                verified++;
            }
            if (changed) { user.PrivacyProtectionVersion = 1; updated++; }
        }
        await db.SaveChangesAsync(token);
        if (updated >= size) return new(scanned, updated, verified);

        var remaining = size - updated;
        var addresses = await db.CustomerAddresses.Where(x =>
                (x.RecipientName != null && x.RecipientNameEncrypted == null) || x.RoadAddressEncrypted == null || x.DetailAddressEncrypted == null)
            .OrderBy(x => x.Id).Take(remaining).ToListAsync(token);
        foreach (var address in addresses)
        {
            scanned++;
            if (!string.IsNullOrWhiteSpace(address.RecipientName)) address.RecipientNameEncrypted ??= protector.Protect(PersonalDataNormalizer.Text(address.RecipientName));
            address.RoadAddressEncrypted ??= protector.Protect(PersonalDataNormalizer.Text(address.RoadAddress));
            address.DetailAddressEncrypted ??= protector.Protect(PersonalDataNormalizer.Text(address.DetailAddress));
            if (address.RecipientNameEncrypted is not null && protector.Unprotect(address.RecipientNameEncrypted) != PersonalDataNormalizer.Text(address.RecipientName!)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED");
            if (protector.Unprotect(address.RoadAddressEncrypted) != PersonalDataNormalizer.Text(address.RoadAddress) || protector.Unprotect(address.DetailAddressEncrypted) != PersonalDataNormalizer.Text(address.DetailAddress)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED");
            address.PrivacyProtectionVersion = 1; updated++; verified++;
        }
        await db.SaveChangesAsync(token);
        remaining = size - updated;
        if (remaining > 0)
        {
            var providers = await db.ProviderProfiles.Where(x => x.BusinessAddress != null && x.BusinessAddressEncrypted == null).OrderBy(x => x.Id).Take(remaining).ToListAsync(token);
            foreach (var value in providers) { scanned++; value.BusinessAddressEncrypted = protector.Protect(PersonalDataNormalizer.Text(value.BusinessAddress!)); if (protector.Unprotect(value.BusinessAddressEncrypted) != PersonalDataNormalizer.Text(value.BusinessAddress!)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED"); value.PrivacyProtectionVersion = 1; updated++; verified++; }
            await db.SaveChangesAsync(token);
        }
        remaining = size - updated;
        if (remaining > 0)
        {
            var requests = await db.ServiceRequests.Where(x => x.DetailAddress != null && x.DetailAddressEncrypted == null).OrderBy(x => x.Id).Take(remaining).ToListAsync(token);
            foreach (var value in requests) { scanned++; value.DetailAddressEncrypted = protector.Protect(PersonalDataNormalizer.Text(value.DetailAddress!)); if (protector.Unprotect(value.DetailAddressEncrypted) != PersonalDataNormalizer.Text(value.DetailAddress!)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED"); value.PrivacyProtectionVersion = 1; updated++; verified++; }
            await db.SaveChangesAsync(token);
        }
        remaining = size - updated;
        if (remaining > 0)
        {
            var subscriptions = await db.SubscriptionRequests.Where(x => x.DetailAddress != null && x.DetailAddressEncrypted == null).OrderBy(x => x.Id).Take(remaining).ToListAsync(token);
            foreach (var value in subscriptions) { scanned++; value.DetailAddressEncrypted = protector.Protect(PersonalDataNormalizer.Text(value.DetailAddress!)); if (protector.Unprotect(value.DetailAddressEncrypted) != PersonalDataNormalizer.Text(value.DetailAddress!)) throw new InvalidOperationException("PRIVACY_BACKFILL_VERIFY_FAILED"); value.PrivacyProtectionVersion = 1; updated++; verified++; }
            await db.SaveChangesAsync(token);
        }
        return new(scanned, updated, verified);
    }
}

public sealed class PrivacyBackfillJob(PrivacyBackfillService service) : IAutomationJob
{
    public string Name => "PRIVACY_BACKFILL";
    public AutomationConfigurationStatus ConfigurationStatus => service.IsReady ? AutomationConfigurationStatus.Disabled : AutomationConfigurationStatus.NotConfigured;
    public TimeSpan Interval => TimeSpan.FromMinutes(5);
    public async Task<AutomationJobResult> ExecuteAsync(CancellationToken token) { var result = await service.BackfillBatchAsync(token); return new(result.UpdatedCount); }
}
