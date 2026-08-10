using System.Text.Json;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class SubscriptionSettlementFeeCalculator : ISubscriptionSettlementFeeCalculator
{
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
            var rate=Number(root,"rate")??(Number(root,"ratePercent") is decimal percent?percent/100m:null);
            var visitFee=Number(root,"perVisitAmount")??Number(root,"visitFeeAmount");
            return code.ToUpperInvariant() switch
            {
                "SUB-RATE" when rate.HasValue=>SubscriptionFeeCalculationResult.Success(code,grossAmount.Value*rate.Value),
                "SUB-VISIT" when visitFee.HasValue=>SubscriptionFeeCalculationResult.Success(code,visitFee.Value),
                "SUB-MIX" when rate.HasValue&&visitFee.HasValue=>SubscriptionFeeCalculationResult.Success(code,grossAmount.Value*rate.Value+visitFee.Value),
                "SUB-MONTH"=>SubscriptionFeeCalculationResult.Pending("월 고정 수수료의 회차 배분 기준이 확정되지 않았습니다."),
                _=>SubscriptionFeeCalculationResult.Pending("구독 수수료 계산에 필요한 정책 값이 확정되지 않았습니다.")
            };
        }
        catch(JsonException)
        {
            return SubscriptionFeeCalculationResult.Pending("구독 수수료 정책 Snapshot 형식을 확인해야 합니다.");
        }
    }

    private static string? Text(JsonElement root,string name)=>root.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
    private static decimal? Number(JsonElement root,string name)=>root.TryGetProperty(name,out var value)&&value.TryGetDecimal(out var result)?result:null;
}
