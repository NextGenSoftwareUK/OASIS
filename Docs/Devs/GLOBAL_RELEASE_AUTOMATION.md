# OASIS global release automation

`Global OASIS Release` is the repeatable monthly release workflow for the public OASIS deliverables. Run it once or twice per month after the normal `Development` CI is green and the intended release commit has been promoted to `master`.

The workflow and local script use the same release plan. Every plan records the exact source commit, selected components, calculated versions and all first-party NuGet package versions in `artifacts/global-release/release-plan.json`.

## Released components

All component switches default to **on**:

| Component | Output |
|---|---|
| All first-party NuGet projects | One validated `.nupkg` per project, including packages that have never been published |
| OASIS Runtime | `OASIS-Runtime-vX.Y.Z` GitHub release and `OASIS.Runtime.vX.Y.Z.zip` |
| STAR ODK Runtime | `STAR-ODK-Runtime-vX.Y.Z` GitHub release and STAR Runtime/CLI archive |
| OGEngineClient | `OGEngineClient-vX.Y.Z` GitHub release and archive; this is the renamed STAR API Client |
| Native Endpoint | `Native-Endpoint-vX.Y.Z` GitHub release and archive |
| MCP Server | Existing comprehensive MCP workflow: native binaries, GitHub release, NuGet package and npm package |

The following application releases are also represented in the same plan, with their versions always written to `release-plan.json`, but their switches default to **off** because they are larger, independently signed products:

| Optional component | Release owner | Current automation |
|---|---|---|
| Our World | `NextGenSoftwareUK/Our-World` | Atomically builds Android APK, iOS Xcode project, Windows, Linux, macOS and tvOS Xcode project artifacts from Unity 2022.3.62f3. Its hosted workflow currently cannot import the Unity Personal entitlement XML because GameCI requires a legacy ULF; use a supported route in the [three-game release map](THREE_GAME_EDGE_RELEASE_ARTIFACT_MAP.md#our-world-unity-licensing-blocker). |
| ODOOM | `NextGenSoftwareUK/ODOOM` | Builds Windows x64, Linux x64 and macOS with the current platform-specific OGEngineClient NativeAOT runtime and SQLite dependency, verifies every package, then publishes atomically. The full non-publishing matrix passed on 28 September 2026. |
| OQUAKE | `NextGenSoftwareUK/OQUAKE` | Builds Windows x64, Linux x64 and macOS arm64 with the current platform-specific OGEngineClient NativeAOT runtime and SQLite dependency, verifies every package, then publishes atomically. The full non-publishing matrix passed on 28 September 2026. |
| OIDE | `NextGenSoftwareUK/OIDE` | Creates the next `vX.Y.Z` tag on `main`; the existing three-platform OIDE release workflow builds and publishes it. |
| ONODE Manager | This repository's `release-onode-manager.yml` | Dispatches the Windows/macOS/Linux Velopack workflow. Its first canonical release is `onode-manager-v1.0.0`; later plans advance only this product-specific stable tag series. |
| OASIS HyperDrive Client | `NextGenSoftwareUK/OASIS-HyperDrive-Client` | Dispatches its Windows/Linux/macOS workflow. Its first stable release is `v1.0.0`; prereleases do not advance the monthly stable version. |

The global workflow generates component-specific, version-specific game notes from each owning repository and dispatches that repository's canonical release workflow. A game release is created only after its integrated executable and required OGEngine runtime library have been built and verified.

The dated distinction between passing rehearsals and permanent public downloads is recorded in the [three-game release validation and distribution snapshot](THREE_GAME_EDGE_RELEASE_ARTIFACT_MAP.md#release-validation-status-28-september-2026). A green run with `publish=false` is build evidence, not a public release.

The workflow builds every selected output before publishing. If a build or metadata validation fails, publishing does not start for that job. NuGet versions are calculated from the greater of the source project version and the latest **listed** stable version on NuGet.org. GitHub release versions use the greater of the source version and the latest matching public release tag. Existing packages advance by one patch. Every package absent from NuGet starts at `1.0.0`, independently of the assembly/API version declared by its source project.

Runtime GitHub releases use separate generated notes for OASIS Runtime, STAR ODK Runtime, OGEngineClient, Native Integrated Endpoint and MCP Server. `Scripts/New-OASISReleaseNotes.ps1` starts with the component introduction, finds its previous release tag, reads commits affecting that component, groups the version-specific features/fixes/other changes, and ends with that component's compare link. The workflow never reuses platform-wide notes for component releases.

Every NuGet package receives the same structure in its embedded `PackageReleaseNotes`: package description, target version, changes to that project since its latest listed NuGet publication, and a package-path changelog link. A package with no prior listed version receives initial-release contents and starts at `1.0.0`.

When WEB4-WEB6 API advancement is explicitly enabled, the Swagger-linked release-history files are updated independently. Each new entry contains the API introduction, every distinct non-merge commit affecting that API since its previous related release tag, a direct link to each commit, and a full comparison link. The generator does not truncate long version changelogs. WEB7-WEB10 histories and versions are not advanced by this option.

## WEB4-WEB6 API versions and Swagger histories

`AdvanceWeb4ToWeb6ApiVersions` on the release script defaults to **off**. Turn it on only while preparing an API release. It advances each of these by at least two minor versions and resets the patch to zero:

- WEB4: `OASISAPIVersion`
- WEB5: `STARAPIVersion`
- WEB6: `WEB6APIVersion`

The same operation adds matching entries to the release-history files linked by each API's Swagger page:

- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md`
- `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR API RELEASE HISTORY.md`
- `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/WEB6 API RELEASE HISTORY.md`

WEB5 and WEB6 source histories live in their owning private submodules, while their Swagger links point to public synchronized copies in this repository:

- `Docs/API/WEB5-STAR-API-RELEASE-HISTORY.md`
- `Docs/API/WEB6-AI-API-RELEASE-HISTORY.md`

The release planner updates both copies in the same preparation PR, and Publish rejects a release if their SHA-256 hashes differ.
All three Swagger histories are ordered chronologically by semantic version, with the oldest release at the top and the newest at the bottom. The release planner appends new entries and the automation test rejects an out-of-order or missing required history.

WEB7, WEB8, WEB9 and WEB10 are deliberately outside this option. The automation checks that their API versions did not move.

Version/history changes must be committed and pass CI before publishing from `master`. For the current release, WEB4, WEB5 and WEB6 are 5.2.0, 3.2.0 and 3.2.0 respectively; WEB7-WEB10 remain 1.0.0.

## Individual releases, version modes and preview

The global workflow is the coordinated monthly orchestrator. Every independently releasable component must also have a manual workflow: WEB4, WEB5, WEB6, MCP Server, OASIS Runtime, STAR ODK Runtime, OGEngineClient, Native Endpoint, Our World, ODOOM, OQUAKE, OIDE, ONODE Manager and HyperDrive Client. Each entry calls shared planning/build/publish code; it must not copy release logic. External products run their canonical owning-repository workflow. A missing tested binary workflow is a preview blocker, never permission to create a source-only release.

Each component offers `Automatic` (default), `Patch`, `Minor`, `Major` and `Manual`. Automatic selects major only for `BREAKING CHANGE`, a Conventional Commit `!`, or `release:major`; new public capabilities select minor; fixes, documentation, packaging and internal changes select patch. `release:major`, `release:minor`, `release:patch` and `release:none` override inference. New public packages start at `1.0.0`. Manual versions must be stable SemVer, greater than the latest publication and unused at every destination. No relevant changes stops the release.

`Preview` is the default and never publishes. It produces a retained plan and Actions summary containing the source commit, current/proposed version, bump reason, relevant commits/PRs, full release notes and comparison, assets/platforms, destinations and blockers. When tracked source must change it creates a release-preparation PR. WEB4-WEB6 previews change only the selected API's BootLoader version and Swagger-linked history; all other API versions remain unchanged.

`Publish` accepts only that reviewed plan after its preparation PR reaches `master`. It verifies source commit, version, notes, artifacts and destinations before any mutation; drift requires a fresh preview. Builds and tests finish before publishing. The global workflow exposes the same per-component modes and defaults them to `Automatic`.

## GitHub Actions run

1. Open **Actions → Global OASIS Release → Run workflow** on `master`.
2. Leave all release components selected, or turn off components intentionally omitted from that month's release.
3. Optional application releases are off by default (`optional_components` is `none`). Replace it with a comma-separated list using `our_world`, `odoom`, `oquake`, `oide`, `onode_manager`, and/or `hyperdrive_client`. Each selected external product dispatches its owning repository's canonical workflow.
4. Keep `publish` enabled for the real release. Disable it for a complete pack/build rehearsal.

API version/history advancement is intentionally a source-preparation operation rather than an ephemeral GitHub runner change. Run the local command below, commit the parent and submodule history changes, pass CI, promote to `master`, and then run the publishing workflow.

Required repository secrets are `PRIVATE_SUBMODULE_PAT` and `NUGET_API_KEY`. npm uses GitHub trusted publishing with OIDC and requires the `@oasisomniverse` npm trusted-publisher configuration described in the MCP release documentation.

## Local commands

Create and validate an online plan without changing API versions:

```powershell
.\Scripts\Invoke-OASISGlobalRelease.ps1 -Operation Plan
```

Prepare an API release and update the three Swagger histories:

```powershell
.\Scripts\Invoke-OASISGlobalRelease.ps1 `
  -Operation Plan `
  -AdvanceWeb4ToWeb6ApiVersions $true `
  -ReleaseNotes "Describe the user-visible API changes here."
```

Build every NuGet package without publishing:

```powershell
.\Scripts\Invoke-OASISGlobalRelease.ps1 -Operation Pack
```

Publish after review and successful CI:

```powershell
$env:NUGET_API_KEY = '<nuget API key>'
.\Scripts\Invoke-OASISGlobalRelease.ps1 -Operation Publish
```

The `.bat` and `.sh` wrappers accept the same arguments. Run `Scripts/Test-OASISGlobalRelease.ps1` (or its wrapper) to validate defaults, metadata, package discovery, and the WEB4-WEB10 version boundary.

## Source files and outputs

| Purpose | Location |
|---|---|
| Global workflow | `.github/workflows/global-release.yml` |
| Planner/packager/publisher | `Scripts/Invoke-OASISGlobalRelease.ps1` plus `.bat` and `.sh` wrappers |
| Automation test | `Scripts/Test-OASISGlobalRelease.ps1` plus `.bat` and `.sh` wrappers |
| Release plan | `artifacts/global-release/release-plan.json` |
| NuGet output | `artifacts/global-release/nuget/` |
| Runtime output | GitHub release assets and the `global-release-runtime-assets` workflow artifact |
| Comprehensive GitHub release body | `Docs/Releases/GLOBAL_RELEASE_NOTES.md` |
| Complete CI/release map | `Docs/Devs/CI_CD_WORKFLOWS_AND_RELEASES.md` |

The emergency disk cleanup wrappers in `Scripts/tools/cleanup-emergency-space.*` remove only the disposable `artifacts/global-release-validation` and `artifacts/global-release-optional-version-check` directories created by release-plan checks. They preserve `artifacts/global-release`, packaged releases, and all build output trees. Pass `-KeepOASISTemporaryArtifacts` on Windows or `--keep-oasis-temp-artifacts` on Linux to retain the validation directories too. The Windows cleanup resolves and checks every target against the repository root before deletion.
