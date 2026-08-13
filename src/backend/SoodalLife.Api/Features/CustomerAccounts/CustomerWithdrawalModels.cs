using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed record CustomerWithdrawalBlocker(string Code, string Domain, string Label, int Count, bool BlocksCompletion);
public sealed record CustomerWithdrawalReadinessResponse(
    bool CanComplete,
    string RecommendedStatus,
    int ActiveWorkCount,
    int OpenAfterServiceCount,
    int OpenDisputeCount,
    int FinancialPendingCount,
    bool HasMultipleActiveRoles,
    IReadOnlyList<string> ActiveRoles,
    IReadOnlyList<CustomerWithdrawalBlocker> Blockers,
    string Guidance,
    string RetentionPolicyStatus,
    string AccountClosurePolicyStatus);
public sealed record CustomerWithdrawalRequestResponse(
    Guid Id, string Scope, string Status, string? Reason, DateTime RequestedAt, DateTime? ProcessedAt,
    string? DecisionReason, string RowVersion, CustomerWithdrawalReadinessResponse Readiness);
public sealed record CustomerWithdrawalDashboardResponse(
    CustomerWithdrawalRequestResponse? Request, CustomerWithdrawalReadinessResponse Readiness);
public sealed record CreateCustomerWithdrawalRequest(
    [param: Required] string ScopeCode,
    [param: StringLength(1000)] string? Reason,
    [param: StringLength(150)] string? IdempotencyKey = null);
public sealed record CancelCustomerWithdrawalRequest(
    [param: Required, StringLength(1000)] string Reason,
    [param: Required, StringLength(150)] string IdempotencyKey,
    [param: Required] string RowVersion);
public sealed record AdminCustomerWithdrawalDecisionRequest(
    [param: Required, StringLength(1000)] string Reason,
    [param: Required, StringLength(150)] string IdempotencyKey,
    [param: Required] string RowVersion);
public sealed record AdminCustomerWithdrawalListItem(
    Guid Id, Guid CustomerId, string CustomerDisplayName, string Scope, string Status, DateTime RequestedAt,
    int ActiveWorkCount, int OpenAfterServiceCount, int OpenDisputeCount, int FinancialPendingCount,
    bool HasMultipleActiveRoles, bool CanComplete);
public sealed record AdminCustomerWithdrawalListResponse(
    int TotalCount, int Page, int PageSize, IReadOnlyList<AdminCustomerWithdrawalListItem> Items);
public sealed record AdminCustomerWithdrawalDetailResponse(
    Guid Id, Guid CustomerId, string CustomerDisplayName, string Scope, string Status, string? Reason,
    DateTime RequestedAt, DateTime? ProcessedAt, string? DecisionReason, string RowVersion,
    CustomerWithdrawalReadinessResponse Readiness);

public sealed class CustomerWithdrawalException(int statusCode, string businessCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string BusinessCode { get; } = businessCode;
}
