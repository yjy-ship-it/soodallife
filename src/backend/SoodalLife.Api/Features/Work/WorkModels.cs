using System.Text.Json.Serialization;

namespace SoodalLife.Api.Features.Work;

public sealed record WorkTransactionListItem(
    Guid Id,
    string CategoryPath,
    string AreaName,
    string RequestSummary,
    decimal AgreedAmount,
    string CurrencyCode,
    string Status,
    DateTime CreatedAt,
    string DisplayStatus,
    string StatusGroup);

public sealed record WorkQuoteItem(
    int LineNo,
    string ItemName,
    string? Description,
    decimal Quantity,
    string? UnitText,
    decimal UnitPriceAmount,
    decimal LineTotalAmount);

public sealed record WorkRequestAnswer(
    string Label,
    object? Value,
    bool IsMasked);

public sealed record PhotoRequirementStatus(
    string RoleCode,
    string RoleName,
    int RequiredCount,
    int UploadedCount,
    bool IsSatisfied);

public sealed record CompletionPolicyStatus(
    string PolicyVersion,
    int RequiredPhotoCount,
    int UploadedPhotoCount,
    bool TotalSatisfied,
    IReadOnlyList<PhotoRequirementStatus> Roles,
    bool IsSatisfied);

public sealed record PhotoRoleOption(string Code, string Name, string? Description);

public sealed record CompletionEvidenceResponse(
    Guid FileId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string RoleCode,
    string RoleName,
    int DisplayOrder,
    string? Description,
    string DownloadUrl);

public sealed record WorkCompletionRevisionResponse(
    Guid Id,
    int RevisionNo,
    string Status,
    string WorkSummary,
    decimal ActualAmount,
    string CurrencyCode,
    DateTime ProviderAttestationAt,
    DateTime RecordedAt,
    string? RevisionReason,
    IReadOnlyList<CompletionEvidenceResponse> Evidence,
    CompletionPolicyStatus Policy);

public sealed record WorkTransactionDetail(
    Guid Id,
    string Status,
    string CategoryPath,
    string AreaName,
    string RequestTitle,
    string? RequestDescription,
    string? CustomerPhone,
    string? DetailAddress,
    string ProviderName,
    decimal AgreedAmount,
    string CurrencyCode,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<WorkQuoteItem> QuoteItems,
    IReadOnlyList<WorkRequestAnswer> RequestAnswers,
    CompletionPolicyStatus CompletionPolicy,
    IReadOnlyList<PhotoRoleOption> AvailablePhotoRoles,
    WorkCompletionRevisionResponse? Completion,
    IReadOnlyList<WorkCompletionRevisionResponse> CompletionRevisions,
    IReadOnlyList<WorkTimelineItem> Timeline,
    WorkRelatedCase? AfterService,
    WorkRelatedCase? Dispute,
    WorkReviewState Review,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] WorkProviderFeeSummary? ProviderFee);

public sealed record WorkTimelineItem(string Code, string Label, DateTime OccurredAt, bool IsCurrent);
public sealed record WorkRelatedCase(Guid Id, string Status, string DisplayStatus, DateTime CreatedAt);
public sealed record WorkReviewState(Guid? Id, bool CanCreate, bool Exists, string? Status);
public sealed record WorkProviderFeeSummary(Guid FeeChargeId, decimal FeeAmount, DateTime ChargedAt, string RestoreStatusCode);

public sealed record SaveCompletionDraftInput(
    string WorkSummary,
    decimal ActualAmount,
    string? RevisionReason,
    string IdempotencyKey);

public sealed record ConfirmCompletionInput(
    Guid CompletionRevisionId,
    string Result,
    string? Comment,
    string IdempotencyKey);

public sealed record CompletionConfirmationResponse(
    Guid ConfirmationId,
    Guid TransactionId,
    Guid CompletionRevisionId,
    string Result,
    string TransactionStatus,
    Guid? ServiceHistoryId);

public sealed class WorkBusinessException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status409Conflict,
    IReadOnlyDictionary<string, string[]>? fieldErrors = null) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; } = fieldErrors;
}
