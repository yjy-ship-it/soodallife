V240 adds provider quote templates to the quote-writing screen.

Run Windows PowerShell 5.1 as Administrator:
& 'D:\SOODALLIFE\Deploy-QuoteTemplatesV240.ps1' -ConfirmProductionDeployment -Confirm:$false

The script validates its ZIP hash and UTF-8 BOM, backs up IIS files and SQL Server, applies the idempotent schema update, deploys API/customer/partner/admin files, preserves API App_Data and appsettings files, and checks the public endpoints.

Template contents are owned by the signed-in provider. A provider can save up to 30 named templates. Saving an existing name updates it. Request-specific start date, validity date and revision reason are deliberately not stored in a template.
