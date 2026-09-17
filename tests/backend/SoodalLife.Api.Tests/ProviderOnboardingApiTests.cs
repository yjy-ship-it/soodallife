using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderOnboardingApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact] public void StoredFilePurposeConstraint_AllowsProviderPromotionImages()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var constraint = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StoredFile))!.GetCheckConstraints()
            .Single(x => x.Name == "CK_files_purpose").Sql;
        Assert.Contains("PROVIDER_PUBLIC_LOGO", constraint);
        Assert.Contains("PROVIDER_PUBLIC_PHOTO", constraint);
    }

    [Fact] public async Task LoginIdAvailability_ReturnsTrimmedNormalizedValue()
    {
        using var client = Client();
        var loginId = $"provider-{Guid.NewGuid():N}";
        var response = await client.GetAsync($"/api/v1/public/provider-registration/availability/login-id?value=%20{loginId}%20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("available").GetBoolean());
        Assert.Equal(loginId, body.RootElement.GetProperty("normalizedValue").GetString());
    }

    [Fact] public async Task PublicProviderRegistration_CreatesPendingInactiveProvider()
    {
        using var client = Client(); var credential = NewCredential();
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProviderRegistrationResponse>();
        Assert.Equal("PENDING", body!.ApprovalStatus); Assert.Equal("INACTIVE", body.ActivityStatus); Assert.Contains(RoleCodes.Customer, body.Roles); Assert.Contains(RoleCodes.Provider, body.Roles);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var userId = await db.Users.Where(x => x.PublicId == body.UserId).Select(x => x.Id).SingleAsync();
        var profileId = await db.ProviderProfiles.Where(x => x.PublicId == body.ProviderId).Select(x => x.Id).SingleAsync();
        Assert.True(await db.CustomerProfiles.AnyAsync(x => x.UserId == userId));
        var wallet = await db.ProviderWallets.SingleAsync(x => x.ProviderProfileId == profileId && x.CurrencyCode == "KRW");
        Assert.Equal(0, wallet.AvailableBalance); Assert.Equal(0, wallet.ReservedBalance); Assert.Equal("ACTIVE", wallet.StatusCode);
    }

    [Fact] public async Task PublicProviderRegistration_SignsInCreatedProvider()
    {
        using var client = Client(); var credential = NewCredential();
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var currentUser = await client.GetFromJsonAsync<AuthenticatedUserResponse>("/api/v1/me");

        Assert.Equal(credential.LoginId, currentUser!.LoginId);
        Assert.Contains(RoleCodes.Customer, currentUser.Roles);
        Assert.Contains(RoleCodes.Provider, currentUser.Roles);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/providers/me")).StatusCode);
    }

    [Fact] public async Task PublicProviderRegistration_AllowsDuplicateCheckedPhoneWithoutIdentityToken()
    {
        using var client = Client(); var credential = NewCredential();
        var input = new { loginId = credential.LoginId, password = credential.Password, passwordConfirmation = credential.Password,
            email = (string?)null, phone = $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}", phoneVerificationToken = (string?)null, providerTypeCode = "BUSINESS",
            businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자",
            businessRegistrationNumber = BusinessNumber(), businessAddress = "서울시 테스트구", businessTypeText = "서비스",
            businessItemText = "생활서비스", introduction = "테스트 전문가", consents = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", input);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] public async Task PublicProviderRegistration_RejectsIndividualProviderType()
    {
        using var client = Client(); var credential = NewCredential();
        var input = new { loginId = credential.LoginId, password = credential.Password, passwordConfirmation = credential.Password,
            email = (string?)null, phone = $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}", phoneVerificationToken = (string?)null, providerTypeCode = "INDIVIDUAL",
            businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자",
            businessRegistrationNumber = (string?)null, businessAddress = (string?)null, businessTypeText = (string?)null,
            businessItemText = (string?)null, introduction = "테스트 전문가", consents = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", input);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("BUSINESS_PROVIDER_ONLY", body.RootElement.GetProperty("businessCode").GetString());
    }

    [Fact] public async Task ProviderRegistration_PreservesCustomerRole_WhenRoleIsAdded()
    {
        var credential = NewCredential(); await CreateCustomer(credential); using var client = Client(); await Login(client, credential);
        var response = await client.PostAsJsonAsync("/api/v1/provider-registration/role", ExistingRoleInput());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProviderRegistrationResponse>();
        Assert.Contains(RoleCodes.Customer, body!.Roles); Assert.Contains(RoleCodes.Provider, body.Roles);
        var currentUser = await client.GetFromJsonAsync<AuthenticatedUserResponse>("/api/v1/me");
        Assert.Contains(RoleCodes.Customer, currentUser!.Roles); Assert.Contains(RoleCodes.Provider, currentUser.Roles);
    }

    [Fact] public async Task ProviderRegistration_RejectsAddingProviderRoleTwice()
    {
        var credential = NewCredential(); await CreateCustomer(credential); using var client = Client(); await Login(client, credential);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/provider-registration/role", ExistingRoleInput())).StatusCode);

        var repeated = await client.PostAsJsonAsync("/api/v1/provider-registration/role", ExistingRoleInput());

        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        using var body = JsonDocument.Parse(await repeated.Content.ReadAsStringAsync());
        Assert.Equal("PROVIDER_ROLE_ALREADY_EXISTS", body.RootElement.GetProperty("businessCode").GetString());
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

    [Fact] public async Task ProviderPromotionLogo_UploadsFiveMbLimitedImage_AndOnlyCurrentImageIsPublic()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        using var firstContent = PromotionImageUpload("file", 1);
        var firstResponse = await provider.PostAsync("/api/v1/providers/me/promotion-images/logo", firstContent);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ProviderProfileResponse>();
        Assert.StartsWith("/api/v1/provider-promotion-images/", first!.PublicLogoUrl);
        using var anonymous = Client();
        var publicImage = await anonymous.GetAsync(first.PublicLogoUrl);
        Assert.Equal(HttpStatusCode.OK, publicImage.StatusCode);
        Assert.Equal("image/png", publicImage.Content.Headers.ContentType!.MediaType);

        using var replacementContent = PromotionImageUpload("file", 1);
        var replacement = await (await provider.PostAsync("/api/v1/providers/me/promotion-images/logo", replacementContent))
            .Content.ReadFromJsonAsync<ProviderProfileResponse>();
        Assert.NotEqual(first.PublicLogoUrl, replacement!.PublicLogoUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(first.PublicLogoUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(replacement.PublicLogoUrl)).StatusCode);
        var validProfile = replacement with { RepresentativeName = replacement.RepresentativeName ?? "대표자", ContactName = replacement.ContactName ?? "담당자" };
        var profileUpdate = await provider.PutAsJsonAsync("/api/v1/providers/me", validProfile);
        Assert.True(profileUpdate.IsSuccessStatusCode, await profileUpdate.Content.ReadAsStringAsync());
    }

    [Fact] public async Task ProviderPromotionStorageStatus_VerifiesCurrentProviderWriteAccess()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var response = await provider.GetAsync("/api/v1/providers/me/promotion-images/storage-status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<ProviderPromotionStorageStatusResponse>();
        Assert.True(status!.Writable); Assert.Contains("사용할 수", status.Message);
    }

    [Fact] public async Task ProviderPromotionImages_AcceptSequentialJsonContentWithoutMultipart()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
        var logoResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/logo-content",
            new { fileName = "logo.png", contentType = "image/png", base64Content = png });
        Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);
        var logo = await logoResponse.Content.ReadFromJsonAsync<ProviderProfileResponse>();
        Assert.StartsWith("/api/v1/provider-promotion-images/", logo!.PublicLogoUrl);

        var firstResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/photo-content",
            new { file = new { fileName = "first.png", contentType = "image/png", base64Content = png }, replaceExisting = true });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ProviderProfileResponse>(); Assert.Single(first!.PublicPhotoUrls);
        var duplicateResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/photo-content",
            new { file = new { fileName = "first.png", contentType = "image/png", base64Content = png }, replaceExisting = false });
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<ProviderProfileResponse>();
        Assert.Single(duplicate!.PublicPhotoUrls);
    }

    [Fact] public async Task ProviderPromotionImageChunks_ReassembleAndAllowFinalRetry()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var signature = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var png = new byte[70 * 1024]; signature.CopyTo(png, 0);
        var uploadId = Guid.NewGuid(); const int split = 64 * 1024;
        var firstResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/chunks", new
        {
            uploadId, purpose = "LOGO", replaceExisting = true, fileName = "chunked-logo.png", contentType = "image/png",
            chunkIndex = 0, totalChunks = 2, base64Chunk = Convert.ToBase64String(png[..split])
        });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ProviderPromotionImageChunkResponse>();
        Assert.False(first!.Completed); Assert.Null(first.Profile);

        var lastChunk = new
        {
            uploadId, purpose = "LOGO", replaceExisting = true, fileName = "chunked-logo.png", contentType = "image/png",
            chunkIndex = 1, totalChunks = 2, base64Chunk = Convert.ToBase64String(png[split..])
        };
        var completedResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/chunks", lastChunk);
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var completed = await completedResponse.Content.ReadFromJsonAsync<ProviderPromotionImageChunkResponse>();
        Assert.True(completed!.Completed); Assert.EndsWith(uploadId.ToString(), completed.Profile!.PublicLogoUrl);

        var retryResponse = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/chunks", lastChunk);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retry = await retryResponse.Content.ReadFromJsonAsync<ProviderPromotionImageChunkResponse>();
        Assert.True(retry!.Completed); Assert.Equal(completed.Profile.PublicLogoUrl, retry.Profile!.PublicLogoUrl);
    }

    [Fact] public async Task ProviderPromotionImageChunks_RecoversStoredButUnlinkedLogo()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var uploadId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var loginId = factory.Credentials[RoleCodes.Provider].LoginId;
            var identity = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                                  where user.LoginId == loginId select new { user.Id, Profile = profile }).SingleAsync();
            var key = $"provider-promotion/{identity.Profile.PublicId:N}/{uploadId:N}.png";
            db.Files.Add(new StoredFile
            {
                PublicId = uploadId, PurposeCode = "PROVIDER_PUBLIC_LOGO", StorageContainer = "development-private",
                StorageKey = key, StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)), OriginalFileName = "recover.png",
                ContentType = "image/png", SizeBytes = 68, Sha256Hex = new string('a', 64), StatusCode = "ACTIVE",
                MalwareScanStatusCode = "NOT_INTEGRATED", PrivacyInspectionStatusCode = "NOT_INTEGRATED",
                SanitizationStatusCode = "NOT_INTEGRATED", UploadedByUserId = identity.Id, CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        var response = await provider.PostAsJsonAsync("/api/v1/providers/me/promotion-images/chunks", new
        {
            uploadId, purpose = "LOGO", replaceExisting = true, fileName = "recover.png", contentType = "image/png",
            chunkIndex = 0, totalChunks = 1, base64Chunk = "AA=="
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recovered = await response.Content.ReadFromJsonAsync<ProviderPromotionImageChunkResponse>();
        Assert.True(recovered!.Completed); Assert.EndsWith(uploadId.ToString(), recovered.Profile!.PublicLogoUrl);
    }

    [Fact] public async Task ProviderPromotionPhotos_AcceptsAtMostFiveImages()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        using var five = PromotionImageUpload("files", 5);
        var response = await provider.PostAsync("/api/v1/providers/me/promotion-images/photos", five);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProviderProfileResponse>();
        Assert.Equal(5, profile!.PublicPhotoUrls.Count);
        using var six = PromotionImageUpload("files", 6);
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PostAsync("/api/v1/providers/me/promotion-images/photos", six)).StatusCode);
    }

    [Fact] public async Task ProviderPromotionImage_RejectsOversizeAndMismatchedContent()
    {
        using var provider = Client(); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var oversizeBytes = new byte[5 * 1024 * 1024 + 1]; oversizeBytes[0] = 0xff; oversizeBytes[1] = 0xd8; oversizeBytes[2] = 0xff;
        using var oversize = PromotionImageUpload("file", 1, oversizeBytes, "image/jpeg", "large.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PostAsync("/api/v1/providers/me/promotion-images/logo", oversize)).StatusCode);
        using var mismatch = PromotionImageUpload("file", 1, Encoding.UTF8.GetBytes("not an image"));
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PostAsync("/api/v1/providers/me/promotion-images/logo", mismatch)).StatusCode);
    }

    [Fact] public async Task PendingProvider_CanConfigureOnboardingService_ButRemainsInactive()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var services = await response.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>();
            Assert.Equal("APPROVED", Assert.Single(services!).ApprovalStatus);
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
            var reloaded = await client.GetFromJsonAsync<List<ProviderServiceAreaResponse>>("/api/v1/providers/me/service-areas");
            var saved = Assert.Single(reloaded!); Assert.Equal(factory.Catalog.ServiceId, saved.ServiceCategoryId); Assert.Equal(factory.Catalog.AreaId, Assert.Single(saved.Areas).Id);
        }
    }

    [Fact] public async Task ProjectService_CanBeSelectedByProvider()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var categoryId = await db.ServiceCategories.Where(value => value.PublicId == factory.Catalog.ServiceId).Select(value => value.Id).SingleAsync();
        var policy = await db.CategoryPolicies.SingleAsync(value => value.CategoryId == categoryId);
        var original = policy.TransactionTypeCode;
        policy.TransactionTypeCode = "PROJECT";
        await db.SaveChangesAsync();
        try
        {
            var (client, _) = await RegisterAndLogin(); using (client)
            {
                var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Single((await response.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);
            }
        }
        finally
        {
            policy.TransactionTypeCode = original;
            await db.SaveChangesAsync();
        }
    }

    [Fact] public async Task NationwideNetwork_IsSelfSelectedWithoutCoverageApprovalOrEvidence()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var categoryId = await db.ServiceCategories.Where(value => value.PublicId == factory.Catalog.ServiceId).Select(value => value.Id).SingleAsync();
        var operation = await db.CategoryOperationPolicies.SingleAsync(value => value.CategoryId == categoryId && value.IsActive);
        var original = operation.CoverageTypeCode;
        operation.CoverageTypeCode = ProviderCoveragePolicy.NationwideNetwork;
        await db.SaveChangesAsync();
        try
        {
            var (client, _) = await RegisterAndLogin(); using (client)
            {
                var serviceResponse = await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
                var registered = Assert.Single((await serviceResponse.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);
                Assert.Equal("APPROVED", registered.ApprovalStatus);
                var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { services = Array.Empty<object>(), nationwideServiceCategoryIds = new[] { factory.Catalog.ServiceId } });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var saved = Assert.Single((await response.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>())!);
                Assert.True(saved.IsNationwide);
                Assert.True(saved.NationwideAllowed);
                Assert.Equal(ProviderCoveragePolicy.NationwideNetwork, saved.CoverageTypeCode);
                Assert.Equal("전국 방문망 제공", saved.CoverageTypeName);
            }
        }
        finally
        {
            operation.CoverageTypeCode = original;
            await db.SaveChangesAsync();
        }
    }

    [Fact] public async Task ProviderArea_MiddleSelectionAppliesToEveryActiveChildService()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId, factory.Catalog.OtherServiceId } });
            Guid middleId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
                var parentId = await db.ServiceCategories.Where(value => value.PublicId == factory.Catalog.ServiceId).Select(value => value.ParentId).SingleAsync();
                middleId = await db.ServiceCategories.Where(value => value.Id == parentId).Select(value => value.PublicId).SingleAsync();
            }
            var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { middles = new[] { new { middleCategoryId = middleId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groups = await response.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>();
            Assert.NotNull(groups); Assert.Equal(2, groups.Count); Assert.All(groups, group => { Assert.Equal(middleId, group.MiddleCategoryId); Assert.Single(group.Areas); });
        }
    }

    [Fact] public async Task ProviderArea_RequiresServiceSelectionFirst()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            var response = await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { middles = Array.Empty<object>() });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("PROVIDER_SERVICE_REQUIRED_FOR_AREA", body.RootElement.GetProperty("businessCode").GetString());
        }
    }

    [Fact] public async Task ProviderDashboard_UsesRealOnboardingCounts()
    {
        var (client, _) = await RegisterAndLogin(); using (client)
        {
            await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } });
            var dashboard = await client.GetFromJsonAsync<ProviderOnboardingDashboardResponse>("/api/v1/providers/me/onboarding-dashboard");
            Assert.Equal(1, dashboard!.RegisteredServiceCount); Assert.Equal(0, dashboard.PendingServiceCount); Assert.Equal("PENDING", dashboard.ApprovalStatus);
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

    [Fact] public async Task ProviderOnboarding_CreatesOneZeroWalletButNoLedgerFeeOrTrustEvents()
    {
        long wallets, ledger, fees, trust; using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); wallets = await db.ProviderWallets.LongCountAsync(); ledger = await db.WalletLedgerEntries.LongCountAsync(); fees = await db.FeeCharges.LongCountAsync(); trust = await db.TrustScoreEvents.LongCountAsync(); }
        var (client, _) = await RegisterAndLogin(); using (client) { await client.PutAsJsonAsync("/api/v1/providers/me/service-categories", new { categoryIds = new[] { factory.Catalog.ServiceId } }); await client.PutAsJsonAsync("/api/v1/providers/me/service-areas", new { services = new[] { new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } } } }); }
        using var verify = factory.Services.CreateScope(); var current = verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); Assert.Equal(wallets + 1, await current.ProviderWallets.LongCountAsync()); Assert.Equal(ledger, await current.WalletLedgerEntries.LongCountAsync()); Assert.Equal(fees, await current.FeeCharges.LongCountAsync()); Assert.Equal(trust, await current.TrustScoreEvents.LongCountAsync());
    }

    private async Task<(HttpClient Client, TestCredential Credential)> RegisterAndLogin()
    {
        var client = Client(); var credential = NewCredential(); var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", Registration(credential)); Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync()); await Login(client, credential); return (client, credential);
    }
    private async Task CreateCustomer(TestCredential credential)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>(); var user = new User { LoginId = credential.LoginId, NormalizedLoginId = credential.LoginId.ToUpperInvariant(), Phone = $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}", StatusCode = "ACTIVE", PhoneVerificationStatusCode = "VERIFIED", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; user.PasswordHash = hasher.HashPassword(user, credential.Password); db.Users.Add(user); await db.SaveChangesAsync(); var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.Customer); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = DateTime.UtcNow }); db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = "Role preservation", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync();
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
    private static object Registration(TestCredential credential, string? email = null) => new { loginId = credential.LoginId, password = credential.Password, passwordConfirmation = credential.Password, email, phone = $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}", phoneVerificationToken = (string?)null, providerTypeCode = "BUSINESS", businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자", businessRegistrationNumber = BusinessNumber(), businessAddress = "서울시 테스트구", businessTypeText = "서비스", businessItemText = "생활서비스", introduction = "테스트 전문가", consents = Array.Empty<object>() };
    private static object ExistingRoleInput() => new { providerTypeCode = "BUSINESS", businessName = $"Provider {Guid.NewGuid():N}", representativeName = "대표자", contactName = "담당자", businessRegistrationNumber = BusinessNumber(), businessAddress = "서울시 테스트구", businessTypeText = "서비스", businessItemText = "생활서비스", introduction = "복수 역할", consents = Array.Empty<object>() };
    private static string BusinessNumber()
    {
        var firstNine = RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000).ToString();
        var digits = firstNine.Select(value => value - '0').ToArray();
        int[] weights = [1, 3, 7, 1, 3, 7, 1, 3, 5];
        var sum = weights.Select((weight, index) => weight * digits[index]).Sum() + digits[8] * 5 / 10;
        return firstNine + ((10 - sum % 10) % 10);
    }
    private static MultipartFormDataContent PdfUpload(Guid typeId, string text) { var body = new MultipartFormDataContent(); body.Add(new StringContent(typeId.ToString()), "documentTypeId"); var bytes = new ByteArrayContent(Encoding.ASCII.GetBytes(text)); bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); body.Add(bytes, "file", "evidence.pdf"); return body; }
    private static MultipartFormDataContent PromotionImageUpload(string field, int count, byte[]? data = null, string contentType = "image/png", string fileName = "promotion.png")
    {
        var body = new MultipartFormDataContent();
        var bytes = data ?? Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        for (var index = 0; index < count; index++)
        {
            var content = new ByteArrayContent(bytes); content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            body.Add(content, field, count == 1 ? fileName : $"promotion-{index + 1}.png");
        }
        return body;
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static TestCredential NewCredential() => new($"provider-{Guid.NewGuid():N}", "Valid!Provider123");
    private static async Task Login(HttpClient client, TestCredential credential) { var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginOrEmail = credential.LoginId, password = credential.Password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
}
