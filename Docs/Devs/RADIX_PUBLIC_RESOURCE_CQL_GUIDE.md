# Radix public resource CQL demo

This read-only profile queries real Mainnet resource definitions through the shared HolonManager/CQL planner. It uses the public Radix Gateway without an account, wallet or transaction. Hosted sandbox enrollment remains pending.

## Run the C# console demo

From the existing worktree, reuse its retained three-resource configuration:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- `
    --radix-public `
    LocalState/COSMIC/HostedRadixPublicSourceCurrent/source.json
```

The demo prints three public resources, then two selected resources. See [commented walkthrough](../../Examples/CosmicCqlDemo/RadixPublicWalkthrough.cs) and [shared LINQ example](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs). Build first when source has changed; use the repository build lease and worker limits.

## Query shape

The walkthrough uses this shared query shape, with an activated Radix adapter and a HolonManager:

```csharp
// Freeze a bounded set of enrolled native resource addresses.
var keys = configuredResources.Take(2).ToArray();

// Select this source explicitly; the scope is separate from HyperDrive routes.
var scope = Require(CosmicQueryScope.Named(ProviderType.RadixOASIS));
var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

// Membership is passed to the native Gateway entity-details request.
// Require is the example's OASISResult helper; errors must be handled.
var query = Require(CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(row => keys.Contains(
        CosmicQueryMetadata.Value<string>(row, "RadixResourceAddress"))));

var linq = Require(CosmicLinq.Create(manager, source, query, maximumResults: 40));
var rows = Require(await linq.ToCosmicListAsync(cancellationToken));
```

Supported native predicate: equality or bounded membership on `RadixResourceAddress`, restricted to the enrolled catalogue (one to eight addresses). Complete native validation precedes paging. Unsupported predicates return capability errors; this profile does not scan the chain. Broader Mongo queries and federation need their separate enrollment and acceptance gates.

## Configuration and consistency

The exact configuration fields are `schema: 1`, `gatewayUrl: "https://mainnet.radixdlt.com/"`, `networkId: 1`, `consistency: "PerQueryLedgerState"`, `resources` and a stable nonempty `sourceOwner` GUID. Preserve the existing catalogue/namespace across runs. The owner identifies projected public holons, not a Radix account or signing key.

Each query acknowledges a Gateway ledger state and requests all selected resources at that state. The returned version, epoch, round and timestamp must match. Separate queries may select different states. This is Gateway consistency, not independent consensus verification or a shared historical snapshot with Mongo.

## Returned metadata

| Key | Meaning |
|---|---|
| RadixResourceAddress | Canonical Bech32m Mainnet resource address |
| RadixResourceType | FungibleResource or NonFungibleResource |
| RadixTotalSupply | Exact native decimal string, up to 18 fractional digits |
| RadixLedgerStateVersion / RadixEpoch / RadixRound | Exact nonnegative Gateway Int64 values exposed as strings |
| RadixLedgerTimestamp | Original validated UTC timestamp |
| RadixNetwork / RadixConsistency | mainnet / PerQueryLedgerState |
| RadixResourceName / RadixResourceSymbol / RadixResourceDescription | Optional native string metadata; absent stays absent |
| Each optional key + UpdatedAtStateVersion | Native metadata update version |

Supply validation uses signed 192-bit scaled-integer bounds; no double or System.Decimal conversion. Address checksum, resource kind, complete identities, duplicate properties, metadata completeness, byte budgets and cancellation are checked before publication. Generic Holon Name/Description identify the projection; native labels remain separate metadata.

## Evidence and remaining gates

The focused suite passed 50/50 with zero skips, including actual shared-manager native three/two selection. [Current Radix status](../WIP/COSMIC%20ORM/RADIX_PUBLIC_SOURCE_STATUS.md) records evidence hashes and rollout gates. Query-only API admission, local HTTP/Mongo federation, hosted configuration, sandbox card and browser interaction remain separate requirements. This source cannot become a default write or replication provider; signed Radix storage is a different implementation.
