namespace SoodalLife.Api.Features.Catalog;

public sealed record PublicCategoryResponse(
    Guid Id,
    string? Code,
    string Name,
    string Level,
    Guid? ParentId,
    int DisplayOrder,
    int ChildCount);

public sealed record PublicPriceSummaryResponse(
    string PriceMethod,
    string PriceDisplayLabel,
    decimal? ReferenceAmount,
    decimal? RecommendedMinAmount,
    decimal? RecommendedMaxAmount,
    string? WorkUnit,
    string Currency,
    string VatPolicyCode,
    string VatDisplayText,
    decimal? VatAmount,
    decimal? TotalAmount,
    string GuidanceText);

public sealed record PublicServiceSummaryResponse(
    Guid Id,
    string? Code,
    string? Slug,
    string Name,
    Guid MajorId,
    string MajorName,
    Guid MiddleId,
    string MiddleName,
    string? Description,
    PublicPriceSummaryResponse? Price);

public sealed record PublicServiceRequestFieldResponse(
    Guid Id,
    string Label,
    string InputType,
    bool Required,
    string? Unit,
    int DisplayOrder);

public sealed record PublicServiceDetailResponse(
    Guid Id,
    string? Code,
    string? Slug,
    string Name,
    Guid MajorId,
    string MajorName,
    Guid MiddleId,
    string MiddleName,
    string? Description,
    PublicPriceSummaryResponse? Price,
    string OnsiteRequirement,
    string CoverageTypeCode,
    bool RequiresServiceAddress,
    bool EmergencyRequestAllowed,
    bool SubscriptionAvailable,
    int DefaultWarrantyDays,
    string RequestGuide,
    string? ProviderRequirementGuide,
    string SeoTitle,
    string SeoDescription,
    string? SearchKeywordsText,
    IReadOnlyList<PublicServiceRequestFieldResponse> RequestFields);
