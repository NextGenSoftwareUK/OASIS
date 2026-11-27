# WEB4 memory at rest: findings (26 November 2026)

Status: measured only. No code, Railway variable or deployment was changed. Memory is the same before and after.

## Measured (read-only `railway ssh`, WEB4 container)

Production cgroup total: 1,663 MB (development: 1,481 MB). Non-reclaimable part in production is about 1,190 MB
(anonymous 972, shared 161, kernel 57); the rest is file cache. Private memory below is RssAnon.

| Process (production) | RSS MB | Private MB |
|---|---|---|
| WEB4 .NET API | 644 | 307, plus 146 for `memfd:doublemapper` |
| SOLID Community Server | 259 | 206 |
| Holochain conductor (HoloOASIS local demo) | 193 | 160 |
| OrbitDB teaching server | 136 | 76 |
| Dapr (daprd) | 133 | 27 |
| OpenVINO model server | 124 | 36 |
| Dapr state store (Garnet) | 102 | 36 |
| Sandbox supervisor (node) | 82 | 30 |
| PouchDB | 78 | 26 |
| GUN | 77 | 25 |
| IPFS | 67 | 15 |
| Waku | 38 | 10 |
| NATS | 23 | 8 |

The 12 sidecar processes hold about 670 MB of private memory, more than the API itself (about 450 MB).

## The provider-loading theory is not the cause

- Boot type is `Warm`. Only providers named in the DNA lists are registered (about 14), and `RegisterProvider`
  activates one only when the boot type is `Hot`. Start-up activates the default provider, or none under
  prepared storage admission (`registrationOnly`). Making this lazier would save little.
- Not measured: the split of the API's 307 MB between managed heap, JIT code and native allocations.

## Options (none applied)

1. Turn off sidecars in production, keeping them on development. Set the `OASIS_SANDBOX_*` flags to `false` and
   disable the HoloOASIS local demo (`CQL_HOLOOASIS_LOCAL_DEMO__Enabled`). Saves up to about 670 MB private memory.
   Cost: those CQL sandbox sources stop working in production. The matching source settings
   (`CQL_*`, `PREPARED_STORAGE_ADMISSION_JSON`) must be checked so start-up does not fail; test on development first.
2. Turn off only the two heaviest (SOLID and the Holochain demo): about 365 MB, losing those two sources.
3. Set `DOTNET_EnableWriteXorExecute=0`: about 150 MB, no feature change. It removes a .NET memory-protection
   hardening (write-xor-execute), so it needs the owner's approval.
4. Start sidecars on first use and stop them when idle. Keeps every source, but is a larger change to
   `Docker/host-sandbox-*.cjs` and the source health checks.

Recommendation: option 1, tried on development first, then option 4 if the sources must stay live in production.

## On-demand sidecars: where work stopped (27 November 2026)

**Do not merge commit `2ed1c759663` as it stands.** `CqlHostedSources` activates every enrolled source at start-up
through `ActivateReadSourceAsync`, and query execution does not call it again. With that commit and
`OASIS_SANDBOX_IDLE_MINUTES` set, sidecars would start at boot, stop when idle and not restart for a query.
Without that variable behaviour is unchanged. Its WEB4 build had not finished, so the C# part is unverified.

Done: `Docker/host-sandbox-ondemand.cjs` (start on request, one shared start, idle stop) and
`Scripts/Sandbox/test_on_demand_sidecars.cjs` (passes).

Remaining:
1. Take a lease around each query's execution (acquire before, release after), so a stopped sidecar restarts
   and is never stopped mid-request. Decide whether start-up verification starts then idles them, or is deferred.
2. DNA `OASIS.Sandbox.Sidecars` (OnDemand, IdleMinutes default 10 and minimum 2, AlwaysOn) with typed defaults,
   every OASIS_DNA copy and Railway `OASIS_DNA_JSON`.
3. `SIDECAR` log lines, Wizard status endpoint `GET /api/admin/cql/sidecars`, "available on demand" in the catalogue.
4. Fake-clock tests for in-flight, crash restart and always-on; one CI test with a real sidecar.
5. Holochain is started by WEB4 itself (`HoloOasisDevelopmentConductor`) and needs its own idle stop.
6. Roll out to dev, then production and staging (staging currently has no sidecars).

## Production outage, 27 November 2026

Setting every `OASIS_SANDBOX_*` flag to `false` in production crashed WEB4 at start-up
("PouchDB CQL requires its owned native runtime to be enabled"): each source's settings must be removed with
its flag. WEB4 production returned 502 for a few minutes (deployment 02:07:52 UTC, restored about 02:19 UTC by
restoring the flags and rolling back). Production has no deploy health check configured, so a crashing
deployment replaced the working one. `ONODE/.../railway.json` declares `/api/health`, but it is not applied.
