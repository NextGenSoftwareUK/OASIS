# Arweave public-header CQL demo

This read-only profile uses shared HolonManager/LINQ planning to select explicit historical Arweave block IDs. It does not use wallet settings, sign transactions or scan the chain. 33 controlled checks, the real native3/2 query and actual shared-manager console3/2 demo pass. Hosted enrollment is a separate gate.

## Create one reusable catalogue

From your OASIS checkout, use PowerShell7. The file contains public IDs and a stable mapping owner, not a wallet or key. Keep it for subsequent runs so mapped holon IDs remain stable.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/ArweaveDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse your existing catalogue; do not replace its source owner.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    restUrl = 'https://arweave.net/'
    network = 'arweave.N.1'
    sourceOwner = [Guid]::NewGuid().ToString()
    blockHashes = @(
        '5H-hJycMS_PnPOpobXu2CNobRlgqmw4yEMQSc5LeBfS7We63l8HjS-Ek3QaxK8ug'
        '1CsR1sm252CmX61pxrXMFOqbh1qTBKcIJH4xTxtxMFqjU8EBIiko2tVik8Z4LUKz'
        '8yrEN8WMkC0RLNX-2ee0aApnf8gsAi2DJ9zvxqHNLB5f0rWnHye0-aZyYxAxKHA7'
    )
}

$source | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath $sourcePath -Encoding utf8NoBOM
```

## Build, then query

Reuse the existing .NET10 SDK and dependencies. The repository build lease bounds producer/consumer work; do not nest it inside another lease.

```powershell
& ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner 'arweave-demo' -Operation {
    dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
        /m:2 /nr:false /p:UseSharedCompilation=false

    if ($LASTEXITCODE -ne 0) { throw 'Demo build failed.' }
}

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --arweave-public LocalState/COSMIC/ArweaveDemo/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/ArweavePublicWalkthrough.cs) queries all three IDs and a real two-ID subset through [shared deferred LINQ setup](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs). Its predicate is:

```csharp
// Choose existing native keys from the configured catalogue.
var selected = keys.Take(2).ToArray();

// The shared planner admits native metadata membership for this source.
var filtered = query.Where(row => selected.Contains(
    CosmicQueryMetadata.Value<string>(row, "ArweaveBlockHash")));
```

`query` is the initialized manager-backed deferred query in NativeKeySetWalkthrough. The console reports three full/two filtered records and exact metadata. It creates no Mongo snapshots or native transactions.

## Contract and limits

- One to eight unique canonical48-byte unpadded base64url block IDs. Native key equality/membership and admitted paging only; arbitrary Name/Description/other metadata filters, numeric ordering, joins and chain scans are unsupported.
- Fixed anonymous HTTPS gateway and per-query `arweave.N.1` network check. Selected hashes must match returned `indep_hash`. Every selected record is validated before paging, including an empty page; no partial results, automatic retries or alternate gateways.
- This explicit profile admits the documented historical format `269510 <= height < 422250`. Other eras are rejected rather than guessed. Heights, timestamps and pool/size integers must be exact nonnegative UInt64 JSON tokens and are exposed as decimal strings. Difficulty fields retain canonical decimal strings up to256digits. No floating-point conversion.
- Transaction IDs must be unique canonical32-byte base64url IDs. Block IDs and parent IDs are canonical48-byte values. Stable mapped IDs include provider, network, source owner and native key.
- Gateway-reported records and linked parents do not independently verify consensus, permanent availability, a shared snapshot or write/replication authority. Source writes are explicitly rejected.
- Cancellation, malformed/oversized responses and HTTP errors return explicit errors. Hosted HTTP federation, visitor preparation/isolation and replica checks must pass separately before the profile is published in the live playground.

[Native HTTP contract and historical formats](https://docs.arweave.org/developers/arweave-node-server/http-api). [Current qualification and evidence](../WIP/COSMIC%20ORM/ARWEAVE_PUBLIC_SOURCE_STATUS.md).
