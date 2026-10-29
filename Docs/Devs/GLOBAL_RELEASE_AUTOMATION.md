# OASIS global release automation

`Global OASIS Release` is the repeatable monthly release workflow for the public OASIS deliverables. Run it once or twice per month after the normal `Development` CI is green and the intended release commit has been promoted to `master`.

The workflow and local script use the same release plan. Every plan records the exact source commit, selected components, calculated versions and all first-party NuGet package versions in `artifacts/global-release/release-plan.json`.

## Released components

All component switches default to **on**:

| Component | Output |
|---|---|
| All first-party NuGet projects | One validated `.nupkg` per project, including packages that have never been published |
| OASIS Runtime | `OASIS-Runtime-vX.Y.Z` GitHub release and `OASIS.Runtime.vX.Y.Z.zip` |
| STAR ODK Runtime | `STAR-ODK-Runtime-vX.Y.Z` GitHub release with self-contained STAR Runtime/CLI archives for Windows x64, Linux x64/ARM64, and macOS x64/ARM64 |
| OASIS Edge Runtime | `OASIS-Edge-Runtime-vX.Y.Z` GitHub release with attested Edge evidence, Unity package, Holochain hApp, Edge/RemoteOnly native profiles, and nine coordinated NuGet packages |
| OGEngineClient | `OGEngineClient-vX.Y.Z` GitHub release and archive; this is the renamed STAR API Client |
| Native Endpoint | `Native-Endpoint-vX.Y.Z` GitHub release and archive |
| MCP Server | Existing comprehensive MCP workflow: native binaries, GitHub release, NuGet package and npm package |

The following application releases are also represented in the same plan, with their versions always written to `release-plan.json`, but their switches default to **off** because they are larger, independently signed products:

| Optional component | Release owner | Current automation |
|---|---|---|
| Our World | `NextGenSoftwareUK/Our-World` | Atomically builds Android APK, iOS Xcode project, Windows, Linux, macOS and tvOS Xcode project artifacts from Unity 2022.3.62f3. Its hosted workflow currently cannot import the Unity Personal entitlement XML because GameCI requires a legacy ULF; use a supported route in the [three-game release map](<../WIP/Edge and Games/THREE_GAME_EDGE_RELEASE_ARTIFACT_MAP.md#our-world-unity-licensing-blocker>). |
| ODOOM | `NextGenSoftwareUK/ODOOM` | Builds Windows x64, Linux x64 and macOS with the current platform-specific OGEngineClient NativeAOT runtime and SQLite dependency, verifies every package, then publishes atomically. The verified three-platform packages are attached to [ODOOM v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/ODOOM_v.1.0.0_ALPHA). |
| OQUAKE | `NextGenSoftwareUK/OQUAKE` | Builds Windows x64, Linux x64 and macOS arm64 with the current platform-specific OGEngineClient NativeAOT runtime and SQLite dependency, verifies every package, then publishes atomically. The verified three-platform packages are attached to [OQuake v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/OQUAKE_v1.0.0_ALPHA). |
| OIDE | `NextGenSoftwareUK/OIDE` | Creates the next `vX.Y.Z` tag on `main`; its workflow tests, packages and publishes Windows, Linux and macOS artifacts. The first canonical release is [OIDE v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/OIDE/releases/tag/v1.0.0). |
| ONODE Manager | This repository's `release-onode-manager.yml` | Dispatches the Windows/macOS/Linux Velopack workflow. The first canonical release is [ONODE Manager v1.0.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/onode-manager-v1.0.0); later plans advance only this product-specific stable tag series. |
| OASIS HyperDrive Client | `NextGenSoftwareUK/OASIS-HyperDrive-Client` | Dispatches its Windows/Linux/macOS workflow. Its first stable release is `v1.0.0`; prereleases do not advance the monthly stable version. |

The global workflow generates component-specific, version-specific game notes from each owning repository and dispatches that repository's canonical release workflow. A game release is created only after its integrated executable and required OGEngine runtime library have been built and verified.

The dated distinction between passing rehearsals and permanent public downloads is recorded in the [three-game release validation and distribution snapshot](<../WIP/Edge and Games/THREE_GAME_EDGE_RELEASE_ARTIFACT_MAP.md#release-validation-status-28-september-2026>). A green run with `publish=false` is build evidence, not a public release.

The workflow builds every selected output before publishing. If a build or metadata validation fails, publishing does not start for that job. NuGet versions are calculated from the greater of the source project version and the latest **listed** stable version on NuGet.org. GitHub release versions use the greater of the source version and the latest matching public release tag. Existing packages advance by one patch. Every package absent from NuGet starts at `1.0.0`, independently of the assembly/API version declared by its source project.

Runtime GitHub releases use separate generated notes for OASIS Runtime, STAR ODK Runtime, OASIS Edge Runtime, OGEngineClient, Native Integrated Endpoint and MCP Server. `Scripts/New-OASISReleaseNotes.ps1` starts with the component introduction, finds its previous release tag, reads commits affecting that component, groups the version-specific features/fixes/other changes, and ends with that component's compare link. The workflow never reuses platform-wide notes for component releases.

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

### One switch for every API (WEB4-WEB10)

Tick `advance_api_versions` in **Global OASIS Release** (or pass `-AdvanceWeb4ToWeb6ApiVersions` and `-AdvanceWeb7ToWeb10ApiVersions` to the script). The run then, with no manual steps:

- checks each API for changes (commits to its paths since the commit that last wrote its release history) and advances only those that changed, by the `version_mode` bump; an unchanged API keeps its version and gets no history entry, and nothing fails because of it. For each changed API it advances the `WEBnAPIVersion` constant and adds its release-history entry (WEB7-WEB10 histories live in this repo; WEB5/WEB6 histories are committed to their submodules and the pins updated);
- sets `OASISRuntimeVersion` equal to `OASISAPIVersion` (the runtime ships with WEB4), and rewrites the version sentence above;
- in OPORTAL-JS (`dev`), renames each Postman collection to the new version, fixes its title and every portal link;
- commits all of it (this repo to the branch the run started from, OPORTAL-JS to `dev`).

The helper `Scripts/api_version_sync.py` does the propagation and every run of the workflow applies it (`--sync`), so drift is corrected rather than reported; `Test-OASISGlobalRelease.ps1` reads the versions from the constants instead of hardcoding them. Needs `PRIVATE_SUBMODULE_PAT` with write access to OPORTAL-JS and the private submodules.

Version/history changes must be committed and pass CI before publishing from `master`. For the current release, WEB4, WEB5 and WEB6 are 5.3.0, 3.2.0 and 3.2.0 respectively; WEB7-WEB10 remain 1.0.0.

## How the next version is decided

Everything below is done by `Scripts/Invoke-OASISGlobalRelease.ps1`, started by the **Global OASIS Release** workflow. There are no per-component release buttons any more: each component is one checkbox on that workflow. Maintenance paths are in **Global release recovery** (see the end of this section).

### Version mode (the `version_mode` option)

| Mode | What happens |
|---|---|
| `Automatic` (default) | The version bump is worked out from the changes, using the rules below |
| `Patch`, `Minor`, `Major` | Forces that bump for every selected component that has changes |
| `Manual` | You type the exact version in `manual_version` (stable `X.Y.Z`, greater than the current one). Only one component may be selected |

### The rules in Automatic mode

For each component the script lists the commits that touched that component's code since its last release (see "Last release" below). Commits whose subject starts with `Merge `, `Promote `, `chore: bump submodule` or `chore: update submodule` are ignored. Then, first match wins:

| Step | Looks for | Result |
|---|---|---|
| 1. Labels | A `release:major`, `release:minor`, `release:patch` or `release:none` label on the pull request of any of the (up to) 50 most recent relevant commits. If several labels are found the order is major, minor, patch, none | That bump. `release:none` blocks the release of that component |
| 2. Breaking | A commit **subject** containing `BREAKING CHANGE`, or a conventional-commit `!`, such as `feat!: ...` or `fix(auth)!: ...` | Major |
| 3. Feature | A commit subject that **starts with** `feat:`, `add:` or `implement:`, optionally with a scope, such as `feat(sandbox): ...` | Minor |
| 4. Anything else | Fixes, docs, refactors, packaging, free-text messages | Patch |

Only subjects are read, not commit bodies. Free-text messages that do not start with one of those prefixes count as patch, so use `feat:` / `fix:` prefixes (or a release label on the PR) when you want a minor or major bump.

| Example commit subject | Bump |
|---|---|
| `feat(native-data): save connections` | Minor |
| `fix: sandbox cleanup skips new avatars` | Patch |
| `feat!: remove the v1 holon routes` | Major |
| `Sandbox and native import: settings come from DNA` | Patch (free text) |
| Any of the above on a PR labelled `release:minor` | Minor |

### APIs (WEB4 to WEB10): routes are checked as well

An API's routes are its public contract, so for each API the script also compares the HTTP routes at its last release with the routes now, read from the controllers folder of that API:

| API | Controllers folder |
|---|---|
| WEB4 | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers` |
| WEB5 | `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers` |
| WEB6 | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers` |
| WEB7 to WEB10 | `WEBn/NextGenSoftware.OASIS.Webn.WebAPI/Controllers` |

- A route is the class `[Route("...")]` prefix plus each `[HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch("...")]` template, with `[controller]` replaced by the controller name. Parameter names are ignored, so renaming `{id}` to `{avatarId}` is not a change.
- A route **removed or changed** (a different verb or path counts as removed plus added) raises the bump to **Major**.
- A route **added** raises a patch to **Minor**.
- The route check only ever **raises** the bump. A release label (step 1) overrides it.
- It cannot see changes inside a route: renamed or removed request or response fields, a new required parameter, or behaviour changes. Those are caught only by commit prefixes and labels, so mark such commits `feat!:` or label the PR `release:major`.
- It reads attribute routing only. Routes defined any other way are not seen.
- The route comparison is `Scripts/api_route_diff.py`.

### Last release (where "since" starts)

- **WEB4 to WEB10:** the APIs are deployed from Railway and have no release tags. Their last release is the commit that last wrote their release-history file (for example `WEB4 OASIS API RELEASE HISTORY.md`). Anything committed to the API's code after that commit counts as a change.
- **Other components** (runtimes, NuGet packages, MCP, and so on): the latest release tag for that component, and for NuGet the version already on the registry. A NuGet package whose inputs have not changed is not packed or pushed.

### What happens to an API that has not changed

With `advance_api_versions` ticked, an API with no commits to its code since its last release is skipped: its version stays, no history entry is written, nothing fails. The plan records "no changes since its last release".

### What `advance_api_versions` does for each API that did change

1. Works out the bump with the rules above and raises that API's `WEBnAPIVersion` in `OASISBootLoader.cs`. WEB4 is `OASISAPIVersion` and WEB5 is `STARAPIVersion`.
2. Writes its release-history entry: the commits since the last release, each linked, with a full-changelog link. WEB5 and WEB6 histories are committed to their submodules and the Railway dependency pins updated; WEB4 and WEB7 to WEB10 are in this repository.
3. Sets `OASISRuntimeVersion` equal to the WEB4 API version, and rewrites the version sentence in this document.
4. In OPORTAL-JS (branch `dev`), renames each Postman collection to the new version, fixes its title and every portal link.
5. Commits all of it (this repository to the branch the run started from, OPORTAL-JS to `dev`).

`Scripts/api_version_sync.py` makes sure every place holding a version agrees, and it fixes any drift instead of failing. `Scripts/Test-OASISGlobalRelease.ps1` reads the versions from the constants rather than hardcoding them. Swagger, the version endpoint and the startup log read the constants at runtime, so they need no edit. The commit step needs `PRIVATE_SUBMODULE_PAT` with write access to OPORTAL-JS and the private submodules.

### The Global release form (12 options)

`publish` (off = build and validate only), `advance_api_versions`, `nuget_packages`, `oasis_runtime`, `star_runtime`, `edge_runtime`, `ogengine_client` (RemoteOnly, Edge and Edge + Unity together; needs `nuget_packages`), `native_endpoint`, `mcp_server`, `optional_components` (comma list of `our_world`, `odoom`, `oquake`, `oide`, `onode_manager`, `hyperdrive_client`), `version_mode`, `manual_version`. NuGet packages always release as the complete set. `onode_manager` runs `release-onode-manager.yml` as a step of the same run; OIDE, HyperDrive Client and the games dispatch their own repositories' workflows.

### Global release recovery

**Global release recovery** (`global-release-recovery.yml`) holds the rare paths, off the main form: *Review NuGet plan only* (builds the plan, packs and publishes nothing) and *Resume partly published NuGet batch* (give the already-published package IDs and their exact count; a wrong count stops before any upload).

### Preview, publish and safety

With `publish` off, the run plans, builds and validates but publishes nothing; the plan and release notes are kept as run artifacts. With `publish` on, publishing only happens after the build jobs have passed, using the plan produced in that run.

## GitHub Actions run

1. Open **Actions → Global OASIS Release → Run workflow** and choose the branch in "Use workflow from" (the form and the code come from that branch).
2. Leave all release components selected, or turn off components intentionally omitted from that month's release.
3. Optional application releases are off by default (`optional_components` is `none`). Replace it with a comma-separated list using `our_world`, `odoom`, `oquake`, `oide`, `onode_manager`, and/or `hyperdrive_client`. Each selected external product dispatches its owning repository's canonical workflow.
4. Keep `publish` enabled for the real release. Disable it for a complete pack/build rehearsal.

API versions are advanced by the same run: tick `advance_api_versions` (see "How the next version is decided"). Nothing needs to be run locally or committed by hand.

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
| Edge package/evidence/release map | `Docs/WIP/Edge and Games/EDGE_RUNTIME_RELEASES.md` |

The emergency disk cleanup wrappers in `Scripts/tools/cleanup-emergency-space.*` remove only the disposable `artifacts/global-release-validation` and `artifacts/global-release-optional-version-check` directories created by release-plan checks. They preserve `artifacts/global-release`, packaged releases, and all build output trees. Pass `-KeepOASISTemporaryArtifacts` on Windows or `--keep-oasis-temp-artifacts` on Linux to retain the validation directories too. The Windows cleanup resolves and checks every target against the repository root before deletion.
