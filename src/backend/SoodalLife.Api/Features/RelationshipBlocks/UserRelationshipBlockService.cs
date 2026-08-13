using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.RelationshipBlocks;

public sealed record CreateProviderBlockRequest(Guid ProviderId, string? ReasonCode, string? PrivateMemo, string IdempotencyKey);
public sealed record ReleaseProviderBlockRequest(string IdempotencyKey, string RowVersion);
public sealed record ProviderBlockResponse(Guid Id, Guid ProviderId, string ProviderName, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt, string RowVersion);
public sealed record AdminProviderBlockResponse(Guid Id, Guid CustomerId, string CustomerName, Guid ProviderId, string ProviderName, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt);

public interface IUserRelationshipBlockPolicy
{
    Task<bool> IsBlockedAsync(long customerProfileId, long providerProfileId, CancellationToken token);
    Task EnsureAllowedAsync(long customerProfileId, long providerProfileId, CancellationToken token);
}

public sealed class UserRelationshipBlockService(SoodalLifeDbContext db) : IUserRelationshipBlockPolicy
{
    public Task<bool> IsBlockedAsync(long customerProfileId, long providerProfileId, CancellationToken token) =>
        db.UserRelationshipBlocks.AsNoTracking().AnyAsync(x => x.CustomerProfileId == customerProfileId && x.ProviderProfileId == providerProfileId && x.StatusCode == "ACTIVE", token);

    public async Task EnsureAllowedAsync(long customerProfileId, long providerProfileId, CancellationToken token)
    {
        if (await IsBlockedAsync(customerProfileId, providerProfileId, token))
            throw Error("USER_RELATIONSHIP_BLOCKED", "차단된 공급자와는 새로운 매칭이나 선택을 진행할 수 없습니다.");
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
        var key = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key)) throw Error("IDEMPOTENCY_KEY_REQUIRED", "중복 실행 방지 키가 필요합니다.", 400);
        var prior = await db.UserRelationshipBlocks.SingleOrDefaultAsync(x => x.IdempotencyKey == key, token);
        if (prior is not null)
        {
            if (prior.CustomerProfileId != customer.ProfileId || prior.DirectionCode != "CUSTOMER_TO_PROVIDER") throw Error("IDEMPOTENCY_KEY_CONFLICT", "이미 다른 차단 요청에 사용된 키입니다.");
            var priorProvider = await db.ProviderProfiles.AsNoTracking().SingleAsync(x => x.Id == prior.ProviderProfileId, token);
            return Map(prior, priorProvider);
        }
        var provider = await db.ProviderProfiles.SingleOrDefaultAsync(x => x.PublicId == input.ProviderId, token) ?? throw Error("PROVIDER_NOT_FOUND", "공급자를 찾을 수 없습니다.", 404);
        if (provider.UserId == customer.UserId) throw Error("SELF_BLOCK_NOT_ALLOWED", "본인의 공급자 역할은 차단할 수 없습니다.", 400);
        var active = await db.UserRelationshipBlocks.SingleOrDefaultAsync(x => x.CustomerProfileId == customer.ProfileId && x.ProviderProfileId == provider.Id && x.DirectionCode == "CUSTOMER_TO_PROVIDER" && x.StatusCode == "ACTIVE", token);
        if (active is not null) return Map(active, provider);
        var now = DateTime.UtcNow;
        var block = new UserRelationshipBlock
        {
            CustomerProfileId = customer.ProfileId, ProviderProfileId = provider.Id, DirectionCode = "CUSTOMER_TO_PROVIDER", StatusCode = "ACTIVE",
            ReasonCode = Clean(input.ReasonCode, 50), PrivateMemo = Clean(input.PrivateMemo, 1000), IdempotencyKey = key,
            CreatedAt = now, CreatedByUserId = customer.UserId, UpdatedAt = now, UpdatedByUserId = customer.UserId
        };
        db.UserRelationshipBlocks.Add(block);
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = customer.UserId, ActorRoleCode = "CUSTOMER", ActionCode = "PROVIDER_BLOCK_CREATED", EntityType = "UserRelationshipBlock", EntityPublicId = block.PublicId, ResultCode = "SUCCESS", BeforeJson = "{\"status\":null}", AfterJson = System.Text.Json.JsonSerializer.Serialize(new { direction = block.DirectionCode, providerId = provider.PublicId, status = block.StatusCode, reasonCode = block.ReasonCode }) });
        await db.SaveChangesAsync(token);
        return Map(block, provider);
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
        return await query.OrderByDescending(x => x.block.CreatedAt).Select(x => new AdminProviderBlockResponse(x.block.PublicId, x.customer.PublicId, x.customer.DisplayName, x.provider.PublicId, x.provider.BusinessName, x.block.DirectionCode, x.block.StatusCode, x.block.ReasonCode, x.block.CreatedAt, x.block.ReleasedAt)).ToListAsync(token);
    }

    public async Task<AdminProviderBlockResponse> AdminDetail(Guid id, CancellationToken token) =>
        await (from block in db.UserRelationshipBlocks.AsNoTracking() join customer in db.CustomerProfiles.AsNoTracking() on block.CustomerProfileId equals customer.Id join provider in db.ProviderProfiles.AsNoTracking() on block.ProviderProfileId equals provider.Id where block.PublicId == id select new AdminProviderBlockResponse(block.PublicId, customer.PublicId, customer.DisplayName, provider.PublicId, provider.BusinessName, block.DirectionCode, block.StatusCode, block.ReasonCode, block.CreatedAt, block.ReleasedAt)).SingleOrDefaultAsync(token)
        ?? throw Error("BLOCK_NOT_FOUND", "차단 내역을 찾을 수 없습니다.", 404);

    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Error("CUSTOMER_REQUIRED", "고객 로그인이 필요합니다.", 403);
        var value = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.PublicId == publicId && user.StatusCode == "ACTIVE" select new { user.Id, ProfileId = profile.Id }).SingleOrDefaultAsync(token);
        return value is null ? throw Error("CUSTOMER_REQUIRED", "고객 로그인이 필요합니다.", 403) : (value.Id, value.ProfileId);
    }
    private static ProviderBlockResponse Map(UserRelationshipBlock x, ProviderProfile p) => new(x.PublicId, p.PublicId, p.BusinessName, x.DirectionCode, x.StatusCode, x.ReasonCode, x.CreatedAt, x.ReleasedAt, Convert.ToBase64String(x.RowVersion));
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static void ApplyVersion(byte[] current, string value) { byte[] expected; try { expected = Convert.FromBase64String(value); } catch { throw Error("ROW_VERSION_INVALID", "변경 버전이 올바르지 않습니다.", 400); } if (!current.SequenceEqual(expected)) throw Error("ROW_VERSION_CONFLICT", "다른 화면에서 차단 상태가 변경되었습니다."); }
    private static WorkBusinessException Error(string code, string message, int status = 409) => new(code, message, status);
}
