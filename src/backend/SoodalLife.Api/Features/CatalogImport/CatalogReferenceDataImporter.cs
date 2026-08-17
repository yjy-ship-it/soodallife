using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.CatalogImport;

public sealed record CatalogImportResult(
    int MajorCategoryCount,
    int MiddleCategoryCount,
    int ServiceCategoryCount,
    int FieldDefinitionCount,
    int FieldAssignmentCount,
    int CategoryPolicyCount,
    int FeePolicyCount,
    int DevelopmentAreaCount,
    IReadOnlyList<string> SelectWithoutOptionsFallbackFieldIds);

public sealed class CatalogReferenceDataImporter(
    SoodalLifeDbContext dbContext,
    CatalogWorkbookReader workbookReader,
    ILogger<CatalogReferenceDataImporter> logger)
{
    // The workbook's explicit field ID is the immutable source identity. It must not be derived from row order.
    private static readonly Regex SourceFieldIdPattern = new(
        "^FLD-[0-9]{5}$",
        RegexOptions.CultureInvariant);

    // Approved MVP exception: only these source SELECT fields may fall back to TEXT while options are blank.
    private static readonly HashSet<string> ApprovedSelectWithoutOptionsFallbacks = new(StringComparer.Ordinal)
    {
        "FLD-00046", "FLD-00057", "FLD-00088", "FLD-00119", "FLD-00190", "FLD-00201",
        "FLD-00212", "FLD-00223", "FLD-00234", "FLD-00245", "FLD-00256", "FLD-00267",
        "FLD-00278", "FLD-00289", "FLD-00300", "FLD-00311", "FLD-00362", "FLD-00664",
    };

    public async Task<CatalogImportResult> ImportAsync(
        string workbookPath,
        bool includeDevelopmentAreas,
        CancellationToken cancellationToken = default)
    {
        var workbook = workbookReader.Read(workbookPath);
        ValidateWorkbook(workbook);

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var now = DateTime.UtcNow;
        var categories = await ImportCategoriesAsync(workbook.Categories, now, cancellationToken);
        var feePolicies = await ImportFeePoliciesAsync(workbook.FeePolicies, now, cancellationToken);
        var categoryPolicyCount = await ImportCategoryPoliciesAsync(workbook.Categories, categories.ServicesBySourceId, feePolicies, now, cancellationToken);
        await ImportCompletionPhotoPoliciesAsync(now, cancellationToken);
        var fieldResult = await ImportFieldsAsync(workbook.RequestFields, categories.MiddleByPath, now, cancellationToken);
        var developmentAreaCount = includeDevelopmentAreas
            ? await EnsureDevelopmentAreasAsync(now, cancellationToken)
            : 0;

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        var result = new CatalogImportResult(
            categories.MajorCount,
            categories.MiddleCount,
            categories.ServiceCount,
            fieldResult.DefinitionCount,
            fieldResult.AssignmentCount,
            categoryPolicyCount,
            feePolicies.Count,
            developmentAreaCount,
            fieldResult.FallbackFieldIds);

        logger.LogInformation(
            "Catalog import completed. Major={MajorCount}, Middle={MiddleCount}, Service={ServiceCount}, Fields={FieldCount}, Assignments={AssignmentCount}, Policies={PolicyCount}, SELECT-to-TEXT fallbacks={FallbackCount}: {FallbackIds}",
            result.MajorCategoryCount,
            result.MiddleCategoryCount,
            result.ServiceCategoryCount,
            result.FieldDefinitionCount,
            result.FieldAssignmentCount,
            result.CategoryPolicyCount,
            result.SelectWithoutOptionsFallbackFieldIds.Count,
            string.Join(",", result.SelectWithoutOptionsFallbackFieldIds));
        return result;
    }

    private async Task<CategoryImportState> ImportCategoriesAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tracked = await dbContext.ServiceCategories.ToListAsync(cancellationToken);
        var majorNames = rows.Select(row => Required(row, "대분류")).Distinct(StringComparer.Ordinal).ToArray();
        var majors = new Dictionary<string, ServiceCategory>(StringComparer.Ordinal);

        for (var index = 0; index < majorNames.Length; index++)
        {
            var name = majorNames[index];
            var category = tracked.SingleOrDefault(item => item.LevelCode == "MAJOR" && item.ParentId == null && item.Name == name)
                ?? AddCategory(tracked, "MAJOR", null, name, StableSourceId("MAJOR", name), index + 1, now);
            UpdateCategory(category, "MAJOR", null, name, null, StableSourceId("MAJOR", name), index + 1, now);
            majors[name] = category;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var middlePaths = rows
            .Select(row => (Major: Required(row, "대분류"), Middle: Required(row, "중분류")))
            .Distinct()
            .ToArray();
        var middles = new Dictionary<string, ServiceCategory>(StringComparer.Ordinal);
        var middleSort = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var path in middlePaths)
        {
            var parent = majors[path.Major];
            var sortOrder = middleSort.GetValueOrDefault(path.Major) + 1;
            middleSort[path.Major] = sortOrder;
            var key = CategoryPath(path.Major, path.Middle);
            var sourceId = StableSourceId("MIDDLE", key);
            var category = tracked.SingleOrDefault(item => item.LevelCode == "MIDDLE" && item.ParentId == parent.Id && item.Name == path.Middle)
                ?? AddCategory(tracked, "MIDDLE", parent.Id, path.Middle, sourceId, sortOrder, now);
            UpdateCategory(category, "MIDDLE", parent.Id, path.Middle, null, sourceId, sortOrder, now);
            middles[key] = category;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var services = new Dictionary<string, ServiceCategory>(StringComparer.Ordinal);
        var serviceSort = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var sourceId = Required(row, "ID");
            var externalCode = Required(row, "카테고리코드");
            var middleKey = CategoryPath(Required(row, "대분류"), Required(row, "중분류"));
            var parent = middles[middleKey];
            var sortOrder = serviceSort.GetValueOrDefault(middleKey) + 1;
            serviceSort[middleKey] = sortOrder;
            var name = Required(row, "하위 서비스");
            var category = tracked.SingleOrDefault(item => item.SourceRecordId == sourceId)
                ?? AddCategory(tracked, "SERVICE", parent.Id, name, sourceId, sortOrder, now);
            UpdateCategory(category, "SERVICE", parent.Id, name, externalCode, sourceId, sortOrder, now);
            category.StatusCode = MapCategoryStatus(Required(row, "사용여부"));
            services[sourceId] = category;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CategoryImportState(majorNames.Length, middlePaths.Length, rows.Count, middles, services);
    }

    private async Task<Dictionary<string, FeePolicy>> ImportFeePoliciesAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var sourceRows = rows.Where(row => Value(row, "정책코드").StartsWith("FEE-", StringComparison.Ordinal)).ToArray();
        if (sourceRows.Length != 9)
        {
            throw new InvalidDataException($"Expected 9 referenced fee policies but found {sourceRows.Length}.");
        }

        var tracked = await dbContext.FeePolicies.ToListAsync(cancellationToken);
        var result = new Dictionary<string, FeePolicy>(StringComparer.Ordinal);
        foreach (var row in sourceRows)
        {
            var code = Required(row, "정책코드");
            var policy = tracked.SingleOrDefault(item => item.Code == code);
            if (policy is null)
            {
                policy = new FeePolicy { Code = code, CreatedAt = now, UpdatedAt = now };
                dbContext.FeePolicies.Add(policy);
                tracked.Add(policy);
            }

            policy.PolicyKindCode = code.StartsWith("FEE-Q", StringComparison.Ordinal) ? "QUOTE"
                : code == "FEE-I1" ? "PROJECT" : "SUPPORT";
            policy.TransactionTypeCode = code.StartsWith("FEE-Q", StringComparison.Ordinal) ? "ONE_TIME"
                : code == "FEE-I1" ? "PROJECT" : "SUBSCRIPTION";
            policy.AppliesToText = Required(row, "적용 거래");
            policy.CalculationMethodText = code == "FEE-I1" ? "SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7" : policy.CalculationMethodText;
            policy.MinBaseAmount = ParseDecimal(row, "기본요금 하한(원)");
            policy.MaxBaseAmount = ParseDecimal(row, "기본요금 상한(원)");
            policy.DisplayFeeAmount = ParseDecimal(row, "견적 화면 표시액(원)");
            policy.CurrencyCode = "KRW";
            policy.ChargeTimingText = Required(row, "실제 차감 시점");
            policy.RestoreRuleText = NullIfEmpty(Value(row, "복원 원칙"));
            policy.Note = NullIfEmpty(Value(row, "비고"));
            policy.IsActive = true;
            policy.UpdatedAt = now;
            result[code] = policy;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<int> ImportCategoryPoliciesAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyDictionary<string, ServiceCategory> servicesBySourceId,
        IReadOnlyDictionary<string, FeePolicy> feePolicies,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tracked = await dbContext.CategoryPolicies.ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            var category = servicesBySourceId[Required(row, "ID")];
            var version = Required(row, "정책버전");
            var policy = tracked.SingleOrDefault(item => item.CategoryId == category.Id && item.PolicyVersion == version);
            if (policy is null)
            {
                policy = new CategoryPolicy { CategoryId = category.Id, PolicyVersion = version, CreatedAt = now, UpdatedAt = now };
                dbContext.CategoryPolicies.Add(policy);
                tracked.Add(policy);
            }

            var feePolicyCode = Required(row, "수수료 정책코드");
            policy.TransactionTypeCode = MapTransactionType(Required(row, "거래유형"));
            policy.RequestMethodText = Required(row, "요청방식");
            policy.OnsiteRequirementText = Required(row, "출장서비스");
            policy.IsEmergencyAllowed = Required(row, "긴급출동 허용") == "허용";
            policy.SubscriptionOptionText = Required(row, "정기구독 허용");
            policy.StandardWorkUnitText = Required(row, "표준 작업단위");
            policy.BasePriceAmount = ParseDecimal(row, "기본요금(원)");
            policy.CurrencyCode = "KRW";
            policy.PriceMethodText = Required(row, "가격방식");
            policy.VatDisplayRuleText = Required(row, "부가세 표시");
            policy.MinimumBudgetAmount = ParseDecimal(row, "최소 희망예산(원)");
            policy.MaxQuoteCount = ParseShort(row, "최대 견적수");
            policy.QuoteValidityMinutes = ParseDurationMinutes(Required(row, "견적 유효시간"), numericMeansHours: true);
            policy.FeePolicyId = feePolicies[feePolicyCode].Id;
            policy.EstimatedQuoteFeeAmount = ParseDecimal(row, "예상 견적수수료(원)");
            policy.FeeChargeTimingText = Required(row, "실제 차감시점");
            policy.FeeRestoreConditionText = Required(row, "수수료 복원조건");
            policy.MatchingAreaRuleText = Required(row, "알림 매칭지역");
            policy.NotificationTargetRuleText = Required(row, "알림톡 발송");
            policy.ProviderResponseDeadlineMinutes = ParseDurationMinutes(Required(row, "공급자 응답기한"), numericMeansHours: false);
            policy.RequestFieldSummaryText = Required(row, "요청 필수필드 요약");
            policy.RequiredCompletionPhotoCount = ParseShort(row, "필수사진 수");
            policy.RequiredQualificationSummaryText = Required(row, "필수 자격·증빙");
            policy.InsuranceRequirementText = Required(row, "보험 확인");
            policy.SafetyGradeCode = MapSafetyGrade(Required(row, "안전등급"));
            policy.CompletionEvidenceRuleText = Required(row, "완료 필수증빙");
            policy.DefaultWarrantyDays = ParseShort(row, "기본 A/S일");
            policy.TrustScoreDisplayText = Required(row, "수달신뢰점수 표시");
            policy.DefaultSortCode = "CREDIT_DESC";
            policy.ServiceAreaLevelCode = "SIGUNGU";
            policy.ReferenceUrl = Required(row, "대표 근거 URL");
            policy.AdminNote = Required(row, "관리 메모");
            policy.EffectiveFrom = ParseDate(Required(row, "적용 시작일"));
            policy.EffectiveTo = ParseNullableDate(Value(row, "적용 종료일"));
            policy.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private async Task ImportCompletionPhotoPoliciesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "ReferenceData", "completion-photo-policy-template.json");
        await using var stream = File.OpenRead(templatePath);
        var template = await JsonSerializer.DeserializeAsync<CompletionPolicyTemplate>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }, cancellationToken) ?? throw new InvalidDataException("Completion photo policy template is empty.");
        var duplicateRole = template.Roles.GroupBy(item => item.Code, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicateRole is not null) throw new InvalidDataException($"Duplicate completion photo role: {duplicateRole.Key}");
        var roleDefinitions = template.Roles.ToDictionary(item => item.Code, StringComparer.Ordinal);
        var requirementTemplates = template.RequirementsByTotal.ToDictionary(item => item.Total);

        var trackedRoles = await dbContext.CompletionPhotoRoles.ToListAsync(cancellationToken);
        foreach (var definition in template.Roles)
        {
            var role = trackedRoles.SingleOrDefault(item => item.Code == definition.Code);
            if (role is null)
            {
                role = new CompletionPhotoRole { Code = definition.Code, CreatedAt = now, CreatedByUserId = null };
                dbContext.CompletionPhotoRoles.Add(role);
                trackedRoles.Add(role);
            }
            role.Name = definition.Name;
            role.Description = definition.Description;
            role.IsActive = true;
            role.UpdatedAt = now;
            role.UpdatedByUserId = null;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var policies = await dbContext.CategoryPolicies.ToListAsync(cancellationToken);
        var trackedRequirements = await dbContext.CategoryCompletionPhotoRequirements.ToListAsync(cancellationToken);
        foreach (var policy in policies)
        {
            if (!requirementTemplates.TryGetValue(policy.RequiredCompletionPhotoCount, out var selected))
                throw new InvalidDataException($"No completion photo requirement template exists for total {policy.RequiredCompletionPhotoCount}.");
            foreach (var requirement in selected.Roles)
            {
                if (!roleDefinitions.ContainsKey(requirement.Code))
                    throw new InvalidDataException($"Unknown completion photo role in template: {requirement.Code}");
            }
            var desiredCodes = selected.Roles.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
            var current = trackedRequirements.Where(item => item.CategoryPolicyId == policy.Id).ToList();
            dbContext.CategoryCompletionPhotoRequirements.RemoveRange(current.Where(item =>
                !desiredCodes.Contains(trackedRoles.Single(role => role.Id == item.PhotoRoleId).Code)));
            for (var index = 0; index < selected.Roles.Count; index++)
            {
                var source = selected.Roles[index];
                var role = trackedRoles.Single(item => item.Code == source.Code);
                var requirement = current.SingleOrDefault(item => item.PhotoRoleId == role.Id);
                if (requirement is null)
                {
                    requirement = new CategoryCompletionPhotoRequirement
                    {
                        CategoryPolicyId = policy.Id, PhotoRoleId = role.Id, CreatedAt = now, CreatedByUserId = null,
                    };
                    dbContext.CategoryCompletionPhotoRequirements.Add(requirement);
                    trackedRequirements.Add(requirement);
                }
                requirement.MinimumCount = checked((short)source.MinimumCount);
                requirement.DisplayOrder = index + 1;
                requirement.UpdatedAt = now;
                requirement.UpdatedByUserId = null;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record CompletionPolicyTemplate(
        IReadOnlyList<CompletionRoleTemplate> Roles,
        IReadOnlyList<CompletionRequirementTemplate> RequirementsByTotal);
    private sealed record CompletionRoleTemplate(string Code, string Name, string Description);
    private sealed record CompletionRequirementTemplate(int Total, IReadOnlyList<CompletionRoleCountTemplate> Roles);
    private sealed record CompletionRoleCountTemplate(string Code, int MinimumCount);

    private async Task<FieldImportResult> ImportFieldsAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyDictionary<string, ServiceCategory> middleByPath,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tracked = await dbContext.CategoryFieldDefinitions.ToListAsync(cancellationToken);
        var fallbackIds = new List<string>();
        var imported = new Dictionary<string, (CategoryFieldDefinition Definition, ServiceCategory Middle)>(StringComparer.Ordinal);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var sourceId = Required(row, "필드ID");
            var middle = middleByPath[CategoryPath(Required(row, "대분류"), Required(row, "중분류"))];
            var sourceType = Required(row, "입력유형");
            var options = NullIfEmpty(Value(row, "선택값·단위"));
            var typeCode = MapFieldType(sourceId, sourceType, options, fallbackIds);
            var definition = tracked.SingleOrDefault(item => item.SourceFieldId == sourceId);
            if (definition is null)
            {
                definition = new CategoryFieldDefinition { SourceFieldId = sourceId, CreatedAt = now, UpdatedAt = now };
                dbContext.CategoryFieldDefinitions.Add(definition);
                tracked.Add(definition);
            }

            definition.OwnerMiddleCategoryId = middle.Id;
            definition.FieldKey = Required(row, "필드키");
            definition.Label = Required(row, "화면 라벨");
            definition.FieldTypeCode = typeCode;
            definition.IsRequired = Required(row, "필수여부") == "필수";
            definition.OptionsOrUnitText = options;
            definition.UnitText = typeCode == "SELECT" ? null : options;
            definition.ProviderVisibilityCode = Required(row, "공급자 공개") == "공개" ? "FULL" : "AREA_ONLY";
            definition.PreAcceptMaskingCode = Required(row, "채택 전 마스킹") == "상세주소 마스킹" ? "DETAIL_ADDRESS" : "NONE";
            definition.ValidationRuleText = Required(row, "검증 규칙");
            definition.DisplayOrder = index + 1;
            definition.StatusCode = "ACTIVE";
            definition.UpdatedAt = now;
            imported[sourceId] = (definition, middle);
        }

        if (!fallbackIds.ToHashSet(StringComparer.Ordinal).SetEquals(ApprovedSelectWithoutOptionsFallbacks))
        {
            throw new InvalidDataException("The SELECT-without-options fallback set differs from the 18 approved MVP exceptions.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var fieldOptions = await dbContext.CategoryFieldOptions.ToListAsync(cancellationToken);
        foreach (var (_, value) in imported)
        {
            var importedOptions = value.Definition.FieldTypeCode == "SELECT" && !string.IsNullOrWhiteSpace(value.Definition.OptionsOrUnitText)
                ? value.Definition.OptionsOrUnitText.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                : [];
            var existingOptions = fieldOptions.Where(option => option.FieldDefinitionId == value.Definition.Id).ToList();
            foreach (var existingOption in existingOptions)
            {
                existingOption.IsActive = false;
                existingOption.UpdatedAt = now;
            }

            for (var optionIndex = 0; optionIndex < importedOptions.Length; optionIndex++)
            {
                var optionValue = importedOptions[optionIndex];
                var option = existingOptions.SingleOrDefault(candidate => candidate.Value == optionValue);
                if (option is null)
                {
                    option = new CategoryFieldOption
                    {
                        FieldDefinitionId = value.Definition.Id,
                        Value = optionValue,
                        Label = optionValue,
                        CreatedAt = now,
                    };
                    dbContext.CategoryFieldOptions.Add(option);
                    fieldOptions.Add(option);
                }

                option.DisplayOrder = optionIndex + 1;
                option.IsActive = true;
                option.UpdatedAt = now;
            }
        }

        var assignments = await dbContext.CategoryFieldAssignments.ToListAsync(cancellationToken);
        foreach (var assignment in assignments.Where(item => imported.Values.Any(value => value.Definition.Id == item.FieldDefinitionId)))
        {
            assignment.IsActive = false;
            assignment.UpdatedAt = now;
        }

        foreach (var (_, value) in imported)
        {
            var assignment = assignments.SingleOrDefault(item =>
                item.FieldDefinitionId == value.Definition.Id && item.TargetCategoryId == value.Middle.Id);
            if (assignment is null)
            {
                assignment = new CategoryFieldAssignment
                {
                    FieldDefinitionId = value.Definition.Id,
                    TargetCategoryId = value.Middle.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                dbContext.CategoryFieldAssignments.Add(assignment);
                assignments.Add(assignment);
            }

            assignment.ScopeCode = "MIDDLE";
            assignment.IsActive = true;
            assignment.IsRequired = value.Definition.IsRequired;
            assignment.DisplayOrder = value.Definition.DisplayOrder;
            assignment.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new FieldImportResult(rows.Count, imported.Count, fallbackIds.Order(StringComparer.Ordinal).ToArray());
    }

    private async Task<int> EnsureDevelopmentAreasAsync(DateTime now, CancellationToken cancellationToken)
    {
        const string sourceSystem = "SOODAL_DEV_TEST";
        var existing = await dbContext.AdministrativeAreas
            .Where(area => area.SourceSystemCode == sourceSystem)
            .ToListAsync(cancellationToken);
        var effectiveFrom = new DateOnly(2026, 8, 9);
        var sido = existing.SingleOrDefault(area => area.AreaCode == "DEV-SIDO-01");
        if (sido is null)
        {
            sido = new AdministrativeArea
            {
                SourceSystemCode = sourceSystem,
                AreaCode = "DEV-SIDO-01",
                AreaName = "개발 테스트 시도",
                AreaLevelCode = "SIDO",
                EffectiveFrom = effectiveFrom,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            dbContext.AdministrativeAreas.Add(sido);
            existing.Add(sido);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var (code, name) in new[]
        {
            ("DEV-SIGUNGU-001", "개발 테스트 시군구 A"),
            ("DEV-SIGUNGU-002", "개발 테스트 시군구 B"),
        })
        {
            var area = existing.SingleOrDefault(item => item.AreaCode == code);
            if (area is null)
            {
                area = new AdministrativeArea { AreaCode = code, CreatedAt = now, UpdatedAt = now };
                dbContext.AdministrativeAreas.Add(area);
                existing.Add(area);
            }

            area.SourceSystemCode = sourceSystem;
            area.AreaName = name;
            area.AreaLevelCode = "SIGUNGU";
            area.ParentAreaId = sido.Id;
            area.SourceParentAreaCode = sido.AreaCode;
            area.EffectiveFrom = effectiveFrom;
            area.IsActive = true;
            area.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return 3;
    }

    private static void ValidateWorkbook(CatalogWorkbookData workbook)
    {
        var majorCount = workbook.Categories.Select(row => Required(row, "대분류")).Distinct(StringComparer.Ordinal).Count();
        var middleCount = workbook.Categories
            .Select(row => CategoryPath(Required(row, "대분류"), Required(row, "중분류")))
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (majorCount != 6 || middleCount != 82 || workbook.Categories.Count != 677 || workbook.RequestFields.Count != 837)
        {
            throw new InvalidDataException(
                $"Workbook reconciliation failed. Expected 6/82/677 categories and 837 fields; found {majorCount}/{middleCount}/{workbook.Categories.Count} and {workbook.RequestFields.Count}.");
        }

        EnsureUnique(workbook.Categories, "ID");
        EnsureUnique(workbook.Categories, "카테고리코드");
        EnsureUnique(workbook.RequestFields, "필드ID");

        var invalidSourceFieldIds = workbook.RequestFields
            .Select(row => Required(row, "필드ID"))
            .Where(sourceFieldId => !SourceFieldIdPattern.IsMatch(sourceFieldId))
            .Take(5)
            .ToArray();
        if (invalidSourceFieldIds.Length > 0)
        {
            throw new InvalidDataException(
                "Workbook field IDs must use the stable FLD-00000 source identity format; row positions are not accepted as identifiers.");
        }
    }

    private static void EnsureUnique(IReadOnlyList<IReadOnlyDictionary<string, string>> rows, string column)
    {
        var duplicates = rows.GroupBy(row => Required(row, column), StringComparer.Ordinal).Where(group => group.Count() > 1).ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException($"Column '{column}' contains duplicate source identifiers.");
        }
    }

    private ServiceCategory AddCategory(
        ICollection<ServiceCategory> tracked,
        string level,
        long? parentId,
        string name,
        string sourceId,
        int sortOrder,
        DateTime now)
    {
        var category = new ServiceCategory { CreatedAt = now, UpdatedAt = now };
        UpdateCategory(category, level, parentId, name, null, sourceId, sortOrder, now);
        dbContext.ServiceCategories.Add(category);
        tracked.Add(category);
        return category;
    }

    private static void UpdateCategory(
        ServiceCategory category,
        string level,
        long? parentId,
        string name,
        string? externalCode,
        string sourceId,
        int sortOrder,
        DateTime now)
    {
        category.LevelCode = level;
        category.ParentId = parentId;
        category.Name = name;
        category.ExternalCode = externalCode;
        category.SourceRecordId = sourceId;
        category.StatusCode = "ACTIVE";
        category.SortOrder = sortOrder;
        category.UpdatedAt = now;
    }

    private static string MapFieldType(string sourceId, string sourceType, string? options, ICollection<string> fallbackIds)
    {
        if (sourceType == "선택" && string.IsNullOrWhiteSpace(options))
        {
            if (!ApprovedSelectWithoutOptionsFallbacks.Contains(sourceId))
            {
                throw new InvalidDataException($"Unapproved SELECT field '{sourceId}' has no options.");
            }

            fallbackIds.Add(sourceId);
            return "TEXT";
        }

        return sourceType switch
        {
            "주소" => "ADDRESS",
            "장문" => "LONG_TEXT",
            "일시" => "DATETIME",
            "금액" => "MONEY",
            "파일" => "FILE",
            "텍스트" => "TEXT",
            "선택" => "SELECT",
            "숫자" => "NUMBER",
            "기간" => "PERIOD",
            "반복일정" => "RECURRENCE",
            _ => throw new InvalidDataException($"Unknown field type '{sourceType}'."),
        };
    }

    private static string MapCategoryStatus(string value) => value switch
    {
        "사용" => "ACTIVE",
        "중지" => "PAUSED",
        "검토" => "REVIEW",
        _ => throw new InvalidDataException($"Unknown category status '{value}'."),
    };

    private static string MapTransactionType(string value) => value switch
    {
        "일회성 견적" => "ONE_TIME",
        "구독정산" => "SUBSCRIPTION",
        "인테리어 프로젝트" => "PROJECT",
        _ => throw new InvalidDataException($"Unknown transaction type '{value}'."),
    };

    private static string MapSafetyGrade(string value) => value switch
    {
        "일반" => "NORMAL",
        "중" => "MEDIUM",
        "고" => "HIGH",
        _ => throw new InvalidDataException($"Unknown safety grade '{value}'."),
    };

    private static int ParseDurationMinutes(string value, bool numericMeansHours)
    {
        if (value.EndsWith("분", StringComparison.Ordinal))
        {
            return int.Parse(value[..^1], CultureInfo.InvariantCulture);
        }

        if (value.EndsWith("시간", StringComparison.Ordinal))
        {
            return checked(int.Parse(value[..^2], CultureInfo.InvariantCulture) * 60);
        }

        var number = int.Parse(value, CultureInfo.InvariantCulture);
        return numericMeansHours ? checked(number * 60) : number;
    }

    private static decimal ParseDecimal(IReadOnlyDictionary<string, string> row, string column) =>
        decimal.Parse(Required(row, column), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static short ParseShort(IReadOnlyDictionary<string, string> row, string column) =>
        short.Parse(Required(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly? ParseNullableDate(string value) => string.IsNullOrWhiteSpace(value) ? null : ParseDate(value);
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string Required(IReadOnlyDictionary<string, string> row, string column) =>
        !string.IsNullOrWhiteSpace(Value(row, column)) ? Value(row, column) : throw new InvalidDataException($"Required Excel column '{column}' is blank.");
    private static string Value(IReadOnlyDictionary<string, string> row, string column) => row.GetValueOrDefault(column, string.Empty);
    private static string CategoryPath(string major, string middle) => $"{major}\u001F{middle}";
    private static string StableSourceId(string prefix, string value) =>
        $"{prefix}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16]}";

    private sealed record CategoryImportState(
        int MajorCount,
        int MiddleCount,
        int ServiceCount,
        IReadOnlyDictionary<string, ServiceCategory> MiddleByPath,
        IReadOnlyDictionary<string, ServiceCategory> ServicesBySourceId);
    private sealed record FieldImportResult(int DefinitionCount, int AssignmentCount, IReadOnlyList<string> FallbackFieldIds);
}
