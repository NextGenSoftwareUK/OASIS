# AWSS3OASIS

## Public object-metadata query source

`AwsS3PublicMetadataSource` is an explicit read-only alternative for public S3
objects. It uses the official SDK's HEAD operation and never downloads object
bodies. CQL can select one configured `S3ObjectKey` or a bounded membership set
(up to eight keys, 128 printable ASCII characters each). It does not advertise
general content queries or owned-record persistence for this profile.

Set `PublicObjectMetadataOnly=true`, `Region`, `BucketName`,
`PublicObjectMetadataKeys` and a nonempty `PublicObjectMetadataOwner` in the
structured provider settings. Do not supply signing credentials or opt into
default credentials. The owner is the host's stable mapping identity for this
public source, not a claim about who uploaded an object. Core's native-record
mapper derives holon identities and metadata; every projected row is public.

Enroll this source as query-only with an explicit tariff and source examples.
Activation reads the first configured object's metadata. Queries read only their
configured selection, validate the complete selected result before paging, and
never return partial rows after an error. Direct backend mutations are unsupported;
normal WEB4 admission must reject writes before provider/outbox dispatch.

The first demo source uses the public [NOAA GOES dataset on AWS](https://registry.opendata.aws/noaa-goes/).
It demonstrates real object metadata, not the NetCDF observations inside each
object. This requires no AWS account, bucket provisioning or signing credentials.
Hosted enrollment/browser acceptance is tracked separately in
[CURRENT_STATUS](../../../Docs/WIP/COSMIC%20ORM/CURRENT_STATUS.md).

## Owned encrypted records

Stores encrypted OASIS records in an existing AWS S3 general-purpose bucket through
the official AWS SDK. This is distinct from `AWSOASIS`, which remains DynamoDB-backed.
The shared key-value layer owns records, identity lookup, queries and history; the
shared S3 layer owns SDK transport and native per-key preconditions.

Listing completion must be internally consistent: a page marked complete cannot
also advertise a next continuation token. Contradictory pages fail rather than
publishing a potentially incomplete list or guessing another request. This follows
the [S3 ListObjectsV2 contract](https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html).

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
