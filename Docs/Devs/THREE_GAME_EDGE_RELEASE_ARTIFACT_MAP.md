# Three-game Edge release: source, tests, builds and outputs

This is the operational map for the shared inventory, GeoNFT and quest-progress path used by Our World, ODOOM and OQuake. All three clients use OGEngineClient and the Edge Runtime locally, then synchronize with the hosted WEB4 ONODE through HyperDrive v2 and ONET. Provider selection, failover and replication remain owned by the existing provider and HyperDrive managers.

The canonical package, provider, test-evidence and GitHub Actions map for the shared runtime is
[OASIS Edge Runtime releases, packages and evidence](EDGE_RUNTIME_RELEASES.md).

## Source ownership

| Area | Canonical source |
|---|---|
| Shared native client and C ABI | `OASIS Omniverse/OGEngineClient` submodule |
| Durable offline store and replay | `OASIS Architecture/NextGenSoftware.OASIS.Edge.Runtime` |
| ONET Edge transport | `OASIS Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime` |
| HyperDrive synchronization contracts | `OASIS Architecture/NextGenSoftware.OASIS.HyperDrive.Synchronization` |
| Hosted commands, projection, fan-out and compaction | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/HyperDrive` |
| Provider-neutral hosted persistence contract | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/IOASISStorageProvider/IOASISHostedHyperDriveProvider.cs` |
| Mongo implementation | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS` |
| Our World Unity host | `OASIS Omniverse/OASIS Hub` |
| ODOOM integration | `OASIS Omniverse/OGames/ODOOM` |
| OQuake integration | `OASIS Omniverse/OGames/OQuake` |

## Release commands

Run commands from the OASIS repository root.

| Product | Windows | Linux/macOS | Result |
|---|---|---|---|
| Native Edge client | `OASIS Omniverse\BUILD_AND_DEPLOY_STAR_CLIENT.bat` | `OASIS Omniverse/OGEngineClient/Scripts/build-and-deploy-star-api-unix.sh` | `artifacts/native-games/native/Edge/<runtime>/publish` |
| ODOOM | `OASIS Omniverse\OGames\ODOOM\BUILD ODOOM.bat batch` | `OASIS Omniverse/OGames/ODOOM/BUILD_ODOOM.sh` | `OASIS Omniverse/OGames/ODOOM/build` |
| OQuake | `OASIS Omniverse\OGames\OQuake\BUILD_OQUAKE.bat batch` | `OASIS Omniverse/OGames/OQuake/BUILD_OQUAKE.sh` | `OASIS Omniverse/OGames/OQuake/build` |
| Unity Edge package | `powershell -File Scripts\build_edge_unity_package.ps1 -Configuration Release -OutputDirectory artifacts\unity-current` | `pwsh -File Scripts/build_edge_unity_package.ps1 -Configuration Release -OutputDirectory artifacts/unity-current` | `artifacts/unity-current` |
| Our World validation APK | `powershell -File Scripts\validate_our_world_android_build.ps1` | `pwsh -File Scripts/validate_our_world_android_build.ps1` | `artifacts/our-world-android-validation` |
| Signed Our World APK/AAB | `Scripts\build_our_world_android_app_bundle.bat` | `Scripts/build_our_world_android_app_bundle.sh` | `artifacts/our-world-release` |

## Published release locations

| Product | Published location | CI / local candidate location |
|---|---|---|
| OASIS MCP standalone binaries | [OASIS GitHub Releases](https://github.com/NextGenSoftwareUK/OASIS/releases), under tags named `mcp-v<version>` | The `oasis-mcp-<runtime>` artifacts on the `Publish MCP Server` workflow run |
| OASIS MCP npm package | [`@oasisomniverse/mcp-server`](https://www.npmjs.com/package/@oasisomniverse/mcp-server) | `WEB6/npm` |
| OASIS MCP .NET tool | [`NextGenSoftware.OASIS.MCP.Server`](https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server) | `WEB6/NextGenSoftware.OASIS.MCP.Server/bin/Release` and the workflow's temporary `nupkgs` directory |
| ONODE Manager installers | [OASIS GitHub Releases](https://github.com/NextGenSoftwareUK/OASIS/releases), under tags named `onode-manager-v<version>` | `release/win-x64`, `release/osx-x64`, and `release/linux-x64` in the release workflow |
| Our World multi-platform release | [Our World GitHub Releases](https://github.com/NextGenSoftwareUK/Our-World/releases); no complete release is published yet | The owning workflow packages Android, iOS, Windows, Linux, macOS and tvOS; local signed Android output is `artifacts/our-world-release` |
| ODOOM | [ODOOM v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/ODOOM_v.1.0.0_ALPHA) | `OASIS Omniverse/OGames/ODOOM/build` |
| OQuake | [OQuake v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/OQUAKE_v1.0.0_ALPHA) | `OASIS Omniverse/OGames/OQuake/build` |
| Unity Edge package | Unity Asset Store release process | `artifacts/unity-store-candidate` or `artifacts/unity-current` |
| OASIS Edge Runtime | [OASIS GitHub Releases](https://github.com/NextGenSoftwareUK/OASIS/releases), tag `OASIS-Edge-Runtime-vX.Y.Z`; coordinated packages on NuGet | `edge-runtime-release-evidence` from `.github/workflows/edge-runtime-validation.yml`; standalone entry point `.github/workflows/release-edge-runtime.yml` |

## Release validation status (28 September 2026)

Build validation and publication are separate gates. A successful rehearsal proves that the workflow can compile, package and upload temporary Actions artifacts. It does not create a permanent GitHub release when `publish=false`.

| Product | Current validation | Current published release |
|---|---|---|
| ODOOM | The release rehearsal builds and packages Windows x64, Linux x64 and macOS successfully. Evidence: [Actions run 36490130954](https://github.com/NextGenSoftwareUK/ODOOM/actions/runs/36490130954). | [ODOOM v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/ODOOM_v.1.0.0_ALPHA) now contains the verified Windows x64, Linux x64 and macOS arm64 archives. |
| OQuake | The release rehearsal builds and packages Windows x64, Linux x64 and macOS arm64 successfully. Evidence: [Actions run 36493577583](https://github.com/NextGenSoftwareUK/OQUAKE/actions/runs/36493577583). | [OQuake v1.0.0 Alpha](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/OQUAKE_v1.0.0_ALPHA) now contains the verified Windows x64, Linux x64 and macOS arm64 archives. |
| Our World | Source preparation and workflow validation are complete, but hosted release builds are blocked at Unity activation before the platform matrix can run. | No GitHub release has been published. |

### Our World Unity licensing blocker

The Edge release workflow installs its pinned Unity editor on an ephemeral GitHub runner, then activates a Unity Personal seat with the repository secrets `UNITY_USERNAME` and `UNITY_PASSWORD` before launching the editor. The credential preflight deliberately fails before a long editor invocation when either secret is absent. An interactive entitlement on a developer workstation is machine-bound and does not activate a GitHub-hosted runner.

**8 October 2026 diagnostic evidence:** [Edge run 37754300843, attempt 2](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/37754300843/attempts/2) passed the credential preflight but failed in the activation action's initial `Unity.Licensing.Client.exe --showEntitlements` query. The client printed `No licenses were found`, a boot-drive serial warning, and an unhandled `ObjectDisposedException` for `IServiceProvider` (exit code 3762504530). The action never reached its login/activation call, so this is not evidence of invalid credentials. The serial warning alone is not established as the cause. Upstream previously recorded a disposed-object activation issue in [activation action PR 18](https://github.com/RageAgainstThePixel/activate-unity-license/pull/18). A single fresh-runner rerun, [attempt 3](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/37754300843/attempts/3), activated successfully at 21:39:51 UTC and entered the aggregate Edge release gate at 21:39:57 UTC. Thus the startup crash did not reproduce and the supplied credentials work; the underlying Unity client defect is not diagnosed. Activation and all editor validation remain required; this diagnostic does not waive either gate or establish successful publication.

Complete the Our World release through one of Unity's supported execution routes:

1. **Hosted Unity Personal activation (current Edge gate):** configure `UNITY_USERNAME` and `UNITY_PASSWORD` as repository Actions secrets. The pinned activation action acquires and returns the ephemeral runner's seat as part of its action lifecycle.
2. **Self-hosted GitHub Actions runner:** use a trusted, access-controlled runner on a machine where Unity 2022.3.62f3 is activated. Never expose a self-hosted workstation runner to untrusted pull-request workflows in this public repository.
3. **Unity Professional or floating licensing:** configure a dedicated professional serial or licensing-server configuration and change the activation inputs as one explicit licensed deployment profile.

After selecting a route, run the release workflow first with publication disabled. Every supported platform job must succeed and upload its expected package. Then run the same reviewed source commit with publication enabled and verify the permanent release or store artifacts. Android and iOS are the primary Our World targets; Windows is also required for desktop testing. A successful source compile or an Actions artifact alone is not a published game release.

### Current public distribution snapshot

| Component | Public location and platform coverage |
|---|---|
| OASIS Runtime 5.0.1 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Runtime-v5.0.1), one runtime archive. |
| STAR ODK Runtime / CLI 4.0.1 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/STAR-ODK-Runtime-v4.0.1), one runtime/CLI archive. |
| OGEngineClient 2.0.2 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-v2.0.2), one client archive. Platform-specific native clients are also embedded in game and MCP build outputs where applicable. |
| Native Integrated Endpoint 2.0.2 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/Native-Endpoint-v2.0.2), one endpoint archive. |
| MCP Server 2.0.5 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/mcp-v2.0.5) with Windows x64, Linux x64/arm64 and macOS x64/arm64 executables; also [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server) and [npm](https://www.npmjs.com/package/@oasisomniverse/mcp-server). |
| HyperDrive Client 1.0.0 Alpha | [GitHub prerelease](https://github.com/NextGenSoftwareUK/OASIS-HyperDrive-Client/releases/tag/v1.0.0) with Windows x64, Linux x64, macOS x64 and macOS arm64 executables. |
| ONODE Manager 1.0.0 | [GitHub release](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/onode-manager-v1.0.0) with a Windows installer and portable archive, Linux AppImage, and macOS installer and portable archive. |
| OIDE 1.0.0 Alpha | [GitHub prerelease](https://github.com/NextGenSoftwareUK/OIDE/releases/tag/v1.0.0) with a Windows installer, Linux AppImage and macOS arm64 DMG. |

This table is a dated verification record. GitHub release pages are authoritative after later releases. For a production claim, inspect the release assets themselves; do not infer publication from a successful non-publishing rehearsal.

The MCP workflow is `.github/workflows/publish-mcp.yml`. Pull requests and normal branch pushes build and protocol-test all five native binaries, but do not publish them. A release operator dispatches the workflow with an exact version and `publish=true`; only after every build and package check passes does it create the GitHub release and publish npm and NuGet packages.

The signing script reads `artifacts/our-world-release-secrets/android-signing.secrets.env`. That ignored file and all private keys must stay outside Git. The deployed ONODE reads `OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY`; each environment also sets `OASIS_OFFLINE_GRANT_SIGNING_PUBLIC_KEY`. The client release embeds only the matching public key in `omniverse_host_config.json`.

## Tests and reports

| Verification | Entry point | Output |
|---|---|---|
| Real three-client online/offline convergence | `Scripts/run_live_three_game_sync_test.ps1` | console/TRX selected by caller |
| Native ABI and Edge behavior | `OASIS Omniverse/OGEngineClient/TestProjects/OGEngine.Client.Tests` | `TestResults` or selected results directory |
| Native export smoke test | `OASIS Omniverse/OGEngineClient/Scripts/publish_and_deploy_star_api.ps1 -Profile Edge -RunSmokeTest` | `artifacts/native-games/native/Edge/<runtime>/native-profile-report.json` and `exports.txt` |
| Unity package validation | `Scripts/validate_edge_unity_package.ps1` | caller-selected log directory |
| Our World integration | `Scripts/validate_our_world_edge_integration.ps1` | Unity validation log |
| Android package validation | `Scripts/validate_our_world_android_build.ps1` | APK and `our-world-android-build.log` |
| Full Edge release gate | `Scripts/validate_edge_runtime_release.ps1` | `artifacts/edge-release-validation` |
| Railway dependency graph | `Scripts/validate_railway_dependency_manifest.py --require-gitlinks` | process result |

`artifacts/our-world-release/release-artifacts.json` records the size, SHA-256 and build time for the current local release set. Artifacts are ignored by Git and must be copied to the release store before generated-data cleanup.

## Deployment and promotion

Development deploys from parent `Development` and each private submodule's `Development` branch. Production deploys from parent `master` and each submodule's `main` branch. `Docker/oasis-dependency-versions.env` must exactly match the committed parent gitlinks.

Use `.github/workflows/promote-development-to-master.yml` for promotion. It first creates submodule `Development` to `main` pull requests, then creates the parent promotion pull request after those merges. The detailed invariant and recovery steps are in [Promoting OASIS from Development to master](DEVELOPMENT_TO_MASTER_PROMOTION.md).

After Railway reports a successful WEB4 production deployment, run the live three-game test against `https://api.web4.oasisomniverse.one` using the production public key. Passing means an inventory change and quest progress created offline by one client are accepted by the hosted ONODE and converge into the other two clients.

## Backups and safe cleanup

MongoDB backups:

- Development: `Scripts/backup_mongodb_dev.bat` or `Scripts/backup_mongodb_dev.sh`
- Production: `Scripts/backup_mongodb_live.bat` or `Scripts/backup_mongodb_live.sh`
- Output: `artifacts/mongodb-backups`

The backup scripts prompt for the MongoDB URI, use the fixed environment database name, produce a gzip archive, and verify that BSON content exists. Keep the verified backup outside the repository before a production migration or destructive maintenance.

Generated `bin`, `obj`, test results and intermediate NativeAOT build trees can be removed only after checking that they are not in use and that required release artifacts and reports have been copied. The disposable Unity validation project under `artifacts` is not the real Our World project. Do not remove Our World's Unity `Library`/cache merely because it is generated; confirm it is unused and obtain explicit cleanup approval. Do not remove `artifacts/our-world-release`, `artifacts/mongodb-backups`, signing secrets, or the final native game build folders until the release is archived.

## Related documentation

- [OASIS Edge Runtime and offline sync architecture](OASIS_EDGE_RUNTIME_OFFLINE_SYNC_ARCHITECTURE.md)
- [Our World offline MVP verification](OUR_WORLD_OFFLINE_MVP_VERIFICATION.md)
- [OGEngineClient Unity Asset Store release](OGENGINECLIENT_UNITY_ASSET_STORE_RELEASE.md)
- [Railway dependency pins](RAILWAY_DEPENDENCY_PINS.md)
- [ODOOM/UZDoom build synchronization](../../OASIS%20Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md)
- [STAR games user guide](../../OASIS%20Omniverse/Docs/STAR_Games_User_Guide.md)
