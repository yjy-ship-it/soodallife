using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Features.ServiceRequests;

public static class CustomerRequestFieldPolicy
{
    private static readonly HashSet<string> RetiredLabels = new(StringComparer.Ordinal)
    {
        "서비스주소",
    };

    private static readonly HashSet<string> RetiredKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "service_address",
        "request_address",
    };

    public static bool IsRetiredStructuralDuplicate(CategoryFieldDefinition field) =>
        IsRetiredStructuralDuplicate(field.FieldKey, field.Label);

    public static bool IsRetiredStructuralDuplicate(string? fieldKey, string? label) =>
        (!string.IsNullOrWhiteSpace(fieldKey) && RetiredKeys.Contains(fieldKey.Trim())) ||
        RetiredLabels.Contains(NormalizeLabel(label));

    private static string NormalizeLabel(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit));
}
