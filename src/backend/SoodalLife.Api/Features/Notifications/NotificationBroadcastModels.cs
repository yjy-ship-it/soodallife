using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Notifications;

public sealed record SaveNotificationBroadcastRequest([param:Required,StringLength(200)]string Name,string AudienceCode,string KindCode,IReadOnlyList<string> Channels,[param:Required,StringLength(300)]string Title,[param:Required,StringLength(3000)]string Body,DateTime? ScheduledAt,string? RowVersion=null);
public sealed record ConfirmNotificationBroadcastRequest(string ConfirmationText,string? RowVersion);
public sealed record CancelNotificationBroadcastRequest([param:Required,StringLength(1000)]string Reason,string? RowVersion);
public sealed record NotificationBroadcastPreview(int TotalActiveMembers,IReadOnlyDictionary<string,int> EligibleByChannel,int ExcludedByRoleOrStatus,int DuplicateRolesRemoved,DateTime CalculatedAt);
public sealed record NotificationBroadcastItem(Guid Id,string Name,string AudienceCode,string KindCode,IReadOnlyList<string> Channels,string Title,string Body,DateTime? ScheduledAt,string StatusCode,int EstimatedAudienceCount,int RecipientsProcessedCount,int DeliveriesCreatedCount,DateTime? PreviewedAt,DateTime? ConfirmedAt,DateTime? CancelledAt,string? CancellationReason,DateTime CreatedAt,string RowVersion);
public sealed record PagedNotificationBroadcasts(IReadOnlyList<NotificationBroadcastItem> Items,int Page,int PageSize,int TotalCount,int TotalPages);
