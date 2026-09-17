SOODAL LIFE V159 DEPLOYMENT

1. Keep this extracted release unchanged.
2. Place the V159 ZIP and Deploy-ProviderProposalMarketplaceV159.ps1 together in D:\SOODALLIFE.
3. Open PowerShell as Administrator.
4. Run:
   Set-Location 'D:\SOODALLIFE'
   .\Deploy-ProviderProposalMarketplaceV159.ps1 -ConfirmProductionDeployment

The script checks the ZIP hash, creates IIS/file/database backups, applies the V159 schema,
preserves production appsettings and App_Data, deploys API and all three frontends, and verifies
database, API readiness and public sites.

External promotion delivery remains safely disabled after deployment. Do not enable it until:
- the final marketing terms are active and customer consent records use the final version;
- ProviderProposals:MarketingTermsFinalized is true;
- NHN channel settings are enabled for the intended channel;
- for PUSH only, PWA is actually enabled and ProviderProposals:PwaPushEnabled is true.
