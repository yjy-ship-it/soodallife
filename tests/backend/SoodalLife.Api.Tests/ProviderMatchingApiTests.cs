using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderMatchingApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task ProviderCanSeeOptInAddressAndDeclineDistantRequestBeforeQuote()
    {
        Guid requestPublicId;
        long dispatchId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var customer = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile).SingleAsync();
            var providerRow = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select new { Profile = profile, User = user }).SingleAsync();
            var provider = providerRow.Profile;
            var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId);
            var policy = await db.CategoryPolicies.FirstAsync(x => x.CategoryId == category.Id);
            var area = await db.AdministrativeAreas.SingleAsync(x => x.PublicId == factory.Catalog.AreaId);
            var now = DateTime.UtcNow;
            var providerCustomer = await db.CustomerProfiles.SingleOrDefaultAsync(x => x.UserId == providerRow.User.Id);
            if (providerCustomer is null)
            {
                providerCustomer = new CustomerProfile { UserId = providerRow.User.Id, DisplayName = "거리 기준 전문가", CreatedAt = now, UpdatedAt = now };
                db.CustomerProfiles.Add(providerCustomer); await db.SaveChangesAsync();
            }
            if (!await db.CustomerAddresses.AnyAsync(x => x.CustomerProfileId == customer.Id && x.AdministrativeAreaId == area.Id))
                db.CustomerAddresses.Add(new CustomerAddress { CustomerProfileId = customer.Id, AddressName = "요청 장소", PostalCode = "41111", RoadAddress = "대구광역시 동구 효동로 72-1", DetailAddress = "301호", AdministrativeAreaId = area.Id, Latitude = 35.880m, Longitude = 128.635m, IsDefault = true, CreatedAt = now, UpdatedAt = now });
            if (!await db.CustomerAddresses.AnyAsync(x => x.CustomerProfileId == providerCustomer.Id))
                db.CustomerAddresses.Add(new CustomerAddress { CustomerProfileId = providerCustomer.Id, AddressName = "출장 출발지", PostalCode = "41112", RoadAddress = "대구광역시 동구 동대구로 1", DetailAddress = string.Empty, AdministrativeAreaId = area.Id, Latitude = 35.870m, Longitude = 128.620m, IsDefault = true, CreatedAt = now, UpdatedAt = now });
            await db.SaveChangesAsync();
            var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = category.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, DetailAddress = "대구광역시 동구 효동로 72-1 301호", DetailAddressDisclosureCode = "BEFORE_QUOTE", Title = "거리 확인 공개 요청", Description = "출장거리를 확인해 주세요.", StatusCode = "OPEN", PolicySnapshotJson = "{}", OpenedAt = now, ExpiresAt = now.AddHours(1), AbuseCountExcluded = true, AbuseExclusionReason = "V173 integration test", CreatedAt = now, UpdatedAt = now };
            db.ServiceRequests.Add(request); await db.SaveChangesAsync();
            var candidate = new DispatchCandidate { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, StatusCode = "DISPATCHED", CategoryMatch = true, AreaMatch = true, ApprovalMatch = true, EvaluatedAt = now, ExpiresAt = now.AddHours(1), CreatedAt = now };
            db.DispatchCandidates.Add(candidate); await db.SaveChangesAsync();
            var dispatch = new RequestDispatch { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, CandidateId = candidate.Id, StatusCode = "AVAILABLE", AvailableAt = now, ExpiresAt = now.AddHours(1), IdempotencyKey = $"distance-{Guid.NewGuid():N}", CreatedAt = now };
            db.RequestDispatches.Add(dispatch); await db.SaveChangesAsync();
            requestPublicId = request.PublicId; dispatchId = dispatch.Id;
        }

        using var providerClient = CreateClient();
        await LoginAsync(providerClient, factory.Credentials[RoleCodes.Provider]);
        var detail = await providerClient.GetFromJsonAsync<ProviderMatchedRequestDetail>($"/api/v1/providers/me/matched-requests/{requestPublicId}");
        Assert.NotNull(detail);
        Assert.Equal("GENERAL", detail.Domain);
        Assert.Equal("대구광역시 동구 효동로 72-1 301호", detail.DetailAddress);
        Assert.Equal("BEFORE_QUOTE", detail.DetailAddressDisclosureCode);
        Assert.NotNull(detail.ApproximateDistanceKm);
        Assert.Equal("SAVED_DEFAULT_ADDRESSES", detail.DistanceBasis);

        var declined = await providerClient.PostAsJsonAsync($"/api/v1/providers/me/matched-requests/{requestPublicId}/decline", new { reasonCode = "DISTANCE" });
        Assert.Equal(HttpStatusCode.NoContent, declined.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await providerClient.GetAsync($"/api/v1/providers/me/matched-requests/{requestPublicId}")).StatusCode);
        Assert.Contains((await providerClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests"))!, x => x.RequestId == requestPublicId && x.DispatchStatus == "DECLINED");

        using (var verifyScope = factory.Services.CreateScope())
        {
            var verify = verifyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.Equal("DECLINED", (await verify.RequestDispatches.SingleAsync(x => x.Id == dispatchId)).StatusCode);
            Assert.Equal("PROVIDER_DISTANCE", (await verify.DispatchCandidates.SingleAsync(x => x.Id == (verify.RequestDispatches.Single(d => d.Id == dispatchId).CandidateId))).ReasonCode);

            var expiredDispatch = await verify.RequestDispatches.SingleAsync(x => x.Id == dispatchId);
            expiredDispatch.StatusCode = "EXPIRED";
            expiredDispatch.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await verify.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await providerClient.GetAsync($"/api/v1/providers/me/matched-requests/{requestPublicId}")).StatusCode);
        Assert.Contains((await providerClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests"))!, x => x.RequestId == requestPublicId && x.DispatchStatus == "EXPIRED");
    }

    [Fact]
    public async Task ProviderOperationsDashboard_UsesProviderOwnedData_AndRejectsCustomer()
    {
        using var provider = CreateClient();
        await LoginAsync(provider, factory.Credentials[RoleCodes.Provider]);
        var dashboard = await provider.GetAsync("/api/v1/providers/me/operations-dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var body = await dashboard.Content.ReadAsStringAsync();
        Assert.Contains("newMatchedRequestCount", body);
        Assert.DoesNotContain("wallet", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("feeCharge", body, StringComparison.OrdinalIgnoreCase);

        using var customer = CreateClient();
        await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.GetAsync("/api/v1/providers/me/operations-dashboard")).StatusCode);
    }

    [Fact]
    public async Task ProviderQuoteList_IsObjectScoped_AndDoesNotExposeCompetingQuotes()
    {
        using var provider = CreateClient();
        await LoginAsync(provider, factory.Credentials[RoleCodes.Provider]);
        var response = await provider.GetAsync("/api/v1/providers/me/quotes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("customerPhone", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detailAddress", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);

        using var customer = CreateClient();
        await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.GetAsync("/api/v1/providers/me/quotes")).StatusCode);
    }

    [Fact]
    public async Task ProviderConfiguration_MatchingDispatchAndInbox_WorkEndToEnd()
    {
        using var providerClient = CreateClient();
        await LoginAsync(providerClient, factory.Credentials[RoleCodes.Provider]);

        var servicesResponse = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        Assert.Equal(HttpStatusCode.OK, servicesResponse.StatusCode);
        Assert.Single((await servicesResponse.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);
        var repeatedServices = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        Assert.Single((await repeatedServices.Content.ReadFromJsonAsync<List<ProviderServiceCategoryResponse>>())!);

        var areasResponse = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            });
        Assert.Equal(HttpStatusCode.OK, areasResponse.StatusCode);
        var areaGroups = (await areasResponse.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>())!;
        Assert.Single(areaGroups);
        Assert.Single(areaGroups[0].Areas);
        var repeatedAreas = await providerClient.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            });
        Assert.Single((await repeatedAreas.Content.ReadFromJsonAsync<List<ProviderServiceAreaResponse>>())![0].Areas);

        using var customerClient = CreateClient();
        await LoginAsync(customerClient, factory.Credentials[RoleCodes.Customer]);
        var created = await CreateValidRequestAsync(customerClient);
        using var uploadForm = new MultipartFormDataContent();
        using var uploadContent = new ByteArrayContent(TestFileSamples.ValidJpeg());
        uploadContent.Headers.ContentType = new("image/jpeg");
        uploadForm.Add(uploadContent, "file", "phone-010-1234-5678.jpg");
        var uploadedResponse = await customerClient.PostAsync($"/api/v1/requests/{created.Id}/files", uploadForm);
        Assert.Equal(HttpStatusCode.OK, uploadedResponse.StatusCode);
        var uploaded = (await uploadedResponse.Content.ReadFromJsonAsync<ServiceRequestFileResponse>())!;
        var publishResponse = await customerClient.PostAsync($"/api/v1/requests/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = (await publishResponse.Content.ReadFromJsonAsync<PublishServiceRequestResponse>())!;
        Assert.Equal("OPEN", published.Status);
        Assert.Equal(1, published.EligibleCandidateCount);
        Assert.Equal(1, published.DispatchCount);

        using var adminClient = CreateClient();
        await LoginAsync(adminClient, factory.Credentials[RoleCodes.Admin]);
        var repeatMatching = await adminClient.PostAsync($"/api/v1/internal/requests/{created.Id}/match-and-dispatch", null);
        var repeatedResult = (await repeatMatching.Content.ReadFromJsonAsync<MatchAndDispatchResult>())!;
        Assert.Equal(1, repeatedResult.DispatchCount);
        Assert.Equal(0, repeatedResult.NewDispatchCount);

        var inbox = await providerClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.Contains(inbox!, item => item.RequestId == created.Id);
        var detailResponse = await providerClient.GetAsync($"/api/v1/providers/me/matched-requests/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<ProviderMatchedRequestDetail>())!;
        Assert.Equal(created.Id, detail.RequestId);
        Assert.Equal("VIEWED", detail.DispatchStatus);
        Assert.Null(detail.DetailAddress);
        Assert.Null(detail.CustomerPhone);
        Assert.Empty(detail.Files);

        Guid derivativePublicId;
        using (var privacyScope = factory.Services.CreateScope())
        {
            var db = privacyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var original = await db.Files.SingleAsync(file => file.PublicId == uploaded.Id);
            original.MalwareScanStatusCode = "CLEAN";
            original.PrivacyInspectionStatusCode = "SAFE";
            await db.SaveChangesAsync();
        }
        var safeDetail = await providerClient.GetFromJsonAsync<ProviderMatchedRequestDetail>($"/api/v1/providers/me/matched-requests/{created.Id}");
        var safePublished = Assert.Single(safeDetail!.Files);
        Assert.Equal("attachment-1.jpg", safePublished.FileName);
        Assert.Equal("PRIVACY_SAFE_ORIGINAL", safePublished.PublicationMode);
        Assert.Equal(HttpStatusCode.OK, (await providerClient.GetAsync(safePublished.DownloadUrl)).StatusCode);

        using (var privacyScope = factory.Services.CreateScope())
        {
            var db = privacyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var storage = privacyScope.ServiceProvider.GetRequiredService<IPrivateFileStorage>();
            var original = await db.Files.SingleAsync(file => file.PublicId == uploaded.Id);
            original.PrivacyInspectionStatusCode = "SENSITIVE_DETECTED";
            original.SanitizationStatusCode = "COMPLETED";
            var storageKey = $"tests/privacy/{Guid.NewGuid():N}.jpg";
            var derived = new StoredFile
            {
                PurposeCode = "REQUEST_ANSWER",
                StorageContainer = "development-private",
                StorageKey = storageKey,
                StorageKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)),
                OriginalFileName = "sanitized-output.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 4,
                Sha256Hex = Convert.ToHexString(SHA256.HashData([0xFF, 0xD8, 0xFF, 0x02])).ToLowerInvariant(),
                StatusCode = "ACTIVE",
                MalwareScanStatusCode = "CLEAN",
                CreatedAt = DateTime.UtcNow,
            };
            db.Files.Add(derived);
            await db.SaveChangesAsync();
            db.FileDerivatives.Add(new StoredFileDerivative
            {
                OriginalFileId = original.Id,
                DerivedFileId = derived.Id,
                DerivativeTypeCode = "PRIVACY_SANITIZED",
                AdapterVersion = "TEST-ONLY",
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            await storage.SaveAsync(storageKey, new MemoryStream([0xFF, 0xD8, 0xFF, 0x02]), default);
            derivativePublicId = derived.PublicId;
        }
        var sanitizedDetailResponse = await providerClient.GetAsync($"/api/v1/providers/me/matched-requests/{created.Id}");
        var sanitizedJson = await sanitizedDetailResponse.Content.ReadAsStringAsync();
        var sanitizedDetail = await sanitizedDetailResponse.Content.ReadFromJsonAsync<ProviderMatchedRequestDetail>();
        var sanitizedPublished = Assert.Single(sanitizedDetail!.Files);
        Assert.Equal(derivativePublicId, sanitizedPublished.Id);
        Assert.Equal("PRIVACY_SANITIZED_DERIVATIVE", sanitizedPublished.PublicationMode);
        Assert.DoesNotContain("phone-010-1234-5678.jpg", sanitizedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", sanitizedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound,
            (await providerClient.GetAsync($"/api/v1/requests/{created.Id}/files/{uploaded.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await providerClient.GetAsync(sanitizedPublished.DownloadUrl)).StatusCode);

        var siteVisitResponse = await providerClient.PostAsJsonAsync(
            $"/api/v1/providers/me/requests/{created.Id}/site-visits",
            new
            {
                scheduledAt = DateTime.UtcNow.AddMinutes(30),
                estimatedDurationMinutes = 30,
                visitFeeAmount = 30000,
                paymentMode = "ON_SITE",
                terms = "현장 상태를 확인한 뒤 최종 견적을 안내합니다.",
                deductFromWorkAmount = true,
                paymentInstruction = (string?)null,
                noShowWaitMinutes = 10,
                expiresAt = DateTime.UtcNow.AddMinutes(10),
                idempotencyKey = $"site-visit-non-self-{Guid.NewGuid():N}",
                rowVersion = (string?)null,
            });
        Assert.Equal(HttpStatusCode.OK, siteVisitResponse.StatusCode);

        await AssertCandidateOutcomesAsync(created.Id);

        using var otherProviderClient = CreateClient();
        await LoginAsync(otherProviderClient, factory.ServiceMismatchProviderCredential);
        var otherInbox = await otherProviderClient.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>("/api/v1/providers/me/matched-requests");
        Assert.DoesNotContain(otherInbox!, item => item.RequestId == created.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProviderClient.GetAsync($"/api/v1/providers/me/matched-requests/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customerClient.GetAsync("/api/v1/providers/me/matched-requests")).StatusCode);
    }

    [Fact]
    public async Task DuplicateSelectionsAndUnsupportedNationwideCoverage_AreRejected()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Provider]);
        var duplicateServices = await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId, factory.Catalog.ServiceId } });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateServices.StatusCode);

        await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } });
        var duplicateAreas = await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId, factory.Catalog.AreaId } },
                },
            });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateAreas.StatusCode);

        var invalidNationwide = await client.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas",
            new { services = Array.Empty<object>(), nationwideServiceCategoryIds = new[] { factory.Catalog.ServiceId } });
        Assert.Equal(HttpStatusCode.BadRequest, invalidNationwide.StatusCode);
        Assert.Contains("PROVIDER_NATIONWIDE_NOT_ALLOWED", await invalidNationwide.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExistingOpenRequest_IsRematched_WhenProviderUpdatesCategoryAndArea_WithoutDuplicates()
    {
        using var provider = CreateClient();
        using var customer = CreateClient();
        await LoginAsync(provider, factory.ServiceMismatchProviderCredential);
        await LoginAsync(customer, factory.Credentials[RoleCodes.Customer]);

        await provider.PutAsJsonAsync("/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.OtherServiceId } });
        await provider.PutAsJsonAsync("/api/v1/providers/me/service-areas", new
        {
            services = new[]
            {
                new { serviceCategoryId = factory.Catalog.OtherServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
            },
        });

        var request = await CreateValidRequestAsync(customer);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsync($"/api/v1/requests/{request.Id}/publish", null)).StatusCode);
        Assert.DoesNotContain((await provider.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>(
            "/api/v1/providers/me/matched-requests"))!, item => item.RequestId == request.Id);

        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas", new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            })).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var providerId = await (from user in db.Users
                                    join profile in db.ProviderProfiles on user.Id equals profile.UserId
                                    where user.LoginId == factory.ServiceMismatchProviderCredential.LoginId
                                    select profile.Id).SingleAsync();
            var categoryId = await db.ServiceCategories.Where(item => item.PublicId == factory.Catalog.ServiceId)
                .Select(item => item.Id).SingleAsync();
            var link = await db.ProviderServiceCategories.SingleAsync(item =>
                item.ProviderProfileId == providerId && item.CategoryId == categoryId);
            var approval = await db.ProviderServiceApprovals.SingleAsync(item => item.ProviderServiceCategoryId == link.Id);
            approval.ApprovalStatusCode = "APPROVED";
            approval.ApprovalDecidedAt = DateTime.UtcNow;
            foreach (var verification in await db.ProviderServiceRequirementVerifications
                         .Where(item => item.ProviderServiceCategoryId == link.Id).ToListAsync())
            {
                verification.VerificationStatusCode = "APPROVED";
                verification.VerifiedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.ServiceId } })).StatusCode);
        Assert.Contains((await provider.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>(
            "/api/v1/providers/me/matched-requests"))!, item => item.RequestId == request.Id);

        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas", new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.ServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            })).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var providerId = await (from user in db.Users
                                    join profile in db.ProviderProfiles on user.Id equals profile.UserId
                                    where user.LoginId == factory.ServiceMismatchProviderCredential.LoginId
                                    select profile.Id).SingleAsync();
            var requestId = await db.ServiceRequests.Where(item => item.PublicId == request.Id)
                .Select(item => item.Id).SingleAsync();
            Assert.Equal(1, await db.RequestDispatches.CountAsync(item =>
                item.ProviderProfileId == providerId && item.ServiceRequestId == requestId));
        }

        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas", new { services = Array.Empty<object>() })).StatusCode);
        Assert.Contains((await provider.GetFromJsonAsync<List<ProviderMatchedRequestListItem>>(
            "/api/v1/providers/me/matched-requests"))!, item => item.RequestId == request.Id && item.DispatchStatus == "EXPIRED");

        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-categories",
            new { categoryIds = new[] { factory.Catalog.OtherServiceId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(
            "/api/v1/providers/me/service-areas", new
            {
                services = new[]
                {
                    new { serviceCategoryId = factory.Catalog.OtherServiceId, administrativeAreaIds = new[] { factory.Catalog.AreaId } },
                },
            })).StatusCode);
    }

    private async Task AssertCandidateOutcomesAsync(Guid requestPublicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var candidates = await (
                from candidate in dbContext.DispatchCandidates
                join request in dbContext.ServiceRequests on candidate.ServiceRequestId equals request.Id
                join provider in dbContext.ProviderProfiles on candidate.ProviderProfileId equals provider.Id
                join user in dbContext.Users on provider.UserId equals user.Id
                where request.PublicId == requestPublicId
                select new { user.LoginId, Candidate = candidate })
            .ToListAsync();

        var matched = candidates.Single(item => item.LoginId == factory.Credentials[RoleCodes.Provider].LoginId).Candidate;
        Assert.Equal("DISPATCHED", matched.StatusCode);
        Assert.True(matched.CategoryMatch && matched.AreaMatch && matched.ApprovalMatch);

        Assert.DoesNotContain(candidates, item => item.LoginId == factory.ServiceMismatchProviderCredential.LoginId);
        Assert.DoesNotContain(candidates, item => item.LoginId == factory.AreaMismatchProviderCredential.LoginId);

        var inactive = candidates.Single(item => item.LoginId == factory.InactiveProviderCredential.LoginId).Candidate;
        Assert.Equal("PROVIDER_NOT_APPROVED_ACTIVE", inactive.ReasonCode);
        Assert.False(inactive.ApprovalMatch);
        Assert.Single(await dbContext.RequestDispatches.Where(dispatch => dispatch.ServiceRequestId == matched.ServiceRequestId).ToListAsync());
    }

    private async Task<ServiceRequestCreatedResponse> CreateValidRequestAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/requests", new
        {
            categoryId = factory.Catalog.ServiceId,
            administrativeAreaId = factory.Catalog.AreaId,
            title = "Provider matching request",
            description = "Provider-visible request summary without customer identity.",
            detailAddress = "Never expose this detail address",
            isUrgent = false,
            idempotencyKey = $"provider-flow-{Guid.NewGuid():N}",
            answers = new object[]
            {
                new { fieldId = factory.Catalog.FieldIds[0], value = "A sufficiently detailed request answer for provider matching tests." },
                new { fieldId = factory.Catalog.FieldIds[1], value = TestScheduleSlots.Future().ToString("O") },
                new { fieldId = factory.Catalog.FieldIds[2], value = "주거" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ServiceRequestCreatedResponse>())!;
        await TestRequestData.ExcludeFromAbuseLimitsAsync(factory, created.Id);
        return created;
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
