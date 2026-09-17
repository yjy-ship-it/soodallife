SOODAL LIFE V155 - CUSTOMER REQUEST ABUSE PROTECTION

Included changes
- Same service: maximum 2 published requests per rolling 24 hours
- All services: maximum 5 per rolling 24 hours and 15 per rolling 7 days
- Concurrent quote collection: maximum 3 requests
- Concurrent emergency request: maximum 1 request
- Duplicate service/area/content request blocking for 24 hours
- Progressive restriction for repeated cancellation, abandoned requests and unviewed quotes
- Customer quote view timestamp and admin abuse-count exclusion audit API
- Customer-facing limit guidance and wave-dispatch wording
- Customer-scoped SQL application lock prevents concurrent publish bypass

Explicitly excluded
- NICE phone identity verification before request publication is NOT included in V155.

Production safety
- The deployment script creates IIS, file and SQL backups before applying the schema.
- Existing requests are not retroactively assigned policy version 1. Progressive restrictions apply to V155-and-later published requests only.
- Administrators can exclude confirmed false requests or system failures from limit counts with a mandatory reason and audit log.
