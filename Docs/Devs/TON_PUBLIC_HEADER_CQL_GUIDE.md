# TON public-header CQL demo

This explicit read-only profile selects historical Mainnet masterchain descriptors through the shared CQL planner and HolonManager/LINQ. No wallet, account, funding or native transaction is required. Native CQL3/2 passes. The console builds, but its current runtime gate is red after an HTTP500 during activation; manager/demo, hosted/API and browser acceptance are not yet granted.

## Create one reusable catalogue

Use PowerShell7 from your OASIS checkout. The owner is a stable mapping identity, not a wallet. Preserve the file and owner between runs.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/TonDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse your existing catalogue; do not replace its owner.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    restUrl = 'https://toncenter.com/api/v2/'
    globalId = -239
    sourceOwner = [Guid]::NewGuid().ToString()
    blocks = @(
        @{
            seqno = 10000000
            rootHash = 'tmBKX+61bZpEgrecLQttjvK0PfMar7UMZUDc3LdlBEo='
            fileHash = '+Sq0deY2rf5ejAhY5ps1DBwxfOVuSDsNO6qrzdE8ALk='
        }
        @{
            seqno = 10000001
            rootHash = 'vnLSqDyLLLS1TELs/YGZkvdhts2Blq+qdA3vLhz/bNg='
            fileHash = 'cFm+eQm/07MOSYWjcTJWl8CcXT79XpsSMBHQ/1QNb/g='
        }
        @{
            seqno = 10000002
            rootHash = 'Y6g3oLh+DDO9ZkeNCxNZTHA0TdpaDwIbHCE0M8B6Nms='
            fileHash = 'GHV0V0YOLN4o8gDHqrGuyX+gtMqkCa8sNvX7r7Ddvzc='
        }
    )
}

$source | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath $sourcePath -Encoding utf8NoBOM
```

Each descriptor pins sequence, root hash and file hash together. The source accepts one to eight unique descriptors; this walkthrough deliberately uses three.

## Build, then query

Reuse your existing .NET10 SDK and dependencies. Do not nest the repository build lease inside another lease.

```powershell
& ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner 'ton-demo' -Operation {
    dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
        /m:2 /nr:false /p:UseSharedCompilation=false

    if ($LASTEXITCODE -ne 0) { throw 'Demo build failed.' }
}

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --ton-public LocalState/COSMIC/TonDemo/source.json
```

[The commented walkthrough](../../Examples/CosmicCqlDemo/TonPublicWalkthrough.cs) runs the full selection, then a two-key subset using [shared deferred LINQ setup](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs):

```csharp
// Choose native root hashes from the explicit descriptor catalogue.
var selected = keys.Take(2).ToArray();

// The shared planner admits bounded native metadata membership.
var filtered = query.Where(row => selected.Contains(
    CosmicQueryMetadata.Value<string>(row, "TonBlockRootHash")));
```

`query` is the initialized manager-backed deferred query in NativeKeySetWalkthrough. The console does not prepare Mongo copies, sign messages or write transactions.

## Exact contract and limits

- Root/file hashes are canonical padded32byte base64. Every response must match the complete requested descriptor, Mainnet global_id-239, masterchain-1/shard-9223372036854775808, predecessor shape and native type.
- Mainnet zero-state root/file anchors are pinned. Tonlib deliberately presents that zero-state identifier with shard0/seqno0; this is checked separately from actual block descriptors, without silently substituting a shard.
- Logical times remain exact canonical UInt64 decimal strings, including values beyond JavaScript integer precision. Native UInt32 integer tokens and booleans are validated without float conversion.
- Every selected row is validated before paging, including an empty page. Unsupported predicates fail capability planning. Arbitrary Name/Description/other metadata filters, scans, general joins and writes are not admitted by this profile.
- One process-wide owner waits1.1seconds after a completed HTTP attempt before dispatching the next, so connection setup or slow responses cannot consume the admission interval. Cancellation while waiting does not reset that clock. This honors the documented anonymous1RPS policy; it adds no retry or alternate endpoint.
- The operator returned429/500 during qualification. Failures remain explicit; no latency, availability or shared-snapshot guarantee is implied. Successful native3/2 proof does not erase earlier failures or independently verify consensus.
- Hosted federation, visitor-owned Mongo preparation, reuse/privacy/replica checks and browser interaction must pass separately before a live sandbox support grant.

Primary contracts: [TON header API](https://docs.ton.org/api/v2/rpc/get-block-header), [masterchain identity API](https://docs.ton.org/api/v2/rpc/get-masterchain-info), [Mainnet anchors](https://ton.org/global-config.json), [tonlib zero-state conversion](https://github.com/ton-blockchain/ton/blob/master/tonlib/tonlib/TonlibClient.cpp). [Evidence and remaining gates](../WIP/COSMIC%20ORM/TON_PUBLIC_SOURCE_STATUS.md).
