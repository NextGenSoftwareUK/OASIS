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

Reproduce via existing MinIO CI job and S3CompatibleStorageConformanceTests opt-in binding. Local41AWS controlled tests separately passed; not part of native52. PR501 stays draft: broader CI/deployment gates remain independent.
