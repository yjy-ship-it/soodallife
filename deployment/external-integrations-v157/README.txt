V157 deployment order
1. Deploy with every external mode left DISABLED. Existing server appsettings are preserved.
2. Merge only reviewed keys from EXTERNAL-INTEGRATION-SETTINGS.example.json into the protected server configuration or secret store.
3. Set NICE, NHN and Toss wallet modes to TEST and restart the API.
4. Verify NICE customer signup and provider signup, NHN allow-listed recipients, and Toss test payment approval/ledger reflection.
5. Keep NHN channel rows disabled except the single channel under verification. Activate SMS, Email, approved Alimtalk templates, then Push in sequence.
6. Change a service to PRODUCTION only after its provider approval is complete. Never place secret keys in frontend files.

Important
- DISABLED is the safe default; no real external send or payment can start without server keys and an explicit mode change.
- Wallet balance is credited only after the server receives a matching Toss DONE approval.
- The NICE production VerificationUrl is the contracted NICE integration bridge/result validation endpoint; confirm its request/response contract before PRODUCTION.
- PWA browser permission is opt-in. NHN Push token/UID registration must be validated for the contracted web-push product before enabling the Push channel.
