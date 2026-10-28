# Sandbox demo avatars: creation, limits and cleanup

The public WEB4 sandbox lets a visitor try everything with one click, no account needed. `POST api/sandbox/session` creates a
throwaway verified avatar named `sandbox-<8 hex>` with the email `sandbox-<8 hex>@sandbox.oasisomniverse.one` and a random
password, routes it across the sandbox's three databases (MongoDB, SQLite, Neo4j) with its own HyperDrive settings, and signs it
in. These endpoints exist only where DNA has `OASIS.Sandbox.Enabled` set to true; everywhere else they answer 404.

## Limits on creating them

`SandboxSessionLimiter` (Core `Managers/SandboxAvatarCleanup.cs`) bounds the public button:

- `MaximumSessionsPerAddressPerHour` demo avatars per address per hour (default 20), and `MaximumSessionsPerDay` across the whole sandbox per 24 hours (default 1,000).
- A refused request answers HTTP 429 with the reason, which the sandbox page shows.
- The counts are in memory, so a restart resets them.

## Cleanup

`SandboxCleanupHostedService` runs inside the WEB4 API and does nothing **unless DNA has `OASIS.Sandbox.Enabled`**, so no other deployment ever deletes anything.
It sweeps every `CleanupIntervalMinutes` (default 15; the first sweep is `CleanupInitialDelayMinutes` after start-up) and removes a demo avatar once it is older than the
retention period, `DemoAvatarRetentionMinutes` (default 1,440, which is 24 hours).

An avatar is treated as a demo avatar only when **all** of these hold, so a real user can never be matched:

- its username starts with `sandbox-`,
- its email ends with `@sandbox.oasisomniverse.one`,
- it is a plain `User` (never a Wizard),
- it has a creation date older than the retention period.

For each such avatar the sweep:

1. lists the holons the avatar owns on each sandbox provider and deletes them (hard delete, not soft),
2. only when every holon is gone, deletes the avatar from each sandbox provider,
3. keeps the avatar when anything failed, so the next sweep retries the same avatar instead of orphaning its data.

That covers everything a visitor creates: saved holons, imported holons and their replicas, saved mappings and settings (stored
with the avatar). One sweep removes at most 50 avatars, oldest first, and logs how many avatars and holons were removed and any
failures. A visitor's in-memory "provider offline" flags clear on restart.

Imported data is not left behind: imported holons are ordinary holons owned by the importing avatar.

## Saved connections

A visitor may save a native-import connection under a name. It is stored encrypted in the demo avatar's own record (see NATIVE_DATA_IMPORT.md, "Saved connections"), so cleanup deletes it with the avatar, like everything else. The sandbox needs DNA `OASIS.NativeImport.ConnectionEncryptionKey` set for the Save button to work.

## Tests

`OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/SandboxAvatarCleanupTests.cs`: the demo-avatar match rules,
holons-before-avatar ordering on every provider, retry after a failed deletion, one failing avatar not stopping the rest,
the per-sweep cap, and the session limiter's sliding windows.

## Settings (all in DNA, none hard-coded)

Everything above is configured in `OASIS.Sandbox` of the node's DNA (`OASIS_DNA.json`, or the `OASIS_DNA_JSON` variable on
Railway). The defaults are written once, in `SandboxSettings`; the shipped `Default/OASIS_DNA.json` lists every setting with the
same values and a test keeps the two identical. A missing section or key takes the default; a value below 1 fails with a message
naming the setting.

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Turns the sandbox endpoints and the cleanup on. Set it only on the sandbox deployment |
| `Providers` | `MongoDBOASIS, SQLLiteDBOASIS, Neo4jOASIS` | The providers a demo avatar's HyperDrive settings route across |
| `DemoAvatarUsernamePrefix` | `sandbox-` | Demo avatars are `<prefix><8 hex>`. Cleanup requires this prefix and the domain below; at least 3 characters |
| `DemoAvatarEmailDomain` | `sandbox.oasisomniverse.one` | Demo avatars get `<username>@<domain>`; must contain a dot |
| `DemoAvatarRetentionMinutes` | `1440` | Age at which a demo avatar and its data are removed |
| `CleanupIntervalMinutes` | `15` | Minutes between sweeps |
| `CleanupInitialDelayMinutes` | `1` | Minutes after start-up before the first sweep |
| `MaximumAvatarsRemovedPerSweep` | `50` | Avatars one sweep removes, oldest first |
| `ProviderOfflineDefaultSeconds` | `60` | How long a visitor's "take provider offline" lasts when the request does not say |
| `ProviderOfflineMinimumSeconds` / `ProviderOfflineMaximumSeconds` | `5` / `120` | The shortest and longest a visitor may take a provider offline for, so a demo can never leave one down |
| `DemoNeo4jLabel` | `ShopLoyaltyMember` | Neo4j label of the demo shop's loyalty members. Also list it in `Neo4jOASIS.NativeImportLabels` |
| `MaximumSessionsPerAddressPerHour` | `20` | Demo avatars one address may create per hour |
| `MaximumSessionsPerDay` | `1000` | Demo avatars the whole sandbox may create per 24 hours |

The old `OASIS_SANDBOX` and `OASIS_SANDBOX_RETENTION_MINUTES` environment variables are no longer read.
