namespace SoodalLife.Api.Features.Trust;

public sealed record ProviderTrustDashboardResponse(
    Guid ProviderId,
    decimal? Score,
    string GradeCode,
    string GradeLabel,
    string EvaluationStatusCode,
    string StatusNotice,
    string? PolicyVersion,
    DateTime? CalculatedAt,
    IReadOnlyList<ProviderTrustGradeResponse> Grades,
    IReadOnlyList<ProviderTrustPolicyComponentResponse> PolicyComponents,
    IReadOnlyList<ProviderTrustEventResponse> Events);

public sealed record ProviderTrustGradeResponse(string Code, string Label, decimal MinimumScore, decimal MaximumScore, string Description);
public sealed record ProviderTrustPolicyComponentResponse(string Code, string Name, decimal Weight, string Description);
public sealed record ProviderTrustEventResponse(Guid Id, DateTime OccurredAt, string EventTypeCode, string EventLabel,
    decimal? ScoreBefore, decimal? ScoreDelta, decimal? ScoreAfter, string? GradeBeforeLabel, string? GradeAfterLabel,
    string Reason, string? PolicyVersion);

public sealed class ProviderTrustException(string businessCode, string message, int statusCode) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
