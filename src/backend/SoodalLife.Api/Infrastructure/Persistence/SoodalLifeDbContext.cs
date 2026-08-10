using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence;

public sealed class SoodalLifeDbContext(DbContextOptions<SoodalLifeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
    public DbSet<ProviderApprovalEvent> ProviderApprovalEvents => Set<ProviderApprovalEvent>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<CategoryPolicy> CategoryPolicies => Set<CategoryPolicy>();
    public DbSet<CategoryPricePolicy> CategoryPricePolicies => Set<CategoryPricePolicy>();
    public DbSet<CategoryFeePolicy> CategoryFeePolicies => Set<CategoryFeePolicy>();
    public DbSet<CategoryOperationPolicy> CategoryOperationPolicies => Set<CategoryOperationPolicy>();
    public DbSet<CategoryPricePolicyOption> CategoryPricePolicyOptions => Set<CategoryPricePolicyOption>();
    public DbSet<CategoryPricePolicySurcharge> CategoryPricePolicySurcharges => Set<CategoryPricePolicySurcharge>();
    public DbSet<CompletionPhotoRole> CompletionPhotoRoles => Set<CompletionPhotoRole>();
    public DbSet<CategoryCompletionPhotoRequirement> CategoryCompletionPhotoRequirements => Set<CategoryCompletionPhotoRequirement>();
    public DbSet<CategoryFieldDefinition> CategoryFieldDefinitions => Set<CategoryFieldDefinition>();
    public DbSet<CategoryFieldAssignment> CategoryFieldAssignments => Set<CategoryFieldAssignment>();
    public DbSet<CategoryFieldOption> CategoryFieldOptions => Set<CategoryFieldOption>();
    public DbSet<FeePolicy> FeePolicies => Set<FeePolicy>();
    public DbSet<QualificationPolicy> QualificationPolicies => Set<QualificationPolicy>();
    public DbSet<AdministrativeArea> AdministrativeAreas => Set<AdministrativeArea>();
    public DbSet<ProviderServiceCategory> ProviderServiceCategories => Set<ProviderServiceCategory>();
    public DbSet<ProviderServiceArea> ProviderServiceAreas => Set<ProviderServiceArea>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestAnswer> RequestAnswers => Set<RequestAnswer>();
    public DbSet<RequestAnswerFile> RequestAnswerFiles => Set<RequestAnswerFile>();
    public DbSet<DispatchCandidate> DispatchCandidates => Set<DispatchCandidate>();
    public DbSet<RequestDispatch> RequestDispatches => Set<RequestDispatch>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteRevision> QuoteRevisions => Set<QuoteRevision>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    public DbSet<WorkCompletion> WorkCompletions => Set<WorkCompletion>();
    public DbSet<WorkCompletionRevision> WorkCompletionRevisions => Set<WorkCompletionRevision>();
    public DbSet<CompletionEvidenceFile> CompletionEvidenceFiles => Set<CompletionEvidenceFile>();
    public DbSet<CustomerConfirmation> CustomerConfirmations => Set<CustomerConfirmation>();
    public DbSet<ServiceHistoryEntry> ServiceHistoryEntries => Set<ServiceHistoryEntry>();
    public DbSet<ServiceHistoryItem> ServiceHistoryItems => Set<ServiceHistoryItem>();
    public DbSet<ServiceAsset> ServiceAssets => Set<ServiceAsset>();
    public DbSet<TransactionAssetLink> TransactionAssetLinks => Set<TransactionAssetLink>();
    public DbSet<AfterServiceCase> AfterServiceCases => Set<AfterServiceCase>();
    public DbSet<AfterServiceAction> AfterServiceActions => Set<AfterServiceAction>();
    public DbSet<AfterServiceFile> AfterServiceFiles => Set<AfterServiceFile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoodalLifeDbContext).Assembly);
    }
}
