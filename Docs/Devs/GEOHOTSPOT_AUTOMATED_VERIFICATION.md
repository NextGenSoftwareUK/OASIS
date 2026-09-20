# GeoHotSpot automated verification

**Updated:** 2026-09-20

This guide describes the repeatable verification system for WEB5 GeoHotSpots, WEB4 GeoNFT rewards, STAR quests, shared inventory, provider behavior, and Our World presentation. The canonical entry point is:

```powershell
./Scripts/run_geohotspot_full_matrix.ps1
```

The runner never converts a missing prerequisite into a pass. Each stage is recorded as `PASS`, `FAIL`, or `SKIP`, with evidence in `TestResults/GeoHotSpotMatrix`:

- `geohotspot-matrix.json` for CI and tooling;
- `geohotspot-matrix.md` for repository review;
- `geohotspot-matrix.html` for a readable dashboard;
- `web5-geohotspot.trx` for .NET test tooling;
- `unity-results.xml` or `unity-open-editor.json` for Unity;
- `spawn-policy.log` and `unity.log` for diagnostics.

Physical-device and deployed-environment acceptance is also machine-verifiable once the observation artifacts exist. Copy `Scripts/geohotspot-acceptance-evidence.template.json`, complete every case during the device/deployment run, attach at least one screenshot, recording, log, or trace to each case, and run:

```powershell
./Scripts/run_geohotspot_full_matrix.ps1 -PortableProviders `
  -AcceptanceEvidencePath C:\TestEvidence\geohotspot-live-acceptance.json
```

`Scripts/validate_geohotspot_acceptance_evidence.ps1` rejects missing cases, duplicate IDs, non-PASS statuses, absent notes, absent artifacts, and invalid run metadata. This makes the human observations auditable without pretending that a desktop test can observe GPS, headset rendering, speaker quality, or a deployed outage.

The exact procedure and pass criteria for all nine cases are in [GeoHotSpot live acceptance runbook](GEOHOTSPOT_LIVE_ACCEPTANCE_RUNBOOK.md).

## Stages

| Stage | What it verifies | Default |
|---|---|---|
| WEB5 Release build | API Core, ONODE Core, STAR and controller contracts compile together | Runs |
| Focused WEB5 suite | evidence validation, linkage, distinct visits, result contracts, durable journal and child operation IDs | Runs |
| Spawn-policy matrix | 1,536 combinations, 14 named boundaries, invalid limits, and GeoNFT/GeoHotSpot policy parity | Runs |
| Local provider persistence | Real SQLite activation plus avatar/holon save-load round trips; loopback MongoDB and Neo4j when configured | Runs |
| Our World runtime | inventory parsing, WEB4/WEB5 identity merge, transparent effects, dynamic orbit artwork, objective then quest presentation, audio separation, and readable title | Runs |
| Provider profiles | the canonical five-tree collection/progression contract against each configured provider deployment | Requires fixture |
| Two-avatar replicas | synchronized requests from two avatars to separate HTTP processes competing for a final allocation in shared SQLite state | Runs against the disposable local contract host; a populated fixture replaces it with full WEB5 replicas |
| Restart/replay | an in-flight process kill followed by replay with the same key, then another replay proving one committed result | Runs against the disposable local contract host; a populated fixture replaces it with a full WEB5 process |
| Device/deployment acceptance | GPS radius/dwell, AR gaze/touch, map/AR/VR/IR rendering, media, quest presentation, deployed replicas, and deployed provider interruption | Requires completed evidence JSON and referenced artifacts |

When Our World is closed, Unity runs in batch mode. When the project is already open, `GeoHotSpotMatrixAutomation` consumes `Library/GeoHotSpotMatrix.request` and runs the same test filter inside that editor. This prevents a second Unity process from corrupting or locking the project.

## Local options

```powershell
# Full local build, API, policy and Unity run
./Scripts/run_geohotspot_full_matrix.ps1

# Reuse the already verified binaries
./Scripts/run_geohotspot_full_matrix.ps1 -SkipBuild

# API-only agent or server without Unity installed
./Scripts/run_geohotspot_full_matrix.ps1 -SkipUnity

# Custom paths
./Scripts/run_geohotspot_full_matrix.ps1 `
  -OurWorldPath C:\Source\Our-World `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
  -OutputPath C:\TestEvidence\GeoHotSpot
```

`-SkipBuild` and `-SkipUnity` produce explicit `SKIP` rows. They are useful for focused development runs but do not constitute a fully passing matrix.

### Disposable local resilience environment

When `-ConcurrencyFixturePath` is omitted, the matrix no longer skips process resilience. It starts a purpose-built local host twice on ports 5055 and 5056. Both processes implement the WEB5 `POST /api/geohotspots/{id}/trigger` response contract and share a temporary SQLite WAL database. The orchestrator creates two temporary bearer identities and a GeoHotSpot with one remaining global allocation, releases both requests together, and requires exactly one acceptance.

The restart case starts a fresh host with a deterministic pre-commit delay, sends a trigger, forcibly terminates that process while the request is in flight, restarts it over the same SQLite database, and replays the same idempotency key twice. The first replay must commit and the second must return the identical count and key. Every process and temporary database is removed in `finally` unless `-KeepArtifacts` is requested.

Run it independently with:

```powershell
./Scripts/run_local_geohotspot_resilience.ps1
```

The components are:

- `Scripts/run_local_geohotspot_resilience.ps1` — complete arrange/run/cleanup orchestration;
- `Scripts/manage_local_geohotspot_resilience_host.ps1` — process lifecycle and PID files;
- `Scripts/TestHosts/geohotspot_resilience_host.py` — isolated HTTP/SQLite trigger-contract host;
- `Scripts/test_geohotspot_live_resilience.ps1` — the same black-box assertions used for full deployments.

This local host verifies the HTTP runner, true cross-process competition, durable SQLite serialization, process termination, and replay behavior without booting OASIS providers or granting real quest/inventory rewards. The focused WEB5 controller tests verify its reservation/effect/finalization rules. A populated fixture remains the integration gate for proving the complete controller and configured provider deployment across replicas; local contract-host evidence must not be relabelled as a provider integration pass.

## Provider profiles

### Disposable provider persistence without Docker

`Scripts/run_local_provider_matrix.ps1` is now part of every full matrix run. SQLite
requires no external service and creates a unique database under the current user's
temporary directory for each test. The test activates the real provider and verifies
avatar and holon save/load round trips. It does not contact WEB4, WEB5, Railway, or a
shared development database.

MongoDB and Neo4j are opt-in because they require server processes. The runner accepts
only loopback connection strings, preventing an accidental provider test against shared
infrastructure:

```powershell
$env:MONGODBOASIS_CONNECTIONSTRING='mongodb://127.0.0.1:27017'
$env:MONGODBOASIS_DBNAME='oasis-geohotspot-it'
$env:NEO4JOASIS_HOST='bolt://127.0.0.1:7687'
$env:NEO4JOASIS_USERNAME='neo4j'
$env:NEO4JOASIS_PASSWORD='<disposable-local-password>'
./Scripts/run_local_provider_matrix.ps1
```

Results are written to `TestResults/GeoHotSpotMatrix/providers`, including one TRX per
executed provider and `local-provider-matrix.json`/`.md`. An absent local server is
`SKIP`; a configured non-loopback endpoint is `FAIL`.

SQLite activation no longer deletes its database. A new database receives the current
EF schema; an existing provider database follows the migration path. This restores the
provider invariant that activation must never erase persisted avatars, holons, inventory,
or GeoNFT-related data.

Copy `Scripts/geohotspot-provider-profiles.example.json` to a secure location and configure disposable WEB4/WEB5 deployments. Each entry contains:

- `name`: evidence label such as `MongoDBOASIS`, `SQLLiteDBOASIS`, or `Neo4jOASIS`;
- `web4BaseUrl` and `web5BaseUrl`;
- `credentialPath`: a PowerShell `Export-Clixml` credential owned by the current Windows account.

Run:

```powershell
./Scripts/run_geohotspot_full_matrix.ps1 `
  -ProviderProfilesPath C:\secure\geohotspot-providers.json
```

The provider stage calls `test_our_world_tree_collection.ps1`. That script validates five canonical identities, once-per-player rules, collection history, Nature inventory rows, objective transitions, final quest completion, duplicate rejection, and reconciliation without replayed events. It resets only tagged demo quest progress before and after the run.

Each profile's `name` is passed to provider-explicit WEB4 GeoNFT routes while its disposable deployments retain their normal OASIS DNA. The automation does not override ProviderManager, disable HyperDrive, or replace automatic failover.

## Concurrency and restart fixture

Copy `Scripts/geohotspot-resilience-fixture.example.json` to a secure location. Use a dedicated hotspot whose global quantity has exactly one allocation remaining. Required fields are:

- two or more `web5BaseUrls` pointing at replicas sharing provider state;
- two avatar JWTs;
- hotspot ID, trigger type, coordinates, accuracy and duration evidence;
- `expectedAcceptedCount`, normally `1`;
- stop/start commands for a disposable WEB5 process used only by restart testing;
- `interruptDelayMilliseconds`, controlling when the runner terminates WEB5 while the trigger request is in flight.

Run:

```powershell
./Scripts/run_geohotspot_full_matrix.ps1 `
  -ConcurrencyFixturePath C:\secure\geohotspot-resilience.json
```

The concurrency stage releases two requests together through PowerShell jobs and verifies the authored accepted count. The restart stage starts the trigger in a background job, waits `interruptDelayMilliseconds`, terminates the disposable WEB5 process, restarts it, then uses one idempotency key for recovery replay and committed replay. A normally successful first response fails the test because it committed before interruption; reduce the delay or use a deliberately slower disposable provider.

## Durable transaction assertions

The transaction contract is tested at three layers:

1. `GeoHotSpotTriggerStateV1.PendingOperations` persists the allocation before effects.
2. Stable child operation IDs scope quest updates, inventory grants, embedded rewards, configured reward IDs, and GeoNFT collections.
3. Owning managers persist their operation ledgers in the same save as their mutation. Quest replay records retain completed objectives, events and reward IDs so a crash after quest persistence cannot lose downstream work.

HTTP `503 Service Unavailable` means the transaction is pending and the caller must replay the same `IdempotencyKey`. A new key represents a new trigger attempt. HyperDrive provider failover remains active throughout.

## CI

Run the local stages on every relevant pull request. Store the entire output directory as a CI artifact even on failure. Run live-provider and replica stages from a protected environment with disposable accounts and secret fixture files. A release gate should require:

- zero `FAIL` rows;
- zero `SKIP` rows for the profiles required by that environment;
- the TRX and Unity result files to exist;
- the Railway dependency manifest validator to pass after submodule advances.

Do not commit JWTs, credentials, production process commands, or populated fixtures.

## Latest verified run

On 2026-09-20 the WEB5 Release build passed, the focused WEB5 contract suite passed 22/22, the policy executable passed all 1,536 combinations plus 14 boundary cases, and the isolated Our World Unity suite passed 6/6. The disposable process suite also passed synchronized two-avatar competition across two host processes and an actual in-flight process kill, restart, first replay commit, and second idempotent replay. The earlier live development Anorak run passed all five unique pickups, duplicate rejection, exact objective transitions, cleanup to 0/5, and preservation of 26 unrelated inventory rows.

Portable loopback provider verification was completed on 2026-09-20 without installing a Windows service or contacting a shared database. Official MongoDB 8.3.8, Neo4j Community 2026.08.1, MongoDB Shell 2.6.0, and Adoptium JDK 21 archives were unpacked below `TestResults/GeoHotSpotMatrix/portable-providers`. MongoDB ran as a single-node replica set on `127.0.0.1:27028`, preserving the provider's transaction requirement, and Neo4j ran on `127.0.0.1:7688`. The final `providers-portable/local-provider-matrix.json` records `PASS 3 / FAIL 0 / SKIP 0`: SQLite 3/3, MongoDB 3/3, and Neo4j 5/5. Both processes were stopped after the run.

This is direct provider activation and persistence evidence, not a claim that complete disposable WEB4 and WEB5 HTTP deployments were run for each backend. A populated full-WEB5 replica fixture is still required for the separate multi-replica HTTP stage.

Reproduce the portable provider run with cached downloads on later runs:

```powershell
.\Scripts\run_portable_provider_matrix.ps1
```

Or include it in the consolidated matrix:

```powershell
.\Scripts\run_geohotspot_full_matrix.ps1 -PortableProviders
```

The script pins official archive versions, creates fresh data below `TestResults`, binds every database listener to `127.0.0.1`, preserves MongoDB's replica-set transaction invariant, and verifies in `finally` that all three database ports have closed.

### Storage-provider harness audit

The local provider harnesses have two distinct responsibilities:

- backend-free unit projects validate provider identity and construction without pretending that a database operation ran;
- populated provider profiles execute the complete five-tree HTTP contract against disposable WEB4/WEB5 deployments.

The audit corrected the MongoDB unit and integration projects' stale project path (`MongoOASIS` is the repository directory), removed backend activation from unit scope, and made the integration suite construct and activate the provider from `MONGODBOASIS_CONNECTIONSTRING` and `MONGODBOASIS_DBNAME`. MongoDB integration now verifies avatar-by-id, avatar-by-username, and holon save/load round trips against a transaction-capable replica set. Neo4j integration now requires explicit `NEO4JOASIS_HOST`, `NEO4JOASIS_USERNAME`, and `NEO4JOASIS_PASSWORD`; the provider accepts either a complete Bolt/Neo4j URI or the legacy host form, and activation verifies live connectivity instead of inheriting the base placeholder. SQLite integration constructs against an isolated database. The obsolete SQLite source file that asserted removed interfaces and “not supported” persistence is excluded from compilation. Neo4j's backend-free metadata suite remains valid. The WEB5 suite now also persists a pending trigger journal to a disposable file, reloads it through an independent store instance, commits it, reloads again, and verifies that the idempotency result survives without process-static state.

These tests establish the provider-independent serialization and replay contract. They do not claim MongoDB, SQLite, Neo4j, or another provider is integration-tested unless its populated provider profile completes successfully. `Neo4jOASIS2` remains a commented, non-implementation provider and is not counted as a usable provider.

## Visual and audio evidence

Unity automation verifies objective and quest sequencing, active state, effect completion, assigned clips, suppression of reused audio, transparent materials, dynamic artwork count, and title markup. These are deterministic product invariants. Subjective judgments such as whether an animation feels pleasant or a voice sounds natural can be reviewed from a recorded build, but they are not suitable pass/fail assertions. Any such review belongs beside the automated report as optional acceptance evidence; it does not replace the automated state checks.

## Troubleshooting

- **Unity result missing:** check `unity.log`. If the project is open, wait for script compilation and confirm `BuildLogs/geohotspot-matrix-unity.json` appears.
- **Provider stage skipped:** supply `-ProviderProfilesPath`; the runner will not infer credentials.
- **Local host cannot start:** install Python 3 or pass `-PythonPath`; the manager also detects the Codex bundled runtime when available.
- **Concurrency stage accepts both avatars:** confirm the fixture has one global allocation remaining and both replicas use the same provider state.
- **Restart first request succeeds:** configure the disposable instance to stop during the request; success means recovery was not exercised.
- **Pending request continues to return 503:** inspect the complete provider error. Retry the same key after restoring the failed provider; do not issue a replacement key.
