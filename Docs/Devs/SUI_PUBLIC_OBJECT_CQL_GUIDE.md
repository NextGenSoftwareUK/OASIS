# Sui public object CQL guide

This read-only profile queries an explicit public Mainnet object catalogue through the shared CQL planner and HolonManager. No wallet, signing key, Sui runtime or transaction is needed. Native tests, the C# CLI, API admission and local HTTP/Mongo federation are verified; hosted sandbox support is still pending. See the [acceptance report](../WIP/COSMIC%20ORM/SUI_PUBLIC_SOURCE_STATUS.md).

## First setup

Reuse `LocalState/COSMIC/HostedSuiPublicSourceCurrent/source.json` if present. Otherwise create that file with this complete public configuration:

```json
{
  "schema": 1,
  "graphqlUrl": "https://graphql.mainnet.sui.io/graphql",
  "genesisDigest": "4btiuiMPvEENsttpZC7CZ53DruC3MAgfznDbASZ7DR6S",
  "consistency": "PerQueryCheckpoint",
  "objects": [
    "0x0000000000000000000000000000000000000000000000000000000000000005",
    "0x0000000000000000000000000000000000000000000000000000000000000006",
    "0x0000000000000000000000000000000000000000000000000000000000000007"
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

`sourceOwner` is a stable public projection namespace, not a credential, wallet or avatar ownership claim. Choose one nonempty GUID once for your application and retain it between runs. Do not regenerate it on each launch.

## Run the C# walkthrough

Build the existing demo project with bounded workers:

```powershell
dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    /m:2 /nr:false /p:UseSharedCompilation=false

dotnet run `
    --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build `
    --no-restore `
    -- --sui-public LocalState/COSMIC/HostedSuiPublicSourceCurrent/source.json
```

The [commented C# walkthrough](../../Examples/CosmicCqlDemo/SuiPublicWalkthrough.cs) reads three objects, then a genuine filtered two-object subset. It prints exact object versions and the selected checkpoint. It does not save Mongo snapshots or mutate the chain.

## Express the query

```csharp
// Select only objects enrolled in the configured public catalogue.
var selected = configuredObjects.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.SuiOASIS).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "SuiObjectId")))
    .Result;

// Execute through HolonManager with the activated explicit source and
// caller read access; NativeKeySetWalkthrough.RunAsync shows the full call.
```

String equality and bounded membership on `SuiObjectId` are supported for one to eight enrolled objects. Addresses must be lowercase `0x` plus 64 hex characters. The result window is applied after validating the complete native page. Arbitrary scans, sorting, Name/Description filters, other metadata predicates and signed storage are separate capabilities.

## Checkpoint and precision semantics

One parameterized GraphQL query requests the Mainnet chain identifier, genesis digest, and all selected objects beneath one explicit checkpoint query scope. Later queries can select newer checkpoints. GraphQL uses an HTTP POST containing a read-only `query`; no mutation operation is submitted.

Metadata includes `SuiObjectId`, `SuiObjectVersion`, `SuiObjectDigest`, `SuiCheckpointSequence`, `SuiCheckpointDigest`, `SuiGenesisDigest` and `SuiConsistency`. GraphQL UInt53 values are validated within their exact range and retained as decimal strings. Full genesis identity is checked against the configured Mainnet digest.

Missing/null objects, partial GraphQL errors, unexpected identities, malformed digest encodings, imprecise numbers, oversized responses and cancellation reject the complete result. No partial page, unscoped retry or endpoint fallback is used. Digest encoding validation does not independently prove object BCS content, state proofs or consensus. A future Mongo snapshot would capture an observation rather than establish a cross-provider historical snapshot.

## Sandbox status

Sui currently has verified native C#/CLI and local HTTP/Mongo demos. Do not advertise a working Sui sandbox preset until hosted acceptance and publication gates pass. Browser interaction is verified separately. This explicit query profile stays outside the signed wallet provider registry and cannot become a default write or replication target.
