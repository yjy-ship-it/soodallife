using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Features.ServiceRequests;

public static class CustomerRequestFieldPolicy
{
    private static readonly HashSet<string> RetiredLabels = new(StringComparer.Ordinal)
    {
        "서비스주소",
        "희망예산",
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

    public static bool IsIncompatibleWithCoverage(CategoryFieldDefinition field, string? coverageTypeCode)
    {
        if (!string.Equals(coverageTypeCode, ProviderCoveragePolicy.NationwideRemote, StringComparison.Ordinal)) return false;
        var key = (field.FieldKey ?? string.Empty).Trim().ToLowerInvariant();
        var label = NormalizeLabel(field.Label);
        string[] remoteExcludedKeys = ["service_address", "request_address", "site_access", "parking", "elevator", "floor", "area_size", "visit_cycle", "onsite_condition"];
        string[] remoteExcludedLabels = ["서비스주소", "현장주소", "주차", "엘리베이터", "층수", "면적", "평수", "현장조건", "방문주기", "출입조건"];
        return remoteExcludedKeys.Any(value => key.Contains(value, StringComparison.OrdinalIgnoreCase)) ||
               remoteExcludedLabels.Any(value => label.Contains(value, StringComparison.Ordinal));
    }

    private static string NormalizeLabel(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit));
}
