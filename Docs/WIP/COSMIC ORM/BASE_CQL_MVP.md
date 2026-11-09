# Base CQL MVP

Updated 29 October 2026. **Local native/C#/HTTP/browser MVP verified.**

The central BootLoader Base registration constructs the shipped `BaseOASIS`
Web3Core class. Its inherited CQL path existed, but the shared compiler admitted
only Polygon/Arbitrum authorities. New Base controls reproduced two failures
before the fix; Base is now explicitly admitted with its own trusted catalogue.
No legacy provider path was changed.

## Supported scope

Captured ID equality/membership, boolean constants, AND/OR and paging select at
most 100 authorized native keys. Queries preserve Base authority, encrypted native
storage and identity/access validation. Unsupported metadata/order/graph shapes
reject before native execution; failures publish no partial rows. Selected Web3Core
reads use one observed block number, not a distributed snapshot or rollup-finality
guarantee. Common HolonManager federation powers the C# and HTTP teaching examples.

This is Base's provider implementation on **local Ganache chain31338**, not the
public Base network, OP Stack execution/finality or full ORM/HyperDrive parity.

## Verification

- Red baseline: Base2 failed/5 passed, expected authority rejection.
- Green controls: **21/21**, Base7 + changed-compiler Polygon7/Arbitrum7.
- Native: **1/1**, three records filtered to two. Original seed reused after full
  runner restart; nonce unchanged during query reads, with no new native writes.
- Paired SDK/HTTP: **1/1, zero skips**, 37s including interactive verification.
  All/subset return five/four physical rows with Base/Mongo provenance. Forged
  actor/unknown preset reject; inactive Base returns 503 without partial rows.
- Actual browser clicks verified both counts and source labels.
- Hub discovery: 8 single/16 plans, 15 paired/30 plans, 16 paired rejections,
  3 archive rejections; listeners unchanged.
- Exact owned Mongo fixture `cosmic_cql_mongo_base_demo` validated/dropped;
  pending/ready markers removed. Owned EVM flushed/stopped, Mongo/host stopped;
  ports18547/27029/5104 closed. No download/install/new runtime or ledger.

| Evidence | SHA256 |
|---|---|
| `TestResults/BaseCqlCurrent/base-cql-before.trx` | `2A1CB889373FF968A7C64A2D02218F305742999371116B5BED9B361050DD9211` |
| `TestResults/BaseCqlCurrent/base-family-controls.trx` | `7A1FB262B5A6E44A57917CEF41E95CB3319DC488E288A87B1688CADEB59B6598` |
| `TestResults/EthereumCqlCurrent/base-cql-native.trx` | `C37CB6FE0DDEB6B8369F0C98E34BA2B258536457A967CE066A382AA8196B65E8` |
| `TestResults/EthereumCqlCurrent/base-mongo-federation.trx` | `94104B03AF496DE4CA3C958DDF534D0FDFDC11F7C70C41D4791898AF4D9C5CCD` |
| `TestResults/BaseCqlCurrent/base-mongo-browser.jpg` | `85D55D3A18FA837E30A890A4ECEF7C06625BB6BE03EE0D91AF36F08C297EC28F` |

Original source manifest: `LocalState/COSMIC/EvmCql/Base/demo-seed.json`, 229 bytes,
SHA256 `18F6F3DE193D88B8103B4A9DA793B3DA5563E298017373D181FE4A11589F3284`.
Retain it, the original encrypted records/contract, shared ledger and protected key.
Uncertain setup must preserve its pending intent; never reset/reseed to hide it.

```powershell
# Interactive host: http://127.0.0.1:5104/.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Base -Federated

# Bounded native/SDK/HTTP verification and cleanup.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Base -Federated -VerifyOnly
```

Next: Avalanche MVP, then remaining red [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md)
rows. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) / [current status](CURRENT_STATUS.md).
