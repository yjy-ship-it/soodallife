using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Providers;

public sealed record ProviderExitBlocker(string Code, string Label, int Count, bool BlocksCompletion);
public sealed record ProviderExitWalletReadiness(Guid WalletId, string CurrencyCode, decimal AvailableBalance,
    decimal ReservedBalance, string StatusCode, decimal PendingRefundAmount, int PendingChargeCount,
    int PendingRefundCount, int PendingFeeRestoreCount, bool RefundRequired, bool ReservedBalanceBlocks);
public sealed record ProviderExitReadinessResponse(bool CanComplete, string RecommendedStatus,
    IReadOnlyList<ProviderExitBlocker> Blockers, ProviderExitWalletReadiness Wallet, int ActiveWorkCount,
    int OpenDisputeCount, int ActiveChatAccessCount, bool AccountClosurePolicyRequired,
    string Guidance, string RetentionPolicyStatus);
public sealed record ProviderExitRequestResponse(Guid Id, string RequestType, string Reason, string Status,
    string ReviewStatus, DateTime RequestedAt, DateTime? ReviewedAt, string? DecisionReason,
    DateTime? CompletedAt, Guid? RefundRequestId, string? RefundStatus, string RowVersion,
    ProviderExitReadinessResponse Readiness);
public sealed record ProviderExitDashboardResponse(string ProviderName, string ApprovalStatus, string ActivityStatus,
    bool HasCustomerRole, ProviderExitRequestResponse? Request, ProviderExitReadinessResponse Readiness);
public sealed record CreateProviderExitRequest(
    [param: Required, StringLength(30)] string RequestType,
    [param: Required, StringLength(1000)] string Reason,
    [param: Required, StringLength(150)] string IdempotencyKey);
public sealed record CancelProviderExitRequest([param: Required, StringLength(1000)] string Reason,
    [param: Required] string RowVersion);
public sealed record AdminProviderExitDecisionRequest([param: Required, StringLength(1000)] string Reason,
    [param: Required] string RowVersion);
public sealed record AdminProviderExitListItem(Guid Id, Guid ProviderId, string ProviderName, string RequestType,
    string Status, string ReviewStatus, DateTime RequestedAt, int ActiveWorkCount, int OpenDisputeCount,
    decimal AvailableBalance, decimal ReservedBalance, bool RefundRequired, string? RefundStatus);
public sealed record AdminProviderExitListResponse(int TotalCount, int Page, int PageSize,
    IReadOnlyList<AdminProviderExitListItem> Items);
public sealed record AdminProviderExitDetailResponse(Guid Id, Guid ProviderId, string ProviderName,
    string RequestType, string Reason, string Status, string ReviewStatus, DateTime RequestedAt,
    DateTime? ReviewedAt, string? DecisionReason, DateTime? CompletedAt, bool HasCustomerRole,
    Guid? RefundRequestId, string? RefundStatus, string RowVersion, ProviderExitReadinessResponse Readiness);

public sealed class ProviderExitException(int statusCode, string businessCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string BusinessCode { get; } = businessCode;
}
