using Microsoft.AspNetCore.Mvc;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemHealthResponse> GetHealth()
    {
        return Ok(new SystemHealthResponse("Soodal Life API", "ok"));
    }
}

public sealed record SystemHealthResponse(string Service, string Status);
