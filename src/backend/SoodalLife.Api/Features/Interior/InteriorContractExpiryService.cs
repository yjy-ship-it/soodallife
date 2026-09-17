using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Interior;

public sealed class InteriorContractExpiryService(SoodalLifeDbContext db, ProviderWalletService wallets)
{
    public const int ConfirmationDays = 7;
    public const int ReminderDays = 2;
    private static readonly string[] ClosedDisputeStatuses = ["RESOLVED", "CLOSED"];

    public async Task<int> ProcessDueAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var ids = await db.InteriorProjects.AsNoTracking()
            .Where(x => x.FeeAssessmentStatusCode == "RESERVED_PENDING_CONTRACT" && x.ContractActionDueAt != null)
            .OrderBy(x => x.ContractActionDueAt).Select(x => x.Id).Take(200).ToListAsync(token);
        var processed = 0;
        foreach (var id in ids)
        {
            var project = await db.InteriorProjects.SingleAsync(x => x.Id == id, token);
            if (!project.ContractActionDueAt.HasValue) continue;
            var dueAt = project.ContractActionDueAt.Value;
            var contract = project.CurrentContractId.HasValue
                ? await db.InteriorContracts.SingleOrDefaultAsync(x => x.Id == project.CurrentContractId.Value, token) : null;
            var disputeOpen = await db.DisputeCases.AsNoTracking().AnyAsync(x => x.InteriorProjectId == project.Id && !ClosedDisputeStatuses.Contains(x.StatusCode), token);
            var correctionOpen = contract?.StatusCode == "CORRECTION_REQUIRED";
            if (disputeOpen || correctionOpen)
            {
                if (!project.ContractExpiryPausedAt.HasValue)
                {
                    project.ContractExpiryPausedAt = now;
                    project.ContractExpiryPauseReasonCode = disputeOpen ? "ACTIVE_DISPUTE" : "CUSTOMER_CORRECTION_REQUEST";
                    AddProjectEvent(project, "CONTRACT_EXPIRY_PAUSED", $"contract-expiry-paused:{project.PublicId:N}:{project.ContractExpiryPauseReasonCode}", null,
                        new { project.ContractExpiryPauseReasonCode, project.ContractActionDueAt }, now);
                    await db.SaveChangesAsync(token);
                    processed++;
                }
                continue;
            }
            if (project.ContractExpiryPausedAt.HasValue)
            {
                project.ContractActionDueAt = project.ContractActionDueAt!.Value.Add(now - project.ContractExpiryPausedAt.Value);
                project.ContractExpiryPausedAt = null;
                project.ContractExpiryPauseReasonCode = null;
                project.ContractExpiryReminderSentAt = null;
                AddProjectEvent(project, "CONTRACT_EXPIRY_RESUMED", $"contract-expiry-resumed:{project.PublicId:N}:{now.Ticks}", null,
                    new { project.ContractActionDueAt }, now);
                await db.SaveChangesAsync(token);
                processed++;
                continue;
            }
            if (!project.ContractExpiryReminderSentAt.HasValue && now >= dueAt.AddDays(-ReminderDays))
            {
                if (await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == "INTERIOR_CONTRACT_DEADLINE_REMINDER" && x.IsActive, token))
                {
                    await AddReminderEvents(project, now, token);
                    project.ContractExpiryReminderSentAt = now;
                    await db.SaveChangesAsync(token);
                    processed++;
                }
            }
            if (now >= dueAt)
            {
                var reason = project.ContractExpiryPhaseCode == "CUSTOMER_REVIEW"
                    ? "CUSTOMER_CONFIRMATION_DEADLINE_EXPIRED" : "PROVIDER_CONTRACT_SUBMISSION_DEADLINE_EXPIRED";
                await ReleaseSelectionAsync(project, reason, false, null, now, token);
                processed++;
            }
        }
        return processed;
    }

    public async Task ReleaseSelectionAsync(InteriorProject project, string reasonCode, bool cancelProject, long? actorUserId, DateTime now, CancellationToken token)
    {
        if (project.FeeAssessmentStatusCode != "RESERVED_PENDING_CONTRACT") return;
        if (!project.ReservedFeeAmount.HasValue || !project.FeeReservationWalletId.HasValue)
            throw new InvalidOperationException("Reserved interior fee metadata is incomplete.");
        await using var transaction = await wallets.BeginTransactionAsync(token);
        var providerUserId = project.SelectedContractorProviderId.HasValue
            ? await db.ProviderProfiles.Where(x => x.Id == project.SelectedContractorProviderId.Value).Select(x => (long?)x.UserId).SingleOrDefaultAsync(token) : null;
        var customerUserId = await db.CustomerProfiles.Where(x => x.Id == project.CustomerProfileId).Select(x => x.UserId).SingleAsync(token);
        var wallet = await db.ProviderWallets.SingleAsync(x => x.Id == project.FeeReservationWalletId.Value, token);
        var amount = project.ReservedFeeAmount.Value;
        if (wallet.ReservedBalance < amount) throw new InvalidOperationException("Reserved wallet balance is insufficient.");
        wallet.ReservedBalance -= amount;
        wallet.AvailableBalance += amount;
        wallet.UpdatedAt = now;
        wallet.UpdatedByUserId = actorUserId;
        var cycle = project.FeeReservationLedgerEntryId?.ToString() ?? project.FeeReservedAt?.Ticks.ToString() ?? "legacy";
        var ledger = wallets.AddLedger(wallet, null, "RELEASE", amount, $"interior-fee-release:{project.PublicId:N}:{cycle}",
            ReleaseReason(reasonCode), "INTERIOR_FEE_RESERVATION", project.PublicId, null, now, actorUserId);
        await wallets.SaveWithConcurrencyAsync(token);
        project.FeeReleaseLedgerEntryId = ledger.Id;
        project.FeeReleasedAt = now;
        project.FeeReleaseReasonCode = reasonCode;
        project.FeeAssessmentStatusCode = "RELEASED";
        project.ContractExpiredAt = reasonCode.EndsWith("DEADLINE_EXPIRED", StringComparison.Ordinal) ? now : null;
        if (project.CurrentContractId.HasValue)
        {
            var contract = await db.InteriorContracts.SingleOrDefaultAsync(x => x.Id == project.CurrentContractId.Value, token);
            if (contract is not null && contract.StatusCode != "EFFECTIVE")
            {
                contract.StatusCode = reasonCode.EndsWith("DEADLINE_EXPIRED", StringComparison.Ordinal) ? "EXPIRED" : "TERMINATED";
                contract.TerminatedAt = now;
                contract.TerminationReason = ReleaseReason(reasonCode);
                contract.UpdatedAt = now;
                contract.UpdatedByUserId = actorUserId;
            }
        }
        foreach (var participant in await db.InteriorProjectParticipants.Where(x => x.InteriorProjectId == project.Id && x.RoleCode == "PRIMARY_CONTRACTOR" && x.StatusCode == "ACTIVE").ToListAsync(token))
        {
            participant.StatusCode = cancelProject ? "CANCELLED" : "ENDED";
            participant.IsPrimary = false;
            participant.EffectiveTo = now;
            participant.UpdatedAt = now;
            participant.UpdatedByUserId = actorUserId;
        }
        project.SelectedContractorProviderId = null;
        project.ContractorSelectedAt = null;
        project.CurrentQuoteRevisionId = null;
        project.CurrentContractId = null;
        project.ContractorTrustScoreSnapshot = null;
        project.ContractExpiryPhaseCode = null;
        project.ContractActionDueAt = null;
        project.ContractExpiryReminderSentAt = null;
        project.ContractExpiryPausedAt = null;
        project.ContractExpiryPauseReasonCode = null;
        project.StatusCode = cancelProject ? "CANCELLED" : "ESTIMATE_READY";
        project.UpdatedAt = now;
        project.UpdatedByUserId = actorUserId;
        AddProjectEvent(project, "CONTRACT_FEE_RESERVATION_RELEASED", $"contract-fee-release:{project.PublicId:N}:{cycle}", actorUserId,
            new { reasonCode, amount, ledgerId = ledger.PublicId, cancelProject }, now);
        await AddOutboxIfTemplate(project, "INTERIOR_CONTRACT_RESERVATION_RELEASED", $"interior-release:{project.PublicId:N}:{cycle}",
            new[] { customerUserId }.Concat(providerUserId.HasValue ? [providerUserId.Value] : []).ToArray(), now, actorUserId, token);
        await wallets.SaveWithConcurrencyAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
    }

    private async Task AddReminderEvents(InteriorProject project, DateTime now, CancellationToken token)
    {
        var customer = await db.CustomerProfiles.Where(x => x.Id == project.CustomerProfileId).Select(x => x.UserId).SingleAsync(token);
        var provider = project.SelectedContractorProviderId.HasValue
            ? await db.ProviderProfiles.Where(x => x.Id == project.SelectedContractorProviderId.Value).Select(x => (long?)x.UserId).SingleOrDefaultAsync(token) : null;
        await AddOutboxIfTemplate(project, "INTERIOR_CONTRACT_DEADLINE_REMINDER", $"interior-contract-reminder:{project.PublicId:N}",
            new[] { customer }.Concat(provider.HasValue ? [provider.Value] : []).ToArray(), now, null, token);
        AddProjectEvent(project, "CONTRACT_DEADLINE_REMINDER_CREATED", $"contract-deadline-reminder:{project.PublicId:N}", null,
            new { project.ContractExpiryPhaseCode, project.ContractActionDueAt }, now);
    }

    private async Task AddOutboxIfTemplate(InteriorProject project, string eventType, string key, long[] recipients, DateTime now, long? actor, CancellationToken token)
    {
        if (!await db.NotificationTemplates.AsNoTracking().AnyAsync(x => x.EventTypeCode == eventType && x.IsActive, token)) return;
        if (await db.OutboxEvents.AsNoTracking().AnyAsync(x => x.IdempotencyKey == key, token)) return;
        db.OutboxEvents.Add(new OutboxEvent { AggregateType = "InteriorProject", AggregatePublicId = project.PublicId, EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(new { projectId = project.PublicId, recipientUserIds = recipients, due_at = project.ContractActionDueAt }),
            StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = key, CreatedByUserId = actor });
    }

    private void AddProjectEvent(InteriorProject project, string type, string key, long? actor, object data, DateTime now) =>
        db.InteriorProjectEvents.Add(new InteriorProjectEvent { InteriorProjectId = project.Id, EventTypeCode = type, ActorUserId = actor,
            IdempotencyKey = key, EventDataJson = JsonSerializer.Serialize(data), OccurredAt = now, CreatedAt = now });

    private static string ReleaseReason(string code) => code switch
    {
        "CUSTOMER_CONFIRMATION_DEADLINE_EXPIRED" => "고객 계약 확인 기한(7일) 만료에 따른 예약 수수료 해제",
        "PROVIDER_CONTRACT_SUBMISSION_DEADLINE_EXPIRED" => "전문가 계약자료 미제출 기한(7일) 만료에 따른 예약 수수료 해제",
        "CUSTOMER_PROVIDER_SELECTION_RELEASED" => "고객의 전문가 선택 해제에 따른 예약 수수료 해제",
        "CUSTOMER_PROJECT_CANCELLED" => "고객의 프로젝트 취소에 따른 예약 수수료 해제",
        _ => "인테리어 계약 예약 수수료 해제"
    };
}
