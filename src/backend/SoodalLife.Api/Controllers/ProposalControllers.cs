using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Proposals;

namespace SoodalLife.Api.Controllers;

[ApiController, Route("api/v1/public/proposals")]
public sealed class PublicProposalsController(ProposalService service) : ControllerBase
{
    [HttpGet("policy-guide")] public ActionResult Guide() => Ok(ProposalService.Guide());
    [HttpGet] public Task<ActionResult> List([FromQuery] Guid? categoryId, [FromQuery] Guid? areaId, [FromQuery] int take = 20, CancellationToken token = default) => Execute(() => service.PublicList(categoryId, areaId, take, User, token));
    [HttpGet("{id:guid}")] public async Task<ActionResult> Detail(Guid id, CancellationToken token) { var value = await service.PublicDetail(id, User, token); return value is null ? NotFound(ApiErrorResponse.Create(HttpContext, "PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.")) : Ok(value); }
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (ProposalException e) { return StatusCode(e.StatusCode, ApiErrorResponse.Create(HttpContext, e.BusinessCode, e.Message)); } }
}

[ApiController, Authorize(Roles = RoleCodes.Provider), Route("api/v1/providers/me/proposals")]
public sealed class ProviderProposalsController(ProposalService service) : ControllerBase
{
    [HttpGet("setup")] public Task<ActionResult> Setup(CancellationToken token) => Execute(() => service.Setup(User, token));
    [HttpGet] public Task<ActionResult> List(CancellationToken token) => Execute(() => service.ProviderList(User, token));
    [HttpPost] public Task<ActionResult> Create(SaveProviderProposalRequest input, CancellationToken token) => Execute(() => service.Create(User, input, token));
    [HttpPut("{id:guid}")] public Task<ActionResult> Update(Guid id, SaveProviderProposalRequest input, CancellationToken token) => Execute(() => service.Update(User, id, input, token));
    [HttpGet("{id:guid}/applications")] public Task<ActionResult> Applications(Guid id, CancellationToken token) => Execute(() => service.Applications(User, id, token));
    [HttpPost("{id:guid}/applications/{applicationId:guid}/confirm")] public Task<ActionResult> Confirm(Guid id, Guid applicationId, CancellationToken token) => Execute(() => service.Confirm(User, id, applicationId, token));
    [HttpPost("{id:guid}/applications/{applicationId:guid}/decline")] public Task<ActionResult> Decline(Guid id, Guid applicationId, CancellationToken token) => Execute(() => service.Decline(User, id, applicationId, token));
    [HttpPost("{id:guid}/cancel")] public Task<ActionResult> Cancel(Guid id, CancellationToken token) => Execute(() => service.CancelCampaign(User, id, token));
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (ProposalException e) { return StatusCode(e.StatusCode, ApiErrorResponse.Create(HttpContext, e.BusinessCode, e.Message)); } }
}

[ApiController, Authorize(Roles = RoleCodes.Customer), Route("api/v1/customers/me/proposals")]
public sealed class CustomerProposalsController(ProposalService service) : ControllerBase
{
    [HttpGet("applications")] public Task<ActionResult> Applications(CancellationToken token) => Execute(() => service.GetMyApplications(User, token));
    [HttpGet("interests")] public Task<ActionResult> Interests(CancellationToken token) => Execute(() => service.GetInterests(User, token));
    [HttpPut("interests")] public Task<ActionResult> SaveInterests(SaveProposalInterestRequest input, CancellationToken token) => Execute(() => service.SaveInterests(User, input, token));
    [HttpPost("signals")] public async Task<ActionResult> Signal(RecordProposalSignalRequest input, CancellationToken token) { try { await service.RecordSignal(User, input, token); return NoContent(); } catch (ProposalException e) { return StatusCode(e.StatusCode, ApiErrorResponse.Create(HttpContext, e.BusinessCode, e.Message)); } }
    [HttpPost("{id:guid}/applications")] public Task<ActionResult> Apply(Guid id, CancellationToken token) => Execute(() => service.Apply(User, id, token));
    [HttpDelete("{id:guid}/applications/me")] public Task<ActionResult> Cancel(Guid id, CancellationToken token) => Execute(() => service.CancelApplication(User, id, token));
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (ProposalException e) { return StatusCode(e.StatusCode, ApiErrorResponse.Create(HttpContext, e.BusinessCode, e.Message)); } }
}

[ApiController, Authorize(Roles = RoleCodes.Customer), Route("api/v1/customers/me/interested-services")]
public sealed class CustomerInterestedServicesController(ProposalService service) : ControllerBase
{
    [HttpGet] public Task<ActionResult> List(CancellationToken token) => Execute(() => service.GetInterestedServices(User, token));
    [HttpGet("{serviceId:guid}/state")] public Task<ActionResult> State(Guid serviceId, CancellationToken token) => Execute(() => service.GetInterestedServiceState(User, serviceId, token));
    [HttpPut("{serviceId:guid}")] public Task<ActionResult> Save(Guid serviceId, SaveInterestedServiceRequest input, CancellationToken token) => Execute(() => service.SaveInterestedService(User, serviceId, input.Interested, token));
    private async Task<ActionResult> Execute<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (ProposalException e) { return StatusCode(e.StatusCode, ApiErrorResponse.Create(HttpContext, e.BusinessCode, e.Message)); } }
}
