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
    public DbSet<ProviderRequirementType> ProviderRequirementTypes => Set<ProviderRequirementType>();
    public DbSet<ProviderRequirementDefinition> ProviderRequirementDefinitions => Set<ProviderRequirementDefinition>();
    public DbSet<ProviderDocumentType> ProviderDocumentTypes => Set<ProviderDocumentType>();
    public DbSet<CategoryProviderRequirementAssignment> CategoryProviderRequirementAssignments => Set<CategoryProviderRequirementAssignment>();
    public DbSet<CategoryProviderRequirementEvidenceType> CategoryProviderRequirementEvidenceTypes => Set<CategoryProviderRequirementEvidenceType>();
    public DbSet<AdministrativeArea> AdministrativeAreas => Set<AdministrativeArea>();
    public DbSet<ProviderServiceCategory> ProviderServiceCategories => Set<ProviderServiceCategory>();
    public DbSet<ProviderServiceApproval> ProviderServiceApprovals => Set<ProviderServiceApproval>();
    public DbSet<ProviderServiceApprovalEvent> ProviderServiceApprovalEvents => Set<ProviderServiceApprovalEvent>();
    public DbSet<ProviderServiceArea> ProviderServiceAreas => Set<ProviderServiceArea>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();
    public DbSet<ProviderServiceRequirementVerification> ProviderServiceRequirementVerifications => Set<ProviderServiceRequirementVerification>();
    public DbSet<ProviderWallet> ProviderWallets => Set<ProviderWallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<WalletChargeRequest> WalletChargeRequests => Set<WalletChargeRequest>();
    public DbSet<FeeCharge> FeeCharges => Set<FeeCharge>();
    public DbSet<FeeRestore> FeeRestores => Set<FeeRestore>();
    public DbSet<WalletRefundRequest> WalletRefundRequests => Set<WalletRefundRequest>();
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
    public DbSet<DisputeCase> DisputeCases => Set<DisputeCase>();
    public DbSet<DisputeEvidence> DisputeEvidence => Set<DisputeEvidence>();
    public DbSet<DisputeAction> DisputeActions => Set<DisputeAction>();
    public DbSet<DisputeResolution> DisputeResolutions => Set<DisputeResolution>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<AdvertisingPlacement> AdvertisingPlacements => Set<AdvertisingPlacement>();
    public DbSet<AdvertisingCampaign> AdvertisingCampaigns => Set<AdvertisingCampaign>();
    public DbSet<AdvertisingCampaignPlacement> AdvertisingCampaignPlacements => Set<AdvertisingCampaignPlacement>();
    public DbSet<AdvertisingCampaignCategory> AdvertisingCampaignCategories => Set<AdvertisingCampaignCategory>();
    public DbSet<AdvertisingCampaignArea> AdvertisingCampaignAreas => Set<AdvertisingCampaignArea>();
    public DbSet<AdvertisingCreative> AdvertisingCreatives => Set<AdvertisingCreative>();
    public DbSet<AdvertisingEvent> AdvertisingEvents => Set<AdvertisingEvent>();
    public DbSet<ManagedContent> ManagedContents => Set<ManagedContent>();
    public DbSet<ManagedContentVersion> ManagedContentVersions => Set<ManagedContentVersion>();
    public DbSet<ManagedContentCategory> ManagedContentCategories => Set<ManagedContentCategory>();
    public DbSet<ManagedContentArea> ManagedContentAreas => Set<ManagedContentArea>();
    public DbSet<TrustPolicy> TrustPolicies => Set<TrustPolicy>();
    public DbSet<ProviderTrustScoreCurrent> ProviderTrustScoreCurrent => Set<ProviderTrustScoreCurrent>();
    public DbSet<TrustScoreEvent> TrustScoreEvents => Set<TrustScoreEvent>();
    public DbSet<ReviewRatingItem> ReviewRatingItems => Set<ReviewRatingItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewRating> ReviewRatings => Set<ReviewRating>();
    public DbSet<ReviewFile> ReviewFiles => Set<ReviewFile>();
    public DbSet<ReviewProviderReply> ReviewProviderReplies => Set<ReviewProviderReply>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoodalLifeDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureWalletLedgerIsAppendOnly();
        EnsureTrustScoreEventsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureWalletLedgerIsAppendOnly();
        EnsureTrustScoreEventsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureWalletLedgerIsAppendOnly()
    {
        if (ChangeTracker.Entries<WalletLedgerEntry>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Wallet 원장은 수정하거나 삭제할 수 없습니다. 반대 방향의 새 원장 항목을 생성해 주세요.");
    }

    private void EnsureTrustScoreEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<TrustScoreEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("신뢰도 변경이력은 수정하거나 삭제할 수 없습니다. 정정이 필요하면 별도의 변경이력을 추가해 주세요.");
    }
}
