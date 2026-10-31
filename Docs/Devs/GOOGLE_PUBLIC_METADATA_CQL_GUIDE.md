# Public Google Cloud metadata with CQL

This read-only GoogleCloudOASIS profile queries metadata for explicitly configured
public Cloud Storage objects. It uses anonymous SDK requests: no Google account,
credentials, billing project, bucket creation or object-body downloads.

Implementation and local HTTP pairing are verified. Hosted enrollment and browser
acceptance remain pending; consult [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md).
The existing Firestore emulator demo is a different storage implementation.

## Sandbox walkthrough after enrollment

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a
demo session and open CQL Playground. Select GoogleCloudOASIS only when discovery
marks it available. The host supplies these four presets:

| Preset | Expected physical rows |
|---|---|
| Public Google object metadata, three keys | 3 public Google records |
| Public Google object metadata, two keys | 2 public Google records |
| Google metadata + my Mongo records | 3 Google + 2 private Mongo = 5 |
| Two-object Google metadata + my Mongo records | 2 Google + 2 private Mongo = 4 |

Before federation, use Prepare records to create two private Mongo snapshots via
normal WEB4 create-only saves. Repeating preparation preserves acknowledged
snapshots. Another visitor sees the public Google records, not your private copies.
Federation preserves provider copies; it does not promise a distributed snapshot.

## HTTP requests

GET `/api/data/query-cql/providers?examplesVersion=2` with your WEB4 session bearer.
Use the discovered GoogleCloudOASIS examples rather than assuming every host has
identical keys. POST a leaf example's request to `/api/data/query-cql`; POST a
Federation example's request to `/api/data/query-cql/federation`.

```json
{
  "provider": "GoogleCloudOASIS",
  "orderBy": "None",
  "take": 8,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["GCSObjectKey"],
      "scalar": "String",
      "operator": "In",
      "value": ["<copy an exact configured key from discovery>"]
    }
  ]
}
```

The placeholder must be replaced with an actual discovered key. Rows appear in
`result.rows` after the normal transport envelope is unwrapped. Metadata describes
the objects, not the imagery or text inside them. Object sizes, generations and
metagenerations are exact decimal strings; updated timestamps retain source text.

## C# usage and executable checks

```csharp
// Use the host's existing stable owner and explicit catalogue. This owner maps
// source identities; it does not claim ownership of the public Google objects.
using var source = new GoogleCloudPublicMetadataSource(bucket, keys, sourceOwner);

var activated = await source.ActivateQuerySourceAsync(cancellationToken);
if (activated.IsError)
    throw new InvalidOperationException(activated.Message);

// Build a deferred predicate over two or three configured keys.
var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result)
    .Where(row => selectedKeys.Contains(
        CosmicQueryMetadata.Value<string>(row, "GCSObjectKey")));

if (query.IsError)
    throw new InvalidOperationException(query.Message);

// The shared planner verifies native capability before any query I/O.
var plan = CosmicQueryPlanner.Plan(
    query.Result,
    ProviderType.GoogleCloudOASIS,
    new[] { new CosmicQueryTarget(ProviderType.GoogleCloudOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);
var result = await source.ExecuteCosmicQueryAsync(
    plan.Result,
    cancellationToken: cancellationToken);

if (result.IsError)
    throw new InvalidOperationException(result.Message);
```

The [provider tests](../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests/GooglePublicMetadataTests.cs)
include real anonymous SDK three/two-record reads. The
[HTTP pairing test](../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/CqlGcsMongoNativeTests.cs)
uses shipping endpoints, two real private Mongo snapshots, five/four-row federation
and a second actor's isolation check. Its controlled bearer/usage authority does
not certify production login, billing, replication or browser behaviour.

Reuse the retained source catalogue and Mongo runtime. Build with the provider
lease and bounded workers, then run
`Scripts/Sandbox/Test-CqlReplicaBaselineRepair.ps1 -GcsMongoHttp`.
The runner owns its own lease. Original intent/ACK guards prevent automatic fixture
replay; review its lifecycle before any rerun. Logs/TRX belong in TestResults.

## Host configuration

`CQL_GCS_PUBLIC_SOURCE_JSON` accepts exactly:

```json
{
  "schema": 1,
  "bucket": "gcp-public-data-landsat",
  "keys": ["<explicit public object key>"],
  "owner": "<stable nonempty mapping UUID>"
}
```

The sample values are placeholders, not a deployable catalogue. The retained demo
configuration has three verified Landsat keys. Maximum JSON size is 8 KiB and
maximum depth is four. Credentials, billing projects, endpoints, duplicate fields
and unknown fields are rejected. Register query-only admission and an explicit
free query tariff, plus four hosted examples. Never enroll this source as a write
or replication target. Canonical Firestore DNA remains unchanged.

`Scripts/Sandbox/Publish-HostedGooglePublicConfiguration.py` prepares a protected
before/candidate snapshot without changing the service. Its explicit apply action
uses the existing journalled publisher without triggering a deployment. Publish
only with the matching tested application/Core revision in a scheduled batch;
reconcile unknown outcomes before any repeat action.

Supported predicates are exact equality and membership on GCSObjectKey, at most
eight configured keys of 128 printable ASCII characters. Unknown keys fail before
source I/O. General name/description/content scans, arbitrary bucket listings,
sorting and cursors are unsupported. NoAcl metadata requests use RetryOptions.Never
and no UserProject. The full selection validates before publishing any page.
