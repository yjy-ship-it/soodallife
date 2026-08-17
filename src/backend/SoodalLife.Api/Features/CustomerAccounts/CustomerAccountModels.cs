using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed record AvailabilityResponse(bool Available, string NormalizedValue);
public sealed record RegistrationConsentRequest(Guid LegalDocumentVersionId, bool Agreed);
public sealed record RegisterCustomerRequest(
    [param: Required, StringLength(256, MinimumLength = 4)] string LoginId,
    [param: Required, StringLength(100, MinimumLength = 2)] string Name,
    [param: EmailAddress, StringLength(320)] string? Email,
    [param: Required, StringLength(32)] string Phone,
    [param: Required, StringLength(128, MinimumLength = 6)] string Password,
    [param: Required, StringLength(128, MinimumLength = 6)] string PasswordConfirmation,
    [param: StringLength(2048)] string? PhoneVerificationToken,
    [param: Required] IReadOnlyList<RegistrationConsentRequest> Consents);

public sealed record CustomerRegistrationResponse(Guid UserId, string LoginId, IReadOnlyList<string> Roles);
public sealed record LegalDocumentResponse(Guid Id, Guid VersionId, string Code, string RequirementCode, string Title, string Content, int VersionNo, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPlaceholder);
public sealed record CustomerProfileResponse(string Name, string LoginId, string? Email, string? Phone, string EmailVerificationStatus, string PhoneVerificationStatus, string AccountStatus, DateTime CreatedAt, DateTime? LastLoginAt);
public sealed record UpdateCustomerProfileRequest([param: Required, StringLength(100, MinimumLength = 2)] string Name, [param: EmailAddress, StringLength(320)] string? Email, [param: StringLength(32)] string? Phone);
public sealed record CustomerAddressResponse(Guid Id, string AddressName, string? RecipientName, string PostalCode, string RoadAddress, string DetailAddress, Guid? AdministrativeAreaId, string? AdministrativeAreaName, decimal? Latitude, decimal? Longitude, bool IsDefault, string ConcurrencyToken);
public sealed record SaveCustomerAddressRequest([param: Required, StringLength(100)] string AddressName, [param: StringLength(100)] string? RecipientName, [param: Required, StringLength(10)] string PostalCode, [param: Required, StringLength(500)] string RoadAddress, [param: Required, StringLength(500)] string DetailAddress, Guid? AdministrativeAreaId, decimal? Latitude, decimal? Longitude, bool IsDefault, string? ConcurrencyToken);
public sealed record ChangePasswordRequest([param: Required] string CurrentPassword, [param: Required, StringLength(128, MinimumLength = 6)] string NewPassword, [param: Required] string NewPasswordConfirmation);
public sealed record PasswordResetRequestInput([param: Required, StringLength(320)] string LoginOrEmail);
public sealed record ConfirmPasswordResetRequest([param: Required] string Token, [param: Required, StringLength(128, MinimumLength = 6)] string NewPassword, [param: Required] string NewPasswordConfirmation);
public sealed record ConsentResponse(Guid LegalDocumentVersionId, string Code, string RequirementCode, string Title, string Content, int VersionNo, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPlaceholder, bool CanWithdraw, string ConsentStatus, DateTime? ConsentedAt, DateTime? WithdrawnAt);
public sealed record UpdateConsentRequest(Guid LegalDocumentVersionId, bool Agreed);
public sealed record CreateWithdrawalRequest([param: Required] string ScopeCode, [param: StringLength(1000)] string? Reason);
public sealed record WithdrawalResponse(Guid Id, string ScopeCode, string StatusCode, DateTime RequestedAt);

public sealed class CustomerAccountException(int statusCode, string businessCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string BusinessCode { get; } = businessCode;
}
