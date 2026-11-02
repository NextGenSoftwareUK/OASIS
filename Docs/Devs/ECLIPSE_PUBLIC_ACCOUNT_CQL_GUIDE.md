# Eclipse account CQL demo

Query a configured catalogue of Eclipse accounts through HolonManager and
deferred LINQ. This profile reads metadata from the public mainnet RPC; it does
not download account data, create wallets or submit transactions.

## Run the spaced C# walkthrough

Read `Examples/CosmicCqlDemo/EclipsePublicWalkthrough.cs`. It activates one
explicit source, reads three configured accounts, then selects the first two.
The query implementation is shared with the other native-key CQL demos in
`NativeKeySetWalkthrough.cs`.

Use a retained configuration with exactly these fields:

```json
{
  "schema": 1,
  "rpcUrl": "https://mainnetbeta-rpc.eclipse.xyz/",
  "addresses": [
    "11111111111111111111111111111111",
    "SysvarC1ock11111111111111111111111111111111",
    "SysvarRent111111111111111111111111111111111"
  ],
  "genesisHash": "EAQLJCV2mh23BsK2P9oYpV5CHVLDNHTxYss3URrNmg3s",
  "sourceOwner": "<your retained projection-owner GUID>"
}
```

The genesis value above was reported during the original qualification and is
checked on every subsequent read. This pin detects a changed network response;
it is not an independent consensus verification. Keep the same source owner and
configuration. The owner identifies an OASIS projection, not the owner of a wallet.

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
  --eclipse-public LocalState/COSMIC/HostedEclipsePublicSourceCurrent/source.json
```

The retained development qualification/acceptance folders must not be regenerated
or replayed to reproduce green. The walkthrough is read-only; hosted demo-copy
saves and their identities have separate journals and acceptance gates.

## Supported query

Select the Eclipse provider explicitly. Capture the chosen keys, then use the
native membership expression below inside a `CosmicQuery<Holon>`:

```csharp
var selectedAddresses = new[]
{
    "11111111111111111111111111111111",
    "SysvarC1ock11111111111111111111111111111111"
};

// Create the query with CosmicQueryScope.Named(ProviderType.EclipseOASIS).
// Check each OASISResult before using Result; the complete runnable example
// uses the shared walkthrough's Require helper to surface real errors.
var filtered = query.Where(row => selectedAddresses.Contains(
    CosmicQueryMetadata.Value<string>(row, "EclipseAccountAddress")));

// Pass the successful query to CosmicLinq.Create(manager, source, ...).
// Native reads happen when ToCosmicListAsync(cancellationToken) executes.
```

`EclipseAccountAddress` equality and membership support one to eight configured
canonical base58 keys. Offset/limit paging follows complete validation of the
selection. Unknown keys, unsupported filters and malformed native records fail
with errors; there is no scan or legacy adapter fallback. Arbitrary name,
description and metadata predicates are not supported by this profile.

## Interpret the results

Each public holon includes address, native owner, executable flag, genesis,
finalized slot, balance units, rent epoch and account space. Integer values remain
exact text, including UInt64 maximum values. One batch shares a finalized context;
separate queries can observe different slots.

Balances use **ETH with 9 decimals**. A balance of `1000000000` native units means
1 ETH on Eclipse. Do not label the units SOL or apply Ethereum's 18-decimal wei
conversion. `EclipseBalanceCurrency` and `EclipseBalanceDecimals` make this explicit.

The public endpoint is documented as limited to 3 requests/second. This reader
serializes requests across its process at one request/second; cancellation and
the aggregate deadline also cover waiting for admission. It has no retry loop or
alternate endpoint. This is not a distributed rate limiter or an uptime guarantee.

Account data is excluded with `dataSlice(offset:0,length:0)`. An absent configured
account fails the whole selection rather than becoming a successful zero-balance
record. Reads do not prove Ethereum settlement, Celestia data availability,
independent consensus or wallet ownership.

See [current Eclipse evidence](../WIP/COSMIC%20ORM/ECLIPSE_PUBLIC_SOURCE_STATUS.md)
for actual C#/HTTP/Mongo/hosted/browser completion. A native protocol proof alone
does not establish those later gates.

References: [RPC limits](https://docs.eclipse.xyz/developers/rpc-and-block-explorers),
[Eclipse currency differences](https://docs.eclipse.xyz/developers/differences-between-eclipse-and-solana.md),
[account RPC shape](https://solana.com/docs/rpc/http/getmultipleaccounts).
