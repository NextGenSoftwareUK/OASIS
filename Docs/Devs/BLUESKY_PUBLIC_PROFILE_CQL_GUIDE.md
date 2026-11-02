# BlueSky public profile CQL

This anonymous read-only profile projects cached public AppView profiles into holons. It is separate from the credentialed BlueSky/PDS storage provider. No BlueSky login, app password, PDS session or source write is required. Public profile data does not establish OASIS login authority or verified DID ownership.

## First setup and C# demo

Reuse a suitable existing checkout and .NET10 installation. In this agent worktree, the qualified catalogue already exists at `LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent/source.json`; never overwrite it. On another developer machine, create one retained file using this schema and assign a stable nonempty owner GUID once:

```json
{
  "schema": 1,
  "apiUrl": "https://public.api.bsky.app/",
  "sourceOwner": "00000000-0000-0000-0000-000000000123",
  "actors": [
    "did:plc:z72i7hdynmk6r22z27h6tvur",
    "did:plc:ewvi7nxzyoun6zhxrhs64oiz",
    "did:plc:oky5czdrnfjpqslsw2a5iclo"
  ]
}
```

The illustrative owner is public projection identity, not a credential. Keep your own chosen owner stable across runs. Build producer before consumer under the repository lease with bounded build settings, then run:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
  --bluesky-public LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent/source.json
```

[Commented walkthrough](../../Examples/CosmicCqlDemo/BlueSkyPublicWalkthrough.cs) activates the explicit public source and queries three DIDs, then two, through shared HolonManager/LINQ. It prints public names and handles; it creates no Mongo copies.

## Query explicitly selected DIDs

```csharp
// Captured stable DIDs identify profiles; handles and display text can change.
var selected = new[] {
    "did:plc:z72i7hdynmk6r22z27h6tvur",
    "did:plc:ewvi7nxzyoun6zhxrhs64oiz"
};

var source = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.BlueSkyOASIS).Result)
    .Where(row => selected.Contains(
        CosmicQueryMetadata.Value<string>(row, "BlueSkyActorDid")))
    .Result;

// Supply the activated adapter explicitly and handle OASISResult errors.
var query = CosmicLinq.Create(manager, adapter, source, maximumResults: 8).Result;
var rows = await query.ToCosmicListAsync(cancellationToken);
```

Exact DID equality and membership of at most eight configured `did:plc` identifiers are supported. Unknown keys fail before native I/O. Unsupported predicates fail planning. One bounded getProfiles batch supplies each query; every selected profile is validated before paging/publication. Missing, duplicate or unexpected DIDs fail the whole query. There is no profile search, firehose, per-account discovery or avatar/banner fetch.

## HTTP filtered queries and Mongo pairing

These requests use the same two-DID selection as the prepared demo presets. Hosted enrollment is still pending; check the current evidence report before presenting this as a live feature. Once enrolled, discovery exposes the native and paired presets for `BlueSkyOASIS`.

Use an authenticated OASIS demo session and a fresh `Idempotency-Key` for each HTTP request. Keep the bearer private. No BlueSky credentials are needed.

Send this body to `POST /api/data/query-cql` to select two public profiles:

```json
{
  "provider": "BlueSkyOASIS",
  "maximumDurationMilliseconds": 30000,
  "orderBy": "None",
  "take": 25,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": [
        "BlueSkyActorDid"
      ],
      "scalar": "String",
      "operator": "In",
      "value": [
        "did:plc:z72i7hdynmk6r22z27h6tvur",
        "did:plc:ewvi7nxzyoun6zhxrhs64oiz"
      ]
    }
  ]
}
```

The filter selects captured DIDs rather than mutable handles. `orderBy: "None"` avoids requesting unsupported sorting. `includeMetadata` exposes observed handles, optional counts and dates with their documented precision and caching semantics.

For pairing, prepare two private Mongo copies once through the demo preparation action, then reuse them. A query creates no copies. Send this body to `POST /api/data/query-cql/federation`:

```json
{
  "scope": "Named",
  "providers": [
    "MongoDBOASIS",
    "BlueSkyOASIS"
  ],
  "source": {
    "orderBy": "None",
    "take": 25,
    "includeMetadata": true,
    "filters": [
      {
        "metadataPath": [
          "BlueSkyActorDid"
        ],
        "scalar": "String",
        "operator": "In",
        "value": [
          "did:plc:z72i7hdynmk6r22z27h6tvur",
          "did:plc:ewvi7nxzyoun6zhxrhs64oiz"
        ]
      }
    ]
  },
  "identityMode": "PreserveCopies",
  "maximumProviderAttempts": 2,
  "maximumDurationMilliseconds": 30000,
  "take": 25
}
```

`Named` restricts execution to MongoDB and the public BlueSky profile source. `PreserveCopies` keeps each provider's rows visible: the two-profile selection returns two public observations plus the current visitor's two matching private copies after preparation. The three-profile preset returns three public observations plus those two private copies. Another visitor cannot see your private copies.

Generic browser preparation retains identity, name, description and mapped provenance; it does not capture the full native metadata payload. Public profile text can change independently of a saved private copy. A native failure fails the paired query rather than silently returning only Mongo rows. Pairing does not claim a shared snapshot or verified DID ownership.

## Fields and semantics

`Name` maps to a nonblank public displayName, otherwise the public handle. `Description` maps to optional biography text. Metadata includes `BlueSkyActorDid`, `BlueSkyHandle`, `BlueSkyObservationPolicy` and optional `BlueSkyDisplayName`, `BlueSkyFollowersCount`, `BlueSkyFollowsCount`, `BlueSkyPostsCount`, `BlueSkyCreatedAt` and `BlueSkyIndexedAt`.

Missing optional metadata stays missing; no count0 or current timestamp is invented. Present counts must be exact nonnegative64-bit integers and remain decimal strings, including values above2^53. Timestamp strings retain their original RFC3339 representation. Unicode profile text is preserved. Avatar/banner and unrelated view fields are neither fetched nor projected.

These are cached public AppView observations, not signed repository records, bi-directionally verified DID documents, authoritative freshness or a cross-profile snapshot. The handle is observed text, not the selection key. The same DID can return changed public content in later queries.

The configuration requires exact fields, an anonymous HTTPS root, one to eight unique canonical `did:plc` keys and a nonempty stable owner. Credential fields/URL credentials are rejected. The production factory uses the shipping guarded sockets transport with cookies disabled. Responses are bounded to256KiB; malformed JSON/duplicate properties/type errors/invalid dates/oversized text/cancellation return errors without partial rows. Native saves/deletes are rejected.

## Verification and remaining gates

[Controlled tests](../../Providers/Social/TestProjects/NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS.UnitTests/BlueSkyPublicProfileMetadataTests.cs) cover the read contract. [Current evidence](../WIP/COSMIC%20ORM/BLUESKY_PUBLIC_SOURCE_STATUS.md) distinguishes native qualification, CQL/manager, API/Mongo, hosted and browser gates. Query-only API admission (CQL_BLUESKY_PUBLIC_SOURCE_JSON), local HTTP native3/2/Mongo5/4/visitor privacy and all4prepared presets now pass. Hosted publication and browser interaction remain pending; local proof does not establish them. Generic browser demo copies currently retain identity/name/description/provenance rather than all native metadata.

Primary references: [API hosts and auth](https://bsky.network/docs/api-directory/), [HTTP reference](https://endpoints.bsky.app/), [public endpoint limits/caching](https://bsky.network/docs/rate-limits/).
