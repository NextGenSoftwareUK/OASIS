# ENS public-name CQL demo

This read-only profile resolves an explicit name catalogue through the Ethereum
mainnet Universal Resolver. It uses the same HolonManager planner and deferred
LINQ execution as the other CQL adapters. It does not enumerate ENS registrations.

## Run the C# example

Use `Examples/CosmicCqlDemo/EnsPublicWalkthrough.cs`. It activates the configured
source, queries three names, then selects the first two. The configuration has
exactly these fields:

```json
{
  "schema": 1,
  "rpcUrl": "https://ethereum-rpc.publicnode.com/",
  "names": ["nick.eth", "vitalik.eth", "ur.integration-tests.eth"],
  "sourceOwner": "<your existing projection-owner GUID>"
}
```

Keep the owner/configuration stable across runs. The development proof uses its
retained configuration under `LocalState/COSMIC/HostedEnsPublicSourceCurrent/`;
do not replace a completed qualification or acceptance identity to reproduce green.

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- `
  --ens-public LocalState/COSMIC/HostedEnsPublicSourceCurrent/source.json
```

The projection owner identifies the OASIS source projection; it does not assert
ownership of an ENS name or of the resolved wallet.

## Query shape

The walkthrough calls the shared `NativeKeySetWalkthrough`, which executes the
following pattern. Check each `OASISResult` before reading its `Result`:

```csharp
var names = new[] { "nick.eth", "vitalik.eth" };

var scopeResult = CosmicQueryScope.Named(ProviderType.ENSOASIS);
// Stop and surface scopeResult if IsError is true.

var queryResult = CosmicQuery<Holon>.Create(scopeResult.Result)
    .Where(row => names.Contains(
        CosmicQueryMetadata.Value<string>(row, "EnsName")));
// Stop and surface queryResult if IsError is true.

var deferredResult = CosmicLinq.Create(
    manager,
    source,
    queryResult.Result,
    maximumResults: 40);
// Stop and surface deferredResult if IsError is true.

var rowsResult = await deferredResult.Result
    .ToCosmicListAsync(cancellationToken);
// Handle rowsResult.IsError before using rowsResult.Result.
```

`EnsName` equality and membership select one to eight configured names. Captured
offset/limit paging follows complete native validation. Unknown keys and
unsupported predicates fail; there is no scan or legacy-subgraph fallback.
Name/description content predicates, joins and arbitrary metadata predicates are
not supported by this profile.

## What the returned holons mean

Each public holon contains the name, ETH address, resolver, Universal Resolver,
response hash and block hash/number. All records in one query use one endpoint-
reported finalized block with EIP-1898 `requireCanonical`. Separate queries may
observe different blocks. IDs include source owner, RPC endpoint and name.

Only pre-normalized lowercase ASCII labels in the configured catalogue are
accepted. This is not general ENSIP-15 normalization. CCIP/offchain resolution,
general Unicode, multicoin/text records, reverse resolution, name ownership,
registration writes and history are separate unsupported scopes. Native errors
fail the whole selection, including a page that would otherwise be empty.

The profile checks chain1 and proxy code. This is not an independent Ethereum
consensus verification. The official `ur.integration-tests.eth` sentinel must
resolve to `0x2222222222222222222222222222222222222222` when selected.

Local HTTP/Mongo federation passed3/2native and5/4paired records; the foreign
avatar saw0private/3public. The owned test database was cleaned. This controlled
fixture does not prove live signup/billing or replication. Hosted/private-copy
reuse/replica checks and the live website have separate acceptance gates. Consult the maintained
[ENS status](../WIP/COSMIC%20ORM/ENS_PUBLIC_SOURCE_STATUS.md) for actual evidence;
this guide alone does not claim those gates have passed.

Protocol references: [Universal Resolver](https://docs.ens.domains/resolvers/universal/),
[ENSv2 readiness](https://docs.ens.domains/web/ensv2-readiness/),
[normalization](https://docs.ens.domains/ensip/15/).
