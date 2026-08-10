using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Admin;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthenticationController(
    SoodalLife.Api.Features.Authentication.IAuthenticationService authenticationService,
    AdminAuditService adminAuditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticatedUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authenticationService.AuthenticateAsync(request.LoginOrEmail, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiErrorResponse.Create(
                HttpContext,
                "AUTH_INVALID_CREDENTIALS",
                "아이디 또는 비밀번호를 확인해 주세요."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
            new(ClaimTypes.Name, user.LoginId),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationConstants.Scheme));
        await HttpContext.SignInAsync(
            AuthenticationConstants.Scheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });

        if (user.Roles.Contains(RoleCodes.Admin, StringComparer.Ordinal))
        {
            await adminAuditService.RecordAuthenticationAsync(
                user.PublicId,
                "ADMIN_LOGIN",
                HttpContext,
                cancellationToken);
        }

        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userPublicId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(RoleCodes.Admin);
        await HttpContext.SignOutAsync(AuthenticationConstants.Scheme);
        if (isAdmin)
        {
            await adminAuditService.RecordAuthenticationAsync(
                userPublicId,
                "ADMIN_LOGOUT",
                HttpContext,
                cancellationToken);
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/me")]
public sealed class CurrentUserController : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType<AuthenticatedUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<AuthenticatedUserResponse> Get()
    {
        var publicId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var loginId = User.FindFirstValue(ClaimTypes.Name)!;
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        return Ok(new AuthenticatedUserResponse(publicId, loginId, roles));
    }
}
