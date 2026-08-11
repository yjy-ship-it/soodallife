using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminTrustService(SoodalLifeDbContext dbContext)
{
    public async Task<AdminTrustListResponse> SearchAsync(string? search, string? evaluationStatus, string? grade,
        string? approvalStatus, string? activityStatus, bool? hasExpiredEvidence, bool? hasAfterService,
        bool? hasDispute, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new AdminServiceCategoryException("ADMIN_TRUST_PAGE_INVALID", "페이지와 페이지당 공급자 수를 확인해 주세요.");

        var query = from provider in dbContext.ProviderProfiles.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                    join currentValue in dbContext.ProviderTrustScoreCurrent.AsNoTracking() on provider.Id equals currentValue.ProviderProfileId into currentValues
                    from current in currentValues.DefaultIfEmpty()
                    where dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null &&
                        dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                    select new { Provider = provider, User = user, Current = current };

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var publicId)) query = query.Where(row => row.Provider.PublicId == publicId || row.User.PublicId == publicId);
            else query = query.Where(row => row.Provider.BusinessName.Contains(term) ||
                (row.Provider.BusinessRegistrationNo != null && row.Provider.BusinessRegistrationNo.Contains(term)) ||
                (row.User.Phone != null && row.User.Phone.Contains(term)) || (row.User.Email != null && row.User.Email.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(evaluationStatus)) query = query.Where(row =>
            (row.Current != null ? row.Current.EvaluationStatusCode : row.Provider.TrustScore == null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY") == evaluationStatus.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(approvalStatus)) query = query.Where(row => row.Provider.ApprovalStatusCode == approvalStatus.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(activityStatus)) query = query.Where(row => row.Provider.ActivityStatusCode == activityStatus.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(grade))
        {
            var gradeCode = grade.ToUpperInvariant();
            query = gradeCode switch
            {
                "NEW" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) == null),
                "SPROUT" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) >= 0 && (row.Current != null ? row.Current.Score : row.Provider.TrustScore) < 60),
                "SAFE" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) >= 60 && (row.Current != null ? row.Current.Score : row.Provider.TrustScore) < 70),
                "TRUSTED" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) >= 70 && (row.Current != null ? row.Current.Score : row.Provider.TrustScore) < 80),
                "EXCELLENT" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) >= 80 && (row.Current != null ? row.Current.Score : row.Provider.TrustScore) < 90),
                "HONOR" => query.Where(row => (row.Current != null ? row.Current.Score : row.Provider.TrustScore) >= 90 && (row.Current != null ? row.Current.Score : row.Provider.TrustScore) <= 100),
                _ => query.Where(_ => false)
            };
        }
        if (hasExpiredEvidence.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = hasExpiredEvidence.Value
                ? query.Where(row => dbContext.ProviderDocuments.Any(document => document.ProviderProfileId == row.Provider.Id && document.ExpiresAt < today))
                : query.Where(row => !dbContext.ProviderDocuments.Any(document => document.ProviderProfileId == row.Provider.Id && document.ExpiresAt < today));
        }
        if (hasAfterService.HasValue) query = hasAfterService.Value
            ? query.Where(row => dbContext.AfterServiceCases.Any(item => item.ProviderProfileId == row.Provider.Id))
            : query.Where(row => !dbContext.AfterServiceCases.Any(item => item.ProviderProfileId == row.Provider.Id));
        if (hasDispute.HasValue) query = hasDispute.Value
            ? query.Where(row => dbContext.Transactions.Any(transaction => transaction.ProviderProfileId == row.Provider.Id && dbContext.DisputeCases.Any(item => item.TransactionId == transaction.Id)))
            : query.Where(row => !dbContext.Transactions.Any(transaction => transaction.ProviderProfileId == row.Provider.Id && dbContext.DisputeCases.Any(item => item.TransactionId == transaction.Id)));

        var totalCount = await query.CountAsync(cancellationToken);
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await query.OrderByDescending(row => row.Current != null ? row.Current.Score : row.Provider.TrustScore)
            .ThenBy(row => row.Provider.BusinessName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new
            {
                row.Provider, row.User,
                Score = row.Current != null ? row.Current.Score : row.Provider.TrustScore,
                EvaluationStatus = row.Current != null ? row.Current.EvaluationStatusCode : row.Provider.TrustScore == null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY",
                CalculatedAt = row.Current != null ? row.Current.CalculatedAt : null,
                Completed = dbContext.Transactions.Count(value => value.ProviderProfileId == row.Provider.Id && value.StatusCode == "COMPLETED"),
                AfterServices = dbContext.AfterServiceCases.Count(value => value.ProviderProfileId == row.Provider.Id),
                Disputes = dbContext.Transactions.Where(value => value.ProviderProfileId == row.Provider.Id).SelectMany(value => dbContext.DisputeCases.Where(item => item.TransactionId == value.Id)).Count(),
                Documents = dbContext.ProviderDocuments.Count(value => value.ProviderProfileId == row.Provider.Id),
                VerifiedDocuments = dbContext.ProviderDocuments.Count(value => value.ProviderProfileId == row.Provider.Id && value.VerificationStatusCode == "APPROVED"),
                ExpiredDocuments = dbContext.ProviderDocuments.Count(value => value.ProviderProfileId == row.Provider.Id && value.ExpiresAt < currentDate)
            }).ToListAsync(cancellationToken);

        return new(totalCount, page, pageSize, rows.Select(row => new AdminTrustListItemResponse(
            row.Provider.PublicId, row.Provider.BusinessName, MaskPhone(row.User.Phone), MaskEmail(row.User.Email),
            MaskBusinessNo(row.Provider.BusinessRegistrationNo), row.User.StatusCode, row.Provider.ApprovalStatusCode,
            row.Provider.ActivityStatusCode, row.Score, GradeLabel(row.Score), row.EvaluationStatus,
            row.EvaluationStatus == "LEGACY_UNKNOWN_POLICY", row.Completed, row.AfterServices, row.Disputes,
            EvidenceStatus(row.Documents, row.VerifiedDocuments, row.ExpiredDocuments), row.CalculatedAt)).ToArray());
    }

    public async Task<AdminTrustDetailResponse?> GetAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var identity = await (from provider in dbContext.ProviderProfiles.AsNoTracking()
                              join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                              where provider.PublicId == providerId && dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null &&
                                  dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                              select new { Provider = provider, User = user }).SingleOrDefaultAsync(cancellationToken);
        if (identity is null) return null;

        var current = await dbContext.ProviderTrustScoreCurrent.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderProfileId == identity.Provider.Id, cancellationToken);
        var score = current?.Score ?? identity.Provider.TrustScore;
        var evaluationStatus = current?.EvaluationStatusCode ?? (identity.Provider.TrustScore is null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY");
        var policyVersion = current?.TrustPolicyId is long policyId
            ? await dbContext.TrustPolicies.AsNoTracking().Where(value => value.Id == policyId).Select(value => value.PolicyVersion).SingleOrDefaultAsync(cancellationToken) : null;

        var submittedQuoteCount = await dbContext.Quotes.CountAsync(value => value.ProviderProfileId == identity.Provider.Id && value.SubmittedAt != null, cancellationToken);
        var acceptedQuoteCount = await dbContext.Quotes.CountAsync(value => value.ProviderProfileId == identity.Provider.Id && value.StatusCode == "ACCEPTED", cancellationToken);
        var transactionCount = await dbContext.Transactions.CountAsync(value => value.ProviderProfileId == identity.Provider.Id, cancellationToken);
        var completedTransactionCount = await dbContext.Transactions.CountAsync(value => value.ProviderProfileId == identity.Provider.Id && value.StatusCode == "COMPLETED", cancellationToken);
        var cancelledTransactionCount = await dbContext.Transactions.CountAsync(value => value.ProviderProfileId == identity.Provider.Id && value.StatusCode == "CANCELLED", cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var documents = await dbContext.ProviderDocuments.AsNoTracking().Where(value => value.ProviderProfileId == identity.Provider.Id)
            .OrderByDescending(value => value.CreatedAt).Select(value => new AdminTrustDocumentResponse(value.Id, value.DocumentTypeCode,
                value.VerificationStatusCode, value.ExpiresAt, value.ExpiresAt < today, value.VerifiedAt)).ToListAsync(cancellationToken);
        var afterServices = await dbContext.AfterServiceCases.AsNoTracking().Where(value => value.ProviderProfileId == identity.Provider.Id)
            .OrderByDescending(value => value.ReceivedAt).Select(value => new AdminTrustAfterServiceResponse(value.PublicId, value.Subject,
                value.StatusCode, value.ReceivedAt, value.CompletedAt, value.ResolutionSummary, value.UnresolvedReason,
                value.RecurrenceOccurred, value.ConvertedToDisputeAt != null)).ToListAsync(cancellationToken);
        var transactionIds = await dbContext.Transactions.AsNoTracking().Where(value => value.ProviderProfileId == identity.Provider.Id).Select(value => value.Id).ToArrayAsync(cancellationToken);
        var disputes = await dbContext.DisputeCases.AsNoTracking().Where(value => value.TransactionId.HasValue && transactionIds.Contains(value.TransactionId.Value))
            .OrderByDescending(value => value.ReceivedAt).Select(value => new AdminTrustDisputeResponse(value.PublicId, value.Subject,
                value.StatusCode, value.ReceivedAt, value.ResolvedAt, value.ClosedAt,
                "현재 책임판정 구조가 없어 분쟁 발생을 공급자 귀책으로 해석하지 않습니다.")).ToListAsync(cancellationToken);
        var events = await (from item in dbContext.TrustScoreEvents.AsNoTracking()
                            join policyValue in dbContext.TrustPolicies.AsNoTracking() on item.TrustPolicyId equals policyValue.Id into policies
                            from policy in policies.DefaultIfEmpty()
                            where item.ProviderProfileId == identity.Provider.Id
                            orderby item.OccurredAt descending, item.Id descending
                            select new AdminTrustEventResponse(item.PublicId, item.OccurredAt, item.EventTypeCode, item.SourceTypeCode,
                                item.SourcePublicId, item.ScoreBefore, item.ScoreDelta, item.ScoreAfter, item.GradeBefore,
                                item.GradeAfter, item.ReasonText, policy != null ? policy.PolicyVersion : null, item.ProcessedAt)).ToListAsync(cancellationToken);
        var reviewCount=await dbContext.Reviews.CountAsync(value=>value.ProviderProfileId==identity.Provider.Id,cancellationToken);
        var publicReviewCount=await dbContext.Reviews.CountAsync(value=>value.ProviderProfileId==identity.Provider.Id&&value.VisibilityStatusCode=="PUBLIC"&&value.VerificationStatusCode=="VERIFIED_TRANSACTION",cancellationToken);
        var ratingAverages=await(from rating in dbContext.ReviewRatings.AsNoTracking() join review in dbContext.Reviews.AsNoTracking() on rating.ReviewId equals review.Id join item in dbContext.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals item.Id where review.ProviderProfileId==identity.Provider.Id&&review.VisibilityStatusCode=="PUBLIC"&&review.VerificationStatusCode=="VERIFIED_TRANSACTION" group rating by new{item.PublicId,item.Name,item.MinValue,item.MaxValue,item.DisplayOrder} into values orderby values.Key.DisplayOrder select new AdminTrustRatingAverageResponse(values.Key.PublicId,values.Key.Name,values.Average(x=>x.RatingValue),values.Count(),values.Key.MinValue,values.Key.MaxValue)).ToListAsync(cancellationToken);

        var notice = evaluationStatus switch
        {
            "NEW_OR_EVALUATING" => "신규·평가중",
            "LEGACY_UNKNOWN_POLICY" => "기존 점수 / 산정정책 확인필요",
            _ => policyVersion is null ? "산정정책 확인필요" : "적용 정책에 따라 산정된 점수입니다."
        };
        TrustCalculationResponse? latest=null;var latestRow=await dbContext.ProviderTrustCalculationResults.AsNoTracking().Where(x=>x.ProviderProfileId==identity.Provider.Id).OrderByDescending(x=>x.CalculatedAt).FirstOrDefaultAsync(cancellationToken);if(latestRow is not null){var policy=await dbContext.TrustPolicies.AsNoTracking().SingleAsync(x=>x.Id==latestRow.TrustPolicyId,cancellationToken);var parts=await dbContext.ProviderTrustScoreComponents.AsNoTracking().Where(x=>x.CalculationResultId==latestRow.Id).OrderBy(x=>x.Id).ToListAsync(cancellationToken);latest=new(latestRow.PublicId,identity.Provider.PublicId,policy.PublicId,policy.PolicyVersion,policy.StatusCode,latestRow.CalculationModeCode,latestRow.ResultStatusCode,latestRow.Score,GradeLabel(latestRow.Score),latestRow.EvaluationStatusCode,latestRow.InsufficiencyReason,latestRow.CompletedTransactionCount,latestRow.VerifiedReviewCount,latestRow.CalculatedAt,parts.Select(x=>new TrustComponentResultResponse(x.ComponentCode,ComponentName(x.ComponentCode),x.Weight,x.RawValueJson,x.NormalizedScore,x.WeightedScore,x.SampleCount,x.IsCalculable,x.UnavailableReason,x.SourceSnapshotJson,EvidenceLink(x.ComponentCode,identity.Provider.PublicId))).ToArray());}
        return new(
            new(identity.Provider.PublicId, identity.Provider.BusinessName, AdminPrivacy.Phone(identity.User.Phone), AdminPrivacy.Email(identity.User.Email),
                AdminPrivacy.BusinessNumber(identity.Provider.BusinessRegistrationNo), identity.User.StatusCode, identity.Provider.ApprovalStatusCode, identity.Provider.ActivityStatusCode),
            new(score, GradeLabel(score), evaluationStatus, notice, policyVersion, current?.CalculatedAt),
            new(submittedQuoteCount, acceptedQuoteCount, transactionCount, completedTransactionCount, cancelledTransactionCount),
            documents, afterServices, disputes, events, new(reviewCount,publicReviewCount,ratingAverages),
            reviewCount==0?"등록된 고객 리뷰가 없습니다.":"리뷰 통계는 신뢰도 점수와 별개의 읽기 전용 근거정보입니다.",latest);
    }

    public static string GradeLabel(decimal? score) => score switch
    {
        null => "신규·평가중",
        < 60m => "새싹수달",
        < 70m => "안심수달",
        < 80m => "믿음수달",
        < 90m => "우수수달",
        <= 100m => "명예수달",
        _ => "점수 확인필요"
    };

    private static string EvidenceStatus(int total, int verified, int expired) => expired > 0 ? $"만료 {expired}건" : total == 0 ? "제출 증빙 없음" : $"검증 {verified}/{total}건";
    private static string? MaskPhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length >= 7 ? $"{value[..3]}-****-{value[^4..]}" : "***";
    private static string? MaskEmail(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var at = value.IndexOf('@'); return at <= 0 ? "***" : $"{value[0]}***{value[at..]}"; }
    private static string? MaskBusinessNo(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length >= 5 ? $"{value[..3]}-**-*****" : "***";
    private static string ComponentName(string code)=>code switch{"EVIDENCE"=>"인증·증빙","TRANSACTION"=>"거래이행","REVIEW"=>"고객평가","AFTER_SERVICE"=>"A/S","DISPUTE"=>"분쟁","SANCTION"=>"제재",_=>code};
    private static string EvidenceLink(string code,Guid providerId)=>code switch{"EVIDENCE"=>$"/admin/providers/{providerId}","TRANSACTION"=>"/admin/transactions","REVIEW"=>"/admin/reviews","AFTER_SERVICE" or "DISPUTE"=>"/admin/disputes","SANCTION"=>"/admin/sanctions",_=>"/admin/trust"};
}
