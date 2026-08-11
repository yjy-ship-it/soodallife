using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Matching;

public sealed class RequestMatchingService(SoodalLifeDbContext dbContext, ProviderTradingEligibilityService eligibilityService)
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
    {
        var now = DateTime.UtcNow;
        if (request.StatusCode != "OPEN" || request.ExpiresAt is null || request.ExpiresAt <= now)
        {
            throw new MatchingException("REQUEST_NOT_OPEN", "공개 중이며 마감 전인 요청만 매칭할 수 있습니다.");
        }
        var administrativeAreaId = request.AdministrativeAreaId
            ?? throw new MatchingException("REQUEST_AREA_REQUIRED", "서비스 지역이 없는 요청은 매칭할 수 없습니다.");

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null)
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var providers = await dbContext.ProviderProfiles.ToListAsync(cancellationToken);
            var providerIds = providers.Select(provider => provider.Id).ToArray();
            var candidates = await dbContext.DispatchCandidates
                .Where(item => item.ServiceRequestId == request.Id)
                .ToListAsync(cancellationToken);
            var dispatches = await dbContext.RequestDispatches
                .Where(item => item.ServiceRequestId == request.Id)
                .ToListAsync(cancellationToken);

            var eligibleCount = 0;
            foreach (var provider in providers)
            {
                var evaluation = await eligibilityService.EvaluateAsync(provider.Id, request.CategoryId, administrativeAreaId, cancellationToken);
                var categoryMatch = evaluation.ServiceRegistered;
                var areaMatch = evaluation.AreaMatched;
                var approvalMatch = evaluation.UserAndRoleActive && evaluation.ProviderApprovedAndActive && evaluation.ServiceApproved;
                var eligible = evaluation.IsEligible;
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
                candidate.ReasonCode = evaluation.ReasonCode;
                candidate.StatusCode = eligible ? "ELIGIBLE" : "INELIGIBLE";
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var newDispatchCount = 0;
            foreach (var candidate in candidates.Where(item => item.StatusCode == "ELIGIBLE"))
            {
                var dispatch = dispatches.SingleOrDefault(item => item.ProviderProfileId == candidate.ProviderProfileId);
                if (dispatch is null)
                {
                    dispatch = new RequestDispatch
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
                }

                candidate.StatusCode = "DISPATCHED";
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var dispatch in dispatches.Where(item => item.AvailableAt == now))
            {
                var recipientUserId = providers.Single(provider => provider.Id == dispatch.ProviderProfileId).UserId;
                var outboxKey = $"notification-outbox:dispatch:{dispatch.PublicId:N}";
                if (await dbContext.OutboxEvents.AnyAsync(item => item.IdempotencyKey == outboxKey, cancellationToken))
                {
                    continue;
                }
                dbContext.OutboxEvents.Add(new OutboxEvent
                {
                    AggregateType = "RequestDispatch",
                    AggregatePublicId = dispatch.PublicId,
                    EventType = "REQUEST_DISPATCHED",
                    PayloadJson = JsonSerializer.Serialize(new { recipientUserId, request_no = request.PublicId.ToString("N")[..10].ToUpperInvariant(), source_no = dispatch.PublicId.ToString("N")[..10].ToUpperInvariant() }),
                    StatusCode = "PENDING",
                    OccurredAt = now,
                    AvailableAt = now,
                    IdempotencyKey = outboxKey,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
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

    public async Task<IReadOnlyList<ProviderMatchedRequestListItem>> GetInboxAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var providerId = await GetProviderIdAsync(principal, cancellationToken);
        var rows = await (
                from dispatch in dbContext.RequestDispatches.AsNoTracking()
                join request in dbContext.ServiceRequests.AsNoTracking() on dispatch.ServiceRequestId equals request.Id
                join service in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                join area in dbContext.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                where dispatch.ProviderProfileId == providerId && dispatch.StatusCode != "EXPIRED"
                orderby dispatch.AvailableAt descending
                select new
                {
                    Request = request,
                    Dispatch = dispatch,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                    area.AreaName,
                })
            .ToListAsync(cancellationToken);
        var desiredDates = await GetDesiredDates(rows.Select(row => row.Request.Id).ToArray(), cancellationToken);

        return rows.Select(row => new ProviderMatchedRequestListItem(
            row.Request.PublicId,
            row.Path,
            row.AreaName,
            row.Request.Title,
            desiredDates.GetValueOrDefault(row.Request.Id),
            row.Dispatch.AvailableAt,
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
        var providerId = await GetProviderIdAsync(principal, cancellationToken);
        var row = await (
                from dispatch in dbContext.RequestDispatches
                join request in dbContext.ServiceRequests on dispatch.ServiceRequestId equals request.Id
                join service in dbContext.ServiceCategories on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories on middle.ParentId equals major.Id
                join area in dbContext.AdministrativeAreas on request.AdministrativeAreaId equals area.Id
                where dispatch.ProviderProfileId == providerId && request.PublicId == requestPublicId && dispatch.StatusCode != "EXPIRED"
                select new
                {
                    Request = request,
                    Dispatch = dispatch,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                    area.AreaName,
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

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
        var desiredAt = answers.FirstOrDefault(item => item.Field.FieldKey == "desired_date")?.Answer.ValueDateTime;
        var files = await (from link in dbContext.ServiceRequestFiles.AsNoTracking()
                           join file in dbContext.Files.AsNoTracking() on link.FileId equals file.Id
                           join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on link.FieldDefinitionId equals field.Id into fieldGroup
                           from field in fieldGroup.DefaultIfEmpty()
                           where link.ServiceRequestId == row.Request.Id && file.StatusCode == "ACTIVE" &&
                                 (field == null || field.ProviderVisibilityCode == "FULL" && field.PreAcceptMaskingCode == "NONE")
                           orderby link.DisplayOrder
                           select new ProviderMatchedRequestFile(
                               file.PublicId,
                               file.OriginalFileName,
                               file.ContentType,
                               file.SizeBytes,
                               file.ScanResultText == "NOT_INTEGRATED" ? "NOT_INTEGRATED" : "UNKNOWN",
                               $"/api/v1/requests/{row.Request.PublicId}/files/{file.PublicId}"))
            .ToListAsync(cancellationToken);

        return new ProviderMatchedRequestDetail(
            row.Request.PublicId,
            row.Path,
            row.AreaName,
            row.Request.Title,
            row.Request.Description,
            row.Request.IsUrgent,
            desiredAt,
            row.Dispatch.AvailableAt,
            row.Dispatch.ExpiresAt,
            row.Dispatch.StatusCode,
            row.Request.StatusCode,
            null,
            null,
            responseAnswers,
            files);
    }

    private async Task<long> GetProviderIdAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
            throw new InvalidOperationException("Authenticated user identifier is invalid.");
        return await (
                from user in dbContext.Users.AsNoTracking()
                join provider in dbContext.ProviderProfiles.AsNoTracking() on user.Id equals provider.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select provider.Id)
            .SingleAsync(cancellationToken);
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
