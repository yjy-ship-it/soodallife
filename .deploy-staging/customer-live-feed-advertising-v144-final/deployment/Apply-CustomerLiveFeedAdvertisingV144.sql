SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.advertising_placements', N'U') IS NULL
    THROW 51440, 'V144 requires dbo.advertising_placements.', 1;
IF OBJECT_ID(N'dbo.provider_advertising_rate_policies', N'U') IS NULL
    THROW 51441, 'V144 requires dbo.provider_advertising_rate_policies.', 1;

BEGIN TRANSACTION;

DECLARE @now datetime2(7) = SYSUTCDATETIME();
DECLARE @placementId bigint;
DECLARE @placementPublicId uniqueidentifier = '11A10000-0000-0000-0000-000000000003';

IF EXISTS (
    SELECT 1 FROM dbo.advertising_placements
    WHERE public_id = @placementPublicId AND code <> 'CUSTOMER_LIVE_ACTIVITY_FEED'
)
    THROW 51442, 'V144 placement public_id is already used by another code.', 1;

SELECT @placementId = id
FROM dbo.advertising_placements WITH (UPDLOCK, HOLDLOCK)
WHERE code = 'CUSTOMER_LIVE_ACTIVITY_FEED';

IF @placementId IS NULL
BEGIN
    INSERT dbo.advertising_placements
        (public_id, code, name, description, route_hint, is_active, created_at, updated_at)
    VALUES
        (@placementPublicId, 'CUSTOMER_LIVE_ACTIVITY_FEED', N'고객 실시간 서비스 목록',
         N'전국에서 지금 진행 중인 서비스 목록 사이에 광고임을 명확히 표시하여 노출',
         '/customer#live-activity', 1, @now, @now);
    SET @placementId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.advertising_placements
    SET name = N'고객 실시간 서비스 목록',
        description = N'전국에서 지금 진행 중인 서비스 목록 사이에 광고임을 명확히 표시하여 노출',
        route_hint = '/customer#live-activity', is_active = 1, updated_at = @now
    WHERE id = @placementId;
END;

DECLARE @rates TABLE
(
    public_id uniqueidentifier NOT NULL,
    duration_days int NOT NULL,
    fixed_amount decimal(18,2) NOT NULL
);
INSERT @rates(public_id, duration_days, fixed_amount) VALUES
('11A20000-0000-0000-0000-000000000007', 7,  28000),
('11A20000-0000-0000-0000-000000000008', 14, 48000),
('11A20000-0000-0000-0000-000000000009', 30, 88000);

IF EXISTS (
    SELECT 1
    FROM @rates r
    JOIN dbo.provider_advertising_rate_policies p ON p.public_id = r.public_id
    WHERE p.placement_id <> @placementId OR p.duration_days <> r.duration_days
)
    THROW 51443, 'V144 rate public_id is already used by another placement or duration.', 1;

UPDATE p
SET p.fixed_amount = r.fixed_amount,
    p.province_unit_amount = 10000,
    p.district_unit_amount = 2000,
    p.regional_fee_cap_amount = 30000,
    p.currency_code = 'KRW',
    p.effective_from = '2026-08-19',
    p.effective_to = NULL,
    p.is_active = 1,
    p.updated_at = @now
FROM dbo.provider_advertising_rate_policies p
JOIN @rates r ON r.public_id = p.public_id;

INSERT dbo.provider_advertising_rate_policies
    (public_id, placement_id, duration_days, fixed_amount, province_unit_amount,
     district_unit_amount, regional_fee_cap_amount, currency_code,
     effective_from, effective_to, is_active, created_at, updated_at)
SELECT r.public_id, @placementId, r.duration_days, r.fixed_amount, 10000,
       2000, 30000, 'KRW', '2026-08-19', NULL, 1, @now, @now
FROM @rates r
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.provider_advertising_rate_policies p
    WHERE p.public_id = r.public_id
);

COMMIT TRANSACTION;

SELECT 'V144_APPLY_OK' AS result, @placementId AS placement_id;
