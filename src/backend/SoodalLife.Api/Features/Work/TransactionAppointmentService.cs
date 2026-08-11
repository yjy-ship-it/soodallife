using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Work;

public sealed record CreateTransactionAppointmentInput(DateTime ScheduledStartAt, DateTime? ScheduledEndAt,
    int? EstimatedDurationMinutes, string? CustomerMemo);
public sealed record RequestAppointmentChangeInput(DateTime RequestedStartAt, DateTime? RequestedEndAt,
    string Reason, string IdempotencyKey);
public sealed record AppointmentChangeResponse(Guid Id, DateTime RequestedStartAt, DateTime? RequestedEndAt,
    string Reason, string Status, DateTime RequestedAt, DateTime? ProcessedAt);
public sealed record TransactionAppointmentResponse(Guid Id, Guid TransactionId, DateTime ScheduledStartAt,
    DateTime? ScheduledEndAt, int? EstimatedDurationMinutes, string? CustomerMemo, string? ProviderMemo,
    string Status, bool IsConfirmed, DateTime? ConfirmedAt, DateTime? CancelledAt,
    IReadOnlyList<AppointmentChangeResponse> Changes);

public sealed class TransactionAppointmentService(SoodalLifeDbContext db)
{
    public async Task<TransactionAppointmentResponse?> GetAsync(ClaimsPrincipal principal, Guid transactionId, CancellationToken token)
    {
        var user = await ActiveUser(principal, token);
        var transaction = await AccessibleTransaction(user.Id, transactionId, token) ?? throw NotFound();
        var appointment = await db.TransactionAppointments.AsNoTracking().SingleOrDefaultAsync(x => x.TransactionId == transaction.Id, token);
        return appointment is null ? null : await Build(appointment, transaction.PublicId, token);
    }

    public async Task<TransactionAppointmentResponse> CreateAsync(ClaimsPrincipal principal, Guid transactionId,
        CreateTransactionAppointmentInput input, CancellationToken token)
    {
        ValidatePeriod(input.ScheduledStartAt, input.ScheduledEndAt);
        if (input.EstimatedDurationMinutes is <= 0 or > 10080) throw Invalid("APPOINTMENT_DURATION_INVALID", "예상 소요시간을 확인해 주세요.");
        if (input.CustomerMemo?.Length > 1000) throw Invalid("APPOINTMENT_MEMO_TOO_LONG", "고객 메모는 1000자 이하여야 합니다.");
        var identity = await Customer(principal, token);
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.PublicId == transactionId && x.CustomerProfileId == identity.ProfileId, token) ?? throw NotFound();
        var existing = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == transaction.Id, token);
        if (existing is not null) return await Build(existing, transaction.PublicId, token);
        if (transaction.StatusCode is "COMPLETED" or "CANCELLED") throw Conflict("APPOINTMENT_TRANSACTION_CLOSED", "종료된 거래에는 일정을 만들 수 없습니다.");
        var now = DateTime.UtcNow;
        var appointment = new TransactionAppointment
        {
            TransactionId = transaction.Id, ScheduledStartAt = input.ScheduledStartAt.ToUniversalTime(),
            ScheduledEndAt = input.ScheduledEndAt?.ToUniversalTime(), EstimatedDurationMinutes = input.EstimatedDurationMinutes,
            CustomerMemo = Clean(input.CustomerMemo), StatusCode = "PROPOSED", CreatedAt = now,
            CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId
        };
        db.TransactionAppointments.Add(appointment);
        db.OutboxEvents.Add(Event(transaction, "TRANSACTION_APPOINTMENT_PROPOSED", identity.UserId, now));
        await db.SaveChangesAsync(token);
        return await Build(appointment, transaction.PublicId, token);
    }

    public async Task<AppointmentChangeResponse> RequestChangeAsync(ClaimsPrincipal principal, Guid transactionId,
        RequestAppointmentChangeInput input, CancellationToken token)
    {
        ValidatePeriod(input.RequestedStartAt, input.RequestedEndAt);
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000) throw Invalid("APPOINTMENT_CHANGE_REASON_INVALID", "변경 사유를 입력해 주세요.");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Length > 100) throw Invalid("IDEMPOTENCY_KEY_INVALID", "유효한 요청 키가 필요합니다.");
        var identity = await Customer(principal, token);
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.PublicId == transactionId && x.CustomerProfileId == identity.ProfileId, token) ?? throw NotFound();
        var appointment = await db.TransactionAppointments.SingleOrDefaultAsync(x => x.TransactionId == transaction.Id, token)
            ?? throw Conflict("APPOINTMENT_REQUIRED", "먼저 거래 일정을 등록해 주세요.");
        var existing = await db.TransactionAppointmentChangeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, token);
        if (existing is not null)
        {
            if (existing.TransactionAppointmentId != appointment.Id) throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "다른 일정에 사용된 요청 키입니다.");
            return Map(existing);
        }
        if (appointment.StatusCode == "CANCELLED" || transaction.StatusCode is "COMPLETED" or "CANCELLED") throw Conflict("APPOINTMENT_CHANGE_NOT_ALLOWED", "현재 상태에서는 일정 변경을 요청할 수 없습니다.");
        var now = DateTime.UtcNow;
        var change = new TransactionAppointmentChangeRequest
        {
            TransactionAppointmentId = appointment.Id, RequestedByUserId = identity.UserId,
            RequestedStartAt = input.RequestedStartAt.ToUniversalTime(), RequestedEndAt = input.RequestedEndAt?.ToUniversalTime(),
            Reason = input.Reason.Trim(), StatusCode = "REQUESTED", RequestedAt = now,
            IdempotencyKey = input.IdempotencyKey.Trim(), CreatedAt = now
        };
        db.TransactionAppointmentChangeRequests.Add(change);
        db.OutboxEvents.Add(Event(transaction, "TRANSACTION_APPOINTMENT_CHANGE_REQUESTED", identity.UserId, now));
        await db.SaveChangesAsync(token);
        return Map(change);
    }

    private async Task<TransactionAppointmentResponse> Build(TransactionAppointment appointment, Guid transactionId, CancellationToken token)
    {
        var changes = await db.TransactionAppointmentChangeRequests.AsNoTracking()
            .Where(x => x.TransactionAppointmentId == appointment.Id).OrderByDescending(x => x.RequestedAt)
            .Select(x => new AppointmentChangeResponse(x.PublicId, x.RequestedStartAt, x.RequestedEndAt, x.Reason,
                x.StatusCode, x.RequestedAt, x.ProcessedAt)).ToListAsync(token);
        return new(appointment.PublicId, transactionId, appointment.ScheduledStartAt, appointment.ScheduledEndAt,
            appointment.EstimatedDurationMinutes, appointment.CustomerMemo, appointment.ProviderMemo, appointment.StatusCode,
            appointment.StatusCode == "CONFIRMED", appointment.ConfirmedAt, appointment.CancelledAt, changes);
    }

    private async Task<User> ActiveUser(ClaimsPrincipal principal, CancellationToken token)
    {
        var id = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : Guid.Empty;
        return await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == id && x.StatusCode == "ACTIVE", token)
            ?? throw new WorkBusinessException("USER_NOT_FOUND", "사용자를 확인할 수 없습니다.", 403);
    }

    private async Task<TransactionRecord?> AccessibleTransaction(long userId, Guid transactionId, CancellationToken token) =>
        await (from transaction in db.Transactions.AsNoTracking()
               join customer in db.CustomerProfiles.AsNoTracking() on transaction.CustomerProfileId equals customer.Id
               join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
               where transaction.PublicId == transactionId && (customer.UserId == userId || provider.UserId == userId)
               select transaction).SingleOrDefaultAsync(token);

    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        var user = await ActiveUser(principal, token);
        var profile = await db.CustomerProfiles.AsNoTracking().Where(x => x.UserId == user.Id).Select(x => x.Id).SingleOrDefaultAsync(token);
        return profile == 0 ? throw new WorkBusinessException("CUSTOMER_PROFILE_REQUIRED", "고객 프로필이 필요합니다.", 403) : (user.Id, profile);
    }

    private static void ValidatePeriod(DateTime start, DateTime? end)
    {
        if (start == default || end.HasValue && end <= start) throw Invalid("APPOINTMENT_PERIOD_INVALID", "일정 시작과 종료를 확인해 주세요.");
    }
    private static AppointmentChangeResponse Map(TransactionAppointmentChangeRequest x) =>
        new(x.PublicId, x.RequestedStartAt, x.RequestedEndAt, x.Reason, x.StatusCode, x.RequestedAt, x.ProcessedAt);
    private static OutboxEvent Event(TransactionRecord transaction, string type, long userId, DateTime now) => new()
    {
        AggregateType = "Transaction", AggregatePublicId = transaction.PublicId, EventType = type,
        PayloadJson = JsonSerializer.Serialize(new { transactionId = transaction.PublicId }), StatusCode = "PENDING",
        OccurredAt = now, AvailableAt = now, IdempotencyKey = $"{type.ToLowerInvariant()}:{transaction.PublicId:N}:{now.Ticks}", CreatedByUserId = userId
    };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkBusinessException NotFound() => new("TRANSACTION_NOT_FOUND", "거래를 찾을 수 없습니다.", 404);
    private static WorkBusinessException Conflict(string code, string message) => new(code, message);
    private static WorkBusinessException Invalid(string code, string message) => new(code, message, 400);
}
