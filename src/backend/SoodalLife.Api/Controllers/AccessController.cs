using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController]
[Route("api/v1/access")]
public sealed class AccessController : ControllerBase
{
    [Authorize(Roles = RoleCodes.Customer)]
    [HttpGet("customer")]
    public ActionResult<AccessCheckResponse> Customer() =>
        Ok(new AccessCheckResponse(RoleCodes.Customer, "고객 접근 권한이 확인되었습니다."));

    [Authorize(Roles = RoleCodes.Provider)]
    [HttpGet("provider")]
    public ActionResult<AccessCheckResponse> Provider() =>
        Ok(new AccessCheckResponse(RoleCodes.Provider, "공급자 접근 권한이 확인되었습니다."));

    [Authorize(Roles = RoleCodes.Admin)]
    [HttpGet("admin")]
    public ActionResult<AccessCheckResponse> Admin() =>
        Ok(new AccessCheckResponse(RoleCodes.Admin, "관리자 접근 권한이 확인되었습니다."));
}
