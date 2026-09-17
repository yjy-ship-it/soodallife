using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Chat;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/chat"), Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Provider)]
public sealed class ChatController(ChatService service, ILogger<ChatController> logger) : ControllerBase
{
    [HttpGet("rooms")]
    public Task<ActionResult<IReadOnlyList<ChatRoomListItem>>> Rooms([FromQuery] string? resourceType, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30, CancellationToken token = default) => Run(() => service.Mine(User, resourceType, page, pageSize, token));

    [HttpGet("rooms/{id:guid}")]
    public Task<ActionResult<ChatRoomDetail>> Room(Guid id, CancellationToken token) => Run(() => service.Detail(id, User, token));

    [HttpGet("transactions/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> TransactionRoom(Guid id, CancellationToken token) => Run(() => service.EnsureForTransactionAsync(User, id, token));

    [HttpGet("subscriptions/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> SubscriptionRoom(Guid id, CancellationToken token) =>
        Run(() => service.EnsureForResourceAsync(User, ChatResourceTypes.Subscription, id, "DIRECT", token));

    [HttpGet("subscription-visits/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> SubscriptionVisitRoom(Guid id, CancellationToken token) =>
        Run(() => service.EnsureForSubscriptionVisitAsync(User, id, token));

    [HttpGet("interiors/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> InteriorRoom(Guid id, [FromQuery] string role = "PRIMARY_CONTRACTOR", CancellationToken token = default)
    {
        var normalized = role.Trim().ToUpperInvariant();
        if (normalized is not ("PRIMARY_CONTRACTOR" or "SITE_SURVEY"))
            return Task.FromResult<ActionResult<ChatRoomDetail>>(BadRequest(new { code = "CHAT_INTERIOR_ROLE_INVALID", message = "지원하지 않는 인테리어 채팅 역할입니다." }));
        return Run(() => service.EnsureForResourceAsync(User, ChatResourceTypes.Interior, id, normalized, token));
    }

    [HttpGet("after-services/{id:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> AfterServiceRoom(Guid id, CancellationToken token) =>
        Run(() => service.EnsureForResourceAsync(User, ChatResourceTypes.AfterService, id, "DIRECT", token));

    [Authorize(Roles = RoleCodes.Customer), HttpPost("provider-consultations/{providerId:guid}/room")]
    public Task<ActionResult<ChatRoomDetail>> ProviderConsultationRoom(Guid providerId, CancellationToken token) =>
        Run(() => service.EnsureProviderConsultationAsync(User, providerId, token));

    [HttpGet("rooms/{id:guid}/messages")]
    public Task<ActionResult<ChatMessagePage>> Messages(Guid id, [FromQuery] Guid? before, [FromQuery] int pageSize = 30, CancellationToken token = default) =>
        Run(() => service.Messages(id, before, pageSize, User, token));

    [HttpPost("rooms/{id:guid}/messages/text")]
    public Task<ActionResult<ChatMessageResponse>> Text(Guid id, SendChatTextInput input, CancellationToken token) => Run(() => service.SendText(id, input, User, token));

    [HttpPost("rooms/{id:guid}/messages/file"), RequestSizeLimit(5 * 1024 * 1024 + 64 * 1024)]
    public Task<ActionResult<ChatMessageResponse>> FileMessage(Guid id, [FromForm] IFormFile file, [FromForm] string idempotencyKey, CancellationToken token) =>
        Run(() => service.SendFile(id, file, idempotencyKey, User, token));

    [HttpPost("rooms/{id:guid}/messages/file-data"), RequestSizeLimit(16 * 1024 * 1024)]
    public Task<ActionResult<ChatMessageResponse>> FileDataMessage(Guid id, SendChatFileInput input, CancellationToken token) =>
        Run(() => service.SendFileData(id, input, User, token));

    [HttpPost("rooms/{id:guid}/messages/file-chunks"), RequestSizeLimit(128 * 1024)]
    public Task<ActionResult<ChatFileChunkResponse>> FileChunkMessage(Guid id, SendChatFileChunkInput input, CancellationToken token) =>
        Run(() => service.SendFileChunk(id, input, User, token));

    [HttpPost("rooms/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, MarkChatReadInput input, CancellationToken token)
    {
        try { await service.MarkRead(id, input, User, token); return NoContent(); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
        catch (Exception error) { return Unexpected(error); }
    }

    [HttpGet("unread-count")]
    public Task<ActionResult<ChatUnreadCountResponse>> Unread(CancellationToken token) => Run(() => service.Unread(User, token));

    [HttpGet("rooms/{roomId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Attachment(Guid roomId, Guid attachmentId, CancellationToken token)
    {
        try { var value = await service.OpenAttachment(roomId, attachmentId, User, token); return File(value.Stream, value.ContentType, value.FileName); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
        catch (Exception error) { return Unexpected(error); }
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (ChatBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); }
        catch (Exception error) { return Unexpected(error); }
    }

    private ObjectResult Unexpected(Exception error)
    {
        var traceId = HttpContext.TraceIdentifier;
        var category = error switch
        {
            UnauthorizedAccessException => "STORAGE_ACCESS",
            IOException => "STORAGE_IO",
            DbUpdateException => "DATABASE_WRITE",
            _ => "UNEXPECTED",
        };
        logger.LogError(error, "Unhandled chat request error {TraceId} ({Category}) at {Path}.", traceId, category, Request.Path.Value);
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            code = "CHAT_SERVER_ERROR",
            message = $"채팅 처리 중 서버 오류가 발생했습니다. 오류번호: {traceId} ({category})",
            traceId,
        });
    }
}
