using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Infrastructure.Authentication;

public sealed class ActiveUserCookieEvents(SoodalLifeDbContext dbContext) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var publicIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(publicIdValue, out var publicId))
        {
            await RejectAsync(context);
            return;
        }

        var activeRoleCodes = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.PublicId == publicId && user.StatusCode == "ACTIVE" && userRole.RevokedAt == null && role.IsActive
                select role.Code)
            .Distinct()
            .ToListAsync(context.HttpContext.RequestAborted);
        if (activeRoleCodes.Count == 0)
        {
            await RejectAsync(context);
            return;
        }
        var activeRoles = activeRoleCodes.ToHashSet(StringComparer.Ordinal);

        var claimedRoles = context.Principal?.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        if (activeRoles.Count == 0 || !activeRoles.SetEquals(claimedRoles))
        {
            await RejectAsync(context);
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) =>
        WriteErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        WriteErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden, "ACCESS_DENIED", "접근 권한이 없습니다.");

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthenticationConstants.Scheme);
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string businessCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(context, businessCode, message));
    }
}
