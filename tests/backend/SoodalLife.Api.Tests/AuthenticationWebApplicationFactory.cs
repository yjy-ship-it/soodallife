using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AuthenticationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"authentication-tests-{Guid.NewGuid():N}";
    private readonly string _privacyHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public IReadOnlyDictionary<string, TestCredential> Credentials { get; } =
        RoleCodes.All.ToDictionary(
            role => role,
            role => new TestCredential(
                $"test-{role.ToLowerInvariant()}-{Guid.NewGuid():N}",
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            StringComparer.Ordinal);

    public TestCredential OtherCustomerCredential { get; } = new(
        $"test-other-customer-{Guid.NewGuid():N}",
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public TestCredential ServiceMismatchProviderCredential { get; } = NewCredential("service-mismatch-provider");
    public TestCredential AreaMismatchProviderCredential { get; } = NewCredential("area-mismatch-provider");
    public TestCredential InactiveProviderCredential { get; } = NewCredential("inactive-provider");

    public TestCatalogIds Catalog { get; private set; } = new(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, []);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SoodalLife"] = "Server=(local);Database=authentication-tests;Trusted_Connection=True;",
                ["PrivacyProtection:DualWriteEnabled"] = "true",
                ["PrivacyProtection:SearchHashKey"] = _privacyHashKey,
                ["PrivacyProtection:EncryptedReadEnabled"] = "true",
                ["SubscriptionPaymentGateway:Enabled"] = "true",
                ["SubscriptionPaymentGateway:ClientKey"] = "test_ck_subscription",
                ["SubscriptionPaymentGateway:SecretKey"] = "test_sk_subscription",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SoodalLifeDbContext>>();
            services.RemoveAll<SoodalLifeDbContext>();
            services.RemoveAll<IIdentityVerificationAdapter>();
            services.RemoveAll<ISubscriptionPaymentGateway>();
            services.AddDbContext<SoodalLifeDbContext>((provider, options) => options
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .AddInterceptors(provider.GetRequiredService<SoodalLife.Api.Infrastructure.Security.PersonalDataProtectionInterceptor>(),
                    provider.GetRequiredService<SoodalLife.Api.Infrastructure.Security.PersonalDataReadInterceptor>()));
            services.AddSingleton<IIdentityVerificationAdapter, TestIdentityVerificationAdapter>();
            services.AddSingleton<ISubscriptionPaymentGateway,TestSubscriptionPaymentGateway>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedAuthenticationData(host.Services);
        return host;
    }

    private void SeedAuthenticationData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        foreach (var (roleCode, credential) in Credentials)
        {
            var role = new Role { Code = roleCode, Name = roleCode, IsActive = true };
            dbContext.Roles.Add(role);

            var user = new User
            {
                LoginId = credential.LoginId,
                NormalizedLoginId = credential.LoginId.ToUpperInvariant(),
                PhoneVerificationStatusCode = "VERIFIED",
                StatusCode = "ACTIVE",
            };
            user.PasswordHash = passwordHasher.HashPassword(user, credential.Password);
            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            if (roleCode == RoleCodes.Customer)
            {
                dbContext.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = "Test Customer" });
            }
            else if (roleCode == RoleCodes.Provider)
            {
                dbContext.ProviderProfiles.Add(new ProviderProfile
                {
                    UserId = user.Id,
                    BusinessName = "Test Provider",
                    ApprovalStatusCode = "APPROVED",
                    ActivityStatusCode = "ACTIVE",
                });
            }
            dbContext.SaveChanges();
        }

        var customerRole = dbContext.Roles.Single(role => role.Code == RoleCodes.Customer);
        var otherCustomer = new User
        {
            LoginId = OtherCustomerCredential.LoginId,
            NormalizedLoginId = OtherCustomerCredential.LoginId.ToUpperInvariant(),
            PhoneVerificationStatusCode = "VERIFIED",
            StatusCode = "ACTIVE",
        };
        otherCustomer.PasswordHash = passwordHasher.HashPassword(otherCustomer, OtherCustomerCredential.Password);
        dbContext.Users.Add(otherCustomer);
        dbContext.SaveChanges();
        dbContext.UserRoles.Add(new UserRole { UserId = otherCustomer.Id, RoleId = customerRole.Id });
        dbContext.CustomerProfiles.Add(new CustomerProfile { UserId = otherCustomer.Id, DisplayName = "Other Test Customer" });
        dbContext.SaveChanges();

        AddProvider(dbContext, passwordHasher, ServiceMismatchProviderCredential, "Service Mismatch", "APPROVED", "ACTIVE");
        AddProvider(dbContext, passwordHasher, AreaMismatchProviderCredential, "Area Mismatch", "APPROVED", "ACTIVE");
        AddProvider(dbContext, passwordHasher, InactiveProviderCredential, "Inactive Provider", "PENDING", "INACTIVE");

        dbContext.ProviderRequirementTypes.AddRange(
            new ProviderRequirementType { Code = "QUALIFICATION", Name = "자격", IsActive = true },
            new ProviderRequirementType { Code = "LICENSE", Name = "면허", IsActive = true },
            new ProviderRequirementType { Code = "INSURANCE", Name = "보험", IsActive = true },
            new ProviderRequirementType { Code = "SAFETY", Name = "안전", IsActive = true },
            new ProviderRequirementType { Code = "EVIDENCE_VALIDITY", Name = "증빙 유효성", IsActive = true });
        dbContext.SaveChanges();

        SeedRequestCatalog(dbContext);
        SeedAdvertisingPlacements(dbContext);
        SeedMatchingProviderScopes(dbContext);
        SeedTradingPrerequisites(dbContext);
        SeedTrustPolicyDraft(dbContext);
    }

    private static void SeedTrustPolicyDraft(SoodalLifeDbContext dbContext)
    {
        if (dbContext.TrustPolicies.Any(item => item.PublicId == TrustPolicyDraftDefaults.PublicId)) return;

        var now = DateTime.UtcNow;
        dbContext.TrustPolicies.Add(new TrustPolicy
        {
            PublicId = TrustPolicyDraftDefaults.PublicId,
            PolicyVersion = TrustPolicyDraftDefaults.Version,
            PolicyName = "전문가 신뢰도 자동산정 정책 초안",
            TargetTypeCode = "PROVIDER",
            ScopeTypeCode = "GLOBAL",
            StatusCode = "DRAFT",
            RulesJson = TrustPolicyDraftDefaults.RulesJson,
            CreatedAt = now,
            UpdatedAt = now,
        });
        dbContext.SaveChanges();
    }

    private static void SeedAdvertisingPlacements(SoodalLifeDbContext dbContext)
    {
        if (dbContext.AdvertisingPlacements.Any()) return;
        var now = DateTime.UtcNow;
        dbContext.AdvertisingPlacements.AddRange(
            new AdvertisingPlacement { Code = "CUSTOMER_HOME", Name = "고객 홈", Description = "고객 역할 홈 화면", RouteHint = "/customer", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new AdvertisingPlacement { Code = "PROVIDER_HOME", Name = "전문가 홈", Description = "전문가 역할 홈 화면", RouteHint = "/provider", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new AdvertisingPlacement { Code = "CUSTOMER_LIVE_ACTIVITY_FEED", Name = "고객 실시간 서비스 목록", Description = "실시간 서비스 목록 사이 광고", RouteHint = "/customer#live-activity", IsActive = true, CreatedAt = now, UpdatedAt = now });
        dbContext.SaveChanges();
    }

    private void SeedRequestCatalog(SoodalLifeDbContext dbContext)
    {
        var major = new ServiceCategory { LevelCode = "MAJOR", Name = "테스트 대분류", StatusCode = "ACTIVE", SortOrder = 1 };
        dbContext.ServiceCategories.Add(major);
        dbContext.SaveChanges();
        var middle = new ServiceCategory { ParentId = major.Id, LevelCode = "MIDDLE", Name = "테스트 중분류", StatusCode = "ACTIVE", SortOrder = 1 };
        dbContext.ServiceCategories.Add(middle);
        dbContext.SaveChanges();
        var service = new ServiceCategory
        {
            ParentId = middle.Id,
            LevelCode = "SERVICE",
            ExternalCode = "TEST-001",
            SourceRecordId = "TEST-001",
            Name = "테스트 서비스",
            StatusCode = "ACTIVE",
            SortOrder = 1,
        };
        var otherService = new ServiceCategory
        {
            ParentId = middle.Id,
            LevelCode = "SERVICE",
            ExternalCode = "TEST-002",
            SourceRecordId = "TEST-002",
            Name = "다른 테스트 서비스",
            StatusCode = "ACTIVE",
            SortOrder = 2,
        };
        var fee = new FeePolicy
        {
            Code = "TEST-FEE",
            PolicyKindCode = "QUOTE",
            TransactionTypeCode = "ONE_TIME",
            AppliesToText = "서비스별 견적 채택",
            DisplayFeeAmount = 3000m,
            ChargeTimingText = "수요자 견적 채택 시",
            RestoreRuleText = "허위요청·시스템오류·본사 승인 사유 시 원장 복원",
            IsActive = true,
        };
        var area = new AdministrativeArea
        {
            SourceSystemCode = "TEST",
            AreaCode = "TEST-SIGUNGU",
            AreaName = "테스트 시군구",
            AreaLevelCode = "SIGUNGU",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            IsActive = true,
        };
        var otherArea = new AdministrativeArea
        {
            SourceSystemCode = "TEST",
            AreaCode = "TEST-OTHER-SIGUNGU",
            AreaName = "다른 테스트 시군구",
            AreaLevelCode = "SIGUNGU",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            IsActive = true,
        };
        dbContext.AddRange(service, otherService, fee, area, otherArea);
        dbContext.SaveChanges();
        var completionPolicy = new CategoryPolicy
        {
            CategoryId = service.Id,
            PolicyVersion = "test-v1",
            TransactionTypeCode = "ONE_TIME",
            RequestMethodText = "테스트",
            OnsiteRequirementText = "필수",
            IsEmergencyAllowed = true,
            SubscriptionOptionText = "선택",
            StandardWorkUnitText = "1회",
            BasePriceAmount = 120000m,
            CurrencyCode = "KRW",
            PriceMethodText = "견적형",
            VatDisplayRuleText = "포함/별도 필수표시",
            MinimumBudgetAmount = 70000m,
            MaxQuoteCount = 5,
            QuoteValidityMinutes = 120,
            FeePolicyId = fee.Id,
            EstimatedQuoteFeeAmount = 3000m,
            FeeChargeTimingText = "수요자 견적 채택 시",
            FeeRestoreConditionText = "허위요청·시스템오류·본사 승인 사유 시 원장 복원",
            MatchingAreaRuleText = "시군구",
            NotificationTargetRuleText = "테스트",
            ProviderResponseDeadlineMinutes = 30,
            RequestFieldSummaryText = "테스트",
            RequiredCompletionPhotoCount = 2,
            RequiredQualificationSummaryText = "관련 자격·사업자 확인",
            InsuranceRequirementText = "필수",
            SafetyGradeCode = "NORMAL",
            CompletionEvidenceRuleText = "테스트",
            DefaultWarrantyDays = 30,
            TrustScoreDisplayText = "테스트",
            DefaultSortCode = "CREDIT_DESC",
            ServiceAreaLevelCode = "SIGUNGU",
            ReferenceUrl = "https://example.test",
            AdminNote = "테스트",
            EffectiveFrom = new DateOnly(2026, 1, 1),
        };
        dbContext.CategoryPolicies.Add(completionPolicy);
        dbContext.CategoryPolicies.Add(CreateTestPolicy(otherService.Id, fee.Id));
        dbContext.SaveChanges();
        foreach (var legacyPolicy in dbContext.CategoryPolicies.ToList())
        {
            var sourceFee = dbContext.FeePolicies.Single(item => item.Id == legacyPolicy.FeePolicyId);
            dbContext.CategoryPricePolicies.Add(new CategoryPricePolicy
            {
                CategoryId = legacyPolicy.CategoryId, LegacyCategoryPolicyId = legacyPolicy.Id, PolicyVersion = legacyPolicy.PolicyVersion,
                LegacyPriceMethodText = legacyPolicy.PriceMethodText, BaseAmount = legacyPolicy.BasePriceAmount,
                MinimumBudgetAmount = legacyPolicy.MinimumBudgetAmount, UnitText = legacyPolicy.StandardWorkUnitText,
                CurrencyCode = legacyPolicy.CurrencyCode, LegacyVatDisplayRuleText = legacyPolicy.VatDisplayRuleText,
                EffectiveFrom = legacyPolicy.EffectiveFrom, EffectiveTo = legacyPolicy.EffectiveTo, IsActive = true,
            });
            dbContext.CategoryFeePolicies.Add(new CategoryFeePolicy
            {
                CategoryId = legacyPolicy.CategoryId, LegacyCategoryPolicyId = legacyPolicy.Id, SourceFeePolicyId = sourceFee.Id,
                PolicyVersion = legacyPolicy.PolicyVersion, PolicyKindCode = sourceFee.PolicyKindCode,
                TransactionTypeCode = sourceFee.TransactionTypeCode, CalculationMethodText = sourceFee.CalculationMethodText,
                FeeAmount = legacyPolicy.EstimatedQuoteFeeAmount, MinBaseAmount = sourceFee.MinBaseAmount, MaxBaseAmount = sourceFee.MaxBaseAmount,
                Rate = sourceFee.Rate, MonthlyAmount = sourceFee.MonthlyAmount, PerVisitAmount = sourceFee.PerVisitAmount,
                CurrencyCode = sourceFee.CurrencyCode, ChargeTimingText = legacyPolicy.FeeChargeTimingText,
                RestoreRuleText = legacyPolicy.FeeRestoreConditionText, EffectiveFrom = legacyPolicy.EffectiveFrom,
                EffectiveTo = legacyPolicy.EffectiveTo, IsActive = sourceFee.IsActive,
            });
            dbContext.CategoryOperationPolicies.Add(new CategoryOperationPolicy
            {
                CategoryId = legacyPolicy.CategoryId, LegacyCategoryPolicyId = legacyPolicy.Id, PolicyVersion = legacyPolicy.PolicyVersion,
                RequestMethodText = legacyPolicy.RequestMethodText, OnsiteRequirementText = legacyPolicy.OnsiteRequirementText,
                IsEmergencyAllowed = legacyPolicy.IsEmergencyAllowed, SubscriptionOptionText = legacyPolicy.SubscriptionOptionText,
                MaxQuoteCount = legacyPolicy.MaxQuoteCount, QuoteValidityMinutes = legacyPolicy.QuoteValidityMinutes,
                MatchingAreaRuleText = legacyPolicy.MatchingAreaRuleText, NotificationTargetRuleText = legacyPolicy.NotificationTargetRuleText,
                ProviderResponseDeadlineMinutes = legacyPolicy.ProviderResponseDeadlineMinutes, RequestFieldSummaryText = legacyPolicy.RequestFieldSummaryText,
                RequiredCompletionPhotoCount = legacyPolicy.RequiredCompletionPhotoCount, RequiredQualificationSummaryText = legacyPolicy.RequiredQualificationSummaryText,
                InsuranceRequirementText = legacyPolicy.InsuranceRequirementText, SafetyGradeCode = legacyPolicy.SafetyGradeCode,
                CompletionEvidenceRuleText = legacyPolicy.CompletionEvidenceRuleText, DefaultWarrantyDays = legacyPolicy.DefaultWarrantyDays,
                TrustScoreDisplayText = legacyPolicy.TrustScoreDisplayText, DefaultSortCode = legacyPolicy.DefaultSortCode,
                ServiceAreaLevelCode = legacyPolicy.ServiceAreaLevelCode, ReferenceUrl = legacyPolicy.ReferenceUrl, AdminNote = legacyPolicy.AdminNote,
                EffectiveFrom = legacyPolicy.EffectiveFrom, EffectiveTo = legacyPolicy.EffectiveTo, IsActive = true,
            });
        }
        dbContext.SaveChanges();
        var before = new CompletionPhotoRole { Code = "BEFORE", Name = "작업 전", Description = "작업 전 사진", IsActive = true };
        var after = new CompletionPhotoRole { Code = "AFTER", Name = "작업 후", Description = "작업 후 사진", IsActive = true };
        var other = new CompletionPhotoRole { Code = "OTHER", Name = "추가 증빙", Description = "작업 과정 또는 상세 사진", IsActive = true };
        dbContext.CompletionPhotoRoles.AddRange(before, after, other);
        dbContext.SaveChanges();
        dbContext.CategoryCompletionPhotoRequirements.AddRange(
            new CategoryCompletionPhotoRequirement { CategoryPolicyId = completionPolicy.Id, PhotoRoleId = before.Id, MinimumCount = 1, DisplayOrder = 1 },
            new CategoryCompletionPhotoRequirement { CategoryPolicyId = completionPolicy.Id, PhotoRoleId = after.Id, MinimumCount = 1, DisplayOrder = 2 });

        var fields = new[]
        {
            new CategoryFieldDefinition
            {
                SourceFieldId = "TEST-FIELD-1", OwnerMiddleCategoryId = middle.Id, FieldKey = "work_scope_detail",
                Label = "작업 요청 상세", FieldTypeCode = "LONG_TEXT", IsRequired = true,
                ValidationRuleText = "20자 이상", DisplayOrder = 1, StatusCode = "ACTIVE",
            },
            new CategoryFieldDefinition
            {
                SourceFieldId = "TEST-FIELD-2", OwnerMiddleCategoryId = middle.Id, FieldKey = "desired_date",
                Label = "희망일시", FieldTypeCode = "DATETIME", IsRequired = true,
                ValidationRuleText = "현재 이후", DisplayOrder = 2, StatusCode = "ACTIVE",
            },
            new CategoryFieldDefinition
            {
                SourceFieldId = "TEST-FIELD-3", OwnerMiddleCategoryId = middle.Id, FieldKey = "space_type",
                Label = "공간 유형", FieldTypeCode = "SELECT", IsRequired = true, OptionsOrUnitText = "주거/상가",
                ValidationRuleText = "선택", DisplayOrder = 3, StatusCode = "ACTIVE",
            },
        };
        dbContext.CategoryFieldDefinitions.AddRange(fields);
        dbContext.SaveChanges();
        dbContext.CategoryFieldAssignments.AddRange(fields.Select(field => new CategoryFieldAssignment
        {
            FieldDefinitionId = field.Id,
            TargetCategoryId = middle.Id,
            ScopeCode = "MIDDLE",
            IsRequired = field.IsRequired,
            DisplayOrder = field.DisplayOrder,
            IsActive = true,
        }));
        dbContext.CategoryFieldOptions.AddRange(
            new CategoryFieldOption { FieldDefinitionId = fields[2].Id, Value = "주거", Label = "주거", DisplayOrder = 1, IsActive = true },
            new CategoryFieldOption { FieldDefinitionId = fields[2].Id, Value = "상가", Label = "상가", DisplayOrder = 2, IsActive = true });
        dbContext.SaveChanges();
        Catalog = new TestCatalogIds(
            service.PublicId,
            otherService.PublicId,
            area.PublicId,
            otherArea.PublicId,
            major.PublicId,
            fields.Select(field => field.PublicId).ToArray());
    }

    private void SeedMatchingProviderScopes(SoodalLifeDbContext dbContext)
    {
        SeedProviderScope(dbContext, Credentials[RoleCodes.Provider].LoginId, Catalog.ServiceId, Catalog.AreaId);
        SeedProviderScope(dbContext, ServiceMismatchProviderCredential.LoginId, Catalog.OtherServiceId, Catalog.AreaId);
        SeedProviderScope(dbContext, AreaMismatchProviderCredential.LoginId, Catalog.ServiceId, Catalog.OtherAreaId);
        SeedProviderScope(dbContext, InactiveProviderCredential.LoginId, Catalog.ServiceId, Catalog.AreaId);
    }

    private void SeedTradingPrerequisites(SoodalLifeDbContext dbContext)
    {
        var serviceId = dbContext.ServiceCategories.Single(item => item.PublicId == Catalog.ServiceId).Id;
        var operationPolicy = dbContext.CategoryOperationPolicies.Single(item => item.CategoryId == serviceId);
        var definition = new ProviderRequirementDefinition
        {
            RequirementTypeCode = "QUALIFICATION", RequirementCode = "TEST_REQUIRED_QUALIFICATION",
            Name = "테스트 필수 자격", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        dbContext.ProviderRequirementDefinitions.Add(definition);
        dbContext.SaveChanges();
        var assignment = new CategoryProviderRequirementAssignment
        {
            CategoryOperationPolicyId = operationPolicy.Id, RequirementDefinitionId = definition.Id,
            IsRequired = true, VerificationRequired = true, ExpiryCheckRequired = false, DisplayOrder = 1,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        dbContext.CategoryProviderRequirementAssignments.Add(assignment);
        dbContext.SaveChanges();
        foreach (var service in dbContext.ProviderServiceCategories.Where(item => item.CategoryId == serviceId).ToList())
        {
            dbContext.ProviderServiceApprovals.Add(new ProviderServiceApproval
            {
                ProviderServiceCategoryId = service.Id, ApprovalStatusCode = "APPROVED", ApprovalRequestedAt = DateTime.UtcNow,
                ApprovalDecidedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
            dbContext.ProviderServiceRequirementVerifications.Add(new ProviderServiceRequirementVerification
            {
                ProviderServiceCategoryId = service.Id, RequirementAssignmentId = assignment.Id,
                VerificationStatusCode = "APPROVED", VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
        }
        foreach (var provider in dbContext.ProviderProfiles.ToList())
        {
            dbContext.ProviderWallets.Add(new ProviderWallet
            {
                ProviderProfileId = provider.Id, CurrencyCode = "KRW", AvailableBalance = 100000m,
                ReservedBalance = 0, StatusCode = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
        }
        dbContext.SaveChanges();
    }

    private static void SeedProviderScope(SoodalLifeDbContext dbContext, string loginId, Guid categoryPublicId, Guid areaPublicId)
    {
        var providerId = (from user in dbContext.Users
                          join provider in dbContext.ProviderProfiles on user.Id equals provider.UserId
                          where user.NormalizedLoginId == loginId.ToUpperInvariant()
                          select provider.Id).Single();
        var categoryId = dbContext.ServiceCategories.Single(item => item.PublicId == categoryPublicId).Id;
        var areaId = dbContext.AdministrativeAreas.Single(item => item.PublicId == areaPublicId).Id;
        var providerService = new ProviderServiceCategory
        {
            ProviderProfileId = providerId,
            CategoryId = categoryId,
            StatusCode = "ACTIVE",
            ActivatedAt = DateTime.UtcNow,
        };
        dbContext.ProviderServiceCategories.Add(providerService);
        dbContext.SaveChanges();
        dbContext.ProviderServiceAreas.Add(new ProviderServiceArea
        {
            ProviderServiceCategoryId = providerService.Id,
            AdministrativeAreaId = areaId,
            StatusCode = "ACTIVE",
            ActivatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();
    }

    private static void AddProvider(
        SoodalLifeDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        TestCredential credential,
        string businessName,
        string approvalStatus,
        string activityStatus)
    {
        var providerRole = dbContext.Roles.Single(role => role.Code == RoleCodes.Provider);
        var user = new User
        {
            LoginId = credential.LoginId,
            NormalizedLoginId = credential.LoginId.ToUpperInvariant(),
            PhoneVerificationStatusCode = "VERIFIED",
            StatusCode = "ACTIVE",
        };
        user.PasswordHash = passwordHasher.HashPassword(user, credential.Password);
        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = providerRole.Id });
        dbContext.ProviderProfiles.Add(new ProviderProfile
        {
            UserId = user.Id,
            BusinessName = businessName,
            ApprovalStatusCode = approvalStatus,
            ActivityStatusCode = activityStatus,
        });
        dbContext.SaveChanges();
    }

    private static CategoryPolicy CreateTestPolicy(long categoryId, long feePolicyId) => new()
    {
        CategoryId = categoryId,
        PolicyVersion = "test-v1",
        TransactionTypeCode = "ONE_TIME",
        RequestMethodText = "요청서 접수",
        OnsiteRequirementText = "필수",
        SubscriptionOptionText = "선택",
        StandardWorkUnitText = "1회",
        BasePriceAmount = 90000m,
        CurrencyCode = "KRW",
        PriceMethodText = "예약가",
        VatDisplayRuleText = "포함/별도 필수표시",
        MinimumBudgetAmount = 50000m,
        MaxQuoteCount = 5,
        QuoteValidityMinutes = 120,
        FeePolicyId = feePolicyId,
        EstimatedQuoteFeeAmount = 2000m,
        FeeChargeTimingText = "수요자 견적 채택 시",
        FeeRestoreConditionText = "허위요청·시스템오류·본사 승인 사유 시 원장 복원",
        MatchingAreaRuleText = "시군구",
        NotificationTargetRuleText = "대상 전문가",
        ProviderResponseDeadlineMinutes = 30,
        RequestFieldSummaryText = "요청 필수항목",
        RequiredQualificationSummaryText = "필수 자격 확인",
        InsuranceRequirementText = "보험 확인",
        SafetyGradeCode = "NORMAL",
        CompletionEvidenceRuleText = "완료 증빙",
        TrustScoreDisplayText = "신뢰점수 표시",
        DefaultSortCode = "CREDIT_DESC",
        ServiceAreaLevelCode = "SIGUNGU",
        ReferenceUrl = "https://example.test",
        AdminNote = "테스트 정책",
        EffectiveFrom = new DateOnly(2026, 1, 1),
    };

    private static TestCredential NewCredential(string prefix) => new(
        $"test-{prefix}-{Guid.NewGuid():N}",
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
}

internal sealed class TestSubscriptionPaymentGateway:ISubscriptionPaymentGateway
{
    public Task<SubscriptionGatewayBillingKeyResult> IssueBillingKeyAsync(string providerCode,string authKey,string customerKey,string idempotencyKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayBillingKeyResult($"billing-{authKey}","CARD","****-****-****-1234"));
    public Task<SubscriptionGatewayPaymentResult> ChargeRecurringAsync(string providerCode,string billingKey,string customerKey,string orderId,string orderName,decimal amount,string currencyCode,string idempotencyKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentResult($"payment-{orderId}",null));
    public Task<SubscriptionGatewayPaymentStatus> GetPaymentAsync(string providerCode,string paymentKey,CancellationToken token){var orderId=paymentKey.StartsWith("payment-",StringComparison.Ordinal)?paymentKey[8..]:string.Empty;return Task.FromResult(new SubscriptionGatewayPaymentStatus(paymentKey,orderId,"DONE",0,0));}
    public Task<SubscriptionGatewayPaymentStatus> GetPaymentByOrderIdAsync(string providerCode,string orderId,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentStatus($"payment-{orderId}",orderId,"DONE",0,0));
    public Task CancelBillingKeyAsync(string providerCode,string billingKey,string idempotencyKey,CancellationToken token)=>Task.CompletedTask;
    public Task<SubscriptionGatewayPaymentResult> RefundPaymentAsync(string providerCode,string paymentKey,decimal amount,string reason,string idempotencyKey,CancellationToken token)=>Task.FromResult(new SubscriptionGatewayPaymentResult(paymentKey,$"refund-{Guid.NewGuid():N}"));
}

internal sealed class TestIdentityVerificationAdapter : IIdentityVerificationAdapter
{
    internal const string VerificationToken = "TEST-PHONE-VERIFIED";

    public Task<IdentityVerificationStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new IdentityVerificationStatus("TEST_INTEGRATED", true));

    public Task<PhoneIdentityVerificationResult> VerifyPhoneAsync(string phone, string? verificationToken, CancellationToken cancellationToken) =>
        Task.FromResult(verificationToken == VerificationToken
            ? new PhoneIdentityVerificationResult("VERIFIED", true, phone)
            : new PhoneIdentityVerificationResult("REJECTED", false, null));
}

public sealed record TestCredential(string LoginId, string Password);
public sealed record TestCatalogIds(
    Guid ServiceId,
    Guid OtherServiceId,
    Guid AreaId,
    Guid OtherAreaId,
    Guid MajorId,
    IReadOnlyList<Guid> FieldIds);
