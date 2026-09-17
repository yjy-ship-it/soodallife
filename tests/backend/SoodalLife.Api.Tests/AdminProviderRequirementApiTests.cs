using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminProviderRequirementApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanReadServiceProviderRequirementCurrentAndDetail()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var list = await client.GetFromJsonAsync<AdminProviderRequirementListResponse>(BasePath(factory.Catalog.ServiceId));
        Assert.NotNull(list);
        var current = Assert.Single(list.Policies, policy => policy.IsCurrentlyEffective);
        Assert.Equal("관련 자격·사업자 확인", current.QualificationAndLicenseRequirement);
        Assert.Equal("필수", current.InsuranceRequirement);
        Assert.Equal("NORMAL", current.SafetyGradeCode);
        Assert.True(list.SupportsStructuredRequirements);
        Assert.True(list.SupportsEvidenceValidityRules);
        Assert.True(list.CanEdit);
        Assert.Single(current.StructuredRequirements);

        var currentResponse = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
        Assert.Equal(current.Id, currentResponse!.Id);
        var detail = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}");
        Assert.Equal(current.Id, detail!.Id);
        Assert.Equal(current.PolicyVersion, detail.PolicyVersion);
        Assert.Equal(current.QualificationAndLicenseRequirement, detail.QualificationAndLicenseRequirement);
        Assert.Equal(current.InsuranceRequirement, detail.InsuranceRequirement);
        Assert.Equal(current.SafetyGradeCode, detail.SafetyGradeCode);
        Assert.Single(detail.StructuredRequirements);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessProviderRequirementAdminApi(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(BasePath(factory.Catalog.ServiceId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{BasePath(factory.Catalog.ServiceId)}/current")).StatusCode);
    }

    [Fact]
    public async Task ServiceRequirementLookup_DoesNotMixAnotherServicePolicy()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var service = await client.GetFromJsonAsync<AdminProviderRequirementListResponse>(BasePath(factory.Catalog.ServiceId));
        var other = await client.GetFromJsonAsync<AdminProviderRequirementListResponse>(BasePath(factory.Catalog.OtherServiceId));
        Assert.NotNull(service);
        Assert.NotNull(other);
        Assert.All(service.Policies, policy => Assert.Equal("관련 자격·사업자 확인", policy.QualificationAndLicenseRequirement));
        Assert.All(other.Policies, policy => Assert.Equal("필수 자격 확인", policy.QualificationAndLicenseRequirement));
        Assert.DoesNotContain(service.Policies.Select(policy => policy.Id), id => other.Policies.Any(policy => policy.Id == id));
    }

    [Fact]
    public async Task Admin_CanUpdateOneServiceAndSelectedMiddleServicesOperationPolicy()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var first = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
        var second = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.OtherServiceId)}/current");
        Assert.NotNull(first); Assert.NotNull(second); Guid middlePublicId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var parentId = await db.ServiceCategories.Where(value => value.PublicId == factory.Catalog.ServiceId).Select(value => value.ParentId).SingleAsync();
            middlePublicId = await db.ServiceCategories.Where(value => value.Id == parentId).Select(value => value.PublicId).SingleAsync();
        }
        try
        {
            var individualResponse = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{first!.Id}/operation-policy", new { QualificationAndLicenseRequirement = "전문 자격증 확인", InsuranceRequirement = "배상책임보험 필수", SafetyGradeCode = "HIGH", first.RowVersion });
            Assert.Equal(HttpStatusCode.OK, individualResponse.StatusCode);
            var individual = await individualResponse.Content.ReadFromJsonAsync<AdminProviderRequirementResponse>();
            Assert.Equal("전문 자격증 확인", individual!.QualificationAndLicenseRequirement); Assert.Equal("HIGH", individual.SafetyGradeCode);
            var stale = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{first.Id}/operation-policy", new { QualificationAndLicenseRequirement = "다른 값", InsuranceRequirement = "필수", SafetyGradeCode = "NORMAL", RowVersion = "AQ==" });
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

            var bulkResponse = await client.PutAsJsonAsync($"/api/v1/admin/service-categories/middles/{middlePublicId}/operation-policy", new { QualificationAndLicenseRequirement = "중분류 공통 자격 확인", InsuranceRequirement = "공통 보험 필수", SafetyGradeCode = "MEDIUM", Services = new[] { new { ServiceId = factory.Catalog.ServiceId, PolicyId = individual.Id, individual.RowVersion }, new { ServiceId = factory.Catalog.OtherServiceId, PolicyId = second!.Id, second.RowVersion } } });
            Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);
            var bulk = (await bulkResponse.Content.ReadFromJsonAsync<IReadOnlyList<AdminProviderRequirementResponse>>())!;
            Assert.Equal(2, bulk.Count); Assert.All(bulk, value => { Assert.Equal("중분류 공통 자격 확인", value.QualificationAndLicenseRequirement); Assert.Equal("공통 보험 필수", value.InsuranceRequirement); Assert.Equal("MEDIUM", value.SafetyGradeCode); });
            using var verifyScope = factory.Services.CreateScope(); var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await verifyDb.AuditLogs.AnyAsync(value => value.ActionCode == "CATEGORY_OPERATION_POLICY_UPDATED"));
            Assert.True(await verifyDb.AuditLogs.AnyAsync(value => value.ActionCode == "CATEGORY_OPERATION_POLICY_BULK_UPDATED"));
        }
        finally
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var serviceIds = new[] { factory.Catalog.ServiceId, factory.Catalog.OtherServiceId };
            var rows = await (from category in db.ServiceCategories join policy in db.CategoryOperationPolicies on category.Id equals policy.CategoryId where serviceIds.Contains(category.PublicId) select new { category.PublicId, Policy = policy }).ToListAsync();
            foreach (var row in rows) { var original = row.PublicId == factory.Catalog.ServiceId ? first! : second!; row.Policy.RequiredQualificationSummaryText = original.QualificationAndLicenseRequirement; row.Policy.InsuranceRequirementText = original.InsuranceRequirement; row.Policy.SafetyGradeCode = original.SafetyGradeCode; }
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Admin_CanManageMasterAssignmentEvidenceAndAudit_WithoutAffectingAnotherService()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var standards = await client.GetFromJsonAsync<AdminProviderRequirementStandardsResponse>(StandardsPath);
        Assert.NotNull(standards);
        Assert.Equal(5, standards.RequirementTypes.Count);
        Assert.Contains(standards.RequirementDefinitions, value => value.RequirementCode == "TEST_REQUIRED_QUALIFICATION");
        Assert.Empty(standards.DocumentTypes);
        AdminProviderRequirementDefinitionResponse? definition = null;
        AdminProviderDocumentTypeResponse? documentType = null;
        AdminCategoryProviderRequirementResponse? assignment = null;
        Guid? futurePolicyId = null;
        try
        {
            using (var seedScope = factory.Services.CreateScope())
            {
                var seedDb = seedScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
                var serviceId = await seedDb.ServiceCategories.Where(item => item.PublicId == factory.Catalog.ServiceId).Select(item => item.Id).SingleAsync();
                var source = await seedDb.CategoryOperationPolicies.SingleAsync(item => item.CategoryId == serviceId);
                var future = new SoodalLife.Api.Domain.Entities.CategoryOperationPolicy { CategoryId = serviceId, PolicyVersion = "test-v1.2", RequiredQualificationSummaryText = source.RequiredQualificationSummaryText,
                    InsuranceRequirementText = source.InsuranceRequirementText, SafetyGradeCode = source.SafetyGradeCode, EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                seedDb.CategoryOperationPolicies.Add(future); await seedDb.SaveChangesAsync(); futurePolicyId = future.PublicId;
            }
            var definitionResponse = await client.PostAsJsonAsync($"{StandardsPath}/definitions", new { RequirementTypeCode = "QUALIFICATION", RequirementCode = "BUSINESS_VERIFICATION", Name = "사업자 확인", Description = "사업자 정보 확인 기준", IsActive = true });
            Assert.Equal(HttpStatusCode.Created, definitionResponse.StatusCode);
            definition = await definitionResponse.Content.ReadFromJsonAsync<AdminProviderRequirementDefinitionResponse>();
            var documentResponse = await client.PostAsJsonAsync($"{StandardsPath}/document-types", new { Code = "BUSINESS_REGISTRATION", Name = "사업자등록증", SupportsExpiry = false, IsActive = true });
            Assert.Equal(HttpStatusCode.Created, documentResponse.StatusCode);
            documentType = await documentResponse.Content.ReadFromJsonAsync<AdminProviderDocumentTypeResponse>();

            var current = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
            var create = await client.PostAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current!.Id}/assignments", AssignmentInput(definition!.Id, true, true, false, 3, true));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var createdPolicy = await create.Content.ReadFromJsonAsync<AdminProviderRequirementResponse>();
            assignment = Assert.Single(createdPolicy!.StructuredRequirements, value => value.RequirementDefinitionId == definition.Id);
            Assert.True(assignment.IsRequired);
            Assert.True(assignment.VerificationRequired);
            Assert.False(assignment.ExpiryCheckRequired);
            Assert.Equal(3, assignment.DisplayOrder);

            var update = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}/assignments/{assignment.Id}", AssignmentInput(definition.Id, false, false, true, 7, true));
            var updated = await update.Content.ReadFromJsonAsync<AdminProviderRequirementResponse>();
            var updatedAssignment = Assert.Single(updated!.StructuredRequirements, value => value.Id == assignment.Id);
            Assert.False(updatedAssignment.IsRequired);
            Assert.False(updatedAssignment.VerificationRequired);
            Assert.True(updatedAssignment.ExpiryCheckRequired);
            Assert.Equal(7, updatedAssignment.DisplayOrder);

            var evidence = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}/assignments/{assignment.Id}/evidence-types", new { EvidenceTypes = new[] { new { DocumentTypeId = documentType!.Id, IsRequired = true, DisplayOrder = 2 } } });
            var withEvidence = await evidence.Content.ReadFromJsonAsync<AdminProviderRequirementResponse>();
            var linked = Assert.Single(Assert.Single(withEvidence!.StructuredRequirements, value => value.Id == assignment.Id).EvidenceTypes);
            Assert.Equal("사업자등록증", linked.Name);
            Assert.Equal(2, linked.DisplayOrder);

            var deactivate = await client.PutAsJsonAsync($"{BasePath(factory.Catalog.ServiceId)}/{current.Id}/assignments/{assignment.Id}", AssignmentInput(definition.Id, false, false, true, 7, false));
            Assert.False(Assert.Single((await deactivate.Content.ReadFromJsonAsync<AdminProviderRequirementResponse>())!.StructuredRequirements, value => value.Id == assignment.Id).IsActive);
            var other = await client.GetFromJsonAsync<AdminProviderRequirementListResponse>(BasePath(factory.Catalog.OtherServiceId));
            Assert.All(other!.Policies, policy => Assert.Empty(policy.StructuredRequirements));
            var versioned = await client.GetFromJsonAsync<AdminProviderRequirementListResponse>(BasePath(factory.Catalog.ServiceId));
            Assert.Empty(versioned!.Policies.Single(policy => policy.Id == futurePolicyId).StructuredRequirements);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "PROVIDER_REQUIREMENT_DEFINITION_CREATED" && log.EntityPublicId == definition.Id));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "PROVIDER_DOCUMENT_TYPE_CREATED" && log.EntityPublicId == documentType.Id));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "PROVIDER_REQUIREMENT_ASSIGNED" && log.EntityPublicId == assignment.Id));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "PROVIDER_REQUIREMENT_EVIDENCE_REPLACED" && log.EntityPublicId == assignment.Id));
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "PROVIDER_REQUIREMENT_ASSIGNMENT_DEACTIVATED" && log.EntityPublicId == assignment.Id));
        }
        finally
        {
            await RemoveStructuredTestDataAsync(definition?.Id, documentType?.Id, assignment?.Id, futurePolicyId);
        }
    }

    [Fact]
    public async Task Admin_CanApplyOneServiceRequirementsToItsMiddleCategory()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var source = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.ServiceId)}/current");
        var target = await client.GetFromJsonAsync<AdminProviderRequirementResponse>($"{BasePath(factory.Catalog.OtherServiceId)}/current");
        Assert.NotNull(source); Assert.NotNull(target); Assert.Single(source.StructuredRequirements); Assert.Empty(target.StructuredRequirements);
        Guid middlePublicId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var parentId = await db.ServiceCategories.Where(value => value.PublicId == factory.Catalog.ServiceId).Select(value => value.ParentId).SingleAsync();
            middlePublicId = await db.ServiceCategories.Where(value => value.Id == parentId).Select(value => value.PublicId).SingleAsync();
        }
        try
        {
            var response = await client.PutAsJsonAsync($"/api/v1/admin/service-categories/middles/{middlePublicId}/provider-requirements/apply", new
            {
                SourceServiceId = factory.Catalog.ServiceId, SourcePolicyId = source!.Id,
                Services = new[] { new { ServiceId = factory.Catalog.ServiceId, PolicyId = source.Id, source.RowVersion }, new { ServiceId = factory.Catalog.OtherServiceId, PolicyId = target!.Id, target.RowVersion } }
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var policies = await response.Content.ReadFromJsonAsync<IReadOnlyList<AdminProviderRequirementResponse>>();
            Assert.NotNull(policies); Assert.Equal(2, policies.Count);
            var copied = Assert.Single(policies.Single(value => value.Id == target.Id).StructuredRequirements);
            Assert.Equal(source.StructuredRequirements.Single().RequirementCode, copied.RequirementCode);
            using var verifyScope = factory.Services.CreateScope(); var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await verifyDb.AuditLogs.AnyAsync(value => value.ActionCode == "MIDDLE_PROVIDER_REQUIREMENTS_APPLIED"));
        }
        finally
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var targetPolicyId = await (from category in db.ServiceCategories join policy in db.CategoryOperationPolicies on category.Id equals policy.CategoryId where category.PublicId == factory.Catalog.OtherServiceId select policy.Id).SingleAsync();
            var copied = await db.CategoryProviderRequirementAssignments.Where(value => value.CategoryOperationPolicyId == targetPolicyId).ToListAsync();
            var copiedIds = copied.Select(value => value.Id).ToArray();
            db.CategoryProviderRequirementEvidenceTypes.RemoveRange(db.CategoryProviderRequirementEvidenceTypes.Where(value => copiedIds.Contains(value.RequirementAssignmentId)));
            db.CategoryProviderRequirementAssignments.RemoveRange(copied); await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Admin_CanApplyApprovedRequirementDefaults_Idempotently()
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var definitionCodes = new[] { "BUSINESS_REGISTRATION_VERIFICATION", "IDENTITY_AND_REPRESENTATIVE_VERIFICATION", "PROFESSIONAL_LICENSE_VERIFICATION", "LIABILITY_INSURANCE_VERIFICATION", "SAFETY_EDUCATION_VERIFICATION", "EVIDENCE_EXPIRY_VERIFICATION" };
        var documentCodes = new[] { "BUSINESS_REGISTRATION_CERTIFICATE", "IDENTITY_VERIFICATION_DOCUMENT", "PROFESSIONAL_LICENSE_CERTIFICATE", "LIABILITY_INSURANCE_CERTIFICATE", "SAFETY_EDUCATION_CERTIFICATE", "CAREER_CERTIFICATE", "TAX_PAYMENT_CERTIFICATE", "BANK_ACCOUNT_COPY" };
        try
        {
            var firstResponse = await client.PostAsync($"{StandardsPath}/defaults/apply", null);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            var first = await firstResponse.Content.ReadFromJsonAsync<AdminProviderRequirementDefaultsResponse>();
            Assert.Equal(6, first!.CreatedRequirementDefinitionCount); Assert.Equal(8, first.CreatedDocumentTypeCount);
            var second = await (await client.PostAsync($"{StandardsPath}/defaults/apply", null)).Content.ReadFromJsonAsync<AdminProviderRequirementDefaultsResponse>();
            Assert.Equal(0, second!.CreatedRequirementTypeCount); Assert.Equal(0, second.CreatedRequirementDefinitionCount); Assert.Equal(0, second.CreatedDocumentTypeCount);
            var standards = await client.GetFromJsonAsync<AdminProviderRequirementStandardsResponse>(StandardsPath);
            Assert.All(definitionCodes, code => Assert.Contains(standards!.RequirementDefinitions, x => x.RequirementCode == code));
            Assert.All(documentCodes, code => Assert.Contains(standards!.DocumentTypes, x => x.Code == code));
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(x => x.ActionCode == "PROVIDER_REQUIREMENT_DEFAULTS_APPLIED"));
        }
        finally
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            db.ProviderRequirementDefinitions.RemoveRange(db.ProviderRequirementDefinitions.Where(x => definitionCodes.Contains(x.RequirementCode)));
            db.ProviderDocumentTypes.RemoveRange(db.ProviderDocumentTypes.Where(x => documentCodes.Contains(x.Code)));
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotManageProviderRequirementStandards(string role)
    {
        using var client = CreateClient(); await LoginAsync(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(StandardsPath)).StatusCode);
    }

    private async Task RemoveStructuredTestDataAsync(Guid? definitionId, Guid? documentTypeId, Guid? assignmentId, Guid? futurePolicyId)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        if (assignmentId.HasValue)
        {
            var assignment = await db.CategoryProviderRequirementAssignments.SingleOrDefaultAsync(item => item.PublicId == assignmentId.Value);
            if (assignment is not null) { db.CategoryProviderRequirementEvidenceTypes.RemoveRange(db.CategoryProviderRequirementEvidenceTypes.Where(item => item.RequirementAssignmentId == assignment.Id)); db.CategoryProviderRequirementAssignments.Remove(assignment); }
        }
        if (definitionId.HasValue) { var item = await db.ProviderRequirementDefinitions.SingleOrDefaultAsync(value => value.PublicId == definitionId.Value); if (item is not null) db.ProviderRequirementDefinitions.Remove(item); }
        if (documentTypeId.HasValue) { var item = await db.ProviderDocumentTypes.SingleOrDefaultAsync(value => value.PublicId == documentTypeId.Value); if (item is not null) db.ProviderDocumentTypes.Remove(item); }
        if (futurePolicyId.HasValue) { var item = await db.CategoryOperationPolicies.SingleOrDefaultAsync(value => value.PublicId == futurePolicyId.Value); if (item is not null) db.CategoryOperationPolicies.Remove(item); }
        await db.SaveChangesAsync();
    }

    private static object AssignmentInput(Guid definitionId, bool required, bool verification, bool expiry, int order, bool active) => new { RequirementDefinitionId = definitionId, IsRequired = required, VerificationRequired = verification, ExpiryCheckRequired = expiry, MinimumValidDays = (short?)null, DisplayOrder = order, IsActive = active };

    private static string BasePath(Guid serviceId) => $"/api/v1/admin/service-categories/services/{serviceId}/provider-requirements";
    private const string StandardsPath = "/api/v1/admin/provider-requirement-standards";
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
