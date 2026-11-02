# Celestia public-header CQL guide

This read-only Celestia profile exposes an explicit mainnet extended-header
catalogue through COSMIC ORM. It does not submit blobs or use the legacy node
wallet configuration. Local source tests and the executable C# walkthrough pass;
Local HTTP native3/2+Mongo5/4/privacy0/3 and query-only admission pass;
hosted enrollment and the live sandbox card have separate pending gates.

## Run the existing C# demo

From the assigned checkout, reuse the retained source configuration and owner:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --celestia-public LocalState/COSMIC/HostedCelestiaPublicSourceCurrent/source.json
```

The output first shows three configured headers, then a real two-height filtered
selection, followed by reported block/data hashes and square widths. The
[commented walkthrough](../../Examples/CosmicCqlDemo/CelestiaPublicWalkthrough.cs)
uses the shared [deferred query example](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs)
through HolonManager. It creates no blob, transaction, wallet, source write or
Mongo copy. Do not rerun consumed qualification scripts merely to get green.

## Query in C#

The source implements the shared `ICosmicQueryStorageProvider` contract. Use
explicit Celestia provider scope and its advertised query capabilities:

```csharp
// Reuse the stable catalogue and projection owner; no wallet is required.
using var source = CelestiaPublicHeaderMetadataSource.FromJson(configurationJson);

var activated = await source.ActivateQuerySourceAsync(cancellationToken);
if (activated.IsError)
    throw new InvalidOperationException(activated.Message, activated.Exception);

// These are exact text keys. Do not convert uint64 heights to floating point.
var selectedHeights = new[] { "15521793", "15521794" };
var scope = CosmicQueryScope.Named(ProviderType.CelestiaOASIS).Result;

var query = CosmicQuery<Holon>.Create(scope)
    .Where(header => selectedHeights.Contains(
        CosmicQueryMetadata.Value<string>(header, "CelestiaHeight")))
    .Result;

var plan = CosmicQueryPlanner.Plan(
    query,
    ProviderType.CelestiaOASIS,
    new[] { new CosmicQueryTarget(ProviderType.CelestiaOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var result = await source.ExecuteCosmicQueryAsync(
    plan.Result,
    maximumResults: 2,
    cancellationToken: cancellationToken);

if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);
```

For the manager/deferred-LINQ form, follow the executable walkthrough above.
Equality, membership of at most eight configured heights, and Skip/Take paging
are admitted. Generic Name/Description predicates, ranges, aggregate terminals,
arbitrary header scans and source writes are not supported by this profile.
Every selected record is validated before paging, including an empty requested
page; a late error never returns partial rows. Unknown heights fail before RPC.

## Explicit source configuration

The exact five-field schema is `schema:1`, `rpcUrl`, `chainId:"celestia"`,
`headers` and `sourceOwner`. `headers` maps one to eight canonical positive uint64
height strings to their captured uppercase 32-byte node-reported commit block IDs.
Use a stable nonempty source-owner GUID. Unknown/duplicate fields, invalid pins,
noncanonical heights, credentials in the URL and non-HTTPS/non-root URLs fail.
The factory uses the production native connection guard, disables cookies and
sets a 15-second request timeout. Never add tokens or wallet keys to this schema.

The existing qualification retains heights15521793/15521794/15521795 and their
original reported IDs. Keep its source owner and pins unchanged. Fresh deployments
must deliberately qualify and retain their own bounded catalogue; do not silently
substitute later heights or another endpoint when a header becomes unavailable.

Each query has a30-second aggregate deadline,1MiB per response,4MiB aggregate
response budget and bounded JSON depth. The RPC response ID/version, chain,
header/commit height, configured reported block ID, timestamp and extended-square
root dimensions/encodings are checked. Integers and timestamps retain native text.

## Metadata and consistency

| Key | Meaning |
|---|---|
| `CelestiaHeight` | Exact configured height; query key |
| `CelestiaChainId` | `celestia` |
| `CelestiaBlockHash` | Pinned node-reported commit block ID |
| `CelestiaDataHash` | Reported header data hash |
| `CelestiaPreviousBlockHash` | Reported previous-block identity |
| `CelestiaTimestamp` | Native UTC timestamp, retained verbatim |
| `CelestiaExtendedSquareWidth` | Reported row/column root count |
| `CelestiaObservationPolicy` | `node-reported-pinned-header` |
| `CelestiaConsensusVerified` | `false` |
| `CelestiaDataAvailabilitySampled` | `false` |

Stable owner-scoped projected holon IDs retain provider/source/key provenance.
Rows are public, active and flat, with empty children. The original header and
validator-set payloads are not represented as private Mongo snapshots here.
Reported hash pinning is not header-hash recomputation, trusted validator-signature
verification, independent consensus, blob inclusion or data-availability sampling.
The public development endpoint has no production SLA.

[Official network details and public endpoint](https://docs.celestia.org/operate/networks/mainnet-beta/)
· [GetByHeight contract](https://www.quicknode.com/docs/celestia/header-getByHeight)
· [NetworkHead contract](https://www.quicknode.com/docs/celestia/header-networkHead)
· [Current evidence and next HTTP gates](../WIP/COSMIC%20ORM/CELESTIA_PUBLIC_SOURCE_STATUS.md).
