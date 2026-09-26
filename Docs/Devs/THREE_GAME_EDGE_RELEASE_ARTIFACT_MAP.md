# Three-game Edge release: source, tests, builds and outputs

This is the operational map for the shared inventory, GeoNFT and quest-progress path used by Our World, ODOOM and OQuake. All three clients use OGEngineClient and the Edge Runtime locally, then synchronize with the hosted WEB4 ONODE through HyperDrive v2 and ONET. Provider selection, failover and replication remain owned by the existing provider and HyperDrive managers.

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

Generated `bin`, `obj`, Unity `Library`, Unity `Temp`, test results and intermediate NativeAOT build trees can be removed after release artifacts and reports have been copied. Do not remove `artifacts/our-world-release`, `artifacts/mongodb-backups`, signing secrets, or the final native game build folders until the release is archived.

## Related documentation

- [OASIS Edge Runtime and offline sync architecture](OASIS_EDGE_RUNTIME_OFFLINE_SYNC_ARCHITECTURE.md)
- [Our World offline MVP verification](OUR_WORLD_OFFLINE_MVP_VERIFICATION.md)
- [OGEngineClient Unity Asset Store release](OGENGINECLIENT_UNITY_ASSET_STORE_RELEASE.md)
- [Railway dependency pins](RAILWAY_DEPENDENCY_PINS.md)
- [ODOOM/UZDoom build synchronization](../../OASIS%20Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md)
- [STAR games user guide](../../OASIS%20Omniverse/Docs/STAR_Games_User_Guide.md)
