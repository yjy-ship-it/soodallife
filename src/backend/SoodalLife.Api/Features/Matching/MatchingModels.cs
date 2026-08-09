namespace SoodalLife.Api.Features.Matching;

public sealed record MatchAndDispatchResult(
    Guid RequestId,
    int EvaluatedProviderCount,
    int EligibleCandidateCount,
    int DispatchCount,
    int NewDispatchCount);

public sealed record DispatchCandidateResponse(
    Guid ProviderId,
    string BusinessName,
    string Status,
    bool CategoryMatch,
    bool AreaMatch,
    bool ApprovalMatch,
    string? ReasonCode);

public sealed record ProviderMatchedRequestListItem(
    Guid RequestId,
    string CategoryPath,
    string AreaName,
    string Summary,
    DateTime? DesiredAt,
    DateTime DispatchedAt,
    string DispatchStatus,
    string RequestStatus);

public sealed record ProviderMatchedRequestAnswer(
    Guid FieldId,
    string Label,
    string InputType,
    object? Value,
    bool IsMasked);

public sealed record ProviderMatchedRequestDetail(
    Guid RequestId,
    string CategoryPath,
    string AreaName,
    string Title,
    string? Description,
    bool IsUrgent,
    DateTime? DesiredAt,
    DateTime DispatchedAt,
    DateTime ExpiresAt,
    string DispatchStatus,
    string RequestStatus,
    IReadOnlyList<ProviderMatchedRequestAnswer> Answers);

public sealed class MatchingException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status409Conflict) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
