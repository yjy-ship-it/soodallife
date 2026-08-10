using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminFeePolicyListResponse(
    IReadOnlyList<AdminFeePolicyResponse> Policies,
    Guid? CurrentPolicyId,
    IReadOnlyList<string> AllowedPolicyKinds,
    IReadOnlyList<string> AllowedTransactionTypes,
    IReadOnlyList<string> AllowedCalculationMethods,
    IReadOnlyList<string> AllowedChargeTimings,
    IReadOnlyList<string> AllowedCurrencies);

public sealed record AdminFeePolicyResponse(
    Guid Id,
    string PolicyVersion,
    string? SourcePolicyCode,
    string PolicyKindCode,
    string TransactionTypeCode,
    string? CalculationMethod,
    decimal? FeeAmount,
    decimal? MinBaseAmount,
    decimal? MaxBaseAmount,
    decimal? Rate,
    decimal? MonthlyAmount,
    decimal? PerVisitAmount,
    string CurrencyCode,
    string ChargeTiming,
    string? RestoreRule,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string EffectiveStatus,
    bool IsCurrentlyEffective,
    bool IsReferenced,
    bool CanEdit,
    bool IsActive);

public sealed record SaveAdminFeePolicyRequest(
    [param: Required, StringLength(30, MinimumLength = 1)] string PolicyVersion,
    [param: Required, StringLength(20, MinimumLength = 1)] string PolicyKindCode,
    [param: Required, StringLength(20, MinimumLength = 1)] string TransactionTypeCode,
    [param: StringLength(100)] string? CalculationMethod,
    decimal? FeeAmount,
    decimal? MinBaseAmount,
    decimal? MaxBaseAmount,
    decimal? Rate,
    decimal? MonthlyAmount,
    decimal? PerVisitAmount,
    [param: Required, StringLength(3, MinimumLength = 3)] string CurrencyCode,
    [param: Required, StringLength(300, MinimumLength = 1)] string ChargeTiming,
    [param: StringLength(1000)] string? RestoreRule,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive = true);

public sealed record EffectiveFeePolicyResult(
    Guid CategoryFeePolicyId,
    string PolicyVersion,
    string PolicyKindCode,
    string TransactionTypeCode,
    string? CalculationMethod,
    decimal? FeeAmount,
    decimal? MinBaseAmount,
    decimal? MaxBaseAmount,
    decimal? Rate,
    decimal? MonthlyAmount,
    decimal? PerVisitAmount,
    string CurrencyCode,
    string ChargeTiming,
    string? RestoreRule,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);
