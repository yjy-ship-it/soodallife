SOODAL LIFE V168 - Help room role and mobile reliability
Release date: 2026-08-30

Scope
- Prevents customer Help Room form controls from overflowing the mobile viewport.
- Uses border-box sizing, zero minimum widths and responsive single-column region fields.
- Separates the provider Help Room title, guidance, actions and empty state from the customer experience.
- Removes the customer create-post action from the partner portal, including dual-role accounts.
- Lists only open posts in categories approved for the active provider and excludes the provider's own customer posts.
- Shows first-advice, follow-up-available and waiting-for-customer states.
- Counts provider answers separately from customer follow-up entries.
- Keeps server-side approval, cooldown, duplicate, follow-up and closed-post protections.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; failed file deployment is restored.

Place Deploy-HelpRoomRoleMobileV168.ps1 and
SoodalLife-HelpRoomRoleMobile-20260830-v168.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-HelpRoomRoleMobileV168.ps1 -ConfirmProductionDeployment
