# Starknet public header CQL demo

Status: native adapter31controls, real native3/2and actual shared-manager C# console3/2pass. Local and hosted HTTP/Mongo federation, reuse, visitor privacy and SQLite/Neo4j replicas are verified. Browser interaction remains open. See [current evidence](../WIP/COSMIC%20ORM/STARKNET_PUBLIC_SOURCE_STATUS.md).

This anonymous read profile queries an explicit catalogue of up to eight historical block hashes. It is separate from the signed Starknet wallet/storage provider. Native key equality and membership, supported paging and public read access use the shared CQL planner. Arbitrary metadata/name predicates, scanning, writes, proof verification and general ORM support are outside this profile.

## Create one reusable catalogue

Run in your existing OASIS checkout. Keep this file and reuse it on later runs; the source owner participates in stable projected holon IDs. No wallet, account or key is required.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/StarknetDemo/source.json'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

    $configuration = @{
        schema = 1
        rpcUrl = 'https://starknet-rpc.publicnode.com/'
        genesisHash = '0x47c3637b57c2b079b93c61539950c17e868a28f46cdef28f88521067f21e943'
        blockHashes = @(
        "0x2a70fb03fe363a2d6be843343a1d81ce6abeda1e9bd5cc6ad8fa9f45e30fdeb",
        "0x4e1f77f39545afe866ac151ac908bd1a347a2a8a7d58bef1276db4f06fdf2f6",
        "0x3dc0a914e03f075189841f7e41ffcac0ec2c6d6b06c22def89149a51de39a74"
        )
        sourceOwner = [Guid]::NewGuid().ToString()
    }

    $configuration | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $sourcePath -Encoding utf8
}
```

## Run the C# walkthrough

Restore/build once in the existing environment; subsequent runs can execute the same built DLL. Repository agents must wrap producer/consumer builds in the documented build lease.

```powershell
dotnet restore ./Examples/CosmicCqlDemo/CosmicCqlDemo.csproj /m:2 /nr:false /p:UseSharedCompilation=false

dotnet build ./Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-restore /m:2 /nr:false /p:UseSharedCompilation=false

dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll --starknet-public $sourcePath
```

[StarknetPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/StarknetPublicWalkthrough.cs) activates the source and sends both selections through [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs), which demonstrates the shared HolonManager/LINQ path:

```csharp
// Use the source's enrolled hashes, rather than scanning the chain.
var selected = keys.Take(2).ToArray();

// The provider's declared capability admits a native metadata membership query.
var filtered = query.Where(row => selected.Contains(
    CosmicQueryMetadata.Value<string>(row, "StarknetBlockHash")));
```

Here `query` is the configured deferred query shown in NativeKeySetWalkthrough; the fragment illustrates its predicate, not standalone initialization.

## Exact representation and limits

- Fixed endpoint, Mainnet chain ID, RPC0.10.3 and pinned genesis are checked before selected records are published.
- Every returned block must match its requested hash and report ACCEPTED_ON_L1. Status is RPC-reported; this code does not independently verify consensus, STARK proofs or recompute block hashes.
- Hashes/state roots/sequencer values are canonical lowercase field-element strings below the Starknet field prime. Heights and timestamps use this profile's explicit nonnegative UInt64 domain and remain exact decimal strings; values above JavaScript's exact integer range are not rounded.
- Errors, cancellation, oversized responses or any invalid selected header publish no partial page. Unsupported capabilities fail explicitly. There is no endpoint substitution or automatic retry.
- This does not enroll a write/replication authority or create Mongo snapshots. The sandbox supplies native3/2and paired Mongo5/4presets; two private copies are reused on repeat setup. Only name, description and native key are copied into Mongo; complete native header metadata stays with the source read.

Contract references: [version-pinned OpenRPC](https://github.com/starkware-libs/starknet-specs/blob/v0.10.3/api/starknet_api_openrpc.json), [official field-element domain](https://www.starknet.io/cairo-book/ch02-02-data-types.html).
