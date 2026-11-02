# Fantom Opera CQL: public balances

This read-only profile queries Fantom Opera, chain250 (`0xfa`). It uses the shared CQL planner, HolonManager and deferred LINQ. No wallet, funding, transaction or native source write is needed.

The local native/manager walkthrough and actual console return three records and a two-record subset. Local shipping WEB4 HTTP/Mongo now passes native3/2, paired5/4 and privacy0/3, with exact fixture cleanup. Hosted HTTP also passes native 3/2, Mongo pairing 5/4, repeat reuse, visitor isolation and SQLite/Neo4j replicas. Browser interaction remains open; see [the current report](../WIP/COSMIC%20ORM/FANTOM_PUBLIC_SOURCE_STATUS.md).

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

## HTTP shape on an explicitly enrolled host

Local verification exercises the shipping `api/data/query-cql` endpoint with this shape. Hosted Fantom must be marked ready before sending it to the public service:

```json
{
  "provider": "FantomOASIS",
  "orderBy": "None",
  "maximumDurationMilliseconds": 30000,
  "take": 8,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["FantomAccountAddress"],
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

A Mongo federation names both `MongoDBOASIS` and `FantomOASIS`, uses the same metadata filter as its source, and preserves copies. Its two private Mongo snapshots require the normal owned save/admission path. Local and hosted HTTP gates verify five/four physical rows and zero private rows visible to another actor. Hosted snapshots also replicate to SQLite and Neo4j with both IDs and version1 preserved. These snapshots use normal WEB4 saves; they do not write to Fantom or replicate chain transactions.

## Hosted demo and remaining scope

Select `FantomOASIS` in the [sandbox CQL tab](https://web4.oasisomniverse.one/web4-sandbox.html). Choose a public-record preset for three addresses or its two-address subset. Choose the Mongo paired preset and prepare two private snapshots once; repeated preparation reuses them. Native counts are 3/2 and paired counts are 5/4. No wallet, gas or account setup is required.

Hosted HTTP admission, repeat-safe snapshots, visitor privacy and two SQLite/two Neo4j replicas are verified. Browser interaction remains unverified. This profile does not certify signed storage, replication from the chain, general ORM parity or Sonic chain146.

Primary contracts: [Fantom RPC endpoints](https://docs.fantom.foundation/build-on-opera/api/public-endpoints), [EIP-1898](https://eips.ethereum.org/EIPS/eip-1898), [Sonic Labs June2026 update retaining Opera](https://www.soniclabs.com/blog/the-first-1/).
