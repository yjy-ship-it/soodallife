using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record SaveAdvertisingCampaignRequest(
    [param: Required, StringLength(200)] string CampaignName,
    [param: Required, StringLength(30)] string CampaignTypeCode,
    [param: Required, StringLength(20)] string AudienceTypeCode,
    [param: Required, StringLength(30)] string OwnerTypeCode,
    [param: StringLength(200)] string? OwnerDisplayName,
    DateTime StartAt,
    DateTime? EndAt,
    int Priority,
    [param: Required, StringLength(30)] string DestinationTypeCode,
    [param: StringLength(2000)] string? DestinationValue,
    IReadOnlyList<string> PlacementCodes,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> AreaIds);

public sealed record SaveAdvertisingCreativeRequest(
    [param: Required, StringLength(200)] string Title,
    [param: StringLength(300)] string? Subtitle,
    [param: StringLength(4000)] string? BodyText,
    Guid? FileId,
    [param: StringLength(300)] string? AltText,
    [param: StringLength(50)] string? ButtonText,
    [param: StringLength(30)] string? DestinationTypeCode,
    [param: StringLength(2000)] string? DestinationValue,
    int DisplayOrder,
    [param: Required, StringLength(20)] string StatusCode);

public sealed record AdvertisingReviewRequest(
    [param: Required, StringLength(20)] string ActionCode,
    [param: StringLength(1000)] string? Reason);

public sealed record AdvertisingPauseRequest([param: Required, StringLength(1000)] string Reason);

public sealed record AdminAdvertisingCampaignListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminAdvertisingCampaignListItem> Items);
public sealed record AdminAdvertisingCampaignListItem(Guid Id, string CampaignName, string CampaignTypeCode, string AudienceTypeCode,
    string? OwnerDisplayName, IReadOnlyList<string> Placements, DateTime StartAt, DateTime? EndAt, string StatusCode,
    string ReviewStatusCode, string PublicationStatus, long ImpressionCount, long ClickCount);
public sealed record AdminAdvertisingCampaignDetail(Guid Id, string CampaignName, string CampaignTypeCode, string AudienceTypeCode,
    string OwnerTypeCode, string? OwnerDisplayName, DateTime StartAt, DateTime? EndAt, string StatusCode, int Priority,
    string DestinationTypeCode, string? DestinationValue, string ReviewStatusCode, DateTime? ApprovedAt,
    string? RejectionReason, string PublicationStatus, IReadOnlyList<AdvertisingPlacementResponse> Placements,
    IReadOnlyList<AdvertisingTargetResponse> Categories, IReadOnlyList<AdvertisingTargetResponse> Areas,
    IReadOnlyList<AdvertisingCreativeResponse> Creatives, AdvertisingMetricResponse Metrics,
    IReadOnlyList<AdvertisingAuditResponse> History, string RowVersion);
public sealed record AdvertisingPlacementResponse(Guid Id, string Code, string Name, string? Description, string? RouteHint, bool IsActive);
public sealed record AdvertisingTargetResponse(Guid Id, string Name);
public sealed record AdvertisingCreativeResponse(Guid Id, string Title, string? Subtitle, string? BodyText, Guid? FileId,
    string? FileName, string? AltText, string? ButtonText, string? DestinationTypeCode, string? DestinationValue,
    int DisplayOrder, string StatusCode, string RowVersion);
public sealed record AdvertisingMetricResponse(long Impressions, long Clicks);
public sealed record AdvertisingAuditResponse(DateTime OccurredAt, string ActionCode, string ResultCode, string? Reason);

public sealed record SaveManagedContentRequest(
    [param: Required, StringLength(30)] string ContentTypeCode,
    [param: Required, StringLength(20)] string AudienceTypeCode,
    [param: Required, StringLength(300)] string Title,
    string? BodyText,
    [param: StringLength(1000)] string? QuestionText,
    string? AnswerText,
    Guid? FileId,
    [param: StringLength(100)] string? LinkText,
    [param: Required, StringLength(30)] string DestinationTypeCode,
    [param: StringLength(2000)] string? DestinationValue,
    DateTime StartAt,
    DateTime? EndAt,
    int DisplayOrder,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> AreaIds,
    [param: StringLength(1000)] string? ChangeReason);

public sealed record AdminManagedContentListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminManagedContentListItem> Items);
public sealed record AdminManagedContentListItem(Guid Id, string ContentTypeCode, string AudienceTypeCode, string Title,
    int CurrentVersionNo, DateTime StartAt, DateTime? EndAt, string StatusCode, string ReviewStatusCode, string PublicationStatus);
public sealed record AdminManagedContentDetail(Guid Id, string ContentTypeCode, string AudienceTypeCode, string StatusCode,
    string ReviewStatusCode, int CurrentVersionNo, int DisplayOrder, DateTime StartAt, DateTime? EndAt, DateTime? ApprovedAt,
    string? RejectionReason, string PublicationStatus, ManagedContentVersionResponse CurrentVersion,
    IReadOnlyList<ManagedContentVersionResponse> Versions, IReadOnlyList<AdvertisingTargetResponse> Categories,
    IReadOnlyList<AdvertisingTargetResponse> Areas, IReadOnlyList<AdvertisingAuditResponse> History, string RowVersion);
public sealed record ManagedContentVersionResponse(Guid Id, int VersionNo, string Title, string? BodyText, string? QuestionText,
    string? AnswerText, Guid? FileId, string? FileName, string? LinkText, string DestinationTypeCode,
    string? DestinationValue, string? ChangeReason, DateTime CreatedAt);

public sealed record PublicAdvertisingCreative(Guid CampaignId, Guid CreativeId, string CampaignTypeCode, string Title,
    string? Subtitle, string? BodyText, Guid? MediaId, string? AltText, string? ButtonText,
    string DestinationTypeCode, string? DestinationValue, int Priority, int DisplayOrder);
public sealed record PublicManagedContent(Guid Id, string ContentTypeCode, string Title, string? BodyText,
    string? QuestionText, string? AnswerText, Guid? MediaId, string? LinkText,
    string DestinationTypeCode, string? DestinationValue, int DisplayOrder, int VersionNo);
public sealed record AdvertisingEventRequest([param: Required, StringLength(20)] string AudienceTypeCode,
    [param: Required, StringLength(50)] string PlacementCode, Guid? CategoryId, Guid? AreaId);

public sealed class AdvertisingContentException(string businessCode, string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
