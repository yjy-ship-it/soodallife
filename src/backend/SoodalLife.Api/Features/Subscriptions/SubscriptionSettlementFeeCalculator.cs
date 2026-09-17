using System.Text.Json;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class SubscriptionSettlementFeeCalculator : ISubscriptionSettlementFeeCalculator
{
    public const decimal MonthlySubscriptionRate = 0.08m;

    public SubscriptionFeeCalculationResult Calculate(string? policySnapshotJson,decimal? grossAmount)
    {
        if (!grossAmount.HasValue) return SubscriptionFeeCalculationResult.Pending("회차 공급가액 산정 기준이 확정되지 않았습니다.");
        if (string.IsNullOrWhiteSpace(policySnapshotJson)) return SubscriptionFeeCalculationResult.Pending("서비스별 구독 수수료 정책 매핑이 확정되지 않았습니다.");
        try
        {
            using var document=JsonDocument.Parse(policySnapshotJson);
            var root=document.RootElement;
            var code=Text(root,"policyCode")??Text(root,"feePolicyCode")??Text(root,"code");
            if (string.IsNullOrWhiteSpace(code)) return SubscriptionFeeCalculationResult.Pending("구독 수수료 정책 코드가 없습니다.");
            return code.ToUpperInvariant() switch
            {
                "SUB-RATE" or "SUB-VISIT" or "SUB-MIX" or "SUB-MONTH"=>SubscriptionFeeCalculationResult.Success(code,Fee(grossAmount.Value)),
                _=>SubscriptionFeeCalculationResult.Pending("구독 수수료 계산에 필요한 정책 값이 확정되지 않았습니다.")
            };
        }
        catch(JsonException)
        {
            return SubscriptionFeeCalculationResult.Pending("구독 수수료 정책 Snapshot 형식을 확인해야 합니다.");
        }
    }

    private static decimal Fee(decimal grossAmount)=>decimal.Round(grossAmount*MonthlySubscriptionRate,0,MidpointRounding.AwayFromZero);
    private static string? Text(JsonElement root,string name)=>root.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
}
