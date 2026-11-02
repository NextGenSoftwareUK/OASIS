# Filecoin public-account CQL demo

This read-only profile uses Filecoin mainnet chain314 and the shared HolonManager/CQL/LINQ pipeline. It reads FEVM balances for an explicit public address catalogue. No wallet, gas, signing key, source transaction or local node is needed.

The published C# walkthrough returns three records and a real two-address subset. Filecoin HTTP/Mongo, hosted presets and browser acceptance are still pending; see [current evidence](../WIP/COSMIC%20ORM/FILECOIN_PUBLIC_SOURCE_STATUS.md).

## Create one reusable catalogue

From your initialized OASIS checkout, create this file once. Keep its sourceOwner and addresses between runs so mapped holon IDs remain stable. Do not replace an existing catalogue.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/FilecoinDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse the existing Filecoin catalogue.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    rpcUrl = 'https://api.node.glif.io/rpc/v1'
    chainId = '0x13a'
    addresses = @(
        '0x0000000000000000000000000000000000000001'
        '0x0000000000000000000000000000000000000002'
        '0x0000000000000000000000000000000000000003'
    )
    sourceOwner = [Guid]::NewGuid().ToString()
}

$source | ConvertTo-Json -Depth 4 | Set-Content $sourcePath -Encoding utf8
```

These addresses may have zero balances. The demonstration is native filtered retrieval and exact values, not a funded-wallet example. The profile permits one to eight unique lowercase20byte addresses; this walkthrough requires three.

## Build once and run

Use the existing .NET10 SDK and project dependencies. In the agent worktree, wrap builds with the established provider build lease.

```powershell
dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    /m:2 /nr:false /p:UseSharedCompilation=false

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --filecoin-public LocalState/COSMIC/FilecoinDemo/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/FilecoinPublicWalkthrough.cs) loads that catalogue, activates the explicit read source, constructs a V2 manager, and runs full and subset membership queries. The [shared query sample](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs) shows the scope, predicate and deferred execution:

```csharp
// Scope names Filecoin explicitly. No provider scan or implicit fallback.
var scope = CosmicQueryScope.Named(ProviderType.FilecoinOASIS).Result;

// Contains plans bounded native membership on the configured address key.
var selection = CosmicQuery<Holon>.Create(scope)
    .Where(row => keys.Contains(
        CosmicQueryMetadata.Value<string>(row, "FilecoinAccountAddress")))
    .Result;

// The activated adapter executes through HolonManager and the shared planner.
var query = CosmicLinq.Create(manager, adapter, selection, maximumResults: 40).Result;
var rows = (await query.ToCosmicListAsync(cancellationToken)).Result;
```

The executable walkthrough checks OASISResult errors; apply that same error handling when integrating the shorter extract above.

## Identity, precision and consistency

Every read verifies chain314 and the exact native genesis tipset CID at height0. Filecoin identity uses `Filecoin.ChainGetGenesis` directly. It never tries that method as a fallback after an Ethereum genesis request fails. Existing EVM profiles retain their original genesis-hash contract.

Each selection obtains one RPC-reported finalized block and binds every balance to its hash with EIP1898 `requireCanonical:true`. Filecoin `/rpc/v1` finalized semantics use the Lotus EC policy; this does not independently verify F3 finality or consensus. Separate queries/providers do not share a snapshot.

Balances are exact decimal strings in attoFIL, including values beyond JavaScript's integer range. The inherited FEVM metadata key is `FilecoinBalanceWei`; here it represents attoFIL, not ETH. Block numbers also remain strings. Do not parse either as a floating-point number.

Supported queries are address equality or bounded membership on `FilecoinAccountAddress`, with captured paging and access control. Unsupported scans, Name/Description searches, aggregates and native writes fail explicitly. Public balance projections do not implement storage-deal queries, CID content retrieval or general ORM parity.

Primary references: [Filecoin Ethereum API](https://docs.filecoin.io/reference/json-rpc/eth), [native genesis method](https://docs.filecoin.io/reference/json-rpc/chain), [Lotus finality semantics](https://github.com/filecoin-project/lotus/blob/master/CHANGELOG.md).
