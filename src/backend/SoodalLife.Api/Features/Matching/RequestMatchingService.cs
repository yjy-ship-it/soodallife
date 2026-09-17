using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Emergency;
using SoodalLife.Api.Features.RelationshipBlocks;
using SoodalLife.Api.Features.Providers;

namespace SoodalLife.Api.Features.Matching;

public sealed class RequestMatchingService(
    SoodalLifeDbContext dbContext,
    ProviderTradingEligibilityService eligibilityService,
    ServiceRequestFilePrivacyResolver filePrivacyResolver,
    IEmergencyAvailabilityResolver emergencyAvailabilityResolver,
    IUserRelationshipBlockPolicy relationshipBlocks,
    ProviderWorkInboxNotifier workInboxNotifier)
{
    public async Task<MatchAndDispatchResult> MatchAndDispatchAsync(Guid requestPublicId, CancellationToken cancellationToken)
    {
        var request = await dbContext.ServiceRequests.SingleOrDefaultAsync(
            item => item.PublicId == requestPublicId,
            cancellationToken);
        if (request is null)
        {
            throw new MatchingException("REQUEST_NOT_FOUND", "서비스 요청을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        }

        return await MatchAndDispatchAsync(request, cancellationToken);
    }

    public async Task<MatchAndDispatchResult> MatchAndDispatchAsync(ServiceRequest request, CancellationToken cancellationToken)
        => await MatchAndDispatchCoreAsync(request, false, cancellationToken);

    private async Task<MatchAndDispatchResult> MatchAndDispatchCoreAsync(ServiceRequest request, bool expandWave, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (request.StatusCode != "OPEN" || request.ExpiresAt is null || request.ExpiresAt <= now)
        {
            throw new MatchingException("REQUEST_NOT_OPEN", "공개 중이며 마감 전인 요청만 매칭할 수 있습니다.");
        }
        var coverageTypeCode = await dbContext.CategoryOperationPolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == request.CategoryId && policy.IsActive && policy.EffectiveFrom <= DateOnly.FromDateTime(now) &&
                             (policy.EffectiveTo == null || policy.EffectiveTo > DateOnly.FromDateTime(now)))
            .OrderByDescending(policy => policy.EffectiveFrom).ThenByDescending(policy => policy.Id)
            .Select(policy => policy.CoverageTypeCode)
            .FirstOrDefaultAsync(cancellationToken) ?? ProviderCoveragePolicy.LocalOnly;
        var administrativeAreaId = request.AdministrativeAreaId;
        if (!administrativeAreaId.HasValue && coverageTypeCode != ProviderCoveragePolicy.NationwideRemote)
            throw new MatchingException("REQUEST_AREA_REQUIRED", "서비스 지역이 없는 요청은 매칭할 수 없습니다.");
        var maxQuoteCount = await dbContext.CategoryPolicies.AsNoTracking()
            .Where(item => item.Id == request.CategoryPolicyId).Select(item => item.MaxQuoteCount).SingleAsync(cancellationToken);
        var currentQuoteCount = await dbContext.Quotes.AsNoTracking().CountAsync(item => item.ServiceRequestId == request.Id &&
            (item.StatusCode == "SUBMITTED" || item.StatusCode == "ACCEPTED"), cancellationToken);
        if (currentQuoteCount >= maxQuoteCount)
            throw new MatchingException("MAX_QUOTES_REACHED", "견적 접수가 이미 마감되었습니다.");

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null)
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var exactAreaProviderIds = !administrativeAreaId.HasValue ? [] : await (
                    from service in dbContext.ProviderServiceCategories.AsNoTracking()
                    join area in dbContext.ProviderServiceAreas.AsNoTracking() on service.Id equals area.ProviderServiceCategoryId
                    where service.CategoryId == request.CategoryId && service.StatusCode == "ACTIVE" &&
                          area.AdministrativeAreaId == administrativeAreaId.Value && area.StatusCode == "ACTIVE"
                    select service.ProviderProfileId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            var nationwideProviderIds = request.IsUrgent
                ? []
                : await dbContext.ProviderServiceCategories.AsNoTracking()
                    .Where(item => item.CategoryId == request.CategoryId && item.StatusCode == "ACTIVE" && item.IsNationwide)
                    .Select(item => item.ProviderProfileId)
                    .Distinct()
                    .ToArrayAsync(cancellationToken);
            var previousProviderIds = await dbContext.DispatchCandidates.AsNoTracking()
                .Where(item => item.ServiceRequestId == request.Id)
                .Select(item => item.ProviderProfileId)
                .Union(dbContext.RequestDispatches.AsNoTracking()
                    .Where(item => item.ServiceRequestId == request.Id)
                    .Select(item => item.ProviderProfileId))
                .ToArrayAsync(cancellationToken);
            var providerIds = exactAreaProviderIds.Concat(nationwideProviderIds).Concat(previousProviderIds).Distinct().ToArray();
            var providers = await dbContext.ProviderProfiles.Where(item => providerIds.Contains(item.Id)).ToListAsync(cancellationToken);
            var requestCustomerUserId = await dbContext.CustomerProfiles.AsNoTracking()
                .Where(item => item.Id == request.CustomerProfileId)
                .Select(item => item.UserId)
                .SingleAsync(cancellationToken);
            var candidates = await dbContext.DispatchCandidates
                .Where(item => item.ServiceRequestId == request.Id)
                .ToListAsync(cancellationToken);
            var dispatches = await dbContext.RequestDispatches
                .Where(item => item.ServiceRequestId == request.Id)
                .ToListAsync(cancellationToken);
            var quotedProviderIds = await dbContext.Quotes.AsNoTracking()
                .Where(item => item.ServiceRequestId == request.Id)
                .Select(item => item.ProviderProfileId)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            var eligibleCount = 0;
            foreach (var provider in providers)
            {
                var selfRequest = provider.UserId == requestCustomerUserId;
                var evaluation = await eligibilityService.EvaluateAsync(provider.Id, request.CategoryId, administrativeAreaId, cancellationToken, !request.IsUrgent);
                var categoryMatch = evaluation.ServiceRegistered;
                var areaMatch = evaluation.AreaMatched;
                var approvalMatch = evaluation.UserAndRoleActive && evaluation.ProviderApprovedAndActive && evaluation.ServiceApproved;
                var emergency = request.IsUrgent
                    ? await emergencyAvailabilityResolver.EvaluateAsync(provider.Id, request.CategoryId, administrativeAreaId!.Value, now, cancellationToken)
                    : new EmergencyAvailabilityDecision(true, "NOT_EMERGENCY", evaluation.ProviderServiceCategoryId);
                var relationshipAllowed = !selfRequest &&
                    !await relationshipBlocks.IsBlockedAsync(request.CustomerProfileId, provider.Id, cancellationToken);
                var eligible = request.IsUrgent
                    ? evaluation.IsEligible && emergency.IsAvailable && relationshipAllowed
                    : evaluation.IsEligible && relationshipAllowed;
                if (eligible) eligibleCount++;

                var candidate = candidates.SingleOrDefault(item => item.ProviderProfileId == provider.Id);
                if (candidate is null)
                {
                    candidate = new DispatchCandidate
                    {
                        ServiceRequestId = request.Id,
                        ProviderProfileId = provider.Id,
                        CreatedAt = now,
                    };
                    dbContext.DispatchCandidates.Add(candidate);
                    candidates.Add(candidate);
                }

                candidate.CategoryMatch = categoryMatch;
                candidate.AreaMatch = areaMatch;
                candidate.ApprovalMatch = approvalMatch;
                candidate.EvaluatedAt = now;
                candidate.ExpiresAt = request.ExpiresAt;
                candidate.ReasonCode = selfRequest ? "SELF_REQUEST_NOT_ALLOWED" : evaluation.ReasonCode ??
                    (emergency.IsAvailable ? (relationshipAllowed ? null : "USER_RELATIONSHIP_BLOCKED") : emergency.ReasonCode);
                candidate.StatusCode = eligible ? "ELIGIBLE" : "INELIGIBLE";
                var existingDispatch = dispatches.SingleOrDefault(item => item.ProviderProfileId == provider.Id);
                if (!eligible && existingDispatch is not null &&
                    existingDispatch.StatusCode is "AVAILABLE" or "VIEWED" &&
                    !quotedProviderIds.Contains(provider.Id))
                {
                    existingDispatch.StatusCode = "EXPIRED";
                    existingDispatch.ExpiresAt = now;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var newDispatchCount = 0;
            var newDispatchProviderIds = new List<long>();
            var activeDispatchCount = dispatches.Count(item => item.StatusCode != "EXPIRED");
            var currentWaveCapacity = Math.Max(ProviderDispatchPolicy.WaveSize,
                ((activeDispatchCount + ProviderDispatchPolicy.WaveSize - 1) / ProviderDispatchPolicy.WaveSize) * ProviderDispatchPolicy.WaveSize);
            var targetCapacity = expandWave && activeDispatchCount >= currentWaveCapacity
                ? currentWaveCapacity + ProviderDispatchPolicy.WaveSize
                : currentWaveCapacity;
            var availableSlots = Math.Max(0, targetCapacity - activeDispatchCount);
            var exactAreaSet = exactAreaProviderIds.ToHashSet();
            var providerById = providers.ToDictionary(item => item.Id);
            var selectedCandidates = candidates
                .Where(item => item.StatusCode == "ELIGIBLE" &&
                               dispatches.All(dispatch => dispatch.ProviderProfileId != item.ProviderProfileId))
                .OrderByDescending(item => exactAreaSet.Contains(item.ProviderProfileId))
                .ThenByDescending(item => providerById.GetValueOrDefault(item.ProviderProfileId)?.TrustScore ?? 0)
                .ThenBy(item => item.ProviderProfileId)
                .Take(availableSlots)
                .ToArray();
            foreach (var candidate in selectedCandidates)
            {
                var dispatch = new RequestDispatch
                {
                    ServiceRequestId = request.Id,
                    ProviderProfileId = candidate.ProviderProfileId,
                    CandidateId = candidate.Id,
                    StatusCode = "AVAILABLE",
                    AvailableAt = now,
                    ExpiresAt = request.ExpiresAt.Value,
                    IdempotencyKey = $"dispatch:{request.PublicId:N}:{candidate.ProviderProfileId}",
                    CreatedAt = now,
                };
                dbContext.RequestDispatches.Add(dispatch);
                dispatches.Add(dispatch);
                newDispatchCount++;
                newDispatchProviderIds.Add(candidate.ProviderProfileId);

                candidate.StatusCode = "DISPATCHED";
            }
            foreach (var candidate in candidates.Where(item => item.StatusCode == "ELIGIBLE" &&
                         dispatches.Any(dispatch => dispatch.ProviderProfileId == item.ProviderProfileId && dispatch.StatusCode != "EXPIRED")))
                candidate.StatusCode = "DISPATCHED";

            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var dispatch in dispatches.Where(item => item.AvailableAt == now))
            {
                var recipientUserId = providers.Single(provider => provider.Id == dispatch.ProviderProfileId).UserId;
                var outboxKey = $"notification-outbox:dispatch:{dispatch.PublicId:N}";
                if (await dbContext.OutboxEvents.AnyAsync(item => item.IdempotencyKey == outboxKey, cancellationToken))
                {
                    continue;
                }
                if (request.IsUrgent && !await dbContext.NotificationTemplates.AsNoTracking().AnyAsync(
                        x => x.EventTypeCode == "EMERGENCY.PUBLISHED" && x.IsActive, cancellationToken))
                    continue;
                dbContext.OutboxEvents.Add(new OutboxEvent
                {
                    AggregateType = "RequestDispatch",
                    AggregatePublicId = dispatch.PublicId,
                    EventType = request.IsUrgent ? "EMERGENCY.PUBLISHED" : "REQUEST_DISPATCHED",
                    PayloadJson = JsonSerializer.Serialize(new { recipientUserId, request_no = request.PublicId.ToString("N")[..10].ToUpperInvariant(), source_no = dispatch.PublicId.ToString("N")[..10].ToUpperInvariant() }),
                    StatusCode = "PENDING",
                    OccurredAt = now,
                    AvailableAt = now,
                    IdempotencyKey = outboxKey,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            if (newDispatchCount > 0)
            {
                var userPublicIds = await (
                        from provider in dbContext.ProviderProfiles.AsNoTracking()
                        join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                        where newDispatchProviderIds.Contains(provider.Id)
                        select user.PublicId)
                    .Distinct()
                    .ToArrayAsync(CancellationToken.None);
                foreach (var userPublicId in userPublicIds)
                    await workInboxNotifier.NotifyProviderAsync(
                        userPublicId,
                        request.IsUrgent ? "EMERGENCY_REQUEST_DISPATCHED" : "REQUEST_DISPATCHED",
                        CancellationToken.None);
            }
            return new MatchAndDispatchResult(request.PublicId, providers.Count, eligibleCount, dispatches.Count, newDispatchCount);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task<int> ExpandDueDispatchWavesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - ProviderDispatchPolicy.ExpansionDelay;
        var requestIds = await dbContext.ServiceRequests.AsNoTracking()
            .Where(request => request.StatusCode == "OPEN" && request.ExpiresAt != null && request.ExpiresAt > now &&
                              dbContext.Quotes.Count(quote => quote.ServiceRequestId == request.Id &&
                                  (quote.StatusCode == "SUBMITTED" || quote.StatusCode == "ACCEPTED")) <
                              dbContext.CategoryPolicies.Where(policy => policy.Id == request.CategoryPolicyId)
                                  .Select(policy => policy.MaxQuoteCount).Single() &&
                              (!dbContext.RequestDispatches.Any(dispatch => dispatch.ServiceRequestId == request.Id) ||
                               (dbContext.RequestDispatches.Where(dispatch => dispatch.ServiceRequestId == request.Id)
                                    .Max(dispatch => dispatch.AvailableAt) <= cutoff &&
                                dbContext.DispatchCandidates.Any(candidate => candidate.ServiceRequestId == request.Id &&
                                    candidate.StatusCode == "ELIGIBLE" &&
                                    !dbContext.RequestDispatches.Any(dispatch => dispatch.ServiceRequestId == request.Id &&
                                        dispatch.ProviderProfileId == candidate.ProviderProfileId)))))
            .OrderBy(request => request.CreatedAt)
            .Select(request => request.Id)
            .Take(ProviderDispatchPolicy.MaximumRequestsPerCycle)
            .ToArrayAsync(cancellationToken);
        var expanded = 0;
        foreach (var requestId in requestIds)
        {
            var request = await dbContext.ServiceRequests.SingleAsync(item => item.Id == requestId, cancellationToken);
            try
            {
                var result = await MatchAndDispatchCoreAsync(request, true, cancellationToken);
                expanded += result.NewDispatchCount;
            }
            catch (MatchingException exception) when (exception.BusinessCode is "MAX_QUOTES_REACHED" or "REQUEST_NOT_OPEN")
            {
                // Another request/quote transaction closed the request while the expansion cycle was running.
            }
        }
        return expanded;
    }

    public async Task<ProviderRematchResult> RefreshProviderMatchesAsync(
        long providerProfileId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var provider = await dbContext.ProviderProfiles
            .SingleOrDefaultAsync(item => item.Id == providerProfileId, cancellationToken)
            ?? throw new MatchingException("PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var userPublicId = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == provider.UserId)
            .Select(item => item.PublicId)
            .SingleAsync(cancellationToken);

        var currentServices = await dbContext.ProviderServiceCategories.AsNoTracking()
            .Where(item => item.ProviderProfileId == providerProfileId && item.StatusCode == "ACTIVE")
            .Select(item => new { item.CategoryId, item.IsNationwide }).ToListAsync(cancellationToken);
        var currentScopes = await (
                from service in dbContext.ProviderServiceCategories.AsNoTracking()
                join area in dbContext.ProviderServiceAreas.AsNoTracking() on service.Id equals area.ProviderServiceCategoryId
                where service.ProviderProfileId == providerProfileId &&
                      service.StatusCode == "ACTIVE" && area.StatusCode == "ACTIVE"
                select new { service.CategoryId, area.AdministrativeAreaId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var currentCategoryIds = currentServices.Select(item => item.CategoryId).Distinct().ToArray();
        var nationwideCategoryIds = currentServices.Where(item => item.IsNationwide).Select(item => item.CategoryId).ToArray();
        var currentAreaIds = currentScopes.Select(item => item.AdministrativeAreaId).Distinct().ToArray();
        var previousRequestIds = await dbContext.DispatchCandidates.AsNoTracking()
            .Where(item => item.ProviderProfileId == providerProfileId)
            .Select(item => item.ServiceRequestId)
            .Union(dbContext.RequestDispatches.AsNoTracking()
                .Where(item => item.ProviderProfileId == providerProfileId)
                .Select(item => item.ServiceRequestId))
            .ToArrayAsync(cancellationToken);

        var requests = await dbContext.ServiceRequests
            .Where(item => item.StatusCode == "OPEN" && item.ExpiresAt != null && item.ExpiresAt > now &&
                           (previousRequestIds.Contains(item.Id) ||
                            (currentCategoryIds.Contains(item.CategoryId) &&
                             (nationwideCategoryIds.Contains(item.CategoryId) ||
                              (item.AdministrativeAreaId != null && currentAreaIds.Contains(item.AdministrativeAreaId.Value))))))
            .ToListAsync(cancellationToken);
        var requestCustomerProfileIds = requests.Select(request => request.CustomerProfileId).Distinct().ToArray();
        var customerUserIds = await dbContext.CustomerProfiles.AsNoTracking()
            .Where(item => requestCustomerProfileIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.UserId, cancellationToken);
        var requestIds = requests.Select(item => item.Id).ToArray();
        var candidates = await dbContext.DispatchCandidates
            .Where(item => item.ProviderProfileId == providerProfileId && requestIds.Contains(item.ServiceRequestId))
            .ToListAsync(cancellationToken);
        var dispatches = await dbContext.RequestDispatches
            .Where(item => item.ProviderProfileId == providerProfileId && requestIds.Contains(item.ServiceRequestId))
            .ToListAsync(cancellationToken);
        var quotedRequestIds = await dbContext.Quotes.AsNoTracking()
            .Where(item => item.ProviderProfileId == providerProfileId && requestIds.Contains(item.ServiceRequestId))
            .Select(item => item.ServiceRequestId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var added = new List<(RequestDispatch Dispatch, ServiceRequest Request)>();
        var revokedCount = 0;
        foreach (var request in requests)
        {
            if (request.ExpiresAt is not { } requestExpiresAt)
                continue;
            var administrativeAreaId = request.AdministrativeAreaId;
            var selfRequest = customerUserIds.GetValueOrDefault(request.CustomerProfileId) == provider.UserId;
            var maxQuoteCount = await dbContext.CategoryPolicies.AsNoTracking().Where(item => item.Id == request.CategoryPolicyId).Select(item => item.MaxQuoteCount).SingleAsync(cancellationToken);
            var submittedQuoteCount = await dbContext.Quotes.AsNoTracking().CountAsync(item => item.ServiceRequestId == request.Id && (item.StatusCode == "SUBMITTED" || item.StatusCode == "ACCEPTED"), cancellationToken);
            var quoteLimitReached = submittedQuoteCount >= maxQuoteCount;
            var evaluation = await eligibilityService.EvaluateAsync(
                    providerProfileId, request.CategoryId, administrativeAreaId, cancellationToken, !request.IsUrgent);
            var emergency = request.IsUrgent
                ? await emergencyAvailabilityResolver.EvaluateAsync(
                    providerProfileId, request.CategoryId, administrativeAreaId!.Value, now, cancellationToken)
                : new EmergencyAvailabilityDecision(true, "NOT_EMERGENCY", evaluation.ProviderServiceCategoryId);
            var relationshipAllowed = !selfRequest && !await relationshipBlocks.IsBlockedAsync(
                request.CustomerProfileId, providerProfileId, cancellationToken);
            var eligible = !quoteLimitReached && (request.IsUrgent
                ? evaluation.IsEligible && emergency.IsAvailable && relationshipAllowed
                : evaluation.IsEligible && relationshipAllowed);

            var candidate = candidates.SingleOrDefault(item => item.ServiceRequestId == request.Id);
            if (candidate is null)
            {
                candidate = new DispatchCandidate
                {
                    ServiceRequestId = request.Id,
                    ProviderProfileId = providerProfileId,
                    CreatedAt = now,
                };
                dbContext.DispatchCandidates.Add(candidate);
                candidates.Add(candidate);
            }
            candidate.CategoryMatch = evaluation.ServiceRegistered;
            candidate.AreaMatch = evaluation.AreaMatched;
            candidate.ApprovalMatch = evaluation.UserAndRoleActive && evaluation.ProviderApprovedAndActive && evaluation.ServiceApproved;
            candidate.EvaluatedAt = now;
            candidate.ExpiresAt = request.ExpiresAt;
            candidate.ReasonCode = selfRequest ? "SELF_REQUEST_NOT_ALLOWED" : quoteLimitReached ? "MAX_QUOTES_REACHED" : evaluation.ReasonCode ??
                (emergency.IsAvailable ? (relationshipAllowed ? null : "USER_RELATIONSHIP_BLOCKED") : emergency.ReasonCode);
            candidate.StatusCode = eligible ? "ELIGIBLE" : "INELIGIBLE";

            var dispatch = dispatches.SingleOrDefault(item => item.ServiceRequestId == request.Id);
            var activeDispatchCount = await dbContext.RequestDispatches.AsNoTracking().CountAsync(
                item => item.ServiceRequestId == request.Id && item.StatusCode != "EXPIRED", cancellationToken);
            var currentWaveCapacity = Math.Max(ProviderDispatchPolicy.WaveSize,
                ((activeDispatchCount + ProviderDispatchPolicy.WaveSize - 1) / ProviderDispatchPolicy.WaveSize) * ProviderDispatchPolicy.WaveSize);
            var canJoinCurrentWave = activeDispatchCount < currentWaveCapacity;
            if (eligible)
            {
                if (dispatch is null && canJoinCurrentWave)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    dispatch = new RequestDispatch
                    {
                        ServiceRequestId = request.Id,
                        ProviderProfileId = providerProfileId,
                        CandidateId = candidate.Id,
                        StatusCode = "AVAILABLE",
                        AvailableAt = now,
                        ExpiresAt = requestExpiresAt,
                        IdempotencyKey = $"dispatch:{request.PublicId:N}:{providerProfileId}",
                        CreatedAt = now,
                    };
                    dbContext.RequestDispatches.Add(dispatch);
                    dispatches.Add(dispatch);
                    added.Add((dispatch, request));
                }
                else if (dispatch?.StatusCode == "EXPIRED" && canJoinCurrentWave && !quotedRequestIds.Contains(request.Id))
                {
                    dispatch.StatusCode = "AVAILABLE";
                    dispatch.AvailableAt = now;
                    dispatch.ViewedAt = null;
                    dispatch.RespondedAt = null;
                    dispatch.ExpiresAt = requestExpiresAt;
                    added.Add((dispatch, request));
                }
                candidate.StatusCode = dispatch is not null && dispatch.StatusCode != "EXPIRED" ? "DISPATCHED" : "ELIGIBLE";
            }
            else if (dispatch is not null && dispatch.StatusCode is "AVAILABLE" or "VIEWED" &&
                     !quotedRequestIds.Contains(request.Id))
            {
                dispatch.StatusCode = "EXPIRED";
                dispatch.ExpiresAt = now;
                revokedCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var item in added)
        {
            var outboxKey = $"notification-outbox:dispatch:{item.Dispatch.PublicId:N}:{item.Dispatch.AvailableAt.Ticks}";
            if (await dbContext.OutboxEvents.AnyAsync(value => value.IdempotencyKey == outboxKey, cancellationToken))
                continue;
            if (item.Request.IsUrgent && !await dbContext.NotificationTemplates.AsNoTracking().AnyAsync(
                    value => value.EventTypeCode == "EMERGENCY.PUBLISHED" && value.IsActive, cancellationToken))
                continue;
            dbContext.OutboxEvents.Add(new OutboxEvent
            {
                AggregateType = "RequestDispatch",
                AggregatePublicId = item.Dispatch.PublicId,
                EventType = item.Request.IsUrgent ? "EMERGENCY.PUBLISHED" : "REQUEST_DISPATCHED",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    recipientUserId = provider.UserId,
                    request_no = item.Request.PublicId.ToString("N")[..10].ToUpperInvariant(),
                    source_no = item.Dispatch.PublicId.ToString("N")[..10].ToUpperInvariant(),
                }),
                StatusCode = "PENDING",
                OccurredAt = now,
                AvailableAt = now,
                IdempotencyKey = outboxKey,
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        if (added.Count > 0 || revokedCount > 0)
            await workInboxNotifier.NotifyProviderAsync(
                userPublicId, added.Count > 0 ? "PROVIDER_MATCHES_ADDED" : "PROVIDER_MATCHES_REVOKED", CancellationToken.None);
        return new ProviderRematchResult(providerProfileId, requests.Count, added.Count, revokedCount);
    }

    public async Task<IReadOnlyList<ProviderMatchedRequestListItem>> GetInboxAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var providerId = identity.ProviderId;
        var rows = await (
                from dispatch in dbContext.RequestDispatches.AsNoTracking()
                join request in dbContext.ServiceRequests.AsNoTracking() on dispatch.ServiceRequestId equals request.Id
                join service in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                join area0 in dbContext.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals (long?)area0.Id into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                join sido0 in dbContext.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals (long?)sido0.Id into sidoGroup
                from sido in sidoGroup.DefaultIfEmpty()
                where dispatch.ProviderProfileId == providerId &&
                      !dbContext.CustomerProfiles.Any(customer => customer.Id == request.CustomerProfileId && customer.UserId == identity.UserId)
                orderby dispatch.AvailableAt descending
                select new
                {
                    Request = request,
                    Dispatch = dispatch,
                    IsInterior = dbContext.InteriorProjects.Any(project => project.ServiceRequestId == request.Id),
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                    AreaName = area == null ? "전국·온라인" : sido == null ? area.AreaName : sido.AreaName + " " + area.AreaName,
                })
            .Take(200)
            .ToListAsync(cancellationToken);
        var desiredDates = await GetDesiredDates(rows.Select(row => row.Request.Id).ToArray(), cancellationToken);

        return rows.Select(row => new ProviderMatchedRequestListItem(
            row.Request.PublicId,
            RequestDomain(row.Request.IsUrgent, row.IsInterior),
            row.Path,
            row.AreaName,
            row.Request.Title,
            desiredDates.GetValueOrDefault(row.Request.Id),
            row.Dispatch.AvailableAt,
            row.Dispatch.ExpiresAt,
            row.Dispatch.StatusCode,
            row.Request.StatusCode)).ToArray();
    }

    public async Task<IReadOnlyList<DispatchCandidateResponse>?> GetCandidatesAsync(
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ServiceRequests.AsNoTracking()
            .AnyAsync(request => request.PublicId == requestPublicId, cancellationToken);
        if (!exists) return null;

        return await (
                from candidate in dbContext.DispatchCandidates.AsNoTracking()
                join request in dbContext.ServiceRequests.AsNoTracking() on candidate.ServiceRequestId equals request.Id
                join provider in dbContext.ProviderProfiles.AsNoTracking() on candidate.ProviderProfileId equals provider.Id
                where request.PublicId == requestPublicId
                orderby provider.BusinessName
                select new DispatchCandidateResponse(
                    provider.PublicId,
                    provider.BusinessName,
                    candidate.StatusCode,
                    candidate.CategoryMatch,
                    candidate.AreaMatch,
                    candidate.ApprovalMatch,
                    candidate.ReasonCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderMatchedRequestDetail?> GetInboxDetailAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var providerId = identity.ProviderId;
        var row = await (
                from dispatch in dbContext.RequestDispatches
                join request in dbContext.ServiceRequests on dispatch.ServiceRequestId equals request.Id
                join service in dbContext.ServiceCategories on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories on middle.ParentId equals major.Id
                join area0 in dbContext.AdministrativeAreas on request.AdministrativeAreaId equals (long?)area0.Id into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                join sido0 in dbContext.AdministrativeAreas on area.ParentAreaId equals (long?)sido0.Id into sidoGroup
                from sido in sidoGroup.DefaultIfEmpty()
                where dispatch.ProviderProfileId == providerId && request.PublicId == requestPublicId &&
                      !dbContext.CustomerProfiles.Any(customer => customer.Id == request.CustomerProfileId && customer.UserId == identity.UserId)
                select new
                {
                    Request = request,
                    Dispatch = dispatch,
                    IsInterior = dbContext.InteriorProjects.Any(project => project.ServiceRequestId == request.Id),
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                    AreaName = area == null ? "전국·온라인" : sido == null ? area.AreaName : sido.AreaName + " " + area.AreaName,
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var coverageTypeCode = await dbContext.CategoryOperationPolicies.AsNoTracking()
            .Where(policy => policy.CategoryId == row.Request.CategoryId && policy.IsActive &&
                             policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today))
            .OrderByDescending(policy => policy.EffectiveFrom).ThenByDescending(policy => policy.Id)
            .Select(policy => policy.CoverageTypeCode)
            .FirstOrDefaultAsync(cancellationToken) ?? ProviderCoveragePolicy.LocalOnly;
        var requiresServiceAddress = ProviderCoveragePolicy.RequiresServiceAddress(coverageTypeCode);

        if (row.Dispatch.StatusCode == "AVAILABLE")
        {
            row.Dispatch.StatusCode = "VIEWED";
            row.Dispatch.ViewedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var answers = await (
                from answer in dbContext.RequestAnswers.AsNoTracking()
                join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                where answer.ServiceRequestId == row.Request.Id
                orderby field.DisplayOrder, field.Id
                select new { Answer = answer, Field = field })
            .ToListAsync(cancellationToken);
        var responseAnswers = answers.Select(item =>
        {
            var visible = item.Field.ProviderVisibilityCode == "FULL" && item.Field.PreAcceptMaskingCode == "NONE";
            return new ProviderMatchedRequestAnswer(
                item.Field.PublicId,
                item.Field.Label,
                item.Field.FieldTypeCode,
                visible ? ReadAnswerValue(item.Answer) : null,
                !visible);
        }).ToArray();
        var desiredDateAnswers = answers.Where(item => item.Field.FieldTypeCode == "DATETIME" && item.Answer.ValueDateTime.HasValue)
            .OrderBy(item => item.Field.DisplayOrder).ThenBy(item => item.Field.Id).ToArray();
        var desiredAt = answers.FirstOrDefault(item => item.Field.FieldKey == "desired_date")?.Answer.ValueDateTime
            ?? desiredDateAnswers.FirstOrDefault()?.Answer.ValueDateTime;
        var desiredAtSecond = desiredDateAnswers.Where(item => item.Answer.ValueDateTime != desiredAt)
            .Select(item => item.Answer.ValueDateTime).FirstOrDefault();
        var publishedFiles = await filePrivacyResolver.GetPublishedAsync(row.Request.Id, providerId, cancellationToken);
        var files = publishedFiles.Select(file => new ProviderMatchedRequestFile(
            file.PublicId, file.FileName, file.ContentType, file.SizeBytes,
            file.MalwareScanStatus, file.PrivacyInspectionStatus, file.SanitizationStatus,
            file.PublicationMode, $"/api/v1/requests/{row.Request.PublicId}/files/{file.PublicId}"))
            .ToArray();
        var customerLocation = await dbContext.CustomerAddresses.AsNoTracking()
            .Where(address => address.CustomerProfileId == row.Request.CustomerProfileId && address.IsActive &&
                address.AdministrativeAreaId == row.Request.AdministrativeAreaId && address.Latitude.HasValue && address.Longitude.HasValue)
            .OrderByDescending(address => address.IsDefault).ThenByDescending(address => address.UpdatedAt)
            .Select(address => new { address.Latitude, address.Longitude }).FirstOrDefaultAsync(cancellationToken);
        var providerLocation = await (from profile in dbContext.CustomerProfiles.AsNoTracking()
                                      join address in dbContext.CustomerAddresses.AsNoTracking() on profile.Id equals address.CustomerProfileId
                                      where profile.UserId == identity.UserId && address.IsActive && address.Latitude.HasValue && address.Longitude.HasValue
                                      orderby address.IsDefault descending, address.UpdatedAt descending
                                      select new { address.Latitude, address.Longitude }).FirstOrDefaultAsync(cancellationToken);
        decimal? approximateDistanceKm = null;
        var distanceBasis = "UNAVAILABLE";
        if (customerLocation?.Latitude is decimal customerLatitude && customerLocation.Longitude is decimal customerLongitude &&
            providerLocation?.Latitude is decimal providerLatitude && providerLocation.Longitude is decimal providerLongitude)
        {
            approximateDistanceKm = HaversineKilometres(customerLatitude, customerLongitude, providerLatitude, providerLongitude);
            distanceBasis = "SAVED_DEFAULT_ADDRESSES";
        }

        return new ProviderMatchedRequestDetail(
            row.Request.PublicId,
            RequestDomain(row.Request.IsUrgent, row.IsInterior),
            row.Path,
            row.AreaName,
            row.Request.Title,
            row.Request.Description,
            row.Request.IsUrgent,
            desiredAt,
            desiredAtSecond,
            row.Dispatch.AvailableAt,
            row.Dispatch.ExpiresAt,
            row.Dispatch.StatusCode,
            row.Request.StatusCode,
            requiresServiceAddress,
            null,
            row.Request.DetailAddressDisclosureCode == "BEFORE_QUOTE" ? row.Request.DetailAddress : null,
            row.Request.DetailAddressDisclosureCode,
            approximateDistanceKm,
            distanceBasis,
            responseAnswers,
            files);
    }

    private static string RequestDomain(bool isUrgent, bool isInterior) =>
        isUrgent ? "EMERGENCY" : isInterior ? "INTERIOR" : "GENERAL";

    public async Task DeclineInboxAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        DeclineMatchedRequestInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var row = await (from dispatch in dbContext.RequestDispatches
                         join request in dbContext.ServiceRequests on dispatch.ServiceRequestId equals request.Id
                         where dispatch.ProviderProfileId == identity.ProviderId && request.PublicId == requestPublicId &&
                               (dispatch.StatusCode == "AVAILABLE" || dispatch.StatusCode == "VIEWED")
                         select new { Dispatch = dispatch, Request = request }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new MatchingException("REQUEST_NOT_DECLINABLE", "이미 응답했거나 종료된 요청입니다.");
        if (await dbContext.Quotes.AnyAsync(x => x.RequestDispatchId == row.Dispatch.Id, cancellationToken))
            throw new MatchingException("REQUEST_QUOTE_EXISTS", "작성 중인 견적이 있어 요청을 건너뛸 수 없습니다.");

        var reason = input.ReasonCode?.Trim().ToUpperInvariant();
        if (reason is not (null or "" or "DISTANCE" or "SCHEDULE" or "SCOPE" or "OTHER"))
            throw new MatchingException("DECLINE_REASON_INVALID", "건너뛰기 사유를 다시 선택해 주세요.", StatusCodes.Status400BadRequest);
        var now = DateTime.UtcNow;
        row.Dispatch.StatusCode = "DECLINED";
        row.Dispatch.RespondedAt = now;
        var candidate = await dbContext.DispatchCandidates.SingleOrDefaultAsync(x => x.Id == row.Dispatch.CandidateId, cancellationToken);
        if (candidate is not null)
        {
            candidate.StatusCode = "DECLINED";
            candidate.ReasonCode = $"PROVIDER_{(string.IsNullOrWhiteSpace(reason) ? "OTHER" : reason)}";
            candidate.EvaluatedAt = now;
            candidate.ExpiresAt = now;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProviderIdentity> GetProviderIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
            throw new InvalidOperationException("Authenticated user identifier is invalid.");
        return await (
                from user in dbContext.Users.AsNoTracking()
                join provider in dbContext.ProviderProfiles.AsNoTracking() on user.Id equals provider.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new ProviderIdentity(user.Id, provider.Id))
            .SingleAsync(cancellationToken);
    }

    private sealed record ProviderIdentity(long UserId, long ProviderId);

    private static decimal HaversineKilometres(decimal firstLatitude, decimal firstLongitude, decimal secondLatitude, decimal secondLongitude)
    {
        const double earthRadiusKm = 6371.0088;
        static double Radians(decimal degrees) => (double)degrees * Math.PI / 180d;
        var latitudeDelta = Radians(secondLatitude - firstLatitude);
        var longitudeDelta = Radians(secondLongitude - firstLongitude);
        var firstLatitudeRadians = Radians(firstLatitude);
        var secondLatitudeRadians = Radians(secondLatitude);
        var value = Math.Pow(Math.Sin(latitudeDelta / 2d), 2d) +
            Math.Cos(firstLatitudeRadians) * Math.Cos(secondLatitudeRadians) * Math.Pow(Math.Sin(longitudeDelta / 2d), 2d);
        var kilometres = earthRadiusKm * 2d * Math.Atan2(Math.Sqrt(value), Math.Sqrt(1d - value));
        return Math.Round((decimal)kilometres, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<Dictionary<long, DateTime?>> GetDesiredDates(long[] requestIds, CancellationToken cancellationToken) =>
        await (
                from answer in dbContext.RequestAnswers.AsNoTracking()
                join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                where requestIds.Contains(answer.ServiceRequestId) && field.FieldKey == "desired_date"
                select new { answer.ServiceRequestId, answer.ValueDateTime })
            .ToDictionaryAsync(item => item.ServiceRequestId, item => item.ValueDateTime, cancellationToken);

    private static object? ReadAnswerValue(RequestAnswer answer)
    {
        if (answer.ValueText is not null) return answer.ValueText;
        if (answer.ValueNumber is not null) return answer.ValueNumber;
        if (answer.ValueBoolean is not null) return answer.ValueBoolean;
        if (answer.ValueDate is not null) return answer.ValueDate.Value.ToString("yyyy-MM-dd");
        if (answer.ValueDateTime is not null) return answer.ValueDateTime.Value;
        return answer.ValueJson is null ? null : JsonSerializer.Deserialize<JsonElement>(answer.ValueJson);
    }
}
