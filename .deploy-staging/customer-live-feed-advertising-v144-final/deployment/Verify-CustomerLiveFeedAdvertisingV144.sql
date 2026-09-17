SET NOCOUNT ON;

DECLARE @placementId bigint;
SELECT @placementId = id
FROM dbo.advertising_placements
WHERE code = 'CUSTOMER_LIVE_ACTIVITY_FEED' AND is_active = 1;

IF @placementId IS NULL
    THROW 51444, 'V144 verification failed: active live activity placement is missing.', 1;

IF (
    SELECT COUNT(*)
    FROM dbo.provider_advertising_rate_policies
    WHERE placement_id = @placementId
      AND is_active = 1
      AND effective_to IS NULL
      AND province_unit_amount = 10000
      AND district_unit_amount = 2000
      AND regional_fee_cap_amount = 30000
      AND ((duration_days = 7 AND fixed_amount = 28000)
        OR (duration_days = 14 AND fixed_amount = 48000)
        OR (duration_days = 30 AND fixed_amount = 88000))
) <> 3
    THROW 51445, 'V144 verification failed: live activity rate policies are incomplete.', 1;

SELECT 'V144_OK' AS verification_result,
       1 AS placement_count,
       COUNT(*) AS active_rate_count
FROM dbo.provider_advertising_rate_policies
WHERE placement_id = @placementId AND is_active = 1 AND effective_to IS NULL;
