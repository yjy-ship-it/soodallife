using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed record IdentityVerificationStatus(string StatusCode, bool IsVerified);
public sealed record PhoneIdentityVerificationResult(string StatusCode, bool IsVerified, string? VerifiedPhone);

public interface IIdentityVerificationAdapter
{
    Task<IdentityVerificationStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<PhoneIdentityVerificationResult> VerifyPhoneAsync(string phone, string? verificationToken, CancellationToken cancellationToken);
}

public sealed class NotIntegratedIdentityVerificationAdapter : IIdentityVerificationAdapter
{
    public Task<IdentityVerificationStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new IdentityVerificationStatus("NOT_INTEGRATED", false));

    public Task<PhoneIdentityVerificationResult> VerifyPhoneAsync(string phone, string? verificationToken, CancellationToken cancellationToken) =>
        Task.FromResult(new PhoneIdentityVerificationResult("NOT_INTEGRATED", false, null));
}

public sealed class NiceIdentityVerificationOptions
{
    public const string SectionName = "NiceIdentityVerification";
    public string Mode { get; set; } = "DISABLED";
    public string VerificationUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 10;
}

public sealed record NiceTestVerificationRequest(string Phone, string TestCode);
public sealed record NiceTestVerificationResponse(string VerificationToken, DateTime ExpiresAt);

public interface INiceTestVerificationIssuer
{
    NiceTestVerificationResponse Issue(string phone, string testCode);
}

public sealed class NiceIdentityVerificationAdapter(
    HttpClient client,
    IDataProtectionProvider protectionProvider,
    IOptions<NiceIdentityVerificationOptions> configured) : IIdentityVerificationAdapter, INiceTestVerificationIssuer
{
    private readonly NiceIdentityVerificationOptions options = configured.Value;
    private readonly IDataProtector protector = protectionProvider.CreateProtector("SoodalLife.NiceIdentityVerification.v1");

    public Task<IdentityVerificationStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var mode = Mode();
        var ready = mode == "TEST" ? !string.IsNullOrWhiteSpace(options.TestCode) :
            mode == "PRODUCTION" && Uri.TryCreate(options.VerificationUrl, UriKind.Absolute, out _) &&
            !string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret);
        return Task.FromResult(new IdentityVerificationStatus(ready ? mode == "TEST" ? "TEST_READY" : "READY" : "NOT_INTEGRATED", false));
    }

    public async Task<PhoneIdentityVerificationResult> VerifyPhoneAsync(string phone, string? verificationToken, CancellationToken cancellationToken)
    {
        if (Mode() == "TEST") return VerifyTest(phone, verificationToken);
        if (Mode() != "PRODUCTION" || string.IsNullOrWhiteSpace(verificationToken))
            return new("NOT_INTEGRATED", false, null);
        if (!Uri.TryCreate(options.VerificationUrl, UriKind.Absolute, out var endpoint) ||
            string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
            return new("NOT_INTEGRATED", false, null);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("X-Client-Id", options.ClientId);
        request.Headers.TryAddWithoutValidation("X-Client-Secret", options.ClientSecret);
        request.Content = JsonContent.Create(new { token = verificationToken, phone });
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return new("NICE_REJECTED", false, null);
            var result = await response.Content.ReadFromJsonAsync<NiceBridgeResult>(cancellationToken: cancellationToken);
            return result is { IsVerified: true } ? new("VERIFIED", true, result.Phone) : new("NICE_REJECTED", false, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new("NICE_UNAVAILABLE", false, null);
        }
    }

    public NiceTestVerificationResponse Issue(string phone, string testCode)
    {
        if (Mode() != "TEST" || string.IsNullOrWhiteSpace(options.TestCode) || !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(options.TestCode), System.Text.Encoding.UTF8.GetBytes(testCode ?? string.Empty)))
            throw new InvalidOperationException("NICE test verification is not available.");
        var expiresAt = DateTime.UtcNow.AddMinutes(Math.Clamp(options.TokenLifetimeMinutes, 1, 30));
        return new(protector.Protect($"{Digits(phone)}|{expiresAt.Ticks}"), expiresAt);
    }

    private PhoneIdentityVerificationResult VerifyTest(string phone, string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token)) return new("TEST_TOKEN_REQUIRED", false, null);
            var parts = protector.Unprotect(token).Split('|');
            var valid = parts.Length == 2 && parts[0] == Digits(phone) && long.TryParse(parts[1], out var ticks) && new DateTime(ticks, DateTimeKind.Utc) >= DateTime.UtcNow;
            return valid ? new("VERIFIED", true, phone) : new("TEST_TOKEN_INVALID", false, null);
        }
        catch { return new("TEST_TOKEN_INVALID", false, null); }
    }

    private string Mode() => options.Mode.Trim().ToUpperInvariant();
    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
    private sealed record NiceBridgeResult(bool IsVerified, string? Phone, string? TransactionId);
}

public interface IPasswordResetDeliveryAdapter
{
    string StatusCode { get; }
    Task DeliverAsync(string maskedDestination, string token, CancellationToken cancellationToken);
}

public sealed class NotIntegratedPasswordResetDeliveryAdapter : IPasswordResetDeliveryAdapter
{
    public string StatusCode => "NOT_INTEGRATED";
    public Task DeliverAsync(string maskedDestination, string token, CancellationToken cancellationToken) => Task.CompletedTask;
}
