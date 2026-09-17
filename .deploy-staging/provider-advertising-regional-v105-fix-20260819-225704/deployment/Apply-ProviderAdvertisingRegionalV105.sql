SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819132801_AddProviderAdvertisingRegionalPricingV105')
 BEGIN
  IF OBJECT_ID(N'dbo.provider_advertising_rate_policies',N'U') IS NULL OR OBJECT_ID(N'dbo.provider_advertising_applications',N'U') IS NULL THROW 51500,'V99 provider advertising tables are required.',1;
  IF COL_LENGTH('dbo.provider_advertising_rate_policies','province_unit_amount') IS NULL ALTER TABLE dbo.provider_advertising_rate_policies ADD province_unit_amount decimal(19,4) NOT NULL CONSTRAINT DF_provider_ad_rate_province_unit DEFAULT(10000);
  IF COL_LENGTH('dbo.provider_advertising_rate_policies','district_unit_amount') IS NULL ALTER TABLE dbo.provider_advertising_rate_policies ADD district_unit_amount decimal(19,4) NOT NULL CONSTRAINT DF_provider_ad_rate_district_unit DEFAULT(2000);
  IF COL_LENGTH('dbo.provider_advertising_rate_policies','regional_fee_cap_amount') IS NULL ALTER TABLE dbo.provider_advertising_rate_policies ADD regional_fee_cap_amount decimal(19,4) NOT NULL CONSTRAINT DF_provider_ad_rate_region_cap DEFAULT(30000);
  IF COL_LENGTH('dbo.provider_advertising_applications','base_fee_amount') IS NULL ALTER TABLE dbo.provider_advertising_applications ADD base_fee_amount decimal(19,4) NOT NULL CONSTRAINT DF_provider_ad_app_base_fee DEFAULT(0);
  IF COL_LENGTH('dbo.provider_advertising_applications','regional_fee_amount') IS NULL ALTER TABLE dbo.provider_advertising_applications ADD regional_fee_amount decimal(19,4) NOT NULL CONSTRAINT DF_provider_ad_app_region_fee DEFAULT(0);
  IF COL_LENGTH('dbo.provider_advertising_applications','province_target_count') IS NULL ALTER TABLE dbo.provider_advertising_applications ADD province_target_count int NOT NULL CONSTRAINT DF_provider_ad_app_province_count DEFAULT(0);
  IF COL_LENGTH('dbo.provider_advertising_applications','district_target_count') IS NULL ALTER TABLE dbo.provider_advertising_applications ADD district_target_count int NOT NULL CONSTRAINT DF_provider_ad_app_district_count DEFAULT(0);
  EXEC sys.sp_executesql N'UPDATE dbo.provider_advertising_applications SET base_fee_amount=fee_amount WHERE base_fee_amount=0;';
  EXEC sys.sp_executesql N'UPDATE dbo.provider_advertising_rate_policies SET province_unit_amount=10000,district_unit_amount=2000,regional_fee_cap_amount=30000 WHERE province_unit_amount=0 AND district_unit_amount=0 AND regional_fee_cap_amount=0;';
  IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.provider_advertising_rate_policies') AND name=N'CK_provider_ad_rate_amount') ALTER TABLE dbo.provider_advertising_rate_policies DROP CONSTRAINT CK_provider_ad_rate_amount;
  EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_rate_policies ADD CONSTRAINT CK_provider_ad_rate_amount CHECK(fixed_amount>0 AND province_unit_amount>=0 AND district_unit_amount>=0 AND regional_fee_cap_amount>=0);';
  IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.provider_advertising_applications') AND name=N'CK_provider_ad_app_amount') ALTER TABLE dbo.provider_advertising_applications DROP CONSTRAINT CK_provider_ad_app_amount;
  EXEC sys.sp_executesql N'ALTER TABLE dbo.provider_advertising_applications ADD CONSTRAINT CK_provider_ad_app_amount CHECK(fee_amount>0 AND base_fee_amount>=0 AND regional_fee_amount>=0 AND province_target_count>=0 AND district_target_count>=0);';
  INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260819132801_AddProviderAdvertisingRegionalPricingV105',N'10.0.10');
 END;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
