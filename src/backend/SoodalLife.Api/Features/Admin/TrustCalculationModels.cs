using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record TrustPolicyListItem(Guid Id,string PolicyVersion,string PolicyName,string StatusCode,DateTime EffectiveFrom,DateTime? EffectiveTo,int MinimumCompletedTransactions,int MinimumVerifiedReviews,decimal TotalWeight,string? CreatedBy,string? ApprovedBy,DateTime? ApprovedAt,string RowVersion,IReadOnlyList<TrustPolicyComponentResponse> Components);
public sealed record TrustPolicyComponentResponse(string Code,string Name,decimal Weight,string RuleType,string SettingsJson);
public sealed record TrustPolicyRequest([param:Required,StringLength(50)]string PolicyVersion,[param:Required,StringLength(200)]string PolicyName,DateTime EffectiveFrom,DateTime? EffectiveTo,int MinimumCompletedTransactions,int MinimumVerifiedReviews,IReadOnlyList<TrustPolicyComponentRequest> Components,string? RowVersion);
public sealed record TrustPolicyComponentRequest([param:Required]string Code,decimal Weight,[param:Required]string RuleType,Dictionary<string,object?> Settings);
public sealed record TrustPolicyActionRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion);
public sealed record TrustPolicyCloneRequest([param:Required,StringLength(50)]string NewVersion,[param:Required,StringLength(200)]string NewName,DateTime EffectiveFrom,DateTime? EffectiveTo);
public sealed record TrustSimulationRequest(Guid PolicyId,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record TrustRecalculationRequest([param:Required,StringLength(150)]string IdempotencyKey);
public sealed record TrustManualAdjustmentRequest(decimal Score,[param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record TrustManualAdjustmentResponse(Guid ProviderId,decimal? ScoreBefore,decimal ScoreAfter,string GradeLabel,DateTime AdjustedAt,Guid EventId);
public sealed record TrustCalculationResponse(Guid ResultId,Guid ProviderId,Guid PolicyId,string PolicyVersion,string PolicyStatus,string CalculationModeCode,string ResultStatusCode,decimal? Score,string GradeLabel,string EvaluationStatusCode,string? InsufficiencyReason,int CompletedTransactionCount,int VerifiedReviewCount,DateTime CalculatedAt,IReadOnlyList<TrustComponentResultResponse> Components);
public sealed record TrustComponentResultResponse(string Code,string Name,decimal Weight,string RawValueJson,decimal? NormalizedScore,decimal? WeightedScore,int SampleCount,bool IsCalculable,string? UnavailableReason,string SourceSnapshotJson,string EvidenceUrl);
public sealed record TrustReferenceDataResponse(IReadOnlyList<TrustRatingItemResponse> RatingItems,IReadOnlyList<TrustMasterItemResponse> LiabilityTypes,IReadOnlyList<TrustMasterItemResponse> SanctionTypes);
public sealed record TrustRatingItemResponse(Guid Id,string Code,string Name,string? Description,decimal MinValue,decimal MaxValue,bool IsRequired,bool IsActive,int DisplayOrder,string RowVersion);
public sealed record TrustMasterItemResponse(Guid Id,string Code,string Name,string? Description,bool IsActive,int DisplayOrder);
public sealed record CreateTrustRatingItemRequest([param:Required,StringLength(50)]string Code,[param:Required,StringLength(100)]string Name,[param:StringLength(1000)]string? Description,decimal MinValue,decimal MaxValue,bool IsRequired,bool IsActive,int DisplayOrder);
public sealed record UpdateTrustRatingItemRequest([param:Required,StringLength(100)]string Name,[param:StringLength(1000)]string? Description,decimal MinValue,decimal MaxValue,bool IsRequired,bool IsActive,int DisplayOrder,string RowVersion);

public sealed class TrustCalculationException(string businessCode,string message,int statusCode=400):Exception(message){public string BusinessCode{get;}=businessCode;public int StatusCode{get;}=statusCode;}
