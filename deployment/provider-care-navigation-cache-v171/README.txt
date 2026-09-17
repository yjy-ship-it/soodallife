SOODAL LIFE V171 - Provider care navigation and frontend cache refresh
Release date: 2026-08-31

Scope
- Renames the provider service entry to make custom care proposals easy to find.
- Labels the provider workflow as customer custom requests, sent proposals, selection/contracts and visits.
- Explains that customers compare proposals before selecting a provider.
- Prevents stale index.html and sw.js responses from surviving a deployment.
- Registers the service worker without HTTP cache and refreshes an already-open page once when a new worker takes control.
- Moves the application shell cache to soodal-life-shell-v4.

Deployment impact
- Customer, partner and admin static applications are replaced so all portals share the same cache policy.
- API binaries: unchanged.
- Database schema/data: unchanged.
- Production settings: unchanged.
- Existing frontend files and IIS configuration are backed up before replacement; failed file deployment is restored.

Place Deploy-ProviderCareNavigationCacheV171.ps1 and
SoodalLife-ProviderCareNavigationCache-20260831-v171.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-ProviderCareNavigationCacheV171.ps1 -ConfirmProductionDeployment
