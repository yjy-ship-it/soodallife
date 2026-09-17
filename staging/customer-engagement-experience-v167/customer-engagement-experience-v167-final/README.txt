SOODAL LIFE V167 - Customer engagement experience
Release date: 2026-08-30

Scope
- Improves customer header and neighborhood action buttons and fixes mobile wrapping.
- Rotates representative services and daily living checks while preserving category diversity.
- Changes neighborhood selection to Sido then Sigungu.
- Requires verified provider phone data for approval and separates optional administrative email.
- Adds safe limited-HTML provider introduction editing and customer preview.
- Uses the SOODAL LIFE brand image when a provider logo is absent.
- Adds quote submitted, unviewed quote, selection and service review reminder automation.
- Revalidates account, verified contact, channel availability, preferences and consent before non-required external delivery.
- Clarifies service rating and written usage-review terminology.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; a failed file deployment is restored.
- KAKAO reminder templates are created inactive and remain fail-closed until template approval and explicit channel activation.

Place Deploy-CustomerEngagementExperienceV167.ps1 and
SoodalLife-CustomerEngagementExperience-20260830-v167.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-CustomerEngagementExperienceV167.ps1 -ConfirmProductionDeployment

The wrapper checks the ZIP SHA-256 hash, validates the extracted release, creates IIS and file backups, deploys API and all three frontends, preserves production settings and data, and verifies health/readiness and public pages.
