SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH('dbo.provider_emergency_service_settings','base_dispatch_fee_amount') IS NULL THROW 51000,'V131 provider pricing columns missing',1;
IF COL_LENGTH('dbo.emergency_responses','payment_mode_code') IS NULL THROW 51000,'V131 response pricing snapshot missing',1;
IF OBJECT_ID('dbo.emergency_dispatch_agreements','U') IS NULL THROW 51000,'V131 emergency agreement table missing',1;
IF EXISTS(
    SELECT 1 FROM dbo.transactions t
    JOIN dbo.service_requests r ON r.id=t.service_request_id AND r.is_urgent=1
    LEFT JOIN dbo.emergency_dispatch_agreements a ON a.transaction_id=t.id
    WHERE a.id IS NULL
) THROW 51000,'V131 emergency agreement backfill incomplete',1;

SELECT
    'V131_OK' AS verification_result,
    (SELECT COUNT(*) FROM dbo.provider_emergency_service_settings WHERE is_enabled=1) AS enabled_emergency_services,
    (SELECT COUNT(*) FROM dbo.emergency_dispatch_agreements) AS emergency_agreements,
    (SELECT COUNT(*) FROM dbo.emergency_progress_events WHERE event_type_code IN ('CUSTOMER_NO_SHOW','PROVIDER_NO_SHOW','NO_SHOW_DISPUTED')) AS no_show_events;
