using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderOnboardingApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact] public async Task PublicProviderRegistration_CreatesPendingInactiveProvider()
    {
        using var client = Client(); var credential = NewCredential();
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProviderRegistrationResponse>();
        Assert.Equal("PENDING", body!.ApprovalStatus); Assert.Equal("INACTIVE", body.ActivityStatus); Assert.Contains(RoleCodes.Provider, body.Roles);
    }

    [Fact] public async Task ProviderRegistration_PreservesCustomerRole_WhenRoleIsAdded()
    {
        var credential = NewCredential(); await CreateCustomer(credential); using var client = Client(); await Login(client, credential);
        var response = await client.PostAsJsonAsync("/api/v1/provider-registration/role", ExistingRoleInput());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProviderRegistrationResponse>();
        Assert.Contains(RoleCodes.Customer, body!.Roles); Assert.Contains(RoleCodes.Provider, body.Roles);
    }

    [Fact] public async Task ProviderRegistration_RejectsDuplicateLoginId()
    {
        using var client = Client(); var credential = NewCredential(); Assert.True((await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential))).IsSuccessStatusCode);
        var repeated = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [Fact] public async Task ProviderRegistration_RejectsDuplicateEmail()
    {
        using var client = Client(); var first = NewCredential(); var email = $"{Guid.NewGuid():N}@example.test";
        Assert.True((await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(first, email))).IsSuccessStatusCode);
        var second = NewCredential(); var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(second, email));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact] public async Task ProviderRegistration_EnforcesPasswordPolicy()
    {
        using var client = Client(); var credential = new TestCredential($"provider-{Guid.NewGuid():N}", "weak");
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] public async Task ProviderProfile_IsOwnerScopedAndCustomerIsForbidden()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        Assert.Equal(HttpStatusCode.OK, (await provider.GetAsync("/api/v1/providers/me")).StatusCode);
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/providers/me")).StatusCode);
    }

    [Fact] public async Task PendingProvider_CanConfigureOnboardingService_ButRemainsInactive()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var services = await response.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>();
            Assert.Equal("PENDING", Assert.Single(services!).ApprovalStatus);
            var profile = await client.GetFromJsonAsync<ProviderProfileResponse>("/api/v1/providers/me"); Assert.Equal("INACTIVE", profile!.ActivityStatus);
        }
    }

    [Fact] public async Task ServiceSelection_CreatesPolicyRequirementsWithoutInventingGlobalRequirements()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            var items = await client.GetFromJsonAsync<List<ProviderRequirementResponse>>("/api/v1/providers/me/requirements");
            var requirement = Assert.Single(items!); Assert.Equal(factory.Catalog.ServiceId, requirement.ServiceCategoryId); Assert.True(requirement.IsRequired);
        }
    }

    [Fact] public async Task ProviderArea_UsesServiceSpecificSigunguSelection()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); var groups = await response.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>(); Assert.Single(Assert.Single(groups!).Areas);
        }
    }

    [Fact] public async Task ProviderDashboard_UsesRealOnboardingCounts()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            var dashboard = await client.GetFromJsonAsync<ProviderOnboardingDashboardResponse>("/api/v1/providers/me/onboarding-dashboard");
            Assert.Equal(1, dashboard!.RegisteredServiceCount); Assert.Equal(1, dashboard.PendingServiceCount); Assert.Equal("PENDING", dashboard.ApprovalStatus);
        }
    }

    [Fact] public async Task ProviderDocumentUpload_ValidatesSignatureAndStoresNotIntegratedScanState()
    {
        await EnsureEvidencePolicy();
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            var type = Assert.Single((await client.GetFromJsonAsync<List<ProviderDocumentTypeResponse>>("/api/v1/providers/me/document-types"))!);
            using var content = PdfUpload(type.Id, "%PDF-1.4\nprovider evidence"); var response = await client.PostAsync("/api/v1/providers/me/documents", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); var document = await response.Content.ReadFromJsonAsync<ProviderDocumentResponse>(); Assert.Equal("NOT_INTEGRATED", document!.MalwareStatus);
        }
    }

    [Fact] public async Task ProviderDocumentUpload_RejectsMismatchedSignature()
    {
        await EnsureEvidencePolicy();
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            var type = Assert.Single((await client.GetFromJsonAsync<List<ProviderDocumentTypeResponse>>("/api/v1/providers/me/document-types"))!);
            using var content = PdfUpload(type.Id, "not-a-pdf"); var response = await client.PostAsync("/api/v1/providers/me/documents", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact] public async Task ProviderDocument_IsDownloadableByOwnerOnly()
    {
        await EnsureEvidencePolicy();
        var (owner, _) = await RegisterAndLogin(); using (owner)
        {
            var type = Assert.Single((await owner.GetFromJsonAsync<List<ProviderDocumentTypeResponse>>("/api/v1/providers/me/document-types"))!);
            using var content = PdfUpload(type.Id, "%PDF-1.4\nprivate"); var uploaded = await (await owner.PostAsync("/api/v1/providers/me/documents", content)).Content.ReadFromJsonAsync<ProviderDocumentResponse>();
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/providers/me/documents/{uploaded!.FileId}/content")).StatusCode);
            using var other = Client(); await Login(other, factory.Credentials[RoleCodes.Provider]);
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/providers/me/documents/{uploaded.FileId}/content")).StatusCode);
        }
    }

    [Fact] public async Task EvidenceLink_RequiresOwnedAcceptedDocumentType()
    {
        await EnsureEvidencePolicy();
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            var requirement = Assert.Single((await client.GetFromJsonAsync<List<ProviderRequirementResponse>>("/api/v1/providers/me/requirements"))!);
            var type = Assert.Single(requirement.AcceptedEvidenceTypes); using var content = PdfUpload(type.DocumentTypeId, "%PDF-1.4\naccepted");
            var document = await (await client.PostAsync("/api/v1/providers/me/documents", content)).Content.ReadFromJsonAsync<ProviderDocumentResponse>();
            var linked = await client.PutAsJsonAsync($"/api/v1/providers/me/requirements/{requirement.VerificationId}/evidence", new { documentId = document!.FileId });
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode); Assert.Equal("PENDING", (await linked.Content.ReadFromJsonAsync<ProviderRequirementResponse>())!.VerificationStatus);
        }
    }

    [Fact] public async Task ProfileResponse_DoesNotExposeInternalPrimaryKeysOrStorageKeys()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var json = await client.GetStringAsync("/api/v1/providers/me");
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public async Task ProviderOnboarding_DoesNotCreateWalletFeeOrTrustEvents()
    {
        long wallets, ledger, fees, trust; using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); wallets = await db.ProviderWallets.LongCountAsync(); ledger = await db.WalletLedgerEntries.LongCountAsync(); fees = await db.FeeCharges.LongCountAsync(); trust = await db.TrustScoreEvents.LongCountAsync(); }
        var (client, _) = await RegisterAndLogin(); using (client) { await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } }); await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } } }); }
        using var verify = factory.Services.CreateScope(); var current = verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Equal(wallets, await current.ProviderWallets.LongCountAsync()); Assert.Equal(ledger, await current.WalletLedgerEntries.LongCountAsync()); Assert.Equal(fees, await current.FeeCharges.LongCountAsync()); Assert.Equal(trust, await current.TrustScoreEvents.LongCountAsync());
    }

    private async Task<(HttpClient Client, TestCredential Credential)> RegisterAndLogin()
    {
        var client = Client(); var credential = NewCredential(); var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential)); Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync()); await Login(client, credential); return (client, credential);
    }
    private async Task CreateCustomer(TestCredential credential)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>(); var user = new User { LoginId = credential.LoginId, NormalizedLoginId = credential.LoginId.ToUpperInvariant(), StatusCode = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; user.PasswordHash = hasher.HashPassword(user, credential.Password); db.Users.Add(user); await db.SaveChangesAsync(); var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Customer); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = DateTime.UtcNow }); db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = "Role preservation", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync();
    }
    private async Task EnsureEvidencePolicy()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        if (await db.ProviderDocumentTypes.AnyAsync(x => x.Code == "TEST_QUALIFICATION")) return;
        var assignment = await db.CategoryProviderRequirementAssignments.FirstAsync();
        var type = new ProviderDocumentType { Code = "TEST_QUALIFICATION", Name = "테스트 자격증", SupportsExpiry = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ProviderDocumentTypes.Add(type); await db.SaveChangesAsync();
        db.CategoryProviderRequirementEvidenceTypes.Add(new CategoryProviderRequirementEvidenceType { RequirementAssignmentId = assignment.Id, DocumentTypeId = type.Id, IsRequired = true, DisplayOrder = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
    private static object Registration(TestCredential credential, string? email = null) => new { loginId = credential.LoginId, password = credential.Password, passwordConfirmation = credential.Password, email, phone = "01012345678", businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자", businessRegistrationNumber = (string?)null, businessAddress = "서울시 테스트구", businessTypeText = "서비스", businessItemText = "생활서비스", introduction = "테스트 공급자", consents = Array.Empty<object>() };
    private static object ExistingRoleInput() => new { businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자", businessRegistrationNumber = (string?)null, businessAddress = "서울시 테스트구", businessTypeText = "서비스", businessItemText = "생활서비스", introduction = "복수 역할", consents = Array.Empty<object>() };
    private static MultipartFormDataContent PdfUpload(Guid typeId, string text) { var body = new MultipartFormDataContent(); body.Add(new StringContent(typeId.ToString()), "documentTypeId"); var bytes = new ByteArrayContent(Encoding.ASCII.GetBytes(text)); bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); body.Add(bytes, "file", "evidence.pdf"); return body; }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static TestCredential NewCredential() => new($"provider-{Guid.NewGuid():N}", "Valid!Provider123");
    private static async Task Login(HttpClient client, TestCredential credential) { var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginOrEmail = credential.LoginId, password = credential.Password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
}
