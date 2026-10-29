# Sandbox API

The public WEB4 sandbox lets a visitor try HyperDrive failover, replication, native data import and CQL with one click and no
account. Route: `api/sandbox`. These endpoints answer **404 everywhere except a node whose DNA has `OASIS.Sandbox.Enabled: true`**.
Design, cleanup rules and every setting: [SANDBOX_DEMO_AVATARS.md](../../SANDBOX_DEMO_AVATARS.md). Postman:
[WEB4-NativeData-Sandbox-CQL.postman_collection.json](WEB4-NativeData-Sandbox-CQL.postman_collection.json).

## Endpoints

| Method | Route | Auth | Does |
|---|---|---|---|
| POST | `/api/sandbox/session` | none | Creates a verified throwaway demo avatar (`<prefix><8 hex>`), routes it across the sandbox databases, signs it in, returns it with its JWT |
| GET | `/api/sandbox/faults` | Bearer | Providers this avatar has taken offline and when each returns |
| GET | `/api/sandbox/refusals` | Bearer | Requests each offline provider has refused for this avatar |
| POST | `/api/sandbox/providers/{provider}/offline?seconds=30` | Bearer | Takes a provider offline for this avatar only, so HyperDrive must fail over. Seconds are clamped to the DNA limits |
| POST | `/api/sandbox/providers/{provider}/online` | Bearer | Brings it back immediately |
| POST | `/api/sandbox/native-demo` | Bearer | Idempotently writes the demo shop (customers, products, orders) so Native Data can import it |

## Limits and cleanup

- Session creation is limited per address per hour and per day across the sandbox (HTTP 429 with the reason).
- Offline durations are bounded by `ProviderOfflineMinimumSeconds` / `ProviderOfflineMaximumSeconds`.
- A demo avatar older than `DemoAvatarRetentionMinutes` is deleted by a background sweep **together with everything it owns**: holons on every sandbox provider, imported data, saved mappings and saved connections. Only avatars matching the DNA username prefix **and** email domain, of type `User`, are ever touched.

## DNA (`OASIS.Sandbox`)

`Enabled`, `Providers`, `DemoAvatarUsernamePrefix`, `DemoAvatarEmailDomain`, `DemoAvatarRetentionMinutes`,
`CleanupIntervalMinutes`, `CleanupInitialDelayMinutes`, `MaximumAvatarsRemovedPerSweep`, `ProviderOfflineDefaultSeconds`,
`ProviderOfflineMinimumSeconds`, `ProviderOfflineMaximumSeconds`, `DemoNeo4jLabel`, `MaximumSessionsPerAddressPerHour`,
`MaximumSessionsPerDay`. Defaults and meanings: [OASIS_DNA_Reference.md](../../OASIS_DNA_Reference.md#sandbox).

---

**← Previous:** [Native Data API](Native-Data-API.md) | **Next:** [CQL API](CQL-API.md) →
