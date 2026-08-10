using System.Data;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Quotes;

public sealed class QuoteService(
    SoodalLifeDbContext dbContext,
    ProviderTradingEligibilityService eligibilityService,
    ProviderWalletService walletService)
{
    private const decimal MaximumAmount = 999_999_999_999_999m;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> AcceptanceLocks = new();

    public async Task<QuoteSubmissionReadinessResponse> GetSubmissionReadinessAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var dispatch = await FindOwnedDispatchAsync(identity.ProviderId, requestPublicId, cancellationToken);
        ValidateRequestOpen(dispatch.Request);
        var evaluation = await eligibilityService.EvaluateAsync(identity.ProviderId, dispatch.Request.CategoryId,
            dispatch.Request.AdministrativeAreaId, cancellationToken);
        var policy = await ResolveAcceptanceFeePolicyAsync(dispatch.Request, cancellationToken);
        var amount = FeeAmount(policy);
        var providerPublicId = await dbContext.ProviderProfiles.AsNoTracking().Where(item => item.Id == identity.ProviderId)
            .Select(item => item.PublicId).SingleAsync(cancellationToken);
        var wallet = await walletService.GetBalanceAsync(providerPublicId, amount, cancellationToken);
        var canSubmit = evaluation.IsEligible && wallet is { StatusCode: "ACTIVE", HasSufficientBalance: true };
        var reason = !evaluation.IsEligible ? evaluation.ReasonCode
            : wallet is null ? "WALLET_NOT_FOUND"
            : wallet.StatusCode != "ACTIVE" ? "WALLET_NOT_ACTIVE"
            : !wallet.HasSufficientBalance ? "WALLET_INSUFFICIENT_BALANCE"
            : null;
        return new(dispatch.Request.PublicId, providerPublicId, policy.PublicId, policy.PolicyVersion, amount,
            policy.CurrencyCode, wallet?.AvailableBalance ?? 0, wallet?.StatusCode ?? "NOT_FOUND", canSubmit, reason);
    }

    public async Task<QuoteDetailResponse?> GetProviderQuoteAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var quote = await dbContext.Quotes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProviderProfileId == identity.ProviderId &&
                                          dbContext.ServiceRequests.Any(request => request.Id == item.ServiceRequestId && request.PublicId == requestPublicId),
                cancellationToken);
        return quote is null ? null : await BuildDetailAsync(quote, cancellationToken);
    }

    public async Task<QuoteDetailResponse> CreateQuoteAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        SaveQuoteRevisionInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var dispatch = await FindOwnedDispatchAsync(identity.ProviderId, requestPublicId, cancellationToken);
        var existing = await dbContext.Quotes.SingleOrDefaultAsync(
            item => item.ServiceRequestId == dispatch.Request.Id && item.ProviderProfileId == identity.ProviderId,
            cancellationToken);
        if (existing is not null)
        {
            return await AddRevisionAsync(identity, existing, dispatch.Request, input, cancellationToken);
        }

        ValidateRequestOpen(dispatch.Request);
        var now = DateTime.UtcNow;
        var quote = new Quote
        {
            ServiceRequestId = dispatch.Request.Id,
            ProviderProfileId = identity.ProviderId,
            RequestDispatchId = dispatch.Dispatch.Id,
            StatusCode = "DRAFT",
            CreatedAt = now,
            CreatedByUserId = identity.UserId,
            UpdatedAt = now,
            UpdatedByUserId = identity.UserId,
        };

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Quotes.Add(quote);
            await dbContext.SaveChangesAsync(cancellationToken);
            await CreateRevisionAsync(identity, quote, dispatch.Request, input, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return (await BuildDetailAsync(quote, cancellationToken))!;
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task<QuoteDetailResponse> AddRevisionAsync(
        ClaimsPrincipal principal,
        Guid quotePublicId,
        SaveQuoteRevisionInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var quote = await dbContext.Quotes.SingleOrDefaultAsync(item => item.PublicId == quotePublicId, cancellationToken)
            ?? throw NotFound();
        if (quote.ProviderProfileId != identity.ProviderId) throw Forbidden("QUOTE_ACCESS_DENIED", "본인의 견적만 수정할 수 있습니다.");
        var request = await dbContext.ServiceRequests.SingleAsync(item => item.Id == quote.ServiceRequestId, cancellationToken);
        return await AddRevisionAsync(identity, quote, request, input, cancellationToken);
    }

    public async Task<QuoteDetailResponse> SubmitAsync(
        ClaimsPrincipal principal,
        Guid quotePublicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetProviderIdentityAsync(principal, cancellationToken);
        var quote = await dbContext.Quotes.SingleOrDefaultAsync(item => item.PublicId == quotePublicId, cancellationToken)
            ?? throw NotFound();
        if (quote.ProviderProfileId != identity.ProviderId) throw Forbidden("QUOTE_ACCESS_DENIED", "본인의 견적만 제출할 수 있습니다.");
        if (quote.StatusCode == "SUBMITTED") return (await BuildDetailAsync(quote, cancellationToken))!;
        if (quote.StatusCode != "DRAFT") throw Conflict("QUOTE_STATE_CONFLICT", "현재 상태에서는 견적을 제출할 수 없습니다.");

        var request = await dbContext.ServiceRequests.SingleAsync(item => item.Id == quote.ServiceRequestId, cancellationToken);
        ValidateRequestOpen(request);
        await ValidateProviderEligibilityAsync(identity.ProviderId, request, cancellationToken);
        var readiness = await BuildSubmissionReadinessAsync(identity.ProviderId, request, cancellationToken);
        if (!readiness.CanSubmit)
            throw Conflict(readiness.UnavailableReason ?? "QUOTE_SUBMISSION_NOT_ALLOWED", "현재 Wallet 잔액 또는 공급자 자격으로 견적을 제출할 수 없습니다.");
        var revision = await LatestRevisionAsync(quote.Id, cancellationToken)
            ?? throw Conflict("QUOTE_REVISION_REQUIRED", "제출할 견적 내용을 먼저 저장해 주세요.");
        var now = DateTime.UtcNow;
        if (revision.ValidUntil <= now) throw Conflict("QUOTE_EXPIRED", "견적 유효기간이 만료되었습니다.");

        var policy = await dbContext.CategoryPolicies.AsNoTracking().SingleAsync(item => item.Id == request.CategoryPolicyId, cancellationToken);
        var submittedCount = await dbContext.Quotes.CountAsync(item => item.ServiceRequestId == request.Id &&
            item.Id != quote.Id && (item.StatusCode == "SUBMITTED" || item.StatusCode == "ACCEPTED"), cancellationToken);
        if (submittedCount >= policy.MaxQuoteCount) throw Conflict("MAX_QUOTES_REACHED", "이 요청은 최대 견적 수에 도달했습니다.");

        var dispatch = await dbContext.RequestDispatches.SingleAsync(item => item.Id == quote.RequestDispatchId, cancellationToken);
        quote.StatusCode = "SUBMITTED";
        quote.SubmittedAt = now;
        quote.ExpiresAt = revision.ValidUntil;
        quote.UpdatedAt = now;
        quote.UpdatedByUserId = identity.UserId;
        dispatch.StatusCode = "RESPONDED";
        dispatch.RespondedAt = now;
        dbContext.OutboxEvents.Add(NewOutbox("Quote", quote.PublicId, "QUOTE_SUBMITTED", new
        {
            quoteId = quote.PublicId,
            requestId = request.PublicId,
            revisionId = revision.PublicId,
        }, $"quote-submitted:{quote.PublicId:N}:{revision.PublicId:N}", identity.UserId, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildDetailAsync(quote, cancellationToken))!;
    }

    public async Task<IReadOnlyList<QuoteListItemResponse>> GetCustomerQuotesAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var customer = await GetCustomerIdentityAsync(principal, cancellationToken);
        var request = await dbContext.ServiceRequests.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == requestPublicId && item.CustomerProfileId == customer.CustomerId, cancellationToken);
        if (request is null) throw NotFound();

        var visibleStatuses = new[] { "SUBMITTED", "ACCEPTED", "NOT_SELECTED" };
        var quotes = await (
                from quote in dbContext.Quotes.AsNoTracking()
                join provider in dbContext.ProviderProfiles.AsNoTracking() on quote.ProviderProfileId equals provider.Id
                where quote.ServiceRequestId == request.Id && visibleStatuses.Contains(quote.StatusCode)
                select new { Quote = quote, provider.BusinessName })
            .ToListAsync(cancellationToken);
        var result = new List<QuoteListItemResponse>();
        foreach (var row in quotes)
        {
            var revision = await LatestRevisionAsync(row.Quote.Id, cancellationToken);
            if (revision is null) continue;
            if (row.Quote.StatusCode == "SUBMITTED" && revision.ValidUntil <= DateTime.UtcNow) continue;
            result.Add(new QuoteListItemResponse(
                row.Quote.PublicId,
                row.BusinessName,
                row.Quote.StatusCode,
                revision.TotalAmount,
                revision.CurrencyCode,
                row.Quote.SubmittedAt,
                revision.RevisionNo,
                revision.ValidUntil,
                row.Quote.StatusCode == "ACCEPTED"));
        }

        return result.OrderBy(item => item.TotalAmount).ThenBy(item => item.SubmittedAt).ToArray();
    }

    public async Task<QuoteDetailResponse> GetCustomerQuoteDetailAsync(
        ClaimsPrincipal principal,
        Guid quotePublicId,
        CancellationToken cancellationToken)
    {
        var customer = await GetCustomerIdentityAsync(principal, cancellationToken);
        var visibleStatuses = new[] { "SUBMITTED", "ACCEPTED", "NOT_SELECTED" };
        var quote = await dbContext.Quotes.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == quotePublicId &&
            visibleStatuses.Contains(item.StatusCode) && dbContext.ServiceRequests.Any(request =>
                request.Id == item.ServiceRequestId && request.CustomerProfileId == customer.CustomerId), cancellationToken);
        if (quote is null) throw NotFound();
        var detail = (await BuildDetailAsync(quote, cancellationToken))!;
        if (quote.StatusCode == "SUBMITTED" && detail.Revision.ValidUntil <= DateTime.UtcNow)
            throw Conflict("QUOTE_EXPIRED", "견적 유효기간이 만료되었습니다.");
        return detail with { CanEdit = false, CanSubmit = false };
    }

    public async Task<AcceptQuoteResponse> AcceptAsync(
        ClaimsPrincipal principal,
        Guid quotePublicId,
        CancellationToken cancellationToken)
    {
        var customer = await GetCustomerIdentityAsync(principal, cancellationToken);
        var requestLockId = await (from quote in dbContext.Quotes.AsNoTracking()
                                   join request in dbContext.ServiceRequests.AsNoTracking() on quote.ServiceRequestId equals request.Id
                                   where quote.PublicId == quotePublicId && request.CustomerProfileId == customer.CustomerId
                                   select (Guid?)request.PublicId).SingleOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        var acceptanceLock = AcceptanceLocks.GetOrAdd(requestLockId, _ => new SemaphoreSlim(1, 1));
        await acceptanceLock.WaitAsync(cancellationToken);
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
            var quote = await dbContext.Quotes.SingleOrDefaultAsync(item => item.PublicId == quotePublicId, cancellationToken)
                ?? throw NotFound();
            var request = await dbContext.ServiceRequests.SingleAsync(item => item.Id == quote.ServiceRequestId, cancellationToken);
            if (request.CustomerProfileId != customer.CustomerId) throw NotFound();

            var existing = await dbContext.Transactions.SingleOrDefaultAsync(item => item.ServiceRequestId == request.Id, cancellationToken);
            if (existing is not null)
            {
                var acceptedRevision = await dbContext.QuoteRevisions.AsNoTracking().SingleAsync(item => item.Id == existing.AcceptedQuoteRevisionId, cancellationToken);
                var acceptedQuote = await dbContext.Quotes.AsNoTracking().SingleAsync(item => item.Id == acceptedRevision.QuoteId, cancellationToken);
                if (acceptedQuote.Id == quote.Id)
                    return new AcceptQuoteResponse(existing.PublicId, quote.PublicId, request.PublicId, existing.StatusCode,
                        existing.AgreedAmount, existing.CurrencyCode, existing.ActualChargedFeeAmount,
                        await GetLedgerPublicIdAsync(existing.WalletLedgerEntryId, cancellationToken));
                throw Conflict("REQUEST_ALREADY_ACCEPTED", "이미 다른 견적이 선택된 요청입니다.");
            }

            if (request.StatusCode != "OPEN") throw Conflict("REQUEST_ALREADY_ACCEPTED", "견적을 선택할 수 없는 요청 상태입니다.");
            if (quote.StatusCode != "SUBMITTED") throw Conflict("QUOTE_STATE_CONFLICT", "제출된 견적만 선택할 수 있습니다.");
            var revision = await LatestRevisionAsync(quote.Id, cancellationToken)
                ?? throw Conflict("QUOTE_REVISION_REQUIRED", "견적 revision이 없습니다.");
            var now = DateTime.UtcNow;
            if (revision.ValidUntil <= now || quote.ExpiresAt <= now) throw Conflict("QUOTE_EXPIRED", "견적 유효기간이 만료되었습니다.");
            var provider = await dbContext.ProviderProfiles.SingleAsync(item => item.Id == quote.ProviderProfileId, cancellationToken);
            await ValidateProviderEligibilityAsync(provider.Id, request, cancellationToken);
            var feePolicy = await ResolveAcceptanceFeePolicyAsync(request, cancellationToken);
            var feeAmount = FeeAmount(feePolicy);
            var balance = await walletService.GetBalanceAsync(provider.PublicId, feeAmount, cancellationToken);
            if (balance is null) throw Conflict("WALLET_NOT_FOUND", "공급자 Wallet을 찾을 수 없습니다.");
            if (balance.StatusCode != "ACTIVE") throw Conflict("WALLET_NOT_ACTIVE", "현재 사용할 수 없는 공급자 Wallet입니다.");
            if (!balance.HasSufficientBalance) throw Conflict("WALLET_INSUFFICIENT_BALANCE", "견적 채택 수수료를 차감할 Wallet 잔액이 부족합니다.");

            var items = await dbContext.QuoteItems.AsNoTracking().Where(item => item.QuoteRevisionId == revision.Id)
                .OrderBy(item => item.LineNo).ToListAsync(cancellationToken);
            var policy = await dbContext.CategoryPolicies.AsNoTracking().SingleAsync(item => item.Id == request.CategoryPolicyId, cancellationToken);
            var requirements = await (
                    from requirement in dbContext.CategoryCompletionPhotoRequirements.AsNoTracking()
                    join role in dbContext.CompletionPhotoRoles.AsNoTracking() on requirement.PhotoRoleId equals role.Id
                    where requirement.CategoryPolicyId == policy.Id
                    orderby requirement.DisplayOrder
                    select new { role.Code, role.Name, requirement.MinimumCount })
                .ToListAsync(cancellationToken);
            if (requirements.Sum(item => item.MinimumCount) != policy.RequiredCompletionPhotoCount)
                throw Conflict("COMPLETION_POLICY_INCOMPLETE", "카테고리 완료사진 정책이 완전하지 않아 거래를 생성할 수 없습니다.");

            var activeTrustScore = await dbContext.ProviderTrustScoreCurrent.AsNoTracking()
                .Where(value => value.ProviderProfileId == provider.Id && value.EvaluationStatusCode == "CALCULATED")
                .Select(value => value.Score).SingleOrDefaultAsync(cancellationToken);
            var transactionRecord = new TransactionRecord
            {
                ServiceRequestId = request.Id,
                AcceptedQuoteRevisionId = revision.Id,
                CustomerProfileId = customer.CustomerId,
                ProviderProfileId = provider.Id,
                CategoryId = request.CategoryId,
                CategoryFeePolicyId = feePolicy.Id,
                StatusCode = "CREATED",
                AgreedAmount = revision.TotalAmount,
                CurrencyCode = revision.CurrencyCode,
                QuoteSnapshotJson = JsonSerializer.Serialize(new
                {
                    quoteId = quote.PublicId,
                    revisionId = revision.PublicId,
                    revision.RevisionNo,
                    revision.Summary,
                    revision.Terms,
                    revision.SubtotalAmount,
                    revision.VatAmount,
                    revision.TotalAmount,
                    revision.CurrencyCode,
                    revision.EstimatedDurationText,
                    revision.AvailableStartAt,
                    revision.ValidUntil,
                    providerId = provider.PublicId,
                    provider.BusinessName,
                    items = items.Select(item => new { item.LineNo, item.ItemName, item.Description, item.Quantity, item.UnitText, item.UnitPriceAmount, item.LineTotalAmount, item.CurrencyCode }),
                }),
                CategoryPolicySnapshotJson = request.PolicySnapshotJson,
                CompletionPolicySnapshotJson = JsonSerializer.Serialize(new
                {
                    policyId = policy.PublicId,
                    policy.PolicyVersion,
                    totalRequiredPhotoCount = policy.RequiredCompletionPhotoCount,
                    policy.CompletionEvidenceRuleText,
                    roles = requirements,
                }),
                FeePolicySnapshotJson = JsonSerializer.Serialize(new
                {
                    policyId = feePolicy.PublicId,
                    feePolicy.PolicyVersion,
                    feePolicy.PolicyKindCode,
                    feePolicy.TransactionTypeCode,
                    feePolicy.CalculationMethodText,
                    calculatedFeeAmount = feeAmount,
                    feePolicy.CurrencyCode,
                    feePolicy.ChargeTimingText,
                    feePolicy.RestoreRuleText,
                    feePolicy.EffectiveFrom,
                    feePolicy.EffectiveTo,
                }),
                FeePolicyVersionSnapshot = feePolicy.PolicyVersion,
                FeePolicyKindSnapshot = feePolicy.PolicyKindCode,
                FeeTransactionTypeSnapshot = feePolicy.TransactionTypeCode,
                FeeCalculationMethodSnapshot = feePolicy.CalculationMethodText,
                CalculatedFeeAmount = feeAmount,
                FeeCurrencyCode = feePolicy.CurrencyCode,
                FeeChargeTimingSnapshot = feePolicy.ChargeTimingText,
                FeeRestoreRuleSnapshot = feePolicy.RestoreRuleText,
                WarrantyDaysSnapshot = policy.DefaultWarrantyDays,
                ProviderTrustScoreSnapshot = activeTrustScore,
                CreatedAt = now,
                CreatedByUserId = customer.UserId,
                UpdatedAt = now,
                UpdatedByUserId = customer.UserId,
            };
            dbContext.Transactions.Add(transactionRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            WalletOperationResponse debit;
            try
            {
                debit = await walletService.DebitFeeAsync(new DebitFeeCommand(provider.PublicId, transactionRecord.PublicId,
                    feePolicy.PublicId, feeAmount, $"quote-accept-fee:{request.PublicId:N}", "고객 견적 채택 수수료"), customer.UserId, cancellationToken);
            }
            catch (WalletOperationException exception)
            {
                throw Conflict(exception.BusinessCode, exception.Message);
            }
            var feeCharge = await dbContext.FeeCharges.SingleAsync(item => item.TransactionId == transactionRecord.Id &&
                item.CategoryFeePolicyId == feePolicy.Id, cancellationToken);
            transactionRecord.WalletLedgerEntryId = feeCharge.LedgerEntryId;
            transactionRecord.ActualChargedFeeAmount = feeCharge.FeeAmount;
            quote.StatusCode = "ACCEPTED";
            quote.AcceptedAt = now;
            quote.UpdatedAt = now;
            quote.UpdatedByUserId = customer.UserId;
            var otherQuotes = await dbContext.Quotes.Where(item => item.ServiceRequestId == request.Id && item.Id != quote.Id && item.StatusCode == "SUBMITTED")
                .ToListAsync(cancellationToken);
            foreach (var other in otherQuotes)
            {
                other.StatusCode = "NOT_SELECTED";
                other.UpdatedAt = now;
                other.UpdatedByUserId = customer.UserId;
            }
            request.StatusCode = "ACCEPTED";
            request.AcceptedAt = now;
            request.UpdatedAt = now;
            request.UpdatedByUserId = customer.UserId;
            dbContext.OutboxEvents.Add(NewOutbox("Transaction", transactionRecord.PublicId, "QUOTE_ACCEPTED", new
            {
                transactionId = transactionRecord.PublicId,
                requestId = request.PublicId,
                quoteId = quote.PublicId,
                revisionId = revision.PublicId,
            }, $"quote-accepted:{request.PublicId:N}", customer.UserId, now));

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new AcceptQuoteResponse(transactionRecord.PublicId, quote.PublicId, request.PublicId, transactionRecord.StatusCode,
                transactionRecord.AgreedAmount, transactionRecord.CurrencyCode, transactionRecord.ActualChargedFeeAmount, debit.LedgerEntryId);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw Conflict("QUOTE_ACCEPTANCE_CONFLICT", "다른 요청에서 견적 채택이 먼저 완료되었습니다.");
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
            acceptanceLock.Release();
        }
    }

    private async Task<QuoteDetailResponse> AddRevisionAsync(
        ProviderIdentity identity,
        Quote quote,
        ServiceRequest request,
        SaveQuoteRevisionInput input,
        CancellationToken cancellationToken)
    {
        if (quote.StatusCode is not ("DRAFT" or "SUBMITTED"))
            throw Conflict("QUOTE_STATE_CONFLICT", "채택 전 견적만 수정할 수 있습니다.");
        ValidateRequestOpen(request);
        if (quote.StatusCode == "SUBMITTED")
            await ValidateProviderEligibilityAsync(identity.ProviderId, request, cancellationToken);
        var existingRevision = await dbContext.QuoteRevisions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == input.IdempotencyKey, cancellationToken);
        if (existingRevision is not null)
        {
            if (existingRevision.QuoteId != quote.Id) throw Conflict("QUOTE_REVISION_CONFLICT", "이미 사용된 중복 방지 키입니다.");
            return (await BuildDetailAsync(quote, cancellationToken))!;
        }

        await CreateRevisionAsync(identity, quote, request, input, cancellationToken);
        if (quote.StatusCode == "SUBMITTED")
        {
            quote.SubmittedAt = DateTime.UtcNow;
            quote.ExpiresAt = input.ValidUntil.ToUniversalTime();
        }
        quote.UpdatedAt = DateTime.UtcNow;
        quote.UpdatedByUserId = identity.UserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildDetailAsync(quote, cancellationToken))!;
    }

    private async Task CreateRevisionAsync(
        ProviderIdentity identity,
        Quote quote,
        ServiceRequest request,
        SaveQuoteRevisionInput input,
        CancellationToken cancellationToken)
    {
        ValidateRevision(input, request);
        var nextRevision = (await dbContext.QuoteRevisions.Where(item => item.QuoteId == quote.Id)
            .MaxAsync(item => (int?)item.RevisionNo, cancellationToken) ?? 0) + 1;
        var subtotal = input.Items.Sum(item => RoundMoney(item.Quantity * item.UnitPriceAmount));
        var total = subtotal + input.VatAmount;
        if (subtotal > MaximumAmount || total > MaximumAmount) throw Invalid("QUOTE_AMOUNT_INVALID", "견적 금액이 허용 범위를 초과했습니다.", "items");
        var now = DateTime.UtcNow;
        var revision = new QuoteRevision
        {
            QuoteId = quote.Id,
            RevisionNo = nextRevision,
            Summary = input.Summary.Trim(),
            Terms = NullIfEmpty(input.Terms),
            SubtotalAmount = subtotal,
            VatAmount = input.VatAmount,
            TotalAmount = total,
            CurrencyCode = "KRW",
            EstimatedDurationText = NullIfEmpty(input.EstimatedDurationText),
            AvailableStartAt = input.AvailableStartAt?.ToUniversalTime(),
            ValidUntil = input.ValidUntil.ToUniversalTime(),
            RevisionReason = NullIfEmpty(input.RevisionReason),
            RevisionPurposeCode = NullIfEmpty(input.RevisionPurposeCode),
            SubmittedAt = now,
            SubmittedByUserId = identity.UserId,
            IdempotencyKey = input.IdempotencyKey.Trim(),
        };
        dbContext.QuoteRevisions.Add(revision);
        await dbContext.SaveChangesAsync(cancellationToken);
        var lineNo = 1;
        foreach (var item in input.Items)
        {
            dbContext.QuoteItems.Add(new QuoteItem
            {
                QuoteRevisionId = revision.Id,
                LineNo = lineNo++,
                ItemName = item.ItemName.Trim(),
                Description = NullIfEmpty(item.Description),
                Quantity = item.Quantity,
                UnitText = NullIfEmpty(item.UnitText),
                UnitPriceAmount = item.UnitPriceAmount,
                LineTotalAmount = RoundMoney(item.Quantity * item.UnitPriceAmount),
                CurrencyCode = "KRW",
                WorkTradeText = NullIfEmpty(item.WorkTradeText),
                SpaceText = NullIfEmpty(item.SpaceText),
                ItemCategoryCode = NullIfEmpty(item.ItemCategoryCode),
                MaterialSpecText = NullIfEmpty(item.MaterialSpecText),
                LaborNoteText = NullIfEmpty(item.LaborNoteText),
            });
        }
    }

    private static void ValidateRevision(SaveQuoteRevisionInput input, ServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(input.Summary) || input.Summary.Trim().Length > 1000)
            throw Invalid("QUOTE_INVALID", "견적 요약은 1~1000자로 입력해 주세요.", "summary");
        if (input.Terms?.Length > 20_000) throw Invalid("QUOTE_INVALID", "견적 조건이 너무 깁니다.", "terms");
        if (input.EstimatedDurationText?.Length > 200) throw Invalid("QUOTE_INVALID", "예상 작업기간은 200자 이하로 입력해 주세요.", "estimatedDurationText");
        if (input.RevisionReason?.Length > 1000) throw Invalid("QUOTE_INVALID", "수정 사유는 1000자 이하로 입력해 주세요.", "revisionReason");
        if (input.RevisionPurposeCode is not null && input.RevisionPurposeCode is not ("PRELIMINARY" or "POST_SITE_VISIT" or "CONTRACT_ESTIMATE" or "FINAL_SETTLEMENT"))
            throw Invalid("QUOTE_REVISION_PURPOSE_INVALID", "견적 작성 목적을 확인해 주세요.", "revisionPurposeCode");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Length > 100)
            throw Invalid("QUOTE_INVALID", "유효한 중복 방지 키가 필요합니다.", "idempotencyKey");
        if (input.Items is null || input.Items.Count == 0) throw Invalid("QUOTE_ITEMS_REQUIRED", "견적 항목을 한 개 이상 입력해 주세요.", "items");
        if (input.Items.Count > 100) throw Invalid("QUOTE_INVALID", "견적 항목은 100개 이하로 입력해 주세요.", "items");
        if (input.VatAmount < 0 || input.VatAmount > MaximumAmount) throw Invalid("QUOTE_AMOUNT_INVALID", "부가세 금액이 올바르지 않습니다.", "vatAmount");
        var validUntil = input.ValidUntil.ToUniversalTime();
        if (validUntil <= DateTime.UtcNow || request.ExpiresAt is null || validUntil > request.ExpiresAt)
            throw Invalid("QUOTE_VALIDITY_INVALID", "견적 유효기간은 현재 이후이면서 요청 마감 이하여야 합니다.", "validUntil");
        if (input.AvailableStartAt.HasValue && input.AvailableStartAt.Value.ToUniversalTime() < DateTime.UtcNow)
            throw Invalid("QUOTE_INVALID", "작업 가능 시작일은 현재 이후여야 합니다.", "availableStartAt");
        for (var index = 0; index < input.Items.Count; index++)
        {
            var item = input.Items[index];
            if (string.IsNullOrWhiteSpace(item.ItemName) || item.ItemName.Trim().Length > 200)
                throw Invalid("QUOTE_ITEM_INVALID", "견적 항목명을 입력해 주세요.", $"items.{index}.itemName");
            if (item.Description?.Length > 1000 || item.UnitText?.Length > 50)
                throw Invalid("QUOTE_ITEM_INVALID", "견적 항목 설명 또는 단위 길이를 확인해 주세요.", $"items.{index}");
            if (item.WorkTradeText?.Length > 200 || item.SpaceText?.Length > 200 || item.ItemCategoryCode?.Length > 100 ||
                item.MaterialSpecText?.Length > 2000 || item.LaborNoteText?.Length > 2000)
                throw Invalid("QUOTE_ITEM_INVALID", "인테리어 상세 견적 항목의 길이를 확인해 주세요.", $"items.{index}");
            if (item.Quantity <= 0 || item.Quantity > MaximumAmount || item.UnitPriceAmount < 0 || item.UnitPriceAmount > MaximumAmount)
                throw Invalid("QUOTE_AMOUNT_INVALID", "수량과 단가를 확인해 주세요.", $"items.{index}");
            if (decimal.Round(item.Quantity, 4) != item.Quantity || decimal.Round(item.UnitPriceAmount, 4) != item.UnitPriceAmount)
                throw Invalid("QUOTE_AMOUNT_INVALID", "수량과 단가는 소수점 4자리까지 입력할 수 있습니다.", $"items.{index}");
        }
    }

    private async Task ValidateProviderEligibilityAsync(long providerId, ServiceRequest request, CancellationToken cancellationToken)
    {
        var evaluation = await eligibilityService.EvaluateAsync(providerId, request.CategoryId, request.AdministrativeAreaId, cancellationToken);
        if (!evaluation.IsEligible)
            throw Forbidden(evaluation.ReasonCode ?? "PROVIDER_NOT_ELIGIBLE", "현재 이 요청에 견적을 제출하거나 채택될 수 없는 공급자 상태입니다.");
    }

    private async Task<QuoteSubmissionReadinessResponse> BuildSubmissionReadinessAsync(long providerId, ServiceRequest request, CancellationToken cancellationToken)
    {
        var evaluation = await eligibilityService.EvaluateAsync(providerId, request.CategoryId, request.AdministrativeAreaId, cancellationToken);
        var policy = await ResolveAcceptanceFeePolicyAsync(request, cancellationToken);
        var amount = FeeAmount(policy);
        var providerPublicId = await dbContext.ProviderProfiles.AsNoTracking().Where(item => item.Id == providerId)
            .Select(item => item.PublicId).SingleAsync(cancellationToken);
        var wallet = await walletService.GetBalanceAsync(providerPublicId, amount, cancellationToken);
        var canSubmit = evaluation.IsEligible && wallet is { StatusCode: "ACTIVE", HasSufficientBalance: true };
        var reason = !evaluation.IsEligible ? evaluation.ReasonCode : wallet is null ? "WALLET_NOT_FOUND"
            : wallet.StatusCode != "ACTIVE" ? "WALLET_NOT_ACTIVE" : !wallet.HasSufficientBalance ? "WALLET_INSUFFICIENT_BALANCE" : null;
        return new(request.PublicId, providerPublicId, policy.PublicId, policy.PolicyVersion, amount, policy.CurrencyCode,
            wallet?.AvailableBalance ?? 0, wallet?.StatusCode ?? "NOT_FOUND", canSubmit, reason);
    }

    private async Task<CategoryFeePolicy> ResolveAcceptanceFeePolicyAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var transactionType = await dbContext.CategoryPolicies.AsNoTracking().Where(item => item.Id == request.CategoryPolicyId)
            .Select(item => item.TransactionTypeCode).SingleAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await dbContext.CategoryFeePolicies.AsNoTracking()
            .Where(item => item.CategoryId == request.CategoryId && item.TransactionTypeCode == transactionType && item.IsActive &&
                item.EffectiveFrom <= today && (item.EffectiveTo == null || item.EffectiveTo > today))
            .OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw Conflict("FEE_POLICY_NOT_FOUND", "현재 적용할 수수료정책이 없습니다.");
    }

    private static decimal FeeAmount(CategoryFeePolicy policy) => policy.FeeAmount is > 0
        ? policy.FeeAmount.Value
        : throw Conflict("FEE_POLICY_NOT_CHARGEABLE", "견적 채택 시 차감할 수수료가 확정되지 않았습니다.");

    private async Task<Guid?> GetLedgerPublicIdAsync(long? ledgerId, CancellationToken cancellationToken) => ledgerId.HasValue
        ? await dbContext.WalletLedgerEntries.AsNoTracking().Where(item => item.Id == ledgerId.Value).Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(cancellationToken)
        : null;

    private async Task<OwnedDispatch> FindOwnedDispatchAsync(long providerId, Guid requestPublicId, CancellationToken cancellationToken)
    {
        var row = await (
                from dispatch in dbContext.RequestDispatches
                join request in dbContext.ServiceRequests on dispatch.ServiceRequestId equals request.Id
                where request.PublicId == requestPublicId && dispatch.ProviderProfileId == providerId && dispatch.StatusCode != "EXPIRED"
                select new OwnedDispatch(dispatch, request))
            .SingleOrDefaultAsync(cancellationToken);
        return row ?? throw Forbidden("REQUEST_NOT_DISPATCHED", "본인에게 배포된 요청에만 견적을 작성할 수 있습니다.");
    }

    private static void ValidateRequestOpen(ServiceRequest request)
    {
        if (request.StatusCode != "OPEN" || request.ExpiresAt is null || request.ExpiresAt <= DateTime.UtcNow)
            throw Conflict("REQUEST_NOT_OPEN", "공개 중이고 마감 전인 요청에만 견적을 작성할 수 있습니다.");
    }

    private async Task<QuoteDetailResponse?> BuildDetailAsync(Quote quote, CancellationToken cancellationToken)
    {
        var revision = await LatestRevisionAsync(quote.Id, cancellationToken);
        if (revision is null) return null;
        var requestPublicId = await dbContext.ServiceRequests.AsNoTracking().Where(item => item.Id == quote.ServiceRequestId)
            .Select(item => item.PublicId).SingleAsync(cancellationToken);
        var providerName = await dbContext.ProviderProfiles.AsNoTracking().Where(item => item.Id == quote.ProviderProfileId)
            .Select(item => item.BusinessName).SingleAsync(cancellationToken);
        var items = await dbContext.QuoteItems.AsNoTracking().Where(item => item.QuoteRevisionId == revision.Id)
            .OrderBy(item => item.LineNo)
            .Select(item => new QuoteItemResponse(item.LineNo, item.ItemName, item.Description, item.Quantity, item.UnitText,
                item.UnitPriceAmount, item.LineTotalAmount, item.CurrencyCode, item.WorkTradeText, item.SpaceText,
                item.ItemCategoryCode, item.MaterialSpecText, item.LaborNoteText))
            .ToListAsync(cancellationToken);
        var transactionId = await dbContext.Transactions.AsNoTracking().Where(item => item.AcceptedQuoteRevisionId == revision.Id)
            .Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(cancellationToken);
        return new QuoteDetailResponse(
            quote.PublicId,
            requestPublicId,
            providerName,
            quote.StatusCode,
            quote.SubmittedAt,
            quote.AcceptedAt,
            quote.ExpiresAt,
            quote.StatusCode is "DRAFT" or "SUBMITTED",
            quote.StatusCode == "DRAFT",
            new QuoteRevisionResponse(revision.PublicId, revision.RevisionNo, revision.Summary, revision.Terms,
                revision.SubtotalAmount, revision.VatAmount, revision.TotalAmount, revision.CurrencyCode,
                revision.EstimatedDurationText, revision.AvailableStartAt, revision.ValidUntil,
                revision.RevisionReason, revision.SubmittedAt, items, revision.RevisionPurposeCode),
            transactionId);
    }

    private Task<QuoteRevision?> LatestRevisionAsync(long quoteId, CancellationToken cancellationToken) =>
        dbContext.QuoteRevisions.AsNoTracking().Where(item => item.QuoteId == quoteId)
            .OrderByDescending(item => item.RevisionNo).FirstOrDefaultAsync(cancellationToken);

    private async Task<ProviderIdentity> GetProviderIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var publicId = PrincipalId(principal);
        var identity = await (
                from user in dbContext.Users
                join provider in dbContext.ProviderProfiles on user.Id equals provider.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new ProviderIdentity(user.Id, provider.Id))
            .SingleOrDefaultAsync(cancellationToken);
        return identity ?? throw Forbidden("PROVIDER_PROFILE_REQUIRED", "공급자 프로필을 찾을 수 없습니다.");
    }

    private async Task<CustomerIdentity> GetCustomerIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var publicId = PrincipalId(principal);
        var identity = await (
                from user in dbContext.Users
                join customer in dbContext.CustomerProfiles on user.Id equals customer.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new CustomerIdentity(user.Id, customer.Id))
            .SingleOrDefaultAsync(cancellationToken);
        return identity ?? throw Forbidden("CUSTOMER_PROFILE_REQUIRED", "고객 프로필을 찾을 수 없습니다.");
    }

    private static Guid PrincipalId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated user identifier is invalid.");

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel? isolation = null)
    {
        if (!dbContext.Database.IsRelational() || dbContext.Database.CurrentTransaction is not null) return null;
        return isolation.HasValue
            ? await dbContext.Database.BeginTransactionAsync(isolation.Value, cancellationToken)
            : await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static OutboxEvent NewOutbox(string aggregateType, Guid aggregateId, string eventType, object payload,
        string idempotencyKey, long userId, DateTime now) => new()
    {
        AggregateType = aggregateType,
        AggregatePublicId = aggregateId,
        EventType = eventType,
        PayloadJson = JsonSerializer.Serialize(payload),
        StatusCode = "PENDING",
        OccurredAt = now,
        AvailableAt = now,
        AttemptCount = 0,
        IdempotencyKey = idempotencyKey,
        CreatedByUserId = userId,
    };

    private static decimal RoundMoney(decimal value) => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static QuoteBusinessException NotFound() => new("QUOTE_NOT_FOUND", "견적을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
    private static QuoteBusinessException Forbidden(string code, string message) => new(code, message, StatusCodes.Status403Forbidden);
    private static QuoteBusinessException Conflict(string code, string message) => new(code, message);
    private static QuoteBusinessException Invalid(string code, string message, string field) =>
        new(code, message, StatusCodes.Status400BadRequest, new Dictionary<string, string[]> { [field] = [message] });

    private sealed record ProviderIdentity(long UserId, long ProviderId);
    private sealed record CustomerIdentity(long UserId, long CustomerId);
    private sealed record OwnedDispatch(RequestDispatch Dispatch, ServiceRequest Request);
}
