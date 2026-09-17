SOODAL LIFE V170 - Provider profile mobile and onboarding reminder
Release date: 2026-08-31

Scope
- Compacts and aligns provider introduction editor actions on mobile only.
- Keeps the desktop provider profile layout unchanged.
- Places business number verification beside the number input.
- Clarifies which provider fields are private, optional, identity-related or approval-required.
- Keeps the verified signup phone and optional head-office email private from customers.
- Queues duplicate-safe D1/D7 reminders when service categories or activity areas are missing.
- Activates WEB reminders and keeps KAKAO fail-closed until template approval, channel activation and consent.
- Prevents the urgent-work action label from wrapping.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; failed file deployment is restored.

Place Deploy-ProviderProfileMobileReminderV170.ps1 and
SoodalLife-ProviderProfileMobileReminder-20260831-v170.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-ProviderProfileMobileReminderV170.ps1 -ConfirmProductionDeployment
