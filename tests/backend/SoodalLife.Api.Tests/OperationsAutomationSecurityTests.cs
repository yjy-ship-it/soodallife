using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Automation;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Tests;

public sealed class OperationsAutomationSecurityTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Outbox_processor_publishes_pending_event_and_is_idempotent()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var row = Event("AUTOMATION_NO_TEMPLATE", "{}"); db.OutboxEvents.Add(row); await db.SaveChangesAsync();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        var first = await processor.ProcessBatchAsync(default); var second = await processor.ProcessBatchAsync(default);
        db.ChangeTracker.Clear(); var saved = await db.OutboxEvents.SingleAsync(x => x.Id == row.Id);
        Assert.True(first.ProcessedCount >= 1); Assert.Equal(0, second.ProcessedCount); Assert.Equal("PUBLISHED", saved.StatusCode); Assert.NotNull(saved.ProcessedAt);
    }

    [Fact]
    public async Task Outbox_failure_is_isolated_retried_and_contains_only_safe_error_code()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        db.NotificationTemplates.Add(new NotificationTemplate { TemplateCode = $"AUTO-{Guid.NewGuid():N}", Name = "test", AudienceTypeCode = "ALL", EventTypeCode = "AUTOMATION_BAD_PAYLOAD", ChannelCode = "WEB", TitleTemplate = "test", BodyTemplate = "test", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        var bad = Event("AUTOMATION_BAD_PAYLOAD", "{not-json"); var good = Event("AUTOMATION_GOOD", "{}"); db.OutboxEvents.AddRange(bad, good); await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessBatchAsync(default);
        db.ChangeTracker.Clear(); bad = await db.OutboxEvents.SingleAsync(x => x.Id == bad.Id); good = await db.OutboxEvents.SingleAsync(x => x.Id == good.Id);
        Assert.Equal("FAILED", bad.StatusCode); Assert.Equal(1, bad.AttemptCount); Assert.Equal("INVALID_JSON", bad.ErrorMessage); Assert.True(bad.AvailableAt > DateTime.UtcNow);
        Assert.Equal("PUBLISHED", good.StatusCode); Assert.True(result.FailedCount >= 1); Assert.True(result.ProcessedCount >= 1);
    }

    [Fact]
    public async Task Scheduler_records_enabled_job_and_skips_not_configured_job()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var jobs = new IAutomationJob[] { new TestJob("TEST_ENABLED", AutomationConfigurationStatus.Enabled), new TestJob("TEST_POLICY", AutomationConfigurationStatus.NotConfigured) };
        var runner = new ScheduledJobRunner(db, jobs, Options.Create(new AutomationOptions { LeaseSeconds = 30 }));
        await runner.RunDueAsync(default); await runner.RunDueAsync(default);
        Assert.Single(await db.ScheduledJobRuns.Where(x => x.JobName == "TEST_ENABLED" && x.StatusCode == "SUCCEEDED").ToListAsync());
        Assert.Empty(await db.ScheduledJobRuns.Where(x => x.JobName == "TEST_POLICY").ToListAsync());
        Assert.Equal("NOT_CONFIGURED", (await db.ScheduledJobLeases.SingleAsync(x => x.JobName == "TEST_POLICY")).ConfigurationStatusCode);
    }

    [Fact]
    public async Task Audit_payload_is_redacted_before_append_only_insert()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var row = new AuditLog { OccurredAt = DateTime.UtcNow, ActionCode = "AUTOMATION_REDACTION_TEST", EntityType = "TEST", ResultCode = "SUCCESS", Reason = "token=raw-token 010-1234-5678", AfterJson = "{\"email\":\"person@example.test\",\"status\":\"ACTIVE\"}" };
        db.AuditLogs.Add(row); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); row = await db.AuditLogs.SingleAsync(x => x.ActionCode == "AUTOMATION_REDACTION_TEST");
        Assert.DoesNotContain("raw-token", row.Reason); Assert.DoesNotContain("010-1234-5678", row.Reason); Assert.DoesNotContain("person@example.test", row.AfterJson); Assert.Contains("ACTIVE", row.AfterJson);
    }

    [Fact]
    public void Encrypted_first_reader_uses_ciphertext_and_falls_back_safely()
    {
        using var scope = factory.Services.CreateScope(); var protector = scope.ServiceProvider.GetRequiredService<IPersonalDataProtector>();
        var metrics = new PersonalDataReadMetrics();
        var enabled = new EncryptedFirstPersonalDataReader(protector, Options.Create(new PrivacyProtectionOptions { EncryptedReadEnabled = true }), metrics);
        Assert.Equal("cipher-value", enabled.Read(protector.Protect("cipher-value"), "plain-value")); Assert.Equal("plain-value", enabled.Read(null, "plain-value"));
        Assert.Equal(1, metrics.PlaintextFallbackCount);
        var disabled = new EncryptedFirstPersonalDataReader(protector, Options.Create(new PrivacyProtectionOptions { EncryptedReadEnabled = false }), metrics); Assert.Equal("plain-value", disabled.Read(protector.Protect("cipher-value"), "plain-value"));
        Assert.Equal(1, metrics.PlaintextFallbackCount);
    }

    [Fact]
    public async Task Privacy_backfill_preflight_validates_keyring_hashes_and_existing_ciphertext()
    {
        _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(x => x.LoginId == factory.Credentials[RoleCodes.Customer].LoginId);
        var customer = await db.CustomerProfiles.SingleAsync(x => x.UserId == user.Id);
        var provider = await db.ProviderProfiles.FirstAsync();
        var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId);
        var policy = await db.CategoryPolicies.FirstAsync(x => x.CategoryId == category.Id);
        var area = await db.AdministrativeAreas.SingleAsync(x => x.PublicId == factory.Catalog.AreaId);
        user.Email = $"privacy-{Guid.NewGuid():N}@example.test";
        user.NormalizedEmail = PersonalDataNormalizer.Email(user.Email);
        user.Phone = "010-9876-5432";
        provider.BusinessAddress = "test provider address";
        db.CustomerAddresses.Add(new CustomerAddress { CustomerProfileId = customer.Id, AddressName = "privacy", RecipientName = "recipient", PostalCode = "12345", RoadAddress = "test road", DetailAddress = "test detail" });
        db.ServiceRequests.Add(new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = category.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, DetailAddress = "request detail", Title = "privacy preflight", PolicySnapshotJson = "{}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.SubscriptionRequests.Add(new SubscriptionRequest { CustomerProfileId = customer.Id, ServiceCategoryId = category.Id, AdministrativeAreaId = area.Id, RequestedScopeText = "privacy preflight", PreferredStartDate = DateOnly.FromDateTime(DateTime.Today), DetailAddress = "subscription detail", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<PrivacyBackfillService>().ValidatePreconditionsAsync(default);
        Assert.True(result.EmailHashesChecked >= 1); Assert.True(result.PhoneHashesChecked >= 1); Assert.True(result.ExistingCiphertextVerified >= 8);
    }

    [Fact]
    public async Task Retention_status_is_report_only_and_does_not_delete_data()
    {
        using var client = factory.CreateClient(); await client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = factory.Credentials["ADMIN"].LoginId, factory.Credentials["ADMIN"].Password });
        using var beforeScope = factory.Services.CreateScope(); var beforeDb = beforeScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var before = await beforeDb.PasswordResetRequests.CountAsync();
        var response = await client.GetFromJsonAsync<SoodalLife.Api.Features.Admin.AdminSystemStatusResponse>("/api/v1/admin/system/status");
        Assert.NotNull(response); Assert.All(response.RetentionCandidates, x => Assert.Contains(x.ActionStatus, new[] { "REPORT_ONLY", "NOT_CONFIGURED" }));
        using var afterScope = factory.Services.CreateScope(); Assert.Equal(before, await afterScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>().PasswordResetRequests.CountAsync());
    }

    private static OutboxEvent Event(string type, string payload) => new() { AggregateType = "AutomationTest", AggregatePublicId = Guid.NewGuid(), EventType = type, PayloadJson = payload, StatusCode = "PENDING", OccurredAt = DateTime.UtcNow, AvailableAt = DateTime.UtcNow, IdempotencyKey = $"automation:{Guid.NewGuid():N}" };
    private sealed class TestJob(string name, AutomationConfigurationStatus status) : IAutomationJob { public string Name => name; public AutomationConfigurationStatus ConfigurationStatus => status; public TimeSpan Interval => TimeSpan.FromHours(1); public Task<AutomationJobResult> ExecuteAsync(CancellationToken token) => Task.FromResult(new AutomationJobResult(1)); }
}
