using System.Text.Json;

namespace SoodalLife.Api.Features.ServiceRequests;

public sealed record CreateServiceRequestInput(
    Guid CategoryId,
    Guid AdministrativeAreaId,
    string Title,
    string? Description,
    string? DetailAddress,
    bool IsUrgent,
    string? IdempotencyKey,
    IReadOnlyList<RequestAnswerInput> Answers);

public sealed record RequestAnswerInput(Guid FieldId, JsonElement Value);

public sealed record ServiceRequestCreatedResponse(Guid Id, string Status);

public sealed record PublishServiceRequestResponse(
    Guid Id,
    string Status,
    int EligibleCandidateCount,
    int DispatchCount);

public sealed record ServiceRequestListItemResponse(
    Guid Id,
    string Title,
    string Status,
    string CategoryPath,
    DateTime CreatedAt,
    DateTime? DesiredAt);

public sealed record ServiceRequestAnswerResponse(
    Guid FieldId,
    string FieldKey,
    string Label,
    string InputType,
    object? Value);

public sealed record ServiceRequestDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    bool IsUrgent,
    string CategoryPath,
    Guid AdministrativeAreaId,
    string AdministrativeAreaName,
    string? DetailAddress,
    DateTime CreatedAt,
    DateTime? DesiredAt,
    IReadOnlyList<ServiceRequestAnswerResponse> Answers);

public sealed class RequestValidationException(
    string businessCode,
    string message,
    IReadOnlyDictionary<string, string[]> fieldErrors) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; } = fieldErrors;
}
