using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SoodalLife.Api.Features.Notifications;

public sealed class NhnNotificationOptions
{
    public const string SectionName = "NhnNotification";
    public string Mode { get; set; } = "DISABLED";
    public string[] TestRecipients { get; set; } = [];
    public NhnChannelOptions Sms { get; set; } = new();
    public NhnChannelOptions Kakao { get; set; } = new();
    public NhnChannelOptions Email { get; set; } = new();
    public NhnChannelOptions Push { get; set; } = new();
}

public sealed class NhnChannelOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string DefaultTemplateCode { get; set; } = string.Empty;
}

public sealed class NhnNotificationChannelSender(HttpClient client, IOptions<NhnNotificationOptions> configured) : INotificationChannelSender
{
    private readonly NhnNotificationOptions options = configured.Value;
    public bool Supports(string channelCode) => channelCode is "KAKAO" or "SMS" or "EMAIL" or "PUSH";
    public bool IsConfigured(string channelCode)
    {
        var channel = Channel(channelCode);
        return Mode() is "TEST" or "PRODUCTION" && !string.IsNullOrWhiteSpace(channel.BaseUrl) &&
               !string.IsNullOrWhiteSpace(channel.AppKey) && !string.IsNullOrWhiteSpace(channel.SecretKey) &&
               (channelCode == "PUSH" || !string.IsNullOrWhiteSpace(channel.Sender));
    }

    public async Task<NotificationChannelSendResult> SendAsync(NotificationChannelMessage message, CancellationToken token)
    {
        if (!IsConfigured(message.ChannelCode)) return Fail("CHANNEL_NOT_CONFIGURED", "NHN 채널 설정이 완료되지 않았습니다.");
        if (string.IsNullOrWhiteSpace(message.Recipient)) return Fail("RECIPIENT_MISSING", "발송 대상이 없습니다.");
        if (Mode() == "TEST" && !options.TestRecipients.Contains(message.Recipient, StringComparer.OrdinalIgnoreCase))
            return Fail("TEST_RECIPIENT_BLOCKED", "TEST 허용목록 밖의 수신자 발송을 차단했습니다.");
        var channel = Channel(message.ChannelCode);
        var uri = Endpoint(message.ChannelCode, channel);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.TryAddWithoutValidation("X-Secret-Key", channel.SecretKey);
        request.Headers.TryAddWithoutValidation("X-NC-API-IDEMPOTENCY-KEY", message.DeliveryId.ToString("N"));
        request.Content = JsonContent.Create(Payload(message, channel));
        try
        {
            using var response = await client.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode) return Fail($"NHN_HTTP_{(int)response.StatusCode}", Safe(body));
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var header = root.TryGetProperty("header", out var value) ? value : root;
            var success = header.TryGetProperty("isSuccessful", out var ok) && ok.GetBoolean();
            var code = header.TryGetProperty("resultCode", out var resultCode) ? resultCode.ToString() : success ? "0" : "UNKNOWN";
            var reason = header.TryGetProperty("resultMessage", out var resultMessage) ? resultMessage.GetString() : null;
            var externalId = FindExternalId(root);
            return success ? new(true, "SENT", code, externalId) : Fail($"NHN_{code}", reason);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Fail("NHN_TRANSPORT_ERROR", exception.GetType().Name);
        }
    }

    private object Payload(NotificationChannelMessage message, NhnChannelOptions channel) => message.ChannelCode switch
    {
        "SMS" => new { body = message.Body, sendNo = channel.Sender, recipientList = new[] { new { recipientNo = Digits(message.Recipient!) } } },
        "EMAIL" => new { senderAddress = channel.Sender, title = message.Title, body = message.Body, receiverList = new[] { new { receiveMailAddr = message.Recipient, receiveType = "MRT0" } } },
        "KAKAO" => new { senderKey = channel.Sender, templateCode = string.IsNullOrWhiteSpace(message.TemplateCode) ? channel.DefaultTemplateCode : message.TemplateCode, recipientList = new[] { new { recipientNo = Digits(message.Recipient!), content = message.Body } } },
        "PUSH" => new { target = new { type = "UID", to = new[] { message.Recipient } }, message = new { title = message.Title, body = message.Body } },
        _ => throw new InvalidOperationException("Unsupported NHN channel.")
    };

    private static string Endpoint(string code, NhnChannelOptions channel)
    {
        var root = channel.BaseUrl.TrimEnd('/');
        return code switch
        {
            "SMS" => $"{root}/sms/v3.0/appKeys/{Uri.EscapeDataString(channel.AppKey)}/sender/sms",
            "EMAIL" => $"{root}/email/v2.0/appKeys/{Uri.EscapeDataString(channel.AppKey)}/sender/mail",
            "KAKAO" => $"{root}/alimtalk/v2.2/appkeys/{Uri.EscapeDataString(channel.AppKey)}/raw-messages",
            "PUSH" => $"{root}/push/v2.2/appkeys/{Uri.EscapeDataString(channel.AppKey)}/messages",
            _ => throw new InvalidOperationException("Unsupported NHN channel.")
        };
    }
    private NhnChannelOptions Channel(string code) => code switch { "SMS" => options.Sms, "KAKAO" => options.Kakao, "EMAIL" => options.Email, "PUSH" => options.Push, _ => new() };
    private string Mode() => options.Mode.Trim().ToUpperInvariant();
    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
    private static NotificationChannelSendResult Fail(string code, string? reason) => new(false, code, FailureReason: Safe(reason));
    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "외부 채널 처리에 실패했습니다." : value.Length > 500 ? value[..500] : value;
    private static string? FindExternalId(JsonElement root)
    {
        foreach (var name in new[] { "requestId", "messageId" })
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }
}
