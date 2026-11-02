# ThreeFold public node queries with CQL

This read-only profile makes public ThreeFold grid node summaries available to
the shared HolonManager and CQL planner. It is separate from the provider's
MinIO storage implementation and does not provision compute or create contracts.

## Configure the source

Retain one source configuration and projection owner so projected holon IDs
remain stable. The accepted local configuration lives outside Git at
`LocalState/COSMIC/HostedThreeFoldPublicSourceCurrent/source.json`:

```json
{
  "schema": 1,
  "apiUrl": "https://gridproxy.grid.tf/",
  "nodeIds": ["1", "2", "3"],
  "sourceOwner": "YOUR-PERSISTENT-PROJECTION-OWNER-GUID"
}
```

Replace the illustrative owner with a nonempty GUID. Only the explicit mainnet
Grid Proxy root is accepted. Node IDs must be unique, canonical nonzero UInt32
decimal strings; at most eight are configured. No account or wallet is needed.

## Run the C# walkthrough

Build `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj` with the normal bounded build
workflow, then run:

```powershell
dotnet Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --threefold-public `
    LocalState/COSMIC/HostedThreeFoldPublicSourceCurrent/source.json
```

`ThreeFoldPublicWalkthrough.cs` explains configuration and source activation.
`NativeKeySetWalkthrough.cs` contains the shared manager, read-access and LINQ
execution flow. The original compiled walkthrough returned three nodes followed
by the two-node subset. It did not provision resources or write native data.

The selection uses the provider's advertised metadata key:

```csharp
// Explicit scope prevents an accidental query against another provider.
var scope = CosmicQueryScope.Named(ProviderType.ThreeFoldOASIS);
if (scope.IsError)
    throw new InvalidOperationException(scope.Message);

// Membership is composed from bounded native point reads, not a server IN API.
var nodeIds = new[] { "1", "2", "3" };
var selection = CosmicQuery<Holon>.Create(scope.Result)
    .Where(node => nodeIds.Contains(
        CosmicQueryMetadata.Value<string>(node, "ThreeFoldNodeId")));

if (selection.IsError)
    throw new InvalidOperationException(selection.Message);
```

Use the activated source and HolonManager with `CosmicLinq.Create`, apply caller
read access before planning, and inspect every `OASISResult` before using its
result. The runnable walkthrough shows the complete sequence.

## Supported data and limits

`ThreeFoldNodeId` supports equality, bounded membership and paging. Projection
metadata includes node ID, farm ID, grid version, country, city and provenance.
Integer values remain canonical decimal strings. IP addresses, contact details,
resource inventories, prices and contracts are excluded.

The operator reports these summaries. `ThreeFoldChainStateVerified` is false;
this is not independent chain-state, capacity, availability or uptime proof.
Queries read only enrolled IDs through `/nodes/{id}` and do not discover nodes.
Native responses must match each requested ID. Malformed, missing, duplicate,
oversized or failed responses reject the whole query, without partial results.

Requests have a 15-second deadline, 256 KiB response limit and one-second pacing.
There is no implicit retry. Save and delete return `CQL_READ_ONLY_SOURCE` before
network activity or mutation.

## Verification status

The adapter's 33 controlled tests, the shared walkthrough regression and the
original compiled native three/two-record run pass. HTTP admission (three tests),
Core read tariffs (one test) and the original local HTTP/Mongo gate also pass:
three/two native rows, five/four paired rows and foreign-visitor isolation. The
exact owned fixture was removed and the owned Mongo process stopped. Hosted
presets, activation and sandbox acceptance remain separate unfinished gates.
Do not interpret the local walkthrough as hosted or browser verification.

Current evidence and remaining gates are maintained in
`Docs/WIP/COSMIC ORM/THREEFOLD_PUBLIC_SOURCE_STATUS.md`.
