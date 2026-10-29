# TRON CQL MVP - adapter controls verified, native demo pending

29 October 2026. TRONOASIS now implements ICosmicQueryStorageProvider through its
existing lifecycle-serialized encrypted backend. No alternate storage path was added.

## Scope

The shared `BoundedIdentityCqlCompiler` owns frozen authorization-catalogue and
ID-predicate algebra. Small S3/TRON compilers bind it to their exact provider identity.
It admits ID equality/membership, Boolean algebra and paging over at most100 keys;
unsupported content/metadata/order/child queries fail before storage I/O.

TRON reads the canonical `RecordKey("holon", id)` through `TronProviderStorageBackend`,
which serializes against activation/deactivation and forwards cancellation to the
existing private backend/SDK. It never enumerates a contract to evaluate a predicate.
Canonical decrypted records must match the authorized ID/actor; deleted rows stay
hidden. Any failure discards all rows. Caller cancellation stays a plain cancellation
error rather than transport failure. Separate reads may observe separate blocks.
The4MiB canonical payload budget is enforced after existing backend decoding; this
is not a preallocation bound on the underlying contract SDK response.

## Verified evidence

`TronCqlTests`:7/7,zero skips,224ms. Frozen caller selection, catalogue intersection,
wrong authority, empty catalogue, unsupported shapes, inactive provider and
pre-cancellation. No runtime/credentials/network calls created by these controls.
Report `TestResults/TronCqlCurrent/tron-cql-controls.trx`, SHA256
`5BCB09B2A7B325E8A63A2A2817D9085C9E5588AA5E5361A27AB7E9E6E1F3571F`.

Shared compiler S3 regression:7/7 plus1/1 signed-SDK encrypted/failure/cancellation
case. Reports under `TestResults/AwsS3CqlCurrent/`:
- `s3-shared-compiler-controls.trx`, SHA256
  `D69A2F427BBF97C85FDFAA71CD8BBE629953DC94B2FE133F9795D34DD7004F70`.
- `s3-shared-compiler-encrypted.trx`, SHA256
  `A75368A0EE72D9D38EC958460F7927A2C05CF14FE97EB9BA2F1B0A06ED2EB346`.

## Exact next step

Inspect the retained TRON source/credential provenance before native queries. The
existing stopped `oasis-tron-tre` container/image exists, but this worktree has no
`TestResults/HyperDriveTron` original manifests/fences. Existing inspection scripts
expect exact historical fence hashes. Do not fabricate them, reset the chain,
replay signed attempts or claim that container existence proves source usability.
The first Windows provider-evidence/tron path check found no retained key directory.
Read the historical admission/inspection sources and ledger to find protected source
identities/key or admit a separate explicit source without changing original outcomes.
No native process or contract mutation was attempted during this milestone.

Then prove native3/2/missing/restart, encrypted source unchanged, and Mongo-paired
SDK/HTTP/browser5/4. TRON remains red in the all-provider inventory. No hosted TRON,
finality, full ORM/history/HyperDrive or EF parity claim follows from these controls.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
