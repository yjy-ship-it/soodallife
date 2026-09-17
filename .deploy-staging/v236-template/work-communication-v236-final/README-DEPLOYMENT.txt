V236 deployment package

Run Windows PowerShell 5.1 as Administrator:
& 'D:\SOODALLIFE\Deploy-WorkCommunicationV236.ps1' -ConfirmProductionDeployment -Confirm:$false

The launcher validates SHA-256, UTF-8 BOM, parser syntax, required files and target paths before deployment.
It creates IIS and file backups and restores files if deployment fails.
Database, API App_Data and production appsettings files are not changed.