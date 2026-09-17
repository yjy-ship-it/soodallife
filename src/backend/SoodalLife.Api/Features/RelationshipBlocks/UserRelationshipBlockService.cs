using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.RelationshipBlocks;

public sealed record CreateProviderBlockRequest(Guid ProviderId, string? ReasonCode, string? PrivateMemo, string IdempotencyKey);
public sealed record ReleaseProviderBlockRequest(string IdempotencyKey, string RowVersion);
public sealed record AdminReleaseProviderBlockRequest(string Reason, string IdempotencyKey, string RowVersion);
public sealed record ProviderBlockResponse(Guid Id, Guid ProviderId, string ProviderName, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt, string RowVersion);
public sealed record ProviderReceivedBlockResponse(Guid Id, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt);
public sealed record AdminProviderBlockResponse(Guid Id, Guid CustomerId, string CustomerName, Guid ProviderId, string ProviderName, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt, string RowVersion, int OpenDisputeCount, bool CanAdminRelease);

public interface IUserRelationshipBlockPolicy
{
    Task<bool> IsBlockedAsync(long customerProfileId, long providerProfileId, CancellationToken token);
    Task EnsureAllowedAsync(long customerProfileId, long providerProfileId, CancellationToken token);
}

public sealed class UserRelationshipBlockService(SoodalLifeDbContext db) : IUserRelationshipBlockPolicy
{
    private static readonly HashSet<string> AllowedReasonCodes = new(StringComparer.Ordinal)
    {
        "CUSTOMER_PREFERENCE", "COMMUNICATION_DIFFICULTY", "SERVICE_MISMATCH", "INAPPROPRIATE_BEHAVIOR", "SAFETY_CONCERN", "OTHER"
    };

    public Task<bool> IsBlockedAsync(long customerProfileId, long providerProfileId, CancellationToken token) =>
        db.UserRelationshipBlocks.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customerProfileId && x.ProviderProfileId == providerProfileId && x.StatusCode == "ACTIVE", token);

    public async Task EnsureAllowedAsync(long customerProfileId, long providerProfileId, CancellationToken token)
    {
        if (await IsBlockedAsync(customerProfileId, providerProfileId, token))
            throw Error("USER_RELATIONSHIP_BLOCKED", "차단된 전문가와는 새로운 매칭이나 선택을 진행할 수 없습니다.");
    }

    public async Task<IReadOnlyList<ProviderBlockResponse>> CustomerList(ClaimsPrincipal principal, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var rows = await (from block in db.UserRelationshipBlocks.AsNoTracking()
                          join provider in db.ProviderProfiles.AsNoTracking() on block.ProviderProfileId equals provider.Id
                          where block.CustomerProfileId == customer.ProfileId
                          orderby block.CreatedAt descending
                          select new { block, provider }).ToListAsync(token);
        return rows.Select(x => Map(x.block, x.provider)).ToArray();
    }

    public async Task<ProviderBlockResponse> Create(ClaimsPrincipal principal, CreateProviderBlockRequest input, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var reasonCode = input.ReasonCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reasonCode)) throw Error("BLOCK_REASON_REQUIRED", "차단 사유를 선택해 주세요.", 400);
        if (!AllowedReasonCodes.Contains(reasonCode)) throw Error("BLOCK_REASON_INVALID", "선택한 차단 사유를 확인해 주세요.", 400);
        var key = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key)) throw Error("IDEMPOTENCY_KEY_REQUIRED", "중복 실행 방지 키가 필요합니다.", 400);
        var prior = await db.UserRelationshipBlocks.SingleOrDefaultAsync(x => x.IdempotencyKey == key, token);
        if (prior is not null)
        {
            if (prior.CustomerProfileId != customer.ProfileId || prior.DirectionCode != "CUSTOMER_TO_PROVIDER") throw Error("IDEMPOTENCY_KEY_CONFLICT", "이미 다른 차단 요청에 사용된 키입니다.");
            var priorProvider = await db.ProviderProfiles.AsNoTracking().SingleAsync(x => x.Id == prior.ProviderProfileId, token);
            return Map(prior, priorProvider);
        }
        var provider = await db.ProviderProfiles.SingleOrDefaultAsync(x => x.PublicId == input.ProviderId, token) ?? throw Error("PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.", 404);
        if (provider.UserId == customer.UserId) throw Error("SELF_BLOCK_NOT_ALLOWED", "본인의 전문가 역할은 차단할 수 없습니다.", 400);
        var active = await db.UserRelationshipBlocks.SingleOrDefaultAsync(x => x.CustomerProfileId == customer.ProfileId && x.ProviderProfileId == provider.Id && x.DirectionCode == "CUSTOMER_TO_PROVIDER" && x.StatusCode == "ACTIVE", token);
        if (active is not null) return Map(active, provider);
        var now = DateTime.UtcNow;
        var block = new UserRelationshipBlock
        {
            CustomerProfileId = customer.ProfileId, ProviderProfileId = provider.Id, DirectionCode = "CUSTOMER_TO_PROVIDER", StatusCode = "ACTIVE",
            ReasonCode = reasonCode, PrivateMemo = Clean(input.PrivateMemo, 1000), IdempotencyKey = key,
            CreatedAt = now, CreatedByUserId = customer.UserId, UpdatedAt = now, UpdatedByUserId = customer.UserId
        };
        db.UserRelationshipBlocks.Add(block);
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = customer.UserId, ActorRoleCode = "CUSTOMER", ActionCode = "PROVIDER_BLOCK_CREATED", EntityType = "UserRelationshipBlock", EntityPublicId = block.PublicId, ResultCode = "SUCCESS", BeforeJson = "{\"status\":null}", AfterJson = System.Text.Json.JsonSerializer.Serialize(new { direction = block.DirectionCode, providerId = provider.PublicId, status = block.StatusCode, reasonCode = block.ReasonCode }) });
        await db.SaveChangesAsync(token);
        return Map(block, provider);
    }

    public async Task<IReadOnlyList<ProviderReceivedBlockResponse>> ProviderList(ClaimsPrincipal principal, CancellationToken token)
    {
        var provider = await Provider(principal, token);
        return await db.UserRelationshipBlocks.AsNoTracking()
            .Where(x => x.ProviderProfileId == provider.ProfileId && x.DirectionCode == "CUSTOMER_TO_PROVIDER")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProviderReceivedBlockResponse(x.PublicId, x.DirectionCode, x.StatusCode, x.ReasonCode, x.CreatedAt, x.ReleasedAt))
            .ToListAsync(token);
    }

    public async Task<ProviderBlockResponse> Release(ClaimsPrincipal principal, Guid id, ReleaseProviderBlockRequest input, CancellationToken token)
    {
        var customer = await Customer(principal, token);
        var key = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key)) throw Error("IDEMPOTENCY_KEY_REQUIRED", "중복 실행 방지 키가 필요합니다.", 400);
        var block = await db.UserRelationshipBlocks.SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == customer.ProfileId, token) ?? throw Error("BLOCK_NOT_FOUND", "차단 내역을 찾을 수 없습니다.", 404);
        var provider = await db.ProviderProfiles.AsNoTracking().SingleAsync(x => x.Id == block.ProviderProfileId, token);
        if (block.StatusCode == "RELEASED")
        {
            if (block.ReleaseIdempotencyKey == key) return Map(block, provider);
            throw Error("BLOCK_ALREADY_RELEASED", "이미 해제된 차단입니다.");
        }
        ApplyVersion(block.RowVersion, input.RowVersion);
        var used = await db.UserRelationshipBlocks.AsNoTracking().AnyAsync(x => x.ReleaseIdempotencyKey == key && x.Id != block.Id, token);
        if (used) throw Error("IDEMPOTENCY_KEY_CONFLICT", "이미 다른 해제 요청에 사용된 키입니다.");
        var now = DateTime.UtcNow;
        block.StatusCode = "RELEASED"; block.ReleasedAt = now; block.ReleasedByUserId = customer.UserId; block.ReleaseIdempotencyKey = key; block.UpdatedAt = now; block.UpdatedByUserId = customer.UserId;
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = customer.UserId, ActorRoleCode = "CUSTOMER", ActionCode = "PROVIDER_BLOCK_RELEASED", EntityType = "UserRelationshipBlock", EntityPublicId = block.PublicId, ResultCode = "SUCCESS", BeforeJson = "{\"status\":\"ACTIVE\"}", AfterJson = System.Text.Json.JsonSerializer.Serialize(new { direction = block.DirectionCode, providerId = provider.PublicId, status = block.StatusCode, reasonCode = block.ReasonCode }) });
        try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { throw Error("ROW_VERSION_CONFLICT", "다른 화면에서 차단 상태가 변경되었습니다."); }
        return Map(block, provider);
    }

    public async Task<IReadOnlyList<AdminProviderBlockResponse>> AdminList(string? status, CancellationToken token)
    {
        var query = from block in db.UserRelationshipBlocks.AsNoTracking()
                    join customer in db.CustomerProfiles.AsNoTracking() on block.CustomerProfileId equals customer.Id
                    join provider in db.ProviderProfiles.AsNoTracking() on block.ProviderProfileId equals provider.Id
                    select new { block, customer, provider };
        if (!string.IsNullOrWhiteSpace(status)) { var code = status.Trim().ToUpperInvariant(); query = query.Where(x => x.block.StatusCode == code); }
        var rows = await query.OrderByDescending(x => x.block.CreatedAt).ToListAsync(token);
        var result = new List<AdminProviderBlockResponse>(rows.Count);
        foreach (var row in rows)
        {
            var openDisputes = await OpenDisputeCount(row.customer.UserId, row.provider.UserId, token);
            result.Add(MapAdmin(row.block, row.customer, row.provider, openDisputes));
        }
        return result;
    }

    public async Task<AdminProviderBlockResponse> AdminDetail(Guid id, CancellationToken token)
    {
        var row = await (from block in db.UserRelationshipBlocks.AsNoTracking()
                         join customer in db.CustomerProfiles.AsNoTracking() on block.CustomerProfileId equals customer.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on block.ProviderProfileId equals provider.Id
                         where block.PublicId == id select new { block, customer, provider }).SingleOrDefaultAsync(token)
                  ?? throw Error("BLOCK_NOT_FOUND", "차단 내역을 찾을 수 없습니다.", 404);
        var openDisputes = await OpenDisputeCount(row.customer.UserId, row.provider.UserId, token);
        return MapAdmin(row.block, row.customer, row.provider, openDisputes);
    }

    public async Task<AdminProviderBlockResponse> AdminRelease(ClaimsPrincipal principal, Guid id, AdminReleaseProviderBlockRequest input, CancellationToken token)
    {
        var actorId = await Admin(principal, token);
        var reason = Clean(input.Reason, 1000) ?? throw Error("ADMIN_RELEASE_REASON_REQUIRED", "관리자 강제해제 사유를 입력해 주세요.", 400);
        var key = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key)) throw Error("IDEMPOTENCY_KEY_REQUIRED", "중복 실행 방지 키가 필요합니다.", 400);
        var row = await (from block in db.UserRelationshipBlocks
                         join customer in db.CustomerProfiles on block.CustomerProfileId equals customer.Id
                         join provider in db.ProviderProfiles on block.ProviderProfileId equals provider.Id
                         where block.PublicId == id select new { block, customer, provider }).SingleOrDefaultAsync(token)
                  ?? throw Error("BLOCK_NOT_FOUND", "차단 내역을 찾을 수 없습니다.", 404);
        if (row.block.StatusCode == "RELEASED")
        {
            if (row.block.ReleaseIdempotencyKey == key) return MapAdmin(row.block, row.customer, row.provider, 0);
            throw Error("BLOCK_ALREADY_RELEASED", "이미 해제된 차단입니다.");
        }
        var openDisputes = await OpenDisputeCount(row.customer.UserId, row.provider.UserId, token);
        if (openDisputes > 0) throw Error("BLOCK_ADMIN_RELEASE_BLOCKED_BY_ACTIVE_DISPUTE", "진행 중인 분쟁이 있어 관리자 강제해제를 할 수 없습니다. 분쟁을 종결한 후 다시 시도해 주세요.");
        ApplyVersion(row.block.RowVersion, input.RowVersion);
        if (await db.UserRelationshipBlocks.AsNoTracking().AnyAsync(x => x.ReleaseIdempotencyKey == key && x.Id != row.block.Id, token))
            throw Error("IDEMPOTENCY_KEY_CONFLICT", "이미 다른 해제 요청에 사용된 키입니다.");
        var now = DateTime.UtcNow;
        row.block.StatusCode = "RELEASED"; row.block.ReleasedAt = now; row.block.ReleasedByUserId = actorId; row.block.ReleaseIdempotencyKey = key; row.block.UpdatedAt = now; row.block.UpdatedByUserId = actorId;
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actorId, ActorRoleCode = "ADMIN", ActionCode = "PROVIDER_BLOCK_ADMIN_RELEASED", EntityType = "UserRelationshipBlock", EntityPublicId = row.block.PublicId, ResultCode = "SUCCESS", Reason = reason, BeforeJson = "{\"status\":\"ACTIVE\"}", AfterJson = System.Text.Json.JsonSerializer.Serialize(new { status = row.block.StatusCode, providerId = row.provider.PublicId, reasonCode = row.block.ReasonCode, releaseReason = reason }) });
        try { await db.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { throw Error("ROW_VERSION_CONFLICT", "다른 화면에서 차단 상태가 변경되었습니다."); }
        return MapAdmin(row.block, row.customer, row.provider, 0);
    }

    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Error("CUSTOMER_REQUIRED", "고객 로그인이 필요합니다.", 403);
        var value = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.PublicId == publicId && user.StatusCode == "ACTIVE" select new { user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return value is null ? throw Error("CUSTOMER_REQUIRED", "고객 로그인이 필요합니다.", 403) : (value.Id, value.ProfileId);
    }
    private async Task<(long UserId, long ProfileId)> Provider(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Error("PROVIDER_REQUIRED", "전문가 로그인이 필요합니다.", 403);
        var value = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId where user.PublicId == publicId && user.StatusCode == "ACTIVE" select new { user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return value is null ? throw Error("PROVIDER_REQUIRED", "전문가 로그인이 필요합니다.", 403) : (value.Id, value.ProfileId);
    }
    private async Task<long> Admin(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Error("ADMIN_REQUIRED", "관리자 로그인이 필요합니다.", 403);
        return await (from user in db.Users.AsNoTracking() join link in db.UserRoles.AsNoTracking() on user.Id equals link.UserId join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id where user.PublicId == publicId && user.StatusCode == "ACTIVE" && link.RevokedAt == null && role.Code == "ADMIN" select (long?)user.Id).SingleOrDefaultAsync(token) ?? throw Error("ADMIN_REQUIRED", "관리자 권한이 필요합니다.", 403);
    }
    private Task<int> OpenDisputeCount(long customerUserId, long providerUserId, CancellationToken token) =>
        db.DisputeCases.AsNoTracking().CountAsync(x => x.StatusCode != "RESOLVED" && x.StatusCode != "CLOSED" && ((x.ApplicantUserId == customerUserId && x.CounterpartyUserId == providerUserId) || (x.ApplicantUserId == providerUserId && x.CounterpartyUserId == customerUserId)), token);
    private static ProviderBlockResponse Map(UserRelationshipBlock x, ProviderProfile p) => new(x.PublicId, p.PublicId, p.BusinessName, x.DirectionCode, x.StatusCode, x.ReasonCode, x.CreatedAt, x.ReleasedAt, Convert.ToBase64String(x.RowVersion));
    private static AdminProviderBlockResponse MapAdmin(UserRelationshipBlock x, CustomerProfile c, ProviderProfile p, int openDisputes) => new(x.PublicId, c.PublicId, c.DisplayName, p.PublicId, p.BusinessName, x.DirectionCode, x.StatusCode, x.ReasonCode, x.CreatedAt, x.ReleasedAt, Convert.ToBase64String(x.RowVersion), openDisputes, x.StatusCode == "ACTIVE" && openDisputes == 0);
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static void ApplyVersion(byte[] current, string value) { byte[] expected; try { expected = Convert.FromBase64String(value); } catch { throw Error("ROW_VERSION_INVALID", "변경 버전이 올바르지 않습니다.", 400); } if (!current.SequenceEqual(expected)) throw Error("ROW_VERSION_CONFLICT", "다른 화면에서 차단 상태가 변경되었습니다."); }
    private static WorkBusinessException Error(string code, string message, int status = 409) => new(code, message, status);
}
