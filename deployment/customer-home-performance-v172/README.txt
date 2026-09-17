SOODAL LIFE V172 - Customer home performance
Release date: 2026-08-31

Scope
- Loads living-home data once and shares it across customer-home sections.
- Displays account-scoped stored living data immediately and refreshes it in the background.
- Starts the public live activity feed only near the viewport and pauses polling in hidden tabs.
- Keeps loading regions at a stable height to reduce layout shift.
- Caches the privacy-filtered public activity feed for 30 seconds.
- Separates public aggregate caches from customer-specific maintenance-calendar caches.
- Adds a Server-Timing response header and structured duration logs for API endpoints without logging request data.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; failed file deployment is restored.

Place Deploy-CustomerHomePerformanceV172.ps1 and
SoodalLife-CustomerHomePerformance-20260831-v172.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-CustomerHomePerformanceV172.ps1 -ConfirmProductionDeployment
