using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(SoodalLifeDbContext dbContext) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemHealthResponse> GetHealth()
    {
        return Ok(new SystemHealthResponse("Soodal Life API", "ok"));
    }

    [HttpGet("ready")]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SystemHealthResponse>> GetReadiness(CancellationToken cancellationToken)
    {
        try
        {
            var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
            return connected
                ? Ok(new SystemHealthResponse("Soodal Life API", "ready"))
                : StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new SystemHealthResponse("Soodal Life API", "database_unavailable"));
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new SystemHealthResponse("Soodal Life API", "database_unavailable"));
        }
    }
}

public sealed record SystemHealthResponse(string Service, string Status);
