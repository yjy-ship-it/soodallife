namespace SoodalLife.Api.Domain.Entities;

public sealed class AdvertisingPlacement
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RouteHint { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AdvertisingCampaign
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignTypeCode { get; set; } = "ADVERTISEMENT";
    public string AudienceTypeCode { get; set; } = "ALL";
    public string OwnerTypeCode { get; set; } = "HEAD_OFFICE";
    public string? OwnerDisplayName { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string StatusCode { get; set; } = "DRAFT";
    public int Priority { get; set; }
    public string DestinationTypeCode { get; set; } = "NONE";
    public string? DestinationValue { get; set; }
    public string ReviewStatusCode { get; set; } = "DRAFT";
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AdvertisingCampaignPlacement
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long PlacementId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class AdvertisingCampaignCategory
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class AdvertisingCampaignArea
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class AdvertisingCreative
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CampaignId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? BodyText { get; set; }
    public long? FileId { get; set; }
    public string? AltText { get; set; }
    public string? ButtonText { get; set; }
    public string? DestinationTypeCode { get; set; }
    public string? DestinationValue { get; set; }
    public int DisplayOrder { get; set; }
    public string StatusCode { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AdvertisingEvent
{
    public long Id { get; set; }
    public long CreativeId { get; set; }
    public long PlacementId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public sealed class ProviderAdvertisingRatePolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PlacementId { get; set; }
    public int DurationDays { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal ProvinceUnitAmount { get; set; } = 10000;
    public decimal DistrictUnitAmount { get; set; } = 2000;
    public decimal RegionalFeeCapAmount { get; set; } = 30000;
    public string CurrencyCode { get; set; } = "KRW";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderAdvertisingApplication
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CampaignId { get; set; }
    public long ProviderProfileId { get; set; }
    public long RatePolicyId { get; set; }
    public long WalletId { get; set; }
    public int DurationDays { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal BaseFeeAmount { get; set; }
    public decimal RegionalFeeAmount { get; set; }
    public int ProvinceTargetCount { get; set; }
    public int DistrictTargetCount { get; set; }
    public bool AutoRenewEnabled { get; set; }
    public string AutoRenewStatusCode { get; set; } = "OFF";
    public DateTime? NextRenewalAt { get; set; }
    public DateTime? RenewalNoticeSentAt { get; set; }
    public bool RenewalConsentRequired { get; set; }
    public DateTime? RenewalConsentAt { get; set; }
    public decimal? RenewalConsentFeeAmount { get; set; }
    public string? RenewalConsentPolicyFingerprint { get; set; }
    public int RenewalCycleNo { get; set; }
    public DateTime? LastRenewedAt { get; set; }
    public DateTime? AutoRenewDisabledAt { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string StatusCode { get; set; } = "SUBMITTED";
    public string FeeStatusCode { get; set; } = "RESERVED";
    public long ReserveLedgerEntryId { get; set; }
    public long? CaptureLedgerEntryId { get; set; }
    public long? ReleaseLedgerEntryId { get; set; }
    public string? SupplementNote { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ResubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderAdvertisingRenewalHistory
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderAdvertisingApplicationId { get; set; }
    public int CycleNo { get; set; }
    public DateTime DueAt { get; set; }
    public decimal BaseFeeAmount { get; set; }
    public decimal RegionalFeeAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public string CurrencyCode { get; set; } = "KRW";
    public string StatusCode { get; set; } = "PENDING";
    public long? ReserveLedgerEntryId { get; set; }
    public long? CaptureLedgerEntryId { get; set; }
    public DateTime? NoticeSentAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public string PolicyFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ManagedContent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string ContentTypeCode { get; set; } = string.Empty;
    public string AudienceTypeCode { get; set; } = "ALL";
    public string StatusCode { get; set; } = "DRAFT";
    public string ReviewStatusCode { get; set; } = "DRAFT";
    public int CurrentVersionNo { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ManagedContentVersion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ContentId { get; set; }
    public int VersionNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public string? QuestionText { get; set; }
    public string? AnswerText { get; set; }
    public long? FileId { get; set; }
    public string? LinkText { get; set; }
    public string DestinationTypeCode { get; set; } = "NONE";
    public string? DestinationValue { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ManagedContentCategory
{
    public long Id { get; set; }
    public long ContentId { get; set; }
    public long CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ManagedContentArea
{
    public long Id { get; set; }
    public long ContentId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}
