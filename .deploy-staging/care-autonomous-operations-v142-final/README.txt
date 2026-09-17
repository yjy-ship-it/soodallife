Soodal Life V142 - Care Autonomous Operations

Scope
- Customer cancellation of an open subscription request before provider selection.
- Provider withdrawal before selection and provider-initiated termination request.
- Schedule-change requests expire after 48 hours without a counterparty decision.
- Provider completion is automatically confirmed after 3 days unless an A/S or dispute case is open.
- Completion reminders are queued only when an active notification template exists.
- Customer skip is limited to 24 hours before the scheduled visit.
- Admin force decisions are exceptions: after 48 hours and with a detailed reason.

Deployment
1. Check ZIP SHA-256.
2. Open an elevated PowerShell window on the server.
3. Run D:\SOODALLIFE\Deploy-CareAutonomousOperationsV142.ps1 -ConfirmProductionDeployment
4. Verify API health/readiness and customer/provider care pages.

Rollback
- IIS backup and file backup are created before deployment.
- API App_Data and production appsettings files are preserved.
- Database schema is not changed by this release.
