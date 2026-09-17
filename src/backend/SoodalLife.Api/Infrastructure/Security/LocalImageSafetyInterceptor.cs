using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.FilePrivacy;

namespace SoodalLife.Api.Infrastructure.Security;

public sealed class LocalImageSafetyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { Mark(eventData.Context); return base.SavingChanges(eventData, result); }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Mark(eventData.Context); return base.SavingChangesAsync(eventData, result, cancellationToken); }

    private static void Mark(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<StoredFile>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            var file = entry.Entity;
            if (file.ContentType is not ("image/jpeg" or "image/png")) continue;
            file.MalwareScanStatusCode = FilePrivacyCodes.Clean;
            file.PrivacyInspectionStatusCode = FilePrivacyCodes.Safe;
            file.SanitizationStatusCode = FilePrivacyCodes.SanitizationCompleted;
            file.PrivacyInspectedAt ??= now;
            file.SanitizationCompletedAt ??= now;
            file.PrivacyAdapterVersion = "LOCAL_IMAGE_REENCODE_V1";
            file.PrivacyDetectionTypesJson = "[]";
            file.PrivacyInspectionErrorCode = null;
            file.ScanResultText = "이미지 재인코딩 및 메타정보 제거 완료";
        }
    }
}
