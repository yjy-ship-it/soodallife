Soodal Life V182-R2 request/interior consistency

This is a self-contained production deployment package.
- It contains API, customer, partner and admin runtime files.
- It does not require D:\ALL_project or any development source tree.
- It preserves API App_Data and all production appsettings files.
- It creates IIS and application-file backups before replacement.
- It verifies production health and readiness after deployment.
- Database schema/data and production settings are not changed.

The related request, quote and autonomous interior API tests passed 48/48 before packaging.
