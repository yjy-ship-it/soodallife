using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoodalLife.Api.Features.Subscriptions;

namespace SoodalLife.Api.Controllers;

[ApiController,AllowAnonymous,Route("api/v1/webhooks/toss/subscriptions")]
public sealed class SubscriptionPaymentWebhooksController(SubscriptionPaymentProcessor processor,ILogger<SubscriptionPaymentWebhooksController> logger):ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody]JsonElement payload,CancellationToken token)
    {
        var eventType=payload.TryGetProperty("eventType",out var eventValue)?eventValue.GetString():null;var data=payload.TryGetProperty("data",out var dataValue)?dataValue:payload;var paymentKey=data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("paymentKey",out var keyValue)?keyValue.GetString():null;var orderId=data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("orderId",out var orderValue)?orderValue.GetString():null;
        if(eventType is not("PAYMENT_STATUS_CHANGED" or "CANCEL_STATUS_CHANGED")||string.IsNullOrWhiteSpace(paymentKey)||string.IsNullOrWhiteSpace(orderId))return Ok();
        try{await processor.ReconcileAsync(paymentKey,orderId,token);return Ok();}
        catch(SubscriptionPaymentGatewayException error){logger.LogWarning(error,"Toss subscription webhook verification failed. code={Code}",error.Code);return StatusCode(StatusCodes.Status503ServiceUnavailable);}
    }
}
