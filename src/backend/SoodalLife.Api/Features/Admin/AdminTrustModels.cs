namespace SoodalLife.Api.Features.Admin;

public sealed record AdminTrustListResponse(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminTrustListItemResponse> Items);
public sealed record AdminTrustListItemResponse(Guid ProviderId, string ProviderName, string? MaskedPhone, string? MaskedEmail,
    string? MaskedBusinessRegistrationNo, string AccountStatusCode, string ApprovalStatusCode, string ActivityStatusCode,
    decimal? Score, string GradeLabel, string EvaluationStatusCode, bool IsLegacyUnknownPolicy,
    int CompletedTransactionCount, int AfterServiceCount, int DisputeCount, string EvidenceStatus,
    DateTime? CalculatedAt);

public sealed record AdminTrustDetailResponse(AdminTrustProviderResponse Provider, AdminTrustSummaryResponse Trust,
    AdminTrustPerformanceResponse Performance, IReadOnlyList<AdminTrustDocumentResponse> Documents,
    IReadOnlyList<AdminTrustAfterServiceResponse> AfterServices, IReadOnlyList<AdminTrustDisputeResponse> Disputes,
    IReadOnlyList<AdminTrustEventResponse> Events, AdminTrustReviewStatisticsResponse Reviews, string ReviewNotice,
    TrustCalculationResponse? LatestCalculation);
public sealed record AdminTrustProviderResponse(Guid Id, string ProviderName, string? Phone, string? Email, string? BusinessRegistrationNo,
    string AccountStatusCode, string ApprovalStatusCode, string ActivityStatusCode);
public sealed record AdminTrustSummaryResponse(decimal? Score, string GradeLabel, string EvaluationStatusCode,
    string StatusNotice, string? PolicyVersion, DateTime? CalculatedAt);
public sealed record AdminTrustPerformanceResponse(int SubmittedQuoteCount, int AcceptedQuoteCount, int TransactionCount,
    int CompletedTransactionCount, int CancelledTransactionCount);
public sealed record AdminTrustDocumentResponse(long Id, string DocumentType, string VerificationStatusCode, DateOnly? ExpiresAt,
    bool IsExpired, DateTime? VerifiedAt);
public sealed record AdminTrustAfterServiceResponse(Guid Id, string Subject, string StatusCode, DateTime ReceivedAt,
    DateTime? CompletedAt, string? ResolutionSummary, string? UnresolvedReason, bool? RecurrenceOccurred, bool ConvertedToDispute);
public sealed record AdminTrustDisputeResponse(Guid Id, string Subject, string StatusCode, DateTime ReceivedAt,
    DateTime? ResolvedAt, DateTime? ClosedAt, string ResponsibilityNotice);
public sealed record AdminTrustEventResponse(Guid Id, DateTime OccurredAt, string EventTypeCode, string SourceTypeCode,
    Guid? SourcePublicId, decimal? ScoreBefore, decimal? ScoreDelta, decimal? ScoreAfter, string? GradeBefore,
    string? GradeAfter, string? ReasonText, string? PolicyVersion, DateTime? ProcessedAt);
public sealed record AdminTrustReviewStatisticsResponse(int ReviewCount,int PublicReviewCount,IReadOnlyList<AdminTrustRatingAverageResponse> RatingItemAverages);
public sealed record AdminTrustRatingAverageResponse(Guid ItemId,string ItemName,decimal AverageValue,int RatingCount,decimal MinValue,decimal MaxValue);
