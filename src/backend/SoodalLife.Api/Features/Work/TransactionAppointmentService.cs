using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Features.Trust;

namespace SoodalLife.Api.Features.Work;

public sealed record CreateTransactionAppointmentInput(DateTime ScheduledStartAt, DateTime? ScheduledEndAt,
    int? EstimatedDurationMinutes, string? Memo, string IdempotencyKey);
public sealed record DecideAppointmentInput(string Decision, string? Reason, string IdempotencyKey, string RowVersion);
public sealed record RequestAppointmentChangeInput(DateTime? RequestedStartAt, DateTime? RequestedEndAt,
    string Reason, string IdempotencyKey, string ChangeType = "RESCHEDULE");
public sealed record DecideAppointmentChangeInput(string Decision, string? Note, string IdempotencyKey, string RowVersion);
public sealed record RequestTransactionCancellationInput(string Reason, string IdempotencyKey);
public sealed record DecideTransactionCancellationInput(string Decision, string? Note, string IdempotencyKey, string RowVersion);
public sealed record AppointmentChangeResponse(Guid Id, string ChangeType, DateTime? RequestedStartAt, DateTime? RequestedEndAt,
    string Reason, string Status, DateTime RequestedAt, DateTime? ProcessedAt, string RowVersion);
public sealed record AppointmentEventResponse(Guid Id, string EventType, string? BeforeStatus, string AfterStatus,
    DateTime? BeforeStartAt, DateTime? BeforeEndAt, DateTime? AfterStartAt, DateTime? AfterEndAt, string? Reason, DateTime OccurredAt);
public sealed record TransactionAppointmentResponse(Guid Id, Guid TransactionId, DateTime ScheduledStartAt,
    DateTime? ScheduledEndAt, int? EstimatedDurationMinutes, string? CustomerMemo, string? ProviderMemo,
    string Status, bool IsConfirmed, DateTime? ConfirmedAt, DateTime? CancelledAt, bool CanRespond,
    string RowVersion, IReadOnlyList<AppointmentChangeResponse> Changes, IReadOnlyList<AppointmentEventResponse> Events);
public sealed record TransactionCancellationResponse(Guid Id, Guid TransactionId, string Reason, string Status,
    DateTime RequestedAt, DateTime? ProcessedAt, string FeeRestoreStatus, bool CanRespond, string RowVersion);

public sealed class TransactionAppointmentService(SoodalLifeDbContext db,TrustRecalculationRunner trustRecalculation)
{
    public async Task<TransactionAppointmentResponse?> GetAsync(ClaimsPrincipal principal, Guid transactionId, CancellationToken token)
    {
        var party = await Party(principal, transactionId, tracking: false, token);
        var appointment = await db.TransactionAppointments.AsNoTracking().SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token);
        return appointment is null ? null : await Build(appointment, party, token);
    }

    public async Task<TransactionAppointmentResponse> ProposeAsync(ClaimsPrincipal principal, Guid transactionId, CreateTransactionAppointmentInput input, CancellationToken token)
    {
        ValidatePeriod(input.ScheduledStartAt, input.ScheduledEndAt); ValidateKey(input.IdempotencyKey);
        if (input.EstimatedDurationMinutes is <= 0 or > 10080) throw Invalid("APPOINTMENT_DURATION_INVALID", "예상 소요시간을 확인해 주세요.");
        if (input.Memo?.Length > 1000) throw Invalid("APPOINTMENT_MEMO_TOO_LONG", "메모는 1000자 이하여야 합니다.");
        var party = await Party(principal, transactionId, tracking: true, token);
        if (party.Transaction.StatusCode != "CREATED") throw Conflict("APPOINTMENT_TRANSACTION_STATE_INVALID", "작업 시작 전 거래에서만 일정을 제안할 수 있습니다.");
        var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token);
        if (appointment?.ProposalIdempotencyKey == input.IdempotencyKey) return await Build(appointment, party, token);
        if (appointment is not null && appointment.StatusCode is "PROPOSED" or "CONFIRMED") throw Conflict("APPOINTMENT_ALREADY_ACTIVE", "응답 대기 또는 확정 일정이 있습니다.");
        var now = DateTime.UtcNow; var beforeStatus = appointment?.StatusCode; var beforeStart = appointment?.ScheduledStartAt; var beforeEnd = appointment?.ScheduledEndAt;
        appointment ??= new TransactionAppointment { TransactionId = party.Transaction.Id, CreatedAt = now, CreatedByUserId = party.UserId };
        appointment.ScheduledStartAt = input.ScheduledStartAt.ToUniversalTime(); appointment.ScheduledEndAt = input.ScheduledEndAt?.ToUniversalTime();
        appointment.EstimatedDurationMinutes = input.EstimatedDurationMinutes; appointment.StatusCode = "PROPOSED";
        appointment.ProposalIdempotencyKey = input.IdempotencyKey.Trim(); appointment.ConfirmedAt = null; appointment.CancelledAt = null;
        appointment.CreatedByUserId = party.UserId;
        if (party.Role == "CUSTOMER") appointment.CustomerMemo = Clean(input.Memo); else appointment.ProviderMemo = Clean(input.Memo);
        appointment.UpdatedAt = now; appointment.UpdatedByUserId = party.UserId;
        if (appointment.Id == 0) db.TransactionAppointments.Add(appointment);
        IDbContextTransaction? databaseTransaction = null;
        if (db.Database.IsRelational()) databaseTransaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            await db.SaveChangesAsync(token);
            AddEvent(appointment, party.UserId, "APPOINTMENT_PROPOSED", beforeStatus, "PROPOSED", beforeStart, beforeEnd,
                appointment.ScheduledStartAt, appointment.ScheduledEndAt, input.Memo, input.IdempotencyKey, now);
            AddOutbox(party.Transaction, "TRANSACTION_APPOINTMENT_PROPOSED", party.UserId, input.IdempotencyKey, now);
            await db.SaveChangesAsync(token);
            if (databaseTransaction is not null) await databaseTransaction.CommitAsync(token);
        }
        catch
        {
            if (databaseTransaction is not null) await databaseTransaction.RollbackAsync(token);
            throw;
        }
        finally
        {
            if (databaseTransaction is not null) await databaseTransaction.DisposeAsync();
        }
        return await Build(appointment, party, token);
    }

    public async Task<TransactionAppointmentResponse> DecideProposalAsync(ClaimsPrincipal principal, Guid transactionId, DecideAppointmentInput input, CancellationToken token)
    {
        ValidateDecision(input.Decision); ValidateKey(input.IdempotencyKey); var party = await Party(principal, transactionId, true, token);
        var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token) ?? throw Conflict("APPOINTMENT_REQUIRED", "일정 제안이 없습니다.");
        var repeated = await db.TransactionAppointmentEvents.AsNoTracking().AnyAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (repeated) return await Build(appointment, party, token);
        if (appointment.StatusCode != "PROPOSED") throw Conflict("APPOINTMENT_NOT_AWAITING_DECISION", "응답할 일정 제안이 없습니다.");
        if (appointment.CreatedByUserId == party.UserId && appointment.ProposalIdempotencyKey is not null) throw Conflict("APPOINTMENT_SELF_DECISION", "제안자는 자신의 제안을 승인하거나 거절할 수 없습니다.");
        SetVersion(appointment, input.RowVersion); var now = DateTime.UtcNow; var target = input.Decision.ToUpperInvariant() == "APPROVE" ? "CONFIRMED" : "REJECTED";
        appointment.StatusCode = target; appointment.ConfirmedAt = target == "CONFIRMED" ? now : null; appointment.UpdatedAt = now; appointment.UpdatedByUserId = party.UserId;
        AddEvent(appointment, party.UserId, target == "CONFIRMED" ? "APPOINTMENT_CONFIRMED" : "APPOINTMENT_REJECTED", "PROPOSED", target,
            appointment.ScheduledStartAt, appointment.ScheduledEndAt, appointment.ScheduledStartAt, appointment.ScheduledEndAt, input.Reason, input.IdempotencyKey, now);
        AddOutbox(party.Transaction, $"TRANSACTION_{target}", party.UserId, input.IdempotencyKey, now); await SaveConcurrent(token);
        return await Build(appointment, party, token);
    }

    public async Task<AppointmentChangeResponse> RequestChangeAsync(ClaimsPrincipal principal, Guid transactionId, RequestAppointmentChangeInput input, CancellationToken token)
    {
        var type = input.ChangeType.ToUpperInvariant(); if (type is not ("RESCHEDULE" or "CANCEL")) throw Invalid("APPOINTMENT_CHANGE_TYPE_INVALID", "변경 종류를 확인해 주세요.");
        if (type == "RESCHEDULE") { if (!input.RequestedStartAt.HasValue) throw Invalid("APPOINTMENT_PERIOD_INVALID", "변경 시작 시간이 필요합니다."); ValidatePeriod(input.RequestedStartAt.Value, input.RequestedEndAt); }
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000) throw Invalid("APPOINTMENT_CHANGE_REASON_INVALID", "변경 또는 취소 사유를 입력해 주세요.");
        ValidateKey(input.IdempotencyKey); var party = await Party(principal, transactionId, true, token);
        var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token) ?? throw Conflict("APPOINTMENT_REQUIRED", "먼저 거래 일정을 등록해 주세요.");
        var existing = await db.TransactionAppointmentChangeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (existing is not null) { if (existing.TransactionAppointmentId != appointment.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 일정에 사용된 요청 키입니다."); return Map(existing); }
        if (appointment.StatusCode != "CONFIRMED" || party.Transaction.StatusCode is "COMPLETED" or "CANCELLED") throw Conflict("APPOINTMENT_CHANGE_NOT_ALLOWED", "확정된 활성 일정만 변경하거나 취소할 수 있습니다.");
        if (await db.TransactionAppointmentChangeRequests.AnyAsync(x => x.TransactionAppointmentId == appointment.Id && x.StatusCode == "REQUESTED", token)) throw Conflict("APPOINTMENT_CHANGE_PENDING", "처리 대기 중인 일정 요청이 있습니다.");
        var now = DateTime.UtcNow; var change = new TransactionAppointmentChangeRequest { TransactionAppointmentId = appointment.Id, RequestedByUserId = party.UserId,
            ChangeTypeCode = type, RequestedStartAt = input.RequestedStartAt?.ToUniversalTime(), RequestedEndAt = input.RequestedEndAt?.ToUniversalTime(), Reason = input.Reason.Trim(),
            StatusCode = "REQUESTED", RequestedAt = now, IdempotencyKey = input.IdempotencyKey.Trim(), CreatedAt = now };
        db.TransactionAppointmentChangeRequests.Add(change); AddOutbox(party.Transaction, type == "CANCEL" ? "TRANSACTION_APPOINTMENT_CANCELLATION_REQUESTED" : "TRANSACTION_APPOINTMENT_CHANGE_REQUESTED", party.UserId, input.IdempotencyKey, now);
        await db.SaveChangesAsync(token); return Map(change);
    }

    public async Task<AppointmentChangeResponse> DecideChangeAsync(ClaimsPrincipal principal, Guid transactionId, Guid requestId, DecideAppointmentChangeInput input, CancellationToken token)
    {
        ValidateDecision(input.Decision); ValidateKey(input.IdempotencyKey); var party = await Party(principal, transactionId, true, token);
        var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token) ?? throw Conflict("APPOINTMENT_REQUIRED", "거래 일정이 없습니다.");
        var change = await db.TransactionAppointmentChangeRequests.SingleOrDefaultAsync(x => x.PublicId == requestId && x.TransactionAppointmentId == appointment.Id, token) ?? throw NotFound();
        var priorEvent = await db.TransactionAppointmentEvents.AsNoTracking().AnyAsync(x => x.IdempotencyKey == input.IdempotencyKey, token); if (priorEvent) return Map(change);
        if (change.StatusCode != "REQUESTED") throw Conflict("APPOINTMENT_CHANGE_ALREADY_PROCESSED", "이미 처리된 일정 요청입니다.");
        if (change.RequestedByUserId == party.UserId) throw Conflict("APPOINTMENT_CHANGE_SELF_DECISION", "요청자는 자신의 요청을 처리할 수 없습니다.");
        SetVersion(change, input.RowVersion); var now = DateTime.UtcNow; var approve = input.Decision.ToUpperInvariant() == "APPROVE";
        change.StatusCode = approve ? "APPROVED" : "REJECTED"; change.ProcessedAt = now; change.ProcessedByUserId = party.UserId; change.ProcessingNote = Clean(input.Note);
        var beforeStatus = appointment.StatusCode; var beforeStart = appointment.ScheduledStartAt; var beforeEnd = appointment.ScheduledEndAt;
        if (approve && change.ChangeTypeCode == "RESCHEDULE") { appointment.ScheduledStartAt = change.RequestedStartAt!.Value; appointment.ScheduledEndAt = change.RequestedEndAt; }
        if (approve && change.ChangeTypeCode == "CANCEL") { appointment.StatusCode = "CANCELLED"; appointment.CancelledAt = now; }
        appointment.UpdatedAt = now; appointment.UpdatedByUserId = party.UserId;
        var eventType = approve ? (change.ChangeTypeCode == "CANCEL" ? "APPOINTMENT_CANCELLED" : "APPOINTMENT_CHANGED") : (change.ChangeTypeCode == "CANCEL" ? "APPOINTMENT_CANCELLATION_REJECTED" : "APPOINTMENT_CHANGE_REJECTED");
        AddEvent(appointment, party.UserId, eventType, beforeStatus, appointment.StatusCode, beforeStart, beforeEnd, appointment.ScheduledStartAt, appointment.ScheduledEndAt, input.Note, input.IdempotencyKey, now);
        AddOutbox(party.Transaction, $"TRANSACTION_{eventType}", party.UserId, input.IdempotencyKey, now); await SaveConcurrent(token); return Map(change);
    }

    public async Task<IReadOnlyList<TransactionCancellationResponse>> GetCancellationsAsync(ClaimsPrincipal principal, Guid transactionId, CancellationToken token)
    {
        var party = await Party(principal, transactionId, false, token);
        return await db.TransactionCancellationRequests.AsNoTracking().Where(x => x.TransactionId == party.Transaction.Id).OrderByDescending(x => x.RequestedAt)
            .Select(x => new TransactionCancellationResponse(x.PublicId, party.Transaction.PublicId, x.Reason, x.StatusCode, x.RequestedAt, x.ProcessedAt, "NOT_EVALUATED", x.StatusCode == "REQUESTED" && x.RequestedByUserId != party.UserId, Convert.ToBase64String(x.RowVersion))).ToListAsync(token);
    }

    public async Task<TransactionCancellationResponse> RequestCancellationAsync(ClaimsPrincipal principal, Guid transactionId, RequestTransactionCancellationInput input, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000) throw Invalid("TRANSACTION_CANCELLATION_REASON_INVALID", "취소 사유를 입력해 주세요."); ValidateKey(input.IdempotencyKey);
        var party = await Party(principal, transactionId, true, token); var existing = await db.TransactionCancellationRequests.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (existing is not null) { if (existing.TransactionId != party.Transaction.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 거래에 사용된 요청 키입니다."); return CancelMap(existing, party.Transaction.PublicId, party.UserId); }
        if (party.Transaction.StatusCode is "COMPLETION_SUBMITTED" or "REVISION_REQUESTED" or "COMPLETED" or "DISPUTED" or "CANCELLED") throw Conflict("TRANSACTION_CANCELLATION_NOT_ALLOWED", "현재 단계에서는 일반 취소 대신 완료 보완 또는 분쟁 절차를 이용해 주세요.");
        if (await db.TransactionCancellationRequests.AnyAsync(x => x.TransactionId == party.Transaction.Id && (x.StatusCode == "REQUESTED" || x.StatusCode == "ADMIN_REVIEW_REQUIRED"), token)) throw Conflict("TRANSACTION_CANCELLATION_PENDING", "처리 대기 중인 거래 취소 요청이 있습니다.");
        var now = DateTime.UtcNow; const string status = "REQUESTED";
        var row = new TransactionCancellationRequest { TransactionId = party.Transaction.Id, RequestedByUserId = party.UserId, Reason = input.Reason.Trim(), StatusCode = status,
            RequestedAt = now, IdempotencyKey = input.IdempotencyKey.Trim(), CreatedAt = now };
        db.TransactionCancellationRequests.Add(row); AddOutbox(party.Transaction, "TRANSACTION_CANCELLATION_REQUESTED", party.UserId, input.IdempotencyKey, now); await db.SaveChangesAsync(token);
        return CancelMap(row, party.Transaction.PublicId, party.UserId);
    }

    public async Task<TransactionCancellationResponse> DecideCancellationAsync(ClaimsPrincipal principal, Guid transactionId, Guid requestId, DecideTransactionCancellationInput input, CancellationToken token)
    {
        ValidateDecision(input.Decision); ValidateKey(input.IdempotencyKey); var party = await Party(principal, transactionId, true, token);
        var row = await db.TransactionCancellationRequests.SingleOrDefaultAsync(x => x.PublicId == requestId && x.TransactionId == party.Transaction.Id, token) ?? throw NotFound();
        if (row.StatusCode is "APPROVED" or "REJECTED")
        {
            if (row.DecisionIdempotencyKey == input.IdempotencyKey) return CancelMap(row, party.Transaction.PublicId, party.UserId);
            throw Conflict("TRANSACTION_CANCELLATION_ALREADY_PROCESSED", "이미 처리된 거래 취소 요청입니다.");
        }
        if (row.StatusCode != "REQUESTED" || party.Transaction.StatusCode is not ("CREATED" or "IN_PROGRESS"))
            throw Conflict("TRANSACTION_CANCELLATION_PARTY_DECISION_REQUIRED", "고객과 전문가가 직접 결정할 수 없는 상태입니다. 합의되지 않으면 분쟁 절차를 이용해 주세요.");
        if (row.RequestedByUserId == party.UserId) throw Conflict("TRANSACTION_CANCELLATION_SELF_DECISION", "요청자는 자신의 취소 요청을 처리할 수 없습니다.");
        SetVersion(row, input.RowVersion); var now = DateTime.UtcNow; var approve = input.Decision.ToUpperInvariant() == "APPROVE";
        row.StatusCode = approve ? "APPROVED" : "REJECTED"; row.ProcessedAt = now; row.ProcessedByUserId = party.UserId; row.ProcessingNote = Clean(input.Note); row.DecisionIdempotencyKey = input.IdempotencyKey.Trim();
        if (approve)
        {
            party.Transaction.StatusCode = "CANCELLED"; party.Transaction.CancelledAt = now; party.Transaction.CancellationReason = row.Reason; party.Transaction.UpdatedAt = now; party.Transaction.UpdatedByUserId = party.UserId;
            var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == party.Transaction.Id, token);
            if (appointment is not null && appointment.StatusCode is "PROPOSED" or "CONFIRMED")
            {
                var before = appointment.StatusCode; appointment.StatusCode = "CANCELLED"; appointment.CancelledAt = now; appointment.UpdatedAt = now; appointment.UpdatedByUserId = party.UserId;
                AddEvent(appointment, party.UserId, "APPOINTMENT_CANCELLED", before, "CANCELLED", appointment.ScheduledStartAt, appointment.ScheduledEndAt, appointment.ScheduledStartAt, appointment.ScheduledEndAt, row.Reason, input.IdempotencyKey, now);
            }
        }
        AddOutbox(party.Transaction, approve ? "TRANSACTION_CANCELLED" : "TRANSACTION_CANCELLATION_REJECTED", party.UserId, input.IdempotencyKey, now); await SaveConcurrent(token);
        if(approve)
        {
            var providerPublicId=await db.ProviderProfiles.AsNoTracking().Where(x=>x.Id==party.Transaction.ProviderProfileId).Select(x=>x.PublicId).SingleAsync(token);
            await trustRecalculation.RunAsync(providerPublicId,"TRANSACTION_CANCELLED",party.Transaction.PublicId,$"trust-transaction-cancelled:{party.Transaction.PublicId:N}:{row.PublicId:N}",token);
        }
        return CancelMap(row, party.Transaction.PublicId, party.UserId);
    }

    private async Task<TransactionAppointmentResponse> Build(TransactionAppointment appointment, PartyContext party, CancellationToken token)
    {
        var changes = await db.TransactionAppointmentChangeRequests.AsNoTracking().Where(x => x.TransactionAppointmentId == appointment.Id).OrderByDescending(x => x.RequestedAt).ToListAsync(token);
        var events = await db.TransactionAppointmentEvents.AsNoTracking().Where(x => x.TransactionAppointmentId == appointment.Id).OrderBy(x => x.OccurredAt).ToListAsync(token);
        return new(appointment.PublicId, party.Transaction.PublicId, appointment.ScheduledStartAt, appointment.ScheduledEndAt, appointment.EstimatedDurationMinutes,
            appointment.CustomerMemo, appointment.ProviderMemo, appointment.StatusCode, appointment.StatusCode == "CONFIRMED", appointment.ConfirmedAt, appointment.CancelledAt,
            appointment.StatusCode == "PROPOSED" && appointment.CreatedByUserId != party.UserId, Convert.ToBase64String(appointment.RowVersion), changes.Select(Map).ToArray(),
            events.Select(x => new AppointmentEventResponse(x.PublicId, x.EventTypeCode, x.BeforeStatusCode, x.AfterStatusCode, x.BeforeStartAt, x.BeforeEndAt, x.AfterStartAt, x.AfterEndAt, x.Reason, x.OccurredAt)).ToArray());
    }

    private async Task<PartyContext> Party(ClaimsPrincipal principal, Guid transactionId, bool tracking, CancellationToken token)
    {
        var publicId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : Guid.Empty;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == publicId && x.StatusCode == "ACTIVE", token) ?? throw new WorkBusinessException("USER_NOT_FOUND", "사용자를 확인할 수 없습니다.", 403);
        var row = await (from t in db.Transactions.AsNoTracking() join c in db.CustomerProfiles.AsNoTracking() on t.CustomerProfileId equals c.Id join p in db.ProviderProfiles.AsNoTracking() on t.ProviderProfileId equals p.Id
                         where t.PublicId == transactionId && (c.UserId == user.Id || p.UserId == user.Id) select new { Transaction = t, CustomerUserId = c.UserId, ProviderUserId = p.UserId }).SingleOrDefaultAsync(token) ?? throw NotFound();
        var transaction = tracking ? await db.Transactions.SingleAsync(x => x.Id == row.Transaction.Id, token) : row.Transaction;
        return new(transaction, user.Id, row.CustomerUserId == user.Id ? "CUSTOMER" : "PROVIDER", row.CustomerUserId, row.ProviderUserId);
    }

    private void AddEvent(TransactionAppointment a, long actor, string type, string? beforeStatus, string afterStatus, DateTime? beforeStart, DateTime? beforeEnd, DateTime? afterStart, DateTime? afterEnd, string? reason, string key, DateTime now) =>
        db.TransactionAppointmentEvents.Add(new() { TransactionAppointmentId = a.Id, ActorUserId = actor, EventTypeCode = type, BeforeStatusCode = beforeStatus, AfterStatusCode = afterStatus,
            BeforeStartAt = beforeStart, BeforeEndAt = beforeEnd, AfterStartAt = afterStart, AfterEndAt = afterEnd, Reason = Clean(reason), IdempotencyKey = key.Trim(), OccurredAt = now });
    private void AddOutbox(TransactionRecord t, string type, long actor, string key, DateTime now) => db.OutboxEvents.Add(new() { AggregateType = "Transaction", AggregatePublicId = t.PublicId,
        EventType = type, PayloadJson = JsonSerializer.Serialize(new { transactionId = t.PublicId }), StatusCode = "PENDING", OccurredAt = now, AvailableAt = now, IdempotencyKey = $"{type.ToLowerInvariant()}:{key.Trim()}", CreatedByUserId = actor });
    private async Task SaveConcurrent(CancellationToken token) { try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { throw Conflict("CONCURRENT_UPDATE", "다른 사용자가 먼저 처리했습니다. 새로고침 후 다시 시도해 주세요."); } }
    private static void SetVersion(object row, string value) { try { row.GetType().GetProperty("RowVersion")!.SetValue(row, Convert.FromBase64String(value)); } catch { throw Invalid("ROW_VERSION_INVALID", "변경 버전 값이 올바르지 않습니다."); } }
    private static AppointmentChangeResponse Map(TransactionAppointmentChangeRequest x) => new(x.PublicId, x.ChangeTypeCode, x.RequestedStartAt, x.RequestedEndAt, x.Reason, x.StatusCode, x.RequestedAt, x.ProcessedAt, Convert.ToBase64String(x.RowVersion));
    private static TransactionCancellationResponse CancelMap(TransactionCancellationRequest x, Guid transactionId, long userId) => new(x.PublicId, transactionId, x.Reason, x.StatusCode, x.RequestedAt, x.ProcessedAt, "NOT_EVALUATED", x.StatusCode == "REQUESTED" && x.RequestedByUserId != userId, Convert.ToBase64String(x.RowVersion));
    private static void ValidatePeriod(DateTime start, DateTime? end) { if (start == default || end.HasValue && end <= start) throw Invalid("APPOINTMENT_PERIOD_INVALID", "일정 시작과 종료를 확인해 주세요."); }
    private static void ValidateKey(string key) { if (string.IsNullOrWhiteSpace(key) || key.Length > 100) throw Invalid("IDEMPOTENCY_KEY_INVALID", "유효한 요청 키가 필요합니다."); }
    private static void ValidateDecision(string decision) { if (decision.ToUpperInvariant() is not ("APPROVE" or "REJECT")) throw Invalid("DECISION_INVALID", "승인 또는 거절을 선택해 주세요."); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException NotFound() => new("TRANSACTION_NOT_FOUND", "거래를 찾을 수 없습니다.", 404);
    private static WorkBusinessException Conflict(string code, string message) => new(code, message);
    private static WorkBusinessException Invalid(string code, string message) => new(code, message, 400);
    private sealed record PartyContext(TransactionRecord Transaction, long UserId, string Role, long CustomerUserId, long ProviderUserId);
}
