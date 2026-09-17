SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819141811_AddProviderAdvertisingAutoRenewalV106') THROW 51610,'V106 migration missing.',1;
IF COL_LENGTH('dbo.provider_advertising_applications','auto_renew_enabled') IS NULL OR COL_LENGTH('dbo.provider_advertising_applications','next_renewal_at') IS NULL OR COL_LENGTH('dbo.provider_advertising_applications','renewal_consent_policy_fingerprint') IS NULL OR COL_LENGTH('dbo.provider_advertising_applications','renewal_cycle_no') IS NULL THROW 51611,'V106 application renewal columns missing.',1;
IF OBJECT_ID(N'dbo.provider_advertising_renewal_history',N'U') IS NULL THROW 51612,'V106 renewal history table missing.',1;
IF (SELECT COUNT(*) FROM dbo.notification_templates WHERE template_code IN ('PROVIDER_AD_RENEWAL_NOTICE_WEB','PROVIDER_AD_RENEWAL_RECONSENT_WEB','PROVIDER_AD_RENEWAL_BALANCE_WEB','PROVIDER_AD_RENEWED_WEB') AND is_active=1)<4 THROW 51613,'V106 renewal notification templates missing.',1;
IF EXISTS(SELECT 1 FROM dbo.provider_advertising_applications WHERE auto_renew_enabled=1 AND duration_days<>30) THROW 51614,'Invalid non-monthly auto renewal application.',1;
IF EXISTS(SELECT 1 FROM dbo.provider_advertising_renewal_history WHERE fee_amount<>base_fee_amount+regional_fee_amount OR cycle_no<=0) THROW 51615,'Invalid renewal ledger snapshot.',1;
SELECT N'V106_OK' verification_result,
 (SELECT COUNT(*) FROM dbo.provider_advertising_applications WHERE auto_renew_enabled=1) auto_renew_application_count,
 (SELECT COUNT(*) FROM dbo.provider_advertising_renewal_history) renewal_history_count,
 (SELECT COUNT(*) FROM dbo.notification_templates WHERE template_code LIKE 'PROVIDER_AD_RENEW%') active_notification_template_count;
GO
