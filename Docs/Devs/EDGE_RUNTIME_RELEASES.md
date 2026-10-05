# OASIS Edge Runtime releases, packages and evidence

This is the release map for HyperDrive durable synchronization, Edge ONODE/runtime libraries, OGEngineClient offline
support, Unity integration, SQLite journalling and Holochain participation. It distinguishes the coordinated Edge
release from the full OASIS Runtime and from the legacy/full providers.

## What is released

The Edge Runtime is one coordinated product made from multiple reusable packages. Applications may consume only the
layer they need; OGEngineClient and the Unity package compose the normal game/mobile stack.

| Package or artifact | Purpose | Destination |
|---|---|---|
| `NextGenSoftware.OASIS.Contracts` | Portable entity contracts | NuGet |
| `NextGenSoftware.OASIS.HyperDrive.Synchronization` | Durable sync protocol and client contracts | NuGet |
| `NextGenSoftware.OASIS.ONET` | Shared ONET protocol | NuGet |
| `NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS` | Mobile/Unity-safe durable entity store and outbox | NuGet |
| `NextGenSoftware.OASIS.Edge.Runtime` | Edge state machine, offline session and synchronization coordinator | NuGet |
| `NextGenSoftware.OASIS.Edge.ONET.Runtime` | ONET transport for Edge synchronization | NuGet |
| `NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge` | Small direct Edge API for non-OGEngine hosts | NuGet |
| `NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity` | Unity-compatible Holochain adapter | NuGet |
| `NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge` | Holochain Edge replication adapter | NuGet |
| `com.nextgensoftware.oasis.edge.tgz` | Unity package containing the managed Edge stack and native SQLite assets | GitHub/Unity candidate |
| `oasis.happ` plus its build manifest | Verified Holochain 0.7 DNA/zomes | GitHub Edge release |
| `ogengine-native-edge-win-x64.zip` | Native OGEngineClient with Edge included | GitHub Edge release |
| `ogengine-native-remoteonly-win-x64.zip` | Lowest-footprint explicit remote-only native profile | GitHub Edge release |
| `OGEngineClient.Edge.v*.zip` | Managed OGEngineClient with the Edge composition | OGEngineClient GitHub release |
| `OGEngineClient.RemoteOnly.v*.zip` | Managed remote-only OGEngineClient profile | OGEngineClient GitHub release |
| `nuget-publish-candidates.zip` | Exact nine planner-versioned packages submitted to NuGet after the gate passes | Edge GitHub release evidence bundle |

The normal global NuGet release also publishes the full `NextGenSoftware.OASIS.API.Providers.HoloOASIS` and
`NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS` packages. They remain full-runtime providers. They were not
made mobile dependencies: Edge uses `HoloOASIS.Unity`/`HoloOASIS.Edge` and `EdgeSQLiteOASIS`, avoiding the full
Core/EF/server dependency graph. This is a deliberate platform boundary, not duplicate fallback behavior.

Both full-provider NuGet projects are also pack-verified at
`artifacts/provider-release-package-audit`. The full Holo provider continues to implement the shared provider and
HyperDrive replication contracts for Full ONODE; the Edge runtime consumes its purpose-built Unity and Edge
adapters. The full SQLLiteDBOASIS provider remains available to Full ONODE; the small `EdgeSQLiteOASIS` package is
the durable mobile journal/store. “Edge support” therefore does not mean loading either full server provider into a
phone process.

The Holochain dependency chain is intentionally layered:

```text
Edge Runtime -> HoloOASIS.Edge -> HoloOASIS.Unity -> HoloNET/Holochain
```

`HoloOASIS.Unity` owns Unity/mobile conductor lifecycle and native-platform integration. `HoloOASIS.Edge` owns the
adapter from that transport into the Edge Runtime replication contract. Keeping those responsibilities separate
allows another Unity application to use the Holochain bridge without the Edge runtime, and allows another Edge host
to replace the Unity lifecycle layer without importing Full OASIS Core.

There is no Edge npm package. npm is applicable to the MCP Server's JavaScript distribution; the Edge public
surfaces are NuGet, Unity UPM, NativeAOT archives and the Holochain hApp.

## GitHub Actions

| Workflow | Role |
|---|---|
| `.github/workflows/edge-runtime-validation.yml` | Reusable release gate and ordinary Edge CI; produces attested `edge-runtime-release-evidence` |
| `.github/workflows/release-edge-runtime.yml` | Standalone Preview/Publish entry point; selects only the nine Edge NuGet packages and the validated Edge bundle |
| `.github/workflows/global-release.yml` | Includes Edge Runtime by default in the coordinated global release |
| `.github/workflows/release-ogengine-client.yml` | Standalone OGEngineClient entry point; the shared release emits separate Edge and RemoteOnly archives |
| `.github/workflows/publish-nuget.yml` | Existing general NuGet publication route; the global planner is the canonical coordinated route |

`Preview` runs the same validation and packaging without publishing. `Publish` creates
`OASIS-Edge-Runtime-v<version>` and pushes the selected NuGet scope. Publishing remains explicit and runs from
reviewed `master`; a local passing gate is not a claim that GitHub or NuGet publication occurred. The workflow packs
and verifies the complete set first, preserves the exact planner-versioned candidates as
`nuget-publish-candidates.zip`, publishes that reviewed NuGet set only after the Edge gate succeeds, and creates the
GitHub release only after NuGet publication succeeds.

## Tests and generated evidence

The authoritative entry point is:

```powershell
./Scripts/validate_edge_runtime_release.ps1 `
  -Profile HoloEnabled `
  -HostedMongoSyncReport <hosted-mongo-sync.trx> `
  -HostedMongoProcessKillReport <hosted-mongo-process-kill.trx>
```

Supporting entry points:

- `Scripts/run_hosted_mongo_release_evidence.ps1` — real replica-set and abrupt-primary-loss evidence.
- `Scripts/validate_full_runtime_regression.ps1` — proves Full ONODE, Native Endpoint and STAR remain intact.
- `Scripts/build_edge_unity_package.ps1` — reproducible Unity package.
- `Scripts/validate_edge_unity_package.ps1` — Unity editor compilation.
- `Scripts/validate_our_world_android_build.ps1` — Android player build and package inspection.
- `Scripts/validate_our_world_edge_integration.ps1` — compiles Our World against the generated package.
- `Scripts/validate_ogengine_profiles.ps1` — managed Edge and RemoteOnly profile separation.
- `Scripts/validate_our_world_device_acceptance.ps1` — strict external physical-device evidence validator.
- `Scripts/EdgeReleaseInspector` — package/API/SBOM/TRX/provenance acceptance report generator.
- `Scripts/Invoke-OASISGlobalRelease.ps1 -NuGetPackageScope Edge` — plans, packs or publishes exactly the nine Edge
  NuGet packages.
- `Scripts/Test-OASISGlobalRelease.ps1` — locks global discovery and the exact Edge package scope.

The release inspector requires every TRX test to pass and rejects skipped, warning, inconclusive, pending,
not-runnable, disconnected or partially executed results. It also requires the complete package set for the selected
profile and verifies that `oasis.happ` matches its build manifest.

Current local evidence (2026-10-05):

| Profile | Tests | Directory |
|---|---:|---|
| SQLite MVP | 555/555 across 19 reports; 7 NuGet packages | `artifacts/current-goal-release-workflow-sqlite` |
| Holo enabled | 651/651 across 22 reports; 9 NuGet packages plus verified `oasis.happ` | `artifacts/current-goal-release-workflow-holo` |

Each evidence directory contains the TRX reports, NuGet packages, Unity `.tgz`, NativeAOT profiles, hApp evidence
when applicable, API compatibility report, SPDX SBOM, acceptance report, Unity/Android/Our World logs and
`SHA256SUMS.txt`. These ignored local outputs must be archived or published before generated-artifact cleanup.

Full-provider package audit outputs:

- `artifacts/provider-release-package-audit/NextGenSoftware.OASIS.API.Providers.HoloOASIS.2.0.1.nupkg`
- `artifacts/provider-release-package-audit/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.2.0.1.nupkg`

## Publication boundary

Automated builds and local evidence are complete software-release evidence, but production publication is separate.
Verify the GitHub release assets, every NuGet package page and the Actions publish jobs after a real Publish run.
Physical Android/iOS profiling and two-device Holochain field qualification remain separate release acceptance
evidence and can be made mandatory with `-RequirePhysicalDeviceEvidence`.
