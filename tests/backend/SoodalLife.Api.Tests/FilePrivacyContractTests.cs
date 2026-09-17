using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Infrastructure.Persistence;

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

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CrossDomainResolver_RequiresBothResourceAuthorizationAndFileLinkage(
        bool resourceParticipant, bool fileLinked)
    {
        await using var db = Context();
        var original = ActiveFile("CLEAN", "SAFE");
        original.UploadedByUserId = 10;

        var result = await Resolver(db).ResolveAsync(original, 20, resourceParticipant, fileLinked, default);

        Assert.False(result.Allowed);
        Assert.Equal("RESOURCE_ACCESS_DENIED", result.StatusCode);
    }

    [Fact]
    public async Task CrossDomainResolver_AllowsOwnerOriginalWithoutInventingScanResults()
    {
        await using var db = Context();
        var original = ActiveFile("NOT_INTEGRATED", "NOT_INTEGRATED");
        original.UploadedByUserId = 10;

        var result = await Resolver(db).ResolveAsync(original, 10, true, true, default);

        Assert.True(result.Allowed);
        Assert.Same(original, result.PublishedFile);
        Assert.Equal("ORIGINAL", result.PublicationMode);
        Assert.Equal("NOT_INTEGRATED", original.MalwareScanStatusCode);
        Assert.Equal("NOT_INTEGRATED", original.PrivacyInspectionStatusCode);
    }

    [Fact]
    public async Task CrossDomainResolver_AllowsCleanSafeOriginalForAuthorizedCounterparty()
    {
        await using var db = Context();
        var original = ActiveFile("CLEAN", "SAFE");
        original.UploadedByUserId = 10;

        var result = await Resolver(db).ResolveAsync(original, 20, true, true, default);

        Assert.True(result.Allowed);
        Assert.Same(original, result.PublishedFile);
        Assert.Equal("PRIVACY_SAFE_ORIGINAL", result.PublicationMode);
    }

    [Theory]
    [InlineData(null, "SECURITY_CHECK_REQUIRED")]
    [InlineData("NOT_INTEGRATED", "SECURITY_CHECK_REQUIRED")]
    [InlineData("PENDING", "CHECK_PENDING")]
    [InlineData("FAILED", "REUPLOAD_REQUIRED")]
    public async Task CrossDomainResolver_FailsClosedForIncompleteMalwareChecks(string? status, string expected)
    {
        await using var db = Context();
        var original = ActiveFile(status, "SAFE");
        original.UploadedByUserId = 10;

        var result = await Resolver(db).ResolveAsync(original, 20, true, true, default);

        Assert.False(result.Allowed);
        Assert.Equal(expected, result.StatusCode);
    }

    [Theory]
    [InlineData(null, "SECURITY_CHECK_REQUIRED")]
    [InlineData("NOT_INTEGRATED", "SECURITY_CHECK_REQUIRED")]
    [InlineData("PENDING", "CHECK_PENDING")]
    [InlineData("FAILED", "REUPLOAD_REQUIRED")]
    public async Task CrossDomainResolver_FailsClosedForIncompletePrivacyChecks(string? status, string expected)
    {
        await using var db = Context();
        var original = ActiveFile("CLEAN", status);
        original.UploadedByUserId = 10;

        var result = await Resolver(db).ResolveAsync(original, 20, true, true, default);

        Assert.False(result.Allowed);
        Assert.Equal(expected, result.StatusCode);
    }

    [Fact]
    public async Task CrossDomainResolver_PublishesDistinctCleanSanitizedDerivative()
    {
        await using var db = Context();
        var original = ActiveFile("CLEAN", "SENSITIVE_DETECTED", "COMPLETED");
        original.UploadedByUserId = 10;
        var derivative = ActiveFile("CLEAN", null);
        derivative.UploadedByUserId = 10;
        db.Files.AddRange(original, derivative);
        await db.SaveChangesAsync();
        db.FileDerivatives.Add(new StoredFileDerivative
        {
            OriginalFileId = original.Id,
            DerivedFileId = derivative.Id,
            DerivativeTypeCode = FilePrivacyCodes.PrivacySanitized,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await Resolver(db).ResolveAsync(original, 20, true, true, default);

        Assert.True(result.Allowed);
        Assert.Equal(derivative.Id, result.PublishedFile!.Id);
        Assert.Equal("PRIVACY_SANITIZED_DERIVATIVE", result.PublicationMode);
    }

    [Fact]
    public async Task CrossDomainResolver_RejectsSanitizedDerivativeThatIsNotClean()
    {
        await using var db = Context();
        var original = ActiveFile("CLEAN", "SENSITIVE_DETECTED", "COMPLETED");
        original.UploadedByUserId = 10;
        var derivative = ActiveFile("NOT_INTEGRATED", null);
        db.Files.AddRange(original, derivative);
        await db.SaveChangesAsync();
        db.FileDerivatives.Add(new StoredFileDerivative
        {
            OriginalFileId = original.Id,
            DerivedFileId = derivative.Id,
            DerivativeTypeCode = FilePrivacyCodes.PrivacySanitized,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await Resolver(db).ResolveAsync(original, 20, true, true, default);

        Assert.False(result.Allowed);
        Assert.Equal("SECURITY_CHECK_REQUIRED", result.StatusCode);
    }

    private static SoodalLifeDbContext Context() => new(
        new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options);

    private static CrossDomainFilePublicationResolver Resolver(SoodalLifeDbContext db) =>
        new(db, new FilePrivacyContract());

    private static StoredFile ActiveFile(string? malware, string? privacy, string? sanitization = null) => new()
    {
        PurposeCode = "TEST_EVIDENCE",
        StorageContainer = "test",
        StorageKey = $"test/{Guid.NewGuid():N}",
        StorageKeyHash = Guid.NewGuid().ToByteArray(),
        OriginalFileName = "evidence.jpg",
        ContentType = "image/jpeg",
        SizeBytes = 100,
        Sha256Hex = new string('a', 64),
        StatusCode = "ACTIVE",
        MalwareScanStatusCode = malware,
        PrivacyInspectionStatusCode = privacy,
        SanitizationStatusCode = sanitization,
        CreatedAt = DateTime.UtcNow,
    };
}
