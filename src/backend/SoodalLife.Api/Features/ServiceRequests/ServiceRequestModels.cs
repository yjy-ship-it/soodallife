using System.Text.Json;

namespace SoodalLife.Api.Features.ServiceRequests;

public sealed record CreateServiceRequestInput(
    Guid CategoryId,
    Guid? AdministrativeAreaId,
    string? Title,
    string? Description,
    string? DetailAddress,
    bool IsUrgent,
    string? IdempotencyKey,
    IReadOnlyList<RequestAnswerInput>? Answers);

public sealed record UpdateServiceRequestDraftInput(
    Guid? AdministrativeAreaId,
    string? Title,
    string? Description,
    string? DetailAddress,
    bool IsUrgent,
    IReadOnlyList<RequestAnswerInput>? Answers);

public sealed record CancelServiceRequestInput(string Reason);
public sealed record RequestAnswerInput(Guid FieldId, JsonElement Value);

public sealed record ServiceRequestCreatedResponse(Guid Id, string Status);

public sealed record PublishServiceRequestResponse(
    Guid Id,
    string Status,
    int EligibleCandidateCount,
    int DispatchCount,
    string CustomerMessage);

public sealed record ServiceRequestListItemResponse(
    Guid Id,
    string Title,
    string Status,
    string DisplayStatus,
    string CategoryPath,
    DateTime CreatedAt,
    DateTime? DesiredAt,
    int QuoteCount,
    Guid? TransactionId);

public sealed record ServiceRequestAnswerResponse(
    Guid FieldId,
    string FieldKey,
    string Label,
    string InputType,
    object? Value);

public sealed record ServiceRequestFileResponse(
    Guid Id,
    Guid? RequestFieldId,
    string PurposeCode,
    string FileName,
    string ContentType,
    long SizeBytes,
    string ScanStatus,
    string MalwareScanStatus,
    string PrivacyInspectionStatus,
    string SanitizationStatus,
    string ProviderVisibilityStatus,
    int DisplayOrder,
    string DownloadUrl);

public sealed record ServiceRequestDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string DisplayStatus,
    bool IsUrgent,
    string CategoryPath,
    Guid MajorCategoryId,
    Guid MiddleCategoryId,
    Guid ServiceCategoryId,
    Guid? AdministrativeAreaId,
    string? AdministrativeAreaName,
    string? DetailAddress,
    DateTime CreatedAt,
    DateTime? DesiredAt,
    int QuoteCount,
    Guid? SelectedProviderId,
    string? SelectedProviderName,
    Guid? TransactionId,
    bool CanEdit,
    bool CanCancel,
    bool CanPublish,
    IReadOnlyList<ServiceRequestAnswerResponse> Answers,
    IReadOnlyList<ServiceRequestFileResponse> Files);

public sealed class RequestValidationException(
    string businessCode,
    string message,
    IReadOnlyDictionary<string, string[]> fieldErrors,
    int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; } = fieldErrors;
    public int StatusCode { get; } = statusCode;
}
