Place Deploy-NavigationMenuReliabilityV166.ps1 and SoodalLife-NavigationMenuReliability-20260830-v166.zip together in D:\SOODALLIFE.
Run elevated PowerShell:

Set-Location 'D:\SOODALLIFE'
.\Deploy-NavigationMenuReliabilityV166.ps1 -ConfirmProductionDeployment

The script verifies the ZIP hash, extracts the release beneath D:\SOODALLIFE\releases, backs up all three frontend apps and IIS configuration, deploys, then verifies health, readiness, public pages, and V166 bundle markers.
