# Oracle OCI Object Storage provider

This .NET 10 provider uses the official `OCI.DotNetSDK.Objectstorage` 149.0.0 client. The native adapter handles signed object requests, pagination and conditional revisions. Existing shared OASIS storage code owns records, identities, metadata selection, history and private-record encryption; this provider does not introduce a separate record format or encryption scheme.

## Configuration

Supply this fragment inside `OASIS.StorageProviders` in OASISDNA, replacing the non-secret example locations and names with an existing test profile and bucket:

```json
"OracleObjectStorageOASIS": {
  "ConfigFilePath": "/protected/oci/config",
  "ProfileName": "OASIS_TEST",
  "RegionId": "your-region-id",
  "NamespaceName": "your-object-storage-namespace",
  "BucketName": "your-existing-test-bucket",
  "KeyPrefix": "oasis/",
  "StorageEncryptionKeyEnvironmentVariable": "OASIS_ORACLE_OBJECT_STORAGE_ENCRYPTION_KEY",
  "IsVersionControlEnabled": true
}
```

These are the actual `OracleObjectStorageOASISSettings` properties used by `OracleObjectStorageProviderFactory`. The provider requires structured settings; a SQL connection-string override is rejected. It does not create buckets, profiles, signing identities or encryption keys. Provision and authorize the existing bucket separately, and keep the OCI profile/private-key files protected and outside source control.

The named environment variable must contain canonical base64 encoding of exactly 32 bytes, persisted separately from the OCI signing key. Configure it through protected deployment settings, not chat or a committed JSON value. Missing or invalid key configuration fails; the provider never silently generates a replacement. Keep the namespace, bucket, prefix and storage key stable for existing records. Changing the key without a reviewed re-encryption plan can make records unreadable. The private backend resolves its key lazily for that instance; changing an environment variable is not an in-place key migration.

`KeyPrefix` must end in `/` and must not contain `.` or `..` navigation segments. Use an isolated test namespace/prefix and record its ownership before writes.

## Storage and mutation behavior

Native completion is checked at the transport boundary: GET, PUT, LIST and bucket HEAD require HTTP 200; DELETE requires HTTP 204. Whole-record GET rejects ranged/partial and not-modified responses, including HTTP 200 carrying Content-Range. An unexpected success acknowledgement after a dispatched mutation is an uncertain outcome requiring original-object reconciliation, not an automatic retry or success. Read protocol failures do not imply that a mutation occurred. Existing native ETag and upload-checksum checks still apply.

Private record envelopes and keyed indexes use the existing shared private backend. Metadata queries are OASIS-level selections, not an OCI SQL/search service: some operations enumerate pages and decrypt records. Account for request count, storage, egress and scan cost when accepting a cloud deployment. Logical history increases retained payload and can increase request work; it is not a promise that an entire multi-object operation is one native transaction.

Conditional writes/deletes use native `If-Match`/`If-None-Match` headers. ETags are opaque revisions, never interpreted as MD5 hashes. A successful single-object upload must return a nonblank ETag and the matching server `opc-content-md5` acknowledgement. MD5 here is OCI transport-integrity metadata, not record encryption.

Only native `ObjectNotFound` is treated as object absence; an authorization/hidden-bucket error is not absence. Known conditional conflicts remain distinct from uncertain outcomes. The adapter limits SDK attempts to one; lost responses, server errors or malformed successful mutation acknowledgements use the existing reconciliation marker. Do not blindly replay a possibly committed operation or assume a failed response cost nothing. Native conditional operations do not themselves prove HyperDrive cross-provider failover, load balancing or replication.

Activation verifies bucket access through the existing backend lifecycle. Factory-created providers own and dispose their SDK client; callers supplying a client directly retain ownership unless they explicitly opt in to provider ownership. Deactivation/disposal is not bucket deletion. Never remove a bucket or encryption key as routine test cleanup.

## Verification boundaries

The unit/protocol tests use the real signed SDK against an in-process loopback fixture. They are not live OCI IAM, region, quota, billing, consistency or recovery acceptance. Live testing requires an authorized profile, isolated bucket and agreed cost limit. The current evidence, resource ledger and remaining Oracle gates are linked from the [working verification report](../../../Docs/WIP/HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md). Oracle Database/Autonomous storage is a separate provider with separate acceptance requirements.
