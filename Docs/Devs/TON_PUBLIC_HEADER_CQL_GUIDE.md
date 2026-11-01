# TON public-header CQL demo

This explicit read-only profile selects historical Mainnet masterchain descriptors through the shared CQL planner and HolonManager/LINQ. No wallet, account, funding or native transaction is required. Native CQL3/2 and the actual shared-manager console3/2 pass on the qualified recent dataset. Hosted native3/2, Mongo-paired5/4, repeat reuse, visitor privacy and SQLite/Neo4j replicas are verified. Browser interaction remains a separate open gate.

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
            seqno = 102765318
            rootHash = 'Y9wDd3pZpST/McLYMsPERk6s7WO6+p19kJ36oNcBOc8='
            fileHash = 'fm6uQx6+r+QPcjvxtYiKq2RYdJ14If7BcskBcBGpFAY='
        }
        @{
            seqno = 102765319
            rootHash = 'gzkgCwmATjDXVh41RK0tjESsu0V+EG0ApXxQe6zsOP4='
            fileHash = '8rGzd/bbQwbuEk76O1OyobR3vZoBnR6GWVbx4AZkLRc='
        }
        @{
            seqno = 102765320
            rootHash = 'Xhj82aH+7pa4qHMEe2NCR2/O0gzJy/nAL/VrYi0cLTU='
            fileHash = 'Y2mAEz86fWZD6YToEZrMbPnTg9aB0ZTRqX+RFwJVjKk='
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
- Logical times remain exact canonical UInt64 decimal strings, including values beyond JavaScript integer precision. Native counters/timestamps and booleans are validated without float conversion. The validator-list hash fragment uses its signedInt32 wire domain and remains an exact signed string in metadata; negative hash fragments are valid.
- Every selected row is validated before paging, including an empty page. Unsupported predicates fail capability planning. Arbitrary Name/Description/other metadata filters, scans, general joins and writes are not admitted by this profile.
- One process-wide owner waits1.1seconds after a completed HTTP attempt before dispatching the next, so connection setup or slow responses cannot consume the admission interval. Cancellation while waiting does not reset that clock. This honors the documented anonymous1RPS policy; it adds no retry or alternate endpoint.
- The operator returned429/500 during qualification. Failures remain explicit; no latency, availability or shared-snapshot guarantee is implied. Successful native3/2 proof does not erase earlier failures or independently verify consensus.
- Hosted federation, visitor-owned Mongo preparation, reuse/privacy/replica checks and browser interaction must pass separately before a live sandbox support grant.

Primary contracts: [TON header API](https://docs.ton.org/api/v2/rpc/get-block-header), [masterchain identity API](https://docs.ton.org/api/v2/rpc/get-masterchain-info), [Mainnet anchors](https://ton.org/global-config.json), [tonlib zero-state conversion](https://github.com/ton-blockchain/ton/blob/master/tonlib/tonlib/TonlibClient.cpp). [Evidence and remaining gates](../WIP/COSMIC%20ORM/TON_PUBLIC_SOURCE_STATUS.md).

The original2021 archive catalogue is retained in the qualification history after an operator Merkle-proof/hash rejection. The recipe above pins the separately qualified recent dataset; it never falls back at runtime or bypasses proof rejection. The source owner was preserved during local promotion, and no native transactions or private copies were written.

Signed hash-fragment domain: [official tonlib API schema](https://github.com/ton-blockchain/ton/blob/master/tl/generate/scheme/tonlib_api.tl).

### Federation budget

The paired TON/Mongo preset must explicitly request `maximumDurationMilliseconds: 30000`. TON serializes public RPCs with a 1.1-second interval after completion; a three-header query includes network validation and three header calls. The normal federation default remains 5000ms. A deadline failure is an error, never partial success or permission to replay saved copies. Both native and paired requests must carry the explicit budget. Local HTTP verification passes native 3/2, paired 5/4 and privacy 0/3 with nine query receipts. Hosted acceptance also passes native3/2, paired5/4, repeat0new/2existing, privacy0/3 and exact SQLite/Neo4j replicas2/2. See the TON status report for release-scoped evidence; browser interaction remains open.

## Live sandbox workflow

In the CQL tab, start a temporary demo session and select TONOASIS. Choose the three-header or two-header preset. For a paired preset, prepare your two private Mongo snapshots through the normal WEB4 save path, then run the query. Repeating setup reuses those same copies; an uncertain save must be reconciled rather than blindly repeated. The chain side stays read-only. Use the configured root-hash keys and the explicit 30000ms budget; arbitrary Name/Description scans are not supported by this profile.
