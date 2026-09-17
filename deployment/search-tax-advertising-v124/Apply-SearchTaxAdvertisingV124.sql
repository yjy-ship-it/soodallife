SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.service_categories', 'search_keywords_text') IS NULL
    ALTER TABLE dbo.service_categories ADD search_keywords_text nvarchar(2000) NULL;
IF COL_LENGTH('dbo.service_categories', 'search_slug') IS NULL
    ALTER TABLE dbo.service_categories ADD search_slug varchar(220) NULL;
IF COL_LENGTH('dbo.service_categories', 'seo_title') IS NULL
    ALTER TABLE dbo.service_categories ADD seo_title nvarchar(200) NULL;
IF COL_LENGTH('dbo.service_categories', 'seo_description') IS NULL
    ALTER TABLE dbo.service_categories ADD seo_description nvarchar(500) NULL;
IF COL_LENGTH('dbo.service_categories', 'is_search_indexable') IS NULL
    ALTER TABLE dbo.service_categories ADD is_search_indexable bit NOT NULL CONSTRAINT DF_service_categories_search_indexable DEFAULT(1);

EXEC(N'UPDATE dbo.service_categories
SET search_slug = ''service-'' + LOWER(REPLACE(CONVERT(varchar(36), public_id), ''-'', '''')),
    seo_title = COALESCE(seo_title, name + N'' 견적·가격 안내 | 수달 라이프''),
    seo_description = COALESCE(seo_description, name + N'' 서비스의 이용 조건, 참고 가격과 견적 요청 방법을 확인하세요.'')
WHERE level_code = ''SERVICE'' AND search_slug IS NULL;');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.service_categories') AND name = 'IX_service_categories_search_slug')
    EXEC(N'CREATE UNIQUE INDEX IX_service_categories_search_slug ON dbo.service_categories(search_slug) WHERE search_slug IS NOT NULL;');

IF COL_LENGTH('dbo.wallet_ledger', 'supply_amount') IS NULL
    ALTER TABLE dbo.wallet_ledger ADD supply_amount decimal(19,4) NOT NULL CONSTRAINT DF_wallet_ledger_supply DEFAULT(0);
IF COL_LENGTH('dbo.wallet_ledger', 'vat_amount') IS NULL
    ALTER TABLE dbo.wallet_ledger ADD vat_amount decimal(19,4) NOT NULL CONSTRAINT DF_wallet_ledger_vat DEFAULT(0);
IF COL_LENGTH('dbo.wallet_ledger', 'tax_treatment_code') IS NULL
    ALTER TABLE dbo.wallet_ledger ADD tax_treatment_code varchar(30) NOT NULL CONSTRAINT DF_wallet_ledger_tax DEFAULT('LEGACY_UNSPLIT');

-- wallet_ledger is append-only and protected by TR_wallet_ledger_append_only.
-- Existing rows keep LEGACY_UNSPLIT; newly appended rows receive the precise
-- treatment, supply amount and VAT from the application services.

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_wallet_ledger_tax_treatment')
    EXEC(N'ALTER TABLE dbo.wallet_ledger ADD CONSTRAINT CK_wallet_ledger_tax_treatment CHECK (tax_treatment_code IN (''DEPOSIT'',''EXPECTED_VAT_INCLUDED'',''EXPECTED_REVERSED'',''TAXABLE_VAT_INCLUDED'',''TAX_REVERSED'',''NON_TAXABLE'',''LEGACY_UNSPLIT''));');

IF COL_LENGTH('dbo.quote_fee_reservations', 'expected_supply_amount') IS NULL
    ALTER TABLE dbo.quote_fee_reservations ADD expected_supply_amount decimal(19,4) NOT NULL CONSTRAINT DF_quote_fee_reservations_supply DEFAULT(0);
IF COL_LENGTH('dbo.quote_fee_reservations', 'expected_vat_amount') IS NULL
    ALTER TABLE dbo.quote_fee_reservations ADD expected_vat_amount decimal(19,4) NOT NULL CONSTRAINT DF_quote_fee_reservations_vat DEFAULT(0);
EXEC(N'UPDATE dbo.quote_fee_reservations SET expected_supply_amount=ROUND(amount/1.1,0), expected_vat_amount=amount-ROUND(amount/1.1,0);');

IF COL_LENGTH('dbo.fee_charges', 'supply_amount') IS NULL
    ALTER TABLE dbo.fee_charges ADD supply_amount decimal(19,4) NOT NULL CONSTRAINT DF_fee_charges_supply DEFAULT(0);
IF COL_LENGTH('dbo.fee_charges', 'vat_amount') IS NULL
    ALTER TABLE dbo.fee_charges ADD vat_amount decimal(19,4) NOT NULL CONSTRAINT DF_fee_charges_vat DEFAULT(0);
IF COL_LENGTH('dbo.fee_charges', 'is_vat_included') IS NULL
    ALTER TABLE dbo.fee_charges ADD is_vat_included bit NOT NULL CONSTRAINT DF_fee_charges_vat_included DEFAULT(1);
EXEC(N'UPDATE dbo.fee_charges SET supply_amount=ROUND(fee_amount/1.1,0), vat_amount=fee_amount-ROUND(fee_amount/1.1,0), is_vat_included=1;');

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260820210000_AddSearchTaxAdvertisingV124')
    INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260820210000_AddSearchTaxAdvertisingV124',N'10.0.10');

COMMIT;
GO
