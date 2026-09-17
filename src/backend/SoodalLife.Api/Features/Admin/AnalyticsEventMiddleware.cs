using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AnalyticsEventMiddleware(IServiceScopeFactory scopeFactory,ILogger<AnalyticsEventMiddleware> logger):IMiddleware
{
    public async Task InvokeAsync(HttpContext context,RequestDelegate next)
    {
        var path=context.Request.Path.Value??string.Empty;if(!path.StartsWith("/api/",StringComparison.OrdinalIgnoreCase)||path.Contains("/admin/dashboard/management")||path.Contains("/admin/system/status")){await next(context);return;}
        var visitor=Visitor(context);var started=Stopwatch.GetTimestamp();Exception? failure=null;try{await next(context);}catch(Exception error){failure=error;throw;}finally
        {
            try
            {
                await using var scope=scopeFactory.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
                var duration=(int)Math.Min(int.MaxValue,Stopwatch.GetElapsedTime(started).TotalMilliseconds);long? userId=null;if(Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),out var userPublic))userId=await db.Users.AsNoTracking().Where(x=>x.PublicId==userPublic).Select(x=>(long?)x.Id).SingleOrDefaultAsync();
                var status=failure is null?context.Response.StatusCode:500;db.AnalyticsEvents.Add(new(){EventTypeCode="API_REQUEST",VisitorId=visitor,UserId=userId,RouteTemplate=context.GetEndpoint()?.DisplayName??path,HttpMethod=context.Request.Method,StatusCode=status,DurationMs=duration,OutcomeCode=status>=500?"FAILED":status>=400?"REJECTED":"SUCCEEDED",OccurredAt=DateTime.UtcNow});
                var business=BusinessEvent(context.Request.Method,path,status);if(business is not null)db.AnalyticsEvents.Add(new(){EventTypeCode=business,VisitorId=visitor,UserId=userId,RouteTemplate=path,HttpMethod=context.Request.Method,StatusCode=status,DurationMs=duration,OutcomeCode=status<400?"SUCCEEDED":"FAILED",OccurredAt=DateTime.UtcNow});await db.SaveChangesAsync();
            }
            catch(Exception telemetryError){logger.LogWarning(telemetryError,"Analytics event recording failed for {Path}",path);}
        }
    }
    private static string? BusinessEvent(string method,string path,int status)
    {
        if(status>=400)return null;if(method=="GET"&&(path.Contains("/catalog")||path.Contains("/services")))return "SERVICE_DISCOVERY_VIEWED";
        if(method=="POST"&&path.Contains("service-requests"))return "SERVICE_REQUEST_CREATED";
        if(method=="GET"&&path.Contains("/quotes/"))return "QUOTE_VIEWED";
        if((method=="POST"||method=="PUT")&&path.Contains("/quotes"))return "QUOTE_SUBMITTED";
        if(method=="POST"&&(path.Contains("complete")||path.Contains("confirm")))return "WORKFLOW_COMPLETED";return null;
    }
    private static Guid Visitor(HttpContext context)
    {
        const string name="SoodalLife.AnalyticsVisitor";if(context.Request.Cookies.TryGetValue(name,out var raw)&&Guid.TryParse(raw,out var existing))return existing;var created=Guid.NewGuid();if(!context.Response.HasStarted)context.Response.Cookies.Append(name,created.ToString(),new(){HttpOnly=true,Secure=context.Request.IsHttps,SameSite=SameSiteMode.Lax,Expires=DateTimeOffset.UtcNow.AddYears(1),IsEssential=false});return created;
    }
}
