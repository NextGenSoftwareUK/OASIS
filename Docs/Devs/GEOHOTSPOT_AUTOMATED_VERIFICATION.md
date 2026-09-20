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

## Stages

| Stage | What it verifies | Default |
|---|---|---|
| WEB5 Release build | API Core, ONODE Core, STAR and controller contracts compile together | Runs |
| Focused WEB5 suite | evidence validation, linkage, distinct visits, result contracts, durable journal and child operation IDs | Runs |
| Spawn-policy matrix | 1,536 combinations, 14 named boundaries, invalid limits, and GeoNFT/GeoHotSpot policy parity | Runs |
| Our World runtime | inventory parsing, WEB4/WEB5 identity merge, transparent effects, dynamic orbit artwork, objective then quest presentation, audio separation, and readable title | Runs |
| Provider profiles | the canonical five-tree collection/progression contract against each configured provider deployment | Requires fixture |
| Two-avatar replicas | synchronized requests from two avatars to separate WEB5 replicas competing for a final allocation | Requires fixture |
| Restart/replay | interruption followed by replay with the same key, then another replay proving one committed result | Requires fixture |

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

## Provider profiles

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

Provider profiles configure provider selection through each disposable deployment's normal OASIS DNA. The automation does not override ProviderManager, disable HyperDrive, or replace automatic failover.

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

## Visual and audio evidence

Unity automation verifies objective and quest sequencing, active state, effect completion, assigned clips, suppression of reused audio, transparent materials, dynamic artwork count, and title markup. These are deterministic product invariants. Subjective judgments such as whether an animation feels pleasant or a voice sounds natural can be reviewed from a recorded build, but they are not suitable pass/fail assertions. Any such review belongs beside the automated report as optional acceptance evidence; it does not replace the automated state checks.

## Troubleshooting

- **Unity result missing:** check `unity.log`. If the project is open, wait for script compilation and confirm `BuildLogs/geohotspot-matrix-unity.json` appears.
- **Provider stage skipped:** supply `-ProviderProfilesPath`; the runner will not infer credentials.
- **Concurrency stage accepts both avatars:** confirm the fixture has one global allocation remaining and both replicas use the same provider state.
- **Restart first request succeeds:** configure the disposable instance to stop during the request; success means recovery was not exercised.
- **Pending request continues to return 503:** inspect the complete provider error. Retry the same key after restoring the failed provider; do not issue a replacement key.
