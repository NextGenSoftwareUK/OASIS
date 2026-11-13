# CQL over public Chainflip historical swaps

`ChainflipPublicSwapMetadataSource` is a separate read-only profile under
`ChainflipOASIS`. It loads a bounded catalogue of completed historical swap
observations through the canonical guarded .NET HTTP transport. It never
requests a quote, opens a deposit address, creates or dispatches a swap.

## Retain one configuration

The qualified demo uses `LocalState/COSMIC/HostedChainflipPublicSourceCurrent/source.json`
with swap IDs500000/500001/500002. Preserve its source owner and catalogue.
The configuration has exactly four fields: `schema` (1), `apiUrl` (anonymous
HTTPS root), `swapIds` (1–8 unique canonical uint64 decimal strings), and
`sourceOwner` (one nonempty retained projection GUID). Unknown/authentication/
signer/billing fields are rejected. Query requests cannot choose another URL.

## Run the commented C# demo

Build `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj` using the existing .NET10SDK
and repository bounded build settings. From the worktree:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --chainflip-public LocalState/COSMIC/HostedChainflipPublicSourceCurrent/source.json
```

`ChainflipPublicWalkthrough.cs` is spaced and commented. It activates the
explicit read profile and runs three-key then two-key membership through the
shared HolonManager planner and deferred LINQ executor. No storage write
provider is registered and no Mongo record is created by the walkthrough.

With your existing `manager`, configured `source`, retained `swapIds`, actor
and cancellation token, the central query pattern is:

```csharp
var scope = CosmicQueryScope.Named(ProviderType.ChainflipOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .WithReadAccess(access)
    .Where(row => swapIds.Contains(
        CosmicQueryMetadata.Value<string>(row, "ChainflipSwapId")))
    .Result;

var linq = CosmicLinq.Create(manager, source, query, maximumResults: 40).Result;
var rows = await linq.ToCosmicListAsync(cancellationToken);

if (rows.IsError)
    throw new InvalidOperationException(rows.Message, rows.Exception);

foreach (var row in rows.Result)
{
    Console.WriteLine(row.MetaData["ChainflipOriginalInputAmount"]);

    if (row.MetaData.TryGetValue("ChainflipDepositAmount", out var deposit))
        Console.WriteLine($"External deposit amount: {deposit}");
    else
        Console.WriteLine("No external deposit is present in this indexed record.");
}
```

Check `OASISResult` at application boundaries; the full executable helper does
so. Equality/membership on `ChainflipSwapId`, Skip and Take are supported.
Complete selection is validated before paging, including an empty requested
page. Unknown keys, unsupported predicates, incomplete records, cancellation
and budget failures publish no partial rows. This profile deliberately requires
native state `COMPLETED`; it is not a pending-swap poller or an arbitrary scan.

## Precision, optional fields and trust

| Metadata | Meaning |
|---|---|
| `ChainflipSwapId` | Canonical configured uint64 ID, exact string/query key |
| `ChainflipSwapState` | Backend-reported `COMPLETED` observation |
| `ChainflipSourceChain`, `ChainflipSourceAsset` | Exact source identifiers |
| `ChainflipDestinationChain`, `ChainflipDestinationAsset` | Exact destination identifiers |
| `ChainflipOriginalInputAmount`, `ChainflipRemainingInputAmount` | Native swap quantities as decimal strings |
| `ChainflipSwappedInputAmount`, `ChainflipSwappedIntermediateAmount`, `ChainflipSwappedOutputAmount` | Distinct reported swap quantities as decimal strings |
| `ChainflipExternalDepositObserved` | Explicit presence flag; not every swap has an external deposit |
| `ChainflipDepositAmount`, `ChainflipDepositTxRef`, `ChainflipDepositWitnessedAtMilliseconds` | Present only for a complete external deposit object |
| `ChainflipRefundObserved`, `ChainflipRefundAmount` | Refund presence; amount only when observed |
| `ChainflipLastStatechainUpdateAtMilliseconds` | Reported millisecond integer retained as a string |
| `ChainflipObservationPolicy` | `indexer-reported-swap-state` |

Amount strings are bounded to78canonical decimal digits for work/precision
safety. They are not converted to floating point or asset display units. The
reported external deposit and net swap input are different quantities and must
not substitute for one another. Absent deposits are not fabricated as zero.

These are per-record indexer observations, not one shared snapshot, independent
consensus/finality or destination-redemption proofs. `COMPLETED` can include a
refund observation. No financial action or source mutation is performed. Public
flat holons have stable owner-scoped projection IDs and empty children; native
writes/deletes are refused.

## Acceptance scope

41source controls and actual HolonManager/deferred-LINQ CLI3/2pass. Native
qualification used the shipping .NET provider; an earlier Python403and the
original optional-deposit assumption failure remain recorded. Do not re-run
consumed qualification intents or replace the projection owner to get green.
HTTP compiler/query-only admission and four prepared presets pass. Local HTTP
returns3/2, Mongo federation5/4 and privacy0/3 with nine receipts. The original
two version1 copies were reused in an explicit read-only completion after the
fixture was corrected to the presets' 30-second budget; no saves were replayed.
Hosted HTTP now passes native3/2, Mongo5/4, repeat setup with zero new copies,
visitor privacy and exact SQLite/Neo4j replicas2/2 each. Two normal API version1
saves are acknowledged, pending intents zero. Code/settings deployment is
c81937d59; the public sandbox card is published with this guide and the commented C#
walkthrough. Browser interaction remains a separate open gate. [Current proof and next step](../WIP/COSMIC%20ORM/CHAINFLIP_PUBLIC_SOURCE_STATUS.md).

Primary contract: [official swap route](https://github.com/chainflip-io/chainflip-sdk-monorepo/blob/main/packages/swap/src/routes/v2/swap.ts).
