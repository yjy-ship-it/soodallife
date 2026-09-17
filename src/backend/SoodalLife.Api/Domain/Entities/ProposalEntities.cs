namespace SoodalLife.Api.Domain.Entities;

public sealed class ProviderProposalCampaign
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProviderProfileId { get; set; }
    public long ProviderServiceCategoryId { get; set; }
    public long WalletId { get; set; }
    public string ProposalTypeCode { get; set; } = "DISCOUNT_SERVICE";
    public string ScopeCode { get; set; } = "LOCAL";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public decimal? NormalPriceAmount { get; set; }
    public decimal OfferPriceAmount { get; set; }
    public int MinimumParticipants { get; set; }
    public int MaximumParticipants { get; set; }
    public int ConfirmedParticipants { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime? ServiceAt { get; set; }
    public string MinimumFailurePolicyCode { get; set; } = "AUTO_CANCEL";
    public string CancellationPolicyText { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "PUBLISHED";
    public decimal FeePerParticipant { get; set; } = 5000;
    public decimal ReservedFeeAmount { get; set; }
    public decimal CapturedFeeAmount { get; set; }
    public string FeeStatusCode { get; set; } = "RESERVED";
    public long ReserveLedgerEntryId { get; set; }
    public long? ReleaseLedgerEntryId { get; set; }
    public DateTime? PublishedNotificationQueuedAt { get; set; }
    public DateTime? MidpointNotificationQueuedAt { get; set; }
    public DateTime? ClosingNotificationQueuedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProviderProposalArea
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class ProviderProposalApplication
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CampaignId { get; set; }
    public long CustomerProfileId { get; set; }
    public string StatusCode { get; set; } = "APPLIED";
    public decimal CapturedFeeAmount { get; set; }
    public long? CaptureLedgerEntryId { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CustomerProposalCategoryInterest
{
    public long Id { get; set; }
    public long CustomerProfileId { get; set; }
    public long CategoryId { get; set; }
    public string SourceCode { get; set; } = "DIRECT";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CustomerProposalAreaInterest
{
    public long Id { get; set; }
    public long CustomerProfileId { get; set; }
    public long AdministrativeAreaId { get; set; }
    public string SourceCode { get; set; } = "DIRECT";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CustomerProposalSignal
{
    public long Id { get; set; }
    public long CustomerProfileId { get; set; }
    public long CategoryId { get; set; }
    public string SignalTypeCode { get; set; } = "SERVICE_DETAIL";
    public DateTime OccurredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
