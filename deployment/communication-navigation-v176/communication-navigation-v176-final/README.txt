SOODAL LIFE V176 - Customer communication navigation reliability
Release date: 2026-08-31

Scope
- Changes the customer desktop Communication item from a duplicate dropdown into a direct hub link.
- Uses real links for Communication Home, Work Chat and My Reviews/Comments.
- Keeps the communication tabs visible on the chat list so the selected destination is clear.
- Closes any other open header menu before changing pages.
- Adds hover and keyboard-focus feedback to both communication choices.
- Keeps the mobile Communication section with Home, Work Chat and My Reviews/Comments.

Deployment impact
- Customer, partner and admin static applications are replaced with the same validated frontend build.
- API binaries: unchanged.
- Database schema/data: unchanged.
- Production settings: unchanged.
- Existing frontend files and IIS configuration are backed up before replacement; failed file deployment is restored.

Place Deploy-CommunicationNavigationV176.ps1 and
SoodalLife-CommunicationNavigation-20260831-v176.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-CommunicationNavigationV176.ps1 -ConfirmProductionDeployment
