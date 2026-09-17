using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Matching;

public sealed record ProviderTradingEligibility(
    long? ProviderServiceCategoryId,
    bool UserAndRoleActive,
    bool ProviderApprovedAndActive,
    bool ServiceRegistered,
    bool ServiceApproved,
    bool AreaMatched,
    bool StructuredRequirementsConfigured,
    bool RequiredEvidenceValid,
    bool IsEligible,
    string? ReasonCode);

public sealed class ProviderTradingEligibilityService(SoodalLifeDbContext db)
{
    private static readonly string[] ExitLockStatuses = ["REQUESTED", "UNDER_REVIEW", "REFUND_REQUIRED", "BLOCKED_BY_ACTIVE_WORK", "READY_TO_COMPLETE"];
    public async Task<ProviderTradingEligibility> EvaluateAsync(
        long providerProfileId,
        long categoryId,
        long? administrativeAreaId,
        CancellationToken cancellationToken,
        bool allowNationwide = true)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var provider = await db.ProviderProfiles.AsNoTracking()
            .Where(item => item.Id == providerProfileId)
            .Select(item => new
            {
                item.UserId,
                item.ApprovalStatusCode,
                item.ActivityStatusCode,
                UserActive = db.Users.Any(user => user.Id == item.UserId && user.StatusCode == "ACTIVE"),
                RoleActive = db.UserRoles.Any(userRole => userRole.UserId == item.UserId && userRole.RevokedAt == null &&
                    db.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER")),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (provider is null)
            return Result(null, false, false, false, false, false, false, false, "PROVIDER_NOT_FOUND");

        var userAndRoleActive = provider.UserActive && provider.RoleActive;
        var providerApproved = provider.ApprovalStatusCode == "APPROVED" && provider.ActivityStatusCode == "ACTIVE";
        var exitLocked = await db.ProviderExitRequests.AsNoTracking().AnyAsync(item => item.ProviderProfileId == providerProfileId && ExitLockStatuses.Contains(item.StatusCode), cancellationToken);
        var service = await db.ProviderServiceCategories.AsNoTracking().SingleOrDefaultAsync(item =>
            item.ProviderProfileId == providerProfileId && item.CategoryId == categoryId && item.StatusCode == "ACTIVE", cancellationToken);
        var serviceRegistered = service is not null;
        var coverageTypeCode = await db.CategoryOperationPolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == categoryId && policy.IsActive && policy.EffectiveFrom <= today &&
                (policy.EffectiveTo == null || policy.EffectiveTo > today))
            .OrderByDescending(policy => policy.EffectiveFrom)
            .Select(policy => policy.CoverageTypeCode)
            .FirstOrDefaultAsync(cancellationToken);
        var areaMatched = service is not null && ((allowNationwide && service.IsNationwide) ||
            administrativeAreaId.HasValue && await db.ProviderServiceAreas.AsNoTracking().AnyAsync(item =>
                item.ProviderServiceCategoryId == service.Id && item.AdministrativeAreaId == administrativeAreaId.Value && item.StatusCode == "ACTIVE", cancellationToken));

        var operationPolicyIds = await db.CategoryOperationPolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == categoryId && policy.IsActive && policy.EffectiveFrom <= today &&
                (policy.EffectiveTo == null || policy.EffectiveTo > today))
            .Select(policy => policy.Id).ToArrayAsync(cancellationToken);
        var assignments = await db.CategoryProviderRequirementAssignments.AsNoTracking()
            .Where(item => operationPolicyIds.Contains(item.CategoryOperationPolicyId) && item.IsActive)
            .ToListAsync(cancellationToken);
        var autonomousNationwide = service is not null && service.IsNationwide &&
            ProviderCoveragePolicy.AllowsNationwide(coverageTypeCode) &&
            assignments.All(item => !item.IsRequired || !item.VerificationRequired);
        var serviceApproved = autonomousNationwide || service is not null && await db.ProviderServiceApprovals.AsNoTracking().AnyAsync(item =>
            item.ProviderServiceCategoryId == service.Id && item.ApprovalStatusCode == "APPROVED", cancellationToken);
        // An empty assignment set means that the category intentionally requires no evidence.
        // It must not be treated as an administrator configuration error.
        var requirementsConfigured = true;
        var required = assignments.Where(item => item.IsRequired).ToArray();
        var evidenceValid = autonomousNationwide || service is not null;
        if (evidenceValid && !autonomousNationwide)
        {
            var assignmentIds = required.Select(item => item.Id).ToArray();
            var verifications = await db.ProviderServiceRequirementVerifications.AsNoTracking()
                .Where(item => item.ProviderServiceCategoryId == service!.Id && assignmentIds.Contains(item.RequirementAssignmentId))
                .ToDictionaryAsync(item => item.RequirementAssignmentId, cancellationToken);
            evidenceValid = required.All(assignment =>
            {
                if (!assignment.VerificationRequired) return true;
                if (!verifications.TryGetValue(assignment.Id, out var verification) || verification.VerificationStatusCode != "APPROVED") return false;
                if (!assignment.ExpiryCheckRequired) return true;
                return verification.ExpiresAt.HasValue && verification.ExpiresAt.Value >= today.AddDays(assignment.MinimumValidDays ?? 0);
            });
        }

        var reason = exitLocked ? "PROVIDER_EXIT_IN_PROGRESS"
            : !userAndRoleActive ? "PROVIDER_USER_OR_ROLE_INACTIVE"
            : !providerApproved ? "PROVIDER_NOT_APPROVED_ACTIVE"
            : !serviceRegistered ? "SERVICE_CATEGORY_MISMATCH"
            : !serviceApproved ? "SERVICE_NOT_APPROVED"
            : !areaMatched ? "SERVICE_AREA_MISMATCH"
            : !requirementsConfigured ? "STRUCTURED_REQUIREMENTS_NOT_CONFIGURED"
            : !evidenceValid ? "REQUIRED_EVIDENCE_INVALID"
            : null;
        return Result(service?.Id, userAndRoleActive, providerApproved, serviceRegistered, serviceApproved, areaMatched,
            requirementsConfigured, evidenceValid, reason);
    }

    private static ProviderTradingEligibility Result(long? serviceId, bool user, bool provider, bool service, bool approval,
        bool area, bool configured, bool evidence, string? reason) =>
        new(serviceId, user, provider, service, approval, area, configured, evidence, reason is null, reason);
}
