SOODAL LIFE V158 DEPLOYMENT

1. Keep this extracted release unchanged.
2. Place the V158 ZIP and Deploy-CustomerHomeDiversifiedFooterV158.ps1 together in D:\SOODALLIFE.
3. Open PowerShell as Administrator.
4. Run:
   Set-Location 'D:\SOODALLIFE'
   .\Deploy-CustomerHomeDiversifiedFooterV158.ps1 -ConfirmProductionDeployment

The deployment script validates the ZIP hash, backs up IIS, files and the database,
preserves API appsettings and App_Data, deploys the API and all frontend apps, and
checks health, readiness, customer home, partner and admin pages.

This release does not change the database schema or data.
