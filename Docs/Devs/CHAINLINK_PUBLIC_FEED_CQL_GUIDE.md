# ChainLink public feed CQL

This read-only profile queries configured AggregatorV3 proxy contracts on Ethereum mainnet. It is separate from signed ChainLink storage. No wallet, funding, local chain or source transaction is required.

## First setup and C# demo

Reuse a suitable existing checkout and .NET10 installation. Retain one configuration/owner between runs. In this agent worktree the qualified catalogue already exists at `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/source.json`; never overwrite it. On another developer machine create a new file once using this schema and a stable nonempty sourceOwner GUID:

```json
{
  "schema": 1,
  "rpcUrl": "https://ethereum-rpc.publicnode.com/",
  "chainId": "0x1",
  "sourceOwner": "00000000-0000-0000-0000-000000000123",
  "feeds": [
    { "address": "0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419", "name": "ETH / USD", "decimals": 8 },
    { "address": "0xf4030086522a5beea4988f8ca5b36dbc97bee88c", "name": "BTC / USD", "decimals": 8 },
    { "address": "0x2c1d072e956affc0d435cb7ac38ef18d24d9127c", "name": "LINK / USD", "decimals": 8 }
  ]
}
```

The sample owner is an illustrative public projection identity, not a credential or write authority. Assign your own owner once for independent local catalogues. Build producer before consumer under the repository lease with bounded build settings, then run:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
  --chainlink-public LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/source.json
```

[Commented walkthrough](../../Examples/CosmicCqlDemo/ChainLinkPublicWalkthrough.cs) activates the anonymous adapter, runs three-feed and two-feed selections through shared HolonManager/LINQ, then prints exact raw answers, decimal scales and round IDs. No Mongo records are created.

## Query shape

```csharp
// Explicit proxy keys become bounded native membership, never a chain scan.
var selected = new[] {
    "0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419",
    "0xf4030086522a5beea4988f8ca5b36dbc97bee88c"
};

var source = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.ChainLinkOASIS).Result)
    .Where(row => selected.Contains(
        CosmicQueryMetadata.Value<string>(row, "ChainLinkFeedAddress")))
    .Result;

// Supply the activated read-only adapter explicitly; handle OASISResult errors.
var query = CosmicLinq.Create(manager, adapter, source, maximumResults: 8).Result;
var rows = await query.ToCosmicListAsync(cancellationToken);
```

Exact address equality and membership of at most eight configured keys are supported. Unsupported predicates fail planning. Unknown keys are rejected before native I/O. Paging happens after every selected row is validated; source failures never return partial rows.

## HTTP and the live CQL tab

Choose `ChainLinkOASIS` in the sandbox CQL tab; discovery reports source availability. Use an authenticated demo session and a fresh `Idempotency-Key` for HTTP requests; never share its bearer. Hosted rollout status is recorded in the linked evidence report.

Native filtered request to `POST /api/data/query-cql`:

```json
{
  "provider": "ChainLinkOASIS",
  "orderBy": "None",
  "take": 8,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["ChainLinkFeedAddress"],
      "scalar": "String",
      "operator": "In",
      "value": [
        "0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419",
        "0xf4030086522a5beea4988f8ca5b36dbc97bee88c"
      ]
    }
  ]
}
```

A paired request to `POST /api/data/query-cql/federation` scopes both providers explicitly. The query itself creates no copies. Use the CQL tab's preparation action once to save two private Mongo identity/name/description/provenance copies, then reuse them:

```json
{
  "scope": "Named",
  "providers": ["MongoDBOASIS", "ChainLinkOASIS"],
  "identityMode": "PreserveCopies",
  "maximumProviderAttempts": 2,
  "take": 8,
  "source": {
    "orderBy": "None",
    "take": 8,
    "includeMetadata": true,
    "filters": [
      {
        "metadataPath": ["ChainLinkFeedAddress"],
        "scalar": "String",
        "operator": "In",
        "value": [
          "0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419",
          "0xf4030086522a5beea4988f8ca5b36dbc97bee88c"
        ]
      }
    ]
  }
}
```

The native response contains two public observations. After preparation, pairing returns those two plus your two matching private Mongo copies. Other visitors cannot see your copies. The copies do not contain the full native feed payload. A native error fails the paired query; it does not silently substitute Mongo results.

## Data and consistency

Every query checks chain1 and the pinned Ethereum genesis, resolves an RPC-reported `finalized` block, and binds all description/decimals/latestRoundData calls to that hash with EIP1898 `requireCanonical:true`. Contract descriptions and scales must match the explicit catalogue. Each query resolves its own block; no cross-query/provider snapshot or independent consensus proof is claimed.

Metadata includes `ChainLinkFeedAddress`, `ChainLinkFeedName`, `ChainLinkChainId`, `ChainLinkDecimals`, `ChainLinkRoundId`, `ChainLinkAnswer`, `ChainLinkStartedAt`, `ChainLinkUpdatedAt`, `ChainLinkAnsweredInRound`, `ChainLinkBlockHash`, `ChainLinkBlockNumber`, `ChainLinkBlockTimestamp` and `ChainLinkCommitment`. Integers are exact decimal strings: uint80 rounds, signed int256 answers, uint256 timestamps. Apply the accompanying decimal scale explicitly; never pass raw values through double. `answeredInRound` is deprecated and is preserved without treating it as a freshness guarantee. Valid signed negative answers remain representable.

The configuration is anonymous HTTPS root only, exact known fields, one to eight unique canonical lowercase nonzero proxy addresses, bounded names and decimal scales0–255. The production factory uses NativeConnectionGuard; injected HttpClient supports controlled transports. Responses have a256KiB budget and duplicate properties/envelope errors/invalid ABI widths, offsets, padding and UTF8 fail closed. Native saves/deletes are rejected.

A finalized observation can contain an old feed update. Expose timestamps and evaluate application-specific freshness separately; this demo does not guarantee a current price or suitability for a financial decision. Catalogue/native evidence is recorded at a specific observation and is not a promise that contracts never change.

## Verification and remaining gates

[Controlled tests](../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.UnitTests/ChainLinkPublicFeedMetadataTests.cs) cover these contracts. [Current evidence](../WIP/COSMIC%20ORM/CHAINLINK_PUBLIC_SOURCE_STATUS.md) separates native qualification, CQL/manager, API/Mongo, hosted and browser gates. Query-only API admission (`CQL_CHAINLINK_PUBLIC_SOURCE_JSON`), local HTTP native3/2/Mongo5/4/visitor privacy and all four prepared presets now pass. Hosted HTTP acceptance now passes: native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching replicas2/2 each on SQLite/Neo4j. Both normal private saves are version1 with no pending preparation intents. Four native/Mongo presets are available through discovery and the sandbox CQL tab. Browser interaction remains unverified; HTTP/public asset matching does not establish it. Generic browser demo copies currently retain identity/name/description/provenance rather than all native metadata.

Primary sources: [Chainlink AggregatorV3 reference](https://docs.chain.link/data-feeds/api-reference), [official Ethereum catalogue configuration](https://raw.githubusercontent.com/smartcontractkit/documentation/main/src/features/data/chains.ts), [reference catalogue](https://reference-data-directory.vercel.app/feeds-mainnet.json).
