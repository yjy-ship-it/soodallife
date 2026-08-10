using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Work;

public sealed class WorkService(
    SoodalLifeDbContext db,
    CompletionPolicyEvaluator policyEvaluator,
    IPrivateFileStorage fileStorage)
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, (string Extension, byte[][] Signatures)> AllowedImages =
        new Dictionary<string, (string, byte[][])>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", [[0xFF, 0xD8, 0xFF]]),
            ["image/png"] = (".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ["image/webp"] = (".webp", [Encoding.ASCII.GetBytes("RIFF")]),
        };

    public async Task<IReadOnlyList<WorkTransactionListItem>> GetProviderTransactionsAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        return await ListAsync(providerId: identity.ProfileId, customerId: null, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkTransactionListItem>> GetCustomerTransactionsAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identity = await CustomerIdentityAsync(principal, cancellationToken);
        return await ListAsync(providerId: null, customerId: identity.ProfileId, cancellationToken);
    }

    public async Task<WorkTransactionDetail> GetProviderDetailAsync(
        ClaimsPrincipal principal, Guid transactionId, CancellationToken cancellationToken)
    {
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        var transaction = await db.Transactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.PublicId == transactionId && item.ProviderProfileId == identity.ProfileId, cancellationToken)
            ?? throw NotFound();
        return await BuildDetailAsync(transaction, providerView: true, cancellationToken);
    }

    public async Task<WorkTransactionDetail> GetCustomerDetailAsync(
        ClaimsPrincipal principal, Guid transactionId, CancellationToken cancellationToken)
    {
        var identity = await CustomerIdentityAsync(principal, cancellationToken);
        var transaction = await db.Transactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.PublicId == transactionId && item.CustomerProfileId == identity.ProfileId, cancellationToken)
            ?? throw NotFound();
        return await BuildDetailAsync(transaction, providerView: false, cancellationToken);
    }

    public async Task<WorkTransactionDetail> StartAsync(
        ClaimsPrincipal principal, Guid transactionId, CancellationToken cancellationToken)
    {
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        var transaction = await OwnedProviderTransactionAsync(identity.ProfileId, transactionId, cancellationToken);
        if (transaction.StatusCode == "IN_PROGRESS") return await BuildDetailAsync(transaction, true, cancellationToken);
        if (transaction.StatusCode != "CREATED") throw Conflict("TRANSACTION_STATE_CONFLICT", "현재 상태에서는 작업을 시작할 수 없습니다.");
        var now = DateTime.UtcNow;
        transaction.StatusCode = "IN_PROGRESS";
        transaction.StartedAt = now;
        transaction.UpdatedAt = now;
        transaction.UpdatedByUserId = identity.UserId;
        db.OutboxEvents.Add(NewOutbox(transaction, "TRANSACTION_STARTED", identity.UserId, now));
        await db.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(transaction, true, cancellationToken);
    }

    public async Task<WorkCompletionRevisionResponse> SaveDraftAsync(
        ClaimsPrincipal principal, Guid transactionId, SaveCompletionDraftInput input, CancellationToken cancellationToken)
    {
        ValidateDraft(input);
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        var transaction = await OwnedProviderTransactionAsync(identity.ProfileId, transactionId, cancellationToken);
        if (transaction.StatusCode is not ("IN_PROGRESS" or "REVISION_REQUESTED"))
            throw Conflict("TRANSACTION_STATE_CONFLICT", "작업 중이거나 보완 요청 상태에서만 완료 내용을 저장할 수 있습니다.");

        var existing = await db.WorkCompletionRevisions.AsNoTracking().SingleOrDefaultAsync(
            item => item.IdempotencyKey == input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var completionId = await db.WorkCompletions.Where(item => item.TransactionId == transaction.Id).Select(item => item.Id).SingleAsync(cancellationToken);
            if (existing.WorkCompletionId != completionId) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 작업에 사용된 요청 키입니다.");
            return await BuildRevisionAsync(transaction, existing, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var completion = await db.WorkCompletions.SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken);
        if (completion is null)
        {
            completion = new WorkCompletion
            {
                TransactionId = transaction.Id, StatusCode = "DRAFT", LatestRevisionNo = 0,
                CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId,
            };
            db.WorkCompletions.Add(completion);
            await db.SaveChangesAsync(cancellationToken);
        }

        var previous = await db.WorkCompletionRevisions
            .Where(item => item.WorkCompletionId == completion.Id)
            .OrderByDescending(item => item.RevisionNo).FirstOrDefaultAsync(cancellationToken);
        if (previous is not null && previous.StatusCode == "DRAFT") previous.StatusCode = "SUPERSEDED";
        var revision = new WorkCompletionRevision
        {
            WorkCompletionId = completion.Id,
            RevisionNo = completion.LatestRevisionNo + 1,
            StatusCode = "DRAFT",
            WorkSummary = input.WorkSummary.Trim(),
            ChecklistJson = JsonSerializer.Serialize(new { actualAmount = input.ActualAmount, currencyCode = transaction.CurrencyCode }),
            ProviderAttestationAt = now,
            SubmittedAt = now,
            SubmittedByUserId = identity.UserId,
            RevisionReason = NullIfEmpty(input.RevisionReason),
            IdempotencyKey = input.IdempotencyKey.Trim(),
        };
        db.WorkCompletionRevisions.Add(revision);
        completion.LatestRevisionNo = revision.RevisionNo;
        completion.StatusCode = "DRAFT";
        completion.UpdatedAt = now;
        completion.UpdatedByUserId = identity.UserId;
        await db.SaveChangesAsync(cancellationToken);

        if (previous is not null)
        {
            var links = await db.CompletionEvidenceFiles.AsNoTracking().Where(item => item.CompletionRevisionId == previous.Id).ToListAsync(cancellationToken);
            db.CompletionEvidenceFiles.AddRange(links.Select(link => new CompletionEvidenceFile
            {
                CompletionRevisionId = revision.Id, FileId = link.FileId, PhotoRoleId = link.PhotoRoleId,
                DisplayOrder = link.DisplayOrder, Description = link.Description, CreatedAt = now, CreatedByUserId = identity.UserId,
            }));
            await db.SaveChangesAsync(cancellationToken);
        }
        return await BuildRevisionAsync(transaction, revision, cancellationToken);
    }

    public async Task<CompletionEvidenceResponse> UploadEvidenceAsync(
        ClaimsPrincipal principal, Guid transactionId, string roleCode, string? description,
        IFormFile upload, CancellationToken cancellationToken)
    {
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        var transaction = await OwnedProviderTransactionAsync(identity.ProfileId, transactionId, cancellationToken);
        if (transaction.StatusCode is not ("IN_PROGRESS" or "REVISION_REQUESTED"))
            throw Conflict("TRANSACTION_STATE_CONFLICT", "현재 상태에서는 완료 증빙을 추가할 수 없습니다.");
        var completion = await db.WorkCompletions.SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken)
            ?? throw Conflict("COMPLETION_DRAFT_REQUIRED", "완료 초안을 먼저 저장해 주세요.");
        var revision = await db.WorkCompletionRevisions.SingleAsync(
            item => item.WorkCompletionId == completion.Id && item.RevisionNo == completion.LatestRevisionNo, cancellationToken);
        if (revision.StatusCode != "DRAFT") throw Conflict("COMPLETION_DRAFT_REQUIRED", "수정 가능한 완료 초안이 없습니다.");
        var role = await db.CompletionPhotoRoles.SingleOrDefaultAsync(item => item.Code == roleCode && item.IsActive, cancellationToken)
            ?? throw Invalid("PHOTO_ROLE_INVALID", "사용할 수 없는 사진 역할입니다.", "roleCode");
        var (contentType, extension, contentHash) = await ValidateImageAsync(upload, cancellationToken);
        if (description?.Length > 500) throw Invalid("PHOTO_DESCRIPTION_TOO_LONG", "사진 설명은 500자 이하여야 합니다.", "description");
        var originalName = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(originalName) || originalName != upload.FileName || originalName.Length > 255)
            throw Invalid("FILE_NAME_INVALID", "안전한 파일명을 사용해 주세요.", "file");

        var now = DateTime.UtcNow;
        var storageKey = $"completion/{transaction.PublicId:N}/{Guid.NewGuid():N}{extension}";
        var stored = new StoredFile
        {
            PurposeCode = "COMPLETION_EVIDENCE", StorageContainer = "development-private", StorageKey = storageKey,
            StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)), OriginalFileName = originalName,
            ContentType = contentType, SizeBytes = upload.Length, Sha256Hex = Convert.ToHexString(contentHash).ToLowerInvariant(),
            StatusCode = "PENDING", UploadedByUserId = identity.UserId, CreatedAt = now,
        };
        db.Files.Add(stored);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            await using var input = upload.OpenReadStream();
            await fileStorage.SaveAsync(storageKey, input, cancellationToken);
            stored.StatusCode = "ACTIVE";
            stored.ActivatedAt = now;
            stored.ScanResultText = "Development-only signature validation; malware scanning is not configured.";
            var order = await db.CompletionEvidenceFiles.CountAsync(item => item.CompletionRevisionId == revision.Id, cancellationToken) + 1;
            var link = new CompletionEvidenceFile
            {
                CompletionRevisionId = revision.Id, FileId = stored.Id, PhotoRoleId = role.Id, DisplayOrder = order,
                Description = NullIfEmpty(description), CreatedAt = now, CreatedByUserId = identity.UserId,
            };
            db.CompletionEvidenceFiles.Add(link);
            await db.SaveChangesAsync(cancellationToken);
            return new CompletionEvidenceResponse(stored.PublicId, stored.OriginalFileName, stored.ContentType, stored.SizeBytes,
                role.Code, role.Name, order, link.Description, $"/api/v1/files/{stored.PublicId}");
        }
        catch
        {
            await fileStorage.DeleteIfExistsAsync(storageKey, cancellationToken);
            throw;
        }
    }

    public async Task<WorkCompletionRevisionResponse> SubmitCompletionAsync(
        ClaimsPrincipal principal, Guid transactionId, CancellationToken cancellationToken)
    {
        var identity = await ProviderIdentityAsync(principal, cancellationToken);
        var transaction = await OwnedProviderTransactionAsync(identity.ProfileId, transactionId, cancellationToken);
        var completion = await db.WorkCompletions.SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken)
            ?? throw Conflict("COMPLETION_DRAFT_REQUIRED", "완료 초안을 먼저 저장해 주세요.");
        var revision = await db.WorkCompletionRevisions.SingleAsync(
            item => item.WorkCompletionId == completion.Id && item.RevisionNo == completion.LatestRevisionNo, cancellationToken);
        if (transaction.StatusCode == "COMPLETION_SUBMITTED" && revision.StatusCode == "SUBMITTED")
            return await BuildRevisionAsync(transaction, revision, cancellationToken);
        if (transaction.StatusCode is not ("IN_PROGRESS" or "REVISION_REQUESTED") || revision.StatusCode != "DRAFT")
            throw Conflict("TRANSACTION_STATE_CONFLICT", "현재 상태에서는 완료 내용을 제출할 수 없습니다.");
        var roleCodes = await EvidenceRoleCodesAsync(revision.Id, cancellationToken);
        var policy = policyEvaluator.Evaluate(transaction.CompletionPolicySnapshotJson, roleCodes);
        if (!policy.TotalSatisfied) throw Conflict("COMPLETION_PHOTO_COUNT_NOT_MET", "완료 사진의 총 수량이 정책에 미달합니다.");
        if (policy.Roles.Any(role => !role.IsSatisfied)) throw Conflict("COMPLETION_PHOTO_ROLE_NOT_MET", "완료 사진의 역할별 최소 수량이 정책에 미달합니다.");
        var now = DateTime.UtcNow;
        revision.StatusCode = "SUBMITTED";
        revision.ProviderAttestationAt = now;
        revision.SubmittedAt = now;
        completion.StatusCode = "SUBMITTED";
        completion.FirstSubmittedAt ??= now;
        completion.UpdatedAt = now;
        completion.UpdatedByUserId = identity.UserId;
        transaction.StatusCode = "COMPLETION_SUBMITTED";
        transaction.UpdatedAt = now;
        transaction.UpdatedByUserId = identity.UserId;
        db.OutboxEvents.Add(NewOutbox(transaction, "COMPLETION_SUBMITTED", identity.UserId, now));
        await db.SaveChangesAsync(cancellationToken);
        return await BuildRevisionAsync(transaction, revision, cancellationToken);
    }

    public async Task<CompletionConfirmationResponse> ConfirmAsync(
        ClaimsPrincipal principal, Guid transactionId, ConfirmCompletionInput input, CancellationToken cancellationToken)
    {
        var identity = await CustomerIdentityAsync(principal, cancellationToken);
        var transaction = await db.Transactions.SingleOrDefaultAsync(
            item => item.PublicId == transactionId && item.CustomerProfileId == identity.ProfileId, cancellationToken)
            ?? throw NotFound();
        var existing = await db.CustomerConfirmations.AsNoTracking().SingleOrDefaultAsync(
            item => item.IdempotencyKey == input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.TransactionId != transaction.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 거래에 사용된 요청 키입니다.");
            return await ConfirmationResponseAsync(transaction, existing, cancellationToken);
        }
        var result = input.Result.Trim().ToUpperInvariant();
        if (result is not ("COMPLETED" or "REVISION_REQUESTED" or "DISPUTED"))
            throw Invalid("CONFIRMATION_RESULT_INVALID", "지원하지 않는 확인 결과입니다.", "result");
        if (result != "COMPLETED" && string.IsNullOrWhiteSpace(input.Comment))
            throw Invalid("CONFIRMATION_COMMENT_REQUIRED", "보완 요청 또는 이의 제기 사유를 입력해 주세요.", "comment");
        if (transaction.StatusCode == "COMPLETED" && result == "COMPLETED")
        {
            var completed = await db.CustomerConfirmations.AsNoTracking().SingleAsync(
                item => item.TransactionId == transaction.Id && item.ResultCode == "COMPLETED", cancellationToken);
            return await ConfirmationResponseAsync(transaction, completed, cancellationToken);
        }
        if (transaction.StatusCode != "COMPLETION_SUBMITTED")
            throw Conflict("TRANSACTION_STATE_CONFLICT", "제출된 작업완료 건만 확인할 수 있습니다.");
        var completion = await db.WorkCompletions.SingleAsync(item => item.TransactionId == transaction.Id, cancellationToken);
        var revision = await db.WorkCompletionRevisions.SingleOrDefaultAsync(
            item => item.PublicId == input.CompletionRevisionId && item.WorkCompletionId == completion.Id && item.RevisionNo == completion.LatestRevisionNo,
            cancellationToken) ?? throw Conflict("COMPLETION_REVISION_INVALID", "현재 제출된 완료 revision이 아닙니다.");
        if (revision.StatusCode != "SUBMITTED") throw Conflict("COMPLETION_REVISION_INVALID", "제출 상태의 완료 revision이 아닙니다.");
        var now = DateTime.UtcNow;
        await using var dbTransaction = await BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
        try
        {
            var confirmation = new CustomerConfirmation
            {
                TransactionId = transaction.Id, CompletionRevisionId = revision.Id, ResultCode = result,
                Comment = NullIfEmpty(input.Comment), ConfirmedAt = now, ConfirmedByUserId = identity.UserId,
                IdempotencyKey = input.IdempotencyKey.Trim(),
            };
            db.CustomerConfirmations.Add(confirmation);
            ServiceHistoryEntry? history = null;
            if (result == "COMPLETED")
            {
                transaction.StatusCode = "COMPLETED";
                transaction.CompletedAt = now;
                completion.StatusCode = "CONFIRMED";
                completion.ConfirmedAt = now;
                revision.StatusCode = "CONFIRMED";
                history = await CreateHistoryAsync(transaction, revision, identity.UserId, now, cancellationToken);
            }
            else
            {
                transaction.StatusCode = result;
                completion.StatusCode = result;
                revision.StatusCode = result;
            }
            transaction.UpdatedAt = now;
            transaction.UpdatedByUserId = identity.UserId;
            completion.UpdatedAt = now;
            completion.UpdatedByUserId = identity.UserId;
            db.OutboxEvents.Add(NewOutbox(transaction, $"COMPLETION_{result}", identity.UserId, now));
            await db.SaveChangesAsync(cancellationToken);
            if (dbTransaction is not null) await dbTransaction.CommitAsync(cancellationToken);
            return new CompletionConfirmationResponse(confirmation.PublicId, transaction.PublicId, revision.PublicId,
                result, transaction.StatusCode, history?.PublicId);
        }
        catch
        {
            if (dbTransaction is not null) await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenEvidenceAsync(
        ClaimsPrincipal principal, Guid fileId, CancellationToken cancellationToken)
    {
        var userPublicId = PrincipalId(principal);
        var row = await (
            from file in db.Files.AsNoTracking()
            join evidence in db.CompletionEvidenceFiles.AsNoTracking() on file.Id equals evidence.FileId
            join revision in db.WorkCompletionRevisions.AsNoTracking() on evidence.CompletionRevisionId equals revision.Id
            join completion in db.WorkCompletions.AsNoTracking() on revision.WorkCompletionId equals completion.Id
            join transaction in db.Transactions.AsNoTracking() on completion.TransactionId equals transaction.Id
            join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
            join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
            join providerUser in db.Users.AsNoTracking() on provider.UserId equals providerUser.Id
            join customerUser in db.Users.AsNoTracking() on customer.UserId equals customerUser.Id
            where file.PublicId == fileId && file.StatusCode == "ACTIVE" &&
                  (providerUser.PublicId == userPublicId || customerUser.PublicId == userPublicId)
            select file).SingleOrDefaultAsync(cancellationToken) ?? throw NotFound("FILE_NOT_FOUND");
        return (await fileStorage.OpenReadAsync(row.StorageKey, cancellationToken), row.ContentType, row.OriginalFileName);
    }

    private async Task<IReadOnlyList<WorkTransactionListItem>> ListAsync(long? providerId, long? customerId, CancellationToken cancellationToken)
    {
        var rows = await (from transaction in db.Transactions.AsNoTracking()
                          join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                          join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                          join middle in db.ServiceCategories.AsNoTracking() on category.ParentId equals middle.Id
                          join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                          join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                          where (!providerId.HasValue || transaction.ProviderProfileId == providerId) &&
                                (!customerId.HasValue || transaction.CustomerProfileId == customerId)
                          orderby transaction.CreatedAt descending
                          select new WorkTransactionListItem(transaction.PublicId, major.Name + " > " + middle.Name + " > " + category.Name,
                              area.AreaName, request.Title, transaction.AgreedAmount, transaction.CurrencyCode, transaction.StatusCode, transaction.CreatedAt))
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<WorkTransactionDetail> BuildDetailAsync(TransactionRecord transaction, bool providerView, CancellationToken cancellationToken)
    {
        var baseData = await (from request in db.ServiceRequests.AsNoTracking()
                              join category in db.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id
                              join middle in db.ServiceCategories.AsNoTracking() on category.ParentId equals middle.Id
                              join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                              join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                              join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
                              join customerUser in db.Users.AsNoTracking() on customer.UserId equals customerUser.Id
                              join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                              where request.Id == transaction.ServiceRequestId
                              select new { Request = request, CustomerPhone = customerUser.Phone, CategoryPath = major.Name + " > " + middle.Name + " > " + category.Name, area.AreaName, provider.BusinessName })
            .SingleAsync(cancellationToken);
        var items = await db.QuoteItems.AsNoTracking().Where(item => item.QuoteRevisionId == transaction.AcceptedQuoteRevisionId)
            .OrderBy(item => item.LineNo).Select(item => new WorkQuoteItem(item.LineNo, item.ItemName, item.Description,
                item.Quantity, item.UnitText, item.UnitPriceAmount, item.LineTotalAmount)).ToListAsync(cancellationToken);
        var answerRows = await (from answer in db.RequestAnswers.AsNoTracking()
                                join field in db.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                                where answer.ServiceRequestId == transaction.ServiceRequestId
                                orderby field.DisplayOrder, field.Id
                                select new { Answer = answer, Field = field }).ToListAsync(cancellationToken);
        var answers = answerRows.Select(row =>
        {
            var masked = providerView && (row.Field.ProviderVisibilityCode != "FULL" || row.Field.PreAcceptMaskingCode != "NONE");
            return new WorkRequestAnswer(row.Field.Label, masked ? null : ReadAnswer(row.Answer), masked);
        }).ToArray();
        var roleCodes = Array.Empty<string>();
        var completion = await db.WorkCompletions.AsNoTracking().SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken);
        WorkCompletionRevisionResponse? revisionResponse = null;
        if (completion is not null && completion.LatestRevisionNo > 0)
        {
            var revision = await db.WorkCompletionRevisions.AsNoTracking().SingleAsync(
                item => item.WorkCompletionId == completion.Id && item.RevisionNo == completion.LatestRevisionNo, cancellationToken);
            revisionResponse = await BuildRevisionAsync(transaction, revision, cancellationToken);
            roleCodes = revisionResponse.Evidence.Select(item => item.RoleCode).ToArray();
        }
        var policy = policyEvaluator.Evaluate(transaction.CompletionPolicySnapshotJson, roleCodes);
        var roles = await db.CompletionPhotoRoles.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id)
            .Select(item => new PhotoRoleOption(item.Code, item.Name, item.Description)).ToListAsync(cancellationToken);
        return new WorkTransactionDetail(transaction.PublicId, transaction.StatusCode, baseData.CategoryPath, baseData.AreaName,
            baseData.Request.Title, baseData.Request.Description, baseData.CustomerPhone, baseData.Request.DetailAddress,
            baseData.BusinessName, transaction.AgreedAmount, transaction.CurrencyCode,
            transaction.CreatedAt, transaction.StartedAt, transaction.CompletedAt, items, answers, policy, roles, revisionResponse);
    }

    private async Task<WorkCompletionRevisionResponse> BuildRevisionAsync(
        TransactionRecord transaction, WorkCompletionRevision revision, CancellationToken cancellationToken)
    {
        var evidence = await (from link in db.CompletionEvidenceFiles.AsNoTracking()
                              join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                              join role in db.CompletionPhotoRoles.AsNoTracking() on link.PhotoRoleId equals role.Id
                              where link.CompletionRevisionId == revision.Id && file.StatusCode == "ACTIVE"
                              orderby link.DisplayOrder
                              select new CompletionEvidenceResponse(file.PublicId, file.OriginalFileName, file.ContentType, file.SizeBytes,
                                  role.Code, role.Name, link.DisplayOrder, link.Description, "/api/v1/files/" + file.PublicId))
            .ToListAsync(cancellationToken);
        var (actualAmount, currency) = ReadActual(revision.ChecklistJson, transaction.CurrencyCode);
        var policy = policyEvaluator.Evaluate(transaction.CompletionPolicySnapshotJson, evidence.Select(item => item.RoleCode).ToArray());
        return new WorkCompletionRevisionResponse(revision.PublicId, revision.RevisionNo, revision.StatusCode, revision.WorkSummary,
            actualAmount, currency, revision.ProviderAttestationAt, revision.SubmittedAt, revision.RevisionReason, evidence, policy);
    }

    private async Task<ServiceHistoryEntry> CreateHistoryAsync(TransactionRecord transaction, WorkCompletionRevision revision,
        long userId, DateTime now, CancellationToken cancellationToken)
    {
        var categoryName = await db.ServiceCategories.AsNoTracking().Where(item => item.Id == transaction.CategoryId).Select(item => item.Name).SingleAsync(cancellationToken);
        var providerName = await db.ProviderProfiles.AsNoTracking().Where(item => item.Id == transaction.ProviderProfileId).Select(item => item.BusinessName).SingleAsync(cancellationToken);
        var evidence = await db.CompletionEvidenceFiles.AsNoTracking().Where(item => item.CompletionRevisionId == revision.Id)
            .Select(item => item.FileId).ToListAsync(cancellationToken);
        var (actualAmount, currency) = ReadActual(revision.ChecklistJson, transaction.CurrencyCode);
        var warrantyStart = DateOnly.FromDateTime(now);
        var history = new ServiceHistoryEntry
        {
            CustomerProfileId = transaction.CustomerProfileId, TransactionId = transaction.Id, SourceCompletionRevisionId = revision.Id,
            EventTypeCode = "COMPLETION", Title = categoryName + " 서비스 완료", Summary = revision.WorkSummary,
            ProviderNameSnapshot = providerName, CategoryNameSnapshot = categoryName, TotalAmountSnapshot = actualAmount,
            CurrencyCode = currency, CompletedAtSnapshot = now, WarrantyStartDate = warrantyStart,
            WarrantyEndDate = warrantyStart.AddDays(transaction.WarrantyDaysSnapshot),
            SnapshotJson = JsonSerializer.Serialize(new { transactionId = transaction.PublicId, completionRevisionId = revision.PublicId,
                revision.RevisionNo, revision.WorkSummary, actualAmount, currencyCode = currency, evidenceFileIds = evidence }),
            OccurredAt = now, IdempotencyKey = $"completion:{transaction.PublicId:N}:{revision.PublicId:N}", CreatedAt = now, CreatedByUserId = userId,
        };
        db.ServiceHistoryEntries.Add(history);
        await db.SaveChangesAsync(cancellationToken);
        var items = await db.QuoteItems.AsNoTracking().Where(item => item.QuoteRevisionId == transaction.AcceptedQuoteRevisionId).OrderBy(item => item.LineNo).ToListAsync(cancellationToken);
        db.ServiceHistoryItems.AddRange(items.Select(item => new ServiceHistoryItem
        {
            ServiceHistoryEntryId = history.Id, LineNo = item.LineNo, ItemName = item.ItemName, Description = item.Description,
            Quantity = item.Quantity, UnitText = item.UnitText, Amount = item.LineTotalAmount, CurrencyCode = item.CurrencyCode,
        }));
        return history;
    }

    private async Task<(string ContentType, string Extension, byte[] Hash)> ValidateImageAsync(IFormFile upload, CancellationToken cancellationToken)
    {
        if (upload.Length <= 0 || upload.Length > MaximumFileSize)
            throw Invalid("FILE_SIZE_INVALID", "이미지는 10MB 이하여야 합니다.", "file");
        if (!AllowedImages.TryGetValue(upload.ContentType, out var rule))
            throw Invalid("FILE_TYPE_INVALID", "JPEG, PNG 또는 WebP 이미지만 업로드할 수 있습니다.", "file");
        await using var stream = upload.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var signatureValid = rule.Signatures.Any(signature => bytes.AsSpan().StartsWith(signature));
        if (upload.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            signatureValid &= bytes.Length >= 12 && bytes.AsSpan(8, 4).SequenceEqual(Encoding.ASCII.GetBytes("WEBP"));
        if (!signatureValid) throw Invalid("FILE_SIGNATURE_INVALID", "파일 내용과 이미지 형식이 일치하지 않습니다.", "file");
        return (upload.ContentType.ToLowerInvariant(), rule.Extension, SHA256.HashData(bytes));
    }

    private Task<TransactionRecord> OwnedProviderTransactionAsync(long providerId, Guid publicId, CancellationToken cancellationToken) =>
        db.Transactions.SingleOrDefaultAsync(item => item.PublicId == publicId && item.ProviderProfileId == providerId, cancellationToken)
            .ContinueWith(task => task.Result ?? throw NotFound(), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    private async Task<IReadOnlyList<string>> EvidenceRoleCodesAsync(long revisionId, CancellationToken cancellationToken) =>
        await (from evidence in db.CompletionEvidenceFiles.AsNoTracking()
               join role in db.CompletionPhotoRoles.AsNoTracking() on evidence.PhotoRoleId equals role.Id
               where evidence.CompletionRevisionId == revisionId
               select role.Code).ToListAsync(cancellationToken);

    private async Task<Identity> ProviderIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
        await (from user in db.Users.AsNoTracking() join provider in db.ProviderProfiles.AsNoTracking() on user.Id equals provider.UserId
               where user.PublicId == PrincipalId(principal) && user.StatusCode == "ACTIVE"
               select new Identity(user.Id, provider.Id)).SingleOrDefaultAsync(cancellationToken)
        ?? throw Forbidden("PROVIDER_PROFILE_REQUIRED", "공급자 프로필을 찾을 수 없습니다.");

    private async Task<Identity> CustomerIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
        await (from user in db.Users.AsNoTracking() join customer in db.CustomerProfiles.AsNoTracking() on user.Id equals customer.UserId
               where user.PublicId == PrincipalId(principal) && user.StatusCode == "ACTIVE"
               select new Identity(user.Id, customer.Id)).SingleOrDefaultAsync(cancellationToken)
        ?? throw Forbidden("CUSTOMER_PROFILE_REQUIRED", "고객 프로필을 찾을 수 없습니다.");

    private async Task<CompletionConfirmationResponse> ConfirmationResponseAsync(TransactionRecord transaction,
        CustomerConfirmation confirmation, CancellationToken cancellationToken)
    {
        var revisionId = await db.WorkCompletionRevisions.AsNoTracking().Where(item => item.Id == confirmation.CompletionRevisionId)
            .Select(item => item.PublicId).SingleAsync(cancellationToken);
        var historyId = await db.ServiceHistoryEntries.AsNoTracking().Where(item => item.TransactionId == transaction.Id && item.EventTypeCode == "COMPLETION")
            .Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(cancellationToken);
        return new CompletionConfirmationResponse(confirmation.PublicId, transaction.PublicId, revisionId,
            confirmation.ResultCode, transaction.StatusCode, historyId);
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolation)
    {
        if (!db.Database.IsRelational() || db.Database.CurrentTransaction is not null) return null;
        return await db.Database.BeginTransactionAsync(isolation, cancellationToken);
    }

    private static OutboxEvent NewOutbox(TransactionRecord transaction, string eventType, long userId, DateTime now) => new()
    {
        AggregateType = "Transaction", AggregatePublicId = transaction.PublicId, EventType = eventType,
        PayloadJson = JsonSerializer.Serialize(new { transactionId = transaction.PublicId, status = transaction.StatusCode }),
        StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"{eventType.ToLowerInvariant()}:{transaction.PublicId:N}:{now.Ticks}", CreatedByUserId = userId,
    };

    private static (decimal Amount, string Currency) ReadActual(string? json, string fallbackCurrency)
    {
        if (string.IsNullOrWhiteSpace(json)) return (0, fallbackCurrency);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var amount = root.TryGetProperty("actualAmount", out var value) && value.TryGetDecimal(out var parsed) ? parsed : 0;
        var currency = root.TryGetProperty("currencyCode", out var code) ? code.GetString() ?? fallbackCurrency : fallbackCurrency;
        return (amount, currency);
    }

    private static object? ReadAnswer(RequestAnswer answer)
    {
        if (answer.ValueText is not null) return answer.ValueText;
        if (answer.ValueNumber.HasValue) return answer.ValueNumber.Value;
        if (answer.ValueBoolean.HasValue) return answer.ValueBoolean.Value;
        if (answer.ValueDate.HasValue) return answer.ValueDate.Value;
        if (answer.ValueDateTime.HasValue) return answer.ValueDateTime.Value;
        if (answer.ValueJson is not null) return JsonSerializer.Deserialize<object>(answer.ValueJson);
        return null;
    }

    private static void ValidateDraft(SaveCompletionDraftInput input)
    {
        if (string.IsNullOrWhiteSpace(input.WorkSummary)) throw Invalid("WORK_SUMMARY_REQUIRED", "작업 내용을 입력해 주세요.", "workSummary");
        if (input.ActualAmount < 0 || input.ActualAmount > 999_999_999_999_999m) throw Invalid("ACTUAL_AMOUNT_INVALID", "실제 작업금액을 확인해 주세요.", "actualAmount");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Length > 100) throw Invalid("IDEMPOTENCY_KEY_INVALID", "유효한 요청 키가 필요합니다.", "idempotencyKey");
        if (input.RevisionReason?.Length > 1000) throw Invalid("REVISION_REASON_TOO_LONG", "수정 사유는 1000자 이하여야 합니다.", "revisionReason");
    }

    private static Guid PrincipalId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Authenticated user identifier is invalid.");
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException NotFound(string code = "TRANSACTION_NOT_FOUND") => new(code, "대상을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    private static WorkBusinessException Forbidden(string code, string message) => new(code, message, StatusCodes.Status403Forbidden);
    private static WorkBusinessException Conflict(string code, string message) => new(code, message);
    private static WorkBusinessException Invalid(string code, string message, string field) =>
        new(code, message, StatusCodes.Status400BadRequest, new Dictionary<string, string[]> { [field] = [message] });
    private sealed record Identity(long UserId, long ProfileId);
}
