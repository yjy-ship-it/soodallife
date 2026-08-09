using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class ServiceRequestConfiguration() : EntityConfiguration<ServiceRequest>("service_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ServiceRequest.CustomerProfileId), "customer_profile_id");
        Mapping.Long(b, nameof(ServiceRequest.CategoryId), "category_id");
        Mapping.Long(b, nameof(ServiceRequest.CategoryPolicyId), "category_policy_id");
        Mapping.Long(b, nameof(ServiceRequest.AdministrativeAreaId), "administrative_area_id");
        Mapping.String(b, nameof(ServiceRequest.DetailAddress), "detail_address", 500, nullable: true);
        Mapping.String(b, nameof(ServiceRequest.Title), "title", 200);
        Mapping.String(b, nameof(ServiceRequest.Description), "description", null, nullable: true);
        Mapping.String(b, nameof(ServiceRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "DRAFT");
        Mapping.Bool(b, nameof(ServiceRequest.IsUrgent), "is_urgent", false);
        Mapping.String(b, nameof(ServiceRequest.PolicySnapshotJson), "policy_snapshot_json", null);
        Mapping.DateTime(b, nameof(ServiceRequest.OpenedAt), "opened_at", nullable: true);
        Mapping.DateTime(b, nameof(ServiceRequest.ExpiresAt), "expires_at", nullable: true);
        Mapping.DateTime(b, nameof(ServiceRequest.AcceptedAt), "accepted_at", nullable: true);
        Mapping.DateTime(b, nameof(ServiceRequest.CancelledAt), "cancelled_at", nullable: true);
        Mapping.String(b, nameof(ServiceRequest.CancellationReason), "cancellation_reason", 1000, nullable: true);
        Mapping.String(b, nameof(ServiceRequest.IdempotencyKey), "idempotency_key", 100, nullable: true, unicode: false);
        Mapping.FullAudit(b);
        Mapping.Fk<ServiceRequest, CustomerProfile>(b, nameof(ServiceRequest.CustomerProfileId));
        Mapping.Fk<ServiceRequest, ServiceCategory>(b, nameof(ServiceRequest.CategoryId));
        Mapping.Fk<ServiceRequest, CategoryPolicy>(b, nameof(ServiceRequest.CategoryPolicyId));
        Mapping.Fk<ServiceRequest, AdministrativeArea>(b, nameof(ServiceRequest.AdministrativeAreaId));
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.CategoryPolicyId);
        b.HasIndex(x => x.AdministrativeAreaId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.IsUrgent);
        b.HasIndex(x => x.OpenedAt);
        b.HasIndex(x => x.ExpiresAt);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[idempotency_key] IS NOT NULL");
        b.HasIndex(x => new { x.CategoryId, x.AdministrativeAreaId, x.StatusCode, x.OpenedAt });
        b.HasIndex(x => new { x.CustomerProfileId, x.CreatedAt }).IsDescending(false, true);
        b.ToTable("service_requests", t =>
        {
            t.HasCheckConstraint("CK_service_requests_status", "[status_code] IN ('DRAFT','OPEN','ACCEPTED','EXPIRED','CANCELLED')");
            t.HasCheckConstraint("CK_service_requests_policy_json", "ISJSON([policy_snapshot_json]) = 1");
        });
    }
}

internal sealed class RequestAnswerConfiguration() : EntityConfiguration<RequestAnswer>("request_answers")
{
    protected override void ConfigureEntity(EntityTypeBuilder<RequestAnswer> b)
    {
        Mapping.Long(b, nameof(RequestAnswer.ServiceRequestId), "service_request_id");
        Mapping.Long(b, nameof(RequestAnswer.FieldDefinitionId), "field_definition_id");
        Mapping.String(b, nameof(RequestAnswer.ValueText), "value_text", null, nullable: true);
        Mapping.Decimal(b, nameof(RequestAnswer.ValueNumber), "value_number", nullable: true);
        b.Property(x => x.ValueBoolean).HasColumnName("value_boolean").HasColumnType("bit").IsRequired(false);
        Mapping.Date(b, nameof(RequestAnswer.ValueDate), "value_date", nullable: true);
        Mapping.DateTime(b, nameof(RequestAnswer.ValueDateTime), "value_datetime", nullable: true);
        Mapping.String(b, nameof(RequestAnswer.ValueJson), "value_json", null, nullable: true);
        Mapping.String(b, nameof(RequestAnswer.ValueCurrencyCode), "value_currency_code", 3, nullable: true, unicode: false, fixedLength: true);
        Mapping.FullAudit(b);
        Mapping.Fk<RequestAnswer, ServiceRequest>(b, nameof(RequestAnswer.ServiceRequestId));
        Mapping.Fk<RequestAnswer, CategoryFieldDefinition>(b, nameof(RequestAnswer.FieldDefinitionId));
        b.HasIndex(x => x.ServiceRequestId);
        b.HasIndex(x => x.FieldDefinitionId);
        b.HasIndex(x => new { x.ServiceRequestId, x.FieldDefinitionId }).IsUnique();
        b.ToTable("request_answers", t => t.HasCheckConstraint("CK_request_answers_value_json", "[value_json] IS NULL OR ISJSON([value_json]) = 1"));
    }
}

internal sealed class RequestAnswerFileConfiguration() : EntityConfiguration<RequestAnswerFile>("request_answer_files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<RequestAnswerFile> b)
    {
        Mapping.Long(b, nameof(RequestAnswerFile.RequestAnswerId), "request_answer_id");
        Mapping.Long(b, nameof(RequestAnswerFile.FileId), "file_id");
        Mapping.Int(b, nameof(RequestAnswerFile.DisplayOrder), "display_order", 0);
        Mapping.CreatedAudit(b);
        Mapping.Fk<RequestAnswerFile, RequestAnswer>(b, nameof(RequestAnswerFile.RequestAnswerId));
        Mapping.Fk<RequestAnswerFile, StoredFile>(b, nameof(RequestAnswerFile.FileId));
        b.HasIndex(x => x.RequestAnswerId);
        b.HasIndex(x => x.FileId);
        b.HasIndex(x => new { x.RequestAnswerId, x.FileId }).IsUnique();
    }
}

internal sealed class DispatchCandidateConfiguration() : EntityConfiguration<DispatchCandidate>("dispatch_candidates")
{
    protected override void ConfigureEntity(EntityTypeBuilder<DispatchCandidate> b)
    {
        Mapping.Long(b, nameof(DispatchCandidate.ServiceRequestId), "service_request_id");
        Mapping.Long(b, nameof(DispatchCandidate.ProviderProfileId), "provider_profile_id");
        Mapping.String(b, nameof(DispatchCandidate.StatusCode), "status_code", 20, unicode: false);
        Mapping.Bool(b, nameof(DispatchCandidate.CategoryMatch), "category_match");
        Mapping.Bool(b, nameof(DispatchCandidate.AreaMatch), "area_match");
        Mapping.Bool(b, nameof(DispatchCandidate.ApprovalMatch), "approval_match");
        Mapping.String(b, nameof(DispatchCandidate.ReasonCode), "reason_code", 50, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(DispatchCandidate.EvaluatedAt), "evaluated_at", utcDefault: true);
        Mapping.DateTime(b, nameof(DispatchCandidate.ExpiresAt), "expires_at", nullable: true);
        Mapping.DateTime(b, nameof(DispatchCandidate.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<DispatchCandidate, ServiceRequest>(b, nameof(DispatchCandidate.ServiceRequestId));
        Mapping.Fk<DispatchCandidate, ProviderProfile>(b, nameof(DispatchCandidate.ProviderProfileId));
        b.HasIndex(x => x.ServiceRequestId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.ReasonCode);
        b.HasIndex(x => x.EvaluatedAt);
        b.HasIndex(x => x.ExpiresAt);
        b.HasIndex(x => new { x.ServiceRequestId, x.ProviderProfileId }).IsUnique();
        b.HasIndex(x => new { x.ServiceRequestId, x.StatusCode });
        b.ToTable("dispatch_candidates", t => t.HasCheckConstraint("CK_dispatch_candidates_status", "[status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','EXPIRED')"));
    }
}

internal sealed class RequestDispatchConfiguration() : EntityConfiguration<RequestDispatch>("request_dispatches")
{
    protected override void ConfigureEntity(EntityTypeBuilder<RequestDispatch> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(RequestDispatch.ServiceRequestId), "service_request_id");
        Mapping.Long(b, nameof(RequestDispatch.ProviderProfileId), "provider_profile_id");
        Mapping.Long(b, nameof(RequestDispatch.CandidateId), "candidate_id");
        Mapping.String(b, nameof(RequestDispatch.StatusCode), "status_code", 20, unicode: false, defaultValue: "AVAILABLE");
        Mapping.DateTime(b, nameof(RequestDispatch.AvailableAt), "available_at", utcDefault: true);
        Mapping.DateTime(b, nameof(RequestDispatch.ViewedAt), "viewed_at", nullable: true);
        Mapping.DateTime(b, nameof(RequestDispatch.RespondedAt), "responded_at", nullable: true);
        Mapping.DateTime(b, nameof(RequestDispatch.ExpiresAt), "expires_at");
        Mapping.String(b, nameof(RequestDispatch.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.DateTime(b, nameof(RequestDispatch.CreatedAt), "created_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<RequestDispatch, ServiceRequest>(b, nameof(RequestDispatch.ServiceRequestId));
        Mapping.Fk<RequestDispatch, ProviderProfile>(b, nameof(RequestDispatch.ProviderProfileId));
        Mapping.Fk<RequestDispatch, DispatchCandidate>(b, nameof(RequestDispatch.CandidateId));
        b.HasIndex(x => x.ServiceRequestId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.CandidateId).IsUnique();
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.AvailableAt);
        b.HasIndex(x => x.ExpiresAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.ServiceRequestId, x.ProviderProfileId }).IsUnique();
        b.HasIndex(x => new { x.ProviderProfileId, x.StatusCode, x.AvailableAt }).IsDescending(false, false, true);
        b.ToTable("request_dispatches", t => t.HasCheckConstraint("CK_request_dispatches_status", "[status_code] IN ('AVAILABLE','VIEWED','RESPONDED','EXPIRED')"));
    }
}

internal sealed class NotificationConfiguration() : EntityConfiguration<Notification>("notifications")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Notification> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(Notification.RecipientUserId), "recipient_user_id");
        Mapping.NullableLong(b, nameof(Notification.RequestDispatchId), "request_dispatch_id");
        Mapping.String(b, nameof(Notification.TypeCode), "type_code", 50, unicode: false);
        Mapping.String(b, nameof(Notification.StatusCode), "status_code", 30, unicode: false, defaultValue: "PENDING");
        Mapping.String(b, nameof(Notification.Title), "title", 200);
        Mapping.String(b, nameof(Notification.Body), "body", 2000);
        Mapping.String(b, nameof(Notification.DataJson), "data_json", null, nullable: true);
        Mapping.Bool(b, nameof(Notification.IsUrgent), "is_urgent", false);
        Mapping.DateTime(b, nameof(Notification.RecordedAt), "recorded_at", utcDefault: true);
        Mapping.DateTime(b, nameof(Notification.ReadAt), "read_at", nullable: true);
        Mapping.String(b, nameof(Notification.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.NullableLong(b, nameof(Notification.CreatedByUserId), "created_by_user_id");
        Mapping.RowVersion(b);
        Mapping.Fk<Notification, User>(b, nameof(Notification.RecipientUserId));
        Mapping.Fk<Notification, RequestDispatch>(b, nameof(Notification.RequestDispatchId));
        Mapping.Fk<Notification, User>(b, nameof(Notification.CreatedByUserId));
        b.HasIndex(x => x.RecipientUserId);
        b.HasIndex(x => x.RequestDispatchId);
        b.HasIndex(x => x.TypeCode);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.IsUrgent);
        b.HasIndex(x => x.RecordedAt);
        b.HasIndex(x => x.ReadAt);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.RecipientUserId, x.ReadAt, x.RecordedAt }).IsDescending(false, false, true);
        b.ToTable("notifications", t =>
        {
            t.HasCheckConstraint("CK_notifications_status", "[status_code] IN ('PENDING','RECORDED','PROCESSING','SENT','PARTIALLY_FAILED','FAILED')");
            t.HasCheckConstraint("CK_notifications_data_json", "[data_json] IS NULL OR ISJSON([data_json]) = 1");
        });
    }
}

internal sealed class NotificationDeliveryConfiguration() : EntityConfiguration<NotificationDelivery>("notification_deliveries")
{
    protected override void ConfigureEntity(EntityTypeBuilder<NotificationDelivery> b)
    {
        Mapping.Long(b, nameof(NotificationDelivery.NotificationId), "notification_id");
        Mapping.String(b, nameof(NotificationDelivery.ChannelCode), "channel_code", 20, unicode: false);
        Mapping.Short(b, nameof(NotificationDelivery.AttemptNo), "attempt_no", 1);
        Mapping.String(b, nameof(NotificationDelivery.StatusCode), "status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.String(b, nameof(NotificationDelivery.ProviderMessageId), "provider_message_id", 200, nullable: true);
        Mapping.String(b, nameof(NotificationDelivery.ErrorCode), "error_code", 100, nullable: true);
        Mapping.String(b, nameof(NotificationDelivery.ErrorMessage), "error_message", 1000, nullable: true);
        Mapping.DateTime(b, nameof(NotificationDelivery.AttemptedAt), "attempted_at", utcDefault: true);
        Mapping.DateTime(b, nameof(NotificationDelivery.CompletedAt), "completed_at", nullable: true);
        Mapping.Fk<NotificationDelivery, Notification>(b, nameof(NotificationDelivery.NotificationId));
        b.HasIndex(x => x.NotificationId);
        b.HasIndex(x => x.ChannelCode);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.ProviderMessageId);
        b.HasIndex(x => x.AttemptedAt);
        b.HasIndex(x => new { x.NotificationId, x.ChannelCode, x.AttemptNo }).IsUnique();
        b.ToTable("notification_deliveries", t =>
        {
            t.HasCheckConstraint("CK_notification_deliveries_channel", "[channel_code] IN ('IN_APP','ALIMTALK')");
            t.HasCheckConstraint("CK_notification_deliveries_status", "[status_code] IN ('PENDING','SENT','FAILED','SKIPPED')");
        });
    }
}

internal sealed class QuoteConfiguration() : EntityConfiguration<Quote>("quotes")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Quote> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(Quote.ServiceRequestId), "service_request_id");
        Mapping.Long(b, nameof(Quote.ProviderProfileId), "provider_profile_id");
        Mapping.Long(b, nameof(Quote.RequestDispatchId), "request_dispatch_id");
        Mapping.String(b, nameof(Quote.StatusCode), "status_code", 20, unicode: false, defaultValue: "DRAFT");
        Mapping.DateTime(b, nameof(Quote.SubmittedAt), "submitted_at", nullable: true);
        Mapping.DateTime(b, nameof(Quote.AcceptedAt), "accepted_at", nullable: true);
        Mapping.DateTime(b, nameof(Quote.WithdrawnAt), "withdrawn_at", nullable: true);
        Mapping.DateTime(b, nameof(Quote.ExpiresAt), "expires_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<Quote, ServiceRequest>(b, nameof(Quote.ServiceRequestId));
        Mapping.Fk<Quote, ProviderProfile>(b, nameof(Quote.ProviderProfileId));
        Mapping.Fk<Quote, RequestDispatch>(b, nameof(Quote.RequestDispatchId));
        b.HasIndex(x => x.ServiceRequestId);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.RequestDispatchId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.SubmittedAt);
        b.HasIndex(x => x.ExpiresAt);
        b.HasIndex(x => new { x.ServiceRequestId, x.ProviderProfileId }).IsUnique();
        b.HasIndex(x => new { x.ServiceRequestId, x.StatusCode, x.SubmittedAt }).IsDescending(false, false, true);
        b.ToTable("quotes", t => t.HasCheckConstraint("CK_quotes_status", "[status_code] IN ('DRAFT','SUBMITTED','ACCEPTED','NOT_SELECTED','WITHDRAWN','EXPIRED','INVALIDATED')"));
    }
}

internal sealed class QuoteRevisionConfiguration() : EntityConfiguration<QuoteRevision>("quote_revisions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<QuoteRevision> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(QuoteRevision.QuoteId), "quote_id");
        Mapping.Int(b, nameof(QuoteRevision.RevisionNo), "revision_no");
        Mapping.String(b, nameof(QuoteRevision.Summary), "summary", 1000);
        Mapping.String(b, nameof(QuoteRevision.Terms), "terms", null, nullable: true);
        Mapping.Decimal(b, nameof(QuoteRevision.SubtotalAmount), "subtotal_amount");
        Mapping.Decimal(b, nameof(QuoteRevision.VatAmount), "vat_amount", defaultValue: 0m);
        Mapping.Decimal(b, nameof(QuoteRevision.TotalAmount), "total_amount");
        Mapping.String(b, nameof(QuoteRevision.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.String(b, nameof(QuoteRevision.EstimatedDurationText), "estimated_duration_text", 200, nullable: true);
        Mapping.DateTime(b, nameof(QuoteRevision.AvailableStartAt), "available_start_at", nullable: true);
        Mapping.DateTime(b, nameof(QuoteRevision.ValidUntil), "valid_until");
        Mapping.String(b, nameof(QuoteRevision.RevisionReason), "revision_reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(QuoteRevision.SubmittedAt), "submitted_at", utcDefault: true);
        Mapping.Long(b, nameof(QuoteRevision.SubmittedByUserId), "submitted_by_user_id");
        Mapping.String(b, nameof(QuoteRevision.IdempotencyKey), "idempotency_key", 100, unicode: false);
        Mapping.Fk<QuoteRevision, Quote>(b, nameof(QuoteRevision.QuoteId));
        Mapping.Fk<QuoteRevision, User>(b, nameof(QuoteRevision.SubmittedByUserId));
        b.HasIndex(x => x.QuoteId);
        b.HasIndex(x => x.TotalAmount);
        b.HasIndex(x => x.ValidUntil);
        b.HasIndex(x => x.SubmittedAt);
        b.HasIndex(x => x.SubmittedByUserId);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.QuoteId, x.RevisionNo }).IsUnique().IsDescending(false, true);
        b.HasIndex(x => new { x.QuoteId, x.SubmittedAt }).IsDescending(false, true);
    }
}

internal sealed class QuoteItemConfiguration() : EntityConfiguration<QuoteItem>("quote_items")
{
    protected override void ConfigureEntity(EntityTypeBuilder<QuoteItem> b)
    {
        Mapping.Long(b, nameof(QuoteItem.QuoteRevisionId), "quote_revision_id");
        Mapping.Int(b, nameof(QuoteItem.LineNo), "line_no");
        Mapping.String(b, nameof(QuoteItem.ItemName), "item_name", 200);
        Mapping.String(b, nameof(QuoteItem.Description), "description", 1000, nullable: true);
        Mapping.Decimal(b, nameof(QuoteItem.Quantity), "quantity", defaultValue: 1m);
        Mapping.String(b, nameof(QuoteItem.UnitText), "unit_text", 50, nullable: true);
        Mapping.Decimal(b, nameof(QuoteItem.UnitPriceAmount), "unit_price_amount");
        Mapping.Decimal(b, nameof(QuoteItem.LineTotalAmount), "line_total_amount");
        Mapping.String(b, nameof(QuoteItem.CurrencyCode), "currency_code", 3, unicode: false, fixedLength: true, defaultValue: "KRW");
        Mapping.Fk<QuoteItem, QuoteRevision>(b, nameof(QuoteItem.QuoteRevisionId));
        b.HasIndex(x => x.QuoteRevisionId);
        b.HasIndex(x => new { x.QuoteRevisionId, x.LineNo }).IsUnique();
        b.ToTable("quote_items", t => t.HasCheckConstraint("CK_quote_items_quantity", "[quantity] > 0"));
    }
}
