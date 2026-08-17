using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public sealed class ProviderConfigurationService(SoodalLifeDbContext dbContext, IPrivateFileStorage fileStorage)
{
    public async Task<ProviderProfileResponse> GetProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        var rejection = await dbContext.ProviderApprovalEvents.AsNoTracking()
            .Where(x => x.ProviderProfileId == identity.Profile.Id && x.ToStatusCode == "REJECTED")
            .OrderByDescending(x => x.DecidedAt).Select(x => x.Reason).FirstOrDefaultAsync(cancellationToken);
        return MapProfile(identity, rejection);
    }

    public async Task<ProviderProfileResponse> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProviderProfileInput input, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        ApplyConcurrency(identity.Profile, input.ConcurrencyToken);
        Require(input.BusinessName, "BUSINESS_NAME_REQUIRED", "상호 또는 공급자 표시명을 입력해 주세요.", 200);
        Require(input.RepresentativeName, "REPRESENTATIVE_NAME_REQUIRED", "대표자명을 입력해 주세요.", 100);
        Require(input.ContactName, "CONTACT_NAME_REQUIRED", "담당자명을 입력해 주세요.", 100);
        var email = Clean(input.Email, 320)?.ToLowerInvariant();
        if (email is not null && (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@')))
            throw Invalid("EMAIL_INVALID", "올바른 이메일을 입력해 주세요.");
        var phone = Clean(input.Phone, 30)?.Replace("-", "").Replace(" ", "");
        var registrationNumber = input.BusinessRegistrationNumber?.Contains('*') == true
            ? identity.Profile.BusinessRegistrationNo
            : NormalizeBusinessNumber(input.BusinessRegistrationNumber);
        if (registrationNumber is not null && await dbContext.ProviderProfiles.AnyAsync(x => x.Id != identity.Profile.Id && x.BusinessRegistrationNo == registrationNumber, token))
            throw new ProviderConfigurationException("BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.", StatusCodes.Status409Conflict);
        if (email is not null)
        {
            var normalized = email.ToUpperInvariant();
            if (await dbContext.Users.AnyAsync(x => x.Id != identity.UserId && x.NormalizedEmail == normalized, token))
                throw new ProviderConfigurationException("EMAIL_DUPLICATE", "이미 사용 중인 이메일입니다.", StatusCodes.Status409Conflict);
        }
        identity.Profile.BusinessName = input.BusinessName.Trim();
        identity.Profile.RepresentativeName = input.RepresentativeName.Trim();
        identity.Profile.ContactName = input.ContactName.Trim();
        identity.Profile.BusinessRegistrationNo = registrationNumber;
        identity.Profile.BusinessAddress = Clean(input.BusinessAddress, 500);
        identity.Profile.BusinessTypeText = Clean(input.BusinessTypeText, 100);
        identity.Profile.BusinessItemText = Clean(input.BusinessItemText, 100);
        identity.Profile.Introduction = Clean(input.Introduction, 1000);
        identity.Profile.PublicIntroductionHtml = ValidatePublicHtml(input.PublicIntroductionHtml);
        identity.Profile.PublicPhone = Clean(input.PublicPhone, 30);
        identity.Profile.PublicEmail = ValidatePublicEmail(input.PublicEmail);
        identity.Profile.PublicAddress = Clean(input.PublicAddress, 500);
        identity.Profile.PublicBlogUrl = ValidateHttpsUrl(input.PublicBlogUrl, "BLOG_URL_INVALID");
        identity.Profile.PublicWebsiteUrl = ValidateHttpsUrl(input.PublicWebsiteUrl, "WEBSITE_URL_INVALID");
        identity.Profile.PublicLogoUrl = ValidatePublicImageUrl(input.PublicLogoUrl, "LOGO_URL_INVALID");
        identity.Profile.PublicPhotoUrlsJson = JsonSerializer.Serialize((input.PublicPhotoUrls ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x)).Take(5).Select(x => ValidatePublicImageUrl(x, "PHOTO_URL_INVALID")!).ToArray());
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        identity.User.Email = email;
        identity.User.NormalizedEmail = email?.ToUpperInvariant();
        identity.User.Phone = phone;
        identity.User.UpdatedAt = DateTime.UtcNow;
        identity.User.UpdatedByUserId = identity.UserId;
        try { await dbContext.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw new ProviderConfigurationException("PROFILE_CONCURRENCY_CONFLICT", "다른 화면에서 정보가 변경되었습니다. 새로고침 후 다시 시도해 주세요.", StatusCodes.Status409Conflict); }
        return await GetProfileAsync(principal, token);
    }

    public async Task<IReadOnlyList<ProviderServiceCategoryResponse>> GetServiceCategoriesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        return await QueryServiceCategories(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceCategoryResponse>> ReplaceServiceCategoriesAsync(
        ClaimsPrincipal principal,
        ReplaceProviderServiceCategoriesInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        var categoryIds = input.CategoryIds ?? [];
        if (categoryIds.Count != categoryIds.Distinct().Count())
        {
            throw Invalid("PROVIDER_SERVICE_DUPLICATE", "동일한 서비스 카테고리를 중복 선택할 수 없습니다.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = await dbContext.ServiceCategories
            .Where(category => categoryIds.Contains(category.PublicId) &&
                               category.LevelCode == "SERVICE" && category.StatusCode == "ACTIVE" &&
                               dbContext.CategoryPolicies.Any(policy =>
                                   policy.CategoryId == category.Id && policy.TransactionTypeCode == "ONE_TIME" &&
                                   policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today)))
            .ToListAsync(cancellationToken);
        if (categories.Count != categoryIds.Count)
        {
            throw Invalid("PROVIDER_SERVICE_INVALID", "현재 제공 가능한 하위 서비스만 선택할 수 있습니다.");
        }

        var now = DateTime.UtcNow;
        var existing = await dbContext.ProviderServiceCategories
            .Where(item => item.ProviderProfileId == identity.Profile.Id)
            .ToListAsync(cancellationToken);
        var desiredInternalIds = categories.Select(category => category.Id).ToHashSet();
        foreach (var item in existing)
        {
            var active = desiredInternalIds.Contains(item.CategoryId);
            var wasActive = item.StatusCode == "ACTIVE";
            item.StatusCode = active ? "ACTIVE" : "INACTIVE";
            item.ActivatedAt = active && !wasActive ? now : item.ActivatedAt;
            item.DeactivatedAt = active ? null : item.DeactivatedAt ?? now;
            item.UpdatedAt = now;
            item.UpdatedByUserId = identity.UserId;
        }

        foreach (var category in categories.Where(category => existing.All(item => item.CategoryId != category.Id)))
        {
            dbContext.ProviderServiceCategories.Add(new ProviderServiceCategory
            {
                ProviderProfileId = identity.Profile.Id,
                CategoryId = category.Id,
                StatusCode = "ACTIVE",
                ActivatedAt = now,
                CreatedAt = now,
                CreatedByUserId = identity.UserId,
                UpdatedAt = now,
                UpdatedByUserId = identity.UserId,
            });
        }

        var deactivatedIds = existing
            .Where(item => item.StatusCode == "INACTIVE")
            .Select(item => item.Id)
            .ToArray();
        if (deactivatedIds.Length > 0)
        {
            var areas = await dbContext.ProviderServiceAreas
                .Where(area => deactivatedIds.Contains(area.ProviderServiceCategoryId) && area.StatusCode == "ACTIVE")
                .ToListAsync(cancellationToken);
            foreach (var area in areas)
            {
                area.StatusCode = "INACTIVE";
                area.DeactivatedAt = now;
                area.UpdatedAt = now;
                area.UpdatedByUserId = identity.UserId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var activeServices = await dbContext.ProviderServiceCategories
            .Where(x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "ACTIVE")
            .ToListAsync(cancellationToken);
        var activeServiceIds = activeServices.Select(x => x.Id).ToArray();
        var existingApprovalIds = await dbContext.ProviderServiceApprovals
            .Where(x => activeServiceIds.Contains(x.ProviderServiceCategoryId))
            .Select(x => x.ProviderServiceCategoryId).ToListAsync(cancellationToken);
        foreach (var service in activeServices.Where(x => !existingApprovalIds.Contains(x.Id)))
            dbContext.ProviderServiceApprovals.Add(new ProviderServiceApproval
            {
                ProviderServiceCategoryId = service.Id, ApprovalStatusCode = "PENDING", ApprovalRequestedAt = now,
                CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId,
            });
        var requirements = await (from service in dbContext.ProviderServiceCategories
                                  join operation in dbContext.CategoryOperationPolicies on service.CategoryId equals operation.CategoryId
                                  join assignment in dbContext.CategoryProviderRequirementAssignments on operation.Id equals assignment.CategoryOperationPolicyId
                                  where activeServiceIds.Contains(service.Id) && operation.IsActive && assignment.IsActive
                                  select new { ServiceId = service.Id, AssignmentId = assignment.Id }).ToListAsync(cancellationToken);
        var verificationKeys = await dbContext.ProviderServiceRequirementVerifications
            .Where(x => activeServiceIds.Contains(x.ProviderServiceCategoryId))
            .Select(x => new { x.ProviderServiceCategoryId, x.RequirementAssignmentId }).ToListAsync(cancellationToken);
        foreach (var item in requirements.Where(x => !verificationKeys.Any(y => y.ProviderServiceCategoryId == x.ServiceId && y.RequirementAssignmentId == x.AssignmentId)))
            dbContext.ProviderServiceRequirementVerifications.Add(new ProviderServiceRequirementVerification
            {
                ProviderServiceCategoryId = item.ServiceId, RequirementAssignmentId = item.AssignmentId,
                VerificationStatusCode = "PENDING", CreatedAt = now, CreatedByUserId = identity.UserId,
                UpdatedAt = now, UpdatedByUserId = identity.UserId,
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await QueryServiceCategories(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceAreaResponse>> GetServiceAreasAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        return await QueryServiceAreas(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderServiceAreaResponse>> ReplaceServiceAreasAsync(
        ClaimsPrincipal principal,
        ReplaceProviderServiceAreasInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        var selections = input.Services ?? [];
        if (selections.Count != selections.Select(selection => selection.ServiceCategoryId).Distinct().Count() ||
            selections.Any(selection => selection.AdministrativeAreaIds.Count != selection.AdministrativeAreaIds.Distinct().Count()))
        {
            throw Invalid("PROVIDER_AREA_DUPLICATE", "동일한 서비스 또는 출장지역을 중복 선택할 수 없습니다.");
        }

        var serviceCategoryPublicIds = selections.Select(selection => selection.ServiceCategoryId).ToArray();
        var providerServices = await (
                from providerService in dbContext.ProviderServiceCategories
                join category in dbContext.ServiceCategories on providerService.CategoryId equals category.Id
                where providerService.ProviderProfileId == identity.Profile.Id && providerService.StatusCode == "ACTIVE" &&
                      serviceCategoryPublicIds.Contains(category.PublicId)
                select new { ProviderService = providerService, Category = category })
            .ToListAsync(cancellationToken);
        if (providerServices.Count != serviceCategoryPublicIds.Length)
        {
            throw Invalid("PROVIDER_AREA_SERVICE_INVALID", "본인의 활성 제공 서비스에 대해서만 출장지역을 설정할 수 있습니다.");
        }

        var areaPublicIds = selections.SelectMany(selection => selection.AdministrativeAreaIds).Distinct().ToArray();
        var areas = await dbContext.AdministrativeAreas
            .Where(area => areaPublicIds.Contains(area.PublicId) && area.AreaLevelCode == "SIGUNGU" && area.IsActive)
            .ToListAsync(cancellationToken);
        if (areas.Count != areaPublicIds.Length)
        {
            throw Invalid("PROVIDER_AREA_INVALID", "활성 시·군·구 지역만 선택할 수 있습니다.");
        }

        var providerServiceIds = await dbContext.ProviderServiceCategories
            .Where(item => item.ProviderProfileId == identity.Profile.Id)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var existing = await dbContext.ProviderServiceAreas
            .Where(item => providerServiceIds.Contains(item.ProviderServiceCategoryId))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var item in existing.Where(item => item.StatusCode == "ACTIVE"))
        {
            item.StatusCode = "INACTIVE";
            item.DeactivatedAt = now;
            item.UpdatedAt = now;
            item.UpdatedByUserId = identity.UserId;
        }

        var areaByPublicId = areas.ToDictionary(area => area.PublicId);
        var serviceByPublicId = providerServices.ToDictionary(item => item.Category.PublicId);
        foreach (var selection in selections)
        {
            var providerService = serviceByPublicId[selection.ServiceCategoryId].ProviderService;
            foreach (var areaPublicId in selection.AdministrativeAreaIds)
            {
                var area = areaByPublicId[areaPublicId];
                var item = existing.SingleOrDefault(candidate =>
                    candidate.ProviderServiceCategoryId == providerService.Id && candidate.AdministrativeAreaId == area.Id);
                if (item is null)
                {
                    item = new ProviderServiceArea
                    {
                        ProviderServiceCategoryId = providerService.Id,
                        AdministrativeAreaId = area.Id,
                        CreatedAt = now,
                        CreatedByUserId = identity.UserId,
                    };
                    dbContext.ProviderServiceAreas.Add(item);
                    existing.Add(item);
                }

                item.StatusCode = "ACTIVE";
                item.ActivatedAt = now;
                item.DeactivatedAt = null;
                item.UpdatedAt = now;
                item.UpdatedByUserId = identity.UserId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await QueryServiceAreas(identity.Profile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderDocumentTypeResponse>> GetDocumentTypesAsync(CancellationToken token) =>
        await dbContext.ProviderDocumentTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProviderDocumentTypeResponse(x.PublicId, x.Code, x.Name, null)).ToListAsync(token);

    public async Task<IReadOnlyList<ProviderRequirementResponse>> GetRequirementsAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var rows = await (from verification in dbContext.ProviderServiceRequirementVerifications.AsNoTracking()
                          join providerService in dbContext.ProviderServiceCategories.AsNoTracking() on verification.ProviderServiceCategoryId equals providerService.Id
                          join service in dbContext.ServiceCategories.AsNoTracking() on providerService.CategoryId equals service.Id
                          join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                          join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                          join assignment in dbContext.CategoryProviderRequirementAssignments.AsNoTracking() on verification.RequirementAssignmentId equals assignment.Id
                          join definition in dbContext.ProviderRequirementDefinitions.AsNoTracking() on assignment.RequirementDefinitionId equals definition.Id
                          join document in dbContext.ProviderDocuments.AsNoTracking() on verification.ProviderDocumentId equals document.Id into documents
                          from document in documents.DefaultIfEmpty()
                          join file in dbContext.Files.AsNoTracking() on document.FileId equals file.Id into files
                          from file in files.DefaultIfEmpty()
                          where providerService.ProviderProfileId == identity.Profile.Id && providerService.StatusCode == "ACTIVE"
                          orderby major.SortOrder, middle.SortOrder, service.SortOrder, assignment.DisplayOrder
                          select new { verification, assignment, definition, ServiceId = service.PublicId,
                              Path = major.Name + " > " + middle.Name + " > " + service.Name,
                              DocumentId = document == null ? (long?)null : document.Id,
                              DocumentPublicId = document == null ? (Guid?)null : document.FileId == 0 ? null : file.PublicId,
                              DocumentName = file == null ? null : file.OriginalFileName }).ToListAsync(token);
        var assignmentIds = rows.Select(x => x.assignment.Id).Distinct().ToArray();
        var evidence = await (from item in dbContext.CategoryProviderRequirementEvidenceTypes.AsNoTracking()
                              join type in dbContext.ProviderDocumentTypes.AsNoTracking() on item.DocumentTypeId equals type.Id
                              where assignmentIds.Contains(item.RequirementAssignmentId) && type.IsActive
                              orderby item.DisplayOrder
                              select new { item.RequirementAssignmentId, type.PublicId, type.Code, type.Name, item.IsRequired }).ToListAsync(token);
        return rows.Select(x => new ProviderRequirementResponse(x.verification.PublicId, x.assignment.PublicId, x.ServiceId, x.Path,
            x.definition.RequirementCode, x.definition.Name, x.definition.RequirementTypeCode, x.assignment.IsRequired,
            x.assignment.VerificationRequired, x.assignment.ExpiryCheckRequired, x.assignment.MinimumValidDays,
            x.verification.VerificationStatusCode, x.DocumentPublicId, x.DocumentName, x.verification.ExpiresAt,
            x.verification.RejectionReason, evidence.Where(e => e.RequirementAssignmentId == x.assignment.Id)
                .Select(e => new ProviderEvidenceTypeResponse(e.PublicId, e.Code, e.Name, e.IsRequired)).ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<ProviderDocumentResponse>> GetDocumentsAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        return await (from document in dbContext.ProviderDocuments.AsNoTracking()
                      join file in dbContext.Files.AsNoTracking() on document.FileId equals file.Id
                      join type in dbContext.ProviderDocumentTypes.AsNoTracking() on document.DocumentTypeId equals type.Id into types
                      from type in types.DefaultIfEmpty()
                      where document.ProviderProfileId == identity.Profile.Id && file.StatusCode != "DELETED"
                      orderby document.CreatedAt descending
                      select new ProviderDocumentResponse(file.PublicId, file.PublicId, document.DocumentTypeCode,
                          type == null ? document.DocumentTypeCode : type.Name, file.OriginalFileName, file.SizeBytes,
                          file.ContentType, document.VerificationStatusCode, file.MalwareScanStatusCode ?? "NOT_INTEGRATED",
                          document.IssuedAt, document.ExpiresAt, document.Note, document.CreatedAt)).ToListAsync(token);
    }

    public async Task<ProviderProfileResponse> UploadPromotionLogoAsync(ClaimsPrincipal principal, IFormFile upload, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var image = await ValidatePromotionImageAsync(upload, token);
        var stored = await StorePromotionImagesAsync(identity, [image], "PROVIDER_PUBLIC_LOGO", token);
        identity.Profile.PublicLogoUrl = PromotionImageUrl(stored[0].PublicId);
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        return await GetProfileAsync(principal, token);
    }

    public async Task<ProviderProfileResponse> UploadPromotionLogoContentAsync(ClaimsPrincipal principal, ProviderPromotionImageContentInput input, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var image = ValidatePromotionImageContent(input);
        var stored = await StorePromotionImagesAsync(identity, [image], "PROVIDER_PUBLIC_LOGO", token);
        identity.Profile.PublicLogoUrl = PromotionImageUrl(stored[0].PublicId);
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        return await GetProfileAsync(principal, token);
    }

    public async Task<ProviderPromotionStorageStatusResponse> CheckPromotionStorageAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var key = $"provider-promotion/{identity.Profile.PublicId:N}/.write-check-{Guid.NewGuid():N}.tmp";
        try
        {
            await using var content = new MemoryStream([0x53, 0x4c]);
            await fileStorage.SaveAsync(key, content, token);
            await fileStorage.DeleteIfExistsAsync(key, token);
            return new(true, "이미지 저장소를 사용할 수 있습니다.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try { await fileStorage.DeleteIfExistsAsync(key, token); } catch { }
            throw new ProviderConfigurationException("PROMOTION_IMAGE_STORAGE_UNAVAILABLE",
                "이미지 저장 폴더에 쓸 수 없습니다. 서버의 App_Data 권한을 확인해 주세요.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<ProviderProfileResponse> UploadPromotionPhotosAsync(ClaimsPrincipal principal, IReadOnlyList<IFormFile> uploads, CancellationToken token)
    {
        if (uploads.Count is < 1 or > 5)
            throw Invalid("PROMOTION_PHOTO_COUNT_INVALID", "홍보 사진은 한 번에 1개 이상, 최대 5개까지 등록할 수 있습니다.");
        var identity = await GetIdentityAsync(principal, token);
        var images = new List<ValidatedPromotionImage>(uploads.Count);
        foreach (var upload in uploads) images.Add(await ValidatePromotionImageAsync(upload, token));
        var stored = await StorePromotionImagesAsync(identity, images, "PROVIDER_PUBLIC_PHOTO", token);
        identity.Profile.PublicPhotoUrlsJson = JsonSerializer.Serialize(stored.Select(x => PromotionImageUrl(x.PublicId)).ToArray());
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        return await GetProfileAsync(principal, token);
    }

    public async Task<ProviderProfileResponse> UploadPromotionPhotoContentAsync(ClaimsPrincipal principal, ProviderPromotionPhotoContentInput input, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var image = ValidatePromotionImageContent(input.File);
        List<string> existingUrls = input.ReplaceExisting ? [] : ParseUrls(identity.Profile.PublicPhotoUrlsJson).ToList();
        if (!input.ReplaceExisting && existingUrls.Count >= 5)
            throw Invalid("PROMOTION_PHOTO_COUNT_INVALID", "홍보 사진은 최대 5개까지 등록할 수 있습니다.");

        var existingIds = existingUrls.Select(url => Guid.TryParse(url.Split('/').LastOrDefault(), out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(image.Bytes)).ToLowerInvariant();
        if (!input.ReplaceExisting && await dbContext.Files.AsNoTracking().AnyAsync(x => existingIds.Contains(x.PublicId) && x.Sha256Hex == hash, token))
            return await GetProfileAsync(principal, token);

        var stored = await StorePromotionImagesAsync(identity, [image], "PROVIDER_PUBLIC_PHOTO", token);
        existingUrls.Add(PromotionImageUrl(stored[0].PublicId));
        identity.Profile.PublicPhotoUrlsJson = JsonSerializer.Serialize(existingUrls);
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        return await GetProfileAsync(principal, token);
    }

    public async Task<ProviderPromotionImageChunkResponse> UploadPromotionImageChunkAsync(ClaimsPrincipal principal,
        ProviderPromotionImageChunkInput input, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var purpose = input.Purpose.Trim().ToUpperInvariant();
        if (input.UploadId == Guid.Empty || purpose is not ("LOGO" or "PHOTO"))
            throw Invalid("PROMOTION_IMAGE_CHUNK_INVALID", "이미지 업로드 정보를 확인해 주세요.");
        if (input.TotalChunks is < 1 or > 80 || input.ChunkIndex < 0 || input.ChunkIndex >= input.TotalChunks ||
            string.IsNullOrWhiteSpace(input.Base64Chunk) || input.Base64Chunk.Length > 90_000)
            throw Invalid("PROMOTION_IMAGE_CHUNK_INVALID", "이미지 조각 정보가 올바르지 않습니다.");

        var chunkParent = $"provider-promotion/{identity.Profile.PublicId:N}";
        var chunkPrefix = $"{chunkParent}/chunks-{input.UploadId:N}";
        var completedFile = await dbContext.Files.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicId == input.UploadId && x.StatusCode == "ACTIVE" &&
            (x.PurposeCode == "PROVIDER_PUBLIC_LOGO" || x.PurposeCode == "PROVIDER_PUBLIC_PHOTO"), token);
        if (completedFile is not null)
        {
            var completedUrl = PromotionImageUrl(input.UploadId);
            if (identity.Profile.PublicLogoUrl == completedUrl || ParseUrls(identity.Profile.PublicPhotoUrlsJson).Contains(completedUrl))
                return new(true, await GetProfileAsync(principal, token));
            var expectedPurpose = purpose == "LOGO" ? "PROVIDER_PUBLIC_LOGO" : "PROVIDER_PUBLIC_PHOTO";
            if (completedFile.UploadedByUserId != identity.UserId || completedFile.PurposeCode != expectedPurpose)
                throw new ProviderConfigurationException("PROMOTION_IMAGE_UPLOAD_CONFLICT",
                    "이미지 업로드 정보가 일치하지 않습니다. 이미지를 다시 선택해 주세요.", StatusCodes.Status409Conflict);
            if (purpose == "LOGO") identity.Profile.PublicLogoUrl = completedUrl;
            else
            {
                List<string> recoveredUrls = input.ReplaceExisting ? [] : ParseUrls(identity.Profile.PublicPhotoUrlsJson).ToList();
                if (!recoveredUrls.Contains(completedUrl) && recoveredUrls.Count < 5) recoveredUrls.Add(completedUrl);
                identity.Profile.PublicPhotoUrlsJson = JsonSerializer.Serialize(recoveredUrls);
            }
            identity.Profile.UpdatedAt = DateTime.UtcNow;
            identity.Profile.UpdatedByUserId = identity.UserId;
            await dbContext.SaveChangesAsync(token);
            await DeletePromotionChunksAsync(chunkPrefix, token);
            return new(true, await GetProfileAsync(principal, token));
        }

        byte[] chunk;
        try { chunk = Convert.FromBase64String(input.Base64Chunk); }
        catch (FormatException) { throw Invalid("PROMOTION_IMAGE_CHUNK_INVALID", "이미지 조각을 읽을 수 없습니다."); }
        if (chunk.Length is < 1 or > 65_536)
            throw Invalid("PROMOTION_IMAGE_CHUNK_INVALID", "이미지 조각의 크기가 올바르지 않습니다.");

        try { await fileStorage.PrepareBoundedUploadDirectoryAsync(chunkParent, chunkPrefix, token); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ProviderConfigurationException("PROMOTION_IMAGE_UPLOAD_LIMIT",
                "처리 중인 이미지가 많습니다. 잠시 후 다시 시도해 주세요.", StatusCodes.Status429TooManyRequests);
        }
        var chunkKey = $"{chunkPrefix}/{input.ChunkIndex:D2}.part";
        try
        {
            await using var content = new MemoryStream(chunk, writable: false);
            await fileStorage.SaveAsync(chunkKey, content, token);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try
            {
                await using var existing = await fileStorage.OpenReadAsync(chunkKey, token);
                using var memory = new MemoryStream();
                await existing.CopyToAsync(memory, token);
                if (!memory.ToArray().AsSpan().SequenceEqual(chunk))
                    throw new ProviderConfigurationException("PROMOTION_IMAGE_CHUNK_CONFLICT",
                        "같은 이미지 업로드의 조각 정보가 변경되었습니다. 이미지를 다시 선택해 주세요.", StatusCodes.Status409Conflict);
            }
            catch (ProviderConfigurationException) { throw; }
            catch (Exception readException) when (readException is IOException or UnauthorizedAccessException)
            {
                throw new ProviderConfigurationException("PROMOTION_IMAGE_CHUNK_UNAVAILABLE",
                    "이미지 조각을 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.", StatusCodes.Status503ServiceUnavailable);
            }
        }

        if (input.ChunkIndex != input.TotalChunks - 1) return new(false, null);

        using var assembled = new MemoryStream();
        try
        {
            for (var index = 0; index < input.TotalChunks; index++)
            {
                await using var part = await fileStorage.OpenReadAsync($"{chunkPrefix}/{index:D2}.part", token);
                await part.CopyToAsync(assembled, token);
                if (assembled.Length > 5_242_880)
                    throw Invalid("PROMOTION_IMAGE_SIZE_INVALID", "로고와 홍보 사진은 파일당 5MB 이하만 등록할 수 있습니다.");
            }
        }
        catch (ProviderConfigurationException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ProviderConfigurationException("PROMOTION_IMAGE_CHUNK_INCOMPLETE",
                "이미지 전송이 완료되지 않았습니다. 다시 저장해 주세요.", StatusCodes.Status409Conflict);
        }

        var image = ValidatePromotionImageBytes(input.FileName, input.ContentType.ToLowerInvariant(), assembled.ToArray());
        if (purpose == "LOGO")
        {
            var stored = await StorePromotionImagesAsync(identity, [image], "PROVIDER_PUBLIC_LOGO", token, [input.UploadId]);
            identity.Profile.PublicLogoUrl = PromotionImageUrl(stored[0].PublicId);
        }
        else
        {
            List<string> existingUrls = input.ReplaceExisting ? [] : ParseUrls(identity.Profile.PublicPhotoUrlsJson).ToList();
            if (!input.ReplaceExisting && existingUrls.Count >= 5)
                throw Invalid("PROMOTION_PHOTO_COUNT_INVALID", "홍보 사진은 최대 5개까지 등록할 수 있습니다.");
            var existingIds = existingUrls.Select(url => Guid.TryParse(url.Split('/').LastOrDefault(), out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty).ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(image.Bytes)).ToLowerInvariant();
            if (!input.ReplaceExisting && await dbContext.Files.AsNoTracking().AnyAsync(x =>
                    existingIds.Contains(x.PublicId) && x.Sha256Hex == hash, token))
            {
                await DeletePromotionChunksAsync(chunkPrefix, token);
                return new(true, await GetProfileAsync(principal, token));
            }
            var stored = await StorePromotionImagesAsync(identity, [image], "PROVIDER_PUBLIC_PHOTO", token, [input.UploadId]);
            existingUrls.Add(PromotionImageUrl(stored[0].PublicId));
            identity.Profile.PublicPhotoUrlsJson = JsonSerializer.Serialize(existingUrls);
        }
        identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.Profile.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        await DeletePromotionChunksAsync(chunkPrefix, token);
        return new(true, await GetProfileAsync(principal, token));
    }

    private async Task DeletePromotionChunksAsync(string prefix, CancellationToken token)
    {
        try { await fileStorage.DeleteDirectoryIfExistsAsync(prefix, token); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public async Task<(Stream Content, string ContentType)> OpenPromotionImageAsync(Guid fileId, CancellationToken token)
    {
        var url = PromotionImageUrl(fileId);
        var file = await dbContext.Files.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicId == fileId && x.StatusCode == "ACTIVE" &&
            (x.PurposeCode == "PROVIDER_PUBLIC_LOGO" || x.PurposeCode == "PROVIDER_PUBLIC_PHOTO"), token);
        if (file is null || !await dbContext.ProviderProfiles.AsNoTracking().AnyAsync(x =>
                x.PublicLogoUrl == url || x.PublicPhotoUrlsJson != null && x.PublicPhotoUrlsJson.Contains(fileId.ToString()), token))
            throw new ProviderConfigurationException("PROMOTION_IMAGE_NOT_FOUND", "공개 중인 홍보 이미지를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        return (await fileStorage.OpenReadAsync(file.StorageKey, token), file.ContentType);
    }

    private async Task<IReadOnlyList<StoredFile>> StorePromotionImagesAsync(ProviderIdentity identity,
        IReadOnlyList<ValidatedPromotionImage> images, string purpose, CancellationToken token,
        IReadOnlyList<Guid>? fileIds = null)
    {
        var now = DateTime.UtcNow;
        var rows = images.Select((image, index) =>
        {
            var publicId = fileIds?[index] ?? Guid.NewGuid();
            var key = $"provider-promotion/{identity.Profile.PublicId:N}/{publicId:N}{image.Extension}";
            return new StoredFile
            {
                PublicId = publicId,
                PurposeCode = purpose, StorageContainer = "development-private", StorageKey = key,
                StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = image.Name,
                ContentType = image.ContentType, SizeBytes = image.Bytes.LongLength,
                Sha256Hex = Convert.ToHexString(SHA256.HashData(image.Bytes)).ToLowerInvariant(), StatusCode = "PENDING",
                MalwareScanStatusCode = "NOT_INTEGRATED", PrivacyInspectionStatusCode = "NOT_INTEGRATED",
                SanitizationStatusCode = "NOT_INTEGRATED", UploadedByUserId = identity.UserId, CreatedAt = now,
            };
        }).ToArray();
        dbContext.Files.AddRange(rows);
        await dbContext.SaveChangesAsync(token);
        try
        {
            for (var index = 0; index < rows.Length; index++)
            {
                await using var content = new MemoryStream(images[index].Bytes, writable: false);
                await fileStorage.SaveAsync(rows[index].StorageKey, content, token);
                rows[index].StatusCode = "ACTIVE";
                rows[index].ActivatedAt = now;
                rows[index].ScanResultText = "NOT_INTEGRATED";
            }
            await dbContext.SaveChangesAsync(token);
            return rows;
        }
        catch (Exception exception)
        {
            foreach (var row in rows)
            {
                try { await fileStorage.DeleteIfExistsAsync(row.StorageKey, token); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            dbContext.Files.RemoveRange(rows);
            await dbContext.SaveChangesAsync(token);
            if (exception is IOException or UnauthorizedAccessException)
                throw new ProviderConfigurationException("PROMOTION_IMAGE_STORAGE_UNAVAILABLE",
                    "이미지 저장소를 사용할 수 없습니다. 잠시 후 다시 시도해 주세요.", StatusCodes.Status503ServiceUnavailable);
            throw;
        }
    }

    private static async Task<ValidatedPromotionImage> ValidatePromotionImageAsync(IFormFile upload, CancellationToken token)
    {
        if (upload.Length is <= 0 or > 5_242_880)
            throw Invalid("PROMOTION_IMAGE_SIZE_INVALID", "로고와 홍보 사진은 파일당 5MB 이하만 등록할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(name) || name != upload.FileName || name.Length > 255)
            throw Invalid("PROMOTION_IMAGE_NAME_INVALID", "안전한 이미지 파일명을 사용해 주세요.");
        var contentType = upload.ContentType.ToLowerInvariant();
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var allowed = (contentType, extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/webp", ".webp") => true,
            _ => false,
        };
        if (!allowed) throw Invalid("PROMOTION_IMAGE_TYPE_INVALID", "JPG, PNG, WEBP 이미지만 등록할 수 있습니다.");
        await using var input = upload.OpenReadStream();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory, token);
        var bytes = memory.ToArray();
        return ValidatePromotionImageBytes(name, contentType, bytes);
    }

    private static ValidatedPromotionImage ValidatePromotionImageContent(ProviderPromotionImageContentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Base64Content) || input.Base64Content.Length > 7_100_000)
            throw Invalid("PROMOTION_IMAGE_SIZE_INVALID", "로고와 홍보 사진은 파일당 5MB 이하만 등록할 수 있습니다.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(input.Base64Content); }
        catch (FormatException) { throw Invalid("PROMOTION_IMAGE_CONTENT_INVALID", "이미지 내용을 읽을 수 없습니다."); }
        return ValidatePromotionImageBytes(input.FileName, input.ContentType.ToLowerInvariant(), bytes);
    }

    private static ValidatedPromotionImage ValidatePromotionImageBytes(string fileName, string contentType, byte[] bytes)
    {
        if (bytes.Length is <= 0 or > 5_242_880)
            throw Invalid("PROMOTION_IMAGE_SIZE_INVALID", "로고와 홍보 사진은 파일당 5MB 이하만 등록할 수 있습니다.");
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name) || name != fileName || name.Length > 255)
            throw Invalid("PROMOTION_IMAGE_NAME_INVALID", "안전한 이미지 파일명을 사용해 주세요.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var allowed = (contentType, extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/webp", ".webp") => true,
            _ => false,
        };
        if (!allowed) throw Invalid("PROMOTION_IMAGE_TYPE_INVALID", "JPG, PNG, WEBP 이미지만 등록할 수 있습니다.");
        var valid = contentType switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };
        if (!valid) throw Invalid("PROMOTION_IMAGE_SIGNATURE_INVALID", "이미지 확장자와 실제 파일 형식이 일치하지 않습니다.");
        return new(bytes, name, contentType, extension);
    }

    private static string PromotionImageUrl(Guid fileId) => $"/api/v1/provider-promotion-images/{fileId}";

    public async Task<ProviderDocumentResponse> UploadDocumentAsync(ClaimsPrincipal principal, RegisterProviderDocumentInput input, IFormFile upload, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var documentType = await dbContext.ProviderDocumentTypes.SingleOrDefaultAsync(x => x.PublicId == input.DocumentTypeId && x.IsActive, token)
            ?? throw Invalid("DOCUMENT_TYPE_INVALID", "사용 가능한 증빙 유형을 선택해 주세요.");
        if (upload.Length is <= 0 or > 10_485_760) throw Invalid("DOCUMENT_FILE_SIZE_INVALID", "증빙 파일은 10MB 이하만 등록할 수 있습니다.");
        var name = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(name) || name != upload.FileName || name.Length > 255) throw Invalid("DOCUMENT_FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!AllowedFile(upload.ContentType, extension)) throw Invalid("DOCUMENT_FILE_TYPE_INVALID", "PDF, JPG, PNG 파일만 등록할 수 있습니다.");
        await using var inputStream = upload.OpenReadStream();
        using var memory = new MemoryStream();
        await inputStream.CopyToAsync(memory, token);
        var bytes = memory.ToArray();
        if (!ValidSignature(upload.ContentType, bytes)) throw Invalid("DOCUMENT_FILE_SIGNATURE_INVALID", "파일 형식과 실제 내용이 일치하지 않습니다.");
        var now = DateTime.UtcNow;
        var key = $"provider-documents/{identity.Profile.PublicId:N}/{Guid.NewGuid():N}{extension}";
        var file = new StoredFile
        {
            PurposeCode = "PROVIDER_DOCUMENT", StorageContainer = "development-private", StorageKey = key,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = name,
            ContentType = upload.ContentType, SizeBytes = bytes.LongLength,
            Sha256Hex = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), StatusCode = "PENDING",
            MalwareScanStatusCode = "NOT_INTEGRATED", PrivacyInspectionStatusCode = "NOT_INTEGRATED",
            SanitizationStatusCode = "NOT_INTEGRATED", UploadedByUserId = identity.UserId, CreatedAt = now,
        };
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync(token);
        try
        {
            memory.Position = 0;
            await fileStorage.SaveAsync(key, memory, token);
            file.StatusCode = "ACTIVE"; file.ActivatedAt = now; file.ScanResultText = "NOT_INTEGRATED";
            dbContext.ProviderDocuments.Add(new ProviderDocument
            {
                ProviderProfileId = identity.Profile.Id, FileId = file.Id, DocumentTypeId = documentType.Id,
                DocumentTypeCode = documentType.Code, DocumentNumber = Clean(input.DocumentNumber, 100), IssuedAt = input.IssuedAt,
                ExpiresAt = input.ExpiresAt, VerificationStatusCode = "PENDING", CreatedAt = now,
                CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId,
            });
            await dbContext.SaveChangesAsync(token);
            return (await GetDocumentsAsync(principal, token)).Single(x => x.FileId == file.PublicId);
        }
        catch { await fileStorage.DeleteIfExistsAsync(key, token); throw; }
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenDocumentAsync(ClaimsPrincipal principal, Guid fileId, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var file = await (from document in dbContext.ProviderDocuments.AsNoTracking()
                          join stored in dbContext.Files.AsNoTracking() on document.FileId equals stored.Id
                          where document.ProviderProfileId == identity.Profile.Id && stored.PublicId == fileId && stored.StatusCode == "ACTIVE"
                          select stored).SingleOrDefaultAsync(token)
            ?? throw new ProviderConfigurationException("PROVIDER_DOCUMENT_NOT_FOUND", "증빙 파일을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        return (await fileStorage.OpenReadAsync(file.StorageKey, token), file.ContentType, file.OriginalFileName);
    }

    public async Task<ProviderRequirementResponse> LinkEvidenceAsync(ClaimsPrincipal principal, Guid verificationId, LinkProviderEvidenceInput input, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var verification = await (from item in dbContext.ProviderServiceRequirementVerifications
                                  join service in dbContext.ProviderServiceCategories on item.ProviderServiceCategoryId equals service.Id
                                  where item.PublicId == verificationId && service.ProviderProfileId == identity.Profile.Id
                                  select item).SingleOrDefaultAsync(token)
            ?? throw new ProviderConfigurationException("PROVIDER_REQUIREMENT_NOT_FOUND", "서비스 요건을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var document = await (from item in dbContext.ProviderDocuments
                              join file in dbContext.Files on item.FileId equals file.Id
                              where file.PublicId == input.DocumentId && item.ProviderProfileId == identity.Profile.Id && file.StatusCode == "ACTIVE"
                              select item).SingleOrDefaultAsync(token)
            ?? throw new ProviderConfigurationException("PROVIDER_DOCUMENT_NOT_FOUND", "본인이 등록한 증빙을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var accepted = await dbContext.CategoryProviderRequirementEvidenceTypes.AnyAsync(x =>
            x.RequirementAssignmentId == verification.RequirementAssignmentId && x.DocumentTypeId == document.DocumentTypeId, token);
        if (!accepted) throw Invalid("DOCUMENT_TYPE_NOT_ACCEPTED", "이 요건에 사용할 수 없는 증빙 유형입니다.");
        verification.ProviderDocumentId = document.Id;
        verification.VerificationStatusCode = "PENDING";
        verification.VerifiedAt = null; verification.VerifiedByUserId = null; verification.RejectionReason = null;
        verification.ExpiresAt = document.ExpiresAt; verification.UpdatedAt = DateTime.UtcNow; verification.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
        return (await GetRequirementsAsync(principal, token)).Single(x => x.VerificationId == verificationId);
    }

    public async Task<ProviderOnboardingDashboardResponse> GetDashboardAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var services = await dbContext.ProviderServiceCategories.AsNoTracking().Where(x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "ACTIVE").Select(x => x.Id).ToListAsync(token);
        var approvals = await dbContext.ProviderServiceApprovals.AsNoTracking().Where(x => services.Contains(x.ProviderServiceCategoryId)).Select(x => x.ApprovalStatusCode).ToListAsync(token);
        var requirements = await dbContext.ProviderServiceRequirementVerifications.AsNoTracking().Where(x => services.Contains(x.ProviderServiceCategoryId)).Select(x => new { x.ProviderDocumentId, x.VerificationStatusCode }).ToListAsync(token);
        var areaCount = await dbContext.ProviderServiceAreas.AsNoTracking().CountAsync(x => services.Contains(x.ProviderServiceCategoryId) && x.StatusCode == "ACTIVE", token);
        var actions = new List<string>();
        if (string.IsNullOrWhiteSpace(identity.Profile.RepresentativeName) || string.IsNullOrWhiteSpace(identity.Profile.ContactName)) actions.Add("기본정보를 완료해 주세요.");
        if (services.Count == 0) actions.Add("제공 서비스를 선택해 주세요.");
        if (areaCount == 0) actions.Add("서비스별 활동지역을 선택해 주세요.");
        if (requirements.Any(x => x.ProviderDocumentId == null)) actions.Add("필수 증빙을 제출해 주세요.");
        if (identity.Profile.ApprovalStatusCode == "PENDING") actions.Add("본사 심사 결과를 기다려 주세요.");
        var rejection = await dbContext.ProviderApprovalEvents.AsNoTracking().Where(x => x.ProviderProfileId == identity.Profile.Id && x.ToStatusCode == "REJECTED").OrderByDescending(x => x.DecidedAt).Select(x => x.Reason).FirstOrDefaultAsync(token);
        return new(identity.Profile.ApprovalStatusCode, identity.Profile.ActivityStatusCode, services.Count,
            approvals.Count(x => x == "APPROVED"), approvals.Count(x => x == "PENDING"), approvals.Count(x => x == "REJECTED"),
            areaCount, requirements.Count, requirements.Count(x => x.ProviderDocumentId != null), requirements.Count(x => x.VerificationStatusCode == "APPROVED"), actions, rejection);
    }

    public async Task<ProviderOperationsDashboardResponse> GetOperationsDashboardAsync(
        ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var transactionIds = dbContext.Transactions.AsNoTracking()
            .Where(x => x.ProviderProfileId == identity.Profile.Id)
            .Select(x => x.Id);
        var appointmentIds = dbContext.TransactionAppointments.AsNoTracking()
            .Where(x => transactionIds.Contains(x.TransactionId)).Select(x => x.Id);

        var newRequests = await dbContext.RequestDispatches.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "AVAILABLE" && x.ExpiresAt > now, token);
        var submittedQuotes = await dbContext.Quotes.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.SubmittedAt != null, token);
        var waitingSelectionQuotes = await dbContext.Quotes.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "SUBMITTED", token);
        var selectedTransactions = await dbContext.Transactions.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode != "CANCELLED", token);
        var appointmentActions = await dbContext.TransactionAppointments.AsNoTracking().CountAsync(
            x => transactionIds.Contains(x.TransactionId) && x.StatusCode == "PROPOSED" && x.CreatedByUserId != identity.UserId, token);
        appointmentActions += await dbContext.TransactionAppointmentChangeRequests.AsNoTracking().CountAsync(
            x => appointmentIds.Contains(x.TransactionAppointmentId) && x.StatusCode == "REQUESTED" && x.RequestedByUserId != identity.UserId, token);
        var todayAppointments = await dbContext.TransactionAppointments.AsNoTracking().CountAsync(
            x => transactionIds.Contains(x.TransactionId) && x.StatusCode == "CONFIRMED" &&
                 x.ScheduledStartAt >= today && x.ScheduledStartAt < tomorrow, token);
        var inProgress = await dbContext.Transactions.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "IN_PROGRESS", token);
        var waitingConfirmation = await dbContext.Transactions.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "COMPLETION_SUBMITTED", token);
        var revisions = await dbContext.Transactions.AsNoTracking().CountAsync(
            x => x.ProviderProfileId == identity.Profile.Id && x.StatusCode == "REVISION_REQUESTED", token);
        var unread = await dbContext.Notifications.AsNoTracking().CountAsync(
            x => x.RecipientUserId == identity.UserId && x.ReadAt == null, token);

        return new(newRequests, submittedQuotes, waitingSelectionQuotes, selectedTransactions, appointmentActions,
            todayAppointments, inProgress, waitingConfirmation, revisions, unread);
    }

    public async Task ResubmitServiceAsync(ClaimsPrincipal principal, Guid categoryId, CancellationToken token)
    {
        var identity = await GetIdentityAsync(principal, token);
        var approval = await (from service in dbContext.ProviderServiceCategories
                              join category in dbContext.ServiceCategories on service.CategoryId equals category.Id
                              join item in dbContext.ProviderServiceApprovals on service.Id equals item.ProviderServiceCategoryId
                              where service.ProviderProfileId == identity.Profile.Id && category.PublicId == categoryId && service.StatusCode == "ACTIVE"
                              select item).SingleOrDefaultAsync(token)
            ?? throw new ProviderConfigurationException("PROVIDER_SERVICE_NOT_FOUND", "등록 서비스를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (approval.ApprovalStatusCode != "REJECTED") throw Invalid("SERVICE_RESUBMIT_NOT_ALLOWED", "반려된 서비스만 재심사를 요청할 수 있습니다.");
        approval.ApprovalStatusCode = "PENDING"; approval.ApprovalRequestedAt = DateTime.UtcNow;
        approval.ApprovalDecidedAt = null; approval.ApprovalDecidedByUserId = null; approval.DecisionReason = null;
        approval.UpdatedAt = DateTime.UtcNow; approval.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(token);
    }

    private static bool AllowedFile(string contentType, string extension) => (contentType, extension) switch
    { ("application/pdf", ".pdf") => true, ("image/jpeg", ".jpg" or ".jpeg") => true, ("image/png", ".png") => true, _ => false };
    private static bool ValidSignature(string contentType, byte[] bytes) => contentType switch
    {
        "application/pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        _ => false,
    };

    private async Task<List<ProviderServiceCategoryResponse>> QueryServiceCategories(long providerProfileId, CancellationToken cancellationToken) =>
        await (
                from providerService in dbContext.ProviderServiceCategories.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on providerService.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                where providerService.ProviderProfileId == providerProfileId && providerService.StatusCode == "ACTIVE"
                orderby major.SortOrder, middle.SortOrder, service.SortOrder
                select new ProviderServiceCategoryResponse(
                    service.PublicId,
                    major.Name + " > " + middle.Name + " > " + service.Name,
                    providerService.StatusCode,
                    dbContext.ProviderServiceApprovals.Where(x => x.ProviderServiceCategoryId == providerService.Id).Select(x => x.ApprovalStatusCode).FirstOrDefault() ?? "PENDING",
                    dbContext.ProviderServiceApprovals.Where(x => x.ProviderServiceCategoryId == providerService.Id).Select(x => x.DecisionReason).FirstOrDefault(),
                    dbContext.ProviderServiceRequirementVerifications.Count(x => x.ProviderServiceCategoryId == providerService.Id),
                    dbContext.ProviderServiceRequirementVerifications.Count(x => x.ProviderServiceCategoryId == providerService.Id && x.VerificationStatusCode == "APPROVED")))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ProviderServiceAreaResponse>> QueryServiceAreas(long providerProfileId, CancellationToken cancellationToken)
    {
        var services = await (
                from providerService in dbContext.ProviderServiceCategories.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on providerService.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                where providerService.ProviderProfileId == providerProfileId && providerService.StatusCode == "ACTIVE"
                orderby major.SortOrder, middle.SortOrder, service.SortOrder
                select new
                {
                    ProviderServiceId = providerService.Id,
                    service.PublicId,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                })
            .ToListAsync(cancellationToken);
        var providerServiceIds = services.Select(service => service.ProviderServiceId).ToArray();
        var areas = await (
                from providerArea in dbContext.ProviderServiceAreas.AsNoTracking()
                join area in dbContext.AdministrativeAreas.AsNoTracking() on providerArea.AdministrativeAreaId equals area.Id
                where providerServiceIds.Contains(providerArea.ProviderServiceCategoryId) && providerArea.StatusCode == "ACTIVE"
                orderby area.AreaName
                select new
                {
                    providerArea.ProviderServiceCategoryId,
                    Area = new ProviderAreaResponse(area.PublicId, area.AreaName, area.AreaCode),
                })
            .ToListAsync(cancellationToken);

        return services.Select(service => new ProviderServiceAreaResponse(
            service.PublicId,
            service.Path,
            areas.Where(area => area.ProviderServiceCategoryId == service.ProviderServiceId)
                .Select(area => area.Area)
                .ToArray())).ToArray();
    }

    private async Task<ProviderIdentity> GetWritableIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await GetIdentityAsync(principal, cancellationToken);
        if (identity.Profile.ApprovalStatusCode != "APPROVED" || identity.Profile.ActivityStatusCode != "ACTIVE")
        {
            throw new ProviderConfigurationException(
                "PROVIDER_NOT_ELIGIBLE",
                "승인되고 활성 상태인 공급자만 공급 범위를 설정할 수 있습니다.",
                StatusCodes.Status403Forbidden);
        }

        return identity;
    }

    private async Task<ProviderIdentity> GetIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
        {
            throw new InvalidOperationException("Authenticated user identifier is invalid.");
        }

        var identity = await (
                from user in dbContext.Users
                join provider in dbContext.ProviderProfiles on user.Id equals provider.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new ProviderIdentity(user.Id, user, provider))
            .SingleOrDefaultAsync(cancellationToken);
        return identity ?? throw new InvalidOperationException("The authenticated PROVIDER role has no provider profile.");
    }

    private static ProviderConfigurationException Invalid(string code, string message) => new(code, message);
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw Invalid("VALUE_TOO_LONG", $"{max}자 이하로 입력해 주세요.");
    private static void Require(string? value, string code, string message, int max) { if (string.IsNullOrWhiteSpace(value)) throw Invalid(code, message); if (value.Trim().Length > max) throw Invalid("VALUE_TOO_LONG", $"{max}자 이하로 입력해 주세요."); }
    private static string? NormalizeBusinessNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = new string(value.Where(char.IsDigit).ToArray());
        if (normalized.Length != 10) throw Invalid("BUSINESS_NUMBER_INVALID", "사업자등록번호는 숫자 10자리로 입력해 주세요.");
        return normalized;
    }
    private void ApplyConcurrency(ProviderProfile profile, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        try { dbContext.Entry(profile).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(token); }
        catch (FormatException) { throw Invalid("CONCURRENCY_TOKEN_INVALID", "동시성 토큰이 올바르지 않습니다."); }
    }
    private static string MaskBusinessNumber(string? value) => string.IsNullOrWhiteSpace(value) || value.Length < 10 ? "미등록" : $"{value[..3]}-**-{value[^5..]}";
    private static ProviderProfileResponse MapProfile(ProviderIdentity identity, string? rejection) => new(
        identity.Profile.PublicId, identity.Profile.BusinessName, identity.Profile.RepresentativeName, identity.Profile.ContactName,
        identity.User.Phone, identity.User.Email, identity.Profile.BusinessRegistrationNo is null ? null : MaskBusinessNumber(identity.Profile.BusinessRegistrationNo), identity.Profile.BusinessAddress,
        identity.Profile.BusinessTypeText, identity.Profile.BusinessItemText, identity.Profile.Introduction,
        identity.Profile.PublicIntroductionHtml, identity.Profile.PublicPhone, identity.Profile.PublicEmail,
        identity.Profile.PublicAddress, identity.Profile.PublicBlogUrl, identity.Profile.PublicWebsiteUrl,
        identity.Profile.PublicLogoUrl, ParseUrls(identity.Profile.PublicPhotoUrlsJson), null,
        identity.Profile.ApprovalStatusCode, identity.Profile.ActivityStatusCode, identity.Profile.TrustScore,
        identity.Profile.TrustScore is null ? "평가 전" : "산정 완료", rejection, Convert.ToBase64String(identity.Profile.RowVersion));
    private static IReadOnlyList<string> ParseUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch (JsonException) { return []; }
    }
    private static string? ValidatePublicEmail(string? value)
    {
        var email = Clean(value, 320)?.ToLowerInvariant();
        if (email is not null && (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@')))
            throw Invalid("PUBLIC_EMAIL_INVALID", "공개 이메일 형식을 확인해 주세요.");
        return email;
    }
    private static string? ValidateHttpsUrl(string? value, string code)
    {
        var text = Clean(value, 1000);
        if (text is null) return null;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw Invalid(code, "공개 링크는 https:// 주소만 입력해 주세요.");
        return uri.ToString();
    }
    private static string? ValidatePublicImageUrl(string? value, string code)
    {
        var text = Clean(value, 1000);
        if (text is null) return null;
        const string prefix = "/api/v1/provider-promotion-images/";
        if (text.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParse(text[prefix.Length..], out _)) return text;
        return ValidateHttpsUrl(text, code);
    }
    private static string? ValidatePublicHtml(string? value)
    {
        var html = Clean(value, 8000);
        if (html is null) return null;
        var lower = html.ToLowerInvariant();
        string[] blocked = ["<script", "<iframe", "<object", "<embed", "<form", "javascript:", "data:", " onerror=", " onload="];
        if (blocked.Any(lower.Contains)) throw Invalid("PUBLIC_INTRODUCTION_HTML_UNSAFE", "공급자 소개에 허용되지 않는 HTML이 포함되어 있습니다.");
        return html;
    }
    private sealed record ProviderIdentity(long UserId, User User, ProviderProfile Profile);
    private sealed record ValidatedPromotionImage(byte[] Bytes, string Name, string ContentType, string Extension);
}
