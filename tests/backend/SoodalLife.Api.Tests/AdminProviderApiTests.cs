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

public sealed class AdminProviderApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_ListContainsOnlyProviderRoleUsers_AndShowsMultipleRoles()
    {
        var providerId = await EnsureProviderDataAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        var result = await client.GetFromJsonAsync<AdminProviderListResponse>(BasePath);
        Assert.NotNull(result); Assert.Equal(4, result.TotalCount); Assert.All(result.Items, item => Assert.Contains(RoleCodes.Provider, item.Roles));
        var provider = Assert.Single(result.Items, item => item.Id == providerId); Assert.Contains(RoleCodes.Customer, provider.Roles);
    }

    [Fact]
    public async Task Admin_CanSearchProviderFields_AndPaginate_WithMaskedPersonalData()
    {
        await EnsureProviderDataAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        foreach (var search in new[] { "수달홈케어", "01024681357", "care@sudal.example.kr", "1234567890" })
            Assert.Equal("수달홈케어", Assert.Single((await client.GetFromJsonAsync<AdminProviderListResponse>($"{BasePath}?search={Uri.EscapeDataString(search)}"))!.Items).ProviderName);
        var first = await client.GetFromJsonAsync<AdminProviderListResponse>($"{BasePath}?page=1&pageSize=1"); var second = await client.GetFromJsonAsync<AdminProviderListResponse>($"{BasePath}?page=2&pageSize=1");
        Assert.NotEqual(first!.Items[0].Id, second!.Items[0].Id);
        var body = await (await client.GetAsync(BasePath)).Content.ReadAsStringAsync(); Assert.DoesNotContain("01024681357", body); Assert.DoesNotContain("care@sudal.example.kr", body); Assert.DoesNotContain("1234567890", body);
    }

    [Fact]
    public async Task Admin_DetailConnectsServicesAreasRequirementsDocumentsQuotesTransactions_WithoutOtherProviderData()
    {
        var providerId = await SeedDetailAsync(); using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Admin]);
        var detail = await client.GetFromJsonAsync<AdminProviderDetailResponse>($"{BasePath}/{providerId}");
        Assert.NotNull(detail); Assert.Equal("수달홈케어", detail.Basic.ProviderName); Assert.Contains(RoleCodes.Customer, detail.Basic.Roles);
        Assert.Equal("1234567890", detail.Business.BusinessRegistrationNo); Assert.False(detail.Business.DetailFieldsSupported);
        Assert.Single(detail.Services); Assert.Single(detail.Areas); Assert.Single(detail.Documents); Assert.False(detail.Documents[0].CanOpenFile);
        var review = Assert.Single(detail.ServiceReviews); Assert.True(review.StructuredRequirementsConfigured); Assert.Equal("관련 자격·사업자 확인", review.LegacyQualificationText);
        var requirement = Assert.Single(review.Requirements); Assert.Equal("PENDING", requirement.VerificationStatusCode); Assert.Equal("사업자등록증", requirement.LinkedDocumentType);
        Assert.Single(detail.Quotes); Assert.True(detail.Quotes[0].IsAccepted); Assert.Single(detail.Transactions);
        Assert.DoesNotContain(detail.Quotes, quote => quote.RequestTitle == "다른 공급자에게 전달된 요청");
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessProviderManagement(string role)
    { using var client = Client(); await Login(client, factory.Credentials[role]); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath)).StatusCode); }

    private async Task<Guid> EnsureProviderDataAsync()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var user = await db.Users.SingleAsync(value => value.LoginId == factory.Credentials[RoleCodes.Provider].LoginId); user.Phone = "01024681357"; user.Email = "care@sudal.example.kr";
        var provider = await db.ProviderProfiles.SingleAsync(value => value.UserId == user.Id); provider.BusinessName = "수달홈케어"; provider.BusinessRegistrationNo = "1234567890";
        var customerRole = await db.Roles.SingleAsync(value => value.Code == RoleCodes.Customer);
        if (!await db.UserRoles.AnyAsync(value => value.UserId == user.Id && value.RoleId == customerRole.Id && value.RevokedAt == null)) db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = customerRole.Id, GrantedAt = DateTime.UtcNow });
        if (!await db.CustomerProfiles.AnyAsync(value => value.UserId == user.Id)) db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = "김수달", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(); return provider.PublicId;
    }

    private async Task<Guid> SeedDetailAsync()
    {
        var id = await EnsureProviderDataAsync(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var provider = await db.ProviderProfiles.SingleAsync(value => value.PublicId == id); var service = await db.ServiceCategories.SingleAsync(value => value.PublicId == factory.Catalog.ServiceId); var area = await db.AdministrativeAreas.SingleAsync(value => value.PublicId == factory.Catalog.AreaId);
        var link = await db.ProviderServiceCategories.SingleOrDefaultAsync(value => value.ProviderProfileId == provider.Id && value.CategoryId == service.Id);
        if (link is null) { link = new ProviderServiceCategory { ProviderProfileId = provider.Id, CategoryId = service.Id, StatusCode = "ACTIVE", ActivatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.ProviderServiceCategories.Add(link); await db.SaveChangesAsync(); }
        if (!await db.ProviderServiceAreas.AnyAsync(value => value.ProviderServiceCategoryId == link.Id)) { db.ProviderServiceAreas.Add(new ProviderServiceArea { ProviderServiceCategoryId = link.Id, AdministrativeAreaId = area.Id, StatusCode = "ACTIVE", ActivatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(); }
        var definition = await db.ProviderRequirementDefinitions.SingleOrDefaultAsync(value => value.RequirementCode == "BUSINESS_CONFIRMATION");
        if (definition is null) { definition = new ProviderRequirementDefinition { RequirementTypeCode = "QUALIFICATION", RequirementCode = "BUSINESS_CONFIRMATION", Name = "사업자 확인", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.ProviderRequirementDefinitions.Add(definition); await db.SaveChangesAsync(); }
        var policy = await db.CategoryOperationPolicies.SingleAsync(value => value.CategoryId == service.Id);
        var assignment = await db.CategoryProviderRequirementAssignments.SingleOrDefaultAsync(value => value.CategoryOperationPolicyId == policy.Id && value.RequirementDefinitionId == definition.Id);
        if (assignment is null) { assignment = new CategoryProviderRequirementAssignment { CategoryOperationPolicyId = policy.Id, RequirementDefinitionId = definition.Id, IsRequired = true, VerificationRequired = true, DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.CategoryProviderRequirementAssignments.Add(assignment); await db.SaveChangesAsync(); }
        var documentType = await db.ProviderDocumentTypes.SingleOrDefaultAsync(value => value.Code == "BUSINESS_REGISTRATION");
        if (documentType is null) { documentType = new ProviderDocumentType { Code = "BUSINESS_REGISTRATION", Name = "사업자등록증", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.ProviderDocumentTypes.Add(documentType); await db.SaveChangesAsync(); }
        if (!await db.CategoryProviderRequirementEvidenceTypes.AnyAsync(value => value.RequirementAssignmentId == assignment.Id)) { db.CategoryProviderRequirementEvidenceTypes.Add(new CategoryProviderRequirementEvidenceType { RequirementAssignmentId = assignment.Id, DocumentTypeId = documentType.Id, IsRequired = true, DisplayOrder = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(); }
        var document = await db.ProviderDocuments.SingleOrDefaultAsync(value => value.ProviderProfileId == provider.Id);
        if (document is null) { var file = new StoredFile { PurposeCode = "PROVIDER_DOCUMENT", StorageContainer = "검증용", StorageKey = "비공개", StorageKeyHash = new byte[32], OriginalFileName = "사업자등록증.pdf", ContentType = "application/pdf", Sha256Hex = new string('0',64), StatusCode = "ACTIVE", CreatedAt = DateTime.UtcNow }; db.Files.Add(file); await db.SaveChangesAsync(); document = new ProviderDocument { ProviderProfileId = provider.Id, FileId = file.Id, DocumentTypeId = documentType.Id, DocumentTypeCode = documentType.Code, DocumentNumber = "1234567890", VerificationStatusCode = "PENDING", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.ProviderDocuments.Add(document); await db.SaveChangesAsync(); }
        if (!await db.ProviderServiceRequirementVerifications.AnyAsync(value => value.ProviderServiceCategoryId == link.Id && value.RequirementAssignmentId == assignment.Id)) { db.ProviderServiceRequirementVerifications.Add(new ProviderServiceRequirementVerification { ProviderServiceCategoryId = link.Id, RequirementAssignmentId = assignment.Id, ProviderDocumentId = document.Id, VerificationStatusCode = "PENDING", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(); }
        await SeedActivityAsync(db, provider, service, area); return id;
    }

    private async Task SeedActivityAsync(SoodalLifeDbContext db, ProviderProfile provider, ServiceCategory service, AdministrativeArea area)
    {
        if (await db.Quotes.AnyAsync(value => value.ProviderProfileId == provider.Id)) return;
        var customer = await db.CustomerProfiles.FirstAsync(value => value.UserId != provider.UserId); var policy = await db.CategoryPolicies.FirstAsync(value => value.CategoryId == service.Id); var now = DateTime.UtcNow;
        var request = new ServiceRequest { CustomerProfileId = customer.Id, CategoryId = service.Id, CategoryPolicyId = policy.Id, AdministrativeAreaId = area.Id, Title = "욕실 수전 교체 요청", StatusCode = "ACCEPTED", PolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now }; db.ServiceRequests.Add(request); await db.SaveChangesAsync();
        var candidate = new DispatchCandidate { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, StatusCode = "DISPATCHED", CategoryMatch = true, AreaMatch = true, ApprovalMatch = true, EvaluatedAt = now, CreatedAt = now }; db.DispatchCandidates.Add(candidate); await db.SaveChangesAsync();
        var dispatch = new RequestDispatch { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, CandidateId = candidate.Id, StatusCode = "RESPONDED", AvailableAt = now, ExpiresAt = now.AddHours(1), IdempotencyKey = "admin-provider-dispatch", CreatedAt = now }; db.RequestDispatches.Add(dispatch); await db.SaveChangesAsync();
        var quote = new Quote { ServiceRequestId = request.Id, ProviderProfileId = provider.Id, RequestDispatchId = dispatch.Id, StatusCode = "ACCEPTED", SubmittedAt = now, AcceptedAt = now, CreatedAt = now, UpdatedAt = now }; db.Quotes.Add(quote); await db.SaveChangesAsync();
        var revision = new QuoteRevision { QuoteId = quote.Id, RevisionNo = 1, Summary = "수전 교체", TotalAmount = 85000, CurrencyCode = "KRW", ValidUntil = now.AddDays(1), SubmittedAt = now, SubmittedByUserId = provider.UserId, IdempotencyKey = "admin-provider-revision" }; db.QuoteRevisions.Add(revision); await db.SaveChangesAsync();
        db.Transactions.Add(new TransactionRecord { ServiceRequestId = request.Id, AcceptedQuoteRevisionId = revision.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, CategoryId = service.Id, StatusCode = "COMPLETED", AgreedAmount = 85000, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", CompletedAt = now, CreatedAt = now, UpdatedAt = now }); await db.SaveChangesAsync();
    }

    private const string BasePath = "/api/v1/admin/providers";
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
