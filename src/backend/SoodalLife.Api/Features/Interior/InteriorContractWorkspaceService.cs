using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Interior;

public sealed class InteriorContractWorkspaceService(SoodalLifeDbContext db, ProviderWalletService wallets)
{
    private const string ProviderDeclaration = "고객과 체결한 최종 계약서이며 입력한 관리정보가 계약서와 일치합니다.";

    public async Task<Guid> RegisterByProvider(Guid projectId, RegisterInteriorContractRecordRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var actor = await Provider(principal, token);
        var project = await db.InteriorProjects.SingleOrDefaultAsync(x => x.PublicId == projectId && x.SelectedContractorProviderId == actor.ProviderId, token)
            ?? throw NotFound("INTERIOR_PROJECT_NOT_FOUND", "최종 선택된 전문가의 인테리어 프로젝트를 찾을 수 없습니다.");
        InteriorContract? correction = null;
        if (project.CurrentContractId.HasValue)
            correction = await db.InteriorContracts.SingleAsync(x => x.Id == project.CurrentContractId.Value, token);
        var isCorrection = project.StatusCode == "CONTRACT_PENDING" && correction?.StatusCode == "CORRECTION_REQUIRED";
        if (project.StatusCode != "ESTIMATE_READY" && !isCorrection)
            throw Conflict("INTERIOR_CONTRACT_RECORD_STATE_INVALID", "현재 단계에서는 계약자료를 등록하거나 보완할 수 없습니다.");
        var revision = await db.QuoteRevisions.SingleAsync(x => x.Id == project.CurrentQuoteRevisionId, token);
        if (input.ContractAmount != revision.TotalAmount || !string.Equals(input.CurrencyCode, revision.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw Conflict("INTERIOR_SELECTED_QUOTE_TERMS_MISMATCH", "최초 계약금액과 통화는 고객이 채택한 견적과 일치해야 합니다. 변경이 필요하면 견적을 먼저 수정해 주세요.");
        if (input.PlannedCompletionDate < input.PlannedStartDate || input.ContractSignedDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            throw Conflict("INTERIOR_CONTRACT_RECORD_INVALID", "계약 체결일과 공사기간을 확인해 주세요.");
        var plans = input.PaymentPlans?.OrderBy(x => x.SequenceNo).ToArray() ?? [];
        if (plans.Length == 0 || plans.Any(x => x.Amount < 0) || plans.Sum(x => x.Amount) != input.ContractAmount || plans.Select(x => x.SequenceNo).Distinct().Count() != plans.Length)
            throw Conflict("INTERIOR_PAYMENT_PLAN_TOTAL_MISMATCH", "지급계획을 한 건 이상 입력하고 합계가 계약금액과 일치하도록 해 주세요.");
        var ids = (input.ContractFileIds ?? []).Distinct().ToArray();
        if (ids.Length == 0) throw Conflict("INTERIOR_SIGNED_CONTRACT_REQUIRED", "고객과 전문가가 서명한 계약서 파일을 등록해 주세요.");
        var files = await db.Files.Where(x => ids.Contains(x.PublicId) && x.UploadedByUserId == actor.UserId && x.StatusCode == "ACTIVE").ToListAsync(token);
        if (files.Count != ids.Length) throw NotFound("INTERIOR_CONTRACT_FILE_NOT_FOUND", "전문가가 직접 업로드한 활성 계약서 파일만 등록할 수 있습니다.");
        if (await db.InteriorProjectEvents.AnyAsync(x => x.IdempotencyKey == input.IdempotencyKey, token))
            return await db.InteriorContracts.Where(x => x.InteriorProjectId == project.Id).Select(x => x.PublicId).SingleAsync(token);

        var now = DateTime.UtcNow;
        if (isCorrection)
        {
            var correctedContract = correction!;
            foreach (var document in await db.InteriorContractDocuments.Where(x => x.InteriorContractId == correctedContract.Id && x.IsCurrent).ToListAsync(token))
                document.IsCurrent = false;
            db.InteriorPaymentPlans.RemoveRange(await db.InteriorPaymentPlans.Where(x => x.InteriorContractId == correctedContract.Id).ToListAsync(token));
            correctedContract.ContractVersion += 1;
            correctedContract.StatusCode = "CUSTOMER_REVIEW_PENDING";
            correctedContract.ContractAmount = input.ContractAmount;
            correctedContract.CurrencyCode = revision.CurrencyCode;
            correctedContract.ScopeSnapshotJson = input.ScopeSnapshotJson;
            correctedContract.ScheduleSnapshotJson = input.ScheduleSnapshotJson;
            correctedContract.WarrantySnapshotJson = input.WarrantySnapshotJson;
            correctedContract.PlannedStartDate = input.PlannedStartDate;
            correctedContract.PlannedCompletionDate = input.PlannedCompletionDate;
            correctedContract.ContractSignedDate = input.ContractSignedDate;
            correctedContract.RegisteredByProviderAt = now;
            correctedContract.ProviderAgreedAt = now;
            correctedContract.ProviderDeclarationText = ProviderDeclaration;
            correctedContract.CustomerAgreedAt = null;
            correctedContract.CustomerMismatchReason = null;
            correctedContract.UpdatedAt = now;
            correctedContract.UpdatedByUserId = actor.UserId;
            foreach (var plan in plans) db.InteriorPaymentPlans.Add(new InteriorPaymentPlan
            {
                InteriorContractId = correctedContract.Id, SequenceNo = plan.SequenceNo, PaymentName = plan.Name.Trim(), PlannedAmount = plan.Amount,
                PlannedDueDate = plan.DueDate, ConditionText = plan.Condition, CreatedAt = now, CreatedByUserId = actor.UserId,
                UpdatedAt = now, UpdatedByUserId = actor.UserId,
            });
            for (var i = 0; i < files.Count; i++) db.InteriorContractDocuments.Add(new InteriorContractDocument
            {
                InteriorContractId = correctedContract.Id, FileId = files[i].Id, DisplayOrder = i, CreatedAt = now, CreatedByUserId = actor.UserId,
            });
            db.InteriorContractVersions.Add(new InteriorContractVersion
            {
                InteriorContractId = correctedContract.Id, VersionNo = correctedContract.ContractVersion, ContractAmount = correctedContract.ContractAmount, CurrencyCode = correctedContract.CurrencyCode,
                ScopeSnapshotJson = correctedContract.ScopeSnapshotJson, ScheduleSnapshotJson = correctedContract.ScheduleSnapshotJson,
                PaymentPlanSnapshotJson = JsonSerializer.Serialize(plans), WarrantySnapshotJson = correctedContract.WarrantySnapshotJson,
                ProviderAgreedAt = now, CreatedAt = now, CreatedByUserId = actor.UserId,
            });
            Event(project, "CONTRACT_RECORD_REPLACED", input.IdempotencyKey, actor.UserId, new { contractId = correctedContract.PublicId, correctedContract.ContractVersion, documentCount = files.Count }, now);
            project.ContractExpiryPhaseCode = "CUSTOMER_REVIEW";
            project.ContractActionDueAt = now.AddDays(InteriorContractExpiryService.ConfirmationDays);
            project.ContractExpiryReminderSentAt = null;
            project.ContractExpiryPausedAt = null;
            project.ContractExpiryPauseReasonCode = null;
            project.UpdatedAt = now;
            project.UpdatedByUserId = actor.UserId;
            await db.SaveChangesAsync(token);
            return correctedContract.PublicId;
        }
        var contract = new InteriorContract
        {
            InteriorProjectId = project.Id, CustomerProfileId = project.CustomerProfileId, ProviderProfileId = actor.ProviderId,
            ContractVersion = 1, StatusCode = "CUSTOMER_REVIEW_PENDING", SelectedQuoteRevisionId = revision.Id,
            ContractAmount = input.ContractAmount, CurrencyCode = revision.CurrencyCode, ScopeSnapshotJson = input.ScopeSnapshotJson,
            QuoteSnapshotJson = JsonSerializer.Serialize(new { revisionId = revision.PublicId, revision.RevisionNo, revision.TotalAmount, revision.CurrencyCode }),
            ScheduleSnapshotJson = input.ScheduleSnapshotJson, WarrantySnapshotJson = input.WarrantySnapshotJson,
            ProviderTrustScoreSnapshot = project.ContractorTrustScoreSnapshot, PlannedStartDate = input.PlannedStartDate,
            PlannedCompletionDate = input.PlannedCompletionDate, ContractSignedDate = input.ContractSignedDate,
            RegisteredByProviderAt = now, ProviderAgreedAt = now, ProviderDeclarationText = ProviderDeclaration,
            CreatedAt = now, CreatedByUserId = actor.UserId, UpdatedAt = now, UpdatedByUserId = actor.UserId,
        };
        db.InteriorContracts.Add(contract);
        await db.SaveChangesAsync(token);
        foreach (var plan in plans) db.InteriorPaymentPlans.Add(new InteriorPaymentPlan
        {
            InteriorContractId = contract.Id, SequenceNo = plan.SequenceNo, PaymentName = plan.Name.Trim(), PlannedAmount = plan.Amount,
            PlannedDueDate = plan.DueDate, ConditionText = plan.Condition, CreatedAt = now, CreatedByUserId = actor.UserId,
            UpdatedAt = now, UpdatedByUserId = actor.UserId,
        });
        for (var i = 0; i < files.Count; i++) db.InteriorContractDocuments.Add(new InteriorContractDocument
        {
            InteriorContractId = contract.Id, FileId = files[i].Id, DisplayOrder = i, CreatedAt = now, CreatedByUserId = actor.UserId,
        });
        db.InteriorContractVersions.Add(new InteriorContractVersion
        {
            InteriorContractId = contract.Id, VersionNo = 1, ContractAmount = contract.ContractAmount, CurrencyCode = contract.CurrencyCode,
            ScopeSnapshotJson = contract.ScopeSnapshotJson, ScheduleSnapshotJson = contract.ScheduleSnapshotJson,
            PaymentPlanSnapshotJson = JsonSerializer.Serialize(plans), WarrantySnapshotJson = contract.WarrantySnapshotJson,
            ProviderAgreedAt = now, CreatedAt = now, CreatedByUserId = actor.UserId,
        });
        project.CurrentContractId = contract.Id; project.StatusCode = "CONTRACT_PENDING";
        project.ContractExpiryPhaseCode = "CUSTOMER_REVIEW"; project.ContractActionDueAt = now.AddDays(InteriorContractExpiryService.ConfirmationDays);
        project.ContractExpiryReminderSentAt = null; project.ContractExpiryPausedAt = null; project.ContractExpiryPauseReasonCode = null;
        project.UpdatedAt = now; project.UpdatedByUserId = actor.UserId;
        Event(project, "CONTRACT_RECORD_REGISTERED", input.IdempotencyKey, actor.UserId, new { contractId = contract.PublicId, documentCount = files.Count }, now);
        await db.SaveChangesAsync(token);
        return contract.PublicId;
    }

    public async Task ReviewByCustomer(Guid projectId, Guid contractId, ReviewInteriorContractRecordRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var actor = await Customer(principal, token);
        var project = await db.InteriorProjects.SingleOrDefaultAsync(x => x.PublicId == projectId && x.CustomerProfileId == actor.ProfileId, token)
            ?? throw NotFound("INTERIOR_PROJECT_NOT_FOUND", "인테리어 프로젝트를 찾을 수 없습니다.");
        var contract = await db.InteriorContracts.SingleOrDefaultAsync(x => x.PublicId == contractId && x.InteriorProjectId == project.Id, token)
            ?? throw NotFound("CONTRACT_NOT_FOUND", "계약자료를 찾을 수 없습니다.");
        if (await db.InteriorProjectEvents.AnyAsync(x => x.IdempotencyKey == input.IdempotencyKey, token)) return;
        if (contract.StatusCode is "EFFECTIVE" or "TERMINATED") throw Conflict("INTERIOR_CONTRACT_RECORD_FINALIZED", "이미 최종 처리된 계약자료입니다.");
        if (!await db.InteriorContractDocuments.AnyAsync(x => x.InteriorContractId == contract.Id && x.IsCurrent, token))
            throw Conflict("INTERIOR_SIGNED_CONTRACT_REQUIRED", "확인할 서명 계약서가 없습니다.");
        var now = DateTime.UtcNow;
        if (!input.Matches)
        {
            if (string.IsNullOrWhiteSpace(input.MismatchReason)) throw Conflict("INTERIOR_CONTRACT_MISMATCH_REASON_REQUIRED", "일치하지 않는 항목과 재등록 요청 사유를 입력해 주세요.");
            contract.StatusCode = "CORRECTION_REQUIRED"; contract.CustomerMismatchReason = input.MismatchReason.Trim(); contract.CustomerAgreedAt = null;
            project.ContractExpiryPausedAt ??= now; project.ContractExpiryPauseReasonCode = "CUSTOMER_CORRECTION_REQUEST";
            Event(project, "CONTRACT_RECORD_CORRECTION_REQUESTED", input.IdempotencyKey, actor.UserId, new { contractId }, now);
        }
        else
        {
            contract.CustomerMismatchReason = null; contract.CustomerAgreedAt = now; contract.StatusCode = "EFFECTIVE"; contract.EffectiveAt = now;
            project.StatusCode = "CONTRACTED";
            await CaptureFee(project, actor.UserId, now, token);
            project.ContractExpiryPhaseCode = null; project.ContractActionDueAt = null; project.ContractExpiryReminderSentAt = null;
            project.ContractExpiryPausedAt = null; project.ContractExpiryPauseReasonCode = null;
            Event(project, "CONTRACT_RECORD_CONFIRMED", input.IdempotencyKey, actor.UserId, new { contractId, meaning = "PARTY_RECORD_MATCH_CONFIRMED" }, now);
        }
        contract.UpdatedAt = now; contract.UpdatedByUserId = actor.UserId; project.UpdatedAt = now; project.UpdatedByUserId = actor.UserId;
        await db.SaveChangesAsync(token);
    }

    private async Task CaptureFee(InteriorProject project, long actorUserId, DateTime now, CancellationToken token)
    {
        if (project.FeeCapturedAt.HasValue) return;
        if (project.FeeAssessmentStatusCode != "RESERVED_PENDING_CONTRACT" || !project.ReservedFeeAmount.HasValue || !project.FeeReservationWalletId.HasValue)
            throw Conflict("INTERIOR_FEE_RESERVATION_REQUIRED", "계약 성사 수수료 예약 상태를 확인할 수 없습니다.");
        var wallet = await db.ProviderWallets.SingleAsync(x => x.Id == project.FeeReservationWalletId.Value, token);
        var amount = project.ReservedFeeAmount.Value;
        if (wallet.StatusCode != "ACTIVE" || wallet.ReservedBalance < amount) throw Conflict("WALLET_RESERVED_BALANCE_INVALID", "예약된 수수료 잔액을 확인할 수 없습니다.");
        var transaction = await db.Transactions.SingleAsync(x => x.ServiceRequestId == project.ServiceRequestId && x.AcceptedQuoteRevisionId == project.CurrentQuoteRevisionId, token);
        if (!transaction.CategoryFeePolicyId.HasValue) throw Conflict("INTERIOR_FEE_POLICY_NOT_FOUND", "거래의 수수료 정책 Snapshot을 확인할 수 없습니다.");
        wallet.ReservedBalance -= amount; wallet.UpdatedAt = now; wallet.UpdatedByUserId = actorUserId;
        var ledger = wallets.AddLedger(wallet, transaction.Id, "USE", -amount, $"interior-fee-capture:{project.PublicId:N}", "계약자료 상호 확인 완료에 따른 인테리어 중개수수료 확정", "FEE_CHARGE", transaction.PublicId, null, now, actorUserId);
        await db.SaveChangesAsync(token);
        db.FeeCharges.Add(new FeeCharge
        {
            TransactionId = transaction.Id, CategoryFeePolicyId = transaction.CategoryFeePolicyId.Value, WalletId = wallet.Id,
            LedgerEntryId = ledger.Id, FeeAmount = amount, SupplyAmount = Math.Abs(ledger.SupplyAmount),
            VatAmount = Math.Abs(ledger.VatAmount), IsVatIncluded = true, ChargedAt = now, RestoreStatusCode = "NOT_RESTORED",
            CreatedAt = now, CreatedByUserId = actorUserId, UpdatedAt = now, UpdatedByUserId = actorUserId,
        });
        transaction.WalletLedgerEntryId = ledger.Id; transaction.ActualChargedFeeAmount = amount; transaction.UpdatedAt = now; transaction.UpdatedByUserId = actorUserId;
        project.FeeCapturedAt = now; project.FeeAssessmentStatusCode = "ASSESSED";
    }

    private void Event(InteriorProject project, string type, string key, long userId, object data, DateTime now) => db.InteriorProjectEvents.Add(new InteriorProjectEvent
    { InteriorProjectId = project.Id, EventTypeCode = type, ActorUserId = userId, IdempotencyKey = key.Trim(), EventDataJson = JsonSerializer.Serialize(data), OccurredAt = now, CreatedAt = now });
    private async Task<ProviderActor> Provider(ClaimsPrincipal principal, CancellationToken token)
    {
        var publicId = Guid.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return await (from u in db.Users where u.PublicId == publicId join p in db.ProviderProfiles on u.Id equals p.UserId select new ProviderActor(u.Id, p.Id)).SingleAsync(token);
    }
    private async Task<CustomerActor> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        var publicId = Guid.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return await (from u in db.Users where u.PublicId == publicId join p in db.CustomerProfiles on u.Id equals p.UserId select new CustomerActor(u.Id, p.Id)).SingleAsync(token);
    }
    private static InteriorBusinessException Conflict(string code, string message) => new(409, code, message);
    private static InteriorBusinessException NotFound(string code, string message) => new(404, code, message);
    private sealed record ProviderActor(long UserId, long ProviderId);
    private sealed record CustomerActor(long UserId, long ProfileId);
}
