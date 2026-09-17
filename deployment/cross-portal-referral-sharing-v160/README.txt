SOODAL LIFE V160 DEPLOYMENT

1. Keep this extracted release unchanged.
2. Place the V160 ZIP and Deploy-CrossPortalReferralSharingV160.ps1 together in D:\SOODALLIFE.
3. Open PowerShell as Administrator.
4. Run:
   Set-Location 'D:\SOODALLIFE'
   .\Deploy-CrossPortalReferralSharingV160.ps1 -ConfirmProductionDeployment

The script verifies the ZIP, creates IIS and frontend file backups, deploys the customer,
partner and admin frontends, and verifies the API health, sites and V160 feature markers.
It does not change the database, API or production settings.
