using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration() : EntityConfiguration<User>("users")
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(User.LoginId), "login_id", 256);
        Mapping.String(b, nameof(User.NormalizedLoginId), "normalized_login_id", 256);
        Mapping.String(b, nameof(User.PasswordHash), "password_hash", 512);
        Mapping.String(b, nameof(User.Email), "email", 320, nullable: true);
        Mapping.String(b, nameof(User.NormalizedEmail), "normalized_email", 320, nullable: true);
        Mapping.String(b, nameof(User.Phone), "phone", 32, nullable: true);
        Mapping.Binary(b, nameof(User.EmailEncrypted), "email_encrypted");
        Mapping.Binary(b, nameof(User.EmailSearchHash), "email_search_hash", 32);
        Mapping.Binary(b, nameof(User.PhoneEncrypted), "phone_encrypted");
        Mapping.Binary(b, nameof(User.PhoneSearchHash), "phone_search_hash", 32);
        Mapping.NullableShort(b, nameof(User.PrivacyProtectionVersion), "privacy_protection_version");
        Mapping.String(b, nameof(User.EmailVerificationStatusCode), "email_verification_status_code", 30, unicode: false, defaultValue: "NOT_INTEGRATED");
        Mapping.String(b, nameof(User.PhoneVerificationStatusCode), "phone_verification_status_code", 30, unicode: false, defaultValue: "NOT_INTEGRATED");
        Mapping.String(b, nameof(User.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.DateTime(b, nameof(User.LastLoginAt), "last_login_at", nullable: true);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.NormalizedLoginId).IsUnique();
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[normalized_email] IS NOT NULL");
        b.HasIndex(x => x.Phone);
        b.HasIndex(x => x.EmailSearchHash).IsUnique().HasFilter("[email_search_hash] IS NOT NULL");
        b.HasIndex(x => x.PhoneSearchHash).HasFilter("[phone_search_hash] IS NOT NULL");
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.CreatedAt);
        b.ToTable("users", t =>
        {
            t.HasCheckConstraint("CK_users_status_code", "[status_code] IN ('ACTIVE','SUSPENDED','WITHDRAWN')");
            t.HasCheckConstraint("CK_users_email_verification", "[email_verification_status_code] IN ('NOT_INTEGRATED','PENDING','VERIFIED')");
            t.HasCheckConstraint("CK_users_phone_verification", "[phone_verification_status_code] IN ('NOT_INTEGRATED','PENDING','VERIFIED')");
        });
    }
}

internal sealed class RoleConfiguration() : EntityConfiguration<Role>("roles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> b)
    {
        Mapping.String(b, nameof(Role.Code), "code", 30, unicode: false);
        Mapping.String(b, nameof(Role.Name), "name", 100);
        Mapping.Bool(b, nameof(Role.IsActive), "is_active", true);
        Mapping.FullAudit(b);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.IsActive);
    }
}

internal sealed class UserRoleConfiguration() : EntityConfiguration<UserRole>("user_roles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserRole> b)
    {
        Mapping.Long(b, nameof(UserRole.UserId), "user_id");
        Mapping.Long(b, nameof(UserRole.RoleId), "role_id");
        Mapping.DateTime(b, nameof(UserRole.GrantedAt), "granted_at", utcDefault: true);
        Mapping.NullableLong(b, nameof(UserRole.GrantedByUserId), "granted_by_user_id");
        Mapping.DateTime(b, nameof(UserRole.RevokedAt), "revoked_at", nullable: true);
        Mapping.NullableLong(b, nameof(UserRole.RevokedByUserId), "revoked_by_user_id");
        Mapping.Fk<UserRole, User>(b, nameof(UserRole.UserId));
        Mapping.Fk<UserRole, Role>(b, nameof(UserRole.RoleId));
        Mapping.Fk<UserRole, User>(b, nameof(UserRole.GrantedByUserId));
        Mapping.Fk<UserRole, User>(b, nameof(UserRole.RevokedByUserId));
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.RoleId);
        b.HasIndex(x => x.RevokedAt);
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasFilter("[revoked_at] IS NULL");
    }
}

internal sealed class CustomerProfileConfiguration() : EntityConfiguration<CustomerProfile>("customer_profiles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerProfile> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(CustomerProfile.UserId), "user_id");
        Mapping.String(b, nameof(CustomerProfile.DisplayName), "display_name", 100);
        Mapping.FullAudit(b);
        Mapping.Fk<CustomerProfile, User>(b, nameof(CustomerProfile.UserId));
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

internal sealed class ProviderProfileConfiguration() : EntityConfiguration<ProviderProfile>("provider_profiles")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderProfile> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderProfile.UserId), "user_id");
        Mapping.String(b, nameof(ProviderProfile.ProviderTypeCode), "provider_type_code", 20, unicode: false, defaultValue: "BUSINESS");
        Mapping.String(b, nameof(ProviderProfile.BusinessName), "business_name", 200);
        Mapping.String(b, nameof(ProviderProfile.BusinessRegistrationNo), "business_registration_no", 32, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.RepresentativeName), "representative_name", 100, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.ContactName), "contact_name", 100, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.BusinessAddress), "business_address", 500, nullable: true);
        Mapping.Binary(b, nameof(ProviderProfile.BusinessAddressEncrypted), "business_address_encrypted");
        Mapping.NullableShort(b, nameof(ProviderProfile.PrivacyProtectionVersion), "privacy_protection_version");
        Mapping.String(b, nameof(ProviderProfile.BusinessTypeText), "business_type_text", 100, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.BusinessItemText), "business_item_text", 100, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.Introduction), "introduction", 1000, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.PublicIntroductionHtml), "public_introduction_html", 8000, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.PublicPhone), "public_phone", 30, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.PublicEmail), "public_email", 320, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.PublicAddress), "public_address", 500, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.PublicBlogUrl), "public_blog_url", 1000, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderProfile.PublicWebsiteUrl), "public_website_url", 1000, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderProfile.PublicLogoUrl), "public_logo_url", 1000, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderProfile.PublicPhotoUrlsJson), "public_photo_urls_json", null, nullable: true);
        Mapping.String(b, nameof(ProviderProfile.ApprovalStatusCode), "approval_status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.String(b, nameof(ProviderProfile.ActivityStatusCode), "activity_status_code", 20, unicode: false, defaultValue: "INACTIVE");
        Mapping.Decimal(b, nameof(ProviderProfile.TrustScore), "trust_score", nullable: true, precision: 9, scale: 4);
        Mapping.DateTime(b, nameof(ProviderProfile.ApprovalDecidedAt), "approval_decided_at", nullable: true);
        Mapping.NullableLong(b, nameof(ProviderProfile.ApprovalDecidedByUserId), "approval_decided_by_user_id");
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderProfile, User>(b, nameof(ProviderProfile.UserId));
        Mapping.Fk<ProviderProfile, User>(b, nameof(ProviderProfile.ApprovalDecidedByUserId));
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => x.BusinessName);
        b.HasIndex(x => x.ProviderTypeCode);
        b.HasIndex(x => x.BusinessRegistrationNo).IsUnique().HasFilter("[business_registration_no] IS NOT NULL");
        b.HasIndex(x => x.ApprovalStatusCode);
        b.HasIndex(x => x.ActivityStatusCode);
        b.HasIndex(x => new { x.ApprovalStatusCode, x.ActivityStatusCode, x.TrustScore }).IsDescending(false, false, true);
        b.ToTable("provider_profiles", t =>
        {
            t.HasCheckConstraint("CK_provider_profiles_approval_status", "[approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
            t.HasCheckConstraint("CK_provider_profiles_activity_status", "[activity_status_code] IN ('ACTIVE','INACTIVE')");
            t.HasCheckConstraint("CK_provider_profiles_provider_type", "[provider_type_code] IN ('BUSINESS','INDIVIDUAL')");
        });
    }
}

internal sealed class ProviderApprovalEventConfiguration() : EntityConfiguration<ProviderApprovalEvent>("provider_approval_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderApprovalEvent> b)
    {
        Mapping.Long(b, nameof(ProviderApprovalEvent.ProviderProfileId), "provider_profile_id");
        Mapping.String(b, nameof(ProviderApprovalEvent.FromStatusCode), "from_status_code", 20, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderApprovalEvent.ToStatusCode), "to_status_code", 20, unicode: false);
        Mapping.String(b, nameof(ProviderApprovalEvent.ActionCode), "action_code", 20, unicode: false);
        Mapping.String(b, nameof(ProviderApprovalEvent.Reason), "reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(ProviderApprovalEvent.DecidedAt), "decided_at", utcDefault: true);
        Mapping.Long(b, nameof(ProviderApprovalEvent.DecidedByUserId), "decided_by_user_id");
        Mapping.Guid(b, nameof(ProviderApprovalEvent.CorrelationId), "correlation_id", nullable: true);
        Mapping.Fk<ProviderApprovalEvent, ProviderProfile>(b, nameof(ProviderApprovalEvent.ProviderProfileId));
        Mapping.Fk<ProviderApprovalEvent, User>(b, nameof(ProviderApprovalEvent.DecidedByUserId));
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.DecidedByUserId);
        b.HasIndex(x => x.CorrelationId);
        b.HasIndex(x => new { x.ProviderProfileId, x.DecidedAt }).IsDescending(false, true);
        b.ToTable("provider_approval_events", t =>
        {
            t.HasCheckConstraint("CK_provider_approval_events_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
            t.HasCheckConstraint("CK_provider_approval_events_to_status", "[to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
            t.HasCheckConstraint("CK_provider_approval_events_action", "[action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME','RESUBMIT')");
        });
    }
}

internal sealed class ProviderServiceCategoryConfiguration() : EntityConfiguration<ProviderServiceCategory>("provider_service_categories")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderServiceCategory> b)
    {
        Mapping.Long(b, nameof(ProviderServiceCategory.ProviderProfileId), "provider_profile_id");
        Mapping.Long(b, nameof(ProviderServiceCategory.CategoryId), "category_id");
        Mapping.String(b, nameof(ProviderServiceCategory.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.Bool(b, nameof(ProviderServiceCategory.IsNationwide), "is_nationwide", false);
        Mapping.DateTime(b, nameof(ProviderServiceCategory.ActivatedAt), "activated_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ProviderServiceCategory.DeactivatedAt), "deactivated_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderServiceCategory, ProviderProfile>(b, nameof(ProviderServiceCategory.ProviderProfileId));
        Mapping.Fk<ProviderServiceCategory, ServiceCategory>(b, nameof(ProviderServiceCategory.CategoryId));
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => new { x.ProviderProfileId, x.CategoryId }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.StatusCode, x.ProviderProfileId });
        b.ToTable("provider_service_categories", t => t.HasCheckConstraint("CK_provider_service_categories_status", "[status_code] IN ('ACTIVE','INACTIVE')"));
    }
}

internal sealed class ProviderServiceApprovalConfiguration() : EntityConfiguration<ProviderServiceApproval>("provider_service_approvals")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderServiceApproval> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderServiceApproval.ProviderServiceCategoryId), "provider_service_category_id");
        Mapping.String(b, nameof(ProviderServiceApproval.ApprovalStatusCode), "approval_status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.DateTime(b, nameof(ProviderServiceApproval.ApprovalRequestedAt), "approval_requested_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ProviderServiceApproval.ApprovalDecidedAt), "approval_decided_at", nullable: true);
        Mapping.NullableLong(b, nameof(ProviderServiceApproval.ApprovalDecidedByUserId), "approval_decided_by_user_id");
        Mapping.String(b, nameof(ProviderServiceApproval.DecisionReason), "decision_reason", 1000, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderServiceApproval, ProviderServiceCategory>(b, nameof(ProviderServiceApproval.ProviderServiceCategoryId));
        Mapping.Fk<ProviderServiceApproval, User>(b, nameof(ProviderServiceApproval.ApprovalDecidedByUserId));
        b.HasIndex(x => x.ProviderServiceCategoryId).IsUnique();
        b.HasIndex(x => x.ApprovalStatusCode);
        b.HasIndex(x => x.ApprovalRequestedAt);
        b.ToTable("provider_service_approvals", t => t.HasCheckConstraint("CK_provider_service_approvals_status", "[approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')"));
    }
}

internal sealed class ProviderServiceApprovalEventConfiguration() : EntityConfiguration<ProviderServiceApprovalEvent>("provider_service_approval_events")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderServiceApprovalEvent> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderServiceApprovalEvent.ProviderServiceCategoryId), "provider_service_category_id");
        Mapping.String(b, nameof(ProviderServiceApprovalEvent.FromStatusCode), "from_status_code", 20, nullable: true, unicode: false);
        Mapping.String(b, nameof(ProviderServiceApprovalEvent.ToStatusCode), "to_status_code", 20, unicode: false);
        Mapping.String(b, nameof(ProviderServiceApprovalEvent.ActionCode), "action_code", 20, unicode: false);
        Mapping.String(b, nameof(ProviderServiceApprovalEvent.DecisionReason), "decision_reason", 1000, nullable: true);
        Mapping.DateTime(b, nameof(ProviderServiceApprovalEvent.DecidedAt), "decided_at", utcDefault: true);
        Mapping.Long(b, nameof(ProviderServiceApprovalEvent.DecidedByUserId), "decided_by_user_id");
        Mapping.DateTime(b, nameof(ProviderServiceApprovalEvent.CreatedAt), "created_at", utcDefault: true);
        Mapping.Fk<ProviderServiceApprovalEvent, ProviderServiceCategory>(b, nameof(ProviderServiceApprovalEvent.ProviderServiceCategoryId));
        Mapping.Fk<ProviderServiceApprovalEvent, User>(b, nameof(ProviderServiceApprovalEvent.DecidedByUserId));
        b.HasIndex(x => x.ProviderServiceCategoryId);
        b.HasIndex(x => x.DecidedByUserId);
        b.HasIndex(x => new { x.ProviderServiceCategoryId, x.DecidedAt }).IsDescending(false, true);
        b.ToTable("provider_service_approval_events", t =>
        {
            t.HasCheckConstraint("CK_provider_service_approval_events_from_status", "[from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
            t.HasCheckConstraint("CK_provider_service_approval_events_to_status", "[to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')");
            t.HasCheckConstraint("CK_provider_service_approval_events_action", "[action_code] IN ('APPROVE','REJECT','SUSPEND','REOPEN')");
        });
    }
}

internal sealed class ProviderServiceAreaConfiguration() : EntityConfiguration<ProviderServiceArea>("provider_service_areas")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderServiceArea> b)
    {
        Mapping.Long(b, nameof(ProviderServiceArea.ProviderServiceCategoryId), "provider_service_category_id");
        Mapping.Long(b, nameof(ProviderServiceArea.AdministrativeAreaId), "administrative_area_id");
        Mapping.String(b, nameof(ProviderServiceArea.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.DateTime(b, nameof(ProviderServiceArea.ActivatedAt), "activated_at", utcDefault: true);
        Mapping.DateTime(b, nameof(ProviderServiceArea.DeactivatedAt), "deactivated_at", nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderServiceArea, ProviderServiceCategory>(b, nameof(ProviderServiceArea.ProviderServiceCategoryId));
        Mapping.Fk<ProviderServiceArea, AdministrativeArea>(b, nameof(ProviderServiceArea.AdministrativeAreaId));
        b.HasIndex(x => x.ProviderServiceCategoryId);
        b.HasIndex(x => x.AdministrativeAreaId);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => new { x.ProviderServiceCategoryId, x.AdministrativeAreaId }).IsUnique();
        b.HasIndex(x => new { x.AdministrativeAreaId, x.StatusCode, x.ProviderServiceCategoryId });
        b.ToTable("provider_service_areas", t => t.HasCheckConstraint("CK_provider_service_areas_status", "[status_code] IN ('ACTIVE','INACTIVE')"));
    }
}

internal sealed class StoredFileConfiguration() : EntityConfiguration<StoredFile>("files")
{
    protected override void ConfigureEntity(EntityTypeBuilder<StoredFile> b)
    {
        Mapping.PublicId(b);
        Mapping.String(b, nameof(StoredFile.PurposeCode), "purpose_code", 30, unicode: false);
        Mapping.String(b, nameof(StoredFile.StorageContainer), "storage_container", 200);
        Mapping.String(b, nameof(StoredFile.StorageKey), "storage_key", 1000);
        b.Property(x => x.StorageKeyHash)
            .HasColumnName("storage_key_hash")
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsFixedLength()
            .IsRequired();
        Mapping.String(b, nameof(StoredFile.OriginalFileName), "original_file_name", 255);
        Mapping.String(b, nameof(StoredFile.ContentType), "content_type", 200, unicode: false);
        Mapping.Long(b, nameof(StoredFile.SizeBytes), "size_bytes");
        Mapping.String(b, nameof(StoredFile.Sha256Hex), "sha256_hex", 64, unicode: false, fixedLength: true);
        Mapping.String(b, nameof(StoredFile.StatusCode), "status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.String(b, nameof(StoredFile.ScanResultText), "scan_result_text", 1000, nullable: true);
        Mapping.String(b, nameof(StoredFile.MalwareScanStatusCode), "malware_scan_status_code", 30, nullable: true, unicode: false);
        Mapping.String(b, nameof(StoredFile.PrivacyInspectionStatusCode), "privacy_inspection_status_code", 30, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(StoredFile.PrivacyInspectedAt), "privacy_inspected_at", nullable: true);
        Mapping.String(b, nameof(StoredFile.PrivacyAdapterVersion), "privacy_adapter_version", 100, nullable: true, unicode: false);
        Mapping.String(b, nameof(StoredFile.PrivacyDetectionTypesJson), "privacy_detection_types_json", null, nullable: true);
        Mapping.String(b, nameof(StoredFile.PrivacyInspectionErrorCode), "privacy_inspection_error_code", 100, nullable: true, unicode: false);
        Mapping.String(b, nameof(StoredFile.SanitizationStatusCode), "sanitization_status_code", 30, nullable: true, unicode: false);
        Mapping.DateTime(b, nameof(StoredFile.SanitizationCompletedAt), "sanitization_completed_at", nullable: true);
        Mapping.DateTime(b, nameof(StoredFile.ActivatedAt), "activated_at", nullable: true);
        Mapping.DateTime(b, nameof(StoredFile.DeletedAt), "deleted_at", nullable: true);
        Mapping.NullableLong(b, nameof(StoredFile.UploadedByUserId), "uploaded_by_user_id");
        Mapping.DateTime(b, nameof(StoredFile.CreatedAt), "created_at", utcDefault: true);
        Mapping.RowVersion(b);
        Mapping.Fk<StoredFile, User>(b, nameof(StoredFile.UploadedByUserId));
        b.HasIndex(x => x.StorageKeyHash).IsUnique();
        b.HasIndex(x => x.PurposeCode);
        b.HasIndex(x => x.ContentType);
        b.HasIndex(x => x.Sha256Hex);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.MalwareScanStatusCode);
        b.HasIndex(x => x.PrivacyInspectionStatusCode);
        b.HasIndex(x => x.SanitizationStatusCode);
        b.HasIndex(x => x.UploadedByUserId);
        b.HasIndex(x => x.CreatedAt);
        b.ToTable("files", t =>
        {
            t.HasCheckConstraint("CK_files_size_bytes", "[size_bytes] >= 0");
            t.HasCheckConstraint("CK_files_purpose", "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO','CHAT_ATTACHMENT','HELP_ROOM_PHOTO','DIRECT_PAYMENT_EVIDENCE')");
            t.HasCheckConstraint("CK_files_status", "[status_code] IN ('PENDING','ACTIVE','QUARANTINED','DELETED')");
            t.HasCheckConstraint("CK_files_malware_scan_status", "[malware_scan_status_code] IS NULL OR [malware_scan_status_code] IN ('NOT_INTEGRATED','PENDING','PROCESSING','CLEAN','INFECTED','FAILED')");
            t.HasCheckConstraint("CK_files_privacy_inspection_status", "[privacy_inspection_status_code] IS NULL OR [privacy_inspection_status_code] IN ('NOT_INTEGRATED','PENDING','PROCESSING','SAFE','SENSITIVE_DETECTED','FAILED')");
            t.HasCheckConstraint("CK_files_sanitization_status", "[sanitization_status_code] IS NULL OR [sanitization_status_code] IN ('NOT_INTEGRATED','NOT_REQUIRED','PENDING','PROCESSING','COMPLETED','FAILED')");
        });
    }
}

internal sealed class StoredFileDerivativeConfiguration() : EntityConfiguration<StoredFileDerivative>("file_derivatives")
{
    protected override void ConfigureEntity(EntityTypeBuilder<StoredFileDerivative> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(StoredFileDerivative.OriginalFileId), "original_file_id");
        Mapping.Long(b, nameof(StoredFileDerivative.DerivedFileId), "derived_file_id");
        Mapping.String(b, nameof(StoredFileDerivative.DerivativeTypeCode), "derivative_type_code", 40, unicode: false);
        Mapping.String(b, nameof(StoredFileDerivative.AdapterVersion), "adapter_version", 100, nullable: true, unicode: false);
        Mapping.CreatedAudit(b);
        Mapping.Fk<StoredFileDerivative, StoredFile>(b, nameof(StoredFileDerivative.OriginalFileId));
        Mapping.Fk<StoredFileDerivative, StoredFile>(b, nameof(StoredFileDerivative.DerivedFileId));
        Mapping.Fk<StoredFileDerivative, User>(b, nameof(StoredFileDerivative.CreatedByUserId));
        b.HasIndex(x => x.OriginalFileId);
        b.HasIndex(x => x.DerivedFileId).IsUnique();
        b.HasIndex(x => new { x.OriginalFileId, x.DerivativeTypeCode });
        b.ToTable("file_derivatives", t =>
        {
            t.HasCheckConstraint("CK_file_derivatives_type", "[derivative_type_code] IN ('PRIVACY_SANITIZED')");
            t.HasCheckConstraint("CK_file_derivatives_distinct", "[original_file_id] <> [derived_file_id]");
        });
    }
}

internal sealed class ProviderDocumentConfiguration() : EntityConfiguration<ProviderDocument>("provider_documents")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderDocument> b)
    {
        Mapping.Long(b, nameof(ProviderDocument.ProviderProfileId), "provider_profile_id");
        Mapping.Long(b, nameof(ProviderDocument.FileId), "file_id");
        Mapping.NullableLong(b, nameof(ProviderDocument.DocumentTypeId), "document_type_id");
        Mapping.String(b, nameof(ProviderDocument.DocumentTypeCode), "document_type_code", 50, unicode: false);
        Mapping.String(b, nameof(ProviderDocument.DocumentNumber), "document_number", 100, nullable: true);
        Mapping.Date(b, nameof(ProviderDocument.IssuedAt), "issued_at", nullable: true);
        Mapping.Date(b, nameof(ProviderDocument.ExpiresAt), "expires_at", nullable: true);
        Mapping.String(b, nameof(ProviderDocument.VerificationStatusCode), "verification_status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.DateTime(b, nameof(ProviderDocument.VerifiedAt), "verified_at", nullable: true);
        Mapping.NullableLong(b, nameof(ProviderDocument.VerifiedByUserId), "verified_by_user_id");
        Mapping.String(b, nameof(ProviderDocument.Note), "note", 1000, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderDocument, ProviderProfile>(b, nameof(ProviderDocument.ProviderProfileId));
        Mapping.Fk<ProviderDocument, StoredFile>(b, nameof(ProviderDocument.FileId));
        Mapping.Fk<ProviderDocument, ProviderDocumentType>(b, nameof(ProviderDocument.DocumentTypeId));
        Mapping.Fk<ProviderDocument, User>(b, nameof(ProviderDocument.VerifiedByUserId));
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.FileId).IsUnique();
        b.HasIndex(x => x.DocumentTypeCode);
        b.HasIndex(x => x.DocumentTypeId);
        b.HasIndex(x => x.ExpiresAt);
        b.HasIndex(x => x.VerificationStatusCode);
    }
}

internal sealed class ProviderServiceRequirementVerificationConfiguration() : EntityConfiguration<ProviderServiceRequirementVerification>("provider_service_requirement_verifications")
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProviderServiceRequirementVerification> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(ProviderServiceRequirementVerification.ProviderServiceCategoryId), "provider_service_category_id");
        Mapping.Long(b, nameof(ProviderServiceRequirementVerification.RequirementAssignmentId), "requirement_assignment_id");
        Mapping.NullableLong(b, nameof(ProviderServiceRequirementVerification.ProviderDocumentId), "provider_document_id");
        Mapping.String(b, nameof(ProviderServiceRequirementVerification.VerificationStatusCode), "verification_status_code", 20, unicode: false, defaultValue: "PENDING");
        Mapping.NullableLong(b, nameof(ProviderServiceRequirementVerification.VerifiedByUserId), "verified_by_user_id");
        Mapping.DateTime(b, nameof(ProviderServiceRequirementVerification.VerifiedAt), "verified_at", nullable: true);
        Mapping.Date(b, nameof(ProviderServiceRequirementVerification.ExpiresAt), "expires_at", nullable: true);
        Mapping.String(b, nameof(ProviderServiceRequirementVerification.RejectionReason), "rejection_reason", 1000, nullable: true);
        Mapping.FullAudit(b);
        Mapping.Fk<ProviderServiceRequirementVerification, ProviderServiceCategory>(b, nameof(ProviderServiceRequirementVerification.ProviderServiceCategoryId));
        Mapping.Fk<ProviderServiceRequirementVerification, CategoryProviderRequirementAssignment>(b, nameof(ProviderServiceRequirementVerification.RequirementAssignmentId));
        Mapping.Fk<ProviderServiceRequirementVerification, ProviderDocument>(b, nameof(ProviderServiceRequirementVerification.ProviderDocumentId));
        Mapping.Fk<ProviderServiceRequirementVerification, User>(b, nameof(ProviderServiceRequirementVerification.VerifiedByUserId));
        b.HasIndex(x => new { x.ProviderServiceCategoryId, x.RequirementAssignmentId }).IsUnique();
        b.HasIndex(x => x.ProviderDocumentId);
        b.HasIndex(x => new { x.VerificationStatusCode, x.ExpiresAt });
    }
}
