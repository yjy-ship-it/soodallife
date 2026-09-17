using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.LivingContent;

namespace SoodalLife.Api.Controllers;

[ApiController,AllowAnonymous,Route("api/v1/public/living-home")]
public sealed class LivingContentController(LivingContentService service):ControllerBase
{
    [HttpGet]
    public Task<LivingHomeResponse> Get(CancellationToken token)=>service.GetAsync(User,token);
}
