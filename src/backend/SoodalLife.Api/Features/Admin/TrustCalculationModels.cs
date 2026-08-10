using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record TrustPolicyListItem(Guid Id,string PolicyVersion,string PolicyName,string StatusCode,DateTime EffectiveFrom,DateTime? EffectiveTo,int MinimumCompletedTransactions,int MinimumVerifiedReviews,decimal TotalWeight,string? CreatedBy,string? ApprovedBy,DateTime? ApprovedAt,string RowVersion,IReadOnlyList<TrustPolicyComponentResponse> Components);
public sealed record TrustPolicyComponentResponse(string Code,string Name,decimal Weight,string RuleType,string SettingsJson);
public sealed record TrustPolicyRequest([param:Required,StringLength(50)]string PolicyVersion,[param:Required,StringLength(200)]string PolicyName,DateTime EffectiveFrom,DateTime? EffectiveTo,int MinimumCompletedTransactions,int MinimumVerifiedReviews,IReadOnlyList<TrustPolicyComponentRequest> Components,string? RowVersion);
public sealed record TrustPolicyComponentRequest([param:Required]string Code,decimal Weight,[param:Required]string RuleType,Dictionary<string,object?> Settings);
public sealed record TrustPolicyActionRequest([param:Required,StringLength(1000)]string Reason,[param:Required,StringLength(150)]string IdempotencyKey,string RowVersion);
public sealed record TrustPolicyCloneRequest([param:Required,StringLength(50)]string NewVersion,[param:Required,StringLength(200)]string NewName,DateTime EffectiveFrom,DateTime? EffectiveTo);
public sealed record TrustSimulationRequest(Guid PolicyId,[param:Required,StringLength(150)]string IdempotencyKey);
public sealed record TrustCalculationResponse(Guid ResultId,Guid ProviderId,Guid PolicyId,string PolicyVersion,string PolicyStatus,string CalculationModeCode,string ResultStatusCode,decimal? Score,string GradeLabel,string EvaluationStatusCode,string? InsufficiencyReason,int CompletedTransactionCount,int VerifiedReviewCount,DateTime CalculatedAt,IReadOnlyList<TrustComponentResultResponse> Components);
public sealed record TrustComponentResultResponse(string Code,string Name,decimal Weight,string RawValueJson,decimal? NormalizedScore,decimal? WeightedScore,int SampleCount,bool IsCalculable,string? UnavailableReason,string SourceSnapshotJson,string EvidenceUrl);

public sealed class TrustCalculationException(string businessCode,string message,int statusCode=400):Exception(message){public string BusinessCode{get;}=businessCode;public int StatusCode{get;}=statusCode;}
