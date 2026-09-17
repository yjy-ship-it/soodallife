SOODAL LIFE V173 - Request address, provider distance decision and hub performance
Release date: 2026-08-31

Scope
- Customers enter the service detail address while registering a request.
- Customers choose whether approved matched providers see it before a quote or only the selected provider sees it after acceptance.
- Providers explicitly confirm travel distance before opening quote entry and may decline a distant request.
- Declined requests are removed from that provider's inbox and quote access.
- Provider hub shows query-specific stored data immediately, refreshes in the background and uses a 15-second provider-scoped server cache.
- Text entry fields, textareas and selects use normal font weight throughout customer, provider and admin applications.

Deployment impact
- Adds service_requests.detail_address_disclosure_code with the privacy-first AFTER_SELECTION default.
- Existing requests remain AFTER_SELECTION; no existing detail address is newly disclosed.
- API binaries and customer, partner and admin static applications are replaced.
- Production appsettings and API App_Data are excluded and preserved.
- IIS files and the SQL database are backed up and verified before changes.

Place Deploy-RequestAddressProviderPerformanceV173.ps1 and
SoodalLife-RequestAddressProviderPerformance-20260831-v173.zip together in D:\SOODALLIFE.

Run elevated PowerShell:
Set-Location 'D:\SOODALLIFE'
.\Deploy-RequestAddressProviderPerformanceV173.ps1 -ConfirmProductionDeployment
