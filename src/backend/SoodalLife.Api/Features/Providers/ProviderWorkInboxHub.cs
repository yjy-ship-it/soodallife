using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Features.Providers;

[Authorize(Roles = RoleCodes.Provider)]
public sealed class ProviderWorkInboxHub : Hub
{
    public const string AllProvidersGroup = "provider-work-inbox";

    public static string ProviderGroup(Guid userPublicId) => $"provider-work-inbox:{userPublicId:N}";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AllProvidersGroup, Context.ConnectionAborted);
        if (Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userPublicId))
            await Groups.AddToGroupAsync(Context.ConnectionId, ProviderGroup(userPublicId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}

public sealed class ProviderWorkInboxNotifier(
    IHubContext<ProviderWorkInboxHub> hub,
    ILogger<ProviderWorkInboxNotifier> logger)
{
    public async Task NotifyAllAsync(string reason, CancellationToken token)
    {
        try
        {
            await hub.Clients.Group(ProviderWorkInboxHub.AllProvidersGroup)
                .SendAsync("InboxChanged", new { reason, occurredAt = DateTime.UtcNow }, token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Provider work inbox realtime notification failed for {Reason}.", reason);
        }
    }

    public async Task NotifyProviderAsync(Guid userPublicId, string reason, CancellationToken token, Guid? requestId = null)
    {
        try
        {
            await hub.Clients.Group(ProviderWorkInboxHub.ProviderGroup(userPublicId))
                .SendAsync("InboxChanged", new { reason, requestId, occurredAt = DateTime.UtcNow }, token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Provider work inbox realtime notification failed for {UserId} and {Reason}.", userPublicId, reason);
        }
    }
}
