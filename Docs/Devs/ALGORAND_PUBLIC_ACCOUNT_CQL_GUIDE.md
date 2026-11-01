# Algorand public account CQL guide

This read-only profile queries a small explicit catalogue of public Mainnet accounts through the shared COSMIC query planner and HolonManager. It needs no wallet, signing key or paid service. Hosted sandbox enrollment is a separate acceptance gate; consult the [current report](../WIP/COSMIC%20ORM/ALGORAND_PUBLIC_SOURCE_STATUS.md).

## Run the C# demo

Use the retained `LocalState/COSMIC/HostedAlgorandPublicSourceCurrent/source.json` catalogue. Do not recreate its sourceOwner between runs. After restoring and building `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj`, run:

```powershell
dotnet run `
    --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build `
    --no-restore `
    -- --algorand-public LocalState/COSMIC/HostedAlgorandPublicSourceCurrent/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/AlgorandPublicWalkthrough.cs) activates the explicit source, queries three configured accounts, then a two-account subset. It prints exact MicroAlgo amounts and each account's observed round. It creates no on-chain or Mongo records.

## Query shape

```csharp
// Capture the selected native account addresses in a local variable.
// They must already belong to the configured public catalogue.
var selected = configuredAccounts.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.AlgorandOASIS).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "AlgorandAddress")))
    .Result;

// Pass this query and the explicit activated read adapter through
// HolonManager, as demonstrated by NativeKeySetWalkthrough.RunAsync.
// No default write provider or wallet registry is changed.
```

Supported predicates are string equality and bounded membership on `AlgorandAddress`, selecting one to eight configured canonical addresses. Result windows are supported within that complete selection. Arbitrary Name/Description filters, other metadata predicates, ordering, scans, historical-round queries and signed writes are not admitted by this profile.

## Configuration and meaning

The exact schema contains `schema: 1`, anonymous HTTPS `algodUrl` and `indexerUrl`, `genesisId: "mainnet-v1.0"`, `genesisHash: "wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8="`, `consistency: "Current"`, one to eight `accounts`, and a nonempty stable `sourceOwner`. Unknown fields, including requested rounds or signing fields, are rejected. The qualification runner retains the real catalogue; no credentials belong in this file.

Both endpoints must report the configured genesis. Addresses use canonical Base32 and the real SHA512/256 checksum. Account identity and account/response rounds must agree. UInt64 amounts are mapped to decimal strings so large values remain exact.

`AlgorandAmountMicroAlgos`, `AlgorandAmountWithoutPendingRewards`, `AlgorandPendingRewards`, `AlgorandRewards`, `AlgorandMinBalance`, `AlgorandStatus`, `AlgorandObservedRound`, `AlgorandGenesisHash`, `AlgorandGenesisId` and `AlgorandConsistency` describe API-reported current state. Rows may have different observed rounds. A Mongo copy is a captured observation; it need not match a later current-state read.

The selected free indexer rejects historical account rewind. The adapter explicitly requests current state and never retries a failed historical request as current. It does not independently verify consensus, state proofs, account ownership or signing authority. Any failed record rejects the complete page, including when a result window would otherwise hide that record.

## Live sandbox

Open the WEB4 sandbox CQL tab, start a temporary session and select AlgorandOASIS. Try the three-account or two-account preset. The Mongo-paired preset prepares two private snapshots; repeated setup reuses them. Hosted HTTP acceptance verifies native3/2, paired5/4, visitor privacy and SQLite/Neo4j replicas. Accounts remain current-state observations with per-row rounds; browser interaction is a separate gate.
