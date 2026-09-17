SET NOCOUNT ON;

DECLARE @active_candidate_count int=(
    SELECT COUNT(*) FROM dbo.dispatch_candidates candidate
    JOIN dbo.service_requests request ON request.id=candidate.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=candidate.provider_profile_id
    WHERE customer.user_id=provider.user_id AND candidate.status_code IN ('ELIGIBLE','DISPATCHED'));
DECLARE @active_dispatch_count int=(
    SELECT COUNT(*) FROM dbo.request_dispatches dispatch
    JOIN dbo.service_requests request ON request.id=dispatch.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=dispatch.provider_profile_id
    WHERE customer.user_id=provider.user_id AND dispatch.status_code IN ('AVAILABLE','VIEWED'));
DECLARE @draft_quote_count int=(
    SELECT COUNT(*) FROM dbo.quotes quote
    JOIN dbo.service_requests request ON request.id=quote.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=quote.provider_profile_id
    WHERE customer.user_id=provider.user_id AND quote.status_code='DRAFT');
DECLARE @active_site_visit_count int=(
    SELECT COUNT(*) FROM dbo.site_visit_proposals proposal
    JOIN dbo.service_requests request ON request.id=proposal.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=proposal.provider_profile_id
    WHERE customer.user_id=provider.user_id AND proposal.status_code='PROPOSED');
DECLARE @active_emergency_count int=(
    SELECT COUNT(*) FROM dbo.emergency_responses response
    JOIN dbo.service_requests request ON request.id=response.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=response.provider_profile_id
    WHERE customer.user_id=provider.user_id AND response.status_code IN ('PENDING','AVAILABLE'));
DECLARE @submitted_quote_count int=(
    SELECT COUNT(*) FROM dbo.quotes quote
    JOIN dbo.service_requests request ON request.id=quote.service_request_id
    JOIN dbo.customer_profiles customer ON customer.id=request.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=quote.provider_profile_id
    WHERE customer.user_id=provider.user_id AND quote.status_code='SUBMITTED');
DECLARE @accepted_transaction_count int=(
    SELECT COUNT(*) FROM dbo.transactions transaction_record
    JOIN dbo.customer_profiles customer ON customer.id=transaction_record.customer_profile_id
    JOIN dbo.provider_profiles provider ON provider.id=transaction_record.provider_profile_id
    WHERE customer.user_id=provider.user_id);

IF @active_candidate_count<>0 OR @active_dispatch_count<>0 OR @draft_quote_count<>0 OR @active_site_visit_count<>0 OR @active_emergency_count<>0
    THROW 51610,'V161 self-trade active-record verification failed.',1;

SELECT 'V161_OK' AS verification_result,
       @active_candidate_count AS active_candidate_count,
       @active_dispatch_count AS active_dispatch_count,
       @draft_quote_count AS draft_quote_count,
       @active_site_visit_count AS active_site_visit_count,
       @active_emergency_count AS active_emergency_count,
       @submitted_quote_count AS submitted_quote_review_count,
       @accepted_transaction_count AS accepted_transaction_review_count;
