# CQL over public Wormhole messages

`WormholePublicOperationMetadataSource` is a read-only profile under
`WormholeOASIS`. It reads explicit public operation identities from Wormholescan
and binds each VAA body to an acknowledged SHA256 fingerprint. The existing
cross-chain tracker is separate; this profile never dispatches a message.

## Configure once and retain the catalogue

Use `LocalState/COSMIC/HostedWormholePublicSourceCurrent/source.json` for the
qualified local demo. Keep its original owner and body fingerprints across runs.
Configuration has exactly four fields: `schema` (1), `apiUrl` (anonymous HTTPS
root), `messages` (1–8 canonical operation-ID/body-SHA256 entries), and
`sourceOwner` (one nonempty retained projection GUID). It accepts no signer,
billing or authentication properties. URLs are host configuration, not query input.

An operation ID is `uint16-emitter-chain/64-lowercase-hex-emitter/uint64-sequence`.
Both integers use canonical unsigned decimal text. A body fingerprint has64
lowercase hex characters. A reorg/body change fails explicitly; never replace
an acknowledged fingerprint simply to make the query succeed.

## Run the C# walkthrough

Build `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj` with the existing .NET10SDK.
Use bounded build workers per repository instructions. Then, from the worktree:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --wormhole-public LocalState/COSMIC/HostedWormholePublicSourceCurrent/source.json
```

`WormholePublicWalkthrough.cs` is spaced and commented. It activates the explicit
read profile, creates a V2 HolonManager, and runs the shared deferred LINQ helper
for three keys and then two. The source owns native reads; HolonManager owns
planning/execution. Unsupported content predicates fail capability planning.

The central pattern, after creating `manager`, `source` and an authenticated
`actor`, is:

```csharp
var scope = CosmicQueryScope.Named(ProviderType.WormholeOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .WithReadAccess(access)
    .Where(row => messageIds.Contains(
        CosmicQueryMetadata.Value<string>(row, "WormholeOperationId")))
    .Result;

var linq = CosmicLinq.Create(manager, source, query, maximumResults: 40).Result;
var rows = await linq.ToCosmicListAsync(cancellationToken);

if (rows.IsError)
    throw new InvalidOperationException(rows.Message, rows.Exception);

foreach (var row in rows.Result)
{
    Console.WriteLine(row.MetaData["WormholeSequence"]);
    Console.WriteLine(row.MetaData["WormholeBodySha256"]);
}
```

Check `OASISResult` at application boundaries; the full executable helper does so.
Exact key equality, membership, Skip and Take are supported. The complete selected
source is read/validated before paging, even for an empty page. Unknown keys,
malformed bodies, cancellation and budget failures publish no partial rows.

## Returned metadata and trust

| Key | Meaning |
|---|---|
| `WormholeOperationId` | Exact configured operation tuple, query key |
| `WormholeEmitterChain`, `WormholeEmitterAddress`, `WormholeSequence` | Body-bound identity; uint64 sequence retains precision |
| `WormholeGuardianSetIndex`, `WormholeSignatureCount` | Framing metadata, not authentication |
| `WormholeTimestamp`, `WormholeNonce`, `WormholeConsistencyLevel` | Exact body integers as decimal strings |
| `WormholePayloadBytes`, `WormholePayloadSha256` | Opaque payload size/content fingerprint |
| `WormholeBodySha256` | Original configured body fingerprint |
| `WormholeObservationPolicy` | `indexer-reported-pinned-vaa-body` |
| `WormholeSignaturesVerified` | `false` |

The parser validates version1, full signature/body framing, increasing guardian
indices, API guardian index, exact emitter tuple, and configured body fingerprint.
It does not supply a trusted guardian set or authenticate signatures. SHA256 here
is a content fingerprint, not Wormhole's signing digest. Payload semantics,
destination redemption, independent consensus and finality are not established.

Source rows are flat public projections with stable owner-scoped OASIS IDs;
normal provider writes/deletes are refused. No native source mutation or default
write-provider registration is performed by the example.

## Hosted HTTP demo verified

Local source35tests, actual manager/LINQ CLI3/2, Core1/API3/localHTTP1,
publisher9/prepared1 and105client controls pass. Original hosted cohort verifies
native3/2, Mongo5/4, reuse0new/2existing, foreign-private0/public3 and exact
SQLite/Neo4j replicas2/2. Two private version1 saves remain unchanged, pending0.
Select WormholeOASIS and its native or Mongo paired preset in the CQL tab.
The public information card and pinned developer links are published. Browser interaction
is separate from these HTTP/client-module and replication checks.

See the [release/evidence report](../WIP/COSMIC%20ORM/WORMHOLE_PUBLIC_SOURCE_STATUS.md).

## Local HTTP integration verified

The shipping compiler/read-profile and query-only admission now pass local HTTP
native3/2, Mongo federation5/4 and foreign-private0/public3. The four prepared
hosted presets also plan against shipping capabilities. Hosted acceptance now passes; the live
card is published; this supersedes the earlier implementation-pending note.

Set host-only `CQL_WORMHOLE_PUBLIC_SOURCE_JSON` to the retained source JSON,
with an explicit independently permitted query-only tariff. This does not grant
source writes or enable cross-chain dispatch. The hosted publisher checks free
pricing and, when encryption is enabled, an explicit queryable-key policy for
`WormholeOperationId`; it does not alter encryption or DNA settings.

POST `/api/data/query-cql` using provider `WormholeOASIS`, `orderBy: "None"`,
`includeMetadata: true`, a bounded take and a filter with
`metadataPath: ["WormholeOperationId"]`, `scalar: "String"`, `operator: "In"`,
and a value array of retained catalogue keys. For federation, POST
`/api/data/query-cql/federation` with scope `Named`, providers
`["MongoDBOASIS", "WormholeOASIS"]`, that filter in `source`,
`identityMode: "PreserveCopies"`, `maximumProviderAttempts: 2` and a bounded
take/duration. The four prepared examples provide native three/two selections
and corresponding Mongo pairs. Authentication and provider permissions still apply.

Local SDK fixtures copy the projected native metadata. Generic browser copies
currently retain identity/name/description/provenance and the operation query key,
not the full native payload. Browser interaction and live replication are separate
acceptance gates. Existing private version1 demo snapshots keep their first
observation; repeat setup never silently updates them.
