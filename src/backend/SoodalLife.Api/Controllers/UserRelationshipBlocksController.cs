using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.RelationshipBlocks;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/customers/me/provider-blocks"), Authorize(Roles = RoleCodes.Customer)]
public sealed class CustomerProviderBlocksController(UserRelationshipBlockService service) : ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<ProviderBlockResponse>>> List(CancellationToken token) => Run(() => service.CustomerList(User, token));
    [HttpPost] public Task<ActionResult<ProviderBlockResponse>> Create(CreateProviderBlockRequest input, CancellationToken token) => Run(() => service.Create(User, input, token));
    [HttpPost("{id:guid}/release")] public Task<ActionResult<ProviderBlockResponse>> Release(Guid id, ReleaseProviderBlockRequest input, CancellationToken token) => Run(() => service.Release(User, id, input, token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); } }
}

[ApiController, Route("api/v1/providers/me/customer-blocks"), Authorize(Roles = RoleCodes.Provider)]
public sealed class ProviderCustomerBlocksController(UserRelationshipBlockService service) : ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<ProviderReceivedBlockResponse>>> List(CancellationToken token) => Run(() => service.ProviderList(User, token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); } }
}

[ApiController, Route("api/v1/admin/user-blocks"), Authorize(Roles = RoleCodes.Admin)]
public sealed class AdminUserRelationshipBlocksController(UserRelationshipBlockService service) : ControllerBase
{
    [HttpGet] public Task<ActionResult<IReadOnlyList<AdminProviderBlockResponse>>> List([FromQuery] string? status, CancellationToken token) => Run(() => service.AdminList(status, token));
    [HttpGet("{id:guid}")] public Task<ActionResult<AdminProviderBlockResponse>> Detail(Guid id, CancellationToken token) => Run(() => service.AdminDetail(id, token));
    [HttpPost("{id:guid}/release")] public Task<ActionResult<AdminProviderBlockResponse>> Release(Guid id, AdminReleaseProviderBlockRequest input, CancellationToken token) => Run(() => service.AdminRelease(User, id, input, token));
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (WorkBusinessException error) { return StatusCode(error.StatusCode, new { code = error.BusinessCode, message = error.Message }); } }
}
