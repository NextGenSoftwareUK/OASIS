# DynamoDB / AWS CQL current restoration

Updated 1 November 2026. Controlled reader restoration passes; current native and hosted qualification remain open.

## Current result

- Reused preserved DynamoDB CQL adapter and13compiler/response controls from work/cosmic-cql-sandbox-integration237403a75. Current controls pass13/13, zero skips.
- Shared BoundedIdentityCqlCompiler now owns catalogue intersection, immutable key capture and the100-key bound; Dynamo wrapper admits only DynamoDBOASIS/AWSOASIS authorities.
- Native operation is BatchGetItem with consistent item reads. ID equality/membership, Boolean AND/OR and bounded paging only; no Scan, managed business predicate, retry or partial response on unprocessed keys.
- Hosted AWS is not proved by mocks or DynamoDB Local. Native count not increased; current programme remains42native-qualified/local types and39hosted.

## Actual retained environments

- Canonical history container oasis-dynamo-retained is stopped and binds runtime/data under C:/Source/OASIS/TestResults/AWSProviderEvidence. Do not start or restore it from this assigned checkout.
- Separate CQL container oasis-cosmic-cql-dynamo is stopped with pinned image6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd. Its data bind is already C:/Source/OASIS-cosmic/LocalState/COSMIC/AwsCql/dynamo-data; shared-local-instance.db is51200bytes. Original demo-seed.json173bytes/environment.lock preserved.
- Its read-only runtime bind still points at the old checkout. The preserved helper also hardcodes that old cache. Repair this runtime dependency while retaining the SAME assigned data/actor/IDs; do not migrate or copy the old history store.
- Existing Windows assigned runtime path is absent. No matching archive was found in Downloads and no DynamoDBLocal.jar in searched WSL /var/tmp or /opt depth4. This is not a whole-machine inventory claim. No download/install/migration performed yet.

## Restore invariant fixed

Restore-ReusableDynamoRuntime.ps1 previously derived the new checkout destination but never verified the retained container actually mounted it. It now checks exact runtime/data binds before allocation/download. Actual mismatched-container test rejects and creates neither runtime directory nor archive. Windows-to-WSL path conversion uses forward slashes and checks output before Trim; the initial null-path failure is preserved separately.

## Exact next step

Inspect remaining eligible pinned caches. Then plan one journalled repair of the CQL runtime binding and complete runner dependencies, preserving the existing51KBstore/seed. Reuse a qualified pinned runtime; download only as a last resort with original digest checks, never use a different JAR or silently recreate data. Keep original container definition/fingerprints through migration verification. No hosted AWS account/API call or fee approval is implied.

## Evidence

- TestResults/AwsCqlCurrent/current-port-controls-corrected.trx:13passed/0failed/0skipped, 21043bytes, SHA2561bde7fbfe24a1528d6dc5bfdf52ebaf7e6d834347a430569e2d4727c811fd0f7.
- Runtime guard: TestResults/AwsCqlCurrent/runtime-mount-guard-corrected.log, expected rejection before allocation; initial path error and initial compiler missing-import failure retained separately.
- Historical native/SDK/HTTP/browser proof remains on branch237403a75; do not substitute it for the current native gate.

[Programme](CURRENT_STATUS.md) | [Provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
