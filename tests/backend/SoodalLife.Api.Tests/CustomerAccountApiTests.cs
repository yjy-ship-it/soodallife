using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CustomerAccountApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Theory]
    [InlineData("login")]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("password")]
    [InlineData("confirmation")]
    public async Task Registration_RejectsInvalidAccountFields(string field)
    {
        var docs = await EnsureLegalDocuments(); using var client = Client(); var login = Login();
        var value = Registration(login, $"{login}@example.com", docs);
        value = field switch
        {
            "login" => value with { LoginId = "1!" },
            "email" => value with { Email = "invalid" },
            "phone" => value with { Phone = "02-123-4567" },
            "password" => value with { Password = "short7", PasswordConfirmation = "short7" },
            _ => value with { PasswordConfirmation = "Bb!12345678" },
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", value)).StatusCode);
    }

    [Fact]
    public async Task Availability_ChecksLoginIdAndEmail()
    {
        await EnsureLegalDocuments();
        using var client = Client();
        Assert.True((await client.GetFromJsonAsync<AvailabilityResponse>($"/api/v1/public/customer-account/availability/login-id?value={Login()}"))!.Available);
        var credential = factory.Credentials[RoleCodes.Customer];
        Assert.False((await client.GetFromJsonAsync<AvailabilityResponse>($"/api/v1/public/customer-account/availability/login-id?value={credential.LoginId}"))!.Available);
        Assert.True((await client.GetFromJsonAsync<AvailabilityResponse>($"/api/v1/public/customer-account/availability/email?value=test-{Guid.NewGuid():N}@example.com"))!.Available);
    }

    [Fact]
    public async Task Registration_AtomicallyCreatesUserCustomerRoleProfileAndVersionedConsents()
    {
        var docs = await EnsureLegalDocuments();
        using var client = Client(); var login = Login(); var email = $"{login}@example.com";
        var response = await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, email, docs, optional: true));
        response.EnsureSuccessStatusCode();
        var json = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        Assert.DoesNotContain("password", json); Assert.DoesNotContain("hash", json); Assert.DoesNotContain("publicid", json);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(x => x.NormalizedLoginId == login.ToUpper());
        Assert.NotEqual("Aa!12345678", user.PasswordHash);
        Assert.True((await db.UserRoles.CountAsync(x => x.UserId == user.Id && x.RevokedAt == null)) == 1);
        Assert.True(await db.CustomerProfiles.AnyAsync(x => x.UserId == user.Id));
        Assert.Equal(3, await db.UserConsents.CountAsync(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task Registration_BlocksDuplicateLoginAndEmail()
    {
        var docs = await EnsureLegalDocuments(); using var client = Client(); var login = Login(); var email = $"{login}@example.com";
        (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, email, docs))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, $"other-{email}", docs))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(Login(), email.ToUpper(), docs))).StatusCode);
    }

    [Fact]
    public async Task Registration_BlocksMissingRequiredConsent_AndStoresOptionalSeparately()
    {
        var docs = await EnsureLegalDocuments(); using var client = Client();
        var blocked = Registration(Login(), $"{Guid.NewGuid():N}@example.com", docs) with { Consents = docs.Select(x => new RegistrationConsentRequest(x.VersionId, x.RequirementCode != "REQUIRED")).ToArray() };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", blocked)).StatusCode);
        var login = Login(); (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, $"{login}@example.com", docs, optional: false))).EnsureSuccessStatusCode();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var userId = await db.Users.Where(x => x.NormalizedLoginId == login.ToUpper()).Select(x => x.Id).SingleAsync();
        Assert.Equal(2, await db.UserConsents.CountAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task LegalDocuments_ReturnCurrentVersionAndPlaceholderFlag()
    {
        await EnsureLegalDocuments(); using var client = Client();
        var docs = await client.GetFromJsonAsync<List<LegalDocumentResponse>>("/api/v1/public/customer-account/legal-documents");
        Assert.Equal(3, docs!.Count); Assert.All(docs, x => Assert.True(x.IsPlaceholder)); Assert.Equal(2, docs.Count(x => x.RequirementCode == "REQUIRED"));
    }

    [Fact]
    public async Task Profile_IsCustomerOwned_AndUpdatesOnlyAllowedFields()
    {
        var (_, client) = await RegisteredClient();
        var profile = await client.GetFromJsonAsync<CustomerProfileResponse>("/api/v1/customer/account/profile"); Assert.NotNull(profile); Assert.DoesNotContain("@", profile.LoginId);
        var updated = await (await client.PutAsJsonAsync("/api/v1/customer/account/profile", new UpdateCustomerProfileRequest("변경 고객", "changed@example.com", "010-9999-8888"))).Content.ReadFromJsonAsync<CustomerProfileResponse>();
        Assert.Equal("변경 고객", updated!.Name); Assert.Equal("010-9999-8888", updated.Phone); Assert.Equal("NOT_INTEGRATED", updated.PhoneVerificationStatus);
    }

    [Fact]
    public async Task ProviderCannotUseCustomerAccountApi_AndAdminRouteStillWorks()
    {
        using var provider = Client(); await LoginAs(provider, RoleCodes.Provider);
        Assert.Equal(HttpStatusCode.Forbidden, (await provider.GetAsync("/api/v1/customer/account/profile")).StatusCode);
        using var admin = Client(); await LoginAs(admin, RoleCodes.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/dashboard/summary")).StatusCode);
    }

    [Fact]
    public async Task Address_CreateUpdateDeactivate_AndKeepsSingleDefault()
    {
        var (_, client) = await RegisteredClient();
        var first = await CreateAddress(client, "우리집", true); var second = await CreateAddress(client, "회사", true);
        var items = await client.GetFromJsonAsync<List<CustomerAddressResponse>>("/api/v1/customer/account/addresses");
        Assert.Single(items!, x => x.IsDefault); Assert.True(items!.Single(x => x.Id == second.Id).IsDefault);
        var updatedResponse = await client.PutAsJsonAsync($"/api/v1/customer/account/addresses/{first.Id}", Address("부모님댁", false, first.ConcurrencyToken)); updatedResponse.EnsureSuccessStatusCode();
        var updated = await updatedResponse.Content.ReadFromJsonAsync<CustomerAddressResponse>(); Assert.Equal("부모님댁", updated!.AddressName);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/customer/account/addresses/{first.Id}?concurrencyToken={Uri.EscapeDataString(updated.ConcurrencyToken)}")).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<CustomerAddressResponse>>("/api/v1/customer/account/addresses"))!, x => x.Id == first.Id);
    }

    [Fact]
    public async Task OtherCustomerAddress_IsHiddenAsNotFound()
    {
        var (_, owner) = await RegisteredClient(); var address = await CreateAddress(owner, "소유자 주소", false);
        var (_, other) = await RegisteredClient();
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/v1/customer/account/addresses/{address.Id}", Address("침범", false, address.ConcurrencyToken))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/v1/customer/account/addresses/{address.Id}")).StatusCode);
    }

    [Fact]
    public async Task PasswordChange_RequiresCurrentPassword_StoresHashAndWritesSafeAudit()
    {
        var (login, client) = await RegisteredClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/customer/account/password/change", new ChangePasswordRequest("wrong", "Bb!12345678", "Bb!12345678"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/customer/account/password/change", new ChangePasswordRequest("Aa!12345678", "Bb!12345678", "Bb!12345678"))).StatusCode);
        using var fresh = Client(); Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(login, "Bb!12345678"))).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var audit = await db.AuditLogs.SingleAsync(x => x.ActionCode == "CUSTOMER_PASSWORD_CHANGED" && x.ActorRoleCode == RoleCodes.Customer);
        Assert.Null(audit.BeforeJson); Assert.Null(audit.AfterJson); Assert.Null(audit.MetadataJson);
    }

    [Fact]
    public async Task PasswordResetStoresOnlyHashesAndRejectsExpiredOrUsedToken()
    {
        var (login, _) = await RegisteredClient(); using var anonymous = Client();
        Assert.Equal(HttpStatusCode.Accepted, (await anonymous.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/requests", new PasswordResetRequestInput(login))).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var generated = await db.PasswordResetRequests.OrderByDescending(x => x.Id).FirstAsync(); Assert.Equal(32, generated.TokenHash.Length); Assert.DoesNotContain(login, generated.RequestedIdentifierMasked);
        var userId = await db.Users.Where(x => x.NormalizedLoginId == login.ToUpper()).Select(x => x.Id).SingleAsync();
        foreach (var pair in new[] { ("expired-token", DateTime.UtcNow.AddMinutes(-1), (DateTime?)null), ("used-token", DateTime.UtcNow.AddMinutes(10), (DateTime?)DateTime.UtcNow) })
            db.PasswordResetRequests.Add(new PasswordResetRequest { UserId = userId, RequestedIdentifierHash = Hash(login), RequestedIdentifierMasked = "te***t", TokenHash = Hash(pair.Item1), ExpiresAt = pair.Item2, UsedAt = pair.Item3, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/confirm", new ConfirmPasswordResetRequest("expired-token", "Cc!12345678", "Cc!12345678"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/confirm", new ConfirmPasswordResetRequest("used-token", "Cc!12345678", "Cc!12345678"))).StatusCode);
    }

    [Fact]
    public async Task ConsentCanChangeOptionalButCannotWithdrawRequired()
    {
        var (_, client) = await RegisteredClient(optional: true); var items = await client.GetFromJsonAsync<List<ConsentResponse>>("/api/v1/customer/account/consents");
        var optional = items!.Single(x => x.RequirementCode == "OPTIONAL"); var required = items!.First(x => x.RequirementCode == "REQUIRED");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/v1/customer/account/consents", new UpdateConsentRequest(optional.LegalDocumentVersionId, false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/customer/account/consents", new UpdateConsentRequest(required.LegalDocumentVersionId, false))).StatusCode);
    }

    [Fact]
    public async Task WithdrawalRequestDoesNotRevokeRolesOrWithdrawWholeAccount()
    {
        var (login, client) = await RegisteredClient();
        var response = await client.PostAsJsonAsync("/api/v1/customer/account/withdrawal-requests", new CreateWithdrawalRequest("CUSTOMER_ROLE", "테스트")); response.EnsureSuccessStatusCode();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var user = await db.Users.SingleAsync(x => x.NormalizedLoginId == login.ToUpper());
        Assert.Equal("ACTIVE", user.StatusCode); Assert.True(await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RevokedAt == null));
    }

    [Fact]
    public async Task AccountPayloadsDoNotExposeInternalIdsHashesSecretsOrRowVersionNames()
    {
        var (_, client) = await RegisteredClient(); await CreateAddress(client, "보안 확인", false);
        foreach (var path in new[] { "/api/v1/customer/account/profile", "/api/v1/customer/account/addresses", "/api/v1/customer/account/consents" })
        {
            var json = (await (await client.GetAsync(path)).Content.ReadAsStringAsync()).ToLowerInvariant();
            Assert.DoesNotContain("passwordhash", json); Assert.DoesNotContain("tokenhash", json); Assert.DoesNotContain("rowversion", json); Assert.DoesNotContain("customerprofileid", json); Assert.DoesNotContain("userid", json);
        }
    }

    [Fact]
    public async Task IdentityVerificationAdapterReportsNotIntegratedWithoutFakeVerification()
    {
        var adapter = new NotIntegratedIdentityVerificationAdapter();
        var status = await adapter.GetStatusAsync(CancellationToken.None);
        Assert.Equal("NOT_INTEGRATED", status!.StatusCode); Assert.False(status.IsVerified);
    }

    [Fact]
    public async Task AnonymousCannotReadCustomerProfile()
    { using var client = Client(); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/customer/account/profile")).StatusCode); }

    [Fact]
    public async Task RegisteredCustomerCanLoginWithNormalizedEmail()
    {
        var docs = await EnsureLegalDocuments(); using var client = Client(); var login = Login(); var email = $"{login}@Example.COM";
        (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, email, docs))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email.ToUpperInvariant(), "Aa!12345678"))).StatusCode);
    }

    [Fact]
    public async Task PasswordResetRequestForUnknownIdentifierIsUniformAndDoesNotReturnToken()
    {
        using var client = Client(); var response = await client.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/requests", new PasswordResetRequestInput($"unknown-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidPasswordResetMarksTokenUsedAndCannotReuseIt()
    {
        var (login, _) = await RegisteredClient(); const string resetToken = "one-time-reset-token";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var userId = await db.Users.Where(x => x.NormalizedLoginId == login.ToUpper()).Select(x => x.Id).SingleAsync();
            db.PasswordResetRequests.Add(new PasswordResetRequest { UserId = userId, RequestedIdentifierHash = Hash(login), RequestedIdentifierMasked = "c***1", TokenHash = Hash(resetToken), ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow }); await db.SaveChangesAsync();
        }
        using var client = Client(); var input = new ConfirmPasswordResetRequest(resetToken, "Dd!12345678", "Dd!12345678");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/confirm", input)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/public/customer-account/password-reset/confirm", input)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(login, "Dd!12345678"))).StatusCode);
    }

    [Fact]
    public async Task UnknownCustomerAddressReturnsNotFound()
    { var (_, client) = await RegisteredClient(); Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/customer/account/addresses/{Guid.NewGuid()}")).StatusCode); }

    [Fact]
    public async Task DefaultAddressCannotBeDeletedUntilAnotherAddressIsSelectedAsDefault()
    {
        var (_, client) = await RegisteredClient(); var first = await CreateAddress(client, "기본", true); await CreateAddress(client, "보조", false);
        var denied = await client.DeleteAsync($"/api/v1/customer/account/addresses/{first.Id}?concurrencyToken={Uri.EscapeDataString(first.ConcurrencyToken)}");
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        var items = (await client.GetFromJsonAsync<List<CustomerAddressResponse>>("/api/v1/customer/account/addresses"))!;
        Assert.Single(items, x => x.IsDefault && x.Id == first.Id);
        var second = items.Single(x => x.Id != first.Id);
        var promoted = await client.PutAsJsonAsync($"/api/v1/customer/account/addresses/{second.Id}", Address(second.AddressName, true, second.ConcurrencyToken));
        promoted.EnsureSuccessStatusCode();
        var refreshedFirst = (await client.GetFromJsonAsync<List<CustomerAddressResponse>>("/api/v1/customer/account/addresses"))!.Single(x => x.Id == first.Id);
        (await client.DeleteAsync($"/api/v1/customer/account/addresses/{first.Id}?concurrencyToken={Uri.EscapeDataString(refreshedFirst.ConcurrencyToken)}")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task FirstAddressBecomesDefaultEvenWhenCheckboxIsNotSelected()
    {
        var (_, client) = await RegisteredClient(); var first = await CreateAddress(client, "첫 주소", false);
        Assert.True(first.IsDefault);
    }

    [Fact]
    public async Task RegistrationDoesNotChangeWalletTrustSubscriptionOrInteriorData()
    {
        await using var beforeScope = factory.Services.CreateAsyncScope(); var beforeDb = beforeScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var before = new[] { await beforeDb.ProviderWallets.CountAsync(), await beforeDb.FeeCharges.CountAsync(), await beforeDb.TrustScoreEvents.CountAsync(), await beforeDb.SubscriptionContracts.CountAsync(), await beforeDb.InteriorProjects.CountAsync() };
        await RegisteredClient();
        await using var afterScope = factory.Services.CreateAsyncScope(); var afterDb = afterScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var after = new[] { await afterDb.ProviderWallets.CountAsync(), await afterDb.FeeCharges.CountAsync(), await afterDb.TrustScoreEvents.CountAsync(), await afterDb.SubscriptionContracts.CountAsync(), await afterDb.InteriorProjects.CountAsync() };
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task OptionalConsentCanBeWithdrawnAndAgreedAgain()
    {
        var (_, client) = await RegisteredClient(optional: true); var optional = (await client.GetFromJsonAsync<List<ConsentResponse>>("/api/v1/customer/account/consents"))!.Single(x => x.RequirementCode == "OPTIONAL");
        (await client.PutAsJsonAsync("/api/v1/customer/account/consents", new UpdateConsentRequest(optional.LegalDocumentVersionId, false))).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/v1/customer/account/consents", new UpdateConsentRequest(optional.LegalDocumentVersionId, true))).EnsureSuccessStatusCode();
        Assert.Equal("CONSENTED", (await client.GetFromJsonAsync<List<ConsentResponse>>("/api/v1/customer/account/consents"))!.Single(x => x.LegalDocumentVersionId == optional.LegalDocumentVersionId).ConsentStatus);
    }

    [Fact]
    public async Task WithdrawalRequestIsIdempotentPerScope()
    {
        var (_, client) = await RegisteredClient(); var input = new CreateWithdrawalRequest("CUSTOMER_ROLE", null);
        var first = await (await client.PostAsJsonAsync("/api/v1/customer/account/withdrawal-requests", input)).Content.ReadFromJsonAsync<WithdrawalResponse>();
        var second = await (await client.PostAsJsonAsync("/api/v1/customer/account/withdrawal-requests", input)).Content.ReadFromJsonAsync<WithdrawalResponse>();
        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public async Task PasswordHashUsesConfiguredHasherAndNeverPlaintext()
    {
        var (login, _) = await RegisteredClient(); await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = await db.Users.SingleAsync(x => x.NormalizedLoginId == login.ToUpper()); Assert.NotEqual("Aa!12345678", user.PasswordHash); Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, "Aa!12345678"));
    }

    [Fact]
    public async Task AddressBookNeverAppearsInAnotherCustomersList()
    {
        var (_, owner) = await RegisteredClient(); await CreateAddress(owner, "비공개 상세주소", false); var (_, other) = await RegisteredClient();
        Assert.Empty((await other.GetFromJsonAsync<List<CustomerAddressResponse>>("/api/v1/customer/account/addresses"))!);
    }

    private async Task<List<LegalDocumentResponse>> EnsureLegalDocuments()
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        if (!await db.LegalDocuments.AnyAsync())
        {
            var now = DateTime.UtcNow.AddMinutes(-1); var definitions = new[] { ("TERMS_OF_SERVICE", "REQUIRED", "이용약관"), ("PRIVACY_POLICY", "REQUIRED", "개인정보 필수동의"), ("MARKETING_CONSENT", "OPTIONAL", "마케팅 수신 동의") };
            var order = 0;
            foreach (var definition in definitions)
            {
                var doc = new LegalDocument { Code = definition.Item1, AudienceCode = "CUSTOMER", RequirementCode = definition.Item2, DisplayOrder = ++order, IsActive = true, IsPlaceholder = true, CreatedAt = now, UpdatedAt = now };
                db.LegalDocuments.Add(doc); await db.SaveChangesAsync();
                db.LegalDocumentVersions.Add(new LegalDocumentVersion { LegalDocumentId = doc.Id, VersionNo = 1, Title = definition.Item3, Content = "개발용 Placeholder", EffectiveFrom = now, IsActive = true, IsPlaceholder = true, CreatedAt = now });
            }
            await db.SaveChangesAsync();
        }
        using var client = Client(); return (await client.GetFromJsonAsync<List<LegalDocumentResponse>>("/api/v1/public/customer-account/legal-documents"))!;
    }

    private async Task<(string Login, HttpClient Client)> RegisteredClient(bool optional = false)
    {
        var docs = await EnsureLegalDocuments(); var client = Client(); var login = Login();
        (await client.PostAsJsonAsync("/api/v1/public/customer-account/register", Registration(login, $"{login}@example.com", docs, optional))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(login, "Aa!12345678"))).EnsureSuccessStatusCode();
        return (login, client);
    }
    private async Task<CustomerAddressResponse> CreateAddress(HttpClient client, string name, bool isDefault)
    { var response = await client.PostAsJsonAsync("/api/v1/customer/account/addresses", Address(name, isDefault, null)); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<CustomerAddressResponse>())!; }
    private static SaveCustomerAddressRequest Address(string name, bool isDefault, string? token) => new(name, "홍고객", "12345", "대구광역시 테스트로 1", "101호", null, null, null, isDefault, token);
    private static RegisterCustomerRequest Registration(string login, string email, List<LegalDocumentResponse> docs, bool optional = false) => new(login, "테스트 고객", email, "010-1234-5678", "Aa!12345678", "Aa!12345678", TestIdentityVerificationAdapter.VerificationToken, docs.Select(x => new RegistrationConsentRequest(x.VersionId, x.RequirementCode == "REQUIRED" || optional)).ToArray());
    private static string Login() => $"c{Guid.NewGuid():N}"[..20];
    private async Task LoginAs(HttpClient client, string role) { var credential = factory.Credentials[role]; (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(credential.LoginId, credential.Password))).EnsureSuccessStatusCode(); }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
