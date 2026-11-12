# MultiversX / Elrond public account CQL guide

This explicit read-only profile queries Mainnet account metadata through CQL's shared planner and HolonManager. The provider type remains `ElrondOASIS`. It uses the public MultiversX indexing API and does not invoke the signed wallet provider. Native CQL, API admission and local HTTP/Mongo federation are verified; hosted HTTP acceptance also passes, with browser interaction still unverified. See [current evidence](../WIP/COSMIC%20ORM/ELROND_PUBLIC_SOURCE_STATUS.md).

## First setup

Reuse `LocalState/COSMIC/HostedElrondPublicSourceCurrent/source.json` when present. Otherwise create it using this complete public example:

```json
{
  "schema": 1,
  "apiUrl": "https://api.multiversx.com/",
  "chainId": "1",
  "consistency": "LatestIndexed",
  "addresses": [
    "erd1qqqqqqqqqqqqqqqpqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqplllst77y4l",
    "erd1rf4hv70arudgzus0ymnnsnc4pml0jkywg2xjvzslg0mz4nn2tg7q7k0t6p",
    "erd18mv2z6r2ksn4rfmm52tmhkc6x5tz6achmynvxftq4ay927029qqqmqpzfw"
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

`sourceOwner` is a stable projection namespace, not a wallet, credential or avatar ownership claim. Choose one nonempty GUID once for your application and retain it. Do not regenerate it on every run. The addresses identify existing public accounts; no account or transaction is created.

## Run the C# walkthrough

Build the existing checkout with its submodules initialized, then run the demo:

```powershell
dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    /m:2 /nr:false /p:UseSharedCompilation=false

dotnet run `
    --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build `
    --no-restore `
    -- --elrond-public LocalState/COSMIC/HostedElrondPublicSourceCurrent/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/ElrondPublicWalkthrough.cs) selects three accounts, then a real two-account subset. It prints exact balances, nonces and shard identifiers. No wallet, signing key, funding or local database is required.

## Express a native selection

```csharp
// Capture one or two accounts from the explicit configured catalogue.
var selected = configuredAccounts.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.ElrondOASIS).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "ElrondAccountAddress")))
    .Result;

// Execute using the activated explicit source and caller read access.
// NativeKeySetWalkthrough.RunAsync shows the full HolonManager/LINQ call.
```

Equality and bounded membership on `ElrondAccountAddress` are supported for one to eight enrolled accounts. Addresses must be canonical lowercase `erd` Bech32 with valid checksum and a 32 byte payload. The native API receives the address predicate and result count; response identities must exactly match the request before a result window is applied. Arbitrary scans, sorting, balance ranges, Name/Description predicates and other metadata operators are separate capabilities.

## Identity, precision and consistency

Each source query checks API-reported Mainnet identity and denomination, then requests the exact address set with explicit fields. `LatestIndexed` does not promise freshness, pinned account state or a snapshot shared across providers. API-reported identity is not independent genesis or consensus verification.

Metadata contains `ElrondAccountAddress`, `ElrondBalanceBaseUnits`, `ElrondNonce`, `ElrondShard`, `ElrondChainId`, `ElrondDenomination` and `ElrondConsistency`. Balances stay canonical decimal strings, including values larger than 64 bits. The 4,096 digit balance limit is a bounded-response contract, not a protocol integer-width claim.

The API emits nonce as a JSON number. This profile rejects values above `2^53 - 1` or noninteger/coerced values, and exposes accepted nonces as decimal strings; it does not claim full UInt64 nonce fidelity through the indexing service. Shards 0/1/2 and metachain 4294967295 are explicit. Token denomination 18 is checked independently of the API's UI display-decimal setting.

Missing, duplicate, unexpected or malformed accounts, imprecise numbers, HTTP failures, duplicate JSON, cancellation and oversized responses fail the complete selection. There is no partial-page publication, endpoint fallback or automatic retry.

## Live sandbox

Open the WEB4 sandbox CQL tab, start a temporary session and select ElrondOASIS. Native presets query three accounts or a two-account subset. Mongo-paired presets prepare two private records and reuse them on repeat setup. Hosted HTTP checks verify native3/2, paired5/4, foreign-private zero and matching SQLite/Neo4j replicas. Browser interaction remains a separate open gate.

Mongo demo copies capture name, description and native account key; they are not full account/state snapshots. Native API observations are latest indexed, with no snapshot shared across providers. No blockchain transaction is submitted. Public account projections do not certify signed storage, conditional writes or replication into MultiversX.
