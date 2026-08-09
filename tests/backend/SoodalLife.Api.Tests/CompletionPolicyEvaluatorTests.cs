using System.Text.Json;
using SoodalLife.Api.Features.Work;

namespace SoodalLife.Api.Tests;

public sealed class CompletionPolicyEvaluatorTests
{
    private readonly CompletionPolicyEvaluator _evaluator = new();

    [Fact]
    public void ZeroPhotoPolicy_AllowsNoEvidence() => Assert.True(Evaluate(0, [], []).IsSatisfied);

    [Fact]
    public void TwoPhotoPolicy_AcceptsBeforeAndAfter() =>
        Assert.True(Evaluate(2, [("BEFORE", 1), ("AFTER", 1)], ["BEFORE", "AFTER"]).IsSatisfied);

    [Fact]
    public void TwoPhotoPolicy_RejectsWrongRoleComposition() =>
        Assert.False(Evaluate(2, [("BEFORE", 1), ("AFTER", 1)], ["BEFORE", "BEFORE"]).IsSatisfied);

    [Fact]
    public void FivePhotoPolicy_AcceptsConfiguredRoles() =>
        Assert.True(Evaluate(5, [("BEFORE", 1), ("AFTER", 1), ("OTHER", 3)], ["BEFORE", "AFTER", "OTHER", "OTHER", "OTHER"]).IsSatisfied);

    [Fact]
    public void FivePhotoPolicy_RejectsMissingOtherEvidence() =>
        Assert.False(Evaluate(5, [("BEFORE", 1), ("AFTER", 1), ("OTHER", 3)], ["BEFORE", "AFTER", "BEFORE", "AFTER", "OTHER"]).IsSatisfied);

    private CompletionPolicyStatus Evaluate(int total, (string Code, int Minimum)[] roles, string[] uploaded) =>
        _evaluator.Evaluate(JsonSerializer.Serialize(new
        {
            policyVersion = "test",
            totalRequiredPhotoCount = total,
            roles = roles.Select(role => new { code = role.Code, name = role.Code, minimumCount = role.Minimum }),
        }), uploaded);
}
