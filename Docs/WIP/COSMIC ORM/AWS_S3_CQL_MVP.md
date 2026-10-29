# AWS S3 CQL MVP — controls verified, native demo pending

29 October 2026. AWSS3OASIS now implements ICosmicQueryStorageProvider.
This is separate from the DynamoDB AWSOASIS demo. Inventory remains red.

## Implementation and acceptance

- Explicit AWSS3OASIS scope and frozen server-authorized ID catalogue.
- ID equality/membership, Boolean algebra and paging; at most100 selected IDs.
- Existing canonical RecordKey and persisted encrypted backend own key derivation,
  SDK reads, authenticated decryption and decoding. No bucket LIST or managed scan.
- Unsupported metadata/content/order/child queries reject before native I/O.
- Whole query fails on inaccessible/corrupt objects; no partial rows published.
- Caller cancellation returns a plain cancellation error; SDK-only failure stays failure.
- Four MiB decoded canonical-response budget and result budget1..10000. This does
  not bound the existing S3 backend's allocation before a whole object is decrypted.
- Separate object reads observe separate moments; no distributed snapshot claim.

## Verified controls

AwsS3CqlTests:7/7, zero skips. Captured selection/catalogue isolation, wrong authority,
empty catalogue, unsupported shapes, inactive provider and pre-cancellation.
Report TestResults/AwsS3CqlCurrent/s3-cql-controls.trx; SHA256 D16ABA5DF998157869DD626490B3828B5DD7D8CB27CE0192A5B1DE1AB2877D9F.

AwsS3CqlEncryptedTests:1/1, zero skips. Official signed AWS SDK over existing controlled
HTTP service: three encrypted records filter to two, no LIST/write/delete, exact
source bytes unchanged, later-object failure discards earlier rows, live caller
cancellation discards rows. This is controlled SDK evidence, not a native service.
Report TestResults/AwsS3CqlCurrent/s3-cql-encrypted.trx; SHA256 F7D7CC6AF56241B49542471FD15D61750E5AAFC0A685EDF969173FBC1E6AB7B3.

## Exact next step

Inspect/reuse the existing CI MinIO service or retained local runtime/image; no
local MinIO runtime was found in the first Windows runtime-cache inspection.
Prior native shared-storage conformance uses CI MinIO (see linked provider report).
Create one retained COSMIC source/key/catalogue with explicit empty-source admission,
prove native3/2 and full restart reuse, then add Mongo-paired SDK/HTTP/browser5/4.
Do not label compatible-service acceptance as hosted AWS. No billable requests
are authorized; hosted Azure remains pending.

Existing CI service: .github/workflows/ci-cd.yml MinIO matrix entry, pinned release
RELEASE.2025-04-22T22-12-26Z. Shared storage proof:
[provider baseline](../HyperDrive-Providers/S3_COMPATIBLE_STORAGE_BASELINE.md).

[Current status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
