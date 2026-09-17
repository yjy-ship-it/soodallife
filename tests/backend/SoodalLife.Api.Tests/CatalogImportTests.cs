using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.CatalogImport;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CatalogImportTests
{
    [Fact]
    public void WorkbookReader_ReconcilesOfficialCountsAndApprovedFallbacks()
    {
        var workbook = new CatalogWorkbookReader().Read(GetWorkbookPath());

        Assert.Equal(677, workbook.Categories.Count);
        Assert.Equal(6, workbook.Categories.Select(row => row["대분류"]).Distinct().Count());
        Assert.Equal(82, workbook.Categories.Select(row => (row["대분류"], row["중분류"])).Distinct().Count());
        Assert.Equal(837, workbook.RequestFields.Count);
        Assert.Equal(18, workbook.RequestFields.Count(row =>
            row["입력유형"] == "선택" && string.IsNullOrWhiteSpace(row["선택값·단위"])));
        Assert.All(workbook.RequestFields, row => Assert.Matches("^FLD-[0-9]{5}$", row["필드ID"]));
        Assert.Equal(837, workbook.RequestFields.Select(row => row["필드ID"]).Distinct().Count());
        Assert.Equal(11, workbook.RequestFields.Count(row => row["적용 서비스"] != "해당 중분류 전체"));
        Assert.Equal(17, workbook.RequestFields
            .GroupBy(row => (row["대분류"], row["중분류"], row["필드키"]))
            .Count(group => group.Count() > 1));
    }

    [Fact]
    public async Task Import_Twice_IsIdempotentAndPreservesOfficialCounts()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseInMemoryDatabase($"catalog-import-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        await using var dbContext = new SoodalLifeDbContext(options);
        var importer = new CatalogReferenceDataImporter(
            dbContext,
            new CatalogWorkbookReader(),
            NullLogger<CatalogReferenceDataImporter>.Instance);

        var first = await importer.ImportAsync(GetWorkbookPath(), includeDevelopmentAreas: false);
        var second = await importer.ImportAsync(GetWorkbookPath(), includeDevelopmentAreas: false);

        Assert.Equal((6, 82, 677, 837, 879, 677),
            (first.MajorCategoryCount, first.MiddleCategoryCount, first.ServiceCategoryCount,
                first.FieldDefinitionCount, first.FieldAssignmentCount, first.CategoryPolicyCount));
        Assert.Equal(18, first.SelectWithoutOptionsFallbackFieldIds.Count);
        Assert.Equal(first with { SelectWithoutOptionsFallbackFieldIds = [] }, second with { SelectWithoutOptionsFallbackFieldIds = [] });
        Assert.Equal(first.SelectWithoutOptionsFallbackFieldIds, second.SelectWithoutOptionsFallbackFieldIds);
        Assert.Equal(765, await dbContext.ServiceCategories.CountAsync());
        Assert.Equal(837, await dbContext.CategoryFieldDefinitions.CountAsync());
        Assert.Equal(879, await dbContext.CategoryFieldAssignments.CountAsync());
        Assert.Equal(879, await dbContext.CategoryFieldAssignments.CountAsync(assignment => assignment.IsActive));
        Assert.Equal(677, await dbContext.CategoryPolicies.CountAsync());
        Assert.Equal(9, await dbContext.FeePolicies.CountAsync());
        Assert.Equal(3, await dbContext.CompletionPhotoRoles.CountAsync());
        var policies = await dbContext.CategoryPolicies.ToListAsync();
        var requirements = await dbContext.CategoryCompletionPhotoRequirements.ToListAsync();
        Assert.All(policies, policy => Assert.Equal(policy.RequiredCompletionPhotoCount,
            requirements.Where(item => item.CategoryPolicyId == policy.Id).Sum(item => item.MinimumCount)));
        Assert.Equal(requirements.Count, requirements.Select(item => (item.CategoryPolicyId, item.PhotoRoleId)).Distinct().Count());
        Assert.Equal(18, await dbContext.CategoryFieldDefinitions.CountAsync(field =>
            first.SelectWithoutOptionsFallbackFieldIds.Contains(field.SourceFieldId) && field.FieldTypeCode == "TEXT"));
        Assert.Equal(66, await dbContext.CategoryFieldDefinitions.CountAsync(field => field.FieldTypeCode == "SELECT"));

        var kitchenFaucet = await dbContext.ServiceCategories.SingleAsync(category =>
            category.LevelCode == "SERVICE" && category.Name == "주방 수전 교체");
        var kitchenMiddle = await dbContext.ServiceCategories.SingleAsync(category =>
            category.LevelCode == "MIDDLE" && category.Name == "주방 수리");
        var quantityField = await dbContext.CategoryFieldDefinitions.SingleAsync(field => field.SourceFieldId == "FLD-00058");
        Assert.Equal("target_quantity", quantityField.FieldKey);
        Assert.Equal("수리·교체 대상 수량", quantityField.Label);
        Assert.Equal("개", quantityField.UnitText);
        Assert.True(await dbContext.CategoryFieldAssignments.AnyAsync(assignment =>
            assignment.FieldDefinitionId == quantityField.Id && assignment.TargetCategoryId == kitchenFaucet.Id && assignment.IsActive));
        Assert.False(await dbContext.CategoryFieldAssignments.AnyAsync(assignment =>
            assignment.FieldDefinitionId == quantityField.Id && assignment.TargetCategoryId == kitchenMiddle.Id && assignment.IsActive));

        var duplicateBusinessKeyGroups = (await dbContext.CategoryFieldDefinitions.ToListAsync())
            .GroupBy(field => (field.OwnerMiddleCategoryId, field.FieldKey))
            .Where(group => group.Count() > 1)
            .ToArray();
        Assert.Equal(17, duplicateBusinessKeyGroups.Length);
        Assert.All(duplicateBusinessKeyGroups, group => Assert.Equal(2, group.Count()));
    }

    [Fact]
    public void Model_UsesSourceIdentityForUniquenessAndAllowsDuplicateBusinessKeys()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseInMemoryDatabase($"catalog-model-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        using var dbContext = new SoodalLifeDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(CategoryFieldDefinition));

        Assert.NotNull(entityType);
        var sourceIdentityIndex = entityType.FindIndex(entityType.FindProperty(nameof(CategoryFieldDefinition.SourceFieldId))!);
        var businessKeyIndex = entityType.FindIndex([
            entityType.FindProperty(nameof(CategoryFieldDefinition.OwnerMiddleCategoryId))!,
            entityType.FindProperty(nameof(CategoryFieldDefinition.FieldKey))!]);

        Assert.NotNull(sourceIdentityIndex);
        Assert.True(sourceIdentityIndex.IsUnique);
        Assert.NotNull(businessKeyIndex);
        Assert.False(businessKeyIndex.IsUnique);
    }

    private static string GetWorkbookPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SoodalLife.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "docs", "source", "수달_라이프_전체_서비스_카테고리_및_수수료_관리대장_v1.2.xlsx");
    }
}
