SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @MigrationId nvarchar(150)=N'20260818101920_AddQuoteFeeReservations';
IF EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=@MigrationId)
BEGIN SELECT @MigrationId AS MigrationId,N'ALREADY_APPLIED' AS Result; RETURN; END;
IF OBJECT_ID(N'dbo.wallets',N'U') IS NULL OR OBJECT_ID(N'dbo.wallet_ledger',N'U') IS NULL OR OBJECT_ID(N'dbo.quotes',N'U') IS NULL
    THROW 51000,'Wallet or quote base tables were not found.',1;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.quote_fee_reservations',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.quote_fee_reservations(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_quote_fee_reservations PRIMARY KEY,
        public_id uniqueidentifier NOT NULL, quote_id bigint NOT NULL, wallet_id bigint NOT NULL,
        category_fee_policy_id bigint NOT NULL, amount decimal(19,4) NOT NULL,
        currency_code char(3) NOT NULL CONSTRAINT DF_quote_fee_reservations_currency DEFAULT('KRW'),
        status_code varchar(20) NOT NULL CONSTRAINT DF_quote_fee_reservations_status DEFAULT('RESERVED'),
        reserve_ledger_entry_id bigint NOT NULL, capture_ledger_entry_id bigint NULL, release_ledger_entry_id bigint NULL,
        reserved_at datetime2(7) NOT NULL CONSTRAINT DF_quote_fee_reservations_reserved DEFAULT(SYSUTCDATETIME()),
        captured_at datetime2(7) NULL, released_at datetime2(7) NULL, release_reason_code varchar(40) NULL,
        created_at datetime2(7) NOT NULL CONSTRAINT DF_quote_fee_reservations_created DEFAULT(SYSUTCDATETIME()),
        created_by_user_id bigint NULL,
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_quote_fee_reservations_updated DEFAULT(SYSUTCDATETIME()),
        updated_by_user_id bigint NULL, row_version rowversion NOT NULL,
        CONSTRAINT CK_quote_fee_reservations_amount CHECK(amount>0),
        CONSTRAINT CK_quote_fee_reservations_status CHECK(status_code IN('RESERVED','CAPTURED','RELEASED')),
        CONSTRAINT FK_quote_fee_reservations_quotes_quote_id FOREIGN KEY(quote_id) REFERENCES dbo.quotes(id),
        CONSTRAINT FK_quote_fee_reservations_wallets_wallet_id FOREIGN KEY(wallet_id) REFERENCES dbo.wallets(id),
        CONSTRAINT FK_quote_fee_reservations_category_fee_policies_category_fee_policy_id FOREIGN KEY(category_fee_policy_id) REFERENCES dbo.category_fee_policies(id),
        CONSTRAINT FK_quote_fee_reservations_wallet_ledger_reserve_ledger_entry_id FOREIGN KEY(reserve_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_quote_fee_reservations_wallet_ledger_capture_ledger_entry_id FOREIGN KEY(capture_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_quote_fee_reservations_wallet_ledger_release_ledger_entry_id FOREIGN KEY(release_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_quote_fee_reservations_users_created_by_user_id FOREIGN KEY(created_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_quote_fee_reservations_users_updated_by_user_id FOREIGN KEY(updated_by_user_id) REFERENCES dbo.users(id));
    CREATE UNIQUE INDEX IX_quote_fee_reservations_public_id ON dbo.quote_fee_reservations(public_id);
    CREATE UNIQUE INDEX IX_quote_fee_reservations_quote_id ON dbo.quote_fee_reservations(quote_id);
    CREATE INDEX IX_quote_fee_reservations_wallet_id ON dbo.quote_fee_reservations(wallet_id);
    CREATE INDEX IX_quote_fee_reservations_category_fee_policy_id ON dbo.quote_fee_reservations(category_fee_policy_id);
    CREATE UNIQUE INDEX IX_quote_fee_reservations_reserve_ledger_entry_id ON dbo.quote_fee_reservations(reserve_ledger_entry_id);
    CREATE UNIQUE INDEX IX_quote_fee_reservations_capture_ledger_entry_id ON dbo.quote_fee_reservations(capture_ledger_entry_id) WHERE capture_ledger_entry_id IS NOT NULL;
    CREATE UNIQUE INDEX IX_quote_fee_reservations_release_ledger_entry_id ON dbo.quote_fee_reservations(release_ledger_entry_id) WHERE release_ledger_entry_id IS NOT NULL;
    CREATE INDEX IX_quote_fee_reservations_created_by_user_id ON dbo.quote_fee_reservations(created_by_user_id);
    CREATE INDEX IX_quote_fee_reservations_updated_by_user_id ON dbo.quote_fee_reservations(updated_by_user_id);
    CREATE INDEX IX_quote_fee_reservations_status_code_reserved_at ON dbo.quote_fee_reservations(status_code,reserved_at);
END;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.wallet_ledger') AND name=N'CK_wallet_ledger_entry_type')
    ALTER TABLE dbo.wallet_ledger DROP CONSTRAINT CK_wallet_ledger_entry_type;
ALTER TABLE dbo.wallet_ledger WITH CHECK ADD CONSTRAINT CK_wallet_ledger_entry_type CHECK(entry_type_code IN('CHARGE','RESERVE','RELEASE','USE','RESTORE','REFUND','ADJUST'));
ALTER TABLE dbo.wallet_ledger CHECK CONSTRAINT CK_wallet_ledger_entry_type;
INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(@MigrationId,N'10.0.10');
COMMIT TRANSACTION;
SELECT @MigrationId AS MigrationId,N'APPLIED' AS Result;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW;
END CATCH;
