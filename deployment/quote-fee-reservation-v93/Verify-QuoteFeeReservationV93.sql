SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.quote_fee_reservations',N'U') IS NULL THROW 51000,'quote_fee_reservations table is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260818101920_AddQuoteFeeReservations') THROW 51000,'Quote fee reservation migration history is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.wallet_ledger') AND name=N'CK_wallet_ledger_entry_type' AND definition LIKE '%RESERVE%' AND definition LIKE '%RELEASE%') THROW 51000,'Wallet ledger reservation entry types are missing.',1;
SELECT
 (SELECT COUNT(*) FROM dbo.quote_fee_reservations WHERE status_code='RESERVED') AS ReservedCount,
 (SELECT COUNT(*) FROM dbo.quote_fee_reservations WHERE status_code='CAPTURED') AS CapturedCount,
 (SELECT COUNT(*) FROM dbo.quote_fee_reservations WHERE status_code='RELEASED') AS ReleasedCount,
 (SELECT COALESCE(SUM(reserved_balance),0) FROM dbo.wallets) AS TotalReservedBalance;
