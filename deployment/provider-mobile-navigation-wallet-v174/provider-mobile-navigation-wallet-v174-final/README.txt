SOODAL LIFE V174 - Provider mobile navigation icons and wallet header
Release date: 2026-08-31

Scope
- Adds distinct SVG icons above Home, Requests, Progress, Chat and My labels in the provider mobile bottom navigation.
- Shows the available wallet balance below the provider brand on mobile when approval and activity are both active.
- Keeps the approval status visible for providers who are pending, rejected, suspended or inactive.
- Keeps the desktop header and desktop wallet controls unchanged.

Deployment impact
- Partner static application only.
- API binaries: unchanged.
- Database schema/data: unchanged.
- Production settings: unchanged.
- Existing partner files and IIS configuration are backed up before replacement.

Place Deploy-ProviderMobileNavigationWalletV174.ps1 and
SoodalLife-ProviderMobileNavigationWallet-20260831-v174.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-ProviderMobileNavigationWalletV174.ps1 -ConfirmProductionDeployment
