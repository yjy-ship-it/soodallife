using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics;
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
using SoodalLife.Api.Features.Subscriptions;
using SoodalLife.Api.Features.Interior;
using SoodalLife.Api.Features.Notifications;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Emergency;
using SoodalLife.Api.Features.SiteVisits;
using SoodalLife.Api.Features.PublicActivity;
using SoodalLife.Api.Features.LivingContent;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Features.Automation;
using SoodalLife.Api.Features.RelationshipBlocks;
using SoodalLife.Api.Features.Advertising;
using SoodalLife.Api.Features.Proposals;
using SoodalLife.Api.Infrastructure.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Serialization;
using SoodalLife.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Prelaunch.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SoodalLife");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
    if (OperatingSystem.IsWindows())
    {
        dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
    }
}

var allowedOrigins = OfficialCorsOrigins.Expand(
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []);
builder.Services.AddCors(options => options.AddPolicy("ProductionUi", policy =>
{
    if (allowedOrigins.Length == 0) return;
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));
builder.Services.Configure<PrivacyProtectionOptions>(builder.Configuration.GetSection(PrivacyProtectionOptions.SectionName));
builder.Services.AddSingleton<SoodalLife.Api.Infrastructure.Security.IPersonalDataProtector, DataProtectionPersonalDataProtector>();
builder.Services.AddSingleton<IPersonalDataSearchHasher, HmacPersonalDataSearchHasher>();
builder.Services.AddSingleton<PersonalDataReadMetrics>();
builder.Services.AddSingleton<IPersonalDataReader, EncryptedFirstPersonalDataReader>();
builder.Services.AddSingleton<IPrivacyContract, PrivacyContract>();
builder.Services.AddSingleton<PersonalDataProtectionInterceptor>();
builder.Services.AddSingleton<PersonalDataReadInterceptor>();
builder.Services.AddSingleton<LocalImageSafetyInterceptor>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<CustomerAccountService>();
builder.Services.AddScoped<CustomerDailyService>();
builder.Services.AddScoped<ICustomerWithdrawalReadinessService, CustomerWithdrawalReadinessService>();
builder.Services.AddScoped<CustomerWithdrawalService>();
builder.Services.AddScoped<UserRelationshipBlockService>();
builder.Services.AddScoped<IUserRelationshipBlockPolicy>(services => services.GetRequiredService<UserRelationshipBlockService>());
builder.Services.Configure<NiceIdentityVerificationOptions>(builder.Configuration.GetSection(NiceIdentityVerificationOptions.SectionName));
builder.Services.AddHttpClient<NiceIdentityVerificationAdapter>();
builder.Services.AddScoped<IIdentityVerificationAdapter>(services => services.GetRequiredService<NiceIdentityVerificationAdapter>());
builder.Services.AddSingleton<IPasswordResetDeliveryAdapter, NotIntegratedPasswordResetDeliveryAdapter>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminAnalyticsService>();
builder.Services.AddScoped<AdminSettlementOperationsService>();
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddScoped<AdminSystemService>();
builder.Services.AddScoped<AdminSecurityService>();
builder.Services.AddScoped<IAdminMfaVerifier>(services => services.GetRequiredService<AdminSecurityService>());
builder.Services.AddScoped<DataRetentionService>();
builder.Services.AddScoped<AnalyticsEventMiddleware>();
builder.Services.AddScoped<AdminDetailRoleMiddleware>();
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
builder.Services.Configure<TossWalletTopUpOptions>(builder.Configuration.GetSection(TossWalletTopUpOptions.SectionName));
builder.Services.AddHttpClient<TossWalletTopUpService>();
builder.Services.AddScoped<SoodalLife.Api.Features.Trust.ProviderTrustService>();
builder.Services.AddScoped<SoodalLife.Api.Features.Trust.TrustRecalculationRunner>();
builder.Services.AddScoped<AdminWalletService>();
builder.Services.AddScoped<IProviderExitReadinessService, ProviderExitReadinessService>();
builder.Services.AddScoped<ProviderExitService>();
builder.Services.AddHostedService<ProviderExitAutomationWorker>();
builder.Services.AddScoped<AdminRequestTransactionService>();
builder.Services.AddScoped<AdvertisingContentService>();
builder.Services.AddScoped<ProviderAdvertisingService>();
builder.Services.AddScoped<PublicProviderProfileService>();
builder.Services.AddScoped<ProviderAdvertisingRenewalService>();
builder.Services.Configure<ProposalOptions>(builder.Configuration.GetSection(ProposalOptions.SectionName));
builder.Services.AddScoped<ProposalService>();
builder.Services.AddScoped<AfterServiceDisputeService>();
  builder.Services.AddScoped<AdminTrustService>();
  builder.Services.AddScoped<TrustCalculationService>();
builder.Services.AddScoped<TrustReferenceDataInitializer>();
builder.Services.AddScoped<TrustReferenceDataService>();
builder.Services.AddScoped<SoodalLife.Api.Features.Reviews.ReviewService>();
builder.Services.AddScoped<SoodalLife.Api.Features.Reviews.ReviewConversationService>();
  builder.Services.AddScoped<AdminReviewService>();
  builder.Services.AddScoped<AdminCaseManagementService>();
builder.Services.AddScoped<ActiveUserCookieEvents>();
builder.Services.AddScoped<DevelopmentAccountInitializer>();
builder.Services.AddSingleton<CatalogWorkbookReader>();
builder.Services.AddScoped<CatalogReferenceDataImporter>();
builder.Services.AddScoped<CatalogQueryService>();
builder.Services.AddScoped<ServiceVisualSettingsService>();
builder.Services.AddScoped<PublicCatalogQueryService>();
builder.Services.AddScoped<CustomerServiceRequestService>();
builder.Services.AddScoped<SoodalLife.Api.Features.HelpRoom.HelpRoomService>();
builder.Services.AddScoped<SoodalLife.Api.Features.HelpRoom.SuggestionService>();
builder.Services.AddScoped<CustomerRequestAbusePolicy>();
builder.Services.AddSingleton<IFilePrivacyContract, FilePrivacyContract>();
builder.Services.AddScoped<ICrossDomainFilePublicationResolver, CrossDomainFilePublicationResolver>();
builder.Services.AddScoped<ServiceRequestFilePrivacyResolver>();
builder.Services.AddScoped<ProviderConfigurationService>();
builder.Services.AddScoped<ProviderOperationsHubService>();
builder.Services.AddSingleton<ProviderWorkInboxNotifier>();
builder.Services.AddScoped<ProviderRegistrationService>();
builder.Services.AddScoped<RequestMatchingService>();
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<ProviderDispatchWaveWorker>();
builder.Services.AddScoped<AdminMatchingOperationsService>();
builder.Services.AddScoped<ProviderTradingEligibilityService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<QuoteFeeReservationService>();
builder.Services.AddSingleton<CompletionPolicyEvaluator>();
builder.Services.AddSingleton<IPrivateFileStorage, DevelopmentPrivateFileStorage>();
builder.Services.AddScoped<WorkService>();
builder.Services.AddScoped<TransactionDirectPaymentService>();
builder.Services.AddScoped<TransactionAppointmentService>();
builder.Services.AddScoped<CustomerDisputeService>();
builder.Services.AddScoped<CustomerAfterServiceService>();
builder.Services.AddScoped<ProviderAftercareService>();
builder.Services.AddScoped<CustomerReportService>();
builder.Services.AddScoped<CareSubscriptionService>();
builder.Services.AddScoped<CustomerCareSubscriptionService>();
builder.Services.AddScoped<ProviderCareService>();
builder.Services.Configure<SubscriptionPaymentGatewayOptions>(builder.Configuration.GetSection(SubscriptionPaymentGatewayOptions.SectionName));
builder.Services.AddSingleton<ISubscriptionPaymentTokenProtector,SubscriptionPaymentTokenProtector>();
builder.Services.AddHttpClient<ISubscriptionPaymentGateway, TossSubscriptionPaymentGateway>(client => client.Timeout = TimeSpan.FromSeconds(65));
builder.Services.AddScoped<SubscriptionPaymentProcessor>();
builder.Services.AddScoped<SubscriptionTerminationService>();
builder.Services.AddSingleton<ISubscriptionVisitVerificationAdapter, NotIntegratedSubscriptionVisitVerificationAdapter>();
builder.Services.AddSingleton<ISubscriptionSettlementFeeCalculator, SubscriptionSettlementFeeCalculator>();
builder.Services.AddScoped<SubscriptionBillingService>();
builder.Services.AddScoped<SubscriptionLifecycleAutomationService>();
builder.Services.AddScoped<InteriorProjectService>();
builder.Services.AddScoped<InteriorContractWorkspaceService>();
builder.Services.AddScoped<InteriorContractExpiryService>();
builder.Services.AddScoped<CustomerInteriorService>();
builder.Services.AddScoped<ProviderInteriorService>();
builder.Services.AddScoped<IEmergencyAvailabilityResolver, EmergencyAvailabilityResolver>();
builder.Services.AddSingleton<IEmergencyPaymentInstructionProtector, EmergencyPaymentInstructionProtector>();
builder.Services.AddScoped<ProviderEmergencyAvailabilityService>();
builder.Services.AddScoped<EmergencyWorkflowService>();
builder.Services.AddScoped<SiteVisitService>();
builder.Services.AddScoped<PublicActivityFeedService>();
builder.Services.AddScoped<LivingContentService>();
builder.Services.AddScoped<NotificationManagementService>();
builder.Services.AddScoped<NotificationOperationsService>();
builder.Services.AddScoped<NotificationDeliveryProcessor>();
builder.Services.AddScoped<NotificationBroadcastService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IChatResourceAuthorizationResolver, ChatResourceAuthorizationResolver>();
builder.Services.Configure<AutomationOptions>(builder.Configuration.GetSection(AutomationOptions.SectionName));
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddScoped<PrivacyBackfillService>();
builder.Services.AddScoped<ScheduledJobRunner>();
builder.Services.AddScoped<IAutomationJob, CustomerEngagementReminderJob>();
builder.Services.AddScoped<IAutomationJob, OutboxAutomationJob>();
builder.Services.AddScoped<IAutomationJob, NotificationDeliveryJob>();
builder.Services.AddScoped<IAutomationJob, NotificationBroadcastQueueJob>();
builder.Services.AddScoped<IAutomationJob, PasswordResetExpiryJob>();
builder.Services.AddScoped<IAutomationJob, SubscriptionVisitGeneratorJob>();
builder.Services.AddScoped<IAutomationJob, SubscriptionRecurringBillingJob>();
builder.Services.AddScoped<IAutomationJob, SubscriptionLifecycleJob>();
builder.Services.AddScoped<IAutomationJob, EmergencyExpiryJob>();
builder.Services.AddScoped<IAutomationJob, QuoteFeeReservationExpiryJob>();
builder.Services.AddScoped<IAutomationJob, ServiceRequestExpiryJob>();
builder.Services.AddScoped<IAutomationJob, InteriorContractExpiryJob>();
builder.Services.AddScoped<IAutomationJob, ProviderAdvertisingRenewalJob>();
builder.Services.AddScoped<IAutomationJob, ProposalLifecycleJob>();
builder.Services.AddScoped<IAutomationJob,DataRetentionAssessmentJob>();
builder.Services.AddScoped<IAutomationJob>(_ => new NotConfiguredAutomationJob("FILE_RETENTION_SCAN"));
builder.Services.AddScoped<IAutomationJob>(_ => new NotConfiguredAutomationJob("VERIFICATION_EXPIRY_SCAN"));
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<OperationsAutomationWorker>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationChannelSender,WebNotificationChannelSender>();
builder.Services.Configure<NhnNotificationOptions>(builder.Configuration.GetSection(NhnNotificationOptions.SectionName));
builder.Services.AddHttpClient<NhnNotificationChannelSender>(client=>client.Timeout=TimeSpan.FromSeconds(30));
builder.Services.AddTransient<INotificationChannelSender>(services=>services.GetRequiredService<NhnNotificationChannelSender>());
builder.Services.AddSingleton<INotificationChannelSender,UnavailableExternalNotificationChannelSender>();

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

    builder.Services.AddDbContext<SoodalLifeDbContext>((services, options) =>
        options.UseSqlServer(connectionString).AddInterceptors(services.GetRequiredService<PersonalDataProtectionInterceptor>(), services.GetRequiredService<PersonalDataReadInterceptor>(), services.GetRequiredService<LocalImageSafetyInterceptor>()));
}

var app = builder.Build();

app.UseExceptionHandler(exceptionApplication =>
{
    exceptionApplication.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var traceId = context.TraceIdentifier;
        app.Logger.LogError(
            exception,
            "Unhandled API exception. TraceId={TraceId} Method={Method} Path={Path}",
            traceId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            var diagnosticsDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data", "diagnostics");
            Directory.CreateDirectory(diagnosticsDirectory);
            var safeTraceId = string.Concat(traceId.Select(character =>
                char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'));
            var diagnosticText = $"""
                TraceId: {traceId}
                OccurredAtUtc: {DateTime.UtcNow:O}
                Method: {context.Request.Method}
                Path: {context.Request.Path}
                Exception:
                {exception}
                """;
            await File.WriteAllTextAsync(
                Path.Combine(diagnosticsDirectory, $"{safeTraceId}.log"),
                diagnosticText);

            var retentionThreshold = DateTime.UtcNow.AddDays(-14);
            foreach (var diagnosticFile in Directory.EnumerateFiles(diagnosticsDirectory, "*.log"))
            {
                if (File.GetLastWriteTimeUtc(diagnosticFile) < retentionThreshold)
                {
                    File.Delete(diagnosticFile);
                }
            }
        }
        catch (Exception diagnosticWriteException)
        {
            app.Logger.LogWarning(
                diagnosticWriteException,
                "Failed to persist API diagnostic. TraceId={TraceId}",
                traceId);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin) &&
            allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers.Append("Vary", "Origin");
        }

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            "UNHANDLED_API_ERROR",
            "서버 처리 중 오류가 발생했습니다. 오류 확인번호를 고객센터에 알려 주세요.",
            null,
            traceId));
    });
});

if (app.Environment.IsDevelopment() && !args.Contains("--privacy-backfill", StringComparer.OrdinalIgnoreCase))
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

if (args.Contains("--privacy-backfill", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Privacy backfill is restricted to the Development environment.");
    await using var scope = app.Services.CreateAsyncScope();
    var service = scope.ServiceProvider.GetRequiredService<PrivacyBackfillService>();
    if (!service.IsReady) throw new InvalidOperationException("Privacy backfill requires the existing secure Data Protection key ring and HMAC key.");
    _ = await service.ValidatePreconditionsAsync(CancellationToken.None);
    PrivacyBackfillResult result;
    do { result = await service.BackfillBatchAsync(CancellationToken.None); } while (result.UpdatedCount > 0);
    return;
}

app.UseHttpsRedirection();

app.UseCors("ProductionUi");
app.UseMiddleware<SafeImageUploadMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AdminDetailRoleMiddleware>();
app.UseMiddleware<AnalyticsEventMiddleware>();
app.Use(async (context,next)=>
{
    if(!context.Request.Path.StartsWithSegments("/api")){await next();return;}
    var stopwatch=Stopwatch.StartNew();
    context.Response.OnStarting(()=>
    {
        context.Response.Headers["Server-Timing"]=$"app;dur={stopwatch.Elapsed.TotalMilliseconds:F1}";
        return Task.CompletedTask;
    });
    try{await next();}
    finally
    {
        stopwatch.Stop();
        var endpoint=context.GetEndpoint()?.DisplayName??"unmatched-api-endpoint";
        app.Logger.LogInformation("API request completed. Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs}",context.Request.Method,endpoint,context.Response.StatusCode,stopwatch.Elapsed.TotalMilliseconds);
    }
});

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ProviderWorkInboxHub>("/hubs/provider-work-inbox");

app.Run();

public partial class Program;
