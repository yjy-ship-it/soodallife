SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @now datetime2(7)=SYSUTCDATETIME();

BEGIN TRANSACTION;

UPDATE candidate
SET candidate.status_code='INELIGIBLE',
    candidate.reason_code='SELF_REQUEST_NOT_ALLOWED',
    candidate.evaluated_at=@now,
    candidate.expires_at=CASE WHEN candidate.expires_at IS NULL OR candidate.expires_at>@now THEN @now ELSE candidate.expires_at END
FROM dbo.dispatch_candidates AS candidate
JOIN dbo.service_requests AS request ON request.id=candidate.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=candidate.provider_profile_id
WHERE customer.user_id=provider.user_id
  AND candidate.status_code IN ('ELIGIBLE','DISPATCHED');

DECLARE @candidate_count int=@@ROWCOUNT;

UPDATE dispatch
SET dispatch.status_code='EXPIRED',
    dispatch.expires_at=CASE WHEN dispatch.expires_at>@now THEN @now ELSE dispatch.expires_at END
FROM dbo.request_dispatches AS dispatch
JOIN dbo.service_requests AS request ON request.id=dispatch.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=dispatch.provider_profile_id
WHERE customer.user_id=provider.user_id
  AND dispatch.status_code IN ('AVAILABLE','VIEWED');

DECLARE @dispatch_count int=@@ROWCOUNT;

UPDATE quote
SET quote.status_code='INVALIDATED',
    quote.expires_at=CASE WHEN quote.expires_at IS NULL OR quote.expires_at>@now THEN @now ELSE quote.expires_at END,
    quote.updated_at=@now
FROM dbo.quotes AS quote
JOIN dbo.service_requests AS request ON request.id=quote.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=quote.provider_profile_id
WHERE customer.user_id=provider.user_id
  AND quote.status_code='DRAFT';

DECLARE @quote_count int=@@ROWCOUNT;

UPDATE proposal
SET proposal.status_code='EXPIRED',
    proposal.expires_at=CASE WHEN proposal.expires_at>@now THEN @now ELSE proposal.expires_at END,
    proposal.updated_at=@now
FROM dbo.site_visit_proposals AS proposal
JOIN dbo.service_requests AS request ON request.id=proposal.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=proposal.provider_profile_id
WHERE customer.user_id=provider.user_id
  AND proposal.status_code='PROPOSED';

DECLARE @site_visit_count int=@@ROWCOUNT;

UPDATE response
SET response.status_code='EXPIRED',
    response.expires_at=CASE WHEN response.expires_at>@now THEN @now ELSE response.expires_at END,
    response.updated_at=@now
FROM dbo.emergency_responses AS response
JOIN dbo.service_requests AS request ON request.id=response.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=response.provider_profile_id
WHERE customer.user_id=provider.user_id
  AND response.status_code IN ('PENDING','AVAILABLE');

DECLARE @emergency_count int=@@ROWCOUNT;

UPDATE notification
SET notification.expires_at=CASE WHEN notification.expires_at IS NULL OR notification.expires_at>@now THEN @now ELSE notification.expires_at END
FROM dbo.notifications AS notification
JOIN dbo.request_dispatches AS dispatch ON dispatch.id=notification.request_dispatch_id
JOIN dbo.service_requests AS request ON request.id=dispatch.service_request_id
JOIN dbo.customer_profiles AS customer ON customer.id=request.customer_profile_id
JOIN dbo.provider_profiles AS provider ON provider.id=dispatch.provider_profile_id
WHERE customer.user_id=provider.user_id;

DECLARE @notification_count int=@@ROWCOUNT;

COMMIT TRANSACTION;

SELECT 'V161_APPLY_OK' AS result,
       @candidate_count AS candidate_count,
       @dispatch_count AS dispatch_count,
       @quote_count AS draft_quote_count,
       @site_visit_count AS site_visit_count,
       @emergency_count AS emergency_response_count,
       @notification_count AS notification_count;
