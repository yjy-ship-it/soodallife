SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.site_visit_proposals',N'U') IS NULL THROW 51000,'site_visit_proposals is missing',1;
IF OBJECT_ID(N'dbo.site_visit_events',N'U') IS NULL THROW 51000,'site_visit_events is missing',1;
IF COL_LENGTH(N'dbo.site_visit_proposals',N'payment_instruction_protected') IS NULL THROW 51000,'protected payment instruction column is missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.site_visit_proposals') AND name=N'UX_site_visit_dispatch' AND is_unique=1) THROW 51000,'unique dispatch index is missing',1;
SELECT 'V132_OK' verification_result,(SELECT COUNT_BIG(*) FROM dbo.site_visit_proposals) proposal_count,(SELECT COUNT_BIG(*) FROM dbo.site_visit_events) event_count;
