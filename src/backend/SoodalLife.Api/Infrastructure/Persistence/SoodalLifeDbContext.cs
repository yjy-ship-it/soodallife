using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence;

public sealed class SoodalLifeDbContext(DbContextOptions<SoodalLifeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<LegalDocumentVersion> LegalDocumentVersions => Set<LegalDocumentVersion>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<CustomerWithdrawalRequest> CustomerWithdrawalRequests => Set<CustomerWithdrawalRequest>();
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
    public DbSet<StoredFileDerivative> FileDerivatives => Set<StoredFileDerivative>();
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
    public DbSet<ServiceRequestFile> ServiceRequestFiles => Set<ServiceRequestFile>();
    public DbSet<DispatchCandidate> DispatchCandidates => Set<DispatchCandidate>();
    public DbSet<RequestDispatch> RequestDispatches => Set<RequestDispatch>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteRevision> QuoteRevisions => Set<QuoteRevision>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    public DbSet<TransactionAppointment> TransactionAppointments => Set<TransactionAppointment>();
    public DbSet<TransactionAppointmentChangeRequest> TransactionAppointmentChangeRequests => Set<TransactionAppointmentChangeRequest>();
    public DbSet<TransactionAppointmentEvent> TransactionAppointmentEvents => Set<TransactionAppointmentEvent>();
    public DbSet<TransactionCancellationRequest> TransactionCancellationRequests => Set<TransactionCancellationRequest>();
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
    public DbSet<ProviderTrustCalculationResult> ProviderTrustCalculationResults => Set<ProviderTrustCalculationResult>();
    public DbSet<ProviderTrustScoreComponent> ProviderTrustScoreComponents => Set<ProviderTrustScoreComponent>();
    public DbSet<ReviewRatingItem> ReviewRatingItems => Set<ReviewRatingItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewRating> ReviewRatings => Set<ReviewRating>();
    public DbSet<ReviewFile> ReviewFiles => Set<ReviewFile>();
    public DbSet<ReviewProviderReply> ReviewProviderReplies => Set<ReviewProviderReply>();
    public DbSet<ReportType> ReportTypes => Set<ReportType>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportEvidence> ReportEvidence => Set<ReportEvidence>();
    public DbSet<ReportAction> ReportActions => Set<ReportAction>();
    public DbSet<SanctionType> SanctionTypes => Set<SanctionType>();
    public DbSet<Sanction> Sanctions => Set<Sanction>();
    public DbSet<SanctionSource> SanctionSources => Set<SanctionSource>();
    public DbSet<SanctionEvent> SanctionEvents => Set<SanctionEvent>();
    public DbSet<SanctionAppeal> SanctionAppeals => Set<SanctionAppeal>();
    public DbSet<SanctionAppealEvidence> SanctionAppealEvidence => Set<SanctionAppealEvidence>();
    public DbSet<SanctionAppealAction> SanctionAppealActions => Set<SanctionAppealAction>();
    public DbSet<DisputeLiabilityType> DisputeLiabilityTypes => Set<DisputeLiabilityType>();
    public DbSet<CareProduct> CareProducts => Set<CareProduct>();
    public DbSet<SubscriptionRequest> SubscriptionRequests => Set<SubscriptionRequest>();
    public DbSet<SubscriptionRecurrenceRule> SubscriptionRecurrenceRules => Set<SubscriptionRecurrenceRule>();
    public DbSet<SubscriptionApplication> SubscriptionApplications => Set<SubscriptionApplication>();
    public DbSet<SubscriptionContract> SubscriptionContracts => Set<SubscriptionContract>();
    public DbSet<SubscriptionVisitSchedule> SubscriptionVisitSchedules => Set<SubscriptionVisitSchedule>();
    public DbSet<SubscriptionVisitFile> SubscriptionVisitFiles => Set<SubscriptionVisitFile>();
    public DbSet<SubscriptionScheduleChange> SubscriptionScheduleChanges => Set<SubscriptionScheduleChange>();
    public DbSet<SubscriptionEvent> SubscriptionEvents => Set<SubscriptionEvent>();
    public DbSet<SubscriptionPaymentMethod> SubscriptionPaymentMethods => Set<SubscriptionPaymentMethod>();
    public DbSet<SubscriptionPaymentRequest> SubscriptionPaymentRequests => Set<SubscriptionPaymentRequest>();
    public DbSet<SubscriptionPaymentLedgerEntry> SubscriptionPaymentLedger => Set<SubscriptionPaymentLedgerEntry>();
    public DbSet<SubscriptionSettlementItem> SubscriptionSettlementItems => Set<SubscriptionSettlementItem>();
    public DbSet<MonthlySettlement> MonthlySettlements => Set<MonthlySettlement>();
    public DbSet<SubscriptionPayout> SubscriptionPayouts => Set<SubscriptionPayout>();
    public DbSet<SubscriptionPayoutEvent> SubscriptionPayoutEvents => Set<SubscriptionPayoutEvent>();
    public DbSet<SubscriptionRefundAdjustment> SubscriptionRefundAdjustments => Set<SubscriptionRefundAdjustment>();
    public DbSet<InteriorProject> InteriorProjects => Set<InteriorProject>();
    public DbSet<InteriorSiteVisit> InteriorSiteVisits => Set<InteriorSiteVisit>();
    public DbSet<InteriorSiteVisitMeasurement> InteriorSiteVisitMeasurements => Set<InteriorSiteVisitMeasurement>();
    public DbSet<InteriorSiteVisitFile> InteriorSiteVisitFiles => Set<InteriorSiteVisitFile>();
    public DbSet<InteriorDesignVersion> InteriorDesignVersions => Set<InteriorDesignVersion>();
    public DbSet<InteriorDesignFile> InteriorDesignFiles => Set<InteriorDesignFile>();
    public DbSet<InteriorContract> InteriorContracts => Set<InteriorContract>();
    public DbSet<InteriorContractVersion> InteriorContractVersions => Set<InteriorContractVersion>();
    public DbSet<InteriorPaymentPlan> InteriorPaymentPlans => Set<InteriorPaymentPlan>();
    public DbSet<InteriorPaymentConfirmation> InteriorPaymentConfirmations => Set<InteriorPaymentConfirmation>();
    public DbSet<InteriorWorkStage> InteriorWorkStages => Set<InteriorWorkStage>();
    public DbSet<InteriorWorkUpdate> InteriorWorkUpdates => Set<InteriorWorkUpdate>();
    public DbSet<InteriorWorkUpdateFile> InteriorWorkUpdateFiles => Set<InteriorWorkUpdateFile>();
    public DbSet<InteriorStageInspection> InteriorStageInspections => Set<InteriorStageInspection>();
    public DbSet<InteriorContractChange> InteriorContractChanges => Set<InteriorContractChange>();
    public DbSet<InteriorDefect> InteriorDefects => Set<InteriorDefect>();
    public DbSet<InteriorProjectEvent> InteriorProjectEvents => Set<InteriorProjectEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoodalLifeDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditLogsAreAppendOnly();
        EnsureWalletLedgerIsAppendOnly();
        EnsureTrustScoreEventsAreAppendOnly();
        EnsureSanctionEventsAreAppendOnly();
        EnsureSubscriptionEventsAreAppendOnly();
        EnsureSubscriptionAccountingLedgersAreAppendOnly();
        EnsureInteriorProjectEventsAreAppendOnly();
        EnsureNotificationHistoryIsAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureAuditLogsAreAppendOnly();
        EnsureWalletLedgerIsAppendOnly();
        EnsureTrustScoreEventsAreAppendOnly();
        EnsureSanctionEventsAreAppendOnly();
        EnsureSubscriptionEventsAreAppendOnly();
        EnsureSubscriptionAccountingLedgersAreAppendOnly();
        EnsureInteriorProjectEventsAreAppendOnly();
        EnsureNotificationHistoryIsAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureAuditLogsAreAppendOnly()
    {
        if (ChangeTracker.Entries<AuditLog>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("감사로그는 수정하거나 삭제할 수 없습니다.");
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

    private void EnsureSanctionEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<SanctionEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("제재 변경이력은 수정하거나 삭제할 수 없습니다. 정정이 필요하면 별도의 변경이력을 추가해 주세요.");
    }

    private void EnsureSubscriptionEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<SubscriptionEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("구독 변경이력은 수정하거나 삭제할 수 없습니다. 정정이 필요하면 별도의 이벤트를 추가해 주세요.");
    }

    private void EnsureSubscriptionAccountingLedgersAreAppendOnly()
    {
        if (ChangeTracker.Entries<SubscriptionPaymentLedgerEntry>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("구독 결제 원장은 수정하거나 삭제할 수 없습니다. 반대 방향의 원장 항목을 추가해 주세요.");
        if (ChangeTracker.Entries<SubscriptionPayoutEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("구독 지급 이력은 수정하거나 삭제할 수 없습니다. 새로운 이벤트를 추가해 주세요.");
    }

    private void EnsureInteriorProjectEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<InteriorProjectEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("인테리어 프로젝트 이벤트는 수정하거나 삭제할 수 없습니다. 정정이 필요하면 새 이벤트를 추가해 주세요.");
    }

    private void EnsureNotificationHistoryIsAppendOnly()
    {
        if (ChangeTracker.Entries<NotificationEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("알림 이벤트는 수정하거나 삭제할 수 없습니다. 정정이 필요하면 새 이벤트를 추가해 주세요.");
        if (ChangeTracker.Entries<NotificationDeliveryAttempt>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("알림 발송 시도 이력은 수정하거나 삭제할 수 없습니다. 재시도는 새 이력으로 기록해 주세요.");
    }
}
