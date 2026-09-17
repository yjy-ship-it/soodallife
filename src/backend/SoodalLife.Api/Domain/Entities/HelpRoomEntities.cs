namespace SoodalLife.Api.Domain.Entities;

public sealed class HelpPost
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerUserId { get; set; }
    public long CategoryId { get; set; }
    public long? AdministrativeAreaId { get; set; }
    public string RegionDisclosureCode { get; set; } = "HIDDEN";
    public string PurposeCode { get; set; } = "DIAGNOSIS";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "PUBLISHED";
    public string IntentCode { get; set; } = "ADVICE";
    public string? IntentReason { get; set; }
    public long? ConvertedServiceRequestId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HelpRoomEntry
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long HelpPostId { get; set; }
    public long AuthorUserId { get; set; }
    public long? ProviderProfileId { get; set; }
    public string AuthorRoleCode { get; set; } = "CUSTOMER";
    public string EntryTypeCode { get; set; } = "CUSTOMER_FOLLOW_UP";
    public string Body { get; set; } = string.Empty;
    public string? CauseText { get; set; }
    public string? CheckText { get; set; }
    public string? DiyStepsText { get; set; }
    public string? RiskText { get; set; }
    public string? NextStepText { get; set; }
    public bool RequiresProfessional { get; set; }
    public string SafetyCode { get; set; } = "NORMAL";
    public string StatusCode { get; set; } = "PUBLISHED";
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class HelpPostFile
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long HelpPostId { get; set; }
    public long FileId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class HelpPostResolution
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long HelpPostId { get; set; }
    public long ResolvedByCustomerUserId { get; set; }
    public string ResolutionCode { get; set; } = "SELF_RESOLVED";
    public string Summary { get; set; } = string.Empty;
    public long? HelpfulEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public sealed class UserSuggestion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string TypeCode { get; set; } = "OTHER";
    public string VisibilityCode { get; set; } = "PRIVATE";
    public string StatusCode { get; set; } = "RECEIVED";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string? DeviceInfo { get; set; }
    public string? AppVersion { get; set; }
    public long? AssignedAdminUserId { get; set; }
    public string? AdminReply { get; set; }
    public string? ReleaseVersion { get; set; }
    public byte[] ContentHash { get; set; } = [];
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class UserSuggestionEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long UserSuggestionId { get; set; }
    public long ActorUserId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string? FromStatusCode { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
