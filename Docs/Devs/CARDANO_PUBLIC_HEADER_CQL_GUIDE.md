# Cardano public header CQL demo

Native CQL35controls and real3/2selection pass. Actual commented C# walkthrough also passes3/2through HolonManager/LINQ. Local API admission, HTTP/Mongo federation and visitor privacy are verified. Hosted sandbox admission and browser interaction remain pending. [Current proof](../WIP/COSMIC%20ORM/CARDANO_PUBLIC_SOURCE_STATUS.md).

This read-only profile selects explicitly enrolled historical block hashes from the public Mainnet Koios indexer. It uses the shared CQL planner and HolonManager/LINQ path, separate from signed Cardano wallet/storage settings. No wallet, account, funding or chain transaction is required.

## One reusable local configuration

Run in your existing OASIS checkout. Keep this file: its source owner participates in stable projected holon IDs. The three hashes are the operator's historical Mainnet examples, not a chain-wide scan.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/CardanoDemo/source.json'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

    $configuration = @{
        schema = 1
        restUrl = 'https://api.koios.rest/api/v1/'
        networkMagic = '764824073'
        blockHashes = @(
            "fb9087c9f1408a7bbd7b022fd294ab565fec8dd3a8ef091567482722a1fa4e30",
            "60188a8dcb6db0d80628815be2cf626c4d17cb3e826cebfca84adaff93ad492a",
            "c6646214a1f377aa461a0163c213fc6b86a559a2d6ebd647d54c4eb00aaab015"
        )
        sourceOwner = [Guid]::NewGuid().ToString()
    }

    $configuration | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $sourcePath -Encoding utf8
}
```

## Build once and run

Reuse your existing SDK/dependencies. Repository agents wrap producer/consumer builds in the existing build lease.

```powershell
dotnet restore ./Examples/CosmicCqlDemo/CosmicCqlDemo.csproj /m:2 /nr:false /p:UseSharedCompilation=false

dotnet build ./Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-restore /m:2 /nr:false /p:UseSharedCompilation=false

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll --cardano-public $sourcePath
```

[CardanoPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/CardanoPublicWalkthrough.cs) activates the read source and queries all three configured hashes, then a two-key subset. [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs) shows the complete shared manager/deferred-LINQ initialization and execution. Its predicate has this form:

```csharp
// Select existing native keys; do not scan or invent projection IDs.
var selected = keys.Take(2).ToArray();

var filtered = query.Where(row => selected.Contains(
    CosmicQueryMetadata.Value<string>(row, "CardanoBlockHash")));
```

Here `query` is the initialized deferred query in NativeKeySetWalkthrough; the fragment illustrates its filter.

## Supported scope and exact values

- One to eight unique lowercase64-digit hashes; native key equality/membership and admitted paging. Arbitrary Name/Description/other metadata predicates, numeric sorting and chain scans are not admitted by this profile.
- Every query checks fixed Mainnet endpoint and genesis-reported network identity. Missing, extra, duplicate or malformed selected rows reject the whole query, including an otherwise empty page.
- The indexer can reorder results. After complete-set validation, the profile restores captured requested-key order before paging. This does not implement arbitrary OrderBy expressions.
- Heights, slots, sizes, timestamps, counts and reported confirmations use the profile's exact nonnegative UInt64 token domain and remain decimal strings. Optional lovelace output/fee strings retain up to256decimal digits. Absent, null and zero stay distinct; no floating-point or decimal rounding.
- Historical records must report at least2160confirmations. This is an explicit profile threshold, not independently verified finality. Confirmation counts are per-response observations and may change; no shared snapshot or latency guarantee is claimed.
- Cancellation, response budget violations, API errors and unsupported capabilities return explicit errors without partial rows, retries or alternate endpoints. This profile cannot become a write/replication authority.

Contract: [operator endpoints](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/paths/block.yaml), [response fields](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/schemas/block.yaml), [pinned example catalogue](https://github.com/cardano-community/koios-artifacts/blob/2e8ad77681a29a9a3aca94617acacebbb85d8d59/specs/examples/fixtures.yaml). Source tests independently cover exact values, complete sets, ordering, nullable fields, malformed domains and cancellation.

## Live sandbox

The hosted HTTP flow is verified: choose Cardano public records or its two-key subset, prepare two private Mongo snapshots, then use the Mongo+Cardano presets (five/four physical rows). Repeating preparation reuses the two snapshots. Visitor isolation and matching SQLite/Neo4j replicas pass. This proves this bounded query/demo flow, not arbitrary metadata queries, owned Cardano writes or browser interaction. Card publication is tracked in the current status report.

[Live playground](https://web4.oasisomniverse.one/web4-sandbox.html) - [acceptance evidence and limits](../WIP/COSMIC%20ORM/CARDANO_PUBLIC_SOURCE_STATUS.md).
