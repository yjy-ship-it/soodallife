using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Features.Chat;

[Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
public sealed class ChatHub(ChatService service) : Hub
{
    public async Task JoinRoom(Guid roomId, CancellationToken token)
    {
        if (!await service.CanJoin(roomId, Context.User!, token)) throw new HubException("CHAT_ROOM_NOT_FOUND");
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatService.Group(roomId), token);
    }

    public Task LeaveRoom(Guid roomId, CancellationToken token) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatService.Group(roomId), token);
}
