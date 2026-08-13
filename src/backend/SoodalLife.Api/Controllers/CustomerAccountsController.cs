using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;

namespace SoodalLife.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/customer-account")]
public sealed class PublicCustomerAccountController(CustomerAccountService service, IIdentityVerificationAdapter verification) : ControllerBase
{
    [HttpGet("availability/login-id")]
    public Task<ActionResult> LoginId([FromQuery] string value, CancellationToken token) => Run(async () => await service.LoginIdAvailable(value, token));

    [HttpGet("availability/email")]
    public Task<ActionResult> Email([FromQuery] string value, CancellationToken token) => Run(async () => await service.EmailAvailable(value, token));

    [HttpGet("legal-documents")]
    public Task<ActionResult> Documents(CancellationToken token) => Run(async () => await service.ActiveDocuments(token));

    [HttpGet("identity-verification/status")]
    public Task<IdentityVerificationStatus> Verification(CancellationToken token) => verification.GetStatusAsync(token);

    [HttpPost("register")]
    public Task<ActionResult> Register(RegisterCustomerRequest input, CancellationToken token) => Run(async () => await service.Register(input, HttpContext, token));

    [HttpPost("password-reset/requests")]
    public async Task<IActionResult> RequestReset(PasswordResetRequestInput input, CancellationToken token)
    {
        try { await service.RequestPasswordReset(input.LoginOrEmail, token); return Accepted(); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmReset(ConfirmPasswordResetRequest input, CancellationToken token)
    {
        try { await service.ConfirmPasswordReset(input.Token, input.NewPassword, input.NewPasswordConfirmation, token); return NoContent(); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    private async Task<ActionResult> Run(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    private ObjectResult Error(CustomerAccountException exception) =>
        StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));
}

[ApiController, Authorize(Roles = RoleCodes.Customer), Route("api/v1/customer/account")]
public sealed class CustomerAccountController(CustomerAccountService service, CustomerWithdrawalService withdrawal) : ControllerBase
{
    [HttpGet("profile")] public Task<ActionResult> Profile(CancellationToken token) => Run(async () => await service.Profile(User, token));
    [HttpPut("profile")] public Task<ActionResult> UpdateProfile(UpdateCustomerProfileRequest input, CancellationToken token) => Run(async () => await service.UpdateProfile(User, input, token));
    [HttpGet("addresses")] public Task<ActionResult> Addresses(CancellationToken token) => Run(async () => await service.Addresses(User, token));
    [HttpPost("addresses")] public Task<ActionResult> CreateAddress(SaveCustomerAddressRequest input, CancellationToken token) => Run(async () => await service.CreateAddress(User, input, token));
    [HttpPut("addresses/{id:guid}")] public Task<ActionResult> UpdateAddress(Guid id, SaveCustomerAddressRequest input, CancellationToken token) => Run(async () => await service.UpdateAddress(User, id, input, token));

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, [FromQuery] string? concurrencyToken, CancellationToken token)
    {
        try { await service.DeleteAddress(User, id, concurrencyToken, token); return NoContent(); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest input, CancellationToken token)
    {
        try { await service.ChangePassword(User, input, HttpContext, token); return NoContent(); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    [HttpGet("consents")] public Task<ActionResult> Consents(CancellationToken token) => Run(async () => await service.Consents(User, token));

    [HttpPut("consents")]
    public async Task<IActionResult> UpdateConsent(UpdateConsentRequest input, CancellationToken token)
    {
        try { await service.UpdateConsent(User, input, HttpContext, token); return NoContent(); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    [HttpGet("withdrawal")] public Task<ActionResult> WithdrawalDashboard(CancellationToken token) => WithdrawalRun(async () => await withdrawal.DashboardAsync(User, token));
    [HttpPost("withdrawal-requests")] public Task<ActionResult> Withdrawal(CreateCustomerWithdrawalRequest input, CancellationToken token) => WithdrawalRun(async () => await withdrawal.RequestAsync(User, input, token));
    [HttpPost("withdrawal-requests/{id:guid}/cancel")] public Task<ActionResult> CancelWithdrawal(Guid id, CancelCustomerWithdrawalRequest input, CancellationToken token) => WithdrawalRun(async () => await withdrawal.CancelAsync(User, id, input, token));

    private async Task<ActionResult> Run(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerAccountException exception) { return Error(exception); }
    }

    private ObjectResult Error(CustomerAccountException exception) =>
        StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message));

    private async Task<ActionResult> WithdrawalRun(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (CustomerWithdrawalException exception) { return StatusCode(exception.StatusCode, ApiErrorResponse.Create(HttpContext, exception.BusinessCode, exception.Message)); }
    }
}
