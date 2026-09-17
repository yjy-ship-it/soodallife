namespace SoodalLife.Api.Features.Providers;

public static class ProviderCoveragePolicy
{
    public const string LocalOnly = "LOCAL_ONLY";
    public const string NationwideRemote = "NATIONWIDE_REMOTE";
    public const string NationwideDelivery = "NATIONWIDE_DELIVERY";
    public const string NationwideNetwork = "NATIONWIDE_NETWORK";
    public const string Flexible = "FLEXIBLE";

    public static bool AllowsNationwide(string? coverageTypeCode) =>
        coverageTypeCode is NationwideRemote or NationwideDelivery or NationwideNetwork or Flexible;

    public static bool RequiresServiceAddress(string? coverageTypeCode) =>
        coverageTypeCode != NationwideRemote;

    public static string DisplayName(string? coverageTypeCode) => coverageTypeCode switch
    {
        NationwideRemote => "전국 원격 제공",
        NationwideDelivery => "전국 배송 제공",
        NationwideNetwork => "전국 방문망 제공",
        Flexible => "지역·전국 선택 제공",
        _ => "지역 방문 제공",
    };
}
