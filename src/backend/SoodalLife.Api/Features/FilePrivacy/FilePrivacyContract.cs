using Microsoft.EntityFrameworkCore;
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

public enum FileAccessAudience { CustomerOwner, ResourceCounterparty, MatchedProviderPreSelection, SelectedProvider, AdminAuthorized }

public sealed record FilePrivacyPublicationDecision(bool Allowed, StoredFile? PublishedFile, string PublicationMode, string ReasonCode);

public interface IFilePrivacyContract
{
    FilePrivacyPublicationDecision Evaluate(StoredFile original, StoredFile? sanitizedDerivative, FileAccessAudience audience);
}

public sealed record FilePublicationResult(
    bool Allowed,
    StoredFile? PublishedFile,
    string PublicationMode,
    string StatusCode,
    string Message);

public interface ICrossDomainFilePublicationResolver
{
    Task<FilePublicationResult> ResolveAsync(
        StoredFile original,
        long? viewerUserId,
        bool resourceParticipant,
        bool fileLinkedToResource,
        CancellationToken token);
}

/// <summary>
/// Applies the common publication boundary after a domain has resolved its resource.
/// Domain participation and file linkage are deliberately separate from file safety.
/// </summary>
public sealed class CrossDomainFilePublicationResolver(
    Infrastructure.Persistence.SoodalLifeDbContext db,
    IFilePrivacyContract contract) : ICrossDomainFilePublicationResolver
{
    public async Task<FilePublicationResult> ResolveAsync(
        StoredFile original,
        long? viewerUserId,
        bool resourceParticipant,
        bool fileLinkedToResource,
        CancellationToken token)
    {
        if (!resourceParticipant || !fileLinkedToResource)
            return Denied("RESOURCE_ACCESS_DENIED", "이 업무에 연결된 파일이 아닙니다.");

        var owner = viewerUserId.HasValue && original.UploadedByUserId == viewerUserId.Value;
        var derivative = owner ? null : await (
            from relation in db.FileDerivatives.AsNoTracking()
            join file in db.Files.AsNoTracking() on relation.DerivedFileId equals file.Id
            where relation.OriginalFileId == original.Id && relation.DerivativeTypeCode == FilePrivacyCodes.PrivacySanitized
            orderby relation.CreatedAt descending
            select file).FirstOrDefaultAsync(token);
        var decision = contract.Evaluate(original, derivative,
            owner ? FileAccessAudience.CustomerOwner : FileAccessAudience.ResourceCounterparty);

        if (decision.Allowed)
            return new(true, decision.PublishedFile, decision.PublicationMode, "AVAILABLE", "다운로드할 수 있습니다.");

        return decision.ReasonCode switch
        {
            "FILE_NOT_ACTIVE" => Denied("FILE_NOT_AVAILABLE", "현재 사용할 수 없는 파일입니다."),
            "MALWARE_SCAN_NOT_CLEAN" when IsFailed(original.MalwareScanStatusCode) =>
                Denied("REUPLOAD_REQUIRED", "안전 확인에 실패하여 재업로드가 필요합니다."),
            "MALWARE_SCAN_NOT_CLEAN" when IsPending(original.MalwareScanStatusCode) =>
                Denied("CHECK_PENDING", "파일 안전 확인을 기다리고 있습니다."),
            "MALWARE_SCAN_NOT_CLEAN" =>
                Denied("SECURITY_CHECK_REQUIRED", "안전 확인 전에는 상대방에게 공개되지 않습니다."),
            "PRIVACY_PROTECTION_INCOMPLETE" when IsFailed(original.PrivacyInspectionStatusCode) || IsFailed(original.SanitizationStatusCode) =>
                Denied("REUPLOAD_REQUIRED", "개인정보 보호 처리에 실패하여 재업로드가 필요합니다."),
            "PRIVACY_PROTECTION_INCOMPLETE" when IsPending(original.PrivacyInspectionStatusCode) || IsPending(original.SanitizationStatusCode) =>
                Denied("CHECK_PENDING", "개인정보 보호 확인을 기다리고 있습니다."),
            _ => Denied("SECURITY_CHECK_REQUIRED", "안전 확인 전에는 상대방에게 공개되지 않습니다.")
        };
    }

    private static bool IsPending(string? value) => value?.Trim().ToUpperInvariant() == "PENDING";
    private static bool IsFailed(string? value) => value?.Trim().ToUpperInvariant() == "FAILED";
    private static FilePublicationResult Denied(string code, string message) => new(false, null, "WITHHELD", code, message);
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
