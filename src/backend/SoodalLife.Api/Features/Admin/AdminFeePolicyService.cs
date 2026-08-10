using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminFeePolicyService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminFeePolicyListResponse?> GetPoliciesAsync(Guid servicePublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;

        var policies = await dbContext.CategoryFeePolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == serviceId)
            .OrderByDescending(policy => policy.EffectiveFrom)
            .ThenByDescending(policy => policy.Id)
            .ToListAsync(cancellationToken);
        var responses = await BuildResponsesAsync(policies, cancellationToken);

        return new AdminFeePolicyListResponse(
            responses,
            responses.FirstOrDefault(policy => policy.IsCurrentlyEffective)?.Id,
            policies.Select(policy => policy.PolicyKindCode).Distinct().OrderBy(value => value).ToArray(),
            policies.Select(policy => policy.TransactionTypeCode).Distinct().OrderBy(value => value).ToArray(),
            policies.Where(policy => policy.CalculationMethodText != null).Select(policy => policy.CalculationMethodText!).Distinct().OrderBy(value => value).ToArray(),
            policies.Select(policy => policy.ChargeTimingText).Distinct().OrderBy(value => value).ToArray(),
            policies.Select(policy => policy.CurrencyCode).Distinct().OrderBy(value => value).ToArray());
    }

    public async Task<AdminFeePolicyResponse?> GetPolicyAsync(Guid servicePublicId, Guid policyPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var policy = await dbContext.CategoryFeePolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == serviceId && item.PublicId == policyPublicId, cancellationToken);
        return policy is null ? null : (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    public async Task<EffectiveFeePolicyResult?> ResolveEffectiveAsync(
        Guid servicePublicId,
        DateOnly effectiveOn,
        string transactionTypeCode,
        CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken);
        if (serviceId is null) return null;
        var normalizedTransactionType = transactionTypeCode.Trim().ToUpperInvariant();
        var policy = await dbContext.CategoryFeePolicies.AsNoTracking()
            .Where(item => item.CategoryId == serviceId && item.TransactionTypeCode == normalizedTransactionType && item.IsActive
                && item.EffectiveFrom <= effectiveOn && (item.EffectiveTo == null || item.EffectiveTo > effectiveOn))
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return policy is null ? null : ToEffectiveResult(policy);
    }

    public async Task<AdminFeePolicyResponse> CreateVersionAsync(
        Guid servicePublicId,
        SaveAdminFeePolicyRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken)
            ?? throw NotFound("수수료정책을 등록할 서비스를 찾을 수 없습니다.");
        var source = await dbContext.CategoryFeePolicies.Where(policy => policy.CategoryId == serviceId)
            .OrderByDescending(policy => policy.EffectiveFrom).ThenByDescending(policy => policy.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_SOURCE_NOT_FOUND", "복사할 기존 수수료정책이 없습니다.", StatusCodes.Status409Conflict);
        await ValidateAsync(serviceId, null, request, cancellationToken);
        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var now = DateTime.UtcNow;
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        foreach (var overlap in await FindOverlapsAsync(serviceId, null, request.EffectiveFrom, request.EffectiveTo, cancellationToken))
        {
            if (overlap.EffectiveFrom >= request.EffectiveFrom)
                throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_PERIOD_OVERLAP", "이미 같은 기간에 적용되는 수수료정책이 있습니다.", StatusCodes.Status409Conflict);
            var before = Snapshot(overlap);
            overlap.EffectiveTo = request.EffectiveFrom;
            overlap.UpdatedAt = now;
            overlap.UpdatedByUserId = actorUserId;
            AddAudit(actorUserId, "FEE_POLICY_PERIOD_UPDATED", overlap.PublicId, before, Snapshot(overlap), now);
        }

        var created = new CategoryFeePolicy
        {
            CategoryId = serviceId,
            LegacyCategoryPolicyId = null,
            SourceFeePolicyId = source.SourceFeePolicyId,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            UpdatedAt = now,
            UpdatedByUserId = actorUserId,
        };
        ApplyValues(created, request);
        dbContext.CategoryFeePolicies.Add(created);
        AddAudit(actorUserId, "FEE_POLICY_CREATED", created.PublicId, null, Snapshot(created), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return (await BuildResponsesAsync([created], cancellationToken)).Single();
    }

    public async Task<AdminFeePolicyResponse> UpdateFutureVersionAsync(
        Guid servicePublicId,
        Guid policyPublicId,
        SaveAdminFeePolicyRequest request,
        Guid actorPublicId,
        CancellationToken cancellationToken)
    {
        var policy = await GetTrackedPolicyAsync(servicePublicId, policyPublicId, cancellationToken);
        await EnsureEditableAsync(policy, cancellationToken);
        await ValidateAsync(policy.CategoryId, policy.Id, request, cancellationToken);
        if ((await FindOverlapsAsync(policy.CategoryId, policy.Id, request.EffectiveFrom, request.EffectiveTo, cancellationToken)).Count > 0)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_PERIOD_OVERLAP", "이미 같은 기간에 적용되는 수수료정책이 있습니다.", StatusCodes.Status409Conflict);

        var actorUserId = await GetActorUserIdAsync(actorPublicId, cancellationToken);
        var before = Snapshot(policy);
        ApplyValues(policy, request);
        policy.UpdatedAt = DateTime.UtcNow;
        policy.UpdatedByUserId = actorUserId;
        AddAudit(actorUserId, "FEE_POLICY_UPDATED", policy.PublicId, before, Snapshot(policy), policy.UpdatedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildResponsesAsync([policy], cancellationToken)).Single();
    }

    private async Task<IReadOnlyList<AdminFeePolicyResponse>> BuildResponsesAsync(
        IReadOnlyList<CategoryFeePolicy> policies,
        CancellationToken cancellationToken)
    {
        var sourceIds = policies.Where(policy => policy.SourceFeePolicyId.HasValue).Select(policy => policy.SourceFeePolicyId!.Value).Distinct().ToArray();
        var sourceCodes = await dbContext.FeePolicies.AsNoTracking().Where(policy => sourceIds.Contains(policy.Id))
            .ToDictionaryAsync(policy => policy.Id, policy => policy.Code, cancellationToken);
        var legacyIds = policies.Where(policy => policy.LegacyCategoryPolicyId.HasValue).Select(policy => policy.LegacyCategoryPolicyId!.Value).ToArray();
        var referencedLegacyIds = await dbContext.ServiceRequests.AsNoTracking().Where(request => legacyIds.Contains(request.CategoryPolicyId))
            .Select(request => request.CategoryPolicyId).Distinct().ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return policies.Select(policy =>
        {
            var referenced = policy.LegacyCategoryPolicyId.HasValue && referencedLegacyIds.Contains(policy.LegacyCategoryPolicyId.Value);
            var status = EffectiveStatus(policy, today);
            return new AdminFeePolicyResponse(
                policy.PublicId,
                policy.PolicyVersion,
                policy.SourceFeePolicyId.HasValue && sourceCodes.TryGetValue(policy.SourceFeePolicyId.Value, out var sourceCode) ? sourceCode : null,
                policy.PolicyKindCode,
                policy.TransactionTypeCode,
                policy.CalculationMethodText,
                policy.FeeAmount,
                policy.MinBaseAmount,
                policy.MaxBaseAmount,
                policy.Rate,
                policy.MonthlyAmount,
                policy.PerVisitAmount,
                policy.CurrencyCode,
                policy.ChargeTimingText,
                policy.RestoreRuleText,
                policy.EffectiveFrom,
                policy.EffectiveTo,
                status,
                status == "CURRENT",
                referenced,
                status == "SCHEDULED" && !referenced,
                policy.IsActive);
        }).ToArray();
    }

    private async Task ValidateAsync(long serviceId, long? policyId, SaveAdminFeePolicyRequest request, CancellationToken cancellationToken)
    {
        if (request.EffectiveFrom == default)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_PERIOD_INVALID", "적용 시작일을 확인해 주세요.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo <= request.EffectiveFrom)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_PERIOD_INVALID", "적용 종료일은 시작일보다 뒤여야 합니다.");
        if (request.FeeAmount < 0 || request.MinBaseAmount < 0 || request.MaxBaseAmount < 0 || request.MonthlyAmount < 0 || request.PerVisitAmount < 0)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_AMOUNT_INVALID", "금액은 0원 이상이어야 합니다.");
        if (request.Rate is < 0 or > 1)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_RATE_INVALID", "요율은 0 이상 1 이하여야 합니다.");
        if (request.MinBaseAmount.HasValue && request.MaxBaseAmount.HasValue && request.MinBaseAmount > request.MaxBaseAmount)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_BASE_RANGE_INVALID", "최소 기준금액은 최대 기준금액보다 클 수 없습니다.");

        var policyKind = request.PolicyKindCode.Trim().ToUpperInvariant();
        var transactionType = request.TransactionTypeCode.Trim().ToUpperInvariant();
        if (!await dbContext.CategoryFeePolicies.AsNoTracking().AnyAsync(policy => policy.CategoryId == serviceId && policy.PolicyKindCode == policyKind && policy.TransactionTypeCode == transactionType, cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_TYPE_INVALID", "현재 승인된 정책유형과 거래유형 조합만 사용할 수 있습니다.");
        if (!string.IsNullOrWhiteSpace(request.CalculationMethod)
            && !await dbContext.CategoryFeePolicies.AsNoTracking().AnyAsync(policy => policy.CategoryId == serviceId && policy.CalculationMethodText == request.CalculationMethod.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_CALCULATION_INVALID", "현재 승인된 계산방식만 사용할 수 있습니다.");
        if (!await dbContext.CategoryFeePolicies.AsNoTracking().AnyAsync(policy => policy.CategoryId == serviceId && policy.CurrencyCode == request.CurrencyCode.Trim().ToUpperInvariant(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_CURRENCY_INVALID", "현재 승인된 통화만 사용할 수 있습니다.");
        if (!await dbContext.CategoryFeePolicies.AsNoTracking().AnyAsync(policy => policy.CategoryId == serviceId && policy.ChargeTimingText == request.ChargeTiming.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_CHARGE_TIMING_INVALID", "현재 승인된 차감시점만 사용할 수 있습니다.");
        if (await dbContext.CategoryFeePolicies.AnyAsync(policy => policy.CategoryId == serviceId && policy.Id != policyId && policy.PolicyVersion == request.PolicyVersion.Trim(), cancellationToken))
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_VERSION_DUPLICATED", "같은 서비스에 동일한 정책버전이 이미 있습니다.", StatusCodes.Status409Conflict);
    }

    private Task<List<CategoryFeePolicy>> FindOverlapsAsync(long serviceId, long? excludedId, DateOnly from, DateOnly? to, CancellationToken cancellationToken) =>
        dbContext.CategoryFeePolicies.Where(policy => policy.CategoryId == serviceId && policy.Id != excludedId && policy.IsActive
            && policy.EffectiveFrom < (to ?? DateOnly.MaxValue) && from < (policy.EffectiveTo ?? DateOnly.MaxValue)).ToListAsync(cancellationToken);

    private async Task<CategoryFeePolicy> GetTrackedPolicyAsync(Guid servicePublicId, Guid policyPublicId, CancellationToken cancellationToken)
    {
        var serviceId = await GetServiceIdAsync(servicePublicId, cancellationToken) ?? throw NotFound("서비스를 찾을 수 없습니다.");
        return await dbContext.CategoryFeePolicies.SingleOrDefaultAsync(policy => policy.CategoryId == serviceId && policy.PublicId == policyPublicId, cancellationToken)
            ?? throw NotFound("수수료정책을 찾을 수 없습니다.");
    }

    private async Task EnsureEditableAsync(CategoryFeePolicy policy, CancellationToken cancellationToken)
    {
        var referenced = policy.LegacyCategoryPolicyId.HasValue
            && await dbContext.ServiceRequests.AnyAsync(request => request.CategoryPolicyId == policy.LegacyCategoryPolicyId.Value, cancellationToken);
        if (policy.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) || referenced)
            throw new AdminServiceCategoryException("ADMIN_FEE_POLICY_HISTORY_PROTECTED", "적용이 시작됐거나 요청에 사용된 정책은 수정할 수 없습니다. 새 버전을 등록해 주세요.", StatusCodes.Status409Conflict);
    }

    private Task<long?> GetServiceIdAsync(Guid publicId, CancellationToken cancellationToken) => dbContext.ServiceCategories.AsNoTracking()
        .Where(category => category.PublicId == publicId && category.LevelCode == "SERVICE")
        .Select(category => (long?)category.Id).SingleOrDefaultAsync(cancellationToken);

    private async Task<long> GetActorUserIdAsync(Guid publicId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.PublicId == publicId && user.StatusCode == "ACTIVE")
            .Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);

    private static void ApplyValues(CategoryFeePolicy policy, SaveAdminFeePolicyRequest request)
    {
        policy.PolicyVersion = request.PolicyVersion.Trim();
        policy.PolicyKindCode = request.PolicyKindCode.Trim().ToUpperInvariant();
        policy.TransactionTypeCode = request.TransactionTypeCode.Trim().ToUpperInvariant();
        policy.CalculationMethodText = string.IsNullOrWhiteSpace(request.CalculationMethod) ? null : request.CalculationMethod.Trim();
        policy.FeeAmount = request.FeeAmount;
        policy.MinBaseAmount = request.MinBaseAmount;
        policy.MaxBaseAmount = request.MaxBaseAmount;
        policy.Rate = request.Rate;
        policy.MonthlyAmount = request.MonthlyAmount;
        policy.PerVisitAmount = request.PerVisitAmount;
        policy.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        policy.ChargeTimingText = request.ChargeTiming.Trim();
        policy.RestoreRuleText = string.IsNullOrWhiteSpace(request.RestoreRule) ? null : request.RestoreRule.Trim();
        policy.EffectiveFrom = request.EffectiveFrom;
        policy.EffectiveTo = request.EffectiveTo;
        policy.IsActive = request.IsActive;
    }

    private void AddAudit(long actorUserId, string actionCode, Guid entityPublicId, string? before, string after, DateTime now) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorRoleCode = RoleCodes.Admin,
            ActionCode = actionCode,
            EntityType = "CATEGORY_FEE_POLICY",
            EntityPublicId = entityPublicId,
            ResultCode = "SUCCESS",
            BeforeJson = before,
            AfterJson = after,
        });

    private static string Snapshot(CategoryFeePolicy policy) => JsonSerializer.Serialize(new
    {
        policy.PolicyVersion,
        policy.PolicyKindCode,
        policy.TransactionTypeCode,
        policy.CalculationMethodText,
        policy.FeeAmount,
        policy.MinBaseAmount,
        policy.MaxBaseAmount,
        policy.Rate,
        policy.MonthlyAmount,
        policy.PerVisitAmount,
        policy.CurrencyCode,
        policy.ChargeTimingText,
        policy.RestoreRuleText,
        policy.EffectiveFrom,
        policy.EffectiveTo,
        policy.IsActive,
    });

    private static EffectiveFeePolicyResult ToEffectiveResult(CategoryFeePolicy policy) => new(
        policy.PublicId,
        policy.PolicyVersion,
        policy.PolicyKindCode,
        policy.TransactionTypeCode,
        policy.CalculationMethodText,
        policy.FeeAmount,
        policy.MinBaseAmount,
        policy.MaxBaseAmount,
        policy.Rate,
        policy.MonthlyAmount,
        policy.PerVisitAmount,
        policy.CurrencyCode,
        policy.ChargeTimingText,
        policy.RestoreRuleText,
        policy.EffectiveFrom,
        policy.EffectiveTo);

    private static string EffectiveStatus(CategoryFeePolicy policy, DateOnly today) =>
        !policy.IsActive ? "INACTIVE"
        : policy.EffectiveFrom > today ? "SCHEDULED"
        : policy.EffectiveTo.HasValue && policy.EffectiveTo <= today ? "ENDED"
        : "CURRENT";

    private static AdminServiceCategoryException NotFound(string message) =>
        new("ADMIN_FEE_POLICY_NOT_FOUND", message, StatusCodes.Status404NotFound);
}
