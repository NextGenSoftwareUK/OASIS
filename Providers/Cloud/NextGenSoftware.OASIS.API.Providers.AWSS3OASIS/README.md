# AWSS3OASIS

Stores encrypted OASIS records in an existing AWS S3 general-purpose bucket through
the official AWS SDK. This is distinct from `AWSOASIS`, which remains DynamoDB-backed.
The shared key-value layer owns records, identity lookup, queries and history; the
shared S3 layer owns SDK transport and native per-key preconditions.

Configure `OASIS.StorageProviders.AWSS3OASIS` with `Region`, `BucketName`, `KeyPrefix`
(default `oasis/`), `UseDefaultCredentials`, and
`StorageEncryptionKeyEnvironmentVariable` (default
`OASIS_AWS_S3_STORAGE_ENCRYPTION_KEY`). Choose either SDK default credentials or
explicit `AccessKey`/`SecretKey` and optional `SessionToken`, never both.
Keep signing credentials outside source control. Persist the storage key separately:
canonical base64 for exactly 32 bytes. Losing it loses access to encrypted records;
the provider never creates a replacement key.

Activation only verifies the bucket. It does not provision one. SDK automatic retries
are disabled: uncertain mutation outcomes must be reconciled before replay. Native
per-key preconditions are not an atomic transaction across all objects in an OASIS save.
Application history and S3 bucket versioning are separate mechanisms.

Whole-object GET explicitly enables the SDK's native checksum validation, including
when the caller uses `ResponseChecksumValidation.WHEN_REQUIRED`. It remains one GET,
not a second HEAD or a custom checksum implementation. General-purpose SSE-KMS
objects require the appropriate `kms:Decrypt` and key-policy permissions for checksum
retrieval. Review those permissions and real account costs before cloud acceptance;
compatible-service errors are not retried without checksum validation.

S3 storage, requests, retrieval, KMS and network transfer can incur charges. Search
and identity scans may require multiple LIST/GET requests. Deletes in versioned
buckets may retain billable historical versions; an acknowledged delete does not
prove storage charges have stopped. No claim of zero-cost or unlimited sponsored
operations is made.

Status: configuration, registration builds and controlled SDK tests are verified.
Live encrypted lifecycle, hosted IAM/credential policies, durability, fees and full
HyperDrive V2 acceptance remain required. See the current audit in
`Docs/WIP/HyperDrive-Providers/AWS_DIRECT_CONFIGURATION_AUDIT_2026-10-20.md`.
