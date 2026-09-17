SOODAL LIFE V177 - Customer communication screen centering
Release date: 2026-08-31

Scope
- Centers the Communication Home tabs, hero and choice cards in a 1180px responsive content area.
- Keeps Work Chat and My Reviews/Comments cards aligned to the same left and right edges.
- Centers the chat navigation tabs in a 900px responsive area matching the chat list.
- Centers customer and provider review conversation pages with the same responsive rules.
- Uses 16px mobile side margins so content remains readable on narrow screens.

Deployment impact
- Customer, partner and admin static applications are replaced with the same validated frontend build.
- API binaries: unchanged.
- Database schema/data: unchanged.
- Production settings: unchanged.
- Existing frontend files and IIS configuration are backed up before replacement; failed file deployment is restored.

Place Deploy-CommunicationCenteringV177.ps1 and
SoodalLife-CommunicationCentering-20260831-v177.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-CommunicationCenteringV177.ps1 -ConfirmProductionDeployment
