SET NOCOUNT ON;
IF COL_LENGTH('dbo.service_categories','search_slug') IS NULL THROW 51000, 'V124 service SEO columns missing.', 1;
IF COL_LENGTH('dbo.wallet_ledger','tax_treatment_code') IS NULL THROW 51000, 'V124 wallet tax columns missing.', 1;
IF COL_LENGTH('dbo.quote_fee_reservations','expected_vat_amount') IS NULL THROW 51000, 'V124 reservation VAT columns missing.', 1;
IF COL_LENGTH('dbo.fee_charges','vat_amount') IS NULL THROW 51000, 'V124 fee VAT columns missing.', 1;
IF EXISTS (SELECT search_slug FROM dbo.service_categories WHERE search_slug IS NOT NULL GROUP BY search_slug HAVING COUNT(*)>1) THROW 51000, 'Duplicate service search slugs.', 1;
IF EXISTS (SELECT 1 FROM dbo.wallet_ledger WHERE tax_treatment_code IS NULL) THROW 51000, 'Wallet tax treatment is null.', 1;
SELECT 'V124_OK' verification_result,
       (SELECT COUNT(*) FROM dbo.service_categories WHERE level_code='SERVICE' AND is_search_indexable=1) indexable_services,
       (SELECT COUNT(*) FROM dbo.wallet_ledger WHERE tax_treatment_code='TAXABLE_VAT_INCLUDED') taxable_wallet_entries,
       (SELECT COUNT(*) FROM dbo.fee_charges WHERE is_vat_included=1) vat_included_fee_charges;
GO
