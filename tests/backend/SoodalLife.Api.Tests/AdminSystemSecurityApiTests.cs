using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminSystemSecurityApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData(RoleCodes.Customer, "/api/v1/admin/system/status")]
    [InlineData(RoleCodes.Provider, "/api/v1/admin/system/status")]
    [InlineData(RoleCodes.Customer, "/api/v1/admin/audit-logs")]
    [InlineData(RoleCodes.Provider, "/api/v1/admin/audit-logs")]
    public async Task AdminSystemApis_NonAdmin_IsForbidden(string role, string path)
    {
        using var client = Client();
        await Login(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/admin/system/status")]
    [InlineData("/api/v1/admin/audit-logs")]
    public async Task AdminSystemApis_Anonymous_IsUnauthorized(string path)
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadSystemAndMaskedAudit_WithoutChangingBusinessData()
    {
        using var client = Client();
        await Login(client, factory.Credentials[RoleCodes.Admin]);
        await SeedSensitiveAuditAsync();
        var before = await SnapshotAsync();

        var statusResponse = await client.GetAsync("/api/v1/admin/system/status");
        var auditResponse = await client.GetAsync("/api/v1/admin/audit-logs?action=SECURITY_MASK_TEST");

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<AdminSystemStatusResponse>();
        var audit = await auditResponse.Content.ReadFromJsonAsync<AdminAuditLogResponse>();
        Assert.NotNull(status);
        Assert.NotNull(audit);
        Assert.Equal("CONNECTED", status.Database.Connection);
        Assert.Contains(status.Integrations, item => item.Code == "PG" && item.Status is "UNINTEGRATED" or "CONFIGURED");
        Assert.Contains(status.Integrations, item => item.Code == "KAKAO_ALIMTALK" && item.Status == "UNINTEGRATED");
        var item = Assert.Single(audit.Items);
        Assert.Contains("***", item.BeforeJson);
        Assert.Contains("***", item.AfterJson);
        var json = await auditResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("raw-password-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("010-1234-5678", json, StringComparison.Ordinal);
        Assert.DoesNotContain("private@example.test", json, StringComparison.Ordinal);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact]
    public async Task AuditLog_HasNoWriteApi_AndEntityIsAppendOnly()
    {
        using var client = Client();
        await Login(client, factory.Credentials[RoleCodes.Admin]);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsync("/api/v1/admin/audit-logs", null)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/api/v1/admin/audit-logs")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var row = await db.AuditLogs.FirstAsync();
        row.Reason = "수정 시도";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("수정하거나 삭제할 수 없습니다", exception.Message);
    }

    private async Task SeedSensitiveAuditAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        if (await db.AuditLogs.AnyAsync(x => x.ActionCode == "SECURITY_MASK_TEST")) return;
        var admin = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Admin].LoginId);
        db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = admin.Id,
            ActorRoleCode = RoleCodes.Admin,
            ActionCode = "SECURITY_MASK_TEST",
            EntityType = "USER",
            EntityPublicId = admin.PublicId,
            ResultCode = "SUCCESS",
            Reason = "담당자 010-1234-5678 private@example.test 확인",
            BeforeJson = "{\"password\":\"raw-password-value\",\"phone\":\"010-1234-5678\"}",
            AfterJson = "{\"email\":\"private@example.test\",\"status\":\"ACTIVE\"}",
        });
        await db.SaveChangesAsync();
    }

    private async Task<(int Audit, int Outbox, int WalletLedger, int FeeCharges, int TrustEvents, int Subscriptions, int Interiors, decimal WalletBalance)> SnapshotAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        return (
            await db.AuditLogs.CountAsync(),
            await db.OutboxEvents.CountAsync(),
            await db.WalletLedgerEntries.CountAsync(),
            await db.FeeCharges.CountAsync(),
            await db.TrustScoreEvents.CountAsync(),
            await db.SubscriptionContracts.CountAsync(),
            await db.InteriorProjects.CountAsync(),
            await db.ProviderWallets.SumAsync(x => x.AvailableBalance + x.ReservedBalance));
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
