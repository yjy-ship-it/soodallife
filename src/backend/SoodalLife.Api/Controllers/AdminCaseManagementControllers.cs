using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Controllers;

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/case-masters")]
public sealed class AdminCaseMastersController(AdminCaseManagementService service):CaseAdminController
{
    [HttpGet("report-types")]public Task<ActionResult> ReportTypes(CancellationToken token)=>Run(async()=>await service.GetReportTypesAsync(token));
    [HttpPost("report-types/initialize")]public Task<ActionResult> InitializeReportTypes(CancellationToken token)=>Run(async()=>await service.InitializeDefaultReportTypesAsync(Actor(),token));
    [HttpPost("report-types")]public Task<ActionResult> CreateReportType(CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveReportTypeAsync(null,request,Actor(),token));
    [HttpPut("report-types/{id:guid}")]public Task<ActionResult> UpdateReportType(Guid id,CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveReportTypeAsync(id,request,Actor(),token));
    [HttpGet("sanction-types")]public Task<ActionResult> SanctionTypes(CancellationToken token)=>Run(async()=>await service.GetSanctionTypesAsync(token));
    [HttpPost("sanction-types")]public Task<ActionResult> CreateSanctionType(CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveSanctionTypeAsync(null,request,Actor(),token));
    [HttpPut("sanction-types/{id:guid}")]public Task<ActionResult> UpdateSanctionType(Guid id,CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveSanctionTypeAsync(id,request,Actor(),token));
    [HttpGet("liability-types")]public Task<ActionResult> LiabilityTypes(CancellationToken token)=>Run(async()=>await service.GetLiabilityTypesAsync(token));
    [HttpPost("liability-types")]public Task<ActionResult> CreateLiabilityType(CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveLiabilityTypeAsync(null,request,Actor(),token));
    [HttpPut("liability-types/{id:guid}")]public Task<ActionResult> UpdateLiabilityType(Guid id,CaseMasterRequest request,CancellationToken token)=>Run(async()=>await service.SaveLiabilityTypeAsync(id,request,Actor(),token));
    [HttpGet("assignees")]public Task<ActionResult> Assignees(CancellationToken token)=>Run(async()=>await service.GetAdminAssigneesAsync(token));
}

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/reports")]
public sealed class AdminReportsController(AdminCaseManagementService service):CaseAdminController
{
    [HttpGet]public Task<ActionResult> Search([FromQuery]string? search,[FromQuery]string? status,[FromQuery]Guid? reportTypeId,[FromQuery]Guid? assignedAdminId,[FromQuery]DateOnly? receivedFrom,[FromQuery]DateOnly? receivedTo,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(async()=>await service.SearchReportsAsync(search,status,reportTypeId,assignedAdminId,receivedFrom,receivedTo,page,pageSize,token));
    [HttpGet("{id:guid}")]public async Task<ActionResult> Get(Guid id,CancellationToken token){var value=await service.GetReportAsync(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"REPORT_NOT_FOUND","신고를 찾을 수 없습니다.")):Ok(value);}
    [HttpPost]public Task<ActionResult> Create(CreateAdminReportRequest request,CancellationToken token)=>Run(async()=>await service.CreateReportAsync(request,Actor(),token));
    [HttpPost("{id:guid}/assignment")]public Task<ActionResult> Assign(Guid id,AssignReportRequest request,CancellationToken token)=>Run(async()=>await service.AssignReportAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/status")]public Task<ActionResult> Status(Guid id,ChangeReportStatusRequest request,CancellationToken token)=>Run(async()=>await service.ChangeReportStatusAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/evidence")]public Task<ActionResult> Evidence(Guid id,AddReportEvidenceRequest request,CancellationToken token)=>Run(async()=>await service.AddReportEvidenceAsync(id,request,Actor(),token));
}

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/sanctions")]
public sealed class AdminSanctionsController(AdminCaseManagementService service):CaseAdminController
{
    [HttpGet]public Task<ActionResult> Search([FromQuery]string? search,[FromQuery]string? status,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken token=default)=>Run(async()=>await service.SearchSanctionsAsync(search,status,page,pageSize,token));
    [HttpGet("{id:guid}")]public async Task<ActionResult> Get(Guid id,CancellationToken token){var value=await service.GetSanctionAsync(id,token);return value is null?NotFound(ApiErrorResponse.Create(HttpContext,"SANCTION_NOT_FOUND","제재를 찾을 수 없습니다.")):Ok(value);}
    [HttpPost]public Task<ActionResult> Create(CreateSanctionRequest request,CancellationToken token)=>Run(async()=>await service.CreateSanctionAsync(request,Actor(),token));
    [HttpPost("{id:guid}/status")]public Task<ActionResult> Status(Guid id,ChangeSanctionStatusRequest request,CancellationToken token)=>Run(async()=>await service.ChangeSanctionStatusAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/appeals")]public Task<ActionResult> Appeal(Guid id,CreateSanctionAppealRequest request,CancellationToken token)=>Run(async()=>await service.CreateAppealAsync(id,request,Actor(),token));
    [HttpPost("{id:guid}/appeals/{appealId:guid}/assignment")]public Task<ActionResult> AssignAppeal(Guid id,Guid appealId,AssignSanctionAppealRequest request,CancellationToken token)=>Run(async()=>await service.AssignAppealAsync(id,appealId,request,Actor(),token));
    [HttpPost("{id:guid}/appeals/{appealId:guid}/decision")]public Task<ActionResult> DecideAppeal(Guid id,Guid appealId,DecideSanctionAppealRequest request,CancellationToken token)=>Run(async()=>await service.DecideAppealAsync(id,appealId,request,Actor(),token));
    [HttpPost("{id:guid}/appeals/{appealId:guid}/evidence")]public Task<ActionResult> AppealEvidence(Guid id,Guid appealId,AddSanctionAppealEvidenceRequest request,CancellationToken token)=>Run(async()=>await service.AddAppealEvidenceAsync(id,appealId,request,Actor(),token));
}

[ApiController,Authorize(Roles=RoleCodes.Admin),Route("api/v1/admin/case-files")]
public sealed class AdminCaseFilesController(AdminCaseManagementService service):CaseAdminController
{
    [HttpPost,RequestSizeLimit(10*1024*1024+64*1024)]public Task<ActionResult> Upload([FromForm]string purposeCode,[FromForm]IFormFile file,CancellationToken token)=>Run(async()=>await service.UploadEvidenceFileAsync(purposeCode,file,Actor(),token));
    [HttpGet("{id:guid}")]public async Task<IActionResult> Open(Guid id,CancellationToken token){try{var file=await service.OpenEvidenceAsync(id,Actor(),token);return File(file.Stream,file.ContentType,file.FileName);}catch(CaseManagementException ex){return StatusCode(ex.StatusCode,ApiErrorResponse.Create(HttpContext,ex.BusinessCode,ex.Message));}}
}

public abstract class CaseAdminController:ControllerBase
{
    protected Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected async Task<ActionResult> Run(Func<Task<object>> action){try{return Ok(await action());}catch(CaseManagementException ex){return StatusCode(ex.StatusCode,ApiErrorResponse.Create(HttpContext,ex.BusinessCode,ex.Message));}}
}
