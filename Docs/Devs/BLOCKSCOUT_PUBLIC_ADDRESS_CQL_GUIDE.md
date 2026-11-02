# Blockscout public-address CQL guide

This read-only profile exposes a bounded address catalogue from an explicitly
configured Blockscout explorer. It uses COSMIC ORM's shared query-provider
contract. It does not use a provider API key, submit transactions or mutate the
explorer. Check the [current proof and remaining gates](../WIP/COSMIC%20ORM/BLOCKSCOUT_PUBLIC_SOURCE_STATUS.md)
for local, hosted and browser verification status.

## Run the spaced C# walkthrough

Reuse the retained catalogue and owner in the assigned checkout:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --blockscout-public LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent/source.json
```

The [commented executable](../../Examples/CosmicCqlDemo/BlockscoutPublicWalkthrough.cs)
queries three configured addresses, then filters to two through HolonManager and
deferred LINQ. It displays the explorer name, native-coin balance and balance
update block when reported. Unknown values stay unknown. It creates no wallet,
account, local node, source write or Mongo copy.

For the retained Ethereum explorer, the balance is the address's native ETH
amount in native units, not its DAI/USDC token balance or token supply. Decimal
amount strings are never converted to floating point or display units.

## Build an explicitly scoped query

```csharp
// Reuse the existing anonymous profile. No API key belongs in this schema.
using var source = BlockscoutPublicAddressMetadataSource.FromJson(configurationJson);

var activated = await source.ActivateQuerySourceAsync(cancellationToken);
if (activated.IsError)
    throw new InvalidOperationException(activated.Message, activated.Exception);

var selectedAddresses = new[]
{
    "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    "0x6b175474e89094c44da98b954eedeac495271d0f"
};

var scope = CosmicQueryScope.Named(ProviderType.BlockscoutOASIS).Result;
var query = CosmicQuery<Holon>.Create(scope)
    .Where(address => selectedAddresses.Contains(
        CosmicQueryMetadata.Value<string>(address, "BlockscoutAddress")))
    .Result;

// Planning checks the source's capabilities before any native request.
var plan = CosmicQueryPlanner.Plan(
    query,
    ProviderType.BlockscoutOASIS,
    new[] { new CosmicQueryTarget(ProviderType.BlockscoutOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var result = await source.ExecuteCosmicQueryAsync(
    plan.Result,
    maximumResults: 2,
    cancellationToken: cancellationToken);

if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);
```

The executable walkthrough shows the manager/deferred-LINQ form. This profile
supports exact configured address equality, membership of up to eight addresses
and Skip/Take paging. Generic name/description searches, arbitrary scans, numeric
aggregates and native writes are not advertised. Every selected record is read
and validated before paging, even for an empty page. Late failure returns no
partial rows. Unknown keys are rejected before native dispatch.

## Explicit profile and limits

The exact four-field JSON schema is `schema:1`, `apiUrl`, `addresses`,
`sourceOwner`. Use one to eight unique canonical lowercase 20-byte `0x` addresses
and a stable nonempty projection-owner GUID. `apiUrl` must be an anonymous HTTPS
URL with exact `/api/v2/` path, no credentials, query or fragment. Unknown and
duplicate configuration fields fail; the source schema has no API-key field.
The qualified existing instance is `https://eth.blockscout.com/api/v2/`.

The factory uses the production native connection guard with cookies disabled
and a15-second request timeout. Queries have a30-second aggregate deadline,
1MiB per response,4MiB aggregate response bound and bounded JSON depth. Request
cancellation is observed. The original qualification and source-owner creation
are consumed operations: retain their config/ACKs and do not replay them merely
to get another green report.

## Metadata and consistency

| Key | Meaning |
|---|---|
| `BlockscoutAddress` | Canonical configured address; query key |
| `BlockscoutReportedAddress` | Returned address spelling, validated against the key |
| `BlockscoutIsContract` | Explorer-reported contract flag |
| `BlockscoutBalanceObserved` | Explicit balance presence; null is false |
| `BlockscoutCoinBalance` | Present only for a reported canonical decimal balance, at most78digits |
| `BlockscoutBalanceBlockObserved` | Explicit reported update-block presence |
| `BlockscoutBalanceUpdatedAtBlock` | Present only for a reported uint64 update height, retained as text |
| `BlockscoutIndexedNameObserved` | Explicit indexed-name presence |
| `BlockscoutIndexedName` | Bounded native name text, present only when reported |
| `BlockscoutObservationPolicy` | `explorer-reported-address-state` |
| `BlockscoutConsensusVerified` | `false` |

Holon Name is the canonical address and Description identifies that explorer
address. Projections are public, active and flat. Provider/source/key provenance
and projected IDs include the explorer endpoint, so the same owner/address at a
different instance cannot alias it. A changed endpoint requires deliberate
qualification; there is no automatic endpoint replacement.

Only the listed public fields are projected. Private tags, watchlist names,
user metadata, exchange rates and nested token payloads are excluded. A valid
but unindexed address can return200with a null balance; that is not proof of
an indexed account or zero funds. Individually reported update blocks can differ
between records and between queries. These observations are not one shared
snapshot, independently verified consensus, finality or a proof of balances.

[Official address controller and unindexed-response semantics](https://github.com/blockscout/blockscout/blob/master/apps/block_scout_web/lib/block_scout_web/controllers/api/v2/address_controller.ex)
· [Official documentation index](https://docs.blockscout.com/llms.txt).
