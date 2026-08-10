using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminPricePolicyListResponse(
    IReadOnlyList<AdminPricePolicyResponse> Policies,
    Guid? CurrentPolicyId,
    IReadOnlyList<string> AllowedPriceMethods,
    IReadOnlyList<string> AllowedVatRules,
    bool SupportsRecommendedMaximumPrice,
    bool SupportsExplicitActiveStatus,
    bool SupportsPriceOptions,
    bool SupportsSurcharges);

public sealed record AdminPricePolicyResponse(
    Guid Id,
    string PolicyVersion,
    string PriceMethod,
    string? PriceTypeCode,
    decimal BasePriceAmount,
    decimal? MinimumBudgetAmount,
    decimal? RecommendedMinAmount,
    decimal? RecommendedMaxAmount,
    string? Unit,
    decimal? UnitPriceAmount,
    decimal? MinimumChargeAmount,
    string CurrencyCode,
    string VatRule,
    string? VatPolicyCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string EffectiveStatus,
    bool IsCurrentlyEffective,
    bool IsReferenced,
    bool CanEdit,
    bool IsActive,
    IReadOnlyList<AdminPricePolicyOptionResponse> Options,
    IReadOnlyList<AdminPricePolicySurchargeResponse> Surcharges);

public sealed record SaveAdminPricePolicyRequest(
    [param: Required, StringLength(30, MinimumLength = 1)] string PolicyVersion,
    [param: Required, StringLength(30, MinimumLength = 1)] string PriceMethod,
    decimal? BasePriceAmount,
    decimal? MinimumBudgetAmount,
    [param: StringLength(100)] string? Unit,
    [param: Required, StringLength(100, MinimumLength = 1)] string VatRule,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive = true);

public sealed record AdminPricePolicyOptionResponse(Guid Id, string OptionName, decimal AdditionalAmount, int DisplayOrder, bool IsActive);
public sealed record SaveAdminPricePolicyOptionRequest(
    [param: Required, StringLength(200, MinimumLength = 1)] string OptionName,
    decimal AdditionalAmount,
    [param: Range(0, int.MaxValue)] int DisplayOrder,
    bool IsActive);

public sealed record AdminPricePolicySurchargeResponse(Guid Id, string SurchargeName, string CalculationTypeCode, decimal? Amount, decimal? Rate, int DisplayOrder, bool IsActive);
public sealed record SaveAdminPricePolicySurchargeRequest(
    [param: Required, StringLength(200, MinimumLength = 1)] string SurchargeName,
    [param: Required, StringLength(20, MinimumLength = 1)] string CalculationTypeCode,
    decimal? Amount,
    decimal? Rate,
    [param: Range(0, int.MaxValue)] int DisplayOrder,
    bool IsActive);
