namespace SoodalLife.Api.Domain.Entities;

public sealed class User
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string LoginId { get; set; } = string.Empty;
    public string NormalizedLoginId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Role
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class UserRole
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime GrantedAt { get; set; }
    public long? GrantedByUserId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long? RevokedByUserId { get; set; }
}

public sealed class CustomerProfile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderProfile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessRegistrationNo { get; set; }
    public string ApprovalStatusCode { get; set; } = "PENDING";
    public string ActivityStatusCode { get; set; } = "INACTIVE";
    public decimal? TrustScore { get; set; }
    public DateTime? ApprovalDecidedAt { get; set; }
    public long? ApprovalDecidedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderApprovalEvent
{
    public long Id { get; set; }
    public long ProviderProfileId { get; set; }
    public string? FromStatusCode { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime DecidedAt { get; set; }
    public long DecidedByUserId { get; set; }
    public Guid? CorrelationId { get; set; }
}

public sealed class ServiceCategory
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? ParentId { get; set; }
    public string LevelCode { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string? SourceRecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "ACTIVE";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CategoryId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public string TransactionTypeCode { get; set; } = string.Empty;
    public string RequestMethodText { get; set; } = string.Empty;
    public string OnsiteRequirementText { get; set; } = string.Empty;
    public bool IsEmergencyAllowed { get; set; }
    public string SubscriptionOptionText { get; set; } = string.Empty;
    public string StandardWorkUnitText { get; set; } = string.Empty;
    public decimal BasePriceAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string PriceMethodText { get; set; } = string.Empty;
    public string VatDisplayRuleText { get; set; } = string.Empty;
    public decimal MinimumBudgetAmount { get; set; }
    public short MaxQuoteCount { get; set; }
    public int QuoteValidityMinutes { get; set; }
    public long FeePolicyId { get; set; }
    public decimal EstimatedQuoteFeeAmount { get; set; }
    public string FeeChargeTimingText { get; set; } = string.Empty;
    public string FeeRestoreConditionText { get; set; } = string.Empty;
    public string MatchingAreaRuleText { get; set; } = string.Empty;
    public string NotificationTargetRuleText { get; set; } = string.Empty;
    public int ProviderResponseDeadlineMinutes { get; set; }
    public string RequestFieldSummaryText { get; set; } = string.Empty;
    public short RequiredCompletionPhotoCount { get; set; }
    public string RequiredQualificationSummaryText { get; set; } = string.Empty;
    public string InsuranceRequirementText { get; set; } = string.Empty;
    public string SafetyGradeCode { get; set; } = string.Empty;
    public string CompletionEvidenceRuleText { get; set; } = string.Empty;
    public short DefaultWarrantyDays { get; set; }
    public string TrustScoreDisplayText { get; set; } = string.Empty;
    public string DefaultSortCode { get; set; } = string.Empty;
    public string ServiceAreaLevelCode { get; set; } = "SIGUNGU";
    public string ReferenceUrl { get; set; } = string.Empty;
    public string AdminNote { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryPricePolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CategoryId { get; set; }
    public long? LegacyCategoryPolicyId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public string LegacyPriceMethodText { get; set; } = string.Empty;
    public string? PriceTypeCode { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal? MinimumBudgetAmount { get; set; }
    public decimal? RecommendedMinAmount { get; set; }
    public decimal? RecommendedMaxAmount { get; set; }
    public string? UnitText { get; set; }
    public decimal? UnitPriceAmount { get; set; }
    public decimal? MinimumChargeAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string LegacyVatDisplayRuleText { get; set; } = string.Empty;
    public string? VatPolicyCode { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryFeePolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CategoryId { get; set; }
    public long? LegacyCategoryPolicyId { get; set; }
    public long? SourceFeePolicyId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public string PolicyKindCode { get; set; } = string.Empty;
    public string TransactionTypeCode { get; set; } = string.Empty;
    public string? CalculationMethodText { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? MinBaseAmount { get; set; }
    public decimal? MaxBaseAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal? MonthlyAmount { get; set; }
    public decimal? PerVisitAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string ChargeTimingText { get; set; } = string.Empty;
    public string? RestoreRuleText { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryOperationPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CategoryId { get; set; }
    public long? LegacyCategoryPolicyId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public string RequestMethodText { get; set; } = string.Empty;
    public string OnsiteRequirementText { get; set; } = string.Empty;
    public bool IsEmergencyAllowed { get; set; }
    public string SubscriptionOptionText { get; set; } = string.Empty;
    public short MaxQuoteCount { get; set; }
    public int QuoteValidityMinutes { get; set; }
    public string MatchingAreaRuleText { get; set; } = string.Empty;
    public string NotificationTargetRuleText { get; set; } = string.Empty;
    public int ProviderResponseDeadlineMinutes { get; set; }
    public string RequestFieldSummaryText { get; set; } = string.Empty;
    public short RequiredCompletionPhotoCount { get; set; }
    public string RequiredQualificationSummaryText { get; set; } = string.Empty;
    public string InsuranceRequirementText { get; set; } = string.Empty;
    public string SafetyGradeCode { get; set; } = string.Empty;
    public string CompletionEvidenceRuleText { get; set; } = string.Empty;
    public short DefaultWarrantyDays { get; set; }
    public string TrustScoreDisplayText { get; set; } = string.Empty;
    public string DefaultSortCode { get; set; } = string.Empty;
    public string ServiceAreaLevelCode { get; set; } = string.Empty;
    public string ReferenceUrl { get; set; } = string.Empty;
    public string AdminNote { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryPricePolicyOption
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PricePolicyId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public decimal AdditionalAmount { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryPricePolicySurcharge
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PricePolicyId { get; set; }
    public string SurchargeName { get; set; } = string.Empty;
    public string CalculationTypeCode { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public decimal? Rate { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CompletionPhotoRole
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryCompletionPhotoRequirement
{
    public long Id { get; set; }
    public long CategoryPolicyId { get; set; }
    public long PhotoRoleId { get; set; }
    public short MinimumCount { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryFieldDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string SourceFieldId { get; set; } = string.Empty;
    public long OwnerMiddleCategoryId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldTypeCode { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? OptionsOrUnitText { get; set; }
    public string? UnitText { get; set; }
    public string ProviderVisibilityCode { get; set; } = "FULL";
    public string PreAcceptMaskingCode { get; set; } = "NONE";
    public string ValidationRuleText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryFieldAssignment
{
    public long Id { get; set; }
    public long FieldDefinitionId { get; set; }
    public long TargetCategoryId { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryFieldOption
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long FieldDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FeePolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string PolicyKindCode { get; set; } = string.Empty;
    public string TransactionTypeCode { get; set; } = string.Empty;
    public string AppliesToText { get; set; } = string.Empty;
    public string? CalculationMethodText { get; set; }
    public decimal? MinBaseAmount { get; set; }
    public decimal? MaxBaseAmount { get; set; }
    public decimal? DisplayFeeAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal? MonthlyAmount { get; set; }
    public decimal? PerVisitAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string ChargeTimingText { get; set; } = string.Empty;
    public string? RestoreRuleText { get; set; }
    public string? Note { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class QualificationPolicy
{
    public long Id { get; set; }
    public string SourcePolicyId { get; set; } = string.Empty;
    public long MiddleCategoryId { get; set; }
    public string IdentityVerificationRuleText { get; set; } = string.Empty;
    public string BusinessRegistrationRuleText { get; set; } = string.Empty;
    public string RequiredLicenseText { get; set; } = string.Empty;
    public string InsuranceRuleText { get; set; } = string.Empty;
    public string EquipmentFacilityRuleText { get; set; } = string.Empty;
    public string BackgroundCheckRuleText { get; set; } = string.Empty;
    public string SafetyGradeCode { get; set; } = string.Empty;
    public string EmergencyRuleText { get; set; } = string.Empty;
    public string ReviewCycleText { get; set; } = string.Empty;
    public string AdminChecklistText { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderRequirementType
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ProviderRequirementDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string RequirementTypeCode { get; set; } = string.Empty;
    public string RequirementCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderDocumentType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool SupportsExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryProviderRequirementAssignment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CategoryOperationPolicyId { get; set; }
    public long RequirementDefinitionId { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool VerificationRequired { get; set; } = true;
    public bool ExpiryCheckRequired { get; set; }
    public short? MinimumValidDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CategoryProviderRequirementEvidenceType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long RequirementAssignmentId { get; set; }
    public long DocumentTypeId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AdministrativeArea
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string SourceSystemCode { get; set; } = "MOIS_STANDARD_CODE";
    public string AreaCode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string AreaLevelCode { get; set; } = string.Empty;
    public long? ParentAreaId { get; set; }
    public string? SourceParentAreaCode { get; set; }
    public DateOnly? SourceCreatedDate { get; set; }
    public DateOnly? SourceAbolishedDate { get; set; }
    public string? AbolitionTypeCode { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderServiceCategory
{
    public long Id { get; set; }
    public long ProviderProfileId { get; set; }
    public long CategoryId { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderServiceArea
{
    public long Id { get; set; }
    public long ProviderServiceCategoryId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class StoredFile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string PurposeCode { get; set; } = string.Empty;
    public string StorageContainer { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public byte[] StorageKeyHash { get; set; } = [];
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hex { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "PENDING";
    public string? ScanResultText { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderDocument
{
    public long Id { get; set; }
    public long ProviderProfileId { get; set; }
    public long FileId { get; set; }
    public long? DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string VerificationStatusCode { get; set; } = "PENDING";
    public DateTime? VerifiedAt { get; set; }
    public long? VerifiedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderServiceRequirementVerification
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderServiceCategoryId { get; set; }
    public long RequirementAssignmentId { get; set; }
    public long? ProviderDocumentId { get; set; }
    public string VerificationStatusCode { get; set; } = "PENDING";
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
