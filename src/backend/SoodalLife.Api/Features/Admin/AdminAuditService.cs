using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminAuditService(SoodalLifeDbContext dbContext)
{
    public async Task RecordAuthenticationAsync(
        Guid userPublicId,
        string actionCode,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = await dbContext.Users
            .Where(user => user.PublicId == userPublicId)
            .Select(user => (long?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);

        dbContext.AuditLogs.Add(new AuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = userId,
            ActorRoleCode = RoleCodes.Admin,
            ActionCode = actionCode,
            EntityType = "USER",
            EntityPublicId = userPublicId,
            ResultCode = "SUCCESS",
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString() is { Length: > 0 } userAgent
                ? userAgent[..Math.Min(userAgent.Length, 1000)]
                : null,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
