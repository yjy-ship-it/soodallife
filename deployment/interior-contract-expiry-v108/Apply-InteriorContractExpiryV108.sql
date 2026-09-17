SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260820123000_AddInteriorContractExpirySafetyV108')
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260820090000_ImproveInteriorPartyContractWorkflowV107') THROW 51800,'V108 requires V107 migration.',1;
  IF OBJECT_ID(N'dbo.interior_projects',N'U') IS NULL OR OBJECT_ID(N'dbo.wallets',N'U') IS NULL OR OBJECT_ID(N'dbo.wallet_ledger',N'U') IS NULL THROW 51801,'V108 requires interior, wallets, and wallet_ledger tables.',1;
  IF COL_LENGTH('dbo.interior_projects','fee_release_ledger_entry_id') IS NULL ALTER TABLE dbo.interior_projects ADD fee_release_ledger_entry_id bigint NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_expiry_phase_code') IS NULL ALTER TABLE dbo.interior_projects ADD contract_expiry_phase_code varchar(40) NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_action_due_at') IS NULL ALTER TABLE dbo.interior_projects ADD contract_action_due_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_expiry_reminder_sent_at') IS NULL ALTER TABLE dbo.interior_projects ADD contract_expiry_reminder_sent_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_expiry_paused_at') IS NULL ALTER TABLE dbo.interior_projects ADD contract_expiry_paused_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_expiry_pause_reason_code') IS NULL ALTER TABLE dbo.interior_projects ADD contract_expiry_pause_reason_code varchar(50) NULL;
  IF COL_LENGTH('dbo.interior_projects','contract_expired_at') IS NULL ALTER TABLE dbo.interior_projects ADD contract_expired_at datetime2 NULL;
  IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_interior_projects_wallet_ledger_fee_release_ledger_entry_id') EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_projects ADD CONSTRAINT FK_interior_projects_wallet_ledger_fee_release_ledger_entry_id FOREIGN KEY(fee_release_ledger_entry_id) REFERENCES dbo.wallet_ledger(id);';
  IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.interior_projects') AND name=N'IX_interior_projects_fee_release_ledger_entry_id') EXEC sys.sp_executesql N'CREATE INDEX IX_interior_projects_fee_release_ledger_entry_id ON dbo.interior_projects(fee_release_ledger_entry_id);';
  IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.interior_projects') AND name=N'IX_interior_projects_contract_action_due_at') EXEC sys.sp_executesql N'CREATE INDEX IX_interior_projects_contract_action_due_at ON dbo.interior_projects(contract_action_due_at) WHERE contract_action_due_at IS NOT NULL;';
  IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.interior_projects') AND name=N'CK_interior_projects_fee') ALTER TABLE dbo.interior_projects DROP CONSTRAINT CK_interior_projects_fee;
  EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_projects ADD CONSTRAINT CK_interior_projects_fee CHECK(fee_assessment_status_code IN (''POLICY_PENDING'',''PENDING_SELECTION'',''NOT_APPLICABLE'',''RESERVED_PENDING_CONTRACT'',''RELEASED'',''ASSESSED''));';
  EXEC sys.sp_executesql N'UPDATE p SET contract_expiry_phase_code=CASE WHEN p.current_contract_id IS NULL THEN ''PROVIDER_SUBMISSION'' ELSE ''CUSTOMER_REVIEW'' END,contract_action_due_at=DATEADD(day,7,COALESCE(c.registered_by_provider_at,p.fee_reserved_at,p.updated_at)),contract_expiry_paused_at=CASE WHEN c.status_code=''CORRECTION_REQUIRED'' THEN SYSUTCDATETIME() ELSE NULL END,contract_expiry_pause_reason_code=CASE WHEN c.status_code=''CORRECTION_REQUIRED'' THEN ''CUSTOMER_CORRECTION_REQUEST'' ELSE NULL END FROM dbo.interior_projects p LEFT JOIN dbo.interior_contracts c ON c.id=p.current_contract_id WHERE p.fee_assessment_status_code=''RESERVED_PENDING_CONTRACT'' AND p.contract_action_due_at IS NULL;';
  DECLARE @templates TABLE(template_code varchar(100),[name] nvarchar(200),audience varchar(30),event_type varchar(100),title nvarchar(300),body nvarchar(3000));
  INSERT INTO @templates VALUES
  ('INTERIOR_CONTRACT_DEADLINE_CUSTOMER_WEB',N'인테리어 계약 확인 기한 안내','CUSTOMER','INTERIOR_CONTRACT_DEADLINE_REMINDER',N'계약 확인 기한이 2일 남았습니다',N'등록된 계약자료를 확인해 주세요. 기한 내 확인하지 않으면 계약 확인 기한이 만료되고 예약 수수료가 해제됩니다.'),
  ('INTERIOR_CONTRACT_DEADLINE_PROVIDER_WEB',N'인테리어 계약 처리 기한 안내','PROVIDER','INTERIOR_CONTRACT_DEADLINE_REMINDER',N'계약 처리 기한이 2일 남았습니다',N'계약자료 제출 또는 고객 확인 상태를 확인해 주세요. 기한이 지나면 공급자 선택과 예약 수수료가 해제될 수 있습니다.'),
  ('INTERIOR_CONTRACT_RELEASE_CUSTOMER_WEB',N'인테리어 예약 해제 안내','CUSTOMER','INTERIOR_CONTRACT_RESERVATION_RELEASED',N'공급자 선택 예약이 해제되었습니다',N'계약 확인 전 예약이 종료되어 공급자 선택이 해제되었습니다. 필요한 경우 다른 견적을 다시 선택할 수 있습니다.'),
  ('INTERIOR_CONTRACT_RELEASE_PROVIDER_WEB',N'인테리어 예약금 해제 안내','PROVIDER','INTERIOR_CONTRACT_RESERVATION_RELEASED',N'예상 수수료 예약이 해제되었습니다',N'계약 확인 전 예약이 종료되어 예약된 예상 수수료가 충전금 사용 가능 잔액으로 반환되었습니다.');
  INSERT dbo.notification_templates(public_id,template_code,[name],[description],audience_type_code,event_type_code,channel_code,title_template,body_template,allowed_variables_json,is_required_business_notice,is_marketing,is_active,effective_from,created_at,updated_at)
  SELECT NEWID(),t.template_code,t.[name],N'인테리어 계약 7일 확인·자동 해제 운영 알림',t.audience,t.event_type,'WEB',t.title,t.body,N'[]',1,0,1,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME() FROM @templates t WHERE NOT EXISTS(SELECT 1 FROM dbo.notification_templates n WHERE n.template_code=t.template_code AND n.channel_code='WEB');
  INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260820123000_AddInteriorContractExpirySafetyV108',N'10.0.10');
 END;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
