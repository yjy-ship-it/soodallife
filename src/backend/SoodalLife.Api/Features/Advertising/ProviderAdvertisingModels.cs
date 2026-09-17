using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Advertising;

public sealed record ProviderAdvertisingRateResponse(Guid Id, Guid PlacementId, string PlacementCode, string PlacementName, string? PlacementDescription, int DurationDays, decimal FixedAmount, decimal ProvinceUnitAmount, decimal DistrictUnitAmount, decimal RegionalFeeCapAmount, string CurrencyCode, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive);
public sealed record ProviderAdvertisingAreaResponse(Guid Id, string Name, IReadOnlyList<ProviderAdvertisingDistrictResponse> Districts);
public sealed record ProviderAdvertisingDistrictResponse(Guid Id, string Name);
public sealed record SaveProviderAdvertisingApplicationRequest(
    Guid RatePolicyId,
    [param: Required, StringLength(200)] string CampaignName,
    DateTime StartAt,
    [param: Required, StringLength(200)] string Title,
    [param: StringLength(300)] string? Subtitle,
    [param: StringLength(4000)] string? BodyText,
    [param: StringLength(40)] string? TemplateCode,
    [param: StringLength(50)] string? ButtonText,
    [param: Required, StringLength(30)] string DestinationTypeCode,
    [param: StringLength(2000)] string? DestinationValue,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> AreaIds,
    bool AutoRenewEnabled,
    [param: StringLength(2000)] string? SupplementNote);
public sealed record ProviderAdvertisingApplicationListResponse(IReadOnlyList<ProviderAdvertisingApplicationResponse> Items);
public sealed record ProviderAdvertisingApplicationResponse(Guid Id, Guid CampaignId, string CampaignName, string ProviderName, string PlacementCode, string PlacementName, int DurationDays, decimal BaseFeeAmount, decimal RegionalFeeAmount, decimal FeeAmount, int ProvinceTargetCount, int DistrictTargetCount, string CurrencyCode, string StatusCode, string FeeStatusCode, bool AutoRenewEnabled, string AutoRenewStatusCode, DateTime? NextRenewalAt, bool RenewalConsentRequired, decimal? RenewalConsentFeeAmount, int RenewalCycleNo, DateTime? LastRenewedAt, DateTime StartAt, DateTime EndAt, string Title, string? Subtitle, string? BodyText, string TemplateCode, string? ButtonText, string DestinationTypeCode, string? DestinationValue, string? SupplementNote, string? RejectionReason, DateTime SubmittedAt, DateTime? ResubmittedAt, DateTime? ApprovedAt, DateTime? PublishedAt, IReadOnlyList<ProviderAdvertisingTargetResponse> Categories, IReadOnlyList<ProviderAdvertisingTargetResponse> Areas);
public sealed record ProviderAdvertisingTargetResponse(Guid Id, string Name, string LevelCode, string? ParentName);
public sealed record ProviderAdvertisingReviewRequest([param: Required, StringLength(20)] string ActionCode, [param: StringLength(2000)] string? Reason);
public sealed record ProviderAdvertisingPublishRequest([param: StringLength(1000)] string? Note);
public sealed record UpdateProviderAdvertisingRateRequest(decimal FixedAmount, decimal ProvinceUnitAmount, decimal DistrictUnitAmount, decimal RegionalFeeCapAmount, bool IsActive, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record ProviderAdvertisingPolicyGuideResponse(string BillingModel, string BillingTiming, string VatTreatment, IReadOnlyList<string> Workflow, IReadOnlyList<string> RefundRules);
public sealed record SetProviderAdvertisingAutoRenewRequest(bool Enabled);
public sealed record ProviderAdvertisingRenewalHistoryResponse(Guid Id,int CycleNo,DateTime DueAt,decimal BaseFeeAmount,decimal RegionalFeeAmount,decimal FeeAmount,string CurrencyCode,string StatusCode,DateTime? NoticeSentAt,DateTime? ProcessedAt,string? FailureReason);
public sealed record ProviderAdvertisingRenewalStateResponse(Guid ApplicationId,bool AutoRenewEnabled,string AutoRenewStatusCode,DateTime? NextRenewalAt,bool RenewalConsentRequired,decimal? RenewalConsentFeeAmount,int RenewalCycleNo);

public sealed class ProviderAdvertisingException(string businessCode, string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
