SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260820090000_ImproveInteriorPartyContractWorkflowV107') THROW 51710,'V107 migration history is missing.',1;
IF OBJECT_ID(N'dbo.interior_contract_documents',N'U') IS NULL THROW 51711,'Contract document table is missing.',1;
IF COL_LENGTH('dbo.interior_projects','reserved_fee_amount') IS NULL OR COL_LENGTH('dbo.interior_projects','fee_captured_at') IS NULL THROW 51712,'Interior fee reservation columns are missing.',1;
IF COL_LENGTH('dbo.interior_contracts','contract_signed_date') IS NULL OR COL_LENGTH('dbo.interior_contracts','customer_mismatch_reason') IS NULL THROW 51713,'Party contract columns are missing.',1;
IF EXISTS(SELECT 1 FROM dbo.interior_contracts WHERE contract_signed_date IS NULL OR registered_by_provider_at IS NULL) THROW 51714,'Required contract registration values are null.',1;
SELECT N'V107_OK' AS verification_result,(SELECT COUNT(*) FROM dbo.interior_contract_documents) AS contract_document_count,(SELECT COUNT(*) FROM dbo.interior_projects WHERE fee_assessment_status_code='RESERVED_PENDING_CONTRACT') AS pending_fee_reservations,(SELECT COUNT(*) FROM dbo.interior_projects WHERE fee_captured_at IS NOT NULL) AS captured_interior_fees;
GO
