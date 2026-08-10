using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminPricePolicyService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminPricePolicyListResponse?> GetPoliciesAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var policies = await dbContext.CategoryPricePolicies.AsNoTracking().Where(policy => policy.CategoryId == serviceId)
            .OrderByDescending(policy => policy.EffectiveFrom).ThenByDescending(policy => policy.Id).ToListAsync(cancellationToken);
        var responses = await BuildResponsesAsync(policies, cancellationToken);
        return new AdminPricePolicyListResponse(
            responses,
            responses.FirstOrDefault(policy => policy.IsCurrentlyEffective)?.Id,
            await dbContext.CategoryPricePolicies.AsNoTracking().Select(policy => policy.LegacyPriceMethodText).Distinct().OrderBy(value => value).ToListAsync(cancellationToken),
            await dbContext.CategoryPricePolicies.AsNoTracking().Select(policy => policy.LegacyVatDisplayRuleText).Distinct().OrderBy(value => value).ToListAsync(cancellationToken),
            true, true, true, true);
    }

    public async Task<AdminPricePolicyResponse?> GetCurrentAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var policy = await dbContext.CategoryPricePolicies.AsNoTracking()
            .Where(item => item.CategoryId == serviceId && item.IsActive && item.EffectiveFrom <= today && (item.EffectiveTo == null || item.EffectiveTo > today))
            .OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        return policy is null ? null : (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse?> GetPolicyAsync(Guid servicePublicId, Guid policyPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var policy = await dbContext.CategoryPricePolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == serviceId && item.PublicId == policyPublicId, cancellationToken);
        return policy is null ? null : (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> CreateVersionAsync(Guid servicePublicId, SaveAdminPricePolicyRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken) ?? throw NotFound("가격정책을 등록할 서비스를 찾을 수 없습니다.");
        var source = await dbContext.CategoryPricePolicies.Where(policy => policy.CategoryId == serviceId)
            .OrderByDescending(policy => policy.EffectiveFrom).ThenByDescending(policy => policy.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_SOURCE_NOT_FOUND", "복사할 기존 가격정책이 없습니다.", StatusCodes.Status409Conflict);
        await ValidateAsync(serviceId, null, request, cancellationToken);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        await using var transaction = dbContext.Database.IsRelational() ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        foreach (var overlap in await FindOverlapsAsync(serviceId, null, request.EffectiveFrom, request.EffectiveTo, cancellationToken))
        {
            if (overlap.EffectiveFrom >= request.EffectiveFrom)
                throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_PERIOD_OVERLAP", "이미 같은 기간에 적용되는 가격정책이 있습니다.", StatusCodes.Status409Conflict);
            overlap.EffectiveTo = request.EffectiveFrom;
            overlap.UpdatedAt = now;
            overlap.UpdatedByUserId = actorUserId;
        }

        var created = new CategoryPricePolicy
        {
            CategoryId = serviceId,
            LegacyCategoryPolicyId = null,
            PriceTypeCode = source.PriceTypeCode,
            RecommendedMinAmount = source.RecommendedMinAmount,
            RecommendedMaxAmount = source.RecommendedMaxAmount,
            UnitPriceAmount = source.UnitPriceAmount,
            MinimumChargeAmount = source.MinimumChargeAmount,
            CurrencyCode = source.CurrencyCode,
            VatPolicyCode = source.VatPolicyCode,
            CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId,
        };
        ApplyValues(created, request);
        dbContext.CategoryPricePolicies.Add(created);
        AddAudit(actorUserId, "PRICE_POLICY_CREATED", "CATEGORY_PRICE_POLICY", created.PublicId, null, Snapshot(created), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return (await BuildResponsesAsync([created], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> UpdateFutureVersionAsync(Guid servicePublicId, Guid policyPublicId, SaveAdminPricePolicyRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        await ValidateAsync(policy.CategoryId, policy.Id, request, cancellationToken);
        if ((await FindOverlapsAsync(policy.CategoryId, policy.Id, request.EffectiveFrom, request.EffectiveTo, cancellationToken)).Count > 0)
            throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_PERIOD_OVERLAP", "이미 같은 기간에 적용되는 가격정책이 있습니다.", StatusCodes.Status409Conflict);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var before = Snapshot(policy);
        ApplyValues(policy, request);
        policy.UpdatedAt = DateTime.UtcNow;
        policy.UpdatedByUserId = actorUserId;
        AddAudit(actorUserId, "PRICE_POLICY_UPDATED", "CATEGORY_PRICE_POLICY", policy.PublicId, before, Snapshot(policy), policy.UpdatedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> CreateOptionAsync(Guid servicePublicId, Guid policyPublicId, SaveAdminPricePolicyOptionRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        var option = new CategoryPricePolicyOption { PricePolicyId = policy.Id, OptionName = request.OptionName.Trim(), AdditionalAmount = request.AdditionalAmount,
            DisplayOrder = request.DisplayOrder, IsActive = request.IsActive, CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId };
        dbContext.CategoryPricePolicyOptions.Add(option);
        AddAudit(actorUserId, "PRICE_POLICY_OPTION_CREATED", "CATEGORY_PRICE_POLICY_OPTION", option.PublicId, null, JsonSerializer.Serialize(request), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> UpdateOptionAsync(Guid servicePublicId, Guid policyPublicId, Guid optionPublicId, SaveAdminPricePolicyOptionRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        var option = await dbContext.CategoryPricePolicyOptions.SingleOrDefaultAsync(item => item.PricePolicyId == policy.Id && item.PublicId == optionPublicId, cancellationToken)
            ?? throw NotFound("가격 옵션을 찾을 수 없습니다.");
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var before = JsonSerializer.Serialize(new { option.OptionName, option.AdditionalAmount, option.DisplayOrder, option.IsActive });
        option.OptionName = request.OptionName.Trim(); option.AdditionalAmount = request.AdditionalAmount; option.DisplayOrder = request.DisplayOrder; option.IsActive = request.IsActive;
        option.UpdatedAt = DateTime.UtcNow; option.UpdatedByUserId = actorUserId;
        AddAudit(actorUserId, "PRICE_POLICY_OPTION_UPDATED", "CATEGORY_PRICE_POLICY_OPTION", option.PublicId, before, JsonSerializer.Serialize(request), option.UpdatedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> CreateSurchargeAsync(Guid servicePublicId, Guid policyPublicId, SaveAdminPricePolicySurchargeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ValidateSurcharge(request);
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        var surcharge = new CategoryPricePolicySurcharge { PricePolicyId = policy.Id, SurchargeName = request.SurchargeName.Trim(), CalculationTypeCode = request.CalculationTypeCode.Trim().ToUpperInvariant(),
            Amount = request.Amount, Rate = request.Rate, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive, CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId };
        dbContext.CategoryPricePolicySurcharges.Add(surcharge);
        AddAudit(actorUserId, "PRICE_POLICY_SURCHARGE_CREATED", "CATEGORY_PRICE_POLICY_SURCHARGE", surcharge.PublicId, null, JsonSerializer.Serialize(request), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<AdminPricePolicyResponse> UpdateSurchargeAsync(Guid servicePublicId, Guid policyPublicId, Guid surchargePublicId, SaveAdminPricePolicySurchargeRequest request, Guid actorPublicId, CancellationToken cancellationToken)
    {
        ValidateSurcharge(request);
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        var surcharge = await dbContext.CategoryPricePolicySurcharges.SingleOrDefaultAsync(item => item.PricePolicyId == policy.Id && item.PublicId == surchargePublicId, cancellationToken)
            ?? throw NotFound("할증 정책을 찾을 수 없습니다.");
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var before = JsonSerializer.Serialize(new { surcharge.SurchargeName, surcharge.CalculationTypeCode, surcharge.Amount, surcharge.Rate, surcharge.DisplayOrder, surcharge.IsActive });
        surcharge.SurchargeName = request.SurchargeName.Trim(); surcharge.CalculationTypeCode = request.CalculationTypeCode.Trim().ToUpperInvariant();
        surcharge.Amount = request.Amount; surcharge.Rate = request.Rate; surcharge.DisplayOrder = request.DisplayOrder; surcharge.IsActive = request.IsActive;
        surcharge.UpdatedAt = DateTime.UtcNow; surcharge.UpdatedByUserId = actorUserId;
        AddAudit(actorUserId, "PRICE_POLICY_SURCHARGE_UPDATED", "CATEGORY_PRICE_POLICY_SURCHARGE", surcharge.PublicId, before, JsonSerializer.Serialize(request), surcharge.UpdatedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    private async Task<IReadOnlyList<AdminPricePolicyResponse>> BuildResponsesAsync(IReadOnlyList<CategoryPricePolicy> policies, CancellationToken cancellationToken)
    {
        var ids = policies.Select(policy => policy.Id).ToArray();
        var legacyIds = policies.Where(policy => policy.LegacyCategoryPolicyId.HasValue).Select(policy => policy.LegacyCategoryPolicyId!.Value).ToArray();
        var referencedLegacyIds = await dbContext.ServiceRequests.AsNoTracking().Where(request => legacyIds.Contains(request.CategoryPolicyId))
            .Select(request => request.CategoryPolicyId).Distinct().ToListAsync(cancellationToken);
        var options = await dbContext.CategoryPricePolicyOptions.AsNoTracking().Where(item => ids.Contains(item.PricePolicyId)).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var surcharges = await dbContext.CategoryPricePolicySurcharges.AsNoTracking().Where(item => ids.Contains(item.PricePolicyId)).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return policies.Select(policy => BuildResponse(policy, policy.LegacyCategoryPolicyId.HasValue && referencedLegacyIds.Contains(policy.LegacyCategoryPolicyId.Value), today,
            options.Where(item => item.PricePolicyId == policy.Id), surcharges.Where(item => item.PricePolicyId == policy.Id))).ToArray();
    }

    private static AdminPricePolicyResponse BuildResponse(CategoryPricePolicy policy, bool referenced, DateOnly today, IEnumerable<CategoryPricePolicyOption> options, IEnumerable<CategoryPricePolicySurcharge> surcharges)
    {
        var status = !policy.IsActive ? "INACTIVE" : policy.EffectiveFrom > today ? "SCHEDULED" : policy.EffectiveTo.HasValue && policy.EffectiveTo <= today ? "ENDED" : "CURRENT";
        return new AdminPricePolicyResponse(policy.PublicId, policy.PolicyVersion, policy.LegacyPriceMethodText, policy.PriceTypeCode,
            policy.BaseAmount, policy.MinimumBudgetAmount, policy.RecommendedMinAmount, policy.RecommendedMaxAmount, policy.UnitText,
            policy.UnitPriceAmount, policy.MinimumChargeAmount, policy.CurrencyCode, policy.LegacyVatDisplayRuleText, policy.VatPolicyCode,
            policy.EffectiveFrom, policy.EffectiveTo, status, status == "CURRENT", referenced, status == "SCHEDULED" && !referenced,
            policy.IsActive,
            options.Select(item => new AdminPricePolicyOptionResponse(item.PublicId, item.OptionName, item.AdditionalAmount, item.DisplayOrder, item.IsActive)).ToArray(),
            surcharges.Select(item => new AdminPricePolicySurchargeResponse(item.PublicId, item.SurchargeName, item.CalculationTypeCode, item.Amount, item.Rate, item.DisplayOrder, item.IsActive)).ToArray());
    }

    private async Task ValidateAsync(long serviceId, long? policyId, SaveAdminPricePolicyRequest request, CancellationToken cancellationToken)
    {
        if (request.EffectiveFrom == default) throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_PERIOD_INVALID", "적용 시작일을 확인해 주세요.");
        if (request.BasePriceAmount < 0 || request.MinimumBudgetAmount < 0) throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_AMOUNT_INVALID", "금액은 0원 이상이어야 합니다.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo <= request.EffectiveFrom) throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_PERIOD_INVALID", "적용 종료일은 시작일보다 뒤여야 합니다.");
        if (await dbContext.CategoryPricePolicies.AnyAsync(policy => policy.CategoryId == serviceId && policy.Id != policyId && policy.PolicyVersion == request.PolicyVersion.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_VERSION_DUPLICATED", "같은 서비스에 동일한 정책버전이 이미 있습니다.", StatusCodes.Status409Conflict);
        if (!await dbContext.CategoryPricePolicies.AsNoTracking().AnyAsync(policy => policy.LegacyPriceMethodText == request.PriceMethod.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_METHOD_INVALID", "현재 데이터에서 사용하는 가격방식만 선택할 수 있습니다.");
        if (!await dbContext.CategoryPricePolicies.AsNoTracking().AnyAsync(policy => policy.LegacyVatDisplayRuleText == request.VatRule.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_VAT_INVALID", "현재 데이터에서 사용하는 VAT 정책만 선택할 수 있습니다.");
    }

    private static void ValidateSurcharge(SaveAdminPricePolicySurchargeRequest request)
    {
        var type = request.CalculationTypeCode.Trim().ToUpperInvariant();
        var valid = type == "AMOUNT" ? request.Amount is >= 0 && request.Rate is null : type == "RATE" && request.Amount is null && request.Rate is >= 0 and <= 1;
        if (!valid) throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_SURCHARGE_INVALID", "할증 계산방식과 금액 또는 비율을 확인해 주세요.");
    }

    private Task<List<CategoryPricePolicy>> FindOverlapsAsync(long serviceId, long? excludedId, DateOnly from, DateOnly? to, CancellationToken cancellationToken) =>
        dbContext.CategoryPricePolicies.Where(policy => policy.CategoryId == serviceId && policy.Id != excludedId && policy.IsActive &&
            policy.EffectiveFrom < (to ?? DateOnly.MaxValue) && from < (policy.EffectiveTo ?? DateOnly.MaxValue)).ToListAsync(cancellationToken);

    private async Task<CategoryPricePolicy> GetTrackedPolicyAsync(Guid servicePublicId, Guid policyPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken) ?? throw NotFound("서비스를 찾을 수 없습니다.");
        return await dbContext.CategoryPricePolicies.SingleOrDefaultAsync(policy => policy.CategoryId == serviceId && policy.PublicId == policyPublicId, cancellationToken)
            ?? throw NotFound("가격정책을 찾을 수 없습니다.");
    }

    private async Task EnsureEditableAsync(CategoryPricePolicy policy, CancellationToken cancellationToken)
    {
        var referenced = policy.LegacyCategoryPolicyId.HasValue && await dbContext.ServiceRequests.AnyAsync(request => request.CategoryPolicyId == policy.LegacyCategoryPolicyId.Value, cancellationToken);
        if (policy.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) || referenced)
            throw new AdminServiceCategoryException("ADMIN_PRICE_POLICY_HISTORY_PROTECTED", "적용이 시작됐거나 요청에 사용된 정책은 수정할 수 없습니다. 새 버전을 등록해 주세요.", StatusCodes.Status409Conflict);
    }

    private Task<long?> GetServiceIdAsync(Guid publicId, CancellationToken cancellationToken) => dbContext.ServiceCategories.AsNoTracking()
        .Where(category => category.PublicId == publicId && category.LevelCode == "SERVICE").Select(category => (long?)category.Id).SingleOrDefaultAsync(cancellationToken);
    private async Task<long> GetActorUserIdAsync(Guid publicId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.PublicId == publicId && user.StatusCode == "ACTIVE").Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);

    private static void ApplyValues(CategoryPricePolicy policy, SaveAdminPricePolicyRequest request)
    {
        policy.PolicyVersion = request.PolicyVersion.Trim(); policy.LegacyPriceMethodText = request.PriceMethod.Trim();
        if (request.BasePriceAmount.HasValue) policy.BaseAmount = request.BasePriceAmount.Value;
        if (request.MinimumBudgetAmount.HasValue) policy.MinimumBudgetAmount = request.MinimumBudgetAmount.Value;
        policy.UnitText = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
        policy.LegacyVatDisplayRuleText = request.VatRule.Trim(); policy.EffectiveFrom = request.EffectiveFrom; policy.EffectiveTo = request.EffectiveTo; policy.IsActive = request.IsActive;
    }

    private void AddAudit(long actorUserId, string actionCode, string entityType, Guid entityPublicId, string? before, string after, DateTime now) =>
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actorUserId, ActorRoleCode = RoleCodes.Admin, ActionCode = actionCode,
            EntityType = entityType, EntityPublicId = entityPublicId, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = after });
    private static string Snapshot(CategoryPricePolicy policy) => JsonSerializer.Serialize(new { policy.PolicyVersion, policy.LegacyPriceMethodText, policy.PriceTypeCode,
        policy.BaseAmount, policy.MinimumBudgetAmount, policy.RecommendedMinAmount, policy.RecommendedMaxAmount, policy.UnitText, policy.UnitPriceAmount,
        policy.MinimumChargeAmount, policy.CurrencyCode, policy.LegacyVatDisplayRuleText, policy.VatPolicyCode, policy.EffectiveFrom, policy.EffectiveTo, policy.IsActive });
    private static AdminServiceCategoryException NotFound(string message) => new("ADMIN_PRICE_POLICY_NOT_FOUND", message, StatusCodes.Status404NotFound);
}
