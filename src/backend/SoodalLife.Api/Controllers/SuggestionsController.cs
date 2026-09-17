using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.HelpRoom;
using SoodalLife.Api.Features.Work;
namespace SoodalLife.Api.Controllers;
[ApiController,Route("api/v1/suggestions"),Authorize]
public sealed class SuggestionsController(SuggestionService service):ControllerBase
{[HttpGet]public Task<ActionResult<IReadOnlyList<SuggestionResponse>>> Mine(CancellationToken t)=>Run(()=>service.Mine(User,t));[HttpGet("{id:guid}")]public Task<ActionResult<SuggestionResponse>> Detail(Guid id,CancellationToken t)=>Run(()=>service.Detail(User,id,t));[HttpPost]public Task<ActionResult<SuggestionResponse>> Create(CreateSuggestionInput input,CancellationToken t)=>Run(()=>service.Create(User,input,t));private async Task<ActionResult<T>> Run<T>(Func<Task<T>> a){try{return Ok(await a());}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}}
[ApiController,Route("api/v1/admin/suggestions"),Authorize(Roles=RoleCodes.Admin)]
public sealed class AdminSuggestionsController(SuggestionService service):ControllerBase
{[HttpGet]public Task<ActionResult<IReadOnlyList<SuggestionResponse>>> List([FromQuery]string? status,CancellationToken t)=>Run(()=>service.AdminList(User,status,t));[HttpGet("{id:guid}")]public Task<ActionResult<SuggestionResponse>> Detail(Guid id,CancellationToken t)=>Run(()=>service.Detail(User,id,t));[HttpPut("{id:guid}")]public Task<ActionResult<SuggestionResponse>> Update(Guid id,UpdateSuggestionInput input,CancellationToken t)=>Run(()=>service.AdminUpdate(User,id,input,t));private async Task<ActionResult<T>> Run<T>(Func<Task<T>> a){try{return Ok(await a());}catch(WorkBusinessException e){return StatusCode(e.StatusCode,new{code=e.BusinessCode,message=e.Message});}}}
