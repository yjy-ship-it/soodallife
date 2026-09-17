namespace SoodalLife.Api.Infrastructure.Security;

public static class OfficialCorsOrigins
{
    private const string CustomerOrigin = "https://soodallife.kr";
    private const string CustomerWwwOrigin = "https://www.soodallife.kr";

    public static string[] Expand(IEnumerable<string> configuredOrigins)
    {
        var origins = configuredOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (origins.Contains(CustomerOrigin, StringComparer.OrdinalIgnoreCase) &&
            !origins.Contains(CustomerWwwOrigin, StringComparer.OrdinalIgnoreCase))
        {
            origins.Add(CustomerWwwOrigin);
        }

        return [.. origins];
    }
}
