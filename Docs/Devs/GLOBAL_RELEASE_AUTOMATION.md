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
| Our World | `NextGenSoftwareUK/Our-World` plus the Unity project assembled by the OASIS release scripts | Version planning is complete. Publishing is guarded until its owning repository has a canonical signed Unity release workflow. |
| ODOOM | `NextGenSoftwareUK/ODOOM` | Version planning is complete. Publishing is guarded until its owning repository packages the tested UZDoom/OGEngine binaries. |
| OQUAKE | `NextGenSoftwareUK/OQUAKE` | Version planning is complete. Publishing is guarded until its owning repository packages the tested vkQuake/OGEngine binaries. |
| OIDE | `NextGenSoftwareUK/OIDE` | Creates the next `vX.Y.Z` tag on `main`; the existing three-platform OIDE release workflow builds and publishes it. |
| ONODE Manager | This repository's `release-onode-manager.yml` | Dispatches the Windows/macOS/Linux Velopack workflow. Its first canonical release is `onode-manager-v1.0.0`; later plans advance only this product-specific stable tag series. |
| OASIS HyperDrive Client | `NextGenSoftwareUK/OASIS-HyperDrive-Client` | Dispatches its Windows/Linux/macOS workflow. Its first stable release is `v1.0.0`; prereleases do not advance the monthly stable version. |

The game guard is deliberate: it prevents a source-only GitHub tag from being presented as a finished game release. Once the three owning repositories expose their canonical signed release workflows, replace the guard with the same explicit workflow dispatch used by OIDE, ONODE Manager and HyperDrive Client.

The workflow builds every selected output before publishing. If a build or metadata validation fails, publishing does not start for that job. NuGet versions are calculated from the greater of the source project version and the latest stable version on NuGet.org. GitHub release versions use the greater of the source version and the latest matching public release tag. Existing packages advance by one patch; a package absent from NuGet starts at its declared source version.

## WEB4-WEB6 API versions and Swagger histories

`AdvanceWeb4ToWeb6ApiVersions` on the release script defaults to **off**. Turn it on only while preparing an API release. It advances each of these by at least two minor versions and resets the patch to zero:

- WEB4: `OASISAPIVersion`
- WEB5: `STARAPIVersion`
- WEB6: `WEB6APIVersion`

The same operation adds matching entries to the release-history files linked by each API's Swagger page:

- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md`
- `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR API RELEASE HISTORY.md`
- `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/WEB6 API RELEASE HISTORY.md`

WEB7, WEB8, WEB9 and WEB10 are deliberately outside this option. The automation checks that their API versions did not move.

Version/history changes must be committed and pass CI before publishing from `master`. For the current release, WEB4, WEB5 and WEB6 are 5.2.0, 3.2.0 and 3.2.0 respectively; WEB7-WEB10 remain 1.0.0.

## GitHub Actions run

1. Open **Actions → Global OASIS Release → Run workflow** on `master`.
2. Leave all release components selected, or turn off components intentionally omitted from that month's release.
3. Optional application releases are off by default. Enable OIDE, ONODE Manager or HyperDrive Client when wanted. Our World, ODOOM and OQUAKE currently stop with a clear guard until their owning release workflows exist.
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
