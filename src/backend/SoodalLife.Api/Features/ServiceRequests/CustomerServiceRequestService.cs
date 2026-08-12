using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.ServiceRequests;

public sealed class CustomerServiceRequestService(
    SoodalLifeDbContext db,
    RequestMatchingService matchingService,
    IPrivateFileStorage fileStorage,
    ServiceRequestFilePrivacyResolver filePrivacyResolver)
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, FileRule> AllowedFiles =
        new Dictionary<string, FileRule>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new(".jpg", [".jpg", ".jpeg"], [[0xFF, 0xD8, 0xFF]]),
            ["image/png"] = new(".png", [".png"], [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ["application/pdf"] = new(".pdf", [".pdf"], [Encoding.ASCII.GetBytes("%PDF-")]),
        };

    public async Task<ServiceRequestCreatedResponse> CreateAsync(
        ClaimsPrincipal principal,
        CreateServiceRequestInput input,
        CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        ValidateDraftInput(input.Title, input.Description, input.DetailAddress, input.IdempotencyKey, input.Answers);

        if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
        {
            var existing = await db.ServiceRequests.AsNoTracking().SingleOrDefaultAsync(
                x => x.IdempotencyKey == input.IdempotencyKey.Trim(), token);
            if (existing is not null)
            {
                if (existing.CustomerProfileId != identity.CustomerProfileId)
                    throw Conflict("REQUEST_IDEMPOTENCY_CONFLICT", "이미 다른 요청에 사용된 중복 방지 키입니다.", "idempotencyKey");
                return new(existing.PublicId, existing.StatusCode);
            }
        }

        var context = await CategoryContextAsync(input.CategoryId, token);
        var areaId = await ResolveAreaIdAsync(input.AdministrativeAreaId, token);
        var answers = await NormalizeAnswersAsync(context, input.Answers ?? [], identity.UserId, requireAll: false, null, token);
        var now = DateTime.UtcNow;
        var request = new ServiceRequest
        {
            CustomerProfileId = identity.CustomerProfileId,
            CategoryId = context.Category.Id,
            CategoryPolicyId = context.Policy.Id,
            AdministrativeAreaId = areaId,
            DetailAddress = EmptyToNull(input.DetailAddress),
            Title = input.Title?.Trim() ?? string.Empty,
            Description = EmptyToNull(input.Description),
            StatusCode = "DRAFT",
            IsUrgent = input.IsUrgent,
            IdempotencyKey = EmptyToNull(input.IdempotencyKey),
            PolicySnapshotJson = PolicySnapshot(context),
            CreatedAt = now,
            CreatedByUserId = identity.UserId,
            UpdatedAt = now,
            UpdatedByUserId = identity.UserId,
        };

        await using var transaction = await BeginTransactionAsync(token);
        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync(token);
        AddAnswers(request.Id, answers, now);
        db.RequestAnswers.AddRange(answers);
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(request.PublicId, request.StatusCode);
    }

    public async Task<ServiceRequestDetailResponse> UpdateDraftAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        UpdateServiceRequestDraftInput input,
        CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var request = await OwnedRequestAsync(identity.CustomerProfileId, requestId, token);
        if (request.StatusCode != "DRAFT")
            throw Conflict("REQUEST_NOT_EDITABLE", "견적 요청 공개 후에는 요청 내용을 수정할 수 없습니다.", "requestId");

        ValidateDraftInput(input.Title, input.Description, input.DetailAddress, null, input.Answers);
        var context = await CategoryContextAsync(request.CategoryId, token);
        var areaId = await ResolveAreaIdAsync(input.AdministrativeAreaId, token);
        var answers = await NormalizeAnswersAsync(context, input.Answers ?? [], identity.UserId, requireAll: false, request.Id, token);
        var now = DateTime.UtcNow;

        await using var transaction = await BeginTransactionAsync(token);
        db.RequestAnswers.RemoveRange(await db.RequestAnswers.Where(x => x.ServiceRequestId == request.Id).ToListAsync(token));
        request.AdministrativeAreaId = areaId;
        request.DetailAddress = EmptyToNull(input.DetailAddress);
        request.Title = input.Title?.Trim() ?? string.Empty;
        request.Description = EmptyToNull(input.Description);
        request.IsUrgent = input.IsUrgent;
        request.UpdatedAt = now;
        request.UpdatedByUserId = identity.UserId;
        await db.SaveChangesAsync(token);
        AddAnswers(request.Id, answers, now);
        db.RequestAnswers.AddRange(answers);
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return (await GetMineByIdAsync(principal, requestId, token))!;
    }

    public async Task<PublishServiceRequestResponse> PublishAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var request = await OwnedRequestAsync(identity.CustomerProfileId, requestId, token);
        if (request.StatusCode is not ("DRAFT" or "OPEN"))
            throw Conflict("REQUEST_NOT_PUBLISHABLE", "작성 중이거나 견적 모집 중인 요청만 공개할 수 있습니다.", "requestId");

        if (request.StatusCode == "DRAFT")
        {
            var context = await CategoryContextAsync(request.CategoryId, token);
            var links = await db.ServiceRequestFiles.AsNoTracking().Where(x => x.ServiceRequestId == request.Id).ToListAsync(token);
            await ValidatePublishAsync(request, context, links, token);
        }

        await using var transaction = await BeginTransactionAsync(token);
        if (request.StatusCode == "DRAFT")
        {
            var policy = await db.CategoryPolicies.SingleAsync(x => x.Id == request.CategoryPolicyId, token);
            var now = DateTime.UtcNow;
            request.StatusCode = "OPEN";
            request.OpenedAt = now;
            request.ExpiresAt = now.AddMinutes(policy.QuoteValidityMinutes);
            request.UpdatedAt = now;
            request.UpdatedByUserId = identity.UserId;
            await db.SaveChangesAsync(token);
        }

        var matching = await matchingService.MatchAndDispatchAsync(request, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        var message = matching.EligibleCandidateCount == 0
            ? "현재 조건에 맞는 공급자를 찾는 중입니다. 운영팀이 확인할 수 있도록 요청은 정상 공개되었습니다."
            : $"조건에 맞는 공급자 {matching.EligibleCandidateCount}명에게 요청이 공개되었습니다.";
        return new(request.PublicId, request.StatusCode, matching.EligibleCandidateCount, matching.DispatchCount, message);
    }

    public async Task<ServiceRequestDetailResponse> CancelAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        CancelServiceRequestInput input,
        CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var request = await OwnedRequestAsync(identity.CustomerProfileId, requestId, token);
        var reason = RequireText(input.Reason, 1000, "reason");
        if (request.StatusCode == "ACCEPTED" || await db.Transactions.AnyAsync(x => x.ServiceRequestId == request.Id, token))
            throw Conflict("REQUEST_ALREADY_ACCEPTED", "공급자를 선택한 요청은 거래 절차에서 취소해야 합니다.", "requestId");
        if (request.StatusCode == "OPEN" && await db.Quotes.AnyAsync(
                x => x.ServiceRequestId == request.Id && x.StatusCode == "SUBMITTED", token))
            throw Conflict("REQUEST_HAS_QUOTES", "도착한 견적이 있는 요청은 현재 화면에서 바로 취소할 수 없습니다.", "requestId");
        if (request.StatusCode is not ("DRAFT" or "OPEN" or "CANCELLED"))
            throw Conflict("REQUEST_NOT_CANCELLABLE", "현재 상태에서는 요청을 취소할 수 없습니다.", "requestId");

        if (request.StatusCode != "CANCELLED")
        {
            var now = DateTime.UtcNow;
            request.StatusCode = "CANCELLED";
            request.CancelledAt = now;
            request.CancellationReason = reason;
            request.UpdatedAt = now;
            request.UpdatedByUserId = identity.UserId;
            var dispatches = await db.RequestDispatches.Where(x => x.ServiceRequestId == request.Id).ToListAsync(token);
            foreach (var dispatch in dispatches) dispatch.StatusCode = "EXPIRED";
            var candidates = await db.DispatchCandidates.Where(x => x.ServiceRequestId == request.Id).ToListAsync(token);
            foreach (var candidate in candidates) candidate.StatusCode = "EXPIRED";
            await db.SaveChangesAsync(token);
        }

        return (await GetMineByIdAsync(principal, requestId, token))!;
    }

    public async Task<ServiceRequestFileResponse> UploadFileAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        Guid? requestFieldId,
        IFormFile upload,
        CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var request = await OwnedRequestAsync(identity.CustomerProfileId, requestId, token);
        if (request.StatusCode != "DRAFT")
            throw Conflict("REQUEST_FILE_NOT_EDITABLE", "작성 중인 요청에만 파일을 추가할 수 있습니다.", "file");

        long? fieldId = null;
        if (requestFieldId.HasValue)
        {
            fieldId = await (from assignment in db.CategoryFieldAssignments.AsNoTracking()
                             join field in db.CategoryFieldDefinitions.AsNoTracking() on assignment.FieldDefinitionId equals field.Id
                             where assignment.IsActive && field.StatusCode == "ACTIVE" && field.FieldTypeCode == "FILE" &&
                                   field.PublicId == requestFieldId &&
                                   (assignment.TargetCategoryId == request.CategoryId ||
                                    db.ServiceCategories.Any(x => x.Id == request.CategoryId && x.ParentId == assignment.TargetCategoryId))
                             select (long?)field.Id).FirstOrDefaultAsync(token);
            if (!fieldId.HasValue)
                throw Invalid("REQUEST_FILE_FIELD_INVALID", "이 서비스에서 사용하는 파일 항목을 선택해 주세요.", "requestFieldId");
        }

        var validated = await ValidateFileAsync(upload, token);
        var now = DateTime.UtcNow;
        var storageKey = $"requests/{request.PublicId:N}/{Guid.NewGuid():N}{validated.Extension}";
        var stored = new StoredFile
        {
            PurposeCode = "REQUEST_ANSWER",
            StorageContainer = "development-private",
            StorageKey = storageKey,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)),
            OriginalFileName = validated.OriginalName,
            ContentType = validated.ContentType,
            SizeBytes = upload.Length,
            Sha256Hex = Convert.ToHexString(validated.Hash).ToLowerInvariant(),
            StatusCode = "PENDING",
            ScanResultText = "NOT_INTEGRATED",
            MalwareScanStatusCode = FilePrivacyCodes.NotIntegrated,
            PrivacyInspectionStatusCode = FilePrivacyCodes.NotIntegrated,
            SanitizationStatusCode = FilePrivacyCodes.NotIntegrated,
            UploadedByUserId = identity.UserId,
            CreatedAt = now,
        };
        db.Files.Add(stored);
        await db.SaveChangesAsync(token);

        try
        {
            await using var source = upload.OpenReadStream();
            await fileStorage.SaveAsync(storageKey, source, token);
            stored.StatusCode = "ACTIVE";
            stored.ActivatedAt = now;
            var displayOrder = await db.ServiceRequestFiles.CountAsync(x => x.ServiceRequestId == request.Id, token) + 1;
            var link = new ServiceRequestFile
            {
                ServiceRequestId = request.Id,
                FileId = stored.Id,
                FieldDefinitionId = fieldId,
                PurposeCode = fieldId.HasValue ? "DYNAMIC_FIELD" : "REQUEST_REFERENCE",
                DisplayOrder = displayOrder,
                CreatedAt = now,
                CreatedByUserId = identity.UserId,
            };
            db.ServiceRequestFiles.Add(link);
            await db.SaveChangesAsync(token);
            return FileResponse(link, stored, requestFieldId, request.PublicId);
        }
        catch
        {
            await fileStorage.DeleteIfExistsAsync(storageKey, token);
            db.Files.Remove(stored);
            await db.SaveChangesAsync(token);
            throw;
        }
    }

    public async Task DeleteFileAsync(ClaimsPrincipal principal, Guid requestId, Guid fileId, CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var request = await OwnedRequestAsync(identity.CustomerProfileId, requestId, token);
        if (request.StatusCode != "DRAFT")
            throw Conflict("REQUEST_FILE_NOT_EDITABLE", "작성 중인 요청의 파일만 삭제할 수 있습니다.", "fileId");
        var row = await (from link in db.ServiceRequestFiles
                         join file in db.Files on link.FileId equals file.Id
                         where link.ServiceRequestId == request.Id && file.PublicId == fileId && file.UploadedByUserId == identity.UserId
                         select new { Link = link, File = file }).SingleOrDefaultAsync(token)
            ?? throw NotFound("REQUEST_FILE_NOT_FOUND", "요청 파일을 찾을 수 없습니다.", "fileId");
        db.ServiceRequestFiles.Remove(row.Link);
        row.File.StatusCode = "DELETED";
        row.File.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        await fileStorage.DeleteIfExistsAsync(row.File.StorageKey, token);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenFileAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        Guid fileId,
        CancellationToken token)
    {
        var userPublicId = PrincipalId(principal);
        var row = await (from link in db.ServiceRequestFiles.AsNoTracking()
                         join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                         join request in db.ServiceRequests.AsNoTracking() on link.ServiceRequestId equals request.Id
                         where request.PublicId == requestId && file.StatusCode == "ACTIVE" &&
                               (file.PublicId == fileId || db.FileDerivatives.Any(relation =>
                                   relation.OriginalFileId == file.Id && relation.DerivedFileId == db.Files
                                       .Where(derived => derived.PublicId == fileId).Select(derived => derived.Id).FirstOrDefault()))
                         select new { Link = link, File = file, Request = request }).SingleOrDefaultAsync(token)
            ?? throw NotFound("REQUEST_FILE_NOT_FOUND", "요청 파일을 찾을 수 없습니다.", "fileId");

        var customerOwns = await (from customer in db.CustomerProfiles.AsNoTracking()
                                  join user in db.Users.AsNoTracking() on customer.UserId equals user.Id
                                  where customer.Id == row.Request.CustomerProfileId && user.PublicId == userPublicId
                                  select customer.Id).AnyAsync(token);
        var providerAllowed = false;
        ProviderPublishedRequestFile? providerPublished = null;
        if (!customerOwns && principal.IsInRole(RoleCodes.Provider))
        {
            var providerId = await (from provider in db.ProviderProfiles.AsNoTracking()
                                    join user in db.Users.AsNoTracking() on provider.UserId equals user.Id
                                    where user.PublicId == userPublicId
                                    select (long?)provider.Id).SingleOrDefaultAsync(token);
            if (providerId.HasValue)
            {
                var relationAllowed = row.Request.StatusCode == "OPEN"
                    ? await db.RequestDispatches.AnyAsync(x => x.ServiceRequestId == row.Request.Id && x.ProviderProfileId == providerId && x.StatusCode != "EXPIRED", token)
                    : row.Request.StatusCode == "ACCEPTED" && await db.Transactions.AnyAsync(x => x.ServiceRequestId == row.Request.Id && x.ProviderProfileId == providerId, token);
                var fieldAllowed = !row.Link.FieldDefinitionId.HasValue || await db.CategoryFieldDefinitions.AsNoTracking().AnyAsync(
                    x => x.Id == row.Link.FieldDefinitionId && x.ProviderVisibilityCode == "FULL" && x.PreAcceptMaskingCode == "NONE", token);
                providerAllowed = relationAllowed && fieldAllowed;
                if (providerAllowed)
                {
                    providerPublished = await filePrivacyResolver.ResolveDownloadAsync(
                        row.Request.Id, providerId.Value, fileId, token);
                    providerAllowed = providerPublished is not null;
                }
            }
        }

        if (!customerOwns && !providerAllowed)
            throw NotFound("REQUEST_FILE_NOT_FOUND", "요청 파일을 찾을 수 없습니다.", "fileId");
        if (customerOwns)
            return (await fileStorage.OpenReadAsync(row.File.StorageKey, token), row.File.ContentType, row.File.OriginalFileName);
        return (await fileStorage.OpenReadAsync(providerPublished!.StorageKey, token),
            providerPublished.ContentType, providerPublished.FileName);
    }

    public async Task<IReadOnlyList<ServiceRequestListItemResponse>> GetMineAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var rows = await (from request in db.ServiceRequests.AsNoTracking()
                          join service in db.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                          join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                          join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                          where request.CustomerProfileId == identity.CustomerProfileId
                          orderby request.CreatedAt descending
                          select new
                          {
                              Request = request,
                              Path = major.Name + " > " + middle.Name + " > " + service.Name,
                              QuoteCount = db.Quotes.Count(x => x.ServiceRequestId == request.Id &&
                                  (x.StatusCode == "SUBMITTED" || x.StatusCode == "ACCEPTED" || x.StatusCode == "NOT_SELECTED")),
                              Transaction = db.Transactions.Where(x => x.ServiceRequestId == request.Id)
                                  .Select(x => new { x.PublicId, x.StatusCode }).FirstOrDefault(),
                          }).ToListAsync(token);
        var desired = await DesiredDatesAsync(rows.Select(x => x.Request.Id).ToArray(), token);
        return rows.Select(x => new ServiceRequestListItemResponse(
            x.Request.PublicId,
            string.IsNullOrWhiteSpace(x.Request.Title) ? "작성 중인 요청" : x.Request.Title,
            x.Request.StatusCode,
            DisplayStatus(x.Request.StatusCode, x.QuoteCount, x.Transaction?.StatusCode),
            x.Path,
            x.Request.CreatedAt,
            desired.GetValueOrDefault(x.Request.Id),
            x.QuoteCount,
            x.Transaction?.PublicId)).ToArray();
    }

    public async Task<ServiceRequestDetailResponse?> GetMineByIdAsync(ClaimsPrincipal principal, Guid requestId, CancellationToken token)
    {
        var identity = await CustomerIdentityAsync(principal, token);
        var row = await (from request in db.ServiceRequests.AsNoTracking()
                         join service in db.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                         join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                         join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                         join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals (long?)area.Id into areaGroup
                         from area in areaGroup.DefaultIfEmpty()
                         where request.PublicId == requestId && request.CustomerProfileId == identity.CustomerProfileId
                         select new
                         {
                             Request = request,
                             Path = major.Name + " > " + middle.Name + " > " + service.Name,
                             MajorPublicId = major.PublicId,
                             MiddlePublicId = middle.PublicId,
                             ServicePublicId = service.PublicId,
                             AreaPublicId = area == null ? (Guid?)null : area.PublicId,
                             AreaName = area == null ? null : area.AreaName,
                         }).SingleOrDefaultAsync(token);
        if (row is null) return null;

        var answerRows = await (from answer in db.RequestAnswers.AsNoTracking()
                                join field in db.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                                where answer.ServiceRequestId == row.Request.Id
                                orderby field.DisplayOrder, field.Id
                                select new { Answer = answer, Field = field }).ToListAsync(token);
        var answers = answerRows.Select(x => new ServiceRequestAnswerResponse(
            x.Field.PublicId, x.Field.FieldKey, x.Field.Label, x.Field.FieldTypeCode, ReadAnswer(x.Answer))).ToArray();
        var files = await RequestFilesAsync(row.Request.Id, row.Request.PublicId, token);
        var quoteCount = await db.Quotes.CountAsync(x => x.ServiceRequestId == row.Request.Id &&
            (x.StatusCode == "SUBMITTED" || x.StatusCode == "ACCEPTED" || x.StatusCode == "NOT_SELECTED"), token);
        var selected = await (from transaction in db.Transactions.AsNoTracking()
                              join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                              where transaction.ServiceRequestId == row.Request.Id
                              select new { TransactionId = transaction.PublicId, TransactionStatus = transaction.StatusCode, ProviderId = provider.PublicId, provider.BusinessName })
            .SingleOrDefaultAsync(token);
        var desiredAt = answerRows.FirstOrDefault(x => x.Field.FieldKey == "desired_date")?.Answer.ValueDateTime;
        var canCancel = row.Request.StatusCode == "DRAFT" || row.Request.StatusCode == "OPEN" && quoteCount == 0;
        return new ServiceRequestDetailResponse(
            row.Request.PublicId,
            string.IsNullOrWhiteSpace(row.Request.Title) ? "작성 중인 요청" : row.Request.Title,
            row.Request.Description,
            row.Request.StatusCode,
            DisplayStatus(row.Request.StatusCode, quoteCount, selected?.TransactionStatus),
            row.Request.IsUrgent,
            row.Path,
            row.MajorPublicId,
            row.MiddlePublicId,
            row.ServicePublicId,
            row.AreaPublicId,
            row.AreaName,
            row.Request.DetailAddress,
            row.Request.CreatedAt,
            desiredAt,
            quoteCount,
            selected?.ProviderId,
            selected?.BusinessName,
            selected?.TransactionId,
            row.Request.StatusCode == "DRAFT",
            canCancel,
            row.Request.StatusCode == "DRAFT",
            answers,
            files);
    }

    private async Task ValidatePublishAsync(ServiceRequest request, CategoryContext context, IReadOnlyList<ServiceRequestFile> files, CancellationToken token)
    {
        if (request.IsUrgent && !context.Policy.IsEmergencyAllowed)
            throw Invalid("EMERGENCY_NOT_ALLOWED", "이 서비스는 긴급출동 요청을 지원하지 않습니다.", "isUrgent");
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.Title)) errors["title"] = ["요청 제목을 입력해 주세요."];
        if (!request.AdministrativeAreaId.HasValue) errors["administrativeAreaId"] = ["서비스 지역을 선택해 주세요."];
        var answers = await db.RequestAnswers.AsNoTracking().Where(x => x.ServiceRequestId == request.Id).ToDictionaryAsync(x => x.FieldDefinitionId, token);
        foreach (var assigned in context.Fields.Where(x => x.Assignment.IsRequired))
        {
            var present = assigned.Field.FieldTypeCode == "FILE"
                ? files.Any(x => x.FieldDefinitionId == assigned.Field.Id)
                : answers.ContainsKey(assigned.Field.Id);
            if (!present) errors[$"answers.{assigned.Field.FieldKey}"] = ["필수 입력 항목입니다."];
        }
        if (errors.Count > 0)
            throw new RequestValidationException("REQUEST_PUBLISH_VALIDATION_FAILED", "견적 요청에 필요한 내용을 확인해 주세요.", errors);
    }

    private async Task<List<RequestAnswer>> NormalizeAnswersAsync(
        CategoryContext context,
        IReadOnlyList<RequestAnswerInput> input,
        long userId,
        bool requireAll,
        long? requestId,
        CancellationToken token)
    {
        var grouped = input.GroupBy(x => x.FieldId).ToDictionary(x => x.Key, x => x.ToArray());
        if (grouped.Any(x => x.Value.Length > 1))
            throw Invalid("DYNAMIC_FIELD_DUPLICATE", "같은 질문에 답을 두 번 보낼 수 없습니다.", "answers");
        var known = context.Fields.Select(x => x.Field.PublicId).ToHashSet();
        if (grouped.Keys.Any(x => !known.Contains(x)))
            throw Invalid("DYNAMIC_FIELD_INVALID", "선택한 서비스에 없는 질문이 포함되어 있습니다.", "answers");

        var fieldIds = context.Fields.Select(x => x.Field.Id).ToArray();
        var optionRows = await db.CategoryFieldOptions.AsNoTracking()
            .Where(x => fieldIds.Contains(x.FieldDefinitionId) && x.IsActive)
            .Select(x => new { x.FieldDefinitionId, x.Value })
            .ToListAsync(token);
        var optionLookup = optionRows.GroupBy(x => x.FieldDefinitionId)
            .ToDictionary(x => x.Key, x => (IReadOnlySet<string>)x.Select(o => o.Value).ToHashSet(StringComparer.Ordinal));
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var result = new List<RequestAnswer>();
        foreach (var assigned in context.Fields)
        {
            var provided = grouped.GetValueOrDefault(assigned.Field.PublicId)?.SingleOrDefault();
            if (provided is null || IsEmpty(provided.Value))
            {
                if (requireAll && assigned.Assignment.IsRequired && assigned.Field.FieldTypeCode != "FILE")
                    errors[$"answers.{assigned.Field.FieldKey}"] = ["필수 입력 항목입니다."];
                continue;
            }
            if (assigned.Field.FieldTypeCode == "FILE") continue;
            try
            {
                var answer = NormalizeAnswer(assigned.Field, provided.Value, userId, optionLookup.GetValueOrDefault(assigned.Field.Id));
                if (requestId.HasValue) answer.ServiceRequestId = requestId.Value;
                result.Add(answer);
            }
            catch (FormatException exception)
            {
                errors[$"answers.{assigned.Field.FieldKey}"] = [exception.Message];
            }
        }
        if (errors.Count > 0)
            throw new RequestValidationException("DYNAMIC_FIELD_INVALID", "동적 요청 항목을 확인해 주세요.", errors);
        return result;
    }

    private static RequestAnswer NormalizeAnswer(CategoryFieldDefinition field, JsonElement value, long userId, IReadOnlySet<string>? options)
    {
        var answer = new RequestAnswer { FieldDefinitionId = field.Id, CreatedByUserId = userId, UpdatedByUserId = userId };
        switch (field.FieldTypeCode)
        {
            case "TEXT":
            case "LONG_TEXT":
            case "ADDRESS":
                answer.ValueText = RequiredString(value);
                break;
            case "SELECT":
                answer.ValueText = RequiredString(value);
                if (options is null || !options.Contains(answer.ValueText)) throw new FormatException("제공된 선택값 중에서 골라 주세요.");
                break;
            case "NUMBER":
            case "MONEY":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) throw new FormatException("숫자로 입력해 주세요.");
                if (field.ValidationRuleText.Contains("0 이상", StringComparison.Ordinal) && number < 0) throw new FormatException("0 이상의 값을 입력해 주세요.");
                answer.ValueNumber = number;
                answer.ValueCurrencyCode = field.FieldTypeCode == "MONEY" ? "KRW" : null;
                break;
            case "DATETIME":
                if (!DateTimeOffset.TryParse(RequiredString(value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
                    throw new FormatException("날짜와 시간을 확인해 주세요.");
                answer.ValueDateTime = dateTime.UtcDateTime;
                if (field.ValidationRuleText.Contains("현재 이후", StringComparison.Ordinal) && answer.ValueDateTime <= DateTime.UtcNow)
                    throw new FormatException("현재 이후의 일시를 입력해 주세요.");
                break;
            case "PERIOD":
            case "RECURRENCE":
                answer.ValueJson = value.GetRawText();
                break;
            default:
                throw new FormatException($"지원하지 않는 입력 유형입니다: {field.FieldTypeCode}");
        }
        return answer;
    }

    private async Task<CategoryContext> CategoryContextAsync(Guid categoryId, CancellationToken token)
    {
        var category = await db.ServiceCategories.SingleOrDefaultAsync(x => x.PublicId == categoryId && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE", token);
        if (category?.ParentId is null) throw Invalid("CATEGORY_NOT_ACTIVE", "현재 요청할 수 있는 서비스를 선택해 주세요.", "categoryId");
        return await CategoryContextAsync(category.Id, token);
    }

    private async Task<CategoryContext> CategoryContextAsync(long categoryId, CancellationToken token)
    {
        var category = await db.ServiceCategories.SingleAsync(x => x.Id == categoryId, token);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var policy = await db.CategoryPolicies.Where(x => x.CategoryId == category.Id && x.TransactionTypeCode == "ONE_TIME" &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo > today))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(token)
            ?? throw Invalid("CATEGORY_NOT_ACTIVE", "현재 적용 가능한 서비스 정책이 없습니다.", "categoryId");
        var fields = await (from assignment in db.CategoryFieldAssignments.AsNoTracking()
                            join field in db.CategoryFieldDefinitions.AsNoTracking() on assignment.FieldDefinitionId equals field.Id
                            where assignment.IsActive && field.StatusCode == "ACTIVE" &&
                                  (assignment.TargetCategoryId == category.Id ||
                                   (assignment.TargetCategoryId == category.ParentId && !db.CategoryFieldAssignments.Any(o =>
                                       o.FieldDefinitionId == field.Id && o.TargetCategoryId == category.Id)))
                            orderby assignment.DisplayOrder, assignment.Id
                            select new AssignedField(field, assignment)).ToListAsync(token);
        return new(category, policy, fields);
    }

    private async Task<long?> ResolveAreaIdAsync(Guid? publicId, CancellationToken token)
    {
        if (!publicId.HasValue) return null;
        return await db.AdministrativeAreas.Where(x => x.PublicId == publicId && x.AreaLevelCode == "SIGUNGU" && x.IsActive)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw Invalid("SERVICE_AREA_INVALID", "사용 가능한 시·군·구를 선택해 주세요.", "administrativeAreaId");
    }

    private async Task<ValidatedFile> ValidateFileAsync(IFormFile upload, CancellationToken token)
    {
        if (upload.Length <= 0 || upload.Length > MaximumFileSize)
            throw Invalid("FILE_SIZE_INVALID", "파일은 10MB 이하만 업로드할 수 있습니다.", "file");
        var originalName = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(originalName) || originalName != upload.FileName || originalName.Length > 255)
            throw Invalid("FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.", "file");
        if (!AllowedFiles.TryGetValue(upload.ContentType, out var rule))
            throw Invalid("FILE_TYPE_INVALID", "JPG, PNG, PDF 파일만 업로드할 수 있습니다.", "file");
        var suppliedExtension = Path.GetExtension(originalName);
        if (!rule.AllowedExtensions.Contains(suppliedExtension, StringComparer.OrdinalIgnoreCase))
            throw Invalid("FILE_EXTENSION_INVALID", "파일 확장자와 형식을 확인해 주세요.", "file");
        await using var source = upload.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, token);
        var bytes = memory.ToArray();
        if (!rule.Signatures.Any(signature => bytes.AsSpan().StartsWith(signature)))
            throw Invalid("FILE_SIGNATURE_INVALID", "파일 내용과 표시된 형식이 일치하지 않습니다.", "file");
        return new(originalName, upload.ContentType.ToLowerInvariant(), rule.StorageExtension, SHA256.HashData(bytes));
    }

    private async Task<List<ServiceRequestFileResponse>> RequestFilesAsync(long requestId, Guid requestPublicId, CancellationToken token)
    {
        var rows = await (from link in db.ServiceRequestFiles.AsNoTracking()
                          join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                          join field in db.CategoryFieldDefinitions.AsNoTracking() on link.FieldDefinitionId equals field.Id into fieldGroup
                          from field in fieldGroup.DefaultIfEmpty()
                          where link.ServiceRequestId == requestId && file.StatusCode == "ACTIVE"
                          orderby link.DisplayOrder
                          select new { Link = link, File = file, FieldPublicId = field == null ? (Guid?)null : field.PublicId }).ToListAsync(token);
        return rows.Select(x => FileResponse(x.Link, x.File, x.FieldPublicId, requestPublicId)).ToList();
    }

    private static ServiceRequestFileResponse FileResponse(ServiceRequestFile link, StoredFile file, Guid? fieldId, Guid requestPublicId) => new(
        file.PublicId, fieldId, link.PurposeCode, file.OriginalFileName, file.ContentType, file.SizeBytes,
        file.ScanResultText == "NOT_INTEGRATED" ? "NOT_INTEGRATED" : "UNKNOWN",
        file.MalwareScanStatusCode ?? "LEGACY_UNSCANNED",
        file.PrivacyInspectionStatusCode ?? "LEGACY_UNSCANNED",
        file.SanitizationStatusCode ?? "LEGACY_UNSCANNED",
        ProviderVisibility(file), link.DisplayOrder,
        $"/api/v1/requests/{requestPublicId}/files/{file.PublicId}");

    private static string ProviderVisibility(StoredFile file) =>
        file.MalwareScanStatusCode == FilePrivacyCodes.Clean &&
        (file.PrivacyInspectionStatusCode == FilePrivacyCodes.Safe || file.SanitizationStatusCode == FilePrivacyCodes.SanitizationCompleted)
            ? "ELIGIBLE_FOR_SERVER_RESOLUTION"
            : "WITHHELD_PRIVACY_PROTECTION_PENDING";

    private async Task<ServiceRequest> OwnedRequestAsync(long customerId, Guid requestId, CancellationToken token) =>
        await db.ServiceRequests.SingleOrDefaultAsync(x => x.PublicId == requestId && x.CustomerProfileId == customerId, token)
        ?? throw NotFound("REQUEST_NOT_FOUND", "서비스 요청을 찾을 수 없습니다.", "requestId");

    private async Task<CustomerIdentity> CustomerIdentityAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var publicId = PrincipalId(principal);
        return await (from user in db.Users.AsNoTracking()
                      join customer in db.CustomerProfiles.AsNoTracking() on user.Id equals customer.UserId
                      where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                      select new CustomerIdentity(user.Id, customer.Id)).SingleOrDefaultAsync(token)
            ?? throw new InvalidOperationException("The authenticated CUSTOMER role has no customer profile.");
    }

    private static Guid PrincipalId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated user identifier is invalid.");

    private async Task<Dictionary<long, DateTime?>> DesiredDatesAsync(long[] requestIds, CancellationToken token) =>
        await (from answer in db.RequestAnswers.AsNoTracking()
               join field in db.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
               where requestIds.Contains(answer.ServiceRequestId) && field.FieldKey == "desired_date"
               select new { answer.ServiceRequestId, answer.ValueDateTime })
            .ToDictionaryAsync(x => x.ServiceRequestId, x => x.ValueDateTime, token);

    private static string DisplayStatus(string status, int quoteCount, string? transactionStatus) => status switch
    {
        "DRAFT" => "작성 중",
        "OPEN" when quoteCount > 0 => "견적 도착",
        "OPEN" => "견적 받는 중",
        "ACCEPTED" when transactionStatus == "CREATED" => "공급자 선택 완료",
        "ACCEPTED" => "거래 진행",
        "CANCELLED" => "요청 취소",
        "EXPIRED" => "견적 모집 종료",
        _ => "상태 확인 중",
    };

    private static object? ReadAnswer(RequestAnswer answer)
    {
        if (answer.ValueText is not null) return answer.ValueText;
        if (answer.ValueNumber is not null) return answer.ValueNumber;
        if (answer.ValueBoolean is not null) return answer.ValueBoolean;
        if (answer.ValueDate is not null) return answer.ValueDate.Value.ToString("yyyy-MM-dd");
        if (answer.ValueDateTime is not null) return answer.ValueDateTime.Value;
        return answer.ValueJson is null ? null : JsonSerializer.Deserialize<JsonElement>(answer.ValueJson);
    }

    private static void AddAnswers(long requestId, IEnumerable<RequestAnswer> answers, DateTime now)
    {
        foreach (var answer in answers)
        {
            answer.ServiceRequestId = requestId;
            answer.CreatedAt = now;
            answer.UpdatedAt = now;
        }
    }

    private static string PolicySnapshot(CategoryContext context) => JsonSerializer.Serialize(new
    {
        categoryId = context.Category.PublicId,
        categoryCode = context.Category.ExternalCode,
        categoryName = context.Category.Name,
        policyId = context.Policy.PublicId,
        policyVersion = context.Policy.PolicyVersion,
        context.Policy.MaxQuoteCount,
        context.Policy.QuoteValidityMinutes,
        context.Policy.ProviderResponseDeadlineMinutes,
        context.Policy.RequiredCompletionPhotoCount,
    });

    private static void ValidateDraftInput(string? title, string? description, string? detailAddress, string? idempotencyKey, IReadOnlyList<RequestAnswerInput>? answers)
    {
        if (title?.Trim().Length > 200) throw Invalid("REQUEST_INVALID", "요청 제목은 200자 이하로 입력해 주세요.", "title");
        if (description?.Length > 20_000) throw Invalid("REQUEST_INVALID", "추가 설명은 20,000자 이하로 입력해 주세요.", "description");
        if (detailAddress?.Length > 500) throw Invalid("REQUEST_INVALID", "상세주소는 500자 이하로 입력해 주세요.", "detailAddress");
        if (idempotencyKey?.Length > 100) throw Invalid("REQUEST_INVALID", "중복 방지 키가 너무 깁니다.", "idempotencyKey");
        if (answers is null) return;
        if (answers.Count > 100) throw Invalid("REQUEST_INVALID", "동적 요청 항목이 너무 많습니다.", "answers");
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken token) =>
        db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token)
            : null;

    private static bool IsEmpty(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
        value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString()) ||
        value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0;
    private static string RequiredString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : throw new FormatException("내용을 입력해 주세요.");
    private static string RequireText(string? value, int maxLength, string field) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength
            ? value.Trim()
            : throw Invalid("REQUEST_INVALID", "취소 사유를 입력해 주세요.", field);
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static RequestValidationException Invalid(string code, string message, string field) =>
        new(code, message, new Dictionary<string, string[]> { [field] = [message] });
    private static RequestValidationException Conflict(string code, string message, string field) =>
        new(code, message, new Dictionary<string, string[]> { [field] = [message] }, StatusCodes.Status409Conflict);
    private static RequestValidationException NotFound(string code, string message, string field) =>
        new(code, message, new Dictionary<string, string[]> { [field] = [message] }, StatusCodes.Status404NotFound);

    private sealed record CustomerIdentity(long UserId, long CustomerProfileId);
    private sealed record AssignedField(CategoryFieldDefinition Field, CategoryFieldAssignment Assignment);
    private sealed record CategoryContext(ServiceCategory Category, CategoryPolicy Policy, IReadOnlyList<AssignedField> Fields);
    private sealed record FileRule(string StorageExtension, string[] AllowedExtensions, byte[][] Signatures);
    private sealed record ValidatedFile(string OriginalName, string ContentType, string Extension, byte[] Hash);
}
