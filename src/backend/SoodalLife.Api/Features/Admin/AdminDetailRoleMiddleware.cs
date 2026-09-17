using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminDetailRoleMiddleware(SoodalLifeDbContext db,IWebHostEnvironment environment):IMiddleware
{
    public async Task InvokeAsync(HttpContext context,RequestDelegate next)
    {
        var path=context.Request.Path.Value??string.Empty;if(!path.StartsWith("/api/v1/admin",StringComparison.OrdinalIgnoreCase)||!context.User.IsInRole(RoleCodes.Admin)){await next(context);return;}
        if(!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),out var userPublic)){context.Response.StatusCode=401;return;}
        var role=await(from user in db.Users.AsNoTracking() join profile in db.AdminSecurityProfiles.AsNoTracking() on user.Id equals profile.UserId where user.PublicId==userPublic select profile.DetailRoleCode).SingleOrDefaultAsync(context.RequestAborted);
        if(role is null&&environment.IsEnvironment("Testing")){await next(context);return;}
        var allowed=Allowed(path);if(role is null||!allowed.Contains(role)){context.Response.StatusCode=403;await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(context,"ADMIN_DETAIL_ROLE_REQUIRED","이 업무에 접근할 세부 관리자 권한이 없습니다."));return;}await next(context);
    }
    private static string[] Allowed(string path)
    {
        if(path.Contains("/security")||path.Contains("/system")||path.Contains("/audit-logs"))return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.Security,AdminDetailRoles.Operations];
        if(path.Contains("/settlement")||path.Contains("/wallet")||path.Contains("/credit")||path.Contains("/pricing")||path.Contains("/fee"))return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.Finance];
        if(path.Contains("/notification")||path.Contains("/content")||path.Contains("/campaign"))return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.Content,AdminDetailRoles.Operations];
        if(path.Contains("/dashboard")||path.Contains("/analytics"))return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.Analyst,AdminDetailRoles.Finance,AdminDetailRoles.Operations];
        if(path.Contains("/customer")||path.Contains("/provider")||path.Contains("/report")||path.Contains("/dispute")||path.Contains("/review")||path.Contains("/sanction"))return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.CustomerSupport,AdminDetailRoles.Operations];
        return[AdminDetailRoles.SuperAdmin,AdminDetailRoles.Operations];
    }
}
