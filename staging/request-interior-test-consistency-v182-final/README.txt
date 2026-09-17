Soodal Life V182 request/interior test-contract consistency

This release aligns automated tests with the already-deployed runtime policies:
- Preferred request schedules use valid future 30-minute slots.
- Request and quote tests include the required detailed-address and ownership context.
- Interior project tests use customer/provider autonomous mutation routes.
- Obsolete administrator mutation routes remain verified as Method Not Allowed.

The package updates test source only and runs six related suites (48 tests).
It does not replace IIS applications, change the database, or modify production settings.
