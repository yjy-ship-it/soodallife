using Microsoft.AspNetCore.DataProtection;

namespace SoodalLife.Api.Features.Emergency;

public interface IEmergencyPaymentInstructionProtector
{
    string Protect(string value);
    string Unprotect(string value);
}

public sealed class EmergencyPaymentInstructionProtector(IDataProtectionProvider provider) : IEmergencyPaymentInstructionProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("SoodalLife.Emergency.PaymentInstruction.v1");
    public string Protect(string value) => protector.Protect(value);
    public string Unprotect(string value) => protector.Unprotect(value);
}
