# Zcash public block queries with CQL

This read-only profile exposes configured public block summaries through the same
HolonManager and LINQ query path used by other CQL providers. It does not expose
wallets, shielded balances or transactions, and cannot save or delete Zcash data.

## Configure the source

Use an anonymous HTTPS indexer with the qualified zecblock v1 contract. Keep one
configuration and source owner: projection IDs depend on that identity. The source
contains schema version1, apiUrl, genesisHash, sourceOwner and one to eight blocks,
each with an integer height and a lowercase64-character block hash. No secrets
belong in this configuration. Native block hashes and OASIS projection IDs differ.

The local accepted catalogue lives outside Git in
`LocalState/COSMIC/HostedZcashPublicSourceCurrent/source.json`.

## Run the C# walkthrough

After building `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj`, run:

```powershell
dotnet Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --zcash-public LocalState/COSMIC/HostedZcashPublicSourceCurrent/source.json
```

`ZcashPublicWalkthrough.cs` shows activation and manager setup;
`NativeKeySetWalkthrough.cs` shows the shared query. Its essential predicate is:

```csharp
// Freeze the native hashes selected by the caller.
var keys = configuredBlocks.Select(block => block.Hash).ToArray();

// Resolve the explicit Zcash scope and caller access before planning.
var scope = CosmicQueryScope.Named(ProviderType.ZcashOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;

// Contains becomes a bounded native membership query, without a block scan.
var query = CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(row => keys.Contains(
        CosmicQueryMetadata.Value<string>(row, "ZcashBlockHash")));
```

Check every OASISResult before using Result, as the runnable shared walkthrough
does. The example runs a three-hash query followed by a two-hash query.

## Supported execution and limits

- String equality or IN against ZcashBlockHash; at most eight configured hashes.
- Skip/Take and explicit result budget; source errors never become partial success.
- Genesis/mainnet, returned height and configured hash validation before publication.
- Fifteen-second per-request deadline,256KiB response bound and one-request/second
  pacing; no implicit retry, block discovery scan or private payload mapping.
- Metadata includes block hash, height, UInt32 timestamp, genesis, freshness and
  observation policy. Freshness can be unknown or stale; it is not forced to fresh.

The indexer reports the block identity. This profile does not independently verify
Equihash, proof of work, consensus or finality; ZcashProofVerified remains false.

## HTTP and federation

The server profile uses CQL_ZCASH_PUBLIC_SOURCE_JSON and requires independent
query permission and read tariff. It never enables wallet/provider write routing.
Native and Mongo-paired three/two presets are covered by planning tests. Hosted native3/2, Mongo5/4, repeat preparation/privacy and exact SQLite/Neo4j
replicas have passed the original cohort audit. The Zcash card is published in the
CQL sandbox; select ZcashOASIS and a native or Mongo paired preset. Browser
interaction and availability certification remain separate gates: consult
`Docs/WIP/COSMIC ORM/ZCASH_PUBLIC_SOURCE_STATUS.md` for current evidence.

Run offline adapter controls with `Scripts/Sandbox/Test-ZcashPublicMetadata.ps1`.
The original acceptance journals must be retained; do not replay a completed
fixture or reseed private copies to recreate its evidence.
