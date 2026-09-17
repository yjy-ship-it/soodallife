namespace SoodalLife.Api.Domain.Entities;

public sealed class SiteVisitProposal
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ServiceRequestId { get; set; }
    public long RequestDispatchId { get; set; }
    public long ProviderProfileId { get; set; }
    public string StatusCode { get; set; } = "PROPOSED";
    public DateTime ScheduledAt { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 30;
    public decimal VisitFeeAmount { get; set; }
    public string PaymentModeCode { get; set; } = "NO_FEE";
    public string PaymentStatusCode { get; set; } = "NOT_REQUIRED";
    public bool DeductFromWorkAmount { get; set; }
    public string? TermsText { get; set; }
    public string? PaymentInstructionProtected { get; set; }
    public int NoShowWaitMinutes { get; set; } = 10;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PaymentReportedAt { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? NoShowStatusCode { get; set; }
    public DateTime? NoShowReportedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SiteVisitEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long SiteVisitProposalId { get; set; }
    public long ActorUserId { get; set; }
    public string EventTypeCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
