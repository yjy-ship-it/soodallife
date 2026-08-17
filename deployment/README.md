# IIS production deployment

The production build keeps all external providers unavailable until their real adapters are configured. It does not mark PG, notification, identity verification, file scanning/OCR, GPS/ETA, or electronic-signature work as successful.

## Build

Run `deployment\Publish-Production.ps1`. The output contains one API package and separate customer, partner, and admin static sites. The frontend is built against `https://api.soodallife.kr`.

## Required IIS configuration

- IIS with Static Content and WebSocket Protocol
- IIS URL Rewrite for SPA fallback and HTTPS redirect
- .NET 10 Hosting Bundle (ASP.NET Core Module V2)
- One `No Managed Code` app pool for the API and static-site app pools or a shared static app pool
- HTTPS SNI bindings for `soodallife.kr`, `partner.soodallife.kr`, `admin.soodallife.kr`, and `api.soodallife.kr`
- Read/execute permission for site folders; modify permission only for the API app-pool identity on the configured private-file and Data Protection key folders

Configure the following on the API app pool or API site's `configuration/system.webServer/aspNetCore/environmentVariables`. Do not commit their values:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__SoodalLife`
- `Cors__AllowedOrigins__0=https://soodallife.kr`
- `Cors__AllowedOrigins__1=https://partner.soodallife.kr`
- `Cors__AllowedOrigins__2=https://admin.soodallife.kr`
- `DataProtection__KeysPath` (persistent directory outside the release folder)
- `FileStorage__PrivateRoot` (private directory outside all IIS web roots)
- `PrivacyProtection__SearchHashKey` (stable Base64 key, at least 32 bytes)
- Privacy dual-write/read flags must match the existing database migration and backfill state; do not guess them during deployment.

Apply pending EF migrations only after a database backup and migration review. Never reset or delete the production database. Validate `GET /api/v1/system/health` for process liveness and `GET /api/v1/system/ready` for database connectivity.
