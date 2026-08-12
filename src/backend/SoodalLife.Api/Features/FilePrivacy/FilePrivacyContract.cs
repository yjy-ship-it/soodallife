using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Features.FilePrivacy;

public static class FilePrivacyCodes
{
    public const string NotIntegrated = "NOT_INTEGRATED";
    public const string Clean = "CLEAN";
    public const string Safe = "SAFE";
    public const string SanitizationCompleted = "COMPLETED";
    public const string PrivacySanitized = "PRIVACY_SANITIZED";
}

public enum FileAccessAudience { CustomerOwner, MatchedProviderPreSelection, SelectedProvider, AdminAuthorized }

public sealed record FilePrivacyPublicationDecision(bool Allowed, StoredFile? PublishedFile, string PublicationMode, string ReasonCode);

public interface IFilePrivacyContract
{
    FilePrivacyPublicationDecision Evaluate(StoredFile original, StoredFile? sanitizedDerivative, FileAccessAudience audience);
}

public sealed class FilePrivacyContract : IFilePrivacyContract
{
    public FilePrivacyPublicationDecision Evaluate(StoredFile original, StoredFile? sanitizedDerivative, FileAccessAudience audience)
    {
        if (original.StatusCode != "ACTIVE") return Denied("FILE_NOT_ACTIVE");
        if (audience is FileAccessAudience.CustomerOwner or FileAccessAudience.AdminAuthorized)
            return new(true, original, "ORIGINAL", "AUTHORIZED_ORIGINAL");
        if (original.MalwareScanStatusCode != FilePrivacyCodes.Clean) return Denied("MALWARE_SCAN_NOT_CLEAN");
        if (original.PrivacyInspectionStatusCode == FilePrivacyCodes.Safe)
            return new(true, original, "PRIVACY_SAFE_ORIGINAL", "PRIVACY_SAFE");
        if (original.SanitizationStatusCode == FilePrivacyCodes.SanitizationCompleted &&
            sanitizedDerivative is { StatusCode: "ACTIVE", MalwareScanStatusCode: FilePrivacyCodes.Clean })
            return new(true, sanitizedDerivative, "PRIVACY_SANITIZED_DERIVATIVE", "SANITIZED");
        return Denied("PRIVACY_PROTECTION_INCOMPLETE");
    }

    private static FilePrivacyPublicationDecision Denied(string reason) => new(false, null, "WITHHELD", reason);
}

public sealed record FilePrivacyInspectionInput(Guid FileId, string ContentType, long SizeBytes, string Sha256Hex, Func<CancellationToken, Task<Stream>> OpenReadAsync);
public sealed record FilePrivacyDetection(string DetectionType, decimal Confidence, int? X, int? Y, int? Width, int? Height);
public sealed record FilePrivacyInspectionResult(string StatusCode, string AdapterVersion, IReadOnlyList<FilePrivacyDetection> Detections, string? ErrorCode);
public interface IFilePrivacyInspectionAdapter
{
    Task<FilePrivacyInspectionResult> InspectAsync(FilePrivacyInspectionInput input, CancellationToken cancellationToken);
}

public sealed record FileSanitizationInput(Guid OriginalFileId, string ContentType, string Sha256Hex, IReadOnlyList<FilePrivacyDetection> Detections, Func<CancellationToken, Task<Stream>> OpenReadAsync);
public sealed record FileSanitizationResult(string StatusCode, string AdapterVersion, string? OutputContentType, Stream? Output, string? ErrorCode);
public interface IFileSanitizationAdapter
{
    Task<FileSanitizationResult> SanitizeAsync(FileSanitizationInput input, CancellationToken cancellationToken);
}
