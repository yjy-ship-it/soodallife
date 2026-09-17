SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819141811_AddProviderAdvertisingAutoRenewalV106')
 BEGIN
  IF OBJECT_ID(N'dbo.provider_advertising_applications',N'U') IS NULL OR OBJECT_ID(N'dbo.provider_advertising_rate_policies',N'U') IS NULL OR OBJECT_ID(N'dbo.wallet_ledger',N'U') IS NULL THROW 51600,'V105 provider advertising and wallet tables are required.',1;
  IF COL_LENGTH('dbo.provider_advertising_applications','auto_renew_disabled_at') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD auto_renew_disabled_at datetime2(7) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','auto_renew_enabled') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD auto_renew_enabled bit NOT NULL CONSTRAINT DF_provider_ad_app_auto_renew_enabled DEFAULT(0);';
  IF COL_LENGTH('dbo.provider_advertising_applications','auto_renew_status_code') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD auto_renew_status_code varchar(30) NOT NULL CONSTRAINT DF_provider_ad_app_auto_renew_status DEFAULT(''OFF'');';
  IF COL_LENGTH('dbo.provider_advertising_applications','last_renewed_at') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD last_renewed_at datetime2(7) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','next_renewal_at') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD next_renewal_at datetime2(7) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_consent_at') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_consent_at datetime2(7) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_consent_fee_amount') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_consent_fee_amount decimal(19,4) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_consent_policy_fingerprint') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_consent_policy_fingerprint varchar(128) NULL;';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_consent_required') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_consent_required bit NOT NULL CONSTRAINT DF_provider_ad_app_renewal_consent_required DEFAULT(0);';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_cycle_no') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_cycle_no int NOT NULL CONSTRAINT DF_provider_ad_app_renewal_cycle DEFAULT(0);';
  IF COL_LENGTH('dbo.provider_advertising_applications','renewal_notice_sent_at') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD renewal_notice_sent_at datetime2(7) NULL;';
  IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.provider_advertising_applications') AND name=N'CK_provider_ad_app_auto_renew_status') EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD CONSTRAINT CK_provider_ad_app_auto_renew_status CHECK(auto_renew_status_code IN (''OFF'',''PENDING_PUBLICATION'',''ACTIVE'',''CONSENT_REQUIRED'',''PAUSED_INSUFFICIENT'',''CANCELLED''));';
  IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.provider_advertising_applications') AND name=N'CK_provider_ad_app_renewal_cycle') EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD CONSTRAINT CK_provider_ad_app_renewal_cycle CHECK(renewal_cycle_no>=0);';
  IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.provider_advertising_applications') AND name=N'IX_provider_advertising_applications_auto_renew_enabled_auto_renew_status_code_next_renewal_at') EXEC sys.sp_executesql N'CREATE INDEX IX_provider_advertising_applications_auto_renew_enabled_auto_renew_status_code_next_renewal_at ON dbo.provider_advertising_applications(auto_renew_enabled,auto_renew_status_code,next_renewal_at);';
  IF OBJECT_ID(N'dbo.provider_advertising_renewal_history',N'U') IS NULL EXEC sys.sp_executesql N'
   CREATE TABLE dbo.provider_advertising_renewal_history(
    id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_provider_advertising_renewal_history PRIMARY KEY,
    public_id uniqueidentifier NOT NULL,
    provider_advertising_application_id bigint NOT NULL,
    cycle_no int NOT NULL,
    due_at datetime2(7) NOT NULL,
    base_fee_amount decimal(19,4) NOT NULL,
    regional_fee_amount decimal(19,4) NOT NULL,
    fee_amount decimal(19,4) NOT NULL,
    currency_code char(3) NOT NULL,
    status_code varchar(30) NOT NULL,
    reserve_ledger_entry_id bigint NULL,
    capture_ledger_entry_id bigint NULL,
    notice_sent_at datetime2(7) NULL,
    processed_at datetime2(7) NULL,
    failure_reason nvarchar(1000) NULL,
    policy_fingerprint varchar(128) NOT NULL,
    created_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_renewal_created DEFAULT(SYSUTCDATETIME()),
    updated_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_renewal_updated DEFAULT(SYSUTCDATETIME()),
    row_version rowversion NOT NULL,
    created_by_user_id bigint NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT CK_provider_ad_renewal_amount CHECK(fee_amount>0 AND base_fee_amount>=0 AND regional_fee_amount>=0),
    CONSTRAINT CK_provider_ad_renewal_cycle CHECK(cycle_no>0),
    CONSTRAINT CK_provider_ad_renewal_status CHECK(status_code IN (''PENDING'',''NOTICE_SENT'',''RENEWED'',''PAUSED_INSUFFICIENT'',''CONSENT_REQUIRED'',''CANCELLED'')),
    CONSTRAINT FK_provider_advertising_renewal_history_provider_advertising_applications_provider_advertising_application_id FOREIGN KEY(provider_advertising_application_id) REFERENCES dbo.provider_advertising_applications(id),
    CONSTRAINT FK_provider_advertising_renewal_history_wallet_ledger_reserve_ledger_entry_id FOREIGN KEY(reserve_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
    CONSTRAINT FK_provider_advertising_renewal_history_wallet_ledger_capture_ledger_entry_id FOREIGN KEY(capture_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
    CONSTRAINT FK_provider_advertising_renewal_history_users_created_by_user_id FOREIGN KEY(created_by_user_id) REFERENCES dbo.users(id),
    CONSTRAINT FK_provider_advertising_renewal_history_users_updated_by_user_id FOREIGN KEY(updated_by_user_id) REFERENCES dbo.users(id)
   );
   CREATE UNIQUE INDEX IX_provider_advertising_renewal_history_public_id ON dbo.provider_advertising_renewal_history(public_id);
   CREATE UNIQUE INDEX IX_provider_advertising_renewal_history_provider_advertising_application_id_cycle_no ON dbo.provider_advertising_renewal_history(provider_advertising_application_id,cycle_no);
   CREATE INDEX IX_provider_advertising_renewal_history_status_code_due_at ON dbo.provider_advertising_renewal_history(status_code,due_at);
   CREATE INDEX IX_provider_advertising_renewal_history_reserve_ledger_entry_id ON dbo.provider_advertising_renewal_history(reserve_ledger_entry_id);
   CREATE INDEX IX_provider_advertising_renewal_history_capture_ledger_entry_id ON dbo.provider_advertising_renewal_history(capture_ledger_entry_id);
   CREATE INDEX IX_provider_advertising_renewal_history_created_by_user_id ON dbo.provider_advertising_renewal_history(created_by_user_id);
   CREATE INDEX IX_provider_advertising_renewal_history_updated_by_user_id ON dbo.provider_advertising_renewal_history(updated_by_user_id);';
  DECLARE @templates TABLE(template_code varchar(100),[name] nvarchar(200),event_type_code varchar(100),title_template nvarchar(300),body_template nvarchar(3000));
  INSERT @templates VALUES
   ('PROVIDER_AD_RENEWAL_NOTICE_WEB',N'광고 자동 갱신 사전 안내','PROVIDER_AD_RENEWAL_NOTICE',N'다음 달 광고비를 안내드립니다',N'광고 자동 갱신 {{cycle}}회차 예정 금액은 {{amount}}원입니다. 갱신 전 충전금 잔액을 확인해 주세요.'),
   ('PROVIDER_AD_RENEWAL_RECONSENT_WEB',N'광고 자동 갱신 정책 재동의','PROVIDER_AD_RENEWAL_RECONSENT_REQUIRED',N'광고 자동 갱신 재동의가 필요합니다',N'노출 지역·위치 또는 요금 정책이 변경되었습니다. 변경된 월 광고비 {{amount}}원을 확인하고 재동의해 주세요.'),
   ('PROVIDER_AD_RENEWAL_BALANCE_WEB',N'광고 자동 갱신 잔액 부족','PROVIDER_AD_RENEWAL_PAUSED_INSUFFICIENT',N'충전금 부족으로 광고 게시가 중지되었습니다',N'자동 갱신 광고비 {{amount}}원보다 충전금 잔액이 부족하여 게시를 중지했습니다. 충전 후 자동 갱신을 다시 설정해 주세요.'),
   ('PROVIDER_AD_RENEWED_WEB',N'광고 자동 갱신 완료','PROVIDER_AD_RENEWED',N'광고가 자동 갱신되었습니다',N'광고 자동 갱신 {{cycle}}회차가 완료되어 충전금에서 {{amount}}원이 차감되었습니다.');
  INSERT dbo.notification_templates(public_id,template_code,[name],[description],audience_type_code,event_type_code,channel_code,title_template,body_template,allowed_variables_json,is_required_business_notice,is_marketing,is_active,effective_from,created_at,updated_at)
  SELECT NEWID(),t.template_code,t.[name],N'공급자 광고 월 자동 갱신 운영 알림','PROVIDER',t.event_type_code,'WEB',t.title_template,t.body_template,N'["source_no","amount","cycle"]',1,0,1,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME() FROM @templates t
  WHERE NOT EXISTS(SELECT 1 FROM dbo.notification_templates n WHERE n.template_code=t.template_code AND n.channel_code='WEB');
  INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260819141811_AddProviderAdvertisingAutoRenewalV106',N'10.0.10');
 END;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
