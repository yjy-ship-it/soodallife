using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.FilePrivacy;

namespace SoodalLife.Api.Tests;

public sealed class FilePrivacyContractTests
{
    private readonly FilePrivacyContract _contract = new();

    [Theory]
    [InlineData(null)]
    [InlineData("NOT_INTEGRATED")]
    [InlineData("PENDING")]
    [InlineData("FAILED")]
    public void ProviderAccess_FailsClosed_WhenPrivacyInspectionIsNotSafe(string? privacyStatus)
    {
        var original = ActiveFile(malware: "CLEAN", privacy: privacyStatus);

        var matched = _contract.Evaluate(original, null, FileAccessAudience.MatchedProviderPreSelection);
        var selected = _contract.Evaluate(original, null, FileAccessAudience.SelectedProvider);

        Assert.False(matched.Allowed);
        Assert.False(selected.Allowed);
        Assert.Equal("PRIVACY_PROTECTION_INCOMPLETE", matched.ReasonCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NOT_INTEGRATED")]
    [InlineData("PENDING")]
    [InlineData("FAILED")]
    [InlineData("INFECTED")]
    public void ProviderAccess_FailsClosed_WhenMalwareScanIsNotClean(string? malwareStatus)
    {
        var decision = _contract.Evaluate(
            ActiveFile(malware: malwareStatus, privacy: "SAFE"), null,
            FileAccessAudience.MatchedProviderPreSelection);

        Assert.False(decision.Allowed);
        Assert.Equal("MALWARE_SCAN_NOT_CLEAN", decision.ReasonCode);
    }

    [Fact]
    public void SafeAndCleanOriginal_IsPublishedWithoutCreatingSyntheticState()
    {
        var original = ActiveFile(malware: "CLEAN", privacy: "SAFE");
        var decision = _contract.Evaluate(original, null, FileAccessAudience.MatchedProviderPreSelection);

        Assert.True(decision.Allowed);
        Assert.Same(original, decision.PublishedFile);
        Assert.Equal("PRIVACY_SAFE_ORIGINAL", decision.PublicationMode);
    }

    [Fact]
    public void CompletedSanitization_PublishesDistinctCleanDerivative()
    {
        var original = ActiveFile(malware: "CLEAN", privacy: "SENSITIVE_DETECTED", sanitization: "COMPLETED");
        var derivative = ActiveFile(malware: "CLEAN", privacy: null);
        var decision = _contract.Evaluate(original, derivative, FileAccessAudience.SelectedProvider);

        Assert.True(decision.Allowed);
        Assert.Same(derivative, decision.PublishedFile);
        Assert.NotSame(original, decision.PublishedFile);
        Assert.Equal("PRIVACY_SANITIZED_DERIVATIVE", decision.PublicationMode);
    }

    [Fact]
    public void CustomerOwner_CanRetainOriginalRegardlessOfScannerIntegration()
    {
        var original = ActiveFile(malware: "NOT_INTEGRATED", privacy: "NOT_INTEGRATED");
        var decision = _contract.Evaluate(original, null, FileAccessAudience.CustomerOwner);

        Assert.True(decision.Allowed);
        Assert.Same(original, decision.PublishedFile);
    }

    private static StoredFile ActiveFile(string? malware, string? privacy, string? sanitization = null) => new()
    {
        StatusCode = "ACTIVE",
        MalwareScanStatusCode = malware,
        PrivacyInspectionStatusCode = privacy,
        SanitizationStatusCode = sanitization,
    };
}
