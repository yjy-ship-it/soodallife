using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.CatalogImport;
using SoodalLife.Api.Features.Wallet;
using SoodalLife.Api.Features.Catalog;
using SoodalLife.Api.Features.ServiceRequests;
using SoodalLife.Api.Features.Providers;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Quotes;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
builder.Services.AddOpenApi();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddScoped<AdminServiceCategoryService>();
builder.Services.AddScoped<AdminRequestFieldService>();
builder.Services.AddScoped<AdminPricePolicyService>();
builder.Services.AddScoped<AdminFeePolicyService>();
builder.Services.AddScoped<AdminProviderRequirementService>();
builder.Services.AddScoped<AdminProviderRequirementStandardService>();
builder.Services.AddScoped<AdminCustomerService>();
builder.Services.AddScoped<AdminProviderService>();
builder.Services.AddScoped<AdminProviderServiceApprovalService>();
builder.Services.AddScoped<ProviderWalletService>();
builder.Services.AddScoped<AdminWalletService>();
builder.Services.AddScoped<AdminRequestTransactionService>();
builder.Services.AddScoped<AdvertisingContentService>();
builder.Services.AddScoped<AfterServiceDisputeService>();
  builder.Services.AddScoped<AdminTrustService>();
  builder.Services.AddScoped<TrustCalculationService>();
builder.Services.AddScoped<SoodalLife.Api.Features.Reviews.ReviewService>();
  builder.Services.AddScoped<AdminReviewService>();
  builder.Services.AddScoped<AdminCaseManagementService>();
builder.Services.AddScoped<ActiveUserCookieEvents>();
builder.Services.AddScoped<DevelopmentAccountInitializer>();
builder.Services.AddSingleton<CatalogWorkbookReader>();
builder.Services.AddScoped<CatalogReferenceDataImporter>();
builder.Services.AddScoped<CatalogQueryService>();
builder.Services.AddScoped<CustomerServiceRequestService>();
builder.Services.AddScoped<ProviderConfigurationService>();
builder.Services.AddScoped<RequestMatchingService>();
builder.Services.AddScoped<ProviderTradingEligibilityService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddSingleton<CompletionPolicyEvaluator>();
builder.Services.AddSingleton<IPrivateFileStorage, DevelopmentPrivateFileStorage>();
builder.Services.AddScoped<WorkService>();

builder.Services
    .AddAuthentication(AuthenticationConstants.Scheme)
    .AddCookie(AuthenticationConstants.Scheme, options =>
    {
        options.Cookie.Name = AuthenticationConstants.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(ActiveUserCookieEvents);
    });
builder.Services.AddAuthorization();

if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("SoodalLife");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'SoodalLife' is not configured. Configure it through User Secrets or an environment variable.");
    }

    builder.Services.AddDbContext<SoodalLifeDbContext>(options =>
        options.UseSqlServer(connectionString));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentAccountInitializer>().InitializeAsync();
}

var importArgumentIndex = Array.IndexOf(args, "--import-catalog");
if (importArgumentIndex >= 0)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Catalog import is restricted to the Development environment.");
    }

    if (importArgumentIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[importArgumentIndex + 1]))
    {
        throw new InvalidOperationException("--import-catalog requires an Excel workbook path.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var importer = scope.ServiceProvider.GetRequiredService<CatalogReferenceDataImporter>();
    await importer.ImportAsync(args[importArgumentIndex + 1], includeDevelopmentAreas: true);
    return;
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
