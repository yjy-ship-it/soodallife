using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class SubscriptionPaymentGatewayOptions
{
    public const string SectionName = "SubscriptionPaymentGateway";
    public string ProviderCode { get; set; } = "TOSS";
    public string BaseUrl { get; set; } = "https://api.tosspayments.com";
    public string ClientKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string BillingSuccessPath { get; set; } = "/customer/care/payments?billing=success";
    public string BillingFailPath { get; set; } = "/customer/care/payments?billing=fail";
}

public sealed class SubscriptionPaymentGatewayException(string code,string message,bool retryable=false) : Exception(message)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}

public sealed class TossSubscriptionPaymentGateway(HttpClient client,IOptions<SubscriptionPaymentGatewayOptions> options) : ISubscriptionPaymentGateway
{
    private readonly SubscriptionPaymentGatewayOptions settings=options.Value;

    public async Task<SubscriptionGatewayBillingKeyResult> IssueBillingKeyAsync(string providerCode,string authKey,string customerKey,string idempotencyKey,CancellationToken token)
    {
        EnsureReady(providerCode);
        using var request=Create(HttpMethod.Post,"/v1/billing/authorizations/issue",idempotencyKey,new{authKey,customerKey});
        using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
        using var json=JsonDocument.Parse(body);var root=json.RootElement;
        var billingKey=root.GetProperty("billingKey").GetString()??throw new SubscriptionPaymentGatewayException("PG_RESPONSE_INVALID","PG 빌링키가 없습니다.");
        var rawMethod=root.TryGetProperty("method",out var methodElement)?methodElement.GetString()??"CARD":"CARD";var method=rawMethod.Contains("카드",StringComparison.OrdinalIgnoreCase)?"CARD":rawMethod.ToUpperInvariant();
        var masked="등록된 결제수단";
        if(root.TryGetProperty("card",out var card)&&card.ValueKind==JsonValueKind.Object&&card.TryGetProperty("number",out var number))masked=number.GetString()??masked;
        return new(billingKey,method,masked);
    }

    public async Task<SubscriptionGatewayPaymentResult> ChargeRecurringAsync(string providerCode,string billingKey,string customerKey,string orderId,string orderName,decimal amount,string currencyCode,string idempotencyKey,CancellationToken token)
    {
        EnsureReady(providerCode);
        EnsureKrwAmount(amount,currencyCode);
        using var request=Create(HttpMethod.Post,$"/v1/billing/{Uri.EscapeDataString(billingKey)}",idempotencyKey,new{amount=decimal.ToInt64(amount),customerKey,orderId,orderName,currency=currencyCode});
        using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
        using var json=JsonDocument.Parse(body);var root=json.RootElement;var paymentKey=root.GetProperty("paymentKey").GetString()??throw new SubscriptionPaymentGatewayException("PG_RESPONSE_INVALID","PG 결제 식별값이 없습니다.");var returnedOrderId=root.TryGetProperty("orderId",out var order)?order.GetString():null;var returnedStatus=root.TryGetProperty("status",out var status)?status.GetString():null;var returnedAmount=root.TryGetProperty("totalAmount",out var total)?total.GetDecimal():0;if(returnedOrderId!=orderId||returnedStatus!="DONE"||returnedAmount!=amount)throw new SubscriptionPaymentGatewayException("PG_RESPONSE_MISMATCH","PG 결제 승인 결과가 요청한 주문과 일치하지 않습니다.");
        return new(paymentKey,null);
    }

    public async Task<SubscriptionGatewayPaymentStatus> GetPaymentAsync(string providerCode,string paymentKey,CancellationToken token)
    {
        EnsureReady(providerCode);using var request=Create(HttpMethod.Get,$"/v1/payments/{Uri.EscapeDataString(paymentKey)}",string.Empty,null);request.Headers.Remove("Idempotency-Key");
        using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
        using var json=JsonDocument.Parse(body);var root=json.RootElement;
        return new(root.GetProperty("paymentKey").GetString()??paymentKey,root.GetProperty("orderId").GetString()??string.Empty,root.GetProperty("status").GetString()??string.Empty,root.TryGetProperty("totalAmount",out var total)?total.GetDecimal():0,root.TryGetProperty("balanceAmount",out var balance)?balance.GetDecimal():0);
    }

    public async Task<SubscriptionGatewayPaymentStatus> GetPaymentByOrderIdAsync(string providerCode,string orderId,CancellationToken token)
    {
        EnsureReady(providerCode);using var request=Create(HttpMethod.Get,$"/v1/payments/orders/{Uri.EscapeDataString(orderId)}",string.Empty,null);request.Headers.Remove("Idempotency-Key");
        using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
        using var json=JsonDocument.Parse(body);var root=json.RootElement;
        return new(root.GetProperty("paymentKey").GetString()??string.Empty,root.GetProperty("orderId").GetString()??orderId,root.GetProperty("status").GetString()??string.Empty,root.TryGetProperty("totalAmount",out var total)?total.GetDecimal():0,root.TryGetProperty("balanceAmount",out var balance)?balance.GetDecimal():0);
    }

    public async Task CancelBillingKeyAsync(string providerCode,string billingKey,string idempotencyKey,CancellationToken token)
    {
        EnsureReady(providerCode);using var request=Create(HttpMethod.Delete,$"/v1/billing/{Uri.EscapeDataString(billingKey)}",idempotencyKey,null);using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
    }

    public async Task<SubscriptionGatewayPaymentResult> RefundPaymentAsync(string providerCode,string paymentKey,decimal amount,string reason,string idempotencyKey,CancellationToken token)
    {
        EnsureReady(providerCode);EnsureKrwAmount(amount,"KRW");using var request=Create(HttpMethod.Post,$"/v1/payments/{Uri.EscapeDataString(paymentKey)}/cancel",idempotencyKey,new{cancelReason=reason.Length>200?reason[..200]:reason,cancelAmount=decimal.ToInt64(amount)});using var response=await client.SendAsync(request,token);var body=await response.Content.ReadAsStringAsync(token);EnsureSuccess(response,body);
        using var json=JsonDocument.Parse(body);var root=json.RootElement;var returnedPaymentKey=root.TryGetProperty("paymentKey",out var key)?key.GetString():paymentKey;string? transactionKey=null;if(root.TryGetProperty("cancels",out var cancels)&&cancels.ValueKind==JsonValueKind.Array&&cancels.GetArrayLength()>0){var last=cancels[cancels.GetArrayLength()-1];if(last.TryGetProperty("transactionKey",out var transaction))transactionKey=transaction.GetString();}
        return new(returnedPaymentKey??paymentKey,transactionKey);
    }

    private HttpRequestMessage Create(HttpMethod method,string path,string key,object? body)
    {
        var request=new HttpRequestMessage(method,new Uri(new Uri(settings.BaseUrl.TrimEnd('/')+"/"),path.TrimStart('/')));var credential=Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.SecretKey+":"));request.Headers.Authorization=new AuthenticationHeaderValue("Basic",credential);request.Headers.TryAddWithoutValidation("Idempotency-Key",key);if(body is not null)request.Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json");return request;
    }

    private void EnsureReady(string providerCode)
    {
        if(!settings.Enabled||string.IsNullOrWhiteSpace(settings.SecretKey))throw new SubscriptionPaymentGatewayException("PG_NOT_CONFIGURED","정기결제 PG 운영키가 설정되지 않았습니다.");if(!providerCode.Equals(settings.ProviderCode,StringComparison.OrdinalIgnoreCase))throw new SubscriptionPaymentGatewayException("PG_PROVIDER_UNSUPPORTED",$"지원하지 않는 정기결제 PG입니다: {providerCode}");
    }

    private static void EnsureKrwAmount(decimal amount,string currencyCode)
    {
        if(!currencyCode.Equals("KRW",StringComparison.OrdinalIgnoreCase)||amount<=0||amount!=decimal.Truncate(amount))throw new SubscriptionPaymentGatewayException("PG_AMOUNT_INVALID","원화 결제금액은 1원 단위의 양의 정수여야 합니다.");
    }

    private static void EnsureSuccess(HttpResponseMessage response,string body)
    {
        if(response.IsSuccessStatusCode)return;string code="PG_REQUEST_FAILED",message="PG 요청을 처리하지 못했습니다.";try{using var json=JsonDocument.Parse(body);if(json.RootElement.TryGetProperty("code",out var c))code=c.GetString()??code;if(json.RootElement.TryGetProperty("message",out var m))message=m.GetString()??message;}catch(JsonException){}throw new SubscriptionPaymentGatewayException(code,message,(int)response.StatusCode>=500);
    }
}
