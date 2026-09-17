Soodal Life V162 - registered member broadcast

Scope
- Registered active CUSTOMER / PROVIDER members only. Dual-role users are deduplicated.
- WEB, SMS, EMAIL and PUSH group delivery. KAKAO is restricted to BUSINESS_NOTICE.
- No external phone/email list import endpoint or UI.
- Audience preparation runs in batches of 200 until the queue is exhausted.
- Channel configuration, verified contact and current consent are checked during preparation and again immediately before external delivery.
- Confirmation requires recent audience preview, admin password/MFA reauthentication and the exact second-confirmation text.
- Manual external-channel retry now supplies the actual verified recipient instead of an empty recipient.

Safety
- Production appsettings*.json and API App_Data are preserved.
- Database, IIS configuration and all four app directories are backed up before deployment.
- External channels remain fail-closed unless their channel settings and provider adapters are explicitly configured.
