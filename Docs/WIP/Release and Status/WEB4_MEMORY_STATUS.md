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

## Resume after the weekly usage reset (29 November 2026)

Decision (Tech Lead): stop now and finish on-demand sidecars after the reset.

- Fix the restart-on-query gap first.
- Build in CI. The local build failed with errors in the HoloNET ORM project only, before reaching WEB4, so the
  C# in `2ed1c759663` is unverified.
- Then DNA settings, `SIDECAR` logs, status endpoint and tests.
- Roll out to development first and verify there, then production and staging. Never take production down.

State at stop: no code from this branch is merged or deployed. Production sidecar flags are back to `true`.

## On-demand sidecars built (27 November 2026)

Resumed on the owner's instruction. Settings: `OASIS.Sandbox.Sidecars` in the DNA: `OnDemand` (default `false`),
`IdleMinutes` (default 10, minimum 2), `AlwaysOn` (comma-separated source providers, for example `SOLIDOASIS, GUNOASIS`).
`CqlV1Providers.Resolve` and source activation lease the sidecar, so a stopped one restarts on a query and is not
stopped mid-request. Logs start with `SIDECAR` (start, ready, stop, crash, failure; reason first-use, idle, crash,
always-on or shutdown). Wizard-only status: `GET /api/admin/cql/sidecars`. The catalogue still reports a verified
source as available while its sidecar is stopped (it has no separate "on demand" state). Start-up verification
starts every enrolled sidecar once; they stop after the idle period. Holochain is not covered. No real-sidecar CI test yet.

## Final status (27 November 2026). This supersedes the earlier sections

On-demand sidecars are live on production and development (`OnDemand: true`, `IdleMinutes: 10`, `AlwaysOn: ""`).
Code: `Development` 094477f302 and a3cb4dc0e2; `master` c28cf1718c. Start-up launches no sidecar; a query starts the
one it needs; idle stops it.

| WEB4 memory (Railway) | Before | After, idle |
|---|---|---|
| Production | about 1,765 MB | about 795 MB |
| Development | 1,600 to 1,730 MB | 780 to 840 MB |

Left at idle: the API (about 630 to 645 MB resident), Holochain (180 to 205 MB) and the supervisor (67 MB).
Production CPU at idle was 0.03 to 0.05 vCPU. Development varied from 0.17 to 0.93 vCPU; one earlier sample showed
Holochain at 86% of a core. Railway's memory figure includes file cache. Memory rises while sidecars are in use
(SOLID alone about 690 MB); production's container total was 2,141 MB just after all had been queried.

Real queries returned rows on both environments for PouchDB, GUN, Dapr, NATS, Waku, OrbitDB and OpenVINO, and on
development for IPFS and SOLID.

### Open defect: IPFS and SOLID example queries fail on production

- Symptom: `POST /api/data/query-cql` with each catalogue example returns 503 `CQL_QUERY_FAILED`, repeatably.
  The catalogue still lists both as available. The same examples return 3 rows on development.
- Errors in production logs: `CQL native failure provider=IPFSOASIS phase=query category=http` and
  `provider=SOLIDOASIS phase=query category=other`. Also `Replication source binding IPFSOASIS unavailable`
  / `Could not capture the IPFS catalogue identity`, and `Connection refused (127.0.0.1:5001)` from
  `IPFSOASIS.ActivateProviderAsync` (seen while the sidecar was stopped).
- Ruled out as the likely cause: on-demand. With `AlwaysOn: "IPFSOASIS,SOLIDOASIS"` (sidecars started with the API,
  sources activated at boot, as before) both still failed. A slow first start is also ruled out: retries with the
  sidecars running failed.
- Not ruled out: these queries were not tested on production before this change, so an old failure is not proven.
  Production source data and settings for the two (volume contents, catalogue CID, SOLID records) were not compared
  with development.
- Related: HyperDrive code outside CQL queries can try to activate IPFSOASIS without starting its sidecar.
  IPFSOASIS is in the production failover and load-balance lists.

### Other follow-ups

- Holochain is not on demand (started by `HoloOasisDevelopmentConductor`).
- No .NET or real-sidecar CI test; supervisor tests are in `Scripts/Sandbox/test_on_demand_sidecars.cjs`.
- Staging has no sidecars and was not redeployed; its Railway DNA and the STAR ODK DNA template lack
  `OASIS.Sandbox.Sidecars` (`{"OnDemand": false, "IdleMinutes": 10, "AlwaysOn": ""}`).
- The catalogue has no separate "available on demand" state.
- The REST index line for the status route is on `Development` only (conflict on `master`). The npm SDK change
  (`main` 8b3c134) is not published. The MCP, gRPC and GraphQL surfaces were not checked for the new route.
- Production and staging now have the `/api/health` deploy check (600 s). Neither deploys on push; a variable
  change builds the latest `master`.
- The Railway guide still says on-demand "is on `Development`"; it is on `master` too.

### Cause of the IPFS and SOLID production failures (found 27 November 2026, not fixed)

Production has the same `OASIS_SANDBOX_IPFS_BOOTSTRAP_JSON` and `OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON` as development,
but different `OASIS_IPFS_STORAGE_ENCRYPTION_KEY` and `OASIS_SOLID_STORAGE_KEY`. Its DNA
`IPFSOASIS.LookUpIPFSAddress` is `QmV7AwEX…`, while the seeded catalogue (and development) is `QmXZ6Sh7…`.
Inference: the seeded records were encrypted with the development keys, so production cannot read them.
Fix options: (a) generate production's own seed bundles with production's keys and set its catalogue ID, or
(b) give production the development keys and catalogue ID. Before (b), check that nothing in production's sandbox
IPFS or SOLID volumes is encrypted with the current production keys, because it would become unreadable.

### IPFS and SOLID production queries fixed (27 November 2026)

Confirmed cause: the seed records were encrypted with development's keys (production's key failed authentication on
them), and production's catalogue ID did not exist in its IPFS store. Fix: the same records re-encrypted with
production's own keys, and `OASIS_SANDBOX_IPFS_BOOTSTRAP_JSON`, `OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON`,
`IPFSOASIS.LookUpIPFSAddress` (now `QmQMABFPm1ZbeTysKfLECNnE2a7kFQ89jShsyWUZKfudpG`) and
`CQL_HOSTED_QUERY_EXAMPLES_JSON` updated. Both example queries on both sources return rows on production.
The old stores are kept on the volume as `/data/COSMIC/SandboxIPFS.pre-reseed` and `SandboxSOLID.pre-reseed`
(about 3 MB); delete them once no longer wanted.

### Next improvements requested by the owner (0 of 3 done; stopped at 96% weekly usage)

1. Holochain conductor on demand (start on first use, stop after 10 idle minutes), unless production needs it for
   real data. It is the main idle CPU cost on development and about 200 MB.
2. Build sandbox-only components into the development image only, behind a build argument.
3. `HostProfile` and per-provider `ConnectionMode` from `Docs/Devs/ONODE_HOST_PROFILES_AND_PROVIDER_HOSTING.md`,
   then move production sources to External where a free option exists. New external accounts or keys need the owner.

Roll out each to development first, then staging and production behind the health check.
