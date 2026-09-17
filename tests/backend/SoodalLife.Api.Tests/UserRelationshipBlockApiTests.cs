using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.RelationshipBlocks;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class UserRelationshipBlockApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task CustomerCanCreateListAndReleaseWithoutExposingPrivateMemo()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var client = Client(); await Login(client, customer.Credential);
        var createdResponse = await client.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "CUSTOMER_PREFERENCE", privateMemo = "상대방에게 공개하면 안 되는 메모", idempotencyKey = $"block-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode); var body = await createdResponse.Content.ReadAsStringAsync(); Assert.DoesNotContain("공개하면 안 되는", body);
        var created = (await createdResponse.Content.ReadFromJsonAsync<BlockDto>())!; Assert.Equal("ACTIVE", created.Status);
        Assert.Contains((await client.GetFromJsonAsync<BlockDto[]>(Path))!, x => x.Id == created.Id);
        var released = await client.PostAsJsonAsync($"{Path}/{created.Id}/release", new { rowVersion = created.RowVersion, idempotencyKey = $"release-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, released.StatusCode); Assert.Equal("RELEASED", (await released.Content.ReadFromJsonAsync<BlockDto>())!.Status);
    }

    [Fact]
    public async Task CreateAndReleaseAreIdempotent()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var client = Client(); await Login(client, customer.Credential);
        var key = $"block-{Guid.NewGuid():N}"; var input = new { providerId = provider.PublicId, reasonCode = "CUSTOMER_PREFERENCE", privateMemo = (string?)null, idempotencyKey = key };
        var first = (await (await client.PostAsJsonAsync(Path, input)).Content.ReadFromJsonAsync<BlockDto>())!;
        var second = (await (await client.PostAsJsonAsync(Path, input)).Content.ReadFromJsonAsync<BlockDto>())!; Assert.Equal(first.Id, second.Id);
        var releaseKey = $"release-{Guid.NewGuid():N}"; var release = new { rowVersion = first.RowVersion, idempotencyKey = releaseKey };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Path}/{first.Id}/release", release)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Path}/{first.Id}/release", release)).StatusCode);
    }

    [Fact]
    public async Task OtherCustomerCannotReleaseAndProviderCannotUseCustomerApi()
    {
        var owner = await SeedCustomer(); var other = await SeedCustomer(); var provider = await Provider(); using var ownerClient = Client(); await Login(ownerClient, owner.Credential);
        var block = (await (await ownerClient.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "OTHER", idempotencyKey = $"block-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<BlockDto>())!;
        using var otherClient = Client(); await Login(otherClient, other.Credential); Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PostAsJsonAsync($"{Path}/{block.Id}/release", new { rowVersion = block.RowVersion, idempotencyKey = $"release-{Guid.NewGuid():N}" })).StatusCode);
        using var providerClient = Client(); await Login(providerClient, factory.Credentials[RoleCodes.Provider]); Assert.Equal(HttpStatusCode.Forbidden, (await providerClient.GetAsync(Path)).StatusCode);
    }

    [Fact]
    public async Task ActiveBlockIsFailClosedForPolicyAndReleaseRestoresFutureEligibility()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var client = Client(); await Login(client, customer.Credential);
        var block = (await (await client.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "SERVICE_MISMATCH", idempotencyKey = $"block-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<BlockDto>())!;
        using (var scope = factory.Services.CreateScope()) { var policy = scope.ServiceProvider.GetRequiredService<IUserRelationshipBlockPolicy>(); Assert.True(await policy.IsBlockedAsync(customer.ProfileId, provider.Id, default)); }
        await client.PostAsJsonAsync($"{Path}/{block.Id}/release", new { rowVersion = block.RowVersion, idempotencyKey = $"release-{Guid.NewGuid():N}" });
        using var after = factory.Services.CreateScope(); Assert.False(await after.ServiceProvider.GetRequiredService<IUserRelationshipBlockPolicy>().IsBlockedAsync(customer.ProfileId, provider.Id, default));
    }

    [Fact]
    public async Task ProviderSeesStandardReasonButNotCustomerIdentityOrPrivateMemo()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var client = Client(); await Login(client, customer.Credential); const string memo = "private-block-memo";
        var block = (await (await client.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "SAFETY_CONCERN", privateMemo = memo, idempotencyKey = $"block-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<BlockDto>())!;
        using var providerClient = Client(); await Login(providerClient, factory.Credentials[RoleCodes.Provider]); var response = await providerClient.GetAsync("/api/v1/providers/me/customer-blocks"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var body = await response.Content.ReadAsStringAsync(); Assert.Contains("SAFETY_CONCERN", body); Assert.DoesNotContain(customer.Credential.LoginId, body); Assert.DoesNotContain(memo, body);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var audit = await db.AuditLogs.Where(x => x.EntityPublicId == block.Id).Select(x => (x.BeforeJson ?? "") + (x.AfterJson ?? "") + (x.MetadataJson ?? "")).ToListAsync(); Assert.DoesNotContain(audit, x => x.Contains(memo));
    }

    [Fact]
    public async Task AdminCanForceReleaseWithReasonAndAuditTrail()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var owner = Client(); await Login(owner, customer.Credential);
        var block = (await (await owner.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "OTHER", privateMemo = "비공개", idempotencyKey = $"block-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<BlockDto>())!;
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); var detail = (await admin.GetFromJsonAsync<AdminBlockDto>($"/api/v1/admin/user-blocks/{block.Id}"))!; Assert.True(detail.CanAdminRelease);
        var release = await admin.PostAsJsonAsync($"/api/v1/admin/user-blocks/{block.Id}/release", new { reason = "고객 본인 확인 및 오등록 정정", rowVersion = detail.RowVersion, idempotencyKey = $"admin-release-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.OK, release.StatusCode); Assert.Equal("RELEASED", (await release.Content.ReadFromJsonAsync<AdminBlockDto>())!.Status);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Contains(await db.AuditLogs.Where(x => x.EntityPublicId == block.Id).ToListAsync(), x => x.ActionCode == "PROVIDER_BLOCK_ADMIN_RELEASED" && x.Reason == "고객 본인 확인 및 오등록 정정");
    }

    [Fact]
    public async Task AdminCannotForceReleaseWhileDisputeIsOpen()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var owner = Client(); await Login(owner, customer.Credential);
        var block = (await (await owner.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "COMMUNICATION_DIFFICULTY", idempotencyKey = $"block-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<BlockDto>())!;
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var now = DateTime.UtcNow; db.DisputeCases.Add(new DisputeCase { ApplicantUserId = customer.UserId, CounterpartyUserId = provider.UserId, Subject = "차단 관련 분쟁", Description = "관리자 강제해제 금지 검증", StatusCode = "OPEN", ReceivedAt = now, CreatedAt = now, UpdatedAt = now }); await db.SaveChangesAsync(); }
        using var admin = Client(); await Login(admin, factory.Credentials[RoleCodes.Admin]); var detail = (await admin.GetFromJsonAsync<AdminBlockDto>($"/api/v1/admin/user-blocks/{block.Id}"))!; Assert.False(detail.CanAdminRelease); Assert.Equal(1, detail.OpenDisputeCount);
        var release = await admin.PostAsJsonAsync($"/api/v1/admin/user-blocks/{block.Id}/release", new { reason = "예외 해제 시도", rowVersion = detail.RowVersion, idempotencyKey = $"admin-release-{Guid.NewGuid():N}" }); Assert.Equal(HttpStatusCode.Conflict, release.StatusCode); Assert.Contains("진행 중인 분쟁", await release.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateRequiresSupportedReasonCode()
    {
        var customer = await SeedCustomer(); var provider = await Provider(); using var client = Client(); await Login(client, customer.Credential);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = (string?)null, idempotencyKey = $"block-{Guid.NewGuid():N}" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Path, new { providerId = provider.PublicId, reasonCode = "UNKNOWN", idempotencyKey = $"block-{Guid.NewGuid():N}" })).StatusCode);
    }

    private async Task<SeededCustomer> SeedCustomer()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(); var now = DateTime.UtcNow;
        var credential = new TestCredential($"block-customer-{Guid.NewGuid():N}", $"Block!{Guid.NewGuid():N}aA1"); var user = new User { LoginId = credential.LoginId, NormalizedLoginId = credential.LoginId.ToUpperInvariant(), StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now }; user.PasswordHash = hasher.HashPassword(user, credential.Password); db.Users.Add(user); await db.SaveChangesAsync();
        var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Customer); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now }); var profile = new CustomerProfile { UserId = user.Id, DisplayName = "차단 검증 고객", CreatedAt = now, UpdatedAt = now }; db.CustomerProfiles.Add(profile); await db.SaveChangesAsync(); return new(user.Id, profile.Id, credential);
    }
    private async Task<(long Id, Guid PublicId, long UserId)> Provider() { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); return await (from profile in db.ProviderProfiles.AsNoTracking() join user in db.Users.AsNoTracking() on profile.UserId equals user.Id where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select new ValueTuple<long, Guid, long>(profile.Id, profile.PublicId, user.Id)).SingleAsync(); }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private const string Path = "/api/v1/customers/me/provider-blocks";
    private sealed record SeededCustomer(long UserId, long ProfileId, TestCredential Credential);
    private sealed record BlockDto(Guid Id, Guid ProviderId, string ProviderName, string Direction, string Status, string? ReasonCode, DateTime CreatedAt, DateTime? ReleasedAt, string RowVersion);
    private sealed record AdminBlockDto(Guid Id, string Status, string RowVersion, int OpenDisputeCount, bool CanAdminRelease);
}
