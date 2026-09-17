SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.provider_advertising_rate_policies',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.provider_advertising_rate_policies(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_provider_advertising_rate_policies PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        placement_id bigint NOT NULL,
        duration_days int NOT NULL,
        fixed_amount decimal(19,4) NOT NULL,
        currency_code char(3) NOT NULL,
        effective_from date NOT NULL,
        effective_to date NULL,
        is_active bit NOT NULL CONSTRAINT DF_provider_ad_rate_active DEFAULT(1),
        created_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_rate_created DEFAULT(SYSUTCDATETIME()),
        created_by_user_id bigint NULL,
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_rate_updated DEFAULT(SYSUTCDATETIME()),
        updated_by_user_id bigint NULL,
        row_version rowversion NOT NULL,
        CONSTRAINT FK_provider_ad_rate_placement FOREIGN KEY(placement_id) REFERENCES dbo.advertising_placements(id),
        CONSTRAINT FK_provider_ad_rate_created_user FOREIGN KEY(created_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_provider_ad_rate_updated_user FOREIGN KEY(updated_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT CK_provider_ad_rate_duration CHECK(duration_days IN(7,14,30)),
        CONSTRAINT CK_provider_ad_rate_amount CHECK(fixed_amount>0),
        CONSTRAINT CK_provider_ad_rate_period CHECK(effective_to IS NULL OR effective_to>=effective_from)
    );
    CREATE UNIQUE INDEX IX_provider_ad_rate_public_id ON dbo.provider_advertising_rate_policies(public_id);
    CREATE UNIQUE INDEX IX_provider_ad_rate_natural ON dbo.provider_advertising_rate_policies(placement_id,duration_days,effective_from);
    CREATE INDEX IX_provider_ad_rate_active ON dbo.provider_advertising_rate_policies(is_active,effective_from,effective_to);
END;

IF OBJECT_ID(N'dbo.provider_advertising_applications',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.provider_advertising_applications(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_provider_advertising_applications PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        campaign_id bigint NOT NULL,
        provider_profile_id bigint NOT NULL,
        rate_policy_id bigint NOT NULL,
        wallet_id bigint NOT NULL,
        duration_days int NOT NULL,
        fee_amount decimal(19,4) NOT NULL,
        currency_code char(3) NOT NULL,
        status_code varchar(20) NOT NULL,
        fee_status_code varchar(20) NOT NULL,
        reserve_ledger_entry_id bigint NOT NULL,
        capture_ledger_entry_id bigint NULL,
        release_ledger_entry_id bigint NULL,
        supplement_note nvarchar(2000) NULL,
        rejection_reason nvarchar(2000) NULL,
        submitted_at datetime2(7) NOT NULL,
        resubmitted_at datetime2(7) NULL,
        approved_at datetime2(7) NULL,
        published_at datetime2(7) NULL,
        cancelled_at datetime2(7) NULL,
        created_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_app_created DEFAULT(SYSUTCDATETIME()),
        created_by_user_id bigint NULL,
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_provider_ad_app_updated DEFAULT(SYSUTCDATETIME()),
        updated_by_user_id bigint NULL,
        row_version rowversion NOT NULL,
        CONSTRAINT FK_provider_ad_app_campaign FOREIGN KEY(campaign_id) REFERENCES dbo.advertising_campaigns(id),
        CONSTRAINT FK_provider_ad_app_provider FOREIGN KEY(provider_profile_id) REFERENCES dbo.provider_profiles(id),
        CONSTRAINT FK_provider_ad_app_rate FOREIGN KEY(rate_policy_id) REFERENCES dbo.provider_advertising_rate_policies(id),
        CONSTRAINT FK_provider_ad_app_wallet FOREIGN KEY(wallet_id) REFERENCES dbo.wallets(id),
        CONSTRAINT FK_provider_ad_app_reserve_ledger FOREIGN KEY(reserve_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_provider_ad_app_capture_ledger FOREIGN KEY(capture_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_provider_ad_app_release_ledger FOREIGN KEY(release_ledger_entry_id) REFERENCES dbo.wallet_ledger(id),
        CONSTRAINT FK_provider_ad_app_created_user FOREIGN KEY(created_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_provider_ad_app_updated_user FOREIGN KEY(updated_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT CK_provider_ad_app_amount CHECK(fee_amount>0),
        CONSTRAINT CK_provider_ad_app_status CHECK(status_code IN('SUBMITTED','REJECTED','RESUBMITTED','APPROVED','PUBLISHED','CANCELLED')),
        CONSTRAINT CK_provider_ad_app_fee_status CHECK(fee_status_code IN('RESERVED','CAPTURED','RELEASED'))
    );
    CREATE UNIQUE INDEX IX_provider_ad_app_public_id ON dbo.provider_advertising_applications(public_id);
    CREATE UNIQUE INDEX IX_provider_ad_app_campaign ON dbo.provider_advertising_applications(campaign_id);
    CREATE INDEX IX_provider_ad_app_provider_status ON dbo.provider_advertising_applications(provider_profile_id,status_code,submitted_at);
    CREATE INDEX IX_provider_ad_app_queue ON dbo.provider_advertising_applications(status_code,updated_at);
END;

DECLARE @Defaults TABLE(placement_code varchar(50),duration_days int,fixed_amount decimal(19,4),public_id uniqueidentifier);
INSERT @Defaults VALUES
('CUSTOMER_HOME',7,35000,'11a20000-0000-0000-0000-000000000001'),
('CUSTOMER_HOME',14,60000,'11a20000-0000-0000-0000-000000000002'),
('CUSTOMER_HOME',30,110000,'11a20000-0000-0000-0000-000000000003'),
('PROVIDER_HOME',7,21000,'11a20000-0000-0000-0000-000000000004'),
('PROVIDER_HOME',14,36000,'11a20000-0000-0000-0000-000000000005'),
('PROVIDER_HOME',30,66000,'11a20000-0000-0000-0000-000000000006');

IF EXISTS(SELECT 1 FROM @Defaults d LEFT JOIN dbo.advertising_placements p ON p.code=d.placement_code WHERE p.id IS NULL)
    THROW 51099,N'기본 광고 노출 위치가 없어 정액 요금을 등록할 수 없습니다.',1;

MERGE dbo.provider_advertising_rate_policies AS target
USING(SELECT p.id placement_id,d.duration_days,d.fixed_amount,d.public_id FROM @Defaults d JOIN dbo.advertising_placements p ON p.code=d.placement_code) AS source
ON target.placement_id=source.placement_id AND target.duration_days=source.duration_days AND target.effective_from=CONVERT(date,'2026-08-19')
WHEN NOT MATCHED THEN INSERT(public_id,placement_id,duration_days,fixed_amount,currency_code,effective_from,is_active,created_at,updated_at)
VALUES(source.public_id,source.placement_id,source.duration_days,source.fixed_amount,'KRW','2026-08-19',1,SYSUTCDATETIME(),SYSUTCDATETIME());

COMMIT TRANSACTION;
SELECT p.code placement_code,r.duration_days,r.fixed_amount,r.currency_code,r.is_active
FROM dbo.provider_advertising_rate_policies r JOIN dbo.advertising_placements p ON p.id=r.placement_id
WHERE r.effective_from='2026-08-19' ORDER BY p.id,r.duration_days;
