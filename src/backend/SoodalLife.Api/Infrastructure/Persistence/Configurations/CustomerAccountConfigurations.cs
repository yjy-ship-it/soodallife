using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class LegalDocumentConfiguration() : EntityConfiguration<LegalDocument>("legal_documents")
{
    protected override void ConfigureEntity(EntityTypeBuilder<LegalDocument> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(LegalDocument.Code), "code", 50, unicode: false);
        Mapping.String(b, nameof(LegalDocument.AudienceCode), "audience_code", 20, unicode: false, defaultValue: "CUSTOMER");
        Mapping.String(b, nameof(LegalDocument.RequirementCode), "requirement_code", 20, unicode: false);
        Mapping.Int(b, nameof(LegalDocument.DisplayOrder), "display_order", 0);
        Mapping.Bool(b, nameof(LegalDocument.IsActive), "is_active", true);
        Mapping.Bool(b, nameof(LegalDocument.IsPlaceholder), "is_placeholder", false);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.AudienceCode, x.IsActive, x.DisplayOrder });
        b.ToTable("legal_documents", t =>
        {
            t.HasCheckConstraint("CK_legal_documents_audience", "[audience_code] IN ('CUSTOMER','PROVIDER','ALL')");
            t.HasCheckConstraint("CK_legal_documents_requirement", "[requirement_code] IN ('REQUIRED','OPTIONAL','NOTICE')");
        });
    }
}

internal sealed class LegalDocumentVersionConfiguration() : EntityConfiguration<LegalDocumentVersion>("legal_document_versions")
{
    protected override void ConfigureEntity(EntityTypeBuilder<LegalDocumentVersion> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(LegalDocumentVersion.LegalDocumentId), "legal_document_id");
        Mapping.Int(b, nameof(LegalDocumentVersion.VersionNo), "version_no");
        Mapping.String(b, nameof(LegalDocumentVersion.Title), "title", 300);
        Mapping.String(b, nameof(LegalDocumentVersion.Content), "content", null);
        Mapping.DateTime(b, nameof(LegalDocumentVersion.EffectiveFrom), "effective_from");
        Mapping.DateTime(b, nameof(LegalDocumentVersion.EffectiveTo), "effective_to", nullable: true);
        Mapping.Bool(b, nameof(LegalDocumentVersion.IsActive), "is_active", true);
        Mapping.Bool(b, nameof(LegalDocumentVersion.IsPlaceholder), "is_placeholder", false);
        Mapping.CreatedAudit(b);
        Mapping.RowVersion(b);
        Mapping.Fk<LegalDocumentVersion, LegalDocument>(b, nameof(LegalDocumentVersion.LegalDocumentId));
        b.HasIndex(x => new { x.LegalDocumentId, x.VersionNo }).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.EffectiveFrom, x.EffectiveTo });
    }
}

internal sealed class UserConsentConfiguration() : EntityConfiguration<UserConsent>("user_consents")
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserConsent> b)
    {
        Mapping.Long(b, nameof(UserConsent.UserId), "user_id");
        Mapping.Long(b, nameof(UserConsent.LegalDocumentVersionId), "legal_document_version_id");
        Mapping.String(b, nameof(UserConsent.ConsentStatusCode), "consent_status_code", 20, unicode: false);
        Mapping.DateTime(b, nameof(UserConsent.ConsentedAt), "consented_at");
        Mapping.DateTime(b, nameof(UserConsent.WithdrawnAt), "withdrawn_at", nullable: true);
        Mapping.String(b, nameof(UserConsent.SourceCode), "source_code", 30, unicode: false);
        Mapping.String(b, nameof(UserConsent.IpAddress), "ip_address", 45, nullable: true, unicode: false);
        Mapping.String(b, nameof(UserConsent.UserAgent), "user_agent", 1000, nullable: true);
        Mapping.DateTime(b, nameof(UserConsent.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<UserConsent, User>(b, nameof(UserConsent.UserId));
        Mapping.Fk<UserConsent, LegalDocumentVersion>(b, nameof(UserConsent.LegalDocumentVersionId));
        b.HasIndex(x => new { x.UserId, x.LegalDocumentVersionId }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.ConsentStatusCode });
        b.ToTable("user_consents", t => t.HasCheckConstraint("CK_user_consents_status", "[consent_status_code] IN ('CONSENTED','WITHDRAWN')"));
    }
}

internal sealed class CustomerAddressConfiguration() : EntityConfiguration<CustomerAddress>("customer_addresses")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerAddress> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CustomerAddress.CustomerProfileId), "customer_profile_id");
        Mapping.String(b, nameof(CustomerAddress.AddressName), "address_name", 100);
        Mapping.String(b, nameof(CustomerAddress.RecipientName), "recipient_name", 100, nullable: true);
        Mapping.String(b, nameof(CustomerAddress.PostalCode), "postal_code", 10);
        Mapping.String(b, nameof(CustomerAddress.RoadAddress), "road_address", 500);
        Mapping.String(b, nameof(CustomerAddress.DetailAddress), "detail_address", 500);
        Mapping.NullableLong(b, nameof(CustomerAddress.AdministrativeAreaId), "administrative_area_id");
        Mapping.Decimal(b, nameof(CustomerAddress.Latitude), "latitude", nullable: true, precision: 10, scale: 7);
        Mapping.Decimal(b, nameof(CustomerAddress.Longitude), "longitude", nullable: true, precision: 10, scale: 7);
        Mapping.Bool(b, nameof(CustomerAddress.IsDefault), "is_default", false);
        Mapping.Bool(b, nameof(CustomerAddress.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        Mapping.Fk<CustomerAddress, CustomerProfile>(b, nameof(CustomerAddress.CustomerProfileId));
        Mapping.Fk<CustomerAddress, AdministrativeArea>(b, nameof(CustomerAddress.AdministrativeAreaId));
        b.HasIndex(x => x.CustomerProfileId);
        b.HasIndex(x => new { x.CustomerProfileId, x.IsDefault }).IsUnique().HasFilter("[is_active] = 1 AND [is_default] = 1");
        b.HasIndex(x => new { x.CustomerProfileId, x.IsActive, x.CreatedAt });
    }
}

internal sealed class PasswordResetRequestConfiguration() : EntityConfiguration<PasswordResetRequest>("password_reset_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<PasswordResetRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.NullableLong(b, nameof(PasswordResetRequest.UserId), "user_id");
        b.Property(x => x.RequestedIdentifierHash).HasColumnName("requested_identifier_hash").HasColumnType("binary(32)").HasMaxLength(32).IsFixedLength().IsRequired();
        Mapping.String(b, nameof(PasswordResetRequest.RequestedIdentifierMasked), "requested_identifier_masked", 320);
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("binary(32)").HasMaxLength(32).IsFixedLength().IsRequired();
        Mapping.DateTime(b, nameof(PasswordResetRequest.ExpiresAt), "expires_at");
        Mapping.DateTime(b, nameof(PasswordResetRequest.UsedAt), "used_at", nullable: true);
        Mapping.String(b, nameof(PasswordResetRequest.DeliveryStatusCode), "delivery_status_code", 30, unicode: false, defaultValue: "NOT_INTEGRATED");
        Mapping.DateTime(b, nameof(PasswordResetRequest.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<PasswordResetRequest, User>(b, nameof(PasswordResetRequest.UserId));
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.RequestedIdentifierHash, x.CreatedAt });
        b.HasIndex(x => new { x.ExpiresAt, x.UsedAt });
        b.ToTable("password_reset_requests", t => t.HasCheckConstraint("CK_password_reset_delivery", "[delivery_status_code] IN ('NOT_INTEGRATED','PENDING','SENT','FAILED')"));
    }
}

internal sealed class CustomerWithdrawalRequestConfiguration() : EntityConfiguration<CustomerWithdrawalRequest>("customer_withdrawal_requests")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerWithdrawalRequest> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CustomerWithdrawalRequest.UserId), "user_id");
        Mapping.String(b, nameof(CustomerWithdrawalRequest.ScopeCode), "scope_code", 30, unicode: false);
        Mapping.String(b, nameof(CustomerWithdrawalRequest.StatusCode), "status_code", 20, unicode: false, defaultValue: "REQUESTED");
        Mapping.String(b, nameof(CustomerWithdrawalRequest.Reason), "reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(CustomerWithdrawalRequest.RequestedAt), "requested_at");
        Mapping.DateTime(b, nameof(CustomerWithdrawalRequest.ProcessedAt), "processed_at", nullable: true);
        Mapping.NullableLong(b, nameof(CustomerWithdrawalRequest.ProcessedByUserId), "processed_by_user_id");
        Mapping.String(b, nameof(CustomerWithdrawalRequest.DecisionReason), "decision_reason", 1000, nullable: true);
        Mapping.RowVersion(b);
        Mapping.Fk<CustomerWithdrawalRequest, User>(b, nameof(CustomerWithdrawalRequest.UserId));
        Mapping.Fk<CustomerWithdrawalRequest, User>(b, nameof(CustomerWithdrawalRequest.ProcessedByUserId));
        b.HasIndex(x => new { x.UserId, x.StatusCode });
        b.HasIndex(x => new { x.UserId, x.ScopeCode }).IsUnique().HasFilter("[status_code] = 'REQUESTED'");
        b.ToTable("customer_withdrawal_requests", t =>
        {
            t.HasCheckConstraint("CK_customer_withdrawal_scope", "[scope_code] IN ('CUSTOMER_ROLE','ACCOUNT')");
            t.HasCheckConstraint("CK_customer_withdrawal_status", "[status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')");
        });
    }
}
