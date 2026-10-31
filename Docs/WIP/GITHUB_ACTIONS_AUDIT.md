# GitHub Actions audit and catalog (29 Oct 2026)

Source: every file in `.github/workflows/` on `Development` (40 files), their triggers, inputs and jobs, plus recent run history from `gh`.
Status: the 3 per-API release buttons, `reusable-api-release`, the Azure deploy and the Pages deploy were deleted (decided with the owner: the WEB4-WEB10 APIs are Railway-hosted Swagger services with no GitHub releases, so those flows were obsolete). Also deleted: the 11 thin `Release — X` buttons, `publish-nuget` and `repair-nuget-initial-versions`, and Global release lost its two legacy OGEngine inputs (`ogengine_remote_only`, `ogengine_edge`). The four maintenance options (`nuget_plan_only`, `star_existing_version`, `excluded_nuget_ids`, `expected_excluded_nuget_count`) moved to the new `global-release-recovery.yml` (modes: Review NuGet plan only, Resume partly published NuGet batch), which calls Global release. The STAR in-place rebuild mode was dropped (ship a new patch instead). `edge_unity` was folded into `ogengine_client`, `nuget_package_scope` was removed (NuGet always releases as the complete set), and `release-edge-runtime.yml` was deleted with them; the main form now has 11 options. Everything else in section 5 is still proposed.

## 1. Why the "API" option is missing from the Run workflow form

The **Run workflow** form is read from the branch chosen in **Use workflow from**. The screenshot shows `master`, and `advance_api_versions` only exists on `Development`
(nothing is merged to `master` yet). Choose `Development` in that dropdown and the checkbox appears.
The same applies to every workflow: the form and the code that runs both come from the selected branch.

Related trap: the 11 thin `Release — …` wrappers (section 3.2) start Global OASIS Release with `--ref master`, so anything run through them uses `master`'s copy, not `Development`'s.

## 2. The big picture

| Group | Files | What they are for |
|---|---|---|
| Continuous checks (automatic) | 9 | Build and test on push and pull request |
| The release machine | 1 (`global-release`) + 3 callable helpers | Plans and publishes NuGet packages, runtimes, Edge, MCP, and now API versions |
| "Release — X" buttons | 15 | One-button front ends that mostly just call Global OASIS Release with some boxes ticked |
| Older, separate NuGet publishers | 5 | Predate Global OASIS Release; overlap with its NuGet job |
| Branch and deploy plumbing | 7 | Promote Development to master, Railway staged release, submodule and pin sync, image build, site deploys |

## 3. Catalog

"Auto" means it runs without you pressing anything. "Button" means it appears under **Run workflow**.

### 3.1 Continuous checks (auto)

| Workflow | Runs on | Does |
|---|---|---|
| OASIS CI/CD Pipeline `ci-cd.yml` (996 lines) | push and PR to main, master, Development | 13 jobs: solution integrity, provider integration, tests for Architecture, ONODE, STAR ODK, Omniverse, STARAPIClient, Unity, packaging, security scan, code quality, coverage |
| ONET/ONODE Tests `onet-onode-tests.yml` | push master and Development; PR to master | Unit, integration and WebAPI unit tests for ONET/ONODE. Overlaps the ONODE tests inside ci-cd |
| Edge Runtime validation `edge-runtime-validation.yml` | PR, push Development and master, button, callable | Desktop sessions, HoloOASIS hApp, Holochain Android runtime, hosted Mongo sync |
| HyperDrive contract validation `hyperdrive-contract-validation.yml` | PR, button | Provider I/O manifests, WEB4 route manifest, V2 storage parity |
| Subscription usage protocol `subscription-usage-protocol.yml` | PR, push Development, button | Ledger tests against a Mongo replica set with fault injection |
| Railway release gate contracts `railway-gate-contracts.yml` | push and PR | Python tests for the Railway gate scripts (17 lines) |
| Sync Railway dependency pins `sync-railway-pins.yml` | push Development and master | Corrects `Docker/oasis-dependency-versions.env` to match the gitlinks and commits it |
| Build and Publish MCP Server `publish-mcp.yml` | PR, push Development, button, callable | Validates, builds binaries, publishes GitHub release, NuGet and npm when asked |
| Render Mermaid Diagrams `render-mermaid.yml` | every push | Renders diagrams to SVG and commits them |

### 3.2 "Release — X" buttons

Each of these is 46 lines with options `operation` (Preview or Publish), `version_mode`, `manual_version`. The shared ones just run:
`gh workflow run global-release.yml --ref master -f oasis_runtime=true -f everything_else=false …`.

| Workflow | What it turns on in Global OASIS Release |
|---|---|
| Release — OASIS Runtime | `oasis_runtime` |
| Release — STAR ODK Runtime (+ `replace_existing_version`) | `star_runtime` |
| Release — MCP Server | `mcp_server` |
| Release — Native Endpoint | `native_endpoint` |
| Release — HyperDrive Client | optional component `hyperdrive_client` |
| Release — ODOOM / OQUAKE / OIDE / Our World | optional components |
| Release — ONODE Manager `release-onode-manager-entry.yml` | optional component `onode_manager` |
| Release — OGEngineClient (+ cross-platform profiles job) | `ogengine_client`, then calls `build-ogengine-profiles.yml` |
| Release — OASIS Edge Runtime `release-edge-runtime.yml` (166 lines, 7 options) | `edge_runtime`, plus artifact-only republish and Standalone/Unity choice |

### 3.3 Global OASIS Release `global-release.yml` (742 lines, 20 options, 16 jobs)

Button only. Jobs: plan, nuget, nuget-publish, edge-runtime-validation, edge-runtime-assets, edge-runtime-publish, edge-standalone, edge-unity-nuget, ogengine-profiles, runtime-assets, runtime-release, star-runtime-assets, star-runtime-release, mcp, optional-established-release-workflows, optional-game-release-workflows.

| Option | Default | Plain meaning | Verdict |
|---|---|---|---|
| `publish` | off | Off = build and validate only; on = publish after everything passes | Keep |
| `version_mode` | Automatic | Automatic reads commit messages; or force Patch/Minor/Major; or Manual | Keep |
| `manual_version` | blank | The X.Y.Z for Manual mode | Keep |
| `advance_api_versions` | off | Bump WEB4-WEB10 versions **only for APIs whose code changed** since their last release; update histories, runtime tie, docs, Postman, portal | Keep (new; available on Development only) |
| `nuget_packages` | on | Release all first-party NuGet packages | Keep |
| `nuget_package_scope` | All | All, or only the Edge bundle packages | Keep, but could merge with `edge_runtime` |
| `nuget_plan_only` | off | Show the NuGet plan without packing | Redundant: `publish` off already does this |
| `excluded_nuget_ids` | blank | Packages already published, to skip (recovery) | Recovery-only; move to a separate recovery workflow |
| `expected_excluded_nuget_count` | 0 | Safety check for the line above | Recovery-only; goes with it |
| `oasis_runtime` | on | Release the OASIS Runtime | Keep |
| `star_runtime` | on | Release STAR Runtime and CLI | Keep |
| `star_existing_version` | blank | Rebuild an existing STAR version in place | Maintenance-only; duplicates `replace_existing_version` on the STAR button |
| `edge_runtime` | on | Release the validated Edge bundle | Keep |
| `edge_unity` | off | Edge + Unity integration | Keep or fold into `edge_runtime` choice |
| `ogengine_client` | on | OGEngineClient (both profiles) | Keep |
| `ogengine_remote_only` | off | RemoteOnly profile alone | Legacy: the form text itself says so |
| `ogengine_edge` | off | Edge profile alone | Legacy: same |
| `native_endpoint` | on | Native Endpoint | Keep |
| `mcp_server` | on | MCP Server to GitHub, NuGet, npm | Keep |
| `optional_components` | none | Comma list: our_world, odoom, oquake, oide, onode_manager, hyperdrive_client | Keep, but a typed comma list is error-prone |

### 3.4 Older NuGet publishers (button)

| Workflow | Does | Verdict |
|---|---|---|
| Publish NuGet Packages `publish-nuget.yml` | Checks out "OASIS2" and OASIS-WEB6, packs and pushes everything | Looks stale (old repo name, no inputs); likely dead |
| Publish OASIS NuGet Packages (selected) `publish-oasis-packages.yml` | Publish a chosen list of projects at a given version | Overlaps Global release |
| Publish HoloNET NuGet Packages `publish-holonet.yml` | HoloNET and NextGenSoftware libraries, separate repos | Needed while those repos stay separate |
| Repair NuGet Initial Versions `repair-nuget-initial-versions.yml` | One-off repair | Done-job; candidate to delete |
| Review immutable NuGet recovery `review-nuget-recovery.yml` | Compares published packages with the registry | Recovery tool; keep with the recovery items above |

### 3.5 Branch, deploy and plumbing

| Workflow | Runs on | Does | Verdict |
|---|---|---|---|
| Promote Development to master | button | Opens submodule and parent promotion PRs | Keep |
| Gated Railway release `railway-staged-release.yml` | button, after workflow_run | Freeze candidate, deploy to staging, approval, promote to production | Keep |
| Submodule Sync | Monday 07:00, button | Moves pointers to branch tips and opens a PR | Keep |
| Build WEB4 image `build-web4-image.yml` | button | Builds and pushes the WEB4 Docker image | Keep, if Railway uses the image |
| Publish — ONODE Manager Binaries `release-onode-manager.yml` | callable only | Tests, builds Windows, macOS, Linux installers, creates release; run by Global release (`onode_manager`) | Done: button and tag trigger removed |
| Build and publish standalone Edge Runtime, Build OGEngineClient profiles | callable only | Helpers that Global release calls | Keep (hidden from the form) |

## 4. Recent activity (last 200 runs, `gh`)

Active: OASIS CI/CD, Subscription usage protocol, Build and Publish MCP Server (38 runs, mostly pull requests), Edge Runtime validation, Sync Railway pins, ONET/ONODE Tests, HyperDrive contract validation, Global OASIS Release (39 runs, #39 on master succeeded, #37 and #35 failed).
Not seen in the last 200 runs (not proof they are unused, GitHub only returned recent history): everything in sections 3.2 (except via Global), 3.4, and the Azure and Pages deploys.

## 5. Proposed clean-up

Nothing here has been done. Order is by how much confusion it removes.

1. **One API release path: done.** Global release (`advance_api_versions`) is the only way API versions change. The per-API buttons only opened a prep PR and created a GitHub release that was never used; the APIs deploy from Railway.
2. **Delete the 11 thin "Release — X" buttons** (OASIS Runtime, STAR, MCP, Native Endpoint, HyperDrive Client, ODOOM, OQUAKE, OIDE, Our World, ONODE Manager entry, OGEngineClient). Global release already has a checkbox for each. That removes 11 of the entries in the left list. Edge Runtime's button has extra options (artifact-only republish), so keep it or move those options into Global release.
3. **Trim Global release from 20 options to about 12:** drop `nuget_plan_only`, `ogengine_remote_only`, `ogengine_edge`, `star_existing_version`; move `excluded_nuget_ids` and `expected_excluded_nuget_count` to the recovery workflow; replace the `optional_components` text box with checkboxes or a dropdown.
4. **Retire the old NuGet publishers:** `publish-nuget` (stale), `repair-nuget-initial-versions` (one-off), and fold `publish-oasis-packages` into Global release. Keep HoloNET and the review tool until they are replaced.
5. **Azure and Pages deploys: done** (deleted).
6. **Hide helpers** from the left list by keeping them callable-only (already true for Edge standalone, OGEngine profiles).
7. Merge `onet-onode-tests` into `ci-cd` if the duplication is confirmed.

Result: 40 files down to about 17, and a Run form with the one checkbox per thing you actually release.

Not verified in this audit: whether the 15 "Release —" files are still used by anyone, the push filter on ONODE Manager and Mermaid, and whether the Azure/Pages deploys are live.
