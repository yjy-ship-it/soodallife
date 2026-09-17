SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260820090000_ImproveInteriorPartyContractWorkflowV107')
 BEGIN
  IF OBJECT_ID(N'dbo.interior_projects',N'U') IS NULL OR OBJECT_ID(N'dbo.interior_contracts',N'U') IS NULL OR OBJECT_ID(N'dbo.wallets',N'U') IS NULL OR OBJECT_ID(N'dbo.wallet_ledger',N'U') IS NULL THROW 51700,'V107 requires interior, wallets, and wallet_ledger tables.',1;
  IF COL_LENGTH('dbo.interior_projects','reserved_fee_amount') IS NULL ALTER TABLE dbo.interior_projects ADD reserved_fee_amount decimal(18,2) NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_reservation_wallet_id') IS NULL ALTER TABLE dbo.interior_projects ADD fee_reservation_wallet_id bigint NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_reservation_ledger_entry_id') IS NULL ALTER TABLE dbo.interior_projects ADD fee_reservation_ledger_entry_id bigint NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_reserved_at') IS NULL ALTER TABLE dbo.interior_projects ADD fee_reserved_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_captured_at') IS NULL ALTER TABLE dbo.interior_projects ADD fee_captured_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_released_at') IS NULL ALTER TABLE dbo.interior_projects ADD fee_released_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_projects','fee_release_reason_code') IS NULL ALTER TABLE dbo.interior_projects ADD fee_release_reason_code varchar(50) NULL;
  IF COL_LENGTH('dbo.interior_contracts','contract_signed_date') IS NULL ALTER TABLE dbo.interior_contracts ADD contract_signed_date date NULL;
  IF COL_LENGTH('dbo.interior_contracts','registered_by_provider_at') IS NULL ALTER TABLE dbo.interior_contracts ADD registered_by_provider_at datetime2 NULL;
  IF COL_LENGTH('dbo.interior_contracts','provider_declaration_text') IS NULL ALTER TABLE dbo.interior_contracts ADD provider_declaration_text nvarchar(1000) NULL;
  IF COL_LENGTH('dbo.interior_contracts','customer_mismatch_reason') IS NULL ALTER TABLE dbo.interior_contracts ADD customer_mismatch_reason nvarchar(2000) NULL;
  EXEC sys.sp_executesql N'UPDATE dbo.interior_contracts SET contract_signed_date=CONVERT(date,created_at),registered_by_provider_at=created_at WHERE contract_signed_date IS NULL OR registered_by_provider_at IS NULL;';
  EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_contracts ALTER COLUMN contract_signed_date date NOT NULL;';
  EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_contracts ALTER COLUMN registered_by_provider_at datetime2 NOT NULL;';
  IF OBJECT_ID(N'dbo.interior_contract_documents',N'U') IS NULL
  BEGIN
   CREATE TABLE dbo.interior_contract_documents(id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_interior_contract_documents PRIMARY KEY,public_id uniqueidentifier NOT NULL CONSTRAINT DF_interior_contract_documents_public_id DEFAULT NEWSEQUENTIALID(),interior_contract_id bigint NOT NULL,file_id bigint NOT NULL,display_order int NOT NULL,is_current bit NOT NULL CONSTRAINT DF_interior_contract_documents_is_current DEFAULT(1),created_at datetime2 NOT NULL CONSTRAINT DF_interior_contract_documents_created_at DEFAULT SYSUTCDATETIME(),created_by_user_id bigint NULL,CONSTRAINT FK_interior_contract_documents_contract FOREIGN KEY(interior_contract_id) REFERENCES dbo.interior_contracts(id),CONSTRAINT FK_interior_contract_documents_file FOREIGN KEY(file_id) REFERENCES dbo.files(id),CONSTRAINT FK_interior_contract_documents_user FOREIGN KEY(created_by_user_id) REFERENCES dbo.users(id),CONSTRAINT UQ_interior_contract_documents UNIQUE(interior_contract_id,file_id));
   CREATE UNIQUE INDEX IX_interior_contract_documents_public_id ON dbo.interior_contract_documents(public_id);CREATE INDEX IX_interior_contract_documents_file_id ON dbo.interior_contract_documents(file_id);
  END;
  IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_interior_projects_wallets_fee_reservation_wallet_id') EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_projects ADD CONSTRAINT FK_interior_projects_wallets_fee_reservation_wallet_id FOREIGN KEY(fee_reservation_wallet_id) REFERENCES dbo.wallets(id);';
  IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_interior_projects_wallet_ledger_fee_reservation_ledger_entry_id') EXEC sys.sp_executesql N'ALTER TABLE dbo.interior_projects ADD CONSTRAINT FK_interior_projects_wallet_ledger_fee_reservation_ledger_entry_id FOREIGN KEY(fee_reservation_ledger_entry_id) REFERENCES dbo.wallet_ledger(id);';
  IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.interior_projects') AND name=N'IX_interior_projects_fee_reservation_wallet_id') EXEC sys.sp_executesql N'CREATE INDEX IX_interior_projects_fee_reservation_wallet_id ON dbo.interior_projects(fee_reservation_wallet_id);';
  IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.interior_projects') AND name=N'IX_interior_projects_fee_reservation_ledger_entry_id') EXEC sys.sp_executesql N'CREATE INDEX IX_interior_projects_fee_reservation_ledger_entry_id ON dbo.interior_projects(fee_reservation_ledger_entry_id);';
  UPDATE dbo.fee_policies SET charge_timing_text=N'고객·공급자가 체결한 계약자료를 양쪽이 최종 확인할 때',restore_rule_text=N'계약 확인 전 취소·허위/중복 요청·부적격 매칭·시스템 오류·관리자 승인 예외 사유',updated_at=SYSUTCDATETIME() WHERE code='FEE-I1';
  UPDATE c SET charge_timing_text=N'고객·공급자가 체결한 계약자료를 양쪽이 최종 확인할 때',restore_rule_text=N'계약 확인 전 취소·허위/중복 요청·부적격 매칭·시스템 오류·관리자 승인 예외 사유',updated_at=SYSUTCDATETIME() FROM dbo.category_fee_policies c INNER JOIN dbo.fee_policies f ON c.source_fee_policy_id=f.id WHERE f.code='FEE-I1';
  INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260820090000_ImproveInteriorPartyContractWorkflowV107',N'10.0.10');
 END;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
