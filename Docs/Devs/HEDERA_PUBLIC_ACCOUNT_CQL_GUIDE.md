# Hedera Mainnet mirror account CQL guide

This explicit read-only profile uses the existing `HashgraphOASIS` provider type and the shared COSMIC planner/HolonManager. It queries a bounded public account catalogue without an SDK payment, signing key, account creation or transaction. See the [acceptance report](../WIP/COSMIC%20ORM/HEDERA_PUBLIC_SOURCE_STATUS.md) for local versus hosted gates.

## Run the C# demo

Reuse `LocalState/COSMIC/HostedHederaPublicSourceCurrent/source.json`. Keep its stable sourceOwner and account catalogue. After restoring/building the demo project:

```powershell
dotnet run `
    --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build `
    --no-restore `
    -- --hedera-public LocalState/COSMIC/HostedHederaPublicSourceCurrent/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/HederaPublicWalkthrough.cs) reads three configured accounts and a two-account subset. It prints exact tinybars and each returned balance timestamp. No on-chain or Mongo records are written.

## Express a selection

```csharp
// Capture native numeric account IDs from the explicit catalogue.
var selected = configuredAccounts.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.HashgraphOASIS).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "HederaAccountId")))
    .Result;

// NativeKeySetWalkthrough.RunAsync demonstrates HolonManager execution
// with this activated read adapter and the caller's read access.
```

Supported predicates are string equality and bounded membership on `HederaAccountId` for one to eight configured numeric identities. Result windows operate on the complete selected account page. Arbitrary chain scans, transaction history, aliases, sorting, Name/Description or other metadata predicates and signed storage are separate capabilities.

## Configuration and meaning

The exact schema is `schema: 1`, `mirrorUrl: "https://mainnet.mirrornode.hedera.com/api/v1/"`, `network: "mainnet"`, `consistency: "LatestIndexed"`, `accounts`, and stable nonempty `sourceOwner`. Unknown fields, signing/history options and other-network endpoints are rejected. Account responses do not provide a network ID; Mainnet is bound by the explicit official endpoint, not by an invented ledger certificate.

The profile validates canonical numeric account IDs, response identity, explicit non-deleted state, nonnegative Int64 tinybar balance and an exact seconds/nine-digit-nanoseconds timestamp. Tinybars are exposed as decimal strings. Each account request uses `limit=1` to bound incidental transaction history; that history and its pagination links are not query output.

Metadata includes `HederaAccountId`, `HederaBalanceTinybars`, `HederaBalanceTimestamp`, `HederaNetwork` and `HederaConsistency`. A balance timestamp can be much older than the request and different from another row's timestamp. The profile promises latest indexed observations, with no freshness SLA or shared/historical snapshot. A Mongo copy is a captured observation and need not match a later native read.

Mirror indexing is not independent state/consensus/signature verification or proof that the caller owns the native account. A failed selected account rejects the complete page, even if a result window would otherwise hide it. [Official Mainnet REST reference](https://mainnet.mirrornode.hedera.com/api/v1/docs/).
