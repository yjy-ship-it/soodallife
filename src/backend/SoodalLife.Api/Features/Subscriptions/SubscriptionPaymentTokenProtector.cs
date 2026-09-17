using Microsoft.AspNetCore.DataProtection;

namespace SoodalLife.Api.Features.Subscriptions;

public interface ISubscriptionPaymentTokenProtector
{
    string Protect(string token);
    string Unprotect(string protectedToken);
}

public sealed class SubscriptionPaymentTokenProtector(IDataProtectionProvider provider) : ISubscriptionPaymentTokenProtector
{
    private const string Prefix = "dp:v1:";
    private readonly IDataProtector protector = provider.CreateProtector("SoodalLife.SubscriptionPaymentToken.v1");

    public string Protect(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("결제 토큰이 비어 있습니다.", nameof(token));
        return token.StartsWith(Prefix, StringComparison.Ordinal) ? token : Prefix + protector.Protect(token);
    }

    public string Unprotect(string protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken)) throw new ArgumentException("결제 토큰이 비어 있습니다.", nameof(protectedToken));
        return protectedToken.StartsWith(Prefix, StringComparison.Ordinal)
            ? protector.Unprotect(protectedToken[Prefix.Length..])
            : protectedToken; // V104 이전 데이터의 무중단 전환
    }
}
