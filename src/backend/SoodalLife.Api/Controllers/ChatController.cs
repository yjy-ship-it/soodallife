using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Chat;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/chat"), Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
public sealed class ChatController(ChatService service) : ControllerBase
{
    [HttpGet("rooms")]
    public Task<ActionResult<IReadOnlyList<ChatRoomListItem>>> Rooms(CancellationToken token) => Run(() => service.Mine(User, token));

    [HttpGet("rooms/{id:guid}")]
    public Task<ActionResult<ChatRoomDetail>> Room(Guid id, CancellationToken token) => Run(() => service.Detail(id, User, token));

    [HttpGet("transactions/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> TransactionRoom(Guid id, CancellationToken token) => Run(() => service.EnsureForTransactionAsync(User, id, token));

    [HttpGet("rooms/{id:guid}/messages")]
    public Task<ActionResult<ChatMessagePage>> Messages(Guid id, [FromQuery] Guid? before, [FromQuery] int pageSize = 30, CancellationToken token = default) =>
        Run(() => service.Messages(id, before, pageSize, User, token));

    [HttpPost("rooms/{id:guid}/messages/text")]
    public Task<ActionResult<ChatMessageResponse>> Text(Guid id, SendChatTextInput input, CancellationToken token) => Run(() => service.SendText(id, input, User, token));

    [HttpPost("rooms/{id:guid}/messages/file"), RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<ChatMessageResponse>> FileMessage(Guid id, [FromForm] IFormFile file, [FromForm] string idempotencyKey, CancellationToken token) =>
        Run(() => service.SendFile(id, file, idempotencyKey, User, token));

    [HttpPost("rooms/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, MarkChatReadInput input, CancellationToken token)
    {
        try { await service.MarkRead(id, input, User, token); return NoContent(); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }

    [HttpGet("unread-count")]
    public Task<ActionResult<ChatUnreadCountResponse>> Unread(CancellationToken token) => Run(() => service.Unread(User, token));

    [HttpGet("rooms/{roomId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Attachment(Guid roomId, Guid attachmentId, CancellationToken token)
    {
        try { var value = await service.OpenAttachment(roomId, attachmentId, User, token); return File(value.Stream, value.ContentType, value.FileName); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
    }
}
