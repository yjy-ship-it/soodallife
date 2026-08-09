using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AuthenticationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"authentication-tests-{Guid.NewGuid():N}";

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

    public TestCatalogIds Catalog { get; private set; } = new(Guid.Empty, Guid.Empty, Guid.Empty, []);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SoodalLife"] = "Server=(local);Database=authentication-tests;Trusted_Connection=True;",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SoodalLifeDbContext>>();
            services.RemoveAll<SoodalLifeDbContext>();
            services.AddDbContext<SoodalLifeDbContext>(options => options.UseInMemoryDatabase(_databaseName));
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
            dbContext.SaveChanges();
        }

        var customerRole = dbContext.Roles.Single(role => role.Code == RoleCodes.Customer);
        var otherCustomer = new User
        {
            LoginId = OtherCustomerCredential.LoginId,
            NormalizedLoginId = OtherCustomerCredential.LoginId.ToUpperInvariant(),
            StatusCode = "ACTIVE",
        };
        otherCustomer.PasswordHash = passwordHasher.HashPassword(otherCustomer, OtherCustomerCredential.Password);
        dbContext.Users.Add(otherCustomer);
        dbContext.SaveChanges();
        dbContext.UserRoles.Add(new UserRole { UserId = otherCustomer.Id, RoleId = customerRole.Id });
        dbContext.CustomerProfiles.Add(new CustomerProfile { UserId = otherCustomer.Id, DisplayName = "Other Test Customer" });
        dbContext.SaveChanges();

        SeedRequestCatalog(dbContext);
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
        var fee = new FeePolicy
        {
            Code = "TEST-FEE",
            PolicyKindCode = "QUOTE",
            TransactionTypeCode = "ONE_TIME",
            AppliesToText = "테스트",
            ChargeTimingText = "테스트",
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
        dbContext.AddRange(service, fee, area);
        dbContext.SaveChanges();
        dbContext.CategoryPolicies.Add(new CategoryPolicy
        {
            CategoryId = service.Id,
            PolicyVersion = "test-v1",
            TransactionTypeCode = "ONE_TIME",
            RequestMethodText = "테스트",
            OnsiteRequirementText = "필수",
            SubscriptionOptionText = "선택",
            StandardWorkUnitText = "1회",
            CurrencyCode = "KRW",
            PriceMethodText = "견적형",
            VatDisplayRuleText = "표시",
            MaxQuoteCount = 5,
            QuoteValidityMinutes = 120,
            FeePolicyId = fee.Id,
            FeeChargeTimingText = "테스트",
            FeeRestoreConditionText = "테스트",
            MatchingAreaRuleText = "시군구",
            NotificationTargetRuleText = "테스트",
            ProviderResponseDeadlineMinutes = 30,
            RequestFieldSummaryText = "테스트",
            RequiredQualificationSummaryText = "테스트",
            InsuranceRequirementText = "테스트",
            SafetyGradeCode = "NORMAL",
            CompletionEvidenceRuleText = "테스트",
            TrustScoreDisplayText = "테스트",
            DefaultSortCode = "CREDIT_DESC",
            ServiceAreaLevelCode = "SIGUNGU",
            ReferenceUrl = "https://example.test",
            AdminNote = "테스트",
            EffectiveFrom = new DateOnly(2026, 1, 1),
        });

        var fields = new[]
        {
            new CategoryFieldDefinition
            {
                SourceFieldId = "TEST-FIELD-1", OwnerMiddleCategoryId = middle.Id, FieldKey = "request_detail",
                Label = "요청 내용", FieldTypeCode = "LONG_TEXT", IsRequired = true,
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
            IsActive = true,
        }));
        dbContext.SaveChanges();
        Catalog = new TestCatalogIds(service.PublicId, area.PublicId, major.PublicId, fields.Select(field => field.PublicId).ToArray());
    }
}

public sealed record TestCredential(string LoginId, string Password);
public sealed record TestCatalogIds(Guid ServiceId, Guid AreaId, Guid MajorId, IReadOnlyList<Guid> FieldIds);
