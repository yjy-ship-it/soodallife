SOODAL LIFE V161 - Self-trade prevention

Release scope
- A user may keep both CUSTOMER and PROVIDER roles.
- A provider is never matched to a service request created by the same user account.
- Legacy self assignments are hidden from provider inboxes and active records are expired during deployment.
- Direct quote readiness, draft creation, revision, submission and customer acceptance are blocked with SELF_REQUEST_NOT_ALLOWED.
- Customer comparison/detail APIs do not expose legacy self quotes.
- Emergency response/selection and general site-visit proposal/acceptance paths are also blocked.
- No frontend or database schema change is included.

Verification
- Solution build: passed (0 errors; one pre-existing nullable warning).
- SelfTradePreventionApiTests: 1 passed.
- Related matching/quote suite: 9 passed; 2 existing request-throttle tests returned 429 because shared abuse limits were reached.
