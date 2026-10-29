# AWS S3 CQL MVP â€” controls verified, native demo pending

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

## Native fixture and CI wiring prepared

`AwsS3CqlNativeTests.EncryptedNativeSourceFiltersThreeToTwoAndPreservesObjects`
now compiles. It requires an explicit already-running loopback service and a separately
retained storage key; it never starts services or accesses hosted AWS. First-use
admission refuses an existing bucket, fsyncs actor/IDs before writes and preserves
pending setup on any failure. Reuse requires the original bucket and decryptable
source. Native three/two/missing-record queries inspect exact encrypted object
bytes and ETags before/after. Production CQL does not use this test-only LIST.

`.github/workflows/ci-cd.yml` reuses the existing MinIO matrix service and its masked
key. It runs the native fixture, restarts that same service and repeats against the
same manifest/source/key. Artifacts contain TRX and synthetic identities, never keys.
Job service/data/key are disposable with that job; this is not a retained local demo.
YAML parsed locally; no CI native execution result exists for this change yet.

Local compilation/admission:7 controls passed,1 native skipped because no configured
service. `TestResults/AwsS3CqlCurrent/s3-cql-native-admission.trx`, SHA256
`4EC16A5A00FCAAE2722B013ADA1408AB1B88200B0CA8F57C1B7F2E6763BAC69C`.
A skip proves no native storage/query behavior.

## Exact next step and environment gate

Obtain the existing-service native/restart results, then complete Mongo-paired
SDK/HTTP/browser5/4. Inventory remains red until actual evidence exists.

Windows runtime-cache and WSL root Podman inventory contained no MinIO image.
The existing CI pinned image was cached once (178620060bytes), ID
`3d5ffffec36e6efdda5ed906939f1b4d0976f03fc33d1ea31ddcc67833089691`, digest
`sha256:d8cfccee3f7bf0182d89c223b2ff53b94c9a88237bc0dd5733dd49fa0f4dab4c`.
Automatic approval review rejected the combined local preparation/launch command
without a detailed reason beyond blocked by policy. Neither proposed runner nor
local native store/key/container was created; no launch retry or bypass was made.
Native test preparation and existing CI wiring proceeded independently.

Do not label compatible-service acceptance as hosted AWS. No billable requests
are authorized; hosted Azure remains pending. Reuse existing runtime resources;
[provider baseline](../HyperDrive-Providers/S3_COMPATIBLE_STORAGE_BASELINE.md).

[Current status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
