# OASIS release workflow operations

## Which workflow should I run?

Normal coordinated releases use **Global OASIS Release** (`global-release.yml`). Component entry workflows dispatch it with the corresponding options. **Release — OASIS Edge Runtime** (`release-edge-runtime.yml`) additionally has an artifact-only publication option for reusing already validated builds; that recovery job does not run Global OASIS Release again.

Normal sequence: checked Development-to-master promotion, review/merge the exact green PR, verify staging/production deployments and affected endpoints, then dispatch the release workflow from **master**. Follow [promotion instructions](DEVELOPMENT_TO_MASTER_PROMOTION.md). Do not assume a successful workflow dispatch means publication completed.

## Global OASIS Release options

| Input | Meaning |
|---|---|
| `publish` | `false`: preview/build without public uploads; `true`: publish after applicable job gates |
| `nuget_packages` | Pack/publish selected reusable first-party NuGet packages |
| `nuget_package_scope` | `All`: all packable first-party projects; `Edge`: nine reviewed Edge packages |
| `oasis_runtime` | OASIS Runtime GitHub archives |
| `star_runtime` | STAR Runtime/CLI GitHub archives |
| `edge_runtime` | Edge validation, aggregate bundle and GitHub publication; requires NuGet packages |
| `ogengine_client` | OGEngineClient managed Edge/remote-only profiles; platform audit remains outstanding |
| `native_endpoint` | Integrated Native Endpoint GitHub archives |
| `mcp_server` | MCP distribution workflow; do not reselect already-published versions |
| `optional_components` | Comma-separated `our_world,odoom,oquake,oide,onode_manager,hyperdrive_client`; `none` selects none |
| `version_mode` | `Automatic`, `Patch`, `Minor`, `Major`, `Manual` |
| `manual_version` | Stable `X.Y.Z`; Manual requires exactly one selected versioned component |
| `star_existing_version` | Existing STAR version for STAR-only maintenance; other components/NuGet/optional selections must be disabled |

Automatic versioning can stop on unchanged optional components. Inspect the plan rather than forcing unnecessary versions. Keep already-completed products disabled when finishing a partial coordinated release. OurWorld requires Unity Cloud configuration; do not substitute a ULF/license workflow.

### Normal coordinated publication

Use the Actions form, select master and enable the required products. A primary-component example:

```powershell
gh workflow run global-release.yml --repo NextGenSoftwareUK/OASIS --ref master `
  -f publish=true -f nuget_packages=true -f nuget_package_scope=All `
  -f oasis_runtime=true -f star_runtime=true -f edge_runtime=true `
  -f native_endpoint=true -f ogengine_client=true -f mcp_server=true `
  -f optional_components=none -f version_mode=Automatic
```

This is the intended coordinated entry point, not a guarantee that every selected product's prerequisites are satisfied. Check the current [release matrix](OASIS_RELEASE_STATUS.md) and plan before dispatch. Add optional products only after their owning-source/platform/prerequisite review. Normal validation stays enabled.

### Edge NuGet only

```powershell
gh workflow run global-release.yml --repo NextGenSoftwareUK/OASIS --ref master `
  -f publish=true -f nuget_packages=true -f nuget_package_scope=Edge `
  -f edge_runtime=false -f oasis_runtime=false -f star_runtime=false `
  -f native_endpoint=false -f ogengine_client=false -f mcp_server=false `
  -f optional_components=none -f version_mode=Automatic
```

This packs/uploads the nine Edge libraries, not the GitHub Edge bundle. It excludes the long coordinated Edge validation jobs. It was explicitly authorized as an expedited publication during this recovery; do not confuse it with normal end-to-end validation or physical-device acceptance.

## Edge component entry workflow

**Release — OASIS Edge Runtime** normally takes:

- `operation`: `Preview` or `Publish`.
- `version_mode` and optional `manual_version`.
- Leave `validated_run_id` and `nuget_run_id` blank for the normal path.

The normal path dispatches Global OASIS Release from master with NuGet scope Edge and Edge Runtime enabled.

### Artifact-only GitHub publication

Use only when reusable validation/assets and NuGet publication have already succeeded:

```powershell
gh workflow run release-edge-runtime.yml --repo NextGenSoftwareUK/OASIS --ref master `
  -f operation=Publish -f version_mode=Manual -f manual_version=1.1.0 `
  -f validated_run_id=38204827438 -f nuget_run_id=38270870776
```

The IDs/version above are historical recovery examples, not defaults for a new release. An already existing version must not be recreated. The workflow change initially dispatched from Development during explicitly authorized expedited recovery; the master command is available only after the workflow change is promoted there.

Artifact-only mode verifies successful `edge-runtime-validation / validate` and `edge-runtime-assets` jobs in the source run, plus a successful `nuget-publish` run. It downloads reusable artifacts on the hosted runner, replaces the nine NuGet candidates with that successful publication's artifacts, regenerates notes and records both runs in `PUBLICATION_PROVENANCE.md`. It does not rebuild binaries, rerun the long suite or republish NuGet packages. Binary validation remains tied to the original run; refreshed notes/packages do not retroactively validate a different binary source.

## Partial publication and reliability

NuGet identities are immutable, including unlisted versions. Never use a blind whole-run retry, `--skip-duplicate`, or version bumps to hide an unexplained conflict. First establish the exact source, reviewed package plan, package identities and already-uploaded contents.

Development now contains frozen-plan packing and `Publish-OASISReviewedNuGet.ps1` whole-set preflight. Packing validates the plan's source SHA, scope, project IDs/paths and versions. Preflight compares existing package ZIP entries by SHA-256, excluding only NuGet's signing entry; conflicting contents stop before any upload, identical published contents are not uploaded again. These changes still require checked promotion and end-to-end recovery verification. Complete historical partial-plan reconciliation is not yet implemented; do not treat this document as proof it is.

## Verify completion

1. Inspect the plan and every selected job's terminal result.
2. Verify public non-draft GitHub releases, correct source/provenance and all required nonempty platform assets.
3. Verify exact NuGet/npm registry versions and README rendering after indexing; upload acceptance can precede visibility.
4. Update the release matrix with actual versions/links and remaining blockers.
5. Keep physical mobile, two-device convergence and Unity IL2CPP acceptance separate from build/publication success.

No workflow can guarantee external GitHub/NuGet availability. Reliability means explicit gates, consistent immutable inputs, honest diagnostics and verified recovery without duplicate or mismatched releases.
