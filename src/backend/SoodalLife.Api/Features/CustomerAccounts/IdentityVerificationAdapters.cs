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
