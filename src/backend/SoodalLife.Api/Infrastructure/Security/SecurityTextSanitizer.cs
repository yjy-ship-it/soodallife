using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Security;

public static partial class SecurityTextSanitizer
{
    private static readonly string[] SensitiveKeys = ["password", "passwd", "token", "secret", "apikey", "authorization", "cookie", "connectionstring", "email", "phone", "recipientname", "roadaddress", "detailaddress", "storagekey", "hmac", "encryptionkey"];

    public static void Sanitize(AuditLog value)
    {
        value.Reason = Text(value.Reason); value.BeforeJson = Json(value.BeforeJson); value.AfterJson = Json(value.AfterJson);
        value.MetadataJson = Json(value.MetadataJson); value.IpAddress = Text(value.IpAddress); value.UserAgent = Text(value.UserAgent);
    }

    public static string ErrorCode(Exception exception) => exception switch
    {
        CryptographicException => "CRYPTOGRAPHIC_OPERATION_FAILED", JsonException => "INVALID_JSON",
        DbUpdateConcurrencyException => "CONCURRENCY_CONFLICT", DbUpdateException => "DATABASE_UPDATE_FAILED",
        TimeoutException => "OPERATION_TIMEOUT", _ => exception.GetType().Name.ToUpperInvariant()
    };

    public static string? Json(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        try { var node = JsonNode.Parse(value); Redact(node); return node?.ToJsonString(); }
        catch (JsonException) { return Text(value); }
    }

    public static string? Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var result = AssignmentRegex().Replace(value, "$1=***"); result = BearerRegex().Replace(result, "Bearer ***");
        result = EmailRegex().Replace(result, "***@***"); return PhoneRegex().Replace(result, "***-****-****");
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj.ToArray()) { var key = pair.Key.Replace("_", "").Replace("-", "").ToLowerInvariant(); if (SensitiveKeys.Any(x => key.Contains(x, StringComparison.Ordinal))) obj[pair.Key] = "***"; else Redact(pair.Value); }
        else if (node is JsonArray array) foreach (var item in array) Redact(item);
    }

    [GeneratedRegex(@"(?i)\b(password|passwd|token|secret|api[_-]?key|authorization|cookie|connectionstring|hmac|encryptionkey)\b\s*[:=]\s*[^\s,;]+", RegexOptions.CultureInvariant)] private static partial Regex AssignmentRegex();
    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.CultureInvariant)] private static partial Regex BearerRegex();
    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex EmailRegex();
    [GeneratedRegex(@"(?<!\d)01[016789][- ]?\d{3,4}[- ]?\d{4}(?!\d)", RegexOptions.CultureInvariant)] private static partial Regex PhoneRegex();
}
