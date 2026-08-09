using System.Text.Json;

namespace SoodalLife.Api.Features.Work;

public sealed class CompletionPolicyEvaluator
{
    public CompletionPolicyStatus Evaluate(string snapshotJson, IReadOnlyCollection<string> uploadedRoleCodes)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        var root = document.RootElement;
        var version = ReadString(root, "policyVersion") ?? "UNKNOWN";
        var requiredTotal = ReadInt(root, "totalRequiredPhotoCount");
        var counts = uploadedRoleCodes.GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var roles = new List<PhotoRequirementStatus>();
        if (TryProperty(root, "roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in rolesElement.EnumerateArray())
            {
                var code = ReadString(role, "code") ?? throw new JsonException("Completion role code is missing.");
                var name = ReadString(role, "name") ?? code;
                var minimum = ReadInt(role, "minimumCount");
                var uploaded = counts.GetValueOrDefault(code);
                roles.Add(new PhotoRequirementStatus(code, name, minimum, uploaded, uploaded >= minimum));
            }
        }
        var totalSatisfied = uploadedRoleCodes.Count >= requiredTotal;
        return new CompletionPolicyStatus(version, requiredTotal, uploadedRoleCodes.Count, totalSatisfied, roles,
            totalSatisfied && roles.All(role => role.IsSatisfied));
    }

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static string? ReadString(JsonElement element, string name) =>
        TryProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int ReadInt(JsonElement element, string name) =>
        TryProperty(element, name, out var value) && value.TryGetInt32(out var number) ? number : 0;
}
