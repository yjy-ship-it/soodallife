using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace SoodalLife.Api.Infrastructure.Security;

public sealed class PrivacyProtectionOptions
{
    public const string SectionName = "PrivacyProtection";
    public bool DualWriteEnabled { get; set; }
    public string? SearchHashKey { get; set; }
    public bool EncryptedReadEnabled { get; set; }
    public int BackfillBatchSize { get; set; } = 200;
}

public interface IPersonalDataReader
{
    string? Read(byte[]? encrypted, string? plaintext);
}

public sealed class EncryptedFirstPersonalDataReader(IPersonalDataProtector protector, IOptions<PrivacyProtectionOptions> options) : IPersonalDataReader
{
    public string? Read(byte[]? encrypted, string? plaintext) =>
        options.Value.EncryptedReadEnabled && encrypted is { Length: > 0 } ? protector.Unprotect(encrypted) : plaintext;
}

public static class PersonalDataNormalizer
{
    public static string Email(string value) => value.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
    public static string Phone(string value) => new(value.Where(char.IsDigit).ToArray());
    public static string Text(string value) => value.Trim().Normalize(NormalizationForm.FormKC);
}

public interface IPersonalDataProtector
{
    byte[] Protect(string value);
    string Unprotect(byte[] value);
}

public sealed class DataProtectionPersonalDataProtector(IDataProtectionProvider provider) : IPersonalDataProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SoodalLife.PersonalData.v1");
    public byte[] Protect(string value) => _protector.Protect(Encoding.UTF8.GetBytes(value));
    public string Unprotect(byte[] value) => Encoding.UTF8.GetString(_protector.Unprotect(value));
}

public interface IPersonalDataSearchHasher
{
    bool IsConfigured { get; }
    byte[] Email(string value);
    byte[] Phone(string value);
}

public sealed class HmacPersonalDataSearchHasher(IOptions<PrivacyProtectionOptions> options) : IPersonalDataSearchHasher
{
    private readonly byte[]? _key = Decode(options.Value.SearchHashKey);
    public bool IsConfigured => _key is { Length: >= 32 };
    public byte[] Email(string value) => Hash("email", PersonalDataNormalizer.Email(value));
    public byte[] Phone(string value) => Hash("phone", PersonalDataNormalizer.Phone(value));

    private byte[] Hash(string purpose, string normalized)
    {
        if (!IsConfigured) throw new InvalidOperationException("PrivacyProtection search hash key is not configured.");
        using var hmac = new HMACSHA256(_key!);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}\0{normalized}"));
    }

    private static byte[]? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return Convert.FromBase64String(value); }
        catch (FormatException) { throw new InvalidOperationException("PrivacyProtection search hash key must be valid Base64."); }
    }
}

public static class PrivacyAudience
{
    public const string Public = "PUBLIC";
    public const string CustomerSelf = "CUSTOMER_SELF";
    public const string SelectedProvider = "SELECTED_PROVIDER";
    public const string AssignedProvider = "ASSIGNED_PROVIDER";
    public const string AdminMasked = "ADMIN_MASKED";
    public const string AdminAuthorized = "ADMIN_AUTHORIZED";
}

public enum PrivacyField { Email, Phone, DetailAddress }
public sealed record PrivacyDecision(bool CanAccess, bool MustMask, string Audience, string ReasonCode);

public interface IPrivacyContract
{
    PrivacyDecision Decide(string audience, PrivacyField field, bool isAssignedParty = false);
}

public sealed class PrivacyContract : IPrivacyContract
{
    public PrivacyDecision Decide(string audience, PrivacyField field, bool isAssignedParty = false) => audience switch
    {
        PrivacyAudience.CustomerSelf => new(true, false, audience, "SELF_ACCESS"),
        PrivacyAudience.SelectedProvider or PrivacyAudience.AssignedProvider when isAssignedParty && field is (PrivacyField.Phone or PrivacyField.DetailAddress)
            => new(true, false, audience, "ASSIGNED_WORK_REQUIRED"),
        PrivacyAudience.AdminMasked => new(true, true, audience, "ADMIN_DEFAULT_MASKED"),
        PrivacyAudience.AdminAuthorized => new(true, false, audience, "ADMIN_REAUTH_AUDIT_REQUIRED"),
        _ => new(false, false, audience, "PERSONAL_DATA_NOT_DISCLOSED"),
    };
}
