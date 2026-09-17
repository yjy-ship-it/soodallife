SOODAL LIFE V153 - Provider area save reliability

Included:
- Provider activity areas are verified by the API response and remain after reload.
- A successful area save is no longer reported as failed when automatic rematching has a separate error.
- Inactive historical areas are excluded from emergency availability calculations.
- Overall emergency availability is available when any enabled service is currently available.
- The provider screen distinguishes saved state from pending changes and shows a nearby completion message.
- Emergency availability is recalculated immediately after an area save.

Database schema changed: false.
The deployment performs database, IIS and application backups before replacing API and frontend files.
