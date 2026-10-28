# AWS S3 adapter on MinIO: shared baseline

28 October 2026. Shipping AWSS3OASIS passes52/52 canonical core-storage-v2 checks on the existing CI MinIO service:26async+26sync. Hosted AWS S3/IAM, full interfaces, fees and HyperDrive routing remain open.

- Source58563f6c0c0b55e7b05a6558bdb72413e0d24d57; [CI job122970030713](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/40731869610/job/122970030713) succeeded. Existing MinIO smoke tests also passed.
- Official AmazonS3Client and existing S3ObjectBackend/PersistedPrivateKeyValueBackend/KeyValueStorageProviderBase; no fake backend or duplicate storage implementation.
- Reused existing CI image ghcr.io/coollabsio/minio:RELEASE.2025-04-22T22-12-26Z. Actual image digest was not captured; tag is not described as immutable.
- Dedicated oasis-s3-conformance bucket, one masked synthetic32-byte key for that disposable job, MaxErrorRetry=0, path-style loopback calls. No hosted AWS credentials or paid calls.
- Both MSTest cases passed after independent SDK bucket-empty assertions. Exact point/raw-collection fixture absence and default hard deletion verified; no raw-prefix sweep. No AWS versioned-bucket/cryptographic-erasure claim.
- CI service/bucket/key lifecycle belongs to disposable job. No local MinIO image/runtime downloaded. Binary/PDB receipt was not captured on this run.

Artifact12835013488:3,205 B; GitHub digest013b4cd9c838f7e103b4cce7efe3d362b85886c3e89219cc33094ecf6edcd194. Retrieved once to fixed TestResults/HyperDrive-Providers/S3CompatibleCurrent. Reports retain original provider label AWSS3OASIS-MinIO; historical blocked AWS results are not relabelled.

Compact tracked aggregate: TestResults/HyperDrive-Providers/NativeEvidence/conformance-awss3oasis-current.json; SHA2564946725d183ecbfdb39b2b222bab809cef90ced1171c4622699be3cef73b1d1b. Original reports under S3CompatibleCurrent/ProviderConformanceCurrent/AWSS3OASIS-MinIO/{async,sync}: SHA2569950c2668b05bd699c543ad24157e3308e49ed2f5f234baa2bbbeda90b694c72 and73d45425aec403c6feac31a41e0b8a3fc3a5cc7379b52ca28277e355d17be44f. TRX2/2 passed;4,077 B compressed/decompressed-verified to1,244 B; raw removed after byte verification.

Reproduce via existing MinIO CI job and S3CompatibleStorageConformanceTests opt-in binding. Local41AWS controlled tests separately passed; not part of native52. PR501 was draft at that run and later merged externally; broader CI/deployment gates remain independent.

## MinIO shipping adapter boundary checkpoint

28October2026: this is the separate MinIOOASIS adapter, not the AWSS3OASIS-MinIO native52 result above. Its current duplicated record implementation remains a shared-family consolidation and native-suite gate.

- Added caller-owned IAmazonS3 injection while retaining the existing endpoint/access-key constructor and ThreeFold inheritance. Deactivation preserves caller ownership; owned clients use MaxErrorRetry=0 to avoid implicit mutation replay.
- Only HTTP404 with NoSuchKey is object absence. NoSuchBucket/AccessDenied and other service errors retain the original SDK exception.
- Avatar/holon soft deletion shares one local generic operation, acknowledges only completed saves and propagates original OASISResult errors via the existing OASISResultHelper. Public holon Guid/provider-key facades preserve exceptions and deletion acknowledgement.
- Eight original read/delete regressions failed before correction; another eight public-facade regressions failed before error propagation was fixed. Final29/29 unit cases pass (24boundary controls+5existing). Real SDK response objects are controlled, not a live MinIO service or core-storage-v2 pass.
- Cached builds MinIO/ThreeFold/MinIO unit host passed with3/2/2warnings and0errors. No new package/runtime/server/download or raw evidence retained; no native data or cloud operation requested.

Next: consolidate MinIO/ThreeFold record policy with the shared S3 backend while preserving public constructors, provider identity and deletion semantics; require explicit persistent encryption-key configuration and validate legacy layout before changing storage. Bind the canonical suite to the actual MinIO provider; never count AWSS3-on-MinIO as MinIOOASIS acceptance.

Reusable output measurements (bin/obj bytes): Providers/Storage/NextGenSoftware.OASIS.API.Providers.MinIOOASIS7,436,876/935,436; Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.UnitTests49,206,152/987,934; Providers/Network/NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS7,475,489/837,977. No active test process; no new environment. Keep one reusable output per project, regenerate via existing lease and cached builds.

## Shared S3 family consolidation (28October)

AWSS3OASIS, MinIOOASIS and ThreeFold QSS share S3StorageProviderBase, official SDK3.7.502 S3ObjectBackend, persisted private-key backend and canonical record/query/history/deletion policy. MinIO's roughly900-line duplicate is removed. Constructors/provider identities and MinIO/ThreeFold default soft holon deletion remain. MinIO endpoint constructors use OASIS_MINIO_STORAGE_KEY; ThreeFold uses OASIS_THREEFOLD_STORAGE_KEY, or supply a persisted-key resolver. Bucket creation is provisioning, not activation; clients are caller-owned by default, Dispose zeros private key state.

A read-only per-instance legacy-prefix check gates every transport entry point, including operations before activation; four plaintext prefixes are refused unchanged. Failed checks remain failed without replay. This does not coordinate concurrent writers using different schemas; retire legacy writers before adoption. QSS HEAD, conditional requests, IAM and pagination still require its own endpoint acceptance.

Controlled canonical26sync+26async checks pass through each actual facade with the same suite and deterministic SDK responses. Local51MinIO/family cases include six canonical runs,24failure-boundary cases and privacy/layout/lifetime/count checks;41AWS,60shared lifecycle and5ThreeFold cases pass. Shared avatar/detail deletion now reports DeletedCount=1 per selected root; eight shared count controls cover both modes/options. No full-interface/native claim follows.

Native MinIO binding uses explicit opt-in, existing isolated empty bucket, preserved checkpoints, default soft deletion plus exact fixture-only hard cleanup, and independent empty checks. It replaces smoke tests that created records without cleanup. CI reuses its existing MinIO service/bucket for AWSS3 and shipping MinIO sequentially. Native result pending CI; controlled ThreeFold proof is not QSS certification.
