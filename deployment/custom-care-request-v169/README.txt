SOODAL LIFE V169 - Custom care request workflow
Release date: 2026-08-31

Scope
- Replaces the customer standard-product choice with one custom care request flow.
- Limits new customer requests to currently allowed service categories.
- Stores new requests as CUSTOM with no CareProductId.
- Stores negotiable, desired monthly and desired per-visit amounts as structured request metadata.
- Shows the customer preference to eligible providers before a proposal is submitted.
- Lets providers propose scope, schedule, monthly amount or per-visit amount.
- Preserves existing STANDARD requests, standard products, contracts and admin product management.

Deployment impact
- API binaries and customer, partner and admin static applications are replaced.
- Database schema/data: unchanged.
- Production appsettings and API App_Data: preserved.
- Existing IIS and application files are backed up before replacement; failed file deployment is restored.

Place Deploy-CustomCareRequestV169.ps1 and
SoodalLife-CustomCareRequest-20260831-v169.zip together in D:\SOODALLIFE.

Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-CustomCareRequestV169.ps1 -ConfirmProductionDeployment
