SOODAL LIFE V175 - Review communications
Release date: 2026-08-31

Scope
- Adds a top-level Communication menu for customer and provider applications.
- Moves customer My Reviews/Comments and provider Customer Reviews/Replies beside private work chat.
- Adds communication hub pages and mobile navigation access.
- Shows unread review-reply badges and refreshes them every 30 seconds.
- Opens a notification directly at the related review conversation and marks visible replies as read.
- Sorts review conversations by latest activity.
- Blocks exact duplicate comments and rapid consecutive posts from the same author.
- Keeps review conversations public and work chat private, with clear guidance in the interface.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; failed file deployment is restored.

Place Deploy-ReviewCommunicationsV175.ps1 and
SoodalLife-ReviewCommunications-20260831-v175.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-ReviewCommunicationsV175.ps1 -ConfirmProductionDeployment
