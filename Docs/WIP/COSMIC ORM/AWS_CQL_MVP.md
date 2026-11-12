# DynamoDB / AWS CQL current restoration

Updated 1 November 2026. Controlled reader, retained native and local SDK/HTTP federation pass; owned hosted AWS qualification remains open.

## Current result

- Reused preserved DynamoDB CQL adapter and13compiler/response controls from work/cosmic-cql-sandbox-integration237403a75. Current controls pass13/13, zero skips.
- Shared BoundedIdentityCqlCompiler now owns catalogue intersection, immutable key capture and the100-key bound; Dynamo wrapper admits only DynamoDBOASIS/AWSOASIS authorities.
- Native operation is BatchGetItem with consistent item reads. ID equality/membership, Boolean AND/OR and bounded paging only; no Scan, managed business predicate, retry or partial response on unprocessed keys.
- Hosted AWS is not proved by mocks or DynamoDB Local. AWSOASIS adds one current local-qualified type: programme43native-qualified/local types and39hosted. DynamoDBOASIS alias requires its own native proof.

## Actual retained environments

- Canonical history container oasis-dynamo-retained is stopped and binds runtime/data under C:/Source/OASIS/TestResults/AWSProviderEvidence. Do not start or restore it from this assigned checkout.
- Separate CQL container oasis-cosmic-cql-dynamo is stopped with pinned image6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd. Its data bind is already C:/Source/OASIS-cosmic/LocalState/COSMIC/AwsCql/dynamo-data; shared-local-instance.db is51200bytes. Original demo-seed.json173bytes/environment.lock preserved.
- The stopped CQL descriptor was replaced once with an assigned-worktree runtime bind; its original stopped descriptor is retained as oasis-cosmic-cql-dynamo-before-runtime-rebind. The SAME assigned data/actor/IDs remain; original data and seed hashes are unchanged. The preserved runner helper still needs its path updated before qualification.
- After eligible cache searches found no usable pinned copy, restored the original SHA-pinned archive once. Assigned runtime TestResults/AWSProviderEvidence/runtime is reusable: DynamoDB Local3.3.2, 50,824,785bytes before provenance files. ZIP42714930bytes was verified and removed after extraction; JAR SHA25667ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620. Existing JDK/image reused. No native process started.

## Restore invariant fixed

Restore-ReusableDynamoRuntime.ps1 previously derived the new checkout destination but never verified the retained container actually mounted it. It now checks exact runtime/data binds before allocation/download. Actual mismatched-container test rejects and creates neither runtime directory nor archive. Windows-to-WSL path conversion uses forward slashes and checks output before Trim; the initial null-path failure is preserved separately.

## Exact next step

Current runner dependencies restored; assigned runtime used successfully. Native retained3/2 and paired SDK/HTTP5/4 pass (one test each, zero skips); forged access rejected, unavailable source returns503 without rows. Exact original data_json records remain unchanged. Temporary Mongo database cleanup verified by test; no pending journal, owned services stopped, ports18002/27029/5096 absent. Seed IDs preserved, no reseed. Next inspect preserved content-query work and hosted admission separately; do not rerun green identity checks.

For the retained local demo: `Scripts/TestHosts/Run-CosmicAwsCqlNative.ps1 -Federated -Serve` serves port5096. Existing reports must be explicitly reviewed/preserved before another run. This is DynamoDB Local, not a hosted AWS sandbox preset.

## Evidence

- Current native TRX: TestResults/AwsCqlCurrent/aws-cql-native.trx, SHA25688D58A9E5988A5471A28AC0CDBB2081147C7BB9400F42E109D38E0B3C59D662F;1passed/0failed/0skipped.
- Current federation TRX: TestResults/AwsCqlCurrent/aws-mongo-federation.trx, SHA256A963BBD1180D825A5350982D6D13048363429CC609AB5B25C06BE8B1BAF35EB2;1passed/0failed/0skipped. Prior successful October reports/logs preserved under hash-named TestResults/AwsCqlCurrent/Archive.

- Runtime repair: TestResults/AwsCqlCurrent/runtime-binding-repair.log; durable LocalState/COSMIC/AwsCql/runtime-binding-{migration.intent,created.acknowledged,ready.acknowledged}.json. Ready ACK confirms unchanged data/seed and nativeQualified=false.

- TestResults/AwsCqlCurrent/current-port-controls-corrected.trx:13passed/0failed/0skipped, 21043bytes, SHA2561bde7fbfe24a1528d6dc5bfdf52ebaf7e6d834347a430569e2d4727c811fd0f7.
- Runtime guard: TestResults/AwsCqlCurrent/runtime-mount-guard-corrected.log, expected rejection before allocation; initial path error and initial compiler missing-import failure retained separately.
- Historical native/SDK/HTTP/browser proof remains on branch237403a75; do not substitute it for the current native gate.

[Programme](CURRENT_STATUS.md) | [Provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
