# Public AWS S3 metadata with CQL

This explicit read-only profile queries native metadata of configured public S3
objects using anonymous AWS SDK HEAD requests. It downloads no object bodies and
creates no AWS account or bucket. The current sample uses NOAA GOES object keys.

## Hosted walkthrough

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a
demo session, then open CQL Playground. Select AWSS3OASIS when discovery marks it
ready. The server supplies these four presets:

| Preset | Expected rows |
|---|---|
| Public S3 metadata (3 objects) | Three native metadata records |
| Public S3 metadata (2 objects) | A two-key subset |
| S3 metadata + my Mongo records | Three source records and two private snapshots |
| Two-object metadata + my Mongo records | Two source records and those same snapshots |

Use the playground preparation action before the paired presets. It creates two
private Mongo snapshots through normal WEB4 saves. Repeat preparation preserves
existing snapshots. Source objects are unchanged. Federation preserves provider
copies; it does not establish a distributed transactional snapshot.

Live release and acceptance evidence are tracked in
[CURRENT_STATUS](../WIP/COSMIC%20ORM/CURRENT_STATUS.md). This guide describes the
contract and does not certify a particular deployment.

## HTTP examples

With the session bearer, GET `/api/data/query-cql/providers?examplesVersion=2`.
Find AWSS3OASIS and use its `public-records` or `public-subset` example's `request`
as the body of POST `/api/data/query-cql`. This uses the host's actual catalogue
instead of assuming every node has the same bucket or keys.

The selection is a single string-membership filter on metadata path
`["S3ObjectKey"]`, with `orderBy: "None"` and `includeMetadata: true`. It selects
three configured keys or two of those keys. Rows are in `result.rows` after the
normal transport envelope is unwrapped. They describe objects, not the NetCDF
observations inside them.

For federation, use the `mongo-public-records` or `mongo-public-subset` request
with POST `/api/data/query-cql/federation`. Each names MongoDBOASIS and AWSS3OASIS
explicitly. Use the playground preparation action first to create your private
snapshots. Another avatar cannot read those private snapshots.

## C# and local verification

The executable C# example is
[`ActualAnonymousS3SdkReturnsThreeMetadataRecordsAndTwoRecordSubset`](../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests/AwsS3PublicMetadataTests.cs).
It constructs the public adapter, awaits read-only activation, binds the caller's
read policy and composes this predicate on `CosmicQuery<Holon>`:

```csharp
// Capture two or three keys from the explicitly configured source catalogue.
// No request runs while this predicate is being composed.
row => selected.Contains(
    CosmicQueryMetadata.Value<string>(row, "S3ObjectKey"))
```

The shared CQL planner checks the named AWSS3OASIS target and its capabilities
before execution. The test proves three native metadata records and a two-record
subset using the actual anonymous SDK, without an S3 emulator or object downloads.

Reuse the existing AWS provider test project/environment. Explicitly set
`OASIS_AWS_S3_PUBLIC_SOURCE` to a catalogue JSON containing `region`, `bucket`,
`keys` and a stable mapping `owner`, then select test category `AwsS3PublicNative`.
It needs network access and creates no S3 objects. Put logs/TRX in the worktree's
`TestResults/`, never in Docs. Retain the reusable source catalogue.

## Host configuration and boundaries

Configure `AWSS3OASIS` with `PublicObjectMetadataOnly=true`, `Region`, `BucketName`,
`PublicObjectMetadataKeys` and a nonempty `PublicObjectMetadataOwner`. The owner
identifies the host's mapped source; it does not claim ownership of NOAA uploads.
Supply no signing credentials, default credentials or connection override for
this profile. Enroll it as query-only with an explicit query tariff and examples.
Do not enroll it as a writable replication target.

Equality and membership support at most eight configured keys, each at most
128 printable ASCII characters. General content/name/description searches,
arbitrary bucket scans, sorting and continuation cursors are unsupported. Unknown
keys reject before source I/O. The complete selection is validated before paging;
native errors and cancellation return errors without partial rows.

This profile is separate from encrypted owned AWSS3OASIS storage and from
AWSOASIS (DynamoDB). See the
[provider README](../../Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSS3OASIS/README.md)
for those storage settings and boundaries.
