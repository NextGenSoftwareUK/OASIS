# Telos EVM public-account CQL demo

This read-only profile queries Telos EVM mainnet, chain40, through the shared
HolonManager/CQL/LINQ pipeline. The native C# walkthrough and actual console
return three configured addresses and a two-address subset. No Telos account,
wallet, funding, node or source transaction is needed.

Local Telos HTTP/Mongo passes native3/2, paired5/4 and visitor privacy0/3. Hosted activation/normal-visitor/replica and browser checks remain pending.
[Current evidence and next step](../WIP/COSMIC%20ORM/TELOS_PUBLIC_SOURCE_STATUS.md).

## Create one reusable catalogue

From an initialized OASIS checkout, create this file once. Keep its owner and
addresses between runs so mapped holon IDs remain stable.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/TelosDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse the existing Telos catalogue.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    rpcUrl = 'https://rpc.telos.net/'
    chainId = '0x28'
    addresses = @(
        '0x0000000000000000000000000000000000000001'
        '0x0000000000000000000000000000000000000002'
        '0x0000000000000000000000000000000000000003'
    )
    sourceOwner = [Guid]::NewGuid().ToString()
}

$source | ConvertTo-Json -Depth 4 | Set-Content $sourcePath -Encoding utf8
```

The reader permits one to eight unique lowercase20byte addresses. This walkthrough
requires three. Balances may be zero; these are public read fixtures.

## Build once and run

Reuse the existing .NET10 SDK/dependencies. Agents must use the established provider
build lease for builds in their assigned worktree.

```powershell
dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    /m:2 /nr:false /p:UseSharedCompilation=false

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --telos-public LocalState/COSMIC/TelosDemo/source.json
```

[TelosPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/TelosPublicWalkthrough.cs)
loads the catalogue, activates the explicit read profile and builds a V2 manager.
[NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs)
shows the shared scoped membership predicate and deferred LINQ execution.

The predicate uses `keys.Contains(CosmicQueryMetadata.Value<string>(row,
"TelosAccountAddress"))`. Scope explicitly names `ProviderType.TelosOASIS`.
The full and two-key selections return3/2. Unsupported predicates fail explicitly;
there is no account scan or implicit provider fallback.

## HTTP shape for an explicitly enrolled host

This request uses the same bounded selection as the native walkthrough. Local Telos
HTTP admission/execution passes; hosted configuration is not active yet.
Send `POST /api/data/query-cql` with your normal authenticated session:

```json
{
  "provider": "TelosOASIS",
  "orderBy": "None",
  "take": 25,
  "includeMetadata": true,
  "maximumDurationMilliseconds": 30000,
  "filters": [
    {
      "metadataPath": ["TelosAccountAddress"],
      "scalar": "String",
      "operator": "In",
      "value": [
        "0x0000000000000000000000000000000000000001",
        "0x0000000000000000000000000000000000000002"
      ]
    }
  ]
}
```

A paired request uses `POST /api/data/query-cql/federation`, named providers
`["MongoDBOASIS", "TelosOASIS"]`, `identityMode: "PreserveCopies"`,
`maximumProviderAttempts: 2`, and the same selection as `source`. Private Mongo
demo records retain identity/name/description and mapping metadata; native
balances/block data remain at the source. Full payload mapping is separate work.

## Identity, precision and consistency

Each query verifies chain40 and the pinned canonical genesis hash, requests an
RPC-reported finalized header, and binds every selected balance to that exact hash
with EIP1898 `requireCanonical:true`. Failure rejects the complete result.
Finality/consensus is not independently verified, and separate queries/providers
do not share a snapshot. The current endpoint is `rpc.telos.net`; old `/evm`
endpoints are legacy. Legacy provider code is unchanged.

`TelosBalanceWei` contains an exact decimal string in native TLOS base units,
not ETH or a currency conversion. Block numbers also remain strings. Do not parse
these values as floating-point numbers.

Supported predicates are equality or bounded membership on `TelosAccountAddress`,
with captured paging and access control. This profile is separate from native
Antelope account names, EOSIO contract storage, native writes, storage migrations
and full ORM parity. Future generic Mongo demo preparation must be labelled as
identity/name/description copying until full payload mapping is implemented.

[Official network/current endpoint](https://docs.telos.net/build/network-info/)
and [legacy endpoint guidance](https://docs.telos.net/evm/smart-contracts/hardhat_sourcify/).
