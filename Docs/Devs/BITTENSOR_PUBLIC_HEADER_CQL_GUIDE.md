# Query Bittensor headers with CQL

This read-only profile queries real Finney block headers. It supports metadata
equality, membership (`Contains` / HTTP `In`) and paging over an explicitly
configured catalogue of one to eight block hashes. It does not query AI outputs,
subnets, miners, account balances or arbitrary chain storage.

The SDK walkthrough has passed a real three-record query followed by a filtered
two-record query through HolonManager and LINQ. HTTP/hosted deployment evidence
is tracked separately in the [current source report](../WIP/COSMIC%20ORM/BITTENSOR_PUBLIC_SOURCE_STATUS.md).

## Configure a public source

Use `BittensorPublicHeaderMetadataSource.FromJson(configurationJson)` from the
Bittensor provider project. The configuration has exactly these fields:

```json
{
  "schema": 1,
  "rpcUrl": "https://bittensor-finney.api.onfinality.io/public",
  "genesisHash": "0x2f0555cc76fc2840a25a6ea3b9637146806f1f44b090c175ffde2a7e5ab36c03",
  "blockHashes": ["<existing canonical lowercase 32-byte block hash>"],
  "sourceOwner": "<persistent nonempty projection-namespace GUID>"
}
```

Replace the placeholders with qualified values. Keep the namespace GUID stable;
it determines the projected holon IDs alongside the provider, genesis and native
block hash. It is an OASIS projection namespace, not ownership of the chain.
No private key or API token belongs in this configuration.

## Run the C# walkthrough

The [commented walkthrough](../../Examples/CosmicCqlDemo/BittensorPublicWalkthrough.cs)
activates the source, runs the shared manager/LINQ query over all three configured
keys, then selects two of them. It prints the holon IDs and native content hashes.
It leaves the default write provider and wallet registry alone.

The core predicate is:

```csharp
// Capture the existing native keys before constructing the query.
var selectedHashes = configuredHashes.Take(2).ToArray();

// Use an explicit Bittensor scope and this source's enrolled capability.
// The shared walkthrough checks each OASISResult before continuing.
var filtered = query.Where(holon => selectedHashes.Contains(
    CosmicQueryMetadata.Value<string>(holon, "BittensorBlockHash")));
```

The complete runnable setup and result checks are in the walkthrough. Unsupported
predicates fail planning; unknown catalogue keys fail before native requests.
This profile does not scan the chain to implement a filter.

The compiled demo also provides this CLI entry point:

```powershell
# Reuse your configured three-header catalogue and existing build.
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --bittensor-public ./LocalState/COSMIC/HostedBittensorPublicSourceCurrent/source.json
```

The live sandbox CQL tab lists `BittensorOASIS` with native and Mongo-paired
presets. Hosted query/privacy/replica evidence is recorded in the source report;
browser interaction is a separate gate.

## Read results accurately

`Name` and `Description` identify the block. Metadata includes its native hash,
block number, parent hash, state root, extrinsics root, genesis, and the node's
reported finalized anchor. Numeric heights are invariant decimal strings.

The adapter recomputes each header's SCALE/BLAKE2b hash and checks that its height
belongs to RPC-reported canonical finalized history. It does not verify a GRANDPA
proof. `BittensorConsensusVerified` remains `false`; the observation policy makes
the node-reported boundary explicit.

Public requests share a one-per-second gate. Responses are limited to 256 KiB
with cancellation and a linked per-request deadline. A failed selected record
fails the complete query; paging, including `Take(0)`, cannot conceal a failure.
Holon saves and deletes return `CQL_READ_ONLY_SOURCE` without native mutation.

## HTTP and Mongo pairing

The server configuration key is `CQL_BITTENSOR_PUBLIC_SOURCE_JSON`. Enabling it
requires separate query permission and an explicit read tariff; it grants no
write admission. A Mongo-paired query preserves physical-copy provenance and
uses the same `BittensorBlockHash` metadata predicate on both targets.

Use the source report for verified deployment and demo availability. A compiled
adapter or local test alone does not establish hosted sandbox availability.
