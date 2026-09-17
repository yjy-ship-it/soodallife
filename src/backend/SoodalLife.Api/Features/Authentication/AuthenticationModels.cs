using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Authentication;

public static class RoleCodes
{
    public const string Customer = "CUSTOMER";
    public const string Provider = "PROVIDER";
    public const string Admin = "ADMIN";

    public static readonly string[] All = [Customer, Provider, Admin];
}

public static class AuthenticationConstants
{
    public const string Scheme = "SoodalLifeCookie";
    public const string CookieName = "SoodalLife.Auth";
}

public sealed record LoginRequest(
    [param: Required, StringLength(320, MinimumLength = 1)] string LoginOrEmail,
    [param: Required, StringLength(1024, MinimumLength = 1)] string Password,
    [param: StringLength(6, MinimumLength = 6)] string? MfaCode = null,
    bool RememberMe = false);

public sealed record AuthenticatedUserResponse(
    Guid PublicId,
    string LoginId,
    IReadOnlyList<string> Roles);

public sealed record AccessCheckResponse(string Role, string Message);

public sealed record ApiErrorResponse(
    string BusinessCode,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors,
    string TraceId)
{
    public static ApiErrorResponse Create(HttpContext context, string businessCode, string message) =>
        new(businessCode, message, null, context.TraceIdentifier);
}
