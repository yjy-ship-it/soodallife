using Microsoft.EntityFrameworkCore;
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
        Assert.Equal(17, workbook.RequestFields
            .GroupBy(row => (row["대분류"], row["중분류"], row["필드키"]))
            .Count(group => group.Count() > 1));
    }

    [Fact]
    public async Task Import_Twice_IsIdempotentAndPreservesOfficialCounts()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseInMemoryDatabase($"catalog-import-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new SoodalLifeDbContext(options);
        var importer = new CatalogReferenceDataImporter(
            dbContext,
            new CatalogWorkbookReader(),
            NullLogger<CatalogReferenceDataImporter>.Instance);

        var first = await importer.ImportAsync(GetWorkbookPath(), includeDevelopmentAreas: false);
        var second = await importer.ImportAsync(GetWorkbookPath(), includeDevelopmentAreas: false);

        Assert.Equal((6, 82, 677, 837, 837, 677),
            (first.MajorCategoryCount, first.MiddleCategoryCount, first.ServiceCategoryCount,
                first.FieldDefinitionCount, first.FieldAssignmentCount, first.CategoryPolicyCount));
        Assert.Equal(18, first.SelectWithoutOptionsFallbackFieldIds.Count);
        Assert.Equal(first with { SelectWithoutOptionsFallbackFieldIds = [] }, second with { SelectWithoutOptionsFallbackFieldIds = [] });
        Assert.Equal(first.SelectWithoutOptionsFallbackFieldIds, second.SelectWithoutOptionsFallbackFieldIds);
        Assert.Equal(765, await dbContext.ServiceCategories.CountAsync());
        Assert.Equal(837, await dbContext.CategoryFieldDefinitions.CountAsync());
        Assert.Equal(837, await dbContext.CategoryFieldAssignments.CountAsync());
        Assert.Equal(677, await dbContext.CategoryPolicies.CountAsync());
        Assert.Equal(9, await dbContext.FeePolicies.CountAsync());
        Assert.Equal(18, await dbContext.CategoryFieldDefinitions.CountAsync(field =>
            first.SelectWithoutOptionsFallbackFieldIds.Contains(field.SourceFieldId) && field.FieldTypeCode == "TEXT"));
        Assert.Equal(66, await dbContext.CategoryFieldDefinitions.CountAsync(field => field.FieldTypeCode == "SELECT"));

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
        return Directory.GetFiles(Path.Combine(directory.FullName, "docs", "source"), "*.xlsx").Single();
    }
}
