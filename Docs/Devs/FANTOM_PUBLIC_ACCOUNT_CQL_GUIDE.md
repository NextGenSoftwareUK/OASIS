# Fantom Opera CQL: public balances

This read-only profile queries Fantom Opera, chain250 (`0xfa`). It uses the shared CQL planner, HolonManager and deferred LINQ. No wallet, funding, transaction or native source write is needed.

The local native/manager walkthrough and actual console return three records and a two-record subset. WEB4 HTTP/Mongo, hosted sandbox and browser gates remain pending in [the current report](../WIP/COSMIC%20ORM/FANTOM_PUBLIC_SOURCE_STATUS.md).

## What the query guarantees

Fantom's selected RPC rejects the `finalized` tag. This profile explicitly selects the latest canonical header, binds all balances to that hash with EIP-1898 `requireCanonical: true`, then rechecks the exact height/hash before publishing rows. A changed block fails the whole query; there is no retry or finalized-to-latest fallback.

`FantomCommitment` is `latest-canonical-at-read`. This is an RPC observation, not permanent finality or an independent consensus proof. Each query chooses its own block; no shared snapshot across queries/providers is promised. Other EVM profiles retain their existing finalized policy.

Supported predicates are exact equality or bounded membership on `FantomAccountAddress`, with paging. The configured catalogue contains at most eight lowercase addresses. General Name/Description/content searches, aggregates, scans and native writes are not admitted. Balance and block-number metadata remain exact decimal strings.

## Create one reusable local catalogue

Use your initialized OASIS checkout with its existing .NET10 SDK/dependencies. Run the setup once and retain its owner/address file between runs:

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/FantomDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse the existing catalogue; do not replace its owner.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    rpcUrl = 'https://rpcapi.fantom.network/'
    chainId = '0xfa'
    addresses = @(1..3 | ForEach-Object { '0x' + $_.ToString('x40') })
    sourceOwner = [Guid]::NewGuid().ToString('D')
}

$source | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath $sourcePath -Encoding utf8NoBOM
```

The mapping owner is a stable identity, not a signing key. Configuration rejects credentials, extra fields and different chain identity. The block policy belongs to the provider descriptor; callers cannot change it through JSON.

## Build once, then query

Do not nest the repository build lease inside another lease:

```powershell
& ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner 'fantom-demo' -Operation {
    dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
        /m:2 /nr:false /p:UseSharedCompilation=false

    if ($LASTEXITCODE -ne 0) { throw 'Demo build failed.' }
}

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --fantom-public LocalState/COSMIC/FantomDemo/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/FantomPublicWalkthrough.cs) activates the explicit public source outside the canonical wallet registry. It calls [shared manager/LINQ code](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs), selecting all three addresses then two:

```csharp
var selected = catalogueAddresses.Take(2).ToArray();

var sourceQuery = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.FantomOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "FantomAccountAddress")))
    .Result;

var query = CosmicLinq.Create(manager, fantomSource, sourceQuery).Result;
var result = await query.ToCosmicListAsync(cancellationToken);
```

Inspect `OASISResult.IsError` before reading `Result` in application code. The reusable walkthrough checks every result. Console output shows each native balance, captured block hash and explicit commitment; it does not print a finalized claim.

## Remaining demo gates

Fantom is not yet enrolled in the hosted API or its free query tariff. HTTP/Mongo privacy, repeat-safe snapshots and replicas must pass before a live sandbox card is published. This local profile does not certify signed storage, replication from the chain, general ORM parity or Sonic chain146.

Primary contracts: [Fantom RPC endpoints](https://docs.fantom.foundation/build-on-opera/api/public-endpoints), [EIP-1898](https://eips.ethereum.org/EIPS/eip-1898), [Sonic Labs June2026 update retaining Opera](https://www.soniclabs.com/blog/the-first-1/).
