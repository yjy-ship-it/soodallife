using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.FilePrivacy;

public sealed record ProviderPublishedRequestFile(Guid PublicId, string FileName, string ContentType, long SizeBytes,
    string MalwareScanStatus, string PrivacyInspectionStatus, string SanitizationStatus, string PublicationMode, string StorageKey);

public sealed class ServiceRequestFilePrivacyResolver(SoodalLifeDbContext db, IFilePrivacyContract privacyContract)
{
    public async Task<IReadOnlyList<ProviderPublishedRequestFile>> GetPublishedAsync(long requestId, long providerId, CancellationToken token)
    {
        var audience = await ResolveAudienceAsync(requestId, providerId, token);
        if (audience is null) return [];
        var rows = await (from link in db.ServiceRequestFiles.AsNoTracking()
                          join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                          join field in db.CategoryFieldDefinitions.AsNoTracking() on link.FieldDefinitionId equals field.Id into fields
                          from field in fields.DefaultIfEmpty()
                          where link.ServiceRequestId == requestId && (field == null || field.ProviderVisibilityCode == "FULL" && field.PreAcceptMaskingCode == "NONE")
                          orderby link.DisplayOrder
                          select new { Original = file }).ToListAsync(token);
        var originalIds = rows.Select(x => x.Original.Id).ToArray();
        var derivatives = await (from relation in db.FileDerivatives.AsNoTracking()
                                 join file in db.Files.AsNoTracking() on relation.DerivedFileId equals file.Id
                                 where originalIds.Contains(relation.OriginalFileId) && relation.DerivativeTypeCode == FilePrivacyCodes.PrivacySanitized
                                 select new { relation.OriginalFileId, File = file }).ToListAsync(token);
        var derivativeByOriginal = derivatives.GroupBy(x => x.OriginalFileId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.File.CreatedAt).First().File);
        var result = new List<ProviderPublishedRequestFile>();
        foreach (var row in rows)
        {
            var decision = privacyContract.Evaluate(row.Original, derivativeByOriginal.GetValueOrDefault(row.Original.Id), audience.Value);
            if (!decision.Allowed || decision.PublishedFile is null) continue;
            var published = decision.PublishedFile;
            result.Add(new(published.PublicId, ProviderFileName(result.Count + 1, published.ContentType), published.ContentType,
                published.SizeBytes, published.MalwareScanStatusCode ?? "LEGACY_UNSCANNED",
                row.Original.PrivacyInspectionStatusCode ?? "LEGACY_UNSCANNED",
                row.Original.SanitizationStatusCode ?? "LEGACY_UNSCANNED", decision.PublicationMode, published.StorageKey));
        }
        return result;
    }

    public async Task<ProviderPublishedRequestFile?> ResolveDownloadAsync(long requestId, long providerId, Guid fileId, CancellationToken token) =>
        (await GetPublishedAsync(requestId, providerId, token)).SingleOrDefault(x => x.PublicId == fileId);

    private async Task<FileAccessAudience?> ResolveAudienceAsync(long requestId, long providerId, CancellationToken token)
    {
        if (await db.Transactions.AsNoTracking().AnyAsync(x => x.ServiceRequestId == requestId && x.ProviderProfileId == providerId, token))
            return FileAccessAudience.SelectedProvider;
        var matched = await db.ServiceRequests.AsNoTracking().AnyAsync(request => request.Id == requestId && request.StatusCode == "OPEN" &&
            db.RequestDispatches.Any(dispatch => dispatch.ServiceRequestId == request.Id && dispatch.ProviderProfileId == providerId && dispatch.StatusCode != "EXPIRED"), token);
        return matched ? FileAccessAudience.MatchedProviderPreSelection : null;
    }

    private static string ProviderFileName(int sequence, string contentType) => $"attachment-{sequence}{contentType switch
    {
        "image/jpeg" => ".jpg", "image/png" => ".png", "application/pdf" => ".pdf", _ => ".bin",
    }}";
}
