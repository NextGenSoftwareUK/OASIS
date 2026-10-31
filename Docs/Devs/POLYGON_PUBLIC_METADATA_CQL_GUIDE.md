# Public Polygon balances with CQL

This explicit read-only PolygonOASIS profile queries configured mainnet address
balances through the common COSMIC CQL interfaces. It needs no wallet, signature,
funding or local Polygon node. Native CQL and local HTTP/Mongo pairing are
verified; hosted enrollment and browser acceptance remain pending. Check
[current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md) before claiming live support.

## Run the C# console demo

From the existing checkout, build Examples/CosmicCqlDemo with the normal provider
build lease and bounded workers. Reuse its installed SDK and package cache.
Then run:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build --no-restore -- `
    --polygon-public LocalState/COSMIC/HostedPolygonPublicSourceCurrent/source.json
```

[PolygonPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/PolygonPublicWalkthrough.cs)
is the spaced, commented executable example. It uses
[NativeKeySetWalkthrough](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs)
through HolonManager and CosmicLinq, prints the three-address selection and a real
two-address subset, exact native-base-unit balance strings and the source block hash, then exits.
It creates no local database or chain transaction.

The sample catalogue is a nonsecret file retained outside TestResults. On a
separate checkout, an explicit catalogue can use this complete shape:

```json
{
  "schema": 1,
  "rpcUrl": "https://polygon-bor-rpc.publicnode.com/",
  "chainId": "0x89",
  "addresses": [
    "0x0000000000000000000000000000000000000001",
    "0x0000000000000000000000000000000000000002",
    "0x0000000000000000000000000000000000000003"
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

Keep the mapping owner stable when reusing a catalogue. It determines mapped OASIS
IDs; it does not control funds or claim ownership of these public addresses.
The retained developer catalogue and hosted catalogue may use different mapping
owners, so use each host's discovered identities rather than copying OASIS IDs.

## What the query means

```csharp
// selectedAddresses contains two or three configured lowercase address strings.
// No RPC runs during composition; the native planner admits this exact shape.
row => selectedAddresses.Contains(
    CosmicQueryMetadata.Value<string>(row, "PolygonAccountAddress"))
```

For each query, the adapter checks mainnet chain ID0x89and pinned genesis, resolves an
RPC-reported finalized block, and reads every balance with its canonical hash.
Native balance and block-number metadata remain exact decimal strings, including UInt256
values above the exact JavaScript number range. Metadata includes chain ID,
block number/hash and commitment. This is a native address-state projection,
not proof of a COSMIC holon being persisted on-chain or a cryptographic light-client
verification of the RPC response. Different queries can observe different blocks.

## HTTP and sandbox presets after hosted enrollment

GET `/api/data/query-cql/providers?examplesVersion=2` with a WEB4 session bearer.
Select PolygonOASIS only when discovery marks it ready. The server owns the
configured address list and supplies these four presets:

| Preset | Expected physical rows |
|---|---|
| Public Polygon balances, three addresses | 3 public Polygon rows |
| Public Polygon balances, two addresses | 2 public Polygon rows |
| Polygon metadata + my Mongo records | 3 Polygon + 2 private Mongo = 5 |
| Two-address Polygon metadata + my Mongo records | 2 Polygon + 2 private Mongo = 4 |

POST a leaf request to `/api/data/query-cql`, or a paired request to
`/api/data/query-cql/federation`. The source predicate has metadataPath
`["PolygonAccountAddress"]`, scalar String, operator In, exact discovered
addresses, orderBy None and includeMetadata true. PreserveCopies keeps provider
provenance; it does not create a cross-provider transactional snapshot.

Use Prepare records before the paired examples. The browser reads the first two
source records, asks WEB4 for avatar-scoped IDs, and creates private Mongo
snapshots through normal create-only saves. It sends no Save to Polygon.
Repeating preparation preserves acknowledged records; uncertain saves retain
original intent keys and are never automatically resent. Other visitors cannot
read your private copies.

## Host setup and boundaries

CQL_POLYGON_PUBLIC_SOURCE_JSON registers the explicit read representation,
separately from canonical Polygon wallet DNA. Query-only permission and a free
query tariff are required independently. Neither grants signed storage writes.
The profile accepts one to eight configured lowercase20-byte addresses, bounded
8KiB/depth4configuration, credential-free root HTTPS RPC, and256KiB/depth16RPC
responses. Unknown/duplicate fields, credential paths, authorization headers and
signer settings are rejected. Unknown catalogue addresses fail before I/O.

Name/description/content scans, sorting, cursor continuation, arbitrary chain
queries, contract execution and owned Polygon writes are unsupported here.
Errors fail the complete selected query without partial pages, endpoint switching
or fallback to latest. Source reads do not certify global failover or replication.

For encrypted Mongo demo snapshots, explicitly grant PolygonAccountAddress in
the trusted AdditionalQueryableKeys policy. The publisher refuses DNA-encrypted
setup without the grant and never weakens encryption or changes wallet DNA.
An authenticated runtime metadata policy must likewise grant the key explicitly.

Publish-HostedPolygonPublicConfiguration.py stages a protected individual
candidate. For a joint Google/Ethereum/Polygon release, use
Publish-HostedGoogleEthereumConfiguration.py with-polygon, which preserves all three profiles,
permissions and all twelve presets. Preparation changes no live settings. Apply
only with the matching tested Core/application revision in the scheduled workflow;
reconcile unknown outcomes before repeating an action.

## Executable verification

[Provider tests](../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests/PolygonPublicAccountMetadataTests.cs)
cover18controlled cases plus real mainnet3/2. The console native test compiles the
published sample files themselves. The
[HTTP pairing test](../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/CqlPolygonMongoNativeTests.cs)
verifies real native3/2, Mongo5/4and actor isolation with controlled bearer/usage
infrastructure. It does not certify production login/billing, replicas or browser
rendering. Reuse the native runner `-PolygonMongoHttp`; preserve its original
intent/ACK lifecycle and never replay an uncertain fixture. Raw logs/TRX belong
in TestResults. The older Ethereum UnitTests target's16stale API compile errors remain documented;
these EVM-family tests reuse the separate EthereumOASIS.CqlTests target.

Protocol references: [EVM JSON-RPC](https://ethereum.org/developers/docs/apis/json-rpc/),
[EIP-1898 block binding](https://eips.ethereum.org/EIPS/eip-1898),
[PublicNode endpoint](https://polygon-bor-rpc.publicnode.com/).

Native balances use Polygon's smallest native unit (the RPC quantity is in wei);
they are not ETH-denominated amounts. The reader is shared with Ethereum, while
the fixed Polygon descriptor pins its own provider/chain/genesis/metadata names.
