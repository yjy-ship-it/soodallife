namespace SoodalLife.Api.Features.Subscriptions;

public sealed record SubscriptionVisitVerificationInput(
    string? GpsEvidence,
    string? PossessionMethodCode,
    string? PossessionEvidence);

public sealed record SubscriptionVisitVerificationResult(
    string GpsStatusCode,
    string PossessionStatusCode,
    string OverallStatusCode,
    string ReasonCode);

public interface ISubscriptionVisitVerificationAdapter
{
    Task<SubscriptionVisitVerificationResult> VerifyAsync(SubscriptionVisitVerificationInput input, CancellationToken token);
}

public sealed class NotIntegratedSubscriptionVisitVerificationAdapter : ISubscriptionVisitVerificationAdapter
{
    public Task<SubscriptionVisitVerificationResult> VerifyAsync(SubscriptionVisitVerificationInput input, CancellationToken token) =>
        Task.FromResult(new SubscriptionVisitVerificationResult(
            "NOT_INTEGRATED", "NOT_INTEGRATED", "NOT_INTEGRATED", "EXTERNAL_VISIT_VERIFICATION_NOT_INTEGRATED"));
}
