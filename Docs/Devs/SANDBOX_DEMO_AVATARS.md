# Sandbox demo avatars: creation, limits and cleanup

The public WEB4 sandbox lets a visitor try everything with one click, no account needed. `POST api/sandbox/session` creates a
throwaway verified avatar named `sandbox-<8 hex>` with the email `sandbox-<8 hex>@sandbox.oasisomniverse.one` and a random
password, routes it across the sandbox's three databases (MongoDB, SQLite, Neo4j) with its own HyperDrive settings, and signs it
in. These endpoints exist only where `OASIS_SANDBOX=true`; everywhere else they answer 404.

## Limits on creating them

`SandboxSessionLimiter` (Core `Managers/SandboxAvatarCleanup.cs`) bounds the public button:

- 20 demo avatars per address per hour, and 1,000 per 24 hours across the whole sandbox.
- A refused request answers HTTP 429 with the reason, which the sandbox page shows.
- The counts are in memory, so a restart resets them.

## Cleanup

`SandboxCleanupHostedService` runs inside the WEB4 API, **only when `OASIS_SANDBOX=true`**, so no other deployment ever runs it.
It sweeps every 15 minutes (first sweep one minute after start-up) and removes a demo avatar once it is older than the
retention period: 24 hours by default, or `OASIS_SANDBOX_RETENTION_MINUTES`.

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

Saved native-import connections (when built) will be stored with the avatar, so demo avatars lose them at cleanup like
everything else. Credentials a visitor typed into the sandbox are not kept beyond the demo avatar's lifetime.

## Tests

`OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/SandboxAvatarCleanupTests.cs`: the demo-avatar match rules,
holons-before-avatar ordering on every provider, retry after a failed deletion, one failing avatar not stopping the rest,
the per-sweep cap, and the session limiter's sliding windows.
