using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProviderExitRequestConfiguration() : EntityConfiguration<ProviderExitRequest>("provider_exit_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderExitRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderExitRequest.UserId), "user_id");
        Mapping.Long(b, nameof(ProviderExitRequest.ProviderProfileId), "provider_profile_id");
        Mapping.NullableLong(b, nameof(ProviderExitRequest.WalletRefundRequestId), "wallet_refund_request_id");
        Mapping.String(b, nameof(ProviderExitRequest.RequestTypeCode), "request_type_code", 30, unicode: false);
        Mapping.String(b, nameof(ProviderExitRequest.Reason), "reason", 1000);
        Mapping.String(b, nameof(ProviderExitRequest.StatusCode), "status_code", 40, unicode: false, defaultValue: "REQUESTED");
        Mapping.String(b, nameof(ProviderExitRequest.ReviewStatusCode), "review_status_code", 30, unicode: false, defaultValue: "PENDING");
        Mapping.DateTime(b, nameof(ProviderExitRequest.RequestedAt), "requested_at");
        Mapping.DateTime(b, nameof(ProviderExitRequest.ReviewedAt), "reviewed_at", nullable: true);
        Mapping.NullableLong(b, nameof(ProviderExitRequest.ReviewedByUserId), "reviewed_by_user_id");
        Mapping.String(b, nameof(ProviderExitRequest.DecisionReason), "decision_reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(ProviderExitRequest.CompletedAt), "completed_at", nullable: true);
        Mapping.String(b, nameof(ProviderExitRequest.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderExitRequest, User>(b, nameof(ProviderExitRequest.UserId));
        Mapping.Fk<ProviderExitRequest, ProviderProfile>(b, nameof(ProviderExitRequest.ProviderProfileId));
        Mapping.Fk<ProviderExitRequest, WalletRefundRequest>(b, nameof(ProviderExitRequest.WalletRefundRequestId));
        Mapping.Fk<ProviderExitRequest, User>(b, nameof(ProviderExitRequest.ReviewedByUserId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.ProviderProfileId, x.StatusCode });
        b.HasIndex(x => x.WalletRefundRequestId).IsUnique().HasFilter("[wallet_refund_request_id] IS NOT NULL");
        b.HasIndex(x => x.ProviderProfileId).IsUnique().HasFilter("[status_code] <> 'COMPLETED' AND [status_code] <> 'REJECTED' AND [status_code] <> 'CANCELLED'");
        b.ToTable("provider_exit_requests", t =>
        {
            t.HasCheckConstraint("CK_provider_exit_requests_type", "[request_type_code] IN ('PROVIDER_ROLE_EXIT','ACCOUNT_WITHDRAWAL')");
            t.HasCheckConstraint("CK_provider_exit_requests_status", "[status_code] IN ('REQUESTED','UNDER_REVIEW','REFUND_REQUIRED','BLOCKED_BY_ACTIVE_WORK','READY_TO_COMPLETE','COMPLETED','REJECTED','CANCELLED')");
            t.HasCheckConstraint("CK_provider_exit_requests_review", "[review_status_code] IN ('PENDING','UNDER_REVIEW','APPROVED','REJECTED')");
        });
    }
}
